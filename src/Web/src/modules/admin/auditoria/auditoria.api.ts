// Camada de API da Trilha de Auditoria (módulo admin) — consulta paginada à trilha
// imutável de alterações (CLAUDE.md §6: Audit Trail para o Tribunal de Contas).
// Segue o PADRÃO-OURO de protocolo/processo.api.ts:
//   - DTOs no topo (espelham a projeção real do AuditLog);
//   - query keys centralizadas (chave inclui os filtros para cache por consulta);
//   - acesso via http client tipado (Authorization + ProblemDetails);
//   - hook TanStack Query (useQuery) com keepPreviousData para paginação suave.
//
// Contrato REAL (admin):
//   GET /api/admin/auditoria?entidade&usuario&acao&pagina&tamanho
//       -> { total, pagina, tamanho, itens: AuditoriaItem[] }
//   Exige permissão "admin.auditoria.ver".
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham a projeção do AuditLog imutável)
// ---------------------------------------------------------------------------

/** Item da trilha de auditoria (registro imutável de uma mutação de estado). */
export interface AuditoriaItem {
  id: string;
  /** Nome da entidade auditada (ex.: "Processo", "Usuario"). */
  entityName: string;
  /** Identificador da instância da entidade afetada. */
  entityId: string;
  /** Operação registrada (ex.: "Added", "Modified", "Deleted"). */
  action: string;
  /** Identificador do usuário responsável pela mutação. */
  userId: string | null;
  /** Endereço IP de origem da requisição. */
  ipAddress: string | null;
  /** Instante (UTC, ISO 8601) em que a mutação foi registrada. */
  timestampUtc: string;
  /** Estado anterior (JSON serializado), quando aplicável. */
  oldValues: string | null;
  /** Estado posterior (JSON serializado), quando aplicável. */
  newValues: string | null;
}

/** Resultado paginado da consulta à trilha de auditoria. */
export interface AuditoriaPagina {
  total: number;
  pagina: number;
  tamanho: number;
  itens: AuditoriaItem[];
}

/** Filtros da consulta à trilha (todos opcionais; paginação 1-based). */
export interface AuditoriaFiltro {
  entidade?: string;
  usuario?: string;
  acao?: string;
  pagina: number;
  tamanho: number;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação; a chave inclui os filtros)
// ---------------------------------------------------------------------------

export const auditoriaKeys = {
  all: ['admin', 'auditoria'] as const,
  lista: (filtro: AuditoriaFiltro) => [...auditoriaKeys.all, 'lista', filtro] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function normalizarTexto(valor?: string): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function consultarAuditoria(filtro: AuditoriaFiltro, signal?: AbortSignal): Promise<AuditoriaPagina> {
  return http.get<AuditoriaPagina>('/admin/auditoria', {
    query: {
      entidade: normalizarTexto(filtro.entidade),
      usuario: normalizarTexto(filtro.usuario),
      acao: normalizarTexto(filtro.acao),
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/**
 * Consulta paginada à trilha de auditoria. Mantém os dados anteriores enquanto a
 * próxima página carrega (keepPreviousData) para evitar "flicker" na navegação.
 * `enabled` permite suprimir a requisição quando o usuário não tem permissão
 * (gating de UI — o backend é a fonte da verdade de autorização).
 */
export function useAuditoria(filtro: AuditoriaFiltro, enabled = true) {
  return useQuery({
    queryKey: auditoriaKeys.lista(filtro),
    queryFn: ({ signal }) => consultarAuditoria(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}
