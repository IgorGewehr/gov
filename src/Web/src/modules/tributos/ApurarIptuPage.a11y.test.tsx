// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// ApurarIptuPage — coração do submódulo IPTU. Valida o estado inicial e a
// apuração carregada (memória de cálculo em DataTable + Alert de cota única).
// O fetch é mockado; um JWT com a claim "perm" libera a ação de lançamento.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { ApurarIptuPage } from './ApurarIptuPage';
import type { ApuracaoIptu } from './iptu.api';

const APURACAO: ApuracaoIptu = {
  imovelId: 'imovel-1',
  exercicio: 2026,
  valorTerreno: 100000,
  valorConstrucao: 150000,
  valorVenal: 250000,
  aliquotaPercentual: 2.0,
  impostoBruto: 5000,
  valorIsencao: 0,
  valorDesconto: 0,
  impostoDevido: 5000,
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
  setAccessToken(fakeToken(['tributos.ver', 'tributos.gerenciar']));
  return renderWithProviders(
    <AuthProvider>
      <Routes>
        <Route path="/tributos/imoveis/:id/iptu" element={<ApurarIptuPage />} />
      </Routes>
    </AuthProvider>,
    { route: '/tributos/imoveis/imovel-1/iptu' },
  );
}

describe('ApurarIptuPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações no estado inicial (sem exercício apurado)', async () => {
    mockFetch(APURACAO);
    const { container } = renderPagina();
    expect(await screen.findByText('Informe o exercício')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações com a apuração carregada', async () => {
    const user = userEvent.setup();
    mockFetch(APURACAO);
    const { container } = renderPagina();

    await user.click(screen.getByRole('button', { name: /Apurar/i }));

    expect(await screen.findByText(/Apuração do IPTU/i)).toBeInTheDocument();
    expect(await screen.findByText('Imposto devido')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
