using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de contas contábeis.</summary>
public sealed class ContaContabilRepository(FinancasDbContext context) : IContaContabilRepository
{
    /// <inheritdoc />
    public void Adicionar(ContaContabil conta)
    {
        ArgumentNullException.ThrowIfNull(conta);
        context.ContasContabeis.Add(conta);
    }

    /// <inheritdoc />
    public Task<ContaContabil?> ObterPorCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        var alvo = CodigoContabil.De(codigo);
        return context.ContasContabeis.FirstOrDefaultAsync(conta => conta.Codigo == alvo, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ContaContabil?> ObterPorIdAsync(ContaContabilId id, CancellationToken cancellationToken)
        => context.ContasContabeis.FirstOrDefaultAsync(conta => conta.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContaContabil>> ListarTodasAsync(CancellationToken cancellationToken)
        => await context.ContasContabeis.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        var alvo = CodigoContabil.De(codigo);
        return context.ContasContabeis.AnyAsync(conta => conta.Codigo == alvo, cancellationToken);
    }
}
