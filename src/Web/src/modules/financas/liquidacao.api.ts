// API do agregado Liquidação (2º estágio da despesa, Lei 4.320/64, art. 63).
// Endpoints: POST /liquidacoes, POST /liquidacoes/{id}/estornar, GET /liquidacoes/{id},
//            GET /empenhos/{empenhoId}/liquidacoes.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { financasKeys } from './financas.api';

/** Retenção apurada sobre a liquidação (RetencaoResumo). */
export interface RetencaoResumo {
  id: string;
  natureza: string;
  valor: number;
  codigoReceita: string | null;
  recolhida: boolean;
}

/** Projeção de resumo/detalhe (LiquidacaoResumo). */
export interface LiquidacaoResumo {
  id: string;
  empenhoId: string;
  valor: number;
  valorPago: number;
  saldoAPagar: number;
  totalRetido: number;
  valorLiquido: number;
  /** Data ISO yyyy-mm-dd. */
  dataLiquidacao: string;
  documento: string;
  situacao: string;
  retencoes: RetencaoResumo[];
}

/** LiquidarDespesaCommand. `tipoDocumento` é valor numérico do enum. */
export interface LiquidarDespesaInput {
  empenhoId: string;
  valor: number;
  /** Data ISO yyyy-mm-dd. */
  dataLiquidacao: string;
  tipoDocumento: number;
  numeroDocumento?: string | null;
  chaveNfse?: string | null;
  dataEmissaoDocumento?: string | null;
}

function obterLiquidacao(id: string, signal?: AbortSignal): Promise<LiquidacaoResumo> {
  return http.get<LiquidacaoResumo>(`/financas/liquidacoes/${id}`, { signal });
}

function listarLiquidacoesPorEmpenho(empenhoId: string, signal?: AbortSignal): Promise<LiquidacaoResumo[]> {
  return http.get<LiquidacaoResumo[]>(`/financas/empenhos/${empenhoId}/liquidacoes`, { signal });
}

function liquidarDespesa(input: LiquidarDespesaInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/financas/liquidacoes', input);
}

function estornarLiquidacao(id: string): Promise<void> {
  return http.post<void>(`/financas/liquidacoes/${id}/estornar`);
}

/** Detalhe de uma liquidação pelo identificador. */
export function useLiquidacao(id: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.liquidacao(id),
    queryFn: ({ signal }) => obterLiquidacao(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

/** Lista as liquidações de um empenho. */
export function useLiquidacoesPorEmpenho(empenhoId: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.liquidacoesPorEmpenho(empenhoId),
    queryFn: ({ signal }) => listarLiquidacoesPorEmpenho(empenhoId, signal),
    enabled: enabled && empenhoId.trim().length > 0,
  });
}

/** Liquida a despesa (2º estágio). Invalida liquidações e o empenho de origem. */
export function useLiquidarDespesa() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: liquidarDespesa,
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: financasKeys.liquidacoes() });
      queryClient.invalidateQueries({ queryKey: financasKeys.empenho(input.empenhoId) });
    },
  });
}

/** Estorna uma liquidação. Invalida o detalhe e as listas. */
export function useEstornarLiquidacao(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => estornarLiquidacao(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financasKeys.liquidacao(id) });
      queryClient.invalidateQueries({ queryKey: financasKeys.liquidacoes() });
    },
  });
}
