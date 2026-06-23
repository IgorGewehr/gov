using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Resultado do envio de um lote (nivel 1, sincrono): protocolo do lote ou rejeicao estrutural do
/// proprio lote (ex.: erro 613/612/607). ESOCIAL-SPEC §2.5.
/// </summary>
/// <param name="Aceito">Verdadeiro se o lote foi recebido (protocolo emitido).</param>
/// <param name="ProtocoloLote">Protocolo de envio (quando aceito).</param>
/// <param name="CodigoErro">Codigo do erro estrutural do lote (quando recusado).</param>
/// <param name="DescricaoErro">Descricao do erro (quando recusado).</param>
public sealed record RespostaEnvioLote(bool Aceito, string? ProtocoloLote, string? CodigoErro, string? DescricaoErro);

/// <summary>Retorno do processamento de UM evento do lote (nivel 2, assincrono).</summary>
/// <param name="IdEvento">Atributo Id do evento.</param>
/// <param name="Aceito">Verdadeiro se aceito (recibo emitido).</param>
/// <param name="NumeroRecibo">Recibo (nrRecibo) por evento (quando aceito).</param>
/// <param name="CodigoErro">Codigo do erro (quando rejeitado).</param>
/// <param name="DescricaoErro">Descricao do erro (quando rejeitado).</param>
public sealed record RetornoEventoLote(string IdEvento, bool Aceito, string? NumeroRecibo, string? CodigoErro, string? DescricaoErro);

/// <summary>
/// Resultado da consulta de um lote (<c>ConsultarLoteEventos</c>): se ainda processando, ou os retornos
/// por evento (recibos/erros). ESOCIAL-SPEC §2.5.
/// </summary>
/// <param name="Processado">Verdadeiro quando o lote terminou de ser processado.</param>
/// <param name="Eventos">Retorno por evento (vazio enquanto em processamento).</param>
public sealed record RespostaConsultaLote(bool Processado, IReadOnlyList<RetornoEventoLote> Eventos);

/// <summary>
/// PORTA (ACL) de transmissao ao eSocial (Application). A implementacao real (Infrastructure) fala
/// SOAP 1.2 + mTLS com Polly; a impl. SIMULADA retorna recibos/protocolos fake para o pipeline ponta-a-
/// ponta sem credenciais. ESOCIAL-SPEC §2/§4.1. // TODO(prod: Producao Restrita / SOAP real / creds).
/// </summary>
public interface IESocialGateway
{
    /// <summary>Envia um lote de eventos assinados (<c>EnviarLoteEventos</c>) e devolve o protocolo (nivel 1).</summary>
    /// <param name="lote">Lote (&lt;=50 eventos e &lt;=5 MB).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Protocolo do lote ou rejeicao estrutural.</returns>
    Task<RespostaEnvioLote> EnviarLoteAsync(LoteEventosESocial lote, CancellationToken cancellationToken);

    /// <summary>Consulta o processamento de um lote por protocolo (<c>ConsultarLoteEventos</c>, nivel 2).</summary>
    /// <param name="protocoloLote">Protocolo retornado no envio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Status do lote e retornos por evento.</returns>
    Task<RespostaConsultaLote> ConsultarLoteAsync(string protocoloLote, CancellationToken cancellationToken);
}
