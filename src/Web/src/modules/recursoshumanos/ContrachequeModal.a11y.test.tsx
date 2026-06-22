// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) do
// ContrachequeModal / demonstrativo de pagamento. Valida o diálogo (role=dialog,
// foco, label) no estado inicial e com o contracheque carregado (DataTable +
// totais). O fetch é mockado; o axe-core valida cada estado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { ContrachequeModal } from './ContrachequeModal';
import type { Contracheque } from './folha.api';
import type { ServidorResumo } from './servidor.api';

const SERVIDORES: ServidorResumo[] = [
  {
    id: 's-1',
    cpf: '***.456.789-**',
    matricula: 'MAT-001',
    nomeServidor: 'Maria da Silva',
    cargoId: 'c-1',
    regime: 'Rpps',
    situacao: 'EmExercicio',
    dataNomeacao: '2024-02-01',
    dataExercicio: '2024-02-15',
  },
];

const CONTRACHEQUE: Contracheque = {
  servidorId: 's-1',
  competencia: '2026-06',
  linhas: [
    { rubrica: 'Vencimento', tipo: 'Provento', valor: 5000 },
    { rubrica: 'INSS', tipo: 'Desconto', valor: 550 },
  ],
  totalProventos: 5000,
  totalDescontos: 550,
  liquidoAPagar: 4450,
};

function mockFetch(resolver: (url: string) => unknown): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    return Promise.resolve(
      new Response(JSON.stringify(resolver(url)), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );
  });
}

describe('ContrachequeModal (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('não tem violações no estado inicial (seleção de servidor)', async () => {
    mockFetch(() => SERVIDORES);
    renderWithProviders(<ContrachequeModal open onClose={() => {}} folhaId="f-1" />);
    await screen.findByRole('option', { name: /MAT-001 — Maria da Silva/i });
    expect(await checarAcessibilidade(document.body)).toHaveNoViolations();
  });

  it('não tem violações com o contracheque carregado', async () => {
    const user = userEvent.setup();
    mockFetch((url) => (url.includes('contracheque') ? CONTRACHEQUE : SERVIDORES));
    renderWithProviders(<ContrachequeModal open onClose={() => {}} folhaId="f-1" />);

    await screen.findByRole('option', { name: /MAT-001 — Maria da Silva/i });
    await user.selectOptions(screen.getByRole('combobox', { name: 'Servidor' }), 's-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('Vencimento')).toBeInTheDocument();
    expect(await checarAcessibilidade(document.body)).toHaveNoViolations();
  });
});
