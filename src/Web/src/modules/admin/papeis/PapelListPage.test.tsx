// Teste da tela de gestão de Papéis (RBAC / Admin), seguindo o PADRÃO-OURO de
// UsuarioListPage.test.tsx + ProcessoListPage.test.tsx. Mocka o fetch global para
// isolar a UI da rede e injeta um JWT com a permissão "identidade.usuarios.gerenciar"
// para que as ações fiquem visíveis. Cobre: render da lista (nome + nº de
// permissões), estado vazio, gating das ações e abertura do modal de criação com
// catálogo de permissões agrupado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { PapelListPage } from './PapelListPage';
import type { Papel } from './papel.api';

const PAPEIS: Papel[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    nome: 'Fiscal de Tributos',
    permissoes: ['tributos.cda.gerar', 'tributos.iptu.lancar'],
  },
];

const CATALOGO = ['tributos.cda.gerar', 'tributos.iptu.lancar', 'identidade.usuarios.gerenciar'];

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

/** Roteia o fetch por URL: /papeis -> lista; /permissoes -> catálogo. Estável a refetches. */
function mockFetchPorRota(papeis: unknown, catalogo: unknown): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    if (url.includes('/identidade/permissoes')) return Promise.resolve(jsonResponse(catalogo));
    return Promise.resolve(jsonResponse(papeis));
  });
}

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <PapelListPage />
    </AuthProvider>,
  );
}

describe('PapelListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista os papéis com nome e contagem de permissões', async () => {
    mockFetchOnce(PAPEIS);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    expect(await screen.findByText('Fiscal de Tributos')).toBeInTheDocument();
    expect(screen.getByText('2 permissões')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há papéis', async () => {
    mockFetchOnce([]);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    await waitFor(() =>
      expect(screen.getByText('Nenhum papel cadastrado')).toBeInTheDocument(),
    );
  });

  it('esconde as ações de gestão quando o usuário não tem permissão', async () => {
    mockFetchOnce(PAPEIS);
    renderComPermissao(['outra.permissao']);

    expect(await screen.findByText('Fiscal de Tributos')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Criar papel/i })).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Editar permissões/i }),
    ).not.toBeInTheDocument();
  });

  it('abre o modal de criação com o catálogo de permissões agrupado', async () => {
    const user = userEvent.setup();
    mockFetchPorRota(PAPEIS, CATALOGO);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    await screen.findByText('Fiscal de Tributos');
    await user.click(screen.getByRole('button', { name: /Criar papel/i }));

    expect(await screen.findByRole('dialog')).toHaveAccessibleName(/Criar papel/i);
    // O catálogo agrupado expõe as permissões como checkboxes rotulados pela chave.
    expect(
      await screen.findByRole('checkbox', { name: /tributos\.cda\.gerar/i }),
    ).toBeInTheDocument();
  });
});
