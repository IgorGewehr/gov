using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Educacao.Application.Fiscal;

/// <summary>
/// Execução fiscal de Educação de um exercício, pronta para o <see cref="ApuradorMde"/>: a receita-base
/// do mínimo (CF art. 212) e as despesas de Educação (ainda a classificar como MDE/não-MDE na apuração).
/// </summary>
/// <param name="Exercicio">Ano de exercício.</param>
/// <param name="ReceitaBaseImpostosTransferencias">Receita-base do mínimo (impostos + transferências constitucionais).</param>
/// <param name="DespesasEducacao">Despesas executadas na função Educação (funcional/fonte/valor).</param>
public sealed record ExecucaoEducacao(
    int Exercicio,
    decimal ReceitaBaseImpostosTransferencias,
    IReadOnlyList<DespesaEducacao> DespesasEducacao);

/// <summary>
/// <b>E-1 — abstração de fonte de dados de execução de Educação.</b> Desacopla o <see cref="ApuradorMde"/>
/// da via usada para obter os dados (Via A2 hoje: deriva das linhas de execução projetadas da
/// contabilidade; Via A1 amanhã: dos campos decompostos já carimbados no contrato de Finanças). A troca
/// A2→A1 NÃO toca o domínio nem o apurador — só a implementação desta porta. Espelha o
/// <c>IExecucaoSaudeReadModel</c> da Saúde.
/// </summary>
public interface IExecucaoEducacaoReadModel
{
    /// <summary>Obtém a execução de Educação do exercício (tenant-scoped).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Execução de Educação, ou base zerada quando não há dados.</returns>
    Task<ExecucaoEducacao> ObterExecucaoAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// Provedor do percentual mínimo MDE vigente (default legal 25% — CF art. 212; a Lei Orgânica municipal
/// pode fixar maior). Parametrizável por tenant+vigência — nunca hardcoded (CLAUDE.md §16).
/// </summary>
public interface IParametroMdeProvider
{
    /// <summary>Obtém o percentual mínimo MDE vigente no exercício (0..1).</summary>
    /// <param name="exercicio">Ano de exercício (âncora de vigência, reprodutível).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Percentual mínimo (0..1).</returns>
    Task<decimal> ObterPercentualMinimoAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// Provedor do piso vigente de aplicação do FUNDEB na remuneração dos profissionais da educação (default
/// legal 70% — EC 108/2020). Parametrizável por tenant+vigência — nunca hardcoded (CLAUDE.md §16).
/// </summary>
public interface IParametroFundebProvider
{
    /// <summary>Obtém o piso vigente de aplicação do FUNDEB no exercício (0..1).</summary>
    /// <param name="exercicio">Ano de exercício (âncora de vigência, reprodutível).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Piso mínimo (0..1).</returns>
    Task<decimal> ObterPisoRemuneracaoAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório das regras de classificação MDE (<see cref="RegraClassificacaoMde"/>).</summary>
public interface IRegraClassificacaoMdeRepository
{
    /// <summary>Adiciona uma regra de classificação MDE.</summary>
    /// <param name="regra">Regra a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(RegraClassificacaoMde regra, CancellationToken cancellationToken);

    /// <summary>Indica se já existe ao menos uma regra MDE para o tenant (para o seed default).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se há regras.</returns>
    Task<bool> ExisteAlgumaAsync(CancellationToken cancellationToken);

    /// <summary>Lista as regras vigentes em uma data de referência (vigência ≤ referência), tenant-scoped.</summary>
    /// <param name="referencia">Data de referência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Regras vigentes do tenant.</returns>
    Task<IReadOnlyList<RegraClassificacaoMde>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken);
}

/// <summary>Natureza de uma linha de execução de Educação a projetar (Via A2).</summary>
public enum TipoLinhaExecucaoEducacao
{
    /// <summary>Receita-base do mínimo (impostos + transferências constitucionais).</summary>
    ReceitaBaseImpostosTransferencias = 1,

