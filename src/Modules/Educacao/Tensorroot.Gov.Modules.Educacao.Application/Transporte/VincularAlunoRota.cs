using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Application.Transporte;

/// <summary>
/// Vincula um aluno a uma rota de transporte, com o ponto de embarque. Reusa Aluno/Matricula por Id
/// (mesmo modulo): o aluno deve existir e estar Ativo (I-R3).
/// </summary>
/// <param name="RotaId">Rota alvo.</param>
/// <param name="AlunoId">Aluno (por Id).</param>
/// <param name="MatriculaId">Matricula vigente (opcional, por Id).</param>
/// <param name="PontoEmbarque">Ponto de embarque.</param>
public sealed record VincularAlunoRotaCommand(
    Guid RotaId,
    Guid AlunoId,
    Guid? MatriculaId,
    string PontoEmbarque) : ICommand<Guid>;

/// <summary>Regras de validacao do vinculo de aluno a rota.</summary>
public sealed class VincularAlunoRotaValidator : AbstractValidator<VincularAlunoRotaCommand>
{
    /// <summary>Define as regras.</summary>
    public VincularAlunoRotaValidator()
    {
        RuleFor(comando => comando.RotaId).NotEmpty().WithMessage("Rota obrigatoria.");
        RuleFor(comando => comando.AlunoId).NotEmpty().WithMessage("Aluno obrigatorio.");
        RuleFor(comando => comando.PontoEmbarque)
            .NotEmpty().MaximumLength(AlunoTransportado.ComprimentoPonto).WithMessage("Ponto de embarque obrigatorio.");
    }
}

/// <summary>Handler do vinculo de aluno a rota.</summary>
public sealed class VincularAlunoRotaHandler(
    IRotaTransporteRepository rotas,
    IAlunoRepository alunos,
    IMatriculaRepository matriculas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<VincularAlunoRotaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(VincularAlunoRotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rota = await rotas.ObterPorIdAsync(new RotaTransporteId(request.RotaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rota nao encontrada.");

        var alunoId = new AlunoId(request.AlunoId);
        var aluno = await alunos.ObterPorIdAsync(alunoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aluno nao encontrado.");
        if (!aluno.Ativo)
        {
            throw new InvalidOperationException("Aluno em situacao terminal nao pode ser transportado.");
        }

        MatriculaId? matriculaId = null;
        if (request.MatriculaId is { } valor)
        {
            var matricula = await matriculas.ObterPorIdAsync(new MatriculaId(valor), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Matricula nao encontrada.");
            if (matricula.AlunoId != alunoId)
            {
                throw new InvalidOperationException("Matricula informada nao pertence ao aluno.");
            }

            matriculaId = matricula.Id;
        }

        var vinculoId = rota.VincularAluno(alunoId, matriculaId, request.PontoEmbarque);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return vinculoId.Value;
    }
}
