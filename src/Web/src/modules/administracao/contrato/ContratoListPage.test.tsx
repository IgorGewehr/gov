// Teste (Vitest + Testing Library) da tela de consulta de Contrato. Cobre os
// estados que toda tela de consulta deve ter: inicial (sem consulta), sucesso
// (tabela de contratos vigentes) e erro (falha na API). O fetch global e mockado
// para isolar da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { ContratoListPage } from './ContratoListPage';
import type { ContratoResumo } from './contrato.api';

const PERMS = ['administracao.ver', 'administracao.gerenciar'];

function renderWithProviders(ui: Parameters<typeof renderComAuth>[0]) {
  return renderComAuth(ui, PERMS);
}

const CONTRATO: ContratoResumo = {
  id: '22222222-2222-2222-2222-222222222222',
  fornecedorId: '33333333-3333-3333-3333-333333333333',
  objeto: 'Manutencao predial continuada',
  valorAtual: 84000.75,
  vigenciaFim: '2026-12-31',
  situacao: 'EmExecucao',
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('ContratoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderWithProviders(<ContratoListPage />);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta os contratos vigentes e mostra o contrato em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce([CONTRATO]);
    renderWithProviders(<ContratoListPage />);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('Manutencao predial continuada')).toBeInTheDocument();
    expect(screen.getByText('R$ 84.000,75')).toBeInTheDocument();
    expect(screen.getByText('Em execução')).toBeInTheDocument();
  });

  it('mostra o estado de erro quando a consulta falha', async () => {
    const user = userEvent.setup();
    mockFetchOnce({ title: 'Internal Server Error' }, 500);
    renderWithProviders(<ContratoListPage />);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() =>
      expect(
        screen.getByText(/Erro ao processar a solicitação|Internal Server Error/i),
      ).toBeInTheDocument(),
    );
  });

  it('abre o formulario ao clicar em "Celebrar contrato" (acao gated)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ContratoListPage />);

    await user.click(screen.getByRole('button', { name: /Celebrar contrato/i }));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
  });
});
