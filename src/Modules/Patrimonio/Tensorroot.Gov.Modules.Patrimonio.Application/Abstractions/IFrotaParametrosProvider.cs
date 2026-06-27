namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>
/// Fonte dos parâmetros de gestão de frota configuráveis POR TENANT (sem número mágico no código —
/// CLAUDE.md §7/§16): sulco mínimo legal de pneu (CONTRAN/CTB) e janelas de alerta de vencimento de CNH
/// e de apólice de seguro. A porta vive na Application; a fonte (config hoje, tabela depois) fica na
/// Infrastructure (espelha <c>IParametrosObraProvider</c>).
/// </summary>
public interface IFrotaParametrosProvider
{
    /// <summary>Sulco mínimo legal de circulação, em milímetros (default 1,6 mm — CONTRAN/CTB).</summary>
    /// <returns>Sulco mínimo legal vigente (mm).</returns>
    decimal SulcoMinimoLegalMilimetros();

    /// <summary>Janela padrão (dias) do alerta de CNH a vencer.</summary>
    /// <returns>Quantidade de dias da janela de alerta de CNH.</returns>
    int AlertaCnhVencendoDias();

    /// <summary>Janela padrão (dias) do alerta de apólice de seguro a vencer.</summary>
    /// <returns>Quantidade de dias da janela de alerta de apólice.</returns>
    int AlertaApoliceVencendoDias();
}
