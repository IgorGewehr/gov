using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;

/// <summary>Identificador forte do agregado <see cref="DotacaoOrcamentaria"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DotacaoOrcamentariaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DotacaoOrcamentariaId"/>.</returns>
    public static DotacaoOrcamentariaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação da dotação orçamentária.</summary>
public enum SituacaoDotacao
{
    /// <summary>Ativa — aceita empenhos e reforços.</summary>
    Ativa = 1,

    /// <summary>Bloqueada — vedada temporariamente a novos empenhos.</summary>
    Bloqueada = 2,

    /// <summary>Encerrada — exercício finalizado, sem novos empenhos.</summary>
    Encerrada = 3,
}

/// <summary>
/// Dotação Orçamentária (crédito orçamentário — Lei 4.320/64): controla o saldo
/// disponível para empenho. SaldoDisponivel = (Dotado + Reforcado − Anulado) − EmpenhadoLiquido.
/// </summary>
public sealed class DotacaoOrcamentaria : AggregateRoot<DotacaoOrcamentariaId>, IMustHaveTenant
{
    /// <summary>Exercício mínimo aceito.</summary>
    public const int ExercicioMinimo = 2000;

    private DotacaoOrcamentaria()
    {
    }

    private DotacaoOrcamentaria(
        DotacaoOrcamentariaId id,
        Guid tenantId,
        int exercicio,
        ClassificacaoOrcamentaria classificacao,
        ValorMonetario valorDotadoInicial)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Classificacao = classificacao;
        ValorDotadoInicial = valorDotadoInicial;
        ValorReforcado = ValorMonetario.Zero;
        ValorAnulado = ValorMonetario.Zero;
        ValorEmpenhadoLiquido = ValorMonetario.Zero;
        Situacao = SituacaoDotacao.Ativa;
        RaiseDomainEvent(new DotacaoCriada(id, exercicio));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício orçamentário.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Classificação orçamentária do crédito.</summary>
    public ClassificacaoOrcamentaria Classificacao { get; private set; } = default!;

    /// <summary>Dotação aprovada na LOA.</summary>
    public ValorMonetario ValorDotadoInicial { get; private set; } = default!;

    /// <summary>Soma de créditos suplementares/adicionais.</summary>
    public ValorMonetario ValorReforcado { get; private set; } = default!;

    /// <summary>Anulações de crédito.</summary>
    public ValorMonetario ValorAnulado { get; private set; } = default!;

    /// <summary>Soma dos empenhos vigentes (empenhado − anulado) que oneram esta dotação.</summary>
    public ValorMonetario ValorEmpenhadoLiquido { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoDotacao Situacao { get; private set; }

    /// <summary>LOA de origem (quando a dotação nasceu da LOA). Nulo em dotações legadas.</summary>
    public Guid? LoaId { get; private set; }

    /// <summary>Item de despesa fixada (QDD) de origem (1:1). Nulo em dotações legadas.</summary>
    public Guid? ItemDespesaFixadaId { get; private set; }

    /// <summary>Ação do PPA de origem (rastreabilidade da cadeia). Nulo em dotações legadas.</summary>
    public Guid? AcaoPpaId { get; private set; }

    /// <summary>Dotação atualizada = Dotado + Reforçado − Anulado.</summary>
    public ValorMonetario ValorAtualizado => ValorDotadoInicial.Somar(ValorReforcado).Subtrair(ValorAnulado);

    /// <summary>Saldo disponível para empenho = Atualizado − EmpenhadoLiquido.</summary>
    public ValorMonetario SaldoDisponivel => ValorAtualizado.Subtrair(ValorEmpenhadoLiquido);

    /// <summary>Cria uma dotação orçamentária.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício orçamentário.</param>
    /// <param name="classificacao">Classificação orçamentária.</param>
    /// <param name="valorDotado">Valor dotado inicial.</param>
    /// <returns>Nova <see cref="DotacaoOrcamentaria"/>.</returns>
    /// <exception cref="ArgumentException">Se o exercício for inválido.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor dotado não for positivo.</exception>
    public static DotacaoOrcamentaria Criar(
        Guid tenantId,
        int exercicio,
        ClassificacaoOrcamentaria classificacao,
        ValorMonetario valorDotado)
    {
        ArgumentNullException.ThrowIfNull(classificacao);
        ArgumentNullException.ThrowIfNull(valorDotado);
        if (exercicio < ExercicioMinimo)
        {
            throw new ArgumentException($"Exercicio deve ser maior ou igual a {ExercicioMinimo}.", nameof(exercicio));
        }

        if (!valorDotado.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valorDotado), "Valor dotado deve ser positivo.");
        }

