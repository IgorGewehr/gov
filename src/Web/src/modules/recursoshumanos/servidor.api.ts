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

/**
 * Página de resultados da busca paginada (envelope padrão do backend
 * `ResultadoPaginado<T>`: `Itens`/`Total`/`Pagina`/`Tamanho`).
 */
export interface PaginaResultado<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanho: number;
}

/**
 * Filtros da busca paginada de servidores. `situacao`/`regime` enviam o NOME do enum
 * do backend (ex.: "EmExercicio", "Rpps") — string vazia = sem filtro. Termo casa nome
 * (trecho, case-insensitive) ou matrícula (igualdade exata do valor completo).
 */
export interface BuscaServidoresFiltro {
  termo: string;
  situacao: string;
  regime: string;
  cargoId: string;
  pagina: number;
}

// --- Ficha funcional (Onda 0 — navegabilidade) ---------------------------------

/** Dados pessoais da ficha funcional (CPF mascarado — LGPD). */
export interface FichaDadosPessoais {
  cpf: string;
  nome: string;
  /** Data de nascimento ("yyyy-MM-dd"). */
  dataNascimento: string;
}

/** Vínculo e cargo da ficha funcional. */
export interface FichaVinculo {
  matricula: string;
  regime: string;
  situacao: string;
  cargoId: string;
  /** Denominação do cargo (nula se o cargo não for resolvido). */
  cargo: string | null;
  /** Tipo (natureza) do cargo (texto; nulo se não resolvido). */
  tipoCargo: string | null;
  /** Vencimento-base do cargo (nulo se não resolvido). */
  vencimento: number | null;
  /** Unidade de lotação do cargo (nula se não resolvida). */
  lotacao: string | null;
}

/** Marco do ciclo de vida do vínculo (timeline da ficha). */
export interface FichaEventoTimeline {
  /** Nome do marco (Nomeacao/Posse/Exercicio/Estabilidade/Desligamento). */
  evento: string;
  /** Data do marco ("yyyy-MM-dd"). */
  data: string;
}

/** Dependente listado na ficha funcional. */
export interface FichaDependente {
  nome: string;
  parentesco: string;
  /** Data de nascimento ("yyyy-MM-dd"). */
  dataNascimento: string;
}

/** Linha de histórico de folha do servidor na ficha. */
export interface FichaFolha {
  folhaId: string;
  ano: number;
  mes: number;
  tipo: string;
  situacao: string;
  totalProventos: number;
  totalDescontos: number;
  liquido: number;
}

/** Linha de histórico de ponto do servidor na ficha. */
export interface FichaPonto {
  apuracaoId: string;
  ano: number;
  mes: number;
  situacao: string;
  minutosExtras: number;
  minutosFalta: number;
  saldoBancoHorasMinutos: number;
}

/** Ficha funcional completa do servidor (dados + vínculo + histórico). CPF mascarado (LGPD). */
export interface FichaFuncional {
  id: string;
  dadosPessoais: FichaDadosPessoais;
  vinculo: FichaVinculo;
  timeline: FichaEventoTimeline[];
  dependentes: FichaDependente[];
  folhas: FichaFolha[];
  ponto: FichaPonto[];
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

function buscarServidores(
  filtro: BuscaServidoresFiltro,
  signal?: AbortSignal,
): Promise<PaginaResultado<ServidorResumo>> {
  return http.get<PaginaResultado<ServidorResumo>>('/recursoshumanos/servidores', {
    query: {
      termo: filtro.termo || null,
      situacao: filtro.situacao || null,
      regime: filtro.regime || null,
      cargoId: filtro.cargoId || null,
      pagina: filtro.pagina,
    },
    signal,
  });
}

function obterFichaFuncional(
  servidorId: string,
  signal?: AbortSignal,
): Promise<FichaFuncional | null> {
  return http.get<FichaFuncional | null>(
    `/recursoshumanos/servidores/${encodeURIComponent(servidorId)}/ficha-funcional`,
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

/** Busca paginada de servidores por nome/matrícula, com filtros situação/regime/cargo. */
export function useBuscaServidores(filtro: BuscaServidoresFiltro) {
  return useQuery({
    queryKey: rhKeys.servidoresBusca(
      filtro.termo,
      filtro.situacao,
      filtro.regime,
      filtro.cargoId,
      filtro.pagina,
    ),
    queryFn: ({ signal }) => buscarServidores(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Obtém a ficha funcional completa de um servidor (por id). Retorna null em 404. */
export function useFichaFuncional(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.fichaFuncional(servidorId),
    queryFn: ({ signal }) => obterFichaFuncional(servidorId, signal),
    enabled: servidorId.trim().length > 0,
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
