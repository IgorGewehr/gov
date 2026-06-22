using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de imóveis.</summary>
public sealed class ImovelRepository(TributosDbContext context) : IImovelRepository
{
    /// <inheritdoc />
    public void Adicionar(Imovel imovel)
    {
        ArgumentNullException.ThrowIfNull(imovel);
        context.Imoveis.Add(imovel);
    }

    /// <inheritdoc />
    public Task<Imovel?> ObterPorIdAsync(ImovelId id, CancellationToken cancellationToken)
        => context.Imoveis.FirstOrDefaultAsync(imovel => imovel.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Imovel>> ListarPorProprietarioAsync(ContribuinteId proprietarioId, CancellationToken cancellationToken)
        => await context.Imoveis
            .Where(imovel => imovel.ProprietarioId == proprietarioId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório de PGV.</summary>
public sealed class PlantaValoresRepository(TributosDbContext context) : IPlantaValoresRepository
{
    /// <inheritdoc />
    public void Adicionar(PlantaValores planta)
    {
        ArgumentNullException.ThrowIfNull(planta);
        context.PlantasValores.Add(planta);
    }

    /// <inheritdoc />
    public Task<PlantaValores?> ObterPorIdAsync(PlantaValoresId id, CancellationToken cancellationToken)
        => context.PlantasValores
            .Include(planta => planta.Zonas)
            .Include(planta => planta.Fatores)
            .FirstOrDefaultAsync(planta => planta.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<PlantaValores?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken)
        => context.PlantasValores
            .Include(planta => planta.Zonas)
            .Include(planta => planta.Fatores)
            .FirstOrDefaultAsync(planta => planta.Exercicio == exercicio && planta.Vigente, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de tabelas de alíquota do IPTU.</summary>
public sealed class TabelaAliquotaIptuRepository(TributosDbContext context) : ITabelaAliquotaIptuRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaAliquotaIptu tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasAliquotaIptu.Add(tabela);
    }

    /// <inheritdoc />
    public Task<TabelaAliquotaIptu?> ObterPorIdAsync(TabelaAliquotaIptuId id, CancellationToken cancellationToken)
        => context.TabelasAliquotaIptu
            .Include(tabela => tabela.Faixas)
            .FirstOrDefaultAsync(tabela => tabela.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<TabelaAliquotaIptu?> ObterVigenteAsync(int exercicio, bool edificado, CancellationToken cancellationToken)
        => context.TabelasAliquotaIptu
            .Include(tabela => tabela.Faixas)
            .FirstOrDefaultAsync(tabela => tabela.Exercicio == exercicio && tabela.Edificado == edificado && tabela.Vigente, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de DAM.</summary>
public sealed class DamRepository(TributosDbContext context) : IDamRepository
{
    /// <inheritdoc />
    public void Adicionar(Dam dam)
    {
        ArgumentNullException.ThrowIfNull(dam);
        context.Dams.Add(dam);
    }

    /// <inheritdoc />
    public Task<Dam?> ObterPorIdAsync(DamId id, CancellationToken cancellationToken)
        => context.Dams
            .Include(dam => dam.Parcelas)
            .FirstOrDefaultAsync(dam => dam.Id == id, cancellationToken);
}
