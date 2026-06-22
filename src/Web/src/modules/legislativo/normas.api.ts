// Camada de API do agregado Norma (modulo Legislativo, parte 2). DTOs + acesso
// HTTP tipado + hooks TanStack Query, cobrindo /api/legislativo/normas: busca
// paginada (termo/tipo/ano), detalhe, cadastro e acoes de revogacao/alteracao.
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

/** Resumo de uma norma para a lista de busca. */
export interface NormaResumo {
  id: string;
  tipo: string;
  numero: string;
  ano: number;
  ementa: string;
  situacao: string;
  dataPublicacao: string;
}

/** Vinculo entre normas (revogacao/alteracao). */
export interface NormaVinculoResumo {
  normaId: string;
  numero: string;
  tipo: string;
}

/** Detalhe completo de uma norma. */
export interface NormaDetalhe extends NormaResumo {
  textoIntegral: string;
  proposicaoId: string | null;
  revogaNormas: NormaVinculoResumo[];
  alteraNormas: NormaVinculoResumo[];
}

/** Filtros da busca paginada de normas. */
export interface BuscaNormasFiltro {
  termo: string;
  tipo: string;
  ano: string;
  pagina: number;
}

/** Payload de cadastro de norma. */
export interface NormaInput {
  tipo: number;
  numero: string;
  ano: number;
  ementa: string;
  textoIntegral: string;
  dataPublicacao: string;
}

/** Payload de revogacao/alteracao (norma afetada por esta norma). */
export interface NormaAcaoInput {
  normaAfetadaId: string;
  justificativa: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarNormas(
  filtro: BuscaNormasFiltro,
  signal?: AbortSignal,
): Promise<PaginaResultado<NormaResumo>> {
  return http.get<PaginaResultado<NormaResumo>>('/legislativo/normas', {
    query: {
      termo: filtro.termo || null,
      tipo: filtro.tipo || null,
      ano: filtro.ano || null,
      pagina: filtro.pagina,
    },
    signal,
  });
}

function obterNorma(id: string, signal?: AbortSignal): Promise<NormaDetalhe> {
  return http.get<NormaDetalhe>(`/legislativo/normas/${id}`, { signal });
}

async function criarNorma(input: NormaInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>('/legislativo/normas', input);
  return id;
}

function revogarNorma(id: string, input: NormaAcaoInput): Promise<void> {
  return http.post<void>(`/legislativo/normas/${id}/revogacao`, input);
}

function alterarNorma(id: string, input: NormaAcaoInput): Promise<void> {
  return http.post<void>(`/legislativo/normas/${id}/alteracao`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Busca paginada de normas por termo/tipo/ano. */
export function useBuscaNormas(filtro: BuscaNormasFiltro) {
  return useQuery({
    queryKey: legislativoKeys.normasBusca(filtro.termo, filtro.tipo, filtro.ano, filtro.pagina),
    queryFn: ({ signal }) => buscarNormas(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Detalhe de uma norma (texto integral + vinculos de revogacao/alteracao). */
export function useNorma(id: string) {
  return useQuery({
    queryKey: legislativoKeys.norma(id),
    queryFn: ({ signal }) => obterNorma(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Cadastra uma nova norma e invalida as buscas. */
export function useCriarNorma() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: criarNorma,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.normas() });
    },
  });
}

/** Invalida o detalhe de uma norma e todas as buscas. */
function useInvalidarNorma(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.norma(id) });
    queryClient.invalidateQueries({ queryKey: legislativoKeys.normas() });
  };
}

/** Revoga (por esta norma) outra norma vigente. */
export function useRevogarNorma(id: string) {
  const invalidar = useInvalidarNorma(id);
  return useMutation({
    mutationFn: (input: NormaAcaoInput) => revogarNorma(id, input),
    onSuccess: invalidar,
  });
}

/** Registra que esta norma altera outra norma vigente. */
export function useAlterarNorma(id: string) {
  const invalidar = useInvalidarNorma(id);
  return useMutation({
    mutationFn: (input: NormaAcaoInput) => alterarNorma(id, input),
    onSuccess: invalidar,
  });
}
