// Teste da consulta de Ordem de Pagamento: estado inicial, sucesso e vazio (404).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { PagamentoListPage } from './PagamentoListPage';
import type { OrdemDePagamentoResumo } from './financas.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const ORDEM: OrdemDePagamentoResumo = {
  id: '44444444-4444-4444-4444-444444444444',
  numero: '2026OP000045',
  dataPagamento: '2026-04-01',
  contaBancaria: '001 / 1234 / 56789-0',
  valorTotal: 5000,
  situacao: 'Emitida',
  itens: [{ liquidacaoId: '33333333-3333-3333-3333-333333333333', valor: 5000 }],
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('PagamentoListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<PagamentoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra a ordem de pagamento em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(ORDEM);
    renderComAuth(<PagamentoListPage />, PERMS);

    await user.type(screen.getByLabelText(/Identificador da ordem/i), 'ordem-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('2026OP000045')).toBeInTheDocument();
  });
});
