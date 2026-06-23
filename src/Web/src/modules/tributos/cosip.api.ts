// Camada de API do submódulo COSIP/CIP (Tributos M6 — CF art. 149-A). Espelha
// FIELMENTE o contrato REAL de TaxasCosipAlvaraMelhoriaEndpoints.cs:
//   POST /api/tributos/cosip/tabelas -> ConfigurarTabelaCosipCommand -> { id }                 [tributos.gerenciar]
//   POST /api/tributos/cosip/lancar  -> LancarCosipCommand           -> ResultadoLancamentoCosip [tributos.gerenciar]
//
// Convenções (PADRÃO-OURO): enums por valor INT no payload; DTOs no topo;
// hooks por operação. Nenhum valor é hardcoded — tudo vem da LEI MUNICIPAL.
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Enums (string na UI; INT no payload via *_VALOR — espelham o domínio C#)
// ---------------------------------------------------------------------------

/** Classe de consumidor — enum ClasseConsumidorCosip (1..5). */
export type ClasseConsumidorCosip =
  | 'Residencial'
  | 'Comercial'
  | 'Industrial'
  | 'Rural'
  | 'PoderPublico';

export const CLASSE_CONSUMIDOR_VALOR: Record<ClasseConsumidorCosip, number> = {
  Residencial: 1,
  Comercial: 2,
  Industrial: 3,
  Rural: 4,
  PoderPublico: 5,
};

export const CLASSE_CONSUMIDOR_LABEL: Record<ClasseConsumidorCosip, string> = {
  Residencial: 'Residencial',
  Comercial: 'Comercial/serviços',
  Industrial: 'Industrial',
  Rural: 'Rural',
  PoderPublico: 'Poder público / demais',
};

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/records reais)
// ---------------------------------------------------------------------------

/** Faixa de consumo por classe — back: FaixaCosipInput. */
export interface FaixaCosipInput {
  /** enum ClasseConsumidorCosip (INT). */
  classe: number;
  consumoMinimoKwh: number;
  /** Nulo = sem teto. */
  consumoMaximoKwh: number | null;
  valor: number;
}

/** ConfigurarTabelaCosipCommand (cria + publica a tabela do exercício). */
export interface ConfigurarTabelaCosipInput {
  exercicio: number;
  fundamentoLegal: string;
  faixas: FaixaCosipInput[];
  publicar?: boolean;
}

/** LancarCosipCommand (apura por lançamento próprio — não faturados). */
export interface LancarCosipInput {
  contribuinteId: string;
  /** enum ClasseConsumidorCosip (INT). */
  classe: number;
  consumoKwh: number;
  ano: number;
  mes: number;
  /** "yyyy-MM-dd". */
  vencimento: string;
  imovelId?: string | null;
}

/** Resultado do lançamento — back: ResultadoLancamentoCosip. */
export interface ResultadoLancamentoCosip {
  lancamentoId: string;
  damId: string;
  valorCosip: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function configurarTabelaCosip(input: ConfigurarTabelaCosipInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/cosip/tabelas', input);
}

function lancarCosip(input: LancarCosipInput): Promise<ResultadoLancamentoCosip> {
  return http.post<ResultadoLancamentoCosip>('/tributos/cosip/lancar', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Configura (cria + publica) a tabela de COSIP de um exercício. */
export function useConfigurarTabelaCosip() {
  return useMutation({ mutationFn: configurarTabelaCosip });
}

/** Apura e lança a COSIP por lançamento próprio (gera lançamento + guia DAM). */
export function useLancarCosip() {
  return useMutation({ mutationFn: lancarCosip });
}
