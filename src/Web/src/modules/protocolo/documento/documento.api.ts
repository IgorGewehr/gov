// Camada de API do agregado Documento (modulo Protocolo) — segue o PADRAO-OURO de Tributos.
// Contrato REAL: ProtocoloEndpoints.cs (rotas /api/protocolo/...).
//   POST /documentos                              -> JuntarDocumentoCommand        (juntada; retorna { id })
//   GET  /processos/{processoId}/documentos       -> ListarDocumentosDoProcesso    (query)
//   POST /documentos/{documentoId}/assinaturas    -> AssinarDocumentoCommand       (transicao Assinar)
//   POST /documentos/{documentoId}/sem-efeito     -> TornarDocumentoSemEfeito      (transicao SemEfeito)
//   GET  /documentos/{documentoId}/integridade    -> VerificarIntegridadeDocumento (query)
// O http client ja prefixa /api. Convencoes: DTOs -> query keys -> funcoes http -> hooks.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham os enums/projecoes do dominio — Documento.rules.md secoes 2 e 6)
// ---------------------------------------------------------------------------

/** Situacao do documento no ciclo de vida (SituacaoDocumento). */
export type SituacaoDocumento = 'Rascunho' | 'Juntado' | 'Assinado' | 'SemEfeito';

/** Criticidade do ato — determina o nivel minimo de assinatura (CriticidadeAto). */
export type CriticidadeAto = 'Baixa' | 'Media' | 'Alta';

/** Nivel de visibilidade do documento (NivelDeAcesso). */
export type NivelDeAcesso = 'Publico' | 'Restrito' | 'Sigiloso';

/** Nivel da assinatura aplicada (TipoAssinatura; Lei 14.063/2020 + Decreto 10.543/2020). */
export type TipoAssinatura = 'AssinaturaSimples' | 'AssinaturaAvancada' | 'AssinaturaQualificada';

/** Valores numericos dos enums conforme o dominio (.NET serializa enums como numeros por padrao). */
export const CriticidadeValor: Record<CriticidadeAto, number> = {
  Baixa: 1,
  Media: 2,
  Alta: 3,
};

export const NivelAcessoValor: Record<NivelDeAcesso, number> = {
  Publico: 1,
  Restrito: 2,
  Sigiloso: 3,
};

export const TipoAssinaturaValor: Record<TipoAssinatura, number> = {
  AssinaturaSimples: 1,
  AssinaturaAvancada: 2,
  AssinaturaQualificada: 3,
};

/** Nivel minimo de assinatura exigido por criticidade (Decreto 10.543/2020; I-5/I-6). */
export const NivelMinimoPorCriticidade: Record<CriticidadeAto, TipoAssinatura> = {
  Baixa: 'AssinaturaSimples',
  Media: 'AssinaturaAvancada',
  Alta: 'AssinaturaQualificada',
};

/** Projecao minimizada (LGPD) retornada por ListarDocumentosDoProcesso. */
export interface DocumentoResumo {
  id: string;
  hash: string;
  criticidade: string;
  situacao: string;
  tipoAssinatura: string | null;
  dataJuntada: string | null;
}

/** Entrada da juntada (JuntarDocumentoCommand). */
export interface JuntarDocumentoInput {
  processoId: string;
  hash: string;
  criticidade: number;
  nivelAcesso: number;
  formatoPdfA: boolean;
}

/** Entrada da assinatura (AssinarDocumentoCommand / AssinarDocumentoPayload). */
export interface AssinarDocumentoInput {
  signatarioId: string;
  tipo: number;
}

/** Entrada do "tornar sem efeito" (TornarDocumentoSemEfeitoCommand / TornarSemEfeitoPayload). */
export interface TornarSemEfeitoInput {
  motivo: string;
}

/** Entrada da verificacao de integridade (VerificarIntegridadeDocumentoQuery). */
export interface VerificarIntegridadeInput {
  documentoId: string;
  hashRecalculado: string;
}

/** Resposta de criacao: o endpoint devolve { id }. */
export interface DocumentoCriadoResposta {
  id: string;
}

