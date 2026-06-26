// Camada de API do agregado DispensaEletronica (modulo Administracao).
// Dispensa de licitacao em razao do valor, na forma eletronica (Lei 14.133/2021, art. 75, I/II;
// IN SEGES/ME 67/2021 — Sistema de Dispensa Eletronica). Espelha o padrao-ouro de licitacao.api.ts.
// Rotas REAIS (AdministracaoEndpoints.cs): /api/administracao/dispensas...
// O http client ja prefixa /api (ver src/api/http) — usamos /administracao/... aqui.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// Enums / uniões de domínio (string = ToString() do enum no backend)
// ---------------------------------------------------------------------------

/** Fundamento legal da dispensa em razão do valor (art. 75, I ou II). */
export type FundamentoDispensaValor = 'ObrasEServicosEngenharia' | 'OutrosServicosECompras';

/** Critério de julgamento admitido na dispensa eletrônica (IN 67/2021). */
export type CriterioJulgamentoDispensa = 'MenorPreco' | 'MaiorDesconto';

/** Situação no ciclo de vida do procedimento (enum SituacaoDispensa). */
export type SituacaoDispensa =
  | 'Aberta'
  | 'AvisoPublicado'
  | 'EmDisputa'
  | 'EmJulgamento'
  | 'Homologada'
  | 'Fracassada'
  | 'Deserta'
  | 'Revogada'
  | 'Anulada';

/** Situação de uma cotação (enum SituacaoCotacao). */
export type SituacaoCotacao = 'Recebida' | 'Classificada' | 'Desclassificada' | 'Vencedora';

// Conjuntos de referência da máquina de estados.
const SITUACOES_ENCERRADAS: ReadonlySet<SituacaoDispensa> = new Set([
  'Homologada',
  'Fracassada',
  'Deserta',
  'Revogada',
  'Anulada',
]);

/** true quando a dispensa está em estado terminal e não admite transições. */
export function situacaoEncerrada(situacao: SituacaoDispensa): boolean {
  return SITUACOES_ENCERRADAS.has(situacao);
}

// Mapeamento numérico do enum SituacaoDispensa (a query exige o valor do enum).
const SITUACAO_NUMERO: Record<SituacaoDispensa, number> = {
  Aberta: 1,
  AvisoPublicado: 2,
  EmDisputa: 3,
  EmJulgamento: 4,
  Homologada: 5,
  Fracassada: 6,
  Deserta: 7,
  Revogada: 8,
  Anulada: 9,
};

// ---------------------------------------------------------------------------
// DTOs (projeções de leitura — espelham os records do Application)
// ---------------------------------------------------------------------------

/** Resumo de uma dispensa para listagem (DispensaResumo). */
export interface DispensaResumo {
  id: string;
  objeto: string;
  fundamento: FundamentoDispensaValor;
  situacao: SituacaoDispensa;
  valorTotalEstimado: number;
  numeroAviso: string | null;
}

/** Resumo de um item da dispensa (ItemDispensaResumo). */
export interface ItemDispensaResumo {
  itemId: string;
  numero: number;
  itemCatalogoId: string | null;
  descricao: string;
  quantidade: number;
  valorUnitarioEstimado: number;
  valorTotalEstimado: number;
}

/** Resumo de uma cotação (CotacaoDispensaResumo). */
export interface CotacaoDispensaResumo {
  cotacaoId: string;
  fornecedorId: string;
  itemId: string;
  valor: number;
  classificacao: number | null;
  situacao: SituacaoCotacao;
}

/** Detalhe completo de uma dispensa (DispensaDetalhe). */
export interface DispensaDetalhe {
  id: string;
  objeto: string;
  fundamento: FundamentoDispensaValor;
  criterioJulgamento: CriterioJulgamentoDispensa;
  situacao: SituacaoDispensa;
  valorTotalEstimado: number;
  limiteLegalVigente: number;
  limiteLegalNormaFonte: string;
  numeroAviso: string | null;
  aberturaDisputa: string | null;
  numeroPncp: string | null;
  cotacaoVencedoraId: string | null;
  itens: ItemDispensaResumo[];
  cotacoes: CotacaoDispensaResumo[];
}

// ---------------------------------------------------------------------------
// Inputs de comando (espelham os *Command + payloads dos endpoints)
// ---------------------------------------------------------------------------

/** AbrirDispensaCommand. */
export interface AbrirDispensaInput {
  objeto: string;
  fundamento: FundamentoDispensaValor;
  criterioJulgamento: CriterioJulgamentoDispensa;
  etpId?: string | null;
  termoReferenciaId?: string | null;
}

/** AdicionarItemDispensaPayload. */
export interface AdicionarItemDispensaInput {
  itemCatalogoId?: string | null;
  descricao: string;
  quantidade: number;
  valorUnitarioEstimado: number;
}

/** PublicarAvisoDispensaPayload. */
export interface PublicarAvisoDispensaInput {
  numeroAviso: string;
  aberturaDisputa: string;
}

/** RegistrarLanceDispensaPayload. */
export interface RegistrarLanceDispensaInput {
  itemId: string;
  fornecedorId: string;
  valor: number;
}

/** HomologarDispensaPayload. */
export interface HomologarDispensaInput {
  vencedorHabilitado: boolean;
}

