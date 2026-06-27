// Camada de API de PROCESSOS TRABALHISTAS — módulo RecursosHumanos. Cadastro/acompanhamento
// (número CNJ, vara, reclamante, objeto, valores, situação) + provisão contábil (NBC TG 25):
// só o prognóstico PROVÁVEL gera provisão.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.ProcessosTrabalhistas.cs
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Situação processual. */
export type SituacaoProcessoTrabalhista =
  | 'EmAndamento'
  | 'Acordo'
  | 'Condenado'
  | 'Improcedente'
  | 'Arquivado';

/** Prognóstico de perda (NBC TG 25). */
export type PrognosticoPerda = 'Provavel' | 'Possivel' | 'Remota';

/** Resumo de um processo trabalhista (linha de lista). */
export interface ProcessoTrabalhistaResumo {
  id: string;
  numeroProcesso: string;
  vara: string;
  reclamante: string;
  valorCausa: number;
  valorProvisionado: number;
  prognostico: PrognosticoPerda;
  situacao: SituacaoProcessoTrabalhista;
  dataAjuizamento: string;
}

/** Detalhe completo de um processo trabalhista. */
export interface ProcessoTrabalhistaDetalhe {
  resumo: ProcessoTrabalhistaResumo;
  servidorId?: string | null;
  objeto: string;
  valorAcordo?: number | null;
  valorCondenacao?: number | null;
  dataEncerramento?: string | null;
}

/** Envelope paginado. */
interface PaginaProcessos {
  itens: ProcessoTrabalhistaResumo[];
  total: number;
  pagina: number;
  tamanho: number;
}

/** Demonstrativo de provisões trabalhistas vigentes. */
export interface DemonstrativoProvisao {
  totalProvisionado: number;
}

/** Entrada do cadastro de processo trabalhista. */
export interface CadastrarProcessoInput {
  numeroProcesso: string;
  vara: string;
  reclamante: string;
  servidorId?: string | null;
  objeto: string;
  valorCausa: number;
  dataAjuizamento: string;
  /** 1=Provavel, 2=Possivel, 3=Remota (enum numérico do backend). */
  prognostico: number;
}

/** Filtros da busca paginada. */
export interface FiltroProcessos {
  situacao: number | null;
  prognostico: number | null;
  termo: string | null;
  pagina: number;
}

const BASE = '/recursoshumanos/processos-trabalhistas';

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarProcessos(filtro: FiltroProcessos, signal?: AbortSignal): Promise<PaginaProcessos> {
  return http.get<PaginaProcessos>(BASE, {
    signal,
    query: {
      ...(filtro.situacao != null ? { situacao: filtro.situacao } : {}),
      ...(filtro.prognostico != null ? { prognostico: filtro.prognostico } : {}),
      ...(filtro.termo ? { termo: filtro.termo } : {}),
      pagina: filtro.pagina,
    },
  });
}

function obterProcesso(id: string, signal?: AbortSignal): Promise<ProcessoTrabalhistaDetalhe> {
  return http.get<ProcessoTrabalhistaDetalhe>(`${BASE}/${id}`, { signal });
}

function obterProvisao(signal?: AbortSignal): Promise<DemonstrativoProvisao> {
  return http.get<DemonstrativoProvisao>(`${BASE}/demonstrativo-provisao`, { signal });
}

function cadastrarProcesso(input: CadastrarProcessoInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(BASE, input);
}

function reavaliarPrognostico(id: string, prognostico: number): Promise<void> {
  return http.post<void>(`${BASE}/${id}/prognostico`, { prognostico });
}

function registrarAcordo(id: string, valor: number, data: string): Promise<void> {
  return http.post<void>(`${BASE}/${id}/acordo`, { valor, data });
}

function registrarCondenacao(id: string, valor: number, data: string): Promise<void> {
  return http.post<void>(`${BASE}/${id}/condenacao`, { valor, data });
}

function registrarImprocedencia(id: string, data: string): Promise<void> {
  return http.post<void>(`${BASE}/${id}/improcedencia`, { data });
}

function arquivarProcesso(id: string): Promise<void> {
  return http.post<void>(`${BASE}/${id}/arquivamento`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Busca paginada de processos por situação/prognóstico/termo. */
export function useBuscarProcessos(filtro: FiltroProcessos) {
  return useQuery({
    queryKey: rhKeys.processosTrabalhistasBusca(
      String(filtro.situacao ?? ''),
      String(filtro.prognostico ?? ''),
      filtro.termo ?? '',
      filtro.pagina,
    ),
    queryFn: ({ signal }) => buscarProcessos(filtro, signal),
  });
}

/** Detalhe de um processo trabalhista. */
export function useProcesso(id: string) {
  return useQuery({
    queryKey: rhKeys.processoTrabalhista(id),
    queryFn: ({ signal }) => obterProcesso(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Demonstrativo de provisões trabalhistas vigentes. */
export function useProvisaoTrabalhista() {
  return useQuery({
    queryKey: rhKeys.provisaoTrabalhista(),
    queryFn: ({ signal }) => obterProvisao(signal),
  });
}

/** Cadastra um processo e invalida as listas/provisão. */
export function useCadastrarProcesso() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarProcesso,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.processosTrabalhistas() });
    },
  });
}

/** Movimenta um processo (prognóstico/acordo/condenação/improcedência/arquivamento). */
export function useMovimentarProcesso(id: string) {
  const queryClient = useQueryClient();
  const invalidar = () =>
    queryClient.invalidateQueries({ queryKey: rhKeys.processosTrabalhistas() });

  return {
    reavaliar: useMutation({
      mutationFn: (prognostico: number) => reavaliarPrognostico(id, prognostico),
      onSuccess: invalidar,
    }),
    acordo: useMutation({
      mutationFn: (p: { valor: number; data: string }) => registrarAcordo(id, p.valor, p.data),
      onSuccess: invalidar,
    }),
    condenacao: useMutation({
      mutationFn: (p: { valor: number; data: string }) =>
        registrarCondenacao(id, p.valor, p.data),
      onSuccess: invalidar,
    }),
    improcedencia: useMutation({
      mutationFn: (data: string) => registrarImprocedencia(id, data),
      onSuccess: invalidar,
    }),
    arquivar: useMutation({
      mutationFn: () => arquivarProcesso(id),
      onSuccess: invalidar,
    }),
  };
}
