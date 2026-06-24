using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

/// <summary>Identificador forte do agregado <see cref="RestoAPagar"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RestoAPagarId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RestoAPagarId"/>.</returns>
    public static RestoAPagarId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Classificação do Resto a Pagar (Lei 4.320/64, art. 36).</summary>
public enum ClassificacaoRestoAPagar
{
    /// <summary>Processado — empenho liquidado e não pago.</summary>
    Processado = 1,

    /// <summary>Não Processado — empenho não liquidado.</summary>
    NaoProcessado = 2,
}

/// <summary>Situação do Resto a Pagar.</summary>
public enum SituacaoRestoAPagar
{
    /// <summary>Inscrito.</summary>
    Inscrito = 1,

    /// <summary>Pago.</summary>
    Pago = 2,

    /// <summary>Cancelado.</summary>
    Cancelado = 3,
}

/// <summary>
/// Resto a Pagar (Lei 4.320/64, art. 36): despesa empenhada e não paga ao fim do
/// exercício. SaldoAPagar = Inscrito − Pago − Cancelado.
/// </summary>
public sealed class RestoAPagar : AggregateRoot<RestoAPagarId>, IMustHaveTenant
{
    private RestoAPagar()
    {
    }

    private RestoAPagar(
        RestoAPagarId id,
        Guid tenantId,
        EmpenhoId empenhoId,
        ClassificacaoRestoAPagar classificacao,
        ValorMonetario valorInscrito,
        int exercicioOrigem,
        int exercicioInscricao)
        : base(id)
    {
        TenantId = tenantId;
        EmpenhoId = empenhoId;
        Classificacao = classificacao;
        ValorInscrito = valorInscrito;
        ValorLiquidado = ValorMonetario.Zero;
        ValorPago = ValorMonetario.Zero;
        ValorCancelado = ValorMonetario.Zero;
        ExercicioOrigem = exercicioOrigem;
        ExercicioInscricao = exercicioInscricao;
        Situacao = SituacaoRestoAPagar.Inscrito;
        RaiseDomainEvent(new RestoAPagarInscrito(id, empenhoId, exercicioInscricao));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Empenho de origem.</summary>
    public EmpenhoId EmpenhoId { get; private set; }

    /// <summary>Classificação (Processado/Não Processado).</summary>
    public ClassificacaoRestoAPagar Classificacao { get; private set; }

    /// <summary>Valor inscrito.</summary>
    public ValorMonetario ValorInscrito { get; private set; } = default!;

    /// <summary>Valor liquidado (relevante para Não Processado).</summary>
    public ValorMonetario ValorLiquidado { get; private set; } = default!;

    /// <summary>Valor pago.</summary>
    public ValorMonetario ValorPago { get; private set; } = default!;

    /// <summary>Valor cancelado.</summary>
    public ValorMonetario ValorCancelado { get; private set; } = default!;

    /// <summary>Exercício de origem do empenho.</summary>
    public int ExercicioOrigem { get; private set; }

    /// <summary>Exercício de inscrição em Restos a Pagar.</summary>
    public int ExercicioInscricao { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoRestoAPagar Situacao { get; private set; }

    /// <summary>Saldo a pagar = Inscrito − Pago − Cancelado.</summary>
    public ValorMonetario SaldoAPagar => ValorInscrito.Subtrair(ValorPago).Subtrair(ValorCancelado);

    /// <summary>Inscreve um resto a pagar.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="empenhoId">Empenho de origem.</param>
    /// <param name="classificacao">Classificação (Processado/Não Processado).</param>
    /// <param name="valorInscrito">Valor inscrito.</param>
    /// <param name="exercicioOrigem">Exercício de origem.</param>
    /// <param name="exercicioInscricao">Exercício de inscrição.</param>
    /// <returns>Novo <see cref="RestoAPagar"/>.</returns>
    public static RestoAPagar Inscrever(
        Guid tenantId,
        EmpenhoId empenhoId,
        ClassificacaoRestoAPagar classificacao,
        ValorMonetario valorInscrito,
        int exercicioOrigem,
        int exercicioInscricao)
    {
        ArgumentNullException.ThrowIfNull(valorInscrito);
        if (!valorInscrito.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valorInscrito), "Valor inscrito deve ser positivo.");
        }

        return new RestoAPagar(RestoAPagarId.New(), tenantId, empenhoId, classificacao, valorInscrito, exercicioOrigem, exercicioInscricao);
    }

