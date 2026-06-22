using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de Restos a Pagar.</summary>
public sealed class RestoAPagarRepository(FinancasDbContext context) : IRestoAPagarRepository
{
    /// <inheritdoc />
    public void Adicionar(RestoAPagar resto)
    {
        ArgumentNullException.ThrowIfNull(resto);
        context.RestosAPagar.Add(resto);
    }

    /// <inheritdoc />
    public Task<RestoAPagar?> ObterPorIdAsync(RestoAPagarId id, CancellationToken cancellationToken)
        => context.RestosAPagar.FirstOrDefaultAsync(resto => resto.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RestoAPagar>> ListarPorExercicioInscricaoAsync(int exercicioInscricao, CancellationToken cancellationToken)
        => await context.RestosAPagar
            .Where(resto => resto.ExercicioInscricao == exercicioInscricao)
            .OrderBy(resto => resto.ExercicioOrigem)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
