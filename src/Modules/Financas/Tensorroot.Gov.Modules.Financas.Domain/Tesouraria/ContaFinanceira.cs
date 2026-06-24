using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

/// <summary>Identificador forte do agregado <see cref="ContaFinanceira"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContaFinanceiraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContaFinanceiraId"/>.</returns>
    public static ContaFinanceiraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade-filha <see cref="MovimentoFinanceiro"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MovimentoFinanceiroId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MovimentoFinanceiroId"/>.</returns>
    public static MovimentoFinanceiroId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Espécie da conta da tesouraria.</summary>
public enum TipoContaFinanceira
{
    /// <summary>Conta bancária (movimentação escritural via banco).</summary>
    Bancaria = 1,

    /// <summary>Caixa físico (numerário em espécie).</summary>
    Caixa = 2,
}

/// <summary>Situação da conta.</summary>
public enum SituacaoContaFinanceira
{
    /// <summary>Ativa (recebe movimentos).</summary>
    Ativa = 1,

    /// <summary>Encerrada (não recebe novos movimentos).</summary>
    Encerrada = 2,
}

/// <summary>Natureza do movimento financeiro na conta.</summary>
public enum TipoMovimentoFinanceiro
{
    /// <summary>Entrada — recebimento/arrecadação.</summary>
    Recebimento = 1,

    /// <summary>Saída — pagamento.</summary>
    Pagamento = 2,

    /// <summary>Saída da conta origem numa transferência entre contas.</summary>
    TransferenciaSaida = 3,

    /// <summary>Entrada na conta destino numa transferência entre contas.</summary>
    TransferenciaEntrada = 4,
}

/// <summary>
/// Conta da tesouraria (bancária ou caixa) com saldo escritural e extrato de movimentos
/// (recebimentos, pagamentos e transferências). Coração operacional diário do financeiro
/// (Lei 4.320/64, arts. 56/74; movimentação financeira do ente). O saldo nunca fica negativo:
/// não há cheque especial na administração pública (vedação de operação a descoberto).
/// </summary>
public sealed class ContaFinanceira : AggregateRoot<ContaFinanceiraId>, IMustHaveTenant
{
    private readonly List<MovimentoFinanceiro> _movimentos = [];

    private ContaFinanceira()
    {
    }

    private ContaFinanceira(
        ContaFinanceiraId id,
        Guid tenantId,
        string nome,
        TipoContaFinanceira tipo,
        ContaBancaria? dadosBancarios,
        ValorMonetario saldoInicial)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Tipo = tipo;
        DadosBancarios = dadosBancarios;
        SaldoInicial = saldoInicial;
        Saldo = saldoInicial;
        Situacao = SituacaoContaFinanceira.Ativa;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome/identificação da conta (ex.: "Conta Movimento — Banrisul 1234").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Espécie (bancária/caixa).</summary>
    public TipoContaFinanceira Tipo { get; private set; }

    /// <summary>Dados bancários (banco/agência/conta) — obrigatórios em conta bancária, nulos em caixa.</summary>
    public ContaBancaria? DadosBancarios { get; private set; }

    /// <summary>Saldo de abertura informado na criação.</summary>
    public ValorMonetario SaldoInicial { get; private set; } = default!;

