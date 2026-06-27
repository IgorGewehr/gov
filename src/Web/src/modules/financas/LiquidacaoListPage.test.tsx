// Teste da lista de Liquidações por empenho: estado inicial, sucesso e vazio.
// O empenho agora é ESCOLHIDO via EmpenhoPicker (escolhe a dotação e depois o
// empenho), não mais digitado como GUID — o mock de fetch responde por URL.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { LiquidacaoListPage } from './LiquidacaoListPage';
import type { LiquidacaoResumo } from './financas.api';
import type { EmpenhoResumo } from './empenho.api';
import type { DotacaoResumo } from './dotacao.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const DOTACAO_ID = '11111111-1111-1111-1111-111111111111';
const EMPENHO_ID = '22222222-2222-2222-2222-222222222222';

const DOTACOES: DotacaoResumo[] = [
  {
    id: DOTACAO_ID,
    exercicio: 2026,
    classificacao: '04.122.0002.2.001',
    valorDotadoInicial: 10000,
    valorReforcado: 0,
    valorAnulado: 0,
    valorAtualizado: 10000,
    valorEmpenhadoLiquido: 5000,
    saldoDisponivel: 5000,
    situacao: 'Ativa',
  },
];

const EMPENHOS: EmpenhoResumo[] = [
  {
    id: EMPENHO_ID,
    numero: '2026NE000123',
    dotacaoId: DOTACAO_ID,
    credorNome: 'Fornecedor Exemplo LTDA',
    credorDocumento: '12.345.678/0001-90',
    valorEmpenhado: 5000,
    valorAnulado: 0,
    valorLiquidado: 0,
    valorPago: 0,
    saldoEmpenhado: 5000,
    saldoALiquidar: 5000,
    saldoAPagar: 0,
    situacao: 'Empenhado',
    exercicio: 2026,
  },
];

const LIQUIDACOES: LiquidacaoResumo[] = [
  {
    id: '33333333-3333-3333-3333-333333333333',
    empenhoId: EMPENHO_ID,
    valor: 5000,
    valorPago: 0,
    saldoAPagar: 5000,
    totalRetido: 0,
    valorLiquido: 5000,
    dataLiquidacao: '2026-03-10',
    documento: 'NF 000123',
    situacao: 'Liquidada',
    retencoes: [],
  },
];

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });
}

/** Mock roteado por URL: dotações, empenhos da dotação e liquidações do empenho. */
function mockRotas(liquidacoes: LiquidacaoResumo[]): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    if (url.includes('/empenhos/') && url.includes('/liquidacoes')) {
      return Promise.resolve(jsonResponse(liquidacoes));
    }
    if (url.includes('/dotacoes/') && url.includes('/empenhos')) {
      return Promise.resolve(jsonResponse(EMPENHOS));
    }
    if (url.includes('/dotacoes')) {
      return Promise.resolve(jsonResponse(DOTACOES));
    }
    return Promise.resolve(jsonResponse([]));
  });
}

async function selecionarEmpenho(user: ReturnType<typeof userEvent.setup>): Promise<void> {
  const selectDotacao = await screen.findByLabelText(/Dotação do empenho/i);
  await waitFor(() =>
    expect(screen.getByRole('option', { name: /04\.122\.0002\.2\.001/ })).toBeInTheDocument(),
  );
  await user.selectOptions(selectDotacao, DOTACAO_ID);

  const opcaoEmpenho = await screen.findByRole('option', { name: /2026NE000123/ });
  const selectEmpenho = opcaoEmpenho.closest('select') as HTMLSelectElement;
  await user.selectOptions(selectEmpenho, EMPENHO_ID);
}

describe('LiquidacaoListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    mockRotas(LIQUIDACOES);
    renderComAuth(<LiquidacaoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra as liquidações do empenho em tabela', async () => {
    const user = userEvent.setup();
    mockRotas(LIQUIDACOES);
    renderComAuth(<LiquidacaoListPage />, PERMS);

    await selecionarEmpenho(user);
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('NF 000123')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando o empenho não tem liquidações', async () => {
    const user = userEvent.setup();
    mockRotas([]);
    renderComAuth(<LiquidacaoListPage />, PERMS);

    await selecionarEmpenho(user);
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() => expect(screen.getByText('Nenhuma liquidação encontrada')).toBeInTheDocument());
  });
});
