// Helper de testes que renderiza a UI DENTRO de um <AuthProvider> com um JWT falso
// (sem assinatura — só o payload importa para a UI) carregando as permissões (claim
// "perm"). Necessario para telas que usam <Can>/useHasPermission. Espelha o padrao
// usado em src/auth/Can.test.tsx, centralizado para reuso entre os modulos.
import type { ReactElement } from 'react';
import { setAccessToken } from '../api/authToken';
import { AuthProvider } from '../auth/AuthProvider';
import { renderWithProviders } from './renderWithProviders';
import type { RenderOptions } from './renderWithProviders';

/** Codifica um payload como segmento JWT base64url (compativel com base64UrlDecode). */
function toBase64Url(obj: unknown): string {
  const json = JSON.stringify(obj);
  const bytes = new TextEncoder().encode(json);
  let binary = '';
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/** Monta um JWT falso (alg none) com as permissoes informadas na claim "perm". */
export function tokenFalso(permissoes: string[]): string {
  const header = toBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = toBase64Url({
    sub: 'user-teste',
    name: 'Servidor Teste',
    email: 'servidor@orgao.gov.br',
    tenant_id: 'tenant-teste',
    perm: permissoes,
  });
  return `${header}.${payload}.`;
}

/**
 * Renderiza `ui` dentro de <AuthProvider> com sessao autenticada e as permissoes
 * informadas (default: assistenciasocial.ver + .gerenciar, para liberar todas as acoes).
 */
export function renderComAuth(
  ui: ReactElement,
  permissoes: string[] = ['assistenciasocial.ver', 'assistenciasocial.gerenciar'],
  options: RenderOptions = {},
) {
  setAccessToken(tokenFalso(permissoes));
  return renderWithProviders(<AuthProvider>{ui}</AuthProvider>, options);
}
