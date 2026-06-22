// Camada de API do agregado ProntuarioSuas (módulo AssistenciaSocial).
// Acompanhamento familiar SIGILOSO PAIF/PAEFI (SUAS). Segue o padrão-ouro de Tributos:
//  - DTOs no topo (espelham os Commands/Queries reais da Application);
//  - query keys centralizadas para invalidação consistente;
//  - funções de acesso via http client tipado (Authorization + ProblemDetails→ApiError);
//  - hooks TanStack Query (useQuery/useMutation) para TODAS as operações.
//
// Rotas reais (AssistenciaSocialEndpoints.cs, prefixo /api):
//   POST  /assistenciasocial/prontuarios                              -> AbrirProntuario
//   POST  /assistenciasocial/prontuarios/{id}/atendimentos           -> RegistrarAtendimento
//   POST  /assistenciasocial/prontuarios/{id}/encerramento           -> EncerrarAcompanhamento
//   POST  /assistenciasocial/prontuarios/{id}/acessos                -> RegistrarAcessoProntuario
//   GET   /assistenciasocial/familias/{familiaId}/prontuario         -> ObterProntuarioDaFamilia (sigiloso)
//   GET   /assistenciasocial/prontuarios/{id}/trilha-acesso          -> ObterTrilhaAcessoProntuario
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (enums serializados como string — ToString() no backend)
// ---------------------------------------------------------------------------

/** Serviço socioassistencial (oferta da unidade). PAIF só em CRAS; PAEFI só em CREAS. */
export type TipoServico = 'Paif' | 'Paefi' | 'Scfv';

/** Situação do prontuário no ciclo de acompanhamento (estado terminal: Encerrado). */
export type SituacaoProntuario = 'Aberto' | 'Encerrado';

/** Resumo (metadado) de um atendimento — sem a descrição sigilosa. */
export interface RegistroResumo {
  servico: string;
  dataAtendimento: string;
}

/** Detalhe (conteúdo sigiloso) do prontuário de uma família — somente autorizado e auditado. */
export interface ProntuarioDetalhe {
  id: string;
  familiaId: string;
  unidadeAtendimentoId: string;
  situacao: string;
  dataAbertura: string;
  registros: RegistroResumo[];
  possuiViolacaoCriancaAdolescente: boolean;
}

/** Resumo imutável de um acesso da trilha (quem/quando/por quê). */
export interface AcessoResumo {
  acessoId: string;
  usuarioId: string;
  motivoAcesso: string;
  dataHoraAcessoUtc: string;
}

/** Entrada de AbrirProntuarioCommand. */
export interface AbrirProntuarioInput {
  familiaId: string;
  unidadeAtendimentoId: string;
}

/** Entrada de RegistrarAtendimentoCommand (Descricao é sigilosa). */
export interface RegistrarAtendimentoInput {
  servico: TipoServico;
  dataAtendimento: string;
  descricao: string;
  profissionalId: string;
}

/** Entrada de EncerrarAcompanhamentoCommand. */
export interface EncerrarAcompanhamentoInput {
  motivoEncerramento: string;
}

/** Entrada de RegistrarAcessoProntuarioCommand (justificativa obrigatória — trilha). */
export interface RegistrarAcessoInput {
  usuarioId: string;
  motivoAcesso: string;
}

