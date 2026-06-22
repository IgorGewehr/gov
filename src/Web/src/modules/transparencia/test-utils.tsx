// Utilitário de testes do módulo Transparência: renderiza páginas DENTRO do
// AuthProvider com um JWT falso (apenas payload) portando a claim "perm", para
// exercitar o gating de UI (<Can>) das ações (transparencia.gerenciar/ver).
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

/** Monta um JWT (sem assinatura) com as permissões informadas na claim "perm". */
export function fakeToken(perm: string[]): string {
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

/**
 * Renderiza a UI dentro do AuthProvider com a sessão portando `perm`.
 * Default: ambas as permissões do módulo (ver + gerenciar).
 */
export function renderWithAuth(
  ui: ReactElement,
  perm: string[] = ['transparencia.ver', 'transparencia.gerenciar'],
  options?: RenderOptions,
) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(<AuthProvider>{ui}</AuthProvider>, options);
}
