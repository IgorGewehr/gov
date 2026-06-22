// Camada de API do submódulo ITBI (Tributos M6). Espelha o contrato REAL provado
// da Minimal API /api/tributos:
//   POST /api/tributos/itbi/aliquotas                                          -> ConfigurarAliquotasItbi -> { id }          [tributos.gerenciar]
//   GET  /api/tributos/itbi/imoveis/{id}/preview?exercicio&valorDeclarado      -> PreviewItbi             -> PreviewItbi      [tributos.ver]
//   POST /api/tributos/itbi/lancar                                             -> LancarItbi              -> LancamentoItbiResultado [tributos.gerenciar]
//
// Regra de base de cálculo: base = MAIOR entre o valor venal de referência e o
// valor declarado na transmissão. Convenções (PADRÃO-OURO): percentuais em
// FRAÇÃO DECIMAL (0,02 = 2%); enums por INT; DTOs no topo; query keys; hooks.
import { useMutation, useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Projeções de leitura
// ---------------------------------------------------------------------------

/** Linha da memória de cálculo do preview/lançamento de ITBI. */
export interface MemoriaItbiLinha {
  rotulo: string;
  detalhe: string;
  valor: number;
}

/** Projeção do PREVIEW do ITBI (GET .../preview). Base = maior venal × declarado. */
export interface PreviewItbi {
  imovelId: string;
  exercicio: number;
  valorVenalReferencia: number;
  valorDeclarado: number;
  baseCalculo: number;
  sfh: boolean;
  aliquotaPercentual: number;
  impostoDevido: number;
  memoria: MemoriaItbiLinha[];
}

/** Resultado do LANÇAMENTO de ITBI (POST .../lancar): gera guia/DAM. */
export interface LancamentoItbiResultado {
  lancamentoId: string;
  guiaNumero: string;
  baseCalculo: number;
  impostoDevido: number;
  vencimento: string;
}

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** ConfigurarAliquotasItbiCommand (geral e SFH por exercício; frações decimais). */
export interface ConfigurarAliquotasItbiInput {
  exercicio: number;
  aliquotaGeral: number;
  aliquotaSfh: number;
}

/** LancarItbiCommand (transmissão -> gera guia/DAM; base = maior venal × declarado). */
export interface LancarItbiInput {
  imovelId: string;
  exercicio: number;
  valorDeclarado: number;
  sfh: boolean;
  transmitente: string;
  adquirente: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const itbiKeys = {
  all: ['tributos', 'itbi'] as const,
  previews: () => [...itbiKeys.all, 'preview'] as const,
  preview: (imovelId: string, exercicio: number, valorDeclarado: number) =>
    [...itbiKeys.previews(), imovelId, exercicio, valorDeclarado] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function configurarAliquotas(input: ConfigurarAliquotasItbiInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/itbi/aliquotas', input);
}

function previewItbi(
  imovelId: string,
  exercicio: number,
  valorDeclarado: number,
  signal?: AbortSignal,
): Promise<PreviewItbi> {
  return http.get<PreviewItbi>(`/tributos/itbi/imoveis/${imovelId}/preview`, {
    query: { exercicio, valorDeclarado },
    signal,
  });
}

function lancarItbi(input: LancarItbiInput): Promise<LancamentoItbiResultado> {
  return http.post<LancamentoItbiResultado>('/tributos/itbi/lancar', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Configura as alíquotas geral/SFH do ITBI num exercício. */
export function useConfigurarAliquotasItbi() {
  return useMutation({ mutationFn: configurarAliquotas });
}

/**
 * PREVIEW do ITBI: base = MAIOR entre venal de referência e valor declarado.
 * Sob demanda (`enabled`); a chave inclui o valor declarado para reagir a ele.
 */
export function usePreviewItbi(
  imovelId: string,
  exercicio: number,
  valorDeclarado: number,
  enabled = true,
) {
  return useQuery({
    queryKey: itbiKeys.preview(imovelId, exercicio, valorDeclarado),
    queryFn: ({ signal }) => previewItbi(imovelId, exercicio, valorDeclarado, signal),
    enabled: enabled && imovelId.trim().length > 0 && exercicio > 0 && valorDeclarado >= 0,
  });
}

/** Lança o ITBI da transmissão (gera guia/DAM). */
export function useLancarItbi() {
  return useMutation({ mutationFn: lancarItbi });
}
