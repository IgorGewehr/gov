// Teste da tela de lista do agregado Processo (módulo Protocolo), seguindo o
// PADRÃO-OURO de tributos/DividaAtivaListPage.test.tsx. Cobre os estados que TODA
// tela de lista deve ter: inicial (sem consulta), sucesso (tabela por setor) e vazio.
// O fetch global é mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { ProcessoListPage } from './ProcessoListPage';
import type { ProcessoResumo } from './processo.api';

const PROCESSOS: ProcessoResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    nup: '00001.000123/2026-45',
    classificacao: '023.1 — Pessoal/Férias',
    situacao: 'EmTramitacao',
    dataAutuacao: '2026-01-10',
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

describe('ProcessoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<ProcessoListPage />, ['protocolo.ver', 'protocolo.gerenciar']);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra os processos do setor em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(PROCESSOS);
    renderComAuth(<ProcessoListPage />, ['protocolo.ver', 'protocolo.gerenciar']);

    await user.type(screen.getByLabelText(/Setor responsável/i), 'setor-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('00001.000123/2026-45')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Processos do setor setor-1/i);
    // "Em tramitação" aparece também na opção do filtro <Select>; restringe-se à
    // célula da tabela (a Tag de situação) via o cabeçalho de coluna correspondente.
    expect(
      screen.getByRole('cell', { name: 'Em tramitação' }),
    ).toBeInTheDocument();
  });

  it('mostra o estado vazio quando o setor não tem processos', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComAuth(<ProcessoListPage />, ['protocolo.ver', 'protocolo.gerenciar']);

    await user.type(screen.getByLabelText(/Setor responsável/i), 'setor-vazio');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() =>
      expect(screen.getByText('Nenhum processo encontrado')).toBeInTheDocument(),
    );
  });
});
