// Camada de API do módulo Tributos (PADRÃO-OURO). Espelha FIELMENTE o contrato REAL
// de TributosEndpoints.cs (Minimal API /api/tributos):
//   POST /api/tributos/contribuintes/pessoa-fisica                 -> CadastrarContribuintePessoaFisica -> { id }            [tributos.gerenciar]
//   POST /api/tributos/lancamentos                                 -> LancarCredito                     -> { id }            [tributos.gerenciar]
//   POST /api/tributos/lancamentos/{lancamentoId}/inscrever-divida-ativa -> InscreverEmDividaAtiva -> { dividaAtivaId }      [tributos.gerenciar]
//   POST /api/tributos/dividas/{dividaAtivaId}/cda                 -> EmitirCda                          -> CdaEmitidaDto     [tributos.gerenciar]
//   POST /api/tributos/dividas/{dividaAtivaId}/protesto/remessa    -> GerarRemessaProtesto    -> ResultadoRemessaProtesto    [tributos.gerenciar]
//   POST /api/tributos/dividas/{dividaAtivaId}/protesto/retorno    -> ProcessarRetornoProtesto           -> 204               [tributos.gerenciar]
//   POST /api/tributos/dividas/{dividaAtivaId}/execucao-fiscal     -> AjuizarExecucaoFiscal              -> 204               [tributos.gerenciar]
//   GET  /api/tributos/dividas/{dividaAtivaId}/prescricao?dataReferencia= -> AvaliarPrescricaoDivida -> AvaliacaoPrescricao  [tributos.ver]
//   GET  /api/tributos/contribuintes/{contribuinteId}/dividas-ativas -> ObterDividasAtivasDoContribuinte -> DividaAtivaResumo[] [tributos.ver]
//
// Convenções: DTOs no topo; query keys centralizadas para invalidação consistente;
// funções de acesso via http client tipado (Authorization + ProblemDetails);
// hooks TanStack Query (useQuery/useMutation) para CADA operação.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham os enums e projeções do domínio Tributos)
// ---------------------------------------------------------------------------

/** Situação da Dívida Ativa — enum SituacaoDividaAtiva (string no JSON). */
export type SituacaoDividaAtiva =
  | 'Inscrita'
  | 'CdaEmitida'
  | 'Protestada'
  | 'EmExecucaoFiscal'
  | 'Parcelada'
  | 'Quitada'
  | 'Cancelada';

/** Espécie tributária — enum TipoTributo (1..4). */
export type TipoTributo = 'Iptu' | 'Iss' | 'Itbi' | 'Taxa';

/** Valor numérico do enum TipoTributo esperado pelo backend (IsInEnum). */
export const TIPO_TRIBUTO_VALOR: Record<TipoTributo, number> = {
  Iptu: 1,
  Iss: 2,
  Itbi: 3,
  Taxa: 4,
};

/** Projeção de resumo (DividaAtivaResumo — ObterDividasAtivasDoContribuinte). */
export interface DividaAtivaResumo {
  id: string;
  contribuinteId: string;
  valorOriginario: number;
  situacao: SituacaoDividaAtiva;
  dataInscricao: string;
  dataPrescricao: string;
  numeroCda: string | null;
  numeroInscricao: number;
}

/** CDA emitida (CdaEmitidaDto — requisitos legais LEF art. 2º §5º). */
export interface CdaEmitidaDto {
  numero: string;
  nomeDevedor: string;
  valorOriginario: number;
  origemNatureza: string;
  fundamentoLegal: string;
  dataInscricao: string;
  numeroInscricao: number;
}

/** Resultado da remessa de protesto (ResultadoRemessaProtesto). */
export interface ResultadoRemessaProtesto {
  remessaProtestoId: string;
  identificadorCra: string;
  conteudoRemessa: string;
}

/**
 * Ocorrência de retorno do protesto (enum OcorrenciaProtesto). O backend aceita o
 * valor numérico (IsInEnum); `Pendente` (0) é recusado pelo validator.
 */
export type OcorrenciaProtesto = 'Lavrado' | 'PagoOuRetirado' | 'Sustado' | 'Rejeitado';

/** Valor numérico do enum OcorrenciaProtesto esperado pelo backend. */
export const OCORRENCIA_PROTESTO_VALOR: Record<OcorrenciaProtesto, number> = {
  Lavrado: 1,
  PagoOuRetirado: 2,
  Sustado: 3,
  Rejeitado: 4,
};

