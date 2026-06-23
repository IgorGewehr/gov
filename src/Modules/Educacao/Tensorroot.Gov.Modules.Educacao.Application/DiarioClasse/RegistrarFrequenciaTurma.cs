using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Presenca de um aluno (por matricula) na chamada coletiva do dia.</summary>
/// <param name="MatriculaId">Matricula (aluno) da turma.</param>
/// <param name="Presente">Presenca do aluno na aula do dia.</param>
public sealed record FrequenciaAlunoLote(Guid MatriculaId, bool Presente);

/// <summary>
/// Chamada em lote do diario coletivo da turma (sub-onda 3a): lanca a frequencia de um dia para os
/// alunos informados, iterando o diario 1-1 de cada matricula (<c>DiarioClasse.RegistrarFrequencia</c>)
/// na mesma transacao. Orquestracao sobre os agregados existentes — sem entidade de dominio nova.
/// </summary>
/// <param name="TurmaId">Turma da chamada.</param>
/// <param name="Data">Data da aula/chamada.</param>
/// <param name="CargaHorariaAula">Carga horaria da aula do dia (ponderacao da frequencia — I-1).</param>
/// <param name="Presencas">Presencas dos alunos (uma por matricula).</param>
public sealed record RegistrarFrequenciaTurmaCommand(
    Guid TurmaId,
    DateOnly Data,
    int CargaHorariaAula,
    IReadOnlyList<FrequenciaAlunoLote> Presencas) : ICommand<int>;

/// <summary>Regras de validacao da chamada em lote.</summary>
public sealed class RegistrarFrequenciaTurmaValidator : AbstractValidator<RegistrarFrequenciaTurmaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarFrequenciaTurmaValidator()
    {
        RuleFor(comando => comando.TurmaId).NotEmpty().WithMessage("Turma obrigatoria.");
        RuleFor(comando => comando.Data).NotEmpty().WithMessage("Data da chamada obrigatoria.");
        RuleFor(comando => comando.CargaHorariaAula).GreaterThan(0).WithMessage("Carga horaria da aula deve ser maior que zero.");
        RuleFor(comando => comando.Presencas).NotEmpty().WithMessage("Informe ao menos uma presenca.");
        RuleForEach(comando => comando.Presencas)
            .ChildRules(presenca => presenca.RuleFor(p => p.MatriculaId).NotEmpty().WithMessage("Matricula obrigatoria."));
    }
}

/// <summary>
/// Handler da chamada em lote. So registra a frequencia das matriculas que sao Ativas na turma E ja
/// possuem diario aberto; ignora silenciosamente as presencas de matriculas fora desse conjunto (a
/// abertura do diario e operacao explicita previa — <c>AbrirDiarioClasse</c>). Retorna quantos diarios
/// receberam o lancamento.
/// </summary>
public sealed class RegistrarFrequenciaTurmaHandler(
    IMatriculaRepository matriculas,
    IDiarioClasseRepository diarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarFrequenciaTurmaCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(RegistrarFrequenciaTurmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ativas = await matriculas
            .ListarAtivasPorTurmaAsync(new TurmaId(request.TurmaId), cancellationToken)
            .ConfigureAwait(false);

        // Apenas as matriculas Ativas da turma que tambem constam na chamada enviada.
        var solicitadas = request.Presencas.ToDictionary(p => p.MatriculaId, p => p.Presente);
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

        var lancados = 0;
        foreach (var diario in diariosDosAlvos)
        {
            if (diario.Situacao != SituacaoDiario.Aberto)
            {
                continue; // diario ja apurado nao recebe novos lancamentos.
            }

            diario.RegistrarFrequencia(request.Data, solicitadas[diario.MatriculaId.Value], request.CargaHorariaAula);
            lancados++;
        }

        if (lancados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return lancados;
    }
}
