namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Entidade cujos metadados de auditoria (criação/alteração) são preenchidos
/// AUTOMATICAMENTE pelo interceptor de auditoria do EF Core — não definir manualmente.
/// </summary>
public interface IAuditable
{
    /// <summary>Data/hora (UTC) de criação do registro.</summary>
    DateTime CreatedOnUtc { get; set; }

    /// <summary>Identificação de quem criou o registro.</summary>
    string? CreatedBy { get; set; }

    /// <summary>Data/hora (UTC) da última alteração, se houver.</summary>
    DateTime? ModifiedOnUtc { get; set; }

    /// <summary>Identificação de quem alterou o registro por último.</summary>
    string? ModifiedBy { get; set; }
}
