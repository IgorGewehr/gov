using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Serviço de domínio que serializa registros do leiaute SIAPC/PAD em linhas POSICIONAIS de largura fixa,
/// codificadas em <b>ISO-8859-1 (Latin-1)</b> com terminador <b>CR/LF</b> (MT Vol. V §2). Aplica
/// alinhamento, preenchimento (espaços/zeros) e sinal de valor conforme o <see cref="CampoLeiaute"/>.
/// Usa <c>Span&lt;char&gt;</c> para montar a linha sem alocação intermediária (CLAUDE.md §7).
/// </summary>
/// <remarks>
/// CONFIANÇA: ALTA quanto ao FORMATO (Latin-1, largura fixa, CR/LF, tipos Caractere/Numérico/Valor/Data,
/// sinal +/-, finalizador). INCERTO quanto à grade EXATA de campos de 2026 — a grade vem do catálogo
/// versionado, marcada <c>// TODO(validar-leiaute-MT-2026)</c>. Este emissor é genérico: NÃO embute
/// posição/tamanho de campo algum.
/// </remarks>
public static class EmissorRegistroSiapc
{
    private const char Espaco = ' ';
    private const char Zero = '0';
    private const char SinalPositivo = '+';
    private const char SinalNegativo = '-';

    /// <summary>Codificação canônica das remessas SIAPC/PAD: ISO-8859-1 (Latin-1).</summary>
    public static readonly Encoding EncodingSiapc = Encoding.Latin1;

    /// <summary>Terminador de linha das remessas SIAPC/PAD: CR/LF (0x0D 0x0A).</summary>
    public const string TerminadorLinha = "\r\n";

    /// <summary>
    /// Serializa uma linha posicional para o registro informado, casando cada <see cref="CampoLeiaute"/>
    /// (na ordem da grade) com o <see cref="ValorCampo"/> correspondente por nome.
    /// </summary>
    /// <param name="definicao">Definição do registro (grade de campos).</param>
    /// <param name="valores">Valores por nome de campo.</param>
    /// <returns>A linha de largura fixa (sem terminador), pronta para concatenação.</returns>
    /// <exception cref="ArgumentNullException">Se definição ou valores forem nulos.</exception>
    /// <exception cref="InvalidOperationException">Se faltar valor para um campo da grade.</exception>
    /// <exception cref="OverflowException">Se um valor não couber na largura do campo.</exception>
    public static string SerializarLinha(RegistroLeiauteDef definicao, IReadOnlyDictionary<string, ValorCampo> valores)
    {
        ArgumentNullException.ThrowIfNull(definicao);
        ArgumentNullException.ThrowIfNull(valores);

        var largura = definicao.LarguraLinha;
        Span<char> linha = largura <= 512 ? stackalloc char[largura] : new char[largura];
        linha.Fill(Espaco);

        foreach (var campo in definicao.Campos)
        {
            if (!valores.TryGetValue(campo.Nome, out var valor))
            {
                throw new InvalidOperationException(
                    $"Valor ausente para o campo '{campo.Nome}' do registro '{definicao.CodigoRegistro}'.");
            }

            // PosicaoInicial é 1-based; o span é 0-based.
            var destino = linha.Slice(campo.PosicaoInicial - 1, campo.Tamanho);
            EscreverCampo(campo, valor, destino);
        }

        return new string(linha);
    }

    /// <summary>
    /// Serializa o corpo completo de um arquivo (várias linhas do mesmo registro) já em bytes ISO-8859-1,
    /// com terminador CR/LF por linha.
    /// </summary>
    /// <param name="definicao">Definição do registro.</param>
    /// <param name="linhas">Conjuntos de valores (uma linha cada).</param>
    /// <returns>Bytes ISO-8859-1 do corpo (sem cabeçalho/finalizador).</returns>
    public static byte[] SerializarCorpo(
        RegistroLeiauteDef definicao,
        IReadOnlyList<IReadOnlyDictionary<string, ValorCampo>> linhas)
    {
        ArgumentNullException.ThrowIfNull(definicao);
        ArgumentNullException.ThrowIfNull(linhas);

        var construtor = new StringBuilder();
        foreach (var valores in linhas)
        {
            construtor.Append(SerializarLinha(definicao, valores));
            construtor.Append(TerminadorLinha);
        }

        return EncodingSiapc.GetBytes(construtor.ToString());
    }

