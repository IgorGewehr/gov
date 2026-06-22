// Teste da MSC: nota de que a transmissão é etapa à parte, gating do botão "Gerar MSC"
// (financas.gerenciar), geração com quantidadeLinhas e caso idempotente (jaExistia).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { MscPage } from './MscPage';

const PERMS = ['financas.ver', 'financas.gerenciar'];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('MscPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('avisa que a transmissão ao SICONFI/TCE é etapa à parte', () => {
    renderComAuth(<MscPage />, PERMS);
    expect(screen.getByText(/transmissão é uma etapa à parte/i)).toBeInTheDocument();
    expect(screen.getAllByText(/SICONFI\/STN/).length).toBeGreaterThan(0);
  });

  it('gera a MSC e exibe a quantidade de linhas', async () => {
    const user = userEvent.setup();
    mockFetchOnce({
      eventId: '11111111-1111-1111-1111-111111111111',
      quantidadeLinhas: 320,
      jaExistia: false,
    });
    renderComAuth(<MscPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Gerar MSC/i }));

    expect(await screen.findByText('MSC gerada')).toBeInTheDocument();
    expect(screen.getByText('320')).toBeInTheDocument();
    expect(
      screen.getByText('11111111-1111-1111-1111-111111111111'),
    ).toBeInTheDocument();
  });

  it('sinaliza idempotência quando a MSC já existia', async () => {
    const user = userEvent.setup();
    mockFetchOnce({
      eventId: '22222222-2222-2222-2222-222222222222',
      quantidadeLinhas: 320,
      jaExistia: true,
    });
    renderComAuth(<MscPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Gerar MSC/i }));

    await waitFor(() =>
      expect(screen.getByText('MSC já existente (idempotência)')).toBeInTheDocument(),
    );
  });

  it('oculta o formulário de geração sem a permissão financas.gerenciar', () => {
    renderComAuth(<MscPage />, ['financas.ver']);
    expect(screen.queryByRole('button', { name: /Gerar MSC/i })).not.toBeInTheDocument();
    expect(screen.getByText(/não possui permissão para gerar a MSC/i)).toBeInTheDocument();
  });
});
