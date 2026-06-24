// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// FamiliaListPage — PÁGINA PRINCIPAL (índice) do módulo Assistência Social (SUAS).
// Valida o estado inicial (pré-consulta) e a tabela de famílias por território.
// O fetch é mockado; renderComAuth injeta as permissões necessárias.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { clearAccessToken } from '../../../api/authToken';
import { checarAcessibilidade } from '../../../test/axe';
import { FamiliaListPage } from './FamiliaListPage';
import type { FamiliaResumo } from './familia.api';

const FAMILIAS: FamiliaResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    nisMascarado: '***.*****.**-1',
    unidadeAtendimentoId: '22222222-2222-2222-2222-222222222222',
    territorio: 'Território Centro',
    rendaPerCapita: 218.5,
    situacao: 'Referenciada',
    dataReferenciamento: '2025-02-01',
    dataUltimaAtualizacaoCadastral: '2025-02-01',
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

describe('FamiliaListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações no estado inicial (faça uma consulta)', async () => {
    mockFetch([]);
    const { container } = renderComAuth(<FamiliaListPage />);

    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações com as famílias do território em tabela', async () => {
    const user = userEvent.setup();
    mockFetch(FAMILIAS);
    const { container } = renderComAuth(<FamiliaListPage />);

    await user.type(screen.getByLabelText(/Território de cobertura/i), 'Território Centro');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('***.*****.**-1')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
