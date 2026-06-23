// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// EmitirAlvaraPage — submódulo Alvarás. Valida o estado inicial (formulário de
// emissão + sub-nav de Tributos) e o resultado da emissão (alvará + TLL). O fetch é
// mockado; um JWT com a claim "perm" libera a ação de emissão.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { EmitirAlvaraPage } from './EmitirAlvaraPage';
import type { ResultadoEmissaoAlvara } from './alvaras.api';

const RESULTADO: ResultadoEmissaoAlvara = {
  alvaraId: 'alvara-1',
  lancamentoTllId: 'lanc-1',
  damId: 'dam-1',
  valorTll: 350.5,
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
        <Route path="/tributos/alvaras" element={<EmitirAlvaraPage />} />
      </Routes>
    </AuthProvider>,
    { route: '/tributos/alvaras' },
  );
}

describe('EmitirAlvaraPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações no estado inicial', async () => {
    mockFetch(RESULTADO);
    const { container } = renderPagina();
    expect(await screen.findByRole('heading', { name: /Emitir alvará/i })).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('emite o alvará e exibe a TLL apurada sem violações', async () => {
    const user = userEvent.setup();
    mockFetch(RESULTADO);
    const { container } = renderPagina();

    await user.type(screen.getByLabelText(/Identificador do contribuinte/i), 'contribuinte-1');
    await user.type(screen.getByLabelText(/^Estabelecimento/i), 'Padaria Central');
    await user.type(screen.getByLabelText(/Atividade \(CNAE\)/i), '4711-3/02');
    await user.type(screen.getByLabelText(/Início de vigência/i), '2026-01-01');
    await user.type(screen.getByLabelText(/Fim de vigência/i), '2026-12-31');
    await user.type(screen.getByLabelText(/Código da TLL/i), 'TLL-001');
    await user.type(screen.getByLabelText(/Vencimento da TLL/i), '2026-02-10');

    await user.click(screen.getByRole('button', { name: /Emitir alvará/i }));

    expect(await screen.findByText('Alvará emitido')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
