using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Repositories;

/// <summary>EF Core: configuracao do portal publico (tenant-scoped pelo Global Query Filter).</summary>
public sealed class PortalPublicoConfigRepository(TransparenciaDbContext context) : IPortalPublicoConfigRepository
{
    /// <inheritdoc />
    public void Adicionar(PortalPublicoConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        context.PortaisPublicos.Add(config);
    }

    /// <inheritdoc />
    public Task<PortalPublicoConfig?> ObterDoTenantAsync(CancellationToken cancellationToken)
        => context.PortaisPublicos.FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> SlugExisteAsync(string slug, CancellationToken cancellationToken)
        // IgnoreQueryFilters: a unicidade do slug e GLOBAL (a URL publica nao pode colidir entre entes).
        // Caminho de leitura dedicado e restrito ao mapa de slug — nao expoe dado de negocio.
        => context.PortaisPublicos.IgnoreQueryFilters().AnyAsync(config => config.Slug == slug, cancellationToken);
}

/// <summary>EF Core: agregado <see cref="PedidoInformacaoSic"/> (e-SIC).</summary>
public sealed class PedidoSicRepository(TransparenciaDbContext context) : IPedidoSicRepository
{
    /// <inheritdoc />
    public void Adicionar(PedidoInformacaoSic pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        context.PedidosInformacaoSic.Add(pedido);
    }

