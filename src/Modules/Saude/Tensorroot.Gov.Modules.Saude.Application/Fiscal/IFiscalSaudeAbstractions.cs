using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Saude.Application.Fiscal;

/// <summary>
/// Execução fiscal de Saúde de um exercício, pronta para o <see cref="ApuradorAsps"/>: a receita-base
/// do mínimo e as despesas de Saúde (ainda a classificar como ASPS/não-ASPS na apuração).
/// </summary>
/// <param name="Exercicio">Ano de exercício.</param>
/// <param name="ReceitaBaseImpostosTransferencias">Receita-base do mínimo (impostos + transferências constitucionais).</param>
/// <param name="DespesasSaude">Despesas executadas na função Saúde (funcional/fonte/valor).</param>
public sealed record ExecucaoSaude(
    int Exercicio,
    decimal ReceitaBaseImpostosTransferencias,
    IReadOnlyList<DespesaSaude> DespesasSaude);

/// <summary>
/// <b>S-1 — abstração de fonte de dados de execução de Saúde.</b> Desacopla o <see cref="ApuradorAsps"/>
/// da via usada para obter os dados (Via A2 hoje: deriva das linhas de execução projetadas da
/// contabilidade; Via A1 amanhã: dos campos decompostos já carimbados no contrato de Finanças). A troca
/// A2→A1 NÃO toca o domínio nem o apurador — só a implementação desta porta.
/// </summary>
public interface IExecucaoSaudeReadModel
{
    /// <summary>Obtém a execução de Saúde do exercício (tenant-scoped).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Execução de Saúde, ou base zerada quando não há dados.</returns>
    Task<ExecucaoSaude> ObterExecucaoAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// Provedor do percentual mínimo ASPS vigente (default legal 15% — LC 141/2012; a Lei Orgânica
/// municipal pode fixar maior). Parametrizável por tenant+vigência — nunca hardcoded (CLAUDE.md §16).
/// </summary>
public interface IParametroAspsProvider
{
    /// <summary>Obtém o percentual mínimo ASPS vigente no exercício (0..1).</summary>
    /// <param name="exercicio">Ano de exercício (âncora de vigência, reprodutível).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Percentual mínimo (0..1).</returns>
    Task<decimal> ObterPercentualMinimoAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório das regras de classificação ASPS (<see cref="RegraClassificacaoAsps"/>).</summary>
public interface IRegraClassificacaoAspsRepository
{
    /// <summary>Adiciona uma regra de classificação ASPS.</summary>
    /// <param name="regra">Regra a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(RegraClassificacaoAsps regra, CancellationToken cancellationToken);

    /// <summary>Indica se já existe ao menos uma regra ASPS para o tenant (para o seed default).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se há regras.</returns>
    Task<bool> ExisteAlgumaAsync(CancellationToken cancellationToken);

    /// <summary>Lista as regras vigentes em uma data de referência (vigência ≤ referência), tenant-scoped.</summary>
    /// <param name="referencia">Data de referência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Regras vigentes do tenant.</returns>
    Task<IReadOnlyList<RegraClassificacaoAsps>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken);
}

/// <summary>Natureza de uma linha de execução de Saúde a projetar (Via A2).</summary>
public enum TipoLinhaExecucaoSaude
{
    /// <summary>Receita-base do mínimo (impostos + transferências constitucionais).</summary>
    ReceitaBaseImpostosTransferencias = 1,

    /// <summary>Despesa executada na função Saúde (a classificar ASPS/não-ASPS na leitura).</summary>
    DespesaSaude = 2,
}

/// <summary>
/// <b>S-1 — porta de escrita da projeção de execução de Saúde (Via A2).</b> Recebe linhas já decompostas
/// (funcional/subfunção/fonte/valor) que o <see cref="IExecucaoSaudeReadModel"/> lê. É o ponto de entrada
/// alimentado por um ACL que consome a contabilidade (Finanças.Contracts); enquanto o alimentador
/// automático não existe, também é como a execução sintética entra. A entidade de persistência fica na
/// Infraestrutura (isolamento de camadas, §2) — a porta trafega só primitivos. Idempotente por OrigemHash.
/// </summary>
public interface ILinhaExecucaoSaudeRepository
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

    /// <summary>Projeta uma linha de despesa de Saúde (a classificar por funcional/fonte na leitura).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="funcao">Função de governo (2 dígitos).</param>
    /// <param name="subfuncao">Subfunção (3 dígitos), opcional — chave da classificação ASPS fina.</param>
    /// <param name="fonteRecurso">Fonte de recurso, opcional.</param>
    /// <param name="valor">Valor executado (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarDespesaAsync(int exercicio, string funcao, string? subfuncao, string? fonteRecurso, decimal valor, string origemHash, CancellationToken cancellationToken);
}

/// <summary>Repositório do <see cref="FundoMunicipalSaude"/> (FMS, raiz de agregado).</summary>
public interface IFundoMunicipalSaudeRepository
{
    /// <summary>Adiciona um novo fundo.</summary>
    /// <param name="fundo">Fundo a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(FundoMunicipalSaude fundo, CancellationToken cancellationToken);

    /// <summary>Obtém o fundo por identificador (com suas contas por bloco), tenant-scoped.</summary>
    /// <param name="id">Identificador do fundo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O fundo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<FundoMunicipalSaude?> ObterPorIdAsync(FundoMunicipalSaudeId id, CancellationToken cancellationToken);
}
