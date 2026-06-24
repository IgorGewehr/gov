// Camada de API do modulo Convenios (DOIS fluxos sob /api/convenios).
// Replica o PADRAO-OURO: DTOs no topo (espelham as Queries reais da Application),
// acesso via http client tipado, query keys centralizadas e hooks TanStack Query.
//
// Contrato real (ConveniosEndpoints.cs / ConsultasConvenio.cs / ConsultasParceria.cs):
//  Fluxo A — Convenios federais RECEBIDOS:
//   GET /convenios/recebidos?situacao   -> ListarConvenios  (ConvenioListItem[])
//   GET /convenios/recebidos/{id}        -> ObterConvenio    (ConvenioDetalhe)
//  Fluxo B — Parcerias OSC (MROSC):
//   GET /convenios/parcerias?situacao    -> ListarParcerias  (ParceriaListItem[])
//   GET /convenios/parcerias/{id}        -> ObterParceria    (ParceriaDetalhe)
//
// NOTA: o http client ja prefixa BASE_URL = '/api'; os paths aqui NAO incluem '/api'
// (resolvem para /api/convenios/...), espelhando os demais modulos.
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';
import type { SituacaoConvenio, SituacaoParceria } from './convenios.helpers';

// ---------------------------------------------------------------------------
// DTOs — Fluxo A (ConvenioListItem / ConvenioDetalhe / PrestacaoConvenioDetalhe)
// ---------------------------------------------------------------------------

/** Item de lista de convenio recebido (ConvenioListItem). */
export interface ConvenioListItem {
  id: string;
  concedenteNome: string;
  /** Numero no Transferegov (nulo ate a celebracao). */
  numeroTransferegov: string | null;
  objeto: string;
  valorGlobal: number;
  /** Situacao projetada de ToString(). */
  situacao: SituacaoConvenio;
}

/** Detalhe de uma PC do convenio (PrestacaoConvenioDetalhe). */
export interface PrestacaoConvenioDetalhe {
  id: string;
  /** Tipo (Parcial/Final) projetado de ToString(). */
  tipo: string;
  /** Situacao da PC projetada de ToString(). */
  situacao: string;
  /** Data de submissao (ISO yyyy-mm-dd), nula enquanto pendente. */
  dataSubmissao: string | null;
  /** Prazo de analise (ISO yyyy-mm-dd), nulo enquanto pendente — alimenta o semaforo. */
  prazoAnalise: string | null;
}

/** Detalhe de um convenio recebido (ConvenioDetalhe). */
export interface ConvenioDetalhe {
  id: string;
  concedenteNome: string;
  concedenteCnpj: string;
  numeroTransferegov: string | null;
  objeto: string;
  valorRepasse: number;
  valorContrapartida: number;
  valorGlobal: number;
  situacao: SituacaoConvenio;
  vigenciaInicio: string | null;
  vigenciaFim: string | null;
  prestacoes: PrestacaoConvenioDetalhe[];
}

// ---------------------------------------------------------------------------
// DTOs — Fluxo B (ParceriaListItem / ParceriaDetalhe / RepasseOscDetalhe)
// ---------------------------------------------------------------------------

/** Item de lista de parceria OSC (ParceriaListItem). */
export interface ParceriaListItem {
  id: string;
  oscRazaoSocial: string;
  /** Tipo de instrumento (Colaboracao/Fomento/Cooperacao) projetado de ToString(). */
  tipoInstrumento: string;
  /** Situacao projetada de ToString(). */
  situacao: SituacaoParceria;
}

/** Detalhe de um repasse a OSC (RepasseOscDetalhe). */
export interface RepasseOscDetalhe {
  numeroOrdem: number;
  valor: number;
  /** Situacao do repasse projetada de ToString(). */
  situacao: string;
  /** Execucao orcamentaria completa (empenho+liquidacao+pagamento). */
  execucaoCompleta: boolean;
}

/** Detalhe de uma parceria OSC (ParceriaDetalhe). */
export interface ParceriaDetalhe {
  id: string;
  oscRazaoSocial: string;
  oscCnpj: string;
  tipoInstrumento: string;
  formaSelecao: string;
  valorGlobal: number;
  situacao: SituacaoParceria;
  vigenciaInicio: string | null;
  vigenciaFim: string | null;
  /** Situacao da PC da OSC (nula ate a abertura). */
  prestacaoSituacao: string | null;
  repasses: RepasseOscDetalhe[];
}

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const conveniosKeys = {
  recebidos: ['convenios', 'recebidos'] as const,
  recebidosLista: (situacao?: SituacaoConvenio) =>
    ['convenios', 'recebidos', 'lista', situacao ?? 'todas'] as const,
  recebido: (id: string) => ['convenios', 'recebidos', 'detalhe', id] as const,
  parcerias: ['convenios', 'parcerias'] as const,
  parceriasLista: (situacao?: SituacaoParceria) =>
    ['convenios', 'parcerias', 'lista', situacao ?? 'todas'] as const,
  parceria: (id: string) => ['convenios', 'parcerias', 'detalhe', id] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarConvenios(
  situacao: SituacaoConvenio | undefined,
  signal?: AbortSignal,
): Promise<ConvenioListItem[]> {
  const qs = situacao ? `?situacao=${encodeURIComponent(situacao)}` : '';
  return http.get<ConvenioListItem[]>(`/convenios/recebidos${qs}`, { signal });
}

function obterConvenio(id: string, signal?: AbortSignal): Promise<ConvenioDetalhe> {
  return http.get<ConvenioDetalhe>(`/convenios/recebidos/${id}`, { signal });
}

function listarParcerias(
  situacao: SituacaoParceria | undefined,
  signal?: AbortSignal,
): Promise<ParceriaListItem[]> {
  const qs = situacao ? `?situacao=${encodeURIComponent(situacao)}` : '';
  return http.get<ParceriaListItem[]>(`/convenios/parcerias${qs}`, { signal });
}

function obterParceria(id: string, signal?: AbortSignal): Promise<ParceriaDetalhe> {
  return http.get<ParceriaDetalhe>(`/convenios/parcerias/${id}`, { signal });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista convenios federais recebidos do tenant, opcionalmente filtrados por situacao. */
export function useConvenios(situacao?: SituacaoConvenio) {
  return useQuery({
    queryKey: conveniosKeys.recebidosLista(situacao),
    queryFn: ({ signal }) => listarConvenios(situacao, signal),
  });
}

/** Detalha um convenio federal recebido (ciclo completo). */
export function useConvenio(id: string) {
  return useQuery({
    queryKey: conveniosKeys.recebido(id),
    queryFn: ({ signal }) => obterConvenio(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Lista parcerias OSC (MROSC) do tenant, opcionalmente filtradas por situacao. */
export function useParcerias(situacao?: SituacaoParceria) {
  return useQuery({
    queryKey: conveniosKeys.parceriasLista(situacao),
    queryFn: ({ signal }) => listarParcerias(situacao, signal),
  });
}

/** Detalha uma parceria OSC (ciclo completo + repasses). */
export function useParceria(id: string) {
  return useQuery({
    queryKey: conveniosKeys.parceria(id),
    queryFn: ({ signal }) => obterParceria(id, signal),
    enabled: id.trim().length > 0,
  });
}
