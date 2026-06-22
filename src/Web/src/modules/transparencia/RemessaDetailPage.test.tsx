// Teste da tela de DETALHE / FLUXO da prestação de contas. Cobre: render dos campos
// (ZIP/protocolo), gating do "Registrar protocolo" (transparencia.remessa.transmitir),
// aviso de que a transmissão é feita fora do sistema, e a reconciliação SICONFI com 503.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { clearAccessToken } from '../../api/authToken';
import { renderWithAuth } from './test-utils';
import { RemessaDetailPage } from './RemessaDetailPage';
import type { RemessaDetalhe } from './api';

const ID = '11111111-1111-1111-1111-111111111111';

const DETALHE: RemessaDetalhe = {
  id: ID,
  exercicio: 2026,
  periodo: '03/2026',
  leiaute: 'SIAPC 2026.1',
  situacao: 'ProntaParaTransmissao',
  hashIntegridade: 'abc123',
  nomeArquivoZip: 'SIAPC-2026-03.zip',
  protocolo: null,
  dataLimite: '2026-04-30',
  dataGeracao: '2026-04-01T10:00:00Z',
  dataEnvio: null,
  totalErros: 0,
  totalAlertas: 2,
  arquivos: [{ nome: 'SIAPC-2026-03.zip', tamanhoBytes: 1024 }],
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });
}

/** Roteia o fetch por URL/método: GET detalhe, GET críticas, POST reconciliação. */
function installFetch(reconciliacaoStatus: number): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    const method = init?.method ?? 'GET';
    if (url.includes('/reconciliacao')) {
      return Promise.resolve(
        reconciliacaoStatus === 503
          ? jsonResponse({ title: 'SICONFI indisponível' }, 503)
          : jsonResponse({ conforme: true }, 200),
      );
    }
    if (url.includes('/criticas')) return Promise.resolve(jsonResponse([]));
    if (method === 'GET') return Promise.resolve(jsonResponse(DETALHE));
    return Promise.resolve(jsonResponse(null, 204));
  });
}

function renderDetail(perm?: string[]) {
  return renderWithAuth(
    <Routes>
      <Route path="/transparencia/remessas-tce/:id" element={<RemessaDetailPage />} />
    </Routes>,
    perm,
    { route: `/transparencia/remessas-tce/${ID}` },
  );
}

describe('RemessaDetailPage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra ZIP, protocolo e o aviso de transmissão fora do sistema', async () => {
    installFetch(200);
    renderDetail(['transparencia.ver', 'transparencia.gerenciar']);
    expect(await screen.findAllByText('SIAPC-2026-03.zip')).not.toHaveLength(0);
    expect(
      screen.getByText(/transmissão ao TCE-RS é feita fora do sistema/i),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Baixar/i })).toBeInTheDocument();
  });

  it('mostra "Registrar protocolo" apenas com a permissão transmitir', async () => {
    installFetch(200);
    renderDetail(['transparencia.ver', 'transparencia.gerenciar', 'transparencia.remessa.transmitir']);
    expect(await screen.findByText('SIAPC 2026.1')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Registrar protocolo/i })).toBeInTheDocument();
  });

  it('esconde "Registrar protocolo" sem a permissão transmitir', async () => {
    installFetch(200);
    renderDetail(['transparencia.ver', 'transparencia.gerenciar']);
    expect(await screen.findByText('SIAPC 2026.1')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Registrar protocolo/i })).not.toBeInTheDocument();
  });

  it('trata 503 do SICONFI de forma graciosa (indisponível)', async () => {
    installFetch(503);
    renderDetail(['transparencia.ver', 'transparencia.gerenciar']);
    fireEvent.click(await screen.findByRole('button', { name: /Reconciliar \(SICONFI\)/i }));
    expect(await screen.findByText(/SICONFI indisponível/i)).toBeInTheDocument();
  });
});
