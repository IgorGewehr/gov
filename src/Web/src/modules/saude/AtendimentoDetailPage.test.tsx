// Teste da tela de DETALHE do Atendimento (Vitest + Testing Library). Cobre: render do
// detalhe a partir do id da rota, o gating das ações ("saude.gerenciar") e a abertura
// do modal de evolução SOAP. O fetch global é mockado para isolar a UI.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { AtendimentoDetailPage } from './AtendimentoDetailPage';
import type { AtendimentoDetalhe } from './api';

const ATENDIMENTO: AtendimentoDetalhe = {
  id: '33333333-3333-3333-3333-333333333333',
  pacienteId: '11111111-1111-1111-1111-111111111111',
  estabelecimentoId: 'cnes-1',
  profissionalId: 'prof-1',
  dataHora: '2026-02-10T13:00:00Z',
  competencia: '2026-02',
  modalidade: 'Presencial',
  situacao: 'EmAndamento',
  nivelGarantia: 'NGS1',
  assinado: false,
  evolucoes: [],
  prescricoes: [],
  exames: [],
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
  const payload = toBase64Url({ sub: 'u-1', name: 'Médico', tenant_id: 't-1', perm });
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

function renderDetail(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <Routes>
        <Route path="/saude/atendimentos/:atendimentoId" element={<AtendimentoDetailPage />} />
      </Routes>
    </AuthProvider>,
    { route: '/saude/atendimentos/33333333-3333-3333-3333-333333333333' },
  );
}

describe('AtendimentoDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra o detalhe e as ações de gestão (com permissão)', async () => {
    mockFetchOnce(ATENDIMENTO);
    renderDetail(['saude.ver', 'saude.gerenciar']);

    expect(await screen.findByText('2026-02')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Adicionar evolução/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Assinar/i })).toBeInTheDocument();
  });

  it('abre o modal de evolução SOAP ao acionar a ação', async () => {
    const user = userEvent.setup();
    mockFetchOnce(ATENDIMENTO);
    renderDetail(['saude.ver', 'saude.gerenciar']);

    await screen.findByText('2026-02');
    await user.click(screen.getByRole('button', { name: /Adicionar evolução/i }));
    expect(await screen.findByLabelText(/Subjetivo/i)).toBeInTheDocument();
  });

  it('esconde as ações sem a permissão "saude.gerenciar"', async () => {
    mockFetchOnce(ATENDIMENTO);
    renderDetail(['saude.ver']);

    await screen.findByText('2026-02');
    expect(screen.queryByRole('button', { name: /Adicionar evolução/i })).not.toBeInTheDocument();
  });
});
