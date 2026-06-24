namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Parametros de feriados POR TENANT lidos da configuracao (secao <see cref="SecaoConfiguracao"/>).
/// Os feriados nacionais FIXOS e MOVEIS sao derivados por lei/algoritmo (nao configuraveis); aqui ficam
/// apenas as escolhas locais do ente: adocao de pontos facultativos e feriados municipais. Defaults
/// documentais espelham o piso legal; valores reais vem da config do tenant (CLAUDE.md S7/S16).
/// Evolucao natural (sem mudar o contrato): tabela <c>core.CalendarioFeriado</c> por tenant.
/// </summary>
public sealed class OpcoesFeriadosTenant
{
    /// <summary>Secao de configuracao por tenant.</summary>
    public const string SecaoConfiguracao = "Tempo:Feriados";

    /// <summary>Se o tenant adota o Carnaval (segunda e terca) como dia nao util. Default: <c>false</c>.</summary>
    public bool AdotaCarnaval { get; init; }

    /// <summary>Se o tenant adota a Quarta-feira de Cinzas como dia nao util. Default: <c>false</c>.</summary>
    public bool AdotaQuartaCinzas { get; init; }

    /// <summary>Se o tenant adota Corpus Christi como dia nao util. Default: <c>false</c>.</summary>
    public bool AdotaCorpusChristi { get; init; }

    /// <summary>
    /// Feriados municipais RECORRENTES em formato "MM-dd" (ex.: aniversario do municipio, padroeiro).
    /// Aplicados todo ano.
    /// </summary>
    public IReadOnlyList<string> MunicipaisFixos { get; init; } = [];

    /// <summary>
    /// Feriados/pontos municipais PONTUAIS em formato "yyyy-MM-dd" (datas decretadas para um ano especifico).
    /// </summary>
    public IReadOnlyList<string> MunicipaisData { get; init; } = [];
}
