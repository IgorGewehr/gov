using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Tensorroot.Gov.IntegrationTests.Infrastructure;

/// <summary>
/// Stubs IN-PROCESS dos sistemas governamentais externos (PNCP, eSocial, TCE-RS), via WireMock.Net.
///
/// Ambiente SEM Docker (decisao de plataforma): este servidor sobe na propria maquina de teste numa
/// porta efemera de loopback, sem container. Substitui Testcontainers, que fica como melhoria de CI
/// (// TODO M10/CI). Reutilizavel por W9.1 (PNCP), W9.4 (TSP/ACT) e W9.6 (Transferegov), conforme os
/// gateways/ACL forem implementados — hoje os schemas sao stubs de "rede de protecao".
///
/// IMPORTANTE: no estado atual NAO ha cliente HTTP real chamando estes endpoints (a ACL do PNCP e
/// futura — W9.1). Este harness existe para que, ao introduzir o gateway, o teste de integracao apenas
/// aponte a base-URL do gateway para <see cref="UrlBase"/> (via configuracao) — sem reescrever o stub.
/// </summary>
public sealed class StubExternosWireMock : IDisposable
{
    private readonly WireMockServer _servidor;

    /// <summary>Sobe o servidor de stub numa porta efemera de loopback e registra os mapeamentos default.</summary>
    public StubExternosWireMock()
    {
        _servidor = WireMockServer.Start(new WireMockServerSettings
        {
            UseSSL = false,
            // Porta 0 => o SO escolhe uma porta livre (sem colisao com a :5080 nem entre testes paralelos).
            Port = 0,
        });

        ConfigurarPncp();
        ConfigurarESocial();
        ConfigurarTce();
    }

    /// <summary>Base-URL do stub (ex.: http://localhost:53412) — injetavel na configuracao do gateway sob teste.</summary>
    public string UrlBase => _servidor.Url!;

    /// <summary>Acesso ao servidor para mapeamentos/assertivas ad-hoc num teste especifico.</summary>
    public WireMockServer Servidor => _servidor;

    // === PNCP (W9.1) — Manual de Integracao v2.5: login JWT + publicacao de contrato. ===
    private void ConfigurarPncp()
    {
        // Login: devolve um token de acesso (Bearer ~1h).
        _servidor
            .Given(Request.Create().WithPath("/pncp/v1/usuarios/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Authorization", "Bearer pncp-token-de-teste")
                .WithBodyAsJson(new { access_token = "pncp-token-de-teste", expires_in = 3600 }));

        // Publicacao de contrato: devolve o numero de controle PNCP (eficacia art. 94) — futuro bloqueio
        // de empenho sem este numero (W9.1) sera testado contra este stub.
        _servidor
            .Given(Request.Create().WithPath("/pncp/v1/orgaos/*/contratos").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBodyAsJson(new { numeroControlePNCP = "07000000000001-2-000001/2026" }));
    }

    // === eSocial (RH/M5) — recepcao de lote (SOAP/REST), retorno de protocolo. ===
    private void ConfigurarESocial()
    {
        _servidor
            .Given(Request.Create().WithPath("/esocial/empregador/lotes/eventos").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { status = new { cdResposta = 201, descResposta = "Lote recebido com sucesso." }, protocoloEnvio = "1.2.202606.0000000001" }));
    }

    // === TCE-RS (Transparencia/M4) — recepcao de remessa SIAPC/PAD. ===
    private void ConfigurarTce()
    {
        _servidor
            .Given(Request.Create().WithPath("/tce-rs/remessas").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(202)
                .WithBodyAsJson(new { situacao = "EM_PROCESSAMENTO", protocolo = "TCE-2026-0000001" }));
    }

    /// <inheritdoc />
    public void Dispose() => _servidor.Dispose();
}
