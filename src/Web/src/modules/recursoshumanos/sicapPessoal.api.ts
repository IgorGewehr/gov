// Camada de API da remessa de auditoria de pessoal ao TCE-RS (SICAP-AP / SIAPESweb) — módulo
// RecursosHumanos. Abertura do lote, inclusão de atos de admissão (auto-preenchidos do servidor),
// geração do arquivo de importação (leiaute estadual 57 posições) e transmissão (registro de
// protocolo; envio real = M10).
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.SicapPessoal.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { getAccessToken } from '../../api/authToken';
import { ApiError } from '../../api/problemDetails';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situação (ciclo de vida) de uma remessa. */
export type SituacaoRemessaSicap = 'Aberta' | 'Gerada' | 'Transmitida';

/** Resumo de uma remessa de pessoal (linha de lista). */
export interface RemessaSicapResumo {
  id: string;
  codigoOrgao: number;
  sequencialLote: number;
  dataGeracaoLote: string;
  quantidadeAtos: number;
  situacao: SituacaoRemessaSicap;
  protocolo?: string | null;
}

/** Item de ato de admissão da remessa (CPF mascarado — LGPD). */
export interface AtoAdmissaoItem {
  id: string;
  identificadorAto: string;
  tipoAto: string;
  regime: string;
  nome: string;
  cpfMascarado: string;
  descricaoCargo: string;
  dataAto: string;
  dataTermino?: string | null;
}

/** Detalhe de uma remessa (cabeçalho + atos). */
export interface RemessaSicapDetalhe {
  resumo: RemessaSicapResumo;
  versaoLeiaute: number;
  atos: AtoAdmissaoItem[];
}

/** Entrada da abertura de uma remessa. */
export interface AbrirRemessaInput {
  codigoOrgao: number;
  dataGeracaoLote?: string | null;
  versaoLeiaute?: number | null;
}

/** Entrada da inclusão de ato a partir de servidor. */
export interface AtoDeServidorInput {
  servidorId: string;
  /** Sobrescrita opcional do título de admissão (enum numérico 1..15). */
  tituloOverride?: number | null;
  /** Sobrescrita opcional do regime jurídico (1=Celetista, 2=Estatutario, 3=Administrativo). */
  regimeOverride?: number | null;
  classificacaoConcurso?: number | null;
}

const BASE = '/recursoshumanos/sicap-pessoal/remessas';

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarRemessas(
  situacao: number | null,
  signal?: AbortSignal,
): Promise<RemessaSicapResumo[]> {
  return http.get<RemessaSicapResumo[]>(BASE, {
    signal,
    query: situacao != null ? { situacao } : {},
  });
}

function obterRemessa(id: string, signal?: AbortSignal): Promise<RemessaSicapDetalhe> {
  return http.get<RemessaSicapDetalhe>(`${BASE}/${id}`, { signal });
}

function abrirRemessa(input: AbrirRemessaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(BASE, input);
}

function adicionarAto(remessaId: string, input: AtoDeServidorInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/${remessaId}/atos`, input);
}

function transmitirRemessa(remessaId: string, protocolo: string): Promise<void> {
  return http.post<void>(`${BASE}/${remessaId}/transmissao`, { protocolo });
}

/**
 * Gera (fecha) a remessa e baixa o arquivo de importação SIAPES no navegador. POST que devolve
 * o arquivo posicional; usa fetch direto (o http client tipado é JSON-only) com o Bearer atual.
 */
async function gerarEbaixarRemessa(remessaId: string): Promise<void> {
  const token = getAccessToken();
  const url = `/api${BASE}/${remessaId}/geracao`;
  const resp = await fetch(url, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!resp.ok) {
    throw new ApiError(`Nao foi possivel gerar a remessa (HTTP ${resp.status}).`, resp.status);
  }

  const disposition = resp.headers.get('content-disposition') ?? '';
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
  const nome = match ? decodeURIComponent(match[1].replace(/"/g, '')) : `SIAPES_${remessaId}.txt`;

  const blob = await resp.blob();
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = nome;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(objectUrl);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as remessas de pessoal (filtro opcional por situação). */
export function useRemessasSicap(situacao: number | null) {
  return useQuery({
    queryKey: rhKeys.remessasSicap(String(situacao ?? '')),
    queryFn: ({ signal }) => listarRemessas(situacao, signal),
  });
}

/** Detalhe de uma remessa (cabeçalho + atos). */
export function useRemessaSicap(id: string) {
  return useQuery({
    queryKey: rhKeys.remessaSicap(id),
    queryFn: ({ signal }) => obterRemessa(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Abre uma remessa e invalida as listas. */
export function useAbrirRemessaSicap() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirRemessa,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.sicapPessoal() });
    },
  });
}

/** Inclui um ato a partir de servidor e invalida a remessa. */
export function useAdicionarAtoSicap(remessaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AtoDeServidorInput) => adicionarAto(remessaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.remessaSicap(remessaId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.sicapPessoal() });
    },
  });
}

/** Gera (fecha) a remessa, baixa o arquivo e invalida a remessa. */
export function useGerarRemessaSicap(remessaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => gerarEbaixarRemessa(remessaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.remessaSicap(remessaId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.sicapPessoal() });
    },
  });
}

/** Marca a remessa como transmitida (registra o protocolo) e invalida a remessa. */
export function useTransmitirRemessaSicap(remessaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (protocolo: string) => transmitirRemessa(remessaId, protocolo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.remessaSicap(remessaId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.sicapPessoal() });
    },
  });
}
