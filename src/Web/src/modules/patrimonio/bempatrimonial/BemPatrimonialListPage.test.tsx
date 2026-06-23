// Teste (Vitest + Testing Library) da tela de lista de Bens Patrimoniais, no
// PADRÃO-OURO de src/modules/tributos. Cobre a LISTA NAVEGÁVEL (Onda 0, busca por
// descrição/tombamento + filtros, GET /patrimonio/bens, dispara no mount) e a lista
// fiscal de depreciáveis por competência (dispara ao clicar em Consultar).
// O fetch global é mockado ROTEADO POR URL para isolar a UI da rede e tolerar a
// consulta de lista que roda na montagem.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { BemPatrimonialListPage } from './BemPatrimonialListPage';
import type { BemDepreciavelResumo, BemPatrimonialItemLista } from './bempatrimonial.api';
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
      <BemPatrimonialListPage />
    </AuthProvider>,
  );
}

const LISTA: ResultadoPaginado<BemPatrimonialItemLista> = {
  itens: [
    {
      id: '44444444-4444-4444-4444-444444444444',
      numeroTombamento: 'TOMBO-2026-0042',
      descricao: 'Notebook de gabinete',
      tipo: 'Movel',
      valorContabil: 4200,
      dataIncorporacao: '2026-01-15',
      situacao: 'Tombado',
    },
  ],
  total: 1,
  pagina: 1,
  tamanho: 20,
};

const DEPRECIAVEIS: BemDepreciavelResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    numeroTombamento: 'TOMBO-2026-0001',
    valorContabil: 1234.56,
    valorResidual: 100,
    parcelaMensal: 50,
  },
];

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

/**
 * Mock de fetch roteado por URL: a lista navegável (GET /patrimonio/bens) dispara no
 * mount; os depreciáveis (/patrimonio/bens/depreciaveis) ao clicar em Consultar.
 */
function mockFetchPorUrl(opcoes: { lista?: unknown; depreciaveis?: unknown }): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url ?? String(input);
    if (url.includes('/bens/depreciaveis')) return Promise.resolve(jsonResponse(opcoes.depreciaveis ?? []));
    if (url.includes('/patrimonio/bens'))
      return Promise.resolve(jsonResponse(opcoes.lista ?? { itens: [], total: 0, pagina: 1, tamanho: 20 }));
    return Promise.resolve(jsonResponse([]));
  });
}

describe('BemPatrimonialListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista os bens navegáveis na montagem (busca)', async () => {
    mockFetchPorUrl({ lista: LISTA });
    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('Notebook de gabinete')).toBeInTheDocument();
    expect(screen.getByText('TOMBO-2026-0042')).toBeInTheDocument();
  });

  it('mostra o estado vazio da busca quando não há bens', async () => {
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('Nenhum bem encontrado')).toBeInTheDocument();
  });

  it('exibe o estado inicial pedindo uma competência (depreciáveis)', async () => {
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    renderComPermissao(['patrimonio.ver']);
    expect(await screen.findByText('Selecione uma competência')).toBeInTheDocument();
  });

  it('consulta e mostra os bens depreciáveis em tabela', async () => {
    const user = userEvent.setup();
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 }, depreciaveis: DEPRECIAVEIS });
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    expect(await screen.findByText('TOMBO-2026-0001')).toBeInTheDocument();
    expect(screen.getByText('R$ 1.234,56')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há bens depreciáveis', async () => {
    const user = userEvent.setup();
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 }, depreciaveis: [] });
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    await waitFor(() => expect(screen.getByText('Nenhum bem depreciável')).toBeInTheDocument());
  });

  it('oculta a ação de incorporar bem sem a permissão patrimonio.gerenciar', async () => {
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    renderComPermissao(['patrimonio.ver']);
    await screen.findByText('Nenhum bem encontrado');
    expect(screen.queryByRole('button', { name: /Incorporar bem/i })).not.toBeInTheDocument();
  });

  it('exibe a ação de incorporar bem com a permissão patrimonio.gerenciar', async () => {
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);
    expect(await screen.findByRole('button', { name: /Incorporar bem/i })).toBeInTheDocument();
  });
});
