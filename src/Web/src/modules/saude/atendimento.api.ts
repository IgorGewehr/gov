// Camada de API do agregado Atendimento (módulo Saúde) — PEP/e-SUS APS.
// Contrato REAL (SaudeEndpoints.cs):
//   POST /saude/atendimentos                               -> RegistrarAtendimento          -> { id }
//   GET  /saude/atendimentos/{id}                          -> ObterAtendimentoPorId         -> AtendimentoDetalhe | null
//   GET  /saude/pacientes/{id}/atendimentos                -> ListarAtendimentosDoPaciente  -> AtendimentoResumo[]
//   POST /saude/atendimentos/{id}/evolucoes                -> AdicionarEvolucaoSOAP         -> 204
//   POST /saude/atendimentos/{id}/assinatura               -> AssinarAtendimento            -> 204
//   POST /saude/atendimentos/{id}/adendos                  -> AdicionarAdendo               -> 204
//   POST /saude/atendimentos/{id}/rnds                     -> CompartilharAtendimentoNaRNDS -> 204
//   POST /saude/atendimentos/{id}/sisab                    -> LancarAtendimentoNoSISAB      -> 204
//   POST /saude/atendimentos/{id}/cancelamento             -> CancelarAtendimento           -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';

// --- DTOs ---

/** Modalidade (1 = Presencial, 2 = Teleconsulta) — Lei 14.510/2022. */
export type ModalidadeAtendimento = 1 | 2;

export interface RegistrarAtendimentoInput {
  pacienteId: string;
  estabelecimentoId: string;
  profissionalId: string;
  dataHora: string; // ISO 8601 (DateTimeOffset)
  modalidade: ModalidadeAtendimento;
}

export interface AtendimentoResumo {
  id: string;
  dataHora: string;
  modalidade: string;
  situacao: string;
  cid: string | null;
  ciap: string | null;
}

export interface EvolucaoSOAP {
  id: string;
  subjetivo: string;
  objetivo: string;
  avaliacao: string;
  plano: string;
  dataHora: string;
  assinada: boolean;
  ehAdendo: boolean;
}

export interface PrescricaoItem {
  id: string;
  item: string;
  posologia: string;
  dataHora: string;
}

export interface SolicitacaoExameItem {
  id: string;
  procedimento: string;
  justificativa: string;
  dataHora: string;
}

export interface AtendimentoDetalhe {
  id: string;
  pacienteId: string;
  estabelecimentoId: string;
  profissionalId: string;
  dataHora: string;
  competencia: string;
  modalidade: string;
  situacao: string;
  nivelGarantia: string;
  assinado: boolean;
  evolucoes: EvolucaoSOAP[];
  prescricoes: PrescricaoItem[];
  exames: SolicitacaoExameItem[];
}

/** AdicionarEvolucaoPayload(Subjetivo, Objetivo, Avaliacao, Plano, Cid?, Ciap?). */
export interface AdicionarEvolucaoInput {
  subjetivo: string;
  objetivo: string;
  avaliacao: string;
  plano: string;
  cid?: string | null;
  ciap?: string | null;
}

/** AssinarAtendimentoPayload(CertificadoIcpBrasil, Hash). */
export interface AssinarAtendimentoInput {
  certificadoIcpBrasil: string;
  hash: string;
}

/** AdicionarAdendoPayload(EvolucaoReferenciadaId, Texto, CertificadoIcpBrasil, Hash). */
export interface AdicionarAdendoInput {
  evolucaoReferenciadaId: string;
  texto: string;
  certificadoIcpBrasil: string;
  hash: string;
}

/** CancelarAtendimentoPayload(Motivo). */
export interface CancelarAtendimentoInput {
  motivo: string;
}

// --- Acesso HTTP ---

function listarAtendimentosDoPaciente(
  pacienteId: string,
  signal?: AbortSignal,
): Promise<AtendimentoResumo[]> {
  return http.get<AtendimentoResumo[]>(`/saude/pacientes/${pacienteId}/atendimentos`, { signal });
}

