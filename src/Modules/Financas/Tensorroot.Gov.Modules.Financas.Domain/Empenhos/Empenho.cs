using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Empenhos;

/// <summary>Identificador forte do agregado <see cref="Empenho"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EmpenhoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EmpenhoId"/>.</returns>
    public static EmpenhoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Tipo de empenho (Lei 4.320/64, art. 60).</summary>
public enum TipoEmpenho
{
    /// <summary>Valor fixo e pagamento único.</summary>
    Ordinario = 1,

    /// <summary>Montante indeterminado (água, energia, combustível).</summary>
    Estimativo = 2,

    /// <summary>Valor conhecido sujeito a parcelamento.</summary>
    Global = 3,
}

/// <summary>Situação do empenho considerando os saldos de liquidação e pagamento.</summary>
public enum SituacaoEmpenho
{
    /// <summary>Empenhado (nada liquidado).</summary>
    Empenhado = 1,

    /// <summary>Parcialmente liquidado.</summary>
    ParcialmenteLiquidado = 2,

    /// <summary>Totalmente liquidado (saldo a liquidar zerado).</summary>
    TotalmenteLiquidado = 3,

    /// <summary>Parcialmente pago.</summary>
    ParcialmentePago = 4,

    /// <summary>Totalmente pago.</summary>
    TotalmentePago = 5,

    /// <summary>Anulado parcialmente.</summary>
    AnuladoParcial = 6,

    /// <summary>Anulado totalmente.</summary>
    Anulado = 7,

    /// <summary>Inscrito em Restos a Pagar.</summary>
    InscritoRestosAPagar = 8,
}

/// <summary>
/// Empenho — 1º estágio da despesa (Lei 4.320/64, art. 58-60). Onera uma dotação e
/// controla os saldos: SaldoEmpenhado = Empenhado − Anulado; SaldoALiquidar =
/// SaldoEmpenhado − Liquidado; SaldoAPagar = Liquidado − Pago.
/// </summary>
public sealed class Empenho : AggregateRoot<EmpenhoId>, IMustHaveTenant
{
    private Empenho()
    {
    }

    private Empenho(
        EmpenhoId id,
        Guid tenantId,
        string numero,
        DotacaoOrcamentariaId dotacaoId,
        Credor credor,
        ValorMonetario valorEmpenhado,
        TipoEmpenho tipo,
        int exercicio,
        DateOnly dataEmpenho)
        : base(id)
    {
        TenantId = tenantId;
        Numero = numero;
        DotacaoId = dotacaoId;
        Credor = credor;
        ValorEmpenhado = valorEmpenhado;
        ValorAnulado = ValorMonetario.Zero;
        ValorLiquidado = ValorMonetario.Zero;
        ValorPago = ValorMonetario.Zero;
        Tipo = tipo;
        Exercicio = exercicio;
        DataEmpenho = dataEmpenho;
        Situacao = SituacaoEmpenho.Empenhado;
        RaiseDomainEvent(new EmpenhoEmitido(id, dotacaoId, valorEmpenhado.Valor));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Número do empenho.</summary>
    public string Numero { get; private set; } = default!;

    /// <summary>Dotação onerada.</summary>
    public DotacaoOrcamentariaId DotacaoId { get; private set; }

    /// <summary>Credor.</summary>
    public Credor Credor { get; private set; } = default!;

    /// <summary>Valor empenhado.</summary>
    public ValorMonetario ValorEmpenhado { get; private set; } = default!;

    /// <summary>Valor anulado.</summary>
    public ValorMonetario ValorAnulado { get; private set; } = default!;

    /// <summary>Valor liquidado acumulado.</summary>
    public ValorMonetario ValorLiquidado { get; private set; } = default!;

    /// <summary>Valor pago acumulado.</summary>
    public ValorMonetario ValorPago { get; private set; } = default!;

    /// <summary>Tipo de empenho.</summary>
    public TipoEmpenho Tipo { get; private set; }

    /// <summary>Exercício orçamentário.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Data do empenho.</summary>
    public DateOnly DataEmpenho { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoEmpenho Situacao { get; private set; }

    /// <summary>Saldo empenhado = Empenhado − Anulado.</summary>
    public ValorMonetario SaldoEmpenhado => ValorEmpenhado.Subtrair(ValorAnulado);

    /// <summary>Saldo a liquidar = SaldoEmpenhado − Liquidado.</summary>
    public ValorMonetario SaldoALiquidar => SaldoEmpenhado.Subtrair(ValorLiquidado);

    /// <summary>Saldo a pagar = Liquidado − Pago.</summary>
    public ValorMonetario SaldoAPagar => ValorLiquidado.Subtrair(ValorPago);

    /// <summary>Emite um novo empenho onerando a dotação informada.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numero">Número do empenho.</param>
    /// <param name="dotacaoId">Dotação onerada.</param>
    /// <param name="credor">Credor.</param>
    /// <param name="valor">Valor a empenhar.</param>
    /// <param name="tipo">Tipo de empenho.</param>
    /// <param name="exercicio">Exercício orçamentário.</param>
    /// <param name="dataEmpenho">Data do empenho.</param>
    /// <returns>Novo <see cref="Empenho"/>.</returns>
    public static Empenho Emitir(
        Guid tenantId,
        string numero,
        DotacaoOrcamentariaId dotacaoId,
        Credor credor,
        ValorMonetario valor,
        TipoEmpenho tipo,
        int exercicio,
        DateOnly dataEmpenho)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        ArgumentNullException.ThrowIfNull(credor);
        ArgumentNullException.ThrowIfNull(valor);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor empenhado deve ser positivo.");
        }

        return new Empenho(EmpenhoId.New(), tenantId, numero, dotacaoId, credor, valor, tipo, exercicio, dataEmpenho);
    }

