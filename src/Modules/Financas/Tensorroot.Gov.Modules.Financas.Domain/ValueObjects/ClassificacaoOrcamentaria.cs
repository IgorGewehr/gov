using System.Text.Json.Serialization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

/// <summary>
/// Natureza econômica da despesa (Lei 4.320/64, anexo). Os códigos seguem o
/// 1º dígito da classificação por categoria econômica.
/// </summary>
public enum CategoriaEconomica
{
    /// <summary>Despesas Correntes (custeio).</summary>
    DespesasCorrentes = 3,

    /// <summary>Despesas de Capital (investimentos/inversões/amortização).</summary>
    DespesasDeCapital = 4,
}

/// <summary>
/// Classificação orçamentária do crédito (Lei 4.320/64): identifica órgão,
/// unidade, funcional-programática, categoria econômica e fonte de recurso.
/// </summary>
public sealed class ClassificacaoOrcamentaria : ValueObject
{
    // [JsonConstructor]: permite a reidratacao do VO pelo System.Text.Json no round-trip do
    // Outbox (eventos de dominio que carregam a classificacao). Os nomes dos parametros casam
    // com as propriedades. Mantem o VO imutavel e o construtor privado (dominio rico preservado).
    [JsonConstructor]
    private ClassificacaoOrcamentaria(
        string orgao,
        string unidadeOrcamentaria,
        string funcionalProgramatica,
        CategoriaEconomica categoriaEconomica,
        string fonteDeRecurso)
    {
        Orgao = orgao;
        UnidadeOrcamentaria = unidadeOrcamentaria;
        FuncionalProgramatica = funcionalProgramatica;
        CategoriaEconomica = categoriaEconomica;
        FonteDeRecurso = fonteDeRecurso;
    }

    /// <summary>Código do órgão (ex.: "02").</summary>
    public string Orgao { get; private set; } = default!;

    /// <summary>Código da unidade orçamentária (ex.: "0201").</summary>
    public string UnidadeOrcamentaria { get; private set; } = default!;

    /// <summary>Funcional-programática (função/subfunção/programa/ação, ex.: "04.122.0002.2010").</summary>
    public string FuncionalProgramatica { get; private set; } = default!;

    /// <summary>Categoria econômica da despesa.</summary>
    public CategoriaEconomica CategoriaEconomica { get; private set; }

    /// <summary>Fonte de recurso (ex.: "0001" recursos livres).</summary>
    public string FonteDeRecurso { get; private set; } = default!;

    /// <summary>Cria uma classificação orçamentária válida.</summary>
    /// <param name="orgao">Código do órgão.</param>
    /// <param name="unidadeOrcamentaria">Código da unidade orçamentária.</param>
    /// <param name="funcionalProgramatica">Funcional-programática.</param>
    /// <param name="categoriaEconomica">Categoria econômica.</param>
    /// <param name="fonteDeRecurso">Fonte de recurso.</param>
    /// <returns>Instância de <see cref="ClassificacaoOrcamentaria"/>.</returns>
    /// <exception cref="ArgumentException">Se algum campo estiver vazio ou fora de formato.</exception>
    // TODO(revisao-contabil): validacao fina dos digitos da funcional-programatica e tabela de fontes
    // conforme TCE-RS/STN fica para o PCASP detalhado.
    public static ClassificacaoOrcamentaria De(
        string orgao,
        string unidadeOrcamentaria,
        string funcionalProgramatica,
        CategoriaEconomica categoriaEconomica,
        string fonteDeRecurso)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orgao);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeOrcamentaria);
        ArgumentException.ThrowIfNullOrWhiteSpace(funcionalProgramatica);
        ArgumentException.ThrowIfNullOrWhiteSpace(fonteDeRecurso);

        if (!Enum.IsDefined(categoriaEconomica))
        {
            throw new ArgumentException("Categoria economica invalida.", nameof(categoriaEconomica));
        }

        return new ClassificacaoOrcamentaria(
            orgao.Trim(),
            unidadeOrcamentaria.Trim(),
            funcionalProgramatica.Trim(),
            categoriaEconomica,
            fonteDeRecurso.Trim());
    }

    /// <summary>Representação textual concatenada da dotação.</summary>
    /// <returns>Texto no formato "orgao.unidade.funcional.fonte".</returns>
    public string ParaTexto()
        => $"{Orgao}.{UnidadeOrcamentaria}.{FuncionalProgramatica}.{(int)CategoriaEconomica}.{FonteDeRecurso}";

    /// <inheritdoc />
    public override string ToString() => ParaTexto();

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Orgao;
        yield return UnidadeOrcamentaria;
        yield return FuncionalProgramatica;
        yield return CategoriaEconomica;
        yield return FonteDeRecurso;
    }
}
