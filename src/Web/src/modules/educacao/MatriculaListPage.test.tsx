// Teste do fluxo de consulta de matrículas por aluno (Vitest + Testing Library).
// Cobre os estados que toda tela de lista sob demanda deve ter: inicial (sem
// consulta), sucesso (tabela) e vazio. O fetch global é mockado para isolar a UI.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { clearAccessToken } from '../../api/authToken';
import { renderEducacao } from './educacao.testUtils';
import { MatriculaListPage } from './MatriculaListPage';
import type { MatriculaResumo } from './api';

const MATRICULAS: MatriculaResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    alunoId: 'aluno-1',
    turmaId: 'turma-abc',
    escolaId: 'escola-xyz',
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

describe('MatriculaListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderEducacao(<MatriculaListPage />);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra as matrículas em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(MATRICULAS);
    renderEducacao(<MatriculaListPage />);

    await user.type(screen.getByLabelText(/Identificador do aluno/i), 'aluno-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('turma-abc')).toBeInTheDocument();
    expect(screen.getByText('Ativa')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Matrículas do aluno aluno-1/i);
  });

  it('mostra o estado vazio quando não há matrículas', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderEducacao(<MatriculaListPage />);

    await user.type(screen.getByLabelText(/Identificador do aluno/i), 'aluno-vazio');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() =>
      expect(screen.getByText('Nenhuma matrícula encontrada')).toBeInTheDocument(),
    );
  });

  it('abre o modal de encerramento ao acionar a ação da linha (com permissão)', async () => {
    const user = userEvent.setup();
    mockFetchOnce(MATRICULAS);
    renderEducacao(<MatriculaListPage />);

    await user.type(screen.getByLabelText(/Identificador do aluno/i), 'aluno-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));
    await screen.findByText('turma-abc');

    await user.click(screen.getByRole('button', { name: 'Encerrar' }));
    expect(await screen.findByText('Encerrar matrícula')).toBeInTheDocument();
    expect(screen.getByLabelText(/Motivo do encerramento/i)).toBeInTheDocument();
  });

  it('esconde as ações de gestão sem a permissão educacao.gerenciar', async () => {
    const user = userEvent.setup();
    mockFetchOnce(MATRICULAS);
    renderEducacao(<MatriculaListPage />, ['educacao.ver']);

    await user.type(screen.getByLabelText(/Identificador do aluno/i), 'aluno-1');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));
    await screen.findByText('turma-abc');

    expect(screen.queryByRole('button', { name: 'Encerrar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Matricular aluno/i })).not.toBeInTheDocument();
  });
});
