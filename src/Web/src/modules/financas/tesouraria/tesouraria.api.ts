// Camada de API da Tesouraria caixa-banco do módulo Finanças. Espelha o contrato REAL de
// FinancasEndpoints.cs -> MapearTesouraria:
//   POST /financas/tesouraria/contas                                  -> { id }
//   GET  /financas/tesouraria/contas                                  -> ContaFinanceiraResumo[]
//   GET  /financas/tesouraria/contas/{id}/extrato?de=&ate=            -> MovimentoFinanceiroLinha[]
//   POST /financas/tesouraria/contas/{id}/recebimentos               -> { id }
//   POST /financas/tesouraria/contas/{id}/pagamentos                  -> { id }
//   POST /financas/tesouraria/transferencias                          -> 204
//   POST /financas/tesouraria/contas/{id}/movimentos/{mid}/conciliar  -> 204
//   GET  /financas/tesouraria/boletim?data=                           -> BoletimCaixaBancoDto
//
// Enums são serializados como INTEIROS pelo System.Text.Json default; o comando de abertura
// envia o valor NUMÉRICO de TipoContaFinanceira (Bancaria=1, Caixa=2); os DTOs trazem
// Tipo/Situacao/Tipo-de-movimento como string.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

/** Espécie da conta da tesouraria (TipoContaFinanceira). */
export const TIPO_CONTA_FINANCEIRA = {
  Bancaria: 1,
  Caixa: 2,
} as const;

/** Data de hoje em ISO yyyy-mm-dd no fuso local (valor inicial de campos de data). */
export function hojeIso(): string {
  const agora = new Date();
  const ano = agora.getFullYear();
  const mes = String(agora.getMonth() + 1).padStart(2, '0');
  const dia = String(agora.getDate()).padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
}

export type TipoContaFinanceira =
  (typeof TIPO_CONTA_FINANCEIRA)[keyof typeof TIPO_CONTA_FINANCEIRA];

/** Resumo de uma conta da tesouraria (ContaFinanceiraResumo). */
export interface ContaFinanceira {
  id: string;
  nome: string;
  /** "Bancaria" | "Caixa". */
  tipo: string;
  banco: string | null;
  agencia: string | null;
  conta: string | null;
  saldoInicial: number;
  saldo: number;
  /** "Ativa" | "Encerrada". */
  situacao: string;
}

/** Linha do extrato (MovimentoFinanceiroLinha). */
export interface MovimentoFinanceiro {
  movimentoId: string;
  data: string;
  /** "Recebimento" | "Pagamento" | "TransferenciaSaida" | "TransferenciaEntrada". */
  tipo: string;
  valor: number;
  saldoApos: number;
  historico: string;
  documento: string | null;
  conciliado: boolean;
}

/** Linha do Boletim de Caixa/Banco por conta (LinhaBoletimContaDto). */
export interface LinhaBoletimConta {
  contaId: string;
  nome: string;
  tipo: string;
  saldoAnterior: number;
  recebimentos: number;
  pagamentos: number;
  saldoDia: number;
}

/** Boletim de Caixa/Banco consolidado de um dia (BoletimCaixaBancoDto). */
export interface BoletimCaixaBanco {
  data: string;
  contas: LinhaBoletimConta[];
  totalSaldoAnterior: number;
  totalRecebimentos: number;
  totalPagamentos: number;
  totalSaldoDia: number;
}

/** Comando de abertura de conta (AbrirContaFinanceiraCommand). */
export interface AbrirContaInput {
  nome: string;
  tipo: TipoContaFinanceira;
  banco?: string | null;
  agencia?: string | null;
  conta?: string | null;
  pix?: string | null;
  saldoInicial: number;
}

/** Comando de movimento simples (recebimento/pagamento). */
export interface MovimentoInput {
  data: string;
  valor: number;
  historico: string;
  documento?: string | null;
}

/** Comando de transferência (TransferirEntreContasCommand). */
export interface TransferenciaInput {
  contaOrigemId: string;
  contaDestinoId: string;
  data: string;
  valor: number;
  historico: string;
}

