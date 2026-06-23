using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Application.Esic;
using Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.PortalPublico;

/// <summary>
/// Endpoints PUBLICOS (anonimos, read-only) do portal de transparencia, FORA do prefixo <c>/api/...</c>
/// (logo o gating de licenca de modulo do ApiHost, que casa <c>/api/&lt;modulo&gt;</c>, NAO os intercepta —
/// a licenca e verificada AQUI pelo resolver). Grupo <c>/publico/transparencia/{slug}</c>:
/// <list type="bullet">
/// <item>Resolve o tenant pelo SLUG via <see cref="ITenantPublicoResolver"/> (sem JWT) e FIXA o
/// <see cref="TenantOverride"/> — assim o Global Query Filter por TenantId volta a valer em TODA leitura
/// (anti-vazamento cross-tenant).</item>
/// <item>Todos os endpoints sao <c>.AllowAnonymous()</c> (cidadao nao tem token).</item>
/// <item>Rate-limit: herdam o GlobalLimiter por IP do ApiHost; recomendado adicionar uma policy
/// dedicada "publico" (mais agressiva) no <c>AddRateLimiter</c> do ApiHost e encadear
/// <c>.RequireRateLimiting("publico")</c> aqui (ver README) — nao aplicado por padrao para nao acoplar a
/// uma policy que pode nao existir.</item>
/// </list>
/// LGPD: a superficie publica so retorna dado publico/minimizado (CPF mascarado na origem; folha sem
/// CPF/matricula; status e-SIC sem PII do solicitante).
/// </summary>
internal static class TransparenciaPublicaEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints
            .MapGroup("/publico/transparencia/{slug}")
            .WithTags("Transparencia.Publico")
            .AllowAnonymous();

        // Consulta de DESPESAS (paginada).
        grupo.MapGet("/despesas", async (
            string slug, int? exercicio, FaseDespesa? fase, string? funcao, string? fonte, string? credor,
            int? pagina, int? tamanho, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var resultado = await sender.Send(new ConsultarDespesasPublicasQuery(
                exercicio, fase, funcao, fonte, credor, pagina ?? 1, tamanho ?? 50), ct);
            return Results.Ok(resultado);
        });

        // Consulta de RECEITAS (paginada).
        grupo.MapGet("/receitas", async (
            string slug, int? exercicio, string? rubrica, string? fonte, int? pagina, int? tamanho,
            HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var resultado = await sender.Send(new ConsultarReceitasPublicasQuery(exercicio, rubrica, fonte, pagina ?? 1, tamanho ?? 50), ct);
            return Results.Ok(resultado);
        });

        // Consulta de CONTRATOS (paginada).
        grupo.MapGet("/contratos", async (
            string slug, int? ano, string? fornecedor, int? pagina, int? tamanho,
            HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var resultado = await sender.Send(new ConsultarContratosPublicosQuery(ano, fornecedor, pagina ?? 1, tamanho ?? 50), ct);
            return Results.Ok(resultado);
        });

        // FOLHA NOMINAL (sem PII; sem CPF/matricula).
        grupo.MapGet("/folha", async (
            string slug, string? competencia, string? lotacao, int? pagina, int? tamanho,
            HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var resultado = await sender.Send(new ConsultarFolhaPublicaQuery(competencia, lotacao, pagina ?? 1, tamanho ?? 50), ct);
            return Results.Ok(resultado);
        });

        // RESUMO FISCAL do exercicio (consulta em tempo real).
        grupo.MapGet("/resumo-fiscal", async (
            string slug, int exercicio, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            return Results.Ok(await sender.Send(new ConsultarResumoFiscalPublicoQuery(exercicio), ct));
        });

        // DICIONARIO/CATALOGO de dados abertos.
        grupo.MapGet("/dados-abertos", async (
            string slug, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            return Results.Ok(await sender.Send(new ObterCatalogoDadosAbertosQuery(), ct));
        });

        // Download CSV de um dataset (stream).
        grupo.MapGet("/dados-abertos/{dataset}.csv", async (
            string slug, string dataset, int? exercicio, HttpContext http, IConsultaPublicaRepository repo, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var cabecalho = repo.CabecalhoDataset(dataset);
            if (cabecalho is null)
            {
                return Results.NotFound();
            }

            var conteudo = await GerarCsvAsync(repo, dataset, exercicio, cabecalho, ct).ConfigureAwait(false);
            return Results.File(conteudo, "text/csv; charset=utf-8", $"{dataset}.csv");
        });

        // e-SIC: ABRE pedido (anonimo permitido — LAI veda exigir motivacao).
        grupo.MapPost("/esic", async (
            string slug, AbrirPedidoSicPublicoPayload payload, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var protocolo = await sender.Send(new AbrirPedidoSicCommand(
                payload.Nome, payload.Documento, payload.Contato, payload.Descricao, payload.FormaResposta), ct);
            return Results.Ok(new { protocolo });
        });

        // e-SIC: consulta STATUS publico (sem PII do solicitante). Catch-all ({**}) porque o protocolo
        // tem o formato AAAA/NNNNNN (contem barra) — um unico segmento de rota nao o capturaria.
        grupo.MapGet("/esic/{**protocolo}", async (
            string slug, string protocolo, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var status = await sender.Send(new ConsultarStatusPedidoSicQuery(protocolo), ct);
            return status is null ? Results.NotFound() : Results.Ok(status);
        });

        // e-SIC: interpoe RECURSO. O protocolo (formato AAAA/NNNNNN, com barra) vai no CORPO — um
        // segmento de rota nao o capturaria, e usar o protocolo como prefixo de rota colidiria com o
        // catch-all do GET de status acima.
        grupo.MapPost("/esic-recurso", async (
            string slug, InterporRecursoPublicoPayload payload, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!await FixarTenantAsync(http, slug, ct).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            await sender.Send(new InterporRecursoSicCommand(payload.Protocolo, payload.Instancia, payload.Fundamento), ct);
            return Results.NoContent();
        });
    }

    /// <summary>
    /// Resolve o tenant pelo slug e FIXA o <see cref="TenantOverride"/> do escopo da requisicao. Retorna
    /// <c>false</c> (→ 404) quando o slug nao existe, o portal esta inativo ou o modulo nao e licenciado.
    /// Apos isso, o Global Query Filter por TenantId vale para TODA leitura subsequente.
    /// </summary>
    private static async Task<bool> FixarTenantAsync(HttpContext http, string slug, CancellationToken ct)
    {
        var resolver = http.RequestServices.GetRequiredService<ITenantPublicoResolver>();
        var ente = await resolver.ResolverPorSlugAsync(slug, ct).ConfigureAwait(false);
        if (ente is null)
        {
            return false;
        }

        http.RequestServices.GetRequiredService<TenantOverride>().TenantId = ente.TenantId;
        return true;
    }

    private static async Task<byte[]> GerarCsvAsync(
        IConsultaPublicaRepository repo, string dataset, int? exercicio, IReadOnlyList<string> cabecalho, CancellationToken ct)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(';', cabecalho.Select(EscaparCsv)));
        await foreach (var linha in repo.StreamDatasetAsync(dataset, exercicio, ct).ConfigureAwait(false))
        {
            builder.AppendLine(string.Join(';', linha.Select(EscaparCsv)));
        }

        // BOM UTF-8 para abertura correta de acentos no Excel (padrao de exportacao do ente).
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string EscaparCsv(string campo)
    {
        if (campo.Contains(';', StringComparison.Ordinal)
            || campo.Contains('"', StringComparison.Ordinal)
            || campo.Contains('\n', StringComparison.Ordinal))
        {
            return string.Create(CultureInfo.InvariantCulture, $"\"{campo.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
        }

        return campo;
    }

    /// <summary>Payload publico de abertura de pedido e-SIC.</summary>
    private sealed record AbrirPedidoSicPublicoPayload(
        string Nome, string? Documento, string? Contato, string Descricao, FormaResposta FormaResposta);

    /// <summary>Payload publico de interposicao de recurso (protocolo AAAA/NNNNNN no corpo).</summary>
    private sealed record InterporRecursoPublicoPayload(string Protocolo, InstanciaRecurso Instancia, string Fundamento);
}
