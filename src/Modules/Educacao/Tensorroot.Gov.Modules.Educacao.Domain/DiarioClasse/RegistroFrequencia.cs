using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

/// <summary>Identificador forte de um <see cref="RegistroFrequencia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroFrequenciaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroFrequenciaId"/>.</returns>
    public static RegistroFrequenciaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Entidade-filha do diario: presenca/falta do aluno em uma aula/dia, ponderada pela carga
/// horaria da aula. Base do calculo do percentual de frequencia (I-1). Auditavel de forma
/// imutavel (I-11).
/// </summary>
public sealed class RegistroFrequencia : Entity<RegistroFrequenciaId>
{
    private RegistroFrequencia()
    {
    }

    private RegistroFrequencia(RegistroFrequenciaId id, DateOnly data, bool presente, int cargaHorariaAula)
        : base(id)
    {
        Data = data;
        Presente = presente;
        CargaHorariaAula = cargaHorariaAula;
    }

    /// <summary>Data do registro de frequencia.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Indica se o aluno esteve presente.</summary>
    public bool Presente { get; private set; }

    /// <summary>Carga horaria da aula computada para a ponderacao da frequencia.</summary>
    public int CargaHorariaAula { get; private set; }

    /// <summary>Registra uma frequencia de aula/dia.</summary>
    /// <param name="data">Data do registro.</param>
    /// <param name="presente">Presenca do aluno.</param>
    /// <param name="cargaHorariaAula">Carga horaria da aula (positiva).</param>
    /// <returns>Novo <see cref="RegistroFrequencia"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a carga horaria nao for positiva.</exception>
    internal static RegistroFrequencia Registrar(DateOnly data, bool presente, int cargaHorariaAula)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cargaHorariaAula);
        return new RegistroFrequencia(RegistroFrequenciaId.New(), data, presente, cargaHorariaAula);
    }
}
