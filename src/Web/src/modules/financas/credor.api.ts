// API do agregado Credor (fornecedor/credor da despesa) e do Extrato do Credor — razão do credor:
// empenhos/liquidacoes/pagamentos + retencoes por natureza + saldo a pagar (#56).
// Endpoints: GET /credores?termo=, GET /credores/{id}, GET /credores/{id}/extrato?exercicio=.
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';
import { financasKeys } from './financas.api';

/** CredorResumo (cabeçalho do credor). `tipo`/`situacao` são strings do enum. */
export interface CredorResumo {
  id: string;
  nome: string;
  tipo: string;
  documento: string;
  banco: string | null;
  agencia: string | null;
  conta: string | null;
  pix: string | null;
  situacao: string;
}

/** CredorExtratoItem — um empenho do credor com execução acumulada. */
export interface CredorExtratoItem {
  empenhoId: string;
  numero: string;
  exercicio: number;
  dataEmpenho: string;
  situacao: string;
  valorEmpenhado: number;
  valorAnulado: number;
  valorLiquidado: number;
  valorPago: number;
}

/** CredorRetencaoResumo — retenções do credor agrupadas por natureza. */
export interface CredorRetencaoResumo {
  natureza: string;
  valorRetido: number;
  valorRecolhido: number;
  valorAReter: number;
}

/** CredorExtrato — razão consolidado do credor. */
export interface CredorExtrato {
  credor: CredorResumo;
  itens: CredorExtratoItem[];
  retencoes: CredorRetencaoResumo[];
  totalEmpenhado: number;
  totalLiquidado: number;
  totalPago: number;
  saldoAPagar: number;
  totalRetido: number;
  totalRetidoAReter: number;
}

function listarCredores(termo: string, signal?: AbortSignal): Promise<CredorResumo[]> {
  const query = termo.trim().length > 0 ? `?termo=${encodeURIComponent(termo.trim())}` : '';
  return http.get<CredorResumo[]>(`/financas/credores${query}`, { signal });
}

function obterCredor(id: string, signal?: AbortSignal): Promise<CredorResumo> {
  return http.get<CredorResumo>(`/financas/credores/${id}`, { signal });
}

function obterExtratoCredor(
  id: string,
  exercicio: number | null,
  signal?: AbortSignal,
): Promise<CredorExtrato> {
  const query = exercicio != null ? `?exercicio=${exercicio}` : '';
  return http.get<CredorExtrato>(`/financas/credores/${id}/extrato${query}`, { signal });
}

/** Lista/busca credores do tenant (termo opcional por nome/documento). */
export function useCredores(termo: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.credoresPorTermo(termo),
    queryFn: ({ signal }) => listarCredores(termo, signal),
    enabled,
  });
}

/** Detalhe de um credor pelo identificador. */
export function useCredor(id: string, enabled = true) {
  return useQuery({
    queryKey: financasKeys.credor(id),
    queryFn: ({ signal }) => obterCredor(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

/** Extrato consolidado (razão) do credor; filtro opcional por exercício. */
export function useCredorExtrato(id: string, exercicio: number | null, enabled = true) {
  return useQuery({
    queryKey: financasKeys.credorExtrato(id, exercicio),
    queryFn: ({ signal }) => obterExtratoCredor(id, exercicio, signal),
    enabled: enabled && id.trim().length > 0,
  });
}
