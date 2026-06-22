// Camada de API do agregado Diario Oficial (modulo Legislativo, parte 2). DTOs +
// acesso HTTP tipado + hooks TanStack Query, cobrindo /api/legislativo/diario:
// lista de edicoes, detalhe, montagem (criar + adicionar materias) e publicacao.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Resumo de uma edicao do Diario Oficial. */
export interface EdicaoResumo {
  id: string;
  numero: number;
  dataReferencia: string;
  situacao: string;
  totalMaterias: number;
}

/** Materia (item) de uma edicao do Diario. */
export interface MateriaResumo {
  id: string;
  ordem: number;
  tipo: string;
  titulo: string;
  conteudo: string;
}

/** Detalhe de uma edicao (cabecalho + materias + dados de publicacao). */
export interface EdicaoDetalhe {
  id: string;
  numero: number;
  dataReferencia: string;
  situacao: string;
  publicadaEm: string | null;
  materias: MateriaResumo[];
}

/** Payload de criacao (montagem) de uma edicao do Diario. */
export interface EdicaoInput {
  numero: number;
  dataReferencia: string;
}

/** Payload de inclusao de materia em uma edicao. */
export interface MateriaInput {
  tipo: number;
  titulo: string;
  conteudo: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarEdicoes(signal?: AbortSignal): Promise<EdicaoResumo[]> {
  return http.get<EdicaoResumo[]>('/legislativo/diario/edicoes', { signal });
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
