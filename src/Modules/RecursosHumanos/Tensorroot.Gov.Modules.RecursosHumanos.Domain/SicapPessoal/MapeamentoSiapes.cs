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
    /// Deriva o REGIME JURIDICO (CD_REGIME_JURIDICO — Tabela 5 do SIAPES) a partir do TIPO DO CARGO, que e
    /// o atributo proprio do vinculo — NAO do regime previdenciario (correcao P1-6 da AUDITORIA-FINAL): o
    /// regime juridico (estatutario/celetista/administrativo) e o regime previdenciario (RPPS/RGPS) sao
    /// dimensoes independentes — um efetivo estatutario num municipio SEM RPPS proprio recolhe ao RGPS e
    /// ainda assim e ESTATUTARIO. Mapeamento: efetivo => Estatutario (cargo de provimento efetivo, RJU);
    /// comissionado => Administrativo (provimento em comissao); temporario => Administrativo (contratacao
    /// administrativa por prazo determinado — CF art. 37, IX). O regime CELETISTA (emprego publico CLT) nao
    /// decorre de nenhum <see cref="TipoCargo"/> modelado; quando aplicavel, o operador o informa via
    /// override (ex.: empregado de fundacao/empresa publica).
    /// </summary>
    /// <param name="tipoCargo">Tipo do cargo provido pelo servidor.</param>
    /// <returns>Regime juridico SIAPES correspondente (padrao).</returns>
    public static RegimeJuridicoSiapes RegimePadraoDe(TipoCargo tipoCargo) => tipoCargo switch
    {
        TipoCargo.Efetivo => RegimeJuridicoSiapes.Estatutario,
        TipoCargo.Comissionado => RegimeJuridicoSiapes.Administrativo,
        TipoCargo.Temporario => RegimeJuridicoSiapes.Administrativo,
        _ => RegimeJuridicoSiapes.Administrativo,
    };
}
