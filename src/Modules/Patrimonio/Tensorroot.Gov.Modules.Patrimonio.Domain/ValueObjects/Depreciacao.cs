namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Parâmetros de depreciação linear de um bem (MCASP / NBC TSP 07): valor depreciável
/// (custo − valor residual) distribuído pela vida útil em parcelas mensais iguais.
/// </summary>
public readonly record struct Depreciacao
{
    private Depreciacao(decimal valorDepreciavel, int vidaUtilMeses)
    {
        ValorDepreciavel = valorDepreciavel;
        VidaUtilMeses = vidaUtilMeses;
    }

    /// <summary>Valor depreciável (custo − valor residual), não-negativo.</summary>
    public decimal ValorDepreciavel { get; }

    /// <summary>Vida útil, em meses (maior que zero).</summary>
    public int VidaUtilMeses { get; }

    /// <summary>Parcela mensal linear (valor depreciável dividido pela vida útil).</summary>
    public decimal ParcelaMensal => decimal.Round(ValorDepreciavel / VidaUtilMeses, 2, MidpointRounding.AwayFromZero);

    /// <summary>Cria os parâmetros de depreciação linear.</summary>
    /// <param name="valorInicial">Custo de ingresso (valor inicial).</param>
    /// <param name="valorResidual">Resíduo estimado ao fim da vida útil.</param>
    /// <param name="vidaUtilMeses">Vida útil em meses (maior que zero).</param>
    /// <returns>Instância de <see cref="Depreciacao"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a vida útil não for positiva ou o residual exceder o inicial.</exception>
    public static Depreciacao De(decimal valorInicial, decimal valorResidual, int vidaUtilMeses)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaUtilMeses);
        ArgumentOutOfRangeException.ThrowIfNegative(valorInicial);
        ArgumentOutOfRangeException.ThrowIfNegative(valorResidual);
        if (valorResidual > valorInicial)
        {
            throw new ArgumentOutOfRangeException(nameof(valorResidual), "Valor residual não pode exceder o valor inicial.");
        }

        return new Depreciacao(valorInicial - valorResidual, vidaUtilMeses);
    }
}
