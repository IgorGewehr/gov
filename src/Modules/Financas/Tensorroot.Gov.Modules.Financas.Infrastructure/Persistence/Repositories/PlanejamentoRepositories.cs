using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de PPA (com a árvore Programa/Ação/Meta — AutoInclude).</summary>
public sealed class PpaRepository(FinancasDbContext context) : IPpaRepository
{
    /// <inheritdoc />
    public void Adicionar(PlanoPlurianual ppa)
    {
        ArgumentNullException.ThrowIfNull(ppa);
        context.Ppas.Add(ppa);
    }

    /// <inheritdoc />
    public Task<PlanoPlurianual?> ObterPorIdAsync(PpaId id, CancellationToken cancellationToken)
        => context.Ppas.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<PlanoPlurianual?> ObterVigentePorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.Ppas
            .Where(p => p.Situacao == SituacaoPpa.Vigente && p.AnoInicio <= exercicio && p.AnoFim >= exercicio)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanoPlurianual>> ListarAsync(CancellationToken cancellationToken)
        => await context.Ppas.OrderByDescending(p => p.AnoInicio).ToListAsync(cancellationToken).ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório de LDO.</summary>
public sealed class LdoRepository(FinancasDbContext context) : ILdoRepository
{
    /// <inheritdoc />
    public void Adicionar(LeiDiretrizes ldo)
    {
        ArgumentNullException.ThrowIfNull(ldo);
        context.Ldos.Add(ldo);
    }

    /// <inheritdoc />
    public Task<LeiDiretrizes?> ObterPorIdAsync(LdoId id, CancellationToken cancellationToken)
        => context.Ldos.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<LeiDiretrizes?> ObterVigentePorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.Ldos
            .Where(l => l.Situacao == SituacaoLdo.Vigente && l.Exercicio == exercicio)
            .FirstOrDefaultAsync(cancellationToken);
}

/// <summary>Implementação EF Core do repositório de LOA.</summary>
public sealed class LoaRepository(FinancasDbContext context) : ILoaRepository
{
    /// <inheritdoc />
    public void Adicionar(LeiOrcamentariaAnual loa)
    {
        ArgumentNullException.ThrowIfNull(loa);
        context.Loas.Add(loa);
    }

    /// <inheritdoc />
    public Task<LeiOrcamentariaAnual?> ObterPorIdAsync(LoaId id, CancellationToken cancellationToken)
        => context.Loas.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<LeiOrcamentariaAnual?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.Loas.Where(l => l.Exercicio == exercicio).FirstOrDefaultAsync(cancellationToken);
}

/// <summary>Implementação EF Core do repositório de crédito adicional.</summary>
public sealed class CreditoAdicionalRepository(FinancasDbContext context) : ICreditoAdicionalRepository
{
    /// <inheritdoc />
    public void Adicionar(CreditoAdicional credito)
    {
        ArgumentNullException.ThrowIfNull(credito);
        context.CreditosAdicionais.Add(credito);
    }

    /// <inheritdoc />
    public Task<CreditoAdicional?> ObterPorIdAsync(CreditoAdicionalId id, CancellationToken cancellationToken)
        => context.CreditosAdicionais.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<decimal> SomarSuplementacoesPorDecretoAsync(LoaId loaId, CancellationToken cancellationToken)
    {
        // O valor monetario passa por value-converter; somamos no cliente apos filtrar no banco.
        var valores = await context.CreditosAdicionais
            .Where(c => c.LoaId == loaId
                && c.Especie == EspecieCredito.Suplementar
                && c.PorDecreto
                && c.Situacao == SituacaoCredito.Aberto)
            .Select(c => c.Valor)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return valores.Aggregate(0m, (acc, v) => acc + v.Valor);
    }
}
