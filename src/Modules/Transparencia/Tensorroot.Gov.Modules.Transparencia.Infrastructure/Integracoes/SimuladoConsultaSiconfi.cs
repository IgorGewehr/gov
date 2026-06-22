using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Consulta SICONFI simulada (dev/demonstração): retorna valores publicados determinísticos para
/// exercitar a reconciliação sem o ambiente real da STN. A implementação real
/// (<see cref="ConsultaSiconfiHttp"/>) fala com a API de Dados Abertos atrás de Polly. Somente CONSULTA.
/// </summary>
public sealed class SimuladoConsultaSiconfi : IConsultaSiconfi
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ValorPublicadoSiconfi>> ConsultarValoresAsync(
        ConsultaSiconfiCriterio criterio,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criterio);

        // Publica valores que BATEM com as contas do seed (1.000,00) para o caminho conciliado, e uma
        // conta com divergência proposital, exercitando a detecção.
        IReadOnlyList<ValorPublicadoSiconfi> valores =
        [
            new("111110100", "saldo", 1_000.00m),
            new("211110000", "saldo", 900.00m),
        ];

        return Task.FromResult(valores);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EntregaSiconfi>> ConsultarEntregasAsync(
        string idEnte,
        int exercicio,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idEnte);

        IReadOnlyList<EntregaSiconfi> entregas =
        [
            new("MSC", exercicio.ToString(System.Globalization.CultureInfo.InvariantCulture), "Em preenchimento", null),
        ];

        return Task.FromResult(entregas);
    }
}
