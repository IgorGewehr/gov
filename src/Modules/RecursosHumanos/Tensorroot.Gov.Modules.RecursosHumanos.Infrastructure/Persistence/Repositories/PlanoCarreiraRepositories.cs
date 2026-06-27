using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="PlanoCarreira"/>.</summary>
public sealed class PlanoCarreiraRepository(RecursosHumanosDbContext context) : IPlanoCarreiraRepository
{
    /// <inheritdoc />
    public void Adicionar(PlanoCarreira plano)
    {
        ArgumentNullException.ThrowIfNull(plano);
        context.PlanosCarreira.Add(plano);
    }

    /// <inheritdoc />
    public Task<PlanoCarreira?> ObterPorIdAsync(PlanoCarreiraId id, CancellationToken cancellationToken)
        => context.PlanosCarreira.FirstOrDefaultAsync(plano => plano.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanoCarreira>> ListarAsync(CancellationToken cancellationToken)
        => await context.PlanosCarreira
            .OrderBy(plano => plano.DenominacaoCarreira)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="EnquadramentoServidor"/>.</summary>
public sealed class EnquadramentoServidorRepository(RecursosHumanosDbContext context) : IEnquadramentoServidorRepository
{
    /// <inheritdoc />
    public void Adicionar(EnquadramentoServidor enquadramento)
    {
        ArgumentNullException.ThrowIfNull(enquadramento);
        context.EnquadramentosCarreira.Add(enquadramento);
    }

    /// <inheritdoc />
    public Task<EnquadramentoServidor?> ObterPorIdAsync(EnquadramentoServidorId id, CancellationToken cancellationToken)
        => context.EnquadramentosCarreira.FirstOrDefaultAsync(enquadramento => enquadramento.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<EnquadramentoServidor?> ObterPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
        => context.EnquadramentosCarreira.FirstOrDefaultAsync(enquadramento => enquadramento.ServidorId == servidorId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteParaServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
        => context.EnquadramentosCarreira.AnyAsync(enquadramento => enquadramento.ServidorId == servidorId, cancellationToken);
}
