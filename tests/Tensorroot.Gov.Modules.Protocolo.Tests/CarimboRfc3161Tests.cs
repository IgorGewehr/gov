using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Carimbo;
using Xunit;

namespace Tensorroot.Gov.Modules.Protocolo.Tests;

/// <summary>
/// Cobertura do carimbo de tempo RFC 3161 (Peca 1 / W9.4): o adapter <see cref="CarimbadorDeTempoAct"/>
/// monta o pedido (hash + nonce), valida a resposta da ACT (status/PKIFailureInfo, imprint, nonce,
/// genTime, cadeia) e mapeia para um <see cref="CarimboDeTempo"/> de origem ACT VINCULADO ao hash.
/// O transporte HTTP e simulado por um <see cref="HttpMessageHandler"/> de teste (sem rede), no lugar
/// do WireMock do harness W9.8. Espelha CarimboRfc3161.rules.md.
/// </summary>
public sealed class CarimboRfc3161Tests
{
    private const string HashDoc = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";
    private const string TokenStub = "MIID-stub-token-base64==";

    /// <summary>
    /// Handler de teste: intercepta o POST, le o nonce enviado e produz a resposta da ACT a partir de
    /// uma funcao do caller (que pode ecoar o nonce/hash ou divergir deliberadamente).
    /// </summary>
    private sealed class AtaFake(Func<string, object> montarResposta) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var corpo = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(corpo);
            var nonce = doc.RootElement.GetProperty("nonce").GetString()!;

            var json = JsonSerializer.Serialize(montarResposta(nonce));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static CarimbadorDeTempoAct Criar(Func<string, object> montarResposta, OpcoesAct? opcoes = null)
    {
        var http = new HttpClient(new AtaFake(montarResposta)) { BaseAddress = new Uri("https://act.teste.local/") };
        return new CarimbadorDeTempoAct(http, Options.Create(opcoes ?? new OpcoesAct()));
    }

    [Fact] // C-1: granted -> TST validado e VINCULADO ao hash (origem ACT, hash carimbado == hash doc).
    public async Task Granted_vincula_o_tst_ao_hash()
    {
        var carimbador = Criar(nonce => new
        {
            granted = true,
            hashCarimbado = HashDoc,
            nonce,
            genTimeUtc = DateTime.UtcNow,
            serialToken = "0A1B2C",
            politica = "2.16.76.1.6.999",
            tokenBase64 = TokenStub,
        });

        var carimbo = await carimbador.CarimbarAsync(Hash.De(HashDoc), CancellationToken.None);

        carimbo.Origem.Should().Be(OrigemCarimbo.Act);
        carimbo.HashCarimbado.Should().Be(HashDoc);
        carimbo.TokenBase64.Should().Be(TokenStub);
        carimbo.SerialToken.Should().Be("0A1B2C");
        carimbo.AtestaHash(HashDoc).Should().BeTrue();
    }

    [Fact] // C-2: PKIFailureInfo (rejection) -> excecao de dominio mapeada, NADA persiste.
    public async Task Rejection_mapeia_pkifailureinfo()
    {
        var carimbador = Criar(_ => new
        {
            granted = false,
            failureInfo = "unacceptedPolicy",
        });

        var acao = async () => await carimbador.CarimbarAsync(Hash.De(HashDoc), CancellationToken.None);

        (await acao.Should().ThrowAsync<CarimboDeTempoException>())
            .Which.Falha.Should().Be(FalhaCarimboDeTempo.UnacceptedPolicy);
    }

    [Fact] // C-3: imprint divergente (ACT atesta outro hash) -> ImprintDivergente, carimbo recusado.
    public async Task Imprint_divergente_e_recusado()
    {
        var carimbador = Criar(nonce => new
        {
            granted = true,
            hashCarimbado = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            nonce,
            genTimeUtc = DateTime.UtcNow,
            serialToken = "0A1B2C",
            politica = "2.16.76.1.6.999",
            tokenBase64 = TokenStub,
        });

        var acao = async () => await carimbador.CarimbarAsync(Hash.De(HashDoc), CancellationToken.None);

        (await acao.Should().ThrowAsync<CarimboDeTempoException>())
            .Which.Falha.Should().Be(FalhaCarimboDeTempo.ImprintDivergente);
    }

    [Fact] // C-4: nonce divergente (replay) -> NonceDivergente, carimbo recusado.
    public async Task Nonce_divergente_e_recusado()
    {
        var carimbador = Criar(_ => new
        {
            granted = true,
            hashCarimbado = HashDoc,
            nonce = "deadbeef", // ignora o nonce enviado -> replay.
            genTimeUtc = DateTime.UtcNow,
            serialToken = "0A1B2C",
            politica = "2.16.76.1.6.999",
            tokenBase64 = TokenStub,
        });

        var acao = async () => await carimbador.CarimbarAsync(Hash.De(HashDoc), CancellationToken.None);

        (await acao.Should().ThrowAsync<CarimboDeTempoException>())
            .Which.Falha.Should().Be(FalhaCarimboDeTempo.NonceDivergente);
    }

    [Fact] // C-5: genTime fora da janela tolerada -> GenTimeForaDaJanela.
    public async Task GenTime_fora_da_janela_e_recusado()
    {
        var carimbador = Criar(nonce => new
        {
            granted = true,
            hashCarimbado = HashDoc,
            nonce,
            genTimeUtc = DateTime.UtcNow.AddHours(2), // muito alem da tolerancia (300s).
            serialToken = "0A1B2C",
            politica = "2.16.76.1.6.999",
            tokenBase64 = TokenStub,
        });

        var acao = async () => await carimbador.CarimbarAsync(Hash.De(HashDoc), CancellationToken.None);

        (await acao.Should().ThrowAsync<CarimboDeTempoException>())
            .Which.Falha.Should().Be(FalhaCarimboDeTempo.GenTimeForaDaJanela);
    }

    [Fact] // C-6: cadeia nao-confiavel (emissor fora da allowlist do tenant) -> CadeiaNaoConfiavel.
    public async Task Cadeia_nao_confiavel_e_recusada()
    {
        var opcoes = new OpcoesAct();
        opcoes.EmissoresConfiaveis.Add("THUMBPRINT-CONFIAVEL");

        var carimbador = Criar(
            nonce => new
            {
                granted = true,
                hashCarimbado = HashDoc,
                nonce,
                genTimeUtc = DateTime.UtcNow,
                serialToken = "0A1B2C",
                politica = "2.16.76.1.6.999",
                tokenBase64 = TokenStub,
                emissorThumbprint = "THUMBPRINT-DESCONHECIDO",
            },
            opcoes);

        var acao = async () => await carimbador.CarimbarAsync(Hash.De(HashDoc), CancellationToken.None);

        (await acao.Should().ThrowAsync<CarimboDeTempoException>())
            .Which.Falha.Should().Be(FalhaCarimboDeTempo.CadeiaNaoConfiavel);
    }
}
