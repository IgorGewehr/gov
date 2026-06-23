// Camada de API da estimativa local do IGD (modulo AssistenciaSocial).
// Contrato real (AssistenciaSocialEndpoints.cs):
//  GET /assistenciasocial/igd/estimativa?exercicio -> EstimarIgd (query -> EstimativaIgdDto)
//
// IMPORTANTE: e uma ESTIMATIVA GERENCIAL LOCAL (derivada de Familia/condicionalidades/beneficios),
// NAO o IGD-PBF/IGD-SUAS oficial do MDS/SAGI. O rotulo oficial vem do backend (campo `rotulo`).
import { useQuery } from '@tanstack/react-query';
import { http } from '../../../api/http';

/** Projecao da estimativa do IGD (EstimativaIgdDto). Cada fator e indice em [0,1]. */
export interface EstimativaIgd {
  exercicio: number;
  /** Indice estimado (0 a 1). */
  indice: number;
  /** Fator de atualizacao cadastral (0 a 1). */
  fatorAtualizacaoCadastral: number;
  /** Fator de cumprimento de condicionalidades (0 a 1). */
  fatorCondicionalidades: number;
  /** Fator de gestao de beneficios (0 a 1). */
  fatorGestaoBeneficios: number;
  /** Rotulo do backend deixando claro: estimativa local, nao oficial. */
  rotulo: string;
}

export const igdKeys = {
  all: ['assistenciasocial', 'igd'] as const,
  estimativa: (exercicio: number) => [...igdKeys.all, 'estimativa', exercicio] as const,
};

function obterEstimativa(exercicio: number, signal?: AbortSignal): Promise<EstimativaIgd> {
  const params = new URLSearchParams({ exercicio: String(exercicio) });
  return http.get<EstimativaIgd>(`/assistenciasocial/igd/estimativa?${params.toString()}`, {
    signal,
  });
}

/** Estima o IGD a partir de indicadores LOCAIS (estimativa gerencial, nao oficial). */
export function useEstimativaIgd(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: igdKeys.estimativa(exercicio),
    queryFn: ({ signal }) => obterEstimativa(exercicio, signal),
    enabled: enabled && exercicio >= 2000,
  });
}
