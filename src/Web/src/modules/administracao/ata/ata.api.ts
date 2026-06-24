// Camada de API da Ata de Registro de Precos (ARP) — modulo Administracao (art. 82-86, Lei 14.133/2021).
// Rotas reais: /api/administracao/atas...
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

/** Situacao da ata (enum SituacaoAta). */
export type SituacaoAta = 'Vigente' | 'Encerrada' | 'Cancelada';

const SITUACAO_NUMERO: Record<SituacaoAta, number> = {
  Vigente: 1,
  Encerrada: 2,
  Cancelada: 3,
};

/** Rotulos de situacao para exibicao. */
export const SITUACAO_ATA_LABEL: Record<SituacaoAta, string> = {
  Vigente: 'Vigente',
  Encerrada: 'Encerrada',
  Cancelada: 'Cancelada',
};

/** Resumo de ata (AtaResumo). */
export interface AtaResumo {
  id: string;
  numero: string;
  situacao: SituacaoAta;
  vigenciaInicio: string;
  vigenciaFim: string;
  quantidadeItens: number;
}

/** Item registrado com saldo (ItemAtaDetalhe). */
export interface ItemAtaDetalhe {
  itemAtaId: string;
  itemCatalogoId: string;
  fornecedorBeneficiarioId: string;
  precoRegistrado: number;
  quantidadeRegistrada: number;
  quantidadeContratada: number;
  saldoDisponivel: number;
}

/** Adesao registrada (AdesaoDetalhe). */
export interface AdesaoDetalhe {
  itemCatalogoId: string;
  orgaoAderente: string;
  quantidade: number;
  data: string;
}

/** Detalhe completo de uma ata (AtaDetalhe). */
export interface AtaDetalhe {
  id: string;
  numero: string;
  licitacaoId: string | null;
  situacao: SituacaoAta;
  vigenciaInicio: string;
  vigenciaFim: string;
  itens: ItemAtaDetalhe[];
  adesoes: AdesaoDetalhe[];
}

/** RegistrarAtaCommand. */
export interface RegistrarAtaInput {
  numero: string;
  licitacaoId?: string | null;
  vigenciaInicio: string;
  vigenciaFim: string;
}

/** RegistrarItemAtaPayload. */
export interface RegistrarItemAtaInput {
  itemCatalogoId: string;
  fornecedorBeneficiarioId: string;
  precoRegistrado: number;
  quantidadeRegistrada: number;
}

/** RegistrarAdesaoPayload. */
export interface RegistrarAdesaoInput {
  itemAtaId: string;
  orgaoAderente: string;
  quantidade: number;
}

/** ContratarItemAtaPayload. */
export interface ContratarItemAtaInput {
  itemAtaId: string;
  quantidade: number;
}

export const ataKeys = {
  all: ['administracao', 'atas'] as const,
  lista: (situacao?: SituacaoAta) => [...ataKeys.all, 'lista', situacao ?? 'todas'] as const,
  detalhe: (id: string) => [...ataKeys.all, 'detalhe', id] as const,
};

function listarAtas(situacao: SituacaoAta | undefined, signal?: AbortSignal): Promise<AtaResumo[]> {
  const query: Record<string, number> = {};
  if (situacao) query.situacao = SITUACAO_NUMERO[situacao];
  return http.get<AtaResumo[]>('/administracao/atas', { query, signal });
}

function obterAta(id: string, signal?: AbortSignal): Promise<AtaDetalhe> {
  return http.get<AtaDetalhe>(`/administracao/atas/${id}`, { signal });
}

function registrarAta(input: RegistrarAtaInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/administracao/atas', input);
}

function registrarItem(ataId: string, input: RegistrarItemAtaInput): Promise<{ itemAtaId: string }> {
  return http.post<{ itemAtaId: string }>(`/administracao/atas/${ataId}/itens`, input);
}

function registrarAdesao(ataId: string, input: RegistrarAdesaoInput): Promise<{ adesaoId: string }> {
  return http.post<{ adesaoId: string }>(`/administracao/atas/${ataId}/adesoes`, input);
}

function contratarItem(ataId: string, input: ContratarItemAtaInput): Promise<void> {
  return http.post<void>(`/administracao/atas/${ataId}/contratar`, input);
}

function cancelarAta(ataId: string, motivo: string): Promise<void> {
  return http.post<void>(`/administracao/atas/${ataId}/cancelar`, { motivo });
}

/** ListarAtas — atas do tenant, com filtro opcional por situacao. */
export function useAtas(situacao?: SituacaoAta) {
  return useQuery({
    queryKey: ataKeys.lista(situacao),
    queryFn: ({ signal }) => listarAtas(situacao, signal),
  });
}

/** ObterAtaPorId — detalhe (itens, saldos e adesoes). */
export function useAta(id: string) {
  return useQuery({
    queryKey: ataKeys.detalhe(id),
    queryFn: ({ signal }) => obterAta(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** RegistrarAta — cria uma nova ARP (nasce Vigente). */
export function useRegistrarAta() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registrarAta,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ataKeys.all }),
  });
}

function invalidarAta(queryClient: ReturnType<typeof useQueryClient>, ataId: string): void {
  queryClient.invalidateQueries({ queryKey: ataKeys.detalhe(ataId) });
  queryClient.invalidateQueries({ queryKey: ataKeys.all });
}

/** RegistrarItemAta — registra preco/quantidade de um item de catalogo na ata. */
export function useRegistrarItemAta(ataId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarItemAtaInput) => registrarItem(ataId, input),
    onSuccess: () => invalidarAta(queryClient, ataId),
  });
}

/** RegistrarAdesao — adesao (carona) a um item da ata, debitando o saldo (art. 86). */
export function useRegistrarAdesao(ataId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarAdesaoInput) => registrarAdesao(ataId, input),
    onSuccess: () => invalidarAta(queryClient, ataId),
  });
}

/** ContratarItemAta — uso direto da ata pelo proprio ente (debita saldo). */
export function useContratarItemAta(ataId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ContratarItemAtaInput) => contratarItem(ataId, input),
    onSuccess: () => invalidarAta(queryClient, ataId),
  });
}

/** CancelarAta — cancela a ata por ato administrativo. */
export function useCancelarAta(ataId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => cancelarAta(ataId, motivo),
    onSuccess: () => invalidarAta(queryClient, ataId),
  });
}
