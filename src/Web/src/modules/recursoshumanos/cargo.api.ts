// Camada de API da entidade Cargo (módulo RecursosHumanos). Segue o PADRÃO-OURO:
// DTOs no topo, funções de acesso via http client tipado, hooks TanStack Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Tipo (natureza jurídica do provimento) de um cargo público. */
export type TipoCargo = 'Efetivo' | 'Comissionado' | 'Temporario';

/** Resumo de um cargo com vagas disponíveis. */
export interface CargoResumo {
  id: string;
  denominacao: string;
  tipo: string;
  vagasDisponiveis: number;
}

/** Detalhe de leitura de um cargo. */
export interface CargoDetalhe {
  id: string;
  denominacao: string;
  tipo: string;
  vencimento: number;
  lotacao: string;
  regime: string;
  quantidadeVagas: number;
  vagasOcupadas: number;
  situacao: string;
  leiCriacao: string;
}

/** Lotação/estabelecimento de exercício do cargo (entrada). */
export interface LotacaoInput {
  inscricaoEstabelecimento: string;
  denominacaoUnidade: string;
  codigoLotacaoTributaria?: string | null;
}

/** Entrada da criação de um cargo público. */
export interface CriarCargoInput {
  denominacao: string;
  /** 1 = Efetivo, 2 = Comissionado, 3 = Temporário (enum numérico do backend). */
  tipo: number;
  vencimento: number;
  lotacao: LotacaoInput;
  quantidadeVagas: number;
  leiCriacao: string;
  planoDeCargosId?: string | null;
}

/** Entrada da alteração do vencimento-base de um cargo. */
export interface AlterarVencimentoInput {
  novoVencimento: number;
}

/** Entrada da extinção de um cargo (lei que extingue). */
export interface ExtinguirCargoInput {
  leiExtincao: string;
}

/** Entrada do reajuste salarial em lote (percentual linear sobre os cargos ativos). */
export interface AplicarReajusteEmLoteInput {
  /** Percentual de reajuste (ex.: 5.5 = +5,5%); em (0, 100]. */
  percentual: number;
  /** Restringe a um tipo de cargo (1=Efetivo, 2=Comissionado, 3=Temporário); ausente = todos. */
  tipo?: number | null;
}

/** Resultado consolidado de um reajuste em lote. */
export interface ReajusteEmLoteResultado {
  cargosReajustados: number;
  percentual: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarCargosComVagas(
  tipo: TipoCargo | null,
  signal?: AbortSignal,
): Promise<CargoResumo[]> {
  return http.get<CargoResumo[]>('/recursoshumanos/cargos/com-vagas', {
    signal,
    query: tipo ? { tipo } : undefined,
  });
}

function obterCargo(id: string, signal?: AbortSignal): Promise<CargoDetalhe> {
  return http.get<CargoDetalhe>(`/recursoshumanos/cargos/${id}`, { signal });
}

function criarCargo(input: CriarCargoInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/cargos', input);
}

function proverCargo(cargoId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/cargos/${cargoId}/provimento`);
}

function vagarCargo(cargoId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/cargos/${cargoId}/vacancia`);
}

function alterarVencimento(cargoId: string, input: AlterarVencimentoInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/cargos/${cargoId}/vencimento`, input);
}

function extinguirCargo(cargoId: string, input: ExtinguirCargoInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/cargos/${cargoId}/extincao`, input);
}

function aplicarReajusteEmLote(input: AplicarReajusteEmLoteInput): Promise<ReajusteEmLoteResultado> {
  return http.post<ReajusteEmLoteResultado>('/recursoshumanos/cargos/reajuste-lote', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista os cargos com vagas disponíveis, opcionalmente por tipo. */
export function useCargosComVagas(tipo: TipoCargo | null = null) {
  return useQuery({
    queryKey: rhKeys.cargosComVagas(tipo),
    queryFn: ({ signal }) => listarCargosComVagas(tipo, signal),
  });
}

/** Detalhe de um cargo por identificador. */
export function useCargo(id: string) {
  return useQuery({
    queryKey: rhKeys.cargo(id),
    queryFn: ({ signal }) => obterCargo(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Cria um cargo público e invalida as listas de cargos. */
export function useCriarCargo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarCargo,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.cargos() });
    },
  });
}

/** Prove (ocupa uma vaga de) o cargo e invalida as consultas de cargo. */
export function useProverCargo(cargoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => proverCargo(cargoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.cargos() });
    },
  });
}

/** Vaga (libera uma vaga d)o cargo e invalida as consultas de cargo. */
export function useVagarCargo(cargoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => vagarCargo(cargoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.cargos() });
    },
  });
}

/** Altera o vencimento-base do cargo e invalida as consultas de cargo. */
export function useAlterarVencimento(cargoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AlterarVencimentoInput) => alterarVencimento(cargoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.cargos() });
    },
  });
}

/** Extingue o cargo (terminal) e invalida as consultas de cargo. */
export function useExtinguirCargo(cargoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ExtinguirCargoInput) => extinguirCargo(cargoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.cargos() });
    },
  });
}

/** Aplica um reajuste salarial em lote (revisão geral) e invalida as consultas de cargo. */
export function useAplicarReajusteEmLote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: aplicarReajusteEmLote,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.cargos() });
    },
  });
}
