using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>
/// Estimativa de receita por natureza e fonte (Lei 4.320/64 art. 2º — a LOA estima a receita).
/// Entidade-filha de <see cref="LeiOrcamentariaAnual"/>.
/// </summary>
public sealed class ReceitaPrevista : Entity<ReceitaPrevistaId>
{
    private ReceitaPrevista()
    {
    }

    private ReceitaPrevista(ReceitaPrevistaId id, LoaId loaId, NaturezaReceita natureza, string fonteDeRecurso, ValorMonetario valorPrevisto)
        : base(id)
    {
        LoaId = loaId;
        Natureza = natureza;
        FonteDeRecurso = fonteDeRecurso;
        ValorPrevisto = valorPrevisto;
    }

    /// <summary>LOA à qual a receita prevista pertence.</summary>
    public LoaId LoaId { get; private set; }

    /// <summary>Natureza/classificação da receita.</summary>
    public NaturezaReceita Natureza { get; private set; } = default!;

    /// <summary>Fonte de recurso (ex.: "0001" recursos livres).</summary>
    public string FonteDeRecurso { get; private set; } = default!;

    /// <summary>Valor previsto.</summary>
    public ValorMonetario ValorPrevisto { get; private set; } = default!;

    /// <summary>Cria uma receita prevista válida.</summary>
    /// <returns>Nova <see cref="ReceitaPrevista"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor previsto não for positivo.</exception>
    internal static ReceitaPrevista Criar(LoaId loaId, NaturezaReceita natureza, string fonteDeRecurso, ValorMonetario valorPrevisto)
    {
        ArgumentNullException.ThrowIfNull(natureza);
        ArgumentNullException.ThrowIfNull(valorPrevisto);
        ArgumentException.ThrowIfNullOrWhiteSpace(fonteDeRecurso);
        if (!valorPrevisto.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valorPrevisto), "Valor previsto deve ser positivo.");
        }

        return new ReceitaPrevista(ReceitaPrevistaId.New(), loaId, natureza, fonteDeRecurso.Trim(), valorPrevisto);
    }
}
