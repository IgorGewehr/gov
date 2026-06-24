using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de contas da tesouraria.</summary>
public sealed class ContaFinanceiraRepository(FinancasDbContext context) : IContaFinanceiraRepository
{
    /// <inheritdoc />
    public void Adicionar(ContaFinanceira conta)
    {
        ArgumentNullException.ThrowIfNull(conta);
        context.ContasFinanceiras.Add(conta);
    }

    /// <inheritdoc />
    public Task<ContaFinanceira?> ObterPorIdAsync(ContaFinanceiraId id, CancellationToken cancellationToken)
        => context.ContasFinanceiras
            .Include(conta => conta.Movimentos)
            .FirstOrDefaultAsync(conta => conta.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContaFinanceira>> ListarAsync(CancellationToken cancellationToken)
        => await context.ContasFinanceiras
            .OrderBy(conta => conta.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
