// Camada de API da MARCAÇÃO de agendamentos (consultas/exames) — módulo Saúde. Também
// concentra os enums compartilhados do agregado de agenda. DTOs + acesso HTTP + hooks
// TanStack. A grade de disponibilidade vive em agenda.api.ts e a fila em filaEspera.api.ts.
// Contrato REAL (SaudeEndpoints.cs — grupo /api/saude; o http client já prefixa /api):
//   POST /saude/agendamentos                               -> MarcarAgendamento  -> { id } (saude.agenda.marcar)
//   GET  /saude/agendamentos?paciente&profissional&data&situacao&pagina&tamanho
//                                                          -> BuscarAgendamentos -> ResultadoPaginado<AgendamentoItemLista> (saude.agenda.ver)
//   POST /saude/agendamentos/{id}/confirmacao              -> ConfirmarAgendamento (204) (saude.agenda.marcar)
//   POST /saude/agendamentos/{id}/cancelamento {motivo,origem} -> CancelarAgendamento (204) (saude.agenda.marcar)
//   POST /saude/agendamentos/{id}/falta                    -> RegistrarFalta (204)      (saude.agenda.marcar)
//   POST /saude/agendamentos/{id}/realizacao {atendimentoId?} -> RealizarAgendamento (204) (saude.agenda.marcar)
// LGPD: a busca de agendamentos é sensível (gera trilha de acesso no backend).
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';
import type { ResultadoPaginado } from './paciente.api';

// --- Enums (espelham o domínio; o backend faz parse por código numérico) ---

/** TipoAtendimentoAgenda: 1 = Consulta, 2 = Exame. */
export type TipoAtendimentoAgenda = 1 | 2;

/** PrioridadeAgendamento: 1 = Eletiva, 2 = Prioritária, 3 = Urgente. */
export type PrioridadeAgendamento = 1 | 2 | 3;

/** OrigemCancelamento: 1 = Paciente, 2 = Unidade, 3 = Profissional. */
export type OrigemCancelamento = 1 | 2 | 3;

/** SituacaoAgendamento (nome do enum): Marcado, Confirmado, Cancelado, Falta, Realizado. */
export type SituacaoAgendamentoNome =
  | 'Marcado'
  | 'Confirmado'
  | 'Cancelado'
  | 'Falta'
  | 'Realizado';

/** SituacaoFilaEspera (nome do enum): Aguardando, Convocado, Atendido, Removido. */
export type SituacaoFilaEsperaNome = 'Aguardando' | 'Convocado' | 'Atendido' | 'Removido';

// --- DTOs ---

/** AgendamentoItemLista — item da lista de agendamentos (GET /agendamentos). */
export interface AgendamentoItemLista {
  id: string;
  pacienteId: string;
  profissionalId: string;
  estabelecimentoId: string;
  dataHora: string;
  tipo: string;
  prioridade: string;
  situacao: string;
}

/** MarcarAgendamentoCommand — ocupa uma vaga LIVRE para um paciente. */
export interface MarcarAgendamentoInput {
  pacienteId: string;
  vagaId: string;
  prioridade: PrioridadeAgendamento;
}

/** CancelarAgendamentoPayload(Motivo, Origem). */
export interface CancelarAgendamentoInput {
  motivo: string;
  origem: OrigemCancelamento;
}

/** RealizarAgendamentoPayload(AtendimentoId?) — ponte opcional para o PEP. */
export interface RealizarAgendamentoInput {
  atendimentoId?: string | null;
}

/** MotivoAgendaPayload(Motivo) — bloqueio de dia (agenda) e remoção da fila. */
export interface MotivoAgendaInput {
  motivo: string;
}

/** Filtros da busca de agendamentos (paginação 1-based; situacao = nome do enum). */
export interface AgendamentoBuscaFiltro {
  paciente?: string;
  profissional?: string;
  data?: string;
  situacao?: string;
  pagina: number;
  tamanho: number;
}

// --- Acesso HTTP ---

function texto(valor?: string | null): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarAgendamentos(
  filtro: AgendamentoBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<AgendamentoItemLista>> {
  return http.get<ResultadoPaginado<AgendamentoItemLista>>('/saude/agendamentos', {
    query: {
      paciente: texto(filtro.paciente),
      profissional: texto(filtro.profissional),
      data: texto(filtro.data),
      situacao: texto(filtro.situacao),
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

async function marcarAgendamento(input: MarcarAgendamentoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/agendamentos', input);
  return id;
}

function confirmarAgendamento(agendamentoId: string): Promise<void> {
  return http.post<void>(`/saude/agendamentos/${agendamentoId}/confirmacao`);
}

function cancelarAgendamento(agendamentoId: string, input: CancelarAgendamentoInput): Promise<void> {
  return http.post<void>(`/saude/agendamentos/${agendamentoId}/cancelamento`, input);
}

function registrarFalta(agendamentoId: string): Promise<void> {
  return http.post<void>(`/saude/agendamentos/${agendamentoId}/falta`);
}

function realizarAgendamento(agendamentoId: string, input: RealizarAgendamentoInput): Promise<void> {
  return http.post<void>(`/saude/agendamentos/${agendamentoId}/realizacao`, input);
}

// --- Hooks TanStack Query — QUERIES ---

/** Lista/busca paginada de agendamentos (sensível — gera trilha de acesso). */
export function useBuscarAgendamentos(filtro: AgendamentoBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.agendamentosBusca(filtro),
    queryFn: ({ signal }) => buscarAgendamentos(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Marca uma consulta/exame ocupando uma vaga livre. Invalida agendamentos e vagas. */
export function useMarcarAgendamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: marcarAgendamento,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendamentos() });
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendas() });
    },
  });
}

/** Confirma o agendamento (Marcado → Confirmado). Invalida agendamentos. */
export function useConfirmarAgendamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (agendamentoId: string) => confirmarAgendamento(agendamentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendamentos() });
    },
  });
}

/** Cancela o agendamento (libera a vaga) — terminal. Invalida agendamentos e vagas. */
export function useCancelarAgendamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { agendamentoId: string; input: CancelarAgendamentoInput }) =>
      cancelarAgendamento(args.agendamentoId, args.input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendamentos() });
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendas() });
    },
  });
}

/** Registra falta do paciente (libera a vaga) — terminal. Invalida agendamentos. */
export function useRegistrarFalta() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (agendamentoId: string) => registrarFalta(agendamentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendamentos() });
    },
  });
}

/** Marca o agendamento como realizado (ponte para o PEP) — terminal. */
export function useRealizarAgendamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { agendamentoId: string; input: RealizarAgendamentoInput }) =>
      realizarAgendamento(args.agendamentoId, args.input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendamentos() });
    },
  });
}
