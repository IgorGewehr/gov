// Camada de API do agregado Norma (modulo Legislativo, parte 2). DTOs + acesso
// HTTP tipado + hooks TanStack Query, cobrindo /api/legislativo/normas: busca
// paginada (termo/tipo/ano), detalhe, cadastro e acoes de revogacao/alteracao.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { getAccessToken } from '../../api/authToken';
import { ApiError } from '../../api/problemDetails';
import {
  legislativoKeys,
  type CriacaoResponse,
  type PaginaResultado,
} from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Resumo de uma norma para a lista de busca. */
export interface NormaResumo {
  id: string;
  tipo: string;
  /** Numero da norma (back: int). */
  numero: number;
  ano: number;
  ementa: string;
  situacao: string;
  /** Data de promulgacao (back: DataPromulgacao, "yyyy-MM-dd"). */
  dataPromulgacao: string;
}

/** Evento da trilha de vigencia de uma norma (back: EventoVigenciaDto). */
export interface EventoVigencia {
  /** Tipo do evento (ex.: "Revogacao", "Alteracao"). */
  tipo: string;
  /** Data do evento ("yyyy-MM-dd"). */
  data: string;
  /** Norma referenciada pelo evento (revogadora/alteradora), se houver. */
  normaReferenciaId: string | null;
  /** Observacao livre, se houver. */
  observacao: string | null;
}

/** Detalhe completo de uma norma. */
export interface NormaDetalhe extends NormaResumo {
  /** Texto articulado da norma (back: TextoArticulado, opcional). */
  textoArticulado: string | null;
  /** Proposicao de origem (back: ProposicaoOrigemId, opcional). */
  proposicaoOrigemId: string | null;
  /** Data de revogacao (back: DataRevogacao, se revogada). */
  dataRevogacao: string | null;
  /** Trilha de vigencia (back: Historico) — revogacoes/alteracoes derivadas por `tipo`. */
  historico: EventoVigencia[];
}

/** Filtros da busca paginada de normas. */
export interface BuscaNormasFiltro {
  termo: string;
  tipo: string;
  ano: string;
  pagina: number;
}

/** Payload de cadastro de norma (CadastrarNormaCommand). */
export interface NormaInput {
  tipo: number;
  /** Numero da norma (back: int). */
  numero: number;
  ano: number;
  ementa: string;
  /** Texto articulado (back: TextoArticulado, opcional). */
  textoArticulado: string;
  /** Data de promulgacao (back: DataPromulgacao, "yyyy-MM-dd", obrigatoria). */
  dataPromulgacao: string;
}

/** Payload de revogacao (RevogarNormaPayload): {normaId} foi revogada por NormaRevogadoraId. */
export interface RevogarNormaInput {
  /** Data da revogacao (back: DataRevogacao, obrigatoria, "yyyy-MM-dd"). */
  dataRevogacao: string;
  /** Norma que revoga esta (back: NormaRevogadoraId, opcional). */
  normaRevogadoraId?: string;
}

/** Payload de alteracao (RegistrarAlteracaoNormaPayload). */
export interface AlterarNormaInput {
  /** Data de referencia da alteracao (back: DataReferencia, obrigatoria, "yyyy-MM-dd"). */
  dataReferencia: string;
  /** Norma que altera esta (back: NormaAlteradoraId, obrigatoria). */
  normaAlteradoraId: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarNormas(
  filtro: BuscaNormasFiltro,
  signal?: AbortSignal,
): Promise<PaginaResultado<NormaResumo>> {
  return http.get<PaginaResultado<NormaResumo>>('/legislativo/normas', {
    query: {
      termo: filtro.termo || null,
      tipo: filtro.tipo || null,
      ano: filtro.ano || null,
      pagina: filtro.pagina,
    },
    signal,
  });
}

function obterNorma(id: string, signal?: AbortSignal): Promise<NormaDetalhe> {
  return http.get<NormaDetalhe>(`/legislativo/normas/${id}`, { signal });
}

async function criarNorma(input: NormaInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/normas', input);
  return id;
}

function revogarNorma(id: string, input: RevogarNormaInput): Promise<void> {
  return http.post<void>(`/legislativo/normas/${id}/revogacao`, input);
}

function alterarNorma(id: string, input: AlterarNormaInput): Promise<void> {
  return http.post<void>(`/legislativo/normas/${id}/alteracao`, input);
}

/** Extrai o filename de um header Content-Disposition (RFC 6266), se presente. */
function nomeArquivoDoHeader(disposition: string | null): string | null {
  if (!disposition) return null;
  const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (utf8?.[1]) return decodeURIComponent(utf8[1]);
  const simples = /filename="?([^";]+)"?/i.exec(disposition);
  return simples?.[1] ?? null;
}

/**
 * Baixa o XML LexML-BR (URN + articulado) da norma e dispara o download no
 * navegador (GET /legislativo/normas/{id}/lexml -> application/xml). Usa fetch
 * direto porque o http client tipado e JSON-only. Export LOCAL (W9.5); a
 * transmissao oficial a base LexML/dados abertos e M10.
 */
export async function baixarLexml(id: string, nomeSugerido: string): Promise<void> {
  const token = getAccessToken();
  const resp = await fetch(`/api/legislativo/normas/${id}/lexml`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!resp.ok) {
    throw new ApiError(`Não foi possível baixar o LexML (HTTP ${resp.status}).`, resp.status);
  }
  const blob = await resp.blob();
  const nome = nomeArquivoDoHeader(resp.headers.get('content-disposition')) ?? nomeSugerido;
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = nome;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(objectUrl);
}

/** Nome de arquivo LexML sugerido (espelha o backend: tipo-numero-ano.xml). */
export function nomeArquivoLexml(norma: { tipo: string; numero: number; ano: number }): string {
  const slug = norma.tipo
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return `${slug || 'norma'}-${norma.numero}-${norma.ano}.xml`;
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Busca paginada de normas por termo/tipo/ano. */
export function useBuscaNormas(filtro: BuscaNormasFiltro) {
  return useQuery({
    queryKey: legislativoKeys.normasBusca(filtro.termo, filtro.tipo, filtro.ano, filtro.pagina),
    queryFn: ({ signal }) => buscarNormas(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Detalhe de uma norma (texto integral + vinculos de revogacao/alteracao). */
export function useNorma(id: string) {
  return useQuery({
    queryKey: legislativoKeys.norma(id),
    queryFn: ({ signal }) => obterNorma(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Cadastra uma nova norma e invalida as buscas. */
export function useCriarNorma() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarNorma,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.normas() });
    },
  });
}

/** Invalida o detalhe de uma norma e todas as buscas. */
function useInvalidarNorma(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.norma(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.normas() });
  };
}

/** Revoga esta norma (registra a revogacao por outra norma). */
export function useRevogarNorma(id: string) {
  const invalidar = useInvalidarNorma(id);
  return useMutation({
    mutationFn: (input: RevogarNormaInput) => revogarNorma(id, input),
    onSuccess: invalidar,
  });
}

/** Registra que esta norma foi alterada por outra norma vigente. */
export function useAlterarNorma(id: string) {
  const invalidar = useInvalidarNorma(id);
  return useMutation({
    mutationFn: (input: AlterarNormaInput) => alterarNorma(id, input),
    onSuccess: invalidar,
  });
}
