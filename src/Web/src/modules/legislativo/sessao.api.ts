// Camada de API do agregado Sessao (modulo Legislativo). DTOs + acesso HTTP
// tipado + hooks TanStack Query, cobrindo os 12 endpoints de /api/legislativo/sessoes.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

export interface SessaoResumo {
  id: string;
  tipo: string;
  dataHora: string;
  situacao: string;
}

export interface ItemOrdemDoDiaResumo {
  proposicaoId: string;
  ordem: number;
}

export interface SessaoDetalhe {
  id: string;
  tipo: string;
  dataHora: string;
  totalMembros: number;
  quorumInstalacao: number;
  presentes: number;
  situacao: string;
  ordemDoDia: ItemOrdemDoDiaResumo[];
}

export interface AgendarSessaoInput {
  tipo: number;
  dataHora: string;
  totalMembros: number;
}

/** Resumo de presenca registrada em uma sessao. */
export interface PresencaResumo {
  vereadorId: string;
  registradaEm: string;
}

/** Payload de registro de presenca de vereador. */
export interface RegistrarPresencaInput {
  vereadorId: string;
}

/** Payload de inclusao de proposicao na Ordem do Dia da sessao. */
export interface IncluirNaOrdemDoDiaInput {
  proposicaoId: string;
}

/** Ata textual gerada de uma sessao. */
export interface AtaSessao {
  sessaoId: string;
  conteudo: string;
  geradaEm: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarSessoesAgendadas(signal?: AbortSignal): Promise<SessaoResumo[]> {
  return http.get<SessaoResumo[]>('/legislativo/sessoes/agendadas', { signal });
}

function obterSessao(id: string, signal?: AbortSignal): Promise<SessaoDetalhe> {
  return http.get<SessaoDetalhe>(`/legislativo/sessoes/${id}`, { signal });
}

function obterPresencas(sessaoId: string, signal?: AbortSignal): Promise<PresencaResumo[]> {
  return http.get<PresencaResumo[]>(`/legislativo/sessoes/${sessaoId}/presencas`, { signal });
}

function obterAta(sessaoId: string, signal?: AbortSignal): Promise<AtaSessao> {
  return http.get<AtaSessao>(`/legislativo/sessoes/${sessaoId}/ata`, { signal });
}

async function agendarSessao(input: AgendarSessaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/sessoes', input);
  return id;
}

function registrarPresenca(sessaoId: string, input: RegistrarPresencaInput): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/presencas`, input);
}

function incluirNaOrdemDoDia(sessaoId: string, input: IncluirNaOrdemDoDiaInput): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/ordem-do-dia`, input);
}

async function verificarQuorum(sessaoId: string): Promise<boolean> {
  const r = await http.post<{ quorumAtingido: boolean }>(`/legislativo/sessoes/${sessaoId}/quorum`);
  return r.quorumAtingido;
}

function abrirSessao(sessaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/abertura`);
}

function suspenderSessao(sessaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/suspensao`);
}

function reabrirSessao(sessaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/reabertura`);
}

function encerrarSessao(sessaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/encerramento`);
}

function cancelarSessao(sessaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/sessoes/${sessaoId}/cancelamento`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as sessoes agendadas do tenant. */
export function useSessoesAgendadas() {
  return useQuery({
    queryKey: legislativoKeys.sessoesAgendadas(),
    queryFn: ({ signal }) => listarSessoesAgendadas(signal),
  });
}

/** Ata textual de uma sessao (gerada/exibida sob demanda). */
export function useAtaSessao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.sessaoAta(id),
    queryFn: ({ signal }) => obterAta(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Detalhe de uma sessao (inclui quorum, presentes e Ordem do Dia). */
export function useSessao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.sessao(id),
    queryFn: ({ signal }) => obterSessao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Lista as presencas registradas em uma sessao. */
export function usePresencasSessao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.sessaoPresencas(id),
    queryFn: ({ signal }) => obterPresencas(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Agenda uma nova sessao e invalida as listas afetadas. */
export function useAgendarSessao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: agendarSessao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.sessoes() });
    },
  });
}

/** Invalida o detalhe/presencas de uma sessao e a lista de agendadas. */
function useInvalidarSessao(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.sessao(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.sessaoPresencas(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.sessoes() });
  };
}

/** Registra a presenca de um vereador na sessao. */
export function useRegistrarPresenca(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({
    mutationFn: (input: RegistrarPresencaInput) => registrarPresenca(id, input),
    onSuccess: invalidar,
  });
}

/** Inclui uma proposicao na Ordem do Dia da sessao. */
export function useIncluirNaOrdemDoDia(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({
    mutationFn: (input: IncluirNaOrdemDoDiaInput) => incluirNaOrdemDoDia(id, input),
    onSuccess: invalidar,
  });
}

/** Verifica o quorum da sessao (devolve se foi atingido). */
export function useVerificarQuorum(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({ mutationFn: () => verificarQuorum(id), onSuccess: invalidar });
}

/** Abre a sessao (transicao Agendada -> Aberta). */
export function useAbrirSessao(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({ mutationFn: () => abrirSessao(id), onSuccess: invalidar });
}

/** Suspende a sessao (Aberta -> Suspensa). */
export function useSuspenderSessao(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({ mutationFn: () => suspenderSessao(id), onSuccess: invalidar });
}

/** Reabre a sessao (Suspensa -> Aberta). */
export function useReabrirSessao(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({ mutationFn: () => reabrirSessao(id), onSuccess: invalidar });
}

/** Encerra a sessao. */
export function useEncerrarSessao(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({ mutationFn: () => encerrarSessao(id), onSuccess: invalidar });
}

/** Cancela a sessao. */
export function useCancelarSessao(id: string) {
  const invalidar = useInvalidarSessao(id);
  return useMutation({ mutationFn: () => cancelarSessao(id), onSuccess: invalidar });
}
