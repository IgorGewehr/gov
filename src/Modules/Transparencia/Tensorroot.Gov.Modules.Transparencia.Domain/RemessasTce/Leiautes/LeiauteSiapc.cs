using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Especificação versionada (por exercício) do leiaute SIAPC/PAD: o conjunto de
/// <see cref="RegistroLeiauteDef"/> (um por arquivo físico), o tipo de Setor de Governo e as regras de
/// cabeçalho/finalizador. É a referência de DADOS que dirige a emissão posicional — nunca constante em
/// C# (CLAUDE.md §7/§16). Igualdade por (<see cref="Codigo"/>, <see cref="Versao"/>).
/// </summary>
/// <remarks>
/// O leiaute conferido é de 2010 e muda em 2026 (novos Balanço Patrimonial/Financeiro + DFC). A grade
/// concreta de campos é carregada de configuração versionada na Infrastructure; aqui o agregado de
/// referência apenas modela a estrutura. Campos/posições exatos: <c>// TODO(validar-leiaute-MT-2026)</c>.
/// </remarks>
public sealed class LeiauteSiapc : ValueObject
{
    private readonly List<RegistroLeiauteDef> _registros;

    private LeiauteSiapc(
        string codigo,
        string versao,
        char tipoSetorGoverno,
        IReadOnlyList<RegistroLeiauteDef> registros)
    {
        Codigo = codigo;
        Versao = versao;
        TipoSetorGoverno = tipoSetorGoverno;
        _registros = [.. registros];
    }

    /// <summary>Código do leiaute (ex.: "SIAPC"; não vazio).</summary>
    public string Codigo { get; }

    /// <summary>Versão/exercício do leiaute (ex.: "2026"; não vazio).</summary>
    public string Versao { get; }

    /// <summary>
    /// Tipo de Setor de Governo (1 caractere): <c>P</c>=Prefeitura, <c>C</c>=Câmara, <c>A</c>, <c>F</c>,
    /// <c>E</c>, <c>S</c>, <c>O</c>. Compõe o nome do ZIP e o cabeçalho.
    /// </summary>
    public char TipoSetorGoverno { get; }

    /// <summary>Definições de registro/arquivo do leiaute (um por arquivo físico).</summary>
    public IReadOnlyList<RegistroLeiauteDef> Registros => _registros;

    /// <summary>Converte para o objeto de valor leve <see cref="Leiaute"/> (código + versão) do agregado.</summary>
    /// <returns>Instância de <see cref="Leiaute"/>.</returns>
    public Leiaute ParaLeiaute() => Leiaute.De(Codigo, Versao);

    /// <summary>Resolve a definição de registro pelo nome do arquivo (case-insensitive).</summary>
    /// <param name="nomeArquivo">Nome do arquivo físico.</param>
    /// <returns>A definição, ou <c>null</c> se não houver.</returns>
    public RegistroLeiauteDef? RegistroPorArquivo(string nomeArquivo)
        => _registros.FirstOrDefault(
            registro => string.Equals(registro.NomeArquivo, nomeArquivo, StringComparison.OrdinalIgnoreCase));

    /// <summary>Define um leiaute SIAPC versionado.</summary>
    /// <param name="codigo">Código do leiaute (não vazio).</param>
    /// <param name="versao">Versão/exercício (não vazio).</param>
    /// <param name="tipoSetorGoverno">Tipo de Setor de Governo (1 caractere; ex.: 'P').</param>
    /// <param name="registros">Definições de registro/arquivo (não vazias).</param>
    /// <returns>Instância de <see cref="LeiauteSiapc"/>.</returns>
    /// <exception cref="ArgumentException">Se código/versão forem vazios ou não houver registros.</exception>
    public static LeiauteSiapc Definir(
        string codigo,
        string versao,
        char tipoSetorGoverno,
        IReadOnlyList<RegistroLeiauteDef> registros)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(versao);
        ArgumentNullException.ThrowIfNull(registros);

        if (registros.Count == 0)
        {
            throw new ArgumentException("Um leiaute deve ter ao menos um registro/arquivo.", nameof(registros));
        }

        if (!char.IsLetter(tipoSetorGoverno))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoSetorGoverno), tipoSetorGoverno, "Tipo de Setor de Governo deve ser uma letra (ex.: 'P').");
        }

        return new LeiauteSiapc(codigo, versao, char.ToUpperInvariant(tipoSetorGoverno), registros);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
        yield return Versao;
    }
}
