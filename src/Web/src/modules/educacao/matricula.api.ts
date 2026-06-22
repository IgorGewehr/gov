// API do agregado Matrícula (módulo Educação). DTOs + acesso HTTP + hooks TanStack
// Query. Espelha os endpoints reais sob /api/educacao (EducacaoEndpoints.cs):
//   POST   /educacao/matriculas                          -> MatricularAluno         -> { id }
//   POST   /educacao/matriculas/rematricula              -> RematricularAluno       -> { id }
//   POST   /educacao/matriculas/{id}/transferencia       -> TransferirAluno         -> 204
//   POST   /educacao/matriculas/{id}/encerramento        -> EncerrarMatricula       -> 204
//   POST   /educacao/matriculas/{id}/situacao-aluno      -> RegistrarSituacaoDoAluno-> 204
//   GET    /educacao/alunos/{alunoId}/matriculas         -> ObterMatriculasDoAluno  -> MatriculaResumo[]
//   GET    /educacao/turmas/{turmaId}/matricula-inicial  -> ListarMatriculasInicial -> MatriculaResumo[]
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';

/** Situação da matrícula (Domain.Matriculas.SituacaoMatricula). */
export type SituacaoMatricula = 'Ativa' | 'Transferida' | 'Concluida' | 'Abandono';

/** Projeção MatriculaResumo (.../Application/Matriculas). */
export interface MatriculaResumo {
  id: string;
  alunoId: string;
  turmaId: string;
  escolaId: string;
  situacao: string;
  /** Data de referência do Censo (Matrícula Inicial) — ISO date. */
  dataReferencia: string;
}

/** Corpo de POST /matriculas (MatricularAlunoCommand). */
export interface MatricularAlunoInput {
  alunoId: string;
  turmaId: string;
  escolaId: string;
  dataReferencia: string;
}

/** Corpo de POST /matriculas/rematricula (RematricularAlunoCommand). */
export interface RematricularAlunoInput {
  matriculaAnteriorId: string;
  turmaDestinoId: string;
  dataReferencia: string;
}

/** Motivo de encerramento da matrícula (Domain.Matriculas.MotivoEncerramento). */
export type MotivoEncerramento = 'Conclusao' | 'Abandono';

/** Valor numérico do enum MotivoEncerramento esperado pelo backend (IsInEnum). */
export const MOTIVO_ENCERRAMENTO_VALOR: Record<MotivoEncerramento, number> = {
  Conclusao: 1,
  Abandono: 2,
};

/** Corpo de POST /matriculas/{id}/encerramento (EncerrarMatriculaPayload). */
export interface EncerrarMatriculaInput {
  motivo: number;
}

/** Rendimento do aluno (Domain.Matriculas.Rendimento). */
export type Rendimento = 'Aprovado' | 'Reprovado';

/** Valor numérico do enum Rendimento esperado pelo backend (IsInEnum). */
export const RENDIMENTO_VALOR: Record<Rendimento, number> = {
  Aprovado: 1,
  Reprovado: 2,
};

/** Movimento do aluno (Domain.Matriculas.Movimento). */
export type Movimento = 'SemMovimento' | 'Transferido' | 'Abandono' | 'Falecido';

/** Valor numérico do enum Movimento esperado pelo backend (IsInEnum). */
export const MOVIMENTO_VALOR: Record<Movimento, number> = {
  SemMovimento: 1,
  Transferido: 2,
  Abandono: 3,
  Falecido: 4,
};

/** Corpo de POST /matriculas/{id}/situacao-aluno (RegistrarSituacaoDoAlunoPayload). */
export interface RegistrarSituacaoDoAlunoInput {
  rendimento: number;
  movimento: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarMatriculasPorAluno(alunoId: string, signal?: AbortSignal): Promise<MatriculaResumo[]> {
  return http.get<MatriculaResumo[]>(`/educacao/alunos/${alunoId}/matriculas`, { signal });
}

function listarMatriculasDaTurma(
  turmaId: string,
  dataReferencia: string,
  signal?: AbortSignal,
): Promise<MatriculaResumo[]> {
  const query = new URLSearchParams({ dataReferencia }).toString();
  return http.get<MatriculaResumo[]>(`/educacao/turmas/${turmaId}/matricula-inicial?${query}`, {
    signal,
  });
}

function matricularAluno(input: MatricularAlunoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/matriculas', input);
}

function rematricularAluno(input: RematricularAlunoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/matriculas/rematricula', input);
}

function transferirAluno(matriculaId: string): Promise<void> {
  return http.post<void>(`/educacao/matriculas/${matriculaId}/transferencia`);
}

function encerrarMatricula(matriculaId: string, input: EncerrarMatriculaInput): Promise<void> {
  return http.post<void>(`/educacao/matriculas/${matriculaId}/encerramento`, input);
}

function registrarSituacaoDoAluno(
  matriculaId: string,
  input: RegistrarSituacaoDoAlunoInput,
): Promise<void> {
  return http.post<void>(`/educacao/matriculas/${matriculaId}/situacao-aluno`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista as matrículas de um aluno. `enabled` controla disparo sob demanda. */
export function useMatriculasDoAluno(alunoId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.matriculasPorAluno(alunoId),
    queryFn: ({ signal }) => listarMatriculasPorAluno(alunoId, signal),
    enabled: enabled && alunoId.trim().length > 0,
  });
}

/** Lista a Matrícula Inicial de uma turma na data de referência do Censo. */
export function useMatriculasDaTurma(turmaId: string, dataReferencia: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.matriculasDaTurma(turmaId, dataReferencia),
    queryFn: ({ signal }) => listarMatriculasDaTurma(turmaId, dataReferencia, signal),
    enabled: enabled && turmaId.trim().length > 0 && dataReferencia.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

function invalidarMatriculas(
  queryClient: ReturnType<typeof useQueryClient>,
  alunoId?: string,
): void {
  if (alunoId) {
    queryClient.invalidateQueries({ queryKey: educacaoKeys.matriculasPorAluno(alunoId) });
  } else {
    queryClient.invalidateQueries({ queryKey: educacaoKeys.matriculas() });
  }
}

/** Matrícula Inicial de um aluno e invalida as matrículas do aluno afetado. */
export function useMatricularAluno() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: matricularAluno,
    onSuccess: (_resultado, input) => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.matriculasPorAluno(input.alunoId) });
    },
  });
}

/** Rematrícula (renova a matrícula de um aluno apto, cria nova matrícula). */
export function useRematricularAluno() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: rematricularAluno,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.matriculas() });
    },
  });
}

/** Transfere o aluno de uma matrícula ativa. Invalida as matrículas do aluno. */
export function useTransferirAluno(alunoId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (matriculaId: string) => transferirAluno(matriculaId),
    onSuccess: () => invalidarMatriculas(queryClient, alunoId),
  });
}

/** Encerra uma matrícula (conclusão/abandono). Invalida as matrículas do aluno. */
export function useEncerrarMatricula(alunoId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ matriculaId, input }: { matriculaId: string; input: EncerrarMatriculaInput }) =>
      encerrarMatricula(matriculaId, input),
    onSuccess: () => invalidarMatriculas(queryClient, alunoId),
  });
}

/** Registra a Situação do Aluno (2ª etapa do Censo). Invalida as matrículas do aluno. */
export function useRegistrarSituacaoDoAluno(alunoId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      matriculaId,
      input,
    }: {
      matriculaId: string;
      input: RegistrarSituacaoDoAlunoInput;
    }) => registrarSituacaoDoAluno(matriculaId, input),
    onSuccess: () => invalidarMatriculas(queryClient, alunoId),
  });
}
