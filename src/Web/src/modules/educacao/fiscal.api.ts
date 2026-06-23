// Camada de API do PAINEL FISCAL da Educação (visão do GESTOR — conformidade
// constitucional). Espelha FIELMENTE o contrato REAL de EducacaoEndpoints.cs
// (grupo /api/educacao/fiscal/*):
//   GET  /educacao/fiscal/mde/{exercicio}                 -> ApuracaoMdeResultado      [educacao.ver]
//   GET  /educacao/fiscal/fundeb/{exercicio}/aplicacao    -> ApuracaoFundeb70Resultado [educacao.ver]
//   POST /educacao/fiscal/fundeb                           -> { id }                    [educacao.gerenciar]
//   POST /educacao/fiscal/fundeb/{id}/esperado             -> 204                       [educacao.gerenciar]
//   POST /educacao/fiscal/fundeb/{id}/parcelas             -> 204                       [educacao.gerenciar]
//   POST /educacao/fiscal/fundeb/remuneracao               -> 204                       [educacao.gerenciar]
//
// Convenções (PADRÃO-OURO de tributos): DTOs no topo (camelCase, como o
// System.Text.Json serializa os records C#); enums por valor INT no payload;
// hooks por operação. Percentuais vêm do backend como FRAÇÃO (0..1).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';

// ---------------------------------------------------------------------------
// Enums (string na UI; INT no payload — espelham OrigemRecursoFundeb / NaturezaAferimentoMde)
// ---------------------------------------------------------------------------

/** Origem do recurso do FUNDEB (Lei 14.113/2020). */
export type OrigemRecursoFundeb =
  | 'CotaParteEstadual'
  | 'ComplementacaoVaaf'
  | 'ComplementacaoVaat'
  | 'ComplementacaoVaar';

export const ORIGEM_FUNDEB_VALOR: Record<OrigemRecursoFundeb, number> = {
  CotaParteEstadual: 1,
  ComplementacaoVaaf: 2,
  ComplementacaoVaat: 3,
  ComplementacaoVaar: 4,
};

export const ORIGEM_FUNDEB_LABEL: Record<OrigemRecursoFundeb, string> = {
  CotaParteEstadual: 'Cota-parte estadual (VAAF do ente)',
  ComplementacaoVaaf: 'Complementação União — VAAF',
  ComplementacaoVaat: 'Complementação União — VAAT',
  ComplementacaoVaar: 'Complementação União — VAAR (por resultados)',
};

/** Natureza da aferição MDE (1 = anual de conformidade; 2 = bimestral informativo). */
export const NATUREZA_MDE_LABEL: Record<number, string> = {
  1: 'Aferição anual (conformidade — TCE)',
  2: 'Indicador bimestral (informativo)',
};

// ---------------------------------------------------------------------------
// DTOs de retorno (espelham os records C#)
// ---------------------------------------------------------------------------

/** ApuracaoMdeResultado — mínimo de 25% MDE (CF art. 212). */
export interface ApuracaoMde {
  exercicio: number;
  /** Valor INT do enum NaturezaAferimentoMde. */
  natureza: number;
  receitaBase: number;
  aplicadoMde: number;
  /** Fração 0..1. */
  percentualAplicado: number;
  /** Fração 0..1 (mínimo vigente; default legal 0,25). */
  percentualMinimo: number;
  atingido: boolean;
  /** Se é a aferição anual de conformidade (a que vale para o TCE). */
  ehConformidade: boolean;
}

/** ApuracaoFundeb70Resultado — piso de 70% na remuneração do magistério. */
export interface ApuracaoFundeb70 {
  exercicio: number;
  receitaFundeb: number;
  remuneracaoProfissionais: number;
  /** Fração 0..1. */
  percentualAplicado: number;
  /** Fração 0..1 (piso vigente; default legal 0,70). */
  pisoMinimo: number;
  atingido: boolean;
}

// ---------------------------------------------------------------------------
// Entradas de comando (gestão da distribuição FUNDEB — parametrizável)
// ---------------------------------------------------------------------------

/** AbrirDistribuicaoFundebCommand(Exercicio). */
export interface AbrirDistribuicaoFundebInput {
  exercicio: number;
}

/** DefinirEsperadoFundebPayload(Origem INT, ValorEsperado). */
export interface DefinirEsperadoFundebInput {
  origem: number;
  valorEsperado: number;
}

/** ReceberParcelaFundebPayload(Origem INT, Valor). */
export interface ReceberParcelaFundebInput {
  origem: number;
  valor: number;
}

/** RegistrarRemuneracaoMagisterioCommand(Exercicio, RemuneracaoProfissionais). */
export interface RegistrarRemuneracaoInput {
  exercicio: number;
  remuneracaoProfissionais: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterMde(exercicio: number, signal?: AbortSignal): Promise<ApuracaoMde> {
  return http.get<ApuracaoMde>(`/educacao/fiscal/mde/${exercicio}`, { signal });
}

function obterFundeb70(exercicio: number, signal?: AbortSignal): Promise<ApuracaoFundeb70> {
  return http.get<ApuracaoFundeb70>(`/educacao/fiscal/fundeb/${exercicio}/aplicacao`, { signal });
}

function abrirDistribuicao(input: AbrirDistribuicaoFundebInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/educacao/fiscal/fundeb', input);
}

function definirEsperado(distribuicaoId: string, input: DefinirEsperadoFundebInput): Promise<void> {
  return http.post<void>(`/educacao/fiscal/fundeb/${distribuicaoId}/esperado`, input);
}

function receberParcela(distribuicaoId: string, input: ReceberParcelaFundebInput): Promise<void> {
  return http.post<void>(`/educacao/fiscal/fundeb/${distribuicaoId}/parcelas`, input);
}

function registrarRemuneracao(input: RegistrarRemuneracaoInput): Promise<void> {
  return http.post<void>('/educacao/fiscal/fundeb/remuneracao', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Apuração MDE de um exercício (25% — aferição anual de conformidade). */
export function useApuracaoMde(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.mde(exercicio),
    queryFn: ({ signal }) => obterMde(exercicio, signal),
    enabled: enabled && Number.isFinite(exercicio) && exercicio > 0,
  });
}

/** Apuração do piso de 70% do FUNDEB (remuneração do magistério). */
export function useApuracaoFundeb70(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.fundebAplicacao(exercicio),
    queryFn: ({ signal }) => obterFundeb70(exercicio, signal),
    enabled: enabled && Number.isFinite(exercicio) && exercicio > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS (gestão da distribuição FUNDEB)
// ---------------------------------------------------------------------------

/** Abre a distribuição do FUNDEB de um exercício. */
export function useAbrirDistribuicaoFundeb() {
  return useMutation({ mutationFn: abrirDistribuicao });
}

/** Define o valor esperado (FNDE/Estado) de uma origem do FUNDEB. */
export function useDefinirEsperadoFundeb(distribuicaoId: string) {
  return useMutation({
    mutationFn: (input: DefinirEsperadoFundebInput) => definirEsperado(distribuicaoId, input),
  });
}

/** Registra o recebimento de uma parcela do FUNDEB numa origem. */
export function useReceberParcelaFundeb(distribuicaoId: string) {
  return useMutation({
    mutationFn: (input: ReceberParcelaFundebInput) => receberParcela(distribuicaoId, input),
  });
}

/** Registra a remuneração paga ao magistério (numerador dos 70%). */
export function useRegistrarRemuneracao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registrarRemuneracao,
    onSuccess: (_data, vars) =>
      queryClient.invalidateQueries({ queryKey: educacaoKeys.fundebAplicacao(vars.exercicio) }),
  });
}
