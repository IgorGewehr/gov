// API do agregado Turma (módulo Educação). DTOs + acesso HTTP + hooks TanStack
// Query. Espelha os endpoints REAIS sob /api/educacao (EducacaoEndpoints.cs):
//   GET    /educacao/turmas?escolaId=&anoLetivo=&turno=&etapa=&situacao=&pagina=&tamanho=
//                                                  -> BuscarTurmas  -> ResultadoPaginado<TurmaItemLista>
//   GET    /educacao/turmas/{turmaId}              -> ObterTurma    -> TurmaFicha | null
//   POST   /educacao/turmas                        -> CriarTurma    -> { id }
//   POST   /educacao/turmas/{turmaId}/abertura     -> AbrirTurma    -> 204
//   POST   /educacao/turmas/{turmaId}/encerramento -> EncerrarTurma -> 204
//   PUT    /educacao/turmas/{turmaId}/vagas        -> AjustarVagas  -> 204
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';
import type { ResultadoPaginado } from './aluno.api';

/** Etapa/modalidade de ensino (Domain.Turmas.Etapa) — valor numérico (IsInEnum). */
export type Etapa = 1 | 2 | 3 | 4 | 5 | 6;

/** Turno de funcionamento (Domain.Turmas.Turno) — valor numérico (IsInEnum). */
export type Turno = 1 | 2 | 3 | 4;

/** Situação da turma (Domain.Turmas.SituacaoTurma). */
export type SituacaoTurma = 'Planejada' | 'Aberta' | 'Encerrada';

/** Valor numérico do enum SituacaoTurma (filtro da busca espera o número). */
export const SITUACAO_TURMA_VALOR: Record<SituacaoTurma, number> = {
  Planejada: 1,
  Aberta: 2,
  Encerrada: 3,
};

/**
 * Item da lista/busca de turmas (TurmaItemLista) — picker do front COM vagas
 * disponíveis (vagasDisponiveis), para a matrícula deixar de digitar GUID.
 */
export interface TurmaItemLista {
  id: string;
  escolaId: string;
  anoLetivo: number;
  etapa: string;
  serie: string;
  turno: string;
  vagas: number;
  matriculados: number;
  vagasDisponiveis: number;
  situacao: string;
}

/** Ficha da turma (TurmaFicha) — mesmos campos do item de lista (sem matriculados detalhados). */
export type TurmaFicha = TurmaItemLista;

/** Filtros da busca de turmas (todos opcionais salvo a paginação; 1-based). */
export interface TurmaBuscaFiltro {
  escolaId?: string;
  anoLetivo?: number;
  /** Valor numérico do enum Turno. */
  turno?: number;
  /** Valor numérico do enum Etapa. */
  etapa?: number;
  /** Valor numérico do enum SituacaoTurma. */
  situacao?: number;
  pagina: number;
  tamanho: number;
}

/** Corpo de POST /turmas (CriarTurmaCommand). */
export interface CriarTurmaInput {
  escolaId: string;
  anoLetivo: number;
  etapa: Etapa;
  serie: string;
  turno: Turno;
  vagas: number;
}

/** Corpo de PUT /turmas/{turmaId}/vagas (AjustarVagasPayload). */
export interface AjustarVagasInput {
  vagas: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarTurmas(
  filtro: TurmaBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<TurmaItemLista>> {
  return http.get<ResultadoPaginado<TurmaItemLista>>('/educacao/turmas', {
    query: {
      escolaId: filtro.escolaId,
      anoLetivo: filtro.anoLetivo,
      turno: filtro.turno,
      etapa: filtro.etapa,
      situacao: filtro.situacao,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterTurma(turmaId: string, signal?: AbortSignal): Promise<TurmaFicha | null> {
  return http.get<TurmaFicha | null>(`/educacao/turmas/${turmaId}`, { signal });
}

function criarTurma(input: CriarTurmaInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/turmas', input);
}

function abrirTurma(turmaId: string): Promise<void> {
  return http.post<void>(`/educacao/turmas/${turmaId}/abertura`);
}

function encerrarTurma(turmaId: string, encerramentoAnoLetivo: boolean): Promise<void> {
  return http.post<void>(`/educacao/turmas/${turmaId}/encerramento`, { encerramentoAnoLetivo });
}

function ajustarVagas(turmaId: string, input: AjustarVagasInput): Promise<void> {
  return http.put<void>(`/educacao/turmas/${turmaId}/vagas`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista/busca paginada de turmas (picker da matrícula, com vagas disponíveis). */
export function useBuscarTurmas(filtro: TurmaBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.turmasBusca(filtro),
    queryFn: ({ signal }) => buscarTurmas(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Obtém a ficha de uma turma. `enabled` controla disparo sob demanda. */
export function useTurma(turmaId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.turmaPorId(turmaId),
    queryFn: ({ signal }) => obterTurma(turmaId, signal),
    enabled: enabled && turmaId.trim().length > 0,
  });
}

/** Cria uma turma (situação inicial Planejada) e invalida as buscas de turmas. */
export function useCriarTurma() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarTurma,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.turmas() });
    },
  });
}

/** Abre uma turma (habilita a enturmação/matrícula). Invalida as buscas de turmas. */
export function useAbrirTurma() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (turmaId: string) => abrirTurma(turmaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.turmas() });
    },
  });
}

/** Encerra uma turma (terminal). Invalida as buscas de turmas. */
export function useEncerrarTurma() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ turmaId, encerramentoAnoLetivo }: { turmaId: string; encerramentoAnoLetivo: boolean }) =>
      encerrarTurma(turmaId, encerramentoAnoLetivo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.turmas() });
    },
  });
}

/** Ajusta a capacidade de vagas de uma turma. Invalida as buscas de turmas. */
export function useAjustarVagas() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ turmaId, input }: { turmaId: string; input: AjustarVagasInput }) =>
      ajustarVagas(turmaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.turmas() });
    },
  });
}
