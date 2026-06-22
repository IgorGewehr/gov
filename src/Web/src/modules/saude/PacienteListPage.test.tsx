// Teste (Vitest + Testing Library) do fluxo de consulta de Paciente por CNS. Cobre os
// estados que toda tela de consulta deve ter: inicial (sem consulta), sucesso (cartão
// do paciente), não-encontrado e o GATING da ação de cadastro ("saude.gerenciar").
// O fetch global é mockado para isolar a UI da rede; um JWT com a claim "perm" libera/
// oculta as ações (padrão DividaAtivaListPage.test.tsx).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { PacienteListPage } from './PacienteListPage';
import type { PacienteResumo } from './api';

const PACIENTE: PacienteResumo = {
  id: '11111111-1111-1111-1111-111111111111',
  cns: '700000000000001',
  nome: 'Maria da Silva',
  nomeSocial: null,
  dataNascimento: '1990-05-20',
  sexo: 'Feminino',
  cnsConfirmado: true,
  situacao: 'Ativo',
};

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

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
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

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComPermissao(['saude.ver']);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('oculta a ação de cadastro sem a permissão "saude.gerenciar"', () => {
    renderComPermissao(['saude.ver']);
    expect(screen.queryByRole('button', { name: /Cadastrar paciente/i })).not.toBeInTheDocument();
  });

  it('exibe a ação de cadastro com a permissão "saude.gerenciar"', () => {
    renderComPermissao(['saude.ver', 'saude.gerenciar']);
    expect(screen.getByRole('button', { name: /Cadastrar paciente/i })).toBeInTheDocument();
  });

  it('consulta por CNS e mostra o cartão do paciente', async () => {
    const user = userEvent.setup();
    mockFetchOnce(PACIENTE);
    renderComPermissao(['saude.ver']);

    await user.type(screen.getByLabelText(/Cartão Nacional de Saúde/i), '700000000000001');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    const prontuario = await screen.findByRole('link', { name: /Abrir prontuário/i });
    expect(prontuario).toHaveAttribute(
      'href',
      '/saude/pacientes/11111111-1111-1111-1111-111111111111',
    );
    expect(screen.getAllByText('Maria da Silva').length).toBeGreaterThan(0);
    expect(screen.getByText('CNS confirmado')).toBeInTheDocument();
  });

  it('mostra o estado de não-encontrado quando o paciente não existe', async () => {
    const user = userEvent.setup();
    mockFetchOnce(null);
    renderComPermissao(['saude.ver']);

    await user.type(screen.getByLabelText(/Cartão Nacional de Saúde/i), '700000000000099');
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    await waitFor(() =>
      expect(screen.getByText('Paciente não encontrado')).toBeInTheDocument(),
    );
  });
});
