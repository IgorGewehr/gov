// Camada de API do Catalogo de materiais/servicos (CATMAT/CATSER) — modulo Administracao.
// Espelha o padrao de licitacao.api.ts. Rotas reais: /api/administracao/catalogo/itens...
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

/** Natureza do item: material (CATMAT) ou servico (CATSER). */
export type NaturezaItem = 'Material' | 'Servico';

/** Situacao cadastral do item. */
export type SituacaoItemCatalogo = 'Ativo' | 'Inativo';

// Mapeamento numerico do enum NaturezaItem (a query aceita o valor do enum).
const NATUREZA_NUMERO: Record<NaturezaItem, number> = {
  Material: 1,
  Servico: 2,
};

/** Rotulos de natureza para exibicao. */
export const NATUREZA_LABEL: Record<NaturezaItem, string> = {
  Material: 'Material',
  Servico: 'Serviço',
};

/** Resumo de item de catalogo (ItemCatalogoResumo). */
export interface ItemCatalogoResumo {
  id: string;
  codigo: string;
  natureza: NaturezaItem;
  descricao: string;
  unidadeFornecimento: string;
  classe: string | null;
  situacao: SituacaoItemCatalogo;
}

/** CadastrarItemCatalogoCommand. */
export interface CadastrarItemCatalogoInput {
  codigo: string;
  natureza: NaturezaItem;
  descricao: string;
  unidadeFornecimento: string;
  classe?: string | null;
}

/** Filtros da listagem de itens. */
export interface ItensCatalogoFiltro {
  natureza?: NaturezaItem;
  termo?: string;
  apenasAtivos?: boolean;
}

export const catalogoKeys = {
  all: ['administracao', 'catalogo'] as const,
  lista: (filtro: ItensCatalogoFiltro) => [...catalogoKeys.all, 'lista', filtro] as const,
};

function listarItens(filtro: ItensCatalogoFiltro, signal?: AbortSignal): Promise<ItemCatalogoResumo[]> {
  const query: Record<string, string | number | boolean> = {};
  if (filtro.natureza) query.natureza = NATUREZA_NUMERO[filtro.natureza];
  if (filtro.termo && filtro.termo.trim().length > 0) query.termo = filtro.termo.trim();
  if (filtro.apenasAtivos) query.apenasAtivos = true;
  return http.get<ItemCatalogoResumo[]>('/administracao/catalogo/itens', { query, signal });
}

function cadastrarItem(input: CadastrarItemCatalogoInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/catalogo/itens', input);
}

function inativarItem(itemId: string): Promise<void> {
  return http.post<void>(`/administracao/catalogo/itens/${itemId}/inativar`, {});
}

function reativarItem(itemId: string): Promise<void> {
  return http.post<void>(`/administracao/catalogo/itens/${itemId}/reativar`, {});
}

/** ListarItensCatalogo — lista itens do catalogo com filtros opcionais. */
export function useItensCatalogo(filtro: ItensCatalogoFiltro, enabled = true) {
  return useQuery({
    queryKey: catalogoKeys.lista(filtro),
    queryFn: ({ signal }) => listarItens(filtro, signal),
    enabled,
  });
}

/** CadastrarItemCatalogo — cria um item padronizado. */
export function useCadastrarItemCatalogo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarItem,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: catalogoKeys.all }),
  });
}

/** InativarItemCatalogo — descontinua um item. */
export function useInativarItemCatalogo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: inativarItem,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: catalogoKeys.all }),
  });
}

/** ReativarItemCatalogo — reativa um item inativo. */
export function useReativarItemCatalogo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: reativarItem,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: catalogoKeys.all }),
  });
}
