using System.Globalization;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Gateway SICONFI simulado (dev/demonstracao): retorna um protocolo deterministico por declaracao,
/// permitindo exercitar o ciclo Consolidada -&gt; Transmitida sem o ambiente real da STN. A implementacao
/// real fala SOAP/REST com resiliencia (Polly) atras de uma Anti-Corruption Layer, idempotente por
/// <see cref="DeclaracaoFiscalId"/>.
/// </summary>
public sealed class SimuladoSiconfiGateway(TimeProvider timeProvider) : ISiconfiGateway
{
    /// <inheritdoc />
    public Task<ResultadoTransmissaoSiconfi> TransmitirAsync(DeclaracaoFiscal declaracao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(declaracao);

        var dataTransmissao = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var protocolo = string.Create(
            CultureInfo.InvariantCulture,
            $"SICONFI-{declaracao.TipoDeclaracao}-{declaracao.Id.Value:N}");

        return Task.FromResult(new ResultadoTransmissaoSiconfi(protocolo, dataTransmissao));
    }
}
