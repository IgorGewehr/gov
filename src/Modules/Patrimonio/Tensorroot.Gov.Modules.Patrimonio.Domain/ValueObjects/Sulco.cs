using System.Globalization;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Profundidade do sulco da banda de rodagem de um pneu, em milímetros (mm). É a medida de desgaste
/// do pneu: decresce com o uso e baliza a vida útil (CONTRAN/CTB exige sulco mínimo legal de 1,6 mm
/// para circulação — o piso é parametrizável por tenant, não fixado neste VO). Grandeza não-negativa
/// e monotônica não crescente (o sulco só diminui ao longo do tempo, salvo recapagem que o restaura).
/// </summary>
/// <param name="Milimetros">Profundidade do sulco (mm), não-negativa.</param>
public readonly record struct Sulco(decimal Milimetros)
{
    /// <summary>Cria uma medida de sulco não-negativa, arredondada a 1 casa decimal (precisão de calibre).</summary>
    /// <param name="milimetros">Profundidade em mm (maior ou igual a zero).</param>
    /// <returns>Instância de <see cref="Sulco"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static Sulco De(decimal milimetros)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(milimetros);
        return new Sulco(decimal.Round(milimetros, 1, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Reduz o sulco para uma nova medição de desgaste, garantindo a monotonicidade não crescente:
    /// a nova medida não pode exceder a atual (o pneu não "ganha" sulco em rodagem; só a recapagem
    /// — operação própria — o restaura). Igual é permitido (sem desgaste medido entre aferições).
    /// </summary>
    /// <param name="novaMedida">Nova profundidade aferida (mm).</param>
    /// <returns>Novo <see cref="Sulco"/> com a medida informada.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se a nova medida exceder a atual (sulco aumentando em rodagem).</exception>
    public Sulco Desgastar(decimal novaMedida)
    {
        var medida = De(novaMedida);
        if (medida.Milimetros > Milimetros)
        {
            throw new InvalidOperationException(
                $"Sulco não pode aumentar em rodagem: atual {Milimetros} mm, informado {medida.Milimetros} mm (use recapagem).");
        }

        return medida;
    }

    /// <summary>Indica se o sulco está no nível mínimo legal informado ou abaixo dele (CTB/CONTRAN).</summary>
    /// <param name="minimoLegalMilimetros">Sulco mínimo legal vigente, em mm (parametrizável por tenant).</param>
    /// <returns><c>true</c> se o sulco atual for menor ou igual ao mínimo legal.</returns>
    public bool AtingiuMinimoLegal(decimal minimoLegalMilimetros) => Milimetros <= minimoLegalMilimetros;

    /// <inheritdoc />
    public override string ToString() => $"{Milimetros.ToString("0.0", CultureInfo.InvariantCulture)} mm";
}
