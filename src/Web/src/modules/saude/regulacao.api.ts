// Camada de API do agregado SolicitacaoRegulacao (módulo Saúde) — SISREG.
// Contrato REAL (SaudeEndpoints.cs):
//   POST /saude/regulacao/solicitacoes                         -> SolicitarRegulacao            -> { id }
//   GET  /saude/regulacao/solicitacoes/{id}                    -> ObterSolicitacaoRegulacaoPorId-> Detalhe | null
//   GET  /saude/regulacao/fila                                 -> ListarFilaDeRegulacao         -> Resumo[]
//   POST /saude/regulacao/solicitacoes/{id}/autorizacao        -> AutorizarSolicitacaoRegulacao -> 204
//   POST /saude/regulacao/solicitacoes/{id}/negativa           -> NegarSolicitacaoRegulacao     -> 204
//   POST /saude/regulacao/solicitacoes/{id}/devolucao          -> DevolverSolicitacaoRegulacao  -> 204
//   POST /saude/regulacao/solicitacoes/{id}/execucao           -> ExecutarSolicitacaoRegulacao  -> 204
//   POST /saude/regulacao/solicitacoes/{id}/cancelamento       -> CancelarSolicitacaoRegulacao  -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';

// --- DTOs ---

/** Classificação de risco/urgência (1 = Eletiva … 4 = Emergência). */
export type Prioridade = 1 | 2 | 3 | 4;

export interface SolicitarRegulacaoInput {
  pacienteId: string;
  estabelecimentoSolicitanteId: string;
  profissionalSolicitanteId: string;
  codigoSigtap: string;
  descricaoProcedimento: string;
  prioridade: Prioridade;
  justificativa: string;
}

export interface SolicitacaoRegulacaoResumo {
  id: string;
  pacienteId: string;
  codigoSigtap: string;
  prioridade: string;
  situacao: string;
  dataSolicitacao: string;
}

export interface SolicitacaoRegulacaoDetalhe {
  id: string;
  pacienteId: string;
  codigoSigtap: string;
  descricaoProcedimento: string;
  prioridade: string;
  situacao: string;
  dataSolicitacao: string;
  dataAutorizacao: string | null;
  protocoloSisreg: string | null;
}

/** MotivoPayload(Motivo) — negativa, devolução e cancelamento de regulação. */
export interface MotivoRegulacaoInput {
  motivo: string;
}

// --- Acesso HTTP ---

function listarFilaDeRegulacao(
  codigoSigtap: string | undefined,
  prioridade: Prioridade | undefined,
  signal?: AbortSignal,
): Promise<SolicitacaoRegulacaoResumo[]> {
  return http.get<SolicitacaoRegulacaoResumo[]>('/saude/regulacao/fila', {
    signal,
    query: { codigoSigtap: codigoSigtap || undefined, prioridade: prioridade ?? undefined },
  });
}

function obterSolicitacaoRegulacao(
  id: string,
  signal?: AbortSignal,
): Promise<SolicitacaoRegulacaoDetalhe | null> {
  return http.get<SolicitacaoRegulacaoDetalhe | null>(`/saude/regulacao/solicitacoes/${id}`, { signal });
}

async function solicitarRegulacao(input: SolicitarRegulacaoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/regulacao/solicitacoes', input);
  return id;
}

function autorizarSolicitacao(solicitacaoId: string): Promise<void> {
  return http.post<void>(`/saude/regulacao/solicitacoes/${solicitacaoId}/autorizacao`);
}

function negarSolicitacao(solicitacaoId: string, input: MotivoRegulacaoInput): Promise<void> {
  return http.post<void>(`/saude/regulacao/solicitacoes/${solicitacaoId}/negativa`, input);
}

function devolverSolicitacao(solicitacaoId: string, input: MotivoRegulacaoInput): Promise<void> {
  return http.post<void>(`/saude/regulacao/solicitacoes/${solicitacaoId}/devolucao`, input);
}

function executarSolicitacao(solicitacaoId: string): Promise<void> {
  return http.post<void>(`/saude/regulacao/solicitacoes/${solicitacaoId}/execucao`);
}

function cancelarSolicitacao(solicitacaoId: string, input: MotivoRegulacaoInput): Promise<void> {
  return http.post<void>(`/saude/regulacao/solicitacoes/${solicitacaoId}/cancelamento`, input);
}

// --- Hooks TanStack Query — QUERIES ---

/** Lista a fila de regulação (solicitações em análise), com filtros opcionais. */
export function useFilaDeRegulacao(codigoSigtap: string, prioridade: Prioridade | undefined) {
  return useQuery({
    queryKey: saudeKeys.filaRegulacao(codigoSigtap, prioridade ? String(prioridade) : 'todas'),
    queryFn: ({ signal }) =>
      listarFilaDeRegulacao(codigoSigtap.trim() || undefined, prioridade, signal),
  });
}

/** Detalhe de uma solicitação de regulação. */
export function useSolicitacaoRegulacao(id: string) {
  return useQuery({
    queryKey: saudeKeys.solicitacao(id),
    queryFn: ({ signal }) => obterSolicitacaoRegulacao(id, signal),
    enabled: id.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Abre uma solicitação de regulação e invalida a fila. */
export function useSolicitarRegulacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: solicitarRegulacao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.regulacao() });
    },
  });
}

/** Autoriza a solicitação (reserva SISREG, consome cota). Invalida detalhe e fila. */
export function useAutorizarSolicitacao(solicitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => autorizarSolicitacao(solicitacaoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.solicitacao(solicitacaoId) });
      queryClient.invalidateQueries({ queryKey: saudeKeys.regulacao() });
    },
  });
}

/** Nega a solicitação (terminal). Invalida detalhe e fila. */
export function useNegarSolicitacao(solicitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoRegulacaoInput) => negarSolicitacao(solicitacaoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.solicitacao(solicitacaoId) });
      queryClient.invalidateQueries({ queryKey: saudeKeys.regulacao() });
    },
  });
}

/** Devolve a solicitação ao solicitante para complementação. Invalida detalhe e fila. */
export function useDevolverSolicitacao(solicitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoRegulacaoInput) => devolverSolicitacao(solicitacaoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.solicitacao(solicitacaoId) });
      queryClient.invalidateQueries({ queryKey: saudeKeys.regulacao() });
    },
  });
}

/** Marca a solicitação autorizada como executada (procedimento realizado) — terminal. */
export function useExecutarSolicitacao(solicitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => executarSolicitacao(solicitacaoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.solicitacao(solicitacaoId) });
      queryClient.invalidateQueries({ queryKey: saudeKeys.regulacao() });
    },
  });
}

/** Cancela a solicitação (pelo solicitante) — terminal. Invalida detalhe e fila. */
export function useCancelarSolicitacao(solicitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoRegulacaoInput) => cancelarSolicitacao(solicitacaoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.solicitacao(solicitacaoId) });
      queryClient.invalidateQueries({ queryKey: saudeKeys.regulacao() });
    },
  });
}
