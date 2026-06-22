using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

/// <summary>Identificador forte de uma <see cref="LinhaContabil"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LinhaContabilId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LinhaContabilId"/>.</returns>
    public static LinhaContabilId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Linha da Matriz de Saldos Contabeis: conta PCASP, natureza, valor e atributos da MSC.</summary>
public sealed class LinhaContabil : Entity<LinhaContabilId>
{
    private LinhaContabil()
    {
    }

    private LinhaContabil(
        LinhaContabilId id,
        string contaPcasp,
        NaturezaSaldo naturezaSaldo,
        ValorMonetario valor,
        string? informacaoComplementar)
        : base(id)
    {
        ContaPcasp = contaPcasp;
        NaturezaSaldo = naturezaSaldo;
        Valor = valor;
        InformacaoComplementar = informacaoComplementar;
    }

    /// <summary>Conta do PCASP (nao vazia).</summary>
    public string ContaPcasp { get; private set; } = default!;

    /// <summary>Natureza do saldo (devedor ou credor).</summary>
    public NaturezaSaldo NaturezaSaldo { get; private set; }

    /// <summary>Valor do saldo da linha.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Atributo/informacao complementar da MSC (opcional).</summary>
    public string? InformacaoComplementar { get; private set; }

    /// <summary>Cria uma linha contabil da matriz.</summary>
    /// <param name="contaPcasp">Conta do PCASP (nao vazia).</param>
    /// <param name="naturezaSaldo">Natureza do saldo (devedor/credor).</param>
    /// <param name="valor">Valor do saldo.</param>
    /// <param name="informacaoComplementar">Informacao complementar (opcional).</param>
    /// <returns>Nova <see cref="LinhaContabil"/>.</returns>
    /// <exception cref="ArgumentException">Se a conta PCASP for vazia.</exception>
    /// <exception cref="ArgumentNullException">Se o valor for nulo.</exception>
    public static LinhaContabil Criar(
        string contaPcasp,
        NaturezaSaldo naturezaSaldo,
        ValorMonetario valor,
        string? informacaoComplementar = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contaPcasp);
        ArgumentNullException.ThrowIfNull(valor);
        return new LinhaContabil(LinhaContabilId.New(), contaPcasp, naturezaSaldo, valor, informacaoComplementar);
    }
}
