// Camada de API das Demonstrações Contábeis (DCASP) do módulo Finanças. Espelha o
// contrato REAL de FinancasEndpoints.cs -> MapearMscEDemonstracoes (todas financas.ver):
//   GET /financas/contabilidade/demonstracoes/balanco-orcamentario?exercicio=&mes=  -> BalancoOrcamentarioDto
//   GET /financas/contabilidade/demonstracoes/balanco-financeiro?exercicio=&mes=     -> BalancoFinanceiroDto
//   GET /financas/contabilidade/demonstracoes/balanco-patrimonial?exercicio=&mes=    -> BalancoPatrimonialDto
//   GET /financas/contabilidade/demonstracoes/variacoes-patrimoniais?exercicio=&mes= -> DemonstracaoVariacoesPatrimoniaisDto
//
// São read-models derivados do balancete do período. Os valores chegam como number
// (decimal serializado). Cada demonstrativo tem um total/equilíbrio próprio (resultado
// orçamentário, total ingressos×dispêndios, superávit financeiro, resultado patrimonial).
import { useQuery } from '@tanstack/react-query';
import { http } from '../../../api/http';
import { contabilidadeKeys } from './contabilidade.api';

// ---------------------------------------------------------------------------
// Tipos do demonstrativo (genéricos)
// ---------------------------------------------------------------------------

/** Demonstrativo disponível (slug da rota da API). */
export type Demonstrativo =
  | 'balanco-orcamentario'
  | 'balanco-financeiro'
  | 'balanco-patrimonial'
  | 'variacoes-patrimoniais';

/** Linha genérica de um quadro (LinhaDemonstrativoDto): rótulo + valores por coluna. */
export interface LinhaDemonstrativo {
  linha: string;
  valores: number[];
}

/** Quadro de um demonstrativo (QuadroDemonstrativoDto): título + colunas + linhas. */
export interface QuadroDemonstrativo {
  quadro: string;
  colunas: string[];
  linhas: LinhaDemonstrativo[];
}

// ---------------------------------------------------------------------------
// DTOs específicos (contrato REAL)
// ---------------------------------------------------------------------------

/** Balanço Orçamentário (Anexo 12). */
export interface BalancoOrcamentario {
  exercicio: number;
  mes: number;
  receitas: QuadroDemonstrativo;
  despesas: QuadroDemonstrativo;
  totalReceitaRealizada: number;
  totalDespesaEmpenhada: number;
  /** Receita realizada − despesa empenhada (déficit se negativo). */
  resultadoOrcamentario: number;
}

/** Balanço Financeiro (Anexo 13). */
export interface BalancoFinanceiro {
  exercicio: number;
  mes: number;
  ingressos: LinhaDemonstrativo[];
  dispendios: LinhaDemonstrativo[];
  totalIngressos: number;
  totalDispendios: number;
}

/** Balanço Patrimonial (Anexo 14). */
export interface BalancoPatrimonial {
  exercicio: number;
  mes: number;
  ativo: LinhaDemonstrativo[];
  passivoPatrimonioLiquido: LinhaDemonstrativo[];
  totalAtivo: number;
  totalPassivoPl: number;
  ativoFinanceiro: number;
  passivoFinanceiro: number;
  /** Ativo financeiro − passivo financeiro (art. 105 Lei 4.320/64). */
  superavitFinanceiro: number;
}

/** Demonstração das Variações Patrimoniais (Anexo 15). */
export interface VariacoesPatrimoniais {
  exercicio: number;
  mes: number;
  variacoesAumentativas: LinhaDemonstrativo[];
  variacoesDiminutivas: LinhaDemonstrativo[];
  totalVpa: number;
  totalVpd: number;
  /** VPA − VPD. */
  resultadoPatrimonial: number;
}

// ---------------------------------------------------------------------------
// Query keys
// ---------------------------------------------------------------------------

export const demonstracoesKeys = {
  all: [...contabilidadeKeys.all, 'demonstracoes'] as const,
  um: (demonstrativo: Demonstrativo, exercicio: number, mes: number) =>
    [...demonstracoesKeys.all, demonstrativo, exercicio, mes] as const,
};

// ---------------------------------------------------------------------------
// Funções de acesso
// ---------------------------------------------------------------------------

function obterBalancoOrcamentario(
  exercicio: number,
  mes: number,
  signal?: AbortSignal,
): Promise<BalancoOrcamentario> {
  return http.get<BalancoOrcamentario>('/financas/contabilidade/demonstracoes/balanco-orcamentario', {
    query: { exercicio, mes },
    signal,
  });
}

function obterBalancoFinanceiro(
  exercicio: number,
  mes: number,
  signal?: AbortSignal,
): Promise<BalancoFinanceiro> {
  return http.get<BalancoFinanceiro>('/financas/contabilidade/demonstracoes/balanco-financeiro', {
    query: { exercicio, mes },
    signal,
  });
}

function obterBalancoPatrimonial(
  exercicio: number,
  mes: number,
  signal?: AbortSignal,
): Promise<BalancoPatrimonial> {
  return http.get<BalancoPatrimonial>('/financas/contabilidade/demonstracoes/balanco-patrimonial', {
    query: { exercicio, mes },
    signal,
  });
}

function obterVariacoesPatrimoniais(
  exercicio: number,
  mes: number,
  signal?: AbortSignal,
): Promise<VariacoesPatrimoniais> {
  return http.get<VariacoesPatrimoniais>(
    '/financas/contabilidade/demonstracoes/variacoes-patrimoniais',
    { query: { exercicio, mes }, signal },
  );
}

/** Habilita a consulta apenas com período válido (mesma regra do balancete). */
function periodoValido(exercicio: number, mes: number): boolean {
  return (
    Number.isInteger(exercicio) &&
    exercicio >= 2000 &&
    Number.isInteger(mes) &&
    mes >= 1 &&
    mes <= 12
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query (um por demonstrativo, sob demanda)
// ---------------------------------------------------------------------------

/** Balanço Orçamentário do período (Anexo 12). */
export function useBalancoOrcamentario(exercicio: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: demonstracoesKeys.um('balanco-orcamentario', exercicio, mes),
    queryFn: ({ signal }) => obterBalancoOrcamentario(exercicio, mes, signal),
    enabled: enabled && periodoValido(exercicio, mes),
  });
}

/** Balanço Financeiro do período (Anexo 13). */
export function useBalancoFinanceiro(exercicio: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: demonstracoesKeys.um('balanco-financeiro', exercicio, mes),
    queryFn: ({ signal }) => obterBalancoFinanceiro(exercicio, mes, signal),
    enabled: enabled && periodoValido(exercicio, mes),
  });
}

/** Balanço Patrimonial do período (Anexo 14). */
export function useBalancoPatrimonial(exercicio: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: demonstracoesKeys.um('balanco-patrimonial', exercicio, mes),
    queryFn: ({ signal }) => obterBalancoPatrimonial(exercicio, mes, signal),
    enabled: enabled && periodoValido(exercicio, mes),
  });
}

/** Demonstração das Variações Patrimoniais do período (Anexo 15). */
export function useVariacoesPatrimoniais(exercicio: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: demonstracoesKeys.um('variacoes-patrimoniais', exercicio, mes),
    queryFn: ({ signal }) => obterVariacoesPatrimoniais(exercicio, mes, signal),
    enabled: enabled && periodoValido(exercicio, mes),
  });
}
