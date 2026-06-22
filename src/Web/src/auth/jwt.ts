// Decodificação do payload do JWT (apenas leitura de claims para a UI — a
// validação real é do backend). NÃO confiar nestas claims para autorização server-side.
import type { AuthenticatedUser } from './types';

interface JwtPayload {
  sub?: string;
  name?: string;
  email?: string;
  tenant_id?: string;
  tenant_name?: string;
  role?: string | string[];
  /** Permissões granulares emitidas pelo backend (claim "perm"). */
  perm?: string | string[];
  exp?: number;
  [key: string]: unknown;
}

function base64UrlDecode(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  // atob lida com latin1; decodifica UTF-8 corretamente para acentos PT-BR.
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
  return new TextDecoder('utf-8').decode(bytes);
}

function parsePayload(token: string): JwtPayload | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;
  try {
    return JSON.parse(base64UrlDecode(parts[1])) as JwtPayload;
  } catch {
    return null;
  }
}

/** true quando o token está estruturalmente inválido ou expirado (claim exp). */
export function isTokenExpired(token: string): boolean {
  const payload = parsePayload(token);
  if (!payload?.exp) return false; // sem exp: deixa o backend decidir.
  return Date.now() >= payload.exp * 1000;
}

/** Extrai o usuário a partir das claims; null se o token for inválido. */
export function userFromToken(token: string): AuthenticatedUser | null {
  const payload = parsePayload(token);
  if (!payload?.sub || !payload.tenant_id) return null;
  const roles = Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : [];
  const permissions = Array.isArray(payload.perm) ? payload.perm : payload.perm ? [payload.perm] : [];
  return {
    id: payload.sub,
    nome: payload.name ?? payload.email ?? 'Usuário',
    email: payload.email ?? null,
    tenantId: payload.tenant_id,
    tenantNome: payload.tenant_name ?? null,
    roles,
    permissions,
  };
}