function obterAtendimento(id: string, signal?: AbortSignal): Promise<AtendimentoDetalhe | null> {
  return http.get<AtendimentoDetalhe | null>(`/saude/atendimentos/${id}`, { signal });
}

async function registrarAtendimento(input: RegistrarAtendimentoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/atendimentos', input);
  return id;
}

function adicionarEvolucao(atendimentoId: string, input: AdicionarEvolucaoInput): Promise<void> {
  return http.post<void>(`/saude/atendimentos/${atendimentoId}/evolucoes`, input);
}

function assinarAtendimento(atendimentoId: string, input: AssinarAtendimentoInput): Promise<void> {
  return http.post<void>(`/saude/atendimentos/${atendimentoId}/assinatura`, input);
}

function adicionarAdendo(atendimentoId: string, input: AdicionarAdendoInput): Promise<void> {
  return http.post<void>(`/saude/atendimentos/${atendimentoId}/adendos`, input);
}

function compartilharNaRnds(atendimentoId: string): Promise<void> {
  return http.post<void>(`/saude/atendimentos/${atendimentoId}/rnds`);
}

function lancarNoSisab(atendimentoId: string): Promise<void> {
  return http.post<void>(`/saude/atendimentos/${atendimentoId}/sisab`);
}

function cancelarAtendimento(atendimentoId: string, input: CancelarAtendimentoInput): Promise<void> {
  return http.post<void>(`/saude/atendimentos/${atendimentoId}/cancelamento`, input);
}

// --- Hooks TanStack Query — QUERIES ---

/** Lista os atendimentos de um paciente. `enabled` controla disparo sob demanda. */
export function useAtendimentosDoPaciente(pacienteId: string, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.atendimentosPorPaciente(pacienteId),
    queryFn: ({ signal }) => listarAtendimentosDoPaciente(pacienteId, signal),
    enabled: enabled && pacienteId.trim().length > 0,
  });
}

/** Detalhe de um atendimento (notas SOAP, prescrições, exames). */
export function useAtendimento(id: string) {
  return useQuery({
    queryKey: saudeKeys.atendimento(id),
    queryFn: ({ signal }) => obterAtendimento(id, signal),
    enabled: id.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Registra um atendimento e invalida a linha do tempo do paciente. */
export function useRegistrarAtendimento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registrarAtendimento,
    onSuccess: (_id, input) => {
      queryClient.invalidateQueries({
        queryKey: saudeKeys.atendimentosPorPaciente(input.pacienteId),
      });
    },
  });
}

/** Adiciona uma evolução SOAP ao atendimento (editável). Invalida o detalhe. */
export function useAdicionarEvolucao(atendimentoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarEvolucaoInput) => adicionarEvolucao(atendimentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.atendimento(atendimentoId) });
    },
  });
}

/** Assina o atendimento em ICP-Brasil (NGS2 — torna imutável). Invalida o detalhe. */
export function useAssinarAtendimento(atendimentoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AssinarAtendimentoInput) => assinarAtendimento(atendimentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.atendimento(atendimentoId) });
    },
  });
}

/** Adiciona um adendo assinado a uma evolução de atendimento já assinado. Invalida o detalhe. */
export function useAdicionarAdendo(atendimentoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarAdendoInput) => adicionarAdendo(atendimentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.atendimento(atendimentoId) });
    },
  });
}

/** Compartilha o RES do atendimento na RNDS. Invalida o detalhe. */
export function useCompartilharNaRnds(atendimentoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => compartilharNaRnds(atendimentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.atendimento(atendimentoId) });
    },
  });
}

/** Lança o atendimento no SISAB (produção e-SUS APS). Invalida o detalhe. */
export function useLancarNoSisab(atendimentoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => lancarNoSisab(atendimentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.atendimento(atendimentoId) });
    },
  });
}

/** Cancela o atendimento (antes da assinatura) — terminal. Invalida o detalhe. */
export function useCancelarAtendimento(atendimentoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CancelarAtendimentoInput) => cancelarAtendimento(atendimentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.atendimento(atendimentoId) });
    },
  });
}
