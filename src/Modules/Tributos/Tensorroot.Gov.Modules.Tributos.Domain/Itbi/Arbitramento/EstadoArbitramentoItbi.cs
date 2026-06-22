namespace Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;

/// <summary>
/// Estado da máquina do processo de arbitramento da base de cálculo do ITBI (CTN art. 148).
/// O contraditório é OBRIGATÓRIO — não se pula <see cref="AguardandoContraditorio"/> (Tema 1.113/STJ).
/// Apenas <see cref="Concluido"/> habilita a elevação da base (lançamento de ofício).
/// </summary>
public enum EstadoArbitramentoItbi
{
    /// <summary>Processo aberto com motivo individualizado (não basta "menor que pauta").</summary>
    Instaurado = 1,

    /// <summary>Contribuinte notificado; prazo para defesa/avaliação contraditória (art. 148, parte final).</summary>
    AguardandoContraditorio = 2,

    /// <summary>Fisco avalia a defesa; o ônus da prova é do fisco.</summary>
    EmAnalise = 3,

    /// <summary>Decisão final fundamentada; gera o <see cref="ResultadoArbitramento"/> (terminal).</summary>
    Concluido = 4,

    /// <summary>Encerrado sem arbitrar — a declaração prevaleceu (terminal).</summary>
    Cancelado = 5,
}
