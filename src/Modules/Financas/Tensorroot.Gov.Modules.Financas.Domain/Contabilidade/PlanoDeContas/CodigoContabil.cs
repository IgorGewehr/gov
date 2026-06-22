using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

/// <summary>
/// Código contábil PCASP segmentado (máscara <c>C.G.SG.T.ST.IT.SI</c>). Encapsula a hierarquia
/// (1–7 níveis), deriva a classe (1º dígito) e fornece o código do pai e as naturezas default
/// fixas do §2 do PCASP. Imutável e comparado por valor.
/// </summary>
public sealed class CodigoContabil : ValueObject
{
    // Níveis 1-4: exatamente 1 dígito (Classe/Grupo/Subgrupo/Título). Níveis 5-7 (Subtítulo/Item/
    // Subitem): 1 ou 2 dígitos — o elenco anual define o detalhamento exato e os códigos reais do
    // PCASP variam (ex.: 6.2.2.1.3.01.00 tem o 5º segmento com 1 dígito). [validar-plano-oficial]:
    // fixar dígitos dos níveis 5-7 contra o MCASP 11ª ed. Vol. IV / PCASP 2026.
    private const int NivelTituloFixo = 4;
    private const int MaxDigitosNivelDetalhe = 2;

    private CodigoContabil(string codigo, int nivel, int classe, string[] segmentos)
    {
        Codigo = codigo;
        Nivel = nivel;
        Classe = classe;
        _segmentos = segmentos;
    }

    private readonly string[] _segmentos;

    /// <summary>Código canônico com pontos (ex.: <c>1.1.1.1.01</c>).</summary>
    public string Codigo { get; }

    /// <summary>Nível hierárquico (1 a 7).</summary>
    public int Nivel { get; }

    /// <summary>Classe (1º dígito, 1 a 8).</summary>
    public int Classe { get; }

    /// <summary>
    /// Cria e valida um código contábil a partir da representação com pontos.
    /// </summary>
    /// <param name="codigo">Código segmentado (ex.: <c>6.2.2.1.3.01.00</c>).</param>
    /// <returns>Instância validada de <see cref="CodigoContabil"/>.</returns>
    /// <exception cref="ArgumentException">Se a máscara/segmentos forem inválidos.</exception>
    public static CodigoContabil De(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var bruto = codigo.Trim();
        var segmentos = bruto.Split('.', StringSplitOptions.None);

        if (segmentos.Length is < 1 or > 7)
        {
            throw new ArgumentException($"Codigo contabil '{bruto}' tem {segmentos.Length} niveis (esperado 1 a 7).", nameof(codigo));
        }

        for (var i = 0; i < segmentos.Length; i++)
        {
            var seg = segmentos[i];
            if (string.IsNullOrEmpty(seg) || !seg.All(char.IsDigit))
            {
                throw new ArgumentException($"Codigo contabil '{bruto}' possui segmento invalido na posicao {i + 1}.", nameof(codigo));
            }

            var nivel = i + 1;
            var ok = nivel <= NivelTituloFixo
                ? seg.Length == 1
                : seg.Length is >= 1 and <= MaxDigitosNivelDetalhe;
            if (!ok)
            {
                throw new ArgumentException(
                    $"Codigo contabil '{bruto}': segmento {nivel} com numero de digitos invalido.",
                    nameof(codigo));
            }
        }

        var classe = int.Parse(segmentos[0], CultureInfo.InvariantCulture);
        if (classe is < 1 or > 8)
        {
            throw new ArgumentException($"Classe contabil invalida ({classe}); esperado 1 a 8.", nameof(codigo));
        }

        return new CodigoContabil(bruto, segmentos.Length, classe, segmentos);
    }

    /// <summary>Código do nível imediatamente superior, ou <c>null</c> se já estiver no nível 1.</summary>
    /// <returns>Código do pai ou <c>null</c>.</returns>
    public string? CodigoPai()
        => Nivel <= 1 ? null : string.Join('.', _segmentos[..(Nivel - 1)]);

    /// <summary>Natureza da informação default derivada da classe (§3 PCASP).</summary>
    /// <returns>Patrimonial (1-4), Orçamentária (5-6) ou Controle (7-8).</returns>
    public NaturezaInformacao NaturezaInformacaoDefault() => Classe switch
    {
        >= 1 and <= 4 => NaturezaInformacao.Patrimonial,
        5 or 6 => NaturezaInformacao.Orcamentaria,
        _ => NaturezaInformacao.Controle,
    };

    /// <summary>Natureza do saldo default derivada da classe (§2 PCASP): ímpares devedoras, pares credoras.</summary>
    /// <returns>Devedora (1,3,5,7) ou Credora (2,4,6,8).</returns>
    public NaturezaSaldo NaturezaSaldoDefault()
        => Classe % 2 == 1 ? NaturezaSaldo.Devedora : NaturezaSaldo.Credora;

    /// <summary>Indica se o código está sob o prefixo informado (para agregação de balancete sintético).</summary>
    /// <param name="prefixo">Prefixo de código (ex.: <c>6.2</c>).</param>
    /// <returns><c>true</c> se este código pertence à subárvore do prefixo.</returns>
    public bool EstaSob(string prefixo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixo);
        return Codigo == prefixo || Codigo.StartsWith(prefixo + ".", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => Codigo;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
    }
}
