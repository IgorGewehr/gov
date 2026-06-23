// Camada de API das REQUISIÇÕES de almoxarifado (módulo Patrimonio — Onda 3b).
// Segue o PADRÃO-OURO do módulo (itemestoque.api.ts): DTOs -> query keys ->
// funções http tipadas -> hooks TanStack Query. Rotas REAIS em
// PatrimonioEndpoints.cs (MapearRequisicoes):
//   POST /api/patrimonio/requisicoes                            (AbrirPedidoCommand)        -> { id }
//   GET  /api/patrimonio/requisicoes?status&setor&unidadeId&pagina&tamanho (BuscarPedidos)  -> ResultadoPaginado<PedidoItemLista>
//   GET  /api/patrimonio/requisicoes/{pedidoId}                 (ObterPedido)               -> PedidoRequisicaoDetalhe
//   POST /api/patrimonio/requisicoes/{pedidoId}/aprovacao       (AprovarPedidoPayload)      -> 204
//   POST /api/patrimonio/requisicoes/{pedidoId}/atendimento     (AtenderPedidoPayload)      -> 204
//   POST /api/patrimonio/requisicoes/{pedidoId}/cancelamento    (CancelarPedidoPayload)     -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import type { ResultadoPaginado } from '../shared/paginacaoTipos';

// ---------------------------------------------------------------------------
// Enum de domínio (SituacaoPedido — numérico no contrato; nome PT-BR na UI)
// ---------------------------------------------------------------------------

/** SituacaoPedido: Solicitado=1, Aprovado=2, Atendido=3, Cancelado=4 (RequisicaoEnums.cs). */
export const SITUACAO_PEDIDO = {
  Solicitado: 1,
  Aprovado: 2,
  Atendido: 3,
  Cancelado: 4,
} as const;
export type SituacaoPedidoValor = (typeof SITUACAO_PEDIDO)[keyof typeof SITUACAO_PEDIDO];

/** Nome da situação (string ToString() do enum, como o backend projeta). */
export type SituacaoPedidoNome = 'Solicitado' | 'Aprovado' | 'Atendido' | 'Cancelado';

// ---------------------------------------------------------------------------
// DTOs de leitura (projeções dos handlers de Query)
// ---------------------------------------------------------------------------

/** PedidoItemLista — projeção enxuta de BuscarPedidosQuery (fila/tabela). */
export interface PedidoItemLista {
  id: string;
  unidadeId: string;
  setorSolicitante: string;
  data: string;
  situacao: SituacaoPedidoNome;
  totalItens: number;
}

/** ItemPedidoDto — linha do pedido (ObterPedidoQuery): solicitado × atendido. */
export interface ItemPedidoDto {
  id: string;
  itemEstoqueId: string;
  quantidadeSolicitada: number;
  quantidadeAtendida: number;
  totalmenteAtendido: boolean;
}

/** PedidoRequisicaoDetalhe — ficha + linhas (ObterPedidoQuery). */
export interface PedidoRequisicaoDetalhe {
  id: string;
  unidadeId: string;
  setorSolicitante: string;
  solicitanteId: string;
  data: string;
  justificativa?: string | null;
  situacao: SituacaoPedidoNome;
  aprovadorId?: string | null;
  dataAprovacao?: string | null;
  dataAtendimento?: string | null;
  motivoCancelamento?: string | null;
  itens: ItemPedidoDto[];
}

/** Filtros da fila de requisições. */
export interface PedidoFiltro {
  status?: SituacaoPedidoValor;
  setor?: string;
  unidadeId?: string;
  pagina: number;
  tamanho: number;
}

// ---------------------------------------------------------------------------
// DTOs de escrita (Commands / Payloads)
// ---------------------------------------------------------------------------

/** Linha de entrada na abertura (ItemPedidoEntrada). */
export interface ItemPedidoEntrada {
  itemEstoqueId: string;
  quantidade: number;
}

/** AbrirPedidoCommand (corpo do POST /requisicoes). */
export interface AbrirPedidoInput {
  unidadeId: string;
  setorSolicitante: string;
  solicitanteId: string;
  data: string;
  justificativa?: string | null;
  itens: ItemPedidoEntrada[];
}

