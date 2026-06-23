using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Cosip;

/// <summary>Identificador forte do agregado <see cref="TabelaCosip"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaCosipId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaCosipId"/>.</returns>
    public static TabelaCosipId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Classe de consumidor da COSIP (espelha as classes da concessionária — residencial, comercial,
/// industrial, etc.). As classes efetivas e suas faixas são definidas pela LEI MUNICIPAL de COSIP.
/// </summary>
public enum ClasseConsumidorCosip
{
    /// <summary>Consumidor residencial.</summary>
    Residencial = 1,

    /// <summary>Consumidor comercial/serviços.</summary>
    Comercial = 2,

    /// <summary>Consumidor industrial.</summary>
    Industrial = 3,

    /// <summary>Consumidor rural.</summary>
    Rural = 4,

    /// <summary>Poder público / iluminação pública / demais classes.</summary>
    PoderPublico = 5,
}

/// <summary>
/// Tabela de faixas da COSIP/CIP (Contribuição para Custeio da Iluminação Pública — CF art. 149-A,
/// EC 39/2002; STF RE 573.675/Tema 44: tributo sui generis, progressividade válida). LEI MUNICIPAL
/// própria, versionada por exercício. A apuração é, tipicamente, por FAIXA DE CONSUMO (kWh) e CLASSE
/// de consumidor — progressiva (rateio do custo da iluminação). NENHUM valor é hardcoded.
/// <para>
/// // TODO(validar-oficial): faixas, classes e valores conforme a lei de COSIP de Maximiliano de
/// Almeida/RS; convênio de cobrança na fatura com a distribuidora (RGE/CEEE na região) é modelado à
/// parte (este agregado cobre o caminho de LANÇAMENTO PRÓPRIO). Ver M6-DESIGN §3.4.
/// </para>
/// </summary>
public sealed class TabelaCosip : AggregateRoot<TabelaCosipId>, IMustHaveTenant
{
    private readonly List<FaixaCosip> _faixas = [];

    private TabelaCosip()
    {
    }

    private TabelaCosip(TabelaCosipId id, Guid tenantId, int exercicio, string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        FundamentoLegal = fundamentoLegal;
        Vigente = false;
        RaiseDomainEvent(new TabelaCosipCriada(id, tenantId, exercicio));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício fiscal de vigência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>
    /// Fundamento legal (lei municipal de COSIP). // TODO(validar-oficial): preencher com a lei de
    /// Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Indica se a tabela está vigente (publicada e imutável).</summary>
    public bool Vigente { get; private set; }

    /// <summary>Faixas de consumo por classe de consumidor.</summary>
    public IReadOnlyList<FaixaCosip> Faixas => _faixas;

    /// <summary>Cria a tabela de COSIP de um exercício (ainda não vigente).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício fiscal (ano ≥ 1900).</param>
    /// <param name="fundamentoLegal">Lei municipal de COSIP.</param>
    /// <returns>Nova <see cref="TabelaCosip"/>.</returns>
    public static TabelaCosip Criar(Guid tenantId, int exercicio, string fundamentoLegal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);
        return new TabelaCosip(TabelaCosipId.New(), tenantId, exercicio, fundamentoLegal.Trim());
    }

    /// <summary>
    /// Define uma faixa de consumo (kWh) de uma classe, com o valor da contribuição. As faixas de uma
    /// mesma classe não podem se sobrepor; é vedado alterar a tabela já vigente.
    /// </summary>
    /// <param name="classe">Classe de consumidor.</param>
    /// <param name="consumoMinimoKwh">Consumo mínimo (inclusivo) da faixa, em kWh.</param>
    /// <param name="consumoMaximoKwh">Consumo máximo (inclusivo) da faixa em kWh; nulo = sem teto.</param>
    /// <param name="valor">Valor da COSIP na faixa.</param>
    /// <exception cref="InvalidOperationException">Se a tabela já estiver vigente ou houver sobreposição na mesma classe.</exception>
    public void DefinirFaixa(ClasseConsumidorCosip classe, decimal consumoMinimoKwh, decimal? consumoMaximoKwh, ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Vigente)
        {
            throw new InvalidOperationException("Não é possível alterar uma tabela de COSIP já vigente.");
        }

        if (!Enum.IsDefined(classe))
        {
            throw new ArgumentOutOfRangeException(nameof(classe), classe, "Classe de consumidor inválida.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(consumoMinimoKwh);
        if (consumoMaximoKwh is not null && consumoMaximoKwh.Value < consumoMinimoKwh)
        {
            throw new ArgumentOutOfRangeException(nameof(consumoMaximoKwh), consumoMaximoKwh, "O consumo máximo não pode ser menor que o mínimo.");
        }

        if (_faixas.Any(f => f.Classe == classe && SeSobrepoe(f, consumoMinimoKwh, consumoMaximoKwh)))
        {
            throw new InvalidOperationException("A faixa de consumo informada se sobrepõe a outra da mesma classe.");
        }

        _faixas.Add(FaixaCosip.Criar(Id, classe, consumoMinimoKwh, consumoMaximoKwh, valor));
        _faixas.Sort((a, b) => a.Classe != b.Classe ? a.Classe.CompareTo(b.Classe) : a.ConsumoMinimoKwh.CompareTo(b.ConsumoMinimoKwh));
    }

    /// <summary>Publica a tabela (torna-a vigente e imutável).</summary>
    /// <exception cref="InvalidOperationException">Se não houver nenhuma faixa.</exception>
    public void Publicar()
    {
        if (_faixas.Count == 0)
        {
            throw new InvalidOperationException("Uma tabela de COSIP exige ao menos uma faixa antes de publicar.");
        }

        Vigente = true;
        RaiseDomainEvent(new TabelaCosipPublicada(Id, TenantId, Exercicio));
    }

    /// <summary>
    /// Apura o valor da COSIP de um consumidor a partir da sua classe e do consumo (kWh) — progressivo
    /// por faixa (RE 573.675). Determinístico e auditável.
    /// </summary>
    /// <param name="classe">Classe do consumidor.</param>
    /// <param name="consumoKwh">Consumo medido (kWh).</param>
    /// <returns>Valor da COSIP devida.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o consumo for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se nenhuma faixa da classe cobrir o consumo.</exception>
    public ValorMonetario Apurar(ClasseConsumidorCosip classe, decimal consumoKwh)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(consumoKwh);
        var faixa = _faixas.FirstOrDefault(f => f.Classe == classe && f.Contem(consumoKwh))
            ?? throw new InvalidOperationException($"Nenhuma faixa de COSIP da classe {classe} cobre o consumo {consumoKwh} kWh.");
        return faixa.Valor;
    }

    private static bool SeSobrepoe(FaixaCosip existente, decimal minimo, decimal? maximo)
    {
        var existenteMaximo = existente.ConsumoMaximoKwh ?? decimal.MaxValue;
        var novoMaximo = maximo ?? decimal.MaxValue;
        return minimo <= existenteMaximo && existente.ConsumoMinimoKwh <= novoMaximo;
    }
}
