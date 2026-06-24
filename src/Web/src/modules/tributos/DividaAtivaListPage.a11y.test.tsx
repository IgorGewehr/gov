// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// DividaAtivaListPage — PÁGINA PRINCIPAL (índice) do módulo Tributos. Valida o
// estado inicial (pré-consulta) e a tabela carregada após consultar um contribuinte.
// O fetch é mockado; um JWT com a claim "perm" libera as ações gated.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
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

function renderPagina() {
  setAccessToken(fakeToken(['tributos.ver', 'tributos.gerenciar']));
  return renderWithProviders(
    <AuthProvider>
      <DividaAtivaListPage />
    </AuthProvider>,
  );
}

describe('DividaAtivaListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações no estado inicial (sem consulta)', async () => {
    const { container } = renderPagina();
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações com as dívidas em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DIVIDAS);
    const { container } = renderPagina();

    await user.type(screen.getByLabelText(/Identificador do contribuinte/i), 'contrib-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('R$ 1.234,56')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
