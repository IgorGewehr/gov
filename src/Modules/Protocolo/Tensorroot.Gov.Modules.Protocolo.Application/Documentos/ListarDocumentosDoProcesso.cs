using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

/// <summary>Resumo (projecao minimizada — LGPD) de um documento juntado a um processo.</summary>
/// <param name="Id">Identificador do documento.</param>
/// <param name="Hash">Resumo SHA-256 do conteudo.</param>
/// <param name="Criticidade">Criticidade do ato (texto).</param>
/// <param name="Situacao">Situacao atual (texto).</param>
/// <param name="TipoAssinatura">Nivel da assinatura aplicada (texto), se assinado.</param>
/// <param name="DataJuntada">Data da juntada ao processo, se juntado.</param>
public sealed record DocumentoResumo(
    Guid Id,
    string Hash,
    string Criticidade,
    string Situacao,
    string? TipoAssinatura,
    DateOnly? DataJuntada);

/// <summary>Lista os documentos juntados a um processo (tenant-scoped; respeita o nivel de acesso).</summary>
/// <param name="ProcessoId">Processo de origem.</param>
public sealed record ListarDocumentosDoProcessoQuery(Guid ProcessoId) : IQuery<IReadOnlyList<DocumentoResumo>>;

/// <summary>Handler da listagem de documentos do processo.</summary>
public sealed class ListarDocumentosDoProcessoHandler(IDocumentoRepository documentos)
    : IQueryHandler<ListarDocumentosDoProcessoQuery, IReadOnlyList<DocumentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentoResumo>> Handle(
        ListarDocumentosDoProcessoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documentosDoProcesso = await documentos
            .ListarPorProcessoAsync(request.ProcessoId, cancellationToken)
            .ConfigureAwait(false);

        return documentosDoProcesso
            .Select(documento => new DocumentoResumo(
                documento.Id.Value,
                documento.Hash.Valor,
                documento.Criticidade.ToString(),
                documento.Situacao.ToString(),
                documento.TipoAssinatura?.ToString(),
                documento.DataJuntada))
            .ToList();
    }
}
