// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// ProposicaoListPage — PÁGINA PRINCIPAL (índice) do módulo Legislativo. A tela
// auto-consulta a situação inicial ao montar. Valida a tabela carregada e o estado
// vazio. O fetch é mockado; renderComAuth injeta as permissões necessárias.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderComAuth } from '../../test/renderComAuth';
import { clearAccessToken } from '../../api/authToken';
import { checarAcessibilidade } from '../../test/axe';
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

describe('ProposicaoListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com as proposições em tabela', async () => {
    mockFetchOnce(PROPOSICOES);
    const { container } = renderComAuth(<ProposicaoListPage />, PERMS);

    expect(
      await screen.findByText('Dispõe sobre o calendário de eventos do município.'),
    ).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações no estado vazio', async () => {
    mockFetchOnce([]);
    const { container } = renderComAuth(<ProposicaoListPage />, PERMS);

    await waitFor(() =>
      expect(screen.getByText('Nenhuma proposição encontrada')).toBeInTheDocument(),
    );
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
