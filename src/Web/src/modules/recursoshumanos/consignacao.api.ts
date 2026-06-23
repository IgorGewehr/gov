// Camada de API das CONSIGNAÇÕES (Onda 2 — Lei 14.131/2021). Segue o PADRÃO-OURO:
// DTOs no topo, funções de acesso via http client tipado, hooks TanStack Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.Consignacoes.cs
// Enums viajam como NOME (JsonStringEnumConverter no ApiHost). O http client prefixa /api.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// Enums (nomes do backend — Domain/Consignacoes/Enums.cs)
// ---------------------------------------------------------------------------

/** Natureza da consignatária. */
export type TipoConsignataria =
  | 'InstituicaoFinanceira'
  | 'EntidadeClassista'
  | 'Seguradora'
  | 'Outra';

/** Situação da consignatária no cadastro mestre. */
export type SituacaoConsignataria = 'Ativa' | 'Suspensa';

/** Categoria (prioridade no corte por margem): obrigatória > facultativa > benefício. */
export type CategoriaConsignavel = 'Obrigatoria' | 'Facultativa' | 'Beneficio';

/** Balde (reserva legal) de margem consumido pela rubrica. */
export type GrupoMargem = 'Geral' | 'CartaoConsignado' | 'CartaoBeneficio';

/** Situação no ciclo de vida do contrato de consignação. */
export type SituacaoConsignacao = 'Averbada' | 'Suspensa' | 'Quitada' | 'Cancelada';

// ---------------------------------------------------------------------------
// DTOs (espelham os records do contrato)
// ---------------------------------------------------------------------------

/** Resumo de leitura de uma consignatária (CNPJ sem máscara — dado cadastral). */
export interface ConsignatariaResumo {
  id: string;
  cnpj: string;
  razaoSocial: string;
  tipo: TipoConsignataria;
  situacao: SituacaoConsignataria;
}

/** Limite/comprometido/disponível de um balde de margem. */
export interface BaldeMargem {
  grupo: GrupoMargem;
  limite: number;
  comprometido: number;
  disponivel: number;
}

/** Margem consignável de um servidor numa competência (3 baldes). */
export interface MargemConsignavel {
  servidorId: string;
  /** Competência (AAAA-MM). */
  competencia: string;
  baseDeCalculo: number;
  baldes: BaldeMargem[];
}

/** Resumo de leitura de um contrato de consignação. */
export interface ContratoConsignacaoResumo {
  id: string;
  servidorId: string;
  consignatariaId: string;
  codigoRubrica: string;
  categoria: CategoriaConsignavel;
  grupoMargem: GrupoMargem;
  numeroContratoExterno: string | null;
  valorParcela: number;
  quantidadeParcelas: number;
  parcelasPagas: number;
  parcelasRestantes: number;
  /** Data da averbação ("yyyy-MM-dd"). */
  dataAverbacao: string;
  situacao: SituacaoConsignacao;
}

// --- Entradas (commands) -------------------------------------------------------

/** Entrada do cadastro de consignatária. */
export interface CadastrarConsignatariaInput {
  cnpj: string;
  razaoSocial: string;
  tipo: TipoConsignataria;
}

/** Entrada da averbação de um contrato de consignação. */
export interface AverbarConsignacaoInput {
  servidorId: string;
  consignatariaId: string;
  codigoRubrica: string;
  numeroContratoExterno?: string | null;
  valorParcela: number;
  quantidadeParcelas: number;
  /** Data da averbação ("yyyy-MM-dd") — define a competência base da margem. */
  dataAverbacao: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarConsignatarias(signal?: AbortSignal): Promise<ConsignatariaResumo[]> {
  return http.get<ConsignatariaResumo[]>('/recursoshumanos/consignatarias', { signal });
}

function cadastrarConsignataria(input: CadastrarConsignatariaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/consignatarias', input);
}

function suspenderConsignataria(consignatariaId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/consignatarias/${consignatariaId}/suspensao`);
}

function reativarConsignataria(consignatariaId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/consignatarias/${consignatariaId}/reativacao`);
}

