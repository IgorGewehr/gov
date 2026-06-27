namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

/// <summary>
/// Natureza da movimentacao funcional na carreira (plano de cargos e salarios). A progressao e' o
/// avanco HORIZONTAL (mudanca de referencia/padrao dentro da mesma classe, por tempo de servico e/ou
/// avaliacao de desempenho); a promocao e' o avanco VERTICAL (mudanca de classe, por requisitos de
/// titulacao/antiguidade); o enquadramento e' o posicionamento INICIAL do servidor na matriz.
/// </summary>
public enum TipoMovimentacaoCarreira
{
    /// <summary>Posicionamento inicial do servidor na matriz salarial (classe + referencia de ingresso).</summary>
    Enquadramento = 1,

    /// <summary>Avanco horizontal: muda a referencia (padrao) dentro da mesma classe (tempo/avaliacao).</summary>
    ProgressaoHorizontal = 2,

    /// <summary>Avanco vertical: muda a classe (promocao por titulacao/antiguidade).</summary>
    PromocaoVertical = 3,
}

/// <summary>Criterio que fundamenta uma progressao horizontal na carreira.</summary>
public enum CriterioProgressao
{
    /// <summary>Progressao por antiguidade/tempo de efetivo exercicio (interstncio cumprido).</summary>
    TempoDeServico = 1,

    /// <summary>Progressao por merecimento/avaliacao de desempenho (nota minima atingida).</summary>
    AvaliacaoDesempenho = 2,

    /// <summary>Progressao combinada (tempo de servico + avaliacao de desempenho).</summary>
    TempoEAvaliacao = 3,
}

/// <summary>Situacao (estado) de um plano de carreira (PCCS) no tenant.</summary>
public enum SituacaoPlanoCarreira
{
    /// <summary>Plano vigente, aceita enquadramento e movimentacoes (estado inicial).</summary>
    Ativo = 1,

    /// <summary>Plano revogado por lei (estado terminal); nao admite novas movimentacoes.</summary>
    Revogado = 2,
}
