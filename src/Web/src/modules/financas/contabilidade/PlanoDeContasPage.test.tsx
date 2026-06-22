// Teste do Plano de Contas: carregamento da árvore, link de razão para conta analítica
// e ação "Semear plano de contas" (gated por financas.gerenciar).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { PlanoDeContasPage } from './PlanoDeContasPage';
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

describe('PlanoDeContasPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('lista as contas e cria link de razão para conta analítica', async () => {
    mockFetchOnce(CONTAS);
    renderComAuth(<PlanoDeContasPage />, PERMS);

    expect(await screen.findByText('ATIVO')).toBeInTheDocument();
    const link = screen.getByRole('link', { name: /Razão da conta 1\.1\.1\.1\.1\.01\.00/i });
    expect(link).toHaveAttribute(
      'href',
      '/financas/contabilidade/contas/22222222-2222-2222-2222-222222222222/razao',
    );
  });

  it('mostra o estado vazio quando o plano não foi semeado', async () => {
    mockFetchOnce([]);
    renderComAuth(<PlanoDeContasPage />, PERMS);
    expect(await screen.findByText('Plano de contas vazio')).toBeInTheDocument();
  });

  it('semeia o plano de contas e avisa quantas contas foram criadas', async () => {
    const user = userEvent.setup();
    // Carga inicial vazia; depois o semear retorna {criadas}; o refetch posterior
    // devolve a árvore preenchida (resposta padrão para chamadas seguintes).
    const fetchSpy = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(
        new Response('[]', { status: 200, headers: { 'content-type': 'application/json' } }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ criadas: 142 }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
      )
      .mockResolvedValue(
        new Response(JSON.stringify(CONTAS), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
      );
    renderComAuth(<PlanoDeContasPage />, PERMS);
    await screen.findByText('Plano de contas vazio');

    await user.click(screen.getByRole('button', { name: /Semear plano de contas/i }));

    await waitFor(() => expect(screen.getByText(/142 conta\(s\) criada\(s\)/i)).toBeInTheDocument());
    expect(fetchSpy).toHaveBeenCalled();
  });

  it('oculta o botão de semear sem a permissão financas.gerenciar', async () => {
    mockFetchOnce(CONTAS);
    renderComAuth(<PlanoDeContasPage />, ['financas.ver']);
    await screen.findByText('ATIVO');
    expect(screen.queryByRole('button', { name: /Semear plano de contas/i })).not.toBeInTheDocument();
  });
});
