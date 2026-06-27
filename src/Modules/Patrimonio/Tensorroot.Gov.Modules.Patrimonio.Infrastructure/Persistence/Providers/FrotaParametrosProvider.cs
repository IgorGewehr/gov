using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Patrimonio.Application;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Providers;

/// <summary>
/// Implementação de <see cref="IFrotaParametrosProvider"/> baseada em <see cref="FrotaOptions"/>
/// (configuração por tenant). Expõe o sulco mínimo legal e as janelas de alerta de CNH/apólice sem
/// número mágico no código (CLAUDE.md §7/§16); os defaults espelham referências usuais (1,6 mm —
/// CONTRAN/CTB; 30 dias), todos sobrescritíveis por configuração. Espelha <c>ParametrosObraProvider</c>.
/// </summary>
/// <param name="options">Opções configuradas de frota para o tenant.</param>
public sealed class FrotaParametrosProvider(IOptions<FrotaOptions> options) : IFrotaParametrosProvider
{
    private readonly FrotaOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public decimal SulcoMinimoLegalMilimetros() => _options.SulcoMinimoLegalMilimetros;

    /// <inheritdoc />
    public int AlertaCnhVencendoDias() => _options.AlertaCnhVencendoDias;

    /// <inheritdoc />
    public int AlertaApoliceVencendoDias() => _options.AlertaApoliceVencendoDias;
}
