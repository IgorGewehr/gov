// Teste da tela de Lançamentos: lista as contas analíticas com link p/ razão e abre o
// modal de lançamento manual (gated por financas.gerenciar).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { LancamentosPage } from './LancamentosPage';
import type { ContaContabil } from './contabilidade.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const CONTAS: ContaContabil[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    codigo: '1',
    titulo: 'ATIVO',
    naturezaInformacao: 'Patrimonial',
    naturezaSaldo: 'Devedora',
    tipo: 'Sintetica',
    nivel: 1,
    ativa: true,
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    codigo: '1.1.1.1.1.01.00',
    titulo: 'Caixa',
    naturezaInformacao: 'Patrimonial',
    naturezaSaldo: 'Devedora',
    tipo: 'Analitica',
    nivel: 7,
    ativa: true,
  },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('LancamentosPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('lista apenas contas analíticas com link para o razão', async () => {
    mockFetchOnce(CONTAS);
    renderComAuth(<LancamentosPage />, PERMS);

    const link = await screen.findByRole('link', { name: /Ver razão/i });
    expect(link).toHaveAttribute(
      'href',
      '/financas/contabilidade/contas/22222222-2222-2222-2222-222222222222/razao',
    );
    // A conta sintética "ATIVO" não é lançável e não aparece na lista.
    expect(screen.queryByText('ATIVO')).not.toBeInTheDocument();
  });

  it('abre o modal de novo lançamento manual', async () => {
    const user = userEvent.setup();
    mockFetchOnce(CONTAS);
    renderComAuth(<LancamentosPage />, PERMS);
    await screen.findByRole('link', { name: /Ver razão/i });

    await user.click(screen.getByRole('button', { name: /Novo lançamento manual/i }));
    expect(await screen.findByRole('button', { name: /Adicionar partida/i })).toBeInTheDocument();
    expect(screen.getByText('Partidas')).toBeInTheDocument();
  });

  it('oculta a ação de novo lançamento sem a permissão financas.gerenciar', async () => {
    mockFetchOnce(CONTAS);
    renderComAuth(<LancamentosPage />, ['financas.ver']);
    await screen.findByRole('link', { name: /Ver razão/i });
    expect(screen.queryByRole('button', { name: /Novo lançamento manual/i })).not.toBeInTheDocument();
  });
});
