// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// PainelAoVivoPage — fluxo: estado inicial, lista de votações (DataTable) e o
// placar carregado. O fetch é mockado para isolar da rede e o axe-core valida
// cada estado (toHaveNoViolations).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { PainelAoVivoPage } from './PainelAoVivoPage';
import type { PainelVotacao, VotacaoResumo } from './votacao.api';

const VOTACOES: VotacaoResumo[] = [
  { id: 'v-1', proposicaoId: 'p-1', tipo: 'Nominal', situacao: 'Aberta', resultado: null },
  { id: 'v-2', proposicaoId: 'p-2', tipo: 'Nominal', situacao: 'Encerrada', resultado: 'Aprovada' },
];

const PAINEL: PainelVotacao = {
  votacaoId: 'v-1',
  sim: 6,
  nao: 3,
  abstencao: 1,
  totalVotos: 10,
  presentes: 10,
  ausentes: 1,
  quorumMinimo: 6,
  quorumAtingido: true,
  resultadoParcial: 'Aprovada',
  situacao: 'Aberta',
  votos: [
    { vereadorId: 'p-1', nomeParlamentar: 'Ana Souza', sentido: 'Sim' },
    { vereadorId: 'p-2', nomeParlamentar: 'Bruno Lima', sentido: 'Não' },
  ],
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

describe('PainelAoVivoPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('não tem violações no estado inicial (sem sessão)', async () => {
    mockFetch(() => VOTACOES);
    const { container } = renderWithProviders(<PainelAoVivoPage />);
    expect(await screen.findByText('Informe uma sessão')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações com a lista de votações e o placar carregado', async () => {
    const user = userEvent.setup();
    mockFetch((url) => (url.includes('/painel') ? PAINEL : VOTACOES));
    const { container } = renderWithProviders(<PainelAoVivoPage />);

    await user.type(screen.getByLabelText(/Identificador da sessão/i), 'sessao-1');
    await user.click(screen.getByRole('button', { name: /Carregar votações/i }));

    const abrir = await screen.findAllByRole('button', { name: /Abrir painel/i });
    await user.click(abrir[0]);

    expect(await screen.findByText('Placar da votação')).toBeInTheDocument();
    expect(await screen.findByText('Ana Souza')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
