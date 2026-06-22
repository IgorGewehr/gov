// Teste da tela de lista do agregado Documento (modulo Protocolo), seguindo o
// PADRAO-OURO de processo/ProcessoListPage.test.tsx. Cobre o estado inicial
// (sem consulta) e o fluxo de consulta -> tabela. O fetch global e mockado.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { DocumentoListPage } from './DocumentoListPage';
import type { DocumentoResumo } from './documento.api';

const DOCUMENTOS: DocumentoResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    hash: 'abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789',
    criticidade: 'Alta',
    situacao: 'Assinado',
    tipoAssinatura: 'AssinaturaQualificada',
    dataJuntada: '2026-02-15',
  },
];

const PROCESSO_ID = '11111111-1111-1111-1111-111111111111';

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('DocumentoListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<DocumentoListPage />, ['protocolo.ver', 'protocolo.gerenciar']);
    expect(screen.getByText('Faca uma consulta')).toBeInTheDocument();
  });

  it('consulta e mostra os documentos do processo em tabela', async () => {
    const user = userEvent.setup();
    mockFetchOnce(DOCUMENTOS);
    renderComAuth(<DocumentoListPage />, ['protocolo.ver', 'protocolo.gerenciar']);

    await user.type(screen.getByLabelText(/Identificador do processo/i), PROCESSO_ID);
    await user.click(screen.getByRole('button', { name: /Consultar/i }));

    expect(
      await screen.findByRole('table', { name: new RegExp(`Documentos do processo ${PROCESSO_ID}`, 'i') }),
    ).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Assinado' })).toBeInTheDocument();
  });
});
