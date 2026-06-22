// Camada de API da gestão de USUÁRIOS (módulo Administração do Sistema / Identidade).
// Segue o PADRÃO-OURO do módulo Protocolo (processo.api.ts):
//   - DTOs no topo (espelham os Commands/Queries reais do backend de Identidade);
//   - query keys centralizadas para invalidação consistente;
//   - funções de acesso via http client tipado (Authorization + ProblemDetails);
//   - hooks TanStack Query (useQuery/useMutation) para CADA operação, com invalidação.
//
// Contrato REAL (Identidade):
//   GET    /api/identidade/usuarios                 -> ListarUsuarios       -> UsuarioResumo[]
//   GET    /api/identidade/usuarios/{id}            -> ObterUsuario         -> UsuarioDetalhe
//   GET    /api/identidade/usuarios/{id}/permissoes -> ObterPermissoes      -> string[]
//   POST   /api/identidade/usuarios                 -> CriarUsuario         -> { id }
//   PUT    /api/identidade/usuarios/{id}            -> AtualizarUsuario     -> 204
//   PUT    /api/identidade/usuarios/{id}/senha      -> RedefinirSenha       -> 204
//   PUT    /api/identidade/usuarios/{id}/papeis     -> DefinirPapeis        -> 204
//   POST   /api/identidade/usuarios/{id}/ativar     -> AtivarUsuario        -> 204
//   POST   /api/identidade/usuarios/{id}/desativar  -> DesativarUsuario     -> 204
//   GET    /api/identidade/papeis                   -> ListarPapeis         -> PapelResumo[]
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Papel (RBAC) — projeção mínima usada na seleção de papéis do usuário. */
export interface PapelResumo {
  id: string;
  nome: string;
}

/** Projeção de resumo da lista de usuários. */
export interface UsuarioResumo {
  id: string;
  nome: string;
  email: string;
  ativo: boolean;
  papeis: string[];
}

/** Entrada do command CriarUsuario: { nome, email, senha, papeisIds[] }. */
export interface CriarUsuarioInput {
  nome: string;
  email: string;
  senha: string;
  papeisIds: string[];
}

/** Entrada do command AtualizarUsuario: { nome, email }. */
export interface AtualizarUsuarioInput {
  nome: string;
  email: string;
}

/** Entrada do command RedefinirSenha: { novaSenha }. */
export interface RedefinirSenhaInput {
  novaSenha: string;
}

/** Entrada do command DefinirPapeis: { papeisIds[] }. */
export interface DefinirPapeisInput {
  papeisIds: string[];
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const usuarioKeys = {
  all: ['identidade', 'usuarios'] as const,
  lista: () => [...usuarioKeys.all, 'lista'] as const,
  porId: (id: string) => [...usuarioKeys.all, 'id', id] as const,
};

export const papelKeys = {
  all: ['identidade', 'papeis'] as const,
  lista: () => [...papelKeys.all, 'lista'] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarUsuarios(signal?: AbortSignal): Promise<UsuarioResumo[]> {
  return http.get<UsuarioResumo[]>('/identidade/usuarios', { signal });
}

function listarPapeis(signal?: AbortSignal): Promise<PapelResumo[]> {
  return http.get<PapelResumo[]>('/identidade/papeis', { signal });
}

function criarUsuario(input: CriarUsuarioInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/identidade/usuarios', input);
}

function atualizarUsuario(id: string, input: AtualizarUsuarioInput): Promise<void> {
  return http.put<void>(`/identidade/usuarios/${id}`, input);
}

function redefinirSenha(id: string, input: RedefinirSenhaInput): Promise<void> {
  return http.put<void>(`/identidade/usuarios/${id}/senha`, input);
}

function definirPapeis(id: string, input: DefinirPapeisInput): Promise<void> {
  return http.put<void>(`/identidade/usuarios/${id}/papeis`, input);
}

function ativarUsuario(id: string): Promise<void> {
  return http.post<void>(`/identidade/usuarios/${id}/ativar`);
}

function desativarUsuario(id: string): Promise<void> {
  return http.post<void>(`/identidade/usuarios/${id}/desativar`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista todos os usuários do tenant. */
export function useUsuarios() {
  return useQuery({
    queryKey: usuarioKeys.lista(),
    queryFn: ({ signal }) => listarUsuarios(signal),
  });
}

/** Lista os papéis disponíveis (catálogo para seleção). */
export function usePapeis(enabled = true) {
  return useQuery({
    queryKey: papelKeys.lista(),
    queryFn: ({ signal }) => listarPapeis(signal),
    enabled,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS (todos invalidam a lista de usuários)
// ---------------------------------------------------------------------------

/** Cria um novo usuário com os papéis selecionados. Invalida a lista. */
export function useCriarUsuario() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarUsuario,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usuarioKeys.all });
    },
  });
}

/** Atualiza nome e e-mail do usuário. Invalida a lista. */
export function useAtualizarUsuario() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: AtualizarUsuarioInput }) =>
      atualizarUsuario(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usuarioKeys.all });
    },
  });
}

/** Redefine a senha do usuário (não invalida a lista — não altera projeção). */
export function useRedefinirSenha() {
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: RedefinirSenhaInput }) =>
      redefinirSenha(id, input),
  });
}

/** Define o conjunto de papéis do usuário. Invalida a lista. */
export function useDefinirPapeis() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: DefinirPapeisInput }) =>
      definirPapeis(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usuarioKeys.all });
    },
  });
}

/** Ativa o usuário. Invalida a lista. */
export function useAtivarUsuario() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ativarUsuario,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usuarioKeys.all });
    },
  });
}

/** Desativa o usuário. Invalida a lista. */
export function useDesativarUsuario() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: desativarUsuario,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usuarioKeys.all });
    },
  });
}
