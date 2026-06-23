using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Indicadores;

/// <summary>
/// (a) Execução orçamentária do exercício (empenhado/liquidado/pago vs dotação atualizada). Os percentuais
/// são frações (0..1); 0 quando a dotação ainda não foi publicada (sem divisão por zero).
/// </summary>
/// <param name="DotacaoAtualizada">Dotação atualizada (LOA + créditos) — denominador.</param>
/// <param name="Empenhado">Total empenhado.</param>
/// <param name="Liquidado">Total liquidado.</param>
/// <param name="Pago">Total pago.</param>
/// <param name="PercentualEmpenhado">Empenhado / dotação (0..1).</param>
/// <param name="PercentualLiquidado">Liquidado / dotação (0..1).</param>
/// <param name="PercentualPago">Pago / dotação (0..1).</param>
public sealed record ExecucaoOrcamentariaDto(
    decimal DotacaoAtualizada,
    decimal Empenhado,
    decimal Liquidado,
    decimal Pago,
    decimal PercentualEmpenhado,
    decimal PercentualLiquidado,
    decimal PercentualPago);

/// <summary>(b) Situação de um mínimo constitucional setorial (Saúde/Educação).</summary>
/// <param name="Setor">Setor (Saude/Educacao).</param>
/// <param name="ReceitaBase">Receita-base do setor.</param>
/// <param name="Aplicado">Valor aplicado computável.</param>
/// <param name="PercentualAplicado">Percentual aplicado (0..1).</param>
/// <param name="PercentualMinimo">Percentual mínimo vigente (0..1).</param>
/// <param name="Situacao">Situação apurada (Atingido/NaoAtingido).</param>
public sealed record MinimoConstitucionalDto(
    string Setor,
    decimal ReceitaBase,
    decimal Aplicado,
    decimal PercentualAplicado,
    decimal PercentualMinimo,
    string Situacao);

/// <summary>(c) Arrecadação tributária + posição da dívida ativa do exercício.</summary>
/// <param name="ArrecadacaoTributaria">Arrecadação tributária acumulada.</param>
/// <param name="DividaAtivaSaldoInscrito">Saldo inscrito (estoque) em dívida ativa.</param>
/// <param name="DividaAtivaSaldoAjuizado">Parcela ajuizada.</param>
/// <param name="DividaAtivaRecuperada">Recuperado no exercício.</param>
public sealed record ArrecadacaoDto(
    decimal ArrecadacaoTributaria,
    decimal DividaAtivaSaldoInscrito,
    decimal DividaAtivaSaldoAjuizado,
    decimal DividaAtivaRecuperada);

/// <summary>(d) Custo de pessoal e % da RCL (LRF) com semáforo frente aos limites legal/prudencial/alerta.</summary>
/// <param name="DespesaPessoal">Despesa total com pessoal (base LRF).</param>
/// <param name="ReceitaCorrenteLiquida">RCL (12 meses) — denominador.</param>
/// <param name="PercentualDaRcl">Despesa de pessoal / RCL (0..1).</param>
/// <param name="LimiteLegal">Limite legal vigente (0..1).</param>
/// <param name="LimitePrudencial">Limite prudencial vigente (0..1).</param>
/// <param name="LimiteAlerta">Limite de alerta vigente (0..1).</param>
/// <param name="Situacao">Semáforo (Adequado/Alerta/Excedido/Indeterminado).</param>
public sealed record DespesaPessoalLrfDto(
    decimal DespesaPessoal,
    decimal ReceitaCorrenteLiquida,
    decimal PercentualDaRcl,
    decimal LimiteLegal,
    decimal LimitePrudencial,
    decimal LimiteAlerta,
    SituacaoLimite Situacao);

/// <summary>(e) Prontidão de prestação de contas (remessas TCE-RS) do exercício.</summary>
/// <param name="RemessasEnviadas">Remessas transmitidas ao TCE-RS.</param>
/// <param name="RemessasComPrazoVencido">Remessas com prazo vencido sem envio.</param>
/// <param name="EmDia">Verdadeiro quando não há remessa com prazo vencido.</param>
/// <param name="Situacao">Semáforo (Adequado/Alerta/Excedido/Indeterminado).</param>
public sealed record PrestacaoContasDto(
    int RemessasEnviadas,
    int RemessasComPrazoVencido,
    bool EmDia,
    SituacaoLimite Situacao);

/// <summary>
/// Painel do Gestor consolidado de um exercício: os 5 KPIs reprodutíveis a partir dos read models
/// materializados via Integration Events. Cada KPI é independente e degrada graciosamente quando o dado
/// de origem ainda não chegou (zeros/Indeterminado), nunca inventando número.
/// </summary>
/// <param name="Exercicio">Exercício (ano) consolidado.</param>
/// <param name="ExecucaoOrcamentaria">(a) Execução orçamentária.</param>
/// <param name="Minimos">(b) Mínimos constitucionais (Saúde/Educação).</param>
/// <param name="Arrecadacao">(c) Arrecadação + dívida ativa.</param>
/// <param name="PessoalLrf">(d) Custo de pessoal + % da RCL (LRF).</param>
/// <param name="PrestacaoContas">(e) Prontidão de prestação de contas (TCE-RS).</param>
public sealed record PainelGestorDto(
    int Exercicio,
    ExecucaoOrcamentariaDto ExecucaoOrcamentaria,
    IReadOnlyList<MinimoConstitucionalDto> Minimos,
    ArrecadacaoDto Arrecadacao,
    DespesaPessoalLrfDto PessoalLrf,
    PrestacaoContasDto PrestacaoContas);
