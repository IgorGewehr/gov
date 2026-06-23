namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

/// <summary>Tipo de ação orçamentária (Portaria MTO/STN; CF 165 §1º).</summary>
public enum TipoAcao
{
    /// <summary>Projeto — expande/aperfeiçoa a ação de governo (tem termo).</summary>
    Projeto = 1,

    /// <summary>Atividade — operação contínua e permanente.</summary>
    Atividade = 2,

    /// <summary>Operação especial — não gera contraprestação direta (dívida, transferências).</summary>
    OperacaoEspecial = 3,
}

/// <summary>Situação (máquina de estados) do Plano Plurianual.</summary>
public enum SituacaoPpa
{
    /// <summary>Em elaboração — editável.</summary>
    Elaboracao = 1,

    /// <summary>Em tramitação no Legislativo — congelado.</summary>
    EmTramitacao = 2,

    /// <summary>Vigente (lei sancionada) — referenciável por LDO/LOA.</summary>
    Vigente = 3,

    /// <summary>Encerrado — quadriênio concluído.</summary>
    Encerrado = 4,

    /// <summary>Revisado — substituído por nova versão (lei de revisão).</summary>
    Revisado = 5,
}
