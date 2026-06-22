// Teste do gating de UI por permissão (Can / useHasPermission). Monta um JWT real
// (sem assinatura — apenas o payload importa para a UI) com a claim "perm" e o
// injeta no sessionStorage; o AuthProvider deriva o usuário das claims. Cobre os
// dois caminhos: permissão presente (renderiza children) e ausente (renderiza nada
// ou o fallback).
import { describe, it, expect, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../api/authToken';
import { AuthProvider } from './AuthProvider';
import { Can } from './Can';

// Codifica um payload como segmento JWT base64url (compatível com base64UrlDecode).
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

function renderWithAuth(perm: string[], ui: React.ReactElement) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(<AuthProvider>{ui}</AuthProvider>);
}

describe('Can', () => {
  afterEach(() => {
    clearAccessToken();
  });

  it('renderiza os children quando o usuário tem a permissão', () => {
    renderWithAuth(
      ['identidade.usuarios.gerenciar'],
      <Can permission="identidade.usuarios.gerenciar">
        <button type="button">Novo usuário</button>
      </Can>,
    );
    expect(screen.getByRole('button', { name: 'Novo usuário' })).toBeInTheDocument();
  });

  it('não renderiza os children quando o usuário não tem a permissão', () => {
    renderWithAuth(
      ['outra.permissao'],
      <Can permission="identidade.usuarios.gerenciar">
        <button type="button">Novo usuário</button>
      </Can>,
    );
    expect(screen.queryByRole('button', { name: 'Novo usuário' })).not.toBeInTheDocument();
  });

  it('renderiza o fallback quando a permissão está ausente', () => {
    renderWithAuth(
      [],
      <Can permission="admin.auditoria.ver" fallback={<span>Sem acesso</span>}>
        <span>Trilha de auditoria</span>
      </Can>,
    );
    expect(screen.getByText('Sem acesso')).toBeInTheDocument();
    expect(screen.queryByText('Trilha de auditoria')).not.toBeInTheDocument();
  });
});
