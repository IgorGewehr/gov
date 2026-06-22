using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>Identificador forte de um <see cref="RegistroLeiaute"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroLeiauteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroLeiauteId"/>.</returns>
    public static RegistroLeiauteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha/registro estruturado de um <see cref="ArquivoRemessa"/>, conforme a especificação do
/// leiaute (entidade-filha). O fatiamento de arquivos pesados é feito com <c>Span&lt;T&gt;</c>/
/// <c>Memory&lt;T&gt;</c>, sem alocação desnecessária.
/// </summary>
public sealed class RegistroLeiaute : Entity<RegistroLeiauteId>
{
    private RegistroLeiaute()
    {
    }

    private RegistroLeiaute(RegistroLeiauteId id, string tipo, string conteudo)
        : base(id)
    {
        Tipo = tipo;
        Conteudo = conteudo;
    }

    /// <summary>Tipo do registro conforme o leiaute.</summary>
    public string Tipo { get; private set; } = default!;

    /// <summary>Linha estruturada conforme a especificação do leiaute.</summary>
    public string Conteudo { get; private set; } = default!;

    /// <summary>Cria um registro estruturado conforme o leiaute.</summary>
    /// <param name="tipo">Tipo do registro (não vazio).</param>
    /// <param name="conteudo">Conteúdo da linha (não nulo).</param>
    /// <returns>Novo <see cref="RegistroLeiaute"/>.</returns>
    /// <exception cref="ArgumentException">Se o tipo for vazio.</exception>
    /// <exception cref="ArgumentNullException">Se o conteúdo for nulo.</exception>
    public static RegistroLeiaute Criar(string tipo, string conteudo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tipo);
        ArgumentNullException.ThrowIfNull(conteudo);
        return new RegistroLeiaute(RegistroLeiauteId.New(), tipo, conteudo);
    }
}
