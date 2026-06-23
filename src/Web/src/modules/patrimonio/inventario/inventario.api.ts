// Camada de API do agregado Inventario (módulo Patrimonio — Lei 4.320 art. 96).
// Segue o PADRÃO-OURO de src/modules/tributos: DTOs -> query keys -> funções http
// tipadas -> hooks TanStack Query. Rotas REAIS em PatrimonioEndpoints.cs (/inventarios):
//   POST   /api/patrimonio/inventarios                                (AbrirInventario)
//   GET    /api/patrimonio/inventarios?exercicio&setor&situacao&pagina&tamanho (BuscarInventarios)
//   GET    /api/patrimonio/inventarios/{id}                           (ObterInventario)
//   GET    /api/patrimonio/inventarios/{id}/divergencias              (ListarDivergencias)
//   POST   /api/patrimonio/inventarios/{id}/snapshot                  (CarregarSnapshot)
//   POST   /api/patrimonio/inventarios/{id}/contagens                 (RegistrarContagem)
//   POST   /api/patrimonio/inventarios/{id}/sobras                    (RegistrarBemNaoCadastrado)
//   POST   /api/patrimonio/inventarios/{id}/conciliacao              (ConciliarInventario)
//   POST   /api/patrimonio/inventarios/{id}/encerramento             (EncerrarInventario)
//   POST   /api/patrimonio/inventarios/{id}/cancelamento             (CancelarInventario)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import type { ResultadoPaginado } from '../shared/paginacaoTipos';

// ---------------------------------------------------------------------------
// Enums de domínio (numéricos no contrato; nomes PascalCase nas projeções de leitura)
// ---------------------------------------------------------------------------

/** TipoInventario: Anual=1, PorSetor=2, Eventual=3, Transferencia=4 (Enums.cs). */
export const TIPO_INVENTARIO = { Anual: 1, PorSetor: 2, Eventual: 3, Transferencia: 4 } as const;
export type TipoInventarioValor = (typeof TIPO_INVENTARIO)[keyof typeof TIPO_INVENTARIO];
export type TipoInventarioNome = 'Anual' | 'PorSetor' | 'Eventual' | 'Transferencia';

/** SituacaoInventario: EmAbertura=1, EmContagem=2, EmConciliacao=3, Encerrado=4, Cancelado=5. */
export const SITUACAO_INVENTARIO = {
  EmAbertura: 1,
  EmContagem: 2,
  EmConciliacao: 3,
  Encerrado: 4,
  Cancelado: 5,
} as const;
export type SituacaoInventarioValor = (typeof SITUACAO_INVENTARIO)[keyof typeof SITUACAO_INVENTARIO];
export type SituacaoInventarioNome =
  | 'EmAbertura'
  | 'EmContagem'
  | 'EmConciliacao'
  | 'Encerrado'
  | 'Cancelado';

/** SituacaoEncontrada: Localizado=1, NaoLocalizado=2, LocalizadoOutroSetor=3, Inservivel=4. */
export const SITUACAO_ENCONTRADA = {
  Localizado: 1,
  NaoLocalizado: 2,
  LocalizadoOutroSetor: 3,
  Inservivel: 4,
} as const;
export type SituacaoEncontradaValor = (typeof SITUACAO_ENCONTRADA)[keyof typeof SITUACAO_ENCONTRADA];
export type SituacaoEncontradaNome =
  | 'Localizado'
  | 'NaoLocalizado'
  | 'LocalizadoOutroSetor'
  | 'Inservivel';

export type TipoDivergenciaNome =
  | 'Falta'
  | 'Sobra'
  | 'DivergenciaLocalizacao'
  | 'DivergenciaEstado'
  | 'DivergenciaValor';
export type RecomendacaoDivergenciaNome = 'Baixa' | 'Transferencia' | 'Incorporacao' | 'Reavaliacao';

// ---------------------------------------------------------------------------
// DTOs de leitura (projeções dos handlers de Query)
// ---------------------------------------------------------------------------

/** MembroComissaoDto — membro da comissão de inventário (ObterInventario.cs). */
export interface MembroComissaoDto {
  responsavelId: string;
  nome: string;
  presidente: boolean;
}

