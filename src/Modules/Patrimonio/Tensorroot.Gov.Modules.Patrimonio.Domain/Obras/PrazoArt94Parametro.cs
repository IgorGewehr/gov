using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>
/// Tríade parametrizável por tenant do prazo do art. 94 §3 (Lei 14.133/2021) aplicável a obras:
/// <b>quantidade</b>, <b>unidade</b> (úteis|corridos) e <b>norma-fonte</b> citável. Reúne o que o VO
/// <see cref="PrazoLegal"/> precisa para resolver o vencimento sem que o número/lei (25/45 d.u.) apareça
/// solto no agregado (CLAUDE.md §7/§16). Espelha <c>PrazoPncpParametro</c> de Administração (W9.1) — o
/// número e os feriados vêm dos parâmetros do tenant; o agregado apenas calcula via calendário transversal.
/// </summary>
/// <param name="Quantidade">Quantidade de dias (&gt;= 0) — ex.: 25 (assinatura) ou 45 (conclusão).</param>
/// <param name="Unidade">Unidade de contagem (úteis|corridos).</param>
/// <param name="NormaFonte">Citação legal (ex.: "Lei 14.133/2021 art. 94 §3").</param>
public sealed record PrazoArt94Parametro(int Quantidade, UnidadePrazo Unidade, string NormaFonte);
