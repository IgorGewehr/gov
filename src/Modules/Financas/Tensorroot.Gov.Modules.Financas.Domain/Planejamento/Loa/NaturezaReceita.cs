using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>
/// Natureza/classificação da receita orçamentária (Lei 4.320/64; Portaria Interministerial
/// STN/SOF). Codifica categoria econômica, origem, espécie e rubrica. Value Object.
/// </summary>
public sealed class NaturezaReceita : ValueObject
{
    private NaturezaReceita(CategoriaEconomicaReceita categoria, string origem, string especie, string rubrica)
    {
        Categoria = categoria;
        Origem = origem;
        Especie = especie;
        Rubrica = rubrica;
    }

    /// <summary>Categoria econômica (Corrente/Capital).</summary>
    public CategoriaEconomicaReceita Categoria { get; private set; }

    /// <summary>Origem (ex.: "1" Impostos; código livre conforme tabela STN).</summary>
    public string Origem { get; private set; } = default!;

    /// <summary>Espécie da receita.</summary>
    public string Especie { get; private set; } = default!;

    /// <summary>Rubrica/código completo da natureza de receita.</summary>
    public string Rubrica { get; private set; } = default!;

    /// <summary>Cria uma natureza de receita válida.</summary>
    /// <param name="categoria">Categoria econômica.</param>
    /// <param name="origem">Origem.</param>
    /// <param name="especie">Espécie.</param>
    /// <param name="rubrica">Rubrica/código.</param>
    /// <returns>Instância de <see cref="NaturezaReceita"/>.</returns>
    /// <exception cref="ArgumentException">Se a categoria for inválida ou campos vazios.</exception>
    // TODO(validar-oficial): validar Origem/Especie/Rubrica contra a tabela vigente de natureza de
    // receita (STN) — hoje string livre, no mesmo padrao da funcional-programatica da execucao.
    public static NaturezaReceita De(CategoriaEconomicaReceita categoria, string origem, string especie, string rubrica)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origem);
        ArgumentException.ThrowIfNullOrWhiteSpace(especie);
        ArgumentException.ThrowIfNullOrWhiteSpace(rubrica);
        if (!Enum.IsDefined(categoria))
        {
            throw new ArgumentException("Categoria economica da receita invalida.", nameof(categoria));
        }

        return new NaturezaReceita(categoria, origem.Trim(), especie.Trim(), rubrica.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Categoria;
        yield return Origem;
        yield return Especie;
        yield return Rubrica;
    }
}

/// <summary>Categoria econômica da receita (Lei 4.320/64, anexo).</summary>
public enum CategoriaEconomicaReceita
{
    /// <summary>Receitas Correntes.</summary>
    ReceitasCorrentes = 1,

    /// <summary>Receitas de Capital.</summary>
    ReceitasDeCapital = 2,
}
