using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Carimbo;

/// <summary>
/// Carimbador de tempo LOCAL (relogio do sistema) — fallback/dev (renomeado de
/// <c>CarimboDeTempoLocalService</c> em W9.4). Produz um <see cref="CarimboDeTempo"/> de
/// <see cref="OrigemCarimbo.Local"/>, SEM token oponivel: serve a desenvolvimento/testes e a
/// tenants sem ACT configurada. Em producao so atende atos de criticidade Baixa/Media — o
/// dominio (<c>Documento.Assinar</c>) RECUSA carimbo local em ato qualificado (criticidade Alta).
/// </summary>
public sealed class CarimbadorDeTempoLocalService(TimeProvider timeProvider, IConfiguration configuration)
    : ICarimbadorDeTempo, ICarimboDeTempoService
{
    /// <summary>Nome padrao da autoridade local quando nao configurado.</summary>
    public const string AutoridadePadrao = "Tensorroot.Gov ACT Local";

    /// <inheritdoc />
    public Task<CarimboDeTempo> CarimbarAsync(Hash hashDocumento, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hashDocumento);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GerarLocal());
    }

    /// <summary>
    /// Contrato LEGADO (<see cref="ICarimboDeTempoService"/>) — sem hash de entrada. Mantido como
    /// adapter para nao quebrar consumidores antigos; produz o mesmo carimbo local sem token.
    /// </summary>
    public Task<CarimboDeTempo> GerarAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GerarLocal());
    }

    private CarimboDeTempo GerarLocal()
    {
        var autoridade = configuration["Protocolo:CarimboTempo:Autoridade"] ?? AutoridadePadrao;
        return CarimboDeTempo.De(timeProvider.GetUtcNow().UtcDateTime, autoridade);
    }
}
