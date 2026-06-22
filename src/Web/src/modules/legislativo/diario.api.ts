// Camada de API do agregado Diario Oficial (modulo Legislativo, parte 2). DTOs +
// acesso HTTP tipado + hooks TanStack Query, cobrindo /api/legislativo/diario:
// lista de edicoes, detalhe, montagem (criar + adicionar materias) e publicacao.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import {
  legislativoKeys,
  type CriacaoResponse,
  type PaginaResultado,
} from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Resumo de uma edicao do Diario Oficial. */
export interface EdicaoResumo {
  id: string;
  numero: number;
  /** Ano da edicao (back: Ano; o backend gera o numero sequencial). */
  ano: number;
  situacao: string;
  /** Data oficial de publicacao (back: DataPublicacao, ISO) ou null se em montagem. */
  dataPublicacao: string | null;
  totalMaterias: number;
}

/** Materia (item) de uma edicao do Diario. */
export interface MateriaResumo {
  id: string;
  ordem: number;
  tipo: string;
  titulo: string;
  /** Conteudo bruto (back: Conteudo, opcional quando ha ReferenciaId). */
  conteudo: string | null;
  /** Referencia a entidade de origem (back: ReferenciaId), se houver. */
  referenciaId?: string | null;
}

/** Detalhe de uma edicao (cabecalho + materias + dados de publicacao). */
export interface EdicaoDetalhe {
  id: string;
  numero: number;
  ano: number;
  situacao: string;
  /** Data oficial de publicacao (back: DataPublicacao, ISO) ou null se em montagem. */
  dataPublicacao: string | null;
  /** Edicao original, se esta for retificacao (back: EdicaoOriginalId). */
  edicaoOriginalId?: string | null;
  materias: MateriaResumo[];
}

/** Payload de criacao (montagem) de uma edicao do Diario (back gera o numero). */
export interface EdicaoInput {
  /** Ano da edicao (back: Ano, obrigatorio, 1900–2100). */
  ano: number;
}

/** Payload de inclusao de materia em uma edicao. */
export interface MateriaInput {
  /** Especie da materia (back: TipoMateria, int). */
  tipoMateria: number;
  titulo: string;
  conteudo: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

async function listarEdicoes(signal?: AbortSignal): Promise<EdicaoResumo[]> {
  // O backend devolve um envelope paginado { itens, total, pagina, tamanho }.
  const pagina = await http.get<PaginaResultado<EdicaoResumo>>('/legislativo/diario/edicoes', {
    signal,
  });
  return pagina.itens;
}

function obterEdicao(id: string, signal?: AbortSignal): Promise<EdicaoDetalhe> {
  return http.get<EdicaoDetalhe>(`/legislativo/diario/edicoes/${id}`, { signal });
}

async function criarEdicao(input: EdicaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/diario/edicoes', input);
  return id;
}

function adicionarMateria(edicaoId: string, input: MateriaInput): Promise<void> {
  return http.post<void>(`/legislativo/diario/edicoes/${edicaoId}/materias`, input);
}

function publicarEdicao(edicaoId: string): Promise<void> {
  return http.post<void>(`/legislativo/diario/edicoes/${edicaoId}/publicacao`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as edicoes do Diario Oficial do tenant. */
export function useEdicoes() {
  return useQuery({
    queryKey: legislativoKeys.diarioEdicoes(),
    queryFn: ({ signal }) => listarEdicoes(signal),
  });
}

/** Detalhe de uma edicao (cabecalho + materias). */
export function useEdicao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.diarioEdicao(id),
    queryFn: ({ signal }) => obterEdicao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Monta (cria) uma nova edicao e invalida a lista. */
export function useCriarEdicao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarEdicao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.diarioEdicoes() });
    },
  });
}

/** Invalida o detalhe de uma edicao e a lista. */
function useInvalidarEdicao(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.diarioEdicao(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.diarioEdicoes() });
  };
}

/** Adiciona uma materia a edicao em montagem. */
export function useAdicionarMateria(id: string) {
  const invalidar = useInvalidarEdicao(id);
  return useMutation({
    mutationFn: (input: MateriaInput) => adicionarMateria(id, input),
    onSuccess: invalidar,
  });
}

/** Publica (verbo fino) a edicao: torna-a oficial e imutavel. */
export function usePublicarEdicao(id: string) {
  const invalidar = useInvalidarEdicao(id);
  return useMutation({ mutationFn: () => publicarEdicao(id), onSuccess: invalidar });
}
