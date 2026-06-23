using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de tabelas de taxa (taxas/TLL).</summary>
public sealed class TabelaTaxaRepository(TributosDbContext context) : ITabelaTaxaRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaTaxa tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasTaxa.Add(tabela);
    }

    /// <inheritdoc />
    public Task<TabelaTaxa?> ObterPorIdAsync(TabelaTaxaId id, CancellationToken cancellationToken)
        => context.TabelasTaxa
            .Include(tabela => tabela.Faixas)
            .FirstOrDefaultAsync(tabela => tabela.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<TabelaTaxa?> ObterVigentePorCodigoAsync(string codigo, int exercicio, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        return context.TabelasTaxa
            .Include(tabela => tabela.Faixas)
            .FirstOrDefaultAsync(
                tabela => tabela.Codigo == codigo && tabela.Exercicio == exercicio && tabela.Vigente,
                cancellationToken);
    }
}

/// <summary>Implementação EF Core do repositório de alvarás.</summary>
public sealed class AlvaraRepository(TributosDbContext context) : IAlvaraRepository
{
    /// <inheritdoc />
    public void Adicionar(Alvara alvara)
    {
        ArgumentNullException.ThrowIfNull(alvara);
        context.Alvaras.Add(alvara);
    }

    /// <inheritdoc />
    public Task<Alvara?> ObterPorIdAsync(AlvaraId id, CancellationToken cancellationToken)
        => context.Alvaras.FirstOrDefaultAsync(alvara => alvara.Id == id, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de tabelas de COSIP.</summary>
public sealed class TabelaCosipRepository(TributosDbContext context) : ITabelaCosipRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaCosip tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasCosip.Add(tabela);
    }

    /// <inheritdoc />
    public Task<TabelaCosip?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken)
        => context.TabelasCosip
            .Include(tabela => tabela.Faixas)
            .FirstOrDefaultAsync(tabela => tabela.Exercicio == exercicio && tabela.Vigente, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de obras de Contribuição de Melhoria.</summary>
public sealed class ObraContribuicaoMelhoriaRepository(TributosDbContext context) : IObraContribuicaoMelhoriaRepository
{
    /// <inheritdoc />
    public void Adicionar(ObraContribuicaoMelhoria obra)
    {
        ArgumentNullException.ThrowIfNull(obra);
        context.ObrasContribuicaoMelhoria.Add(obra);
    }

    /// <inheritdoc />
    public Task<ObraContribuicaoMelhoria?> ObterPorIdAsync(ObraContribuicaoMelhoriaId id, CancellationToken cancellationToken)
        => context.ObrasContribuicaoMelhoria
            .Include(obra => obra.Imoveis)
            .FirstOrDefaultAsync(obra => obra.Id == id, cancellationToken);
}
