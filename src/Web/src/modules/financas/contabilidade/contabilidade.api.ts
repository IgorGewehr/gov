// Camada de API da Contabilidade (PCASP/MCASP) do módulo Finanças. Espelha o contrato
// REAL de FinancasEndpoints.cs -> MapearContabilidade:
//   POST /financas/contabilidade/plano-de-contas/semear          -> { criadas }
//   GET  /financas/contabilidade/plano-de-contas                 -> ContaContabilDto[]
//   GET  /financas/contabilidade/balancete?exercicio=&mes=       -> LinhaBalanceteDto[]
//   GET  /financas/contabilidade/contas/{contaId}/razao?exercicio= -> LinhaBalanceteDto[]
//   POST /financas/contabilidade/lancamentos                     -> Guid (lançamento manual)
//
// Enums são serializados como INTEIROS pelo System.Text.Json default no ApiHost; por
// isso o comando de lançamento envia o valor NUMÉRICO de LadoPartida (Debito=1, Credito=2)
// e os DTOs de leitura trazem natureza/tipo como string.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// Enums do domínio (valor numérico = enum do backend)
// ---------------------------------------------------------------------------

/** Lado da partida contábil (LadoPartida). */
export const LADO_PARTIDA = {
  Debito: 1,
  Credito: 2,
} as const;

export type LadoPartida = (typeof LADO_PARTIDA)[keyof typeof LADO_PARTIDA];

// ---------------------------------------------------------------------------
// DTOs (contrato REAL)
// ---------------------------------------------------------------------------

/** Conta do plano de contas (ContaContabilDto). */
export interface ContaContabil {
  id: string;
  codigo: string;
  titulo: string;
  naturezaInformacao: string;
  naturezaSaldo: string;
  /** Tipo da conta: "Sintetica" | "Analitica". */
  tipo: string;
  nivel: number;
  ativa: boolean;
}

/** Linha de balancete / razão (LinhaBalanceteDto). */
export interface LinhaBalancete {
  contaId: string;
  codigoConta: string;
  titulo: string;
  naturezaSaldo: string;
  naturezaInformacao: string;
  saldoAnterior: number;
  totalDebitos: number;
  totalCreditos: number;
  saldoAtual: number;
}

/** Partida de um lançamento manual (LinhaManual). */
export interface LinhaManual {
  codigoConta: string;
  lado: LadoPartida;
  valor: number;
}

/** Comando de lançamento manual (RegistrarLancamentoManualCommand). */
export interface LancamentoManualInput {
  /** Data do lançamento em ISO yyyy-mm-dd (DateOnly no backend). */
  data: string;
  historico: string;
  linhas: LinhaManual[];
}

// ---------------------------------------------------------------------------
// Query keys
// ---------------------------------------------------------------------------

export const contabilidadeKeys = {
  all: ['financas', 'contabilidade'] as const,
  planoDeContas: () => [...contabilidadeKeys.all, 'plano-de-contas'] as const,
  balancete: (exercicio: number, mes: number) =>
    [...contabilidadeKeys.all, 'balancete', exercicio, mes] as const,
  razao: (contaId: string, exercicio: number) =>
    [...contabilidadeKeys.all, 'razao', contaId, exercicio] as const,
  lancamentos: () => [...contabilidadeKeys.all, 'lancamentos'] as const,
};

// ---------------------------------------------------------------------------
// Funções de acesso
// ---------------------------------------------------------------------------

function listarPlanoDeContas(signal?: AbortSignal): Promise<ContaContabil[]> {
  return http.get<ContaContabil[]>('/financas/contabilidade/plano-de-contas', { signal });
}

function semearPlanoDeContas(): Promise<{ criadas: number }> {
  return http.post<{ criadas: number }>('/financas/contabilidade/plano-de-contas/semear');
}

function consultarBalancete(
  exercicio: number,
  mes: number,
  signal?: AbortSignal,
): Promise<LinhaBalancete[]> {
  return http.get<LinhaBalancete[]>('/financas/contabilidade/balancete', {
    query: { exercicio, mes },
    signal,
  });
}

function consultarRazao(
  contaId: string,
  exercicio: number,
  signal?: AbortSignal,
): Promise<LinhaBalancete[]> {
  return http.get<LinhaBalancete[]>(`/financas/contabilidade/contas/${contaId}/razao`, {
    query: { exercicio },
    signal,
  });
}

function registrarLancamentoManual(input: LancamentoManualInput): Promise<string> {
  return http.post<string>('/financas/contabilidade/lancamentos', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista todo o plano de contas do tenant. */
export function usePlanoDeContas(enabled = true) {
  return useQuery({
    queryKey: contabilidadeKeys.planoDeContas(),
    queryFn: ({ signal }) => listarPlanoDeContas(signal),
    enabled,
  });
}

/** Semeia o plano de contas padrão (PCASP). Invalida o plano em caso de sucesso. */
export function useSemearPlanoDeContas() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: semearPlanoDeContas,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contabilidadeKeys.planoDeContas() });
    },
  });
}

/** Consulta o balancete de um período (exercício + mês), sob demanda. */
export function useBalancete(exercicio: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: contabilidadeKeys.balancete(exercicio, mes),
    queryFn: ({ signal }) => consultarBalancete(exercicio, mes, signal),
    enabled:
      enabled &&
      Number.isInteger(exercicio) &&
      exercicio >= 2000 &&
      Number.isInteger(mes) &&
      mes >= 1 &&
      mes <= 12,
  });
}

/** Consulta o razão (linhas mensais) de uma conta num exercício, sob demanda. */
export function useRazaoConta(contaId: string, exercicio: number, enabled = true) {
  return useQuery({
    queryKey: contabilidadeKeys.razao(contaId, exercicio),
    queryFn: ({ signal }) => consultarRazao(contaId, exercicio, signal),
    enabled: enabled && contaId !== '' && Number.isInteger(exercicio) && exercicio >= 2000,
  });
}

/** Registra um lançamento contábil manual. Invalida balancetes/razões. */
export function useRegistrarLancamentoManual() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registrarLancamentoManual,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contabilidadeKeys.all });
    },
  });
}
