using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Identidade.Application.Autenticacao;
using Tensorroot.Gov.Modules.Identidade.Application.Papeis;
using Tensorroot.Gov.Modules.Identidade.Application.Unidades;
using Tensorroot.Gov.Modules.Identidade.Application.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.Permissoes;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do modulo Identidade: login (anonimo) e administracao de usuarios
/// e papeis (exigindo a permissao "identidade.usuarios.gerenciar"). E SEGURANCA CRITICA.
/// </summary>
internal static class IdentidadeEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/identidade").WithTags("Identidade");

        // === Login (anonimo): resolve o tenant pelo indice central e autentica no banco do tenant ===
        grupo.MapPost("/login", async (
            LoginPayload payload,
            IUsuarioTenantIndexService indiceCentral,
            TenantOverride tenantOverride,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            // Resolve o tenant (id + nome) a partir do e-mail (indice central no banco de controle).
            // Falha indistinguivel de credenciais invalidas (nao revela existencia da conta/tenant).
            var tenant = await indiceCentral
                .ResolverTenantDetalhePorEmailAsync(payload.Email, cancellationToken)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                return Results.Json(new { erro = "Credenciais invalidas." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            // Define o tenant no escopo: o IdentidadeDbContext passa a apontar para o banco DEDICADO
            // do tenant e o Global Query Filter usa este tenant.
            tenantOverride.TenantId = tenant.Value.TenantId;

            try
            {
                var resultado = await sender
                    .Send(new AutenticarCommand(tenant.Value.TenantId, payload.Email, payload.Senha, tenant.Value.Nome), cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(new { accessToken = resultado.Token.AccessToken, expiraEm = resultado.Token.ExpiraEm });
            }
            catch (AutenticacaoFalhouException)
            {
                return Results.Json(new { erro = "Credenciais invalidas." }, statusCode: StatusCodes.Status401Unauthorized);
            }
        }).AllowAnonymous().RequireRateLimiting("login");

        // === Administracao de usuarios e papeis (RBAC: identidade.usuarios.gerenciar) ===
        var admin = grupo.MapGroup(string.Empty).RequirePermission(Permissoes.IdentidadeUsuariosGerenciar);

        // --- Usuarios ---
        admin.MapGet("/usuarios", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarUsuariosQuery(), cancellationToken)));

        admin.MapGet("/usuarios/{usuarioId:guid}", async (
            Guid usuarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterUsuarioQuery(usuarioId), cancellationToken)));

        admin.MapGet("/usuarios/{usuarioId:guid}/permissoes", async (
            Guid usuarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPermissoesEfetivasDoUsuarioQuery(usuarioId), cancellationToken)));

        admin.MapPost("/usuarios", async (
            CriarUsuarioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }));

        admin.MapPut("/usuarios/{usuarioId:guid}", async (
            Guid usuarioId, EditarUsuarioPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new EditarUsuarioCommand(usuarioId, payload.Nome, payload.Email), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // W10.6 ID-2: escopo/I4 reprovou a edicao (alvo fora do escopo ou mais poderoso) → 403.
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        admin.MapPut("/usuarios/{usuarioId:guid}/senha", async (
            Guid usuarioId, AlterarSenhaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new AlterarSenhaCommand(usuarioId, payload.NovaSenha), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // W10.6 ID-1 (P0): escopo/I4 reprovou o reset (anti-tomada de conta) → 403 auditado.
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        admin.MapPut("/usuarios/{usuarioId:guid}/papeis", async (
            Guid usuarioId, DefinirPapeisPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new DefinirPapeisDoUsuarioCommand(usuarioId, payload.PapeisIds), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // I4 reprovou a atribuicao GLOBAL de papeis (AA-2) → 403 com motivo claro (deny-by-default).
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        // --- Atribuicoes de papel COM ESCOPO de UO (RBAC+ABAC) — APLICAM A REGRA I4 no dominio ---
        admin.MapPost("/usuarios/{usuarioId:guid}/atribuicoes", async (
            Guid usuarioId, AtribuirPapelPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(
                    new AtribuirPapelAoUsuarioCommand(
                        usuarioId,
                        payload.PapelId,
                        payload.UnidadeId,
                        payload.IncluiSubunidades,
                        payload.VigenciaInicio,
                        payload.VigenciaFim),
                    cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // I4 reprovou no dominio → 403 com motivo claro (deny-by-default, nunca silencioso).
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        admin.MapDelete("/usuarios/{usuarioId:guid}/atribuicoes", async (
            Guid usuarioId, Guid papelId, Guid unidadeId, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new RevogarAtribuicaoCommand(usuarioId, papelId, unidadeId), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        admin.MapPost("/usuarios/{usuarioId:guid}/ativar", async (
            Guid usuarioId, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new AtivarUsuarioCommand(usuarioId), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // W10.6 ID-2: escopo/I4 reprovou a reativacao fora do escopo → 403 auditado.
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        admin.MapPost("/usuarios/{usuarioId:guid}/desativar", async (
            Guid usuarioId, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new DesativarUsuarioCommand(usuarioId), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // W10.6 ID-2: escopo/I4 reprovou a desativacao (ex.: do admin-raiz) → 403 auditado.
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        // --- Papeis ---
        admin.MapGet("/papeis", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarPapeisQuery(), cancellationToken)));

        admin.MapPost("/papeis", async (
            CriarPapelCommand comando, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(new { id = await sender.Send(comando, cancellationToken) });
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // I4 reprovou a COMPOSICAO do papel (AA-1) → 403 com motivo claro (deny-by-default).
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        admin.MapPut("/papeis/{papelId:guid}/permissoes", async (
            Guid papelId, DefinirPermissoesPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                await sender.Send(new DefinirPermissoesDoPapelCommand(papelId, payload.Permissoes), cancellationToken);
                return Results.NoContent();
            }
            catch (ConcessaoNaoAutorizadaException excecao)
            {
                // I4 reprovou a COMPOSICAO do papel (AA-1) → 403 com motivo claro (deny-by-default).
                return Results.Json(
                    new { erro = excecao.Message, motivo = excecao.Motivo.ToString() },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        });

        // Catalogo canonico de permissoes (auxilia o front a montar a tela de papeis).
        admin.MapGet("/permissoes", () => Results.Ok(Permissoes.Todas));

        // === Estrutura Organizacional (UOs) — RBAC: identidade.usuarios.gerenciar ===
        admin.MapGet("/unidades", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarArvoreUnidadesQuery(), cancellationToken)));

        admin.MapPost("/unidades", async (
            CriarUnidadePayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                id = await sender.Send(
                    new CriarUnidadeCommand(payload.Codigo, payload.Nome, payload.Tipo, payload.UnidadePaiId),
                    cancellationToken),
            }));

        admin.MapPut("/unidades/{unidadeId:guid}", async (
            Guid unidadeId, RenomearUnidadePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RenomearUnidadeCommand(unidadeId, payload.Nome, payload.Tipo), cancellationToken);
            return Results.NoContent();
        });

        admin.MapPost("/unidades/{unidadeId:guid}/mover", async (
            Guid unidadeId, MoverUnidadePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new MoverUnidadeCommand(unidadeId, payload.NovoPaiId), cancellationToken);
            return Results.NoContent();
        });

        admin.MapPost("/unidades/{unidadeId:guid}/ativar", async (
            Guid unidadeId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtivarUnidadeCommand(unidadeId), cancellationToken);
            return Results.NoContent();
        });

        admin.MapPost("/unidades/{unidadeId:guid}/desativar", async (
            Guid unidadeId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesativarUnidadeCommand(unidadeId), cancellationToken);
            return Results.NoContent();
        });
    }

    private sealed record LoginPayload(string Email, string Senha);

    private sealed record EditarUsuarioPayload(string Nome, string Email);

    private sealed record AlterarSenhaPayload(string NovaSenha);

    private sealed record DefinirPapeisPayload(IReadOnlyCollection<Guid> PapeisIds);

    private sealed record DefinirPermissoesPayload(IReadOnlyCollection<string> Permissoes);

    private sealed record AtribuirPapelPayload(
        Guid PapelId,
        Guid UnidadeId,
        bool IncluiSubunidades,
        DateTimeOffset? VigenciaInicio = null,
        DateTimeOffset? VigenciaFim = null);

    private sealed record CriarUnidadePayload(string Codigo, string Nome, TipoUnidade Tipo, Guid? UnidadePaiId = null);

    private sealed record RenomearUnidadePayload(string Nome, TipoUnidade Tipo);

    private sealed record MoverUnidadePayload(Guid NovoPaiId);
}