function consultarMargem(
  servidorId: string,
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<MargemConsignavel> {
  return http.get<MargemConsignavel>(
    `/recursoshumanos/servidores/${encodeURIComponent(servidorId)}/margem`,
    { query: { ano, mes }, signal },
  );
}

function listarConsignacoesDoServidor(
  servidorId: string,
  signal?: AbortSignal,
): Promise<ContratoConsignacaoResumo[]> {
  return http.get<ContratoConsignacaoResumo[]>(
    `/recursoshumanos/servidores/${encodeURIComponent(servidorId)}/consignacoes`,
    { signal },
  );
}

function averbarConsignacao(input: AverbarConsignacaoInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/consignacoes', input);
}

function suspenderConsignacao(contratoId: string, motivo: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/consignacoes/${contratoId}/suspensao`, { motivo });
}

function cancelarConsignacao(contratoId: string, motivo: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/consignacoes/${contratoId}/cancelamento`, { motivo });
}

function reativarConsignacao(contratoId: string, dataReferencia: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/consignacoes/${contratoId}/reativacao`, {
    dataReferencia,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as consignatárias cadastradas no tenant. */
export function useConsignatarias() {
  return useQuery({
    queryKey: rhKeys.consignatarias(),
    queryFn: ({ signal }) => listarConsignatarias(signal),
  });
}

/** Cadastra uma consignatária e invalida a lista. */
export function useCadastrarConsignataria() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarConsignataria,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: rhKeys.consignatarias() }),
  });
}

/** Suspende uma consignatária (deixa de receber novas averbações). */
export function useSuspenderConsignataria() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (consignatariaId: string) => suspenderConsignataria(consignatariaId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: rhKeys.consignatarias() }),
  });
}

/** Reativa uma consignatária suspensa. */
export function useReativarConsignataria() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (consignatariaId: string) => reativarConsignataria(consignatariaId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: rhKeys.consignatarias() }),
  });
}

/** Consulta a margem consignável do servidor numa competência (3 baldes). */
export function useMargemConsignavel(servidorId: string, ano: number, mes: number) {
  return useQuery({
    queryKey: rhKeys.margem(servidorId, ano, mes),
    queryFn: ({ signal }) => consultarMargem(servidorId, ano, mes, signal),
    enabled: servidorId.trim().length > 0 && mes >= 1 && mes <= 12,
  });
}

/** Lista as consignações (histórico) de um servidor. */
export function useConsignacoesDoServidor(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.consignacoesDoServidor(servidorId),
    queryFn: ({ signal }) => listarConsignacoesDoServidor(servidorId, signal),
    enabled: servidorId.trim().length > 0,
  });
}

/** Averba um contrato (rejeitado pelo backend se a parcela estoura a margem do balde). */
export function useAverbarConsignacao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: averbarConsignacao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.consignacoesDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: [...rhKeys.all, 'margem', servidorId] });
    },
  });
}

/** Suspende uma consignação averbada (libera margem). */
export function useSuspenderConsignacao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ contratoId, motivo }: { contratoId: string; motivo: string }) =>
      suspenderConsignacao(contratoId, motivo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.consignacoesDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: [...rhKeys.all, 'margem', servidorId] });
    },
  });
}

/** Cancela uma consignação antes da quitação (libera margem; terminal). */
export function useCancelarConsignacao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ contratoId, motivo }: { contratoId: string; motivo: string }) =>
      cancelarConsignacao(contratoId, motivo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.consignacoesDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: [...rhKeys.all, 'margem', servidorId] });
    },
  });
}

/** Reativa uma consignação suspensa (re-checa a margem na competência informada). */
export function useReativarConsignacao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ contratoId, dataReferencia }: { contratoId: string; dataReferencia: string }) =>
      reativarConsignacao(contratoId, dataReferencia),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.consignacoesDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: [...rhKeys.all, 'margem', servidorId] });
    },
  });
}
