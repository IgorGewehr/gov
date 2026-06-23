// Camada de API do PAINEL FISCAL da Saúde (visão do GESTOR — conformidade
// constitucional). Espelha FIELMENTE o contrato REAL de SaudeEndpoints.cs (grupo
// /api/saude/fiscal/*):
//   GET  /saude/fiscal/asps/{exercicio}            -> ApuracaoAspsResultado   [saude.ver]
//   GET  /saude/fiscal/fms/{fundoId}/execucao      -> ExecucaoFmsResultado    [saude.ver]
//   POST /saude/fiscal/fms                          -> { id }                  [saude.gerenciar]
//   POST /saude/fiscal/fms/{fundoId}/parcelas       -> 204                     [saude.gerenciar]
//   POST /saude/fiscal/fms/{fundoId}/execucoes      -> 204                     [saude.gerenciar]
//
// Convenções (PADRÃO-OURO de tributos): DTOs no topo (campos camelCase, como o
// System.Text.Json serializa os records C#); enums por valor INT no payload;
// hooks por operação. Percentuais vêm do backend como FRAÇÃO (0..1).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';

// ---------------------------------------------------------------------------
// Enums (string na UI; INT no payload via *_VALOR — espelham BlocoFinanciamentoSaude)
// ---------------------------------------------------------------------------

/** Bloco de financiamento federal da Saúde (Port. GM/MS 3.992/2017). */
export type BlocoFinanciamentoSaude = 'Custeio' | 'Investimento';

export const BLOCO_SAUDE_VALOR: Record<BlocoFinanciamentoSaude, number> = {
  Custeio: 1,
  Investimento: 2,
};

/** Rótulo do bloco a partir do valor INT serializado pelo backend. */
export const BLOCO_SAUDE_LABEL: Record<number, string> = {
  1: 'Custeio (Manutenção das ASPS)',
  2: 'Investimento (Estruturação da Rede)',
};

// ---------------------------------------------------------------------------
// DTOs de retorno (espelham os records C#)
// ---------------------------------------------------------------------------

/** ApuracaoAspsResultado — apuração do mínimo de 15% ASPS (LC 141/2012). */
export interface ApuracaoAsps {
  exercicio: number;
  receitaBase: number;
  aplicadoAsps: number;
  /** Fração 0..1. */
  percentualAplicado: number;
  /** Fração 0..1 (mínimo vigente; default legal 0,15). */
  percentualMinimo: number;
  atingido: boolean;
}

/** SaldoBlocoFms — saldo segregado de um bloco do FMS. */
export interface SaldoBlocoFms {
  /** Valor INT do enum BlocoFinanciamentoSaude. */
  bloco: number;
  recebido: number;
  executado: number;
  saldo: number;
}

/** ExecucaoFmsResultado — execução do FMS por bloco. */
export interface ExecucaoFms {
  fundoId: string;
  nome: string;
  blocos: SaldoBlocoFms[];
}

// ---------------------------------------------------------------------------
// Entradas de comando (gestão do FMS — parametrizável)
// ---------------------------------------------------------------------------

/** AbrirFundoMunicipalSaudeCommand(Nome, Cnpj) — abre a unidade gestora do FMS. */
export interface AbrirFmsInput {
  nome: string;
  /** CNPJ próprio do Fundo (unidade gestora). */
  cnpj: string;
}

/** Parcela/execução por bloco — back: ParcelaFmsPayload (bloco INT, fonte, valor). */
export interface MovimentoBlocoFmsInput {
  bloco: number;
  fonteRecurso: string;
  valor: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterAsps(exercicio: number, signal?: AbortSignal): Promise<ApuracaoAsps> {
  return http.get<ApuracaoAsps>(`/saude/fiscal/asps/${exercicio}`, { signal });
}

function obterExecucaoFms(fundoId: string, signal?: AbortSignal): Promise<ExecucaoFms> {
  return http.get<ExecucaoFms>(`/saude/fiscal/fms/${fundoId}/execucao`, { signal });
}

function abrirFms(input: AbrirFmsInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/saude/fiscal/fms', input);
}

function receberParcelaFms(fundoId: string, input: MovimentoBlocoFmsInput): Promise<void> {
  return http.post<void>(`/saude/fiscal/fms/${fundoId}/parcelas`, input);
}

function executarDespesaFms(fundoId: string, input: MovimentoBlocoFmsInput): Promise<void> {
  return http.post<void>(`/saude/fiscal/fms/${fundoId}/execucoes`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Apuração ASPS de um exercício (15%). `enabled` controla o disparo. */
export function useApuracaoAsps(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.asps(exercicio),
    queryFn: ({ signal }) => obterAsps(exercicio, signal),
    enabled: enabled && Number.isFinite(exercicio) && exercicio > 0,
  });
}

/** Execução do FMS por bloco. `enabled` exige um fundoId válido. */
export function useExecucaoFms(fundoId: string) {
  return useQuery({
    queryKey: saudeKeys.execucaoFms(fundoId),
    queryFn: ({ signal }) => obterExecucaoFms(fundoId, signal),
    enabled: fundoId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS (gestão do FMS)
// ---------------------------------------------------------------------------

/** Abre a unidade gestora do Fundo Municipal de Saúde. */
export function useAbrirFms() {
  return useMutation({ mutationFn: abrirFms });
}

/** Recebe uma parcela do FNS num bloco/fonte (invalida a execução do fundo). */
export function useReceberParcelaFms(fundoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MovimentoBlocoFmsInput) => receberParcelaFms(fundoId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: saudeKeys.execucaoFms(fundoId) }),
  });
}

/** Executa despesa num bloco/fonte (transposição entre blocos é vedada). */
export function useExecutarDespesaFms(fundoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MovimentoBlocoFmsInput) => executarDespesaFms(fundoId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: saudeKeys.execucaoFms(fundoId) }),
  });
}
