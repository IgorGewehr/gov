// Camada de API do PONTO ELETRONICO (Portaria MTP 671/2021) no modulo RecursosHumanos.
// Segue o PADRAO-OURO: DTOs no topo, acesso via http client tipado, hooks TanStack Query.
// Contrato real (provado, M5): src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
//   POST /ponto/jornadas                     -> define/substitui a jornada do servidor -> { id }
//   POST /ponto/marcacoes                    -> registra a batida (imutavel)           -> { nsr }
//   POST /ponto/apuracoes                    -> apura a competencia (espelho/banco)    -> { id, ... }
//   POST /ponto/apuracoes/{id}/fechamento    -> congela o espelho/AEJ (204)
//   GET  /ponto/afd  (binario)               -> Arquivo Fonte de Dados do periodo
//   GET  /ponto/aej  (binario)               -> Arquivo Eletronico de Jornada da competencia
//
// O backend nao expoe GET de marcacoes nem de apuracao: a tela compoe o espelho a partir
// da resposta da apuracao e mantem as marcacoes registradas em estado da pagina (cada POST
// devolve o NSR sequencial atribuido). Os arquivos AFD/AEJ sao baixados via Blob (fetch
// direto + Authorization), como na remessa TCE-RS — o http client tipado e JSON-only.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { getAccessToken } from '../../api/authToken';
import { ApiError } from '../../api/problemDetails';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Regime da jornada (rotulo -> enum numerico do backend). */
export type RegimeJornada = 'Estatutario' | 'Celetista';

/** Sentido de uma marcacao (rotulo -> enum numerico do backend). */
export type SentidoMarcacao = 'Entrada' | 'Saida';

/** Tipo de REP que originou a marcacao (rotulo -> enum numerico do backend). */
export type TipoRep = 'RepC' | 'RepA' | 'RepP';

/** Entrada da definicao/substituicao de jornada de um servidor. */
export interface DefinirJornadaInput {
  servidorId: string;
  cargaDiariaMinutos: number;
  intervaloMinutos: number;
  /** 1 = Estatutario, 2 = Celetista (enum numerico do backend). */
  regime: number;
  /** Inicio da vigencia (DateOnly AAAA-MM-DD). */
  vigenciaInicio: string;
  toleranciaMinutos: number;
}

/** Entrada do registro de uma marcacao (batida). */
export interface RegistrarMarcacaoInput {
  servidorId: string;
  /** Data/hora exata da batida (ISO 8601 com offset). */
  dataHora: string;
  /** 1 = Entrada, 2 = Saida (enum numerico do backend). */
  sentido: number;
  /** 1 = REP-C, 2 = REP-A, 3 = REP-P; ausente => padrao do tenant. */
  origem?: number | null;
}

/** Resposta do registro de marcacao: o NSR sequencial atribuido (REP). */
export interface MarcacaoRegistrada {
  nsr: number;
}

/** Entrada da apuracao de jornada de uma competencia. */
export interface ApurarJornadaInput {
  servidorId: string;
  ano: number;
  mes: number;
}

/**
 * Resposta da apuracao. O contrato garante o `id`; quando o backend tambem devolve o
 * espelho (minutos trabalhados/extras/falta + banco de horas), os campos opcionais sao
 * exibidos. Permanece compativel caso a resposta seja apenas `{ id }`.
 */
export interface ApuracaoResultado {
  id: string;
  minutosTrabalhados?: number;
  minutosDevidos?: number;
  minutosExtras?: number;
  minutosFalta?: number;
  saldoPeriodoMinutos?: number;
  saldoBancoHorasAnteriorMinutos?: number;
  saldoBancoHorasAtualMinutos?: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

const BASE = '/recursoshumanos/ponto';

function definirJornada(input: DefinirJornadaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/jornadas`, input);
}

function registrarMarcacao(input: RegistrarMarcacaoInput): Promise<MarcacaoRegistrada> {
  return http.post<MarcacaoRegistrada>(`${BASE}/marcacoes`, input);
}

function apurarJornada(input: ApurarJornadaInput): Promise<ApuracaoResultado> {
  return http.post<ApuracaoResultado>(`${BASE}/apuracoes`, input);
}

function fecharApuracao(apuracaoId: string): Promise<void> {
  return http.post<void>(`${BASE}/apuracoes/${apuracaoId}/fechamento`);
}

/**
 * Baixa um arquivo posicional do ponto (AFD/AEJ) e dispara o download no navegador.
 * Usa fetch direto (o http client tipado e JSON-only) com o Bearer atual, derivando o
 * nome do arquivo do header Content-Disposition quando presente.
 */
async function baixarArquivoPonto(
  recurso: 'afd' | 'aej',
  query: Record<string, string | number | boolean>,
  nomePadrao: string,
): Promise<void> {
  const token = getAccessToken();
  const params = new URLSearchParams();
  for (const [chave, valor] of Object.entries(query)) params.append(chave, String(valor));
  const url = `/api${BASE}/${recurso}?${params.toString()}`;

  const resp = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!resp.ok) {
    throw new ApiError(`Nao foi possivel gerar o arquivo (HTTP ${resp.status}).`, resp.status);
  }

  const disposition = resp.headers.get('content-disposition') ?? '';
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
  const nome = match ? decodeURIComponent(match[1].replace(/"/g, '')) : nomePadrao;

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

/** Gera e baixa o AFD (Arquivo Fonte de Dados) de um periodo [inicio, fim]. */
export function baixarAfd(inicio: string, fim: string): Promise<void> {
  return baixarArquivoPonto('afd', { inicio, fim }, `AFD_${inicio}_${fim}.txt`);
}

/** Gera e baixa o AEJ (Arquivo Eletronico de Jornada) de uma competencia. */
export function baixarAej(ano: number, mes: number): Promise<void> {
  return baixarArquivoPonto(
    'aej',
    { ano, mes },
    `AEJ_${ano}${String(mes).padStart(2, '0')}.txt`,
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Define (ou substitui) a jornada de um servidor e invalida as consultas de ponto. */
export function useDefinirJornada() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: definirJornada,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.ponto() });
    },
  });
}

/** Registra uma marcacao (batida) e devolve o NSR sequencial atribuido. */
export function useRegistrarMarcacao() {
  return useMutation({ mutationFn: registrarMarcacao });
}

/** Apura a jornada de uma competencia (espelho + banco de horas). */
export function useApurarJornada() {
  return useMutation({ mutationFn: apurarJornada });
}

/** Fecha a apuracao (congela o espelho/AEJ; gancho para a folha via Outbox). */
export function useFecharApuracao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: fecharApuracao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.ponto() });
    },
  });
}
