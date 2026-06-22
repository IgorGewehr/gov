// Teste da tela de Configuração de Módulos do tenant (módulo Admin), seguindo o
// PADRÃO-OURO de protocolo/ProcessoListPage.test.tsx. Cobre: gating sem permissão,
// listagem (sucesso) e a interação de ativar/desativar via switch (PUT). O hook
// useAuth é mockado para controlar tenantId e permissões; o fetch global é mockado
// para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { ModulosConfigPage } from './ModulosConfigPage';
import type { TenantModulo } from './modulos.api';

const TENANT_ID = 'tenant-abc';

// --- Mock de useAuth: permissões controláveis por teste ---
let permissoes: string[] = [];
vi.mock('../../../auth/useAuth', () => ({
  useAuth: () => ({
    user: {
      id: 'u1',
      nome: 'Admin',
      email: 'admin@orgao.gov.br',
      tenantId: TENANT_ID,
      tenantNome: 'Prefeitura de Maximiliano de Almeida',
      roles: [],
      permissions: permissoes,
    },
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
    hasRole: () => false,
    hasPermission: (p: string) => permissoes.includes(p),
  }),
}));

const MODULOS: TenantModulo[] = [
  { modulo: 'tributos', ativo: true },
  { modulo: 'saude', ativo: false },
];

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(status === 204 ? null : JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('ModulosConfigPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    permissoes = ['admin.modulos.configurar'];
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('bloqueia a tela quando o usuário não tem a permissão', () => {
    permissoes = [];
    renderWithProviders(<ModulosConfigPage />);
    expect(screen.getByText('Acesso restrito')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('lista os módulos do tenant com seu status', async () => {
    mockFetchOnce(MODULOS);
    renderWithProviders(<ModulosConfigPage />);

    // Tributos ativo => switch marcado; Saúde inativo => switch desmarcado.
    const switchTributos = await screen.findByRole('switch', { name: /Tributos .* desativar/i });
    expect(switchTributos).toBeChecked();
    const switchSaude = screen.getByRole('switch', { name: /Saúde .* ativar/i });
    expect(switchSaude).not.toBeChecked();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Módulos licenciados/i);
    expect(screen.getAllByRole('switch')).toHaveLength(2);
  });

  it('ativa um módulo inativo ao clicar no switch (PUT)', async () => {
    const user = userEvent.setup();
    mockFetchOnce(MODULOS); // GET inicial
    renderWithProviders(<ModulosConfigPage />);

    const switchSaude = await screen.findByRole('switch', { name: /Saúde .* ativar/i });
    const fetchSpy = vi.spyOn(globalThis, 'fetch');
    fetchSpy.mockResolvedValueOnce(new Response(null, { status: 204 })); // PUT
    fetchSpy.mockResolvedValueOnce(
      new Response(JSON.stringify([{ modulo: 'tributos', ativo: true }, { modulo: 'saude', ativo: true }]), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    ); // refetch da invalidação

    await user.click(switchSaude);

    await waitFor(() => {
      const putCall = fetchSpy.mock.calls.find(
        ([url, init]) => String(url).includes('/modulos/saude') && init?.method === 'PUT',
      );
      expect(putCall).toBeTruthy();
      expect(String(putCall?.[1]?.body)).toContain('"ativo":true');
    });
  });
});
