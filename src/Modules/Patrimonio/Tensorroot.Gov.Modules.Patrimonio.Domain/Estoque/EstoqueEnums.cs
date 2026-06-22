namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

/// <summary>Tipo de movimento de estoque que altera o saldo do item.</summary>
public enum TipoMovimento
{
    /// <summary>Ingresso de itens (compra, doação, devolução).</summary>
    Entrada = 1,

    /// <summary>Consumo/baixa por requisição.</summary>
    Saida = 2,
}

/// <summary>Método de custeio (valoração da saída) do item de estoque.</summary>
public enum MetodoCusteio
{
    /// <summary>Primeiro a entrar, primeiro a sair (FIFO).</summary>
    Peps = 1,

    /// <summary>Custo médio ponderado.</summary>
    Medio = 2,
}

/// <summary>Classificação do item na Curva ABC (relevância de valor/giro).</summary>
public enum CurvaABC
{
    /// <summary>Alta relevância de valor/giro.</summary>
    A = 1,

    /// <summary>Relevância média.</summary>
    B = 2,

    /// <summary>Baixa relevância.</summary>
    C = 3,
}

/// <summary>Situação (estado) do item no almoxarifado.</summary>
public enum SituacaoItemEstoque
{
    /// <summary>Item ativo no almoxarifado (movimentável).</summary>
    Ativo = 1,

    /// <summary>Item inativado — terminal para movimentação (não admite entrada/saída).</summary>
    Inativo = 2,
}
