// API do agregado Ordem de Pagamento (3º estágio da despesa, Lei 4.320/64, art. 64).
// Endpoints: POST /ordens-pagamento, POST /ordens-pagamento/{id}/efetuar,
//            POST /ordens-pagamento/{id}/cancelar, GET /ordens-pagamento/{id}.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { financasKeys } from './financas.api';

/** Item da ordem (vínculo a uma liquidação). */
export interface ItemPagamento {
  liquidacaoId: string;
  valor: number;
}

/** Projeção de resumo/detalhe (OrdemDePagamentoResumo). */
export interface OrdemDePagamentoResumo {
  id: string;
  numero: string;
  /** Data ISO yyyy-mm-dd. */
  dataPagamento: string;
  contaBancaria: string;
  valorTotal: number;
  situacao: string;
  itens: ItemPagamento[];
}

/** EmitirOrdemDePagamentoCommand. */
export interface EmitirOrdemInput {
  numero: string;
  /** Data ISO yyyy-mm-dd. */
  dataPagamento: string;
  banco: string;
  agencia: string;
  conta: string;
  pix?: string | null;
  itens: ItemPagamento[];
}

function obterOrdem(id: string, signal?: AbortSignal): Promise<OrdemDePagamentoResumo> {
  return http.get<OrdemDePagamentoResumo>(`/financas/ordens-pagamento/${id}`, { signal });
}

function emitirOrdem(input: EmitirOrdemInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/financas/ordens-pagamento', input);
}

function efetuarPagamento(id: string): Promise<void> {
  return http.post<void>(`/financas/ordens-pagamento/${id}/efetuar`);
}

function cancelarOrdem(id: string): Promise<void> {
  return http.post<void>(`/financas/ordens-pagamento/${id}/cancelar`);
}

/** Detalhe de uma ordem de pagamento pelo identificador. */
export function useOrdemDePagamento(id: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.ordemPagamento(id),
    queryFn: ({ signal }) => obterOrdem(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

/** Emite uma ordem de pagamento. Invalida as listas de pagamento. */
export function useEmitirOrdem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: emitirOrdem,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.pagamentos() });
    },
  });
}

/** Efetua o pagamento (baixa financeira). Invalida o detalhe afetado. */
export function useEfetuarPagamento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => efetuarPagamento(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.ordemPagamento(id) });
    },
  });
}

/** Cancela a ordem de pagamento. Invalida o detalhe afetado. */
export function useCancelarOrdem(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => cancelarOrdem(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.ordemPagamento(id) });
    },
  });
}
