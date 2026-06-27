using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="BancoDeHoras"/>.</summary>
public sealed class BancoDeHorasRepository(RecursosHumanosDbContext context) : IBancoDeHorasRepository
{
    /// <inheritdoc />
    public void Adicionar(BancoDeHoras banco)
    {
        ArgumentNullException.ThrowIfNull(banco);
        context.BancosDeHoras.Add(banco);
    }

    /// <inheritdoc />
    public Task<BancoDeHoras?> ObterPorServidorAsync(Guid servidorId, CancellationToken cancellationToken)
        => context.BancosDeHoras.FirstOrDefaultAsync(b => b.ServidorId == servidorId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BancoDeHoras>> ListarTodosAsync(CancellationToken cancellationToken)
        => await context.BancosDeHoras.ToListAsync(cancellationToken).ConfigureAwait(false);
}
