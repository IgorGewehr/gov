using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>Identificador forte de um <see cref="HistoricoDepreciacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct HistoricoDepreciacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="HistoricoDepreciacaoId"/>.</returns>
    public static HistoricoDepreciacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Registro, por competência, da depreciação reconhecida e do valor contábil resultante.</summary>
public sealed class HistoricoDepreciacao : Entity<HistoricoDepreciacaoId>
{
    private HistoricoDepreciacao()
    {
    }

    private HistoricoDepreciacao(
        HistoricoDepreciacaoId id,
        DateOnly competencia,
        decimal valorDepreciado,
        ValorMonetario valorContabilResultante)
        : base(id)
    {
        Competencia = competencia;
        ValorDepreciado = valorDepreciado;
        ValorContabilResultante = valorContabilResultante;
    }

    /// <summary>Competência (mês/ano) do reconhecimento.</summary>
    public DateOnly Competencia { get; private set; }

    /// <summary>Valor depreciado na competência.</summary>
    public decimal ValorDepreciado { get; private set; }

    /// <summary>Valor contábil resultante após a depreciação.</summary>
    public ValorMonetario ValorContabilResultante { get; private set; } = default!;

    /// <summary>Registra a depreciação de uma competência.</summary>
    /// <param name="competencia">Competência (mês/ano).</param>
    /// <param name="valorDepreciado">Valor depreciado.</param>
    /// <param name="valorContabilResultante">Valor contábil resultante.</param>
    /// <returns>Novo <see cref="HistoricoDepreciacao"/>.</returns>
    public static HistoricoDepreciacao Registrar(
        DateOnly competencia,
        decimal valorDepreciado,
        ValorMonetario valorContabilResultante)
    {
        ArgumentNullException.ThrowIfNull(valorContabilResultante);
        return new HistoricoDepreciacao(HistoricoDepreciacaoId.New(), competencia, valorDepreciado, valorContabilResultante);
    }
}
