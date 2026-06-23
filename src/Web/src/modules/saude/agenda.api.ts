// Camada de API da GRADE de disponibilidade (agenda do profissional/UBS) e das vagas
// livres (slots). DTOs + acesso HTTP + hooks TanStack. Separado de agendamento.api.ts
// (marcação) para manter cada arquivo < 300 linhas.
// Contrato REAL (SaudeEndpoints.cs — grupo /api/saude; o http client já prefixa /api):
//   POST /saude/agendas                            -> AbrirAgenda        -> { id } (saude.agenda.gerenciar)
//   GET  /saude/agendas/{agendaId}                 -> ObterAgendaPorId   -> AgendaDetalhe (saude.agenda.ver)
//   POST /saude/agendas/{agendaId}/publicacao      -> PublicarAgenda     -> 204    (saude.agenda.gerenciar)
//   POST /saude/agendas/{agendaId}/bloqueio {motivo}-> BloquearDiaAgenda -> 204    (saude.agenda.gerenciar)
//   POST /saude/agendas/{agendaId}/reabertura      -> ReabrirDiaAgenda   -> 204    (saude.agenda.gerenciar)
//   GET  /saude/agendas/vagas?profissional&estabelecimento&de&ate&tipo
//                                                  -> BuscarVagasLivres  -> VagaLivreItem[] (saude.agenda.ver)
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';
import type { MotivoAgendaInput, TipoAtendimentoAgenda } from './agendamento.api';

// --- DTOs ---

/** VagaLivreItem — slot disponível para marcação (GET /agendas/vagas). */
export interface VagaLivreItem {
  agendaId: string;
  vagaId: string;
  profissionalId: string;
  estabelecimentoId: string;
  tipo: string;
  dataHora: string;
}

/** AgendaDetalhe — ficha da grade de disponibilidade (GET /agendas/{id}). */
export interface AgendaDetalhe {
  id: string;
  profissionalId: string;
  estabelecimentoId: string;
  tipo: string;
  data: string;
  horaInicio: string;
  horaFim: string;
  duracaoSlotMinutos: number;
  capacidadeVagas: number;
  situacao: string;
  totalVagas: number;
  vagasLivres: number;
}

/** AbrirAgendaCommand — abre uma grade (em rascunho) para um profissional/CNES. */
export interface AbrirAgendaInput {
  profissionalId: string;
  estabelecimentoId: string;
  tipo: TipoAtendimentoAgenda;
  data: string; // ISO yyyy-mm-dd (DateOnly)
  horaInicio: string; // HH:mm (TimeOnly)
  horaFim: string; // HH:mm (TimeOnly)
  duracaoSlotMinutos: number;
  capacidadeVagas: number;
}

/** Filtros da busca de vagas livres (todos opcionais; datas em ISO yyyy-mm-dd). */
export interface VagasLivresFiltro {
  profissional?: string;
  estabelecimento?: string;
  de?: string;
  ate?: string;
  tipo?: TipoAtendimentoAgenda;
}

// --- Acesso HTTP ---

function texto(valor?: string | null): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarVagasLivres(
  filtro: VagasLivresFiltro,
  signal?: AbortSignal,
): Promise<VagaLivreItem[]> {
  return http.get<VagaLivreItem[]>('/saude/agendas/vagas', {
    query: {
      profissional: texto(filtro.profissional),
      estabelecimento: texto(filtro.estabelecimento),
      de: texto(filtro.de),
      ate: texto(filtro.ate),
      tipo: filtro.tipo ?? undefined,
    },
    signal,
  });
}

function obterAgenda(agendaId: string, signal?: AbortSignal): Promise<AgendaDetalhe> {
  return http.get<AgendaDetalhe>(`/saude/agendas/${agendaId}`, { signal });
}

async function abrirAgenda(input: AbrirAgendaInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/agendas', input);
  return id;
}

function publicarAgenda(agendaId: string): Promise<void> {
  return http.post<void>(`/saude/agendas/${agendaId}/publicacao`);
}

function bloquearDiaAgenda(agendaId: string, input: MotivoAgendaInput): Promise<void> {
  return http.post<void>(`/saude/agendas/${agendaId}/bloqueio`, input);
}

function reabrirDiaAgenda(agendaId: string): Promise<void> {
  return http.post<void>(`/saude/agendas/${agendaId}/reabertura`);
}

// --- Hooks TanStack Query — QUERIES ---

/** Lista as vagas LIVRES por profissional/estabelecimento/tipo numa janela de datas. */
export function useVagasLivres(filtro: VagasLivresFiltro, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.vagasLivres(filtro),
    queryFn: ({ signal }) => buscarVagasLivres(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Ficha de uma grade de disponibilidade (contagem de vagas). */
export function useAgenda(agendaId: string) {
  return useQuery({
    queryKey: saudeKeys.agenda(agendaId),
    queryFn: ({ signal }) => obterAgenda(agendaId, signal),
    enabled: agendaId.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Abre uma grade de disponibilidade (rascunho). Invalida agendas/vagas. */
export function useAbrirAgenda() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirAgenda,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendas() });
    },
  });
}

/** Publica a grade (rascunho → aberta), liberando as vagas para marcação. */
export function usePublicarAgenda(agendaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => publicarAgenda(agendaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendas() });
    },
  });
}

/** Bloqueia integralmente o dia da grade. Invalida agendas/vagas. */
export function useBloquearDiaAgenda(agendaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MotivoAgendaInput) => bloquearDiaAgenda(agendaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendas() });
    },
  });
}

/** Reabre o dia bloqueado da grade. Invalida agendas/vagas. */
export function useReabrirDiaAgenda(agendaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reabrirDiaAgenda(agendaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.agendas() });
    },
  });
}
