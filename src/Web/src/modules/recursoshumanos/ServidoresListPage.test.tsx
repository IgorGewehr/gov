// Teste (Vitest + Testing Library) da página de lista de servidores do RH.
// Cobre os estados que TODA tela de lista deve ter: sucesso (tabela) e vazio.
// O fetch global é mockado para isolar a UI da rede (espelha o teste do PADRÃO-OURO).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { AuthProvider } from '../../auth/AuthProvider';
import { ServidoresListPage } from './ServidoresListPage';
import type { ServidorResumo } from './api';

function renderPage() {
  return renderWithProviders(
    <AuthProvider>
      <ServidoresListPage />
    </AuthProvider>,
  );
}

const SERVIDORES: ServidorResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    cpf: '***.456.789-**',
    matricula: 'MAT-001',
    nomeServidor: 'Maria da Silva',
    cargoId: '22222222-2222-2222-2222-222222222222',
    regime: 'Rpps',
    situacao: 'EmExercicio',
    dataNomeacao: '2024-02-01',
    dataExercicio: '2024-02-15',
  },
];

function mockFetch(body: unknown, status = 200): void {
  // Constrói uma Response nova a cada chamada (o corpo só pode ser lido uma vez).
  vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify(body), {
        status,
        headers: { 'content-type': 'application/json' },
      }),
    ),
  );
}

describe('ServidoresListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista os servidores ativos em tabela', async () => {
    mockFetch(SERVIDORES);
    renderPage();

    expect(await screen.findByText('Maria da Silva')).toBeInTheDocument();
    expect(screen.getByText('MAT-001')).toBeInTheDocument();
    expect(screen.getByText('EmExercicio')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Servidores ativos do órgão/i);
  });

  it('mostra o estado vazio quando não há servidores ativos', async () => {
    mockFetch([]);
    renderPage();

    await waitFor(() =>
      expect(screen.getByText('Nenhum servidor ativo')).toBeInTheDocument(),
    );
  });

  it('exibe o cabeçalho da página', async () => {
    mockFetch([]);
    renderPage();

    expect(screen.getByRole('heading', { name: 'Servidores' })).toBeInTheDocument();
  });
});