/** Comandos de encerramento com motivação (Revogar/Anular/Fracassar). */
export interface MotivoInput {
  motivo: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const dispensaKeys = {
  all: ['administracao', 'dispensas'] as const,
  porSituacao: (situacao: SituacaoDispensa) => [...dispensaKeys.all, 'situacao', situacao] as const,
  detalhe: (id: string) => [...dispensaKeys.all, 'detalhe', id] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas reais de AdministracaoEndpoints.cs)
// ---------------------------------------------------------------------------

function listarPorSituacao(situacao: SituacaoDispensa, signal?: AbortSignal): Promise<DispensaResumo[]> {
  return http.get<DispensaResumo[]>('/administracao/dispensas', {
    query: { situacao: SITUACAO_NUMERO[situacao] },
    signal,
  });
}

function obterDispensa(id: string, signal?: AbortSignal): Promise<DispensaDetalhe> {
  return http.get<DispensaDetalhe>(`/administracao/dispensas/${id}`, { signal });
}

function abrirDispensa(input: AbrirDispensaInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/dispensas', input);
}

function adicionarItem(dispensaId: string, input: AdicionarItemDispensaInput): Promise<{ itemId: string }> {
  return http.post<{ itemId: string }>(`/administracao/dispensas/${dispensaId}/itens`, input);
}

function publicarAviso(dispensaId: string, input: PublicarAvisoDispensaInput): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/aviso`, input);
}

function abrirDisputa(dispensaId: string): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/disputa/abrir`, {});
}

function registrarLance(dispensaId: string, input: RegistrarLanceDispensaInput): Promise<{ cotacaoId: string }> {
  return http.post<{ cotacaoId: string }>(`/administracao/dispensas/${dispensaId}/lances`, input);
}

function encerrarDisputa(dispensaId: string): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/disputa/encerrar`, {});
}

function homologarDispensa(dispensaId: string, input: HomologarDispensaInput): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/homologar`, input);
}

function declararFracassada(dispensaId: string, input: MotivoInput): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/fracassar`, input);
}

function declararDeserta(dispensaId: string): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/deserta`, {});
}

function revogarDispensa(dispensaId: string, input: MotivoInput): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/revogar`, input);
}

function anularDispensa(dispensaId: string, input: MotivoInput): Promise<void> {
  return http.post<void>(`/administracao/dispensas/${dispensaId}/anular`, input);
}

// ---------------------------------------------------------------------------
// Hooks de consulta (queries)
// ---------------------------------------------------------------------------

/** ListarDispensasPorSituacao — lista as dispensas do tenant na situação informada. */
export function useDispensasPorSituacao(situacao: SituacaoDispensa, enabled = true) {
  return useQuery({
    queryKey: dispensaKeys.porSituacao(situacao),
    queryFn: ({ signal }) => listarPorSituacao(situacao, signal),
    enabled,
  });
}

/** ObterDispensaPorId — detalhe completo (itens + cotações). */
export function useDispensa(id: string) {
  return useQuery({
    queryKey: dispensaKeys.detalhe(id),
    queryFn: ({ signal }) => obterDispensa(id, signal),
    enabled: id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks de comando (mutations)
// ---------------------------------------------------------------------------

/** AbrirDispensa — cria uma nova dispensa (situação inicial Aberta). */
export function useAbrirDispensa() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirDispensa,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: dispensaKeys.porSituacao('Aberta') });
    },
  });
}

// Invalida o detalhe + todas as listas por situação (a transição muda a situação).
function invalidarDispensa(queryClient: ReturnType<typeof useQueryClient>, dispensaId: string): void {
  queryClient.invalidateQueries({ queryKey: dispensaKeys.detalhe(dispensaId) });
  queryClient.invalidateQueries({ queryKey: dispensaKeys.all });
}

/** AdicionarItemDispensa — inclui um item (fail-closed do teto legal no backend). */
export function useAdicionarItemDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarItemDispensaInput) => adicionarItem(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** PublicarAvisoDispensa — publica o aviso de contratação direta (valida prazo mínimo). */
export function usePublicarAvisoDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PublicarAvisoDispensaInput) => publicarAviso(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** AbrirDisputaDispensa — abre a etapa de envio de lances. */
export function useAbrirDisputaDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => abrirDisputa(dispensaId),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** RegistrarLanceDispensa — registra uma cotação/lance de um fornecedor. */
export function useRegistrarLanceDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarLanceDispensaInput) => registrarLance(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** EncerrarDisputaDispensa — encerra a disputa e julga (indica vencedora). */
export function useEncerrarDisputaDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => encerrarDisputa(dispensaId),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** HomologarDispensa — ato da autoridade competente (EmJulgamento -> Homologada). */
export function useHomologarDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: HomologarDispensaInput) => homologarDispensa(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** DeclararDispensaFracassada — encerra por inexistência de cotação válida/habilitada. */
export function useDeclararDispensaFracassada(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoInput) => declararFracassada(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** DeclararDispensaDeserta — encerra por ausência total de cotações. */
export function useDeclararDispensaDeserta(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => declararDeserta(dispensaId),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** RevogarDispensa — encerra por conveniência/oportunidade (motivo obrigatório). */
export function useRevogarDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoInput) => revogarDispensa(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}

/** AnularDispensa — encerra por ilegalidade (motivo obrigatório). */
export function useAnularDispensa(dispensaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoInput) => anularDispensa(dispensaId, input),
    onSuccess: () => invalidarDispensa(queryClient, dispensaId),
  });
}
