using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;

/// <summary>Identificador forte do agregado <see cref="OrdemDePagamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct OrdemDePagamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="OrdemDePagamentoId"/>.</returns>
    public static OrdemDePagamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade-filha <see cref="ItemPagamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemPagamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemPagamentoId"/>.</returns>
    public static ItemPagamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação da Ordem de Pagamento.</summary>
public enum SituacaoPagamento
{
    /// <summary>Emitida (em montagem).</summary>
    Emitida = 1,

    /// <summary>Efetuada (paga).</summary>
    Efetuada = 2,

    /// <summary>Cancelada.</summary>
    Cancelada = 3,
}

/// <summary>Item de uma ordem de pagamento: quita parcela de uma liquidação específica.</summary>
public sealed class ItemPagamento : Entity<ItemPagamentoId>
{
    private ItemPagamento()
    {
    }

    private ItemPagamento(ItemPagamentoId id, LiquidacaoId liquidacaoId, ValorMonetario valor)
        : base(id)
    {
        LiquidacaoId = liquidacaoId;
        Valor = valor;
    }

    /// <summary>Liquidação quitada por este item.</summary>
    public LiquidacaoId LiquidacaoId { get; private set; }

    /// <summary>Valor do item.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Cria um item de pagamento.</summary>
    /// <param name="liquidacaoId">Liquidação quitada.</param>
    /// <param name="valor">Valor do item.</param>
    /// <returns>Novo <see cref="ItemPagamento"/>.</returns>
    internal static ItemPagamento Criar(LiquidacaoId liquidacaoId, ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor do item deve ser positivo.");
        }

        return new ItemPagamento(ItemPagamentoId.New(), liquidacaoId, valor);
    }
}

/// <summary>
/// Ordem de Pagamento — 3º estágio da despesa (Lei 4.320/64, art. 64). Quita uma ou
/// mais liquidações. ValorTotal = soma dos itens; o pagamento exige liquidação prévia.
/// </summary>
public sealed class OrdemDePagamento : AggregateRoot<OrdemDePagamentoId>, IMustHaveTenant
{
    private readonly List<ItemPagamento> _itens = [];

    private OrdemDePagamento()
    {
    }

    private OrdemDePagamento(
        OrdemDePagamentoId id,
        Guid tenantId,
        string numero,
        DateOnly dataPagamento,
        ContaBancaria contaBancaria)
        : base(id)
    {
        TenantId = tenantId;
        Numero = numero;
        DataPagamento = dataPagamento;
        ContaBancaria = contaBancaria;
        ValorTotal = ValorMonetario.Zero;
        Situacao = SituacaoPagamento.Emitida;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Número da ordem de pagamento.</summary>
    public string Numero { get; private set; } = default!;

    /// <summary>Data do pagamento.</summary>
    public DateOnly DataPagamento { get; private set; }

    /// <summary>Conta bancária pagadora.</summary>
    public ContaBancaria ContaBancaria { get; private set; } = default!;

    /// <summary>Valor total = soma dos itens.</summary>
    public ValorMonetario ValorTotal { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoPagamento Situacao { get; private set; }

    /// <summary>Itens (liquidações quitadas).</summary>
    public IReadOnlyCollection<ItemPagamento> Itens => _itens.AsReadOnly();

    /// <summary>Emite uma ordem de pagamento vazia.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numero">Número da ordem.</param>
    /// <param name="dataPagamento">Data do pagamento.</param>
    /// <param name="contaBancaria">Conta bancária pagadora.</param>
    /// <returns>Nova <see cref="OrdemDePagamento"/>.</returns>
    public static OrdemDePagamento Emitir(
        Guid tenantId,
        string numero,
        DateOnly dataPagamento,
        ContaBancaria contaBancaria)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        ArgumentNullException.ThrowIfNull(contaBancaria);
        return new OrdemDePagamento(OrdemDePagamentoId.New(), tenantId, numero, dataPagamento, contaBancaria);
    }

    /// <summary>Adiciona um item (liquidação a quitar) enquanto a ordem está em montagem.</summary>
    /// <param name="liquidacaoId">Liquidação quitada.</param>
    /// <param name="valor">Valor do item.</param>
    /// <exception cref="InvalidOperationException">Se a ordem não estiver emitida.</exception>
    public void AdicionarItem(LiquidacaoId liquidacaoId, ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Situacao != SituacaoPagamento.Emitida)
        {
            throw new InvalidOperationException($"So e possivel adicionar itens com a ordem emitida. Situacao atual: {Situacao}.");
        }

        _itens.Add(ItemPagamento.Criar(liquidacaoId, valor));
        ValorTotal = ValorTotal.Somar(valor);
    }

    /// <summary>Efetua o pagamento (Lei 4.320/64, art. 64).</summary>
    /// <exception cref="InvalidOperationException">Se a ordem não estiver emitida ou estiver vazia.</exception>
    public void Efetuar()
    {
        if (Situacao != SituacaoPagamento.Emitida)
        {
            throw new InvalidOperationException($"Apenas ordem emitida pode ser efetuada. Situacao atual: {Situacao}.");
        }

        if (_itens.Count == 0 || !ValorTotal.EhPositivo())
        {
            throw new InvalidOperationException("Ordem de pagamento sem itens nao pode ser efetuada.");
        }

        Situacao = SituacaoPagamento.Efetuada;
        RaiseDomainEvent(new PagamentoEfetuado(Id, ValorTotal.Valor));
    }

    /// <summary>Cancela a ordem (somente antes de efetuada).</summary>
    /// <exception cref="InvalidOperationException">Se já efetuada/cancelada.</exception>
    public void Cancelar()
    {
        if (Situacao != SituacaoPagamento.Emitida)
        {
            throw new InvalidOperationException($"Apenas ordem emitida pode ser cancelada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoPagamento.Cancelada;
    }
}
