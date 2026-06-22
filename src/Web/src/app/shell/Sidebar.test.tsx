// Teste do gating de visibilidade da Sidebar por permissão. Itens de domínio
// (sem nav.permissions) aparecem sempre que há sessão; o item da Administração
// do Sistema (com nav.permissions) só aparece para quem possui ALGUMA permissão
// admin (semântica "OU"). Monta um JWT real (só o payload importa para a UI) com
// a claim "perm" e o injeta via AuthProvider, como em auth/Can.test.tsx.
import { describe, it, expect, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { Sidebar } from './Sidebar';

function toBase64Url(obj: unknown): string {
  const bytes = new TextEncoder().encode(JSON.stringify(obj));
  let binary = '';
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeToken(perm: string[]): string {
  const header = toBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = toBase64Url({ sub: 'u-1', name: 'Servidor', tenant_id: 't-1', perm });
  return `${header}.${payload}.`;
}

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <Sidebar />
    </AuthProvider>,
  );
}

describe('Sidebar', () => {
  afterEach(() => {
    clearAccessToken();
  });

  it('mostra os módulos de domínio para qualquer sessão autenticada', () => {
    renderComPermissao([]);
    expect(screen.getByRole('link', { name: /Protocolo/i })).toBeInTheDocument();
  });

  it('esconde a Administração do Sistema quando o usuário não tem permissão admin', () => {
    renderComPermissao(['outra.permissao']);
    expect(
      screen.queryByRole('link', { name: /Administração do Sistema/i }),
    ).not.toBeInTheDocument();
  });

  it('mostra a Administração do Sistema com qualquer permissão admin (OU)', () => {
    renderComPermissao(['admin.auditoria.ver']);
    expect(
      screen.getByRole('link', { name: /Administração do Sistema/i }),
    ).toBeInTheDocument();
  });
});
