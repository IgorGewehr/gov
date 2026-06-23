namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

/// <summary>
/// Semáforo de um indicador de gestão frente ao seu limite legal. A COR é status (não decoração):
/// dirige o alerta exibido ao gestor. Genérico — serve tanto para limites de teto (LRF: quanto maior,
/// pior) quanto para mínimos (Saúde/Educação: quanto menor, pior), interpretados pelo apurador.
/// </summary>
public enum SituacaoLimite
{
    /// <summary>Indeterminado: faltam dados para apurar (ex.: RCL ainda não publicada).</summary>
    Indeterminado = 0,

    /// <summary>Verde: dentro de margem confortável.</summary>
    Adequado = 1,

    /// <summary>Amarelo: faixa de alerta/prudencial — exige atenção da gestão.</summary>
    Alerta = 2,

    /// <summary>Vermelho: limite legal atingido/excedido — consequências legais.</summary>
    Excedido = 3,
}
