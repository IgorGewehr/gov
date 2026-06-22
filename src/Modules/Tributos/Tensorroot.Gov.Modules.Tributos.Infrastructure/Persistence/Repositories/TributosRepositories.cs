using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de contribuintes.</summary>
public sealed class ContribuinteRepository(TributosDbContext context) : IContribuinteRepository
{
    /// <inheritdoc />
    public void Adicionar(Contribuinte contribuinte)
    {
        ArgumentNullException.ThrowIfNull(contribuinte);
        context.Contribuintes.Add(contribuinte);
    }

    /// <inheritdoc />
    public Task<Contribuinte?> ObterPorIdAsync(ContribuinteId id, CancellationToken cancellationToken)
        => context.Contribuintes.FirstOrDefaultAsync(contribuinte => contribuinte.Id == id, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de lançamentos.</summary>
public sealed class LancamentoRepository(TributosDbContext context) : ILancamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(Lancamento lancamento)
    {
        ArgumentNullException.ThrowIfNull(lancamento);
        context.Lancamentos.Add(lancamento);
    }

    /// <inheritdoc />
    public Task<Lancamento?> ObterPorIdAsync(LancamentoId id, CancellationToken cancellationToken)
        => context.Lancamentos.FirstOrDefaultAsync(lancamento => lancamento.Id == id, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de dívidas ativas.</summary>
public sealed class DividaAtivaRepository(TributosDbContext context) : IDividaAtivaRepository
{
    /// <inheritdoc />
    public void Adicionar(DividaAtiva dividaAtiva)
    {
        ArgumentNullException.ThrowIfNull(dividaAtiva);
        context.DividasAtivas.Add(dividaAtiva);
    }

    /// <inheritdoc />
    public Task<DividaAtiva?> ObterPorIdAsync(DividaAtivaId id, CancellationToken cancellationToken)
        => context.DividasAtivas.FirstOrDefaultAsync(divida => divida.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DividaAtiva>> ListarPorContribuinteAsync(ContribuinteId contribuinteId, CancellationToken cancellationToken)
        => await context.DividasAtivas
            .Where(divida => divida.ContribuinteId == contribuinteId)
            .OrderBy(divida => divida.DataInscricao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
