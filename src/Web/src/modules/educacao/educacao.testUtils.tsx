// Utilitários de teste do módulo Educação: renderiza dentro do AuthProvider com um
// JWT falso (apenas payload) contendo as claims de permissão desejadas, sobre o
// renderWithProviders padrão (QueryClient + Router + Toast). Espelha Can.test.tsx.
import type { ReactElement } from 'react';
import { renderWithProviders } from '../../test/renderWithProviders';
import type { RenderOptions } from '../../test/renderWithProviders';
import { setAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';

function toBase64Url(obj: unknown): string {
  const json = JSON.stringify(obj);
  const bytes = new TextEncoder().encode(json);
  let binary = '';
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeToken(perm: string[]): string {
  const header = toBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = toBase64Url({
    sub: 'user-1',
    name: 'Servidor Teste',
    email: 'servidor@orgao.gov.br',
    tenant_id: 'tenant-1',
    perm,
  });
  return `${header}.${payload}.`;
}

/** Renderiza a UI autenticada com as permissões informadas (padrão: ver + gerenciar). */
export function renderEducacao(
  ui: ReactElement,
  perm: string[] = ['educacao.ver', 'educacao.gerenciar'],
  options: RenderOptions = {},
) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(<AuthProvider>{ui}</AuthProvider>, options);
}
