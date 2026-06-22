// Teste (Vitest + Testing Library) da consulta de rubricas vigentes. Cobre o fluxo
// sob demanda (consultar -> tabela) e a apresentação das bases de incidência. O fetch
// global é mockado para isolar da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { AuthProvider } from '../../auth/AuthProvider';
import { RubricasListPage } from './RubricasListPage';
import type { RubricaResumo } from './api';

function renderPage() {
  return renderWithProviders(
    <AuthProvider>
      <RubricasListPage />
    </AuthProvider>,
  );
}

const RUBRICAS: RubricaResumo[] = [
  {
    id: '33333333-3333-3333-3333-333333333333',
    codigo: '1010',
    descricao: 'Vencimento Base',
    natureza: 'Provento',
    incideInss: true,
    incideRpps: false,
    incideIrrf: true,
    incideFgts: false,
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

describe('RubricasListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('consulta as rubricas vigentes sob demanda e exibe a tabela', async () => {
    const user = userEvent.setup();
    mockFetch(RUBRICAS);
    renderPage();

    // Antes de consultar, a página orienta a seleção da competência.
    expect(screen.getByText(/Selecione uma competência/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('Vencimento Base')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Rubricas vigentes/i);
    // Bases de incidência consolidadas (INSS, IRRF marcadas).
    expect(screen.getByText('INSS, IRRF')).toBeInTheDocument();
  });
});
