namespace Tensorroot.Gov.Modules.Financas.Domain.Exceptions;

/// <summary>Exceção base para violações de invariante de saldo no ciclo da despesa.</summary>
public abstract class SaldoInsuficienteException : InvalidOperationException
{
    /// <summary>Inicializa com a mensagem informada.</summary>
    /// <param name="message">Mensagem de erro.</param>
    protected SaldoInsuficienteException(string message) : base(message)
    {
    }
}

/// <summary>
/// Lançada ao tentar empenhar acima do saldo disponível da dotação orçamentária
/// (Lei 4.320/64, art. 60: vedado empenho sem crédito).
/// </summary>
public sealed class SaldoOrcamentarioInsuficienteException : SaldoInsuficienteException
{
    /// <summary>Cria a exceção com os valores envolvidos.</summary>
    /// <param name="saldoDisponivel">Saldo disponível da dotação.</param>
    /// <param name="valorSolicitado">Valor que se tentou empenhar.</param>
    public SaldoOrcamentarioInsuficienteException(decimal saldoDisponivel, decimal valorSolicitado)
        : base($"Saldo orcamentario insuficiente: disponivel {saldoDisponivel:0.00}, solicitado {valorSolicitado:0.00}.")
    {
    }
}

/// <summary>Lançada ao tentar liquidar/anular acima do saldo do empenho.</summary>
public sealed class SaldoEmpenhoInsuficienteException : SaldoInsuficienteException
{
    /// <summary>Cria a exceção com os valores envolvidos.</summary>
    /// <param name="saldoDisponivel">Saldo a liquidar do empenho.</param>
    /// <param name="valorSolicitado">Valor solicitado.</param>
    public SaldoEmpenhoInsuficienteException(decimal saldoDisponivel, decimal valorSolicitado)
        : base($"Saldo do empenho insuficiente: disponivel {saldoDisponivel:0.00}, solicitado {valorSolicitado:0.00}.")
    {
    }
}

/// <summary>Lançada ao tentar pagar acima do saldo liquidado (Lei 4.320/64, art. 62).</summary>
public sealed class SaldoLiquidacaoInsuficienteException : SaldoInsuficienteException
{
    /// <summary>Cria a exceção com os valores envolvidos.</summary>
    /// <param name="saldoDisponivel">Saldo a pagar da liquidação.</param>
    /// <param name="valorSolicitado">Valor solicitado.</param>
    public SaldoLiquidacaoInsuficienteException(decimal saldoDisponivel, decimal valorSolicitado)
        : base($"Saldo da liquidacao insuficiente: disponivel {saldoDisponivel:0.00}, solicitado {valorSolicitado:0.00}.")
    {
    }
}
