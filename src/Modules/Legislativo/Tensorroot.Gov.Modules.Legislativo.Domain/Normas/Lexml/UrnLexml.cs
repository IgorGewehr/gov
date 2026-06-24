using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;

/// <summary>
/// URN LexML-BR de uma norma juridica (identificador unico, persistente e resolvel de documento
/// legislativo/normativo — padrao LexML Brasil, Parte 2: URN; namespace <c>lex</c>).
/// <para>
/// Formato: <c>urn:lex:[local]:[autoridade]:[tipo];[descritor]</c>, onde o descritor de uma norma
/// numerada e <c>[data];[numero]</c> (data de assinatura/promulgacao no formato ISO <c>aaaa-mm-dd</c>).
/// Exemplo: <c>urn:lex:br;rs;maximiliano.de.almeida:camara.municipal:lei:2025-03-10;1234</c>.
/// </para>
/// <para>
/// VO imutavel comparado por valor (a string canonica). A composicao e deterministica: mesmo ente +
/// autoridade + tipo + data + numero geram sempre a mesma URN (idempotencia/colacao no acervo).
/// </para>
/// </summary>
public sealed class UrnLexml : ValueObject
{
    /// <summary>Prefixo fixo do esquema de nomes LexML.</summary>
    public const string Prefixo = "urn:lex";

    private UrnLexml(string valor) => Valor = valor;

    /// <summary>Representacao canonica da URN.</summary>
    public string Valor { get; }

    /// <summary>
    /// Compoe a URN LexML de uma norma a partir do ente/autoridade, do tipo (mapeado para o vocabulario
    /// LexML), da data de promulgacao e do numero.
    /// </summary>
    /// <param name="ente">Identificacao do ente (local + autoridade).</param>
    /// <param name="tipo">Especie da norma.</param>
    /// <param name="dataPromulgacao">Data de promulgacao/assinatura (compoe o descritor, ISO 8601).</param>
    /// <param name="numero">Numero da norma (positivo).</param>
    /// <returns>URN LexML canonica.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="ente"/> for nulo.</exception>
    /// <exception cref="ArgumentException">Se o tipo nao for reconhecido ou o numero nao for positivo.</exception>
    public static UrnLexml Compor(IdentificacaoEnte ente, TipoNorma tipo, DateOnly dataPromulgacao, int numero)
    {
        ArgumentNullException.ThrowIfNull(ente);
        if (numero <= 0)
        {
            throw new ArgumentException("Numero da norma deve ser positivo.", nameof(numero));
        }

        var tipoLexml = MapearTipo(tipo);
        var data = dataPromulgacao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var numeroTexto = numero.ToString(CultureInfo.InvariantCulture);

        // urn:lex:[local]:[autoridade]:[tipo]:[data];[numero]
        var valor = $"{Prefixo}:{ente.Local}:{ente.Autoridade}:{tipoLexml}:{data};{numeroTexto}";
        return new UrnLexml(valor);
    }

    /// <summary>
    /// Mapeia a especie da norma para o nome de TIPO DE DOCUMENTO no vocabulario LexML (grafia canonica,
    /// minuscula e com pontos). Mantem a lista exaustiva para que um tipo novo nao escape silenciosamente.
    /// </summary>
    /// <param name="tipo">Especie da norma.</param>
    /// <returns>Nome do tipo no vocabulario LexML.</returns>
    /// <exception cref="ArgumentException">Se o tipo nao estiver mapeado.</exception>
    public static string MapearTipo(TipoNorma tipo) => tipo switch
    {
        TipoNorma.Lei => "lei",
        TipoNorma.LeiComplementar => "lei.complementar",
        TipoNorma.DecretoLegislativo => "decreto.legislativo",
        TipoNorma.Resolucao => "resolucao",
        TipoNorma.EmendaLOM => "emenda.lei.organica",
        TipoNorma.LeiOrganica => "lei.organica",
        _ => throw new ArgumentException($"Tipo de norma sem mapeamento LexML: {tipo}.", nameof(tipo)),
    };

    /// <inheritdoc />
    public override string ToString() => Valor;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
