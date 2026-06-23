// API do DIÁRIO COLETIVO da turma (sub-onda 3a). DTOs + acesso HTTP + hooks TanStack
// Query. Espelha os endpoints REAIS sob /api/educacao (EducacaoEndpoints.cs ->
// MapearDiarioTurma) + os read models de DiarioTurmaDtos.cs:
//   GET  /educacao/turmas/{turmaId}/diario?data=          -> ObterDiarioDaTurma    -> DiarioTurmaView | null
//   POST /educacao/turmas/{turmaId}/diario/frequencias    -> RegistrarFrequenciaTurma -> { lancados }
//   POST /educacao/turmas/{turmaId}/diario/notas          -> LancarNotasTurma      -> { lancados }
//   GET  /educacao/matriculas/{matriculaId}/boletim       -> ObterBoletim          -> BoletimAlunoView | null
//   GET  /educacao/alunos/{alunoId}/historico-escolar     -> ObterHistoricoEscolar -> HistoricoEscolarView
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';

// --- Read models (espelham DiarioTurmaDtos.cs) ---

/** Linha de chamada de um aluno na grade do diário coletivo (LinhaDiarioTurma). */
export interface LinhaDiarioTurma {
  matriculaId: string;
  alunoId: string;
  nomeAluno: string;
  /** Diário 1-1 da matrícula, quando aberto; nulo se ainda não existir. */
  diarioId: string | null;
  /** Presença na data consultada; nula se sem lançamento no dia. */
  presente: boolean | null;
  percentualFrequencia: number;
}

/** Grade de chamada do diário coletivo da turma em uma data (DiarioTurmaView). */
export interface DiarioTurmaView {
  turmaId: string;
  escolaId: string;
  anoLetivo: number;
  serie: string;
  turno: string;
  data: string;
  linhas: LinhaDiarioTurma[];
}

/** Média de um componente curricular no boletim (MediaComponente). */
export interface MediaComponente {
  componenteCurricularId: string;
  media: number;
  quantidadeNotas: number;
}

/** Boletim do aluno em um diário (BoletimAlunoView). */
export interface BoletimAlunoView {
  matriculaId: string;
  alunoId: string;
  turmaId: string;
  diarioId: string | null;
  situacao: string | null;
  percentualFrequencia: number;
  diasLetivosRegistrados: number;
  resultado: string | null;
  medias: MediaComponente[];
}

/** Item longitudinal do histórico escolar (ItemHistoricoEscolar). */
export interface ItemHistoricoEscolar {
  matriculaId: string;
  turmaId: string;
  escolaId: string;
  anoLetivo: number;
  serie: string;
  situacaoMatricula: string;
  percentualFrequencia: number | null;
  resultado: string | null;
}

/** Histórico escolar longitudinal do aluno (HistoricoEscolarView). */
export interface HistoricoEscolarView {
  alunoId: string;
  itens: ItemHistoricoEscolar[];
}

// --- Corpos de POST ---

/** Presença de um aluno na chamada coletiva (FrequenciaAlunoLote). */
export interface FrequenciaAlunoLote {
  matriculaId: string;
  presente: boolean;
}

/** Corpo de POST /turmas/{id}/diario/frequencias (RegistrarFrequenciaTurmaPayload). */
export interface RegistrarFrequenciaTurmaInput {
  data: string;
  cargaHorariaAula: number;
  presencas: FrequenciaAlunoLote[];
}

/** Nota de um aluno na grade de lançamento coletivo (NotaAlunoLote). */
export interface NotaAlunoLote {
  matriculaId: string;
  valor: number;
}

/** Corpo de POST /turmas/{id}/diario/notas (LancarNotasTurmaPayload). */
export interface LancarNotasTurmaInput {
  componenteCurricularId: string;
  periodo: string;
  notas: NotaAlunoLote[];
}

/** Resposta dos lançamentos em lote (Results.Ok(new { lancados })). */
export interface LoteResponse {
  lancados: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterDiarioDaTurma(
  turmaId: string,
  data: string,
  signal?: AbortSignal,
): Promise<DiarioTurmaView | null> {
  return http.get<DiarioTurmaView | null>(`/educacao/turmas/${turmaId}/diario`, {
    query: { data },
    signal,
  });
}

function registrarFrequenciaTurma(
  turmaId: string,
  input: RegistrarFrequenciaTurmaInput,
): Promise<LoteResponse> {
  return http.post<LoteResponse>(`/educacao/turmas/${turmaId}/diario/frequencias`, input);
}

function lancarNotasTurma(turmaId: string, input: LancarNotasTurmaInput): Promise<LoteResponse> {
  return http.post<LoteResponse>(`/educacao/turmas/${turmaId}/diario/notas`, input);
}

function obterBoletim(matriculaId: string, signal?: AbortSignal): Promise<BoletimAlunoView | null> {
  return http.get<BoletimAlunoView | null>(`/educacao/matriculas/${matriculaId}/boletim`, { signal });
}

function obterHistoricoEscolar(alunoId: string, signal?: AbortSignal): Promise<HistoricoEscolarView> {
  return http.get<HistoricoEscolarView>(`/educacao/alunos/${alunoId}/historico-escolar`, { signal });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Grade de chamada do diário coletivo da turma em uma data. */
export function useDiarioDaTurma(turmaId: string, data: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.diarioDaTurma(turmaId, data),
    queryFn: ({ signal }) => obterDiarioDaTurma(turmaId, data, signal),
    enabled: enabled && turmaId.trim().length > 0 && data.trim().length > 0,
  });
}

/** Boletim do aluno (médias por componente, frequência, resultado) de uma matrícula. */
export function useBoletimDaMatricula(matriculaId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.boletimDaMatricula(matriculaId),
    queryFn: ({ signal }) => obterBoletim(matriculaId, signal),
    enabled: enabled && matriculaId.trim().length > 0,
  });
}

/** Histórico escolar longitudinal do aluno (uma matrícula por ano letivo/turma). */
export function useHistoricoEscolar(alunoId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.historicoEscolar(alunoId),
    queryFn: ({ signal }) => obterHistoricoEscolar(alunoId, signal),
    enabled: enabled && alunoId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Registra a chamada (frequência) em lote da turma na data e invalida a grade. */
export function useRegistrarFrequenciaTurma(turmaId: string, data: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarFrequenciaTurmaInput) => registrarFrequenciaTurma(turmaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.diarioDaTurma(turmaId, data) });
    },
  });
}

/** Lança notas em lote de um componente/período na turma e invalida a grade. */
export function useLancarNotasTurma(turmaId: string, data: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: LancarNotasTurmaInput) => lancarNotasTurma(turmaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.diarioDaTurma(turmaId, data) });
    },
  });
}
