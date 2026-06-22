// Camada de API do agregado Licitacao (modulo Administracao — Lei 14.133/2021).
// Espelha o padrao-ouro de src/modules/tributos/api.ts:
//   DTOs -> query keys -> funcoes http tipadas -> hooks TanStack Query.
// Rotas REAIS (AdministracaoEndpoints.cs): /api/administracao/licitacoes...
// O http client ja prefixa /api (ver src/api/http) — usamos /administracao/... aqui.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// Enums / uniões de domínio (string = ToString() do enum no backend)
// ---------------------------------------------------------------------------

/** Modalidade do certame (art. 28/74/75 da NLLC). */
export type ModalidadeLicitacao =
  | 'Pregao'
  | 'Concorrencia'
  | 'DialogoCompetitivo'
  | 'Dispensa'
  | 'Inexigibilidade';

/** Critério de julgamento (art. 33 da NLLC). */
export type CriterioJulgamento =
  | 'MenorPreco'
  | 'MaiorDesconto'
  | 'MelhorTecnica'
  | 'TecnicaEPreco'
  | 'MaiorLance'
  | 'MaiorRetornoEconomico';

/** Situação no ciclo de vida do certame (enum SituacaoLicitacao). */
export type SituacaoLicitacao =
  | 'Aberta'
  | 'EmJulgamento'
  | 'Homologada'
  | 'Fracassada'
  | 'Deserta'
  | 'Revogada'
  | 'Anulada';

/** Situação de uma proposta (enum SituacaoProposta). */
export type SituacaoProposta = 'Recebida' | 'Classificada' | 'Desclassificada' | 'Vencedora';

/** Resultado da habilitação de um licitante (enum ResultadoHabilitacao). */
export type ResultadoHabilitacao = 'Habilitado' | 'Inabilitado';

// Conjuntos de referência da máquina de estados (rules §2.4 / §4).
const SITUACOES_ENCERRADAS: ReadonlySet<SituacaoLicitacao> = new Set([
  'Homologada',
  'Fracassada',
  'Deserta',
  'Revogada',
  'Anulada',
]);

/** true quando o certame está em estado terminal e não admite transições (I-12). */
export function situacaoEncerrada(situacao: SituacaoLicitacao): boolean {
  return SITUACOES_ENCERRADAS.has(situacao);
}

// Mapeamento numérico do enum SituacaoLicitacao (a query exige o valor do enum).
const SITUACAO_NUMERO: Record<SituacaoLicitacao, number> = {
  Aberta: 1,
  EmJulgamento: 2,
  Homologada: 3,
  Fracassada: 4,
  Deserta: 5,
  Revogada: 6,
  Anulada: 7,
};

// ---------------------------------------------------------------------------
// DTOs (projeções de leitura — espelham os records do Application)
// ---------------------------------------------------------------------------

/** Resumo de uma licitação para listagem (LicitacaoResumo). */
export interface LicitacaoResumo {
  id: string;
  objeto: string;
  modalidade: ModalidadeLicitacao;
  situacao: SituacaoLicitacao;
  valorEstimado: number;
  numeroEditalPncp: string | null;
}

/** Resumo de um lote (LoteResumo). */
export interface LoteResumo {
  loteId: string;
  numero: number;
  descricao: string;
  valorEstimado: number;
}

/** Resumo de uma proposta (PropostaResumo). */
export interface PropostaResumo {
  propostaId: string;
  fornecedorId: string;
  loteId: string;
  valor: number;
  classificacao: number | null;
  situacao: SituacaoProposta;
}

/** Detalhe completo de uma licitação (LicitacaoDetalhe). */
export interface LicitacaoDetalhe {
  id: string;
  objeto: string;
  modalidade: ModalidadeLicitacao;
  criterioJulgamento: CriterioJulgamento;
  valorEstimado: number;
  situacao: SituacaoLicitacao;
  numeroEditalPncp: string | null;
  propostaVencedoraId: string | null;
  lotes: LoteResumo[];
  propostas: PropostaResumo[];
}

// ---------------------------------------------------------------------------
// Inputs de comando (espelham os *Command + payloads dos endpoints)
// ---------------------------------------------------------------------------

/** AbrirLicitacaoCommand. */
export interface AbrirLicitacaoInput {
  objeto: string;
  modalidade: ModalidadeLicitacao;
  criterioJulgamento: CriterioJulgamento;
  valorEstimado: number;
  etpId?: string | null;
  termoReferenciaId?: string | null;
}

/** PublicarEditalPncpPayload. */
export interface PublicarEditalPncpInput {
  numeroEditalPncp: string;
}

/** JulgarPropostasPayload. */
export interface JulgarPropostasInput {
  propostaVencedoraId: string;
}

/** HabilitarLicitanteCommand (sem o LicitacaoId, que vai na URL). */
export interface HabilitarLicitanteInput {
  fornecedorId: string;
  resultado: ResultadoHabilitacao;
  motivo?: string | null;
}

