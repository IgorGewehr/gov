namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>
/// Feriados/pontos MOVEIS derivados da Pascoa (Computus de Gauss/Meeus — "anonymous Gregorian
/// algorithm"). Puro, sem relogio, SEM hardcode de ano: tudo deriva do <c>int ano</c>. A Pascoa fixa
/// Carnaval (-47 d), Sexta-feira Santa (-2 d) e Corpus Christi (+60 d). Tabela puro-dominio do
/// SharedKernel, reusavel pelo calendario de qualquer tenant (CLAUDE.md S7/S16).
/// </summary>
public static class FeriadosMoveis
{
    /// <summary>
    /// Domingo de Pascoa do <paramref name="ano"/> (algoritmo de Meeus/Jones/Butcher — gregoriano).
    /// Validado contra tabela conhecida: 2024-03-31, 2025-04-20, 2026-04-05, 2027-03-28.
    /// </summary>
    /// <param name="ano">Ano (gregoriano) a calcular.</param>
    /// <returns>Data do Domingo de Pascoa.</returns>
    public static DateOnly Pascoa(int ano)
    {
        int a = ano % 19,
            b = ano / 100,
            c = ano % 100,
            d = b / 4,
            e = b % 4,
            f = (b + 8) / 25,
            g = (b - f + 1) / 3,
            h = ((19 * a) + b - d - g + 15) % 30,
            i = c / 4,
            k = c % 4,
            l = (32 + (2 * e) + (2 * i) - h - k) % 7,
            m = (a + (11 * h) + (22 * l)) / 451,
            mes = (h + l - (7 * m) + 114) / 31,
            dia = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(ano, mes, dia);
    }

    /// <summary>Sexta-feira Santa (Pascoa - 2 d) — feriado religioso consolidado.</summary>
    /// <param name="ano">Ano a calcular.</param>
    /// <returns>Data da Sexta-feira Santa.</returns>
    public static DateOnly SextaFeiraSanta(int ano) => Pascoa(ano).AddDays(-2);

    /// <summary>Segunda-feira de Carnaval (Pascoa - 48 d) — ponto facultativo.</summary>
    /// <param name="ano">Ano a calcular.</param>
    /// <returns>Data da segunda de Carnaval.</returns>
    public static DateOnly SegundaCarnaval(int ano) => Pascoa(ano).AddDays(-48);

    /// <summary>Terca-feira de Carnaval (Pascoa - 47 d) — ponto facultativo.</summary>
    /// <param name="ano">Ano a calcular.</param>
    /// <returns>Data da terca de Carnaval.</returns>
    public static DateOnly TercaCarnaval(int ano) => Pascoa(ano).AddDays(-47);

    /// <summary>Quarta-feira de Cinzas (Pascoa - 46 d) — meio-expediente em alguns entes.</summary>
    /// <param name="ano">Ano a calcular.</param>
    /// <returns>Data da Quarta-feira de Cinzas.</returns>
    public static DateOnly QuartaCinzas(int ano) => Pascoa(ano).AddDays(-46);

    /// <summary>Corpus Christi (Pascoa + 60 d) — ponto facultativo (feriado em muitos municipios).</summary>
    /// <param name="ano">Ano a calcular.</param>
    /// <returns>Data de Corpus Christi.</returns>
    public static DateOnly CorpusChristi(int ano) => Pascoa(ano).AddDays(60);
}
