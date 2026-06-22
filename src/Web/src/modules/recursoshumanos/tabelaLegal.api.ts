// Camada de API das Tabelas Legais (INSS/IRRF federais + RPPS municipal) do módulo
// RecursosHumanos. Segue o PADRÃO-OURO: DTOs no topo, funções de acesso via http client
// tipado, hooks de mutation. Não há GET (apenas semeadura/cadastro — escrita).
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
//   POST /recursoshumanos/tabelas-legais/semear-federais -> semeia INSS/IRRF oficiais
//   POST /recursoshumanos/tabelas-legais/rpps            -> cadastra RPPS municipal (lei do ente)
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Faixa de uma tabela progressiva (limites em R$, alíquota em fração decimal). */
export interface FaixaProgressivaInput {
  limiteInferior: number;
  limiteSuperior: number;
  /** Alíquota em fração decimal (ex.: 0.14 para 14%). */
  aliquota: number;
}

/** Entrada do cadastro da tabela RPPS municipal (NUNCA default federal — fail-closed). */
export interface CriarTabelaRppsInput {
  anoVigencia: number;
  mesVigencia: number;
  faixas: FaixaProgressivaInput[];
  /** Teto da base (opcional). */
  teto?: number | null;
  /** Lei previdenciária municipal (obrigatória). */
  baseLegal: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function semearTabelasFederais(): Promise<void> {
  return http.post<void>('/recursoshumanos/tabelas-legais/semear-federais');
}

function criarTabelaRpps(input: CriarTabelaRppsInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/tabelas-legais/rpps', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Semeia as tabelas federais oficiais (INSS/IRRF) do tenant e invalida o módulo. */
export function useSemearTabelasFederais() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: semearTabelasFederais,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.all });
    },
  });
}

/** Cadastra a tabela RPPS municipal (lei do ente) e invalida o módulo. */
export function useCriarTabelaRpps() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarTabelaRpps,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.all });
    },
  });
}