    /// <summary>Despesa executada na função Educação (a classificar MDE/não-MDE na leitura).</summary>
    DespesaEducacao = 2,
}

/// <summary>
/// <b>E-1 — porta de escrita da projeção de execução de Educação (Via A2).</b> Recebe linhas já decompostas
/// (funcional/subfunção/fonte/valor) que o <see cref="IExecucaoEducacaoReadModel"/> lê. É o ponto de entrada
/// alimentado por um ACL que consome a contabilidade (Finanças.Contracts); enquanto o alimentador
/// automático não existe, também é como a execução sintética entra. A entidade de persistência fica na
/// Infraestrutura (isolamento de camadas, §2) — a porta trafega só primitivos. Idempotente por OrigemHash.
/// Espelha o <c>ILinhaExecucaoSaudeRepository</c> da Saúde.
/// </summary>
public interface ILinhaExecucaoEducacaoRepository
{
    /// <summary>Indica se já existe uma linha com a origem informada (idempotência).</summary>
    /// <param name="origemHash">Hash determinístico da origem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já projetada.</returns>
    Task<bool> ExisteAsync(string origemHash, CancellationToken cancellationToken);

    /// <summary>Projeta uma linha de receita-base (impostos + transferências constitucionais).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="valor">Valor da receita (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarReceitaBaseAsync(int exercicio, decimal valor, string origemHash, CancellationToken cancellationToken);

    /// <summary>Projeta uma linha de despesa de Educação (a classificar por funcional/fonte na leitura).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="funcao">Função de governo (2 dígitos).</param>
    /// <param name="subfuncao">Subfunção (3 dígitos), opcional — chave da classificação MDE fina.</param>
    /// <param name="fonteRecurso">Fonte de recurso, opcional.</param>
    /// <param name="valor">Valor executado (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarDespesaAsync(int exercicio, string funcao, string? subfuncao, string? fonteRecurso, decimal valor, string origemHash, CancellationToken cancellationToken);
}

/// <summary>Repositório da <see cref="DistribuicaoFundeb"/> (E-3, raiz de agregado).</summary>
public interface IDistribuicaoFundebRepository
{
    /// <summary>Adiciona uma nova distribuição.</summary>
    /// <param name="distribuicao">Distribuição a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(DistribuicaoFundeb distribuicao, CancellationToken cancellationToken);

    /// <summary>Obtém a distribuição por identificador (com suas contas por origem), tenant-scoped.</summary>
    /// <param name="id">Identificador da distribuição.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A distribuição, ou <c>null</c> se inexistente no tenant.</returns>
    Task<DistribuicaoFundeb?> ObterPorIdAsync(DistribuicaoFundebId id, CancellationToken cancellationToken);

    /// <summary>Obtém a distribuição do exercício (tenant-scoped), ou <c>null</c> se inexistente.</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A distribuição do exercício, ou <c>null</c>.</returns>
    Task<DistribuicaoFundeb?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// <b>E-2 — porta de leitura da remuneração dos profissionais da educação custeada com FUNDEB.</b>
/// Desacopla o cálculo do piso de 70% da fonte do dado: hoje, o total chega da folha do RH (via Contracts,
/// como a MSC/remessa-folha) OU é informado pelo município como parâmetro; amanhã, do cruzamento
/// automático. A troca de fonte NÃO toca o domínio. Tenant-scoped, reprodutível por exercício.
/// </summary>
public interface IRemuneracaoMagisterioReadModel
{
    /// <summary>Obtém o total de remuneração dos profissionais da educação no exercício (0 se ausente).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Total pago a profissionais da educação básica (numerador dos 70%).</returns>
    Task<decimal> ObterRemuneracaoProfissionaisAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>
    /// Define (idempotente por exercício) o total de remuneração dos profissionais da educação do
    /// exercício — alimentado pela folha do RH (Contracts) ou informado como parâmetro pelo município.
    /// </summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="remuneracaoProfissionais">Total pago a profissionais da educação básica (&gt;= 0).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task DefinirRemuneracaoProfissionaisAsync(int exercicio, decimal remuneracaoProfissionais, CancellationToken cancellationToken);
}
