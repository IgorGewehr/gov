using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Administracao.Application;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Dispensas;

/// <summary>
/// Implementacao de <see cref="IDispensaParametros"/> baseada em <see cref="DispensaOptions"/>
/// (configuracao por tenant). Traduz os limites do art. 75, I/II e o prazo minimo de divulgacao
/// (IN SEGES/ME 67/2021) em valores citaveis consumidos pelos handlers/agregado — sem numero magico no
/// codigo (CLAUDE.md §7/§16).
/// </summary>
/// <param name="options">Opcoes configuradas da dispensa para o tenant.</param>
public sealed class DispensaParametros(IOptions<DispensaOptions> options) : IDispensaParametros
{
    private readonly DispensaOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public LimiteDispensa LimiteVigente(FundamentoDispensaValor fundamento)
    {
        var valor = fundamento switch
        {
            FundamentoDispensaValor.ObrasEServicosEngenharia => _options.LimiteObrasEngenharia,
            FundamentoDispensaValor.OutrosServicosECompras => _options.LimiteOutrosServicosCompras,
            _ => throw new ArgumentOutOfRangeException(nameof(fundamento), fundamento, "Fundamento de dispensa invalido."),
        };

        return new LimiteDispensa(valor, _options.LimiteNormaFonte);
    }

    /// <inheritdoc />
    public int PrazoMinimoDivulgacaoDiasUteis() => _options.PrazoMinimoDivulgacaoDiasUteis;
}