    /// <summary>Codifica um texto qualquer (ex.: cabeçalho/finalizador já montados) em bytes ISO-8859-1.</summary>
    /// <param name="texto">Texto a codificar.</param>
    /// <returns>Bytes ISO-8859-1.</returns>
    public static byte[] Codificar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        return EncodingSiapc.GetBytes(texto);
    }

    private static void EscreverCampo(CampoLeiaute campo, ValorCampo valor, Span<char> destino)
    {
        switch (campo.Tipo)
        {
            case TipoCampoLeiaute.Caractere:
                EscreverCaractere(campo, valor.Texto ?? string.Empty, destino);
                break;
            case TipoCampoLeiaute.Numerico:
                EscreverNumerico(campo, valor.Numero ?? 0, destino);
                break;
            case TipoCampoLeiaute.Valor:
                EscreverValor(campo, valor.ValorMonetario ?? 0m, destino);
                break;
            case TipoCampoLeiaute.Data:
                EscreverData(valor.Data, destino);
                break;
            default:
                throw new InvalidOperationException($"Tipo de campo não suportado: {campo.Tipo}.");
        }
    }

    private static void EscreverCaractere(CampoLeiaute campo, string texto, Span<char> destino)
    {
        // Caractere: alinhado à ESQUERDA, completado com ESPAÇOS à direita; trunca se exceder.
        var fonte = texto.AsSpan();
        if (fonte.Length > destino.Length)
        {
            fonte = fonte[..destino.Length];
        }

        destino.Fill(Espaco);
        fonte.CopyTo(destino);
        _ = campo;
    }

    private static void EscreverNumerico(CampoLeiaute campo, long numero, Span<char> destino)
    {
        // Numérico: alinhado à DIREITA, completado com ZEROS à esquerda. Não cabe ⇒ erro (sem truncar dígito).
        Span<char> digitos = stackalloc char[20];
        if (!numero.TryFormat(digitos, out var escritos, provider: CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException($"Falha ao formatar o campo numérico '{campo.Nome}'.");
        }

        var texto = digitos[..escritos];
        if (texto.Length > destino.Length)
        {
            throw new OverflowException(
                $"Valor numérico do campo '{campo.Nome}' ({numero}) não cabe em {destino.Length} posições.");
        }

        destino.Fill(Zero);
        texto.CopyTo(destino[(destino.Length - texto.Length)..]);
    }

    private static void EscreverValor(CampoLeiaute campo, decimal valor, Span<char> destino)
    {
        // Valor: 1ª posição = SINAL (+/-); restante = CENTAVOS (sem ponto), à direita com ZEROS à esquerda.
        if (destino.Length < 2)
        {
            throw new OverflowException(
                $"Campo de valor '{campo.Nome}' precisa de ao menos 2 posições (sinal + 1 dígito).");
        }

        var sinal = valor < 0 ? SinalNegativo : SinalPositivo;
        var centavos = (long)decimal.Round(Math.Abs(valor) * 100m, 0, MidpointRounding.AwayFromZero);

        Span<char> digitos = stackalloc char[20];
        if (!centavos.TryFormat(digitos, out var escritos, provider: CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException($"Falha ao formatar o campo de valor '{campo.Nome}'.");
        }

        var texto = digitos[..escritos];
        var capacidadeDigitos = destino.Length - 1;
        if (texto.Length > capacidadeDigitos)
        {
            throw new OverflowException(
                $"Valor monetário do campo '{campo.Nome}' ({valor}) não cabe em {capacidadeDigitos} dígitos.");
        }

        destino[0] = sinal;
        var corpo = destino[1..];
        corpo.Fill(Zero);
        texto.CopyTo(corpo[(corpo.Length - texto.Length)..]);
    }

    private static void EscreverData(DateOnly? data, Span<char> destino)
    {
        // Data: ddmmaaaa (8 posições). Sem data ⇒ ZEROS (campo "00000000").
        if (data is null)
        {
            destino.Fill(Zero);
            return;
        }

        var valor = data.Value;
        Span<char> formatada = stackalloc char[8];
        formatada[0] = (char)(Zero + valor.Day / 10);
        formatada[1] = (char)(Zero + valor.Day % 10);
        formatada[2] = (char)(Zero + valor.Month / 10);
        formatada[3] = (char)(Zero + valor.Month % 10);
        var ano = valor.Year;
        formatada[4] = (char)(Zero + ano / 1000 % 10);
        formatada[5] = (char)(Zero + ano / 100 % 10);
        formatada[6] = (char)(Zero + ano / 10 % 10);
        formatada[7] = (char)(Zero + ano % 10);
        formatada.CopyTo(destino);
    }
}
