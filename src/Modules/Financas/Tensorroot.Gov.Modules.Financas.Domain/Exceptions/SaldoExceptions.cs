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

/// <summary>
/// Lançada ao tentar reter, sobre uma liquidação, mais do que o valor liquidado
/// (a soma das consignações não pode exceder o bruto — geraria líquido negativo).
/// </summary>
public sealed class RetencaoExcedeLiquidacaoException : InvalidOperationException
{
    /// <summary>Cria a exceção com os valores envolvidos.</summary>
    /// <param name="valorLiquidacao">Valor liquidado (bruto).</param>
    /// <param name="totalRetencoes">Soma das retenções que excede o bruto.</param>
    public RetencaoExcedeLiquidacaoException(decimal valorLiquidacao, decimal totalRetencoes)
        : base($"Total de retencoes {totalRetencoes:0.00} excede o valor liquidado {valorLiquidacao:0.00}.")
    {
    }
}

/// <summary>
/// Lançada ao tentar cancelar um Resto a Pagar fora da janela legal de prazo/decadência
/// (Decreto 93.872/86 e normas TCE-RS, parametrizável por tenant). Fail-closed.
/// </summary>
public sealed class PrazoCancelamentoRestoAPagarException : InvalidOperationException
{
    /// <summary>Cria a exceção com o contexto do prazo violado.</summary>
    /// <param name="exercicioReferencia">Exercício da data de referência do cancelamento.</param>
    /// <param name="exercicioLimiteVigencia">Último exercício de vigência do RAP.</param>
    public PrazoCancelamentoRestoAPagarException(int exercicioReferencia, int exercicioLimiteVigencia)
        : base($"Cancelamento de Resto a Pagar fora da janela legal: exercicio de referencia {exercicioReferencia} excede o limite de vigencia {exercicioLimiteVigencia}.")
    {
    }
}
