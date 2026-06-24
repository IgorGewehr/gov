// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// LicitacaoListPage — PÁGINA PRINCIPAL (índice) do módulo Administração (Compras e
// Licitações, Lei 14.133/2021). A tela auto-consulta a situação "Aberta" ao montar.
// O fetch é mockado; renderComAuth injeta as permissões necessárias.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderComAuth } from '../../../test/renderComAuth';
import { clearAccessToken } from '../../../api/authToken';
import { checarAcessibilidade } from '../../../test/axe';
import { LicitacaoListPage } from './LicitacaoListPage';
import type { LicitacaoResumo } from './licitacao.api';

const PERMS = ['administracao.ver', 'administracao.gerenciar'];

const LICITACAO: LicitacaoResumo = {
  id: '11111111-1111-1111-1111-111111111111',
  objeto: 'Aquisicao de equipamentos de informatica',
  modalidade: 'Pregao',
  situacao: 'Aberta',
  valorEstimado: 200000,
  numeroEditalPncp: null,
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('LicitacaoListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com as licitações em tabela', async () => {
    mockFetchOnce([LICITACAO]);
    const { container } = renderComAuth(<LicitacaoListPage />, PERMS);

    expect(await screen.findByText('Aquisicao de equipamentos de informatica')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações no estado vazio', async () => {
    mockFetchOnce([]);
    const { container } = renderComAuth(<LicitacaoListPage />, PERMS);

    expect(await screen.findByText('Nenhuma licitação encontrada')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
