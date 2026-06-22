// API do agregado Diário de Classe (módulo Educação). DTOs + acesso HTTP + hooks
// TanStack Query. Espelha os endpoints reais sob /api/educacao (EducacaoEndpoints.cs):
//   POST   /educacao/diarios                         -> AbrirDiarioClasse     -> { id }
//   POST   /educacao/diarios/{id}/frequencias        -> RegistrarFrequencia   -> 204
//   POST   /educacao/diarios/{id}/notas              -> LancarNota            -> 204
//   POST   /educacao/diarios/{id}/aulas              -> RegistrarAula         -> 204
//   POST   /educacao/diarios/{id}/apuracao           -> ApurarResultado       -> 204
//   GET    /educacao/diarios/{id}/frequencia         -> ObterFrequenciaDoDiario -> FrequenciaConsolidada
//   GET    /educacao/matriculas/{id}/diario          -> ObterDiarioDaMatricula -> DiarioClasseResumo | null
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';

/** Projeção DiarioClasseResumo (.../Application/DiarioClasse). */
export interface DiarioClasseResumo {
  id: string;
  matriculaId: string;
  situacao: string;
  percentualFrequencia: number;
  resultado: string | null;
  diasLetivosRegistrados: number;
}

/** Frequência consolidada de um diário (FrequenciaConsolidada). */
export interface FrequenciaConsolidada {
  diarioClasseId: string;
  percentualFrequencia: number;
  aulasComputadas: number;
  atingiuMinimo: boolean;
}

/** Corpo de POST /diarios (AbrirDiarioClasseCommand). */
export interface AbrirDiarioClasseInput {
  matriculaId: string;
  /** Carga horária anual de referência (800h/1.000h). */
  cargaHorariaTotal: number;
}

/** Corpo de POST /diarios/{id}/frequencias (RegistrarFrequenciaPayload). */
export interface RegistrarFrequenciaInput {
  data: string;
  presente: boolean;
  cargaHorariaAula: number;
}

/** Corpo de POST /diarios/{id}/notas (LancarNotaPayload). */
export interface LancarNotaInput {
  componenteCurricularId: string;
  periodo: string;
  valor: number;
}

/** Corpo de POST /diarios/{id}/aulas (RegistrarAulaPayload). */
export interface RegistrarAulaInput {
  data: string;
  conteudo: string;
  diaLetivo: boolean;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterDiarioDaMatricula(
  matriculaId: string,
  signal?: AbortSignal,
): Promise<DiarioClasseResumo | null> {
  return http.get<DiarioClasseResumo | null>(`/educacao/matriculas/${matriculaId}/diario`, {
    signal,
  });
}

function obterFrequenciaDoDiario(diarioId: string, signal?: AbortSignal): Promise<FrequenciaConsolidada> {
  return http.get<FrequenciaConsolidada>(`/educacao/diarios/${diarioId}/frequencia`, { signal });
}

function abrirDiarioClasse(input: AbrirDiarioClasseInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/diarios', input);
}

function registrarFrequencia(diarioId: string, input: RegistrarFrequenciaInput): Promise<void> {
  return http.post<void>(`/educacao/diarios/${diarioId}/frequencias`, input);
}

function lancarNota(diarioId: string, input: LancarNotaInput): Promise<void> {
  return http.post<void>(`/educacao/diarios/${diarioId}/notas`, input);
}

function registrarAula(diarioId: string, input: RegistrarAulaInput): Promise<void> {
  return http.post<void>(`/educacao/diarios/${diarioId}/aulas`, input);
}

function apurarResultado(diarioId: string): Promise<void> {
  return http.post<void>(`/educacao/diarios/${diarioId}/apuracao`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Obtém o diário vinculado a uma matrícula (vínculo 1-1). */
export function useDiarioDaMatricula(matriculaId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.diarioDaMatricula(matriculaId),
    queryFn: ({ signal }) => obterDiarioDaMatricula(matriculaId, signal),
    enabled: enabled && matriculaId.trim().length > 0,
  });
}

/** Obtém a frequência consolidada de um diário (percentual, mínimo LDB 75%). */
export function useFrequenciaDoDiario(diarioId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.frequenciaDoDiario(diarioId),
    queryFn: ({ signal }) => obterFrequenciaDoDiario(diarioId, signal),
    enabled: enabled && diarioId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Abre um diário de classe para uma matrícula ativa e invalida o diário da matrícula. */
export function useAbrirDiarioClasse() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirDiarioClasse,
    onSuccess: (_resultado, input) => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.diarioDaMatricula(input.matriculaId) });
    },
  });
}

/** Hook genérico de invalidação do diário (resumo + frequência) de uma matrícula. */
function useInvalidarDiario(matriculaId: string, diarioId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: educacaoKeys.diarioDaMatricula(matriculaId) });
    queryClient.invalidateQueries({ queryKey: educacaoKeys.frequenciaDoDiario(diarioId) });
  };
}

/** Registra a frequência (presença/falta) de uma aula em um diário aberto. */
export function useRegistrarFrequencia(matriculaId: string, diarioId: string) {
  const invalidar = useInvalidarDiario(matriculaId, diarioId);
  return useMutation({
    mutationFn: (input: RegistrarFrequenciaInput) => registrarFrequencia(diarioId, input),
    onSuccess: invalidar,
  });
}

/** Lança a nota de um componente curricular em um período, em um diário aberto. */
export function useLancarNota(matriculaId: string, diarioId: string) {
  const invalidar = useInvalidarDiario(matriculaId, diarioId);
  return useMutation({
    mutationFn: (input: LancarNotaInput) => lancarNota(diarioId, input),
    onSuccess: invalidar,
  });
}

/** Registra uma aula/dia letivo em um diário aberto. */
export function useRegistrarAula(matriculaId: string, diarioId: string) {
  const invalidar = useInvalidarDiario(matriculaId, diarioId);
  return useMutation({
    mutationFn: (input: RegistrarAulaInput) => registrarAula(diarioId, input),
    onSuccess: invalidar,
  });
}

/** Apura o resultado anual de um diário aberto, fechando-o em Apurado. */
export function useApurarResultado(matriculaId: string, diarioId: string) {
  const invalidar = useInvalidarDiario(matriculaId, diarioId);
  return useMutation({
    mutationFn: () => apurarResultado(diarioId),
    onSuccess: invalidar,
  });
}
