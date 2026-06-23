using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Taxas;

/// <summary>Identificador forte do agregado <see cref="TabelaTaxa"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaTaxaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaTaxaId"/>.</returns>
    public static TabelaTaxaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Espécie da taxa quanto ao fato gerador (CTN art. 77): exercício do poder de polícia (fiscalização)
/// ou utilização de serviço público específico e divisível.
/// </summary>
public enum EspecieTaxa
{
    /// <summary>Taxa pelo exercício regular do poder de polícia (fiscalização) — CTN art. 78.</summary>
    PoderPolicia = 1,

    /// <summary>Taxa pela utilização de serviço público específico e divisível — CTN art. 79.</summary>
    Servico = 2,

    /// <summary>
    /// Taxa de Licença de Localização/Funcionamento (TLL) — subtipo de poder de polícia vinculado ao
    /// cadastro de alvará/estabelecimento. M6-DESIGN §3.3.
    /// </summary>
    LicencaLocalizacaoFuncionamento = 3,
}

/// <summary>Modo de cálculo do valor da taxa (a partir da tabela do CTM, parametrizável por tenant).</summary>
public enum ModoCalculoTaxa
{
    /// <summary>Valor fixo por incidência (independe de quantidade).</summary>
    ValorFixo = 1,

    /// <summary>Valor unitário multiplicado por uma quantidade-base (ex.: R$/m² de área fiscalizada).</summary>
    PorUnidade = 2,

    /// <summary>Valor por faixa da quantidade-base (ex.: faixas de metragem/atividade) — tabela escalonada.</summary>
    PorFaixa = 3,
}

/// <summary>
/// Tabela de uma taxa municipal (LEI MUNICIPAL — Código Tributário Municipal), versionada por exercício.
/// Modela o valor de uma taxa de poder de polícia, de serviço ou de licença (TLL) conforme o modo de
/// cálculo (fixo, por unidade ou por faixa). NENHUM valor é hardcoded — tudo vem do CTM do tenant.
/// <para>
/// Invariante de domínio (CTN art. 80 + SV 29/STF): a taxa NÃO pode ter base de cálculo idêntica à de
/// imposto NEM ser calculada em função do capital da empresa — por isso a quantidade-base é um elemento
/// físico (metragem, unidade, atividade), nunca o capital/faturamento. // TODO(validar-oficial): os
/// valores/faixas/atividades efetivas conforme o CTM de Maximiliano de Almeida/RS. Ver M6-DESIGN §3.2.
/// </para>
/// </summary>
public sealed class TabelaTaxa : AggregateRoot<TabelaTaxaId>, IMustHaveTenant
{
    private readonly List<FaixaTaxa> _faixas = [];

    private TabelaTaxa()
    {
    }

    private TabelaTaxa(
        TabelaTaxaId id,
        Guid tenantId,
        string codigo,
        string descricao,
        EspecieTaxa especie,
        ModoCalculoTaxa modoCalculo,
        int exercicio,
        ValorMonetario valorBase,
        string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Descricao = descricao;
        Especie = especie;
        ModoCalculo = modoCalculo;
        Exercicio = exercicio;
        ValorBase = valorBase;
        FundamentoLegal = fundamentoLegal;
        Vigente = false;
        RaiseDomainEvent(new TabelaTaxaCriada(id, tenantId, codigo, exercicio));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Código da taxa no CTM (ex.: "TLL", "TXLIXO", "TXFISC").</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Descrição da taxa.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Espécie quanto ao fato gerador (polícia/serviço/licença).</summary>
    public EspecieTaxa Especie { get; private set; }

    /// <summary>Modo de cálculo do valor.</summary>
    public ModoCalculoTaxa ModoCalculo { get; private set; }

    /// <summary>Exercício fiscal de vigência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>
    /// Valor base: usado como valor fixo (modo ValorFixo) ou como valor unitário por quantidade-base
    /// (modo PorUnidade). No modo PorFaixa serve apenas de mínimo/fallback; o valor real vem da faixa.
    /// </summary>
    public ValorMonetario ValorBase { get; private set; } = default!;

    /// <summary>
    /// Fundamento legal (artigo do CTM). // TODO(validar-oficial): preencher com a lei de
    /// Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Indica se a tabela está vigente (publicada e imutável).</summary>
    public bool Vigente { get; private set; }

    /// <summary>Faixas da quantidade-base (apenas no modo PorFaixa), ordenadas por limite inferior.</summary>
    public IReadOnlyList<FaixaTaxa> Faixas => _faixas;

    /// <summary>Cria uma tabela de taxa de um exercício (ainda não vigente).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Código da taxa no CTM.</param>
    /// <param name="descricao">Descrição da taxa.</param>
    /// <param name="especie">Espécie (polícia/serviço/licença).</param>
    /// <param name="modoCalculo">Modo de cálculo.</param>
    /// <param name="exercicio">Exercício fiscal (ano ≥ 1900).</param>
    /// <param name="valorBase">Valor base/unitário (lei municipal).</param>
    /// <param name="fundamentoLegal">Artigo do CTM.</param>
    /// <returns>Nova <see cref="TabelaTaxa"/>.</returns>
    public static TabelaTaxa Criar(
        Guid tenantId,
        string codigo,
        string descricao,
        EspecieTaxa especie,
        ModoCalculoTaxa modoCalculo,
        int exercicio,
        ValorMonetario valorBase,
        string fundamentoLegal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        ArgumentNullException.ThrowIfNull(valorBase);
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);
        if (!Enum.IsDefined(especie))
        {
            throw new ArgumentOutOfRangeException(nameof(especie), especie, "Espécie de taxa inválida.");
        }

        if (!Enum.IsDefined(modoCalculo))
        {
            throw new ArgumentOutOfRangeException(nameof(modoCalculo), modoCalculo, "Modo de cálculo de taxa inválido.");
        }

        return new TabelaTaxa(TabelaTaxaId.New(), tenantId, codigo.Trim(), descricao.Trim(), especie, modoCalculo, exercicio, valorBase, fundamentoLegal.Trim());
    }

