using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Lexml;

/// <summary>Representacao LexML-BR de uma norma (URN + XML), pronta para dados abertos/prestacao.</summary>
/// <param name="NormaId">Identificador da norma.</param>
/// <param name="Urn">URN LexML canonica (<c>urn:lex:...</c>).</param>
/// <param name="Xml">Documento XML LexML serializado (UTF-8).</param>
/// <param name="NomeArquivo">Nome de arquivo sugerido para o download (derivado da URN).</param>
public sealed record NormaLexmlDto(Guid NormaId, string Urn, string Xml, string NomeArquivo);

/// <summary>
/// Gera a representacao LexML-BR (URN + XML) de uma norma do tenant — export LOCAL (W9.5; sem
/// credencial). A transmissao oficial a base LexML/dados abertos do TCE/governo e // TODO(M10).
/// </summary>
/// <param name="NormaId">Identificador da norma a exportar.</param>
public sealed record ExportarNormaLexmlQuery(Guid NormaId) : IQuery<NormaLexmlDto?>;

/// <summary>Handler do export LexML de uma norma.</summary>
public sealed class ExportarNormaLexmlHandler(
    INormaRepository normas,
    ILegislativoParametros parametros) : IQueryHandler<ExportarNormaLexmlQuery, NormaLexmlDto?>
{
    /// <inheritdoc />
    public async Task<NormaLexmlDto?> Handle(ExportarNormaLexmlQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var norma = await normas.ObterPorIdAsync(new NormaId(request.NormaId), cancellationToken).ConfigureAwait(false);
        if (norma is null)
        {
            return null;
        }

        // Ente/autoridade do tenant (jurisdicao LexML configurada por Camara) — nunca hardcoded.
        var ente = parametros.IdentificacaoEnte();

        var urn = UrnLexml.Compor(ente, norma.Tipo, norma.DataPromulgacao, norma.Numero);
        var xml = DocumentoLexml.Serializar(norma, ente);

        return new NormaLexmlDto(norma.Id.Value, urn.Valor, xml, NomeArquivo(norma));
    }

    // Nome de arquivo estavel e legivel: tipo-numero-ano.xml (ex.: lei-1234-2025.xml).
    private static string NomeArquivo(Norma norma)
        => $"{UrnLexml.MapearTipo(norma.Tipo).Replace('.', '-')}-{norma.Numero}-{norma.Ano}.xml";
}
