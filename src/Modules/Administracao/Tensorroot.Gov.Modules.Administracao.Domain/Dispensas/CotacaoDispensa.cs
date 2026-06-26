using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

/// <summary>Identificador forte de uma <see cref="CotacaoDispensa"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CotacaoDispensaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CotacaoDispensaId"/>.</returns>
    public static CotacaoDispensaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Cotacao (lance/proposta) apresentada por um fornecedor para um item da dispensa eletronica na
/// etapa de envio de lances (IN SEGES/ME 67/2021). Mantem o melhor lance vigente do fornecedor para o
/// item: lances sucessivos so melhoram a oferta (menor preco) e sao registrados em sequencia para
/// determinismo apos reidratacao.
/// </summary>
public sealed class CotacaoDispensa : Entity<CotacaoDispensaId>
{
    private CotacaoDispensa()
    {
    }

    private CotacaoDispensa(
        CotacaoDispensaId id,
        Guid fornecedorId,
        ItemDispensaId itemId,
        ValorMonetario valor,
        DateTimeOffset dataRegistro,
        long sequencia)
        : base(id)
    {
        FornecedorId = fornecedorId;
        ItemId = itemId;
        Valor = valor;
        DataRegistro = dataRegistro;
        Sequencia = sequencia;
        Situacao = SituacaoCotacao.Recebida;
    }

    /// <summary>Fornecedor proponente (outra raiz; referencia por Id).</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Item ao qual a cotacao se refere.</summary>
    public ItemDispensaId ItemId { get; private set; }

    /// <summary>Valor (unitario) ofertado.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Momento do registro do lance (relogio de borda; nunca do dominio).</summary>
    public DateTimeOffset DataRegistro { get; private set; }

    /// <summary>Sequencia monotonica do lance no procedimento (desempate deterministico apos reidratacao).</summary>
    public long Sequencia { get; private set; }

    /// <summary>Classificacao na disputa (nulo enquanto nao julgada).</summary>
    public int? Classificacao { get; private set; }

    /// <summary>Situacao atual da cotacao.</summary>
    public SituacaoCotacao Situacao { get; private set; }

    /// <summary>Registra uma nova cotacao recebida.</summary>
    /// <param name="fornecedorId">Fornecedor proponente.</param>
    /// <param name="itemId">Item disputado.</param>
    /// <param name="valor">Valor ofertado.</param>
    /// <param name="dataRegistro">Momento do registro (relogio externo via handler).</param>
    /// <param name="sequencia">Sequencia monotonica do lance.</param>
    /// <returns>Nova <see cref="CotacaoDispensa"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o fornecedor nao for informado.</exception>
    public static CotacaoDispensa Registrar(
        Guid fornecedorId,
        ItemDispensaId itemId,
        ValorMonetario valor,
        DateTimeOffset dataRegistro,
        long sequencia)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorId), "Fornecedor e obrigatorio na cotacao.");
        }

        return new CotacaoDispensa(CotacaoDispensaId.New(), fornecedorId, itemId, valor, dataRegistro, sequencia);
    }

    /// <summary>Classifica a cotacao na posicao informada.</summary>
    /// <param name="classificacao">Posicao na ordenacao (maior que zero).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a classificacao nao for positiva.</exception>
    public void Classificar(int classificacao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(classificacao);
        Classificacao = classificacao;
        Situacao = SituacaoCotacao.Classificada;
    }

    /// <summary>Desclassifica a cotacao.</summary>
    public void Desclassificar()
    {
        Situacao = SituacaoCotacao.Desclassificada;
    }

    /// <summary>Marca a cotacao como vencedora.</summary>
    public void MarcarVencedora()
    {
        Situacao = SituacaoCotacao.Vencedora;
    }
}
