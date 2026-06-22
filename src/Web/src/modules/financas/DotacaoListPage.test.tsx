// Teste da lista de Dotações: estado inicial (sem consulta), sucesso (tabela por
// exercício) e vazio. fetch global mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { DotacaoListPage } from './DotacaoListPage';
import type { DotacaoResumo } from './financas.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const DOTACOES: DotacaoResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    exercicio: 2026,
    classificacao: '10.301.0002.2010 — Saúde',
    valorDotadoInicial: 100000,
    valorReforcado: 0,
    valorAnulado: 0,
    valorAtualizado: 100000,
    valorEmpenhadoLiquido: 0,
    saldoDisponivel: 100000,
    situacao: 'Aberta',
  },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('DotacaoListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<DotacaoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra as dotações do exercício em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DOTACOES);
    renderComAuth(<DotacaoListPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('10.301.0002.2010 — Saúde')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Dotações do exercício 2026/i);
  });

  it('mostra o estado vazio quando o exercício não tem dotações', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComAuth(<DotacaoListPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() =>
      expect(screen.getByText('Nenhuma dotação encontrada')).toBeInTheDocument(),
    );
  });
});
