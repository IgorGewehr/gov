// Camada de API dos AFASTAMENTOS/LICENÇAS tipados do servidor (Onda 1). PADRÃO-OURO:
// DTOs no topo → acesso HTTP tipado → hooks TanStack Query.
// Contrato: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
//   POST /recursoshumanos/servidores/{servidorId}/afastamentos          (RegistrarAfastamento → { id })
//   GET  /recursoshumanos/servidores/{servidorId}/afastamentos          (ListarAfastamentosDoServidor)
//   POST /recursoshumanos/afastamentos/{afastamentoId}/encerramento     (EncerrarAfastamento)
//   POST /recursoshumanos/afastamentos/{afastamentoId}/cancelamento     (CancelarAfastamento)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs (espelham AfastamentoResumo e os payloads dos endpoints)
// ---------------------------------------------------------------------------

/** Tipo legal do afastamento (NOME do enum `TipoAfastamento` do backend). */
export type TipoAfastamento =
  | 'LicencaMaternidade'
  | 'LicencaPaternidade'
  | 'DoencaAte15Dias'
  | 'DoencaInss'
  | 'AcidenteTrabalho'
  | 'LicencaPremio'
  | 'LicencaSemVencimento'
  | 'CessaoComOnus'
  | 'CessaoSemOnus'
  | 'MandatoEletivo';

/** Situação do afastamento no ciclo de vida (NOME do enum `SituacaoAfastamento`). */
export type SituacaoAfastamento = 'Vigente' | 'Encerrado' | 'Cancelado';

/**
 * Resumo de leitura de um afastamento (projeção `AfastamentoResumo` do backend). O efeito na
 * folha (`suspendeProventos`/`percentualRemuneracao`/`diasPagosPeloEnte`/`contaTempo`) é o
 * SNAPSHOT da regra do tipo no momento do registro — não é digitado pelo usuário.
 */
export interface AfastamentoResumo {
  id: string;
  servidorId: string;
  tipo: string;
  /** Início ("yyyy-MM-dd"). */
  inicio: string;
  /** Fim previsto (nulo quando indeterminado). */
  fimPrevisto: string | null;
  /** Fim efetivo (nulo enquanto vigente). */
  fimEfetivo: string | null;
  situacao: string;
  suspendeProventos: boolean;
  percentualRemuneracao: number;
  diasPagosPeloEnte: number;
  contaTempo: boolean;
  documento: string | null;
}

/**
 * Entrada do registro de afastamento TIPADO. O usuário escolhe o `tipo`; o efeito na folha
 * vem da regra do tipo no backend (nunca enviado pela UI). `fimPrevisto`/`documento` opcionais.
 */
export interface RegistrarAfastamentoInput {
  /** NOME do enum `TipoAfastamento`. */
  tipo: TipoAfastamento;
  inicio: string;
  fimPrevisto?: string | null;
  documento?: string | null;
}

/** Entrada do encerramento (retorno do servidor): grava o fim efetivo. */
export interface EncerrarAfastamentoInput {
  fimEfetivo: string;
}

/** Entrada do cancelamento (lançado por engano/revogado). */
export interface CancelarAfastamentoInput {
  motivo: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarAfastamentosDoServidor(
  servidorId: string,
  signal?: AbortSignal,
): Promise<AfastamentoResumo[]> {
  return http.get<AfastamentoResumo[]>(
    `/recursoshumanos/servidores/${encodeURIComponent(servidorId)}/afastamentos`,
    { signal },
  );
}

function registrarAfastamento(
  servidorId: string,
  input: RegistrarAfastamentoInput,
): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(
    `/recursoshumanos/servidores/${encodeURIComponent(servidorId)}/afastamentos`,
    {
      tipo: input.tipo,
      inicio: input.inicio,
      fimPrevisto: input.fimPrevisto ?? null,
      documento: input.documento ?? null,
    },
  );
}

function encerrarAfastamento(
  afastamentoId: string,
  input: EncerrarAfastamentoInput,
): Promise<void> {
  return http.post<void>(
    `/recursoshumanos/afastamentos/${encodeURIComponent(afastamentoId)}/encerramento`,
    input,
  );
}

function cancelarAfastamento(
  afastamentoId: string,
  input: CancelarAfastamentoInput,
): Promise<void> {
  return http.post<void>(
    `/recursoshumanos/afastamentos/${encodeURIComponent(afastamentoId)}/cancelamento`,
    input,
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista o histórico de afastamentos de um servidor (ficha de afastamentos). */
export function useAfastamentosDoServidor(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.afastamentosDoServidor(servidorId),
    queryFn: ({ signal }) => listarAfastamentosDoServidor(servidorId, signal),
    enabled: servidorId.trim().length > 0,
  });
}

/** Registra um afastamento tipado e invalida a lista do servidor + a lista de servidores. */
export function useRegistrarAfastamentoTipado(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarAfastamentoInput) => registrarAfastamento(servidorId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.afastamentosDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Encerra um afastamento vigente (retorno do servidor). */
export function useEncerrarAfastamento(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ afastamentoId, input }: { afastamentoId: string; input: EncerrarAfastamentoInput }) =>
      encerrarAfastamento(afastamentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.afastamentosDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}

/** Cancela um afastamento vigente (lançado por engano/revogado). */
export function useCancelarAfastamento(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ afastamentoId, input }: { afastamentoId: string; input: CancelarAfastamentoInput }) =>
      cancelarAfastamento(afastamentoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.afastamentosDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.servidores() });
    },
  });
}
