// API de Retenções/Consignações (IRRF IN RFB 1234/2012, INSS, ISS, caução) e recolhimento
// extra-orçamentário (guias). Endpoints sob /financas/retencoes/* e /financas/liquidacoes/{id}/retencoes.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';

/** Natureza da retenção (espelha o enum NaturezaRetencao do backend). */
export const NaturezaRetencao = {
  IrrfPessoaJuridica: 1,
  IrrfPessoaFisica: 2,
  InssRetido: 3,
  IssRetido: 4,
  ContribuicoesFederais: 5,
  CaucaoGarantia: 6,
  Outras: 99,
} as const;
export type NaturezaRetencaoValor = (typeof NaturezaRetencao)[keyof typeof NaturezaRetencao];

export const NATUREZA_RETENCAO_LABEL: Record<number, string> = {
  1: 'IRRF — Pessoa Jurídica',
  2: 'IRRF — Pessoa Física',
  3: 'INSS retido',
  4: 'ISS retido',
  5: 'Contribuições federais (CSLL/COFINS/PIS)',
  6: 'Caução/garantia',
  99: 'Outras consignações',
};

/** Situação da guia (espelha SituacaoGuiaRecolhimento). */
export const SITUACAO_GUIA_LABEL: Record<number, string> = {
  1: 'Emitida',
  2: 'Recolhida',
  3: 'Cancelada',
};

export interface FaixaIrrf {
  codigo: string;
  descricao: string;
  aliquota: number;
  codigoReceitaDarf: string;
}

export interface TabelaIrrf {
  vigenciaInicio: string;
  vigenciaFim: string | null;
  valorMinimoRetencao: number;
  faixas: FaixaIrrf[];
}

export interface GuiaRecolhimento {
  guiaRecolhimentoId: string;
  natureza: number;
  codigoReceita: string | null;
  valorTotal: number;
  competencia: string;
  dataVencimento: string;
  situacao: number;
  dataRecolhimento: string | null;
  quantidadeItens: number;
}

export interface AdicionarRetencaoInput {
  natureza: number;
  enquadramentoIrrf?: string | null;
  baseCalculo?: number | null;
  valorInformado?: number | null;
  aliquotaInformada?: number | null;
  codigoReceita?: string | null;
  favorecidoDocumento?: string | null;
  descricao?: string | null;
}

export const retencoesKeys = {
  tabelaIrrf: (data?: string) => ['financas', 'retencoes', 'tabela-irrf', data ?? 'hoje'] as const,
  guias: (situacao?: number) => ['financas', 'retencoes', 'guias', situacao ?? 'todas'] as const,
};

function obterTabelaIrrf(data?: string, signal?: AbortSignal): Promise<TabelaIrrf | null> {
  return http.get<TabelaIrrf | null>('/financas/retencoes/tabela-irrf', { query: { data }, signal });
}

function listarGuias(situacao?: number, signal?: AbortSignal): Promise<GuiaRecolhimento[]> {
  return http.get<GuiaRecolhimento[]>('/financas/retencoes/guias', { query: { situacao }, signal });
}

/** Consulta a tabela de IRRF/PJ vigente (IN RFB 1234/2012). */
export function useTabelaIrrf(data?: string) {
  return useQuery({
    queryKey: retencoesKeys.tabelaIrrf(data),
    queryFn: ({ signal }) => obterTabelaIrrf(data, signal),
  });
}

/** Lista guias de recolhimento (filtro opcional por situação). */
export function useGuiasRecolhimento(situacao?: number) {
  return useQuery({
    queryKey: retencoesKeys.guias(situacao),
    queryFn: ({ signal }) => listarGuias(situacao, signal),
  });
}

/** Semeia a tabela de IRRF/PJ vigente para o tenant. */
export function useSemearTabelaIrrf() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => http.post<{ faixas: number }>('/financas/retencoes/tabela-irrf/semear'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['financas', 'retencoes', 'tabela-irrf'] }),
  });
}

/** Adiciona uma retenção a uma liquidação. */
export function useAdicionarRetencao(liquidacaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarRetencaoInput) =>
      http.post<{ id: string }>(`/financas/liquidacoes/${liquidacaoId}/retencoes`, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['financas', 'liquidacao', liquidacaoId] });
    },
  });
}

/** Registra o recolhimento de uma guia. */
export function useRecolherGuia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { guiaId: string; dataRecolhimento: string }) =>
      http.post<void>(`/financas/retencoes/guias/${payload.guiaId}/recolher`, {
        dataRecolhimento: payload.dataRecolhimento,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['financas', 'retencoes', 'guias'] }),
  });
}
