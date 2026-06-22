namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Unidade de trabalho de um módulo: confirma, de forma transacional, as alterações
/// pendentes (incluindo a gravação das mensagens de Outbox na mesma transação).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persiste as alterações pendentes.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de registros afetados.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
