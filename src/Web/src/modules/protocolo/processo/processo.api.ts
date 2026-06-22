// Camada de API do agregado Processo (módulo Protocolo) — Processo Administrativo
// Eletrônico (PAE). Segue o PADRÃO-OURO do módulo Tributos:
//   - DTOs no topo (espelham os Commands/Queries reais de ...Protocolo.Application);
//   - query keys centralizadas para invalidação consistente;
//   - funções de acesso via http client tipado (Authorization + ProblemDetails);
//   - hooks TanStack Query (useQuery/useMutation) para CADA operação.
//
// Contrato REAL (ProtocoloEndpoints.cs):
//   POST   /api/protocolo/processos                              -> AutuarProcesso       -> { id }
//   GET    /api/protocolo/processos/{nup}                        -> ObterProcessoPorNup  -> ProcessoDetalhe | 404
//   GET    /api/protocolo/setores/{setorId}/processos           -> ListarProcessosDoSetor-> ProcessoResumo[]
//   POST   /api/protocolo/processos/{processoId}/tramitacoes    -> TramitarProcesso      -> 204
//   POST   /api/protocolo/processos/{processoId}/despachos      -> DespacharProcesso     -> 204
//   POST   /api/protocolo/processos/{processoId}/sobrestamento  -> SobrestarProcesso     -> 204
//   POST   /api/protocolo/processos/{processoId}/arquivamento   -> ArquivarProcesso      -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham os enums e projeções do domínio Protocolo.Processo)
// ---------------------------------------------------------------------------

/** Nível de acesso (visibilidade) — enum NivelDeAcesso (1..3). */
export type NivelDeAcesso = 'Publico' | 'Restrito' | 'Sigiloso';

/** Situação do processo no ciclo de vida do PAE — enum SituacaoProcesso (1..4). */
export type SituacaoProcesso = 'Autuado' | 'EmTramitacao' | 'Sobrestado' | 'Arquivado';

/** Valor numérico do enum NivelDeAcesso esperado pelo backend (IsInEnum). */
export const NIVEL_ACESSO_VALOR: Record<NivelDeAcesso, number> = {
  Publico: 1,
  Restrito: 2,
  Sigiloso: 3,
};

/** Projeção de resumo (ListarProcessosDoSetor). */
export interface ProcessoResumo {
  id: string;
  nup: string;
  classificacao: string;
  situacao: SituacaoProcesso;
  dataAutuacao: string;
}

/** Projeção de detalhe (ObterProcessoPorNup). */
export interface ProcessoDetalhe {
  id: string;
  nup: string;
  classificacao: string;
  nivelAcesso: NivelDeAcesso;
  situacao: SituacaoProcesso;
  dataAutuacao: string;
  setorAtualId: string | null;
  origemModulo: string | null;
}

// --- Entradas de comando (espelham os Commands/Payloads reais) ---

/** AutuarProcessoCommand(RequerimentoId, Classificacao, NivelAcesso, OrigemModulo, OrigemId). */
export interface AutuarProcessoInput {
  requerimentoId?: string | null;
  classificacao: string;
  nivelAcesso: number;
  origemModulo?: string | null;
  origemId?: string | null;
}

/** TramitarProcessoPayload(SetorDestinoId, Observacao). */
export interface TramitarProcessoInput {
  setorDestinoId: string;
  observacao?: string | null;
}

/** DespacharProcessoPayload(Texto, AutoridadeId). */
export interface DespacharProcessoInput {
  texto: string;
  autoridadeId: string;
}

/** SobrestarProcessoPayload(Motivo). */
export interface SobrestarProcessoInput {
  motivo: string;
}

