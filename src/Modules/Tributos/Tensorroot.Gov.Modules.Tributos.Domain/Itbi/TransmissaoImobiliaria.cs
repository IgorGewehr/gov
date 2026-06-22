using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
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
        Origem = memoria.Origem;
        ProcessoArbitramentoId = memoria.ProcessoArbitramentoId;
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

    /// <summary>
    /// Base de cálculo adotada (R$): valor declarado por padrão (Tema 1.113/STJ) ou valor arbitrado
    /// (CTN art. 148) após processo administrativo concluído.
    /// </summary>
    public ValorMonetario BaseCalculo { get; private set; } = default!;

    /// <summary>Origem da base adotada: declarada (padrão) ou arbitrada por processo CTN 148.</summary>
    public OrigemBaseCalculoItbi Origem { get; private set; }

    /// <summary>Processo de arbitramento vinculado quando a base foi arbitrada; caso contrário, nulo.</summary>
    public Guid? ProcessoArbitramentoId { get; private set; }

    /// <summary>Alíquota aplicada (%).</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>ITBI devido após isenção (R$).</summary>
    public ValorMonetario ImpostoDevido { get; private set; } = default!;

    /// <summary>
    /// Registra a transmissão e lança o ITBI pela base DECLARADA (Tema 1.113/STJ — presunção de
    /// veracidade). A elevação da base é uma decisão posterior de processo administrativo (CTN art. 148)
    /// aplicada por <see cref="AplicarArbitramento"/>, nunca automática.
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

        // A transmissão NASCE sempre pela base declarada (Tema 1.113/STJ). Arbitramento é posterior.
        if (memoria.Origem != OrigemBaseCalculoItbi.Declarada)
        {
            throw new InvalidOperationException("A transmissão deve ser registrada com a base DECLARADA; o arbitramento é aplicado posteriormente (CTN art. 148).");
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

    /// <summary>
    /// Sinaliza, para a fila de revisão fiscal, uma divergência relevante de triagem entre o valor
    /// declarado e o valor venal de referência (Tema 1.113/STJ). NÃO altera o tributo: a guia segue pelo
    /// valor declarado. Apenas deflagra a decisão humana de (talvez) instaurar o arbitramento (CTN 148).
    /// </summary>
    /// <param name="valorVenalReferencia">Valor venal de referência usado na triagem.</param>
    /// <exception cref="ArgumentNullException">Se o valor venal de referência for nulo.</exception>
    public void SinalizarDivergenciaTriagem(ValorMonetario valorVenalReferencia)
    {
        ArgumentNullException.ThrowIfNull(valorVenalReferencia);
        RaiseDomainEvent(new AlertaDivergenciaItbi(Id, TenantId, ValorDeclarado.Valor, valorVenalReferencia.Valor));
    }

    /// <summary>
    /// Aplica o resultado de um arbitramento CONCLUÍDO (CTN art. 148): eleva a base para o valor arbitrado,
    /// recalcula o imposto e habilita o lançamento de ofício complementar auditado. Só pode ser aplicado
    /// uma vez (a base não volta a ser arbitrada) e exige a memória recalculada com origem arbitrada.
    /// </summary>
    /// <param name="resultado">Resultado do processo de arbitramento concluído.</param>
    /// <param name="memoria">Memória recalculada (origem ArbitradaArt148) vinculada ao mesmo processo.</param>
    /// <exception cref="ArgumentNullException">Se o resultado ou a memória forem nulos.</exception>
    /// <exception cref="InvalidOperationException">Se a base já tiver sido arbitrada ou a memória for inconsistente.</exception>
    public void AplicarArbitramento(ResultadoArbitramento resultado, MemoriaItbi memoria)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(memoria);
        if (!resultado.ProcessoConcluido)
        {
            throw new InvalidOperationException("Arbitramento exige processo administrativo (CTN art. 148) concluído com contraditório.");
        }

        if (Origem == OrigemBaseCalculoItbi.ArbitradaArt148)
        {
            throw new InvalidOperationException("A base desta transmissão já foi arbitrada.");
        }

        if (memoria.Origem != OrigemBaseCalculoItbi.ArbitradaArt148
            || memoria.ProcessoArbitramentoId != resultado.ProcessoArbitramentoId)
        {
            throw new InvalidOperationException("A memória recalculada deve ter origem arbitrada e estar vinculada ao processo informado.");
        }

        BaseCalculo = memoria.BaseCalculo;
        Origem = OrigemBaseCalculoItbi.ArbitradaArt148;
        ProcessoArbitramentoId = resultado.ProcessoArbitramentoId;
        AliquotaPercentual = memoria.AliquotaPercentual;
        ImpostoDevido = memoria.ImpostoDevido;
        RaiseDomainEvent(new ArbitramentoItbiAplicado(
            Id,
            TenantId,
            new ProcessoArbitramentoItbiId(resultado.ProcessoArbitramentoId),
            memoria.ImpostoDevido.Valor));
    }
}
