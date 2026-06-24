// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// DotacaoListPage — PÁGINA PRINCIPAL (índice) do módulo Finanças (ciclo da despesa).
// Valida o estado inicial (pré-consulta) e a tabela do exercício após consultar.
// O fetch é mockado; renderComAuth injeta um JWT com as permissões necessárias.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { clearAccessToken } from '../../api/authToken';
import { checarAcessibilidade } from '../../test/axe';
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

describe('DotacaoListPage (acessibilidade)', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações no estado inicial (sem consulta)', async () => {
    const { container } = renderComAuth(<DotacaoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações com as dotações do exercício em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DOTACOES);
    const { container } = renderComAuth(<DotacaoListPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('10.301.0002.2010 — Saúde')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
