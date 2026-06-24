// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// MeusDebitosPage — PÁGINA PRINCIPAL do Portal do Cidadão (realm externo): débitos
// tributários em aberto do próprio cidadão + 2a via de DAM. A página usa o cidadaoApi
// (fetch global) e dispara a consulta no mount. Valida a tabela carregada e o estado
// vazio. O fetch é mockado; basta o QueryClient (sem <Can>/auth do admin).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { MeusDebitosPage } from './MeusDebitosPage';
import type { MeuLancamento } from './cidadaoApi';

const DEBITOS: MeuLancamento[] = [
  {
    lancamentoId: '11111111-1111-1111-1111-111111111111',
    tributo: 'IPTU',
    competencia: '2026-01',
    vencimento: '2026-03-10',
    valorPrincipal: 1234.56,
    situacao: 'EmAberto',
  },
];

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('MeusDebitosPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('não tem violações com os débitos em tabela', async () => {
    mockFetch(DEBITOS);
    const { container } = renderWithProviders(<MeusDebitosPage />);

    expect(await screen.findByText('R$ 1.234,56')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações no estado vazio (sem débitos)', async () => {
    mockFetch([]);
    const { container } = renderWithProviders(<MeusDebitosPage />);

    await waitFor(() =>
      expect(screen.getByText('Voce nao tem debitos em aberto.')).toBeInTheDocument(),
    );
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