    /// <summary>Anula parcialmente o empenho (até o saldo a liquidar).</summary>
    /// <param name="valor">Valor a anular.</param>
    /// <exception cref="SaldoEmpenhoInsuficienteException">Se exceder o saldo a liquidar.</exception>
    public void AnularParcial(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirAtivo();
        if (valor.EhMaiorQue(SaldoALiquidar))
        {
            throw new SaldoEmpenhoInsuficienteException(SaldoALiquidar.Valor, valor.Valor);
        }

        ValorAnulado = ValorAnulado.Somar(valor);
        Situacao = SaldoEmpenhado.EhPositivo() ? SituacaoEmpenho.AnuladoParcial : SituacaoEmpenho.Anulado;
        AtualizarSituacaoPorSaldos();
        RaiseDomainEvent(new EmpenhoAnulado(Id, valor.Valor));
    }

    /// <summary>Anula totalmente o empenho (somente sem liquidação registrada).</summary>
    /// <exception cref="InvalidOperationException">Se houver valor liquidado.</exception>
    public void AnularTotal()
    {
        if (ValorLiquidado.EhPositivo())
        {
            throw new InvalidOperationException("Nao e possivel anular totalmente empenho com liquidacao registrada.");
        }

        AnularParcial(SaldoALiquidar);
        Situacao = SituacaoEmpenho.Anulado;
    }

    /// <summary>Registra liquidação de parcela do empenho (Lei 4.320/64, art. 63).</summary>
    /// <param name="valor">Valor liquidado.</param>
    /// <exception cref="SaldoEmpenhoInsuficienteException">Se exceder o saldo a liquidar.</exception>
    public void RegistrarLiquidacao(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirAtivo();
        if (valor.EhMaiorQue(SaldoALiquidar))
        {
            throw new SaldoEmpenhoInsuficienteException(SaldoALiquidar.Valor, valor.Valor);
        }

        ValorLiquidado = ValorLiquidado.Somar(valor);
        AtualizarSituacaoPorSaldos();
    }