/** Comandos de encerramento com motivação (Revogar/Anular/DeclararFracassada). */
export interface MotivoInput {
  motivo: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const licitacaoKeys = {
  all: ['administracao', 'licitacoes'] as const,
  porSituacao: (situacao: SituacaoLicitacao) => [...licitacaoKeys.all, 'situacao', situacao] as const,
  detalhe: (id: string) => [...licitacaoKeys.all, 'detalhe', id] as const,
  propostas: (id: string) => [...licitacaoKeys.all, 'propostas', id] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas reais de AdministracaoEndpoints.cs)
// ---------------------------------------------------------------------------

function listarPorSituacao(situacao: SituacaoLicitacao, signal?: AbortSignal): Promise<LicitacaoResumo[]> {
  // GET /api/administracao/licitacoes?situacao={enum}. O binding aceita o valor numérico do enum.
  return http.get<LicitacaoResumo[]>('/administracao/licitacoes', {
    query: { situacao: SITUACAO_NUMERO[situacao] },
    signal,
  });
}

function obterLicitacao(id: string, signal?: AbortSignal): Promise<LicitacaoDetalhe> {
  return http.get<LicitacaoDetalhe>(`/administracao/licitacoes/${id}`, { signal });
}

function listarPropostas(id: string, signal?: AbortSignal): Promise<PropostaResumo[]> {
  // Sem endpoint dedicado: as propostas vêm na projeção de detalhe (LicitacaoDetalhe.Propostas).
  return obterLicitacao(id, signal).then((detalhe) => detalhe.propostas);
}

function abrirLicitacao(input: AbrirLicitacaoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/licitacoes', input);
}

function publicarEditalPncp(licitacaoId: string, input: PublicarEditalPncpInput): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/edital-pncp`, input);
}

function julgarPropostas(licitacaoId: string, input: JulgarPropostasInput): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/julgar`, input);
}

function homologarLicitacao(licitacaoId: string): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/homologar`, {});
}

function habilitarLicitante(licitacaoId: string, input: HabilitarLicitanteInput): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/habilitar`, input);
}

function declararFracassada(licitacaoId: string, input: MotivoInput): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/fracassada`, input);
}

function declararDeserta(licitacaoId: string): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/deserta`, {});
}

function revogarLicitacao(licitacaoId: string, input: MotivoInput): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/revogar`, input);
}

function anularLicitacao(licitacaoId: string, input: MotivoInput): Promise<void> {
  return http.post<void>(`/administracao/licitacoes/${licitacaoId}/anular`, input);
}

// ---------------------------------------------------------------------------
// Hooks de consulta (queries)
// ---------------------------------------------------------------------------

/** ListarLicitacoesPorSituacao — lista o certame do tenant na situação informada. */
export function useLicitacoesPorSituacao(situacao: SituacaoLicitacao, enabled = true) {
  return useQuery({
    queryKey: licitacaoKeys.porSituacao(situacao),
    queryFn: ({ signal }) => listarPorSituacao(situacao, signal),
    enabled,
  });
}

/** ObterLicitacaoPorId — detalhe completo (lotes + propostas). */
export function useLicitacao(id: string) {
  return useQuery({
    queryKey: licitacaoKeys.detalhe(id),
    queryFn: ({ signal }) => obterLicitacao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** ListarPropostasDaLicitacao — propostas recebidas no certame. */
export function usePropostasDaLicitacao(id: string, enabled = true) {
  return useQuery({
    queryKey: licitacaoKeys.propostas(id),
    queryFn: ({ signal }) => listarPropostas(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks de comando (mutations)
// ---------------------------------------------------------------------------

/** AbrirLicitacao — cria um novo certame (situação inicial Aberta). */
export function useAbrirLicitacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirLicitacao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: licitacaoKeys.porSituacao('Aberta') });
    },
  });
}

// Invalida o detalhe + propostas + todas as listas por situação (a transição muda a situação).
function invalidarLicitacao(queryClient: ReturnType<typeof useQueryClient>, licitacaoId: string): void {
  queryClient.invalidateQueries({ queryKey: licitacaoKeys.detalhe(licitacaoId) });
  queryClient.invalidateQueries({ queryKey: licitacaoKeys.propostas(licitacaoId) });
  queryClient.invalidateQueries({ queryKey: licitacaoKeys.all });
}

/** PublicarEditalNoPncp — registra o número do edital no PNCP (art. 174). */
export function usePublicarEditalPncp(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PublicarEditalPncpInput) => publicarEditalPncp(licitacaoId, input),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** JulgarPropostas — indica a proposta vencedora (Aberta -> EmJulgamento). */
export function useJulgarPropostas(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: JulgarPropostasInput) => julgarPropostas(licitacaoId, input),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** HomologarLicitacao — ato do ordenador de despesa (EmJulgamento -> Homologada). */
export function useHomologarLicitacao(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => homologarLicitacao(licitacaoId),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** HabilitarLicitante — registra a habilitação/inabilitação de um licitante. */
export function useHabilitarLicitante(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: HabilitarLicitanteInput) => habilitarLicitante(licitacaoId, input),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** DeclararLicitacaoFracassada — encerra por inexistência de proposta válida/habilitada. */
export function useDeclararFracassada(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoInput) => declararFracassada(licitacaoId, input),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** DeclararLicitacaoDeserta — encerra por ausência total de interessados. */
export function useDeclararDeserta(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => declararDeserta(licitacaoId),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** RevogarLicitacao — encerra por conveniência/oportunidade (motivo obrigatório). */
export function useRevogarLicitacao(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoInput) => revogarLicitacao(licitacaoId, input),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}

/** AnularLicitacao — encerra por ilegalidade (motivo obrigatório). */
export function useAnularLicitacao(licitacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoInput) => anularLicitacao(licitacaoId, input),
    onSuccess: () => invalidarLicitacao(queryClient, licitacaoId),
  });
}