/** AprovarPedidoPayload. */
export interface AprovarPedidoInput {
  aprovadorId: string;
  data: string;
}

/** AtenderPedidoPayload. */
export interface AtenderPedidoInput {
  data: string;
}

/** CancelarPedidoPayload. */
export interface CancelarPedidoInput {
  motivo: string;
}

interface CriadoResponse {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação consistente)
// ---------------------------------------------------------------------------

export const requisicaoKeys = {
  all: ['patrimonio', 'requisicao'] as const,
  detalhe: (id: string) => [...requisicaoKeys.all, 'detalhe', id] as const,
  lista: (filtro: PedidoFiltro) =>
    [
      ...requisicaoKeys.all,
      'lista',
      filtro.status ?? '',
      filtro.setor ?? '',
      filtro.unidadeId ?? '',
      filtro.pagina,
      filtro.tamanho,
    ] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarPedidos(
  filtro: PedidoFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<PedidoItemLista>> {
  return http.get<ResultadoPaginado<PedidoItemLista>>('/patrimonio/requisicoes', {
    query: {
      status: filtro.status,
      setor: filtro.setor,
      unidadeId: filtro.unidadeId,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterPedido(pedidoId: string, signal?: AbortSignal): Promise<PedidoRequisicaoDetalhe> {
  return http.get<PedidoRequisicaoDetalhe>(`/patrimonio/requisicoes/${pedidoId}`, { signal });
}

function abrirPedido(input: AbrirPedidoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/patrimonio/requisicoes', input);
}

function aprovarPedido(pedidoId: string, input: AprovarPedidoInput): Promise<void> {
  return http.post<void>(`/patrimonio/requisicoes/${pedidoId}/aprovacao`, input);
}

function atenderPedido(pedidoId: string, input: AtenderPedidoInput): Promise<void> {
  return http.post<void>(`/patrimonio/requisicoes/${pedidoId}/atendimento`, input);
}

function cancelarPedido(pedidoId: string, input: CancelarPedidoInput): Promise<void> {
  return http.post<void>(`/patrimonio/requisicoes/${pedidoId}/cancelamento`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — Consultas
// ---------------------------------------------------------------------------

/** Fila paginada de requisições por situação/setor/UO. Mantém dados ao paginar/filtrar. */
export function usePedidosLista(filtro: PedidoFiltro) {
  return useQuery({
    queryKey: requisicaoKeys.lista(filtro),
    queryFn: ({ signal }) => buscarPedidos(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Detalhe (ficha + linhas) de um pedido por Id. */
export function usePedido(id: string) {
  return useQuery({
    queryKey: requisicaoKeys.detalhe(id),
    queryFn: ({ signal }) => obterPedido(id, signal),
    enabled: id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — Comandos
// ---------------------------------------------------------------------------

/** AbrirPedido — abre requisição multi-item (Solicitado); invalida a fila. */
export function useAbrirPedido() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirPedido,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...requisicaoKeys.all, 'lista'] });
    },
  });
}

/** AprovarPedido (Solicitado -> Aprovado); invalida detalhe e fila. */
export function useAprovarPedido(pedidoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AprovarPedidoInput) => aprovarPedido(pedidoId, input),
    onSuccess: () => invalidarPedido(queryClient, pedidoId),
  });
}

/** AtenderPedido (Aprovado -> Atendido, baixa parcial por item); invalida detalhe e fila. */
export function useAtenderPedido(pedidoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AtenderPedidoInput) => atenderPedido(pedidoId, input),
    onSuccess: () => invalidarPedido(queryClient, pedidoId),
  });
}

/** CancelarPedido (Solicitado/Aprovado -> Cancelado); invalida detalhe e fila. */
export function useCancelarPedido(pedidoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CancelarPedidoInput) => cancelarPedido(pedidoId, input),
    onSuccess: () => invalidarPedido(queryClient, pedidoId),
  });
}

function invalidarPedido(queryClient: ReturnType<typeof useQueryClient>, pedidoId: string): void {
  queryClient.invalidateQueries({ queryKey: requisicaoKeys.detalhe(pedidoId) });
  queryClient.invalidateQueries({ queryKey: [...requisicaoKeys.all, 'lista'] });
}
