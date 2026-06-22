using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;

/// <summary>
/// Linha de um roteiro contábil: define o lado da partida e a conta a movimentar por seletor —
/// código fixo (quando estável) OU papel semântico (resolvido pelo mapa do tenant). A natureza da
/// informação é declarada para o agrupamento por natureza (um lançamento por natureza).
/// </summary>
public sealed class LinhaRoteiro : ValueObject
{
    private LinhaRoteiro(
        LadoPartida lado,
        NaturezaInformacao naturezaInformacao,
        string? codigoContaFixo,
        PapelConta? papel,
        BaseValorRoteiro baseValor)
    {
        Lado = lado;
        NaturezaInformacao = naturezaInformacao;
        CodigoContaFixo = codigoContaFixo;
        Papel = papel;
        BaseValor = baseValor;
    }

    /// <summary>Lado da partida (débito/crédito).</summary>
    public LadoPartida Lado { get; }

    /// <summary>Natureza da informação da conta-alvo (para o agrupamento).</summary>
    public NaturezaInformacao NaturezaInformacao { get; }

    /// <summary>Código fixo da conta (quando estável), ou <c>null</c> se resolvido por papel.</summary>
    public string? CodigoContaFixo { get; }

    /// <summary>Papel semântico (quando o código é resolvido pelo mapa do tenant), ou <c>null</c>.</summary>
    public PapelConta? Papel { get; }

    /// <summary>Base do valor da partida.</summary>
    public BaseValorRoteiro BaseValor { get; }

    /// <summary>Cria uma linha que referencia a conta por código fixo (ex.: classes 5/6).</summary>
    /// <param name="lado">Lado.</param>
    /// <param name="naturezaInformacao">Natureza da conta.</param>
    /// <param name="codigoContaFixo">Código PCASP fixo.</param>
    /// <param name="baseValor">Base do valor.</param>
    /// <returns>Nova <see cref="LinhaRoteiro"/>.</returns>
    public static LinhaRoteiro PorCodigo(
        LadoPartida lado,
        NaturezaInformacao naturezaInformacao,
        string codigoContaFixo,
        BaseValorRoteiro baseValor = BaseValorRoteiro.ValorDoFato)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoContaFixo);
        // Valida a máscara já na configuração do roteiro.
        _ = CodigoContabil.De(codigoContaFixo);
        return new LinhaRoteiro(lado, naturezaInformacao, codigoContaFixo, null, baseValor);
    }

    /// <summary>Cria uma linha que referencia a conta por papel semântico (resolvido pelo mapa).</summary>
    /// <param name="lado">Lado.</param>
    /// <param name="naturezaInformacao">Natureza da conta.</param>
    /// <param name="papel">Papel semântico.</param>
    /// <param name="baseValor">Base do valor.</param>
    /// <returns>Nova <see cref="LinhaRoteiro"/>.</returns>
    public static LinhaRoteiro PorPapel(
        LadoPartida lado,
        NaturezaInformacao naturezaInformacao,
        PapelConta papel,
        BaseValorRoteiro baseValor = BaseValorRoteiro.ValorDoFato)
        => new(lado, naturezaInformacao, null, papel, baseValor);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Lado;
        yield return NaturezaInformacao;
        yield return CodigoContaFixo;
        yield return Papel;
        yield return BaseValor;
    }
}
