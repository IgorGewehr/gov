// Camada de API do agregado Proposicao (modulo Legislativo). DTOs + acesso HTTP
// tipado + hooks TanStack Query, cobrindo os 12 endpoints de /api/legislativo/proposicoes.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

export interface ProposicaoResumo {
  id: string;
  tipo: string;
  ementa: string;
  situacao: string;
  dataApresentacao: string;
}

export interface TramitacaoResumo {
  id: string;
  fase: string;
  comissao: string | null;
  parecerFavoravel: boolean | null;
  data: string;
}

export interface ProposicaoDetalhe {
  id: string;
  tipo: string;
  ementa: string;
  autoria: string;
  regime: string;
  protocolo: string;
  dataApresentacao: string;
  situacao: string;
  numeroAutografo: string | null;
  tramitacoes: TramitacaoResumo[];
}

export interface ApresentarProposicaoInput {
  tipo: number;
  ementa: string;
  autoria: string;
  regime: number;
}

/** Payload de apresentacao de emenda a uma proposicao. */
export interface ApresentarEmendaInput {
  texto: string;
  autoria: string;
}

/** Payload de registro de parecer de comissao. */
export interface RegistrarParecerInput {
  comissao: string;
  favoravel: boolean;
}

/** Payload de deliberacao (aprovacao/rejeicao) vinculada a uma votacao. */
export interface DeliberacaoInput {
  votacaoId: string;
}

/** Payload de geracao de autografo. */
export interface GerarAutografoInput {
  numeroAutografo: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarProposicoesPorSituacao(situacao: number, signal?: AbortSignal): Promise<ProposicaoResumo[]> {
  return http.get<ProposicaoResumo[]>('/legislativo/proposicoes', { query: { situacao }, signal });
}

function obterProposicao(id: string, signal?: AbortSignal): Promise<ProposicaoDetalhe> {
  return http.get<ProposicaoDetalhe>(`/legislativo/proposicoes/${id}`, { signal });
}

function obterTramitacao(id: string, signal?: AbortSignal): Promise<TramitacaoResumo[]> {
  return http.get<TramitacaoResumo[]>(`/legislativo/proposicoes/${id}/tramitacao`, { signal });
}

async function apresentarProposicao(input: ApresentarProposicaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/proposicoes', input);
  return id;
}

function distribuirProposicao(proposicaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/distribuicao`);
}

async function apresentarEmenda(proposicaoId: string, input: ApresentarEmendaInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>(`/legislativo/proposicoes/${proposicaoId}/emendas`, input);
  return id;
}

function registrarParecer(proposicaoId: string, input: RegistrarParecerInput): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/pareceres`, input);
}

function incluirProposicaoEmOrdemDoDia(proposicaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/ordem-do-dia`);
}

function aprovarProposicao(proposicaoId: string, input: DeliberacaoInput): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/aprovacao`, input);
}

function rejeitarProposicao(proposicaoId: string, input: DeliberacaoInput): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/rejeicao`, input);
}

function gerarAutografo(proposicaoId: string, input: GerarAutografoInput): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/autografo`, input);
}

function arquivarProposicao(proposicaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/proposicoes/${proposicaoId}/arquivamento`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as proposicoes do tenant em uma situacao. */
export function useProposicoesPorSituacao(situacao: number) {
  return useQuery({
    queryKey: legislativoKeys.proposicoesPorSituacao(situacao),
    queryFn: ({ signal }) => listarProposicoesPorSituacao(situacao, signal),
  });
}

/** Detalhe de uma proposicao (inclui a trilha de tramitacao). */
export function useProposicao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.proposicao(id),
    queryFn: ({ signal }) => obterProposicao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Trilha imutavel de tramitacao de uma proposicao (transparencia LAI). */
export function useTramitacaoProposicao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.proposicaoTramitacao(id),
    queryFn: ({ signal }) => obterTramitacao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Apresenta (protocola) uma nova proposicao e invalida as listas afetadas. */
export function useApresentarProposicao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: apresentarProposicao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.proposicoes() });
    },
  });
}

/** Invalida o detalhe/tramitacao de uma proposicao e todas as listas. */
function useInvalidarProposicao(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.proposicao(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.proposicaoTramitacao(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.proposicoes() });
  };
}

/** Distribui a proposicao as comissoes (transicao Apresentada -> Distribuida). */
export function useDistribuirProposicao(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({ mutationFn: () => distribuirProposicao(id), onSuccess: invalidar });
}

/** Apresenta uma emenda a proposicao. */
export function useApresentarEmenda(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({
    mutationFn: (input: ApresentarEmendaInput) => apresentarEmenda(id, input),
    onSuccess: invalidar,
  });
}

/** Registra um parecer de comissao na proposicao. */
export function useRegistrarParecer(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({
    mutationFn: (input: RegistrarParecerInput) => registrarParecer(id, input),
    onSuccess: invalidar,
  });
}

/** Inclui a proposicao em Ordem do Dia. */
export function useIncluirProposicaoEmOrdemDoDia(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({ mutationFn: () => incluirProposicaoEmOrdemDoDia(id), onSuccess: invalidar });
}

/** Aprova a proposicao com base em uma votacao. */
export function useAprovarProposicao(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({
    mutationFn: (input: DeliberacaoInput) => aprovarProposicao(id, input),
    onSuccess: invalidar,
  });
}

/** Rejeita a proposicao com base em uma votacao. */
export function useRejeitarProposicao(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({
    mutationFn: (input: DeliberacaoInput) => rejeitarProposicao(id, input),
    onSuccess: invalidar,
  });
}

/** Gera o autografo da proposicao aprovada. */
export function useGerarAutografo(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({
    mutationFn: (input: GerarAutografoInput) => gerarAutografo(id, input),
    onSuccess: invalidar,
  });
}

/** Arquiva a proposicao. */
export function useArquivarProposicao(id: string) {
  const invalidar = useInvalidarProposicao(id);
  return useMutation({ mutationFn: () => arquivarProposicao(id), onSuccess: invalidar });
}
