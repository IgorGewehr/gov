using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

/// <summary>
/// Calculadora PURA do reajuste salarial (revisao geral anual — CF art. 37, X): aplica um percentual
/// linear sobre um vencimento-base e devolve o novo valor (BRL, 2 casas). Centraliza a aritmetica do
/// reajuste para reuso pelo motor de aplicacao em lote, mantendo o agregado <see cref="Cargo"/> limpo.
/// </summary>
public static class ReajusteVencimento
{
    /// <summary>Percentual minimo aceito para um reajuste (acima de zero — reajuste sempre acresce).</summary>
    public const decimal PercentualMinimo = 0m;

    /// <summary>
    /// Percentual maximo aceito num unico reajuste (guarda de sanidade contra erro de digitacao;
    /// reajustes acima disso devem ser fracionados/justificados manualmente).
    /// </summary>
    public const decimal PercentualMaximo = 100m;

    /// <summary>
    /// Aplica um percentual de reajuste a um vencimento, devolvendo o novo vencimento arredondado a 2 casas.
    /// </summary>
    /// <param name="vencimentoAtual">Vencimento-base atual.</param>
    /// <param name="percentual">Percentual de reajuste (ex.: 5.5 = +5,5%); deve estar em (0, 100].</param>
    /// <returns>Novo <see cref="Vencimento"/> reajustado.</returns>
    /// <exception cref="ArgumentNullException">Se o vencimento atual for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o percentual estiver fora da faixa permitida.</exception>
    public static Vencimento Aplicar(Vencimento vencimentoAtual, decimal percentual)
    {
        ArgumentNullException.ThrowIfNull(vencimentoAtual);
        if (percentual <= PercentualMinimo || percentual > PercentualMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percentual),
                percentual,
                $"Percentual de reajuste deve estar em ({PercentualMinimo}, {PercentualMaximo}].");
        }

        var fator = 1m + (percentual / 100m);
        var novoValor = decimal.Round(vencimentoAtual.Valor * fator, 2, MidpointRounding.AwayFromZero);
        return Vencimento.De(novoValor);
    }
}
