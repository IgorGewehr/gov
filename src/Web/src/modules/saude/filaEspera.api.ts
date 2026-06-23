// Camada de API da Fila de Espera (módulo Saúde) — pacientes aguardando vaga. DTOs +
// acesso HTTP + hooks TanStack. Separado de agendamento.api.ts para manter < 300 linhas.
// Contrato REAL (SaudeEndpoints.cs — grupo /api/saude; o http client já prefixa /api):
//   POST /saude/fila-espera                          -> EntrarNaFilaDeEspera   -> { id } (saude.agenda.marcar)
//   GET  /saude/fila-espera?estabelecimento&situacao&pagina&tamanho
//                                                    -> BuscarFilaDeEspera     -> ResultadoPaginado<FilaEsperaItem> (saude.agenda.ver)
//   POST /saude/fila-espera/{filaId}/convocacao      -> ConvocarDaFilaDeEspera -> 204    (saude.agenda.marcar)
//   POST /saude/fila-espera/{filaId}/remocao {motivo}-> RemoverDaFilaDeEspera  -> 204    (saude.agenda.marcar)
// LGPD: a busca da fila é sensível (gera trilha de acesso no backend).
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';
import type { ResultadoPaginado } from './paciente.api';
import type { MotivoAgendaInput, PrioridadeAgendamento, TipoAtendimentoAgenda } from './agendamento.api';

// --- DTOs ---

/** FilaEsperaItem — item da fila de espera (GET /fila-espera). */
export interface FilaEsperaItem {
  id: string;
  pacienteId: string;
  estabelecimentoId: string;
  profissionalId: string | null;
  especialidade: string | null;
  tipo: string;
  prioridade: string;
  dataEntrada: string;
  situacao: string;
}

/** EntrarNaFilaDeEsperaCommand — exige profissional OU especialidade (CBO). */
export interface EntrarNaFilaInput {
  pacienteId: string;
  estabelecimentoId: string;
  profissionalId?: string | null;
  especialidade?: string | null;
  tipo: TipoAtendimentoAgenda;
  prioridade: PrioridadeAgendamento;
}

/** Filtros da busca da fila de espera (paginação 1-based; situacao = nome do enum). */
export interface FilaBuscaFiltro {
  estabelecimento?: string;
  situacao?: string;
  pagina: number;
  tamanho: number;
}

// --- Acesso HTTP ---

function texto(valor?: string | null): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarFilaDeEspera(
  filtro: FilaBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<FilaEsperaItem>> {
  return http.get<ResultadoPaginado<FilaEsperaItem>>('/saude/fila-espera', {
    query: {
      estabelecimento: texto(filtro.estabelecimento),
      situacao: texto(filtro.situacao),
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

async function entrarNaFila(input: EntrarNaFilaInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/fila-espera', input);
  return id;
}

function convocarDaFila(filaId: string): Promise<void> {
  return http.post<void>(`/saude/fila-espera/${filaId}/convocacao`);
}

function removerDaFila(filaId: string, input: MotivoAgendaInput): Promise<void> {
  return http.post<void>(`/saude/fila-espera/${filaId}/remocao`, input);
}

// --- Hooks TanStack Query ---

/** Lista/busca paginada da fila de espera (ordem de convocação). */
export function useBuscarFilaDeEspera(filtro: FilaBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.filaEsperaBusca(filtro),
    queryFn: ({ signal }) => buscarFilaDeEspera(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Inclui um paciente na fila de espera (sem vaga). Invalida a fila. */
export function useEntrarNaFila() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: entrarNaFila,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.filaEspera() });
    },
  });
}

/** Convoca explicitamente uma entrada da fila (Aguardando → Convocado). */
export function useConvocarDaFila() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (filaId: string) => convocarDaFila(filaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.filaEspera() });
    },
  });
}

/** Remove uma entrada da fila de espera (desistência/obsoleto) — terminal. */
export function useRemoverDaFila() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { filaId: string; input: MotivoAgendaInput }) =>
      removerDaFila(args.filaId, args.input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.filaEspera() });
    },
  });
}
