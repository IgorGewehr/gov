// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// ServidoresListPage — PÁGINA PRINCIPAL (índice) do módulo Recursos Humanos. Valida
// a lista paginada carregada e o estado vazio. O fetch é mockado e roteado por URL
// (a lista monta o modal de admissão, que consulta /cargos/com-vagas).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { ServidoresListPage } from './ServidoresListPage';
import type { PaginaResultado, ServidorResumo } from './api';

const SERVIDORES: ServidorResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    cpf: '***.456.789-**',
    matricula: 'MAT-001',
    nomeServidor: 'Maria da Silva',
    cargoId: '22222222-2222-2222-2222-222222222222',
    regime: 'Rpps',
    situacao: 'EmExercicio',
    dataNomeacao: '2024-02-01',
    dataExercicio: '2024-02-15',
  },
];

function pagina(itens: ServidorResumo[]): PaginaResultado<ServidorResumo> {
  return { itens, total: itens.length, pagina: 1, tamanho: 20 };
}

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

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function mockFetch(servidores: PaginaResultado<ServidorResumo>): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    if (url.includes('/cargos/com-vagas')) {
      return Promise.resolve(json([]));
    }
    return Promise.resolve(json(servidores));
  });
}

function renderPagina(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <ServidoresListPage />
    </AuthProvider>,
  );
}

describe('ServidoresListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com os servidores em tabela paginada', async () => {
    mockFetch(pagina(SERVIDORES));
    const { container } = renderPagina(['recursoshumanos.ver', 'recursoshumanos.gerenciar']);

    expect(await screen.findByText('Maria da Silva')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações no estado vazio da busca', async () => {
    mockFetch(pagina([]));
    const { container } = renderPagina(['recursoshumanos.ver']);

    await waitFor(() =>
      expect(screen.getByText('Nenhum servidor encontrado')).toBeInTheDocument(),
    );
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
