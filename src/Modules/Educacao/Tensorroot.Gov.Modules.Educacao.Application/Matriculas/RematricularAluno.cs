using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>
/// Rematricula: renova a matricula de um aluno apto, criando uma NOVA matricula Ativa para a
/// turma destino no ano seguinte (nao e transicao da matricula anterior).
/// </summary>
/// <param name="MatriculaAnteriorId">Matricula anterior (base da aptidao do aluno).</param>
/// <param name="TurmaDestinoId">Turma destino da rematricula.</param>
/// <param name="DataReferencia">Data de referencia do Censo (Matricula Inicial do ano seguinte).</param>
public sealed record RematricularAlunoCommand(
    Guid MatriculaAnteriorId,
    Guid TurmaDestinoId,
    DateOnly DataReferencia) : ICommand<Guid>;

/// <summary>Handler da rematricula.</summary>
public sealed class RematricularAlunoHandler(
    IMatriculaRepository matriculas,
    ITurmaRepository turmas,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RematricularAlunoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RematricularAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var anterior = await matriculas.ObterPorIdAsync(new MatriculaId(request.MatriculaAnteriorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Matricula nao encontrada.");

        // B-8: aluno apto ao ano seguinte (etapa concluida ou regra de aprovacao da Situacao do Aluno).
        if (!AlunoApto(anterior))
        {
            throw new InvalidOperationException("Aluno inapto a rematricula.");
        }

        var turmaDestinoId = new TurmaId(request.TurmaDestinoId);

        // I-T2: turma destino existente, aberta e com vaga (carrega o agregado real).
        var turmaDestino = await turmas.ObterPorIdAsync(turmaDestinoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Turma destino nao encontrada.");

        // I-3: aluno sem matricula ativa conflitante na data de referencia.
        if (await matriculas.ExisteMatriculaAtivaConflitanteAsync(anterior.AlunoId, request.DataReferencia, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Aluno ja possui matricula ativa conflitante.");
        }

        // I-T2: a enturmacao na turma destino incrementa o contador (valida turma Aberta com vaga).
        turmaDestino.IncrementarMatriculados();

        var novaMatricula = Matricula.MatricularAluno(
            tenant.TenantId,
            anterior.AlunoId,
            turmaDestinoId,
            turmaDestino.EscolaId,
            request.DataReferencia);

        matriculas.Adicionar(novaMatricula);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AlunoMatriculadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            novaMatricula.Id.Value,
            novaMatricula.AlunoId.Value,
            novaMatricula.TurmaId.Value,
            novaMatricula.EscolaId.Value);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return novaMatricula.Id.Value;
    }

    private static bool AlunoApto(Matricula anterior)
        => anterior.Situacao == SituacaoMatricula.Concluida
        || anterior.SituacaoDoAluno?.Rendimento == Rendimento.Aprovado;
}
