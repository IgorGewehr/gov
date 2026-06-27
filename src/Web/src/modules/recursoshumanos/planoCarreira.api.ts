// Camada de API do PLANO DE CARGOS E SALARIOS (PCCS) — módulo RecursosHumanos. Matriz salarial
// (classe x referência, vencimento derivado), enquadramento do servidor e movimentações
// (progressão horizontal por tempo/avaliação; promoção vertical por titulação/antiguidade).
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.PlanoCarreira.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situação (ciclo de vida) de um plano de carreira. */
export type SituacaoPlanoCarreira = 'Ativo' | 'Revogado';

/** Critério que fundamenta uma progressão horizontal. */
export type CriterioProgressao = 'TempoDeServico' | 'AvaliacaoDesempenho' | 'TempoEAvaliacao';

/** Resumo de um plano de carreira (linha de lista). */
export interface PlanoCarreiraResumo {
  id: string;
  denominacaoCarreira: string;
  leiInstituicao: string;
  vencimentoBase: number;
  numeroClasses: number;
  numeroReferencias: number;
  situacao: SituacaoPlanoCarreira;
}

/** Célula da matriz salarial (posição + vencimento derivado). */
export interface CelulaMatriz {
  classe: number;
  referencia: number;
  vencimento: number;
}

/** Detalhe de um plano de carreira com a matriz salarial completa. */
export interface PlanoCarreiraDetalhe {
  resumo: PlanoCarreiraResumo;
  percentualEntreReferencias: number;
  percentualEntreClasses: number;
  intersticioMeses: number;
  notaMinimaProgressao: number;
  matriz: CelulaMatriz[];
}

/** Item do histórico de movimentações funcionais. */
export interface MovimentacaoCarreiraItem {
  tipo: string;
  classeOrigem: number | null;
  referenciaOrigem: number | null;
  classeDestino: number;
  referenciaDestino: number;
  vencimentoResultante: number;
  criterio: string | null;
  dataEfeito: string;
  fundamento: string;
}

/** Enquadramento vigente de um servidor + histórico. */
export interface EnquadramentoDetalhe {
  enquadramentoId: string;
  servidorId: string;
  planoCarreiraId: string;
  denominacaoCarreira: string;
  classeAtual: number;
  referenciaAtual: number;
  vencimentoAtual: number;
  permiteProgressao: boolean;
  permitePromocao: boolean;
  movimentacoes: MovimentacaoCarreiraItem[];
}

/** Entrada da instituição de um plano de carreira. */
export interface InstituirPlanoInput {
  denominacaoCarreira: string;
  leiInstituicao: string;
  vencimentoBase: number;
  numeroClasses: number;
  numeroReferencias: number;
  percentualEntreReferencias: number;
  percentualEntreClasses: number;
  intersticioMeses: number;
  notaMinimaProgressao: number;
}

/** Entrada do enquadramento inicial de um servidor. */
export interface EnquadrarInput {
  servidorId: string;
  planoCarreiraId: string;
  classe: number;
  referencia: number;
  fundamento: string;
  dataEfeito?: string | null;
}

/** Entrada de uma progressão horizontal. */
export interface ProgressaoInput {
  /** 1=TempoDeServico, 2=AvaliacaoDesempenho, 3=TempoEAvaliacao (enum numérico do backend). */
  criterio: number;
  fundamento: string;
  portariaId?: string | null;
  dataEfeito?: string | null;
}

/** Entrada de uma promoção vertical. */
export interface PromocaoInput {
  fundamento: string;
  portariaId?: string | null;
  dataEfeito?: string | null;
}

const BASE = '/recursoshumanos/planos-carreira';

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarPlanos(signal?: AbortSignal): Promise<PlanoCarreiraResumo[]> {
  return http.get<PlanoCarreiraResumo[]>(BASE, { signal });
}

function obterPlano(id: string, signal?: AbortSignal): Promise<PlanoCarreiraDetalhe> {
  return http.get<PlanoCarreiraDetalhe>(`${BASE}/${id}`, { signal });
}

function instituirPlano(input: InstituirPlanoInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(BASE, input);
}

function obterEnquadramento(
  servidorId: string,
  signal?: AbortSignal,
): Promise<EnquadramentoDetalhe | null> {
  return http.get<EnquadramentoDetalhe | null>(`${BASE}/servidores/${servidorId}/enquadramento`, {
    signal,
  });
}

function enquadrar(input: EnquadrarInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/enquadramentos`, input);
}

function progredir(
  servidorId: string,
  input: ProgressaoInput,
): Promise<{ vencimento: number }> {
  return http.post<{ vencimento: number }>(`${BASE}/servidores/${servidorId}/progressao`, input);
}

function promover(servidorId: string, input: PromocaoInput): Promise<{ vencimento: number }> {
  return http.post<{ vencimento: number }>(`${BASE}/servidores/${servidorId}/promocao`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista os planos de carreira do tenant. */
export function usePlanosCarreira() {
  return useQuery({
    queryKey: rhKeys.planosCarreira(),
    queryFn: ({ signal }) => listarPlanos(signal),
  });
}

/** Detalhe de um plano de carreira com a matriz salarial derivada. */
export function usePlanoCarreira(id: string) {
  return useQuery({
    queryKey: rhKeys.planoCarreira(id),
    queryFn: ({ signal }) => obterPlano(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Enquadramento vigente de um servidor (posição + histórico). */
export function useEnquadramentoServidor(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.enquadramentoServidor(servidorId),
    queryFn: ({ signal }) => obterEnquadramento(servidorId, signal),
    enabled: servidorId.trim().length > 0,
  });
}

/** Institui um plano de carreira e invalida a lista. */
export function useInstituirPlanoCarreira() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: instituirPlano,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.planosCarreira() });
    },
  });
}

/** Enquadra um servidor e invalida o enquadramento do servidor. */
export function useEnquadrarServidor(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: enquadrar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.enquadramentoServidor(servidorId) });
    },
  });
}

/** Concede uma progressão horizontal e invalida o enquadramento. */
export function useConcederProgressao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ProgressaoInput) => progredir(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.enquadramentoServidor(servidorId) });
    },
  });
}

/** Concede uma promoção vertical e invalida o enquadramento. */
export function useConcederPromocao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PromocaoInput) => promover(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.enquadramentoServidor(servidorId) });
    },
  });
}
