// Camada de API da gestao ARQUIVISTICA do Protocolo (CONARQ / e-ARQ v2 / W9.4):
// Plano de Classificacao, Tabela de Temporalidade (TTD) e Destinacao (eliminacao/
// guarda permanente). Segue o PADRAO-OURO do modulo (DTOs espelham os Commands reais;
// hooks TanStack Query por operacao).
//
// Contrato REAL (ProtocoloEndpoints.cs -> MapearTemporalidade):
//   POST /api/protocolo/planos-classificacao                              -> CadastrarPlanoClassificacao   -> { id }
//   POST /api/protocolo/tabelas-temporalidade                            -> CadastrarTabelaTemporalidade  -> { id }
//   POST /api/protocolo/destinacoes/avaliar-aptidao                      -> AvaliarAptidaoDestinacoes     -> { promovidas }
//   POST /api/protocolo/destinacoes/{id}/autorizacao-eliminacao          -> AutorizarEliminacao           -> 204
//   POST /api/protocolo/destinacoes/{id}/eliminacao                      -> RegistrarEliminacao           -> 204
//
// IMPORTANTE: o contrato NAO expoe GET de planos/TTD/fila de destinacoes nem o status
// do carimbo de tempo num DTO. Portanto NAO inventamos leitura — as telas trabalham
// sobre cadastro (POST) e acoes pontuais por identificador de ficha.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// Enums do dominio (Domain/Arquivistica/Enums.cs) — valores numericos do backend
// ---------------------------------------------------------------------------

/** Destinacao final (TTD/CONARQ) — enum Destinacao (1..2). */
export type Destinacao = 'Eliminacao' | 'GuardaPermanente';

/** Valor numerico esperado pelo backend (IsInEnum). */
export const DESTINACAO_VALOR: Record<Destinacao, number> = {
  Eliminacao: 1,
  GuardaPermanente: 2,
};

/** Evento base da contagem do prazo de guarda — enum EventoContagem (1..3). */
export type EventoContagem = 'DataAutuacao' | 'DataArquivamento' | 'AprovacaoContas';

/** Valor numerico esperado pelo backend (IsInEnum). */
export const EVENTO_CONTAGEM_VALOR: Record<EventoContagem, number> = {
  DataAutuacao: 1,
  DataArquivamento: 2,
  AprovacaoContas: 3,
};

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** ItemPlanoClassificacao(Codigo, Assunto, AtividadeFim, CodigoPai). */
export interface ItemPlanoClassificacaoInput {
  codigo: string;
  assunto: string;
  atividadeFim: boolean;
  codigoPai?: string | null;
}

/** CadastrarPlanoClassificacaoCommand(Nome, Classes). */
export interface CadastrarPlanoClassificacaoInput {
  nome: string;
  classes: ItemPlanoClassificacaoInput[];
}

/** ItemTabelaTemporalidade(CodigoClassificacao, Prazos, Destinacao, EventoContagem, Observacao). */
export interface ItemTabelaTemporalidadeInput {
  codigoClassificacao: string;
  prazoGuardaCorrenteAnos: number;
  prazoGuardaIntermediariaAnos: number;
  destinacao: number;
  eventoContagem: number;
  observacao?: string | null;
}

/** CadastrarTabelaTemporalidadeCommand(Nome, Regras). */
export interface CadastrarTabelaTemporalidadeInput {
  nome: string;
  regras: ItemTabelaTemporalidadeInput[];
}

/** AutorizarEliminacaoPayload(AutorizadoPor). */
export interface AutorizarEliminacaoInput {
  autorizadoPor: string;
}

/** RegistrarEliminacaoPayload(TermoEliminacaoHash, EditalEliminacaoRef). */
export interface RegistrarEliminacaoInput {
  termoEliminacaoHash: string;
  editalEliminacaoRef: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function cadastrarPlanoClassificacao(
  input: CadastrarPlanoClassificacaoInput,
): Promise<{ id: string }> {
  return http.post<{ id: string }>('/protocolo/planos-classificacao', input);
}

function cadastrarTabelaTemporalidade(
  input: CadastrarTabelaTemporalidadeInput,
): Promise<{ id: string }> {
  return http.post<{ id: string }>('/protocolo/tabelas-temporalidade', input);
}

function avaliarAptidaoDestinacoes(): Promise<{ promovidas: number }> {
  return http.post<{ promovidas: number }>('/protocolo/destinacoes/avaliar-aptidao', {});
}

function autorizarEliminacao(destinacaoId: string, input: AutorizarEliminacaoInput): Promise<void> {
  return http.post<void>(`/protocolo/destinacoes/${destinacaoId}/autorizacao-eliminacao`, input);
}

function registrarEliminacao(destinacaoId: string, input: RegistrarEliminacaoInput): Promise<void> {
  return http.post<void>(`/protocolo/destinacoes/${destinacaoId}/eliminacao`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Cadastra o Plano de Classificacao documental do tenant (e-ARQ v2 / CONARQ). */
export function useCadastrarPlanoClassificacao() {
  return useMutation({ mutationFn: cadastrarPlanoClassificacao });
}

/** Cadastra a Tabela de Temporalidade e Destinacao (TTD) do tenant. */
export function useCadastrarTabelaTemporalidade() {
  return useMutation({ mutationFn: cadastrarTabelaTemporalidade });
}

/**
 * Varre as fichas em AguardandoPrazo e promove para AptoEliminar as cujo prazo de
 * guarda ja decorreu. Devolve quantas fichas foram promovidas.
 */
export function useAvaliarAptidaoDestinacoes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: avaliarAptidaoDestinacoes,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['protocolo', 'destinacoes'] }),
  });
}

/** Autoriza a eliminacao de uma ficha apta por ato humano (RBAC). I-T1/I-T2 protegem o irreversivel. */
export function useAutorizarEliminacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ destinacaoId, input }: { destinacaoId: string; input: AutorizarEliminacaoInput }) =>
      autorizarEliminacao(destinacaoId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['protocolo', 'destinacoes'] }),
  });
}

/** Registra a eliminacao efetiva com termo carimbado (hash) + edital sob WORM (I-T4). */
export function useRegistrarEliminacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ destinacaoId, input }: { destinacaoId: string; input: RegistrarEliminacaoInput }) =>
      registrarEliminacao(destinacaoId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['protocolo', 'destinacoes'] }),
  });
}
