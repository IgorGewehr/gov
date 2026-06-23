using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.PortalPublico;

/// <summary>
/// Resolve o tenant a partir do SLUG publico, SEM JWT (cidadao anonimo). Caminho de leitura dedicado e
/// auditado sobre o catalogo CENTRAL da plataforma (mesma necessidade do login do Identidade, que
/// resolve o tenant por email): le SOMENTE o mapa publico nome→tenant. O slug e DERIVADO do nome do ente
/// pela mesma normalizacao de <see cref="PortalPublicoConfig"/> — assim nao exige alterar o catalogo da
/// plataforma. Verifica a LICENCA do modulo Transparencia (senao 404, sem revelar existencia).
/// <para>
/// IMPORTANTE: este resolver NAO abre nenhum banco dedicado de tenant nem le dado de negocio — so o
/// catalogo (Nome/Ativo do tenant). Apos a resolucao, o endpoint fixa o <c>TenantOverride</c> e TODAS as
/// leituras subsequentes passam pelo Global Query Filter por TenantId (anti-vazamento cross-tenant).
/// </para>
/// </summary>
public sealed class TenantPublicoResolver(
    PlatformDbContext plataforma,
    ITenantModuleProvider modulos) : ITenantPublicoResolver
{
    private const string NomeModulo = "Transparencia";

    /// <inheritdoc />
    public async Task<EntePublicoResolvido?> ResolverPorSlugAsync(string slug, CancellationToken cancellationToken)
    {
        var alvo = PortalPublicoConfig.NormalizarSlug(slug);
        if (alvo.Length == 0)
        {
            return null;
        }

        // Le SO o catalogo central (Id/Nome dos tenants ativos) — sem tocar banco dedicado nem dado sensivel.
        var entes = await plataforma.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Ativo)
            .Select(tenant => new { tenant.Id, tenant.Nome })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // O slug publico e derivado deterministicamente do nome do ente (kebab-case ASCII).
        var ente = entes.FirstOrDefault(t =>
            string.Equals(PortalPublicoConfig.NormalizarSlug(t.Nome), alvo, StringComparison.Ordinal));
        if (ente is null)
        {
            return null;
        }

        // Licenca: so responde se o tenant tiver o modulo Transparencia licenciado (senao 404, sem revelar).
        var licenciado = await modulos.IsModuleEnabledAsync(ente.Id, NomeModulo, cancellationToken).ConfigureAwait(false);
        return licenciado ? new EntePublicoResolvido(ente.Id, alvo, ente.Nome) : null;
    }
}
