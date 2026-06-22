using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>Identificador forte do agregado <see cref="DividaAtiva"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DividaAtivaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DividaAtivaId"/>.</returns>
    public static DividaAtivaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação (estado) da Dívida Ativa.</summary>
public enum SituacaoDividaAtiva
{
    /// <summary>Inscrita em Dívida Ativa.</summary>
    Inscrita = 1,

    /// <summary>Com Certidão de Dívida Ativa (CDA) emitida.</summary>
    CdaEmitida = 2,

    /// <summary>Protestada em cartório.</summary>
    Protestada = 3,

    /// <summary>Em execução fiscal (Lei 6.830/80).</summary>
    EmExecucaoFiscal = 4,

    /// <summary>Parcelada (REFIS) — exigibilidade suspensa.</summary>
    Parcelada = 5,

    /// <summary>Quitada.</summary>
    Quitada = 6,

    /// <summary>Cancelada.</summary>
    Cancelada = 7,
}

/// <summary>
/// Crédito tributário inscrito em Dívida Ativa: título exigível com prazo prescricional
/// de 5 anos (CTN art. 174), passível de CDA, protesto, execução fiscal e parcelamento.
/// </summary>
public sealed class DividaAtiva : AggregateRoot<DividaAtivaId>, IMustHaveTenant
{
    /// <summary>Prazo prescricional do crédito tributário, em anos (CTN art. 174).</summary>
    public const int AnosPrescricao = 5;

    private DividaAtiva()
    {
    }

    private DividaAtiva(
        DividaAtivaId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        LancamentoId lancamentoId,
        ValorMonetario valorInscrito,
        DateOnly dataInscricao)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        LancamentoId = lancamentoId;
        ValorInscrito = valorInscrito;
        DataInscricao = dataInscricao;
        Situacao = SituacaoDividaAtiva.Inscrita;
        RaiseDomainEvent(new DividaAtivaInscrita(id, contribuinteId, lancamentoId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte devedor.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Lançamento de origem.</summary>
    public LancamentoId LancamentoId { get; private set; }

    /// <summary>Valor inscrito.</summary>
    public ValorMonetario ValorInscrito { get; private set; } = default!;

    /// <summary>Data de inscrição em Dívida Ativa.</summary>
    public DateOnly DataInscricao { get; private set; }

    /// <summary>Número da CDA, quando emitida.</summary>
    public string? NumeroCda { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoDividaAtiva Situacao { get; private set; }

    /// <summary>Data-limite da prescrição (data de inscrição + 5 anos).</summary>
    public DateOnly DataPrescricao => DataInscricao.AddYears(AnosPrescricao);

    /// <summary>Inscreve um crédito vencido em Dívida Ativa.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte devedor.</param>
    /// <param name="lancamentoId">Lançamento de origem.</param>
    /// <param name="valorInscrito">Valor inscrito.</param>
    /// <param name="dataInscricao">Data de inscrição.</param>
    /// <returns>Nova <see cref="DividaAtiva"/>.</returns>
    public static DividaAtiva Inscrever(
        Guid tenantId,
        ContribuinteId contribuinteId,
        LancamentoId lancamentoId,
        ValorMonetario valorInscrito,
        DateOnly dataInscricao)
    {
        ArgumentNullException.ThrowIfNull(valorInscrito);
        return new DividaAtiva(DividaAtivaId.New(), tenantId, contribuinteId, lancamentoId, valorInscrito, dataInscricao);
    }

    /// <summary>Emite a Certidão de Dívida Ativa (CDA).</summary>
    /// <param name="numeroCda">Número da CDA.</param>
    /// <exception cref="InvalidOperationException">Se a dívida não estiver recém-inscrita.</exception>
    public void EmitirCda(string numeroCda)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroCda);
        if (Situacao != SituacaoDividaAtiva.Inscrita)
        {
            throw new InvalidOperationException($"A CDA só pode ser emitida para dívida recém-inscrita. Situação atual: {Situacao}.");
        }

        NumeroCda = numeroCda;
        Situacao = SituacaoDividaAtiva.CdaEmitida;
        RaiseDomainEvent(new CdaEmitida(Id, numeroCda));
    }

    /// <summary>Registra o protesto extrajudicial da CDA.</summary>
    /// <exception cref="InvalidOperationException">Se não houver CDA emitida ou a dívida não estiver exigível.</exception>
    public void Protestar()
    {
        GarantirExigivel();
        if (Situacao != SituacaoDividaAtiva.CdaEmitida)
        {
            throw new InvalidOperationException("O protesto requer CDA emitida.");
        }

        Situacao = SituacaoDividaAtiva.Protestada;
    }

    /// <summary>Ajuíza a execução fiscal (Lei 6.830/80).</summary>
    /// <exception cref="InvalidOperationException">Se não houver CDA emitida/protestada ou a dívida não estiver exigível.</exception>
    public void AjuizarExecucaoFiscal()
    {
        GarantirExigivel();
        if (Situacao is not (SituacaoDividaAtiva.CdaEmitida or SituacaoDividaAtiva.Protestada))
        {
            throw new InvalidOperationException("A execução fiscal requer CDA emitida (eventualmente protestada).");
        }

        Situacao = SituacaoDividaAtiva.EmExecucaoFiscal;
    }

    /// <summary>Firma um parcelamento (REFIS) — suspende a exigibilidade e interrompe a prescrição.</summary>
    /// <exception cref="InvalidOperationException">Se a dívida não estiver exigível.</exception>
    public void FirmarParcelamento()
    {
        GarantirExigivel();
        Situacao = SituacaoDividaAtiva.Parcelada;
        RaiseDomainEvent(new ParcelamentoFirmado(Id));
    }

    /// <summary>Quita a dívida.</summary>
    /// <exception cref="InvalidOperationException">Se a dívida já estiver encerrada.</exception>
    public void Quitar()
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada)
        {
            throw new InvalidOperationException($"Dívida já encerrada. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoDividaAtiva.Quitada;
        RaiseDomainEvent(new DividaQuitada(Id));
    }

    /// <summary>Indica se a dívida está prescrita na data informada.</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns><c>true</c> se prescrita.</returns>
    public bool EstaPrescrita(DateOnly hoje)
        => Situacao is not (SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Parcelada or SituacaoDividaAtiva.Cancelada)
        && hoje > DataPrescricao;

    private void GarantirExigivel()
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada or SituacaoDividaAtiva.Parcelada)
        {
            throw new InvalidOperationException($"A dívida não está exigível. Situação atual: {Situacao}.");
        }
    }
}
