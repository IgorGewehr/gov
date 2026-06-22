// Camada de API do agregado Comissao (modulo Legislativo, parte 2). DTOs + acesso
// HTTP tipado + hooks TanStack Query, cobrindo /api/legislativo/comissoes: lista,
// detalhe (composicao), criacao e inclusao de membros (vereadores).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Resumo de uma comissao para listagem. */
export interface ComissaoResumo {
  id: string;
  nome: string;
  tipo: string;
  totalMembros: number;
}

/** Membro (vereador) de uma comissao. */
export interface MembroComissaoResumo {
  vereadorId: string;
  vereadorNome: string;
  cargo: string;
}

/** Detalhe de uma comissao (cabecalho + composicao). */
export interface ComissaoDetalhe {
  id: string;
  nome: string;
  tipo: string;
  finalidade: string;
  membros: MembroComissaoResumo[];
}

/** Payload de criacao de comissao. */
export interface ComissaoInput {
  nome: string;
  tipo: number;
  finalidade: string;
}

/** Payload de inclusao de membro na comissao. */
export interface MembroInput {
  vereadorId: string;
  cargo: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarComissoes(signal?: AbortSignal): Promise<ComissaoResumo[]> {
  return http.get<ComissaoResumo[]>('/legislativo/comissoes', { signal });
}

function obterComissao(id: string, signal?: AbortSignal): Promise<ComissaoDetalhe> {
  return http.get<ComissaoDetalhe>(`/legislativo/comissoes/${id}`, { signal });
}

async function criarComissao(input: ComissaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/comissoes', input);
  return id;
}

function adicionarMembro(comissaoId: string, input: MembroInput): Promise<void> {
  return http.post<void>(`/legislativo/comissoes/${comissaoId}/membros`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as comissoes do tenant. */
export function useComissoes() {
  return useQuery({
    queryKey: legislativoKeys.comissoes(),
    queryFn: ({ signal }) => listarComissoes(signal),
  });
}

/** Detalhe de uma comissao (inclui a composicao de membros). */
export function useComissao(id: string) {
  return useQuery({
    queryKey: legislativoKeys.comissao(id),
    queryFn: ({ signal }) => obterComissao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Cria uma nova comissao e invalida a lista. */
export function useCriarComissao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarComissao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.comissoes() });
    },
  });
}

/** Inclui um vereador (membro) na composicao da comissao. */
export function useAdicionarMembro(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: MembroInput) => adicionarMembro(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.comissao(id) });
      queryClient.invalidateQueries({ queryKey: legislativoKeys.comissoes() });
    },
  });
}
