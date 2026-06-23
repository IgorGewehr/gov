// API do agregado Merenda (PNAE) — módulo Educação. DTOs + acesso HTTP + hooks
// TanStack Query. Espelha os endpoints REAIS sob /api/educacao/merenda
// (EducacaoEndpoints.cs / MerendaDtos.cs):
//   GET    /educacao/merenda/cardapios?escolaId=&semana=        -> ListarCardapios -> CardapioDto[]
//   GET    /educacao/merenda/cardapios/{cardapioId}             -> ObterCardapio   -> CardapioDto | null
//   POST   /educacao/merenda/cardapios                          -> PlanejarCardapio        -> { id }
//   POST   /educacao/merenda/cardapios/{cardapioId}/itens       -> AdicionarItemCardapio    -> { id }
//   POST   /educacao/merenda/cardapios/{cardapioId}/publicacao  -> PublicarCardapio         -> 204
//   POST   /educacao/merenda/distribuicoes                      -> RegistrarDistribuicao    -> { id }
//   GET    /educacao/merenda/consumo?escolaId=&de=&ate=         -> ObterConsumoMerenda      -> RelatorioConsumoDto
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';

/** Faixa etária PNAE (Domain.Merenda.FaixaEtariaPnae) — valor numérico (IsInEnum). */
export type FaixaEtariaPnae = 0 | 1 | 2 | 3;

/** Tipo de refeição (Domain.Merenda.TipoRefeicao) — valor numérico (IsInEnum). */
export type TipoRefeicao = 0 | 1 | 2 | 3 | 4;

/** Dia da semana do cardápio (Domain.Merenda.DiaSemanaCardapio) — valor numérico. */
export type DiaSemanaCardapio = 1 | 2 | 3 | 4 | 5;

/** Item planejado projetado na ficha do cardápio (ItemCardapioDto). */
export interface ItemCardapioDto {
  id: string;
  /** Dia da semana (descrição: Segunda…Sexta). */
  dia: string;
  /** Tipo de refeição (descrição). */
  refeicao: string;
  /** Gênero (ItemEstoque de Patrimônio) por Id. */
  generoEstoqueId: string;
  quantidadePerCapita: number;
  unidadeMedida: string;
}

/** Ficha/projeção do cardápio semanal com itens (CardapioDto). */
export interface CardapioDto {
  id: string;
  escolaId: string;
  /** Faixa etária PNAE (descrição). */
  faixaEtaria: string;
  /** Semana (segunda-feira) — ISO yyyy-MM-dd. */
  semana: string;
  /** Situação (descrição: Planejado | Publicado | Encerrado). */
  situacao: string;
  itens: ItemCardapioDto[];
}

/** Consumo agregado por gênero no relatório de consumo PNAE (ConsumoGeneroPeriodoDto). */
export interface ConsumoGeneroPeriodoDto {
  generoEstoqueId: string;
  quantidadeTotal: number;
  unidadeMedida: string;
}

/** Relatório de consumo de gêneros por escola/período (RelatorioConsumoDto). */
export interface RelatorioConsumoDto {
  escolaId: string;
  de: string;
  ate: string;
  totalComensais: number;
  generos: ConsumoGeneroPeriodoDto[];
}

/** Corpo de POST /merenda/cardapios (PlanejarCardapioCommand). */
export interface PlanejarCardapioInput {
  escolaId: string;
  faixaEtaria: FaixaEtariaPnae;
  /** Semana (segunda-feira) — ISO yyyy-MM-dd. */
  semana: string;
}

/** Corpo de POST /merenda/cardapios/{cardapioId}/itens (AdicionarItemCardapioPayload). */
export interface AdicionarItemCardapioInput {
  dia: DiaSemanaCardapio;
  refeicao: TipoRefeicao;
  generoEstoqueId: string;
  quantidadePerCapita: number;
  unidadeMedida: string;
}