        return new DotacaoOrcamentaria(DotacaoOrcamentariaId.New(), tenantId, exercicio, classificacao, valorDotado);
    }

    /// <summary>
    /// Cria uma dotação a partir de um item de despesa fixada da LOA (a dotação NASCE da LOA):
    /// <c>ValorDotadoInicial</c> = despesa fixada do item, com a origem rastreável (LOA/item/ação).
    /// Caminho de produção do vínculo planejamento→execução (DESIGN §6). Não quebra a execução legada.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício orçamentário.</param>
    /// <param name="classificacao">Classificação orçamentária do item.</param>
    /// <param name="valorFixado">Despesa fixada (= dotação inicial).</param>
    /// <param name="loaId">LOA de origem.</param>
    /// <param name="itemDespesaFixadaId">Item de despesa fixada de origem.</param>
    /// <param name="acaoPpaId">Ação do PPA de origem.</param>
    /// <returns>Nova <see cref="DotacaoOrcamentaria"/> com origem na LOA.</returns>
    public static DotacaoOrcamentaria CriarDeLoa(
        Guid tenantId,
        int exercicio,
        ClassificacaoOrcamentaria classificacao,
        ValorMonetario valorFixado,
        Guid loaId,
        Guid itemDespesaFixadaId,
        Guid acaoPpaId)
    {
        var dotacao = Criar(tenantId, exercicio, classificacao, valorFixado);
        dotacao.LoaId = loaId;
        dotacao.ItemDespesaFixadaId = itemDespesaFixadaId;
        dotacao.AcaoPpaId = acaoPpaId;
        return dotacao;
    }

    /// <summary>Reforça o crédito (crédito suplementar/adicional).</summary>
    /// <param name="valor">Valor do reforço.</param>
    /// <exception cref="InvalidOperationException">Se a dotação não estiver ativa.</exception>
    public void Reforcar(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirAtiva();
        ValorReforcado = ValorReforcado.Somar(valor);
        RaiseDomainEvent(new CreditoReforcado(Id, valor.Valor));
    }

    /// <summary>Anula crédito orçamentário (limitado ao saldo disponível).</summary>
    /// <param name="valor">Valor a anular.</param>
    /// <exception cref="SaldoOrcamentarioInsuficienteException">Se exceder o saldo disponível.</exception>
    public void AnularCredito(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirAtiva();
        if (valor.EhMaiorQue(SaldoDisponivel))
        {
            throw new SaldoOrcamentarioInsuficienteException(SaldoDisponivel.Valor, valor.Valor);
        }

        ValorAnulado = ValorAnulado.Somar(valor);
        RaiseDomainEvent(new CreditoAnulado(Id, valor.Valor));
    }

    /// <summary>Reserva saldo para um empenho (débito da dotação). Invariante central.</summary>
    /// <param name="valor">Valor a empenhar.</param>
    /// <exception cref="SaldoOrcamentarioInsuficienteException">Se exceder o saldo disponível.</exception>
    public void ReservarEmpenho(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (Situacao != SituacaoDotacao.Ativa)
        {
            throw new InvalidOperationException($"Dotacao nao aceita empenho. Situacao atual: {Situacao}.");
        }

        if (valor.EhMaiorQue(SaldoDisponivel))
        {
            throw new SaldoOrcamentarioInsuficienteException(SaldoDisponivel.Valor, valor.Valor);
        }

        ValorEmpenhadoLiquido = ValorEmpenhadoLiquido.Somar(valor);
    }

    /// <summary>Libera saldo ao anular um empenho (crédito de volta à dotação).</summary>
    /// <param name="valor">Valor a liberar.</param>
    public void LiberarEmpenho(ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ValorEmpenhadoLiquido = valor.EhMaiorQue(ValorEmpenhadoLiquido)
            ? ValorMonetario.Zero
            : ValorEmpenhadoLiquido.Subtrair(valor);
    }

    /// <summary>Bloqueia a dotação (veda novos empenhos temporariamente).</summary>
    public void Bloquear() => Situacao = SituacaoDotacao.Bloqueada;

    /// <summary>Encerra a dotação no fechamento do exercício.</summary>
    public void Encerrar() => Situacao = SituacaoDotacao.Encerrada;

    private void GarantirAtiva()
    {
        if (Situacao != SituacaoDotacao.Ativa)
        {
            throw new InvalidOperationException($"Operacao exige dotacao ativa. Situacao atual: {Situacao}.");
        }
    }
}
