// Camada de API do agregado Contrato (módulo Administracao) — segue o PADRÃO-OURO de Tributos.
// Cobre TODAS as operações do manifest de Contrato.rules.md:
//   queries:  ObterContratoPorId, ListarContratosVigentes, ListarContratosPorFornecedor
//   commands: CelebrarContrato, PublicarContratoNoPncp, IniciarExecucaoContrato,
//             CelebrarAditivo, ApostilarContrato, PrestarGarantia, EncerrarContrato,
//             RescindirContrato
// Convenções:
//  - tipos de DTO no topo (espelham os Commands/Queries de .../Application/Contratos);
//  - rotas reais de AdministracaoEndpoints.cs (/api/administracao/...);
//  - query keys centralizadas para invalidação consistente;
//  - hooks TanStack Query (useQuery/useMutation) exportados para as páginas.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs e enums (espelham o domínio Administracao)
// ---------------------------------------------------------------------------

/** Situação do contrato no ciclo de vida (enum SituacaoContrato). */
export type SituacaoContrato = 'Assinado' | 'Eficaz' | 'EmExecucao' | 'Encerrado' | 'Rescindido';

/** Fundamento da contratação (enum OrigemContratacao). */
export type OrigemContratacao = 'Licitacao' | 'Dispensa' | 'Inexigibilidade';

/** Numérico do enum OrigemContratacao no domínio (1/2/3). */
export const ORIGEM_NUMERICA: Record<OrigemContratacao, number> = {
  Licitacao: 1,
  Dispensa: 2,
  Inexigibilidade: 3,
};

/** Tipo de aditivo (enum TipoAditivo). */
export type TipoAditivo = 'Acrescimo' | 'Supressao' | 'Prazo' | 'Reequilibrio' | 'Qualitativo';

export const TIPO_ADITIVO_NUMERICO: Record<TipoAditivo, number> = {
  Acrescimo: 1,
  Supressao: 2,
  Prazo: 3,
  Reequilibrio: 4,
  Qualitativo: 5,
};

/** Tipo de apostilamento (enum TipoApostilamento). */
export type TipoApostilamento = 'Reajuste' | 'Dotacao' | 'Correcao';

export const TIPO_APOSTILAMENTO_NUMERICO: Record<TipoApostilamento, number> = {
  Reajuste: 1,
  Dotacao: 2,
  Correcao: 3,
};

/** Modalidade da garantia (enum ModalidadeGarantia). */
export type ModalidadeGarantia = 'CaucaoDinheiro' | 'SeguroGarantia' | 'FiancaBancaria' | 'TitulosDividaPublica';

export const MODALIDADE_GARANTIA_NUMERICA: Record<ModalidadeGarantia, number> = {
  CaucaoDinheiro: 1,
  SeguroGarantia: 2,
  FiancaBancaria: 3,
  TitulosDividaPublica: 4,
};

/** Resumo de contrato para listagens (ContratoResumo). */
export interface ContratoResumo {
  id: string;
  fornecedorId: string;
  objeto: string;
  valorAtual: number;
  vigenciaFim: string;
  situacao: SituacaoContrato;
}

/** Resumo de um aditivo na projeção de detalhe (AditivoResumo). */
export interface AditivoResumo {
  aditivoId: string;
  numero: number;
  tipo: TipoAditivo;
  percentual: number;
  publicadoNoPncp: boolean;
}

/** Resumo de uma garantia na projeção de detalhe (GarantiaResumo). */
export interface GarantiaResumo {
  garantiaId: string;
  modalidade: ModalidadeGarantia;
  percentual: number;
  valor: number;
  validadeFim: string;
}

/** Projeção de detalhe do contrato (ContratoDetalhe). */
export interface ContratoDetalhe {
  id: string;
  licitacaoId: string | null;
  fornecedorId: string;
  origem: OrigemContratacao;
  objeto: string;
  valorContratado: number;
  valorAtual: number;
  vigenciaInicio: string;
  vigenciaFim: string;
  situacao: SituacaoContrato;
  publicadoNoPncp: boolean;
  dotacaoConfirmada: boolean;
  numeroContratoPncp: string | null;
  aditivos: AditivoResumo[];
  garantias: GarantiaResumo[];
}

// --- Inputs de comando (espelham os *Command) ------------------------------

/** CelebrarContratoCommand. */
export interface CelebrarContratoInput {
  licitacaoId?: string | null;
  fornecedorId: string;
  origem: number;
  objeto: string;
  valor: number;
  vigenciaInicio: string;
  vigenciaFim: string;
  empenhoId?: string | null;
  numeroEmpenho?: string | null;
  justificativaContratacaoDireta?: string | null;
}

