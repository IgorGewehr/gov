// Teste da lista de Empenhos por dotação: estado inicial, sucesso (tabela) e vazio.
// A dotação agora é ESCOLHIDA via DotacaoPicker (GET /financas/dotacoes?exercicio=),
// não mais digitada como GUID — o mock de fetch responde por URL.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../test/renderComAuth';
import { EmpenhoListPage } from './EmpenhoListPage';
import type { EmpenhoResumo } from './financas.api';
import type { DotacaoResumo } from './dotacao.api';

const PERMS = ['financas.ver', 'financas.gerenciar'];

const DOTACAO_ID = '11111111-1111-1111-1111-111111111111';

const DOTACOES: DotacaoResumo[] = [
  {
    id: DOTACAO_ID,
    exercicio: 2026,
    classificacao: '04.122.0002.2.001',
    valorDotadoInicial: 10000,
    valorReforcado: 0,
    valorAnulado: 0,
    valorAtualizado: 10000,
    valorEmpenhadoLiquido: 0,
    saldoDisponivel: 10000,
    situacao: 'Ativa',
  },
];

const EMPENHOS: EmpenhoResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
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

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });
}

/** Mock roteado por URL: dotações sempre; empenhos da dotação com o corpo informado. */
function mockRotas(empenhos: EmpenhoResumo[]): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    if (url.includes('/dotacoes/') && url.includes('/empenhos')) {
      return Promise.resolve(jsonResponse(empenhos));
    }
    if (url.includes('/dotacoes')) {
      return Promise.resolve(jsonResponse(DOTACOES));
    }
    return Promise.resolve(jsonResponse([]));
  });
}

async function selecionarDotacao(user: ReturnType<typeof userEvent.setup>): Promise<void> {
  const select = await screen.findByLabelText(/Dotação orçamentária/i);
  await waitFor(() =>
    expect(screen.getByRole('option', { name: /04\.122\.0002\.2\.001/ })).toBeInTheDocument(),
  );
  await user.selectOptions(select, DOTACAO_ID);
}

describe('EmpenhoListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    mockRotas(EMPENHOS);
    renderComAuth(<EmpenhoListPage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra os empenhos da dotação em tabela', async () => {
    const user = userEvent.setup();
    mockRotas(EMPENHOS);
    renderComAuth(<EmpenhoListPage />, PERMS);

    await selecionarDotacao(user);
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(await screen.findByText('2026NE000123')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Fornecedor Exemplo LTDA' })).toBeInTheDocument();
  });

  it('mostra o estado vazio quando a dotação não tem empenhos', async () => {
    const user = userEvent.setup();
    mockRotas([]);
    renderComAuth(<EmpenhoListPage />, PERMS);

    await selecionarDotacao(user);
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() => expect(screen.getByText('Nenhum empenho encontrado')).toBeInTheDocument());
  });
});
