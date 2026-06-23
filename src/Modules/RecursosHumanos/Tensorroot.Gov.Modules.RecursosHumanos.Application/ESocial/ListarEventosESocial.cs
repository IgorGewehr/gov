using System.Text;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Projecao de leitura de um <c>EventoESocial</c> para inspecao/auditoria via HTTP: estado da maquina,
/// XML gerado (fiel ao leiaute S-1.3), protocolo do lote e recibo (<c>nrRecibo</c>) por evento.
/// </summary>
/// <param name="Id">Identificador do evento.</param>
/// <param name="Tipo">Tipo (S-1000, S-1200, ...).</param>
/// <param name="IdEvento">Atributo Id de negocio no XML.</param>
/// <param name="Estado">Estado atual na maquina de estados.</param>
/// <param name="Ambiente">Ambiente de transmissao (Producao/ProducaoRestrita).</param>
/// <param name="HashXmlGerado">Hash SHA-256 (hex) do XML gerado.</param>
/// <param name="ProtocoloLote">Protocolo do lote (nivel 1 sincrono); nulo antes de transmitir.</param>
/// <param name="NumeroRecibo">Recibo por evento; nulo antes do aceite.</param>
/// <param name="Assinado">Indica se ja ha XML assinado (XML-DSig A1 via Cofre).</param>
/// <param name="Xml">XML do evento gerado (UTF-8 como texto), para inspecao do leiaute.</param>
public sealed record EventoESocialDto(
    Guid Id,
    string Tipo,
    string IdEvento,
    string Estado,
    string Ambiente,
    string HashXmlGerado,
    string? ProtocoloLote,
    string? NumeroRecibo,
    bool Assinado,
    string Xml);

/// <summary>Lista os eventos eSocial do tenant atual (inspecao/auditoria).</summary>
public sealed record ListarEventosESocialQuery : IQuery<IReadOnlyList<EventoESocialDto>>;

/// <summary>Handler da listagem de eventos eSocial.</summary>
public sealed class ListarEventosESocialHandler(IEventoESocialRepository eventos)
    : IQueryHandler<ListarEventosESocialQuery, IReadOnlyList<EventoESocialDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventoESocialDto>> Handle(ListarEventosESocialQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var todos = await eventos.ListarTodosAsync(cancellationToken).ConfigureAwait(false);

        return todos
            .Select(e => new EventoESocialDto(
                e.Id.Value,
                e.Tipo.ToString(),
                e.IdEvento,
                e.Estado.ToString(),
                e.Ambiente.ToString(),
                e.HashXmlGerado,
                e.ProtocoloLote,
                e.NumeroRecibo,
                e.XmlAssinado is { Length: > 0 },
                Encoding.UTF8.GetString(e.Xml)))
            .ToList();
    }
}