/**
 * Payload do endpoint POST /contratos/{id}/contrato-pncp (PublicarContratoPncpPayload).
 * W9.1: o número de controle PNCP NÃO vem mais do cliente — a ACL (IPncpGateway) transmite ao
 * PNCP e devolve o número oficial, que o backend grava no contrato. O front envia apenas os dados
 * de identificação da transmissão (órgão/unidade/contrato interno/fornecedor).
 */
export interface PublicarContratoNoPncpInput {
  cnpjOrgao: string;
  codigoUnidade: string;
  numeroContratoInterno: string;
  anoContrato: number;
  processo: string;
  /** niFornecedor: CNPJ/CPF/identificador estrangeiro (Manual PNCP 2.3.5). */
  niFornecedor: string;
  /** tipoPessoaFornecedor: 'PessoaJuridica' | 'PessoaFisica' | 'PessoaEstrangeira'. */
  tipoPessoaFornecedor: 'PessoaJuridica' | 'PessoaFisica' | 'PessoaEstrangeira';
  nomeRazaoSocialFornecedor: string;
  /** tipoContratoId: código da tabela de domínio do PNCP. */
  tipoContratoId: number;
  /** categoriaProcessoId: código da tabela de domínio do PNCP. */
  categoriaProcessoId: number;
  numeroParcelas?: number;
  cnpjCompra?: string;
  anoCompra?: number;
  sequencialCompra?: number;
  numeroControlePncpCompra?: string;
  frutoAdesao?: boolean;
}

/** Resposta da varredura de prazos PNCP (POST /contratos/prazos-pncp/varrer). */
export interface VarrerPrazosPncpResultado {
  alertas: number;
}

/** CelebrarAditivoCommand. */
export interface CelebrarAditivoInput {
  tipo: number;
  percentual: number;
  valorDelta: number;
  novaVigenciaFim?: string | null;
  justificativa: string;
  ehReforma: boolean;
}

/** ApostilarContratoCommand. */
export interface ApostilarContratoInput {
  tipo: number;
  descricao: string;
}

/** PrestarGarantiaCommand. */
export interface PrestarGarantiaInput {
  modalidade: number;
  percentual: number;
  valor: number;
  validadeFim: string;
  ehGrandeVulto: boolean;
}

/** RescindirContratoCommand. */
export interface RescindirContratoInput {
  motivo: string;
}

