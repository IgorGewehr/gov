// Camada de API do módulo Finanças — ciclo da despesa pública orçamentária (Lei 4.320/64):
// Dotação → Empenho → Liquidação → Pagamento → Restos a Pagar. Espelha o PADRÃO-OURO
// (Tributos): DTOs + enums no topo, query keys centralizadas, funções de acesso via http
// client tipado (Authorization + ProblemDetails) e hooks TanStack Query por operação.
//
// Contrato REAL (FinancasEndpoints.cs). Enums são serializados como INTEIROS pelo
// System.Text.Json default (sem JsonStringEnumConverter no ApiHost); por isso os
// comandos enviam o valor NUMÉRICO do enum, e os Resumos trazem a Situação como string.
// ---------------------------------------------------------------------------
// Enums do domínio (valor numérico = enum do backend)
// ---------------------------------------------------------------------------

/** Categoria econômica da despesa (ClassificacaoOrcamentaria). */
export const CATEGORIA_ECONOMICA = {
  DespesasCorrentes: 3,
  DespesasDeCapital: 4,
} as const;

/** Tipo de empenho (Lei 4.320/64, art. 60). */
export const TIPO_EMPENHO = {
  Ordinario: 1,
  Estimativo: 2,
  Global: 3,
} as const;

/** Natureza do credor. */
export const TIPO_PESSOA = {
  Fisica: 1,
  Juridica: 2,
} as const;

/** Documento comprobatório da liquidação. */
export const TIPO_DOCUMENTO_COMPROBATORIO = {
  NotaFiscal: 1,
  NfseChaveAcesso: 2,
  Fatura: 3,
  Recibo: 4,
  Outro: 5,
} as const;

// ---------------------------------------------------------------------------
// Query keys (fonte única de invalidação para todo o módulo)
// ---------------------------------------------------------------------------

export const financasKeys = {
  all: ['financas'] as const,

  dotacoes: () => [...financasKeys.all, 'dotacoes'] as const,
  dotacoesPorExercicio: (exercicio: number) =>
    [...financasKeys.dotacoes(), 'exercicio', exercicio] as const,
  dotacao: (id: string) => [...financasKeys.dotacoes(), 'detalhe', id] as const,

  empenhos: () => [...financasKeys.all, 'empenhos'] as const,
  empenhosPorDotacao: (dotacaoId: string) =>
    [...financasKeys.empenhos(), 'dotacao', dotacaoId] as const,
  empenho: (id: string) => [...financasKeys.empenhos(), 'detalhe', id] as const,

  liquidacoes: () => [...financasKeys.all, 'liquidacoes'] as const,
  liquidacoesPorEmpenho: (empenhoId: string) =>
    [...financasKeys.liquidacoes(), 'empenho', empenhoId] as const,
  liquidacao: (id: string) => [...financasKeys.liquidacoes(), 'detalhe', id] as const,

  pagamentos: () => [...financasKeys.all, 'ordens-pagamento'] as const,
  ordemPagamento: (id: string) => [...financasKeys.pagamentos(), 'detalhe', id] as const,

  restos: () => [...financasKeys.all, 'restos-a-pagar'] as const,
  restosPorExercicio: (exercicio: number) =>
    [...financasKeys.restos(), 'exercicio', exercicio] as const,

  credores: () => [...financasKeys.all, 'credores'] as const,
  credoresPorTermo: (termo: string) => [...financasKeys.credores(), 'busca', termo] as const,
  credor: (id: string) => [...financasKeys.credores(), 'detalhe', id] as const,
  credorExtrato: (id: string, exercicio: number | null) =>
    [...financasKeys.credor(id), 'extrato', exercicio ?? 'todos'] as const,
};

// ---------------------------------------------------------------------------
// Re-export tipado dos hooks por agregado (ponto único de import para as telas)
// ---------------------------------------------------------------------------

export * from './dotacao.api';
export * from './empenho.api';
export * from './liquidacao.api';
export * from './pagamento.api';
export * from './restoApagar.api';
export * from './credor.api';
