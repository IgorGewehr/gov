// Teste da lista de Empenhos por dotação: estado inicial, sucesso (tabela) e vazio.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { EmpenhoListPage } from './EmpenhoListPage';
import type { EmpenhoResumo } from './financas.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const EMPENHOS: EmpenhoResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    numero: '2026NE000123',
    dotacaoId: '11111111-1111-1111-1111-111111111111',
    credorNome: 'Fornecedor Exemplo LTDA',
    credorDocumento: '12.345.678/0001-90',
    valorEmpenhado: 5000,
    valorAnulado: 0,
    valorLiquidado: 0,
    valorPago: 0,
    saldoEmpenhado: 5000,
    saldoALiquidar: 5000,
    saldoAPagar: 0,
    situacao: 'Empenhado',
    exercicio: 2026,
  },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('EmpenhoListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<EmpenhoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra os empenhos da dotação em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(EMPENHOS);
    renderComAuth(<EmpenhoListPage />, PERMS);

    await user.type(screen.getByLabelText(/Identificador da dotação/i), 'dotacao-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('2026NE000123')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Fornecedor Exemplo LTDA' })).toBeInTheDocument();
  });

  it('mostra o estado vazio quando a dotação não tem empenhos', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComAuth(<EmpenhoListPage />, PERMS);

    await user.type(screen.getByLabelText(/Identificador da dotação/i), 'dotacao-vazia');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() => expect(screen.getByText('Nenhum empenho encontrado')).toBeInTheDocument());
  });
});
