// API do agregado Transporte (PNATE) — módulo Educação. DTOs + acesso HTTP +
// hooks TanStack Query. Espelha os endpoints REAIS sob /api/educacao/transporte
// (EducacaoEndpoints.cs / TransporteDtos.cs):
//   GET    /educacao/transporte/rotas?escolaId=                         -> ObterRotasPorEscola -> RotaItemLista[]
//   GET    /educacao/transporte/rotas/{rotaId}/alunos                   -> ListarAlunosDaRota  -> RotaDto | null
//   POST   /educacao/transporte/rotas                                   -> CriarRota           -> { id }
//   POST   /educacao/transporte/rotas/{rotaId}/alunos                   -> VincularAlunoRota    -> { id }
//   POST   /educacao/transporte/rotas/{rotaId}/alunos/{id}/desligamento -> DesligarAlunoRota    -> 204
//   POST   /educacao/transporte/rotas/{rotaId}/ativacao                 -> AtivarRota           -> 204
//   POST   /educacao/transporte/rotas/{rotaId}/encerramento             -> EncerrarRota         -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';
// O turno da rota reusa o enum Turno de Turmas (mesmo contrato, valores 1..4) para
// evitar colisão de nomes no barrel './api'.
import type { Turno } from './turma.api';

/** Modalidade de execução (Domain.Transporte.ModalidadeTransporte) — valor numérico. */
export type ModalidadeTransporte = 0 | 1;

/** Aluno transportado projetado na ficha da rota (AlunoTransportadoDto). */
export interface AlunoTransportadoDto {
  id: string;
  alunoId: string;
  matriculaId: string | null;
  pontoEmbarque: string;
  ativo: boolean;
}

/** Item da lista de rotas (RotaTransporteItemLista) — picker do front. */
export interface RotaTransporteItemLista {
  id: string;
  escolaId: string;
  nome: string;
  /** Turno (descrição). */
  turno: string;
  /** Modalidade (descrição: Proprio | Terceirizado). */
  modalidade: string;
  /** Veículo da Frota (Patrimônio) por Id, se Próprio. */
  veiculoId: string | null;
  quilometragem: number;
  /** Situação (descrição: Planejada | Ativa | Encerrada). */
  situacao: string;
  totalAtivos: number;
}

/** Ficha da rota com os alunos transportados (RotaTransporteDto). */
export interface RotaTransporteDto {
  id: string;
  escolaId: string;
  nome: string;
  turno: string;
  modalidade: string;
  veiculoId: string | null;
  quilometragem: number;
  situacao: string;
  alunos: AlunoTransportadoDto[];
}

/** Corpo de POST /transporte/rotas (CriarRotaCommand). */
export interface CriarRotaInput {
  escolaId: string;
  nome: string;
  turno: Turno;
  modalidade: ModalidadeTransporte;
  /** Obrigatório se modalidade Próprio (0); deve ser nulo no Terceirizado (1). */
  veiculoId: string | null;
  quilometragem: number;
}

/** Corpo de POST /transporte/rotas/{rotaId}/alunos (VincularAlunoRotaPayload). */
export interface VincularAlunoInput {
  alunoId: string;
  matriculaId: string | null;
  pontoEmbarque: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarRotas(
  escolaId: string | undefined,
  signal?: AbortSignal,
): Promise<RotaTransporteItemLista[]> {
  return http.get<RotaTransporteItemLista[]>('/educacao/transporte/rotas', {
    query: { escolaId },
    signal,
  });
}

function obterRota(rotaId: string, signal?: AbortSignal): Promise<RotaTransporteDto | null> {
  return http.get<RotaTransporteDto | null>(`/educacao/transporte/rotas/${rotaId}/alunos`, {
    signal,
  });
}

function criarRota(input: CriarRotaInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/transporte/rotas', input);
}

function vincularAluno(rotaId: string, input: VincularAlunoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>(`/educacao/transporte/rotas/${rotaId}/alunos`, input);
}

function desligarAluno(rotaId: string, alunoTransportadoId: string): Promise<void> {
  return http.post<void>(
    `/educacao/transporte/rotas/${rotaId}/alunos/${alunoTransportadoId}/desligamento`,
  );
}

function ativarRota(rotaId: string): Promise<void> {
  return http.post<void>(`/educacao/transporte/rotas/${rotaId}/ativacao`);
}

function encerrarRota(rotaId: string): Promise<void> {
  return http.post<void>(`/educacao/transporte/rotas/${rotaId}/encerramento`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista rotas por escola (filtro opcional). */
export function useRotas(escolaId?: string) {
  const filtro = { escolaId: escolaId || undefined };
  return useQuery({
    queryKey: educacaoKeys.rotasBusca(filtro),
    queryFn: ({ signal }) => listarRotas(filtro.escolaId, signal),
  });
}

/** Obtém a ficha da rota (com alunos transportados). `enabled` dispara sob demanda. */
export function useRota(rotaId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.rotaPorId(rotaId),
    queryFn: ({ signal }) => obterRota(rotaId, signal),
    enabled: enabled && rotaId.trim().length > 0,
  });
}

/** Cria uma rota (situação inicial Planejada) e invalida as listas de rotas. */
export function useCriarRota() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarRota,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotas() });
    },
  });
}

/** Vincula um aluno a uma rota (com ponto de embarque). Invalida a ficha + listas. */
export function useVincularAluno(rotaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: VincularAlunoInput) => vincularAluno(rotaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotaPorId(rotaId) });
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotas() });
    },
  });
}

/** Desliga (logicamente) um aluno da rota. Invalida a ficha + listas. */
export function useDesligarAluno(rotaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (alunoTransportadoId: string) => desligarAluno(rotaId, alunoTransportadoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotaPorId(rotaId) });
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotas() });
    },
  });
}

/** Ativa a rota (Planejada → Ativa; exige ao menos um aluno ativo). Invalida ficha + listas. */
export function useAtivarRota(rotaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => ativarRota(rotaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotaPorId(rotaId) });
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotas() });
    },
  });
}

/** Encerra a rota (terminal). Invalida ficha + listas. */
export function useEncerrarRota(rotaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => encerrarRota(rotaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotaPorId(rotaId) });
      queryClient.invalidateQueries({ queryKey: educacaoKeys.rotas() });
    },
  });
}
