using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

/// <summary>
/// Conta bancária pagadora de uma Ordem de Pagamento (banco/agência/conta, com PIX opcional).
/// </summary>
public sealed class ContaBancaria : ValueObject
{
    private ContaBancaria(string banco, string agencia, string conta, string? pix)
    {
        Banco = banco;
        Agencia = agencia;
        Conta = conta;
        Pix = pix;
    }

    /// <summary>Código do banco (COMPE/ISPB).</summary>
    public string Banco { get; private set; } = default!;

    /// <summary>Agência.</summary>
    public string Agencia { get; private set; } = default!;

    /// <summary>Número da conta.</summary>
    public string Conta { get; private set; } = default!;

    /// <summary>Chave PIX (opcional).</summary>
    public string? Pix { get; private set; }

    /// <summary>Cria uma conta bancária válida.</summary>
    /// <param name="banco">Código do banco.</param>
    /// <param name="agencia">Agência.</param>
    /// <param name="conta">Conta.</param>
    /// <param name="pix">Chave PIX (opcional).</param>
    /// <returns>Instância de <see cref="ContaBancaria"/>.</returns>
    /// <exception cref="ArgumentException">Se algum campo obrigatório estiver vazio.</exception>
    public static ContaBancaria De(string banco, string agencia, string conta, string? pix = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(banco);
        ArgumentException.ThrowIfNullOrWhiteSpace(agencia);
        ArgumentException.ThrowIfNullOrWhiteSpace(conta);
        return new ContaBancaria(banco.Trim(), agencia.Trim(), conta.Trim(), string.IsNullOrWhiteSpace(pix) ? null : pix.Trim());
    }

    /// <inheritdoc />
    public override string ToString() => $"{Banco}/{Agencia}/{Conta}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Banco;
        yield return Agencia;
        yield return Conta;
        yield return Pix;
    }
}
