// Teste (Vitest + Testing Library) da tela de lista de Proposicoes. Cobre os
// estados que toda tela de lista deve ter: carregamento -> sucesso (tabela) e
// vazio, alem da troca de filtro de situacao. O fetch global e mockado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { ProposicaoListPage } from './ProposicaoListPage';
import type { ProposicaoResumo } from './api';

const PERMS = ['legislativo.ver', 'legislativo.gerenciar'];

const PROPOSICOES: ProposicaoResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    tipo: 'ProjetoDeLeiOrdinaria',
    ementa: 'Dispõe sobre o calendário de eventos do município.',
    situacao: 'Apresentada',
    dataApresentacao: '2026-03-10',
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

describe('ProposicaoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista as proposições da situação inicial em tabela', async () => {
    mockFetchOnce(PROPOSICOES);
    renderComAuth(<ProposicaoListPage />, PERMS);

    expect(await screen.findByText('Dispõe sobre o calendário de eventos do município.')).toBeInTheDocument();
    expect(screen.getByText('ProjetoDeLeiOrdinaria')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Proposições na situação Apresentada/i);
  });

  it('mostra o estado vazio quando não há proposições', async () => {
    mockFetchOnce([]);
    renderComAuth(<ProposicaoListPage />, PERMS);

    await waitFor(() =>
      expect(screen.getByText('Nenhuma proposição encontrada')).toBeInTheDocument(),
    );
  });

  it('recarrega ao trocar o filtro de situação', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]); // situação inicial (Apresentada)
    renderComAuth(<ProposicaoListPage />, PERMS);

    await waitFor(() =>
      expect(screen.getByText('Nenhuma proposição encontrada')).toBeInTheDocument(),
    );

    mockFetchOnce(PROPOSICOES); // nova consulta após trocar a situação
    await user.selectOptions(screen.getByLabelText(/Situação/i), '4');

    expect(await screen.findByText('Dispõe sobre o calendário de eventos do município.')).toBeInTheDocument();
  });
});
