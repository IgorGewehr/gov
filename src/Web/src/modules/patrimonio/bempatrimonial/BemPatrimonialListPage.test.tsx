// Teste (Vitest + Testing Library) da tela de lista de Bens Patrimoniais, no
// PADRÃO-OURO de src/modules/tributos. Cobre os estados que toda tela de lista
// deve ter: inicial (sem competência), sucesso (tabela de depreciáveis) e vazio.
// O fetch global é mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { BemPatrimonialListPage } from './BemPatrimonialListPage';
import type { BemDepreciavelResumo } from './bempatrimonial.api';

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
      <BemPatrimonialListPage />
    </AuthProvider>,
  );
}

const DEPRECIAVEIS: BemDepreciavelResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    numeroTombamento: 'TOMBO-2026-0001',
    valorContabil: 1234.56,
    valorResidual: 100,
    parcelaMensal: 50,
  },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('BemPatrimonialListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial pedindo uma competência', () => {
    renderComPermissao(['patrimonio.ver']);
    expect(screen.getByText('Selecione uma competência')).toBeInTheDocument();
  });

  it('consulta e mostra os bens depreciáveis em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DEPRECIAVEIS);
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    expect(await screen.findByText('TOMBO-2026-0001')).toBeInTheDocument();
    expect(screen.getByText('R$ 1.234,56')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há bens depreciáveis', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    await waitFor(() =>
      expect(screen.getByText('Nenhum bem depreciável')).toBeInTheDocument(),
    );
  });

  it('oculta a ação de incorporar bem sem a permissão patrimonio.gerenciar', () => {
    renderComPermissao(['patrimonio.ver']);
    expect(screen.queryByRole('button', { name: /Incorporar bem/i })).not.toBeInTheDocument();
  });

  it('exibe a ação de incorporar bem com a permissão patrimonio.gerenciar', () => {
    renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);
    expect(screen.getByRole('button', { name: /Incorporar bem/i })).toBeInTheDocument();
  });
});
