// Teste da tela de LISTA da prestação de contas. Cobre: estado inicial (sem consulta),
// a consulta exibindo situação/nome do ZIP/protocolo, e o gating do botão "Gerar remessa".
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { clearAccessToken } from '../../api/authToken';
import { renderWithAuth } from './test-utils';
import { RemessaListPage } from './RemessaListPage';
import type { RemessaResumo } from './api';

const RESUMO: RemessaResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    exercicio: 2026,
    periodo: '03/2026',
    situacao: 'ProntaParaTransmissao',
    nomeArquivoZip: 'SIAPC-2026-03.zip',
    protocolo: null,
    dataLimite: '2026-04-30',
    dataEnvio: null,
  },
];

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('RemessaListPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra estado inicial e o botão de gerar para quem gerencia', () => {
    mockFetch([]);
    renderWithAuth(<RemessaListPage />, ['transparencia.ver', 'transparencia.gerenciar']);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Gerar remessa/i })).toBeInTheDocument();
  });

  it('esconde o botão de gerar para quem só visualiza', () => {
    mockFetch([]);
    renderWithAuth(<RemessaListPage />, ['transparencia.ver']);
    expect(screen.queryByRole('button', { name: /Gerar remessa/i })).not.toBeInTheDocument();
  });

  it('lista a remessa com situação e nome do ZIP após consultar', async () => {
    mockFetch(RESUMO);
    renderWithAuth(<RemessaListPage />);
    fireEvent.click(screen.getByRole('button', { name: /Consultar/i }));
    expect(await screen.findByText('SIAPC-2026-03.zip')).toBeInTheDocument();
    // "Pronta para transmissão" também aparece como opção do filtro; a Tag está no <td>.
    const tag = screen.getByText('Pronta para transmissão', { selector: 'span' });
    expect(tag).toBeInTheDocument();
  });
});
