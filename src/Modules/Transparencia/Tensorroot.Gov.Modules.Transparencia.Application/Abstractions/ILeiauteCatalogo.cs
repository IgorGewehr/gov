using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Catálogo de leiautes suportados (Resolução TCE-RS vigente) e derivação do prazo legal por tenant.
/// Garante que o leiaute/versão seja suportado, RESOLVE a grade posicional versionada do exercício
/// (dirigida por dados — CLAUDE.md §7/§16, nunca <i>hardcoded</i> em C#) e que a <c>DataLimite</c> seja
/// parametrizável por tenant (I-11).
/// </summary>
public interface ILeiauteCatalogo
{
    /// <summary>Indica se o leiaute/versão informado é suportado pelo catálogo vigente.</summary>
    /// <param name="leiaute">Leiaute versionado a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se suportado.</returns>
    Task<bool> SuportaAsync(Leiaute leiaute, CancellationToken cancellationToken);

    /// <summary>
    /// Resolve a grade posicional completa (<see cref="LeiauteSiapc"/>) do leiaute/versão, a partir da
    /// configuração versionada por exercício (JSON/seed na Infrastructure). Lança se não suportado.
    /// </summary>
    /// <param name="leiaute">Leiaute versionado a resolver.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A definição posicional do leiaute.</returns>
    Task<LeiauteSiapc> ResolverAsync(Leiaute leiaute, CancellationToken cancellationToken);

    /// <summary>Deriva a data-limite legal de envio da remessa do período, parametrizada por tenant.</summary>
    /// <param name="periodo">Período (competência/exercício) da remessa.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Data-limite de envio.</returns>
    Task<DateOnly> ObterDataLimiteAsync(Periodo periodo, CancellationToken cancellationToken);

    /// <summary>
    /// Dados de identificação do ente (CNPJ) e o próximo Código da Remessa para compor o nome do ZIP e o
    /// cabeçalho — parametrizados por tenant/exercício (o Código da Remessa é gerado pelo PRÓPRIO ENTE).
    /// </summary>
    /// <param name="periodo">Período da remessa.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificação do ente para a remessa.</returns>
    Task<IdentificacaoEnteRemessa> ObterIdentificacaoEnteAsync(Periodo periodo, CancellationToken cancellationToken);
}

/// <summary>
/// Identificação do ente para a remessa SIAPC/PAD: CNPJ, nome do Setor de Governo, intervalo do período e
/// Código da Remessa (Numérico 12, gerado pelo ente). Parametrizado por tenant/exercício.
/// </summary>
/// <param name="Cnpj">CNPJ do ente (14 dígitos).</param>
/// <param name="NomeSetorGoverno">Nome do Setor de Governo (cabeçalho).</param>
/// <param name="DataInicioPeriodo">Início da competência (compõe o nome do ZIP).</param>
/// <param name="DataFimPeriodo">Fim da competência (compõe o nome do ZIP).</param>
/// <param name="CodigoRemessa">Código da Remessa gerado pelo ente (Numérico 12).</param>
public sealed record IdentificacaoEnteRemessa(
    string Cnpj,
    string NomeSetorGoverno,
    DateOnly DataInicioPeriodo,
    DateOnly DataFimPeriodo,
    long CodigoRemessa);
