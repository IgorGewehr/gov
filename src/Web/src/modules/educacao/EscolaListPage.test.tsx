// Teste da lista de escolas da rede (Vitest + Testing Library). Cobre o render da
// tabela e o gating do botão de credenciamento por permissão (educacao.gerenciar).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { clearAccessToken } from '../../api/authToken';
import { renderEducacao } from './educacao.testUtils';
import { EscolaListPage } from './EscolaListPage';
import type { EscolaResumo } from './api';

const ESCOLAS: EscolaResumo[] = [
  {
    id: '66666666-6666-6666-6666-666666666666',
    codigoInep: '87654321',
    nome: 'EMEF Central',
    dependencia: 'Municipal',
    situacao: 'Credenciada',
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

describe('EscolaListPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra as escolas e o botão de credenciar (com permissão)', async () => {
    mockFetchOnce(ESCOLAS);
    renderEducacao(<EscolaListPage />);

    expect(await screen.findByText('EMEF Central')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Credenciar escola/i })).toBeInTheDocument();
  });

  it('esconde o botão de credenciar sem a permissão educacao.gerenciar', async () => {
    mockFetchOnce(ESCOLAS);
    renderEducacao(<EscolaListPage />, ['educacao.ver']);

    expect(await screen.findByText('EMEF Central')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Credenciar escola/i })).not.toBeInTheDocument();
  });
});
