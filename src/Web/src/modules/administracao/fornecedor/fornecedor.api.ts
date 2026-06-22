// Camada de API do agregado Fornecedor (modulo Administracao — Lei 14.133/2021).
// Segue o PADRAO-OURO de src/modules/tributos/api.ts:
//   DTOs -> query keys -> funcoes http tipadas -> hooks TanStack Query.
// Rotas reais: src/Modules/Administracao/.../AdministracaoEndpoints.cs (MapFornecedores)
// e os Commands/Queries em .../Application/Fornecedores.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham os records de Application/Fornecedores; enums = nome do valor)
// ---------------------------------------------------------------------------

/** SituacaoFornecedor (rules §2.4). */
export type SituacaoFornecedor = 'Ativo' | 'Sancionado' | 'Inativo';

/** NivelCadastralSICAF (rules §2.2 — art. 87). */
export type NivelCadastralSICAF =
  | 'NaoCadastrado'
  | 'CredenciamentoNivel1'
  | 'HabilitacaoJuridicaNivel2'
  | 'RegularidadeFiscalNivel3'
  | 'QualificacaoEconomicaNivel4'
  | 'QualificacaoTecnicaNivel5';

/** TipoSancao (rules §2.2 — art. 156). */
export type TipoSancao = 'Advertencia' | 'Multa' | 'Impedimento' | 'Inidoneidade';

/** Resumo de uma sancao (SancaoResumo em Application). */
export interface SancaoResumo {
  id: string;
  tipo: TipoSancao;
  dataInicio: string;
  dataFim: string | null;
  processoAdministrativo: string;
  valorMulta: number | null;
}

/** Resumo de fornecedor para listagens (FornecedorResumo em Application). */
export interface FornecedorResumo {
  id: string;
  cnpj: string;
  razaoSocial: string;
  situacao: SituacaoFornecedor;
  nivelCadastralSICAF: NivelCadastralSICAF;
}

/** Detalhe de fornecedor (FornecedorDetalhe em Application). */
export interface FornecedorDetalhe {
  id: string;
  cnpj: string;
  razaoSocial: string;
  nivelCadastralSICAF: NivelCadastralSICAF;
  situacao: SituacaoFornecedor;
  estaImpedido: boolean;
  sancoes: SancaoResumo[];
}

// --- Entradas de comando ---------------------------------------------------

/** CadastrarFornecedorCommand. */
export interface CadastrarFornecedorInput {
  cnpj: string;
  razaoSocial: string;
}

/** AplicarSancaoCommand (payload em /fornecedores/{id}/sancoes). */
export interface AplicarSancaoInput {
  tipo: TipoSancao;
  dataInicio: string;
  dataFim?: string | null;
  processoAdministrativo: string;
  fundamentacao: string;
  valorMulta?: number | null;
}

/** AtualizarNivelSicafCommand. */
export interface AtualizarNivelSicafInput {
  nivel: NivelCadastralSICAF;
}

// ---------------------------------------------------------------------------
// Catalogos de apresentacao (rotulos PT-BR dos enums)
// ---------------------------------------------------------------------------

export const NIVEL_SICAF_LABEL: Record<NivelCadastralSICAF, string> = {
  NaoCadastrado: 'Nao cadastrado',
  CredenciamentoNivel1: 'Credenciamento (nivel I)',
  HabilitacaoJuridicaNivel2: 'Habilitacao juridica (nivel II)',
  RegularidadeFiscalNivel3: 'Regularidade fiscal e trabalhista (nivel III)',
  QualificacaoEconomicaNivel4: 'Qualificacao economico-financeira (nivel IV)',
  QualificacaoTecnicaNivel5: 'Qualificacao tecnica (nivel V)',
};

export const TIPO_SANCAO_LABEL: Record<TipoSancao, string> = {
  Advertencia: 'Advertencia (art. 156, I)',
  Multa: 'Multa (art. 156, II)',
  Impedimento: 'Impedimento de licitar e contratar (art. 156, III)',
  Inidoneidade: 'Declaracao de inidoneidade (art. 156, IV)',
};

export const SITUACAO_LABEL: Record<SituacaoFornecedor, string> = {
  Ativo: 'Ativo',
  Sancionado: 'Sancionado',
  Inativo: 'Inativo',
};

/** Sancoes impeditivas (rules §2.4 — conjunto Impeditivos). */
export const TIPOS_IMPEDITIVOS: ReadonlySet<TipoSancao> = new Set<TipoSancao>([
  'Impedimento',
  'Inidoneidade',
]);

/** Lista ordenada para <Select>. */
export const NIVEIS_SICAF: NivelCadastralSICAF[] = [
  'NaoCadastrado',
  'CredenciamentoNivel1',
  'HabilitacaoJuridicaNivel2',
  'RegularidadeFiscalNivel3',
  'QualificacaoEconomicaNivel4',
  'QualificacaoTecnicaNivel5',
];

