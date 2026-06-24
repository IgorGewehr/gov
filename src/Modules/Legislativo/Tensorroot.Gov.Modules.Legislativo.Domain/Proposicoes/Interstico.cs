namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>
/// Intersticio (intervalo minimo, em dias) entre o primeiro e o segundo turno de votacao de uma
/// materia de rito qualificado (Emenda a LOM). A CF/88 (art. 29, caput) e a Lei Organica Municipal
/// exigem dois turnos com intervalo, mas o numero de dias e fixado pelo Regimento Interno de cada
/// Camara — por isso e PARAMETRIZAVEL por tenant, jamais um numero magico embutido no dominio.
/// O dominio impoe apenas que o intervalo nao seja negativo e que, quando exigido pelo tipo, seja
/// efetivamente observado entre os turnos.
/// </summary>
public readonly record struct Interstico
{
    private Interstico(int dias) => Dias = dias;

    /// <summary>Quantidade minima de dias exigida entre os dois turnos.</summary>
    public int Dias { get; }

    /// <summary>Cria um intersticio validado (numero de dias nao negativo).</summary>
    /// <param name="dias">Quantidade minima de dias entre turnos, conforme o Regimento do tenant.</param>
    /// <returns>Instancia de <see cref="Interstico"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="dias"/> for negativo.</exception>
    public static Interstico DeDias(int dias)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dias);
        return new Interstico(dias);
    }

    /// <summary>Indica se a <paramref name="dataTurnoSeguinte"/> respeita o intersticio a partir da <paramref name="dataTurnoAnterior"/>.</summary>
    /// <param name="dataTurnoAnterior">Data do turno ja aprovado.</param>
    /// <param name="dataTurnoSeguinte">Data pretendida para o turno seguinte.</param>
    /// <returns><c>true</c> se houver pelo menos <see cref="Dias"/> dias de intervalo.</returns>
    public bool Respeitado(DateOnly dataTurnoAnterior, DateOnly dataTurnoSeguinte)
        => dataTurnoSeguinte.DayNumber - dataTurnoAnterior.DayNumber >= Dias;
}
