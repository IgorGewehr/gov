// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) do
// PainelPlacar — destaque do PoC legislativo. Renderiza o placar nominal (region +
// aria-live) com votos e roda o axe-core (toHaveNoViolations).
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { checarAcessibilidade } from '../../test/axe';
import { PainelPlacar } from './PainelPlacar';
import type { PainelVotacao } from './votacao.api';

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
    { vereadorId: 'p-3', nomeParlamentar: 'Carla Dias', sentido: 'Abstenção' },
  ],
};

describe('PainelPlacar (acessibilidade)', () => {
  it('não tem violações de acessibilidade com votação aberta', async () => {
    const { container } = render(<PainelPlacar painel={PAINEL} atualizando />);
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações de acessibilidade sem votos registrados', async () => {
    const { container } = render(
      <PainelPlacar
        painel={{ ...PAINEL, situacao: 'Encerrada', votos: [] }}
        atualizando={false}
      />,
    );
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
