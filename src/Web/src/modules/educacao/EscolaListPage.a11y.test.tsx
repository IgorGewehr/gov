// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// EscolaListPage — PÁGINA PRINCIPAL (índice) do módulo Educação. Valida a tabela
// da rede escolar carregada. O fetch é mockado; renderEducacao injeta as permissões.
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { clearAccessToken } from '../../api/authToken';
import { checarAcessibilidade } from '../../test/axe';
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

describe('EscolaListPage (acessibilidade)', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com a rede escolar em tabela', async () => {
    mockFetchOnce(ESCOLAS);
    const { container } = renderEducacao(<EscolaListPage />);

    expect(await screen.findByText('EMEF Central')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
