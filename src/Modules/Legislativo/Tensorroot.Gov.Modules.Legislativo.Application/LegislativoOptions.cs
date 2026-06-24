namespace Tensorroot.Gov.Modules.Legislativo.Application;

/// <summary>
/// Parametros do processo legislativo configuraveis por tenant (Regimento Interno de cada Camara).
/// A CF/88 (art. 29, caput) e a LOM exigem DOIS turnos com intervalo para a Emenda a LOM, mas o
/// numero exato de dias do intersticio cabe ao Regimento — por isso e parametrizado, nunca um numero
/// magico no dominio (CLAUDE.md §7).
/// </summary>
public sealed class LegislativoOptions
{
    /// <summary>Secao de configuracao raiz do modulo Legislativo.</summary>
    public const string SecaoConfiguracao = "Legislativo";

    /// <summary>
    /// Intervalo minimo (em dias) entre o primeiro e o segundo turno de votacao de materia de rito
    /// qualificado (Emenda a LOM). Conforme o Regimento Interno do tenant. Nao negativo.
    /// </summary>
    public int IntersticioEntreTurnosDias { get; set; } = IntersticioPadraoDias;

    /// <summary>
    /// Intersticio padrao (1 dia) aplicado quando o tenant nao parametriza o seu Regimento — garante,
    /// no minimo, que os dois turnos NAO ocorram no mesmo dia (datas/sessoes distintas).
    /// </summary>
    public const int IntersticioPadraoDias = 1;

    /// <summary>
    /// Identificacao do ente para a URN/XML LexML-BR (W9.5). Cada Camara informa a sua jurisdicao
    /// (UF + municipio) e a autoridade emanadora; sem isso a esfera <c>br;[uf];[municipio]</c> nao pode
    /// ser composta. Parametro por tenant — nunca hardcoded.
    /// </summary>
    public LexmlOptions Lexml { get; set; } = new();

    /// <summary>
    /// Parametros do limite de despesa da Camara — art. 29-A da CF/88 (W9.5). Faixas populacionais x
    /// percentual, subteto da folha sobre o repasse (§1), limiar de atencao do semaforo e exercicio-corte
    /// da EC 109/2021. Todos parametrizaveis por tenant (norma-fonte), com defaults legais usuais.
    /// </summary>
    public LimiteCamaraOptions LimiteCamara { get; set; } = new();
}

/// <summary>Configuracao da identificacao do ente para a URN/XML LexML-BR (por tenant).</summary>
public sealed class LexmlOptions
{
    /// <summary>Sigla da UF da Camara (ex.: <c>RS</c>). Normalizada para a grafia LexML.</summary>
    public string Uf { get; set; } = string.Empty;

    /// <summary>Nome do municipio (ex.: <c>Maximiliano de Almeida</c>). Normalizado para a grafia LexML.</summary>
    public string Municipio { get; set; } = string.Empty;

    /// <summary>
    /// Autoridade emanadora padrao das normas (ex.: <c>camara.municipal</c>). Usada quando a norma nao
    /// especifica origem diversa.
    /// </summary>
    public string Autoridade { get; set; } = AutoridadePadrao;

    /// <summary>Autoridade padrao (Camara Municipal) quando o tenant nao parametriza.</summary>
    public const string AutoridadePadrao = "camara.municipal";
}

/// <summary>
/// Configuracao parametrizavel do art. 29-A (por tenant). Os defaults refletem os percentuais usuais
/// pos-EC 58/2009, mas sao SOBRESCRITIVEIS via configuracao (norma-fonte), atendendo ao CLAUDE.md §7.
/// </summary>
public sealed class LimiteCamaraOptions
{
    /// <summary>
    /// Faixas populacionais (limite superior INCLUSIVE de habitantes x percentual em fracao). A ultima
    /// faixa deve ser aberta superiormente (use <see cref="int.MaxValue"/>). Defaults: ate 100.000 = 7%;
    /// 300.000 = 6%; 500.000 = 5%; 3.000.000 = 4,5%; 8.000.000 = 4%; acima = 3,5%.
    /// </summary>
    public IList<FaixaPopulacionalOptions> Faixas { get; set; } =
    [
        new() { PopulacaoMaximaInclusive = 100_000, Percentual = 0.07m },
        new() { PopulacaoMaximaInclusive = 300_000, Percentual = 0.06m },
        new() { PopulacaoMaximaInclusive = 500_000, Percentual = 0.05m },
        new() { PopulacaoMaximaInclusive = 3_000_000, Percentual = 0.045m },
        new() { PopulacaoMaximaInclusive = 8_000_000, Percentual = 0.04m },
        new() { PopulacaoMaximaInclusive = int.MaxValue, Percentual = 0.035m },
    ];

    /// <summary>Subteto da folha da Camara como fracao do repasse (§1: max. 70% = 0,70).</summary>
    public decimal SubtetoFolhaSobreRepasse { get; set; } = 0.70m;

    /// <summary>Fracao de utilizacao do teto a partir da qual o semaforo passa a ATENCAO (default 95%).</summary>
    public decimal LimiarAtencao { get; set; } = 0.95m;

    /// <summary>Exercicio-corte da EC 109/2021 (art. 7º): inativos/pensionistas no teto a partir de 2025.</summary>
    public int ExercicioCorteInativos { get; set; } = 2025;
}

/// <summary>Uma faixa populacional configuravel do art. 29-A (limite de habitantes x percentual).</summary>
public sealed class FaixaPopulacionalOptions
{
    /// <summary>Limite superior INCLUSIVE de habitantes da faixa (<see cref="int.MaxValue"/> = sem teto).</summary>
    public int PopulacaoMaximaInclusive { get; set; }

    /// <summary>Percentual-limite da receita (fracao em (0, 1]; ex.: 0,07 = 7%).</summary>
    public decimal Percentual { get; set; }
}
