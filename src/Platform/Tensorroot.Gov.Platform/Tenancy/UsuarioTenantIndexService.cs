using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Platform.Persistence;

namespace Tensorroot.Gov.Platform.Tenancy;

/// <summary>
/// Servico do indice central email → tenant (banco de CONTROLE). Permite ao modulo Identidade
/// resolver o tenant de um login (os usuarios vivem no banco dedicado de cada tenant) e registrar
/// novas associacoes ao provisionar usuarios. NUNCA guarda credenciais.
/// </summary>
public interface IUsuarioTenantIndexService
{
    /// <summary>
    /// Resolve o tenant de um e-mail de login. Retorna <c>null</c> quando o e-mail e desconhecido —
    /// o chamador (autenticacao) deve tratar isso de forma indistinguivel de senha incorreta.
    /// </summary>
    /// <param name="email">E-mail informado (sera normalizado para minusculas).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tenant correspondente, ou <c>null</c> se inexistente.</returns>
    Task<Guid?> ResolverTenantPorEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Resolve o tenant (id + nome) de um e-mail de login, em uma unica consulta. Retorna <c>null</c>
    /// quando o e-mail e desconhecido.
    /// </summary>
    /// <param name="email">E-mail informado (sera normalizado para minusculas).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Par (tenantId, nome) correspondente, ou <c>null</c> se inexistente.</returns>
    Task<(Guid TenantId, string? Nome)?> ResolverTenantDetalhePorEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Registra (idempotentemente) a associacao email → tenant. Falha se o e-mail ja pertencer a
    /// OUTRO tenant (unicidade global de login).
    /// </summary>
    /// <param name="email">E-mail de login (sera normalizado).</param>
    /// <param name="tenantId">Tenant dono do usuario.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <exception cref="InvalidOperationException">Se o e-mail ja estiver em uso por outro tenant.</exception>
    Task RegistrarAsync(string email, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Remove a associacao de um e-mail (ex.: ao excluir/realocar um usuario).</summary>
    /// <param name="email">E-mail de login (sera normalizado).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RemoverAsync(string email, CancellationToken cancellationToken);
}

/// <summary>Implementacao EF Core do <see cref="IUsuarioTenantIndexService"/> sobre o <see cref="PlatformDbContext"/>.</summary>
public sealed class UsuarioTenantIndexService(PlatformDbContext context) : IUsuarioTenantIndexService
{
    /// <inheritdoc />
    public async Task<Guid?> ResolverTenantPorEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizado = Normalizar(email);
        if (normalizado is null)
        {
            return null;
        }

        var indice = await context.UsuariosTenantIndex
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Email == normalizado, cancellationToken)
            .ConfigureAwait(false);

        return indice?.TenantId;
    }

    /// <inheritdoc />
    public async Task<(Guid TenantId, string? Nome)?> ResolverTenantDetalhePorEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizado = Normalizar(email);
        if (normalizado is null)
        {
            return null;
        }

        var detalhe = await context.UsuariosTenantIndex
            .AsNoTracking()
            .Where(item => item.Email == normalizado)
            .Join(
                context.Tenants,
                indice => indice.TenantId,
                tenant => tenant.Id,
                (indice, tenant) => new { tenant.Id, tenant.Nome })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return detalhe is null ? null : (detalhe.Id, detalhe.Nome);
    }

    /// <inheritdoc />
    public async Task RegistrarAsync(string email, Guid tenantId, CancellationToken cancellationToken)
    {
        var normalizado = Normalizar(email)
            ?? throw new ArgumentException("E-mail invalido.", nameof(email));

        var existente = await context.UsuariosTenantIndex
            .FirstOrDefaultAsync(item => item.Email == normalizado, cancellationToken)
            .ConfigureAwait(false);

        if (existente is not null)
        {
            if (existente.TenantId != tenantId)
            {
                throw new InvalidOperationException("E-mail de login ja em uso por outro tenant.");
            }

            return;
        }

        // Integridade referencial (control-plane): não registra índice para tenant inexistente — um
        // órfão faria o login falhar de forma indistinguível de e-mail não cadastrado (o JOIN com
        // Tenants em ResolverTenantDetalhePorEmailAsync retornaria null silenciosamente).
        var tenantExiste = await context.Tenants
            .AnyAsync(item => item.Id == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!tenantExiste)
        {
            throw new InvalidOperationException($"Tenant {tenantId} nao encontrado para registrar o indice de login.");
        }

        context.UsuariosTenantIndex.Add(new UsuarioTenantIndex(normalizado, tenantId));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoverAsync(string email, CancellationToken cancellationToken)
    {
        var normalizado = Normalizar(email);
        if (normalizado is null)
        {
            return;
        }

        var existente = await context.UsuariosTenantIndex
            .FirstOrDefaultAsync(item => item.Email == normalizado, cancellationToken)
            .ConfigureAwait(false);

        if (existente is not null)
        {
            context.UsuariosTenantIndex.Remove(existente);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? Normalizar(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLower(CultureInfo.InvariantCulture);
    }
}
