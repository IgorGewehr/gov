// API do agregado Dotação Orçamentária (crédito orçamentário, Lei 4.320/64).
// Endpoints: POST /dotacoes, POST /dotacoes/{id}/reforcar, POST /dotacoes/{id}/anular-credito,
//            GET /dotacoes/{id}, GET /dotacoes?exercicio=.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { financasKeys } from './financas.api';

/** Projeção de resumo/detalhe (DotacaoResumo). */
export interface DotacaoResumo {
  id: string;
  exercicio: number;
  classificacao: string;
  valorDotadoInicial: number;
  valorReforcado: number;
  valorAnulado: number;
  valorAtualizado: number;
  valorEmpenhadoLiquido: number;
  saldoDisponivel: number;
  situacao: string;
}

/** CriarDotacaoCommand. `categoriaEconomica` é o valor numérico do enum. */
export interface CriarDotacaoInput {
  exercicio: number;
  orgao: string;
  unidade: string;
  funcionalProgramatica: string;
  categoriaEconomica: number;
  fonteRecurso: string;
  valorDotado: number;
}

function listarDotacoes(exercicio: number, signal?: AbortSignal): Promise<DotacaoResumo[]> {
  return http.get<DotacaoResumo[]>('/financas/dotacoes', { query: { exercicio }, signal });
}

function obterDotacao(id: string, signal?: AbortSignal): Promise<DotacaoResumo> {
  return http.get<DotacaoResumo>(`/financas/dotacoes/${id}`, { signal });
}

function criarDotacao(input: CriarDotacaoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/financas/dotacoes', input);
}

function reforcarDotacao(id: string, valor: number): Promise<void> {
  return http.post<void>(`/financas/dotacoes/${id}/reforcar`, { valor });
}

function anularCreditoDotacao(id: string, valor: number): Promise<void> {
  return http.post<void>(`/financas/dotacoes/${id}/anular-credito`, { valor });
}

/** Lista as dotações de um exercício (consulta sob demanda via `enabled`). */
export function useDotacoesPorExercicio(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: financasKeys.dotacoesPorExercicio(exercicio),
    queryFn: ({ signal }) => listarDotacoes(exercicio, signal),
    enabled: enabled && Number.isInteger(exercicio) && exercicio >= 2000,
  });
}

/** Detalhe de uma dotação pelo identificador. */
export function useDotacao(id: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.dotacao(id),
    queryFn: ({ signal }) => obterDotacao(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

/** Cria uma nova dotação e invalida as listas de dotação. */
export function useCriarDotacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarDotacao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.dotacoes() });
    },
  });
}

/** Reforça (suplementa) o crédito de uma dotação. Invalida detalhe e listas. */
export function useReforcarDotacao(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (valor: number) => reforcarDotacao(id, valor),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.dotacao(id) });
      queryClient.invalidateQueries({ queryKey: financasKeys.dotacoes() });
    },
  });
}

/** Anula crédito de uma dotação. Invalida detalhe e listas. */
export function useAnularCreditoDotacao(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (valor: number) => anularCreditoDotacao(id, valor),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.dotacao(id) });
      queryClient.invalidateQueries({ queryKey: financasKeys.dotacoes() });
    },
  });
}
