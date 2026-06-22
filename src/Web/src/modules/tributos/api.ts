// Camada de API do módulo Tributos (PADRÃO-OURO). Espelha FIELMENTE o contrato REAL
// de TributosEndpoints.cs (Minimal API /api/tributos):
//   POST /api/tributos/contribuintes/pessoa-fisica                 -> CadastrarContribuintePessoaFisica -> { id }            [tributos.gerenciar]
//   POST /api/tributos/lancamentos                                 -> LancarCredito                     -> { id }            [tributos.gerenciar]
//   POST /api/tributos/lancamentos/{lancamentoId}/inscrever-divida-ativa -> InscreverEmDividaAtiva       -> { dividaAtivaId } [tributos.gerenciar]
//   POST /api/tributos/dividas/{dividaAtivaId}/cda                 -> EmitirCda                          -> 204               [tributos.gerenciar]
//   GET  /api/tributos/contribuintes/{contribuinteId}/dividas-ativas -> ObterDividasAtivasDoContribuinte -> DividaAtivaResumo[] [tributos.ver]
//
// Convenções: DTOs no topo; query keys centralizadas para invalidação consistente;
// funções de acesso via http client tipado (Authorization + ProblemDetails);
// hooks TanStack Query (useQuery/useMutation) para CADA operação.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham os enums e projeções do domínio Tributos)
// ---------------------------------------------------------------------------

/** Situação da Dívida Ativa — enum SituacaoDividaAtiva (string no JSON). */
export type SituacaoDividaAtiva =
  | 'Inscrita'
  | 'CdaEmitida'
  | 'Protestada'
  | 'EmExecucaoFiscal'
  | 'Parcelada'
  | 'Quitada'
  | 'Cancelada';

/** Espécie tributária — enum TipoTributo (1..4). */
export type TipoTributo = 'Iptu' | 'Iss' | 'Itbi' | 'Taxa';

/** Valor numérico do enum TipoTributo esperado pelo backend (IsInEnum). */
export const TIPO_TRIBUTO_VALOR: Record<TipoTributo, number> = {
  Iptu: 1,
  Iss: 2,
  Itbi: 3,
  Taxa: 4,
};

/** Projeção de resumo (ObterDividasAtivasDoContribuinte). */
export interface DividaAtivaResumo {
  id: string;
  contribuinteId: string;
  valorInscrito: number;
  situacao: SituacaoDividaAtiva;
  dataInscricao: string;
  dataPrescricao: string;
  numeroCda: string | null;
}

// --- Entradas de comando (espelham os Commands/Payloads reais) ---

/** CadastrarContribuintePessoaFisicaCommand(Cpf, Nome, InscricaoMunicipal). */
export interface CadastrarContribuintePfInput {
  cpf: string;
  nome: string;
  inscricaoMunicipal?: string | null;
}

/** LancarCreditoCommand(ContribuinteId, TipoTributo, Ano, Mes, ValorPrincipal, Vencimento). */
export interface LancarCreditoInput {
  contribuinteId: string;
  tipoTributo: number;
  ano: number;
  mes: number;
  valorPrincipal: number;
  vencimento: string;
}

/** EmitirCdaPayload(NumeroCda). */
export interface EmitirCdaInput {
  numeroCda: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const tributosKeys = {
  all: ['tributos'] as const,
  dividas: () => [...tributosKeys.all, 'dividas-ativas'] as const,
  dividasPorContribuinte: (contribuinteId: string) =>
    [...tributosKeys.dividas(), 'contribuinte', contribuinteId] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarDividasPorContribuinte(
  contribuinteId: string,
  signal?: AbortSignal,
): Promise<DividaAtivaResumo[]> {
  return http.get<DividaAtivaResumo[]>(
    `/tributos/contribuintes/${contribuinteId}/dividas-ativas`,
    { signal },
  );
}

function cadastrarContribuintePf(input: CadastrarContribuintePfInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/contribuintes/pessoa-fisica', input);
}

function lancarCredito(input: LancarCreditoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/lancamentos', input);
}

function inscreverEmDividaAtiva(lancamentoId: string): Promise<{ dividaAtivaId: string }> {
  return http.post<{ dividaAtivaId: string }>(
    `/tributos/lancamentos/${lancamentoId}/inscrever-divida-ativa`,
  );
}

function emitirCda(dividaAtivaId: string, input: EmitirCdaInput): Promise<void> {
  return http.post<void>(`/tributos/dividas/${dividaAtivaId}/cda`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista as dívidas ativas de um contribuinte. `enabled` controla disparo sob demanda. */
export function useDividasPorContribuinte(contribuinteId: string, enabled = true) {
  return useQuery({
    queryKey: tributosKeys.dividasPorContribuinte(contribuinteId),
    queryFn: ({ signal }) => listarDividasPorContribuinte(contribuinteId, signal),
    enabled: enabled && contribuinteId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Cadastra um contribuinte pessoa física. */
export function useCadastrarContribuintePf() {
  return useMutation({ mutationFn: cadastrarContribuintePf });
}

/** Constitui (lança) um crédito tributário para um contribuinte. */
export function useLancarCredito() {
  return useMutation({ mutationFn: lancarCredito });
}

/** Inscreve um lançamento vencido em Dívida Ativa. Invalida as dívidas do contribuinte. */
export function useInscreverEmDividaAtiva(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (lancamentoId: string) => inscreverEmDividaAtiva(lancamentoId),
    onSuccess: () => {
      if (contribuinteIdParaInvalidar && contribuinteIdParaInvalidar.trim().length > 0) {
        queryClient.invalidateQueries({
          queryKey: tributosKeys.dividasPorContribuinte(contribuinteIdParaInvalidar),
        });
      } else {
        queryClient.invalidateQueries({ queryKey: tributosKeys.dividas() });
      }
    },
  });
}

/** Emite a Certidão de Dívida Ativa (CDA) de um título. Invalida as dívidas do contribuinte. */
export function useEmitirCda(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ dividaAtivaId, input }: { dividaAtivaId: string; input: EmitirCdaInput }) =>
      emitirCda(dividaAtivaId, input),
    onSuccess: () => {
      if (contribuinteIdParaInvalidar && contribuinteIdParaInvalidar.trim().length > 0) {
        queryClient.invalidateQueries({
          queryKey: tributosKeys.dividasPorContribuinte(contribuinteIdParaInvalidar),
        });
      } else {
        queryClient.invalidateQueries({ queryKey: tributosKeys.dividas() });
      }
    },
  });
}
