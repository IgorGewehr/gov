// Teste (Vitest + Testing Library) da tela de CONSULTA sigilosa do Prontuario SUAS
// (ObterProntuarioDaFamiliaQuery). Cobre: estado inicial (consulta sigilosa), o aviso
// de leitura auditada (LGPD art. 11) e a guarda do <Can> sobre a acao de abrir prontuario.
// O fetch global e mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderComAuth } from '../../../test/renderComAuth';
import { clearAccessToken } from '../../../api/authToken';
import { ProntuarioSuasListPage } from './ProntuarioSuasListPage';

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify(body), {
        status,
        headers: { 'content-type': 'application/json' },
      }),
    ),
  );
}

describe('ProntuarioSuasListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o aviso de leitura sigilosa/auditada e a ação de abrir prontuário (com permissão)', () => {
    mockFetch([]);
    renderComAuth(<ProntuarioSuasListPage />);

    expect(screen.getByRole('heading', { name: 'Prontuário SUAS' })).toBeInTheDocument();
    expect(screen.getByText('Leitura sigilosa e auditada')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Abrir prontuário/i })).toBeInTheDocument();
    expect(screen.getByText('Consulta sigilosa')).toBeInTheDocument();
  });

  it('mantém o botão de consultar desabilitado enquanto faltam campos obrigatórios', () => {
    mockFetch([]);
    renderComAuth(<ProntuarioSuasListPage />);

    expect(screen.getByRole('button', { name: /Consultar prontuário/i })).toBeDisabled();
  });

  it('oculta a ação de abrir prontuário quando o usuário não tem assistenciasocial.gerenciar', () => {
    mockFetch([]);
    renderComAuth(<ProntuarioSuasListPage />, ['assistenciasocial.ver']);

    expect(screen.queryByRole('button', { name: /Abrir prontuário/i })).not.toBeInTheDocument();
  });
});
