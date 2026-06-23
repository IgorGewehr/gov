// Teste (Vitest + Testing Library) da tela de LISTA + BUSCA de pacientes (Onda 0 —
// Navegabilidade). Cobre: render da lista paginada, link para o prontuário, estado
// vazio e o GATING da ação de cadastro ("saude.gerenciar"). O fetch global é mockado
// para isolar a UI da rede; um JWT com a claim "perm" libera/oculta as ações.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
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

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <PacienteListPage />
    </AuthProvider>,
  );
}

describe('PacienteListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista os pacientes e linka para o prontuário', async () => {
    mockFetch(pagina([PACIENTE]));
    renderComPermissao(['saude.prontuario.ler']);

    const prontuario = await screen.findByRole('link', { name: /Prontuário/i });
    expect(prontuario).toHaveAttribute(
      'href',
      '/saude/pacientes/11111111-1111-1111-1111-111111111111',
    );
    expect(screen.getByText('Maria da Silva')).toBeInTheDocument();
    expect(screen.getByText('Confirmado')).toBeInTheDocument();
  });

  it('exibe o estado vazio quando a busca não retorna pacientes', async () => {
    mockFetch(pagina([]));
    renderComPermissao(['saude.prontuario.ler']);

    await waitFor(() =>
      expect(screen.getByText('Nenhum paciente encontrado')).toBeInTheDocument(),
    );
  });

  it('aplica o termo de busca ao submeter o formulário', async () => {
    const user = userEvent.setup();
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        new Response(JSON.stringify(pagina([PACIENTE])), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
      );
    renderComPermissao(['saude.prontuario.ler']);

    await screen.findByText('Maria da Silva');
    await user.type(screen.getByLabelText(/Buscar paciente/i), 'Maria');
    await user.click(screen.getByRole('button', { name: /Buscar/i }));

    await waitFor(() => {
      const chamou = fetchMock.mock.calls.some(([url]) => String(url).includes('termo=Maria'));
      expect(chamou).toBe(true);
    });
  });

  it('oculta a ação de cadastro sem a permissão "saude.gerenciar"', async () => {
    mockFetch(pagina([PACIENTE]));
    renderComPermissao(['saude.prontuario.ler']);
    await screen.findByText('Maria da Silva');
    expect(screen.queryByRole('button', { name: /Cadastrar paciente/i })).not.toBeInTheDocument();
  });

  it('exibe a ação de cadastro com a permissão "saude.gerenciar"', async () => {
    mockFetch(pagina([PACIENTE]));
    renderComPermissao(['saude.prontuario.ler', 'saude.gerenciar']);
    await screen.findByText('Maria da Silva');
    expect(screen.getByRole('button', { name: /Cadastrar paciente/i })).toBeInTheDocument();
  });
});
