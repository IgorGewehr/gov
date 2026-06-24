// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// PacienteListPage — PÁGINA PRINCIPAL (índice) do módulo Saúde. Valida a lista
// paginada (com link para o prontuário) e o estado vazio. O fetch é mockado e um
// JWT com a claim "perm" libera a leitura do prontuário.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { PacienteListPage } from './PacienteListPage';
import type { PacienteItemLista, ResultadoPaginado } from './api';

const PACIENTE: PacienteItemLista = {
  id: '11111111-1111-1111-1111-111111111111',
  cns: '700000000000001',
  nome: 'Maria da Silva',
  nomeSocial: null,
  dataNascimento: '1990-05-20',
  sexo: 'Feminino',
  cnsConfirmado: true,
  situacao: 'Ativo',
};

function pagina(itens: PacienteItemLista[]): ResultadoPaginado<PacienteItemLista> {
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

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

function renderPagina(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <PacienteListPage />
    </AuthProvider>,
  );
}

describe('PacienteListPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com os pacientes em tabela', async () => {
    mockFetch(pagina([PACIENTE]));
    const { container } = renderPagina(['saude.prontuario.ler', 'saude.gerenciar']);

    expect(await screen.findByText('Maria da Silva')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });

  it('não tem violações no estado vazio da busca', async () => {
    mockFetch(pagina([]));
    const { container } = renderPagina(['saude.prontuario.ler']);

    await waitFor(() =>
      expect(screen.getByText('Nenhum paciente encontrado')).toBeInTheDocument(),
    );
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
