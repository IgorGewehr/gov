using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cofre.Domain;

/// <summary>Evento: um certificado A1 foi cadastrado no cofre do tenant.</summary>
/// <param name="CertificadoId">Identificador do certificado cadastrado.</param>
/// <param name="TenantId">Tenant dono do certificado.</param>
/// <param name="Thumbprint">Thumbprint (identificacao, nao sigiloso).</param>
public sealed record CertificadoA1Cadastrado(Guid CertificadoId, Guid TenantId, string Thumbprint) : IDomainEvent;

/// <summary>Evento: o certificado anterior foi substituido na rotacao.</summary>
/// <param name="CertificadoId">Identificador do certificado substituido.</param>
/// <param name="TenantId">Tenant dono do certificado.</param>
public sealed record CertificadoA1Substituido(Guid CertificadoId, Guid TenantId) : IDomainEvent;

/// <summary>Evento: o certificado foi revogado manualmente.</summary>
/// <param name="CertificadoId">Identificador do certificado revogado.</param>
/// <param name="TenantId">Tenant dono do certificado.</param>
public sealed record CertificadoA1Revogado(Guid CertificadoId, Guid TenantId) : IDomainEvent;
