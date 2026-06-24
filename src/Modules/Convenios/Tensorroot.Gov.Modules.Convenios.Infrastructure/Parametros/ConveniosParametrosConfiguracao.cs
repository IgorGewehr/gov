using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Parametros;

/// <summary>
/// Implementacao de <see cref="IConveniosParametros"/> que resolve prazos/percentuais/indice por tenant a
/// partir da configuracao (secao <c>Convenios:Parametros</c>), com DEFAULTS legais quando o tenant nao
/// sobrescreve. Cada parametro carrega a NORMA-FONTE citavel — nenhum numero magico no dominio (CLAUDE.md
/// S7/S16). Os defaults espelham a tabela do DESIGN (A.5/B.5):
/// PC parcial 60 d / final 180 d; contrapartida 20%; PC OSC 90+30 d; analise OSC 150 d; saneamento 45 d.
/// <para>
/// // TODO(M10): evoluir de IConfiguration para tabela versionada por tenant+vigencia (espelhando
/// ParametroFiscalVigente do nucleo fiscal M7) + tabela Selic mensal por exercicio para o IndiceDevolucao.
/// </para>
/// </summary>
public sealed class ConveniosParametrosConfiguracao(IConfiguration configuration) : IConveniosParametros
{
    // Defaults legais (norma-fonte viaja com o parametro; nenhum literal solto no dominio).
    private const string SecaoBase = "Convenios:Parametros";

    /// <inheritdoc />
    public ParametroPrazo PrazoAnalisePcParcial(Guid tenantId)
        => ResolverPrazo("PrazoAnalisePcParcial", 60, UnidadePrazo.DiasCorridos, "Dec. 11.531/2023 (analise PC parcial)");

    /// <inheritdoc />
    public ParametroPrazo PrazoAnalisePcFinal(Guid tenantId)
        => ResolverPrazo("PrazoAnalisePcFinal", 180, UnidadePrazo.DiasCorridos, "Dec. 11.531/2023 (analise PC final)");

    /// <inheritdoc />
    public ParametroPercentual PercentualContrapartidaMinimo(Guid tenantId)
        => ResolverPercentual("PercentualContrapartidaMinimo", 20m, "Portaria Conjunta 33/2023 (contrapartida minima)");

    /// <inheritdoc />
    public ParametroPrazo PrazoEntregaPcOsc(Guid tenantId)
        => ResolverPrazo("PrazoEntregaPcOsc", 90, UnidadePrazo.DiasCorridos, "Lei 13.019/2014 art. 69 (entrega PC OSC)");

    /// <inheritdoc />
    public ParametroPrazo PrazoProrrogacaoPcOsc(Guid tenantId)
        => ResolverPrazo("PrazoProrrogacaoPcOsc", 30, UnidadePrazo.DiasCorridos, "Lei 13.019/2014 art. 69 (prorrogacao PC OSC)");

    /// <inheritdoc />
    public ParametroPrazo PrazoAnalisePcOsc(Guid tenantId)
        => ResolverPrazo("PrazoAnalisePcOsc", 150, UnidadePrazo.DiasCorridos, "Lei 13.019/2014 art. 71 (analise PC OSC)");

    /// <inheritdoc />
    public ParametroPrazo PrazoSaneamento(Guid tenantId)
        => ResolverPrazo("PrazoSaneamento", 45, UnidadePrazo.DiasCorridos, "Saneamento de pendencias (ambos os fluxos)");

    /// <inheritdoc />
    public ParametroIndiceDevolucao IndiceDevolucao(Guid tenantId, int exercicio)
    {
        // // TODO(M10): tabela Selic mensal por exercicio (hoje, percentual acumulado parametrizavel; 0 = sem juros).
        var secao = configuration.GetSection($"{SecaoBase}:IndiceDevolucao");
        var dia = secao.GetValue("DiaInicioContagem", 30);
        var percentual = secao.GetValue("PercentualAcumulado", 0m);
        var norma = secao.GetValue("NormaFonte", "Selic a partir do 30o dia (devolucao)") ?? "Selic a partir do 30o dia (devolucao)";
        return new ParametroIndiceDevolucao(dia, percentual, norma);
    }

    private ParametroPrazo ResolverPrazo(string chave, int defaultQuantidade, UnidadePrazo defaultUnidade, string defaultNorma)
    {
        var secao = configuration.GetSection($"{SecaoBase}:{chave}");
        var quantidade = secao.GetValue("Quantidade", defaultQuantidade);
        var unidade = secao.GetValue("Unidade", defaultUnidade);
        var norma = secao.GetValue("NormaFonte", defaultNorma) ?? defaultNorma;
        return new ParametroPrazo(quantidade, unidade, norma);
    }

    private ParametroPercentual ResolverPercentual(string chave, decimal defaultPercentual, string defaultNorma)
    {
        var secao = configuration.GetSection($"{SecaoBase}:{chave}");
        var percentual = secao.GetValue("Percentual", defaultPercentual);
        var norma = secao.GetValue("NormaFonte", defaultNorma) ?? defaultNorma;
        return new ParametroPercentual(percentual, norma);
    }
}
