using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Nota de um aluno (por matricula) na grade de lancamento coletivo de notas.</summary>
/// <param name="MatriculaId">Matricula (aluno) da turma.</param>
/// <param name="Valor">Valor da nota (0 a 10).</param>
public sealed record NotaAlunoLote(Guid MatriculaId, decimal Valor);

/// <summary>
/// Lancamento de notas em lote do diario coletivo da turma (sub-onda 3a): lanca a nota de um
/// componente curricular em um periodo para os alunos informados, iterando o diario 1-1 de cada
/// matricula (<c>DiarioClasse.LancarNota</c>) na mesma transacao. Orquestracao sobre os agregados
/// existentes — sem entidade de dominio nova.
/// </summary>
/// <param name="TurmaId">Turma do lancamento.</param>
/// <param name="ComponenteCurricularId">Componente curricular (FK logica — catalogo e P3/M10).</param>
/// <param name="Periodo">Periodo de avaliacao (ex.: "1Bim").</param>
/// <param name="Notas">Notas dos alunos (uma por matricula).</param>
public sealed record LancarNotasTurmaCommand(
    Guid TurmaId,
    Guid ComponenteCurricularId,
    string Periodo,
    IReadOnlyList<NotaAlunoLote> Notas) : ICommand<int>;

/// <summary>Regras de validacao do lancamento de notas em lote.</summary>
public sealed class LancarNotasTurmaValidator : AbstractValidator<LancarNotasTurmaCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarNotasTurmaValidator()
    {
        RuleFor(comando => comando.TurmaId).NotEmpty().WithMessage("Turma obrigatoria.");
        RuleFor(comando => comando.ComponenteCurricularId).NotEmpty().WithMessage("Componente curricular obrigatorio.");
        RuleFor(comando => comando.Periodo).NotEmpty().MaximumLength(20).WithMessage("Periodo obrigatorio (max. 20 caracteres).");
        RuleFor(comando => comando.Notas).NotEmpty().WithMessage("Informe ao menos uma nota.");
        RuleForEach(comando => comando.Notas).ChildRules(nota =>
        {
            nota.RuleFor(n => n.MatriculaId).NotEmpty().WithMessage("Matricula obrigatoria.");
            nota.RuleFor(n => n.Valor).InclusiveBetween(0, 10).WithMessage("Nota deve estar entre 0 e 10.");
        });
    }
}

/// <summary>
/// Handler do lancamento de notas em lote. So lanca para as matriculas Ativas na turma que ja
/// possuem diario aberto; ignora as demais. Retorna quantos diarios receberam o lancamento.
/// </summary>
public sealed class LancarNotasTurmaHandler(
    IMatriculaRepository matriculas,
    IDiarioClasseRepository diarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LancarNotasTurmaCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(LancarNotasTurmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ativas = await matriculas
            .ListarAtivasPorTurmaAsync(new TurmaId(request.TurmaId), cancellationToken)
            .ConfigureAwait(false);

        var solicitadas = request.Notas.ToDictionary(n => n.MatriculaId, n => n.Valor);
        var alvos = ativas
            .Where(matricula => solicitadas.ContainsKey(matricula.Id.Value))
            .Select(matricula => matricula.Id)
            .ToList();

        if (alvos.Count == 0)
        {
            return 0;
        }

        var diariosDosAlvos = await diarios
            .ListarPorMatriculasAsync(alvos, cancellationToken)
            .ConfigureAwait(false);

        var componente = new ComponenteCurricularId(request.ComponenteCurricularId);
        var lancados = 0;
        foreach (var diario in diariosDosAlvos)
        {
            if (diario.Situacao != SituacaoDiario.Aberto)
            {
                continue;
            }

            diario.LancarNota(componente, request.Periodo, solicitadas[diario.MatriculaId.Value]);
            lancados++;
        }

        if (lancados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return lancados;
    }
}
