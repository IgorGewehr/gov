using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do agregado de encerramento de exercício.</summary>
public sealed class EncerramentoExercicioRepository(FinancasDbContext context) : IEncerramentoExercicioRepository
{
    /// <inheritdoc />
    public void Adicionar(EncerramentoExercicio encerramento)
    {
        ArgumentNullException.ThrowIfNull(encerramento);
        context.EncerramentosExercicio.Add(encerramento);
    }

    /// <inheritdoc />
    public Task<EncerramentoExercicio?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.EncerramentosExercicio
            .FirstOrDefaultAsync(encerramento => encerramento.Exercicio == exercicio, cancellationToken);
}