export const tesourariaKeys = {
  all: ['financas', 'tesouraria'] as const,
  contas: () => [...tesourariaKeys.all, 'contas'] as const,
  extrato: (contaId: string, de?: string, ate?: string) =>
    [...tesourariaKeys.all, 'extrato', contaId, de ?? '', ate ?? ''] as const,
  boletim: (data: string) => [...tesourariaKeys.all, 'boletim', data] as const,
};

function listarContas(signal?: AbortSignal): Promise<ContaFinanceira[]> {
  return http.get<ContaFinanceira[]>('/financas/tesouraria/contas', { signal });
}

function abrirConta(input: AbrirContaInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/financas/tesouraria/contas', input);
}

function consultarExtrato(
  contaId: string,
  de?: string,
  ate?: string,
  signal?: AbortSignal,
): Promise<MovimentoFinanceiro[]> {
  return http.get<MovimentoFinanceiro[]>(`/financas/tesouraria/contas/${contaId}/extrato`, {
    query: { de: de || undefined, ate: ate || undefined },
    signal,
  });
}

function registrarRecebimento(contaId: string, input: MovimentoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>(`/financas/tesouraria/contas/${contaId}/recebimentos`, input);
}

function registrarPagamento(contaId: string, input: MovimentoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>(`/financas/tesouraria/contas/${contaId}/pagamentos`, input);
}

function transferir(input: TransferenciaInput): Promise<void> {
  return http.post<void>('/financas/tesouraria/transferencias', input);
}

function conciliar(
  contaId: string,
  movimentoId: string,
  dataConciliacao: string,
): Promise<void> {
  return http.post<void>(
    `/financas/tesouraria/contas/${contaId}/movimentos/${movimentoId}/conciliar`,
    { dataConciliacao },
  );
}

function gerarBoletim(data: string, signal?: AbortSignal): Promise<BoletimCaixaBanco> {
  return http.get<BoletimCaixaBanco>('/financas/tesouraria/boletim', {
    query: { data },
    signal,
  });
}

/** Lista as contas da tesouraria com saldo corrente. */
export function useContasFinanceiras(enabled = true) {
  return useQuery({
    queryKey: tesourariaKeys.contas(),
    queryFn: ({ signal }) => listarContas(signal),
    enabled,
  });
}

/** Abre uma conta. Invalida a lista de contas. */
export function useAbrirConta() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirConta,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tesourariaKeys.contas() });
    },
  });
}

/** Extrato de uma conta no intervalo (sob demanda). */
export function useExtratoConta(contaId: string, de?: string, ate?: string, enabled = true) {
  return useQuery({
    queryKey: tesourariaKeys.extrato(contaId, de, ate),
    queryFn: ({ signal }) => consultarExtrato(contaId, de, ate, signal),
    enabled: enabled && contaId !== '',
  });
}

/** Registra um recebimento. Invalida contas e extrato. */
export function useRegistrarRecebimento(contaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MovimentoInput) => registrarRecebimento(contaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tesourariaKeys.all });
    },
  });
}

/** Registra um pagamento. Invalida contas e extrato. */
export function useRegistrarPagamento(contaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MovimentoInput) => registrarPagamento(contaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tesourariaKeys.all });
    },
  });
}

/** Transfere entre contas. Invalida toda a tesouraria. */
export function useTransferir() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: transferir,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tesourariaKeys.all });
    },
  });
}

/** Concilia manualmente um movimento. Invalida o extrato. */
export function useConciliarMovimento(contaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { movimentoId: string; dataConciliacao: string }) =>
      conciliar(contaId, input.movimentoId, input.dataConciliacao),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tesourariaKeys.all });
    },
  });
}

/** Gera o Boletim de Caixa/Banco de um dia (sob demanda). */
export function useBoletimCaixaBanco(data: string, enabled = true) {
  return useQuery({
    queryKey: tesourariaKeys.boletim(data),
    queryFn: ({ signal }) => gerarBoletim(data, signal),
    enabled: enabled && data !== '',
  });
}
