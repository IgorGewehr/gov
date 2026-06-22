// Teste da tela de detalhe da Escola (Vitest + Testing Library). Cobre: render do
// detalhe a partir do código INEP da rota e o gating das ações de gestão por
// permissão (educacao.gerenciar). O fetch global é mockado para isolar a UI.
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { clearAccessToken } from '../../api/authToken';
import { renderEducacao } from './educacao.testUtils';
import { EscolaDetailPage } from './EscolaDetailPage';
import type { EscolaResumo } from './api';

const ESCOLA: EscolaResumo = {
  id: '22222222-2222-2222-2222-222222222222',
  codigoInep: '12345678',
  nome: 'EMEF Teste',
  dependencia: 'Municipal',
  situacao: 'Credenciada',
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

function renderDetail(perm?: string[]) {
  return renderEducacao(
    <Routes>
      <Route path="/educacao/escolas/:codigoInep" element={<EscolaDetailPage />} />
    </Routes>,
    perm,
    { route: '/educacao/escolas/12345678' },
  );
}

describe('EscolaDetailPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra os dados da escola e a ação de desativar (com permissão)', async () => {
    mockFetchOnce(ESCOLA);
    renderDetail();

    expect(await screen.findByText('EMEF Teste')).toBeInTheDocument();
    expect(screen.getByText('12345678')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Desativar escola/i })).toBeInTheDocument();
  });

  it('abre o modal de desativação ao acionar a ação', async () => {
    const user = userEvent.setup();
    mockFetchOnce(ESCOLA);
    renderDetail();

    await screen.findByText('EMEF Teste');
    await user.click(screen.getByRole('button', { name: /Desativar escola/i }));
    expect(await screen.findByText(/Ação irreversível/i)).toBeInTheDocument();
  });

  it('esconde as ações de gestão sem a permissão educacao.gerenciar', async () => {
    mockFetchOnce(ESCOLA);
    renderDetail(['educacao.ver']);

    await screen.findByText('EMEF Teste');
    expect(screen.queryByRole('button', { name: /Desativar escola/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Atualizar dados do Censo/i })).not.toBeInTheDocument();
  });
});
