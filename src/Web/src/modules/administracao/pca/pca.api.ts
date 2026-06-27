// Camada de API do Plano de Contratacoes Anual (PCA) — modulo Administracao (art. 12, VII, Lei 14.133/2021).
// Rotas reais: /api/administracao/pca...
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import { ApiError } from '../../../api/problemDetails';

/** Situacao do PCA (enum SituacaoPca). */
export type SituacaoPca = 'EmElaboracao' | 'Aprovado' | 'Publicado' | 'EmRevisao';

/** Rotulos de situacao para exibicao. */
export const SITUACAO_PCA_LABEL: Record<SituacaoPca, string> = {
  EmElaboracao: 'Em elaboração',
  Aprovado: 'Aprovado',
  Publicado: 'Publicado',
  EmRevisao: 'Em revisão',
};

/** Natureza da contratacao vinculada a um item do PCA (enum FonteContratacaoPca). */
export type FonteContratacaoPca = 'Licitacao' | 'AtaRegistroPrecos' | 'Dispensa' | 'Inexigibilidade';

/** Rotulos da fonte de contratacao para exibicao. */
export const FONTE_CONTRATACAO_LABEL: Record<FonteContratacaoPca, string> = {
  Licitacao: 'Licitação',
  AtaRegistroPrecos: 'Ata de Registro de Preços',
  Dispensa: 'Dispensa',
  Inexigibilidade: 'Inexigibilidade / Credenciamento',
};

/** Item do PCA (ItemPcaDetalhe). */
export interface ItemPcaDetalhe {
  itemPcaId: string;
  itemCatalogoId: string;
  quantidade: number;
  valorEstimado: number;
  trimestreDesejado: number;
  justificativa: string | null;
  fonteContratacao: FonteContratacaoPca | null;
  contratacaoReferenciaId: string | null;
  contratacaoIdentificacao: string | null;
}

/** Detalhe do PCA (PcaDetalhe). */
export interface PcaDetalhe {
  id: string;
  exercicio: number;
  situacao: SituacaoPca;
  numeroPncp: string | null;
  valorTotalEstimado: number;
  numeroRevisao: number;
  motivoRevisaoAtual: string | null;
  itens: ItemPcaDetalhe[];
}

/** Input de vínculo de contratação a um item do PCA. */
export interface VincularContratacaoItemInput {
  fonte: FonteContratacaoPca;
  referenciaId: string;
  identificacao?: string | null;
}

/** IncluirItemPcaPayload. */
export interface IncluirItemPcaInput {
  itemCatalogoId: string;
  quantidade: number;
  valorEstimado: number;
  trimestreDesejado: number;
  justificativa?: string | null;
}

export const pcaKeys = {
  all: ['administracao', 'pca'] as const,
  exercicio: (exercicio: number) => [...pcaKeys.all, 'exercicio', exercicio] as const,
};

function obterPca(exercicio: number, signal?: AbortSignal): Promise<PcaDetalhe | null> {
  // O endpoint devolve 404 quando nao ha PCA do exercicio; tratamos como null.
  return http.get<PcaDetalhe>(`/administracao/pca/${exercicio}`, { signal }).catch((erro: unknown) => {
    if (erro instanceof ApiError && erro.status === 404) {
      return null;
    }
    throw erro;
  });
}

function abrirPca(exercicio: number): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/pca', { exercicio });
}

function incluirItem(pcaId: string, input: IncluirItemPcaInput): Promise<{ itemPcaId: string }> {
  return http.post<{ itemPcaId: string }>(`/administracao/pca/${pcaId}/itens`, input);
}

function removerItem(pcaId: string, itemPcaId: string): Promise<void> {
  return http.delete<void>(`/administracao/pca/${pcaId}/itens/${itemPcaId}`);
}

function aprovarPca(pcaId: string): Promise<void> {
  return http.post<void>(`/administracao/pca/${pcaId}/aprovar`, {});
}

function publicarPca(pcaId: string, numeroPncp: string): Promise<void> {
  return http.post<void>(`/administracao/pca/${pcaId}/publicar-pncp`, { numeroPncp });
}

function revisarPca(pcaId: string, motivo: string): Promise<void> {
  return http.post<void>(`/administracao/pca/${pcaId}/revisar`, { motivo });
}

function vincularContratacao(pcaId: string, itemPcaId: string, input: VincularContratacaoItemInput): Promise<void> {
  return http.post<void>(`/administracao/pca/${pcaId}/itens/${itemPcaId}/vincular-contratacao`, input);
}

/** ObterPcaPorExercicio — o PCA do exercicio (ou null se inexistente). */
export function usePcaPorExercicio(exercicio: number) {
  return useQuery({
    queryKey: pcaKeys.exercicio(exercicio),
    queryFn: ({ signal }) => obterPca(exercicio, signal),
  });
}

/** AbrirPca — abre o plano do exercicio (nasce EmElaboracao). */
export function useAbrirPca(exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => abrirPca(exercicio),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}

/** IncluirItemPca — inclui um item de contratacao pretendida. */
export function useIncluirItemPca(pcaId: string, exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: IncluirItemPcaInput) => incluirItem(pcaId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}

/** RemoverItemPca — remove um item do plano em elaboracao. */
export function useRemoverItemPca(pcaId: string, exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (itemPcaId: string) => removerItem(pcaId, itemPcaId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}

/** AprovarPca — aprova o plano (congela itens). */
export function useAprovarPca(pcaId: string, exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => aprovarPca(pcaId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}

/** PublicarPcaNoPncp — marca o plano como publicado no PNCP. */
export function usePublicarPca(pcaId: string, exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (numeroPncp: string) => publicarPca(pcaId, numeroPncp),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}

/** RevisarPca — reabre o plano vigente para revisão formal de itens. */
export function useRevisarPca(pcaId: string, exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => revisarPca(pcaId, motivo),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}

/** VincularContratacaoItemPca — vincula um item à licitação/ata/dispensa/inexigibilidade gerada. */
export function useVincularContratacaoItemPca(pcaId: string, exercicio: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ itemPcaId, input }: { itemPcaId: string; input: VincularContratacaoItemInput }) =>
      vincularContratacao(pcaId, itemPcaId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pcaKeys.exercicio(exercicio) }),
  });
}
