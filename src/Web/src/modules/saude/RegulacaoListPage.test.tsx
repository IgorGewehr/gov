// Teste (Vitest + Testing Library) da FILA DE REGULAÇÃO. Cobre: render com sucesso
// (tabela da fila), o gating da ação "Nova solicitação" ("saude.gerenciar") e o
// estado vazio. fetch global mockado; JWT com claim "perm" libera/oculta as ações.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { RegulacaoListPage } from './RegulacaoListPage';
import type { SolicitacaoRegulacaoResumo } from './api';

const FILA: SolicitacaoRegulacaoResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    pacienteId: '11111111-1111-1111-1111-111111111111',
    codigoSigtap: '0301010010',
    prioridade: 'Urgente',
    situacao: 'Solicitada',
    dataSolicitacao: '2026-02-01',
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

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <RegulacaoListPage />
    </AuthProvider>,
  );
}

describe('RegulacaoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista as solicitações da fila em tabela', async () => {
    mockFetchOnce(FILA);
    renderComPermissao(['saude.ver']);

    expect(await screen.findByText('0301010010')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Urgente' })).toBeInTheDocument();
  });

  it('oculta a ação "Nova solicitação" sem a permissão "saude.gerenciar"', async () => {
    mockFetchOnce(FILA);
    renderComPermissao(['saude.ver']);

    await screen.findByText('0301010010');
    expect(screen.queryByRole('button', { name: /Nova solicitação/i })).not.toBeInTheDocument();
  });

  it('exibe a ação "Nova solicitação" com a permissão "saude.gerenciar"', async () => {
    mockFetchOnce(FILA);
    renderComPermissao(['saude.ver', 'saude.gerenciar']);

    await screen.findByText('0301010010');
    expect(screen.getByRole('button', { name: /Nova solicitação/i })).toBeInTheDocument();
  });

  it('mostra o estado vazio quando a fila está vazia', async () => {
    mockFetchOnce([]);
    renderComPermissao(['saude.ver']);

    await waitFor(() => expect(screen.getByText('Fila vazia')).toBeInTheDocument());
  });
});
