using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

/// <summary>Identificador forte de um <see cref="RegistroAula"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroAulaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroAulaId"/>.</returns>
    public static RegistroAulaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Entidade-filha do diario: conteudo ministrado em um dia. Quando <see cref="DiaLetivo"/>
/// e verdadeiro, conta para o minimo de 200 dias letivos do calendario (I-9).
/// </summary>
public sealed class RegistroAula : Entity<RegistroAulaId>
{
    private RegistroAula()
    {
    }

    private RegistroAula(RegistroAulaId id, DateOnly data, string conteudo, bool diaLetivo)
        : base(id)
    {
        Data = data;
        Conteudo = conteudo;
        DiaLetivo = diaLetivo;
    }

    /// <summary>Data da aula.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Conteudo ministrado.</summary>
    public string Conteudo { get; private set; } = default!;

    /// <summary>Indica se o dia conta como dia letivo (base dos 200 dias — I-9).</summary>
    public bool DiaLetivo { get; private set; }

    /// <summary>Registra uma aula/dia.</summary>
    /// <param name="data">Data da aula.</param>
    /// <param name="conteudo">Conteudo ministrado.</param>
    /// <param name="diaLetivo">Indica se conta como dia letivo.</param>
    /// <returns>Novo <see cref="RegistroAula"/>.</returns>
    /// <exception cref="ArgumentException">Se o conteudo for vazio.</exception>
    internal static RegistroAula Registrar(DateOnly data, string conteudo, bool diaLetivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conteudo);
        return new RegistroAula(RegistroAulaId.New(), data, conteudo, diaLetivo);
    }
}
