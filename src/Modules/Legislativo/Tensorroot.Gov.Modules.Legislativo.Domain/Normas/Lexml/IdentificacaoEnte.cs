using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;

/// <summary>
/// Identificacao do ENTE/AUTORIDADE para composicao da URN LexML-BR de uma norma do Legislativo
/// municipal (padrao LexML — Parte 2: URN; baseado no namespace <c>lex-br</c>). Compoe a porcao de
/// <b>local</b> (jurisdicao) e a <b>autoridade</b> emanadora da norma.
/// <para>
/// LOCAL: a hierarquia LexML do Brasil e <c>br;[uf];[municipio]</c>. Para a Camara de um municipio do RS,
/// o local e <c>br;rs;[municipio]</c> (ex.: <c>br;rs;maximiliano.de.almeida</c>). A grafia do municipio
/// segue a convencao LexML (minusculas, sem acento, espacos -> ponto).
/// </para>
/// <para>
/// AUTORIDADE: quem expede a norma. Para leis municipais a autoridade e <c>camara.municipal</c> (origem
/// no Legislativo) ou <c>municipio</c> (Executivo, quando promulgada/sancionada pelo Prefeito). Para
/// decreto legislativo e resolucao, a autoridade e sempre <c>camara.municipal</c>.
/// </para>
/// <para>
/// VO imutavel, comparado por valor. NAO carrega numeros magicos: a esfera/UF/municipio/autoridade sao
/// configurados por tenant (cada Camara informa a sua jurisdicao no cadastro/parametro).
/// </para>
/// </summary>
public sealed class IdentificacaoEnte : ValueObject
{
    /// <summary>Sigla do pais na hierarquia LexML (Brasil).</summary>
    public const string Pais = "br";

    private IdentificacaoEnte(string uf, string municipio, string autoridade)
    {
        Uf = uf;
        Municipio = municipio;
        Autoridade = autoridade;
    }

    /// <summary>Unidade da federacao (sigla, minuscula — ex.: <c>rs</c>).</summary>
    public string Uf { get; }

    /// <summary>Municipio na grafia LexML (minusculo, sem acento, com pontos — ex.: <c>maximiliano.de.almeida</c>).</summary>
    public string Municipio { get; }

    /// <summary>Autoridade emanadora (ex.: <c>camara.municipal</c>, <c>municipio</c>).</summary>
    public string Autoridade { get; }

    /// <summary>Porcao de LOCAL da URN LexML: <c>br;[uf];[municipio]</c>.</summary>
    public string Local => $"{Pais};{Uf};{Municipio}";

    /// <summary>
    /// Cria a identificacao do ente, normalizando UF/municipio/autoridade para a convencao LexML
    /// (minusculas, sem diacriticos, espacos convertidos em ponto, sem caracteres invalidos).
    /// </summary>
    /// <param name="uf">Sigla da UF (2 letras).</param>
    /// <param name="municipio">Nome do municipio (livre; normalizado).</param>
    /// <param name="autoridade">Autoridade emanadora (livre; normalizada).</param>
    /// <returns>Identificacao do ente normalizada.</returns>
    /// <exception cref="ArgumentException">Se UF nao tiver 2 letras, ou municipio/autoridade ficarem vazios.</exception>
    public static IdentificacaoEnte De(string uf, string municipio, string autoridade)
    {
        var ufNormalizada = LexmlSlug.Slugificar(uf);
        if (ufNormalizada.Length != 2)
        {
            throw new ArgumentException("UF deve ter 2 letras (ex.: rs).", nameof(uf));
        }

        var municipioNormalizado = LexmlSlug.Slugificar(municipio);
        if (municipioNormalizado.Length == 0)
        {
            throw new ArgumentException("Municipio nao pode ser vazio.", nameof(municipio));
        }

        var autoridadeNormalizada = LexmlSlug.Slugificar(autoridade);
        if (autoridadeNormalizada.Length == 0)
        {
            throw new ArgumentException("Autoridade nao pode ser vazia.", nameof(autoridade));
        }

        return new IdentificacaoEnte(ufNormalizada, municipioNormalizado, autoridadeNormalizada);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Uf;
        yield return Municipio;
        yield return Autoridade;
    }
}
