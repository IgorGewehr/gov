// Camada de API da ESTRUTURA ORGANIZACIONAL (Unidades Organizacionais / Identidade).
// Segue o PADRÃO-OURO do módulo de Usuários (usuario.api.ts):
//   - DTOs no topo (espelham os Commands/Queries reais do backend de Identidade);
//   - query keys centralizadas para invalidação consistente;
//   - funções de acesso via http client tipado (Authorization + ProblemDetails);
//   - hooks TanStack Query (useQuery/useMutation) para CADA operação, com invalidação.
//
// Contrato REAL (Identidade — validado em IdentidadeEndpoints.cs):
//   GET    /api/identidade/unidades                       -> ListarArvoreUnidades -> NoUnidade[] (árvore)
//   POST   /api/identidade/unidades                       -> CriarUnidade         -> { id }
//   PUT    /api/identidade/unidades/{id}                  -> RenomearUnidade      -> 204  { nome, tipo }
//   POST   /api/identidade/unidades/{id}/mover            -> MoverUnidade         -> 204  { novoPaiId }
//   POST   /api/identidade/unidades/{id}/ativar           -> AtivarUnidade        -> 204
//   POST   /api/identidade/unidades/{id}/desativar        -> DesativarUnidade     -> 204
//   POST   /api/identidade/usuarios/{id}/atribuicoes      -> AtribuirPapel        -> 204 (atribuição escopada)
//   DELETE /api/identidade/usuarios/{id}/atribuicoes      -> RevogarAtribuicao    -> 204
//   GET    /api/identidade/papeis                         -> ListarPapeis         -> PapelResumo[]
//
// OBS de contrato:
//   - O enum TipoUnidade é serializado por NOME (JsonStringEnumConverter no ApiHost).
//   - O nó da árvore expõe "ativa" (booleano), e os filhos aninhados em "filhos".
//   - O PUT de renomear EXIGE { nome, tipo } (não apenas nome).
//   - O POST de mover usa a chave "novoPaiId".
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Natureza administrativa da UO (espelha o enum TipoUnidade do domínio). */
export type TipoUnidade = 'Secretaria' | 'Departamento' | 'Setor' | 'Gabinete' | 'Fundo';

/** Nó da árvore de UOs (projeção de leitura, com filhos aninhados). */
export interface NoUnidade {
  id: string;
  codigo: string;
  nome: string;
  tipo: TipoUnidade;
  ativa: boolean;
  filhos: NoUnidade[];
}

/** Papel (RBAC) — projeção mínima usada na atribuição escopada. */
export interface PapelResumo {
  id: string;
  nome: string;
}

/** Entrada do command CriarUnidade: { codigo, nome, tipo, unidadePaiId? }. */
export interface CriarUnidadeInput {
  codigo: string;
  nome: string;
  tipo: TipoUnidade;
  unidadePaiId?: string;
}

/** Entrada do command RenomearUnidade: { nome, tipo }. */
export interface RenomearUnidadeInput {
  nome: string;
  tipo: TipoUnidade;
}

/** Entrada do command MoverUnidade: { novoPaiId }. */
export interface MoverUnidadeInput {
  novoPaiId: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const unidadeKeys = {
  all: ['identidade', 'unidades'] as const,
  arvore: () => [...unidadeKeys.all, 'arvore'] as const,
};

export const papelKeys = {
  all: ['identidade', 'papeis'] as const,
  lista: () => [...papelKeys.all, 'lista'] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarArvore(signal?: AbortSignal): Promise<NoUnidade[]> {
  return http.get<NoUnidade[]>('/identidade/unidades', { signal });
}

function listarPapeis(signal?: AbortSignal): Promise<PapelResumo[]> {
  return http.get<PapelResumo[]>('/identidade/papeis', { signal });
}

function criarUnidade(input: CriarUnidadeInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/identidade/unidades', input);
}

function renomearUnidade(id: string, input: RenomearUnidadeInput): Promise<void> {
  return http.put<void>(`/identidade/unidades/${id}`, input);
}

function moverUnidade(id: string, input: MoverUnidadeInput): Promise<void> {
  return http.post<void>(`/identidade/unidades/${id}/mover`, input);
}

function ativarUnidade(id: string): Promise<void> {
  return http.post<void>(`/identidade/unidades/${id}/ativar`);
}

function desativarUnidade(id: string): Promise<void> {
  return http.post<void>(`/identidade/unidades/${id}/desativar`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista a árvore hierárquica de UOs do tenant (raízes com filhos aninhados). */
export function useArvoreUnidades() {
  return useQuery({
    queryKey: unidadeKeys.arvore(),
    queryFn: ({ signal }) => listarArvore(signal),
  });
}

/** Lista os papéis disponíveis (catálogo para a atribuição escopada). */
export function usePapeis(enabled = true) {
  return useQuery({
    queryKey: papelKeys.lista(),
    queryFn: ({ signal }) => listarPapeis(signal),
    enabled,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS (todos invalidam a árvore de unidades)
// ---------------------------------------------------------------------------

/** Cria uma UO (raiz quando sem pai, ou filha de unidadePaiId). Invalida a árvore. */
export function useCriarUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarUnidade,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unidadeKeys.all });
    },
  });
}

/** Renomeia/redefine o tipo da UO. Invalida a árvore. */
export function useRenomearUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: RenomearUnidadeInput }) =>
      renomearUnidade(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unidadeKeys.all });
    },
  });
}

/** Move a UO para um novo pai. Invalida a árvore. */
export function useMoverUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: MoverUnidadeInput }) =>
      moverUnidade(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unidadeKeys.all });
    },
  });
}

/** Ativa a UO. Invalida a árvore. */
export function useAtivarUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ativarUnidade,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unidadeKeys.all });
    },
  });
}

/** Desativa a UO. Invalida a árvore. */
export function useDesativarUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: desativarUnidade,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unidadeKeys.all });
    },
  });
}
