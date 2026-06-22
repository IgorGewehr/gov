using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="RestoAPagar"/>.</summary>
public interface IRestoAPagarRepository
{
    /// <summary>Marca um novo resto a pagar para inserção.</summary>
    /// <param name="resto">Resto a pagar a adicionar.</param>
    void Adicionar(RestoAPagar resto);

    /// <summary>Obtém um resto a pagar por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resto a pagar, ou <c>null</c>.</returns>
    Task<RestoAPagar?> ObterPorIdAsync(RestoAPagarId id, CancellationToken cancellationToken);

    /// <summary>Lista os restos a pagar de um exercício de inscrição.</summary>
    /// <param name="exercicioInscricao">Exercício de inscrição.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Restos a pagar do exercício.</returns>
    Task<IReadOnlyList<RestoAPagar>> ListarPorExercicioInscricaoAsync(int exercicioInscricao, CancellationToken cancellationToken);
}
