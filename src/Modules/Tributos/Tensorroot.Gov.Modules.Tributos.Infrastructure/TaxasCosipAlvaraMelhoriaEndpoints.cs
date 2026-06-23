using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Application.Cosip;
using Tensorroot.Gov.Modules.Tributos.Application.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Application.Taxas;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) das TAXAS (poder de polícia/serviço), ALVARÁS + TLL, COSIP e
/// CONTRIBUIÇÃO DE MELHORIA. Todos os valores/faixas vêm da LEI MUNICIPAL (parametrizável por tenant) —
/// nada hardcoded. RBAC por permissões tributos.* (negar por padrão — CLAUDE.md §6). Ver M6-DESIGN §3.
/// </summary>
internal static class TaxasCosipAlvaraMelhoriaEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos").WithTags("Tributos.TaxasCosipAlvaraMelhoria");

        MapTaxas(grupo);
        MapAlvaras(grupo);
        MapCosip(grupo);
        MapMelhoria(grupo);
    }

    // TAXAS — tabela parametrizável (CTM) e lançamento por fato gerador (CTN arts. 77–80; SV 19/29).
    private static void MapTaxas(RouteGroupBuilder grupo)
    {
        // Configura (cria + publica) a tabela de uma taxa (lei municipal).
        grupo.MapPost("/taxas/tabelas", async (
            ConfigurarTabelaTaxaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // Lança uma taxa (poder de polícia/serviço) a um contribuinte, vínculo opcional ao imóvel.
        grupo.MapPost("/taxas/lancar", async (
            LancarTaxaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(comando, cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    // ALVARÁS — ato de polícia + TLL (a taxa correlata) gerada à parte.
    private static void MapAlvaras(RouteGroupBuilder grupo)
    {
        // Emite o alvará e lança a TLL correspondente (a partir da tabela de licença vigente).
        grupo.MapPost("/alvaras/emitir", async (
            EmitirAlvaraCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(comando, cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // Renova o alvará por novo período e lança a TLL de renovação anual.
        grupo.MapPost("/alvaras/{alvaraId:guid}/renovar", async (
            Guid alvaraId, RenovarAlvaraPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new RenovarAlvaraCommand(alvaraId, payload.NovoInicioVigencia, payload.NovoFimVigencia, payload.CodigoTaxaTll, payload.Exercicio, payload.QuantidadeBaseTll, payload.VencimentoTll),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    // COSIP — tabela de faixas (lei municipal, CF art. 149-A) + lançamento próprio (não faturados).
    private static void MapCosip(RouteGroupBuilder grupo)
    {
        // Configura (cria + publica) a tabela de COSIP por faixa de consumo/classe (lei municipal).
        grupo.MapPost("/cosip/tabelas", async (
            ConfigurarTabelaCosipCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // Apura e lança a COSIP por lançamento próprio (caminho para não faturados pela distribuidora).
        grupo.MapPost("/cosip/lancar", async (
            LancarCosipCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(comando, cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    // CONTRIBUIÇÃO DE MELHORIA — edital + impugnação ≥30d + rateio (CTN arts. 81–82).
    private static void MapMelhoria(RouteGroupBuilder grupo)
    {
        // Publica o edital da obra (CTN art. 82): início obrigatório do processo, prazo de impugnação ≥ 30d.
        grupo.MapPost("/melhoria/obras", async (
            PublicarEditalMelhoriaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // Adiciona um imóvel beneficiado com a valorização individual apurada (limite individual CTN art. 81).
        grupo.MapPost("/melhoria/obras/{obraId:guid}/imoveis", async (
            Guid obraId, AdicionarImovelBeneficiadoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new AdicionarImovelBeneficiadoCommand(obraId, payload.ImovelId, payload.ProprietarioId, payload.ValorizacaoIndividual), cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // Encerra o prazo de impugnação (só a partir do fim do prazo) — habilita o rateio.
        grupo.MapPost("/melhoria/obras/{obraId:guid}/impugnacao/encerrar", async (
            Guid obraId, EncerrarImpugnacaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new EncerrarPrazoImpugnacaoCommand(obraId, payload.DataReferencia), cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // Rateia a contribuição proporcionalmente à valorização e gera um lançamento + guia por imóvel.
        grupo.MapPost("/melhoria/obras/{obraId:guid}/ratear", async (
            Guid obraId, RatearMelhoriaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new RatearContribuicaoMelhoriaCommand(obraId, payload.Vencimento, payload.NumeroParcelas), cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    private static async Task<object> EnviarOk(ISender sender, IRequest comando, CancellationToken cancellationToken)
    {
        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
        return new { ok = true };
    }

    private sealed record RenovarAlvaraPayload(
        DateOnly NovoInicioVigencia,
        DateOnly NovoFimVigencia,
        string CodigoTaxaTll,
        int Exercicio,
        decimal QuantidadeBaseTll,
        DateOnly VencimentoTll);

    private sealed record AdicionarImovelBeneficiadoPayload(Guid ImovelId, Guid ProprietarioId, decimal ValorizacaoIndividual);

    private sealed record EncerrarImpugnacaoPayload(DateOnly DataReferencia);

    private sealed record RatearMelhoriaPayload(DateOnly Vencimento, int NumeroParcelas = 1);
}