/** Resposta da verificacao de integridade: o endpoint devolve { integro }. */
export interface IntegridadeResposta {
  integro: boolean;
}

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao consistente)
// ---------------------------------------------------------------------------

export const documentoKeys = {
  all: ['protocolo', 'documentos'] as const,
  porProcesso: (processoId: string) => [...documentoKeys.all, 'processo', processoId] as const,
  integridade: (documentoId: string, hash: string) =>
    [...documentoKeys.all, 'integridade', documentoId, hash] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas reais de ProtocoloEndpoints.cs)
// ---------------------------------------------------------------------------

function listarDocumentosDoProcesso(processoId: string, signal?: AbortSignal): Promise<DocumentoResumo[]> {
  return http.get<DocumentoResumo[]>(`/protocolo/processos/${processoId}/documentos`, { signal });
}

function juntarDocumento(input: JuntarDocumentoInput): Promise<DocumentoCriadoResposta> {
  return http.post<DocumentoCriadoResposta>('/protocolo/documentos', input);
}

function assinarDocumento(documentoId: string, input: AssinarDocumentoInput): Promise<void> {
  // 204 No Content.
  return http.post<void>(`/protocolo/documentos/${documentoId}/assinaturas`, input);
}

function tornarDocumentoSemEfeito(documentoId: string, input: TornarSemEfeitoInput): Promise<void> {
  // 204 No Content.
  return http.post<void>(`/protocolo/documentos/${documentoId}/sem-efeito`, input);
}

function verificarIntegridade(
  documentoId: string,
  hashRecalculado: string,
  signal?: AbortSignal,
): Promise<IntegridadeResposta> {
  // O backend recebe `hashRecalculado` como query string (binding do Minimal API).
  return http.get<IntegridadeResposta>(`/protocolo/documentos/${documentoId}/integridade`, {
    query: { hashRecalculado },
    signal,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — TODAS as operacoes do agregado Documento
// ---------------------------------------------------------------------------

/** Query 6.1 — lista os documentos juntados a um processo (tenant-scoped). */
export function useDocumentosDoProcesso(processoId: string, enabled = true) {
  return useQuery({
    queryKey: documentoKeys.porProcesso(processoId),
    queryFn: ({ signal }) => listarDocumentosDoProcesso(processoId, signal),
    enabled: enabled && processoId.trim().length > 0,
  });
}

/** Query 6.2 — verifica a integridade de um documento (SHA-256; I-12). Sob demanda. */
export function useVerificarIntegridade(input: VerificarIntegridadeInput | null) {
  const habilitado = input !== null && input.documentoId.trim().length > 0 && input.hashRecalculado.trim().length > 0;
  return useQuery({
    queryKey: habilitado
      ? documentoKeys.integridade(input!.documentoId, input!.hashRecalculado)
      : [...documentoKeys.all, 'integridade', 'inativa'],
    queryFn: ({ signal }) => verificarIntegridade(input!.documentoId, input!.hashRecalculado, signal),
    enabled: habilitado,
    gcTime: 0,
  });
}

/** Command 5.1 — junta um documento (PDF/A + hash) e invalida a lista do processo. */
export function useJuntarDocumento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: juntarDocumento,
    onSuccess: (_resposta, variables) => {
      queryClient.invalidateQueries({ queryKey: documentoKeys.porProcesso(variables.processoId) });
    },
  });
}

/** Command 5.2 — assina um documento por criticidade (transicao Assinar). */
export function useAssinarDocumento(processoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ documentoId, input }: { documentoId: string; input: AssinarDocumentoInput }) =>
      assinarDocumento(documentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentoKeys.porProcesso(processoId) });
    },
  });
}

/** Command 5.3 — torna um documento sem efeito (transicao SemEfeito; preserva a trilha). */
export function useTornarDocumentoSemEfeito(processoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ documentoId, input }: { documentoId: string; input: TornarSemEfeitoInput }) =>
      tornarDocumentoSemEfeito(documentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentoKeys.porProcesso(processoId) });
    },
  });
}
