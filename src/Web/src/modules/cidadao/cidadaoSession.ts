// Sessao do cidadao derivada das claims do JWT do realm externo. Apenas LEITURA para a
// UI — a validacao real e do backend (NAO confiar nestas claims para autorizacao).
// Claims emitidas por EmissorTokenCidadao.cs: sub, name, tenant_id, tipo=cidadao,
// documento e (opcional) selo.

/** Cidadao autenticado no portal (projecao das claims). */
export interface CidadaoSessao {
  id: string;
  nome: string;
  /** CPF/CNPJ (somente digitos) do proprio cidadao — claim "documento". */
  documento: string;
  tenantId: string;
}

interface CidadaoJwtPayload {
  sub?: string;
  name?: string;
  tenant_id?: string;
  tipo?: string;
  documento?: string;
  exp?: number;
}

function base64UrlDecode(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
  return new TextDecoder('utf-8').decode(bytes);
}

function parsePayload(token: string): CidadaoJwtPayload | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;
  try {
    return JSON.parse(base64UrlDecode(parts[1])) as CidadaoJwtPayload;
  } catch {
    return null;
  }
}

/** true quando o token esta estruturalmente invalido ou expirado (claim exp). */
export function cidadaoTokenExpirado(token: string): boolean {
  const payload = parsePayload(token);
  if (!payload?.exp) return false;
  return Date.now() >= payload.exp * 1000;
}

/** Extrai a sessao do cidadao; null se invalido OU se nao for do realm cidadao. */
export function cidadaoFromToken(token: string): CidadaoSessao | null {
  const payload = parsePayload(token);
  // Discriminante de realm: so aceita tokens com tipo=cidadao (defesa contra reuso de
  // um token de admin nesta chave). Documento e tenant sao obrigatorios.
  if (!payload?.sub || payload.tipo !== 'cidadao' || !payload.tenant_id || !payload.documento) {
    return null;
  }
  return {
    id: payload.sub,
    nome: payload.name ?? 'Cidadao',
    documento: payload.documento,
    tenantId: payload.tenant_id,
  };
}
