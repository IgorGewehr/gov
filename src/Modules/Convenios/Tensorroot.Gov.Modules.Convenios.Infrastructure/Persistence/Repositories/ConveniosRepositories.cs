using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio EF Core do agregado <see cref="ConvenioRecebido"/> (fluxo A). Tenant-scoped pelo Global Query Filter.</summary>
public sealed class ConvenioRecebidoRepository(ConveniosDbContext context) : IConvenioRecebidoRepository
{
    /// <inheritdoc />
    public void Adicionar(ConvenioRecebido convenio)
    {
        ArgumentNullException.ThrowIfNull(convenio);
        context.ConveniosRecebidos.Add(convenio);
    }

    /// <inheritdoc />
    public Task<ConvenioRecebido?> ObterPorIdAsync(ConvenioRecebidoId id, CancellationToken cancellationToken)
        => context.ConveniosRecebidos.FirstOrDefaultAsync(convenio => convenio.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConvenioRecebido>> ListarAsync(
        SituacaoConvenioRecebido? situacao, CancellationToken cancellationToken)
    {
        var consulta = context.ConveniosRecebidos.AsQueryable();
        if (situacao is { } valor)
        {
            consulta = consulta.Where(convenio => convenio.Situacao == valor);
        }

        return await consulta
            .OrderByDescending(convenio => convenio.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Repositorio EF Core do agregado <see cref="ParceriaOsc"/> (fluxo B). Tenant-scoped pelo Global Query Filter.</summary>
public sealed class ParceriaOscRepository(ConveniosDbContext context) : IParceriaOscRepository
{
    /// <inheritdoc />
    public void Adicionar(ParceriaOsc parceria)
    {
        ArgumentNullException.ThrowIfNull(parceria);
        context.ParceriasOsc.Add(parceria);
    }

    /// <inheritdoc />
    public Task<ParceriaOsc?> ObterPorIdAsync(ParceriaOscId id, CancellationToken cancellationToken)
        => context.ParceriasOsc.FirstOrDefaultAsync(parceria => parceria.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParceriaOsc>> ListarAsync(SituacaoParceriaOsc? situacao, CancellationToken cancellationToken)
    {
        var consulta = context.ParceriasOsc.AsQueryable();
        if (situacao is { } valor)
        {
            consulta = consulta.Where(parceria => parceria.Situacao == valor);
        }

        return await consulta
            .OrderByDescending(parceria => parceria.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
