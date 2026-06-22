using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo;

/// <summary>
/// Autoridade de carimbo de tempo LOCAL (relogio do sistema), usada em desenvolvimento/testes
/// como Anti-Corruption Layer ate a integracao com uma ACT credenciada ICP-Brasil (Lei 14.063/2020).
/// Em producao, substituir por um cliente HTTP resiliente (Polly) configurado por tenant.
/// </summary>
public sealed class CarimboDeTempoLocalService(TimeProvider timeProvider, IConfiguration configuration) : ICarimboDeTempoService
{
    /// <summary>Nome padrao da autoridade local quando nao configurado.</summary>
    public const string AutoridadePadrao = "Tensorroot.Gov ACT Local";

    /// <inheritdoc />
    public Task<CarimboDeTempo> GerarAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var autoridade = configuration["Protocolo:CarimboTempo:Autoridade"] ?? AutoridadePadrao;
        var carimbo = CarimboDeTempo.De(timeProvider.GetUtcNow().UtcDateTime, autoridade);
        return Task.FromResult(carimbo);
    }
}
