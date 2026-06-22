using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Cofre.Application.Abstractions;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence;

/// <summary>
/// Repositorio EF Core do cofre, tenant-scoped via Global Query Filter (A1-DESIGN §7 risco 8): toda
/// consulta resolve apenas o tenant corrente; nao ha acesso por id cru cross-tenant.
/// </summary>
public sealed class CofreCertificadoRepository(CofreDbContext context) : ICofreCertificadoRepository
{
    /// <inheritdoc />
    public Task<CertificadoA1Cofre?> ObterAtivoAsync(CancellationToken cancellationToken)
        => context.Certificados.FirstOrDefaultAsync(
            certificado => certificado.Status == CertificadoStatus.Ativo,
            cancellationToken);

    /// <inheritdoc />
    public void Adicionar(CertificadoA1Cofre certificado)
    {
        ArgumentNullException.ThrowIfNull(certificado);
        context.Certificados.Add(certificado);
    }

    /// <inheritdoc />
    public void RegistrarUso(AssinaturaAuditLog registro)
    {
        ArgumentNullException.ThrowIfNull(registro);
        context.AssinaturaAuditLogs.Add(registro);
    }

    /// <inheritdoc />
    public Task<int> SalvarAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
