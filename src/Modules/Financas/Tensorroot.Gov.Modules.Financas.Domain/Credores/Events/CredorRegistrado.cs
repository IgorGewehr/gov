using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Credores.Events;

/// <summary>Um novo credor/fornecedor foi cadastrado no ente.</summary>
/// <param name="CredorId">Identificador do credor.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Documento">Documento (CPF/CNPJ) normalizado.</param>
public sealed record CredorRegistrado(
    CredorId CredorId,
    Guid TenantId,
    string Documento) : IDomainEvent;
