using System.Globalization;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.ESocial;

/// <summary>
/// Implementacao SIMULADA do <see cref="IESocialGateway"/> (ACL): valida os limites do lote localmente
/// (50 eventos / 5 MB), empacota o envelope de envio e devolve protocolo/recibo FAKE deterministicos,
/// permitindo exercitar o pipeline ponta-a-ponta (gerar -> assinar -> empacotar -> transmitir ->
/// consultar) SEM credenciais nem rede. ESOCIAL-SPEC §4.1.
/// <para>
/// // TODO(prod: creds homologacao): a impl. REAL (Producao Restrita -> Producao) fala SOAP 1.2 +
/// mTLS (TLS 1.2, cifras fixas) com Polly (retry + circuit breaker), usando o A1 do Cofre como
/// certificado de conexao; envia <c>EnviarLoteEventos</c> e consulta <c>ConsultarLoteEventos</c> nas
/// URLs por IOptions/Key Vault (ESOCIAL-SPEC §2.1-2.2). Esta classe sera trocada por
/// <c>ESocialGatewaySoap</c> quando as credenciais/cadastro do ente estiverem disponiveis.
/// </para>
/// </summary>
public sealed class ESocialGatewaySimulado(ILogger<ESocialGatewaySimulado> logger) : IESocialGateway
{
    // Memoria do processo: protocolo -> ids de evento do lote, para devolver recibos na consulta.
    private readonly Dictionary<string, IReadOnlyList<string>> _lotesPorProtocolo = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<RespostaEnvioLote> EnviarLoteAsync(LoteEventosESocial lote, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lote);

        // Validacao local dos limites (barra ANTES de "gastar cota") — mesma guarda da impl. real.
        if (lote.Itens.Count > LoteEventosESocial.MaxEventosPorLote)
        {
            return Task.FromResult(new RespostaEnvioLote(false, null, "613", "Lote excede 50 eventos."));
        }

        if (lote.TamanhoBytes > LoteEventosESocial.MaxBytesPorLote)
        {
            return Task.FromResult(new RespostaEnvioLote(false, null, "612", "Mensagem do lote excede 5 MB."));
        }

        // Empacota o envelope (estrutura fiel) — exercita a montagem do XML do lote.
        var envelope = EmpacotadorLoteESocial.Empacotar(lote);
        logger.LogInformation(
            "[eSocial SIMULADO] Lote empacotado: {Eventos} eventos, {Bytes} bytes (envelope {EnvelopeBytes}).",
            lote.Itens.Count,
            lote.TamanhoBytes,
            envelope.Length);

        var protocolo = "SIM-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        _lotesPorProtocolo[protocolo] = lote.Itens.Select(i => i.IdEvento).ToList();
        return Task.FromResult(new RespostaEnvioLote(true, protocolo, null, null));
    }

    /// <inheritdoc />
    public Task<RespostaConsultaLote> ConsultarLoteAsync(string protocoloLote, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocoloLote);

        if (!_lotesPorProtocolo.TryGetValue(protocoloLote, out var idsEventos))
        {
            // Protocolo desconhecido nesta instancia (ex.: reinicio do processo): trata como ainda nao
            // processado para que o worker tente de novo (a impl. real consulta o servico de fato).
            return Task.FromResult(new RespostaConsultaLote(false, []));
        }

        // SIMULADO: todos os eventos aceitos com recibo fake deterministico por id.
        var retornos = idsEventos
            .Select(id => new RetornoEventoLote(
                id,
                Aceito: true,
                NumeroRecibo: "REC-" + Math.Abs(id.GetHashCode(StringComparison.Ordinal)).ToString(CultureInfo.InvariantCulture),
                CodigoErro: null,
                DescricaoErro: null))
            .ToList();

        return Task.FromResult(new RespostaConsultaLote(true, retornos));
    }
}
