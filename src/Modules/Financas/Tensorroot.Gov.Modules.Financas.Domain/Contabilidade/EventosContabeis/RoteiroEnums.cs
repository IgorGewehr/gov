namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;

/// <summary>
/// Fato contábil do ciclo — gancho estável que conecta os domain events de execução
/// (empenho/liquidação/pagamento/receita/RP) ao roteiro contábil parametrizável.
/// </summary>
public enum FatoContabil
{
    /// <summary>Dotação aprovada / crédito reforçado (fixação da despesa).</summary>
    DotacaoAprovada = 1,

    /// <summary>Empenho emitido (reserva orçamentária).</summary>
    EmpenhoEmitido = 2,

    /// <summary>Empenho anulado.</summary>
    EmpenhoAnulado = 3,

    /// <summary>Despesa em liquidação (fato gerador, antes da liquidação formal).</summary>
    DespesaEmLiquidacao = 4,

    /// <summary>Despesa liquidada (2º estágio).</summary>
    DespesaLiquidada = 5,

    /// <summary>Liquidação estornada.</summary>
    LiquidacaoEstornada = 6,

    /// <summary>Pagamento efetuado (3º estágio).</summary>
    PagamentoEfetuado = 7,

    /// <summary>Receita arrecadada.</summary>
    ReceitaArrecadada = 8,

    /// <summary>Resto a pagar NÃO PROCESSADO inscrito (encerramento — empenhado não liquidado).</summary>
    RestoAPagarInscrito = 9,

    /// <summary>Resto a pagar liquidado.</summary>
    RestoAPagarLiquidado = 10,

    /// <summary>Resto a pagar pago.</summary>
    RestoAPagarPago = 11,

    /// <summary>Resto a pagar PROCESSADO inscrito (encerramento — liquidado não pago).</summary>
    RestoAPagarProcessadoInscrito = 12,
}

/// <summary>
/// Papel semântico de uma conta no roteiro. O seed mapeia cada papel → código PCASP real,
/// permitindo trocar o código analítico (especialmente patrimonial) sem alterar o roteiro.
/// </summary>
public enum PapelConta
{
    /// <summary>Crédito orçamentário disponível (6.2.2.1.1).</summary>
    CreditoDisponivel = 1,

    /// <summary>Crédito empenhado a liquidar (6.2.2.1.3.01).</summary>
    CreditoEmpenhadoALiquidar = 2,

    /// <summary>Crédito empenhado em liquidação (6.2.2.1.3.02).</summary>
    CreditoEmpenhadoEmLiquidacao = 3,

    /// <summary>Crédito empenhado liquidado a pagar (6.2.2.1.3.03).</summary>
    CreditoLiquidadoAPagar = 4,

    /// <summary>Crédito empenhado liquidado pago (6.2.2.1.3.04).</summary>
    CreditoLiquidadoPago = 5,

    /// <summary>Dotação orçamentária / fixação (5.2.2.1).</summary>
    DotacaoOrcamentaria = 6,

    /// <summary>Receita orçamentária a realizar (6.2.1.1).</summary>
    ReceitaARealizar = 7,

    /// <summary>Receita orçamentária realizada (6.2.1.2).</summary>
    ReceitaRealizada = 8,

    /// <summary>RP não processado inscrito (6.2.2.1.3.05).</summary>
    RestoAPagarNaoProcessado = 9,

    /// <summary>Caixa e equivalentes (1.1.1) — patrimonial. [validar-plano-oficial]</summary>
    CaixaEquivalentes = 20,

    /// <summary>Fornecedores e contas a pagar de curto prazo (2.1.3) — patrimonial. [validar-plano-oficial]</summary>
    FornecedoresCP = 21,

    /// <summary>VPD de uso de bens/serviços (3.x) — patrimonial. [validar-plano-oficial]</summary>
    VpdServicos = 22,

    /// <summary>VPA de impostos/transferências (4.x) — patrimonial. [validar-plano-oficial]</summary>
    VpaTributos = 23,
}

/// <summary>De onde sai o valor da partida roteirizada.</summary>
public enum BaseValorRoteiro
{
    /// <summary>Valor integral do fato (default).</summary>
    ValorDoFato = 1,
}
