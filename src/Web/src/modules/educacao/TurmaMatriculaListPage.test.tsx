// Teste da consulta da Matrícula Inicial por turma (Vitest + Testing Library).
// Cobre o estado inicial (sem consulta) e a consulta com resultado em tabela.
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { clearAccessToken } from '../../api/authToken';
import { renderEducacao } from './educacao.testUtils';
import { TurmaMatriculaListPage } from './TurmaMatriculaListPage';
import type { MatriculaResumo } from './api';

const MATRICULAS: MatriculaResumo[] = [
  {
    id: '55555555-5555-5555-5555-555555555555',
    alunoId: 'aluno-x',
    turmaId: 'turma-1',
    escolaId: 'escola-1',
    situacao: 'Ativa',
    dataReferencia: '2026-03-31',
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

describe('TurmaMatriculaListPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderEducacao(<TurmaMatriculaListPage />);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta a turma e mostra a Matrícula Inicial em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(MATRICULAS);
    renderEducacao(<TurmaMatriculaListPage />);

    await user.type(screen.getByLabelText(/Identificador da turma/i), 'turma-1');
    await user.type(screen.getByLabelText(/Data de referência/i), '2026-03-31');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('aluno-x')).toBeInTheDocument();
    expect(screen.getByText('Ativa')).toBeInTheDocument();
  });
});
