using Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

namespace Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

/// <summary>
/// Read model do diario coletivo, boletim e historico escolar (sub-onda 3a). Projeta sobre os
/// agregados ja existentes do proprio modulo (Turma, Matricula, Aluno e o diario 1-1 por matricula),
/// sem entidade de dominio nova. Todas as consultas sao read-only e tenant-scoped via Global Query
/// Filter do DbContext de Educacao.
/// </summary>
public interface IDiarioTurmaReadModel
{
    /// <summary>
    /// Monta a grade de chamada do diario coletivo da turma em uma data: cabecalho + uma linha por
    /// aluno com matricula Ativa na turma, com a presenca daquele dia (quando lancada).
    /// </summary>
    /// <param name="turmaId">Turma.</param>
    /// <param name="data">Data da chamada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O diario da turma, ou <c>null</c> se a turma nao existir no tenant.</returns>
    Task<DiarioTurmaView?> ObterDiarioDaTurmaAsync(Guid turmaId, DateOnly data, CancellationToken cancellationToken);

    /// <summary>Monta o boletim de uma matricula (medias por componente, frequencia e resultado).</summary>
    /// <param name="matriculaId">Matricula.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O boletim, ou <c>null</c> se a matricula nao existir no tenant.</returns>
    Task<BoletimAlunoView?> ObterBoletimAsync(Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>Monta o historico escolar longitudinal do aluno (serie de matriculas/diarios).</summary>
    /// <param name="alunoId">Aluno.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O historico (vazio quando o aluno nao tem matriculas no tenant).</returns>
    Task<HistoricoEscolarView> ObterHistoricoEscolarAsync(Guid alunoId, CancellationToken cancellationToken);
}
