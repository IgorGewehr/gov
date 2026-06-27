using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

/// <summary>
/// Mapeamento PURO entre a tipologia interna de pessoal (Cargo/Regime previdenciario) e os codigos do
/// leiaute SIAPES do TCE-RS (titulo de admissao e regime juridico). Centraliza a correspondencia para
/// que o caso de uso preencha um ato de admissao a partir de um servidor existente, sem espalhar
/// conversoes pela aplicacao.
/// </summary>
public static class MapeamentoSiapes
{
    /// <summary>
    /// Deriva o TITULO de admissao (CD_TIPO_ATO) a partir do tipo do cargo: efetivo => concurso publico
    /// (01); temporario => prazo determinado (02); comissionado => outros (15), por nao decorrer de
    /// concurso. O operador pode sobrescrever quando o caso concreto exigir (ex.: decisao judicial).
    /// </summary>
    /// <param name="tipoCargo">Tipo do cargo provido pelo servidor.</param>
    /// <returns>Titulo de admissao SIAPES correspondente (padrao).</returns>
    public static TipoAtoAdmissao TituloPadraoDe(TipoCargo tipoCargo) => tipoCargo switch
    {
        TipoCargo.Efetivo => TipoAtoAdmissao.ConcursoPublico,
        TipoCargo.Temporario => TipoAtoAdmissao.PrazoDeterminado,
        TipoCargo.Comissionado => TipoAtoAdmissao.Outros,
        _ => TipoAtoAdmissao.Outros,
    };

    /// <summary>
    /// Deriva o REGIME JURIDICO (CD_REGIME_JURIDICO — Tabela 5) a partir do regime previdenciario:
    /// RPPS (efetivo estatutario) => Estatutario; RGPS => Celetista. Comissionados sem vinculo celetista
    /// devem ser ajustados pelo operador para Administrativo quando for o caso.
    /// </summary>
    /// <param name="regime">Regime previdenciario do servidor.</param>
    /// <returns>Regime juridico SIAPES correspondente (padrao).</returns>
    public static RegimeJuridicoSiapes RegimePadraoDe(RegimePrevidenciario regime) => regime switch
    {
        RegimePrevidenciario.Rpps => RegimeJuridicoSiapes.Estatutario,
        RegimePrevidenciario.Rgps => RegimeJuridicoSiapes.Celetista,
        _ => RegimeJuridicoSiapes.Administrativo,
    };
}
