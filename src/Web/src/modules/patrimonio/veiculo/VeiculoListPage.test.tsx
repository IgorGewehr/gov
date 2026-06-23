// Teste (Vitest + Testing Library) da tela de Frota (Veiculo), no PADRÃO-OURO de
// src/modules/tributos. A página é abas: a aba inicial "Veículos" é a LISTA NAVEGÁVEL
// (Onda 0) — busca por descrição/placa/RENAVAM + filtro de situação, paginada
// (GET /patrimonio/veiculos), que dispara na montagem; ao trocar para "Multas
// pendentes" dispara ListarMultasPendentes. O fetch global é mockado por URL para
// isolar a UI da rede e tolerar a consulta de lista que roda no mount.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { VeiculoListPage } from './VeiculoListPage';
import type { MultaResumo, VeiculoItemLista } from './veiculo.api';
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
      <VeiculoListPage />
    </AuthProvider>,
  );
}

const VEICULOS: ResultadoPaginado<VeiculoItemLista> = {
  itens: [
    {
      id: '33333333-3333-3333-3333-333333333333',
      placa: 'XYZ9A88',
      renavam: '00999888777',
      descricao: 'Caminhão basculante',
      numeroTombamento: 'TOMBO-2026-0009',
      odometro: 45000,
      valorContabil: 180000,
      situacao: 'Tombado',
    },
  ],
  total: 1,
  pagina: 1,
  tamanho: 20,
};

const MULTAS: MultaResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    veiculoId: '22222222-2222-2222-2222-222222222222',
    placa: 'ABC1D23',
    codigoInfracaoCtb: '74550',
    valor: 195.23,
    dataInfracao: '2026-03-10',
  },
];

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

/**
 * Mock de fetch roteado por URL (resiliente à ordem das chamadas): a lista navegável
 * de veículos dispara no mount; as outras consultas disparam ao trocar de aba.
 */
function mockFetchPorUrl(opcoes: {
  veiculos?: unknown;
  multas?: unknown;
  licenciamentos?: unknown;
}): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url ?? String(input);
    if (url.includes('/veiculos/multas-pendentes')) return Promise.resolve(jsonResponse(opcoes.multas ?? []));
    if (url.includes('/veiculos/licenciamentos-pendentes'))
      return Promise.resolve(jsonResponse(opcoes.licenciamentos ?? []));
    if (url.includes('/patrimonio/veiculos'))
      return Promise.resolve(jsonResponse(opcoes.veiculos ?? { itens: [], total: 0, pagina: 1, tamanho: 20 }));
    return Promise.resolve(jsonResponse([]));
  });
}

describe('VeiculoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe a aba inicial com a lista navegável de veículos e a ação de incorporação', async () => {
    mockFetchPorUrl({ veiculos: VEICULOS });
    renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);

    expect(await screen.findByText('XYZ9A88')).toBeInTheDocument();
    expect(screen.getByText('Caminhão basculante')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Incorporar veículo/i })).toBeInTheDocument();
  });

  it('mostra o estado vazio quando a busca de veículos não retorna resultados', async () => {
    mockFetchPorUrl({ veiculos: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    renderComPermissao(['patrimonio.ver']);

    expect(await screen.findByText('Nenhum veículo encontrado')).toBeInTheDocument();
  });

  it('oculta a ação de incorporação sem a permissão patrimonio.gerenciar', async () => {
    mockFetchPorUrl({ veiculos: { itens: [], total: 0, pagina: 1, tamanho: 20 } });
    renderComPermissao(['patrimonio.ver']);
    await screen.findByText('Nenhum veículo encontrado');
    expect(screen.queryByRole('button', { name: /Incorporar veículo/i })).not.toBeInTheDocument();
  });

  it('lista as multas pendentes ao trocar para a aba Multas', async () => {
    const user = userEvent.setup();
    mockFetchPorUrl({ veiculos: VEICULOS, multas: MULTAS });
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('tab', { name: /Multas pendentes/i }));

    expect(await screen.findByText('ABC1D23')).toBeInTheDocument();
    expect(screen.getByText('R$ 195,23')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há multas pendentes', async () => {
    const user = userEvent.setup();
    mockFetchPorUrl({ veiculos: VEICULOS, multas: [] });
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('tab', { name: /Multas pendentes/i }));

    expect(await screen.findByText('Nenhuma multa pendente')).toBeInTheDocument();
  });
});
