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

/** Linha do livro fiscal eletrônico apurado (uma NFS-e processada). */
export interface LivroEletronicoLinha {
  chaveAcesso: string;
  numeroNfse: string;
  dataEmissao: string;
  itemListaServico: string;
  baseCalculo: number;
  aliquotaPercentual: number;
  forma: FormaRecolhimentoIss;
  issDevido: number;
}

/** Totais por forma de recolhimento (próprio/retido/substituição). */
export interface TotaisPorForma {
  issProprio: number;
  issRetido: number;
  issSubstituicao: number;
}

/** Projeção da APURAÇÃO mensal do ISS (POST .../apurar). */
export interface ApuracaoIss {
  contribuinteId: string;
  competencia: string;
  quantidadeNotas: number;
  baseCalculoTotal: number;
  totais: TotaisPorForma;
  issTotalDevido: number;
  lancamentoId: string | null;
  livro: LivroEletronicoLinha[];
}

/** Resultado da SINCRONIZAÇÃO sob demanda das NFS-e do ADN (ingestão passiva). */
export interface SincronizacaoNfseResultado {
  notasRecebidas: number;
  notasNovas: number;
  notasDuplicadas: number;
  ultimaSincronizacao: string;
}

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** Alíquota de um item da Lista de Serviços (LC 116/2003). */
export interface ItemAliquotaIssInput {
  itemListaServico: string;
  descricao: string;
  aliquota: number;
  retencao: boolean;
  substituicao: boolean;
}

/** ConfigurarAliquotasIssCommand (alíquotas por item da LC 116, com retenção/substituição). */
export interface ConfigurarAliquotasIssInput {
  exercicio: number;
  itens: ItemAliquotaIssInput[];
}

/** ApurarIssCommand (apuração mensal -> livro + lançamento ISS próprio). */
export interface ApurarIssInput {
  competencia: string;
}

/** SincronizarNfseCommand (ingestão sob demanda do ADN; passiva). */
export interface SincronizarNfseInput {
  contribuinteId: string;
  competencia: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const issKeys = {
  all: ['tributos', 'iss'] as const,
  apuracoes: () => [...issKeys.all, 'apuracao'] as const,
  apuracao: (contribuinteId: string, competencia: string) =>
    [...issKeys.apuracoes(), contribuinteId, competencia] as const,
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

/** APURA o ISS mensal do contribuinte (gera livro + lançamento próprio). */
export function useApurarIss(contribuinteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ApurarIssInput) => apurarIss(contribuinteId, input),
    onSuccess: (resultado) => {
      queryClient.setQueryData(
        issKeys.apuracao(contribuinteId, resultado.competencia),
        resultado,
      );
    },
  });
}

/** Sincroniza sob demanda as NFS-e do ADN (ingestão passiva). */
export function useSincronizarNfse() {
  return useMutation({ mutationFn: sincronizarNfse });
}
