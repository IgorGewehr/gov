using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório da tabela de IRRF/PJ.</summary>
public sealed class TabelaIrrfServicosRepository(FinancasDbContext context) : ITabelaIrrfServicosRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaIrrfServicos tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasIrrfServicos.Add(tabela);
    }

    /// <inheritdoc />
    public async Task<TabelaIrrfServicos?> ObterVigenteAsync(DateOnly data, CancellationToken cancellationToken)
    {
        var candidatas = await context.TabelasIrrfServicos
            .Include(tabela => tabela.Faixas)
            .Where(tabela => tabela.VigenciaInicio <= data
                && (tabela.VigenciaFim == null || tabela.VigenciaFim >= data))
            .OrderByDescending(tabela => tabela.VigenciaInicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return candidatas.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TabelaIrrfServicos>> ListarTodasAsync(CancellationToken cancellationToken)
        => await context.TabelasIrrfServicos
            .Include(tabela => tabela.Faixas)
            .OrderByDescending(tabela => tabela.VigenciaInicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> ExisteVigenciaAsync(DateOnly vigenciaInicio, CancellationToken cancellationToken)
        => context.TabelasIrrfServicos.AnyAsync(tabela => tabela.VigenciaInicio == vigenciaInicio, cancellationToken);
}
