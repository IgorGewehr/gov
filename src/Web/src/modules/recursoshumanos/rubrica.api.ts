// Camada de API da entidade RubricaFolha (verbas parametrizáveis da folha — S-1010).
// Segue o PADRÃO-OURO: DTOs no topo, funções de acesso via http client tipado, hooks Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
//   POST /recursoshumanos/rubricas            -> cria rubrica (recursoshumanos.gerenciar)
//   GET  /recursoshumanos/rubricas/vigentes   -> lista vigentes na competência (recursoshumanos.ver)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Natureza (classe) de uma rubrica parametrizada (S-1010). */
export type NaturezaRubrica = 'Provento' | 'Desconto' | 'Informativa' | 'InformativaDedutora';

/** Projeção de leitura de uma rubrica vigente. */
export interface RubricaResumo {
  id: string;
  codigo: string;
  descricao: string;
  natureza: string;
  incideInss: boolean;
  incideRpps: boolean;
  incideIrrf: boolean;
  incideFgts: boolean;
}

/** Entrada da criação de uma rubrica parametrizável (valor fixo OU percentual). */
export interface CriarRubricaInput {
  codigo: string;
  descricao: string;
  /** 1 = Provento, 2 = Desconto, 3 = Informativa, 4 = InformativaDedutora (enum numérico). */
  natureza: number;
  anoVigencia: number;
  mesVigencia: number;
  incideInss: boolean;
  incideRpps: boolean;
  incideIrrf: boolean;
  incideFgts: boolean;
  /** Valor fixo em R$ (exclusivo com percentual). */
  valorFixo?: number | null;
  /** Percentual sobre a base, em fração decimal 0..1 (exclusivo com valor fixo). */
  percentual?: number | null;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarRubricasVigentes(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<RubricaResumo[]> {
  return http.get<RubricaResumo[]>('/recursoshumanos/rubricas/vigentes', {
    signal,
    query: { ano, mes },
  });
}

function criarRubrica(input: CriarRubricaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/rubricas', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as rubricas vigentes na competência. `enabled` controla disparo sob demanda. */
export function useRubricasVigentes(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.rubricasVigentes(ano, mes),
    queryFn: ({ signal }) => listarRubricasVigentes(ano, mes, signal),
    enabled,
  });
}

/** Cria uma rubrica parametrizável e invalida as consultas de rubrica. */
export function useCriarRubrica() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarRubrica,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.rubricas() });
    },
  });
}
