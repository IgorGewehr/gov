// Teste da tela de gestão de Usuários (Identidade / Admin), seguindo o PADRÃO-OURO
// de ProcessoListPage.test.tsx + Can.test.tsx (gating). Mocka o fetch global para
// isolar a UI da rede e injeta um JWT com a permissão "identidade.usuarios.gerenciar"
// para que as ações fiquem visíveis. Cobre: render da lista, estado vazio, gating
// das ações e abertura do modal de criação.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { UsuarioListPage } from './UsuarioListPage';
import type { UsuarioResumo } from './usuario.api';

const USUARIOS: UsuarioResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    nome: 'Maria Servidora',
    email: 'maria@orgao.gov.br',
    ativo: true,
    papeis: ['Administrador'],
  },
];

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
  const payload = toBase64Url({ sub: 'u-1', name: 'Admin', tenant_id: 't-1', perm });
  return `${header}.${payload}.`;
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(jsonResponse(body, status));
}

/** Mock por rota (resolve cada endpoint independentemente da ordem das queries). */
function mockFetchByUrl(map: { usuarios: unknown; papeis: unknown }): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    if (url.includes('/identidade/papeis')) return Promise.resolve(jsonResponse(map.papeis));
    return Promise.resolve(jsonResponse(map.usuarios));
  });
}

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <UsuarioListPage />
    </AuthProvider>,
  );
}

describe('UsuarioListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista os usuários com nome, e-mail, status e papéis', async () => {
    mockFetchOnce(USUARIOS);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    expect(await screen.findByText('Maria Servidora')).toBeInTheDocument();
    expect(screen.getByText('maria@orgao.gov.br')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Ativo' })).toBeInTheDocument();
    expect(screen.getByText('Administrador')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há usuários', async () => {
    mockFetchOnce([]);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    await waitFor(() =>
      expect(screen.getByText('Nenhum usuário cadastrado')).toBeInTheDocument(),
    );
  });

  it('esconde as ações de gestão quando o usuário não tem permissão', async () => {
    mockFetchOnce(USUARIOS);
    renderComPermissao(['outra.permissao']);

    expect(await screen.findByText('Maria Servidora')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Novo usuário/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Editar/i })).not.toBeInTheDocument();
  });

  it('abre o modal de criação ao clicar em "Novo usuário"', async () => {
    const user = userEvent.setup();
    mockFetchByUrl({ usuarios: USUARIOS, papeis: [] });
    renderComPermissao(['identidade.usuarios.gerenciar']);

    await screen.findByText('Maria Servidora');
    await user.click(screen.getByRole('button', { name: /Novo usuário/i }));

    expect(await screen.findByRole('dialog')).toHaveAccessibleName(/Novo usuário/i);
  });
});
