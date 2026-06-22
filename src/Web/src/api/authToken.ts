// Armazenamento do JWT em sessionStorage (some ao fechar a aba — menor superfície
// que localStorage). O token carrega a claim "tenant_id" usada pelo backend para
// o isolamento multi-tenant. NUNCA logar nem persistir o token fora daqui.

const STORAGE_KEY = 'tensorroot.access_token';

/** Evento disparado quando o token muda (login/logout/expiração) para sincronizar a UI. */
export const AUTH_TOKEN_CHANGED = 'tensorroot:auth-token-changed';

export function getAccessToken(): string | null {
  return sessionStorage.getItem(STORAGE_KEY);
}

export function setAccessToken(token: string): void {
  sessionStorage.setItem(STORAGE_KEY, token);
  window.dispatchEvent(new Event(AUTH_TOKEN_CHANGED));
}

export function clearAccessToken(): void {
  sessionStorage.removeItem(STORAGE_KEY);
  window.dispatchEvent(new Event(AUTH_TOKEN_CHANGED));
}