/** Avaliação de prescrição/encargos numa data de referência (AvaliacaoPrescricaoDivida). */
export interface AvaliacaoPrescricaoDivida {
  dividaAtivaId: string;
  termoInicialPrescricao: string;
  dataPrescricao: string;
  estaPrescrita: boolean;
  valorOriginario: number;
  correcaoMonetaria: number;
  multa: number;
  juros: number;
  valorAtualizado: number;
}

// --- Entradas de comando (espelham os Commands/Payloads reais) ---

/** CadastrarContribuintePessoaFisicaCommand(Cpf, Nome, InscricaoMunicipal). */
export interface CadastrarContribuintePfInput {
  cpf: string;
  nome: string;
  inscricaoMunicipal?: string | null;
}

/** LancarCreditoCommand(ContribuinteId, TipoTributo, Ano, Mes, ValorPrincipal, Vencimento). */
export interface LancarCreditoInput {
  contribuinteId: string;
  tipoTributo: number;
  ano: number;
  mes: number;
  valorPrincipal: number;
  vencimento: string;
}

/**
 * InscreverDividaPayload — encargos PARAMETRIZÁVEIS por tenant (lei municipal). Datas
 * opcionais (DateOnly 'YYYY-MM-DD'); ausentes -> backend usa o vencimento do lançamento.
 */
export interface InscreverDividaInput {
  fundamentoLegal: string;
  multaMoraPercentual: number;
  jurosMoraPercentualMensal: number;
  correcaoPercentualMensal: number;
  fundamentoEncargos: string;
  dataConstituicaoDefinitiva?: string | null;
  dataInscricao?: string | null;
  anosPrescricao?: number | null;
}

/** EmitirCdaPayload(NumeroCda, DataBaseEncargos, Domicilio?, CoResponsaveis?, Processo?). */
export interface EmitirCdaInput {
  numeroCda: string;
  dataBaseEncargos: string;
  domicilioDevedor?: string | null;
  coResponsaveis?: string | null;
  processoAdministrativo?: string | null;
}

