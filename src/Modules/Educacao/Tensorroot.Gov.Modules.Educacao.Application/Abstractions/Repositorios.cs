using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

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
    /// Lista as matriculas Ativas de uma turma (independe da data de referencia do Censo) —
    /// base do lancamento em lote do diario coletivo da turma (sub-onda 3a). Tenant-scoped.
    /// </summary>
    /// <param name="turmaId">Turma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Matriculas Ativas da turma.</returns>
    Task<IReadOnlyList<Matricula>> ListarAtivasPorTurmaAsync(
        TurmaId turmaId,
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
/// Repositorio do agregado <see cref="Turma"/> (mesmo modulo Educacao). Onda 1: passa a materializar
/// a Turma real — a verificacao de vaga (I-T2) consulta o agregado (contagem matriculados &lt; vagas),
/// substituindo o stub historico (<c>id != Guid.Empty</c>). O contador <c>Matriculados</c> e mantido
/// pelo proprio agregado na mesma transacao da matricula.
/// </summary>
public interface ITurmaRepository
{
    /// <summary>Marca uma nova turma para insercao.</summary>
    /// <param name="turma">Turma a adicionar.</param>
    void Adicionar(Turma turma);

    /// <summary>Obtem uma turma por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A turma, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Turma?> ObterPorIdAsync(TurmaId id, CancellationToken cancellationToken);

    /// <summary>Indica se a turma existe, esta aberta e possui vaga disponivel (I-T2) — sem carregar o agregado.</summary>
    /// <param name="turmaId">Turma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a turma existe, esta aberta e tem vaga.</returns>
    Task<bool> PossuiVagaAsync(TurmaId turmaId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se ja existe turma com a mesma combinacao (escola, ano letivo, serie, turno) no tenant (I-T4).
    /// </summary>
    /// <param name="escolaId">Escola.</param>
    /// <param name="anoLetivo">Ano letivo.</param>
    /// <param name="serie">Serie/ano.</param>
    /// <param name="turno">Turno.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a turma identica ja existir.</returns>
    Task<bool> ExisteDuplicadaAsync(
        Tensorroot.Gov.Modules.Educacao.Domain.Escolas.EscolaId escolaId,
        int anoLetivo,
        string serie,
        Turno turno,
        CancellationToken cancellationToken);

    /// <summary>Busca paginada de turmas por escola/ano/turno/etapa (navegabilidade — picker do front).</summary>
    /// <param name="escolaId">Filtro opcional por escola.</param>
    /// <param name="anoLetivo">Filtro opcional por ano letivo.</param>
    /// <param name="turno">Filtro opcional por turno.</param>
    /// <param name="etapa">Filtro opcional por etapa.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Par (itens da pagina, total).</returns>
    Task<(IReadOnlyList<Turma> Itens, int Total)> BuscarAsync(
        Tensorroot.Gov.Modules.Educacao.Domain.Escolas.EscolaId? escolaId,
        int? anoLetivo,
        Turno? turno,
        Etapa? etapa,
        SituacaoTurma? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}

/// <summary>Repositorio do agregado <see cref="Aluno"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IAlunoRepository
{
    /// <summary>Marca um novo aluno para insercao.</summary>
    /// <param name="aluno">Aluno a adicionar.</param>
    void Adicionar(Aluno aluno);

    /// <summary>Obtem um aluno por identificador (com os responsaveis carregados).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O aluno, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Aluno?> ObterPorIdAsync(AlunoId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe aluno com o CPF informado no tenant (I-A4).</summary>
    /// <param name="cpf">CPF (somente digitos).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o CPF ja estiver cadastrado.</returns>
    Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken);

    /// <summary>Busca paginada de alunos por nome/CPF/data de nascimento (picker do front).</summary>
    /// <param name="termo">Termo livre (nome ou CPF); nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Par (itens da pagina, total).</returns>
    Task<(IReadOnlyList<Aluno> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoAluno? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
