using System.Data.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Cidadao.Application.Autenticacao;
using Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do Portal do Cidadao sob <c>/api/cidadao</c>. Duas superficies:
/// <list type="bullet">
/// <item>ANONIMA (cadastro/login local por CPF/CNPJ): resolve o tenant (municipio) informado e emite o
/// token do cidadao. Sem barreira de RBAC (o cidadao ainda nao tem conta/token).</item>
/// <item>PROPRIA (/meus-*): exige a policy <see cref="PortalCidadaoPolicy.Nome"/> (token tipo=cidadao);
/// cada handler resolve a pessoa do PROPRIO token (ancora dado-proprio) — NENHUM aceita CPF/id de
/// terceiro. SEGURANCA dado-proprio a prova de bala.</item>
/// </list>
/// </summary>
internal static class CidadaoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/cidadao").WithTags("Cidadao");

        MapearAutenticacao(grupo);
        MapearMeusDados(grupo);
    }

    private static void MapearAutenticacao(RouteGroupBuilder grupo)
    {
        // === CADASTRO local (anonimo): cria a conta-cidadao no municipio informado ===
        grupo.MapPost("/cadastro", async (
            CadastroPayload payload,
            TenantOverride tenantOverride,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            // O cidadao escolhe o municipio (tenant) no portal; o cadastro nasce escopado a ele.
            tenantOverride.TenantId = payload.TenantId;
            try
            {
                await sender.Send(
                    new RegistrarCidadaoCommand(payload.Documento, payload.Nome, payload.Senha, payload.Email, payload.Telefone),
                    cancellationToken);

                // ANTI-ENUMERACAO: resposta GENERICA e UNIFORME — IDENTICA para documento novo e para
                // documento ja cadastrado. Nao retornamos o id da conta (que so existiria no caminho
                // "novo") nem qualquer sinal de existencia; a confirmacao real segue por canal lateral
                // (e-mail). Assim, atacante anonimo iterando CPFs/CNPJs NAO distingue conta existente de
                // nova. A unicidade real permanece no banco. // TODO(M10-creds): confirmacao por e-mail.
                return Results.Accepted(value: new { mensagem = "Se os dados forem elegiveis, enviaremos a confirmacao do cadastro." });
            }
            catch (DbException)
            {
                // ROBUSTEZ (fail-closed): tenantId informado pelo cliente que NAO corresponde a um
                // municipio provisionado (banco/schema inexistente) NAO pode derrubar a borda com 500 nem
                // virar canal de sondagem de tenants. Responde 404 uniforme, sem vazar a causa.
                return Results.Json(new { erro = "Municipio nao disponivel para cadastro." }, statusCode: StatusCodes.Status404NotFound);
            }
        }).AllowAnonymous();

        // === LOGIN local (anonimo): autentica por CPF/CNPJ + senha no municipio informado ===
        grupo.MapPost("/login", async (
            LoginPayload payload,
            TenantOverride tenantOverride,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            tenantOverride.TenantId = payload.TenantId;
            try
            {
                var resultado = await sender.Send(
                    new AutenticarCidadaoCommand(payload.Documento, payload.Senha), cancellationToken);
                return Results.Ok(new { accessToken = resultado.Token.AccessToken, expiraEm = resultado.Token.ExpiraEm });
            }
            catch (AutenticacaoCidadaoFalhouException)
            {
                // Mensagem uniforme (anti-enumeracao): nao revela se foi documento/senha/conta inativa.
                return Results.Json(new { erro = "Credenciais invalidas." }, statusCode: StatusCodes.Status401Unauthorized);
            }
            catch (DbException)
            {
                // ROBUSTEZ (fail-closed): tenantId informado pelo cliente sem municipio provisionado
                // (banco/schema inexistente) cairia em 500. Trata como credencial invalida (MESMA resposta
                // uniforme do caminho de falha) — nao derruba a borda e nao vira sondagem de tenants.
                return Results.Json(new { erro = "Credenciais invalidas." }, statusCode: StatusCodes.Status401Unauthorized);
            }
        }).AllowAnonymous();

        // === LOGIN gov.br (// TODO(M10-creds)): bloqueado ate creds + adesao do municipio ===
        // Atras de feature flag/stub: o handler real (authorization code + JWKS + selo) entra no M10.
        grupo.MapPost("/login/govbr", () => Results.Json(
            new { erro = "Login gov.br indisponivel (// TODO(M10-creds)). Use o login local." },
            statusCode: StatusCodes.Status501NotImplemented))
            .AllowAnonymous();
    }

    private static void MapearMeusDados(RouteGroupBuilder grupo)
    {
        // SUPERFICIE PROPRIA: gated por tipo=cidadao (NUNCA por permissao RBAC). Cada handler resolve a
        // pessoa do PROPRIO token; o cliente nao envia (nem ha como forjar) CPF/contribuinte de terceiro.
        var meu = grupo.MapGroup(string.Empty).RequireAuthorization(PortalCidadaoPolicy.Nome);

        // MEUS DEBITOS: lancamentos tributarios em aberto do proprio cidadao.
        meu.MapGet("/meus-debitos", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterMeusDebitosQuery(), cancellationToken)));

        // MINHA DIVIDA ATIVA: posicao consolidada (valor atualizado na data-base; default hoje).
        meu.MapGet("/minha-divida-ativa", async (DateOnly? dataBase, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterMinhaDividaAtivaQuery(dataBase), cancellationToken)));

        // 2a VIA DE DAM: revalida titularidade server-side (anti-IDOR) — 404 se nao for do cidadao.
        meu.MapGet("/dams/{damId:guid}/segunda-via", async (Guid damId, ISender sender, CancellationToken cancellationToken) =>
        {
            var dam = await sender.Send(new ObterSegundaViaDamQuery(damId), cancellationToken);
            return dam is null ? Results.NotFound() : Results.Ok(dam);
        });

        // MINHA CERTIDAO DE REGULARIDADE (CND/CPEN): emite em autosservico a certidao do proprio cidadao
        // (CTN arts. 205/206). Retorna 404 se nao ha contribuinte com o documento no tenant (sem vinculo).
        meu.MapPost("/minha-certidao-regularidade", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var certidao = await sender.Send(new EmitirMinhaCertidaoRegularidadeCommand(), cancellationToken);
            return certidao is null ? Results.NotFound() : Results.Ok(certidao);
        });

        // MEUS PROCESSOS: processos publicos de que o cidadao e interessado.
        meu.MapGet("/meus-processos", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterMeusProcessosQuery(), cancellationToken)));

        // MEU PROCESSO (detalhe): revalida titularidade + nivel de acesso (anti-IDOR) — 404 se nao for.
        meu.MapGet("/meus-processos/{processoId:guid}", async (Guid processoId, ISender sender, CancellationToken cancellationToken) =>
        {
            var processo = await sender.Send(new ObterMeuProcessoQuery(processoId), cancellationToken);
            return processo is null ? Results.NotFound() : Results.Ok(processo);
        });
    }

    private sealed record CadastroPayload(
        Guid TenantId,
        string Documento,
        string Nome,
        string Senha,
        string? Email = null,
        string? Telefone = null);

    private sealed record LoginPayload(Guid TenantId, string Documento, string Senha);
}