    /// <summary>Estorna liquidação (reduz o valor liquidado).</summary>
    /// <param name="valor">Valor a estornar.</param>
    /// <exception cref="SaldoEmpenhoInsuficienteException">Se exceder o liquidado não pago.</exception>
    public void EstornarLiquidacao(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        var liquidadoNaoPago = ValorLiquidado.Subtrair(ValorPago);
        if (valor.EhMaiorQue(liquidadoNaoPago))
        {
            throw new SaldoEmpenhoInsuficienteException(liquidadoNaoPago.Valor, valor.Valor);
        }

        ValorLiquidado = ValorLiquidado.Subtrair(valor);
        AtualizarSituacaoPorSaldos();
    }

    /// <summary>Registra pagamento de parcela liquidada (Lei 4.320/64, art. 62).</summary>
    /// <param name="valor">Valor pago.</param>
    /// <exception cref="SaldoLiquidacaoInsuficienteException">Se exceder o saldo a pagar.</exception>
    public void RegistrarPagamento(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);

        // C-B6 — Fail-closed: o pagamento ordinario so se aplica a empenho ATIVO do exercicio. Empenho
        // Anulado/TotalmentePago nao admite pagamento; e empenho InscritoRestosAPagar tem fluxo PROPRIO
        // (pagamento de RP no exercicio seguinte), pois aqui AtualizarSituacaoPorSaldos retornaria cedo e
        // deixaria ValorPago avancando com a situacao travada em InscritoRestosAPagar (estado ambiguo).
        GarantirAtivo();

        if (valor.EhMaiorQue(SaldoAPagar))
        {
            throw new SaldoLiquidacaoInsuficienteException(SaldoAPagar.Valor, valor.Valor);
        }

        ValorPago = ValorPago.Somar(valor);
        AtualizarSituacaoPorSaldos();
    }

    /// <summary>Inscreve o empenho em Restos a Pagar no encerramento do exercício.</summary>
    /// <param name="exercicioInscricao">Exercício de inscrição.</param>
    /// <exception cref="InvalidOperationException">Se não houver saldo a inscrever.</exception>
    public void InscreverEmRestosAPagar(int exercicioInscricao)
    {
        if (!SaldoEmpenhado.Subtrair(ValorPago).EhPositivo())
        {
            throw new InvalidOperationException("Empenho sem saldo a inscrever em Restos a Pagar.");
        }

        Situacao = SituacaoEmpenho.InscritoRestosAPagar;
    }

    /// <summary>Indica se há saldo a inscrever em Restos a Pagar (empenhado − pago).</summary>
    /// <returns><c>true</c> se houver saldo.</returns>
    public bool PossuiSaldoParaRestosAPagar() => SaldoEmpenhado.Subtrair(ValorPago).EhPositivo();

    private void AtualizarSituacaoPorSaldos()
    {
        if (Situacao is SituacaoEmpenho.Anulado or SituacaoEmpenho.InscritoRestosAPagar)
        {
            return;
        }

        if (ValorPago.EhPositivo())
        {
            Situacao = SaldoAPagar.EhPositivo() || SaldoALiquidar.EhPositivo()
                ? SituacaoEmpenho.ParcialmentePago
                : SituacaoEmpenho.TotalmentePago;
            return;
        }

        if (ValorLiquidado.EhPositivo())
        {
            Situacao = SaldoALiquidar.EhPositivo()
                ? SituacaoEmpenho.ParcialmenteLiquidado
                : SituacaoEmpenho.TotalmenteLiquidado;
            return;
        }

        Situacao = ValorAnulado.EhPositivo() ? SituacaoEmpenho.AnuladoParcial : SituacaoEmpenho.Empenhado;
    }

    private void GarantirAtivo()
    {
        if (Situacao is SituacaoEmpenho.Anulado or SituacaoEmpenho.InscritoRestosAPagar or SituacaoEmpenho.TotalmentePago)
        {
            throw new InvalidOperationException($"Empenho nao admite a operacao. Situacao atual: {Situacao}.");
        }
    }
}
