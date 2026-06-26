using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>Identificador forte da <see cref="TabelaIrrf"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaIrrfId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaIrrfId"/>.</returns>
    public static TabelaIrrfId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tabela progressiva mensal do IRRF, versionada por competencia/tenant. Inclui as faixas, a deducao
/// por dependente e o desconto simplificado opcional (regra do mais vantajoso). Sao PARAMETROS LEGAIS
/// por exercicio (Receita Federal) — NUNCA hardcoded no motor (CLAUDE.md S7/S16). Raiz de agregado.
/// </summary>
public sealed class TabelaIrrf : AggregateRoot<TabelaIrrfId>, IMustHaveTenant
{
    private readonly List<FaixaIrrf> _faixas = [];

    private TabelaIrrf()
    {
    }

    private TabelaIrrf(
        TabelaIrrfId id,
        Guid tenantId,
        Competencia vigenciaInicio,
        IReadOnlyList<FaixaIrrf> faixas,
        decimal deducaoPorDependente,
        decimal descontoSimplificado,
        string baseLegal,
        RedutorIrrf? redutor)
        : base(id)
    {
        TenantId = tenantId;
        VigenciaInicio = vigenciaInicio;
        _faixas.AddRange(faixas);
        DeducaoPorDependente = deducaoPorDependente;
        DescontoSimplificado = descontoSimplificado;
        BaseLegal = baseLegal;
        Redutor = redutor;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Competencia inicial de vigencia (AAAA-MM).</summary>
    public Competencia VigenciaInicio { get; private set; } = default!;

    /// <summary>Deducao mensal por dependente.</summary>
    public decimal DeducaoPorDependente { get; private set; }

    /// <summary>Desconto simplificado mensal (regra do mais vantajoso); zero quando inaplicavel.</summary>
    public decimal DescontoSimplificado { get; private set; }

    /// <summary>Fundamento legal (ex.: Lei/IN da Receita) para auditoria/TCE.</summary>
    public string BaseLegal { get; private set; } = default!;

    /// <summary>
    /// Redutor mensal do IRRF (Lei 15.270/2025, vigencia 2026); nulo nas competencias sem redutor.
    /// Parametro legal por exercicio — aplicado APOS a tabela progressiva e limitado ao imposto apurado.
    /// </summary>
    public RedutorIrrf? Redutor { get; private set; }

    /// <summary>Faixas progressivas ordenadas por limite (somente leitura).</summary>
    public IReadOnlyList<FaixaIrrf> Faixas => _faixas;

    /// <summary>Cria uma tabela IRRF valida para uma competencia inicial.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="vigenciaInicio">Competencia inicial de vigencia.</param>
    /// <param name="faixas">Faixas progressivas ordenadas por limite superior crescente.</param>
    /// <param name="deducaoPorDependente">Deducao mensal por dependente.</param>
    /// <param name="descontoSimplificado">Desconto simplificado mensal (zero se inaplicavel).</param>
    /// <param name="baseLegal">Fundamento legal.</param>
    /// <param name="redutor">Redutor mensal do IRRF (Lei 15.270/2025); nulo nas competencias sem redutor.</param>
    /// <returns>Nova <see cref="TabelaIrrf"/>.</returns>
    /// <exception cref="ArgumentException">Se faixas/base legal forem invalidas.</exception>
    public static TabelaIrrf Criar(
        Guid tenantId,
        Competencia vigenciaInicio,
        IReadOnlyList<FaixaIrrf> faixas,
        decimal deducaoPorDependente,
        decimal descontoSimplificado,
        string baseLegal,
        RedutorIrrf? redutor = null)
    {
        ArgumentNullException.ThrowIfNull(vigenciaInicio);
        ArgumentNullException.ThrowIfNull(faixas);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseLegal);
        ArgumentOutOfRangeException.ThrowIfNegative(deducaoPorDependente);
        ArgumentOutOfRangeException.ThrowIfNegative(descontoSimplificado);
        if (faixas.Count == 0)
        {
            throw new ArgumentException("Tabela IRRF exige ao menos uma faixa.", nameof(faixas));
        }

        for (var i = 1; i < faixas.Count; i++)
        {
            if (faixas[i].LimiteSuperior <= faixas[i - 1].LimiteSuperior)
            {
                throw new ArgumentException("As faixas do IRRF devem estar ordenadas por limite superior crescente.", nameof(faixas));
            }
        }

        return new TabelaIrrf(
            TabelaIrrfId.New(),
            tenantId,
            vigenciaInicio,
            faixas,
            deducaoPorDependente,
            descontoSimplificado,
            baseLegal.Trim(),
            redutor);
    }

