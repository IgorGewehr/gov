using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Carimbo;

/// <summary>
/// Adapter de PRODUCAO da ACT credenciada ICP-Brasil (RFC 3161 / DOC-ICP-12 v2.1) — Anti-Corruption
/// Layer do carimbo de tempo. Monta o pedido de carimbo (MessageImprint = hash + nonce + certReq),
/// faz POST ao endpoint da ACT, le a resposta, VALIDA (status/PKIFailureInfo, imprint, nonce, cadeia/
/// EKU, genTime) e mapeia para <see cref="CarimboDeTempo.DeToken"/>. A resiliencia (retry idempotente,
/// circuit breaker, timeout — Polly) e configurada no <c>HttpClient</c> nomeado registrado no
/// <c>ProtocoloModule</c> (mesmo padrao do <c>AdnNfseGateway</c>).
/// <para>
/// W9.4 (M9): implementacao STUB/SIMULADA — a troca acontece via HTTP (testavel com WireMock servindo
/// <c>application/timestamp-reply</c> pre-gravados), e TODA a logica de VALIDACAO (status, imprint,
/// nonce, genTime, cadeia confiavel) ja roda aqui. A serializacao ASN.1/DER do TSQ e o parsing do CMS
/// SignedData via BouncyCastle (token DER real), bem como a contratacao da ACT credenciada, sao
/// // TODO(M10): integracao real (custo por carimbo = decisao comercial).
/// </para>
/// </summary>
public sealed class CarimbadorDeTempoAct(HttpClient httpClient, IOptions<OpcoesAct> opcoes) : ICarimbadorDeTempo
{
    private readonly OpcoesAct _opcoes = opcoes.Value;

    /// <inheritdoc />
    public async Task<CarimboDeTempo> CarimbarAsync(Hash hashDocumento, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hashDocumento);

        // Nonce aleatorio (anti-replay): ecoado pela ACT e conferido na resposta.
        var nonce = GerarNonce();

        // MessageImprint = { SHA-256, hash }. A ACT recebe SO o hash — nunca o conteudo (privacidade).
        // TODO(M10): montar o TimeStampReq ASN.1/DER (BouncyCastle TimeStampRequestGenerator),
        //            Content-Type application/timestamp-query, certReq=true, e ler o reply DER.
        var pedido = new TsqStub(_opcoes.PoliticaEsperada, AlgoritmoSha256, hashDocumento.Valor, nonce);

        TstReplyStub? resposta;
        try
        {
            using var http = await httpClient
                .PostAsJsonAsync("tsp", pedido, cancellationToken)
                .ConfigureAwait(false);

            // Erro de transporte/servidor: a resiliencia (Polly) ja aplicou retry/circuit breaker no
            // HttpClient; aqui a falha residual vira erro de dominio explicito (nada persiste).
            if (!http.IsSuccessStatusCode)
            {
                throw new CarimboDeTempoException(
                    FalhaCarimboDeTempo.SystemFailure,
                    $"HTTP {(int)http.StatusCode} da ACT.");
            }

            resposta = await http.Content
                .ReadFromJsonAsync<TstReplyStub>(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.SystemFailure, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout do HttpClient (Polly) — falha controlada, sem corromper estado.
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.TimeNotAvailable, ex.Message);
        }

