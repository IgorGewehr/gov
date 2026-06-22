using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.Modules.Identidade.Domain.Permissoes;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.ApiHost.Admin;

/// <summary>
/// Endpoints ADMINISTRATIVOS (só-admin) para configurar as licenças de MÓDULO de um tenant.
/// Protegidos pela permissão RBAC <c>admin.modulos.configurar</c> (claim "perm"): sem ela, 403.
/// Reusa os serviços da Plataforma multi-tenant (catálogo de licenças por tenant).
/// </summary>
internal static class AdminEndpoints
{
    // XT-1: configurar licencas de modulo e operacao de PLATAFORMA (banco de controle, nao isolado
    // por tenant). Gate por permissao de plataforma FORA de Permissoes.Todas — admin de tenant nunca
    // a recebe (antes era "admin.modulos.configurar", que TODO admin de tenant tinha).
    private const string PermissaoConfigurarModulos = Permissoes.PlataformaModulosConfigurar;
    private const string PermissaoVerAuditoria = "admin.auditoria.ver";
    private const string PermissaoVerificarAuditoria = "admin.auditoria.verificar";
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
            ITenantContext tenant,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // XT-1 (defesa-em-profundidade): se ha um tenant resolvido no contexto (principal de
            // tenant), a rota NAO pode operar outro tenant. 403 AUDITADO. Operador de plataforma sem
            // tenant ligado (onboarding) passa.
            if (TenantDaRotaDivergeDoContexto(tenantId, tenant, loggerFactory, httpContext, "listar-modulos"))
            {
                return ProibidoCrossTenant();
            }

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
            ITenantContext tenant,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(requisicao);

            // XT-1 (defesa-em-profundidade): tenant da rota deve casar com o do contexto, quando ha um.
            if (TenantDaRotaDivergeDoContexto(tenantId, tenant, loggerFactory, httpContext, "configurar-modulo"))
            {
                return ProibidoCrossTenant();
            }

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
            var brutos = await consulta
                .OrderByDescending(item => item.TimestampUtc)
                .Skip((paginaAtual - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(item => new AuditoriaDto(
                    item.Id, item.EntityName, item.EntityId, item.Action,
                    item.UserId, item.IpAddress, item.TimestampUtc, item.OldValues, item.NewValues))
                .ToListAsync(cancellationToken);

            // LG-3: o visualizador NUNCA devolve PII em claro. A redacao ja ocorre na ESCRITA
            // (PoliticaRedacaoAuditoria no interceptor); aqui mascaramos novamente como defesa-em-
            // profundidade — cobre linhas LEGADAS gravadas antes de LG-3 (CPF/NIS/dado clinico crus).
            var itens = brutos
                .Select(item => item with
                {
                    OldValues = PoliticaRedacaoAuditoria.MascararJson(item.OldValues),
                    NewValues = PoliticaRedacaoAuditoria.MascararJson(item.NewValues),
                })
                .ToList();

            return Results.Ok(new { total, pagina = paginaAtual, tamanho = tamanhoPagina, itens });
        });

        // === VERIFICADOR de imutabilidade (A2) — só-admin, permissão dedicada ===
        // Recomputa a cadeia de hash do tenant e aponta a 1ª linha adulterada/removida. Detecção
        // determinística que não depende do banco impor WORM (complementa o trigger em SqlServer).
        var verificacao = endpoints
            .MapGroup("/api/admin/auditoria/verificacao")
            .WithTags("Admin")
            .RequirePermission(PermissaoVerificarAuditoria);

        verificacao.MapGet("/", async (
            AuditoriaReadDbContext contexto,
            IVerificadorTrilhaAuditoria verificador,
            ITenantContext tenant,
            CancellationToken cancellationToken) =>
        {
            var resultado = await verificador.VerificarAsync(
                contexto.Trilha.AsNoTracking(), tenant.TenantId, cancellationToken);

            // 200 quando íntegra; 409 (Conflict) quando há adulteração — facilita alertas/monitoração.
            return resultado.Integra
                ? Results.Ok(resultado)
                : Results.Json(resultado, statusCode: StatusCodes.Status409Conflict);
        });
    }

    /// <summary>
    /// Decisão PURA da defesa-em-profundidade XT-1 (sem I/O, testável): nega quando há um tenant
    /// resolvido no contexto (principal de TENANT) e o tenant da rota é OUTRO. Operador de PLATAFORMA
    /// (sem tenant ligado ao principal) NÃO é negado — opera qualquer tenant no onboarding.
    /// </summary>
    /// <param name="tenantIdRota">Tenant alvo vindo da rota.</param>
    /// <param name="contextoTemTenant">Se há um tenant resolvido no contexto.</param>
    /// <param name="tenantContexto">Tenant do contexto (válido somente quando <paramref name="contextoTemTenant"/>).</param>
    /// <returns><c>true</c> se a operação deve ser negada por divergência cross-tenant.</returns>
    internal static bool DeveNegarOperacaoCrossTenant(Guid tenantIdRota, bool contextoTemTenant, Guid tenantContexto)
        => contextoTemTenant && tenantContexto != tenantIdRota;

    /// <summary>
    /// Defesa-em-profundidade XT-1: indica se o <paramref name="tenantIdRota"/> diverge do tenant
    /// resolvido no contexto da requisição. Operador de PLATAFORMA (sem tenant ligado ao principal)
    /// passa — opera qualquer tenant para onboarding. Já um principal de TENANT (HasTenant) só pode
    /// operar o próprio tenant: divergência é tentativa cross-tenant, registrada (AUDITADA) e negada.
    /// </summary>
    private static bool TenantDaRotaDivergeDoContexto(
        Guid tenantIdRota,
        ITenantContext tenant,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        string acao)
    {
        if (!DeveNegarOperacaoCrossTenant(tenantIdRota, tenant.HasTenant, tenant.HasTenant ? tenant.TenantId : Guid.Empty))
        {
            return false;
        }

        var logger = loggerFactory.CreateLogger("Tensorroot.Gov.ApiHost.Admin.CrossTenant");
        logger.LogWarning(
            "XT-1 negado: principal do tenant {TenantContexto} tentou {Acao} no tenant {TenantRota} (IP {Ip}, sub {Sub}).",
            tenant.TenantId,
            acao,
            tenantIdRota,
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "?",
            httpContext.User.FindFirst("sub")?.Value ?? "?");

        return true;
    }

    /// <summary>Resposta neutra de 403 para tentativa cross-tenant (não revela detalhe do alvo).</summary>
    private static IResult ProibidoCrossTenant()
        => Results.Json(
            new { erro = "Operacao nao permitida para o tenant do contexto." },
            statusCode: StatusCodes.Status403Forbidden);

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
