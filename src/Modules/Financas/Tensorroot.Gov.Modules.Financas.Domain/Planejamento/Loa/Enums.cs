namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>Situação (máquina de estados) da Lei Orçamentária Anual.</summary>
public enum SituacaoLoa
{
    /// <summary>Projeto de lei — editável (receita/despesa).</summary>
    ProjetoLei = 1,

    /// <summary>Em tramitação no Legislativo — congelado.</summary>
    EmTramitacao = 2,

    /// <summary>Aprovada (compatibilidade validada) — pronta para executar.</summary>
    Aprovada = 3,

    /// <summary>Em execução — dotações geradas; recebe créditos adicionais.</summary>
    EmExecucao = 4,

    /// <summary>Encerrada (fim do exercício).</summary>
    Encerrada = 5,
}
