using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Esic;

/// <summary>
/// Status PUBLICO de um pedido e-SIC pelo protocolo. LGPD/LAI: revela situacao/datas/resposta/recurso,
/// mas <b>NUNCA os dados pessoais do solicitante</b> (a LAI protege o requerente — nao expor nome/doc/
/// contato a terceiros). Retorna <c>null</c> quando o protocolo nao existe no ente.
/// </summary>
/// <param name="Protocolo">Protocolo (AAAA/NNNNNN).</param>
public sealed record ConsultarStatusPedidoSicQuery(string Protocolo) : IQuery<StatusPedidoSicPublicoDto?>;

/// <summary>Status publico do pedido — SEM PII do solicitante.</summary>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="PrazoResposta">Prazo legal base.</param>
/// <param name="ProrrogadoAte">Prazo apos prorrogacao (se houver).</param>
/// <param name="DataResposta">Data da resposta (se houver).</param>
/// <param name="TextoResposta">Texto da resposta (se houver).</param>
/// <param name="FundamentoIndeferimento">Fundamento do indeferimento (se houver).</param>
/// <param name="RecursoSituacao">Resumo do recurso (se houver).</param>
public sealed record StatusPedidoSicPublicoDto(
    string Protocolo,
    SituacaoPedidoSic Situacao,
    DateOnly DataAbertura,
    DateOnly PrazoResposta,
    DateOnly? ProrrogadoAte,
    DateOnly? DataResposta,
    string? TextoResposta,
    string? FundamentoIndeferimento,
    string? RecursoSituacao);

/// <summary>Handler da consulta publica de status (sem PII).</summary>
public sealed class ConsultarStatusPedidoSicHandler(IPedidoSicRepository pedidos)
    : IQueryHandler<ConsultarStatusPedidoSicQuery, StatusPedidoSicPublicoDto?>
{
    /// <inheritdoc />
    public async Task<StatusPedidoSicPublicoDto?> Handle(ConsultarStatusPedidoSicQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pedido = await pedidos.ObterPorProtocoloAsync(request.Protocolo, cancellationToken).ConfigureAwait(false);
        if (pedido is null)
        {
            return null;
        }

        string? recursoSituacao = pedido.Recurso is null
            ? null
            : pedido.Recurso.Decidido
                ? $"{pedido.Recurso.Instancia} instancia: {pedido.Recurso.Resultado}"
                : $"{pedido.Recurso.Instancia} instancia: em analise";

        // SO campos publicos: nada do Solicitante (nome/doc/contato) atravessa este DTO.
        return new StatusPedidoSicPublicoDto(
            pedido.Protocolo.Valor,
            pedido.Situacao,
            pedido.DataAbertura,
            pedido.PrazoResposta,
            pedido.ProrrogadoAte,
            pedido.Resposta?.Data,
            pedido.Resposta?.Texto,
            pedido.FundamentoIndeferimento,
            recursoSituacao);
    }
}