    /// <summary>Saldo escritural corrente (derivado dos movimentos).</summary>
    public ValorMonetario Saldo { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoContaFinanceira Situacao { get; private set; }

    /// <summary>Movimentos (extrato cronológico).</summary>
    public IReadOnlyCollection<MovimentoFinanceiro> Movimentos => _movimentos.AsReadOnly();

    /// <summary>Abre uma conta da tesouraria.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nome">Nome/identificação.</param>
    /// <param name="tipo">Espécie (bancária/caixa).</param>
    /// <param name="dadosBancarios">Dados bancários (obrigatórios se bancária).</param>
    /// <param name="saldoInicial">Saldo de abertura (≥ 0).</param>
    /// <returns>Nova <see cref="ContaFinanceira"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se conta bancária sem dados bancários.</exception>
    public static ContaFinanceira Abrir(
        Guid tenantId,
        string nome,
        TipoContaFinanceira tipo,
        ContaBancaria? dadosBancarios,
        ValorMonetario saldoInicial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentNullException.ThrowIfNull(saldoInicial);
        if (tipo == TipoContaFinanceira.Bancaria && dadosBancarios is null)
        {
            throw new InvalidOperationException("Conta bancaria exige dados bancarios (banco/agencia/conta).");
        }

        return new ContaFinanceira(
            ContaFinanceiraId.New(),
            tenantId,
            nome.Trim(),
            tipo,
            tipo == TipoContaFinanceira.Bancaria ? dadosBancarios : null,
            saldoInicial);
    }

    /// <summary>Registra um recebimento (entrada).</summary>
    /// <param name="data">Data do movimento.</param>
    /// <param name="valor">Valor recebido (positivo).</param>
    /// <param name="historico">Descrição/origem.</param>
    /// <param name="documento">Documento de referência (opcional).</param>
    /// <param name="origemReferenciaId">Id do fato de origem (idempotência — opcional).</param>
    /// <returns>O movimento registrado.</returns>
    public MovimentoFinanceiro RegistrarRecebimento(
        DateOnly data,
        ValorMonetario valor,
        string historico,
        string? documento = null,
        Guid? origemReferenciaId = null)
        => Lancar(TipoMovimentoFinanceiro.Recebimento, data, valor, historico, documento, origemReferenciaId, null);

    /// <summary>Registra um pagamento (saída).</summary>
    /// <param name="data">Data do movimento.</param>
    /// <param name="valor">Valor pago (positivo).</param>
    /// <param name="historico">Descrição/credor.</param>
    /// <param name="documento">Documento de referência (opcional).</param>
    /// <param name="origemReferenciaId">Id do fato de origem (idempotência — opcional).</param>
    /// <returns>O movimento registrado.</returns>
    public MovimentoFinanceiro RegistrarPagamento(
        DateOnly data,
        ValorMonetario valor,
        string historico,
        string? documento = null,
        Guid? origemReferenciaId = null)
        => Lancar(TipoMovimentoFinanceiro.Pagamento, data, valor, historico, documento, origemReferenciaId, null);

    /// <summary>Debita esta conta como ORIGEM de uma transferência entre contas.</summary>
    /// <param name="data">Data do movimento.</param>
    /// <param name="valor">Valor transferido (positivo).</param>
    /// <param name="historico">Descrição.</param>
    /// <param name="contraparteContaId">Conta destino.</param>
    /// <returns>O movimento de saída.</returns>
    public MovimentoFinanceiro DebitarTransferencia(
        DateOnly data,
        ValorMonetario valor,
        string historico,
        ContaFinanceiraId contraparteContaId)
        => Lancar(TipoMovimentoFinanceiro.TransferenciaSaida, data, valor, historico, null, null, contraparteContaId);

    /// <summary>Credita esta conta como DESTINO de uma transferência entre contas.</summary>
    /// <param name="data">Data do movimento.</param>
    /// <param name="valor">Valor transferido (positivo).</param>
    /// <param name="historico">Descrição.</param>
    /// <param name="contraparteContaId">Conta origem.</param>
    /// <returns>O movimento de entrada.</returns>
    public MovimentoFinanceiro CreditarTransferencia(
        DateOnly data,
        ValorMonetario valor,
        string historico,
        ContaFinanceiraId contraparteContaId)
        => Lancar(TipoMovimentoFinanceiro.TransferenciaEntrada, data, valor, historico, null, null, contraparteContaId);

    /// <summary>Encerra a conta (não recebe mais movimentos).</summary>
    /// <exception cref="InvalidOperationException">Se já encerrada.</exception>
    public void Encerrar()
    {
        if (Situacao == SituacaoContaFinanceira.Encerrada)
        {
            throw new InvalidOperationException("Conta ja encerrada.");
        }

        Situacao = SituacaoContaFinanceira.Encerrada;
    }

    private MovimentoFinanceiro Lancar(
        TipoMovimentoFinanceiro tipo,
        DateOnly data,
        ValorMonetario valor,
        string historico,
        string? documento,
        Guid? origemReferenciaId,
        ContaFinanceiraId? contraparteContaId)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(historico);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor do movimento deve ser positivo.");
        }

        if (Situacao != SituacaoContaFinanceira.Ativa)
        {
            throw new InvalidOperationException($"Conta {Nome} nao esta ativa; nao aceita movimentos.");
        }

        var ehEntrada = tipo is TipoMovimentoFinanceiro.Recebimento or TipoMovimentoFinanceiro.TransferenciaEntrada;
        if (!ehEntrada && valor.EhMaiorQue(Saldo))
        {
            throw new InvalidOperationException(
                $"Saldo insuficiente na conta {Nome} (saldo {Saldo}, movimento {valor}). Operacao a descoberto vedada.");
        }

        var novoSaldo = ehEntrada ? Saldo.Somar(valor) : Saldo.Subtrair(valor);
        var movimento = MovimentoFinanceiro.Criar(Id, tipo, data, valor, novoSaldo, historico, documento, origemReferenciaId, contraparteContaId);
        _movimentos.Add(movimento);
        Saldo = novoSaldo;

        RaiseDomainEvent(new MovimentoFinanceiroRegistrado(Id, movimento.Id, tipo, valor.Valor, data));
        return movimento;
    }
}
