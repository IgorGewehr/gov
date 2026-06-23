// Camada de API do e-SIC INTERNO (LAI Lei 12.527/2011) — gestão dos pedidos de
// informação do ente. Contrato real (src/Modules/.../Infrastructure/TransparenciaEndpoints.cs,
// grupo /api/transparencia/esic; DTOs em Application/Esic/*.cs):
//   GET    /esic?ano=&situacao=                         (perm transparencia.esic.ver)
//       -> PedidoSicResumoDto[]
//   GET    /esic/{pedidoId}                              (perm transparencia.esic.ver)
//       -> PedidoSicDetalheInternoDto (COM PII — gera trilha de acesso LG-3)
//   POST   /esic/{pedidoId}/atendimento                 (perm transparencia.esic.responder)
//   POST   /esic/{pedidoId}/resposta {texto,referenciaAnexo?}
//   POST   /esic/{pedidoId}/prorrogacao {motivo}
//   POST   /esic/{pedidoId}/indeferimento {fundamentoLegal}
//   POST   /esic/{pedidoId}/recurso/decisao {resultado,decisao}
// Enums serializam como STRING (JsonStringEnumConverter); propriedades camelCase.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { transparenciaKeys } from './keys';

// ---------------------------------------------------------------------------
// DTOs (espelham os records do backend — Domain/Esic/Enums.cs)
// ---------------------------------------------------------------------------

/** Situação do pedido (máquina de estados LAI). */
export type SituacaoPedidoSic =
  | 'Aberto'
  | 'EmAtendimento'
  | 'Respondido'
  | 'Indeferido'
  | 'RecursoAberto'
  | 'RecursoRespondido'
  | 'Encerrado';

/** Resultado da decisão de um recurso (LAI art. 15-16). */
export type ResultadoRecurso = 'Provido' | 'ParcialmenteProvido' | 'Negado';

/** Item da fila interna de pedidos (resumo, sem texto integral de PII). */
export interface PedidoSicResumo {
  pedidoId: string;
  protocolo: string;
  solicitante: string;
  situacao: SituacaoPedidoSic;
  dataAbertura: string;
  prazoVigente: string;
  emAtraso: boolean;
}

/** Detalhe interno do pedido COM dados do solicitante (PII — trilha de acesso). */
export interface PedidoSicDetalheInterno {
  pedidoId: string;
  protocolo: string;
  solicitanteNome: string;
  solicitanteDocumento: string | null;
  solicitanteContato: string | null;
  descricao: string;
  situacao: SituacaoPedidoSic;
  dataAbertura: string;
  prazoResposta: string;
  prorrogadoAte: string | null;
  motivoProrrogacao: string | null;
  textoResposta: string | null;
  fundamentoIndeferimento: string | null;
}

export interface ListarPedidosParams {
  ano?: number | null;
  situacao?: SituacaoPedidoSic | null;
}

export interface ResponderInput {
  texto: string;
  referenciaAnexo?: string | null;
}

export interface DecidirRecursoInput {
  resultado: ResultadoRecurso;
  decisao: string;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

const BASE = '/transparencia/esic';

function listar(params: ListarPedidosParams, signal?: AbortSignal): Promise<PedidoSicResumo[]> {
  return http.get<PedidoSicResumo[]>(BASE, {
    query: { ano: params.ano, situacao: params.situacao },
    signal,
  });
}

function obter(pedidoId: string, signal?: AbortSignal): Promise<PedidoSicDetalheInterno> {
  return http.get<PedidoSicDetalheInterno>(`${BASE}/${pedidoId}`, { signal });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — leitura
// ---------------------------------------------------------------------------

/** Fila interna de pedidos e-SIC do ente (com filtros opcionais). */
export function usePedidosSic(params: ListarPedidosParams, enabled = true) {
  return useQuery({
    queryKey: transparenciaKeys.esicLista(params),
    queryFn: ({ signal }) => listar(params, signal),
    enabled,
  });
}

/** Detalhe interno (COM PII — cada leitura gera trilha de acesso LG-3 no backend). */
export function usePedidoSic(pedidoId: string) {
  return useQuery({
    queryKey: transparenciaKeys.esic(pedidoId),
    queryFn: ({ signal }) => obter(pedidoId, signal),
    enabled: pedidoId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — transições (mutações). Cada ação invalida a fila + o detalhe.
// ---------------------------------------------------------------------------

function useAcaoPedido<I>(mutationFn: (input: I) => Promise<unknown>, pedidoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.esicAll() });
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.esic(pedidoId) });
    },
  });
}

/** Inicia o atendimento (Aberto → EmAtendimento). */
export function useIniciarAtendimento(pedidoId: string) {
  return useAcaoPedido(() => http.post<void>(`${BASE}/${pedidoId}/atendimento`), pedidoId);
}

/** Responde o pedido (acesso concedido — LAI art. 11). */
export function useResponderPedido(pedidoId: string) {
  return useAcaoPedido(
    (input: ResponderInput) =>
      http.post<void>(`${BASE}/${pedidoId}/resposta`, {
        texto: input.texto,
        referenciaAnexo: input.referenciaAnexo ?? null,
      }),
    pedidoId,
  );
}

/** Prorroga o prazo (+10 dias úteis, única, justificada — LAI art. 11 §2º). */
export function useProrrogarPedido(pedidoId: string) {
  return useAcaoPedido(
    (motivo: string) => http.post<void>(`${BASE}/${pedidoId}/prorrogacao`, { motivo }),
    pedidoId,
  );
}

/** Indefere o pedido com fundamento legal (LAI art. 11 §1º). */
export function useIndeferirPedido(pedidoId: string) {
  return useAcaoPedido(
    (fundamentoLegal: string) =>
      http.post<void>(`${BASE}/${pedidoId}/indeferimento`, { fundamentoLegal }),
    pedidoId,
  );
}

/** Decide o recurso interposto pelo cidadão (RecursoAberto → RecursoRespondido). */
export function useDecidirRecurso(pedidoId: string) {
  return useAcaoPedido(
    (input: DecidirRecursoInput) => http.post<void>(`${BASE}/${pedidoId}/recurso/decisao`, input),
    pedidoId,
  );
}
