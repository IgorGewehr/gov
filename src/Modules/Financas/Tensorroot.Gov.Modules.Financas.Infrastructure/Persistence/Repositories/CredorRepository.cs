using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Credores;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de credores cadastrados.</summary>
public sealed class CredorRepository(FinancasDbContext context) : ICredorRepository
{
    /// <inheritdoc />
    public void Adicionar(CredorCadastrado credor)
    {
        ArgumentNullException.ThrowIfNull(credor);
        context.Credores.Add(credor);
    }

    /// <inheritdoc />
    public Task<CredorCadastrado?> ObterPorIdAsync(CredorId id, CancellationToken cancellationToken)
        => context.Credores.FirstOrDefaultAsync(credor => credor.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteComDocumentoAsync(string documento, CancellationToken cancellationToken)
        => context.Credores.AnyAsync(credor => credor.Documento == documento, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CredorCadastrado>> ListarAsync(string? termo, CancellationToken cancellationToken)
    {
        var consulta = context.Credores.AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            consulta = consulta.Where(credor => EF.Functions.Like(credor.Nome, $"%{t}%") || credor.Documento.Contains(t));
        }

        return await consulta
            .OrderBy(credor => credor.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
