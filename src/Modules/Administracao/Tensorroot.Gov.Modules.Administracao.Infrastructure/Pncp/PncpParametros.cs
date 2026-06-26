using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Administracao.Application;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Pncp;

/// <summary>
/// Implementacao de <see cref="IPncpParametros"/> baseada em <see cref="PncpOptions"/> (configuracao por
/// tenant). Traduz os parametros do art. 94 (quantidade/unidade/norma) em triades
/// <see cref="ParametroPrazo"/> consumidas pelo agregado/handlers — sem numero magico no codigo
/// (CLAUDE.md §7/§16).
/// </summary>
/// <param name="options">Opcoes configuradas do PNCP para o tenant.</param>
public sealed class PncpParametros(IOptions<PncpOptions> options) : IPncpParametros
{
    private readonly PncpOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public ParametroPrazo Divulgacao()
        => new(_options.DivulgacaoQuantidade, _options.DivulgacaoUnidade, _options.DivulgacaoNormaFonte);

    /// <inheritdoc />
    public ParametroPrazo DivulgacaoDireta()
        => new(_options.DivulgacaoDiretaQuantidade, _options.DivulgacaoDiretaUnidade, _options.DivulgacaoDiretaNormaFonte);

    /// <inheritdoc />
    public ParametroPrazo RegistroExtrato()
        => new(_options.RegistroExtratoQuantidade, _options.RegistroExtratoUnidade, _options.RegistroExtratoNormaFonte);

    /// <inheritdoc />
    public int AntecedenciaAlertaDiasUteis() => _options.AntecedenciaAlertaDiasUteis;
}
