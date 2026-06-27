using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

/// <summary>Identificador forte do agregado <see cref="TabelaIrrfServicos"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaIrrfServicosId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaIrrfServicosId"/>.</returns>
    public static TabelaIrrfServicosId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Faixa (item) da tabela de IRRF sobre pagamentos a PJ — IN RFB 1.234/2012, Anexo I (Tabela 1).
/// Cada item liga uma natureza de bem/serviço a uma alíquota e ao código de receita (DARF) próprio.
/// É a tabela do IRRF "de fornecedores", DISTINTA da tabela progressiva da folha (RecursosHumanos).
/// </summary>
public sealed class FaixaIrrfServicos : ValueObject
{
    private FaixaIrrfServicos(string codigo, string descricao, decimal aliquota, string codigoReceitaDarf)
    {
        Codigo = codigo;
        Descricao = descricao;
        Aliquota = aliquota;
        CodigoReceitaDarf = codigoReceitaDarf;
    }

    /// <summary>Código curto do enquadramento (chave de negócio dentro da tabela).</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Descrição da natureza do bem/serviço.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Alíquota (fração — ex.: 0.012 = 1,2%).</summary>
    public decimal Aliquota { get; private set; }

    /// <summary>Código de receita do DARF (ex.: 6147, 9060, 6188, 6190, 6256).</summary>
    public string CodigoReceitaDarf { get; private set; } = default!;

    /// <summary>Cria uma faixa da tabela.</summary>
    /// <param name="codigo">Código curto do enquadramento.</param>
    /// <param name="descricao">Descrição da natureza.</param>
    /// <param name="aliquota">Alíquota (fração entre 0 e 1).</param>
    /// <param name="codigoReceitaDarf">Código de receita DARF.</param>
    /// <returns>Nova <see cref="FaixaIrrfServicos"/>.</returns>
    public static FaixaIrrfServicos De(string codigo, string descricao, decimal aliquota, string codigoReceitaDarf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoReceitaDarf);
        if (aliquota is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquota), aliquota, "Aliquota deve estar entre 0 e 1 (fracao).");
        }

        return new FaixaIrrfServicos(codigo.Trim(), descricao.Trim(), aliquota, codigoReceitaDarf.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
        yield return Aliquota;
        yield return CodigoReceitaDarf;
    }
}

/// <summary>
/// Tabela de IRRF sobre pagamentos a pessoa jurídica — IN RFB 1.234/2012 (alterada pela IN RFB
/// 2.145/2023, que estendeu a retenção a Estados e Municípios). Versionada por vigência (parametrizável
/// por tenant — CLAUDE.md §7: nada hardcoded). Dispensa de retenção abaixo do valor mínimo legal
/// (IN 1.234/2012, art. 3º, §6º — R$ 10,00 por DARF). Agregado de cadastro fiscal do ente.
/// </summary>
public sealed class TabelaIrrfServicos : AggregateRoot<TabelaIrrfServicosId>, IMustHaveTenant
{
    private readonly List<FaixaIrrfServicos> _faixas = [];

    private TabelaIrrfServicos()
    {
    }

