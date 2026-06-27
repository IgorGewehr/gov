// Camada de API do agregado Credenciamento (modulo Administracao).
// Credenciamento como forma auxiliar de contratacao (Lei 14.133/2021, art. 78, I e art. 79),
// processado por inexigibilidade (art. 74, IV). Rotas REAIS: /api/administracao/credenciamentos...
// O http client ja prefixa /api (ver src/api/http) — usamos /administracao/... aqui.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// Enums / uniões de domínio (string = ToString() do enum no backend)
// ---------------------------------------------------------------------------

/** Hipótese autorizadora (art. 79, I a III). */
export type HipoteseCredenciamento =
  | 'ParalelaNaoExcludente'
  | 'SelecaoCriterioBeneficiario'
  | 'MercadosFluidos';

/** Situação do edital (enum SituacaoCredenciamento). */
export type SituacaoCredenciamento =
  | 'EmElaboracao'
  | 'ChamamentoAberto'
  | 'Suspenso'
  | 'Encerrado'
  | 'Anulado'
  | 'Revogado';

/** Situação da inscrição (enum SituacaoCredenciado). */
export type SituacaoCredenciado =
  | 'EmAnalise'
  | 'Credenciado'
  | 'Indeferido'
  | 'Suspenso'
  | 'Descredenciado';

// Mapeamento numérico do enum SituacaoCredenciamento (a query aceita o valor do enum).
const SITUACAO_NUMERO: Record<SituacaoCredenciamento, number> = {
  EmElaboracao: 1,
  ChamamentoAberto: 2,
  Suspenso: 3,
  Encerrado: 4,
  Anulado: 5,
  Revogado: 6,
};

const SITUACOES_TERMINAIS: ReadonlySet<SituacaoCredenciamento> = new Set([
  'Encerrado',
  'Anulado',
  'Revogado',
]);

/** true quando o edital está em estado terminal e não admite transições. */
export function credenciamentoEncerrado(situacao: SituacaoCredenciamento): boolean {
  return SITUACOES_TERMINAIS.has(situacao);
}

// ---------------------------------------------------------------------------
// DTOs (projeções de leitura — espelham os records do Application)
// ---------------------------------------------------------------------------

/** Item credenciável (objeto + preço fixado). */
export interface ItemCredenciamentoDetalhe {
  itemId: string;
  numero: number;
  itemCatalogoId: string | null;
  descricao: string;
  unidadeMedida: string;
  precoFixado: number;
}

/** Inscrição/credenciado no rol. */
export interface CredenciadoDetalhe {
  credenciadoId: string;
  fornecedorId: string;
  dataInscricao: string;
  situacao: SituacaoCredenciado;
  dataCredenciamento: string | null;
  dataDescredenciamento: string | null;
  motivo: string | null;
}

/** Detalhe completo do credenciamento. */
export interface CredenciamentoDetalhe {
  id: string;
  objeto: string;
  hipotese: HipoteseCredenciamento;
  situacao: SituacaoCredenciamento;
  numeroEdital: string | null;
  vigenciaInicio: string;
  vigenciaFim: string;
  fundamentacaoLegal: string;
  numeroPncp: string | null;
  quantidadeCredenciadosAptos: number;
  itens: ItemCredenciamentoDetalhe[];
  credenciados: CredenciadoDetalhe[];
}

/** Resumo do credenciamento, para listagem. */
export interface CredenciamentoResumo {
  id: string;
  objeto: string;
  hipotese: HipoteseCredenciamento;
  situacao: SituacaoCredenciamento;
  numeroEdital: string | null;
  vigenciaFim: string;
  quantidadeCredenciadosAptos: number;
}

// ---------------------------------------------------------------------------
// Inputs de comando (espelham os *Command + payloads dos endpoints)
// ---------------------------------------------------------------------------

/** AbrirCredenciamentoCommand. */
export interface AbrirCredenciamentoInput {
  objeto: string;
  hipotese: HipoteseCredenciamento;
  vigenciaInicio: string;
  vigenciaFim: string;
  fundamentacaoLegal: string;
  etpId?: string | null;
  termoReferenciaId?: string | null;
}

/** AdicionarItemCredenciamentoPayload. */
export interface AdicionarItemCredenciamentoInput {
  itemCatalogoId?: string | null;
  descricao: string;
  unidadeMedida: string;
  precoFixado: number;
}

/** Comandos com motivação (suspender/encerrar/indeferir/descredenciar). */
export interface MotivoInput {
  motivo: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const credenciamentoKeys = {
  all: ['administracao', 'credenciamentos'] as const,
  lista: (situacao: SituacaoCredenciamento | 'Todas') =>
    [...credenciamentoKeys.all, 'lista', situacao] as const,
  detalhe: (id: string) => [...credenciamentoKeys.all, 'detalhe', id] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas reais de AdministracaoEndpoints.Credenciamentos.cs)
// ---------------------------------------------------------------------------

function listar(situacao: SituacaoCredenciamento | 'Todas', signal?: AbortSignal): Promise<CredenciamentoResumo[]> {
  const query = situacao === 'Todas' ? undefined : { situacao: SITUACAO_NUMERO[situacao] };
  return http.get<CredenciamentoResumo[]>('/administracao/credenciamentos', { query, signal });
}

function obter(id: string, signal?: AbortSignal): Promise<CredenciamentoDetalhe> {
  return http.get<CredenciamentoDetalhe>(`/administracao/credenciamentos/${id}`, { signal });
}

function abrir(input: AbrirCredenciamentoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/credenciamentos', input);
}

function adicionarItem(id: string, input: AdicionarItemCredenciamentoInput): Promise<{ itemId: string }> {
  return http.post<{ itemId: string }>(`/administracao/credenciamentos/${id}/itens`, input);
}

function removerItem(id: string, itemId: string): Promise<void> {
  return http.delete<void>(`/administracao/credenciamentos/${id}/itens/${itemId}`);
}

function publicarChamamento(id: string, numeroEdital: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/publicar-chamamento`, { numeroEdital });
}

function alterarChamamento(id: string, suspender: boolean, motivo?: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/chamamento`, { suspender, motivo });
}

