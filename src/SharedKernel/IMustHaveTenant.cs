namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Contrato obrigatório para toda entidade isolada por inquilino (tenant).
/// Habilita o Global Query Filter e o carimbo automático de <see cref="TenantId"/>.
/// </summary>
public interface IMustHaveTenant
{
    /// <summary>Identificador do tenant (ente público) dono do registro.</summary>
    Guid TenantId { get; }
}