    /// <summary>
    /// Calcula o IRRF mensal aplicando a regra do mais vantajoso: compara a base com deducoes legais
    /// (INSS/RPPS + dependentes + pensao) contra a base com desconto simplificado e usa a MENOR base;
    /// enquadra na faixa e aplica aliquota menos parcela a deduzir. Deterministico.
    /// </summary>
    /// <param name="rendimentoTributavel">Soma das rubricas tributaveis pelo IRRF.</param>
    /// <param name="descontoPrevidenciario">INSS/RPPS retido (deducao legal).</param>
    /// <param name="quantidadeDependentes">Quantidade de dependentes para deducao.</param>
    /// <param name="pensaoAlimenticia">Pensao alimenticia dedutivel.</param>
    /// <param name="aplicarSimplificado">
    /// Quando <c>true</c> (default, comportamento mensal), aplica a regra do mais vantajoso usando o
    /// desconto simplificado. Quando <c>false</c>, o desconto simplificado e VEDADO — caso do IRRF do
    /// 13o salario, tributacao exclusiva na fonte (Lei 7.713/88 art. 12-A; IN RFB 1.500/2014). Acrescimo
    /// ADITIVO e parametrizado: nao altera o calculo mensal e nao embute numeros no motor (design §2.2).
    /// </param>
    /// <returns>Valor do IRRF (2 casas), nao-negativo.</returns>
    public decimal CalcularImposto(
        decimal rendimentoTributavel,
        decimal descontoPrevidenciario,
        int quantidadeDependentes,
        decimal pensaoAlimenticia,
        bool aplicarSimplificado = true)
    {
        if (rendimentoTributavel <= 0m)
        {
            return 0m;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(descontoPrevidenciario);
        ArgumentOutOfRangeException.ThrowIfNegative(quantidadeDependentes);
        ArgumentOutOfRangeException.ThrowIfNegative(pensaoAlimenticia);

        // Base com deducoes legais (modelo completo).
        var deducoesLegais = descontoPrevidenciario
            + (quantidadeDependentes * DeducaoPorDependente)
            + pensaoAlimenticia;
        var baseCompleta = rendimentoTributavel - deducoesLegais;

        // Regra do mais vantajoso (desconto simplificado) — VEDADA no 13o (art. 12-A): quando
        // aplicarSimplificado=false, usa-se exclusivamente a base com deducoes legais.
        var baseImposto = aplicarSimplificado
            ? Math.Min(baseCompleta, rendimentoTributavel - DescontoSimplificado)
            : baseCompleta;
        if (baseImposto <= 0m)
        {
            return 0m;
        }

        var faixa = _faixas.FirstOrDefault(f => f.Enquadra(baseImposto)) ?? _faixas[^1];
        var imposto = faixa.Imposto(baseImposto);

        // Redutor mensal (Lei 15.270/2025, vigencia 2026): aplicado APOS a tabela e limitado ao proprio
        // imposto apurado (art. 3o-A, § 1o). Incide sobre o rendimento tributavel BRUTO (antes das
        // deducoes), conforme orientacao RFB. Nulo nas competencias anteriores (sem alteracao de calculo).
        // Tambem aplicavel ao IRRF do 13o (§ 3o), independente da regra do desconto simplificado mensal.
        if (Redutor is { } redutor && imposto > 0m)
        {
            var valorRedutor = Math.Min(imposto, redutor.Calcular(rendimentoTributavel));
            imposto = decimal.Round(imposto - valorRedutor, 2, MidpointRounding.AwayFromZero);
        }

        return imposto;
    }
}
