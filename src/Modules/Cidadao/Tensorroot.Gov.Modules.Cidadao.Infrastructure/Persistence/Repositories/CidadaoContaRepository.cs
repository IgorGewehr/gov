using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacao EF Core do repositorio de contas-cidadao. Todas as consultas respeitam o Global Query
/// Filter por tenant do <see cref="CidadaoDbContext"/> — uma conta de outro municipio nunca e enxergada
/// (isolamento por tenant a prova de bala).
/// </summary>
public sealed class CidadaoContaRepository(CidadaoDbContext context) : ICidadaoContaRepository
{
    /// <inheritdoc />
    public void Adicionar(CidadaoConta conta)
    {
        ArgumentNullException.ThrowIfNull(conta);
        context.Contas.Add(conta);
    }

    /// <inheritdoc />
    public Task<CidadaoConta?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken)
        => context.Contas.FirstOrDefaultAsync(conta => conta.Documento == documento, cancellationToken);

    /// <inheritdoc />
    public Task<CidadaoConta?> ObterPorIdAsync(CidadaoContaId contaId, CancellationToken cancellationToken)
        => context.Contas.FirstOrDefaultAsync(conta => conta.Id == contaId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DocumentoJaCadastradoAsync(string documento, CancellationToken cancellationToken)
        => context.Contas.AnyAsync(conta => conta.Documento == documento, cancellationToken);
}
