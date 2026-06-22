// Teste (Vitest + Testing Library) da tela de lista do Almoxarifado (ItemEstoque),
// no PADRÃO-OURO de src/modules/tributos. As duas consultas de "entrada"
// (ListarItensAbaixoDoPontoPedido §6.2 e ObterPosicaoCurvaAbc §6.4) disparam na
// montagem, então mockamos o fetch global para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { ItemEstoqueListPage } from './ItemEstoqueListPage';
import type { ItemReposicaoResumo, PosicaoAbcResumo } from './itemestoque.api';

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

const REPOSICAO: ItemReposicaoResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    codigo: 'ALM-001',
    descricao: 'Resma de papel A4',
    saldo: 2,
    pontoPedido: 10,
  },
];

const CURVA_ABC: PosicaoAbcResumo[] = [
  { classe: 'A', quantidadeItens: 5, valorTotal: 1234.56 },
];

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
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

  it('lista os itens em reposição e a posição da curva ABC', async () => {
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse(REPOSICAO))
      .mockResolvedValueOnce(jsonResponse(CURVA_ABC));

    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('ALM-001')).toBeInTheDocument();
    expect(screen.getByText('Resma de papel A4')).toBeInTheDocument();
    expect(await screen.findByText('R$ 1.234,56')).toBeInTheDocument();
  });

  it('mostra os estados vazios quando não há itens', async () => {
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]));

    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('Nenhum item para repor')).toBeInTheDocument();
    expect(await screen.findByText('Sem itens classificados')).toBeInTheDocument();
  });

  it('exibe a ação de cadastro de item com a permissão patrimonio.gerenciar', () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse([]));

    renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);

    expect(screen.getByRole('button', { name: /Cadastrar item/i })).toBeInTheDocument();
  });

  it('oculta a ação de cadastro de item sem a permissão patrimonio.gerenciar', () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse([]));

    renderComPermissao(['patrimonio.ver']);

    expect(screen.queryByRole('button', { name: /Cadastrar item/i })).not.toBeInTheDocument();
  });
});