export const TIPOS_SANCAO: TipoSancao[] = ['Advertencia', 'Multa', 'Impedimento', 'Inidoneidade'];

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const fornecedorKeys = {
  all: ['administracao', 'fornecedores'] as const,
  impedidos: (referencia: string) => [...fornecedorKeys.all, 'impedidos', referencia] as const,
  detalhe: (id: string) => [...fornecedorKeys.all, 'detalhe', id] as const,
  porCnpj: (cnpj: string) => [...fornecedorKeys.all, 'cnpj', cnpj] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP (rotas reais /api/administracao/fornecedores/...)
// ---------------------------------------------------------------------------

// Query — ListarFornecedoresImpedidos (GET /fornecedores/impedidos?referencia=).
function listarImpedidos(referencia: string, signal?: AbortSignal): Promise<FornecedorResumo[]> {
  return http.get<FornecedorResumo[]>('/administracao/fornecedores/impedidos', {
    query: { referencia },
    signal,
  });
}

// Query — ObterFornecedorPorId (GET /fornecedores/{id}).
function obterPorId(id: string, signal?: AbortSignal): Promise<FornecedorDetalhe> {
  return http.get<FornecedorDetalhe>(`/administracao/fornecedores/${id}`, { signal });
}

// Query — ObterFornecedorPorCnpj (GET /fornecedores/por-cnpj/{cnpj}).
function obterPorCnpj(cnpj: string, signal?: AbortSignal): Promise<FornecedorDetalhe> {
  return http.get<FornecedorDetalhe>(`/administracao/fornecedores/por-cnpj/${encodeURIComponent(cnpj)}`, {
    signal,
  });
}

// Command — CadastrarFornecedor (POST /fornecedores) -> { id }.
function cadastrar(input: CadastrarFornecedorInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/fornecedores', input);
}

// Command — AplicarSancao (POST /fornecedores/{id}/sancoes) -> { sancaoId }.
function aplicarSancao(fornecedorId: string, input: AplicarSancaoInput): Promise<{ sancaoId: string }> {
  return http.post<{ sancaoId: string }>(`/administracao/fornecedores/${fornecedorId}/sancoes`, input);
}

// Command — AtualizarNivelSicaf (PUT /fornecedores/{id}/nivel-sicaf) -> 204.
function atualizarNivelSicaf(fornecedorId: string, input: AtualizarNivelSicafInput): Promise<void> {
  return http.put<void>(`/administracao/fornecedores/${fornecedorId}/nivel-sicaf`, input);
}

// Command — ReabilitarFornecedor (POST /fornecedores/{id}/reabilitar) -> 204.
function reabilitar(fornecedorId: string): Promise<void> {
  return http.post<void>(`/administracao/fornecedores/${fornecedorId}/reabilitar`);
}

// Command — InativarFornecedor (POST /fornecedores/{id}/inativar) -> 204.
function inativar(fornecedorId: string): Promise<void> {
  return http.post<void>(`/administracao/fornecedores/${fornecedorId}/inativar`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** ListarFornecedoresImpedidos na data de referencia (tenant-scoped). */
export function useFornecedoresImpedidos(referencia: string, enabled = true) {
  return useQuery({
    queryKey: fornecedorKeys.impedidos(referencia),
    queryFn: ({ signal }) => listarImpedidos(referencia, signal),
    enabled: enabled && referencia.trim().length > 0,
  });
}

/** ObterFornecedorPorId. */
export function useFornecedor(id: string) {
  return useQuery({
    queryKey: fornecedorKeys.detalhe(id),
    queryFn: ({ signal }) => obterPorId(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** ObterFornecedorPorCnpj (busca sob demanda; `enabled` controla disparo). */
export function useFornecedorPorCnpj(cnpj: string, enabled = true) {
  return useQuery({
    queryKey: fornecedorKeys.porCnpj(cnpj),
    queryFn: ({ signal }) => obterPorCnpj(cnpj, signal),
    enabled: enabled && cnpj.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** CadastrarFornecedor e invalida as listas afetadas. */
export function useCadastrarFornecedor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrar,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.all });
    },
  });
}

/** AplicarSancao a um fornecedor e revalida o detalhe + listas. */
export function useAplicarSancao(fornecedorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AplicarSancaoInput) => aplicarSancao(fornecedorId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.detalhe(fornecedorId) });
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.all });
    },
  });
}

/** AtualizarNivelSicaf e revalida o detalhe. */
export function useAtualizarNivelSicaf(fornecedorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AtualizarNivelSicafInput) => atualizarNivelSicaf(fornecedorId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.detalhe(fornecedorId) });
    },
  });
}

/** ReabilitarFornecedor (volta a Ativo) e revalida o detalhe + listas. */
export function useReabilitarFornecedor(fornecedorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reabilitar(fornecedorId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.detalhe(fornecedorId) });
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.all });
    },
  });
}

/** InativarFornecedor (terminal) e revalida o detalhe + listas. */
export function useInativarFornecedor(fornecedorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => inativar(fornecedorId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.detalhe(fornecedorId) });
      void queryClient.invalidateQueries({ queryKey: fornecedorKeys.all });
    },
  });
}
