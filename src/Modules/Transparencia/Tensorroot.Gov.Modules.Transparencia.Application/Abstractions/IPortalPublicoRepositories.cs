using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Filtro/paginacao comum das consultas publicas (read-only).</summary>
/// <param name="Pagina">Pagina (1-based).</param>
/// <param name="Tamanho">Tamanho da pagina (limitado no handler).</param>
public sealed record PaginacaoPublica(int Pagina = 1, int Tamanho = 50);

/// <summary>Pagina de resultados (sem expor total exato custoso; expoe se ha proxima).</summary>
/// <typeparam name="T">Tipo do item.</typeparam>
/// <param name="Itens">Itens da pagina.</param>
/// <param name="Pagina">Pagina atual.</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
/// <param name="TemProxima">Indica se ha proxima pagina.</param>
public sealed record PaginaResultado<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, bool TemProxima);

/// <summary>
/// Resolve o tenant a partir do slug PUBLICO do portal, SEM JWT (cidadao anonimo). Caminho de leitura
/// dedicado e auditado: le SOMENTE o mapa slug -&gt; (tenant, nome) do <see cref="PortalPublicoConfig"/>
/// ativo, sem tocar dado sensivel. Apos resolver, o endpoint fixa o <c>TenantOverride</c> para que o
/// Global Query Filter por TenantId volte a valer em TODA leitura subsequente (anti-vazamento).
/// </summary>
public interface ITenantPublicoResolver
{
    /// <summary>
    /// Resolve o ente publico pelo slug. Retorna <c>null</c> quando o slug nao existe, o portal esta
    /// inativo OU o modulo Transparencia nao esta licenciado para o tenant (nao revelar existencia → 404).
    /// </summary>
    /// <param name="slug">Slug publico (sera normalizado).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Ente resolvido ou <c>null</c>.</returns>
    Task<EntePublicoResolvido?> ResolverPorSlugAsync(string slug, CancellationToken cancellationToken);
}

/// <summary>Ente publico resolvido pelo slug (dado nao sensivel).</summary>
/// <param name="TenantId">Tenant resolvido.</param>
/// <param name="Slug">Slug normalizado.</param>
/// <param name="NomeEnte">Nome de exibicao do ente.</param>
public sealed record EntePublicoResolvido(Guid TenantId, string Slug, string NomeEnte);

/// <summary>Porta de leitura/escrita da configuracao do portal publico (tenant-scoped).</summary>
public interface IPortalPublicoConfigRepository
{
    /// <summary>Adiciona uma configuracao de portal publico.</summary>
    /// <param name="config">Configuracao.</param>
    void Adicionar(PortalPublicoConfig config);

    /// <summary>Obtem a configuracao do tenant atual (se houver).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Configuracao ou <c>null</c>.</returns>
    Task<PortalPublicoConfig?> ObterDoTenantAsync(CancellationToken cancellationToken);

    /// <summary>Indica se um slug ja existe (em qualquer tenant) — unicidade global do slug publico.</summary>
    /// <param name="slug">Slug normalizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe.</returns>
    Task<bool> SlugExisteAsync(string slug, CancellationToken cancellationToken);
}

/// <summary>Porta de leitura PUBLICA (paginada) dos read models de transparencia ativa.</summary>
public interface IConsultaPublicaRepository
{
    /// <summary>Pagina despesas publicas do tenant atual (Global Query Filter ja aplicado).</summary>
    Task<PaginaResultado<PublicacaoDespesa>> ConsultarDespesasAsync(
        int? exercicio, FaseDespesa? fase, string? funcaoSubfuncao, string? fonteRecurso, string? credor,
        PaginacaoPublica paginacao, CancellationToken cancellationToken);

    /// <summary>Pagina receitas publicas do tenant atual.</summary>
    Task<PaginaResultado<PublicacaoReceita>> ConsultarReceitasAsync(
        int? exercicio, string? rubrica, string? fonteRecurso, PaginacaoPublica paginacao, CancellationToken cancellationToken);

    /// <summary>Pagina contratos publicos do tenant atual.</summary>
    Task<PaginaResultado<PublicacaoContrato>> ConsultarContratosAsync(
        int? exercicio, string? fornecedor, PaginacaoPublica paginacao, CancellationToken cancellationToken);

    /// <summary>Pagina a folha nominal publica do tenant atual (SEM CPF/matricula por construcao).</summary>
    Task<PaginaResultado<PublicacaoFolhaNominal>> ConsultarFolhaAsync(
        string? competencia, string? lotacao, PaginacaoPublica paginacao, CancellationToken cancellationToken);

    /// <summary>Resumo fiscal do exercicio: receita arrecadada x despesa por fase (consulta em tempo real).</summary>
    Task<ResumoFiscalPublico> ConsultarResumoFiscalAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Enumera TODAS as linhas de um dataset (stream, sem paginacao) para export CSV de dados abertos.</summary>
    IAsyncEnumerable<IReadOnlyList<string>> StreamDatasetAsync(string dataset, int? exercicio, CancellationToken cancellationToken);

    /// <summary>Cabecalho do CSV do dataset (nomes de coluna), ou <c>null</c> se o dataset nao existir.</summary>
    IReadOnlyList<string>? CabecalhoDataset(string dataset);
}

/// <summary>Resumo fiscal agregado para o portal publico (consulta em tempo real).</summary>
/// <param name="Exercicio">Exercicio.</param>
/// <param name="ReceitaArrecadada">Total de receita arrecadada.</param>
/// <param name="DespesaEmpenhada">Total empenhado.</param>
/// <param name="DespesaLiquidada">Total liquidado.</param>
/// <param name="DespesaPaga">Total pago.</param>
public sealed record ResumoFiscalPublico(
    int Exercicio,
    decimal ReceitaArrecadada,
    decimal DespesaEmpenhada,
    decimal DespesaLiquidada,
    decimal DespesaPaga);

/// <summary>Porta do agregado <see cref="PedidoInformacaoSic"/> (e-SIC).</summary>
public interface IPedidoSicRepository
{
    /// <summary>Adiciona um pedido.</summary>
    void Adicionar(PedidoInformacaoSic pedido);

    /// <summary>Obtem por id (tenant-scoped).</summary>
    Task<PedidoInformacaoSic?> ObterPorIdAsync(PedidoInformacaoSicId id, CancellationToken cancellationToken);

    /// <summary>Obtem por protocolo no tenant atual.</summary>
    Task<PedidoInformacaoSic?> ObterPorProtocoloAsync(string protocolo, CancellationToken cancellationToken);

    /// <summary>Proximo sequencial de protocolo do (tenant, ano).</summary>
    Task<int> ProximoSequencialAsync(int ano, CancellationToken cancellationToken);

    /// <summary>Lista/filtra pedidos do tenant (superficie interna, com PII).</summary>
    Task<IReadOnlyList<PedidoInformacaoSic>> ListarAsync(int? ano, SituacaoPedidoSic? situacao, CancellationToken cancellationToken);
}
