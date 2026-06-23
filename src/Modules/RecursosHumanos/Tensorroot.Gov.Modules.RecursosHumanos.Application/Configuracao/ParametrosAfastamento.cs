namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

/// <summary>
/// Parametros LEGAIS de afastamento parametrizaveis por tenant (nunca <em>hardcoded</em> — CLAUDE.md S7/S16).
/// Servem de DEFAULT quando o tenant nao cadastrou uma <c>RegraAfastamento</c> persistida para o tipo: o
/// provider materializa a regra a partir destes valores. Os defaults documentais espelham a legislacao
/// federal; o ente sobrescreve por lei municipal (ex.: licenca-premio, adesao ao Empresa Cidada).
/// </summary>
public sealed class ParametrosAfastamento
{
    /// <summary>Secao de configuracao.</summary>
    public const string SecaoConfiguracao = "RecursosHumanos:Afastamentos";

    /// <summary>Dias da licenca-maternidade. Padrao 120 (CF art. 7 XVIII); 180 se <see cref="AderiuEmpresaCidada"/>.</summary>
    public int DiasLicencaMaternidade { get; init; } = 120;

    /// <summary>Se o ente aderiu ao Programa Empresa Cidada/lei municipal (estende a maternidade para 180d).</summary>
    public bool AderiuEmpresaCidada { get; init; }

    /// <summary>Dias da licenca-maternidade quando ha adesao ao Empresa Cidada. Padrao 180.</summary>
    public int DiasLicencaMaternidadeAmpliada { get; init; } = 180;

    /// <summary>Dias da licenca-paternidade. Padrao 5 (CF/ADCT); 20 em programa de adesao.</summary>
    public int DiasLicencaPaternidade { get; init; } = 5;

    /// <summary>Dias da licenca-paternidade ampliada (programa de adesao). Padrao 20.</summary>
    public int DiasLicencaPaternidadeAmpliada { get; init; } = 20;

    /// <summary>Dias iniciais pagos pelo ENTE no afastamento por doenca/acidente (RGPS). Padrao 15.</summary>
    public int DiasPagosPeloEnteDoenca { get; init; } = 15;

    /// <summary>Dias padrao da licenca-premio (se prevista em lei municipal). Padrao 90 (3 meses por quinquenio).</summary>
    public int DiasLicencaPremio { get; init; } = 90;

    /// <summary>Dias maximos da licenca para tratar de interesse particular (sem vencimento). Padrao 730 (2 anos).</summary>
    public int DiasMaximosLicencaSemVencimento { get; init; } = 730;

    /// <summary>
    /// No RPPS, manter o provento do ente no afastamento por doenca alem dos 15 dias (lei propria do
    /// ente pode prever a manutencao integral pelo RPPS em vez de beneficio do INSS). Padrao <c>false</c>
    /// (segue o RGPS: suspende e remete ao INSS). Parametrizavel por tenant.
    /// </summary>
    public bool RppsMantemDoencaAlem15Dias { get; init; }
}
