// Teste (Vitest + Testing Library) da lista de cargos com vagas. Cobre sucesso (tabela)
// e uma interação: filtrar por tipo. O fetch global é mockado para isolar da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { AuthProvider } from '../../auth/AuthProvider';
import { CargosListPage } from './CargosListPage';
import type { CargoResumo } from './api';

function renderPage() {
  return renderWithProviders(
    <AuthProvider>
      <CargosListPage />
    </AuthProvider>,
  );
}

const CARGOS: CargoResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    denominacao: 'Auxiliar Administrativo',
    tipo: 'Efetivo',
    vagasDisponiveis: 3,
  },
];

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify(body), {
        status,
        headers: { 'content-type': 'application/json' },
      }),
    ),
  );
}

describe('CargosListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista os cargos com vagas em tabela', async () => {
    mockFetch(CARGOS);
    renderPage();

    expect(await screen.findByText('Auxiliar Administrativo')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Cargos com vagas disponíveis/i);
  });

  it('permite filtrar por tipo de cargo (interação)', async () => {
    const user = userEvent.setup();
    mockFetch(CARGOS);
    renderPage();

    await screen.findByText('Auxiliar Administrativo');
    await user.selectOptions(screen.getByLabelText(/Filtrar por tipo/i), '1');

    await waitFor(() =>
      expect(screen.getByRole('button', { name: /Limpar filtro/i })).toBeInTheDocument(),
    );
  });
});
