// Camada de API da entidade FolhaDePagamento (módulo RecursosHumanos). Segue o
// PADRÃO-OURO: DTOs no topo, funções de acesso via http client tipado, hooks Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situação (estado) da folha na competência. */
export type SituacaoFolha = 'Aberta' | 'Calculada' | 'Fechada' | 'Paga';

/** Natureza de um evento (verba) da folha. */
export type TipoEvento = 'Provento' | 'Desconto';

/** Resumo de uma folha de pagamento por competência. */
export interface FolhaResumo {
  id: string;
  competencia: string;
  situacao: string;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
  dataFechamento: string | null;
}

/** Linha (rubrica) de um contracheque. */
export interface LinhaContracheque {
  rubrica: string;
  tipo: string;
  valor: number;
}

/** Contracheque de um servidor numa folha. */
export interface Contracheque {
  servidorId: string;
  competencia: string;
  linhas: LinhaContracheque[];
  totalProventos: number;
  totalDescontos: number;
  liquidoAPagar: number;
}

/** Entrada da abertura de folha para uma competência. */
export interface AbrirFolhaInput {
  ano: number;
  mes: number;
}

/** Entrada do lançamento de um evento (provento/desconto) numa folha. */
export interface AdicionarEventoInput {
  servidorId: string;
  rubrica: string;
  /** 1 = Provento, 2 = Desconto (enum numérico do backend). */
  tipo: number;
  baseCalculo: number;
  valor: number;
}

/** Entrada da efetivação do pagamento de uma folha fechada. */
export interface PagamentoInput {
  dataPagamento: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterFolhaPorCompetencia(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<FolhaResumo | null> {
  return http.get<FolhaResumo | null>('/recursoshumanos/folhas/por-competencia', {
    signal,
    query: { ano, mes },
  });
}

function abrirFolha(input: AbrirFolhaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/folhas', input);
}

function adicionarEvento(folhaId: string, input: AdicionarEventoInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/folhas/${folhaId}/eventos`, input);
}

function calcularFolha(folhaId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/folhas/${folhaId}/calculo`);
}

function fecharFolha(folhaId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/folhas/${folhaId}/fechamento`);
}

function efetuarPagamento(folhaId: string, input: PagamentoInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/folhas/${folhaId}/pagamento`, input);
}

function obterContracheque(
  folhaId: string,
  servidorId: string,
  signal?: AbortSignal,
): Promise<Contracheque | null> {
  return http.get<Contracheque | null>(
    `/recursoshumanos/folhas/${folhaId}/servidores/${servidorId}/contracheque`,
    { signal },
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Folha de pagamento de uma competência. `enabled` controla disparo sob demanda. */
export function useFolhaPorCompetencia(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.folhaPorCompetencia(ano, mes),
    queryFn: ({ signal }) => obterFolhaPorCompetencia(ano, mes, signal),
    enabled,
  });
}

/** Abre uma folha para a competência e invalida as consultas de folha. */
export function useAbrirFolha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirFolha,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Lança um evento (provento/desconto) numa folha aberta e invalida-a. */
export function useAdicionarEvento(folhaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AdicionarEventoInput) => adicionarEvento(folhaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Calcula a folha (apura proventos/descontos/líquido) e invalida-a. */
export function useCalcularFolha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: calcularFolha,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Fecha a competência da folha e invalida-a. */
export function useFecharFolha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: fecharFolha,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Efetua o pagamento da folha fechada (transição Fechada → Paga) e invalida-a. */
export function useEfetuarPagamento(folhaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PagamentoInput) => efetuarPagamento(folhaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Contracheque de um servidor numa folha. */
export function useContracheque(folhaId: string, servidorId: string, enabled = true) {
  return useQuery({
    queryKey: rhKeys.contracheque(folhaId, servidorId),
    queryFn: ({ signal }) => obterContracheque(folhaId, servidorId, signal),
    enabled: enabled && folhaId.trim().length > 0 && servidorId.trim().length > 0,
  });
}
