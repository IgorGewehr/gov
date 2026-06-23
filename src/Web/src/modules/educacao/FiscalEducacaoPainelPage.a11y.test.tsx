// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) do
// Painel Fiscal da Educação — indicador MDE (25%, CF art. 212) e FUNDEB 70%
// (EC 108/2020). O fetch é mockado; um JWT com as claims "educacao.ver"/
// "educacao.gerenciar" libera a área e as ações de gestão.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { FiscalEducacaoPainelPage } from './FiscalEducacaoPainelPage';

const ANO = new Date().getFullYear();

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

// Responde MDE ou FUNDEB conforme a URL chamada (ambas as queries disparam no mount).
function mockFetchPorRota(): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    const corpo = url.includes('/fundeb/')
      ? {
          exercicio: ANO,
          receitaFundeb: 5_000_000,
          remuneracaoProfissionais: 3_600_000,
          percentualAplicado: 0.72,
          pisoMinimo: 0.7,
          atingido: true,
        }
      : {
          exercicio: ANO,
          natureza: 1,
          receitaBase: 10_000_000,
          aplicadoMde: 2_600_000,
          percentualAplicado: 0.26,
          percentualMinimo: 0.25,
          atingido: true,
          ehConformidade: true,
        };
    return new Response(JSON.stringify(corpo), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    });
  });
}

function renderPagina() {
  setAccessToken(fakeToken(['educacao.ver', 'educacao.gerenciar']));
  return renderWithProviders(
    <AuthProvider>
      <Routes>
        <Route path="/educacao/fiscal" element={<FiscalEducacaoPainelPage />} />
      </Routes>
    </AuthProvider>,
    { route: '/educacao/fiscal' },
  );
}

describe('FiscalEducacaoPainelPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com MDE e FUNDEB 70% carregados', async () => {
    mockFetchPorRota();
    const { container } = renderPagina();
    expect(await screen.findByText(/Indicador MDE/i)).toBeInTheDocument();
    expect(await screen.findByText('Piso atingido')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
