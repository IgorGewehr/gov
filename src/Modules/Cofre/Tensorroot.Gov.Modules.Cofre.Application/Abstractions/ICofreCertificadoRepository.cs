using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Application.Abstractions;

/// <summary>
/// Repositorio do cofre de certificados A1, sempre resolvido no banco DEDICADO do tenant corrente
/// (Global Query Filter por TenantId — A1-DESIGN §7, risco 8). Nunca aceita id cru cross-tenant.
/// </summary>
public interface ICofreCertificadoRepository
{
    /// <summary>Recupera o certificado A1 ATIVO do tenant corrente (ou nulo se nao houver).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O certificado ativo ou nulo.</returns>
    Task<CertificadoA1Cofre?> ObterAtivoAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona um certificado ao cofre.</summary>
    /// <param name="certificado">Certificado a persistir.</param>
    void Adicionar(CertificadoA1Cofre certificado);

    /// <summary>Persiste a trilha de auditoria de USO da assinatura (sucesso ou falha).</summary>
    /// <param name="registro">Registro imutavel de auditoria de uso.</param>
    void RegistrarUso(AssinaturaAuditLog registro);

    /// <summary>Confirma a unidade de trabalho (persiste as alteracoes pendentes).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    Task<int> SalvarAsync(CancellationToken cancellationToken);
}
