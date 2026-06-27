using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;

/// <summary>Identificador forte do agregado <see cref="GuiaRecolhimento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct GuiaRecolhimentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="GuiaRecolhimentoId"/>.</returns>
    public static GuiaRecolhimentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação da guia de recolhimento.</summary>
public enum SituacaoGuiaRecolhimento
{
    /// <summary>Emitida (consignações reunidas, ainda não recolhida).</summary>
    Emitida = 1,

    /// <summary>Recolhida (passivo extra-orçamentário baixado).</summary>
    Recolhida = 2,

    /// <summary>Cancelada antes do recolhimento.</summary>
    Cancelada = 3,
}

/// <summary>
/// Item da guia: aponta a retenção (consignação) baixada e seu valor. Entidade própria (owned) do
/// agregado <see cref="GuiaRecolhimento"/> — classe (não struct) por exigência do mapeamento EF Core
/// de coleção própria (OwnsMany requer tipo por referência).
/// </summary>
public sealed class ItemGuiaRecolhimento
{
    private ItemGuiaRecolhimento()
    {
    }

    /// <summary>Cria um item de guia de recolhimento.</summary>
    /// <param name="liquidacaoId">Liquidação que apurou a retenção.</param>
    /// <param name="retencaoId">Retenção baixada.</param>
    /// <param name="valor">Valor da retenção.</param>
    public ItemGuiaRecolhimento(LiquidacaoId liquidacaoId, RetencaoId retencaoId, decimal valor)
    {
        LiquidacaoId = liquidacaoId;
        RetencaoId = retencaoId;
        Valor = valor;
    }

    /// <summary>Liquidação que apurou a retenção.</summary>
    public LiquidacaoId LiquidacaoId { get; private set; }

    /// <summary>Retenção baixada.</summary>
    public RetencaoId RetencaoId { get; private set; }

    /// <summary>Valor da retenção.</summary>
    public decimal Valor { get; private set; }
}

/// <summary>
/// Guia de recolhimento de consignações/retenções (DARF para IRRF/contribuições federais, GPS para
/// INSS, guia municipal para ISS). Reúne retenções de mesma natureza/código de receita e formaliza o
/// recolhimento — o dispêndio EXTRA-ORÇAMENTÁRIO que baixa o passivo "a recolher" (consignação) nascido
/// na liquidação (Lei 4.320/64; MCASP, ingressos/dispêndios extra-orçamentários). Agregado próprio para
/// manter a trilha do recolhimento e o casamento com o evento contábil de baixa.
/// </summary>
public sealed class GuiaRecolhimento : AggregateRoot<GuiaRecolhimentoId>, IMustHaveTenant
{
    private readonly List<ItemGuiaRecolhimento> _itens = [];

    private GuiaRecolhimento()
    {
    }

    private GuiaRecolhimento(
        GuiaRecolhimentoId id,
        Guid tenantId,
        NaturezaRetencao natureza,
        string? codigoReceita,
        string? favorecidoDocumento,
        DateOnly dataVencimento,
        DateOnly competencia)
        : base(id)
    {
        TenantId = tenantId;
        Natureza = natureza;
        CodigoReceita = codigoReceita;
        FavorecidoDocumento = favorecidoDocumento;
        DataVencimento = dataVencimento;
        Competencia = competencia;
        ValorTotal = ValorMonetario.Zero;
        Situacao = SituacaoGuiaRecolhimento.Emitida;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Natureza recolhida (IRRF, INSS, ISS, etc.).</summary>
    public NaturezaRetencao Natureza { get; private set; }

    /// <summary>Código de receita (DARF/GPS/guia municipal).</summary>
    public string? CodigoReceita { get; private set; }

    /// <summary>Documento (CNPJ/CPF) do favorecido/recolhedor.</summary>
    public string? FavorecidoDocumento { get; private set; }

    /// <summary>Data de vencimento do recolhimento.</summary>
    public DateOnly DataVencimento { get; private set; }

    /// <summary>Competência (mês/ano de referência das retenções).</summary>
    public DateOnly Competencia { get; private set; }

    /// <summary>Valor total = soma dos itens.</summary>
    public ValorMonetario ValorTotal { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoGuiaRecolhimento Situacao { get; private set; }

    /// <summary>Data do recolhimento efetivo (nulo até recolher).</summary>
    public DateOnly? DataRecolhimento { get; private set; }

    /// <summary>Itens (retenções baixadas).</summary>
    public IReadOnlyCollection<ItemGuiaRecolhimento> Itens => _itens.AsReadOnly();

    /// <summary>Emite uma guia de recolhimento vazia.</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="natureza">Natureza recolhida.</param>
    /// <param name="codigoReceita">Código de receita (opcional).</param>
    /// <param name="favorecidoDocumento">Documento do favorecido (opcional).</param>
    /// <param name="dataVencimento">Vencimento.</param>
    /// <param name="competencia">Competência.</param>
    /// <returns>Nova <see cref="GuiaRecolhimento"/>.</returns>
    public static GuiaRecolhimento Emitir(
        Guid tenantId,
        NaturezaRetencao natureza,
        string? codigoReceita,
        string? favorecidoDocumento,
        DateOnly dataVencimento,
        DateOnly competencia)
    {
        return new GuiaRecolhimento(GuiaRecolhimentoId.New(), tenantId, natureza, codigoReceita, favorecidoDocumento, dataVencimento, competencia);
    }

    /// <summary>Adiciona uma retenção à guia (enquanto emitida).</summary>
    /// <param name="item">Item (retenção a recolher).</param>
    /// <exception cref="InvalidOperationException">Se a guia não estiver emitida.</exception>
    public void AdicionarItem(ItemGuiaRecolhimento item)
    {
        if (Situacao != SituacaoGuiaRecolhimento.Emitida)
        {
            throw new InvalidOperationException($"So e possivel adicionar itens com a guia emitida. Situacao atual: {Situacao}.");
        }

        if (item.Valor <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(item), "Valor do item deve ser positivo.");
        }

        _itens.Add(item);
        ValorTotal = ValorTotal.Somar(ValorMonetario.De(item.Valor));
    }

    /// <summary>Registra o recolhimento efetivo (baixa do passivo extra-orçamentário).</summary>
    /// <param name="dataRecolhimento">Data do recolhimento.</param>
    /// <exception cref="InvalidOperationException">Se não estiver emitida ou estiver vazia.</exception>
    public void Recolher(DateOnly dataRecolhimento)
    {
        if (Situacao != SituacaoGuiaRecolhimento.Emitida)
        {
            throw new InvalidOperationException($"Apenas guia emitida pode ser recolhida. Situacao atual: {Situacao}.");
        }

        if (_itens.Count == 0 || !ValorTotal.EhPositivo())
        {
            throw new InvalidOperationException("Guia de recolhimento sem itens nao pode ser recolhida.");
        }

        Situacao = SituacaoGuiaRecolhimento.Recolhida;
        DataRecolhimento = dataRecolhimento;
        RaiseDomainEvent(new RecolhimentoEfetuado(Id, Natureza, ValorTotal.Valor, dataRecolhimento));
    }

    /// <summary>Cancela a guia (somente antes do recolhimento).</summary>
    /// <exception cref="InvalidOperationException">Se já recolhida/cancelada.</exception>
    public void Cancelar()
    {
        if (Situacao != SituacaoGuiaRecolhimento.Emitida)
        {
            throw new InvalidOperationException($"Apenas guia emitida pode ser cancelada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoGuiaRecolhimento.Cancelada;
    }
}
