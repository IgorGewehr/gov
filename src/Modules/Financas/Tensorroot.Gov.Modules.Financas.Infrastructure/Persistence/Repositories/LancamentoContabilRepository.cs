using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de lançamentos contábeis.</summary>
public sealed class LancamentoContabilRepository(FinancasDbContext context) : ILancamentoContabilRepository
{
    /// <inheritdoc />
    public void Adicionar(LancamentoContabil lancamento)
    {
        ArgumentNullException.ThrowIfNull(lancamento);
        context.LancamentosContabeis.Add(lancamento);
    }

    /// <inheritdoc />
    public Task<LancamentoContabil?> ObterPorIdAsync(LancamentoContabilId id, CancellationToken cancellationToken)
        => context.LancamentosContabeis
            .Include(lancamento => lancamento.Partidas)
            .FirstOrDefaultAsync(lancamento => lancamento.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteParaOrigemAsync(Guid origemReferenciaId, Guid eventoContabilId, CancellationToken cancellationToken)
        => context.LancamentosContabeis.AnyAsync(
            lancamento => lancamento.OrigemReferenciaId == origemReferenciaId
                && lancamento.EventoContabilId == eventoContabilId,
            cancellationToken);
}
