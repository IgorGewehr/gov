// Camada de API do agregado Familia (modulo AssistenciaSocial) — segue o padrao-ouro
// de src/modules/tributos: DTOs -> query keys -> funcoes http tipadas -> hooks TanStack Query.
// Rotas REAIS expostas por AssistenciaSocialEndpoints.cs (grupo /api/assistenciasocial).
//
// Cobertura (todas as operacoes do agregado Familia):
//   Queries:
//     - ObterFamiliasDoTerritorio  GET  /familias?territorio={territorio}
//     - ObterResumoCadUnico        GET  /familias/{familiaId}/cadunico
//   Commands:
//     - ReferenciarFamilia         POST /familias                         -> { id }
//     - AtualizarRendaFamiliar     PUT  /familias/{familiaId}/renda       -> 204
//     - ProcessarVigenciaCadastral POST /familias/{familiaId}/vigencia-cadastral -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import type { TagVariant } from '../../../components/ui';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situacao da familia no ciclo de referenciamento (enum SituacaoFamilia do dominio). */
export type SituacaoFamilia = 'Referenciada' | 'AtualizacaoVencida' | 'Regularizada';

/** Parentesco do membro com o responsavel familiar (enum Parentesco — valor numerico). */
export type ParentescoValor = 1 | 2 | 3 | 9;

/** Membro do nucleo familiar (composicao + renda). Espelha MembroFamiliarDto (Application). */
export interface MembroFamiliarInput {
  /** CPF do membro (com ou sem mascara). */
  cpf: string;
  /** Parentesco: 1=Responsavel familiar, 2=Conjuge, 3=Filho, 9=Outro. */
  parentesco: ParentescoValor;
  /** Data de nascimento (ISO yyyy-mm-dd). */
  dataNascimento: string;
  /** Renda individual declarada (nao-negativa). */
  rendaIndividual: number;
  /** Indicador de pessoa com deficiencia (dado sensivel — art. 11 LGPD). */
  ehPcd: boolean;
}

/** Endereco territorializado (vinculado ao territorio de cobertura do CRAS). */
export interface EnderecoTerritorializadoInput {
  logradouro: string;
  municipio: string;
  cep: string;
  /** Territorio de cobertura do CRAS. */
  territorio: string;
}

/** Entrada do comando ReferenciarFamilia (ReferenciarFamiliaCommand). */
export interface ReferenciarFamiliaInput {
  nis: string;
  cpfResponsavel: string;
  unidadeAtendimentoId: string;
  endereco: EnderecoTerritorializadoInput;
  membros: MembroFamiliarInput[];
}

/** Entrada do comando AtualizarRendaFamiliar (AtualizarRendaPayload). */
export interface AtualizarRendaFamiliarInput {
  familiaId: string;
  membros: MembroFamiliarInput[];
}

/** Projecao de leitura de familia por territorio (FamiliaResumo — NIS mascarado, LGPD). */
export interface FamiliaResumo {
  id: string;
  nisMascarado: string;
  unidadeAtendimentoId: string;
  territorio: string;
  rendaPerCapita: number;
  situacao: SituacaoFamilia;
  dataReferenciamento: string;
  dataUltimaAtualizacaoCadastral: string;
}

/** Folha resumo do CadUnico (read model federal, somente leitura — ResumoCadUnico). */
export interface ResumoCadUnico {
  nisMascarado: string;
  rendaFamiliarDeclarada: number;
  quantidadeMembros: number;
  dataUltimaAtualizacao: string;
  dentroDaVigencia: boolean;
}

/** Resposta do POST /familias (id da familia criada). */
interface ReferenciarFamiliaResposta {
  id: string;
}

// ---------------------------------------------------------------------------
// Apresentacao (helpers de UI)
// ---------------------------------------------------------------------------

/** Rotulo legivel (pt-BR) da situacao da familia. */
export const SITUACAO_LABEL: Record<SituacaoFamilia, string> = {
  Referenciada: 'Referenciada',
  AtualizacaoVencida: 'Atualização vencida',
  Regularizada: 'Regularizada',
};

/** Mapeia a situacao da familia para a variante semantica da Tag (cor + texto). */
export function situacaoTagVariant(situacao: SituacaoFamilia): TagVariant {
  switch (situacao) {
    case 'Referenciada':
      return 'info';
    case 'Regularizada':
      return 'success';
    case 'AtualizacaoVencida':
      return 'danger';
    default:
      return 'default';
  }
}

