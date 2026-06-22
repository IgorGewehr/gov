// Teste (Vitest + Testing Library) da tela de lista de Sessoes. Cobre sucesso
// (tabela), vazio e a interacao de abrir o formulario de agendamento. O fetch
// global e mockado; <AuthProvider> via renderComAuth libera as acoes (<Can>).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { SessaoListPage } from './SessaoListPage';
import type { SessaoResumo } from './api';

const PERMS = ['legislativo.ver', 'legislativo.gerenciar'];

const SESSOES: SessaoResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    tipo: 'Ordinaria',
    dataHora: '2026-04-01T19:00:00Z',
    situacao: 'Agendada',
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

describe('SessaoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista as sessões agendadas em tabela', async () => {
    mockFetchOnce(SESSOES);
    renderComAuth(<SessaoListPage />, PERMS);

    expect(await screen.findByText('Ordinaria')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Sessões agendadas/i);
  });

  it('mostra o estado vazio quando não há sessões', async () => {
    mockFetchOnce([]);
    renderComAuth(<SessaoListPage />, PERMS);

    await waitFor(() =>
      expect(screen.getByText('Nenhuma sessão agendada')).toBeInTheDocument(),
    );
  });

  it('abre o formulário de agendamento ao clicar na ação', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComAuth(<SessaoListPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Agendar sessão/i }));

    expect(await screen.findByRole('dialog')).toHaveAccessibleName(/Agendar sessão/i);
  });
});
