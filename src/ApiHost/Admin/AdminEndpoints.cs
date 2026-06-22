using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.ApiHost.Admin;

/// <summary>
/// Endpoints ADMINISTRATIVOS (só-admin) para configurar as licenças de MÓDULO de um tenant.
/// Protegidos pela permissão RBAC <c>admin.modulos.configurar</c> (claim "perm"): sem ela, 403.
/// Reusa os serviços da Plataforma multi-tenant (catálogo de licenças por tenant).
/// </summary>
internal static class AdminEndpoints
{
    private const string PermissaoConfigurarModulos = "admin.modulos.configurar";
    private const string PermissaoVerAuditoria = "admin.auditoria.ver";
    private const int TamanhoPaginaMaximo = 200;

    /// <summary>Mapeia os endpoints administrativos de configuração de módulos no pipeline.</summary>
    /// <param name="endpoints">Construtor de rotas de endpoint (ex.: <c>app</c>).</param>
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var grupo = endpoints
            .MapGroup("/api/admin/tenants/{tenantId:guid}/modulos")
            .WithTags("Admin")
            .RequirePermission(PermissaoConfigurarModulos);

        // GET → lista TODOS os módulos descobertos com a flag "ativo" (licenciado) para o tenant.
        grupo.MapGet("/", async (
            Guid tenantId,
            IEnumerable<IModule> modulos,
            ITenantModuleProvider moduleProvider,
            CancellationToken cancellationToken) =>
        {
            var ativos = await moduleProvider.EnabledModulesAsync(tenantId, cancellationToken);
            var ativosSet = new HashSet<string>(ativos, StringComparer.Ordinal);

            var resultado = modulos
                .Select(modulo => new ModuloTenantDto(modulo.Name, ativosSet.Contains(modulo.Name)))
                .OrderBy(item => item.Modulo, StringComparer.Ordinal)
                .ToList();

            return Results.Ok(resultado);
        });

        // PUT → ativa/desativa a licença de UM módulo para o tenant.
        grupo.MapPut("/{modulo}", async (
            Guid tenantId,
            string modulo,
            DefinirModuloRequest requisicao,
            IEnumerable<IModule> modulos,
            ITenantProvisioningService provisioningService,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(requisicao);

            // Resolve o nome canônico do módulo (case-insensitive na rota; nome canônico no catálogo).
            var nomeCanonico = modulos
                .Select(item => item.Name)
                .FirstOrDefault(name => string.Equals(name, modulo, StringComparison.OrdinalIgnoreCase));

            if (nomeCanonico is null)
            {
                return Results.NotFound(new { erro = $"Módulo '{modulo}' não existe." });
            }

            await provisioningService.DefinirModuloAsync(tenantId, nomeCanonico, requisicao.Ativo, cancellationToken);

            return Results.Ok(new ModuloTenantDto(nomeCanonico, requisicao.Ativo));
        });

        // === Visualizador de AUDITORIA (só-admin) — "usuário X fez Y, quando, antes/depois" ===
        var auditoria = endpoints
            .MapGroup("/api/admin/auditoria")
            .WithTags("Admin")
            .RequirePermission(PermissaoVerAuditoria);

        // GET → trilha do tenant, paginada e filtrável (entidade, usuário, ação).
        auditoria.MapGet("/", async (
            AuditoriaReadDbContext contexto,
            ITenantContext tenant,
            string? entidade,
            string? usuario,
            string? acao,
            int? pagina,
            int? tamanho,
            CancellationToken cancellationToken) =>
        {
            var paginaAtual = Math.Max(1, pagina ?? 1);
            var tamanhoPagina = Math.Clamp(tamanho ?? 50, 1, TamanhoPaginaMaximo);

            var consulta = contexto.Trilha.AsNoTracking().Where(item => item.TenantId == tenant.TenantId);
            if (!string.IsNullOrWhiteSpace(entidade))
            {
                consulta = consulta.Where(item => item.EntityName == entidade);
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                consulta = consulta.Where(item => item.UserId == usuario);
            }

            if (!string.IsNullOrWhiteSpace(acao))
            {
                consulta = consulta.Where(item => item.Action == acao);
            }

            var total = await consulta.CountAsync(cancellationToken);
            var itens = await consulta
                .OrderByDescending(item => item.TimestampUtc)
                .Skip((paginaAtual - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(item => new AuditoriaDto(
                    item.Id, item.EntityName, item.EntityId, item.Action,
                    item.UserId, item.IpAddress, item.TimestampUtc, item.OldValues, item.NewValues))
                .ToListAsync(cancellationToken);

            return Results.Ok(new { total, pagina = paginaAtual, tamanho = tamanhoPagina, itens });
        });
    }

    /// <summary>Estado de licença de um módulo para um tenant.</summary>
    /// <param name="Modulo">Nome canônico do módulo.</param>
    /// <param name="Ativo">Indica se o módulo está licenciado e ativo.</param>
    private sealed record ModuloTenantDto(string Modulo, bool Ativo);

    /// <summary>Corpo da requisição de ativação/desativação de um módulo.</summary>
    /// <param name="Ativo">Estado desejado da licença.</param>
    private sealed record DefinirModuloRequest(bool Ativo);

    /// <summary>Entrada da trilha de auditoria para o visualizador administrativo.</summary>
    private sealed record AuditoriaDto(
        Guid Id,
        string EntityName,
        string? EntityId,
        string Action,
        string? UserId,
        string? IpAddress,
        DateTime TimestampUtc,
        string? OldValues,
        string? NewValues);
}
