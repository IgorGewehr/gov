using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Definição de um TIPO de registro/arquivo do leiaute SIAPC/PAD: o código do registro, o nome do
/// arquivo físico (ex.: <c>EMPENHO.TXT</c>, <c>TCE_4810.TXT</c>) e a grade ordenada de
/// <see cref="CampoLeiaute"/> que compõem cada linha de largura fixa. Objeto de valor (definição imutável,
/// versionada por <see cref="LeiauteSiapc"/>).
/// </summary>
/// <remarks>
/// A grade é DIRIGIDA POR DADOS (catálogo versionado na Infrastructure), nunca constante em C#
/// (CLAUDE.md §7/§16). Marcar <c>// TODO(validar-leiaute-MT-2026)</c> em qualquer campo cujo
/// posição/tamanho/tipo exato dependa da publicação do MT SIAPC 2026.
/// </remarks>
public sealed class RegistroLeiauteDef : ValueObject
{
    private readonly List<CampoLeiaute> _campos;

    private RegistroLeiauteDef(string codigoRegistro, string nomeArquivo, IReadOnlyList<CampoLeiaute> campos)
    {
        CodigoRegistro = codigoRegistro;
        NomeArquivo = nomeArquivo;
        _campos = [.. campos];
    }

    /// <summary>Código do registro conforme o leiaute (ex.: "10", "EMPENHO"; não vazio).</summary>
    public string CodigoRegistro { get; }

    /// <summary>Nome do arquivo físico gerado (ex.: "EMPENHO.TXT"; não vazio, em MAIÚSCULAS).</summary>
    public string NomeArquivo { get; }

    /// <summary>Grade ordenada de campos posicionais da linha.</summary>
    public IReadOnlyList<CampoLeiaute> Campos => _campos;

    /// <summary>Largura total da linha (em colunas/bytes), derivada da última posição final dos campos.</summary>
    public int LarguraLinha => _campos.Count == 0 ? 0 : _campos.Max(campo => campo.PosicaoFinal);

    /// <summary>
    /// Define um tipo de registro/arquivo, validando que os campos não se sobreponham e formem uma
    /// faixa contígua a partir da posição 1 (sem buracos), de modo que a largura fixa seja determinística.
    /// </summary>
    /// <param name="codigoRegistro">Código do registro (não vazio).</param>
    /// <param name="nomeArquivo">Nome do arquivo físico (não vazio).</param>
    /// <param name="campos">Grade de campos (não vazia).</param>
    /// <returns>Instância de <see cref="RegistroLeiauteDef"/>.</returns>
    /// <exception cref="ArgumentException">Se código/nome forem vazios ou a grade for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se os campos se sobrepuserem ou deixarem buracos.</exception>
    public static RegistroLeiauteDef Definir(
        string codigoRegistro,
        string nomeArquivo,
        IReadOnlyList<CampoLeiaute> campos)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoRegistro);
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeArquivo);
        ArgumentNullException.ThrowIfNull(campos);

        if (campos.Count == 0)
        {
            throw new ArgumentException("Um registro de leiaute deve ter ao menos um campo.", nameof(campos));
        }

        var ordenados = campos.OrderBy(campo => campo.PosicaoInicial).ToList();
        var proximaPosicao = 1;
        foreach (var campo in ordenados)
        {
            if (campo.PosicaoInicial != proximaPosicao)
            {
                throw new InvalidOperationException(
                    $"Grade do registro '{codigoRegistro}' inconsistente: campo '{campo.Nome}' inicia em "
                    + $"{campo.PosicaoInicial}, esperado {proximaPosicao} (faixa contígua, sem sobreposição/buraco).");
            }

            proximaPosicao = campo.PosicaoFinal + 1;
        }

        return new RegistroLeiauteDef(codigoRegistro, nomeArquivo.ToUpperInvariant(), ordenados);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CodigoRegistro;
        yield return NomeArquivo;
        foreach (var campo in _campos)
        {
            yield return campo;
        }
    }
}
