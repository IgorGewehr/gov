using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Esic;

/// <summary>Item da lista INTERNA de pedidos e-SIC (resumo; sem texto integral de PII).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="Solicitante">Nome do solicitante (visivel so na superficie interna autenticada).</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="PrazoVigente">Prazo vigente (prorrogado, se houver).</param>
/// <param name="EmAtraso">Indica se o prazo ja venceu sem desfecho.</param>
public sealed record PedidoSicResumoDto(
    Guid PedidoId,
    string Protocolo,
    string Solicitante,
    SituacaoPedidoSic Situacao,
    DateOnly DataAbertura,
    DateOnly PrazoVigente,
    bool EmAtraso);

/// <summary>INTERNO: lista/filtra pedidos do tenant (superficie autenticada).</summary>
/// <param name="Ano">Ano (opcional).</param>
/// <param name="Situacao">Situacao (opcional).</param>
public sealed record ListarPedidosSicQuery(int? Ano, SituacaoPedidoSic? Situacao) : IQuery<IReadOnlyList<PedidoSicResumoDto>>;

/// <summary>Handler da lista interna.</summary>
public sealed class ListarPedidosSicHandler(IPedidoSicRepository pedidos, TimeProvider relogio)
    : IQueryHandler<ListarPedidosSicQuery, IReadOnlyList<PedidoSicResumoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PedidoSicResumoDto>> Handle(ListarPedidosSicQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO(fuso): trocar por IDataHojeTenant.Hoje() (prazo/data de dominio no fuso do tenant; ver W9 fix de fuso).
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        var lista = await pedidos.ListarAsync(request.Ano, request.Situacao, cancellationToken).ConfigureAwait(false);

        return lista.Select(pedido => new PedidoSicResumoDto(
            pedido.Id.Value,
            pedido.Protocolo.Valor,
            pedido.Solicitante.Nome,
            pedido.Situacao,
            pedido.DataAbertura,
            pedido.PrazoVigente,
            EmAtraso: (pedido.Situacao is SituacaoPedidoSic.Aberto or SituacaoPedidoSic.EmAtendimento) && hoje > pedido.PrazoVigente))
            .ToList();
    }
}

/// <summary>Detalhe INTERNO do pedido, COM os dados do solicitante (PII).</summary>
/// <param name="PedidoId">Identificador.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="SolicitanteNome">Nome do solicitante.</param>
/// <param name="SolicitanteDocumento">Documento do solicitante (PII).</param>
/// <param name="SolicitanteContato">Contato do solicitante (PII).</param>
/// <param name="Descricao">Descricao do pedido.</param>
/// <param name="Situacao">Situacao.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="PrazoResposta">Prazo base.</param>
/// <param name="ProrrogadoAte">Prazo prorrogado (se houver).</param>
/// <param name="MotivoProrrogacao">Motivo da prorrogacao (se houver).</param>
/// <param name="TextoResposta">Texto da resposta (se houver).</param>
/// <param name="FundamentoIndeferimento">Fundamento do indeferimento (se houver).</param>
public sealed record PedidoSicDetalheInternoDto(
    Guid PedidoId,
    string Protocolo,
    string SolicitanteNome,
    string? SolicitanteDocumento,
    string? SolicitanteContato,
    string Descricao,
    SituacaoPedidoSic Situacao,
    DateOnly DataAbertura,
    DateOnly PrazoResposta,
    DateOnly? ProrrogadoAte,
    string? MotivoProrrogacao,
    string? TextoResposta,
    string? FundamentoIndeferimento);

/// <summary>
/// INTERNO: detalhe do pedido COM PII do solicitante. Implementa <see cref="ISensivelLgpd"/> → o
/// pipeline gera trilha de ACESSO (LG-2/LG-3: quem leu o pedido com PII, quando, sob qual base legal).
/// Base legal: cumprimento de obrigacao legal (atender a LAI).
/// </summary>
/// <param name="PedidoId">Identificador do pedido.</param>
public sealed record ObterPedidoSicInternoQuery(Guid PedidoId)
    : IQuery<PedidoSicDetalheInternoDto?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "PedidoInformacaoSic";

    /// <inheritdoc />
    public string? EntidadeId => PedidoId.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ObrigacaoLegal;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis { get; } =
        new HashSet<BaseLegalLgpd> { BaseLegalLgpd.ObrigacaoLegal };
}

/// <summary>Handler do detalhe interno (com PII).</summary>
public sealed class ObterPedidoSicInternoHandler(IPedidoSicRepository pedidos)
    : IQueryHandler<ObterPedidoSicInternoQuery, PedidoSicDetalheInternoDto?>
{
    /// <inheritdoc />
    public async Task<PedidoSicDetalheInternoDto?> Handle(ObterPedidoSicInternoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await pedidos.ObterPorIdAsync(new PedidoInformacaoSicId(request.PedidoId), cancellationToken).ConfigureAwait(false);
        if (pedido is null)
        {
            return null;
        }

        return new PedidoSicDetalheInternoDto(
            pedido.Id.Value,
            pedido.Protocolo.Valor,
            pedido.Solicitante.Nome,
            pedido.Solicitante.Documento,
            pedido.Solicitante.Contato,
            pedido.Descricao,
            pedido.Situacao,
            pedido.DataAbertura,
            pedido.PrazoResposta,
            pedido.ProrrogadoAte,
            pedido.MotivoProrrogacao,
            pedido.Resposta?.Texto,
            pedido.FundamentoIndeferimento);
    }
}
