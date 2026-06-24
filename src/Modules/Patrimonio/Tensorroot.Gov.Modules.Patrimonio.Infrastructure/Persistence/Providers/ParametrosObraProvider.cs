using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Patrimonio.Application;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Providers;

/// <summary>
/// Implementação de <see cref="IParametrosObraProvider"/> baseada em <see cref="ObraOptions"/> (configuração
/// por tenant). Traduz os parâmetros do art. 94 §3 (quantidade/unidade/norma) em
/// <see cref="PrazoArt94Parametro"/> consumidos pelo agregado/handlers — sem número mágico no código
/// (CLAUDE.md §7/§16).
/// </summary>
/// <param name="options">Opções configuradas de obras para o tenant.</param>
public sealed class ParametrosObraProvider(IOptions<ObraOptions> options) : IParametrosObraProvider
{
    private readonly ObraOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public PrazoArt94Parametro PrazoAposAssinatura()
        => new(_options.AposAssinaturaQuantidade, _options.AposAssinaturaUnidade, _options.AposAssinaturaNormaFonte);

    /// <inheritdoc />
    public PrazoArt94Parametro PrazoAposConclusao()
        => new(_options.AposConclusaoQuantidade, _options.AposConclusaoUnidade, _options.AposConclusaoNormaFonte);

    /// <inheritdoc />
    public int AntecedenciaAlertaDiasUteis() => _options.AntecedenciaAlertaDiasUteis;
}
