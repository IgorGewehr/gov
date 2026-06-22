// Teste (Vitest + Testing Library) da tela de Frota (Veiculo), no PADRÃO-OURO de
// src/modules/tributos. A página é abas: a aba inicial "Veículo" oferece a consulta
// por identificador (ObterVeiculo, via DetailPage) e a ação de incorporação; ao
// trocar para "Multas pendentes" dispara ListarMultasPendentes. O fetch global é
// mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { VeiculoListPage } from './VeiculoListPage';
import type { MultaResumo } from './veiculo.api';

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

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('VeiculoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe a aba inicial de consulta de veículo e a ação de incorporação', () => {
    renderComPermissao(['patrimonio.ver', 'patrimonio.gerenciar']);
    expect(screen.getByText('Consulte um veículo da frota')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Incorporar veículo/i })).toBeInTheDocument();
  });

  it('oculta a ação de incorporação sem a permissão patrimonio.gerenciar', () => {
    renderComPermissao(['patrimonio.ver']);
    expect(screen.queryByRole('button', { name: /Incorporar veículo/i })).not.toBeInTheDocument();
  });

  it('lista as multas pendentes ao trocar para a aba Multas', async () => {
    const user = userEvent.setup();
    mockFetchOnce(MULTAS);
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('tab', { name: /Multas pendentes/i }));

    expect(await screen.findByText('ABC1D23')).toBeInTheDocument();
    expect(screen.getByText('R$ 195,23')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando não há multas pendentes', async () => {
    const user = userEvent.setup();
    mockFetchOnce([]);
    renderComPermissao(['patrimonio.ver']);

    await user.click(screen.getByRole('tab', { name: /Multas pendentes/i }));

    expect(await screen.findByText('Nenhuma multa pendente')).toBeInTheDocument();
  });
});
