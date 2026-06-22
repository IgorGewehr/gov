// Teste da tela da Trilha de Auditoria (módulo admin). Cobre: gating por permissão
// (sem "admin.auditoria.ver" → aviso, sem requisição), sucesso (tabela com itens) e
// o expand de uma linha mostrando os valores JSON (oldValues/newValues) formatados.
// O JWT é montado como em auth/Can.test.tsx (apenas o payload importa para a UI) e
// o fetch global é mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test/renderWithProviders';
import { setAccessToken, clearAccessToken } from '../../../api/authToken';
import { AuthProvider } from '../../../auth/AuthProvider';
import { AuditoriaListPage } from './AuditoriaListPage';
import type { AuditoriaPagina } from './auditoria.api';

function toBase64Url(obj: unknown): string {
  const json = JSON.stringify(obj);
  const bytes = new TextEncoder().encode(json);
  let binary = '';
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeToken(perm: string[]): string {
  const header = toBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = toBase64Url({
    sub: 'user-1',
    name: 'Servidor Teste',
    email: 'servidor@orgao.gov.br',
    tenant_id: 'tenant-1',
    perm,
  });
  return `${header}.${payload}.`;
}

function renderComPermissao(perm: string[]) {
  setAccessToken(fakeToken(perm));
  return renderWithProviders(
    <AuthProvider>
      <AuditoriaListPage />
    </AuthProvider>,
  );
}

const PAGINA: AuditoriaPagina = {
  total: 1,
  pagina: 1,
  tamanho: 20,
  itens: [
    {
      id: 'aud-1',
      entityName: 'Processo',
      entityId: 'proc-123',
      action: 'Modified',
      userId: 'user-9',
      ipAddress: '10.0.0.1',
      timestampUtc: '2026-01-10T13:45:00Z',
      oldValues: '{"situacao":"Autuado"}',
      newValues: '{"situacao":"EmTramitacao"}',
    },
  ],
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('AuditoriaListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('bloqueia o acesso quando o usuário não tem a permissão admin.auditoria.ver', () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch');
    renderComPermissao(['outra.permissao']);

    expect(
      screen.getByText(/não possui permissão para consultar a trilha de auditoria/i),
    ).toBeInTheDocument();
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it('lista os registros da trilha em tabela', async () => {
    mockFetchOnce(PAGINA);
    renderComPermissao(['admin.auditoria.ver']);

    expect(await screen.findByText('Processo')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Alteração' })).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Registros da trilha de auditoria/i);
  });

  it('expande uma linha e mostra os valores JSON formatados', async () => {
    const user = userEvent.setup();
    mockFetchOnce(PAGINA);
    renderComPermissao(['admin.auditoria.ver']);

    await user.click(await screen.findByRole('button', { name: /Ver valores/i }));

    await waitFor(() => expect(screen.getByText('Valores anteriores')).toBeInTheDocument());
    expect(screen.getByText('Valores posteriores')).toBeInTheDocument();
    expect(screen.getByText(/"situacao": "EmTramitacao"/)).toBeInTheDocument();
  });
});
