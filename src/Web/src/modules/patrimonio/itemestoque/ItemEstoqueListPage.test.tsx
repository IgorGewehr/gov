// Teste (Vitest + Testing Library) da tela de lista do Almoxarifado (ItemEstoque),
// no PADRÃO-OURO de src/modules/tributos. Três consultas disparam na montagem:
// a LISTA NAVEGÁVEL (Onda 0, GET /estoque/itens), ListarItensAbaixoDoPontoPedido
// (§6.2) e ObterPosicaoCurvaAbc (§6.4). Mockamos o fetch global ROTEADO POR URL para
// isolar a UI da rede e tolerar a ordem das chamadas concorrentes.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { ItemEstoqueListPage } from './ItemEstoqueListPage';
import type {
  ItemEstoqueItemLista,
  ItemReposicaoResumo,
  PosicaoAbcResumo,
} from './itemestoque.api';
import type { ResultadoPaginado } from '../shared/paginacaoTipos';

function toBase64Url(obj: unknown): string {
  const bytes = new TextEncoder().encode(JSON.stringify(obj));
  let binary = '';
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeToken(perm: string[]): string {
  const header = toBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = toBase64Url({ sub: 'u-1', name: 'Servidor', tenant_id: 't-1', perm });
  return `${header}.${payload}.`;
}

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <ItemEstoqueListPage />
    </AuthProvider>,
  );
}

const LISTA: ResultadoPaginado<ItemEstoqueItemLista> = {
  itens: [
    {
      id: '99999999-9999-9999-9999-999999999999',
      codigo: 'ALM-777',
      descricao: 'Caneta esferográfica azul',
      unidadeMedida: 'UN',
      saldo: 320,
      custoMedio: 1.25,
      classificacaoAbc: 'C',
      situacao: 'Ativo',
    },
  ],
  total: 1,
  pagina: 1,
  tamanho: 20,
};

const REPOSICAO: ItemReposicaoResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    codigo: 'ALM-001',
    descricao: 'Resma de papel A4',
    saldo: 2,
    pontoPedido: 10,
  },
];

const CURVA_ABC: PosicaoAbcResumo[] = [{ classe: 'A', quantidadeItens: 5, valorTotal: 1234.56 }];

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

/** Mock de fetch roteado por URL (resiliente à ordem das três consultas do mount). */
function mockFetchPorUrl(opcoes: {
  lista?: unknown;
  reposicao?: unknown;
  curvaAbc?: unknown;
}): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url ?? String(input);
    if (url.includes('/estoque/reposicao')) return Promise.resolve(jsonResponse(opcoes.reposicao ?? []));
    if (url.includes('/estoque/curva-abc')) return Promise.resolve(jsonResponse(opcoes.curvaAbc ?? []));
    if (url.includes('/estoque/itens'))
      return Promise.resolve(jsonResponse(opcoes.lista ?? { itens: [], total: 0, pagina: 1, tamanho: 20 }));
    return Promise.resolve(jsonResponse([]));
  });
}

describe('ItemEstoqueListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista os itens navegáveis, os itens em reposição e a posição da curva ABC', async () => {
    mockFetchPorUrl({ lista: LISTA, reposicao: REPOSICAO, curvaAbc: CURVA_ABC });

    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('ALM-777')).toBeInTheDocument();
    expect(await screen.findByText('ALM-001')).toBeInTheDocument();
    expect(screen.getByText('Resma de papel A4')).toBeInTheDocument();
    expect(await screen.findByText('R$ 1.234,56')).toBeInTheDocument();
  });

  it('mostra os estados vazios quando não há itens', async () => {
    mockFetchPorUrl({
      lista: { itens: [], total: 0, pagina: 1, tamanho: 20 },
      reposicao: [],
      curvaAbc: [],
    });

    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('Nenhum item encontrado')).toBeInTheDocument();
    expect(await screen.findByText('Nenhum item para repor')).toBeInTheDocument();
    expect(await screen.findByText('Sem itens classificados')).toBeInTheDocument();
  });

  it('exibe a ação de cadastro de item com a permissão patrimonio.gerenciar', () => {
    mockFetchPorUrl({});

    renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);

    expect(screen.getByRole('button', { name: /Cadastrar item/i })).toBeInTheDocument();
  });

  it('oculta a ação de cadastro de item sem a permissão patrimonio.gerenciar', () => {
    mockFetchPorUrl({});

    renderComPermissao(['patrimonio.ver']);

    expect(screen.queryByRole('button', { name: /Cadastrar item/i })).not.toBeInTheDocument();
  });
});
