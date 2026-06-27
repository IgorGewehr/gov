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

    /// <summary>tpRegTrab — CLT (S-1.3 <c>vinculo/tpRegTrab</c> = 1).</summary>
    public const int TpRegTrabClt = 1;

    /// <summary>tpRegTrab — Estatutario/regimes proprios (S-1.3 <c>vinculo/tpRegTrab</c> = 2).</summary>
    public const int TpRegTrabEstatutario = 2;

    /// <summary>
    /// Deriva o <c>tpRegTrab</c> (regime trabalhista do vinculo, S-1.3 <c>vinculo/tpRegTrab</c>) a partir do
    /// regime previdenciario do servidor — coerente com o resto do RH, que usa o
    /// <see cref="RegimePrevidenciario"/> como proxy de estatutario/celetista: RPPS (cargo efetivo) =>
    /// Estatutario (2); RGPS (comissionado/temporario) => CLT (1). // TODO(validar-oficial): quando o cargo
    /// carregar o regime juridico explicito (RJU x CLT), usar o do cadastro em vez de derivar do
    /// previdenciario (ver achado P1-6 SIAPES).
    /// </summary>
    /// <param name="regime">Regime previdenciario do servidor.</param>
    /// <returns>2 (Estatutario) para RPPS; 1 (CLT) para RGPS.</returns>
    public static int DerivarTpRegTrab(RegimePrevidenciario regime)
        => regime == RegimePrevidenciario.Rpps ? TpRegTrabEstatutario : TpRegTrabClt;

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
