using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Integracoes;

/// <summary>
/// Implementacao SIMULADA do <see cref="ITransferegovGateway"/> (fluxo A) para o M9. A leitura de dados
/// abertos (DTPAR) e respondida deterministicamente (sem rede); a transmissao da PC oficial e registrada e
/// devolve um protocolo simulado.
/// <para>
/// // TODO(M10): substituir por gateway HTTP real do Transferegov.br atras de Polly (AddStandardResilienceHandler)
/// + WireMock no harness de testes (W9.8). A leitura DTPAR e o primeiro passo; a TRANSMISSAO real exige
/// credenciais/certificado e e restrita a orgaos integrados (defere producao).
/// </para>
/// </summary>
public sealed class SimuladoTransferegovGateway(ILogger<SimuladoTransferegovGateway> logger) : ITransferegovGateway
{
    /// <inheritdoc />
    public Task<DadosConvenioTransferegov?> ConsultarConvenioAsync(string numeroConvenio, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroConvenio);
        logger.LogInformation("Consulta DTPAR (simulada) ao Transferegov para o convenio {Numero}.", numeroConvenio);

        // // TODO(M10): consulta real a API de dados abertos do Transferegov.br.
        var dados = new DadosConvenioTransferegov(numeroConvenio, "EM_EXECUCAO", "00000000000000", 0m);
        return Task.FromResult<DadosConvenioTransferegov?>(dados);
    }

    /// <inheritdoc />
    public Task<string> TransmitirPrestacaoContasAsync(string numeroConvenio, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroConvenio);
        // // TODO(M10): transmissao oficial real (creds/cert + base de producao).
        logger.LogWarning(
            "Transmissao de PC ao Transferegov SIMULADA para o convenio {Numero} (integracao real defere M10).",
            numeroConvenio);
        var protocolo = $"SIMULADO-{numeroConvenio}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        return Task.FromResult(protocolo);
    }
}
