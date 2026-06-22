// Camada de API das ATRIBUIÇÕES de papel com escopo organizacional (M1 / Identidade).
// Segue o PADRÃO-OURO de usuario.api.ts:
//   - DTOs no topo (espelham os Commands/Queries reais do backend de Identidade);
//   - query keys centralizadas para invalidação consistente;
//   - funções de acesso via http client tipado (Authorization + ProblemDetails);
//   - hooks TanStack Query (useQuery/useMutation) com invalidação.
//
// Contrato REAL (Identidade — M1 validada):
//   GET    /api/identidade/unidades                          -> árvore de UNIDADES (UnidadeNo[])
//   GET    /api/identidade/usuarios/{id}/atribuicoes         -> AtribuicaoResumo[]
//   POST   /api/identidade/usuarios/{id}/atribuicoes  {...}  -> atribuir papel com escopo
//   DELETE /api/identidade/usuarios/{id}/atribuicoes  {...}  -> revogar atribuição
//
// Regra de negócio CRÍTICA imposta pelo backend: "não delega o que não tem" — um
// usuário só pode conceder/revogar dentro do PRÓPRIO escopo. Violação => HTTP 403,
// tratado na UI com mensagem amigável (ver AtribuicoesModal).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Nó da árvore de unidades organizacionais (GET /identidade/unidades). */
export interface UnidadeNo {
  id: string;
  codigo: string;
  nome: string;
  tipo: string;
  ativo: boolean;
  filhos: UnidadeNo[];
}

/** Atribuição atual de um usuário (papel + UO + escopo + vigência). */
export interface AtribuicaoResumo {
  papelId: string;
  papelNome: string;
  unidadeId: string;
  unidadeNome: string;
  incluiSubunidades: boolean;
  /** ISO date (yyyy-MM-dd) ou null para vigência indeterminada. */
  vigenciaFim: string | null;
}

/** Entrada do command AtribuirPapel / RevogarPapel. usuarioId vai também na rota. */
export interface AtribuicaoInput {
  usuarioId: string;
  papelId: string;
  unidadeId: string;
  incluiSubunidades: boolean;
  /** ISO date (yyyy-MM-dd); omitido quando indeterminada. */
  vigenciaFim?: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const unidadeKeys = {
  all: ['identidade', 'unidades'] as const,
  arvore: () => [...unidadeKeys.all, 'arvore'] as const,
};

export const atribuicaoKeys = {
  all: ['identidade', 'atribuicoes'] as const,
  doUsuario: (usuarioId: string) => [...atribuicaoKeys.all, 'usuario', usuarioId] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarUnidades(signal?: AbortSignal): Promise<UnidadeNo[]> {
  return http.get<UnidadeNo[]>('/identidade/unidades', { signal });
}

function listarAtribuicoes(usuarioId: string, signal?: AbortSignal): Promise<AtribuicaoResumo[]> {
  return http.get<AtribuicaoResumo[]>(`/identidade/usuarios/${usuarioId}/atribuicoes`, { signal });
}

function atribuirPapel(input: AtribuicaoInput): Promise<void> {
  return http.post<void>(`/identidade/usuarios/${input.usuarioId}/atribuicoes`, input);
}

function revogarPapel(input: AtribuicaoInput): Promise<void> {
  // DELETE com corpo (a rota identifica o usuário; o corpo identifica a atribuição).
  return http.delete<void>(`/identidade/usuarios/${input.usuarioId}/atribuicoes`, { body: input });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Árvore de unidades organizacionais (catálogo para seleção da UO de escopo). */
export function useUnidades(enabled = true) {
  return useQuery({
    queryKey: unidadeKeys.arvore(),
    queryFn: ({ signal }) => listarUnidades(signal),
    enabled,
  });
}

/** Atribuições atuais do usuário (papel + UO + escopo + vigência). */
export function useAtribuicoes(usuarioId: string | null, enabled = true) {
  return useQuery({
    queryKey: atribuicaoKeys.doUsuario(usuarioId ?? ''),
    queryFn: ({ signal }) => listarAtribuicoes(usuarioId as string, signal),
    enabled: enabled && usuarioId !== null,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS (invalidam as atribuições do usuário)
// ---------------------------------------------------------------------------

/** Atribui um papel ao usuário em uma UO, com escopo e vigência. */
export function useAtribuirPapel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: atribuirPapel,
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: atribuicaoKeys.doUsuario(input.usuarioId) });
      // A projeção resumida de usuários pode exibir papéis — revalida a lista também.
      queryClient.invalidateQueries({ queryKey: ['identidade', 'usuarios'] });
    },
  });
}

/** Revoga uma atribuição específica do usuário. */
export function useRevogarPapel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: revogarPapel,
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: atribuicaoKeys.doUsuario(input.usuarioId) });
      queryClient.invalidateQueries({ queryKey: ['identidade', 'usuarios'] });
    },
  });
}
