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

/**
 * Projeção do PREVIEW do ITBI (GET .../preview) — espelha ResultadoItbi.
 * Base = VALOR DECLARADO (Tema 1.113/STJ); o venal de referência só dispara a
 * triagem (`haDivergenciaReferencia`). `aliquotaPercentual` em % (ex.: 2.0 = 2%).
 */
export interface PreviewItbi {
  imovelId: string;
  exercicio: number;
  valorVenalReferencia: number;
  valorDeclarado: number;
  baseCalculo: number;
  /** Origem da base de cálculo (back: Origem, string/enum). */
  origem: string;
  /** Há divergência com o valor venal de referência (back: HaDivergenciaReferencia). */
  haDivergenciaReferencia: boolean;
  aliquotaPercentual: number;
  impostoBruto: number;
  valorIsencao: number;
  impostoDevido: number;
}

/** Resultado do LANÇAMENTO de ITBI (POST .../lancar) — espelha ResultadoLancamentoItbi. */
export interface LancamentoItbiResultado {
  transmissaoId: string;
  lancamentoId: string;
  damId: string;
  baseCalculo: number;
  /** Origem da base de cálculo (back: Origem). */
  origem: string;
  haDivergenciaReferencia: boolean;
  impostoDevido: number;
}

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** ConfigurarAliquotaItbiCommand (geral e SFH por exercício; alíquotas em %). */
export interface ConfigurarAliquotasItbiInput {
  exercicio: number;
  /** Alíquota geral em % (back: AliquotaGeralPercentual). */
  aliquotaGeralPercentual: number;
  /** Alíquota SFH financiada em % (back: AliquotaSfhFinanciadaPercentual). */
  aliquotaSfhFinanciadaPercentual: number;
  /** Lei municipal de alíquota (back: FundamentoLegal, obrigatório). */
  fundamentoLegal: string;
  publicar?: boolean;
}

/** LancarItbiCommand (transmissão -> lança ITBI pela base declarada + DAM). */
export interface LancarItbiInput {
  imovelId: string;
  /** Transmitente (back: TransmitenteId, Guid do contribuinte). */
  transmitenteId: string;
  /** Adquirente (back: AdquirenteId, Guid do contribuinte). */
  adquirenteId: string;
  exercicio: number;
  valorDeclarado: number;
  /** Vencimento da guia (back: Vencimento, "yyyy-MM-dd", obrigatório). */
  vencimento: string;
  /** Usar a alíquota de SFH financiada (back: UsarAliquotaSfh). */
  usarAliquotaSfh: boolean;
  /** Percentual de isenção (back: PercentualIsencao, default 0). */
  percentualIsencao?: number;
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
