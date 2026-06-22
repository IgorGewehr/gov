using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Itbi;

/// <summary>Identificador forte do agregado <see cref="TransmissaoImobiliaria"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TransmissaoImobiliariaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TransmissaoImobiliariaId"/>.</returns>
    public static TransmissaoImobiliariaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Transmissão imobiliária inter vivos onerosa (fato gerador do ITBI — CTN art. 35). Vincula o
/// <see cref="Imovel"/> ao transmitente e ao adquirente (<see cref="Contribuinte"/>), guarda o valor
/// declarado e a memória de cálculo do ITBI, e gera a guia avulsa de recolhimento (DAM). Exigível
/// no REGISTRO no CRI (LC 227/2026 vetou antecipação) — modelado como guia avulsa por transação.
/// Ver M6-DESIGN §3.1. NÃO emite documento cartorial.
/// </summary>
public sealed class TransmissaoImobiliaria : AggregateRoot<TransmissaoImobiliariaId>, IMustHaveTenant
{
    private TransmissaoImobiliaria()
    {
    }

    private TransmissaoImobiliaria(
        TransmissaoImobiliariaId id,
        Guid tenantId,
        ImovelId imovelId,
        ContribuinteId transmitenteId,
        ContribuinteId adquirenteId,
        int exercicio,
        ValorMonetario valorDeclarado,
        MemoriaItbi memoria)
        : base(id)
    {
        TenantId = tenantId;
        ImovelId = imovelId;
        TransmitenteId = transmitenteId;
        AdquirenteId = adquirenteId;
        Exercicio = exercicio;
        ValorDeclarado = valorDeclarado;
        ValorVenalReferencia = memoria.ValorVenalReferencia;
        BaseCalculo = memoria.BaseCalculo;
        BaseFoiValorVenal = memoria.BaseFoiValorVenal;
        AliquotaPercentual = memoria.AliquotaPercentual;
        ImpostoDevido = memoria.ImpostoDevido;
        RaiseDomainEvent(new TransmissaoImobiliariaRegistrada(id, tenantId, imovelId, memoria.ImpostoDevido.Valor));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Imóvel transmitido (cadastro imobiliário).</summary>
    public ImovelId ImovelId { get; private set; }

    /// <summary>Contribuinte transmitente (alienante).</summary>
    public ContribuinteId TransmitenteId { get; private set; }

    /// <summary>Contribuinte adquirente (sujeito passivo usual do ITBI — CTN art. 42).</summary>
    public ContribuinteId AdquirenteId { get; private set; }

    /// <summary>Exercício fiscal do fato gerador (para a alíquota vigente).</summary>
    public int Exercicio { get; private set; }

    /// <summary>Valor declarado da transação (R$).</summary>
    public ValorMonetario ValorDeclarado { get; private set; } = default!;

    /// <summary>Valor venal de referência (motor do Imóvel/PGV), R$.</summary>
    public ValorMonetario ValorVenalReferencia { get; private set; } = default!;

    /// <summary>Base de cálculo adotada (maior entre venal e declarado), R$.</summary>
    public ValorMonetario BaseCalculo { get; private set; } = default!;

    /// <summary>Indica se a base adotada foi o valor venal de referência.</summary>
    public bool BaseFoiValorVenal { get; private set; }

    /// <summary>Alíquota aplicada (%).</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>ITBI devido após isenção (R$).</summary>
    public ValorMonetario ImpostoDevido { get; private set; } = default!;

    /// <summary>
    /// Registra a transmissão e calcula o ITBI (base = maior entre valor venal e valor declarado).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="imovelId">Imóvel transmitido.</param>
    /// <param name="transmitenteId">Transmitente.</param>
    /// <param name="adquirenteId">Adquirente.</param>
    /// <param name="exercicio">Exercício fiscal do fato gerador.</param>
    /// <param name="valorDeclarado">Valor declarado da transação.</param>
    /// <param name="memoria">Memória de cálculo do ITBI já apurada.</param>
    /// <returns>Nova <see cref="TransmissaoImobiliaria"/>.</returns>
    public static TransmissaoImobiliaria Registrar(
        Guid tenantId,
        ImovelId imovelId,
        ContribuinteId transmitenteId,
        ContribuinteId adquirenteId,
        int exercicio,
        ValorMonetario valorDeclarado,
        MemoriaItbi memoria)
    {
        ArgumentNullException.ThrowIfNull(valorDeclarado);
        ArgumentNullException.ThrowIfNull(memoria);
        if (transmitenteId.Value == adquirenteId.Value)
        {
            throw new InvalidOperationException("O transmitente e o adquirente não podem ser o mesmo contribuinte.");
        }

        return new TransmissaoImobiliaria(
            TransmissaoImobiliariaId.New(),
            tenantId,
            imovelId,
            transmitenteId,
            adquirenteId,
            exercicio,
            valorDeclarado,
            memoria);
    }
}