        if (resposta is null)
        {
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.BadDataFormat, "Resposta vazia da ACT.");
        }

        return ValidarEMapear(resposta, hashDocumento, nonce);
    }

    /// <summary>
    /// Valida o TST conforme RFC 3161 (status/imprint/nonce/cadeia/EKU/genTime) e mapeia para o VO.
    /// Falha em qualquer item => <see cref="CarimboDeTempoException"/> (nada persiste).
    /// TODO(M10): substituir as conferencias por inspecao do TSTInfo/CMS DER (BouncyCastle), incluindo
    ///            verificacao da assinatura CMS e encadeamento ate a AC do tempo credenciada.
    /// </summary>
    private CarimboDeTempo ValidarEMapear(TstReplyStub resposta, Hash hashDocumento, string nonce)
    {
        // (a) PKIStatus = granted/grantedWithMods; senao mapeia o PKIFailureInfo.
        if (!resposta.Granted)
        {
            throw new CarimboDeTempoException(MapearFalha(resposta.FailureInfo));
        }

        // (b) MessageImprint do TSTInfo bate com o hash enviado.
        if (!string.Equals(resposta.HashCarimbado?.Trim().ToLowerInvariant(), hashDocumento.Valor, StringComparison.Ordinal))
        {
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.ImprintDivergente);
        }

        // (c) nonce ecoado == nonce enviado.
        if (!string.Equals(resposta.Nonce, nonce, StringComparison.Ordinal))
        {
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.NonceDivergente);
        }

        // (d) cadeia/EKU: o emissor consta em EmissoresConfiaveis (validacao da cadeia credenciada).
        //     Stub: confere o thumbprint anunciado contra a allowlist do tenant. Lista vazia => modo
        //     dev permissivo. TODO(M10): validar a assinatura CMS e o EKU id-kp-timeStamping reais.
        if (_opcoes.EmissoresConfiaveis.Count > 0
            && (resposta.EmissorThumbprint is null
                || !_opcoes.EmissoresConfiaveis.Contains(resposta.EmissorThumbprint, StringComparer.OrdinalIgnoreCase)))
        {
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.CadeiaNaoConfiavel);
        }

        // (e) genTime dentro da janela tolerada.
        var agora = DateTime.UtcNow;
        var tolerancia = TimeSpan.FromSeconds(_opcoes.ToleranciaGenTimeSegundos);
        if (resposta.GenTimeUtc < agora - tolerancia || resposta.GenTimeUtc > agora + tolerancia)
        {
            throw new CarimboDeTempoException(FalhaCarimboDeTempo.GenTimeForaDaJanela);
        }

        var politica = string.IsNullOrWhiteSpace(resposta.Politica) ? _opcoes.PoliticaEsperada : resposta.Politica;

        return CarimboDeTempo.DeToken(
            resposta.GenTimeUtc,
            _opcoes.Autoridade,
            resposta.TokenBase64 ?? throw new CarimboDeTempoException(FalhaCarimboDeTempo.BadDataFormat, "TST ausente."),
            resposta.SerialToken ?? throw new CarimboDeTempoException(FalhaCarimboDeTempo.BadDataFormat, "Serial ausente."),
            politica,
            hashDocumento.Valor,
            AlgoritmoSha256);
    }

    private static FalhaCarimboDeTempo MapearFalha(string? failureInfo) => failureInfo?.ToLowerInvariant() switch
    {
        "badalg" => FalhaCarimboDeTempo.BadAlg,
        "badrequest" => FalhaCarimboDeTempo.BadRequest,
        "baddataformat" => FalhaCarimboDeTempo.BadDataFormat,
        "timenotavailable" => FalhaCarimboDeTempo.TimeNotAvailable,
        "unacceptedpolicy" => FalhaCarimboDeTempo.UnacceptedPolicy,
        "unacceptedextension" => FalhaCarimboDeTempo.UnacceptedExtension,
        "addinfonotavailable" => FalhaCarimboDeTempo.AddInfoNotAvailable,
        "systemfailure" => FalhaCarimboDeTempo.SystemFailure,
        _ => FalhaCarimboDeTempo.StatusNaoConcedido,
    };

    private static string GerarNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(NonceBytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private const string AlgoritmoSha256 = "SHA-256";
    private const int NonceBytes = 16;

    /// <summary>
    /// Stub do TimeStampReq (W9.4). TODO(M10): trocar por TimeStampRequest ASN.1/DER (BouncyCastle).
    /// </summary>
    private sealed record TsqStub(
        [property: JsonPropertyName("politica")] string Politica,
        [property: JsonPropertyName("algoritmo")] string Algoritmo,
        [property: JsonPropertyName("hash")] string Hash,
        [property: JsonPropertyName("nonce")] string Nonce);

    /// <summary>
    /// Stub do TimeStampResp/TSTInfo (W9.4) servido como JSON pela ACT-fake (WireMock).
    /// TODO(M10): parsear o TimeStampToken (CMS SignedData) DER real.
    /// </summary>
    private sealed record TstReplyStub
    {
        /// <summary>PKIStatus = granted/grantedWithMods.</summary>
        [JsonPropertyName("granted")]
        public bool Granted { get; init; }

        /// <summary>PKIFailureInfo textual quando nao concedido.</summary>
        [JsonPropertyName("failureInfo")]
        public string? FailureInfo { get; init; }

        /// <summary>MessageImprint ecoado (hash carimbado).</summary>
        [JsonPropertyName("hashCarimbado")]
        public string? HashCarimbado { get; init; }

        /// <summary>Nonce ecoado.</summary>
        [JsonPropertyName("nonce")]
        public string? Nonce { get; init; }

        /// <summary>genTime do TSTInfo (UTC).</summary>
        [JsonPropertyName("genTimeUtc")]
        public DateTime GenTimeUtc { get; init; }

        /// <summary>serialNumber do TSTInfo.</summary>
        [JsonPropertyName("serialToken")]
        public string? SerialToken { get; init; }

        /// <summary>OID da politica de carimbo.</summary>
        [JsonPropertyName("politica")]
        public string? Politica { get; init; }

        /// <summary>TST (CMS DER) em Base64 — prova oponivel.</summary>
        [JsonPropertyName("tokenBase64")]
        public string? TokenBase64 { get; init; }

        /// <summary>Thumbprint da AC do tempo emissora (validacao de cadeia no stub).</summary>
        [JsonPropertyName("emissorThumbprint")]
        public string? EmissorThumbprint { get; init; }
    }
}
