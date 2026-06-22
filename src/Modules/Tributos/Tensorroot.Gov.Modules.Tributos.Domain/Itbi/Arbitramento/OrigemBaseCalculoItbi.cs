namespace Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;

/// <summary>
/// Origem da base de cálculo do ITBI adotada (Tema 1.113/STJ, REsp 1.937.821).
/// Substitui o antigo booleano <c>BaseFoiValorVenal</c>: o valor venal de referência NUNCA é
/// origem de base — serve apenas a triagem/alerta. A base só sobe via processo de arbitramento
/// regular (CTN art. 148).
/// </summary>
public enum OrigemBaseCalculoItbi
{
    /// <summary>Base = valor declarado pelo contribuinte (regra padrão; presunção de veracidade — tese b).</summary>
    Declarada = 0,

    /// <summary>Base = valor arbitrado por processo administrativo regular (CTN art. 148) com contraditório.</summary>
    ArbitradaArt148 = 1,
}