    /// <inheritdoc />
    public Task<PedidoInformacaoSic?> ObterPorIdAsync(PedidoInformacaoSicId id, CancellationToken cancellationToken)
        => context.PedidosInformacaoSic.FirstOrDefaultAsync(pedido => pedido.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<PedidoInformacaoSic?> ObterPorProtocoloAsync(string protocolo, CancellationToken cancellationToken)
        => context.PedidosInformacaoSic.FirstOrDefaultAsync(pedido => pedido.Protocolo.Valor == protocolo, cancellationToken);

    /// <inheritdoc />
    public async Task<int> ProximoSequencialAsync(int ano, CancellationToken cancellationToken)
    {
        // Sequencial por (tenant, ano) — tenant-scoped pelo Global Query Filter.
        var maximo = await context.PedidosInformacaoSic
            .Where(pedido => pedido.Protocolo.Ano == ano)
            .Select(pedido => (int?)pedido.Protocolo.Sequencial)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);
        return (maximo ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PedidoInformacaoSic>> ListarAsync(int? ano, SituacaoPedidoSic? situacao, CancellationToken cancellationToken)
        => await context.PedidosInformacaoSic
            .Where(pedido => ano == null || pedido.Protocolo.Ano == ano)
            .Where(pedido => situacao == null || pedido.Situacao == situacao)
            .OrderByDescending(pedido => pedido.DataAbertura)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>
/// EF Core: UPSERT idempotente dos read models publicos (projecao I-13). Cada upsert salva no proprio
/// escopo do consumidor (o despachante do Outbox isola cada handler em escopo dedicado e NAO salva por
/// nos). Idempotente por chave de origem: reprocessar a mesma origem ATUALIZA a linha, nunca duplica.
/// </summary>
public sealed class ProjecaoPublicaRepository(TransparenciaDbContext context, IUnitOfWork unitOfWork) : IProjecaoPublicaRepository
{
    /// <inheritdoc />
    public async Task UpsertDespesaAsync(
        Guid tenantId, Guid origemEventoId, int exercicio, FaseDespesa fase, decimal valor, DateOnly data,
        string? numeroEmpenho, string? credorNome, string? credorDocumento, string? funcaoSubfuncao, string? fonteRecurso,
        CancellationToken cancellationToken)
    {
        var existente = await context.PublicacoesDespesa
            .FirstOrDefaultAsync(d => d.OrigemEventoId == origemEventoId, cancellationToken).ConfigureAwait(false);
        if (existente is null)
        {
            context.PublicacoesDespesa.Add(PublicacaoDespesa.Materializar(
                tenantId, origemEventoId, exercicio, fase, valor, data, numeroEmpenho, credorNome, credorDocumento, funcaoSubfuncao, fonteRecurso));
        }
        else
        {
            existente.Aplicar(exercicio, fase, valor, data, numeroEmpenho, credorNome, credorDocumento, funcaoSubfuncao, fonteRecurso);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertReceitaAsync(
        Guid tenantId, Guid origemEventoId, int exercicio, decimal valor, DateOnly data, string? rubrica, string? fonteRecurso,
        CancellationToken cancellationToken)
    {
        var existente = await context.PublicacoesReceita
            .FirstOrDefaultAsync(r => r.OrigemEventoId == origemEventoId, cancellationToken).ConfigureAwait(false);
        if (existente is null)
        {
            context.PublicacoesReceita.Add(PublicacaoReceita.Materializar(tenantId, origemEventoId, exercicio, valor, data, rubrica, fonteRecurso));
        }
        else
        {
            existente.Aplicar(exercicio, valor, data, rubrica, fonteRecurso);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertContratoAsync(
        Guid tenantId, Guid origemEventoId, int exercicio, decimal valor, string? numeroContrato, string? fornecedor,
        string? objeto, string? modalidade, string? numeroContratoPncp, CancellationToken cancellationToken)
    {
        var existente = await context.PublicacoesContrato
            .FirstOrDefaultAsync(c => c.OrigemEventoId == origemEventoId, cancellationToken).ConfigureAwait(false);
        if (existente is null)
        {
            context.PublicacoesContrato.Add(PublicacaoContrato.Materializar(
                tenantId, origemEventoId, exercicio, valor, numeroContrato, fornecedor, objeto, modalidade, numeroContratoPncp));
        }
        else
        {
            existente.Aplicar(exercicio, valor, numeroContrato, fornecedor, objeto, modalidade, numeroContratoPncp);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RegistrarPncpContratoAsync(Guid tenantId, Guid contratoId, string? numeroContratoPncp, CancellationToken cancellationToken)
    {
        var existente = await context.PublicacoesContrato
            .FirstOrDefaultAsync(c => c.OrigemEventoId == contratoId, cancellationToken).ConfigureAwait(false);
        if (existente is null)
        {
            // Evento PNCP pode chegar antes do contrato projetado: cria a linha minima (idempotente por chave).
            context.PublicacoesContrato.Add(PublicacaoContrato.Materializar(
                tenantId, contratoId, 0, 0m, numeroContrato: contratoId.ToString(), fornecedor: null, objeto: null, modalidade: null, numeroContratoPncp));
        }
        else
        {
            existente.RegistrarPncp(numeroContratoPncp);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertFolhaNominalAsync(
        Guid tenantId, string competencia, string codigoServidor, string servidorNome, string? cargo, string? lotacao,
        decimal remuneracaoBruta, decimal descontos, CancellationToken cancellationToken)
    {
        var origem = $"{competencia}|{codigoServidor}";
        var existente = await context.PublicacoesFolhaNominal
            .FirstOrDefaultAsync(f => f.OrigemEventoId == origem, cancellationToken).ConfigureAwait(false);
        if (existente is null)
        {
            context.PublicacoesFolhaNominal.Add(PublicacaoFolhaNominal.Materializar(
                tenantId, competencia, codigoServidor, servidorNome, cargo, lotacao, remuneracaoBruta, descontos));
        }
        else
        {
            existente.Aplicar(competencia, servidorNome, cargo, lotacao, remuneracaoBruta, descontos);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// EF Core: leitura PUBLICA (paginada) dos read models de transparencia ativa. Tenant-scoped pelo Global
/// Query Filter (o endpoint publico ja fixou o TenantOverride a partir do slug) — anti-vazamento.
/// </summary>
public sealed class ConsultaPublicaRepository(TransparenciaDbContext context) : IConsultaPublicaRepository
{
    /// <inheritdoc />
    public async Task<PaginaResultado<PublicacaoDespesa>> ConsultarDespesasAsync(
        int? exercicio, FaseDespesa? fase, string? funcaoSubfuncao, string? fonteRecurso, string? credor,
        PaginacaoPublica paginacao, CancellationToken cancellationToken)
    {
        var consulta = context.PublicacoesDespesa.AsNoTracking()
            .Where(d => exercicio == null || d.Exercicio == exercicio)
            .Where(d => fase == null || d.Fase == fase)
            .Where(d => funcaoSubfuncao == null || d.FuncaoSubfuncao == funcaoSubfuncao)
            .Where(d => fonteRecurso == null || d.FonteRecurso == fonteRecurso)
            .Where(d => credor == null || (d.CredorNomeOuRazao != null && d.CredorNomeOuRazao.Contains(credor)))
            .OrderByDescending(d => d.Data).ThenBy(d => d.Id);
        return await PaginarAsync(consulta, paginacao, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<PublicacaoReceita>> ConsultarReceitasAsync(
        int? exercicio, string? rubrica, string? fonteRecurso, PaginacaoPublica paginacao, CancellationToken cancellationToken)
    {
        var consulta = context.PublicacoesReceita.AsNoTracking()
            .Where(r => exercicio == null || r.Exercicio == exercicio)
            .Where(r => rubrica == null || (r.RubricaReceita != null && r.RubricaReceita.Contains(rubrica)))
            .Where(r => fonteRecurso == null || r.FonteRecurso == fonteRecurso)
            .OrderByDescending(r => r.Data).ThenBy(r => r.Id);
        return await PaginarAsync(consulta, paginacao, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<PublicacaoContrato>> ConsultarContratosAsync(
        int? exercicio, string? fornecedor, PaginacaoPublica paginacao, CancellationToken cancellationToken)
    {
        var consulta = context.PublicacoesContrato.AsNoTracking()
            .Where(c => exercicio == null || c.Exercicio == exercicio)
            .Where(c => fornecedor == null || (c.Fornecedor != null && c.Fornecedor.Contains(fornecedor)))
            .OrderByDescending(c => c.Exercicio).ThenBy(c => c.Id);
        return await PaginarAsync(consulta, paginacao, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<PublicacaoFolhaNominal>> ConsultarFolhaAsync(
        string? competencia, string? lotacao, PaginacaoPublica paginacao, CancellationToken cancellationToken)
    {
        var consulta = context.PublicacoesFolhaNominal.AsNoTracking()
            .Where(f => competencia == null || f.Competencia == competencia)
            .Where(f => lotacao == null || (f.Lotacao != null && f.Lotacao.Contains(lotacao)))
            .OrderBy(f => f.ServidorNome).ThenBy(f => f.Id);
        return await PaginarAsync(consulta, paginacao, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ResumoFiscalPublico> ConsultarResumoFiscalAsync(int exercicio, CancellationToken cancellationToken)
    {
        // SQLite nao agrega Sum sobre decimal no servidor — materializa os valores e soma no cliente.
        var valoresReceita = await context.PublicacoesReceita.AsNoTracking()
            .Where(r => r.Exercicio == exercicio).Select(r => r.Valor).ToListAsync(cancellationToken).ConfigureAwait(false);
        var receita = valoresReceita.Sum();
        var empenhada = await SomarDespesaAsync(exercicio, FaseDespesa.Empenhada, cancellationToken).ConfigureAwait(false);
        var liquidada = await SomarDespesaAsync(exercicio, FaseDespesa.Liquidada, cancellationToken).ConfigureAwait(false);
        var paga = await SomarDespesaAsync(exercicio, FaseDespesa.Paga, cancellationToken).ConfigureAwait(false);
        return new ResumoFiscalPublico(exercicio, receita, empenhada, liquidada, paga);
    }

    /// <inheritdoc />
    public IReadOnlyList<string>? CabecalhoDataset(string dataset)
        => CatalogoDadosAbertos.Datasets
            .FirstOrDefault(d => string.Equals(d.Dataset, dataset, StringComparison.OrdinalIgnoreCase))?.Colunas;

    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyList<string>> StreamDatasetAsync(
        string dataset, int? exercicio, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        switch (dataset.ToLowerInvariant())
        {
            case "despesas":
                await foreach (var linha in StreamDespesasAsync(exercicio, fase: null, cancellationToken).ConfigureAwait(false))
                {
                    yield return linha;
                }

                break;
            case "diarias":
                // Diarias derivadas: elemento 339014 na natureza da despesa (heuristica honesta — refino futuro).
                await foreach (var linha in StreamDespesasAsync(exercicio, fase: null, cancellationToken, "339014").ConfigureAwait(false))
                {
                    yield return linha;
                }

                break;
            case "repasses":
                await foreach (var linha in StreamDespesasAsync(exercicio, fase: null, cancellationToken, "335043", "444042").ConfigureAwait(false))
                {
                    yield return linha;
                }

                break;
            case "receitas":
                await foreach (var receita in context.PublicacoesReceita.AsNoTracking()
                                   .Where(r => exercicio == null || r.Exercicio == exercicio)
                                   .OrderBy(r => r.Data).AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return [Texto(receita.Exercicio), receita.RubricaReceita ?? string.Empty, receita.FonteRecurso ?? string.Empty, Texto(receita.Valor), Texto(receita.Data)];
                }

                break;
            case "contratos":
                await foreach (var contrato in context.PublicacoesContrato.AsNoTracking()
                                   .Where(c => exercicio == null || c.Exercicio == exercicio)
                                   .OrderBy(c => c.Exercicio).AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return [Texto(contrato.Exercicio), contrato.NumeroContrato ?? string.Empty, contrato.Fornecedor ?? string.Empty, contrato.Objeto ?? string.Empty, Texto(contrato.Valor), contrato.Modalidade ?? string.Empty, contrato.NumeroContratoPncp ?? string.Empty];
                }

                break;
            case "folha":
                await foreach (var folha in context.PublicacoesFolhaNominal.AsNoTracking()
                                   .Where(f => exercicio == null || f.Competencia.StartsWith(Texto(exercicio.Value)))
                                   .OrderBy(f => f.ServidorNome).AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    // SEM CPF/matricula por construcao do read model.
                    yield return [folha.Competencia, folha.ServidorNome, folha.CargoDescricao ?? string.Empty, folha.Lotacao ?? string.Empty, Texto(folha.RemuneracaoBruta), Texto(folha.Descontos), Texto(folha.Liquido)];
                }

                break;
            default:
                yield break;
        }
    }

    private async IAsyncEnumerable<IReadOnlyList<string>> StreamDespesasAsync(
        int? exercicio, FaseDespesa? fase, [EnumeratorCancellation] CancellationToken cancellationToken, params string[] prefixosNd)
    {
        var consulta = context.PublicacoesDespesa.AsNoTracking()
            .Where(d => exercicio == null || d.Exercicio == exercicio)
            .Where(d => fase == null || d.Fase == fase)
            .OrderBy(d => d.Data).AsAsyncEnumerable();

        await foreach (var despesa in consulta.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (prefixosNd.Length > 0 && !CasaPrefixoNd(despesa.FuncaoSubfuncao, prefixosNd))
            {
                continue;
            }

            // Documento ja mascarado no read model — exportacao NUNCA expoe CPF integral.
            yield return [Texto(despesa.Exercicio), despesa.Fase.ToString(), despesa.NumeroEmpenho ?? string.Empty, despesa.CredorNomeOuRazao ?? string.Empty, despesa.CredorDocMascarado ?? string.Empty, despesa.FuncaoSubfuncao ?? string.Empty, despesa.FonteRecurso ?? string.Empty, Texto(despesa.Valor), Texto(despesa.Data)];
        }
    }

    private static bool CasaPrefixoNd(string? campo, string[] prefixos)
    {
        if (string.IsNullOrEmpty(campo))
        {
            return false;
        }

        foreach (var prefixo in prefixos)
        {
            if (campo.Contains(prefixo, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<decimal> SomarDespesaAsync(int exercicio, FaseDespesa fase, CancellationToken cancellationToken)
    {
        // SQLite nao agrega Sum sobre decimal no servidor — materializa os valores e soma no cliente.
        var valores = await context.PublicacoesDespesa.AsNoTracking()
            .Where(d => d.Exercicio == exercicio && d.Fase == fase)
            .Select(d => d.Valor)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return valores.Sum();
    }

    private static async Task<PaginaResultado<T>> PaginarAsync<T>(
        IQueryable<T> consulta, PaginacaoPublica paginacao, CancellationToken cancellationToken)
    {
        // Busca tamanho+1 para saber se ha proxima pagina sem COUNT custoso.
        var saltar = (paginacao.Pagina - 1) * paginacao.Tamanho;
        var itens = await consulta.Skip(saltar).Take(paginacao.Tamanho + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
        var temProxima = itens.Count > paginacao.Tamanho;
        if (temProxima)
        {
            itens.RemoveAt(itens.Count - 1);
        }

        return new PaginaResultado<T>(itens, paginacao.Pagina, paginacao.Tamanho, temProxima);
    }

    private static string Texto(int valor) => valor.ToString(CultureInfo.InvariantCulture);

    private static string Texto(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Texto(DateOnly valor) => valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
