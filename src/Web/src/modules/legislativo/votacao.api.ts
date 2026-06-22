// Camada de API do agregado Votacao (modulo Legislativo). DTOs + acesso HTTP
// tipado + hooks TanStack Query, cobrindo os 7 endpoints de /api/legislativo/votacoes.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

export interface VotacaoDetalhe {
  id: string;
  sessaoId: string;
  proposicaoId: string;
  tipo: string;
  maioriaExigida: string;
  totalMembros: number;
  presentes: number;
  turno: number;
  situacao: string;
  resultado: string | null;
  votosSim: number;
  votosNao: number;
  abstencoes: number;
}

export interface IniciarVotacaoInput {
  sessaoId: string;
  proposicaoId: string;
  tipo: number;
  maioriaExigida: number;
  totalMembros: number;
  presentes: number;
  turno: number;
}

/** Placar agregado da votacao (em tempo real). */
export interface PlacarVotacao {
  votacaoId: string;
  sim: number;
  nao: number;
  abstencao: number;
  totalVotos: number;
  situacao: string;
}

/** Resumo de um voto nominal. */
export interface VotoResumo {
  vereadorId: string;
  sentido: string;
  registradoEm: string;
}

/** Payload de registro de voto nominal. */
export interface RegistrarVotoInput {
  votoId: string;
  vereadorId: string;
  sentido: number;
}

/** Resumo de uma votacao para listagem (por sessao). */
export interface VotacaoResumo {
  id: string;
  proposicaoId: string;
  tipo: string;
  situacao: string;
  resultado: string | null;
}

/** Voto nominal exibido no painel ao vivo (inclui o nome do vereador). */
export interface VotoNominalPainel {
  vereadorId: string;
  /** Nome parlamentar (back: NomeParlamentar). */
  nomeParlamentar: string;
  sentido: string;
}

/**
 * Painel eletronico (PLACAR ao vivo) de uma votacao: totais Sim/Nao/Abstencao,
 * presenca, quorum e o resultado parcial, com a lista nominal dos votos.
 */
export interface PainelVotacao {
  votacaoId: string;
  sim: number;
  nao: number;
  abstencao: number;
  totalVotos: number;
  presentes: number;
  ausentes: number;
  quorumMinimo: number;
  quorumAtingido: boolean;
  resultadoParcial: string;
  situacao: string;
  votos: VotoNominalPainel[];
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterVotacao(id: string, signal?: AbortSignal): Promise<VotacaoDetalhe> {
  return http.get<VotacaoDetalhe>(`/legislativo/votacoes/${id}`, { signal });
}

function obterPlacar(id: string, signal?: AbortSignal): Promise<PlacarVotacao> {
  return http.get<PlacarVotacao>(`/legislativo/votacoes/${id}/placar`, { signal });
}

function listarVotacoesPorSessao(sessaoId: string, signal?: AbortSignal): Promise<VotacaoResumo[]> {
  return http.get<VotacaoResumo[]>('/legislativo/votacoes', { query: { sessaoId }, signal });
}

function obterPainel(id: string, signal?: AbortSignal): Promise<PainelVotacao> {
  return http.get<PainelVotacao>(`/legislativo/votacoes/${id}/painel`, { signal });
}

function listarVotosNominais(id: string, signal?: AbortSignal): Promise<VotoResumo[]> {
  return http.get<VotoResumo[]>(`/legislativo/votacoes/${id}/votos-nominais`, { signal });
}

async function iniciarVotacao(input: IniciarVotacaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/votacoes', input);
  return id;
}

function registrarVoto(votacaoId: string, input: RegistrarVotoInput): Promise<void> {
  return http.post<void>(`/legislativo/votacoes/${votacaoId}/votos`, input);
}

async function encerrarVotacao(votacaoId: string): Promise<string> {
  const r = await http.post<{ resultado: string }>(`/legislativo/votacoes/${votacaoId}/encerramento`);
  return r.resultado;
}

function cancelarVotacao(votacaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/votacoes/${votacaoId}/cancelamento`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Detalhe de uma votacao (placar agregado + resultado). */
export function useVotacao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.votacao(id),
    queryFn: ({ signal }) => obterVotacao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Placar agregado da votacao (Sim/Nao/Abstencao em tempo real). */
export function usePlacarVotacao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.votacaoPlacar(id),
    queryFn: ({ signal }) => obterPlacar(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Lista as votacoes de uma sessao (abertas costumam vir primeiro do backend). */
export function useVotacoesPorSessao(sessaoId: string) {
  return useQuery({
    queryKey: legislativoKeys.votacoesPorSessao(sessaoId),
    queryFn: ({ signal }) => listarVotacoesPorSessao(sessaoId, signal),
    enabled: sessaoId.trim().length > 0,
  });
}

/**
 * Painel eletronico (PLACAR ao vivo) de uma votacao. Faz auto-refresh por
 * polling (refetchInterval) ENQUANTO a votacao estiver Aberta, dando a sensacao
 * de tempo real; para de pollar assim que ela e Encerrada/Cancelada.
 */
export function usePainelVotacao(id: string, intervaloMs = 3000) {
  return useQuery({
    queryKey: legislativoKeys.votacaoPainel(id),
    queryFn: ({ signal }) => obterPainel(id, signal),
    enabled: id.trim().length > 0,
    refetchInterval: (query) =>
      query.state.data?.situacao === 'Aberta' ? intervaloMs : false,
  });
}

/** Votos nominais da votacao (oculto em votacao secreta — backend nega). */
export function useVotosNominais(id: string) {
  return useQuery({
    queryKey: legislativoKeys.votacaoVotosNominais(id),
    queryFn: ({ signal }) => listarVotosNominais(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Inicia (abre) uma votacao e invalida as listas afetadas. */
export function useIniciarVotacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: iniciarVotacao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.votacoes() });
    },
  });
}

/** Invalida o detalhe/placar/votos de uma votacao. */
function useInvalidarVotacao(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.votacao(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.votacaoPlacar(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.votacaoVotosNominais(id) });
  };
}

/** Registra um voto nominal na votacao. */
export function useRegistrarVoto(id: string) {
  const invalidar = useInvalidarVotacao(id);
  return useMutation({
    mutationFn: (input: RegistrarVotoInput) => registrarVoto(id, input),
    onSuccess: invalidar,
  });
}

/** Encerra a votacao e devolve o resultado apurado. */
export function useEncerrarVotacao(id: string) {
  const invalidar = useInvalidarVotacao(id);
  return useMutation({ mutationFn: () => encerrarVotacao(id), onSuccess: invalidar });
}

/** Cancela a votacao. */
export function useCancelarVotacao(id: string) {
  const invalidar = useInvalidarVotacao(id);
  return useMutation({ mutationFn: () => cancelarVotacao(id), onSuccess: invalidar });
}
