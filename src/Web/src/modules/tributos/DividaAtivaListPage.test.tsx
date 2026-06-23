// Teste (Vitest + Testing Library) da tela de consulta de Dívida Ativa por
// contribuinte. Cobre os estados que toda tela de lista deve ter: inicial (sem
// consulta), sucesso (tabela), e o gating das ações ("tributos.gerenciar"). O
// fetch global é mockado para isolar da rede e um JWT com a claim "perm" é
// injetado para liberar/ocultar as ações (padrão EmpenhoListPage.test.tsx).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { DividaAtivaListPage } from './DividaAtivaListPage';
import type { DividaAtivaResumo } from './api';

const DIVIDAS: DividaAtivaResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    contribuinteId: 'contrib-1',
    valorOriginario: 1234.56,
    situacao: 'Inscrita',
    dataInscricao: '2024-01-10',
    dataPrescricao: '2029-01-10',
    numeroCda: null,
    numeroInscricao: 2024000001,
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
  const payload = toBase64Url({ sub: 'u-1', name: 'Servidor', tenant_id: 't-1', perm });
  return `${header}.${payload}.`;
}

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <DividaAtivaListPage />
    </AuthProvider>,
  );
}

describe('DividaAtivaListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComPermissao(['tributos.ver']);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('mostra as ações de cabeçalho apenas com a permissão tributos.gerenciar', () => {
    const { unmount } = renderComPermissao(['tributos.ver']);
    expect(screen.queryByRole('button', { name: /Lançar crédito/i })).not.toBeInTheDocument();
    unmount();

    renderComPermissao(['tributos.ver', 'tributos.gerenciar']);
    expect(screen.getByRole('button', { name: /Lançar crédito/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Cadastrar contribuinte/i })).toBeInTheDocument();
  });

  it('consulta e mostra as dívidas em tabela com a ação Emitir CDA', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DIVIDAS);
    renderComPermissao(['tributos.ver', 'tributos.gerenciar']);

    await user.type(screen.getByLabelText(/Identificador do contribuinte/i), 'contrib-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('R$ 1.234,56')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(
      /Dívidas ativas do contribuinte contrib-1/i,
    );
    // Dívida "Inscrita" admite emissão de CDA (botão gated por tributos.gerenciar).
    expect(await screen.findByRole('button', { name: /Emitir CDA/i })).toBeInTheDocument();
  });
});