/** ArquivarProcessoPayload(Motivo). */
export interface ArquivarProcessoInput {
  motivo?: string | null;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const processoKeys = {
  all: ['protocolo', 'processos'] as const,
  porNup: (nup: string) => [...processoKeys.all, 'nup', nup] as const,
  porSetor: (setorId: string) => [...processoKeys.all, 'setor', setorId] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterProcessoPorNup(nup: string, signal?: AbortSignal): Promise<ProcessoDetalhe> {
  return http.get<ProcessoDetalhe>(`/protocolo/processos/${encodeURIComponent(nup)}`, { signal });
}

function listarProcessosDoSetor(setorId: string, signal?: AbortSignal): Promise<ProcessoResumo[]> {
  return http.get<ProcessoResumo[]>(`/protocolo/setores/${setorId}/processos`, { signal });
}

function autuarProcesso(input: AutuarProcessoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/protocolo/processos', input);
}

function tramitarProcesso(processoId: string, input: TramitarProcessoInput): Promise<void> {
  return http.post<void>(`/protocolo/processos/${processoId}/tramitacoes`, input);
}

function despacharProcesso(processoId: string, input: DespacharProcessoInput): Promise<void> {
  return http.post<void>(`/protocolo/processos/${processoId}/despachos`, input);
}

function sobrestarProcesso(processoId: string, input: SobrestarProcessoInput): Promise<void> {
  return http.post<void>(`/protocolo/processos/${processoId}/sobrestamento`, input);
}

function arquivarProcesso(processoId: string, input: ArquivarProcessoInput): Promise<void> {
  return http.post<void>(`/protocolo/processos/${processoId}/arquivamento`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Consulta o detalhe de um processo pelo NUP (sob demanda via `enabled`). */
export function useProcessoPorNup(nup: string, enabled = true) {
  return useQuery({
    queryKey: processoKeys.porNup(nup),
    queryFn: ({ signal }) => obterProcessoPorNup(nup, signal),
    enabled: enabled && nup.trim().length > 0,
  });
}

/** Lista os processos cujo setor atual é o informado (sob demanda via `enabled`). */
export function useProcessosDoSetor(setorId: string, enabled = true) {
  return useQuery({
    queryKey: processoKeys.porSetor(setorId),
    queryFn: ({ signal }) => listarProcessosDoSetor(setorId, signal),
    enabled: enabled && setorId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Autua um novo processo administrativo (gera o NUP). Invalida as listas afetadas. */
export function useAutuarProcesso(setorIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: autuarProcesso,
    onSuccess: () => {
      if (setorIdParaInvalidar && setorIdParaInvalidar.trim().length > 0) {
        queryClient.invalidateQueries({ queryKey: processoKeys.porSetor(setorIdParaInvalidar) });
      } else {
        queryClient.invalidateQueries({ queryKey: processoKeys.all });
      }
    },
  });
}

/** Tramita o processo para um setor de destino. Invalida detalhe e listas de setor. */
export function useTramitarProcesso(nup: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ processoId, input }: { processoId: string; input: TramitarProcessoInput }) =>
      tramitarProcesso(processoId, input),
    onSuccess: (_data, variables) => {
      if (nup) queryClient.invalidateQueries({ queryKey: processoKeys.porNup(nup) });
      queryClient.invalidateQueries({ queryKey: processoKeys.porSetor(variables.input.setorDestinoId) });
    },
  });
}

/** Registra um despacho (append-only) no processo, sem alterar a situação. */
export function useDespacharProcesso(nup: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ processoId, input }: { processoId: string; input: DespacharProcessoInput }) =>
      despacharProcesso(processoId, input),
    onSuccess: () => {
      if (nup) queryClient.invalidateQueries({ queryKey: processoKeys.porNup(nup) });
    },
  });
}

/** Sobresta o processo (suspende o andamento). Invalida o detalhe. */
export function useSobrestarProcesso(nup: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ processoId, input }: { processoId: string; input: SobrestarProcessoInput }) =>
      sobrestarProcesso(processoId, input),
    onSuccess: () => {
      if (nup) queryClient.invalidateQueries({ queryKey: processoKeys.porNup(nup) });
    },
  });
}

/** Arquiva o processo (terminal — TTD/CONARQ). Invalida o detalhe. */
export function useArquivarProcesso(nup: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ processoId, input }: { processoId: string; input: ArquivarProcessoInput }) =>
      arquivarProcesso(processoId, input),
    onSuccess: () => {
      if (nup) queryClient.invalidateQueries({ queryKey: processoKeys.porNup(nup) });
    },
  });
}
