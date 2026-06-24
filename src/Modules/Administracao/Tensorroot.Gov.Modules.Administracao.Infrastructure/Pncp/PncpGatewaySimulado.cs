using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Pncp;

/// <summary>
/// Implementacao SIMULADA do <see cref="IPncpGateway"/> (ACL do PNCP): exercita o fluxo de divulgacao de
/// contrato ponta-a-ponta (pre-cadastro logico + idempotencia) SEM credenciais nem rede de producao,
/// devolvendo um NUMERO DE CONTROLE PNCP DETERMINISTICO derivado da chave de idempotencia (mesma chave =&gt;
/// mesmo numero, garantindo idempotencia at-least-once do handler do Outbox). No M9 e o alvo dos testes
/// contra WireMock (schemas do Manual de Integracao).
/// <para>
/// // TODO(M10): substituir por <c>PncpGatewayHttp</c> — cliente HTTP real do PNCP (login JWT ~1h,
/// pre-cadastro orgao -&gt; unidade -&gt; compra/edital -&gt; contrato -&gt; aditivo -&gt; arquivos) atras de
/// <c>AddStandardResilienceHandler</c> (Polly), com credenciais/JWT no Azure Key Vault e validacao em
/// <c>treina.pncp.gov.br</c> antes da base <c>pncp.gov.br</c> de producao. A interface NAO muda.
/// </para>
/// </summary>
public sealed class PncpGatewaySimulado(ILogger<PncpGatewaySimulado> logger) : IPncpGateway
{
    /// <inheritdoc />
    public Task<PublicacaoContratoPncpResultado> PublicarContratoAsync(
        PublicacaoContratoPncpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validacao local minima (barra ANTES de "transmitir") — espelha a guarda da impl. real.
        if (string.IsNullOrWhiteSpace(request.CnpjOrgao))
        {
            return Task.FromResult(PublicacaoContratoPncpResultado.Falha("PNCP-400", "CNPJ do orgao ausente."));
        }

        if (string.IsNullOrWhiteSpace(request.ChaveIdempotencia))
        {
            return Task.FromResult(PublicacaoContratoPncpResultado.Falha("PNCP-400", "Chave de idempotencia ausente."));
        }

        // Numero de controle DETERMINISTICO pela chave de idempotencia: replays geram o MESMO numero,
        // de modo que reprocessar a mensagem do Outbox (at-least-once) nao "republica" outro registro.
        var numeroControle = GerarNumeroControleDeterministico(request);

        logger.LogInformation(
            "[PNCP SIMULADO] Contrato {ContratoId} divulgado (orgao {CnpjOrgao}, unidade {Unidade}): numero de controle {Numero}.",
            request.ContratoId,
            request.CnpjOrgao,
            request.CodigoUnidade,
            numeroControle);

        return Task.FromResult(PublicacaoContratoPncpResultado.Ok(numeroControle));
    }

    // Formato lembra o numero de controle PNCP (cnpj-1-sequencial/ano); aqui o "sequencial" e um hash
    // estavel da chave de idempotencia, garantindo determinismo sem persistir contador.
    private static string GerarNumeroControleDeterministico(PublicacaoContratoPncpRequest request)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(request.ChaveIdempotencia));
        var sequencial = BitConverter.ToUInt32(bytes, 0) % 1_000_000u;
        var raizCnpj = new string(request.CnpjOrgao.Where(char.IsDigit).Take(8).ToArray()).PadLeft(8, '0');
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{raizCnpj}-1-{sequencial:D6}/{request.DataAssinatura.Year}");
    }
}
