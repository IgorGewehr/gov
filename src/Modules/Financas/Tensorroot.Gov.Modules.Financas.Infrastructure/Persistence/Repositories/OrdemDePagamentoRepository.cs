using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de ordens de pagamento.</summary>
public sealed class OrdemDePagamentoRepository(FinancasDbContext context) : IOrdemDePagamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(OrdemDePagamento ordem)
    {
        ArgumentNullException.ThrowIfNull(ordem);
        context.OrdensDePagamento.Add(ordem);
    }

    /// <inheritdoc />
    public Task<OrdemDePagamento?> ObterPorIdAsync(OrdemDePagamentoId id, CancellationToken cancellationToken)
        => context.OrdensDePagamento
            .Include(ordem => ordem.Itens)
            .FirstOrDefaultAsync(ordem => ordem.Id == id, cancellationToken);
}
