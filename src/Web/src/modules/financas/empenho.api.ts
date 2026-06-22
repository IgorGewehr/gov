// API do agregado Empenho (1º estágio da despesa, Lei 4.320/64, art. 58-60).
// Endpoints: POST /empenhos, POST /empenhos/{id}/anular, GET /empenhos/{id},
//            GET /dotacoes/{dotacaoId}/empenhos.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { financasKeys } from './financas.api';

/** Projeção de resumo/detalhe (EmpenhoResumo). `situacao` é string do enum. */
export interface EmpenhoResumo {
  id: string;
  numero: string;
  dotacaoId: string;
  credorNome: string;
  credorDocumento: string;
  valorEmpenhado: number;
  valorAnulado: number;
  valorLiquidado: number;
  valorPago: number;
  saldoEmpenhado: number;
  saldoALiquidar: number;
  saldoAPagar: number;
  situacao: string;
  exercicio: number;
}

/** EmpenharCommand. Enums (`tipoEmpenho`, `credorTipo`) como valor numérico. */
export interface EmpenharInput {
  numero: string;
  dotacaoId: string;
  tipoEmpenho: number;
  exercicio: number;
  /** Data ISO yyyy-mm-dd (DateOnly no backend). */
  dataEmpenho: string;
  credorNome: string;
  credorTipo: number;
  credorDocumento: string;
  valor: number;
}

function obterEmpenho(id: string, signal?: AbortSignal): Promise<EmpenhoResumo> {
  return http.get<EmpenhoResumo>(`/financas/empenhos/${id}`, { signal });
}

function listarEmpenhosPorDotacao(dotacaoId: string, signal?: AbortSignal): Promise<EmpenhoResumo[]> {
  return http.get<EmpenhoResumo[]>(`/financas/dotacoes/${dotacaoId}/empenhos`, { signal });
}

function empenhar(input: EmpenharInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/financas/empenhos', input);
}

/** Anula (total ou parcial) o empenho. `valor` ausente => anulação total. */
function anularEmpenho(id: string, valor?: number): Promise<void> {
  return http.post<void>(`/financas/empenhos/${id}/anular`, { valor: valor ?? null });
}

/** Detalhe de um empenho pelo identificador. */
export function useEmpenho(id: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.empenho(id),
    queryFn: ({ signal }) => obterEmpenho(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

/** Lista os empenhos de uma dotação. */
export function useEmpenhosPorDotacao(dotacaoId: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.empenhosPorDotacao(dotacaoId),
    queryFn: ({ signal }) => listarEmpenhosPorDotacao(dotacaoId, signal),
    enabled: enabled && dotacaoId.trim().length > 0,
  });
}

/** Emite um novo empenho. Invalida as listas de empenho e a dotação afetada. */
export function useEmpenhar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: empenhar,
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: financasKeys.empenhos() });
      queryClient.invalidateQueries({ queryKey: financasKeys.dotacao(input.dotacaoId) });
    },
  });
}

/** Anula o empenho (total/parcial). Invalida o detalhe afetado. */
export function useAnularEmpenho(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (valor?: number) => anularEmpenho(id, valor),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.empenho(id) });
      queryClient.invalidateQueries({ queryKey: financasKeys.empenhos() });
    },
  });
}
