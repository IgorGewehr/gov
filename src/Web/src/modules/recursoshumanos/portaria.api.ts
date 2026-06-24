// Camada de API de PORTARIAS / atos de pessoal (módulo RecursosHumanos). Segue o PADRÃO-OURO:
// DTOs no topo, acesso via http client tipado, hooks TanStack Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.Pessoal.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Natureza do ato de pessoal (enum numérico do backend na entrada; nome do enum na saída). */
export type TipoPortaria = 'Nomeacao' | 'Exoneracao' | 'Designacao' | 'Concessao' | 'Outro';

/** Situação (ciclo de vida) de uma portaria. */
export type SituacaoPortaria = 'Emitida' | 'Revogada';

/** Resumo de uma portaria (linha de lista). */
export interface PortariaResumo {
  id: string;
  numero: string;
  exercicio: number;
  sequencial: number;
  tipo: TipoPortaria;
  dataAto: string;
  ementa: string;
  servidorId?: string | null;
  situacao: SituacaoPortaria;
}

/** Detalhe completo de uma portaria (inclui o texto integral). */
export interface PortariaDetalhe extends PortariaResumo {
  texto: string;
  motivoRevogacao?: string | null;
}

/** Envelope paginado (ResultadoPaginado&lt;T&gt; do backend). */
interface PaginaPortarias {
  itens: PortariaResumo[];
  total: number;
  pagina: number;
  tamanho: number;
}

/** Entrada da emissão de uma portaria. A numeração é apurada no backend (sequencial do exercício). */
export interface EmitirPortariaInput {
  /** 1=Nomeacao, 2=Exoneracao, 3=Designacao, 4=Concessao, 5=Outro (enum numérico do backend). */
  tipo: number;
  ementa: string;
  texto: string;
  servidorId?: string | null;
  /** Data do ato (yyyy-MM-dd); quando ausente, o backend usa o "hoje" do tenant. */
  dataAto?: string | null;
}

/** Filtros da busca paginada de portarias. */
export interface FiltroPortarias {
  tipo: number | null;
  situacao: number | null;
  exercicio: number | null;
  servidorId: string | null;
  pagina: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarPortarias(filtro: FiltroPortarias, signal?: AbortSignal): Promise<PaginaPortarias> {
  return http.get<PaginaPortarias>('/recursoshumanos/portarias', {
    signal,
    query: {
      ...(filtro.tipo != null ? { tipo: filtro.tipo } : {}),
      ...(filtro.situacao != null ? { situacao: filtro.situacao } : {}),
      ...(filtro.exercicio != null ? { exercicio: filtro.exercicio } : {}),
      ...(filtro.servidorId ? { servidorId: filtro.servidorId } : {}),
      pagina: filtro.pagina,
    },
  });
}

function obterPortaria(id: string, signal?: AbortSignal): Promise<PortariaDetalhe> {
  return http.get<PortariaDetalhe>(`/recursoshumanos/portarias/${id}`, { signal });
}

function emitirPortaria(input: EmitirPortariaInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/recursoshumanos/portarias', input);
}

function revogarPortaria(portariaId: string, motivo: string): Promise<void> {
  return http.post<void>(`/recursoshumanos/portarias/${portariaId}/revogacao`, { motivo });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Busca paginada de portarias por tipo/situação/exercício/servidor. */
export function useBuscarPortarias(filtro: FiltroPortarias) {
  return useQuery({
    queryKey: rhKeys.portariasBusca(
      String(filtro.tipo ?? ''),
      String(filtro.situacao ?? ''),
      String(filtro.exercicio ?? ''),
      filtro.servidorId ?? '',
      filtro.pagina,
    ),
    queryFn: ({ signal }) => buscarPortarias(filtro, signal),
  });
}

/** Detalhe completo de uma portaria por identificador. */
export function usePortaria(id: string) {
  return useQuery({
    queryKey: rhKeys.portaria(id),
    queryFn: ({ signal }) => obterPortaria(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Emite uma portaria e invalida as listas de portarias. */
export function useEmitirPortaria() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: emitirPortaria,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.portarias() });
    },
  });
}

/** Revoga uma portaria e invalida as consultas de portarias. */
export function useRevogarPortaria(portariaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => revogarPortaria(portariaId, motivo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.portarias() });
    },
  });
}