function encerrar(id: string, situacao: SituacaoCredenciamento, motivo: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/encerrar`, { situacao, motivo });
}

function inscrever(id: string, fornecedorId: string): Promise<{ credenciadoId: string }> {
  return http.post<{ credenciadoId: string }>(`/administracao/credenciamentos/${id}/inscricoes`, { fornecedorId });
}

function deferir(id: string, credenciadoId: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/inscricoes/${credenciadoId}/deferir`, {});
}

function indeferir(id: string, credenciadoId: string, motivo: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/inscricoes/${credenciadoId}/indeferir`, { motivo });
}

function alterarCredenciado(id: string, credenciadoId: string, suspender: boolean, motivo?: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/credenciados/${credenciadoId}/situacao`, { suspender, motivo });
}

function descredenciar(id: string, credenciadoId: string, motivo: string): Promise<void> {
  return http.post<void>(`/administracao/credenciamentos/${id}/credenciados/${credenciadoId}/descredenciar`, { motivo });
}

// ---------------------------------------------------------------------------
// Hooks de consulta (queries)
// ---------------------------------------------------------------------------

/** ListarCredenciamentos — lista do tenant (todos ou por situação). */
export function useCredenciamentos(situacao: SituacaoCredenciamento | 'Todas') {
  return useQuery({
    queryKey: credenciamentoKeys.lista(situacao),
    queryFn: ({ signal }) => listar(situacao, signal),
  });
}

/** ObterCredenciamentoPorId — detalhe completo (itens + rol). */
export function useCredenciamento(id: string) {
  return useQuery({
    queryKey: credenciamentoKeys.detalhe(id),
    queryFn: ({ signal }) => obter(id, signal),
    enabled: id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks de comando (mutations)
// ---------------------------------------------------------------------------

/** AbrirCredenciamento — cria um novo edital (situação inicial EmElaboracao). */
export function useAbrirCredenciamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrir,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: credenciamentoKeys.all }),
  });
}

// Invalida o detalhe + todas as listas (transições mudam a situação).
function invalidar(queryClient: ReturnType<typeof useQueryClient>, id: string): void {
  queryClient.invalidateQueries({ queryKey: credenciamentoKeys.detalhe(id) });
  queryClient.invalidateQueries({ queryKey: credenciamentoKeys.all });
}

/** AdicionarItemCredenciamento — inclui um item credenciável (preço fixado). */
export function useAdicionarItemCredenciamento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarItemCredenciamentoInput) => adicionarItem(id, input),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** RemoverItemCredenciamento — remove um item do edital em elaboração. */
export function useRemoverItemCredenciamento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (itemId: string) => removerItem(id, itemId),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** PublicarChamamento — abre as inscrições permanentes (art. 79, par. único). */
export function usePublicarChamamento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (numeroEdital: string) => publicarChamamento(id, numeroEdital),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** AlterarChamamento — suspende/reabre o recebimento de inscrições. */
export function useAlterarChamamento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ suspender, motivo }: { suspender: boolean; motivo?: string }) =>
      alterarChamamento(id, suspender, motivo),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** EncerrarCredenciamento — encerra/anula/revoga o edital (terminal). */
export function useEncerrarCredenciamento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ situacao, motivo }: { situacao: SituacaoCredenciamento; motivo: string }) =>
      encerrar(id, situacao, motivo),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** InscreverInteressado — protocola a adesão de um fornecedor (ingresso a qualquer tempo). */
export function useInscreverInteressado(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (fornecedorId: string) => inscrever(id, fornecedorId),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** DeferirInscricao — habilita o interessado (torna-se credenciado apto). */
export function useDeferirInscricao(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (credenciadoId: string) => deferir(id, credenciadoId),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** IndeferirInscricao — recusa a inscrição por não atendimento das condições. */
export function useIndeferirInscricao(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ credenciadoId, motivo }: { credenciadoId: string; motivo: string }) =>
      indeferir(id, credenciadoId, motivo),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** AlterarCredenciado — suspende/reabilita um credenciado. */
export function useAlterarCredenciado(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ credenciadoId, suspender, motivo }: { credenciadoId: string; suspender: boolean; motivo?: string }) =>
      alterarCredenciado(id, credenciadoId, suspender, motivo),
    onSuccess: () => invalidar(queryClient, id),
  });
}

/** Descredenciar — encerra o credenciamento de um interessado (terminal). */
export function useDescredenciar(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ credenciadoId, motivo }: { credenciadoId: string; motivo: string }) =>
      descredenciar(id, credenciadoId, motivo),
    onSuccess: () => invalidar(queryClient, id),
  });
}
