// Camada de API da PRESTAÇÃO DE CONTAS ao TCE-RS (SIAPC/PAD) — contrato M4.
// Fluxo do agregado Remessa: Gerada → Validada → ProntaParaTransmissao → Enviada.
//   gerar (POST)             -> consolida o pacote do exercício/período
//   validação (POST)         -> roda o e-Validador (gera RDI/críticas)
//   críticas (GET)           -> lista as ocorrências do RDI
//   empacotamento (POST)     -> monta o ZIP final pronto para transmissão
//   arquivo (GET, binário)   -> baixa um .TXT/ZIP do pacote
//   protocolo (POST, gated)  -> REGISTRA o protocolo da transmissão feita FORA do
//                               sistema (PAD/e-Protocolo). Não transmite.
//   reconciliação (POST)     -> confronta com o SICONFI (pode retornar 503).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { getAccessToken } from '../../api/authToken';
import { ApiError } from '../../api/problemDetails';
import { transparenciaKeys } from './keys';
import type { CriacaoResponse } from './keys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Tipo de período (competência) da remessa ao TCE-RS. */
export type TipoPeriodo = 'Mensal' | 'Bimestre' | 'Quadrimestre' | 'Anual';

/** Situação no ciclo Gerada → Validada → ProntaParaTransmissao → Enviada. */
export type SituacaoRemessa = 'Gerada' | 'Validada' | 'ProntaParaTransmissao' | 'Enviada';

/** Severidade de uma ocorrência do RDI (Relatório de Diagnóstico de Inconsistências). */
export type SeveridadeCritica = 'Erro' | 'Alerta' | 'Informacao';

export interface RemessaResumo {
  id: string;
  exercicio: number;
  periodo: string;
  situacao: SituacaoRemessa;
  nomeArquivoZip: string | null;
  protocolo: string | null;
  dataLimite: string;
  dataEnvio: string | null;
}

export interface RemessaArquivo {
  /** Nome do arquivo (chave para o GET ?arquivo=). */
  nome: string;
  /** Tamanho em bytes, quando informado pelo backend. */
  tamanhoBytes: number | null;
}

export interface RemessaDetalhe {
  id: string;
  exercicio: number;
  periodo: string;
  leiaute: string;
  situacao: SituacaoRemessa;
  hashIntegridade: string | null;
  nomeArquivoZip: string | null;
  protocolo: string | null;
  dataLimite: string;
  dataGeracao: string;
  dataEnvio: string | null;
  /** Resumo do RDI: total de erros/alertas apurados na validação. */
  totalErros: number;
  totalAlertas: number;
  /** Arquivos disponíveis para download após o empacotamento. */
  arquivos: RemessaArquivo[];
}

/** Ocorrência do RDI emitido pelo e-Validador do TCE-RS. */
export interface RemessaCritica {
  codigo: string;
  severidade: SeveridadeCritica;
  registro: string | null;
  mensagem: string;
}

export interface GerarRemessaInput {
  exercicio: number;
  tipoPeriodo: TipoPeriodo;
  numeroPeriodo: number;
}

export interface RegistrarProtocoloInput {
  id: string;
  protocolo: string;
}

export interface ListarRemessasParams {
  exercicio: number;
  tipo?: TipoPeriodo | null;
  situacao?: SituacaoRemessa | null;
}

