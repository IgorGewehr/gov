// Teste da tela de Estrutura Organizacional (Identidade / Admin), seguindo o
// PADRÃO-OURO de UsuarioListPage.test.tsx + Can.test.tsx (gating). Mocka o fetch
// global para isolar a UI da rede e injeta um JWT com a permissão
// "identidade.usuarios.gerenciar" para que as ações fiquem visíveis. Cobre: render
// da árvore (com indentação hierárquica), estado vazio, gating das ações e abertura
// do modal de criação de raiz.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { UnidadesPage } from './UnidadesPage';
import type { NoUnidade } from './unidades.api';

const ARVORE: NoUnidade[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    codigo: 'SEMSA',
    nome: 'Secretaria de Saúde',
    tipo: 'Secretaria',
    ativa: true,
    filhos: [
      {
        id: '22222222-2222-2222-2222-222222222222',
        codigo: 'DEPVS',
        nome: 'Departamento de Vigilância',
        tipo: 'Departamento',
        ativa: true,
        filhos: [],
      },
    ],
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

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <UnidadesPage />
    </AuthProvider>,
  );
}

describe('UnidadesPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista a árvore de unidades (raiz e subunidade) com código e tipo', async () => {
    mockFetchOnce(ARVORE);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    expect(await screen.findByText('Secretaria de Saúde')).toBeInTheDocument();
    expect(screen.getByText('Departamento de Vigilância')).toBeInTheDocument();
    expect(screen.getByText('SEMSA')).toBeInTheDocument();
    expect(screen.getByText('Departamento')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há unidades', async () => {
    mockFetchOnce([]);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    await waitFor(() =>
      expect(screen.getByText('Nenhuma unidade cadastrada')).toBeInTheDocument(),
    );
  });

  it('esconde as ações de gestão quando o usuário não tem permissão', async () => {
    mockFetchOnce(ARVORE);
    renderComPermissao(['outra.permissao']);

    expect(await screen.findByText('Secretaria de Saúde')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Nova unidade raiz/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Editar$/i })).not.toBeInTheDocument();
  });

  it('abre o modal de criação ao clicar em "Nova unidade raiz"', async () => {
    const user = userEvent.setup();
    mockFetchOnce(ARVORE);
    renderComPermissao(['identidade.usuarios.gerenciar']);

    await screen.findByText('Secretaria de Saúde');
    await user.click(screen.getByRole('button', { name: /Nova unidade raiz/i }));

    expect(await screen.findByRole('dialog')).toHaveAccessibleName(/Nova unidade/i);
  });
});
