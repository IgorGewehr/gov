// Camada de API do agregado BemPatrimonial (módulo Patrimonio).
// Espelha o contrato REAL de PatrimonioEndpoints.cs (rotas /api/patrimonio/bens/...)
// e os Commands/Queries da camada Application.
// Convenções (padrão-ouro Tributos):
//  - DTOs no topo;
//  - query keys centralizadas para invalidação consistente;
//  - funções de acesso usando o http client tipado (Authorization + ProblemDetails);
//  - hooks TanStack Query (useQuery/useMutation) para TODAS as operações.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import type { ResultadoPaginado } from '../shared/paginacaoTipos';

// ---------------------------------------------------------------------------
// DTOs (espelham as projeções/Commands do backend)
// ---------------------------------------------------------------------------

/** Situação do bem no ciclo de vida (enum SituacaoBemPatrimonial, serializado por nome). */
export type SituacaoBemPatrimonial = 'EmIncorporacao' | 'Tombado' | 'Cedido' | 'Baixada' | 'Alienada';

/** Tipo do bem (enum TipoBem). 1 = Móvel, 2 = Imóvel. */
export type TipoBemNumero = 1 | 2;

/** Detalhe do bem (ObterBemPatrimonialQuery -> BemPatrimonialDetalhe). */
export interface BemPatrimonialDetalhe {
  id: string;
  numeroTombamento: string | null;
  descricao: string;
  /** Tipo serializado por nome ("Movel" | "Imovel"). */
  tipo: string;
  valorInicial: number;
  valorResidual: number;
  valorContabil: number;
  vidaUtilMeses: number;
  dataIncorporacao: string;
  situacao: SituacaoBemPatrimonial;
}

/**
 * Item da LISTA NAVEGÁVEL de bens (Onda 0 — Navegabilidade).
 * Projeção de GET /patrimonio/bens?termo&tipo&situacao&pagina&tamanho -> BemPatrimonialItemLista.
 * Busca casa por trecho em descrição e número de tombamento (case-insensitive).
 */
export interface BemPatrimonialItemLista {
  id: string;
  numeroTombamento: string | null;
  descricao: string;
  /** Tipo serializado por nome ("Movel" | "Imovel"). */
  tipo: string;
  valorContabil: number;
  dataIncorporacao: string;
  situacao: SituacaoBemPatrimonial;
}

/** Filtros da lista navegável de bens. */
export interface BemPatrimonialFiltro {
  termo?: string;
  tipo?: TipoBemNumero;
  situacao?: SituacaoBemPatrimonial;
  pagina: number;
  tamanho: number;
}

/** Resumo de bem depreciável (ListarBensDepreciaveisQuery -> BemDepreciavelResumo). */
export interface BemDepreciavelResumo {
  id: string;
  numeroTombamento: string;
  valorContabil: number;
  valorResidual: number;
  parcelaMensal: number;
}

/** Resumo de movimentação patrimonial (ListarMovimentacoesDoBemQuery -> MovimentacaoResumo). */
export interface MovimentacaoResumo {
  id: string;
  localizacaoOrigem: string;
  localizacaoDestino: string;
  responsavelId: string;
  data: string;
}

// --- Entradas dos Commands ---

/** IncorporarBemCommand. */
export interface IncorporarBemInput {
  descricao: string;
  tipo: TipoBemNumero;
  valorInicial: number;
  valorResidual: number;
  vidaUtilMeses: number;
  dataIncorporacao: string;
  origem: string;
}

/** TombarBemCommand (payload do endpoint). */
export interface TombarBemInput {
  numeroTombamento: string;
}

/** DepreciarBemCommand (payload do endpoint). */
export interface DepreciarBemInput {
  anoCompetencia: number;
  mesCompetencia: number;
}

/** ReavaliarBemCommand (payload do endpoint). */
export interface ReavaliarBemInput {
  novoValorJusto: number;
  laudoUri: string;
  dataReavaliacao: string;
}

/** RegistrarImpairmentCommand (payload do endpoint). */
export interface RegistrarImpairmentInput {
  valorRecuperavel: number;
  laudoUri: string;
  dataTeste: string;
}

/** TransferirBemCommand (payload do endpoint). */
export interface TransferirBemInput {
  localizacaoDestino: string;
  responsavelDestinoId: string;
}

/** CederBemCommand (payload do endpoint). */
export interface CederBemInput {
  terceiroId: string;
  gratuito: boolean;
  dataInicio: string;
  dataFim: string | null;
}

/** BaixarBemCommand (payload do endpoint). */
export interface BaixarBemInput {
  motivoBaixa: number;
  laudoUri: string;
  autorizacaoId: string;
}

/** AlienarBemCommand (payload do endpoint). */
export interface AlienarBemInput {
  avaliacaoPreviaId: string;
  porLeilao: boolean;
  valorAlienacao: number;
}