/** ItemInventarioDto — linha do inventário: snapshot congelado + contagem física. */
export interface ItemInventarioDto {
  id: string;
  bemPatrimonialId: string;
  numeroTombamento: string | null;
  descricaoSnapshot: string;
  localizacaoEsperada: string | null;
  valorContabilSnapshot: number;
  situacaoEncontrada: SituacaoEncontradaNome | null;
  localizacaoEncontrada: string | null;
  contado: boolean;
}

/** DivergenciaInventarioDto — divergência apurada na conciliação (ConciliarInventario.cs). */
export interface DivergenciaInventarioDto {
  id: string;
  tipo: TipoDivergenciaNome;
  bemPatrimonialId: string | null;
  descricao: string;
  recomendacao: RecomendacaoDivergenciaNome;
}

/** InventarioDetalhe — ficha + comissão + itens + divergências (ObterInventario.cs). */
export interface InventarioDetalhe {
  id: string;
  exercicio: number;
  tipo: TipoInventarioNome;
  setor: string | null;
  portaria: string;
  situacao: SituacaoInventarioNome;
  dataAbertura: string;
  dataEncerramento: string | null;
  comissao: MembroComissaoDto[];
  itens: ItemInventarioDto[];
  divergencias: DivergenciaInventarioDto[];
}

/** InventarioItemLista — projeção enxuta da lista (BuscarInventarios.cs). */
export interface InventarioItemLista {
  id: string;
  exercicio: number;
  tipo: TipoInventarioNome;
  setor: string | null;
  situacao: SituacaoInventarioNome;
  dataAbertura: string;
  dataEncerramento: string | null;
  totalItens: number;
  totalDivergencias: number;
}

/** Filtros da lista navegável de inventários. */
export interface InventarioFiltro {
  exercicio?: number;
  setor?: string;
  situacao?: SituacaoInventarioValor;
  pagina: number;
  tamanho: number;
}

// ---------------------------------------------------------------------------
// DTOs de escrita (Commands / Payloads)
// ---------------------------------------------------------------------------

/** MembroComissaoEntrada — membro no AbrirInventarioCommand. */
export interface MembroComissaoEntradaInput {
  responsavelId: string;
  nome: string;
  presidente: boolean;
}

/** AbrirInventarioCommand (AbrirInventario.cs). */
export interface AbrirInventarioInput {
  exercicio: number;
  tipo: number;
  setor?: string | null;
  portaria: string;
  dataAbertura: string;
  membros: MembroComissaoEntradaInput[];
  minimoMembrosComissao?: number | null;
}

/** RegistrarContagemPayload (PatrimonioEndpoints.cs). */
export interface RegistrarContagemInput {
  bemPatrimonialId: string;
  situacaoEncontrada: number;
  localizacaoEncontrada?: string | null;
  observacao?: string | null;
}

/** RegistrarBemNaoCadastradoPayload (sobra). */
export interface RegistrarSobraInput {
  descricao: string;
  localizacao: string;
  valorEstimado: number;
}

/** EncerrarInventarioPayload. */
export interface EncerrarInventarioInput {
  dataEncerramento: string;
}

/** CancelarInventarioPayload. */
export interface CancelarInventarioInput {
  motivo: string;
}

interface CriadoResponse {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação consistente)
// ---------------------------------------------------------------------------

