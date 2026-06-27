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

    // 5o nivel das contas PATRIMONIAIS (classes 1-4) = digito padronizado de consolidacao / saldos
    // reciprocos (MCASP 11a ed. Parte IV item 3.2.2; Regras Gerais MSC 2026 "Conta Contabil" p.6).
    // Valores: 1=Consolidacao (saldo nao se anula entre entes), 2=Intra-OFSS (anula dentro do MESMO
    // ente/OFSS), 3=Inter-OFSS Uniao, 4=Inter-OFSS Estado/DF, 5=Inter-OFSS Municipio. Esse 5o digito so
    // existe a partir do nivel 5; abaixo dele (niveis 1-4) a conta nao carrega indicador de consolidacao.
    // Modelado e exposto via IndicadorConsolidacao(); a validacao 1-5 e aplicada pelos consumidores que
    // sabem que a conta esta no nivel padronizado de saldos reciprocos (nem todo 5o digito patrimonial e
    // consolidacao — pode ser subtitulo do PCASP Estendido).
    private const int NivelDigitoConsolidacao = 5;
    private const int ClassePatrimonialMaxima = 4;

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

    /// <summary>
    /// Indicador de consolidacao / saldos reciprocos (5o digito padronizado) das contas patrimoniais
    /// (classes 1-4): 1=Consolidacao, 2=Intra-OFSS, 3=Inter-OFSS Uniao, 4=Inter-OFSS Estado/DF,
    /// 5=Inter-OFSS Municipio (MCASP P.IV item 3.2.2). Retorna <c>null</c> para contas nao patrimoniais
    /// ou ainda sem o 5o nivel (sinteticas ate o 4o).
    /// </summary>
    /// <returns>Indicador 1-5, ou <c>null</c> se nao aplicavel.</returns>
    public int? IndicadorConsolidacao()
    {
        if (Classe > ClassePatrimonialMaxima || Nivel < NivelDigitoConsolidacao)
        {
            return null;
        }

        return int.Parse(_segmentos[NivelDigitoConsolidacao - 1], CultureInfo.InvariantCulture);
    }

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