/** Parâmetros de leitura sigilosa do prontuário de uma família (exige motivo de acesso). */
export interface ConsultaProntuarioParams {
  familiaId: string;
  usuarioId: string;
  motivoAcesso: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const prontuarioSuasKeys = {
  all: ['assistenciasocial', 'prontuarios-suas'] as const,
  prontuarioDaFamilia: (params: ConsultaProntuarioParams) =>
    [...prontuarioSuasKeys.all, 'familia', params.familiaId, params.usuarioId, params.motivoAcesso] as const,
  trilha: (prontuarioId: string) =>
    [...prontuarioSuasKeys.all, 'trilha-acesso', prontuarioId] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

/** ObterProntuarioDaFamilia — leitura SIGILOSA (registra trilha de acesso no backend). */
function obterProntuarioDaFamilia(
  params: ConsultaProntuarioParams,
  signal?: AbortSignal,
): Promise<ProntuarioDetalhe> {
  return http.get<ProntuarioDetalhe>(`/assistenciasocial/familias/${params.familiaId}/prontuario`, {
    query: { usuarioId: params.usuarioId, motivoAcesso: params.motivoAcesso },
    signal,
  });
}

/** ObterTrilhaAcessoProntuario — trilha imutável (quem/quando/por quê). */
function obterTrilhaAcesso(prontuarioId: string, signal?: AbortSignal): Promise<AcessoResumo[]> {
  return http.get<AcessoResumo[]>(`/assistenciasocial/prontuarios/${prontuarioId}/trilha-acesso`, { signal });
}

/** AbrirProntuario — retorna `{ id }`. */
function abrirProntuario(input: AbrirProntuarioInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/assistenciasocial/prontuarios', input);
}

/** RegistrarAtendimento — 204 No Content. */
function registrarAtendimento(prontuarioId: string, input: RegistrarAtendimentoInput): Promise<void> {
  return http.post<void>(`/assistenciasocial/prontuarios/${prontuarioId}/atendimentos`, input);
}

/** EncerrarAcompanhamento — 204 No Content (transição terminal). */
function encerrarAcompanhamento(prontuarioId: string, input: EncerrarAcompanhamentoInput): Promise<void> {
  return http.post<void>(`/assistenciasocial/prontuarios/${prontuarioId}/encerramento`, input);
}

/** RegistrarAcessoProntuario — 204 No Content (append-only na trilha). */
function registrarAcesso(prontuarioId: string, input: RegistrarAcessoInput): Promise<void> {
  return http.post<void>(`/assistenciasocial/prontuarios/${prontuarioId}/acessos`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/**
 * Leitura SIGILOSA do prontuário de uma família. Cada execução registra a trilha de
 * acesso no backend (I-7), por isso é disparada sob demanda (`enabled`) e não refaz
 * fetch em foco/reconexão. Exige família, usuário e motivo de acesso preenchidos.
 */
export function useProntuarioDaFamilia(params: ConsultaProntuarioParams, enabled = true) {
  const habilitado =
    enabled &&
    params.familiaId.trim().length > 0 &&
    params.usuarioId.trim().length > 0 &&
    params.motivoAcesso.trim().length > 0;
  return useQuery({
    queryKey: prontuarioSuasKeys.prontuarioDaFamilia(params),
    queryFn: ({ signal }) => obterProntuarioDaFamilia(params, signal),
    enabled: habilitado,
    // Leitura auditada: não revalidar automaticamente (cada fetch gera um acesso).
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    gcTime: 0,
    staleTime: Infinity,
    retry: false,
  });
}

/** Consulta da trilha de acesso imutável de um prontuário (controle social / TCE). */
export function useTrilhaAcesso(prontuarioId: string, enabled = true) {
  return useQuery({
    queryKey: prontuarioSuasKeys.trilha(prontuarioId),
    queryFn: ({ signal }) => obterTrilhaAcesso(prontuarioId, signal),
    enabled: enabled && prontuarioId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** AbrirProntuario — inicia o acompanhamento familiar. */
export function useAbrirProntuario() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirProntuario,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: prontuarioSuasKeys.all });
    },
  });
}

/** RegistrarAtendimento — adiciona atendimento PAIF/PAEFI/SCFV (mantém `Aberto`). */
export function useRegistrarAtendimento(prontuarioId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarAtendimentoInput) => registrarAtendimento(prontuarioId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: prontuarioSuasKeys.all });
    },
  });
}

/** EncerrarAcompanhamento — transição terminal `Aberto` → `Encerrado` (destrutiva). */
export function useEncerrarAcompanhamento(prontuarioId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: EncerrarAcompanhamentoInput) => encerrarAcompanhamento(prontuarioId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: prontuarioSuasKeys.all });
    },
  });
}

/** RegistrarAcessoProntuario — registra acesso manual na trilha (append-only). */
export function useRegistrarAcesso(prontuarioId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarAcessoInput) => registrarAcesso(prontuarioId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: prontuarioSuasKeys.trilha(prontuarioId) });
    },
  });
}

// ---------------------------------------------------------------------------
// Helpers de apresentação
// ---------------------------------------------------------------------------

import type { TagVariant } from '../../../components/ui';

/** Mapeia a situação do prontuário para a variante semântica da Tag. */
export function situacaoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberto':
      return 'success';
    case 'Encerrado':
      return 'default';
    default:
      return 'info';
  }
}

/** Opções de serviço para os selects (PAIF/PAEFI/SCFV). */
export const opcoesServico: { value: TipoServico; label: string }[] = [
  { value: 'Paif', label: 'PAIF — Proteção e Atendimento Integral à Família (CRAS)' },
  { value: 'Paefi', label: 'PAEFI — Atendimento Especializado a Famílias e Indivíduos (CREAS)' },
  { value: 'Scfv', label: 'SCFV — Serviço de Convivência e Fortalecimento de Vínculos' },
];
