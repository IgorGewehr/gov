// Camada de API do submódulo ISS (Tributos M6). Espelha o contrato REAL provado
// da Minimal API /api/tributos:
//   POST /api/tributos/iss/aliquotas                          -> ConfigurarAliquotasIss -> { id }       [tributos.gerenciar]
//   POST /api/tributos/iss/contribuintes/{id}/apurar          -> ApurarIss              -> ApuracaoIss  [tributos.gerenciar]
//   POST /api/tributos/iss/nfse/sincronizar                   -> SincronizarNfse        -> SincronizacaoNfseResultado [tributos.gerenciar]
//
// Convenções (PADRÃO-OURO): percentuais em FRAÇÃO DECIMAL (0,05 = 5%); enums por
// valor INT no payload; DTOs no topo; query keys centralizadas; hooks por operação.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Enums (string no domínio; INT no payload via *_VALOR)
// ---------------------------------------------------------------------------

/** Forma de recolhimento do ISS sobre o serviço — enum FormaRecolhimentoIss. */
export type FormaRecolhimentoIss = 'Proprio' | 'Retido' | 'Substituicao';

export const FORMA_RECOLHIMENTO_VALOR: Record<FormaRecolhimentoIss, number> = {
  Proprio: 1,
  Retido: 2,
  Substituicao: 3,
};

// ---------------------------------------------------------------------------
// Projeções de leitura
// ---------------------------------------------------------------------------

/**
 * Resultado da APURAÇÃO mensal do ISS (POST .../apurar) — espelha
 * ResultadoApuracaoIss (achatado; o backend NÃO devolve livro/competência).
 */
export interface ApuracaoIss {
  apuracaoId: string;
  lancamentoId: string | null;
  quantidadeNotas: number;
  issProprio: number;
  issRetido: number;
  issSubstituicao: number;
}

/** Resultado da SINCRONIZAÇÃO sob demanda das NFS-e do ADN (back: { importadas }). */
export interface SincronizacaoNfseResultado {
  importadas: number;
}

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** Alíquota de um item da Lista de Serviços (LC 116/2003) — back: ItemAliquotaIssInput. */
export interface ItemAliquotaIssInput {
  itemListaServico: string;
  /** Alíquota em % (ex.: 2.0 = 2%) (back: AliquotaPercentual). */
  aliquotaPercentual: number;
  /** Retenção na fonte obrigatória (back: RetencaoObrigatoria). */
  retencaoObrigatoria: boolean;
  /** Substituição tributária (back: SubstituicaoTributaria). */
  substituicaoTributaria: boolean;
}

/** ConfigurarTabelaAliquotaIssCommand (alíquotas por item da LC 116). */
export interface ConfigurarAliquotasIssInput {
  /** Início de vigência em AAAAMM (ex.: 202601) (back: VigenciaInicioAaaaMm). */
  vigenciaInicioAaaaMm: number;
  /** Lei municipal de alíquotas (back: FundamentoLegal, obrigatório). */
  fundamentoLegal: string;
  itens: ItemAliquotaIssInput[];
  publicar?: boolean;
}

/** ApurarIssPayload (apuração mensal -> lançamento ISS próprio). */
export interface ApurarIssInput {
  ano: number;
  mes: number;
  /** Vencimento do ISS próprio (back: VencimentoIssProprio, "yyyy-MM-dd", obrigatório). */
  vencimentoIssProprio: string;
}

/** SincronizarNfsePayload (ingestão sob demanda do ADN; passiva). */
export interface SincronizarNfseInput {
  /** CNPJs dos prestadores (back: Prestadores). */
  prestadores: string[];
  /** Data inicial da janela (back: Desde, "yyyy-MM-dd"). */
  desde: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const issKeys = {
  all: ['tributos', 'iss'] as const,
  apuracoes: () => [...issKeys.all, 'apuracao'] as const,
  apuracao: (contribuinteId: string, ano: number, mes: number) =>
    [...issKeys.apuracoes(), contribuinteId, ano, mes] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function configurarAliquotas(input: ConfigurarAliquotasIssInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/iss/aliquotas', input);
}

function apurarIss(contribuinteId: string, input: ApurarIssInput): Promise<ApuracaoIss> {
  return http.post<ApuracaoIss>(`/tributos/iss/contribuintes/${contribuinteId}/apurar`, input);
}

function sincronizarNfse(input: SincronizarNfseInput): Promise<SincronizacaoNfseResultado> {
  return http.post<SincronizacaoNfseResultado>('/tributos/iss/nfse/sincronizar', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Configura as alíquotas do ISS por item da LC 116 num exercício. */
export function useConfigurarAliquotasIss() {
  return useMutation({ mutationFn: configurarAliquotas });
}

/** APURA o ISS mensal do contribuinte (gera lançamento próprio). */
export function useApurarIss(contribuinteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ApurarIssInput) => apurarIss(contribuinteId, input),
    onSuccess: (resultado, variables) => {
      queryClient.setQueryData(
        issKeys.apuracao(contribuinteId, variables.ano, variables.mes),
        resultado,
      );
    },
  });
}

/** Sincroniza sob demanda as NFS-e do ADN (ingestão passiva). */
export function useSincronizarNfse() {
  return useMutation({ mutationFn: sincronizarNfse });
}
