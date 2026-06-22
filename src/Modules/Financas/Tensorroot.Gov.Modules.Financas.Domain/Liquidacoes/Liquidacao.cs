using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;

/// <summary>Identificador forte do agregado <see cref="Liquidacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LiquidacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LiquidacaoId"/>.</returns>
    public static LiquidacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação da liquidação.</summary>
public enum SituacaoLiquidacao
{
    /// <summary>Liquidada (nada pago).</summary>
    Liquidada = 1,

    /// <summary>Parcialmente paga.</summary>
    ParcialmentePaga = 2,

    /// <summary>Totalmente paga.</summary>
    Paga = 3,

    /// <summary>Estornada.</summary>
    Estornada = 4,
}

/// <summary>
/// Liquidação — 2º estágio da despesa (Lei 4.320/64, art. 63): verifica o direito do
/// credor mediante documento comprobatório. SaldoAPagar = Valor − Pago.
/// </summary>
public sealed class Liquidacao : AggregateRoot<LiquidacaoId>, IMustHaveTenant
{
    private Liquidacao()
    {
    }

    private Liquidacao(
        LiquidacaoId id,
        Guid tenantId,
        EmpenhoId empenhoId,
        ValorMonetario valor,
        DateOnly dataLiquidacao,
        DocumentoComprobatorio documento)
        : base(id)
    {
        TenantId = tenantId;
        EmpenhoId = empenhoId;
        Valor = valor;
        ValorPago = ValorMonetario.Zero;
        DataLiquidacao = dataLiquidacao;
        Documento = documento;
        Situacao = SituacaoLiquidacao.Liquidada;
        RaiseDomainEvent(new DespesaLiquidada(id, empenhoId, valor.Valor));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Empenho vinculado.</summary>
    public EmpenhoId EmpenhoId { get; private set; }

    /// <summary>Valor liquidado.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Valor já pago desta liquidação.</summary>
    public ValorMonetario ValorPago { get; private set; } = default!;

    /// <summary>Data da liquidação.</summary>
    public DateOnly DataLiquidacao { get; private set; }

    /// <summary>Documento comprobatório do direito do credor.</summary>
    public DocumentoComprobatorio Documento { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoLiquidacao Situacao { get; private set; }

    /// <summary>Saldo a pagar = Valor − Pago.</summary>
    public ValorMonetario SaldoAPagar => Valor.Subtrair(ValorPago);

    /// <summary>Registra uma liquidação de despesa.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="empenhoId">Empenho vinculado.</param>
    /// <param name="valor">Valor liquidado.</param>
    /// <param name="dataLiquidacao">Data da liquidação.</param>
    /// <param name="documento">Documento comprobatório.</param>
    /// <returns>Nova <see cref="Liquidacao"/>.</returns>
    public static Liquidacao Registrar(
        Guid tenantId,
        EmpenhoId empenhoId,
        ValorMonetario valor,
        DateOnly dataLiquidacao,
        DocumentoComprobatorio documento)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentNullException.ThrowIfNull(documento);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor liquidado deve ser positivo.");
        }

        return new Liquidacao(LiquidacaoId.New(), tenantId, empenhoId, valor, dataLiquidacao, documento);
    }

    /// <summary>Registra o pagamento de parcela desta liquidação.</summary>
    /// <param name="valor">Valor pago.</param>
    /// <exception cref="SaldoLiquidacaoInsuficienteException">Se exceder o saldo a pagar.</exception>
    public void RegistrarPagamento(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Situacao == SituacaoLiquidacao.Estornada)
        {
            throw new InvalidOperationException("Liquidacao estornada nao admite pagamento.");
        }

        if (valor.EhMaiorQue(SaldoAPagar))
        {
            throw new SaldoLiquidacaoInsuficienteException(SaldoAPagar.Valor, valor.Valor);
        }

        ValorPago = ValorPago.Somar(valor);
        Situacao = SaldoAPagar.EhPositivo() ? SituacaoLiquidacao.ParcialmentePaga : SituacaoLiquidacao.Paga;
    }

    /// <summary>Estorna a liquidação (somente sem pagamento).</summary>
    /// <exception cref="InvalidOperationException">Se houver valor pago.</exception>
    public void Estornar()
    {
        if (ValorPago.EhPositivo())
        {
            throw new InvalidOperationException("Nao e possivel estornar liquidacao com pagamento registrado.");
        }

        Situacao = SituacaoLiquidacao.Estornada;
        RaiseDomainEvent(new LiquidacaoEstornada(Id, EmpenhoId, Valor.Valor));
    }
}
