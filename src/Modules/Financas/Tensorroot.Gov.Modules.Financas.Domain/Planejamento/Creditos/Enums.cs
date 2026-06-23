namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;

/// <summary>Espécie de crédito adicional (Lei 4.320/64, art. 41).</summary>
public enum EspecieCredito
{
    /// <summary>Suplementar — reforço de dotação existente (autorizado na própria LOA).</summary>
    Suplementar = 1,

    /// <summary>Especial — destinado a despesa sem dotação específica (lei específica).</summary>
    Especial = 2,

    /// <summary>Extraordinário — despesa urgente/imprevista (calamidade, guerra — CF 167 §3º).</summary>
    Extraordinario = 3,
}

/// <summary>Fonte de recurso do crédito adicional (Lei 4.320/64, art. 43 §1º).</summary>
public enum FonteRecursoCredito
{
    /// <summary>Superávit financeiro apurado em balanço patrimonial do exercício anterior.</summary>
    SuperavitFinanceiro = 1,

    /// <summary>Excesso de arrecadação.</summary>
    ExcessoArrecadacao = 2,

    /// <summary>Anulação parcial/total de dotações.</summary>
    AnulacaoDotacao = 3,

    /// <summary>Produto de operações de crédito.</summary>
    OperacaoCredito = 4,

    /// <summary>Dispensada (crédito extraordinário — art. 44).</summary>
    Dispensada = 5,
}

/// <summary>Situação do crédito adicional.</summary>
public enum SituacaoCredito
{
    /// <summary>Registrado (ainda não aplicado às dotações).</summary>
    Registrado = 1,

    /// <summary>Aberto — efeito aplicado às dotações (reforço/anulação/criação).</summary>
    Aberto = 2,
}
