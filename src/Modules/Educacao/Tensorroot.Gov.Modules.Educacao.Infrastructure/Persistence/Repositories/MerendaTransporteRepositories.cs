using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Cardapio"/>. Tenant-scoped via Global Query Filter.</summary>
public sealed class CardapioRepository(EducacaoDbContext context) : ICardapioRepository
{
    /// <inheritdoc />
    public void Adicionar(Cardapio cardapio)
    {
        ArgumentNullException.ThrowIfNull(cardapio);
        context.Cardapios.Add(cardapio);
    }

    /// <inheritdoc />
    public Task<Cardapio?> ObterPorIdAsync(CardapioId id, CancellationToken cancellationToken)
        => context.Cardapios
            .Include(cardapio => cardapio.Itens)
            .FirstOrDefaultAsync(cardapio => cardapio.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Cardapio>> ListarAsync(EscolaId? escolaId, DateOnly? semana, CancellationToken cancellationToken)
    {
        var consulta = context.Cardapios.Include(cardapio => cardapio.Itens).AsQueryable();

        if (escolaId is { } escola)
        {
            consulta = consulta.Where(cardapio => cardapio.EscolaId == escola);
        }

        if (semana is { } semanaFiltro)
        {
            consulta = consulta.Where(cardapio => cardapio.Semana == semanaFiltro);
        }

        return await consulta
            .OrderByDescending(cardapio => cardapio.Semana)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="DistribuicaoMerenda"/>. Tenant-scoped.</summary>
public sealed class DistribuicaoMerendaRepository(EducacaoDbContext context) : IDistribuicaoMerendaRepository
{
    /// <inheritdoc />
    public void Adicionar(DistribuicaoMerenda distribuicao)
    {
        ArgumentNullException.ThrowIfNull(distribuicao);
        context.DistribuicoesMerenda.Add(distribuicao);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DistribuicaoMerenda>> ListarPorEscolaEPeriodoAsync(
        EscolaId escolaId,
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken)
        => await context.DistribuicoesMerenda
            .Include(distribuicao => distribuicao.Consumos)
            .Where(distribuicao => distribuicao.EscolaId == escolaId
                && distribuicao.Data >= de
                && distribuicao.Data <= ate)
            .OrderBy(distribuicao => distribuicao.Data)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="RotaTransporte"/>. Tenant-scoped.</summary>
public sealed class RotaTransporteRepository(EducacaoDbContext context) : IRotaTransporteRepository
{
    /// <inheritdoc />
    public void Adicionar(RotaTransporte rota)
    {
        ArgumentNullException.ThrowIfNull(rota);
        context.RotasTransporte.Add(rota);
    }

    /// <inheritdoc />
    public Task<RotaTransporte?> ObterPorIdAsync(RotaTransporteId id, CancellationToken cancellationToken)
        => context.RotasTransporte
            .Include(rota => rota.Alunos)
            .FirstOrDefaultAsync(rota => rota.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RotaTransporte>> ListarPorEscolaAsync(EscolaId? escolaId, CancellationToken cancellationToken)
    {
        var consulta = context.RotasTransporte.Include(rota => rota.Alunos).AsQueryable();

        if (escolaId is { } escola)
        {
            consulta = consulta.Where(rota => rota.EscolaId == escola);
        }

        return await consulta
            .OrderBy(rota => rota.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
