// Teste do fluxo da tela de lista de Declarações Fiscais (SICONFI).
// Cobre os estados de toda tela de lista (inicial/sucesso/vazio) e o gating de UI
// da ação de consolidar (transparencia.gerenciar). O fetch global é mockado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { clearAccessToken } from '../../api/authToken';
import { renderWithAuth } from './test-utils';
import { DeclaracaoFiscalListPage } from './DeclaracaoFiscalListPage';
import type { DeclaracaoFiscalResumo } from './api';

const DECLARACOES: DeclaracaoFiscalResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    tipoDeclaracao: 'Msc',
    exercicio: 2026,
    periodo: '03/2026',
    situacao: 'Consolidada',
    dataLimite: '2026-04-30',
    dataTransmissao: null,
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

describe('DeclaracaoFiscalListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderWithAuth(<DeclaracaoFiscalListPage />);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra as declarações em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DECLARACOES);
    renderWithAuth(<DeclaracaoFiscalListPage />);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('03/2026')).toBeInTheDocument();
    const tabela = screen.getByRole('table');
    expect(within(tabela).getByText('Consolidada')).toBeInTheDocument();
    expect(tabela).toHaveAccessibleName(/Declarações fiscais do exercício/i);
  });

  it('mostra o estado vazio quando não há declarações', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderWithAuth(<DeclaracaoFiscalListPage />);

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() =>
      expect(screen.getByText('Nenhuma declaração encontrada')).toBeInTheDocument(),
    );
  });

  it('esconde a ação de consolidar para quem só tem transparencia.ver', () => {
    renderWithAuth(<DeclaracaoFiscalListPage />, ['transparencia.ver']);
    expect(
      screen.queryByRole('button', { name: /Consolidar declaração/i }),
    ).not.toBeInTheDocument();
  });
});
