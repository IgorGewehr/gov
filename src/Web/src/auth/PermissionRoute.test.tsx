// Teste do guard de rota por permissão (PermissionRoute). Sem nenhuma das
// permissões exigidas, redireciona para o destino (Home); com ALGUMA, renderiza
// a subárvore (Outlet). Monta um JWT real (só o payload importa) e um router de
// memória mínimo com a rota guardada e a rota de destino.
import { describe, it, expect, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../api/authToken';
import { AuthProvider } from './AuthProvider';
import { PermissionRoute } from './PermissionRoute';

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

function renderEm(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <Routes>
        <Route path="/" element={<p>Home</p>} />
        <Route path="/admin" element={<PermissionRoute permissions={['admin.auditoria.ver']} />}>
          <Route index element={<p>Área administrativa</p>} />
        </Route>
      </Routes>
    </AuthProvider>,
    { route: '/admin' },
  );
}

describe('PermissionRoute', () => {
  afterEach(() => {
    clearAccessToken();
  });

  it('renderiza a subárvore quando o usuário tem a permissão', () => {
    renderEm(['admin.auditoria.ver']);
    expect(screen.getByText('Área administrativa')).toBeInTheDocument();
  });

  it('redireciona para a Home quando faltam todas as permissões', () => {
    renderEm(['outra.permissao']);
    expect(screen.queryByText('Área administrativa')).not.toBeInTheDocument();
    expect(screen.getByText('Home')).toBeInTheDocument();
  });
});