    /// <summary>
    /// Define uma faixa da quantidade-base (modo PorFaixa). As faixas não podem se sobrepor; é vedado
    /// alterar a tabela já vigente.
    /// </summary>
    /// <param name="limiteInferior">Limite inferior (inclusivo) da quantidade-base.</param>
    /// <param name="limiteSuperior">Limite superior (inclusivo); nulo = sem teto.</param>
    /// <param name="valor">Valor da taxa na faixa.</param>
    /// <exception cref="InvalidOperationException">Se a tabela já estiver vigente, não for PorFaixa ou houver sobreposição.</exception>
    public void DefinirFaixa(decimal limiteInferior, decimal? limiteSuperior, ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Vigente)
        {
            throw new InvalidOperationException("Não é possível alterar uma tabela de taxa já vigente.");
        }

        if (ModoCalculo != ModoCalculoTaxa.PorFaixa)
        {
            throw new InvalidOperationException("Faixas só se aplicam ao modo de cálculo PorFaixa.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(limiteInferior);
        if (limiteSuperior is not null && limiteSuperior.Value < limiteInferior)
        {
            throw new ArgumentOutOfRangeException(nameof(limiteSuperior), limiteSuperior, "O limite superior não pode ser menor que o inferior.");
        }

        if (_faixas.Any(f => SeSobrepoe(f, limiteInferior, limiteSuperior)))
        {
            throw new InvalidOperationException("A faixa informada se sobrepõe a uma faixa já definida.");
        }

        _faixas.Add(FaixaTaxa.Criar(Id, limiteInferior, limiteSuperior, valor));
        _faixas.Sort((a, b) => a.LimiteInferior.CompareTo(b.LimiteInferior));
    }

    /// <summary>Publica a tabela (torna-a vigente e imutável).</summary>
    /// <exception cref="InvalidOperationException">Se PorFaixa e sem faixas definidas.</exception>
    public void Publicar()
    {
        if (ModoCalculo == ModoCalculoTaxa.PorFaixa && _faixas.Count == 0)
        {
            throw new InvalidOperationException("Uma tabela PorFaixa exige ao menos uma faixa antes de publicar.");
        }

        Vigente = true;
        RaiseDomainEvent(new TabelaTaxaPublicada(Id, TenantId, Codigo, Exercicio));
    }

    /// <summary>
    /// Calcula o valor da taxa para uma quantidade-base (área, unidades, etc.), conforme o modo de cálculo.
    /// Determinístico e auditável (sem relógio nem aleatoriedade).
    /// </summary>
    /// <param name="quantidadeBase">Quantidade-base do fato gerador (ex.: m², nº de unidades); ignorada no modo ValorFixo.</param>
    /// <returns>Valor da taxa.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade-base for negativa.</exception>
    /// <exception cref="InvalidOperationException">Se PorFaixa e nenhuma faixa cobrir a quantidade.</exception>
    public ValorMonetario Calcular(decimal quantidadeBase)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidadeBase);

        return ModoCalculo switch
        {
            ModoCalculoTaxa.ValorFixo => ValorBase,
            ModoCalculoTaxa.PorUnidade => ValorMonetario.De(decimal.Round(ValorBase.Valor * quantidadeBase, 2, MidpointRounding.AwayFromZero)),
            ModoCalculoTaxa.PorFaixa => ValorDaFaixa(quantidadeBase),
            _ => throw new InvalidOperationException($"Modo de cálculo não suportado: {ModoCalculo}."),
        };
    }

    private ValorMonetario ValorDaFaixa(decimal quantidadeBase)
    {
        var faixa = _faixas.FirstOrDefault(f => f.Contem(quantidadeBase))
            ?? throw new InvalidOperationException($"Nenhuma faixa da taxa {Codigo} cobre a quantidade-base {quantidadeBase}.");
        return faixa.Valor;
    }

    private static bool SeSobrepoe(FaixaTaxa existente, decimal inferior, decimal? superior)
    {
        var existenteSuperior = existente.LimiteSuperior ?? decimal.MaxValue;
        var novoSuperior = superior ?? decimal.MaxValue;
        return inferior <= existenteSuperior && existente.LimiteInferior <= novoSuperior;
    }
}