/** ProtestoRetornoPayload(RemessaProtestoId, Ocorrencia, DataRetorno, ProtocoloCartorio?). */
export interface ProtestoRetornoInput {
  remessaProtestoId: string;
  ocorrencia: number;
  dataRetorno: string;
  protocoloCartorio?: string | null;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const tributosKeys = {
  all: ['tributos'] as const,
  dividas: () => [...tributosKeys.all, 'dividas-ativas'] as const,
  dividasPorContribuinte: (contribuinteId: string) =>
    [...tributosKeys.dividas(), 'contribuinte', contribuinteId] as const,
  prescricao: (dividaAtivaId: string, dataReferencia: string) =>
    [...tributosKeys.all, 'prescricao', dividaAtivaId, dataReferencia] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarDividasPorContribuinte(
  contribuinteId: string,
  signal?: AbortSignal,
): Promise<DividaAtivaResumo[]> {
  return http.get<DividaAtivaResumo[]>(
    `/tributos/contribuintes/${contribuinteId}/dividas-ativas`,
    { signal },
  );
}

function cadastrarContribuintePf(input: CadastrarContribuintePfInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/contribuintes/pessoa-fisica', input);
}

function lancarCredito(input: LancarCreditoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/lancamentos', input);
}

function inscreverEmDividaAtiva(
  lancamentoId: string,
  input: InscreverDividaInput,
): Promise<{ dividaAtivaId: string }> {
  return http.post<{ dividaAtivaId: string }>(
    `/tributos/lancamentos/${lancamentoId}/inscrever-divida-ativa`,
    input,
  );
}

function emitirCda(dividaAtivaId: string, input: EmitirCdaInput): Promise<CdaEmitidaDto> {
  return http.post<CdaEmitidaDto>(`/tributos/dividas/${dividaAtivaId}/cda`, input);
}

function gerarRemessaProtesto(
  dividaAtivaId: string,
  dataGeracao: string,
): Promise<ResultadoRemessaProtesto> {
  return http.post<ResultadoRemessaProtesto>(
    `/tributos/dividas/${dividaAtivaId}/protesto/remessa`,
    { dataGeracao },
  );
}

function processarRetornoProtesto(
  dividaAtivaId: string,
  input: ProtestoRetornoInput,
): Promise<void> {
  return http.post<void>(`/tributos/dividas/${dividaAtivaId}/protesto/retorno`, input);
}

function ajuizarExecucaoFiscal(dividaAtivaId: string, dataAjuizamento: string): Promise<void> {
  return http.post<void>(`/tributos/dividas/${dividaAtivaId}/execucao-fiscal`, { dataAjuizamento });
}

function avaliarPrescricao(
  dividaAtivaId: string,
  dataReferencia: string,
  signal?: AbortSignal,
): Promise<AvaliacaoPrescricaoDivida> {
  return http.get<AvaliacaoPrescricaoDivida>(`/tributos/dividas/${dividaAtivaId}/prescricao`, {
    query: { dataReferencia },
    signal,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista as dívidas ativas de um contribuinte. `enabled` controla disparo sob demanda. */
export function useDividasPorContribuinte(contribuinteId: string, enabled = true) {
  return useQuery({
    queryKey: tributosKeys.dividasPorContribuinte(contribuinteId),
    queryFn: ({ signal }) => listarDividasPorContribuinte(contribuinteId, signal),
    enabled: enabled && contribuinteId.trim().length > 0,
  });
}

/**
 * Avalia a prescrição (CTN art. 174) e os encargos de uma dívida numa data de
 * referência (determinístico, sem relógio). Disparo sob demanda via `enabled`.
 */
export function useAvaliarPrescricao(
  dividaAtivaId: string,
  dataReferencia: string,
  enabled = true,
) {
  return useQuery({
    queryKey: tributosKeys.prescricao(dividaAtivaId, dataReferencia),
    queryFn: ({ signal }) => avaliarPrescricao(dividaAtivaId, dataReferencia, signal),
    enabled: enabled && dividaAtivaId.length > 0 && dataReferencia.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Cadastra um contribuinte pessoa física. */
export function useCadastrarContribuintePf() {
  return useMutation({ mutationFn: cadastrarContribuintePf });
}

/** Constitui (lança) um crédito tributário para um contribuinte. */
export function useLancarCredito() {
  return useMutation({ mutationFn: lancarCredito });
}

/** Invalida a lista de dívidas do contribuinte (ou todas, se não informado). */
function invalidarDividas(
  queryClient: ReturnType<typeof useQueryClient>,
  contribuinteIdParaInvalidar?: string,
): void {
  if (contribuinteIdParaInvalidar && contribuinteIdParaInvalidar.trim().length > 0) {
    queryClient.invalidateQueries({
      queryKey: tributosKeys.dividasPorContribuinte(contribuinteIdParaInvalidar),
    });
  } else {
    queryClient.invalidateQueries({ queryKey: tributosKeys.dividas() });
  }
}

/** Inscreve um lançamento vencido em Dívida Ativa. Invalida as dívidas do contribuinte. */
export function useInscreverEmDividaAtiva(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ lancamentoId, input }: { lancamentoId: string; input: InscreverDividaInput }) =>
      inscreverEmDividaAtiva(lancamentoId, input),
    onSuccess: () => invalidarDividas(queryClient, contribuinteIdParaInvalidar),
  });
}

/** Emite a Certidão de Dívida Ativa (CDA) de um título. Invalida as dívidas do contribuinte. */
export function useEmitirCda(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ dividaAtivaId, input }: { dividaAtivaId: string; input: EmitirCdaInput }) =>
      emitirCda(dividaAtivaId, input),
    onSuccess: () => invalidarDividas(queryClient, contribuinteIdParaInvalidar),
  });
}

/** Gera a remessa de protesto extrajudicial (CRA estadual). Invalida as dívidas. */
export function useGerarRemessaProtesto(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ dividaAtivaId, dataGeracao }: { dividaAtivaId: string; dataGeracao: string }) =>
      gerarRemessaProtesto(dividaAtivaId, dataGeracao),
    onSuccess: () => invalidarDividas(queryClient, contribuinteIdParaInvalidar),
  });
}

/** Processa o retorno do CRA/cartório de uma remessa de protesto. Invalida as dívidas. */
export function useProcessarRetornoProtesto(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      dividaAtivaId,
      input,
    }: {
      dividaAtivaId: string;
      input: ProtestoRetornoInput;
    }) => processarRetornoProtesto(dividaAtivaId, input),
    onSuccess: () => invalidarDividas(queryClient, contribuinteIdParaInvalidar),
  });
}

/** Ajuíza (gancho) a execução fiscal de uma CDA. Invalida as dívidas do contribuinte. */
export function useAjuizarExecucaoFiscal(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      dividaAtivaId,
      dataAjuizamento,
    }: {
      dividaAtivaId: string;
      dataAjuizamento: string;
    }) => ajuizarExecucaoFiscal(dividaAtivaId, dataAjuizamento),
    onSuccess: () => invalidarDividas(queryClient, contribuinteIdParaInvalidar),
  });
}
