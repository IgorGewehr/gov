using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Matricula"/>.</summary>
public interface IMatriculaRepository
{
    /// <summary>Marca uma nova matricula para insercao.</summary>
    /// <param name="matricula">Matricula a adicionar.</param>
    void Adicionar(Matricula matricula);

    /// <summary>Obtem uma matricula por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A matricula, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Matricula?> ObterPorIdAsync(MatriculaId id, CancellationToken cancellationToken);

    /// <summary>Lista as matriculas de um aluno (sempre tenant-scoped via Global Query Filter).</summary>
    /// <param name="alunoId">Aluno.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Matriculas do aluno.</returns>
    Task<IReadOnlyList<Matricula>> ListarPorAlunoAsync(AlunoId alunoId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista a Matricula Inicial de uma turma na data de referencia do Censo
    /// (sempre tenant-scoped via Global Query Filter).
    /// </summary>
    /// <param name="turmaId">Turma.</param>
    /// <param name="dataReferencia">Data de referencia do Censo (Matricula Inicial).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Matriculas da turma na data de referencia.</returns>
    Task<IReadOnlyList<Matricula>> ListarPorTurmaEDataReferenciaAsync(
        TurmaId turmaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken);

    /// <summary>
    /// Indica se o aluno ja possui matricula Ativa conflitante (mesmo periodo/turno) na data de
    /// referencia (I-3 — vedacao de vinculo conflitante).
    /// </summary>
    /// <param name="alunoId">Aluno.</param>
    /// <param name="dataReferencia">Data de referencia do Censo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir matricula ativa conflitante.</returns>
    Task<bool> ExisteMatriculaAtivaConflitanteAsync(
        AlunoId alunoId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken);
}

/// <summary>
/// Repositorio de leitura da Turma (agregado de outro contexto do modulo Educacao), usado pelas
/// operacoes de matricula para verificar a invariante de vagas (I-11: matriculados &lt;= vagas).
/// </summary>
public interface ITurmaRepository
{
    /// <summary>
    /// Indica se a turma existe e possui vaga disponivel (<c>matriculados &lt; vagas</c>) — I-11.
    /// </summary>
    /// <param name="turmaId">Turma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a turma existe e tem vaga.</returns>
    Task<bool> PossuiVagaAsync(TurmaId turmaId, CancellationToken cancellationToken);
}
