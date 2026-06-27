using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="RemessaSicapPessoal"/>.</summary>
public sealed class RemessaSicapPessoalRepository(RecursosHumanosDbContext context) : IRemessaSicapPessoalRepository
{
    /// <inheritdoc />
    public void Adicionar(RemessaSicapPessoal remessa)
    {
        ArgumentNullException.ThrowIfNull(remessa);
        context.RemessasSicapPessoal.Add(remessa);
    }

    /// <inheritdoc />
    public Task<RemessaSicapPessoal?> ObterPorIdAsync(RemessaSicapPessoalId id, CancellationToken cancellationToken)
        => context.RemessasSicapPessoal.FirstOrDefaultAsync(remessa => remessa.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<int> ProximoSequencialAsync(int codigoOrgao, CancellationToken cancellationToken)
    {
        // Maior sequencial do orgao no tenant (Global Query Filter aplica o TenantId) + 1.
        var maximo = await context.RemessasSicapPessoal
            .Where(remessa => remessa.CodigoOrgao == codigoOrgao)
            .Select(remessa => (int?)remessa.SequencialLote)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return (maximo ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RemessaSicapPessoal>> ListarAsync(SituacaoRemessaSicap? situacao, CancellationToken cancellationToken)
    {
        var consulta = context.RemessasSicapPessoal.AsQueryable();
        if (situacao is { } filtro)
        {
            consulta = consulta.Where(remessa => remessa.Situacao == filtro);
        }

        return await consulta
            .OrderByDescending(remessa => remessa.CodigoOrgao)
            .ThenByDescending(remessa => remessa.SequencialLote)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
