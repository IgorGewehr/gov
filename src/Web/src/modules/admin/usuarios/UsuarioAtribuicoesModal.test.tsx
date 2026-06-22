// Teste do modal de ATRIBUIÇÕES de papel com escopo (M1 / Identidade). Mocka o fetch
// global por rota para isolar a UI da rede. Cobre: render das atribuições atuais
// (papel + UO + escopo + vigência), atribuir (POST), revogar (DELETE) e o tratamento
// amigável do 403 da regra "não delega o que não tem".
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { UsuarioAtribuicoesModal } from './UsuarioAtribuicoesModal';
import type { UsuarioResumo } from './usuario.api';
import type { AtribuicaoResumo, UnidadeNo } from './atribuicao.api';

const USUARIO: UsuarioResumo = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Maria Servidora',
  email: 'maria@orgao.gov.br',
  ativo: true,
  papeis: ['Operador'],
};

const PAPEIS = [
  { id: 'p-1', nome: 'Operador' },
  { id: 'p-2', nome: 'Gestor' },
];

const UNIDADES: UnidadeNo[] = [
  {
    id: 'u-1',
    codigo: 'SEC',
    nome: 'Secretaria de Fazenda',
    tipo: 'Secretaria',
    ativo: true,
    filhos: [
      { id: 'u-2', codigo: 'DEP', nome: 'Departamento de Tributos', tipo: 'Departamento', ativo: true, filhos: [] },
    ],
  },
];

const ATRIBUICOES: AtribuicaoResumo[] = [
  {
    papelId: 'p-1',
    papelNome: 'Operador',
    unidadeId: 'u-2',
    unidadeNome: 'Departamento de Tributos',
    incluiSubunidades: true,
    vigenciaFim: '2026-12-31',
  },
];

function toBase64Url(obj: unknown): string {
  const bytes = new TextEncoder().encode(JSON.stringify(obj));
  let binary = '';
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeToken(): string {
  const header = toBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = toBase64Url({ sub: 'u-1', tenant_id: 't-1', perm: ['identidade.usuarios.gerenciar'] });
  return `${header}.${payload}.`;
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

interface MockOptions {
  atribuicoes?: AtribuicaoResumo[];
  onWrite?: (method: string) => Response;
}

function mockApi(options: MockOptions = {}): void {
  const atribuicoes = options.atribuicoes ?? ATRIBUICOES;
  vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    const method = (init?.method ?? 'GET').toUpperCase();
    if (url.includes('/atribuicoes')) {
      if (method !== 'GET' && options.onWrite) return Promise.resolve(options.onWrite(method));
      if (method !== 'GET') return Promise.resolve(jsonResponse(null, 204));
      return Promise.resolve(jsonResponse(atribuicoes));
    }
    if (url.includes('/identidade/papeis')) return Promise.resolve(jsonResponse(PAPEIS));
    if (url.includes('/identidade/unidades')) return Promise.resolve(jsonResponse(UNIDADES));
    return Promise.resolve(jsonResponse([]));
  });
}

function abrir() {
  setAccessToken(fakeToken());
  return renderWithProviders(
    <UsuarioAtribuicoesModal open onClose={() => {}} usuario={USUARIO} />,
  );
}

describe('UsuarioAtribuicoesModal', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('lista as atribuições atuais com papel, UO, escopo e vigência', async () => {
    mockApi();
    abrir();

    const lista = await screen.findByRole('list');
    expect(within(lista).getByText('Operador')).toBeInTheDocument();
    expect(within(lista).getByText(/Departamento de Tributos/)).toBeInTheDocument();
    expect(within(lista).getByText('Esta UO e subunidades')).toBeInTheDocument();
    expect(within(lista).getByText('Até 31/12/2026')).toBeInTheDocument();
  });

  it('exibe a regra de escopo "não delega o que não tem"', async () => {
    mockApi();
    abrir();
    expect(await screen.findByText(/não delega o que você não tem/i)).toBeInTheDocument();
  });

  it('atribui um papel com UO e escopo (POST)', async () => {
    const onWrite = vi.fn(() => jsonResponse(null, 204));
    mockApi({ onWrite });
    const user = userEvent.setup();
    abrir();

    await screen.findByRole('list');
    await user.selectOptions(screen.getByLabelText(/^Papel/), 'p-2');
    await user.selectOptions(
      screen.getByLabelText(/^Unidade organizacional/),
      'u-1',
    );
    await user.click(screen.getByRole('button', { name: 'Atribuir papel' }));

    await waitFor(() => expect(onWrite).toHaveBeenCalledWith('POST'));
  });

  it('revoga uma atribuição (DELETE)', async () => {
    const onWrite = vi.fn(() => jsonResponse(null, 204));
    mockApi({ onWrite });
    const user = userEvent.setup();
    abrir();

    const revogar = await screen.findByRole('button', {
      name: /Revogar Operador em Departamento de Tributos/i,
    });
    await user.click(revogar);

    await waitFor(() => expect(onWrite).toHaveBeenCalledWith('DELETE'));
  });

  it('trata 403 com mensagem amigável sobre escopo', async () => {
    mockApi({
      onWrite: () =>
        jsonResponse({ title: 'Forbidden', detail: 'Sem escopo', status: 403 }, 403),
    });
    const user = userEvent.setup();
    abrir();

    const revogar = await screen.findByRole('button', {
      name: /Revogar Operador em Departamento de Tributos/i,
    });
    await user.click(revogar);

    expect(
      await screen.findByText(/não pode conceder ou revogar um papel além do seu próprio escopo/i),
    ).toBeInTheDocument();
  });
});
