using Microsoft.Extensions.Options;
using Tensorroot.Gov.Modules.Legislativo.Application;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;
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

    /// <inheritdoc />
    public IdentificacaoEnte IdentificacaoEnte()
    {
        var lexml = _options.Lexml;
        if (string.IsNullOrWhiteSpace(lexml.Uf) || string.IsNullOrWhiteSpace(lexml.Municipio))
        {
            throw new InvalidOperationException(
                "Jurisdicao LexML do tenant nao configurada (Legislativo:Lexml:Uf e :Municipio sao obrigatorios para o export LexML).");
        }

        var autoridade = string.IsNullOrWhiteSpace(lexml.Autoridade) ? LexmlOptions.AutoridadePadrao : lexml.Autoridade;
        return Domain.Normas.Lexml.IdentificacaoEnte.De(lexml.Uf, lexml.Municipio, autoridade);
    }

    /// <inheritdoc />
    public ParametrosArt29A ParametrosArt29A()
    {
        var limite = _options.LimiteCamara;
        var faixas = limite.Faixas.Select(faixa => FaixaPopulacional.De(faixa.PopulacaoMaximaInclusive, faixa.Percentual));
        return Domain.LimiteCamara.ParametrosArt29A.De(
            faixas,
            limite.SubtetoFolhaSobreRepasse,
            limite.LimiarAtencao,
            limite.ExercicioCorteInativos);
    }
}
