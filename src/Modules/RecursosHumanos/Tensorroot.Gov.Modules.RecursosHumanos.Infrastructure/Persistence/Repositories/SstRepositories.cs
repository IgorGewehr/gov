using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ExameOcupacional"/> (ASO).</summary>
public sealed class ExameOcupacionalRepository(RecursosHumanosDbContext context) : IExameOcupacionalRepository
{
    /// <inheritdoc />
    public void Adicionar(ExameOcupacional exame)
    {
        ArgumentNullException.ThrowIfNull(exame);
        context.SstExamesOcupacionais.Add(exame);
    }

    /// <inheritdoc />
    public Task<ExameOcupacional?> ObterPorIdAsync(ExameOcupacionalId id, CancellationToken cancellationToken)
        => context.SstExamesOcupacionais.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExameOcupacional>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
    {
        // DataExame nao e ordenavel em SQLite (DateOnly) de forma confiavel no SQL; materializa e ordena
        // em memoria (lote pequeno por servidor). Em SqlServer o filtro usa o indice (TenantId, ServidorId).
        var itens = await context.SstExamesOcupacionais
            .Where(e => e.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. itens.OrderByDescending(e => e.DataExame)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExameOcupacional>> ListarComProximoExameAteAsync(DateOnly ateData, CancellationToken cancellationToken)
    {
        var itens = await context.SstExamesOcupacionais
            .Where(e => e.Situacao == SituacaoRegistroSst.Registrado && e.DataProximoExame != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. itens
            .Where(e => e.DataProximoExame <= ateData)
            .OrderBy(e => e.DataProximoExame)];
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ExposicaoAgenteNocivo"/> (S-2240/PPP).</summary>
public sealed class ExposicaoAgenteNocivoRepository(RecursosHumanosDbContext context) : IExposicaoAgenteNocivoRepository
{
    /// <inheritdoc />
    public void Adicionar(ExposicaoAgenteNocivo exposicao)
    {
        ArgumentNullException.ThrowIfNull(exposicao);
        context.SstExposicoesAgenteNocivo.Add(exposicao);
    }

    /// <inheritdoc />
    public Task<ExposicaoAgenteNocivo?> ObterPorIdAsync(ExposicaoAgenteNocivoId id, CancellationToken cancellationToken)
        => context.SstExposicoesAgenteNocivo.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExposicaoAgenteNocivo>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
    {
        var itens = await context.SstExposicoesAgenteNocivo
            .Where(e => e.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. itens.OrderByDescending(e => e.InicioExposicao)];
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ComunicacaoAcidente"/> (CAT — S-2210).</summary>
public sealed class ComunicacaoAcidenteRepository(RecursosHumanosDbContext context) : IComunicacaoAcidenteRepository
{
    /// <inheritdoc />
    public void Adicionar(ComunicacaoAcidente cat)
    {
        ArgumentNullException.ThrowIfNull(cat);
        context.SstComunicacoesAcidente.Add(cat);
    }

    /// <inheritdoc />
    public Task<ComunicacaoAcidente?> ObterPorIdAsync(ComunicacaoAcidenteId id, CancellationToken cancellationToken)
        => context.SstComunicacoesAcidente.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComunicacaoAcidente>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
    {
        var itens = await context.SstComunicacoesAcidente
            .Where(e => e.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. itens.OrderByDescending(e => e.DataHoraAcidente)];
    }
}
