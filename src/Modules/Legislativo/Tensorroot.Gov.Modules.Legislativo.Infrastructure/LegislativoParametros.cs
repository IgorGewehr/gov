using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Legislativo.Application;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure;

/// <summary>
/// Implementacao de <see cref="ILegislativoParametros"/> baseada em <see cref="LegislativoOptions"/>
/// (configuracao por tenant). Traduz os parametros do Regimento Interno em value objects do dominio.
/// </summary>
/// <param name="options">Opcoes configuradas do modulo Legislativo.</param>
public sealed class LegislativoParametros(IOptions<LegislativoOptions> options) : ILegislativoParametros
{
    private readonly LegislativoOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public Interstico IntersticioEntreTurnos()
        => Interstico.DeDias(_options.IntersticioEntreTurnosDias);
}
