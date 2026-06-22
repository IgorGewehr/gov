using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;

/// <summary>
/// Resultado (objeto de valor) de um processo de arbitramento CONCLUÍDO (CTN art. 148). É a ÚNICA
/// porta de entrada para elevar a base de cálculo do ITBI no motor (ver
/// <see cref="Calculo.CalculadoraItbi.RecalcularComArbitramento"/>). Produzido apenas por um
/// <see cref="ProcessoArbitramentoItbi"/> no estado <see cref="EstadoArbitramentoItbi.Concluido"/>.
/// </summary>
/// <param name="ProcessoArbitramentoId">Identificador do processo de arbitramento de origem.</param>
/// <param name="ValorArbitrado">Valor arbitrado pela decisão final (base de cálculo de ofício).</param>
/// <param name="ProcessoConcluido">
/// Verdadeiro SOMENTE quando o processo está em <see cref="EstadoArbitramentoItbi.Concluido"/>.
/// O motor recusa o recálculo se for falso (garante "só após processo").
/// </param>
public sealed record ResultadoArbitramento(
    Guid ProcessoArbitramentoId,
    ValorMonetario ValorArbitrado,
    bool ProcessoConcluido);
