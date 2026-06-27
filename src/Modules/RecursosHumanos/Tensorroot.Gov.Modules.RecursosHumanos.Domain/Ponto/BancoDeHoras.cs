using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Identificador forte do agregado <see cref="BancoDeHoras"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct BancoDeHorasId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="BancoDeHorasId"/>.</returns>
    public static BancoDeHorasId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Banco de horas de UM servidor: livro-razao (ledger) dos creditos (horas extras) e debitos (compensacao/
/// faltas/prescricao) acumulados ao longo do tempo, com saldo corrente em minutos (positivo = horas a
/// compensar/pagar a favor do servidor; negativo = horas devidas pelo servidor). Diferente da
/// <see cref="ApuracaoPonto"/> (snapshot de UMA competencia), o banco de horas e o SALDO VIVO e auditavel
/// por servidor, alimentado pelo fechamento das apuracoes (gancho via Outbox) e sujeito a PRESCRICAO da
/// janela parametrizavel (6 meses por acordo individual / 12 meses por ACT-CCT — Portaria MTP 671/2021;
/// para o estatutario, lei municipal/RJU). Raiz de agregado <see cref="IMustHaveTenant"/>.
/// </summary>
public sealed class BancoDeHoras : AggregateRoot<BancoDeHorasId>, IMustHaveTenant
{
    private readonly List<LancamentoBancoHoras> _lancamentos = [];

    private BancoDeHoras()
    {
    }

    private BancoDeHoras(BancoDeHorasId id, Guid tenantId, Guid servidorId)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        SaldoMinutos = 0;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor titular do banco de horas (unico por tenant).</summary>
    public Guid ServidorId { get; private set; }

    /// <summary>Saldo corrente do banco de horas em minutos (positivo a favor do servidor; negativo devido).</summary>
    public int SaldoMinutos { get; private set; }

    /// <summary>Lancamentos do livro-razao (creditos/debitos), expostos somente pela raiz.</summary>
    public IReadOnlyList<LancamentoBancoHoras> Lancamentos => _lancamentos;

    /// <summary>Abre um banco de horas zerado para um servidor.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor titular.</param>
    /// <returns>Novo <see cref="BancoDeHoras"/> com saldo zero.</returns>
    public static BancoDeHoras Abrir(Guid tenantId, Guid servidorId)
        => new(BancoDeHorasId.New(), tenantId, servidorId);

    /// <summary>
    /// Credita minutos de hora extra ao banco (resultado do fechamento de uma apuracao de ponto).
    /// Idempotente pela <paramref name="referencia"/>: re-aplicar a mesma referencia e no-op, evitando
    /// dupla contagem quando o gancho de fechamento for reprocessado (Outbox at-least-once).
    /// </summary>
    /// <param name="minutos">Minutos a creditar (&gt; 0).</param>
    /// <param name="data">Data do credito (competencia de origem; base da prescricao).</param>
    /// <param name="referencia">Referencia de negocio idempotente (ex.: "apuracao:{id}").</param>
    /// <param name="descricao">Descricao do lancamento.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="minutos"/> nao for positivo.</exception>
    /// <exception cref="ArgumentException">Se a referencia for vazia.</exception>
    public void Creditar(int minutos, DateOnly data, string referencia, string descricao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutos);
        Lancar(TipoLancamentoBancoHoras.Credito, minutos, data, referencia, descricao);
    }

    /// <summary>
    /// Debita minutos do banco (faltas/atrasos compensados pelo saldo, ou compensacao formal). Idempotente
    /// pela <paramref name="referencia"/>.
    /// </summary>
    /// <param name="minutos">Minutos a debitar (&gt; 0).</param>
    /// <param name="data">Data do debito.</param>
    /// <param name="referencia">Referencia de negocio idempotente.</param>
    /// <param name="descricao">Descricao do lancamento.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="minutos"/> nao for positivo.</exception>
    /// <exception cref="ArgumentException">Se a referencia for vazia.</exception>
    public void Debitar(int minutos, DateOnly data, string referencia, string descricao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutos);
        Lancar(TipoLancamentoBancoHoras.Debito, minutos, data, referencia, descricao);
    }

    /// <summary>
    /// Aplica o resultado liquido (extras - faltas) de uma apuracao de ponto fechada: credita o excedente
    /// ou debita o deficit, num unico lancamento idempotente pela apuracao de origem. Saldo nulo nao gera
    /// lancamento. Ponto de integracao do fechamento da apuracao (gancho via Outbox).
    /// </summary>
    /// <param name="saldoLiquidoMinutos">Saldo liquido do periodo (extras - faltas); pode ser negativo.</param>
    /// <param name="competencia">Competencia de origem (data-base da prescricao).</param>
    /// <param name="apuracaoId">Identificador da apuracao de origem (idempotencia).</param>
    public void AplicarApuracao(int saldoLiquidoMinutos, DateOnly competencia, Guid apuracaoId)
    {
        var referencia = $"apuracao:{apuracaoId}";
        if (saldoLiquidoMinutos > 0)
        {
            Creditar(saldoLiquidoMinutos, competencia, referencia, "Credito de horas extras (fechamento de apuracao).");
        }
        else if (saldoLiquidoMinutos < 0)
        {
            Debitar(-saldoLiquidoMinutos, competencia, referencia, "Debito de faltas/atrasos (fechamento de apuracao).");
        }
    }

    /// <summary>
    /// Prescreve (zera) os creditos POSITIVOS cuja data-base e anterior ao limite da janela (creditos nao
    /// compensados dentro do prazo legal/parametrizado). Lanca o debito de prescricao correspondente ao
    /// montante prescrito, reduzindo o saldo. Idempotente por mes-base: a referencia carimba o limite.
    /// Em saldo nao-positivo nada prescreve (nao ha credito a perder).
    /// </summary>
    /// <param name="limiteData">Data-limite: creditos com data &lt; limite estao fora da janela.</param>
    /// <param name="hoje">Data do lancamento de prescricao.</param>
    /// <returns>Minutos prescritos (0 quando nada a prescrever).</returns>
    public int PrescreverCreditosAnterioresA(DateOnly limiteData, DateOnly hoje)
    {
        var referencia = $"prescricao:{limiteData:yyyy-MM-dd}";
        if (_lancamentos.Any(l => string.Equals(l.Referencia, referencia, StringComparison.Ordinal)))
        {
            return 0; // Idempotente por limite: a prescricao DESTE limite ja foi aplicada.
        }

        // So prescreve quando o saldo e positivo: creditos vencidos limitados ao saldo vigente (debitos
        // posteriores ja consumiram parte dos creditos antigos — nao se prescreve o que ja foi usado).
        if (SaldoMinutos <= 0)
        {
            return 0;
        }

        var creditosVencidos = _lancamentos
            .Where(l => l.Tipo == TipoLancamentoBancoHoras.Credito && l.Data < limiteData)
            .Sum(l => l.Minutos);

        // Desconta o que JA foi prescrito em execucoes anteriores (o job avanca o limite mes a mes e os
        // lancamentos de credito originais nunca sao removidos). Sem isto, um credito vencido seria contado
        // de novo a cada limite posterior, prescrevendo a mais e destruindo horas validas (P1-5 da
        // AUDITORIA-FINAL). Prescreve-se apenas o vencido AINDA NAO prescrito, limitado ao saldo vigente.
        var jaPrescrito = _lancamentos
            .Where(l => l.Tipo == TipoLancamentoBancoHoras.Prescricao)
            .Sum(l => l.Minutos);

        var vencidosNaoPrescritos = creditosVencidos - jaPrescrito;
        var aPrescrever = Math.Min(vencidosNaoPrescritos, SaldoMinutos);
        if (aPrescrever <= 0)
        {
            return 0;
        }

        RegistrarLancamento(TipoLancamentoBancoHoras.Prescricao, aPrescrever, hoje, referencia, "Prescricao de creditos nao compensados na janela.");
        RaiseDomainEvent(new CreditosBancoHorasPrescritos(Id, ServidorId, aPrescrever, limiteData));
        return aPrescrever;
    }

    private void Lancar(TipoLancamentoBancoHoras tipo, int minutos, DateOnly data, string referencia, string descricao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        var referenciaNormalizada = referencia.Trim();

        // Idempotencia: a mesma referencia de negocio nunca lanca duas vezes (gancho at-least-once).
        if (_lancamentos.Any(l => string.Equals(l.Referencia, referenciaNormalizada, StringComparison.Ordinal)))
        {
            return;
        }

        RegistrarLancamento(tipo, minutos, data, referenciaNormalizada, descricao);
        RaiseDomainEvent(new LancamentoBancoHorasRegistrado(Id, ServidorId, tipo, minutos, SaldoMinutos));
    }

    private void RegistrarLancamento(TipoLancamentoBancoHoras tipo, int minutos, DateOnly data, string referencia, string descricao)
    {
        var lancamento = LancamentoBancoHoras.Criar(tipo, minutos, data, referencia, descricao);
        _lancamentos.Add(lancamento);

        // Credito soma; debito e prescricao subtraem do saldo corrente.
        SaldoMinutos += tipo == TipoLancamentoBancoHoras.Credito ? minutos : -minutos;
    }
}
