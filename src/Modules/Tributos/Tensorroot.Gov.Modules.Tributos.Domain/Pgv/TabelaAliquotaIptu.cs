using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

/// <summary>Identificador forte do agregado <see cref="TabelaAliquotaIptu"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaAliquotaIptuId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaAliquotaIptuId"/>.</returns>
    public static TabelaAliquotaIptuId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tabela de alíquotas do IPTU por exercício (LEI MUNICIPAL). Suporta alíquota única ou
/// progressiva por faixa de valor venal (EC 29/2000; CF art. 156 §1) e diferenciação predial ×
/// territorial (por <see cref="TipoUsoImovel"/>). O motor lê a tabela vigente — nunca números
/// hardcoded. Ver M6-DESIGN §1.3.
/// </summary>
public sealed class TabelaAliquotaIptu : AggregateRoot<TabelaAliquotaIptuId>, IMustHaveTenant
{
    /// <summary>
    /// Sentinela de "faixa sem teto" (limite superior). Valor alto e armazenável em decimal(18,2)
    /// — evita <see cref="decimal.MaxValue"/>, que estouraria a precisão da coluna.
    /// </summary>
    public const decimal SemTeto = 1_000_000_000_000m;

    private readonly List<FaixaAliquotaIptu> _faixas = [];

    private TabelaAliquotaIptu()
    {
    }

    private TabelaAliquotaIptu(TabelaAliquotaIptuId id, Guid tenantId, int exercicio, bool edificado, string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Edificado = edificado;
        FundamentoLegal = fundamentoLegal;
        Vigente = false;
        RaiseDomainEvent(new TabelaAliquotaIptuCriada(id, tenantId, exercicio));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício fiscal de vigência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>
    /// Indica se a tabela é a predial (edificado = <c>true</c>) ou territorial (lote sem construção
    /// = <c>false</c>) — a diferenciação predial × territorial é facultada por lei municipal.
    /// </summary>
    public bool Edificado { get; private set; }

    /// <summary>
    /// Fundamento legal (lei municipal de alíquotas do exercício).
    /// // TODO(validar-oficial): preencher com o CTM/decreto de Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Indica se a tabela está vigente (publicada e imutável).</summary>
    public bool Vigente { get; private set; }

    /// <summary>Faixas de progressividade por valor venal (ordenadas pelo limite inferior).</summary>
    public IReadOnlyCollection<FaixaAliquotaIptu> Faixas => _faixas;

    /// <summary>Cria uma tabela de alíquotas (ainda não vigente).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício fiscal (ano ≥ 1900).</param>
    /// <param name="edificado">Tabela predial (true) ou territorial (false).</param>
    /// <param name="fundamentoLegal">Lei municipal de alíquotas.</param>
    /// <returns>Nova <see cref="TabelaAliquotaIptu"/>.</returns>
    public static TabelaAliquotaIptu Criar(Guid tenantId, int exercicio, bool edificado, string fundamentoLegal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        return new TabelaAliquotaIptu(TabelaAliquotaIptuId.New(), tenantId, exercicio, edificado, fundamentoLegal.Trim());
    }

    /// <summary>
    /// Acrescenta uma faixa de progressividade. Para alíquota única, adicione uma faixa de 0 a
    /// <see cref="SemTeto"/>. As faixas não podem se sobrepor nem deixar lacunas.
    /// </summary>
    /// <param name="valorVenalMinimo">Limite inferior do valor venal (inclusivo, ≥ 0).</param>
    /// <param name="valorVenalMaximo">Limite superior do valor venal (exclusivo); use <see cref="SemTeto"/> para "sem teto".</param>
    /// <param name="aliquotaPercentual">Alíquota em % (ex.: 1.0 = 1%).</param>
    public void AdicionarFaixa(decimal valorVenalMinimo, decimal valorVenalMaximo, decimal aliquotaPercentual)
    {
        GarantirEditavel();
        var faixa = FaixaAliquotaIptu.Criar(Id, valorVenalMinimo, valorVenalMaximo, aliquotaPercentual);

        if (_faixas.Any(existente => faixa.ValorVenalMinimo < existente.ValorVenalMaximo && existente.ValorVenalMinimo < faixa.ValorVenalMaximo))
        {
            throw new InvalidOperationException("A faixa de valor venal se sobrepõe a uma faixa existente.");
        }

        _faixas.Add(faixa);
        _faixas.Sort((a, b) => a.ValorVenalMinimo.CompareTo(b.ValorVenalMinimo));
    }

    /// <summary>Publica a tabela (torna vigente e imutável).</summary>
    /// <exception cref="InvalidOperationException">Se não houver faixas ou houver lacuna a partir de zero.</exception>
    public void Publicar()
    {
        if (_faixas.Count == 0)
        {
            throw new InvalidOperationException("A tabela de alíquotas exige ao menos uma faixa antes de publicar.");
        }

        // Garante cobertura contínua a partir de zero (sem lacunas entre faixas).
        decimal limite = 0m;
        foreach (var faixa in _faixas.OrderBy(f => f.ValorVenalMinimo))
        {
            if (faixa.ValorVenalMinimo != limite)
            {
                throw new InvalidOperationException("As faixas de valor venal devem ser contínuas a partir de zero, sem lacunas.");
            }

            limite = faixa.ValorVenalMaximo;
        }

        Vigente = true;
        RaiseDomainEvent(new TabelaAliquotaIptuPublicada(Id, TenantId, Exercicio));
    }

    /// <summary>
    /// Seleciona a alíquota (%) aplicável a um valor venal. Modelo de faixa marginal-simples: a
    /// alíquota é a da faixa em que o valor venal se enquadra (regra municipal mais comum).
    /// </summary>
    /// <param name="valorVenal">Valor venal do imóvel (≥ 0).</param>
    /// <returns>Alíquota em % aplicável.</returns>
    /// <exception cref="InvalidOperationException">Se nenhuma faixa cobrir o valor.</exception>
    public decimal AliquotaPara(decimal valorVenal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valorVenal);
        var faixa = _faixas.FirstOrDefault(f => valorVenal >= f.ValorVenalMinimo && valorVenal < f.ValorVenalMaximo)
            ?? throw new InvalidOperationException($"Nenhuma faixa de alíquota cobre o valor venal {valorVenal}.");
        return faixa.AliquotaPercentual;
    }

    private void GarantirEditavel()
    {
        if (Vigente)
        {
            throw new InvalidOperationException("A tabela já está vigente e não pode ser alterada (crie nova versão por exercício).");
        }
    }
}
