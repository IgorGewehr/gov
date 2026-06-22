using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de liquidações.</summary>
public sealed class LiquidacaoRepository(FinancasDbContext context) : ILiquidacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Liquidacao liquidacao)
    {
        ArgumentNullException.ThrowIfNull(liquidacao);
        context.Liquidacoes.Add(liquidacao);
    }

    /// <inheritdoc />
    public Task<Liquidacao?> ObterPorIdAsync(LiquidacaoId id, CancellationToken cancellationToken)
        => context.Liquidacoes.FirstOrDefaultAsync(liquidacao => liquidacao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Liquidacao>> ListarPorEmpenhoAsync(EmpenhoId empenhoId, CancellationToken cancellationToken)
        => await context.Liquidacoes
            .Where(liquidacao => liquidacao.EmpenhoId == empenhoId)
            .OrderBy(liquidacao => liquidacao.DataLiquidacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
