// Camada de API do agregado Vereador (modulo Legislativo). DTOs + acesso HTTP
// tipado + hooks TanStack Query, cobrindo os endpoints de /api/legislativo/vereadores.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Resumo de um vereador para listagem. */
export interface VereadorResumo {
  id: string;
  nomeParlamentar: string;
  partido: string;
  legislatura: string;
  cargoMesa: string;
  situacao: string;
}

/** Detalhe completo de um vereador. */
export interface VereadorDetalhe extends VereadorResumo {
  nomeCivil: string;
}

/** Payload de cadastro/edicao de vereador. */
export interface VereadorInput {
  nomeCivil: string;
  nomeParlamentar: string;
  partido: string;
  legislatura: string;
  cargoMesa: number;
  situacao: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarVereadores(signal?: AbortSignal): Promise<VereadorResumo[]> {
  return http.get<VereadorResumo[]>('/legislativo/vereadores', { signal });
}

function obterVereador(id: string, signal?: AbortSignal): Promise<VereadorDetalhe> {
  return http.get<VereadorDetalhe>(`/legislativo/vereadores/${id}`, { signal });
}

async function criarVereador(input: VereadorInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/vereadores', input);
  return id;
}

function atualizarVereador(id: string, input: VereadorInput): Promise<void> {
  return http.put<void>(`/legislativo/vereadores/${id}`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista os vereadores do tenant. */
export function useVereadores() {
  return useQuery({
    queryKey: legislativoKeys.vereadores(),
    queryFn: ({ signal }) => listarVereadores(signal),
  });
}

/** Detalhe de um vereador. */
export function useVereador(id: string) {
  return useQuery({
    queryKey: legislativoKeys.vereador(id),
    queryFn: ({ signal }) => obterVereador(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Cadastra um novo vereador e invalida a lista. */
export function useCriarVereador() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarVereador,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.vereadores() });
    },
  });
}

/** Atualiza um vereador e invalida a lista e o detalhe. */
export function useAtualizarVereador(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: VereadorInput) => atualizarVereador(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.vereadores() });
      queryClient.invalidateQueries({ queryKey: legislativoKeys.vereador(id) });
    },
  });
}
