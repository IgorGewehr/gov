namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>
/// <b>E-3 — Origem do recurso recebido do FUNDEB</b> (Lei 14.113/2020; EC 108/2020). O FUNDEB é um fundo
/// de natureza contábil-redistributiva: o município recebe a sua <b>cota-parte</b> (rateio estadual pelo
/// nº de matrículas ponderadas — VAAF) e, quando o Estado não alcança o mínimo nacional, recebe
/// <b>complementação da União</b> em três modalidades. Modela-se a <b>origem</b> de cada parcela para que a
/// <see cref="DistribuicaoFundeb"/> <b>concilie</b> o recebido por origem — quem rateia/complementa é o ente
/// estadual/FNDE; o município <b>não recalcula a cota</b>, apenas comprova.
/// <para>
/// // TODO(validar-oficial): os fatores de ponderação CIF, os valores VAAF/VAAT/VAAR-MIN (Portaria
/// Interministerial MEC/MF) e a regra de saldo (até 10% no 1º trimestre seguinte — art. 25 da Lei
/// 14.113/2020) dependem do ato vigente. As origens abaixo são o eixo confirmado da Lei 14.113/2020.
/// </para>
/// </summary>
public enum OrigemRecursoFundeb
{
    /// <summary>Cota-parte estadual do FUNDEB (rateio pelas matrículas ponderadas — VAAF do ente).</summary>
    CotaParteEstadual = 1,

    /// <summary>Complementação da União VAAF (Valor Anual por Aluno — fundos abaixo do mínimo nacional).</summary>
    ComplementacaoVaaf = 2,

    /// <summary>Complementação da União VAAT (Valor Anual Total por Aluno — disponibilidade total por aluno).</summary>
    ComplementacaoVaat = 3,

    /// <summary>Complementação da União VAAR (Valor Anual por Aluno por Resultados — condicionada a metas).</summary>
    ComplementacaoVaar = 4,
}
