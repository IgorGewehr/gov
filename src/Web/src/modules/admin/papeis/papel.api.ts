// Camada de API da gestão de Papéis (RBAC) do módulo Administração do Sistema.
// Segue o PADRÃO-OURO de protocolo/processo.api.ts:
//   - DTOs no topo (espelham os Commands/Queries reais de ...Identidade.Application);
//   - query keys centralizadas para invalidação consistente;
//   - funções de acesso via http client tipado (Authorization + ProblemDetails);
//   - hooks TanStack Query (useQuery/useMutation) para CADA operação.
//
// Contrato REAL (endpoints de Identidade):
//   GET  /api/identidade/papeis                       -> ListarPapeis        -> Papel[]
//   POST /api/identidade/papeis                        -> CriarPapel          -> { id } | 201
//   PUT  /api/identidade/papeis/{id}/permissoes        -> DefinirPermissoes   -> 204
//   GET  /api/identidade/permissoes                    -> CatalogoPermissoes  -> Permissao[]
//
// Gating de UI: "identidade.usuarios.gerenciar" (PERM_USUARIOS_GERENCIAR).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Projeção de papel (ListarPapeis). O backend retorna a lista de permissões. */
export interface Papel {
  id: string;
  nome: string;
  permissoes: string[];
}

/**
 * Item do catálogo canônico de permissões (GET /api/identidade/permissoes).
 * O backend pode devolver apenas a chave (string) OU um objeto com descrição;
 * normalizamos para { chave, descricao? } em `obterCatalogoPermissoes`.
 */
export interface Permissao {
  chave: string;
  descricao?: string | null;
}

/** Resposta crua do catálogo, tolerante a string[] ou objeto[]. */
type PermissaoCrua = string | { chave?: string; nome?: string; descricao?: string | null };

// --- Entradas de comando ---

/** CriarPapelCommand(Nome, Permissoes). */
export interface CriarPapelInput {
  nome: string;
  permissoes: string[];
}

/** DefinirPermissoesPayload(Permissoes). */
export interface DefinirPermissoesInput {
  permissoes: string[];
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const papelKeys = {
  all: ['identidade', 'papeis'] as const,
  lista: () => [...papelKeys.all, 'lista'] as const,
  permissoesCatalogo: () => ['identidade', 'permissoes', 'catalogo'] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarPapeis(signal?: AbortSignal): Promise<Papel[]> {
  return http.get<Papel[]>('/identidade/papeis', { signal });
}

async function obterCatalogoPermissoes(signal?: AbortSignal): Promise<Permissao[]> {
  const cru = await http.get<PermissaoCrua[]>('/identidade/permissoes', { signal });
  return cru.map((item) =>
    typeof item === 'string'
      ? { chave: item }
      : { chave: item.chave ?? item.nome ?? '', descricao: item.descricao ?? null },
  );
}

function criarPapel(input: CriarPapelInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/identidade/papeis', input);
}

function definirPermissoes(papelId: string, input: DefinirPermissoesInput): Promise<void> {
  return http.put<void>(`/identidade/papeis/${papelId}/permissoes`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista todos os papéis do tenant. */
export function usePapeis() {
  return useQuery({
    queryKey: papelKeys.lista(),
    queryFn: ({ signal }) => listarPapeis(signal),
  });
}

/** Catálogo canônico de permissões (sob demanda via `enabled`). */
export function useCatalogoPermissoes(enabled = true) {
  return useQuery({
    queryKey: papelKeys.permissoesCatalogo(),
    queryFn: ({ signal }) => obterCatalogoPermissoes(signal),
    enabled,
    staleTime: 5 * 60 * 1000,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Cria um novo papel com permissões iniciais. Invalida a lista de papéis. */
export function useCriarPapel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarPapel,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: papelKeys.lista() });
    },
  });
}

/** Redefine (substitui) as permissões de um papel. Invalida a lista de papéis. */
export function useDefinirPermissoes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ papelId, input }: { papelId: string; input: DefinirPermissoesInput }) =>
      definirPermissoes(papelId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: papelKeys.lista() });
    },
  });
}
