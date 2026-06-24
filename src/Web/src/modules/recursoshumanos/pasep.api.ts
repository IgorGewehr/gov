// Camada de API do PASEP (módulo RecursosHumanos). Apuração da base (folha bruta da competência)
// x alíquota parametrizável; transmissão/recolhimento real = M10.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.Pessoal.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situação (ciclo de vida) de uma apuração de PASEP. */
export type SituacaoApuracaoPasep = 'Apurada' | 'Transmitida';

/** Resumo de uma apuração de PASEP. */
export interface ApuracaoPasepResumo {
  id: string;
  ano: number;
  mes: number;
  baseContribuicao: number;
  aliquota: number;
  valor: number;
  situacao: SituacaoApuracaoPasep;
}

/** Entrada da apuração do PASEP de uma competência. */
export interface ApurarPasepInput {
  ano: number;
  mes: number;
  /** Alíquota (percentual) sobrescrita; ausente usa a parametrizada do tenant (1% padrão). */
  aliquotaOverride?: number | null;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarApuracoesPasep(ano: number, signal?: AbortSignal): Promise<ApuracaoPasepResumo[]> {
  return http.get<ApuracaoPasepResumo[]>('/recursoshumanos/pasep/apuracoes', {
    signal,
    query: { ano },
  });
}

function apurarPasep(input: ApurarPasepInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/pasep/apuracoes', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as apurações de PASEP de um ano. */
export function useApuracoesPasep(ano: number) {
  return useQuery({
    queryKey: rhKeys.pasepPorAno(ano),
    queryFn: ({ signal }) => listarApuracoesPasep(ano, signal),
  });
}

/** Apura o PASEP de uma competência e invalida o painel do PASEP. */
export function useApurarPasep() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: apurarPasep,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.pasep() });
    },
  });
}
