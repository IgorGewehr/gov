// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// BemPatrimonialListPage — PÁGINA PRINCIPAL (índice) do módulo Patrimônio. Valida a
// lista navegável de bens (GET /patrimonio/bens, dispara no mount) e o estado vazio.
// O fetch é mockado ROTEADO POR URL; um JWT com a claim "perm" libera a leitura.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { checarAcessibilidade } from '../../../test/axe';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { BemPatrimonialListPage } from './BemPatrimonialListPage';
import type { BemPatrimonialItemLista } from './bempatrimonial.api';
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

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function mockFetchPorUrl(opcoes: { lista?: unknown; depreciaveis?: unknown }): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url ?? String(input);
    if (url.includes('/bens/depreciaveis')) return Promise.resolve(jsonResponse(opcoes.depreciaveis ?? []));
    if (url.includes('/patrimonio/bens'))
      return Promise.resolve(jsonResponse(opcoes.lista ?? { itens: [], total: 0, pagina: 1, tamanho: 20 }));
    return Promise.resolve(jsonResponse([]));
  });
}

describe('BemPatrimonialListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com os bens navegáveis em tabela', async () => {
    mockFetchPorUrl({ lista: LISTA });
    const { container } = renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);

    expect(await screen.findByText('Notebook de gabinete')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações no estado vazio da busca', async () => {
    mockFetchPorUrl({ lista: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    const { container } = renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('Nenhum bem encontrado')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
