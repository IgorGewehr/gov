// Camada de API do agregado ItemEstoque (módulo Patrimonio — Almoxarifado).
// Segue o PADRÃO-OURO de src/modules/tributos: DTOs -> query keys -> funções http
// tipadas -> hooks TanStack Query. Rotas reais em PatrimonioEndpoints.cs:
//   POST   /api/patrimonio/estoque/itens                          (CadastrarItemEstoque)
//   GET    /api/patrimonio/estoque/itens/{itemId}                 (ObterItemEstoque)
//   POST   /api/patrimonio/estoque/itens/{itemId}/entradas        (RegistrarEntrada)
//   POST   /api/patrimonio/estoque/itens/{itemId}/requisicoes     (AtenderRequisicao)
//   POST   /api/patrimonio/estoque/itens/{itemId}/vrl             (AjustarValorRealizavelLiquido)
//   POST   /api/patrimonio/estoque/itens/{itemId}/classificacao-abc (ReclassificarAbc)
//   POST   /api/patrimonio/estoque/itens/{itemId}/inativacao      (InativarItem)
//   GET    /api/patrimonio/estoque/itens/{itemId}/movimentos?de&ate (ListarMovimentosDoItem)
//   GET    /api/patrimonio/estoque/reposicao                      (ListarItensAbaixoDoPontoPedido)
//   GET    /api/patrimonio/estoque/curva-abc                      (ObterPosicaoCurvaAbc)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// Enums de domínio (numéricos no contrato; rótulos PT-BR na UI)
// ---------------------------------------------------------------------------

/** MetodoCusteio: Peps=1, Medio=2 (ItemEstoque.rules.md §2). */
export const METODO_CUSTEIO = { Peps: 1, Medio: 2 } as const;
export type MetodoCusteioValor = (typeof METODO_CUSTEIO)[keyof typeof METODO_CUSTEIO];

/** CurvaABC: A=1, B=2, C=3 (ItemEstoque.rules.md §2). */
export const CLASSIFICACAO_ABC = { A: 1, B: 2, C: 3 } as const;
export type ClassificacaoAbcValor = (typeof CLASSIFICACAO_ABC)[keyof typeof CLASSIFICACAO_ABC];

export type MetodoCusteioNome = 'Peps' | 'Medio';
export type ClassificacaoAbcNome = 'A' | 'B' | 'C';
export type SituacaoItemEstoque = 'Ativo' | 'Inativo';
export type TipoMovimento = 'Entrada' | 'Saida';

// ---------------------------------------------------------------------------
// DTOs de leitura (projeções dos handlers de Query)
// ---------------------------------------------------------------------------

/** ItemEstoqueDetalhe — projeção de ObterItemEstoqueQuery (§6.1). */
export interface ItemEstoqueDetalhe {
  id: string;
  codigo: string;
  descricao: string;
  unidadeMedida: string;
  saldo: number;
  pontoPedido: number;
  metodoCusteio: MetodoCusteioNome;
  custoMedio: number;
  classificacaoAbc: ClassificacaoAbcNome;
  situacao: SituacaoItemEstoque;
}

/** ItemReposicaoResumo — projeção de ListarItensAbaixoDoPontoPedidoQuery (§6.2). */
export interface ItemReposicaoResumo {
  id: string;
  codigo: string;
  descricao: string;
  saldo: number;
  pontoPedido: number;
}

/** MovimentoResumo — projeção de ListarMovimentosDoItemQuery (§6.3). */
export interface MovimentoResumo {
  id: string;
  tipo: TipoMovimento;
  quantidade: number;
  valorUnitario: number;
  data: string;
  documento: string;
}

/** PosicaoAbcResumo — projeção de ObterPosicaoCurvaAbcQuery (§6.4). */
export interface PosicaoAbcResumo {
  classe: ClassificacaoAbcNome;
  quantidadeItens: number;
  valorTotal: number;
}

// ---------------------------------------------------------------------------
// DTOs de escrita (Commands / Payloads)
// ---------------------------------------------------------------------------

/** CadastrarItemEstoqueCommand (§5.1). */
export interface CadastrarItemEstoqueInput {
  codigo: string;
  descricao: string;
  unidadeMedida: string;
  metodoCusteio: number;
  pontoPedido: number;
  classificacaoAbc: number;
}

/** RegistrarEntradaPayload (§5.2). */
export interface RegistrarEntradaInput {
  quantidade: number;
  custoUnitario: number;
  dataEntrada: string;
  validade?: string | null;
  documento: string;
}

/** AtenderRequisicaoPayload (§5.3). */
export interface AtenderRequisicaoInput {
  requisicaoId: string;
  solicitanteId: string;
  quantidade: number;
  data: string;
}

/** AjustarVrlPayload (§5.4). */
export interface AjustarVrlInput {
  valorRealizavelLiquido: number;
  data: string;
}

/** ReclassificarAbcPayload (§5.5). */
export interface ReclassificarAbcInput {
  classificacaoAbc: number;
}

interface CriadoResponse {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação consistente)
// ---------------------------------------------------------------------------