export const inventarioKeys = {
  all: ['patrimonio', 'inventario'] as const,
  detalhe: (id: string) => [...inventarioKeys.all, 'detalhe', id] as const,
  divergencias: (id: string) => [...inventarioKeys.all, 'divergencias', id] as const,
  lista: (filtro: InventarioFiltro) =>
    [
      ...inventarioKeys.all,
      'lista',
      filtro.exercicio ?? '',
      filtro.setor ?? '',
      filtro.situacao ?? '',
      filtro.pagina,
      filtro.tamanho,
    ] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarInventarios(
  filtro: InventarioFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<InventarioItemLista>> {
  return http.get<ResultadoPaginado<InventarioItemLista>>('/patrimonio/inventarios', {
    query: {
      exercicio: filtro.exercicio,
      setor: filtro.setor,
      situacao: filtro.situacao,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterInventario(id: string, signal?: AbortSignal): Promise<InventarioDetalhe> {
  return http.get<InventarioDetalhe>(`/patrimonio/inventarios/${id}`, { signal });
}

function listarDivergencias(id: string, signal?: AbortSignal): Promise<DivergenciaInventarioDto[]> {
  return http.get<DivergenciaInventarioDto[]>(`/patrimonio/inventarios/${id}/divergencias`, {
    signal,
  });
}

function abrirInventario(input: AbrirInventarioInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/patrimonio/inventarios', input);
}

function carregarSnapshot(id: string): Promise<void> {
  return http.post<void>(`/patrimonio/inventarios/${id}/snapshot`);
}

function registrarContagem(id: string, input: RegistrarContagemInput): Promise<void> {
  return http.post<void>(`/patrimonio/inventarios/${id}/contagens`, input);
}

function registrarSobra(id: string, input: RegistrarSobraInput): Promise<void> {
  return http.post<void>(`/patrimonio/inventarios/${id}/sobras`, input);
}

function conciliarInventario(id: string): Promise<DivergenciaInventarioDto[]> {
  return http.post<DivergenciaInventarioDto[]>(`/patrimonio/inventarios/${id}/conciliacao`);
}

function encerrarInventario(id: string, input: EncerrarInventarioInput): Promise<void> {
  return http.post<void>(`/patrimonio/inventarios/${id}/encerramento`, input);
}

function cancelarInventario(id: string, input: CancelarInventarioInput): Promise<void> {
  return http.post<void>(`/patrimonio/inventarios/${id}/cancelamento`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — Consultas
// ---------------------------------------------------------------------------

/** Lista/busca paginada de inventários por exercício/setor/situação. */
export function useInventariosLista(filtro: InventarioFiltro) {
  return useQuery({
    queryKey: inventarioKeys.lista(filtro),
    queryFn: ({ signal }) => listarInventarios(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** ObterInventario — detalhe (ficha + comissão + itens + divergências). */
export function useInventario(id: string) {
  return useQuery({
    queryKey: inventarioKeys.detalhe(id),
    queryFn: ({ signal }) => obterInventario(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** ListarDivergencias — divergências apuradas do inventário. */
export function useDivergencias(id: string, enabled = true) {
  return useQuery({
    queryKey: inventarioKeys.divergencias(id),
    queryFn: ({ signal }) => listarDivergencias(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — Comandos
// ---------------------------------------------------------------------------

/** AbrirInventario — cria inventário com comissão/portaria; invalida a lista. */
export function useAbrirInventario() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirInventario,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...inventarioKeys.all, 'lista'] });
    },
  });
}

/** CarregarSnapshot — congela o acervo (EmAbertura -> EmContagem); invalida o detalhe. */
export function useCarregarSnapshot(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => carregarSnapshot(id),
    onSuccess: () => invalidarInventario(queryClient, id),
  });
}

/** RegistrarContagem — registra a contagem física de um item; invalida o detalhe. */
export function useRegistrarContagem(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarContagemInput) => registrarContagem(id, input),
    onSuccess: () => invalidarInventario(queryClient, id),
  });
}

/** RegistrarSobra — registra bem físico sem tombo (achado); invalida o detalhe. */
export function useRegistrarSobra(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarSobraInput) => registrarSobra(id, input),
    onSuccess: () => invalidarInventario(queryClient, id),
  });
}

/** ConciliarInventario — apura divergências (EmContagem -> EmConciliacao); invalida o detalhe. */
export function useConciliarInventario(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => conciliarInventario(id),
    onSuccess: () => invalidarInventario(queryClient, id),
  });
}

/** EncerrarInventario — encerra (exige conciliação prévia); invalida o detalhe. */
export function useEncerrarInventario(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: EncerrarInventarioInput) => encerrarInventario(id, input),
    onSuccess: () => invalidarInventario(queryClient, id),
  });
}

/** CancelarInventario — cancela (terminal, sem efeito patrimonial); invalida o detalhe. */
export function useCancelarInventario(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CancelarInventarioInput) => cancelarInventario(id, input),
    onSuccess: () => invalidarInventario(queryClient, id),
  });
}

function invalidarInventario(queryClient: ReturnType<typeof useQueryClient>, id: string): void {
  queryClient.invalidateQueries({ queryKey: inventarioKeys.detalhe(id) });
  queryClient.invalidateQueries({ queryKey: inventarioKeys.divergencias(id) });
  queryClient.invalidateQueries({ queryKey: [...inventarioKeys.all, 'lista'] });
}