/** Corpo de POST /merenda/distribuicoes (RegistrarDistribuicaoMerendaCommand). */
export interface RegistrarDistribuicaoInput {
  cardapioId: string;
  /** Data efetiva — ISO yyyy-MM-dd. */
  data: string;
  dia: DiaSemanaCardapio;
  refeicao: TipoRefeicao;
  comensais: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarCardapios(
  escolaId: string | undefined,
  semana: string | undefined,
  signal?: AbortSignal,
): Promise<CardapioDto[]> {
  return http.get<CardapioDto[]>('/educacao/merenda/cardapios', {
    query: { escolaId, semana },
    signal,
  });
}

function obterCardapio(cardapioId: string, signal?: AbortSignal): Promise<CardapioDto | null> {
  return http.get<CardapioDto | null>(`/educacao/merenda/cardapios/${cardapioId}`, { signal });
}

function planejarCardapio(input: PlanejarCardapioInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/merenda/cardapios', input);
}

function adicionarItem(
  cardapioId: string,
  input: AdicionarItemCardapioInput,
): Promise<CriadoResponse> {
  return http.post<CriadoResponse>(`/educacao/merenda/cardapios/${cardapioId}/itens`, input);
}

function publicarCardapio(cardapioId: string): Promise<void> {
  return http.post<void>(`/educacao/merenda/cardapios/${cardapioId}/publicacao`);
}

function registrarDistribuicao(input: RegistrarDistribuicaoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/merenda/distribuicoes', input);
}

function obterConsumo(
  escolaId: string,
  de: string,
  ate: string,
  signal?: AbortSignal,
): Promise<RelatorioConsumoDto> {
  return http.get<RelatorioConsumoDto>('/educacao/merenda/consumo', {
    query: { escolaId, de, ate },
    signal,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista cardápios por escola e/ou semana (filtros opcionais). */
export function useCardapios(escolaId?: string, semana?: string) {
  const filtro = { escolaId: escolaId || undefined, semana: semana || undefined };
  return useQuery({
    queryKey: educacaoKeys.cardapiosBusca(filtro),
    queryFn: ({ signal }) => listarCardapios(filtro.escolaId, filtro.semana, signal),
  });
}

/** Obtém a ficha de um cardápio (com itens). `enabled` controla disparo sob demanda. */
export function useCardapio(cardapioId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.cardapioPorId(cardapioId),
    queryFn: ({ signal }) => obterCardapio(cardapioId, signal),
    enabled: enabled && cardapioId.trim().length > 0,
  });
}

/** Planeja um cardápio semanal (situação inicial Planejado) e invalida as listas. */
export function usePlanejarCardapio() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: planejarCardapio,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.cardapios() });
    },
  });
}

/** Adiciona um item (gênero) ao cardápio em dia/refeição. Invalida a ficha + listas. */
export function useAdicionarItemCardapio(cardapioId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarItemCardapioInput) => adicionarItem(cardapioId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.cardapioPorId(cardapioId) });
      queryClient.invalidateQueries({ queryKey: educacaoKeys.cardapios() });
    },
  });
}

/** Publica o cardápio (base das distribuições; não admite mais edição de itens). */
export function usePublicarCardapio(cardapioId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => publicarCardapio(cardapioId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.cardapioPorId(cardapioId) });
      queryClient.invalidateQueries({ queryKey: educacaoKeys.cardapios() });
    },
  });
}

/** Registra a distribuição/consumo de merenda de um dia. Invalida o relatório de consumo. */
export function useRegistrarDistribuicao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registrarDistribuicao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.merenda() });
    },
  });
}

/** Relatório de consumo de gêneros por escola/período. `enabled` dispara sob demanda. */
export function useConsumoMerenda(escolaId: string, de: string, ate: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.consumoMerenda(escolaId, de, ate),
    queryFn: ({ signal }) => obterConsumo(escolaId, de, ate, signal),
    enabled: enabled && escolaId.trim().length > 0 && de.length > 0 && ate.length > 0,
  });
}
