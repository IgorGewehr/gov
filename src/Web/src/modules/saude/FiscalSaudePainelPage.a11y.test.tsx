// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) do
// Painel Fiscal da Saúde — indicador ASPS (15%, LC 141/2012). O fetch é mockado;
// um JWT com as claims "saude.ver"/"saude.gerenciar" libera a área e as ações.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { FiscalSaudePainelPage } from './FiscalSaudePainelPage';
import type { ApuracaoAsps } from './fiscal.api';

const ASPS: ApuracaoAsps = {
  exercicio: new Date().getFullYear(),
  receitaBase: 10_000_000,
  aplicadoAsps: 1_700_000,
  percentualAplicado: 0.17,
  percentualMinimo: 0.15,
  atingido: true,
};

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

function mockFetch(body: unknown): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

function renderPagina() {
  setAccessToken(fakeToken(['saude.ver', 'saude.gerenciar']));
  return renderWithProviders(
    <AuthProvider>
      <Routes>
        <Route path="/saude/fiscal" element={<FiscalSaudePainelPage />} />
      </Routes>
    </AuthProvider>,
    { route: '/saude/fiscal' },
  );
}

describe('FiscalSaudePainelPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com o indicador ASPS carregado', async () => {
    mockFetch(ASPS);
    const { container } = renderPagina();
    expect(await screen.findByText(/Indicador ASPS/i)).toBeInTheDocument();
    expect(await screen.findByText('Mínimo atingido')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
