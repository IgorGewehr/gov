// Teste da lista de Liquidações por empenho: estado inicial, sucesso e vazio.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { LiquidacaoListPage } from './LiquidacaoListPage';
import type { LiquidacaoResumo } from './financas.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const LIQUIDACOES: LiquidacaoResumo[] = [
  {
    id: '33333333-3333-3333-3333-333333333333',
    empenhoId: '22222222-2222-2222-2222-222222222222',
    valor: 5000,
    valorPago: 0,
    saldoAPagar: 5000,
    dataLiquidacao: '2026-03-10',
    documento: 'NF 000123',
    situacao: 'Liquidada',
  },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('LiquidacaoListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<LiquidacaoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra as liquidações do empenho em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(LIQUIDACOES);
    renderComAuth(<LiquidacaoListPage />, PERMS);

    await user.type(screen.getByLabelText(/Identificador do empenho/i), 'empenho-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('NF 000123')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando o empenho não tem liquidações', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComAuth(<LiquidacaoListPage />, PERMS);

    await user.type(screen.getByLabelText(/Identificador do empenho/i), 'empenho-vazio');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() => expect(screen.getByText('Nenhuma liquidação encontrada')).toBeInTheDocument());
  });
});
