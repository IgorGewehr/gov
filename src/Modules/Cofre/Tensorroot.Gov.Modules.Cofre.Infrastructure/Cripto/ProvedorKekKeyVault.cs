using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Provedor de KEK para PRODUCAO: a chave-mestra vive no Azure Key Vault/HSM e as operacoes
/// wrap/unwrap (RSA-OAEP) acontecem DENTRO do cofre — a KEK NUNCA sai do HSM (A1-DESIGN §1/§7,
/// risco 2). Toda chamada ao Key Vault passa por Polly (timeout + retry + circuit breaker —
/// CLAUDE.md §11/A1-DESIGN §5). // TODO(validar-oficial): algoritmo de wrap (RSA-OAEP-256) e
/// politica de rotacao conforme padrao do tenant.
/// </summary>
internal sealed class ProvedorKekKeyVault : IProvedorKek
{
    private static readonly KeyWrapAlgorithm Algoritmo = KeyWrapAlgorithm.RsaOaep256;

    private readonly CryptographyClient _cliente;
    private readonly ResiliencePipeline _resiliencia;

    /// <summary>Inicializa o provedor com o cliente de criptografia do Key Vault.</summary>
    /// <param name="options">Opcoes do cofre (URI do Key Vault e nome da KEK).</param>
    /// <exception cref="InvalidOperationException">Se a configuracao de PRODUCAO estiver incompleta.</exception>
    public ProvedorKekKeyVault(IOptions<CofreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var valores = options.Value;

        if (string.IsNullOrWhiteSpace(valores.KeyVaultUri) || string.IsNullOrWhiteSpace(valores.KekKeyName))
        {
            throw new InvalidOperationException(
                "Cofre:KeyVaultUri e Cofre:KekKeyName sao obrigatorios para o provedor KeyVault (PRODUCAO).");
        }

        var keyId = new Uri(new Uri(valores.KeyVaultUri), $"keys/{valores.KekKeyName}");
        KekKeyId = keyId.ToString();

        // DefaultAzureCredential: Managed Identity em PROD; sem segredo no repo (CLAUDE.md §6).
        _cliente = new CryptographyClient(keyId, new DefaultAzureCredential());

        _resiliencia = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true,
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(15),
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    /// <inheritdoc />
    public string KekKeyId { get; }

    /// <inheritdoc />
    public async Task<byte[]> EnvolverDekAsync(ReadOnlyMemory<byte> dek, CancellationToken cancellationToken)
    {
        var resultado = await _resiliencia.ExecuteAsync(
            async ct => await _cliente.WrapKeyAsync(Algoritmo, dek.ToArray(), ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        return resultado.EncryptedKey;
    }

    /// <inheritdoc />
    public async Task<byte[]> RevelarDekAsync(ReadOnlyMemory<byte> dekEmbrulhada, string kekKeyId, CancellationToken cancellationToken)
    {
        var resultado = await _resiliencia.ExecuteAsync(
            async ct => await _cliente.UnwrapKeyAsync(Algoritmo, dekEmbrulhada.ToArray(), ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        return resultado.Key;
    }
}
