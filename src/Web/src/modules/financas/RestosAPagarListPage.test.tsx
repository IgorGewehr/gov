// Teste da lista de Restos a Pagar por exercício de inscrição: inicial, sucesso e vazio.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { RestosAPagarListPage } from './RestosAPagarListPage';
import type { RestoAPagarResumo } from './financas.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const RESTOS: RestoAPagarResumo[] = [
  {
    id: '55555555-5555-5555-5555-555555555555',
    empenhoId: '22222222-2222-2222-2222-222222222222',
    classificacao: '10.301.0002.2010 — Saúde',
    valorInscrito: 5000,
    valorLiquidado: 5000,
    valorPago: 0,
    valorCancelado: 0,
    saldoAPagar: 5000,
    exercicioOrigem: 2025,
    exercicioInscricao: 2026,
    situacao: 'Inscrito',
  },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('RestosAPagarListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<RestosAPagarListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra os restos a pagar inscritos em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(RESTOS);
    renderComAuth(<RestosAPagarListPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    expect(await screen.findByText('10.301.0002.2010 — Saúde')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Restos a pagar inscritos em 2026/i);
  });

  it('mostra o estado vazio quando não há restos inscritos', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComAuth(<RestosAPagarListPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    await waitFor(() =>
      expect(screen.getByText('Nenhum resto a pagar encontrado')).toBeInTheDocument(),
    );
  });
});
