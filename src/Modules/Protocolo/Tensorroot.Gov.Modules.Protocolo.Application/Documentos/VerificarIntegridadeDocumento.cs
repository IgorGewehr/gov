using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

/// <summary>
/// Verifica a integridade de um documento comparando o hash armazenado com o digest SHA-256
/// recalculado do conteudo (I-12). Tenant-scoped; nao vaza existencia cross-tenant.
/// </summary>
/// <param name="DocumentoId">Documento a verificar.</param>
/// <param name="HashRecalculado">Digest SHA-256 recalculado do conteudo.</param>
public sealed record VerificarIntegridadeDocumentoQuery(Guid DocumentoId, string HashRecalculado) : IQuery<bool>;

/// <summary>Handler da verificacao de integridade do documento.</summary>
public sealed class VerificarIntegridadeDocumentoHandler(IDocumentoRepository documentos)
    : IQueryHandler<VerificarIntegridadeDocumentoQuery, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(VerificarIntegridadeDocumentoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documento = await documentos
            .ObterPorIdAsync(new DocumentoId(request.DocumentoId), cancellationToken)
            .ConfigureAwait(false);

        // B-13: documento inexistente (ou de outro tenant) => integridade indeterminada (false).
        return documento is not null && documento.VerificarIntegridade(request.HashRecalculado);
    }
}
