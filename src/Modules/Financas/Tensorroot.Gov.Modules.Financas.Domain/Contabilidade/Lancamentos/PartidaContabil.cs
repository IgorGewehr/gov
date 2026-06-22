using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;

/// <summary>Identificador forte da entidade-filha <see cref="PartidaContabil"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PartidaContabilId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PartidaContabilId"/>.</returns>
    public static PartidaContabilId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Lado da partida (débito ou crédito).</summary>
public enum LadoPartida
{
    /// <summary>Débito.</summary>
    Debito = 1,

    /// <summary>Crédito.</summary>
    Credito = 2,
}

/// <summary>
/// Partida de um lançamento contábil: uma única conta movimentada num lado (débito/crédito) por
/// um valor positivo. Guarda snapshots do código e da natureza da conta para o balancete sem join.
/// </summary>
public sealed class PartidaContabil : Entity<PartidaContabilId>
{
    private PartidaContabil()
    {
    }

    private PartidaContabil(
        PartidaContabilId id,
        ContaContabilId contaId,
        string codigoConta,
        NaturezaInformacao naturezaInformacao,
        LadoPartida lado,
        ValorMonetario valor)
        : base(id)
    {
        ContaId = contaId;
        CodigoConta = codigoConta;
        NaturezaInformacao = naturezaInformacao;
        Lado = lado;
        Valor = valor;
    }

    /// <summary>Conta movimentada.</summary>
    public ContaContabilId ContaId { get; private set; }

    /// <summary>Snapshot do código contábil (desnormalização para o balancete).</summary>
    public string CodigoConta { get; private set; } = default!;

    /// <summary>Snapshot da natureza da informação da conta (para a invariante sem carregar a conta).</summary>
    public NaturezaInformacao NaturezaInformacao { get; private set; }

    /// <summary>Lado da partida.</summary>
    public LadoPartida Lado { get; private set; }

    /// <summary>Valor (sempre positivo).</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Cria uma partida validada (valor positivo).</summary>
    /// <param name="contaId">Conta movimentada.</param>
    /// <param name="codigoConta">Código (snapshot).</param>
    /// <param name="naturezaInformacao">Natureza (snapshot).</param>
    /// <param name="lado">Lado.</param>
    /// <param name="valor">Valor.</param>
    /// <returns>Nova <see cref="PartidaContabil"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor não for positivo.</exception>
    internal static PartidaContabil Criar(
        ContaContabilId contaId,
        string codigoConta,
        NaturezaInformacao naturezaInformacao,
        LadoPartida lado,
        ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoConta);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor da partida deve ser positivo.");
        }

        return new PartidaContabil(PartidaContabilId.New(), contaId, codigoConta, naturezaInformacao, lado, valor);
    }
}
