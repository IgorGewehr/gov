using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

/// <summary>Identificador forte de um <see cref="RegistroNota"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroNotaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroNotaId"/>.</returns>
    public static RegistroNotaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Entidade-filha do diario: nota de um componente curricular em um periodo. Toda nota
/// referencia um componente valido e um periodo informado (I-8). Auditavel de forma
/// imutavel (I-11).
/// </summary>
public sealed class RegistroNota : Entity<RegistroNotaId>
{
    private RegistroNota()
    {
    }

    private RegistroNota(RegistroNotaId id, ComponenteCurricularId componente, string periodo, decimal valor)
        : base(id)
    {
        Componente = componente;
        Periodo = periodo;
        Valor = valor;
    }

    /// <summary>Componente curricular (disciplina) da nota.</summary>
    public ComponenteCurricularId Componente { get; private set; }

    /// <summary>Periodo de avaliacao (ex.: "1Bim").</summary>
    public string Periodo { get; private set; } = default!;

    /// <summary>Valor da nota (0 a 10).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Lanca uma nota de um componente curricular em um periodo.</summary>
    /// <param name="componente">Componente curricular (nao vazio — I-8).</param>
    /// <param name="periodo">Periodo informado (nao vazio — I-8).</param>
    /// <param name="valor">Valor da nota.</param>
    /// <returns>Novo <see cref="RegistroNota"/>.</returns>
    /// <exception cref="ArgumentException">Se o componente for vazio ou o periodo nao for informado (I-8).</exception>
    internal static RegistroNota Lancar(ComponenteCurricularId componente, string periodo, decimal valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodo);
        if (componente.Value == Guid.Empty)
        {
            throw new ArgumentException("Componente curricular invalido.", nameof(componente));
        }

        // Invariante de domínio: nota no intervalo [0, 10]. Protege o cálculo de médias/aprovação
        // (DiarioClasse.MediasSuficientes) contra valores inválidos — não confia só na camada Application.
        ArgumentOutOfRangeException.ThrowIfLessThan(valor, 0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(valor, 10m);

        return new RegistroNota(RegistroNotaId.New(), componente, periodo, valor);
    }
}
