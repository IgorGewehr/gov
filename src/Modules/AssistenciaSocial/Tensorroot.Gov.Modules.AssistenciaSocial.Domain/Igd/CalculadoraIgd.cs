namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Igd;

/// <summary>
/// Fatores LOCAIS que alimentam a estimativa do IGD (cada fator no intervalo [0,1]). Sao indicadores
/// que o municipio JA possui localmente — taxa de atualizacao cadastral, cumprimento de condicionalidades
/// e gestao de beneficios — espelhando a logica dos componentes do IGD-PBF/IGD-SUAS, porem calculados a
/// partir do dado proprio (nao do extrato oficial do MDS).
/// </summary>
/// <param name="FatorAtualizacaoCadastral">Proporcao de familias com cadastro dentro da vigencia (TAC local).</param>
/// <param name="FatorCondicionalidades">Proporcao de acompanhamentos sem descumprimento efetivo.</param>
/// <param name="FatorGestaoBeneficios">Proporcao de beneficios geridos sem pendencia (deferidos/concedidos).</param>
public readonly record struct FatoresIgd(
    decimal FatorAtualizacaoCadastral,
    decimal FatorCondicionalidades,
    decimal FatorGestaoBeneficios);

/// <summary>
/// Resultado da ESTIMATIVA do IGD (indice gerencial local, 0 a 1). NAO e o IGD oficial: o indice oficial
/// e calculado pelo MDS a partir de bases federais (SICON/CECAD). Este valor apoia a gestao local.
/// </summary>
/// <param name="Indice">Indice estimado (media dos fatores, 0 a 1), arredondado a 4 casas.</param>
/// <param name="Fatores">Fatores locais usados na estimativa (transparencia do calculo).</param>
/// <param name="Rotulo">Rotulo obrigatorio: deixa explicito que e estimativa local, nao oficial.</param>
public readonly record struct EstimativaIgdResultado(decimal Indice, FatoresIgd Fatores, string Rotulo);

/// <summary>
/// <b>3d.3 — Calculadora da EstimativaIgd (servico de dominio, sem estado).</b> Combina os fatores LOCAIS
/// (atualizacao cadastral × condicionalidades × gestao de beneficios) num indice 0-1, espelhando a logica
/// do IGD-PBF/IGD-SUAS — mas SEMPRE rotulado como <i>estimativa local, nao oficial</i>. O IGD oficial e o
/// AgilizaSUAS dependem de credencial/convenio MDS.
/// // TODO(M10): substituir os fatores locais pelos componentes oficiais do IGD (SICON/CECAD) e a
/// publicacao no AgilizaSUAS — requer credencial MDS (AgilizaSUAS sem API publica estavel documentada).
/// </summary>
public static class CalculadoraIgd
{
    /// <summary>Rotulo obrigatorio que acompanha toda estimativa (nao confundir com o IGD oficial do MDS).</summary>
    public const string RotuloEstimativaLocal = "Estimativa gerencial local — nao e o IGD oficial (MDS).";

    /// <summary>
    /// Estima o IGD a partir dos fatores locais. O indice e a media simples dos tres fatores (cada um
    /// em [0,1]); cada fator e clampeado em [0,1] para robustez. Resultado em [0,1].
    /// </summary>
    /// <param name="fatores">Fatores locais (cada um esperado em [0,1]).</param>
    /// <returns>Estimativa do IGD (indice + fatores + rotulo).</returns>
    public static EstimativaIgdResultado Estimar(FatoresIgd fatores)
    {
        var tac = Clampear(fatores.FatorAtualizacaoCadastral);
        var cond = Clampear(fatores.FatorCondicionalidades);
        var gestao = Clampear(fatores.FatorGestaoBeneficios);

        var indice = decimal.Round((tac + cond + gestao) / 3m, 4, MidpointRounding.AwayFromZero);
        var normalizados = new FatoresIgd(tac, cond, gestao);
        return new EstimativaIgdResultado(indice, normalizados, RotuloEstimativaLocal);
    }

    private static decimal Clampear(decimal fator) => Math.Clamp(fator, 0m, 1m);
}
