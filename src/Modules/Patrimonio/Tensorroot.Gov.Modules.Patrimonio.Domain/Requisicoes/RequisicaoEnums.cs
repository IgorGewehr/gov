namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

/// <summary>
/// Situação (estado) de um pedido de requisição de almoxarifado na máquina de estados
/// do fluxo self-service: Solicitado → Aprovado → Atendido (terminal de sucesso) ou Cancelado (terminal).
/// </summary>
public enum SituacaoPedido
{
    /// <summary>Pedido aberto pelo setor solicitante; pendente de aprovação.</summary>
    Solicitado = 1,

    /// <summary>Pedido autorizado (RBAC do setor/almoxarifado); pendente de atendimento.</summary>
    Aprovado = 2,

    /// <summary>Atendido — total ou parcialmente (gerou saída de estoque por item). Terminal.</summary>
    Atendido = 3,

    /// <summary>Cancelado (terminal) — sem efeito de estoque.</summary>
    Cancelado = 4,
}
