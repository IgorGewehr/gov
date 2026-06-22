using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Resultado da transmissao de uma declaracao ao SICONFI.</summary>
/// <param name="Protocolo">Protocolo retornado pelo SICONFI.</param>
/// <param name="DataTransmissao">Data efetiva da transmissao.</param>
public sealed record ResultadoTransmissaoSiconfi(string Protocolo, DateOnly DataTransmissao);

/// <summary>
/// Anti-Corruption Layer da entrega da declaracao ao SICONFI (STN).
/// </summary>
/// <remarks>
/// IMPORTANTE: o SICONFI NAO tem API de upload/envio — a carga da MSC e a homologacao (e-CPF A3) sao ATO
/// HUMANO no portal. Portanto esta porta NAO faz POST de envio: ela MODELA o registro do protocolo/recibo
/// retornado pelo portal (o operador, apos a carga manual, informa o protocolo). A geracao do artefato MSC
/// e feita por <c>IGeradorMsc</c> e a reconciliacao por <c>IConsultaSiconfi</c> (somente consulta).
/// Idempotente por <see cref="DeclaracaoFiscalId"/>.
/// </remarks>
public interface ISiconfiGateway
{
    /// <summary>
    /// Registra o protocolo/recibo da entrega da declaracao ao SICONFI (ato humano; NAO e POST de envio).
    /// </summary>
    /// <param name="declaracao">Declaracao consolidada entregue.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com protocolo e data de transmissao registrados.</returns>
    Task<ResultadoTransmissaoSiconfi> TransmitirAsync(DeclaracaoFiscal declaracao, CancellationToken cancellationToken);
}
