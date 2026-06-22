using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Identificador forte do agregado <see cref="ApuracaoPonto"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ApuracaoPontoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ApuracaoPontoId"/>.</returns>
    public static ApuracaoPontoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Apuracao de jornada de UM servidor numa competencia (resultado do PTRP — Portaria MTP 671/2021):
/// consolida minutos trabalhados/devidos/extras/falta e o saldo do banco de horas, congelando o
/// espelho de ponto ao fechar. NAO recalcula a folha: expoe os totais (extras/faltas) como GANCHO —
/// um caso de uso pode traduzi-los em eventos de folha (rubricas) sem que este agregado conheca a
/// folha. Banco de horas: 6 meses (acordo individual) / 1 ano (ACT/CCT) — janela parametrizavel.
/// Raiz de agregado.
/// </summary>
public sealed class ApuracaoPonto : AggregateRoot<ApuracaoPontoId>, IMustHaveTenant
{
    private ApuracaoPonto()
    {
    }

    private ApuracaoPonto(
        ApuracaoPontoId id,
        Guid tenantId,
        Guid servidorId,
        Competencia competencia,
        int minutosTrabalhados,
        int minutosDevidos,
        int minutosExtras,
        int minutosFalta,
        int saldoBancoHorasAnteriorMinutos)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        Competencia = competencia;
        MinutosTrabalhados = minutosTrabalhados;
        MinutosDevidos = minutosDevidos;
        MinutosExtras = minutosExtras;
        MinutosFalta = minutosFalta;
        SaldoBancoHorasAnteriorMinutos = saldoBancoHorasAnteriorMinutos;
        Situacao = SituacaoApuracaoPonto.Aberta;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor apurado.</summary>
    public Guid ServidorId { get; private set; }

    /// <summary>Competencia (AAAA-MM) da apuracao.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Minutos efetivamente trabalhados no periodo.</summary>
    public int MinutosTrabalhados { get; private set; }

    /// <summary>Minutos devidos (jornada contratada) no periodo.</summary>
    public int MinutosDevidos { get; private set; }

    /// <summary>Minutos de hora extra apurados (apos tolerancia).</summary>
    public int MinutosExtras { get; private set; }

    /// <summary>Minutos de falta/atraso apurados (apos tolerancia).</summary>
    public int MinutosFalta { get; private set; }

    /// <summary>Saldo do banco de horas ANTES desta competencia (minutos; pode ser negativo).</summary>
    public int SaldoBancoHorasAnteriorMinutos { get; private set; }

    /// <summary>Situacao da apuracao (aberta/fechada).</summary>
    public SituacaoApuracaoPonto Situacao { get; private set; }

    /// <summary>Data de fechamento (nula enquanto aberta).</summary>
    public DateOnly? DataFechamento { get; private set; }

    /// <summary>Saldo do periodo (extras menos faltas) que credita/debita o banco de horas.</summary>
    public int SaldoPeriodoMinutos => MinutosExtras - MinutosFalta;

    /// <summary>Saldo acumulado do banco de horas APOS esta competencia.</summary>
    public int SaldoBancoHorasAtualMinutos => SaldoBancoHorasAnteriorMinutos + SaldoPeriodoMinutos;

    /// <summary>
    /// Cria/recalcula a apuracao de uma competencia a partir do resultado do <see cref="TratamentoJornada"/>.
    /// Idempotente em estado: pode ser refeita enquanto Aberta (o caso de uso recria o agregado).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor apurado.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="resultado">Resultado do tratamento de jornada.</param>
    /// <param name="saldoBancoHorasAnteriorMinutos">Saldo do banco de horas anterior (default 0).</param>
    /// <returns>Nova <see cref="ApuracaoPonto"/> aberta.</returns>
    /// <exception cref="ArgumentNullException">Se competencia ou resultado forem nulos.</exception>
    public static ApuracaoPonto Apurar(
        Guid tenantId,
        Guid servidorId,
        Competencia competencia,
        ResultadoApuracaoCompetencia resultado,
        int saldoBancoHorasAnteriorMinutos = 0)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentNullException.ThrowIfNull(resultado);

        return new ApuracaoPonto(
            ApuracaoPontoId.New(),
            tenantId,
            servidorId,
            competencia,
            resultado.MinutosTrabalhados,
            resultado.MinutosDevidos,
            resultado.MinutosExtras,
            resultado.MinutosFalta,
            saldoBancoHorasAnteriorMinutos);
    }

    /// <summary>
    /// Fecha a apuracao (congela espelho/AEJ) e emite <see cref="ApuracaoPontoFechada"/> — gancho
    /// (via Outbox) para a folha traduzir extras/faltas em rubricas. NAO altera a folha aqui.
    /// </summary>
    /// <param name="hoje">Data de referencia do fechamento.</param>
    /// <exception cref="InvalidOperationException">Se a apuracao ja estiver fechada.</exception>
    public void Fechar(DateOnly hoje)
    {
        if (Situacao == SituacaoApuracaoPonto.Fechada)
        {
            throw new InvalidOperationException("Apuracao de ponto ja fechada.");
        }

        Situacao = SituacaoApuracaoPonto.Fechada;
        DataFechamento = hoje;
        RaiseDomainEvent(new ApuracaoPontoFechada(
            Id, ServidorId, Competencia.Ano, Competencia.Mes, MinutosExtras, MinutosFalta));
    }
}
