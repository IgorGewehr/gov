// Camada de API da Configuração de Módulos licenciados do tenant (módulo Admin).
// Segue o PADRÃO-OURO do agregado Processo (protocolo/processo.api.ts):
//   - DTOs no topo (espelham o contrato real do ApiHost);
//   - query keys centralizadas (por tenant) para invalidação consistente;
//   - funções de acesso via http client tipado (Authorization + ProblemDetails);
//   - hooks TanStack Query (useQuery/useMutation) com invalidação dirigida.
//
// Contrato REAL (Admin endpoints):
//   GET  /api/admin/tenants/{tenantId}/modulos           -> [{ modulo, ativo }]
//   PUT  /api/admin/tenants/{tenantId}/modulos/{modulo}  { ativo: boolean } -> 204
//
// O tenantId vem SEMPRE da sessão (claim "tenant_id" => user.tenantId): a UI
// nunca permite escolher o tenant, preservando o isolamento multi-tenant.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Estado de licenciamento de um módulo para o tenant (projeção do backend). */
export interface TenantModulo {
  /** Identificador canônico do módulo (ex.: "tributos", "saude"). */
  modulo: string;
  /** true quando o módulo está ativo (licenciado/habilitado) para o tenant. */
  ativo: boolean;
}

/** Corpo do PUT que ativa/desativa um módulo. */
export interface DefinirModuloInput {
  ativo: boolean;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação) — sempre escopadas pelo tenant
// ---------------------------------------------------------------------------

export const modulosKeys = {
  all: ['admin', 'modulos'] as const,
  porTenant: (tenantId: string) => [...modulosKeys.all, tenantId] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarModulos(tenantId: string, signal?: AbortSignal): Promise<TenantModulo[]> {
  return http.get<TenantModulo[]>(`/admin/tenants/${encodeURIComponent(tenantId)}/modulos`, { signal });
}

function definirModulo(tenantId: string, modulo: string, input: DefinirModuloInput): Promise<void> {
  return http.put<void>(
    `/admin/tenants/${encodeURIComponent(tenantId)}/modulos/${encodeURIComponent(modulo)}`,
    input,
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERY
// ---------------------------------------------------------------------------

/** Lista os módulos do tenant e seu estado de ativação (sob demanda via enabled). */
export function useModulosDoTenant(tenantId: string, enabled = true) {
  return useQuery({
    queryKey: modulosKeys.porTenant(tenantId),
    queryFn: ({ signal }) => listarModulos(tenantId, signal),
    enabled: enabled && tenantId.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMAND
// ---------------------------------------------------------------------------

/**
 * Ativa/desativa um módulo do tenant. Ao concluir, invalida a lista do tenant
 * para refletir o novo estado vindo do backend (fonte da verdade).
 */
export function useDefinirModulo(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ modulo, ativo }: { modulo: string; ativo: boolean }) =>
      definirModulo(tenantId, modulo, { ativo }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: modulosKeys.porTenant(tenantId) });
    },
  });
}
