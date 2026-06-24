// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// ProcessoListPage — PÁGINA PRINCIPAL (índice) do módulo Protocolo (processo
// administrativo eletrônico). Valida o estado inicial (pré-consulta) e a tabela de
// processos por setor. O fetch é mockado; renderComAuth injeta as permissões.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { clearAccessToken } from '../../../api/authToken';
import { checarAcessibilidade } from '../../../test/axe';
import { ProcessoListPage } from './ProcessoListPage';
import type { ProcessoResumo } from './processo.api';

const PERMS = ['protocolo.ver', 'protocolo.gerenciar'];

const PROCESSOS: ProcessoResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    nup: '00001.000123/2026-45',
    classificacao: '023.1 — Pessoal/Férias',
    situacao: 'EmTramitacao',
    dataAutuacao: '2026-01-10',
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

describe('ProcessoListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações no estado inicial (sem consulta)', async () => {
    const { container } = renderComAuth(<ProcessoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações com os processos do setor em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(PROCESSOS);
    const { container } = renderComAuth(<ProcessoListPage />, PERMS);

    await user.type(screen.getByLabelText(/Setor responsável/i), 'setor-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('00001.000123/2026-45')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
