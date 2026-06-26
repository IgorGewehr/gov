namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

/// <summary>
/// Politica previdenciaria do ENTE: determina, em funcao de o municipio possuir ou nao RPPS proprio
/// instituido por lei municipal (EC 103/2019), qual regime (<see cref="RegimePrevidenciario"/>) se aplica
/// a cada <see cref="TipoCargo"/>. E PARAMETRO do tenant — substitui o antigo roteamento fixo
/// "efetivo → RPPS" (CLAUDE.md §7/§16).
/// <para>
/// Regra: o RPPS so abrange titulares de cargo EFETIVO, e somente quando o ente o institui por lei. Sem
/// RPPS proprio (caso default de municipios pequenos como Maximiliano de Almeida ~5 mil hab.) TODO o quadro
/// — inclusive efetivos — recolhe ao RGPS/INSS. IPE-Prev e RPPS ESTADUAL (servidores do Estado do RS), NAO
/// cobre municipios; logo nao e opcao de vinculo do municipio.
/// </para>
/// </summary>
/// <param name="PossuiRppsProprio">
/// Verdadeiro se o ente possui RPPS proprio instituido por lei municipal. // TODO(confirmar-dono): para
/// Maximiliano de Almeida, confirmar Sim/Nao (provavel Nao -> SomenteRgps).
/// </param>
public readonly record struct PoliticaPrevidenciaria(bool PossuiRppsProprio)
{
    /// <summary>Politica de municipio SEM RPPS proprio: todo o quadro recolhe ao RGPS/INSS (default).</summary>
    public static PoliticaPrevidenciaria SomenteRgps => new(PossuiRppsProprio: false);

    /// <summary>Politica de municipio COM RPPS proprio: efetivo → RPPS; comissionado/temporario → RGPS.</summary>
    public static PoliticaPrevidenciaria ComRppsProprio => new(PossuiRppsProprio: true);

    /// <summary>Resolve o regime previdenciario aplicavel a um tipo de cargo segundo a politica do ente.</summary>
    /// <param name="tipo">Tipo (natureza) do cargo.</param>
    /// <returns>RPPS apenas para cargo efetivo de ente com RPPS proprio; RGPS nos demais casos.</returns>
    public RegimePrevidenciario RegimeDe(TipoCargo tipo)
        => PossuiRppsProprio && tipo == TipoCargo.Efetivo
            ? RegimePrevidenciario.Rpps
            : RegimePrevidenciario.Rgps;
}