    /// <summary>Liquida parcela de Resto a Pagar Não Processado.</summary>
    /// <param name="valor">Valor liquidado.</param>
    /// <exception cref="InvalidOperationException">Se Processado ou exceder o inscrito.</exception>
    public void Liquidar(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Classificacao != ClassificacaoRestoAPagar.NaoProcessado)
        {
            throw new InvalidOperationException("Apenas Restos a Pagar Nao Processados sao liquidados.");
        }

        var liquidavel = ValorInscrito.Subtrair(ValorLiquidado);
        if (valor.EhMaiorQue(liquidavel))
        {
            throw new SaldoEmpenhoInsuficienteException(liquidavel.Valor, valor.Valor);
        }

        ValorLiquidado = ValorLiquidado.Somar(valor);
        RaiseDomainEvent(new RestoAPagarLiquidado(Id, valor.Valor));
    }

    /// <summary>Registra pagamento de Resto a Pagar.</summary>
    /// <param name="valor">Valor pago.</param>
    /// <exception cref="SaldoLiquidacaoInsuficienteException">Se exceder o saldo (ou o liquidado, quando NP).</exception>
    public void RegistrarPagamento(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Situacao != SituacaoRestoAPagar.Inscrito)
        {
            throw new InvalidOperationException($"Resto a Pagar nao admite pagamento. Situacao atual: {Situacao}.");
        }

        if (valor.EhMaiorQue(SaldoAPagar))
        {
            throw new SaldoLiquidacaoInsuficienteException(SaldoAPagar.Valor, valor.Valor);
        }

        if (Classificacao == ClassificacaoRestoAPagar.NaoProcessado)
        {
            var pagavelNp = ValorLiquidado.Subtrair(ValorPago);
            if (valor.EhMaiorQue(pagavelNp))
            {
                throw new SaldoLiquidacaoInsuficienteException(pagavelNp.Valor, valor.Valor);
            }
        }

        ValorPago = ValorPago.Somar(valor);
        if (!SaldoAPagar.EhPositivo())
        {
            Situacao = SituacaoRestoAPagar.Pago;
        }

        RaiseDomainEvent(new RestoAPagarPago(Id, valor.Valor));
    }

    /// <summary>
    /// Cancela parcela do Resto a Pagar por prazo/decadência (Decreto 93.872/86 e normas
    /// TCE-RS). A janela legal é validada contra a <paramref name="politicaPrazo"/>
    /// parametrizável por tenant — fail-closed: cancelamento fora da vigência é rejeitado.
    /// </summary>
    /// <param name="valor">Valor a cancelar.</param>
    /// <param name="dataReferencia">Data de referência do cancelamento (competência).</param>
    /// <param name="politicaPrazo">Política de prazo/decadência configurada pelo tenant.</param>
    /// <exception cref="ArgumentNullException">Se <paramref name="politicaPrazo"/> for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se exceder o saldo a pagar.</exception>
    /// <exception cref="PrazoCancelamentoRestoAPagarException">Se o cancelamento ocorrer fora da janela legal.</exception>
    public void Cancelar(ValorMonetario valor, DateOnly dataReferencia, PoliticaPrazoRestoAPagar politicaPrazo)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentNullException.ThrowIfNull(politicaPrazo);

        // Fail-closed: cancelamento por prazo só é admitido dentro da janela de vigência
        // (do exercício de inscrição até o exercício-limite de decadência, inclusive),
        // conforme prazo parametrizável por tenant.
        var exercicioReferencia = dataReferencia.Year;
        var exercicioLimite = politicaPrazo.ExercicioLimiteVigencia(ExercicioInscricao, Classificacao);
        if (exercicioReferencia < ExercicioInscricao || exercicioReferencia > exercicioLimite)
        {
            throw new PrazoCancelamentoRestoAPagarException(exercicioReferencia, exercicioLimite);
        }

        if (valor.EhMaiorQue(SaldoAPagar))
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor a cancelar excede o saldo a pagar.");
        }

        ValorCancelado = ValorCancelado.Somar(valor);
        if (!SaldoAPagar.EhPositivo())
        {
            Situacao = SituacaoRestoAPagar.Cancelado;
        }
    }
}
