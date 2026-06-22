// Teste (Vitest + Testing Library) da tela de consulta de Licitacao (query
// ListarLicitacoesPorSituacao). Cobre os estados que toda tela de consulta deve
// ter: sucesso (tabela), vazio (nenhuma licitacao) e erro (falha na API). A tela
// auto-consulta na situacao "Aberta" ao montar; o fetch global e mockado para
// isolar da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { LicitacaoListPage } from './LicitacaoListPage';
import type { LicitacaoResumo } from './licitacao.api';

const PERMS = ['administracao.ver', 'administracao.gerenciar'];

function renderWithProviders(ui: Parameters<typeof renderComAuth>[0]) {
  return renderComAuth(ui, PERMS);
}

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

describe('LicitacaoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista as licitacoes da situacao em tabela', async () => {
    mockFetchOnce([LICITACAO]);
    renderWithProviders(<LicitacaoListPage />);

    expect(await screen.findByText('Aquisicao de equipamentos de informatica')).toBeInTheDocument();
    expect(screen.getByText('R$ 200.000,00')).toBeInTheDocument();
    expect(screen.getByText('Pregão')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando nao ha licitacoes', async () => {
    mockFetchOnce([]);
    renderWithProviders(<LicitacaoListPage />);

    expect(await screen.findByText('Nenhuma licitação encontrada')).toBeInTheDocument();
  });

  it('mostra o estado de erro quando a consulta falha', async () => {
    mockFetchOnce({ title: 'Internal Server Error' }, 500);
    renderWithProviders(<LicitacaoListPage />);

    expect(
      await screen.findByText(/Erro ao processar a solicitação|Internal Server Error/i),
    ).toBeInTheDocument();
  });

  it('abre o formulario ao clicar em "Abrir licitação" (acao gated)', async () => {
    const user = userEvent.setup();
    mockFetchOnce([LICITACAO]);
    renderWithProviders(<LicitacaoListPage />);

    await user.click(await screen.findByRole('button', { name: /Abrir licitação/i }));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
  });
});
