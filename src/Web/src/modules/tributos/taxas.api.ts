// Camada de API do submódulo TAXAS (Tributos M6). Espelha FIELMENTE o contrato REAL
// de TaxasCosipAlvaraMelhoriaEndpoints.cs (Minimal API /api/tributos):
//   POST /api/tributos/taxas/tabelas -> ConfigurarTabelaTaxaCommand -> { id }                  [tributos.gerenciar]
//   POST /api/tributos/taxas/lancar  -> LancarTaxaCommand           -> ResultadoLancamentoTaxa [tributos.gerenciar]
//
// Convenções (PADRÃO-OURO): enums por valor INT no payload (IsInEnum); DTOs no topo;
// hooks por operação. Nenhum valor é hardcoded — tudo vem da LEI MUNICIPAL.
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Enums (string na UI; INT no payload via *_VALOR — espelham o domínio C#)
// ---------------------------------------------------------------------------

/** Espécie da taxa — enum EspecieTaxa (1..3). */
export type EspecieTaxa = 'PoderPolicia' | 'Servico' | 'LicencaLocalizacaoFuncionamento';

export const ESPECIE_TAXA_VALOR: Record<EspecieTaxa, number> = {
  PoderPolicia: 1,
  Servico: 2,
  LicencaLocalizacaoFuncionamento: 3,
};

export const ESPECIE_TAXA_LABEL: Record<EspecieTaxa, string> = {
  PoderPolicia: 'Poder de polícia',
  Servico: 'Serviço público',
  LicencaLocalizacaoFuncionamento: 'Licença de localização/funcionamento',
};

/** Modo de cálculo da taxa — enum ModoCalculoTaxa (1..3). */
export type ModoCalculoTaxa = 'ValorFixo' | 'PorUnidade' | 'PorFaixa';

export const MODO_CALCULO_VALOR: Record<ModoCalculoTaxa, number> = {
  ValorFixo: 1,
  PorUnidade: 2,
  PorFaixa: 3,
};

export const MODO_CALCULO_LABEL: Record<ModoCalculoTaxa, string> = {
  ValorFixo: 'Valor fixo',
  PorUnidade: 'Por unidade (valor × quantidade-base)',
  PorFaixa: 'Por faixa da quantidade-base',
};

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/records reais)
// ---------------------------------------------------------------------------

/** Faixa da tabela (modo PorFaixa) — back: FaixaTaxaInput. */
export interface FaixaTaxaInput {
  limiteInferior: number;
  /** Nulo = sem teto. */
  limiteSuperior: number | null;
  valor: number;
}

/** ConfigurarTabelaTaxaCommand (cria + publica a tabela de uma taxa). */
export interface ConfigurarTabelaTaxaInput {
  codigo: string;
  descricao: string;
  /** enum EspecieTaxa (INT). */
  especie: number;
  /** enum ModoCalculoTaxa (INT). */
  modoCalculo: number;
  exercicio: number;
  /** Valor fixo/unitário (R$). */
  valorBase: number;
  fundamentoLegal: string;
  /** Somente no modo PorFaixa. */
  faixas?: FaixaTaxaInput[] | null;
  publicar?: boolean;
}

/** LancarTaxaCommand (lança a taxa a um contribuinte). */
export interface LancarTaxaInput {
  contribuinteId: string;
  codigoTaxa: string;
  exercicio: number;
  /** Quantidade-base (m², unidades…); ignorada no modo ValorFixo. */
  quantidadeBase: number;
  /** "yyyy-MM-dd". */
  vencimento: string;
  imovelId?: string | null;
  numeroParcelas?: number;
}

/** Resultado do lançamento — back: ResultadoLancamentoTaxa. */
export interface ResultadoLancamentoTaxa {
  lancamentoId: string;
  damId: string;
  valorTaxa: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function configurarTabelaTaxa(input: ConfigurarTabelaTaxaInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/taxas/tabelas', input);
}

function lancarTaxa(input: LancarTaxaInput): Promise<ResultadoLancamentoTaxa> {
  return http.post<ResultadoLancamentoTaxa>('/tributos/taxas/lancar', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Configura (cria + publica) a tabela de uma taxa municipal. */
export function useConfigurarTabelaTaxa() {
  return useMutation({ mutationFn: configurarTabelaTaxa });
}

/** Lança uma taxa a um contribuinte (gera lançamento + guia DAM). */
export function useLancarTaxa() {
  return useMutation({ mutationFn: lancarTaxa });
}