/** Opcoes de parentesco para selects de formulario. */
export const PARENTESCO_OPCOES: { value: string; label: string }[] = [
  { value: '1', label: 'Responsável familiar' },
  { value: '2', label: 'Cônjuge' },
  { value: '3', label: 'Filho(a)' },
  { value: '9', label: 'Outro' },
];

/** Rotulo legivel (pt-BR) do parentesco a partir do valor numerico. */
export function parentescoLabel(valor: ParentescoValor): string {
  return PARENTESCO_OPCOES.find((o) => o.value === String(valor))?.label ?? 'Outro';
}

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const familiaKeys = {
  all: ['assistenciasocial', 'familias'] as const,
  porTerritorio: (territorio: string) => [...familiaKeys.all, 'territorio', territorio] as const,
  cadunico: (familiaId: string) => [...familiaKeys.all, 'cadunico', familiaId] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarFamiliasPorTerritorio(territorio: string, signal?: AbortSignal): Promise<FamiliaResumo[]> {
  return http.get<FamiliaResumo[]>('/assistenciasocial/familias', { query: { territorio }, signal });
}

function obterResumoCadUnico(familiaId: string, signal?: AbortSignal): Promise<ResumoCadUnico> {
  return http.get<ResumoCadUnico>(`/assistenciasocial/familias/${familiaId}/cadunico`, { signal });
}

function referenciarFamilia(input: ReferenciarFamiliaInput): Promise<ReferenciarFamiliaResposta> {
  return http.post<ReferenciarFamiliaResposta>('/assistenciasocial/familias', {
    nis: input.nis,
    cpfResponsavel: input.cpfResponsavel,
    unidadeAtendimentoId: input.unidadeAtendimentoId,
    endereco: input.endereco,
    membros: input.membros,
  });
}

function atualizarRendaFamiliar(input: AtualizarRendaFamiliarInput): Promise<void> {
  // PUT /familias/{id}/renda — corpo: { membros }. Responde 204 No Content.
  return http.put<void>(`/assistenciasocial/familias/${input.familiaId}/renda`, {
    membros: input.membros,
  });
}

function processarVigenciaCadastral(familiaId: string): Promise<void> {
  // POST /familias/{id}/vigencia-cadastral — sem corpo. Responde 204 No Content.
  return http.post<void>(`/assistenciasocial/familias/${familiaId}/vigencia-cadastral`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as familias de um territorio (tenant-scoped no backend). Disparo sob demanda. */
export function useFamiliasDoTerritorio(territorio: string, enabled = true) {
  return useQuery({
    queryKey: familiaKeys.porTerritorio(territorio),
    queryFn: ({ signal }) => listarFamiliasPorTerritorio(territorio, signal),
    enabled: enabled && territorio.trim().length > 0,
  });
}

/** Resumo do CadUnico (read model federal) de uma familia. */
export function useResumoCadUnico(familiaId: string, enabled = true) {
  return useQuery({
    queryKey: familiaKeys.cadunico(familiaId),
    queryFn: ({ signal }) => obterResumoCadUnico(familiaId, signal),
    enabled: enabled && familiaId.trim().length > 0,
  });
}

/** Referencia uma nova familia ao CRAS e invalida a lista do territorio. */
export function useReferenciarFamilia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: referenciarFamilia,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: familiaKeys.all });
    },
  });
}

/** Atualiza a composicao/renda da familia (recalcula renda per capita; pode regularizar). */
export function useAtualizarRendaFamiliar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: atualizarRendaFamiliar,
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: familiaKeys.all });
      queryClient.invalidateQueries({ queryKey: familiaKeys.cadunico(variables.familiaId) });
    },
  });
}

/** Processa a vigencia cadastral (sinaliza AtualizacaoVencida quando > 24 meses). */
export function useProcessarVigenciaCadastral() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: processarVigenciaCadastral,
    onSuccess: (_data, familiaId) => {
      queryClient.invalidateQueries({ queryKey: familiaKeys.all });
      queryClient.invalidateQueries({ queryKey: familiaKeys.cadunico(familiaId) });
    },
  });
}
