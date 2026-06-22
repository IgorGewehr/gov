// Camada de API da entidade Servidor (módulo RecursosHumanos). Segue o PADRÃO-OURO:
// DTOs no topo, funções de acesso via http client tipado, hooks TanStack Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situação (estado) do vínculo do servidor no ciclo de vida (Domain/Servidores). */
export type SituacaoServidor =
  | 'Nomeado'
  | 'Empossado'
  | 'EmExercicio'
  | 'Estavel'
  | 'Afastado'
  | 'Desligado';

/** Regime previdenciário associado ao cargo/servidor (EC 103/2019). */
export type RegimePrevidenciario = 'Rpps' | 'Rgps';

/** Resumo de leitura de um servidor (CPF mascarado — LGPD). */
export interface ServidorResumo {
  id: string;
  cpf: string;
  matricula: string;
  nomeServidor: string;
  cargoId: string;
  regime: string;
  situacao: string;
  dataNomeacao: string;
  dataExercicio: string | null;
}

/** Dados cadastrais sensíveis do servidor (entrada — LGPD). */
export interface DadosPessoaisInput {
  nome: string;
  dataNascimento: string;
}

/** Entrada da admissão (provimento) de um servidor. */
export interface AdmitirServidorInput {
  cpf: string;
  matricula: string;
  dadosPessoais: DadosPessoaisInput;
  cargoId: string;
  /** 1 = RPPS, 2 = RGPS (enum numérico do backend). */
  regime: number;
  dataNomeacao: string;
}

/** Entrada do registro de POSSE do servidor (DateOnly AAAA-MM-DD). */
export interface PosseInput {
  dataPosse: string;
}

/** Entrada do início de EXERCÍCIO do servidor (DateOnly AAAA-MM-DD). */
export interface ExercicioInput {
  dataExercicio: string;
}

/** Entrada do registro de AFASTAMENTO do servidor. */
export interface AfastamentoInput {
  inicio: string;
  fim?: string | null;
  motivo: string;
}

/** Entrada do DESLIGAMENTO (vacância) do servidor. */
export interface DesligamentoInput {
  dataDesligamento: string;
  motivo: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarServidoresAtivos(signal?: AbortSignal): Promise<ServidorResumo[]> {
  return http.get<ServidorResumo[]>('/recursoshumanos/servidores/ativos', { signal });
}

function obterServidorPorMatricula(
  matricula: string,
  signal?: AbortSignal,
): Promise<ServidorResumo | null> {
  return http.get<ServidorResumo | null>(
    `/recursoshumanos/servidores/por-matricula/${encodeURIComponent(matricula)}`,
    { signal },
  );
}

function admitirServidor(input: AdmitirServidorInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/servidores', input);
}

function registrarPosse(servidorId: string, input: PosseInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/servidores/${servidorId}/posse`, input);
}

function iniciarExercicio(servidorId: string, input: ExercicioInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/servidores/${servidorId}/exercicio`, input);
}

function concederEstabilidade(servidorId: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/servidores/${servidorId}/estabilidade`);
}

function registrarAfastamento(servidorId: string, input: AfastamentoInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/servidores/${servidorId}/afastamento`, input);
}

function desligarServidor(servidorId: string, input: DesligamentoInput): Promise<void> {
  return http.post<void>(`/recursoshumanos/servidores/${servidorId}/desligamento`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista os servidores ativos do tenant. */
export function useServidoresAtivos() {
  return useQuery({
    queryKey: rhKeys.servidoresAtivos(),
    queryFn: ({ signal }) => listarServidoresAtivos(signal),
  });
}

/** Obtém um servidor pela matrícula. `enabled` controla disparo sob demanda. */
export function useServidorPorMatricula(matricula: string, enabled = true) {
  return useQuery({
    queryKey: rhKeys.servidorPorMatricula(matricula),
    queryFn: ({ signal }) => obterServidorPorMatricula(matricula, signal),
    enabled: enabled && matricula.trim().length > 0,
  });
}

/** Admite (provimento) um servidor e invalida a lista de ativos. */
export function useAdmitirServidor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: admitirServidor,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Registra a posse do servidor (transição Nomeado → Empossado). */
export function useRegistrarPosse(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PosseInput) => registrarPosse(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Inicia o exercício do servidor (transição Empossado → EmExercicio). */
export function useIniciarExercicio(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ExercicioInput) => iniciarExercicio(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Concede estabilidade ao servidor (transição EmExercicio → Estavel). */
export function useConcederEstabilidade(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => concederEstabilidade(servidorId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Registra afastamento do servidor (transição → Afastado). */
export function useRegistrarAfastamento(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AfastamentoInput) => registrarAfastamento(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Desliga o servidor (transição terminal → Desligado). */
export function useDesligarServidor(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: DesligamentoInput) => desligarServidor(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}