export const itemEstoqueKeys = {
  all: ['patrimonio', 'itemestoque'] as const,
  item: (id: string) => [...itemEstoqueKeys.all, 'detalhe', id] as const,
  reposicao: () => [...itemEstoqueKeys.all, 'reposicao'] as const,
  curvaAbc: () => [...itemEstoqueKeys.all, 'curva-abc'] as const,
  movimentos: (id: string, de: string, ate: string) =>
    [...itemEstoqueKeys.all, 'movimentos', id, de, ate] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterItem(itemId: string, signal?: AbortSignal): Promise<ItemEstoqueDetalhe> {
  return http.get<ItemEstoqueDetalhe>(`/patrimonio/estoque/itens/${itemId}`, { signal });
}

function listarReposicao(signal?: AbortSignal): Promise<ItemReposicaoResumo[]> {
  return http.get<ItemReposicaoResumo[]>('/patrimonio/estoque/reposicao', { signal });
}

function obterCurvaAbc(signal?: AbortSignal): Promise<PosicaoAbcResumo[]> {
  return http.get<PosicaoAbcResumo[]>('/patrimonio/estoque/curva-abc', { signal });
}

function listarMovimentos(
  itemId: string,
  de: string,
  ate: string,
  signal?: AbortSignal,
): Promise<MovimentoResumo[]> {
  return http.get<MovimentoResumo[]>(`/patrimonio/estoque/itens/${itemId}/movimentos`, {
    query: { de, ate },
    signal,
  });
}

function cadastrarItem(input: CadastrarItemEstoqueInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/patrimonio/estoque/itens', input);
}

function registrarEntrada(itemId: string, input: RegistrarEntradaInput): Promise<void> {
  return http.post<void>(`/patrimonio/estoque/itens/${itemId}/entradas`, input);
}

function atenderRequisicao(itemId: string, input: AtenderRequisicaoInput): Promise<void> {
  return http.post<void>(`/patrimonio/estoque/itens/${itemId}/requisicoes`, input);
}

function ajustarVrl(itemId: string, input: AjustarVrlInput): Promise<void> {
  return http.post<void>(`/patrimonio/estoque/itens/${itemId}/vrl`, input);
}

function reclassificarAbc(itemId: string, input: ReclassificarAbcInput): Promise<void> {
  return http.post<void>(`/patrimonio/estoque/itens/${itemId}/classificacao-abc`, input);
}

function inativarItem(itemId: string): Promise<void> {
  return http.post<void>(`/patrimonio/estoque/itens/${itemId}/inativacao`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — Consultas (§6)
// ---------------------------------------------------------------------------

/** ObterItemEstoque — detalhe do item por Id (§6.1). */
export function useItemEstoque(id: string) {
  return useQuery({
    queryKey: itemEstoqueKeys.item(id),
    queryFn: ({ signal }) => obterItem(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** ListarItensAbaixoDoPontoPedido — itens em ponto de pedido/reposição (§6.2). */
export function useItensReposicao() {
  return useQuery({
    queryKey: itemEstoqueKeys.reposicao(),
    queryFn: ({ signal }) => listarReposicao(signal),
  });
}

/** ObterPosicaoCurvaAbc — agregação por classe A/B/C (§6.4). */
export function usePosicaoCurvaAbc() {
  return useQuery({
    queryKey: itemEstoqueKeys.curvaAbc(),
    queryFn: ({ signal }) => obterCurvaAbc(signal),
  });
}

/** ListarMovimentosDoItem — entradas/saídas do item no período (§6.3). */
export function useMovimentosDoItem(itemId: string, de: string, ate: string, enabled = true) {
  return useQuery({
    queryKey: itemEstoqueKeys.movimentos(itemId, de, ate),
    queryFn: ({ signal }) => listarMovimentos(itemId, de, ate, signal),
    enabled: enabled && itemId.trim().length > 0 && de.length > 0 && ate.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — Comandos (§5)
// ---------------------------------------------------------------------------

/** CadastrarItemEstoque (§5.1) — cria item e invalida listas agregadas. */
export function useCadastrarItemEstoque() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarItem,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.reposicao() });
      queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.curvaAbc() });
    },
  });
}

/** RegistrarEntrada (§5.2) — incrementa saldo/lote; invalida item, movimentos e reposição. */
export function useRegistrarEntrada(itemId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarEntradaInput) => registrarEntrada(itemId, input),
    onSuccess: () => invalidarItem(queryClient, itemId),
  });
}

/** AtenderRequisicao (§5.3) — baixa de saldo por requisição; invalida item, movimentos, reposição e ABC. */
export function useAtenderRequisicao(itemId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AtenderRequisicaoInput) => atenderRequisicao(itemId, input),
    onSuccess: () => invalidarItem(queryClient, itemId),
  });
}

/** AjustarValorRealizavelLiquido (§5.4) — menor entre custo e VRL; invalida item e ABC. */
export function useAjustarVrl(itemId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AjustarVrlInput) => ajustarVrl(itemId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.item(itemId) });
      queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.curvaAbc() });
    },
  });
}

/** ReclassificarAbc (§5.5) — altera classe A/B/C; invalida item e ABC. */
export function useReclassificarAbc(itemId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ReclassificarAbcInput) => reclassificarAbc(itemId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.item(itemId) });
      queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.curvaAbc() });
    },
  });
}

/** InativarItem (§5.6) — situação -> Inativo (exige saldo 0); invalida item, reposição e ABC. */
export function useInativarItem(itemId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => inativarItem(itemId),
    onSuccess: () => invalidarItem(queryClient, itemId),
  });
}

function invalidarItem(queryClient: ReturnType<typeof useQueryClient>, itemId: string): void {
  queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.item(itemId) });
  queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.reposicao() });
  queryClient.invalidateQueries({ queryKey: itemEstoqueKeys.curvaAbc() });
  // Movimentos: invalida toda a família de keys de movimentos do item.
  queryClient.invalidateQueries({ queryKey: [...itemEstoqueKeys.all, 'movimentos', itemId] });
}
