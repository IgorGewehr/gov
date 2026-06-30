// API de busca de Contribuintes para o picker do balcão (P1): em vez de exigir o GUID
// digitado, oferece busca textual por nome ou CPF/CNPJ (endpoint real GET
// /tributos/contribuintes?termo=). O documento vem MASCARADO (LGPD) do backend.
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';

/** Resumo do contribuinte para o picker. `documentoMascarado` já vem ocultado pelo backend. */
export interface ContribuinteResumo {
  id: string;
  nome: string;
  tipoPessoa: string;
  documentoMascarado: string;
  inscricaoMunicipal: string | null;
}

/** Query keys da busca de contribuintes (fonte única para cache/invalidação). */
export const contribuinteKeys = {
  all: ['tributos', 'contribuintes'] as const,
  porTermo: (termo: string) => [...contribuinteKeys.all, 'busca', termo] as const,
};

function listarContribuintes(termo: string, signal?: AbortSignal): Promise<ContribuinteResumo[]> {
  const query = termo.trim().length > 0 ? `?termo=${encodeURIComponent(termo.trim())}` : '';
  return http.get<ContribuinteResumo[]>(`/tributos/contribuintes${query}`, { signal });
}

/** Busca contribuintes do tenant por nome ou CPF/CNPJ (picker do balcão). */
export function useContribuintes(termo: string, enabled = true) {
  return useQuery({
    queryKey: contribuinteKeys.porTermo(termo),
    queryFn: ({ signal }) => listarContribuintes(termo, signal),
    enabled,
  });
}
