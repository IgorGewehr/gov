// Armazenamento do JWT do CIDADAO — realm EXTERNO, separado do token do admin.
//
// COEXISTENCIA: o admin guarda o token em 'tensorroot.access_token' (api/authToken.ts);
// o cidadao usa OUTRA chave ('tensorroot.cidadao_token'). Assim um servidor pode estar
// logado no back-office E o portal do cidadao continua com sessao propria, sem um
// realm derrubar o outro. Como o admin, fica em sessionStorage (some ao fechar a aba —
// menor superficie) e NUNCA e logado.
//
// O token do cidadao carrega as claims: sub, name, tenant_id, tipo=cidadao, documento e
// (opcional) selo. O backend resolve o dado-proprio do PROPRIO token (a UI nunca envia
// CPF/id de terceiro) — ver CidadaoEndpoints.cs (/api/cidadao/meus-*).

const STORAGE_KEY = 'tensorroot.cidadao_token';

/** Evento disparado quando o token do cidadao muda (login/logout/expiracao). */
export const CIDADAO_TOKEN_CHANGED = 'tensorroot:cidadao-token-changed';

export function getCidadaoToken(): string | null {
  return sessionStorage.getItem(STORAGE_KEY);
}

export function setCidadaoToken(token: string): void {
  sessionStorage.setItem(STORAGE_KEY, token);
  window.dispatchEvent(new Event(CIDADAO_TOKEN_CHANGED));
}

export function clearCidadaoToken(): void {
  sessionStorage.removeItem(STORAGE_KEY);
  window.dispatchEvent(new Event(CIDADAO_TOKEN_CHANGED));
}
