// API do agregado Restos a Pagar (Lei 4.320/64, art. 36 — despesas empenhadas e não
// pagas no encerramento do exercício). Endpoints: POST /restos-a-pagar/encerrar-exercicio,
// POST /restos-a-pagar/{id}/pagar, POST /restos-a-pagar/{id}/cancelar,
// GET /restos-a-pagar?exercicioInscricao=.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { financasKeys } from './financas.api';

/** Projeção de resumo/detalhe (RestoAPagarResumo). */
export interface RestoAPagarResumo {
  id: string;
  empenhoId: string;
  classificacao: string;
  valorInscrito: number;
  valorLiquidado: number;
  valorPago: number;
  valorCancelado: number;
  saldoAPagar: number;
  exercicioOrigem: number;
  exercicioInscricao: number;
  situacao: string;
}

function listarRestos(exercicioInscricao: number, signal?: AbortSignal): Promise<RestoAPagarResumo[]> {
  return http.get<RestoAPagarResumo[]>('/financas/restos-a-pagar', {
    query: { exercicioInscricao },
    signal,
  });
}

function encerrarExercicio(exercicio: number): Promise<{ inscritos: number }> {
  return http.post<{ inscritos: number }>('/financas/restos-a-pagar/encerrar-exercicio', {
    exercicio,
  });
}

function pagarResto(id: string, valor: number): Promise<void> {
  return http.post<void>(`/financas/restos-a-pagar/${id}/pagar`, { valor });
}

function cancelarResto(id: string, valor: number): Promise<void> {
  return http.post<void>(`/financas/restos-a-pagar/${id}/cancelar`, { valor });
}

/** Lista os restos a pagar inscritos em um exercício (consulta sob demanda). */
export function useRestosPorExercicio(exercicioInscricao: number, enabled = true) {
  return useQuery({
    queryKey: financasKeys.restosPorExercicio(exercicioInscricao),
    queryFn: ({ signal }) => listarRestos(exercicioInscricao, signal),
    enabled: enabled && Number.isInteger(exercicioInscricao) && exercicioInscricao >= 2000,
  });
}

/** Encerra o exercício, inscrevendo restos a pagar. Invalida as listas. */
export function useEncerrarExercicio() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: encerrarExercicio,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.restos() });
    },
  });
}

/** Paga (parcialmente) um resto a pagar inscrito. Invalida as listas. */
export function usePagarResto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, valor }: { id: string; valor: number }) => pagarResto(id, valor),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.restos() });
    },
  });
}

/** Cancela (parcialmente) um resto a pagar inscrito. Invalida as listas. */
export function useCancelarResto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, valor }: { id: string; valor: number }) => cancelarResto(id, valor),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.restos() });
    },
  });
}
