using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Identificador forte de um <see cref="ItemAta"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemAtaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemAtaId"/>.</returns>
    public static ItemAtaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item de uma Ata de Registro de Precos: vincula um item do catalogo a um preco registrado, uma
/// quantidade maxima registrada e o fornecedor beneficiario. Controla o saldo disponivel para
/// contratacao (a quantidade ja contratada nao pode exceder a registrada). Entidade filha da
/// <see cref="Ata"/>.
/// </summary>
public sealed class ItemAta : Entity<ItemAtaId>
{
    private ItemAta()
    {
    }

    private ItemAta(
        ItemAtaId id,
        ItemCatalogoId itemCatalogoId,
        Guid fornecedorBeneficiarioId,
        ValorMonetario precoRegistrado,
        decimal quantidadeRegistrada)
        : base(id)
    {
        ItemCatalogoId = itemCatalogoId;
        FornecedorBeneficiarioId = fornecedorBeneficiarioId;
        PrecoRegistrado = precoRegistrado;
        QuantidadeRegistrada = quantidadeRegistrada;
        QuantidadeContratada = 0m;
    }

    /// <summary>Item de catalogo registrado.</summary>
    public ItemCatalogoId ItemCatalogoId { get; private set; }

    /// <summary>Fornecedor beneficiario do preco registrado para este item.</summary>
    public Guid FornecedorBeneficiarioId { get; private set; }

    /// <summary>Preco unitario registrado.</summary>
    public ValorMonetario PrecoRegistrado { get; private set; } = default!;

    /// <summary>Quantidade maxima registrada para contratacao.</summary>
    public decimal QuantidadeRegistrada { get; private set; }

    /// <summary>Quantidade ja contratada/empenhada contra este item (consome o saldo).</summary>
    public decimal QuantidadeContratada { get; private set; }

    /// <summary>Saldo ainda disponivel para contratacao (registrada - contratada).</summary>
    public decimal SaldoDisponivel => QuantidadeRegistrada - QuantidadeContratada;

    /// <summary>Cria um item de ata com preco e quantidade registrados.</summary>
    /// <param name="itemCatalogoId">Item de catalogo registrado.</param>
    /// <param name="fornecedorBeneficiarioId">Fornecedor beneficiario.</param>
    /// <param name="precoRegistrado">Preco unitario registrado (positivo).</param>
    /// <param name="quantidadeRegistrada">Quantidade maxima registrada (positiva).</param>
    /// <returns>Novo <see cref="ItemAta"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o preco for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva ou o fornecedor for vazio.</exception>
    public static ItemAta Criar(
        ItemCatalogoId itemCatalogoId,
        Guid fornecedorBeneficiarioId,
        ValorMonetario precoRegistrado,
        decimal quantidadeRegistrada)
    {
        ArgumentNullException.ThrowIfNull(precoRegistrado);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidadeRegistrada);
        if (fornecedorBeneficiarioId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorBeneficiarioId), "Fornecedor beneficiario obrigatorio.");
        }

        return new ItemAta(ItemAtaId.New(), itemCatalogoId, fornecedorBeneficiarioId, precoRegistrado, quantidadeRegistrada);
    }

    /// <summary>
    /// Consome quantidade do saldo registrado (uso direto ou adesao/carona), abatendo do disponivel.
    /// </summary>
    /// <param name="quantidade">Quantidade a contratar (positiva).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se a quantidade exceder o saldo disponivel.</exception>
    public void ConsumirSaldo(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (quantidade > SaldoDisponivel)
        {
            throw new InvalidOperationException(
                $"Quantidade {quantidade} excede o saldo disponivel {SaldoDisponivel} do item registrado.");
        }

        QuantidadeContratada += quantidade;
    }
}
