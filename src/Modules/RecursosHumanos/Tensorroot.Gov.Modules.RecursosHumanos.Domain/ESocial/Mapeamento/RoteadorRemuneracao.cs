using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>
/// Roteia a remuneracao de UM servidor entre S-1200 (RGPS) e S-1202 (RPPS) e deriva o <c>tpRegPrev</c>
/// (servico de dominio PURO). CONFIRMADO (ESOCIAL-SPEC §1.6 / verificacao §3.2-3.3 — erro aqui rejeita
/// a folha inteira): S-1202 SOMENTE para RPPS; S-1200 para RGPS. <c>tpRegPrev</c>: 1=RGPS, 2=RPPS,
/// 3=Exterior, 4=SPSMFA. // TODO(validar-oficial): roteamento completo por <c>codCateg</c> (Tabela 01) +
/// tpRegPrev no XSD travado — hoje deriva do <see cref="RegimePrevidenciario"/> do servidor.
/// </summary>
public static class RoteadorRemuneracao
{
    /// <summary>tpRegPrev — RGPS.</summary>
    public const int TpRegPrevRgps = 1;

    /// <summary>tpRegPrev — RPPS.</summary>
    public const int TpRegPrevRpps = 2;

    /// <summary>Deriva o <c>tpRegPrev</c> a partir do regime previdenciario do servidor.</summary>
    /// <param name="regime">Regime previdenciario do servidor.</param>
    /// <returns>1 (RGPS) ou 2 (RPPS).</returns>
    public static int DerivarTpRegPrev(RegimePrevidenciario regime)
        => regime == RegimePrevidenciario.Rpps ? TpRegPrevRpps : TpRegPrevRgps;

    /// <summary>Indica se a remuneracao deve ser transmitida como S-1202 (RPPS) em vez de S-1200 (RGPS).</summary>
    /// <param name="regime">Regime previdenciario do servidor.</param>
    /// <returns><c>true</c> para S-1202 (RPPS); <c>false</c> para S-1200 (RGPS).</returns>
    public static bool EhRpps(RegimePrevidenciario regime) => regime == RegimePrevidenciario.Rpps;

    /// <summary>Tipo do evento de remuneracao do servidor conforme o regime.</summary>
    /// <param name="regime">Regime previdenciario.</param>
    /// <returns>S-1202 (RPPS) ou S-1200 (RGPS).</returns>
    public static TipoEventoESocial TipoRemuneracao(RegimePrevidenciario regime)
        => EhRpps(regime) ? TipoEventoESocial.S1202RemuneracaoRpps : TipoEventoESocial.S1200Remuneracao;
}
