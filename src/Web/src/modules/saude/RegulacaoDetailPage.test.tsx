// Teste da tela de DETALHE da Solicitação de Regulação (Vitest + Testing Library).
// Cobre: render do detalhe a partir do id da rota, gating das ações ("saude.gerenciar")
// e abertura do modal de negativa (ação com motivo). fetch global mockado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { RegulacaoDetailPage } from './RegulacaoDetailPage';
import type { SolicitacaoRegulacaoDetalhe } from './api';

const SOLICITACAO: SolicitacaoRegulacaoDetalhe = {
  id: '22222222-2222-2222-2222-222222222222',
  pacienteId: '11111111-1111-1111-1111-111111111111',
  codigoSigtap: '0301010010',
  descricaoProcedimento: 'Consulta especializada',
  prioridade: 'Urgente',
  situacao: 'Solicitada',
  dataSolicitacao: '2026-02-01',
  dataAutorizacao: null,
  protocoloSisreg: null,
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
  const payload = toBase64Url({ sub: 'u-1', name: 'Regulador', tenant_id: 't-1', perm });
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
        <Route path="/saude/regulacao/:solicitacaoId" element={<RegulacaoDetailPage />} />
      </Routes>
    </AuthProvider>,
    { route: '/saude/regulacao/22222222-2222-2222-2222-222222222222' },
  );
}

describe('RegulacaoDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra o detalhe e as ações de decisão (com permissão)', async () => {
    mockFetchOnce(SOLICITACAO);
    renderDetail(['saude.ver', 'saude.gerenciar']);

    expect(await screen.findByText('Consulta especializada')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Autorizar/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^Negar/i })).toBeInTheDocument();
  });

  it('abre o modal de negativa ao acionar a ação', async () => {
    const user = userEvent.setup();
    mockFetchOnce(SOLICITACAO);
    renderDetail(['saude.ver', 'saude.gerenciar']);

    await screen.findByText('Consulta especializada');
    await user.click(screen.getByRole('button', { name: /^Negar/i }));
    expect(await screen.findByLabelText(/Motivo da negativa/i)).toBeInTheDocument();
  });

  it('esconde as ações sem a permissão "saude.gerenciar"', async () => {
    mockFetchOnce(SOLICITACAO);
    renderDetail(['saude.ver']);

    await screen.findByText('Consulta especializada');
    expect(screen.queryByRole('button', { name: /Autorizar/i })).not.toBeInTheDocument();
  });
});
