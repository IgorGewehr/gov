// Camada de API do submódulo CONTRIBUIÇÃO DE MELHORIA (Tributos M6 — CTN arts. 81–82).
// Espelha FIELMENTE o contrato REAL de TaxasCosipAlvaraMelhoriaEndpoints.cs:
//   POST /api/tributos/melhoria/obras                        -> PublicarEditalMelhoriaCommand -> { id }   [tributos.gerenciar]
//   POST /api/tributos/melhoria/obras/{id}/imoveis           -> AdicionarImovelBeneficiadoPayload -> { ok } [tributos.gerenciar]
//   POST /api/tributos/melhoria/obras/{id}/impugnacao/encerrar -> EncerrarImpugnacaoPayload     -> { ok }   [tributos.gerenciar]
//   POST /api/tributos/melhoria/obras/{id}/ratear            -> RatearMelhoriaPayload         -> ResultadoRateioMelhoria [tributos.gerenciar]
//
// Fluxo legal: publicar edital (prazo de impugnação ≥ 30 dias) -> adicionar imóveis
// beneficiados -> encerrar prazo de impugnação -> ratear (gera lançamento + guia por
// imóvel). Nenhum valor é hardcoded — tudo vem da LEI da obra + CTM.
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

/** CTN art. 82, II: prazo de impugnação não inferior a 30 dias. */
export const PRAZO_MINIMO_IMPUGNACAO_DIAS = 30;

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** PublicarEditalMelhoriaCommand (inicia o processo da obra). */
export interface PublicarEditalMelhoriaInput {
  identificacaoObra: string;
  memorialDescritivo: string;
  custoTotalObra: number;
  /** Percentual do custo a financiar (0,01 a 100). */
  parcelaCustoFinanciadaPercentual: number;
  zonaBeneficiada: string;
  /** Fator de absorção do benefício (0,01 a 100). */
  fatorAbsorcaoPercentual: number;
  /** "yyyy-MM-dd". */
  dataPublicacaoEdital: string;
  /** "yyyy-MM-dd" (≥ 30 dias após a publicação). */
  fimPrazoImpugnacao: string;
  fundamentoLegal: string;
}

/** AdicionarImovelBeneficiadoPayload. */
export interface AdicionarImovelBeneficiadoInput {
  imovelId: string;
  proprietarioId: string;
  valorizacaoIndividual: number;
}

/** EncerrarImpugnacaoPayload. */
export interface EncerrarImpugnacaoInput {
  /** "yyyy-MM-dd" (deve ser ≥ fim do prazo). */
  dataReferencia: string;
}

/** RatearMelhoriaPayload. */
export interface RatearMelhoriaInput {
  /** "yyyy-MM-dd". */
  vencimento: string;
  numeroParcelas?: number;
}

// ---------------------------------------------------------------------------
// Projeções de leitura (resultado do rateio)
// ---------------------------------------------------------------------------

/** Lançamento gerado por imóvel — back: LancamentoMelhoriaPorImovel. */
export interface LancamentoMelhoriaPorImovel {
  imovelId: string;
  lancamentoId: string;
  damId: string;
  contribuicaoRateada: number;
}

/** Resultado do rateio — back: ResultadoRateioMelhoria. */
export interface ResultadoRateioMelhoria {
  obraId: string;
  totalRateado: number;
  lancamentos: LancamentoMelhoriaPorImovel[];
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function publicarEdital(input: PublicarEditalMelhoriaInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/melhoria/obras', input);
}

function adicionarImovel(obraId: string, input: AdicionarImovelBeneficiadoInput): Promise<{ ok: boolean }> {
  return http.post<{ ok: boolean }>(`/tributos/melhoria/obras/${obraId}/imoveis`, input);
}

function encerrarImpugnacao(obraId: string, input: EncerrarImpugnacaoInput): Promise<{ ok: boolean }> {
  return http.post<{ ok: boolean }>(`/tributos/melhoria/obras/${obraId}/impugnacao/encerrar`, input);
}

function ratear(obraId: string, input: RatearMelhoriaInput): Promise<ResultadoRateioMelhoria> {
  return http.post<ResultadoRateioMelhoria>(`/tributos/melhoria/obras/${obraId}/ratear`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Publica o edital de uma obra de Contribuição de Melhoria. */
export function usePublicarEditalMelhoria() {
  return useMutation({ mutationFn: publicarEdital });
}

/** Adiciona um imóvel beneficiado à obra (fase de edital). */
export function useAdicionarImovelBeneficiado() {
  return useMutation({
    mutationFn: ({ obraId, input }: { obraId: string; input: AdicionarImovelBeneficiadoInput }) =>
      adicionarImovel(obraId, input),
  });
}

/** Encerra o prazo de impugnação ao edital (habilita o rateio). */
export function useEncerrarImpugnacao() {
  return useMutation({
    mutationFn: ({ obraId, input }: { obraId: string; input: EncerrarImpugnacaoInput }) =>
      encerrarImpugnacao(obraId, input),
  });
}

/** Rateia a contribuição entre os imóveis beneficiados (gera lançamentos + guias). */
export function useRatearMelhoria() {
  return useMutation({
    mutationFn: ({ obraId, input }: { obraId: string; input: RatearMelhoriaInput }) =>
      ratear(obraId, input),
  });
}
