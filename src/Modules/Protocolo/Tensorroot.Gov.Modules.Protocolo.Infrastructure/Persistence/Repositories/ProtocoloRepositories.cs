using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do agregado <see cref="Processo"/>.</summary>
public sealed class ProcessoRepository(ProtocoloDbContext context) : IProcessoRepository
{
    /// <inheritdoc />
    public Task<bool> ExisteAsync(Guid processoId, CancellationToken cancellationToken)
    {
        var id = new ProcessoId(processoId);
        return context.Processos.AnyAsync(processo => processo.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void Adicionar(Processo processo)
    {
        ArgumentNullException.ThrowIfNull(processo);
        context.Processos.Add(processo);
    }

    /// <inheritdoc />
    public Task<Processo?> ObterPorIdAsync(ProcessoId id, CancellationToken cancellationToken)
        => context.Processos.FirstOrDefaultAsync(processo => processo.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Processo?> ObterPorNupAsync(Nup nup, CancellationToken cancellationToken)
        => context.Processos.FirstOrDefaultAsync(processo => processo.Nup == nup, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Processo>> ListarPorSetorAtualAsync(Guid setorId, CancellationToken cancellationToken)
        => await context.Processos
            .Where(processo => processo.SetorAtualId == setorId)
            .OrderBy(processo => processo.DataAutuacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório do agregado <see cref="Documento"/>.</summary>
public sealed class DocumentoRepository(ProtocoloDbContext context) : IDocumentoRepository
{
    /// <inheritdoc />
    public void Adicionar(Documento documento)
    {
        ArgumentNullException.ThrowIfNull(documento);
        context.Documentos.Add(documento);
    }

    /// <inheritdoc />
    public Task<Documento?> ObterPorIdAsync(DocumentoId id, CancellationToken cancellationToken)
        => context.Documentos.FirstOrDefaultAsync(documento => documento.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Documento>> ListarPorProcessoAsync(Guid processoId, CancellationToken cancellationToken)
        => await context.Documentos
            .Where(documento => documento.ProcessoId == processoId)
            .OrderBy(documento => documento.DataJuntada)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
