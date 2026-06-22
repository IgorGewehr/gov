// Teste (Vitest + Testing Library) da tela de detalhe de Proposicao. Garante o
// render do detalhe (useQuery por id) e que a barra de ACOES (gated por <Can>)
// abre o modal de emenda. O fetch global e mockado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { renderComAuth } from '../../test/renderComAuth';
import { ProposicaoDetailPage } from './ProposicaoDetailPage';
import type { ProposicaoDetalhe } from './api';

const PERMS = ['legislativo.ver', 'legislativo.gerenciar'];
const ID = '11111111-1111-1111-1111-111111111111';

const DETALHE: ProposicaoDetalhe = {
  id: ID,
  tipo: 'ProjetoDeLeiOrdinaria',
  ementa: 'Dispõe sobre o calendário de eventos do município.',
  autoria: 'Vereador Fulano',
  regime: 'Ordinario',
  protocolo: '00001/2026',
  dataApresentacao: '2026-03-10',
  situacao: 'Apresentada',
  numeroAutografo: null,
  tramitacoes: [],
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

function renderDetalhe(perms = PERMS) {
  return renderComAuth(
    <Routes>
      <Route path="/legislativo/proposicoes/:id" element={<ProposicaoDetailPage />} />
    </Routes>,
    perms,
    { route: `/legislativo/proposicoes/${ID}` },
  );
}

describe('ProposicaoDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe o detalhe da proposição', async () => {
    mockFetchOnce(DETALHE);
    renderDetalhe();
    expect(await screen.findByText('Dispõe sobre o calendário de eventos do município.')).toBeInTheDocument();
  });

  it('abre o modal de emenda a partir da barra de ações', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DETALHE);
    renderDetalhe();

    await screen.findByText('Dispõe sobre o calendário de eventos do município.');
    await user.click(screen.getByRole('button', { name: /Apresentar emenda/i }));

    expect(await screen.findByRole('dialog')).toHaveAccessibleName(/Apresentar emenda/i);
  });

  it('oculta a barra de ações sem a permissão de gerenciar', async () => {
    mockFetchOnce(DETALHE);
    renderDetalhe(['legislativo.ver']);

    await screen.findByText('Dispõe sobre o calendário de eventos do município.');
    expect(screen.queryByRole('button', { name: /Apresentar emenda/i })).not.toBeInTheDocument();
  });
});