interface IdResponse {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const contratoKeys = {
  all: ['administracao', 'contratos'] as const,
  vigentes: (referencia: string) => [...contratoKeys.all, 'vigentes', referencia] as const,
  porFornecedor: (fornecedorId: string) => [...contratoKeys.all, 'fornecedor', fornecedorId] as const,
  detalhe: (id: string) => [...contratoKeys.all, 'detalhe', id] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas reais de AdministracaoEndpoints.cs)
// ---------------------------------------------------------------------------

function listarVigentes(referencia: string, signal?: AbortSignal): Promise<ContratoResumo[]> {
  return http.get<ContratoResumo[]>('/administracao/contratos/vigentes', { query: { referencia }, signal });
}

function listarPorFornecedor(fornecedorId: string, signal?: AbortSignal): Promise<ContratoResumo[]> {
  return http.get<ContratoResumo[]>(`/administracao/fornecedores/${fornecedorId}/contratos`, { signal });
}

function obterContrato(id: string, signal?: AbortSignal): Promise<ContratoDetalhe> {
  return http.get<ContratoDetalhe>(`/administracao/contratos/${id}`, { signal });
}

function celebrarContrato(input: CelebrarContratoInput): Promise<IdResponse> {
  return http.post<IdResponse>('/administracao/contratos', input);
}

function publicarNoPncp(contratoId: string, input: PublicarContratoNoPncpInput): Promise<void> {
  return http.post<void>(`/administracao/contratos/${contratoId}/contrato-pncp`, input);
}

function varrerPrazosPncp(): Promise<VarrerPrazosPncpResultado> {
  return http.post<VarrerPrazosPncpResultado>('/administracao/contratos/prazos-pncp/varrer');
}

function iniciarExecucao(contratoId: string): Promise<void> {
  return http.post<void>(`/administracao/contratos/${contratoId}/iniciar-execucao`);
}

function encerrarContrato(contratoId: string): Promise<void> {
  return http.post<void>(`/administracao/contratos/${contratoId}/encerrar`);
}

// As rotas abaixo seguem a convenção REST do módulo (verbos do agregado expostos
// como sub-recursos do contrato), espelhando os Commands existentes na Application.
function celebrarAditivo(contratoId: string, input: CelebrarAditivoInput): Promise<IdResponse> {
  return http.post<IdResponse>(`/administracao/contratos/${contratoId}/aditivos`, input);
}

function apostilarContrato(contratoId: string, input: ApostilarContratoInput): Promise<IdResponse> {
  return http.post<IdResponse>(`/administracao/contratos/${contratoId}/apostilamentos`, input);
}

function prestarGarantia(contratoId: string, input: PrestarGarantiaInput): Promise<IdResponse> {
  return http.post<IdResponse>(`/administracao/contratos/${contratoId}/garantias`, input);
}

function rescindirContrato(contratoId: string, input: RescindirContratoInput): Promise<void> {
  return http.post<void>(`/administracao/contratos/${contratoId}/rescindir`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista os contratos vigentes do tenant em uma data de referência (Eficaz/EmExecucao). */
export function useContratosVigentes(referencia: string, enabled = true) {
  return useQuery({
    queryKey: contratoKeys.vigentes(referencia),
    queryFn: ({ signal }) => listarVigentes(referencia, signal),
    enabled: enabled && referencia.trim().length > 0,
  });
}

/** Lista os contratos de um fornecedor (consulta sob demanda). */
export function useContratosPorFornecedor(fornecedorId: string, enabled = true) {
  return useQuery({
    queryKey: contratoKeys.porFornecedor(fornecedorId),
    queryFn: ({ signal }) => listarPorFornecedor(fornecedorId, signal),
    enabled: enabled && fornecedorId.trim().length > 0,
  });
}

/** Detalhe de um contrato (com aditivos e garantias). */
export function useContrato(id: string) {
  return useQuery({
    queryKey: contratoKeys.detalhe(id),
    queryFn: ({ signal }) => obterContrato(id, signal),
    enabled: id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Celebra (assina) um novo contrato; invalida listas afetadas. */
export function useCelebrarContrato() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: celebrarContrato,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

/**
 * Divulga o contrato no PNCP — condição de EFICÁCIA (Lei 14.133/2021, art. 94). O backend transmite
 * via ACL e grava o número de controle PNCP; o detalhe é reconsultado para refletir o número devolvido.
 */
export function usePublicarNoPncp(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PublicarContratoNoPncpInput) => publicarNoPncp(contratoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

/**
 * Varre os prazos de divulgação no PNCP (art. 94) e enfileira alertas (a vencer/vencido) ao Portal do
 * Gestor. Idempotente; retorna a quantidade de alertas emitidos. Invalida as listas afetadas.
 */
export function useVarrerPrazosPncp() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: varrerPrazosPncp,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

/** Inicia a execução do contrato (exige eficácia + dotação — I-7/I-8). */
export function useIniciarExecucao(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => iniciarExecucao(contratoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

/** Celebra um termo aditivo (limite 25%/50% — I-9). */
export function useCelebrarAditivo(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CelebrarAditivoInput) => celebrarAditivo(contratoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

/** Registra um apostilamento (dispensa termo aditivo — art. 136). */
export function useApostilarContrato(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ApostilarContratoInput) => apostilarContrato(contratoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
    },
  });
}

/** Presta uma garantia de execução (limite 5%/10% — art. 96/98). */
export function usePrestarGarantia(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PrestarGarantiaInput) => prestarGarantia(contratoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
    },
  });
}

/** Encerra o contrato (vigência concluída — passa a Encerrado). */
export function useEncerrarContrato(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => encerrarContrato(contratoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

/** Rescinde o contrato (extinção antecipada com motivação — I-15). */
export function useRescindirContrato(contratoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RescindirContratoInput) => rescindirContrato(contratoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contratoKeys.detalhe(contratoId) });
      queryClient.invalidateQueries({ queryKey: contratoKeys.all });
    },
  });
}

// ---------------------------------------------------------------------------
// Helpers de apresentação
// ---------------------------------------------------------------------------

export const SITUACAO_ROTULO: Record<SituacaoContrato, string> = {
  Assinado: 'Assinado',
  Eficaz: 'Eficaz',
  EmExecucao: 'Em execução',
  Encerrado: 'Encerrado',
  Rescindido: 'Rescindido',
};

export const ORIGEM_ROTULO: Record<OrigemContratacao, string> = {
  Licitacao: 'Licitação',
  Dispensa: 'Dispensa',
  Inexigibilidade: 'Inexigibilidade',
};

export const TIPO_ADITIVO_ROTULO: Record<TipoAditivo, string> = {
  Acrescimo: 'Acréscimo',
  Supressao: 'Supressão',
  Prazo: 'Prazo',
  Reequilibrio: 'Reequilíbrio',
  Qualitativo: 'Qualitativo',
};

export const TIPO_APOSTILAMENTO_ROTULO: Record<TipoApostilamento, string> = {
  Reajuste: 'Reajuste',
  Dotacao: 'Dotação',
  Correcao: 'Correção',
};

export const MODALIDADE_GARANTIA_ROTULO: Record<ModalidadeGarantia, string> = {
  CaucaoDinheiro: 'Caução em dinheiro',
  SeguroGarantia: 'Seguro-garantia',
  FiancaBancaria: 'Fiança bancária',
  TitulosDividaPublica: 'Títulos da dívida pública',
};
