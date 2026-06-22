// Teste das Demonstrações: estado inicial, consulta exibindo o Balanço Orçamentário com
// totais e o resultado orçamentário, e troca de aba para o Balanço Patrimonial (BP).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { DemonstracoesPage } from './DemonstracoesPage';

const PERMS = ['financas.ver'];

const BO = {
  exercicio: 2026,
  mes: 6,
  receitas: {
    quadro: 'Receitas orçamentárias',
    colunas: ['Realizada'],
    linhas: [{ linha: 'Receitas Correntes', valores: [1000] }],
  },
  despesas: {
    quadro: 'Despesas orçamentárias',
    colunas: ['Empenhada'],
    linhas: [{ linha: 'Despesas Correntes', valores: [800] }],
  },
  totalReceitaRealizada: 1000,
  totalDespesaEmpenhada: 800,
  resultadoOrcamentario: 200,
};

const BP = {
  exercicio: 2026,
  mes: 6,
  ativo: [{ linha: 'Caixa e equivalentes', valores: [500] }],
  passivoPatrimonioLiquido: [{ linha: 'Patrimônio Líquido', valores: [500] }],
  totalAtivo: 500,
  totalPassivoPl: 500,
  ativoFinanceiro: 500,
  passivoFinanceiro: 100,
  superavitFinanceiro: 400,
};

function mockJson(body: unknown) {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });
}

describe('DemonstracoesPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<DemonstracoesPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra o Balanço Orçamentário com totais e resultado', async () => {
    const user = userEvent.setup();
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(mockJson(BO));
    renderComAuth(<DemonstracoesPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    expect(await screen.findByText('Receitas orçamentárias')).toBeInTheDocument();
    expect(screen.getByText('Total da receita realizada')).toBeInTheDocument();
    expect(screen.getByText('Superávit orçamentário')).toBeInTheDocument();
  });

  it('troca para a aba do Balanço Patrimonial e mostra o superávit financeiro', async () => {
    const user = userEvent.setup();
    vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
      const url = String(input);
      if (url.includes('balanco-patrimonial')) return Promise.resolve(mockJson(BP));
      return Promise.resolve(mockJson(BO));
    });
    renderComAuth(<DemonstracoesPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));
    await user.click(screen.getByRole('button', { name: /BP — Balanço Patrimonial/i }));

    expect(await screen.findByText('Superávit financeiro')).toBeInTheDocument();
    expect(screen.getByText('Total do ativo')).toBeInTheDocument();
  });
});