    private TabelaIrrfServicos(
        TabelaIrrfServicosId id,
        Guid tenantId,
        DateOnly vigenciaInicio,
        DateOnly? vigenciaFim,
        decimal valorMinimoRetencao,
        IEnumerable<FaixaIrrfServicos> faixas)
        : base(id)
    {
        TenantId = tenantId;
        VigenciaInicio = vigenciaInicio;
        VigenciaFim = vigenciaFim;
        ValorMinimoRetencao = valorMinimoRetencao;
        _faixas.AddRange(faixas);
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Início da vigência da tabela.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Fim da vigência (nulo = vigente por prazo indeterminado).</summary>
    public DateOnly? VigenciaFim { get; private set; }

    /// <summary>
    /// Valor mínimo a reter por DARF abaixo do qual há dispensa de retenção
    /// (IN RFB 1.234/2012, art. 3º, §6º). Parametrizável (não hardcoded).
    /// </summary>
    public decimal ValorMinimoRetencao { get; private set; }

    /// <summary>Faixas (enquadramentos) da tabela.</summary>
    public IReadOnlyCollection<FaixaIrrfServicos> Faixas => _faixas.AsReadOnly();

    /// <summary>Cria uma tabela de IRRF de serviços vigente a partir de uma data.</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="vigenciaFim">Fim de vigência (opcional).</param>
    /// <param name="valorMinimoRetencao">Valor mínimo a reter (dispensa abaixo).</param>
    /// <param name="faixas">Faixas/enquadramentos.</param>
    /// <returns>Nova <see cref="TabelaIrrfServicos"/>.</returns>
    public static TabelaIrrfServicos Criar(
        Guid tenantId,
        DateOnly vigenciaInicio,
        DateOnly? vigenciaFim,
        decimal valorMinimoRetencao,
        IEnumerable<FaixaIrrfServicos> faixas)
    {
        ArgumentNullException.ThrowIfNull(faixas);
        ArgumentOutOfRangeException.ThrowIfNegative(valorMinimoRetencao);
        var lista = faixas.ToList();
        if (lista.Count == 0)
        {
            throw new ArgumentException("Tabela IRRF deve conter ao menos uma faixa.", nameof(faixas));
        }

        if (vigenciaFim is { } fim && fim < vigenciaInicio)
        {
            throw new ArgumentException("Fim de vigencia anterior ao inicio.", nameof(vigenciaFim));
        }

        return new TabelaIrrfServicos(TabelaIrrfServicosId.New(), tenantId, vigenciaInicio, vigenciaFim, valorMinimoRetencao, lista);
    }

    /// <summary>Indica se a tabela está vigente na data informada.</summary>
    /// <param name="data">Data de referência.</param>
    /// <returns><c>true</c> se vigente.</returns>
    public bool VigenteEm(DateOnly data) =>
        data >= VigenciaInicio && (VigenciaFim is null || data <= VigenciaFim);

    /// <summary>Obtém a faixa por código de enquadramento.</summary>
    /// <param name="codigo">Código do enquadramento.</param>
    /// <returns>A faixa, ou <c>null</c> se não houver.</returns>
    public FaixaIrrfServicos? ObterFaixa(string codigo) =>
        _faixas.FirstOrDefault(f => string.Equals(f.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Apura o IRRF/PJ de um pagamento conforme a faixa de enquadramento. Aplica a dispensa por valor
    /// mínimo (art. 3º, §6º): se o valor a reter for inferior ao mínimo, retém zero.
    /// </summary>
    /// <param name="codigoFaixa">Código do enquadramento (natureza do bem/serviço).</param>
    /// <param name="baseCalculo">Base de cálculo (valor bruto do documento/fatura).</param>
    /// <returns>Apuração com valor retido e código de receita.</returns>
    /// <exception cref="ArgumentException">Se a faixa não existir.</exception>
    public ApuracaoIrrfServicos Apurar(string codigoFaixa, ValorMonetario baseCalculo)
    {
        ArgumentNullException.ThrowIfNull(baseCalculo);
        var faixa = ObterFaixa(codigoFaixa)
            ?? throw new ArgumentException($"Enquadramento IRRF '{codigoFaixa}' inexistente na tabela vigente.", nameof(codigoFaixa));

        var valorBruto = decimal.Round(baseCalculo.Valor * faixa.Aliquota, 2, MidpointRounding.AwayFromZero);
        var dispensado = valorBruto < ValorMinimoRetencao;
        var retido = dispensado ? 0m : valorBruto;
        return new ApuracaoIrrfServicos(faixa.Codigo, faixa.Aliquota, faixa.CodigoReceitaDarf, ValorMonetario.De(retido), dispensado);
    }
}

/// <summary>Resultado da apuração do IRRF/PJ sobre um pagamento.</summary>
/// <param name="CodigoFaixa">Enquadramento aplicado.</param>
/// <param name="Aliquota">Alíquota aplicada (fração).</param>
/// <param name="CodigoReceitaDarf">Código de receita DARF para o recolhimento.</param>
/// <param name="ValorRetido">Valor a reter (já com dispensa por mínimo aplicada).</param>
/// <param name="DispensadoPorMinimo">Indica se houve dispensa por valor mínimo.</param>
public readonly record struct ApuracaoIrrfServicos(
    string CodigoFaixa,
    decimal Aliquota,
    string CodigoReceitaDarf,
    ValorMonetario ValorRetido,
    bool DispensadoPorMinimo);
