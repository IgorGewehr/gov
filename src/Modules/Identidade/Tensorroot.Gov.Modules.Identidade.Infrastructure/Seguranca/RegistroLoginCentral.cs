using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;

/// <summary>
/// Adaptador do <see cref="IRegistroLoginCentral"/> sobre o <see cref="IUsuarioTenantIndexService"/>
/// da plataforma. Mantem o indice central email → tenant (banco de CONTROLE) sincronizado com a
/// criacao/edicao de usuarios no banco DEDICADO do tenant. NUNCA registra credenciais.
/// </summary>
public sealed class RegistroLoginCentral(IUsuarioTenantIndexService indice) : IRegistroLoginCentral
{
    /// <inheritdoc />
    public Task RegistrarAsync(string email, Guid tenantId, CancellationToken cancellationToken)
        => indice.RegistrarAsync(email, tenantId, cancellationToken);

    /// <inheritdoc />
    public async Task AtualizarAsync(string emailAntigo, string emailNovo, Guid tenantId, CancellationToken cancellationToken)
    {
        // Garante a unicidade global do novo e-mail ANTES de soltar o antigo.
        await indice.RegistrarAsync(emailNovo, tenantId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(emailAntigo, emailNovo, StringComparison.OrdinalIgnoreCase))
        {
            await indice.RemoverAsync(emailAntigo, cancellationToken).ConfigureAwait(false);
        }
    }
}