/** Resultado da reconciliação com o SICONFI. `indisponivel` => o serviço retornou 503. */
export interface ReconciliacaoResultado {
  indisponivel: boolean;
  conforme?: boolean;
  divergencias?: number;
  mensagem?: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

const BASE = '/transparencia/remessas-tce';

function listar(params: ListarRemessasParams, signal?: AbortSignal): Promise<RemessaResumo[]> {
  return http.get<RemessaResumo[]>(BASE, {
    query: { exercicio: params.exercicio, tipo: params.tipo, situacao: params.situacao },
    signal,
  });
}

function obter(id: string, signal?: AbortSignal): Promise<RemessaDetalhe> {
  return http.get<RemessaDetalhe>(`${BASE}/${id}`, { signal });
}

function obterCriticas(id: string, signal?: AbortSignal): Promise<RemessaCritica[]> {
  return http.get<RemessaCritica[]>(`${BASE}/${id}/criticas`, { signal });
}

function gerar(input: GerarRemessaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(BASE, input);
}

function validar(id: string): Promise<void> {
  return http.post<void>(`${BASE}/${id}/validacao`);
}

function empacotar(id: string): Promise<void> {
  return http.post<void>(`${BASE}/${id}/empacotamento`);
}

function registrarProtocolo({ id, protocolo }: RegistrarProtocoloInput): Promise<void> {
  return http.post<void>(`${BASE}/${id}/protocolo`, { protocolo });
}

async function reconciliar(id: string): Promise<ReconciliacaoResultado> {
  try {
    const r = await http.post<Omit<ReconciliacaoResultado, 'indisponivel'>>(
      `${BASE}/${id}/reconciliacao`,
    );
    return { indisponivel: false, ...r };
  } catch (error) {
    // 503: SICONFI indisponível — tratamos como resultado "indisponível", não erro fatal.
    if (error instanceof ApiError && error.status === 503) {
      return { indisponivel: true, mensagem: error.userMessage };
    }
    throw error;
  }
}

/**
 * Baixa um arquivo binário (.TXT/ZIP) do pacote da remessa e dispara o download no
 * navegador. Usa fetch direto (o http client tipado é JSON-only) com o Bearer atual.
 */
export async function baixarArquivoRemessa(id: string, arquivo: string): Promise<void> {
  const token = getAccessToken();
  const url = `/api${BASE}/${id}/arquivo?arquivo=${encodeURIComponent(arquivo)}`;
  const resp = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!resp.ok) {
    throw new ApiError(`Não foi possível baixar o arquivo (HTTP ${resp.status}).`, resp.status);
  }
  const blob = await resp.blob();
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = arquivo;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(objectUrl);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as remessas de prestação de contas de um exercício, com filtros opcionais. */
export function useRemessas(params: ListarRemessasParams, enabled = true) {
  return useQuery({
    queryKey: transparenciaKeys.remessasLista(params),
    queryFn: ({ signal }) => listar(params, signal),
    enabled: enabled && Number.isFinite(params.exercicio) && params.exercicio > 0,
  });
}

/** Detalhe de uma remessa. */
export function useRemessa(id: string) {
  return useQuery({
    queryKey: transparenciaKeys.remessa(id),
    queryFn: ({ signal }) => obter(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Críticas (RDI) de uma remessa — habilitado apenas após a validação. */
export function useRemessaCriticas(id: string, enabled: boolean) {
  return useQuery({
    queryKey: transparenciaKeys.remessaCriticas(id),
    queryFn: ({ signal }) => obterCriticas(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

/** Gera (consolida) uma nova remessa e invalida as listas. */
export function useGerarRemessa() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.remessas() });
    },
  });
}

function useAcaoRemessa<I>(mutationFn: (input: I) => Promise<unknown>, id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.remessas() });
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.remessa(id) });
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.remessaCriticas(id) });
    },
  });
}

/** Dispara a validação (e-Validador / RDI). */
export function useValidarRemessa(id: string) {
  return useAcaoRemessa(() => validar(id), id);
}

/** Monta o pacote ZIP final (empacotamento). */
export function useEmpacotarRemessa(id: string) {
  return useAcaoRemessa(() => empacotar(id), id);
}

/** REGISTRA o protocolo da transmissão feita fora do sistema (gated). */
export function useRegistrarProtocolo(id: string) {
  return useAcaoRemessa((protocolo: string) => registrarProtocolo({ id, protocolo }), id);
}

/** Reconcilia com o SICONFI; trata 503 (indisponível) como resultado, não erro. */
export function useReconciliarRemessa(id: string) {
  return useMutation({ mutationFn: () => reconciliar(id) });
}