/** Resposta do POST /bens (id criado). */
export interface IdCriado {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const bemPatrimonialKeys = {
  all: ['patrimonio', 'bens'] as const,
  detalhe: (id: string) => [...bemPatrimonialKeys.all, 'detalhe', id] as const,
  movimentacoes: (id: string) => [...bemPatrimonialKeys.all, 'movimentacoes', id] as const,
  depreciaveis: (ano: number, mes: number) =>
    [...bemPatrimonialKeys.all, 'depreciaveis', ano, mes] as const,
  lista: (filtro: BemPatrimonialFiltro) =>
    [
      ...bemPatrimonialKeys.all,
      'lista',
      filtro.termo ?? '',
      filtro.tipo ?? '',
      filtro.situacao ?? '',
      filtro.pagina,
      filtro.tamanho,
    ] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas REAIS de PatrimonioEndpoints.cs)
// ---------------------------------------------------------------------------

// --- Queries ---

function obterBem(id: string, signal?: AbortSignal): Promise<BemPatrimonialDetalhe> {
  return http.get<BemPatrimonialDetalhe>(`/patrimonio/bens/${id}`, { signal });
}

function listarBens(
  filtro: BemPatrimonialFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<BemPatrimonialItemLista>> {
  return http.get<ResultadoPaginado<BemPatrimonialItemLista>>('/patrimonio/bens', {
    query: {
      termo: filtro.termo,
      tipo: filtro.tipo,
      situacao: filtro.situacao,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function listarDepreciaveis(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<BemDepreciavelResumo[]> {
  return http.get<BemDepreciavelResumo[]>('/patrimonio/bens/depreciaveis', {
    query: { ano, mes },
    signal,
  });
}

function listarMovimentacoes(id: string, signal?: AbortSignal): Promise<MovimentacaoResumo[]> {
  return http.get<MovimentacaoResumo[]>(`/patrimonio/bens/${id}/movimentacoes`, { signal });
}

// --- Commands ---

function incorporarBem(input: IncorporarBemInput): Promise<IdCriado> {
  return http.post<IdCriado>('/patrimonio/bens', input);
}

function tombarBem(id: string, input: TombarBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/tombamento`, input);
}

function depreciarBem(id: string, input: DepreciarBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/depreciacao`, input);
}

function reavaliarBem(id: string, input: ReavaliarBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/reavaliacao`, input);
}

function registrarImpairment(id: string, input: RegistrarImpairmentInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/impairment`, input);
}

function transferirBem(id: string, input: TransferirBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/transferencia`, input);
}

function cederBem(id: string, input: CederBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/cessao`, input);
}

function baixarBem(id: string, input: BaixarBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/baixa`, input);
}

function alienarBem(id: string, input: AlienarBemInput): Promise<void> {
  return http.post<void>(`/patrimonio/bens/${id}/alienacao`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/**
 * LISTA NAVEGÁVEL de bens (Onda 0) — busca por descrição/tombamento + filtros,
 * paginada. Mantém os dados anteriores enquanto pagina/filtra (placeholderData).
 */
export function useBensLista(filtro: BemPatrimonialFiltro) {
  return useQuery({
    queryKey: bemPatrimonialKeys.lista(filtro),
    queryFn: ({ signal }) => listarBens(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Detalhe de um bem patrimonial (ObterBemPatrimonial). */
export function useBemPatrimonial(id: string) {
  return useQuery({
    queryKey: bemPatrimonialKeys.detalhe(id),
    queryFn: ({ signal }) => obterBem(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Lista os bens depreciáveis do tenant na competência (ListarBensDepreciaveis). */
export function useBensDepreciaveis(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: bemPatrimonialKeys.depreciaveis(ano, mes),
    queryFn: ({ signal }) => listarDepreciaveis(ano, mes, signal),
    enabled: enabled && ano > 0 && mes >= 1 && mes <= 12,
  });
}

/** Lista as movimentações patrimoniais de um bem (ListarMovimentacoesDoBem). */
export function useMovimentacoesDoBem(id: string, enabled = true) {
  return useQuery({
    queryKey: bemPatrimonialKeys.movimentacoes(id),
    queryFn: ({ signal }) => listarMovimentacoes(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Incorpora um novo bem ao acervo (IncorporarBem). */
export function useIncorporarBem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: incorporarBem,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bemPatrimonialKeys.all });
    },
  });
}

/** Invalida o detalhe (e movimentações) de um bem após uma transição de estado. */
function useInvalidarBem(id: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: bemPatrimonialKeys.detalhe(id) });
    void queryClient.invalidateQueries({ queryKey: bemPatrimonialKeys.movimentacoes(id) });
    void queryClient.invalidateQueries({ queryKey: bemPatrimonialKeys.all });
  };
}

/** Atribui número de tombo ao bem (TombarBem). */
export function useTombarBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: TombarBemInput) => tombarBem(id, input),
    onSuccess: invalidar,
  });
}

/** Reconhece a depreciação mensal do bem (DepreciarBem). */
export function useDepreciarBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: DepreciarBemInput) => depreciarBem(id, input),
    onSuccess: invalidar,
  });
}

/** Reavalia o bem a valor justo (ReavaliarBem). */
export function useReavaliarBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: ReavaliarBemInput) => reavaliarBem(id, input),
    onSuccess: invalidar,
  });
}

/** Registra perda por impairment (RegistrarImpairment). */
export function useRegistrarImpairment(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: RegistrarImpairmentInput) => registrarImpairment(id, input),
    onSuccess: invalidar,
  });
}

/** Transfere o bem (localização/responsável) dentro do tenant (TransferirBem). */
export function useTransferirBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: TransferirBemInput) => transferirBem(id, input),
    onSuccess: invalidar,
  });
}

/** Cede o bem em cessão/comodato a terceiro (CederBem). */
export function useCederBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: CederBemInput) => cederBem(id, input),
    onSuccess: invalidar,
  });
}

/** Baixa o bem do acervo — exige laudo e autorização (BaixarBem). */
export function useBaixarBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: BaixarBemInput) => baixarBem(id, input),
    onSuccess: invalidar,
  });
}

/** Aliena o bem — exige avaliação prévia (AlienarBem). */
export function useAlienarBem(id: string) {
  const invalidar = useInvalidarBem(id);
  return useMutation({
    mutationFn: (input: AlienarBemInput) => alienarBem(id, input),
    onSuccess: invalidar,
  });
}
