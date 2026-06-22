// Teste da tela de detalhe do Diário de Classe (Vitest + Testing Library). Cobre:
// render do diário aberto + frequência consolidada, abertura do modal de apuração e
// o gating das ações por permissão. O fetch é mockado por URL (diário/frequência).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { clearAccessToken } from '../../api/authToken';
import { renderEducacao } from './educacao.testUtils';
import { DiarioClasseDetailPage } from './DiarioClasseDetailPage';
import type { DiarioClasseResumo, FrequenciaConsolidada } from './api';

const MATRICULA_ID = '33333333-3333-3333-3333-333333333333';
const DIARIO_ID = '44444444-4444-4444-4444-444444444444';

const DIARIO: DiarioClasseResumo = {
  id: DIARIO_ID,
  matriculaId: MATRICULA_ID,
  situacao: 'Aberto',
  percentualFrequencia: 80,
  resultado: null,
  diasLetivosRegistrados: 100,
};

const FREQUENCIA: FrequenciaConsolidada = {
  diarioClasseId: DIARIO_ID,
  percentualFrequencia: 80,
  aulasComputadas: 100,
  atingiuMinimo: true,
};

function mockFetchByUrl(): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    const body = url.includes('/frequencia') ? FREQUENCIA : DIARIO;
    return Promise.resolve(
      new Response(JSON.stringify(body), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );
  });
}

function renderDetail(perm?: string[]) {
  return renderEducacao(
    <Routes>
      <Route path="/educacao/matriculas/:matriculaId/diario" element={<DiarioClasseDetailPage />} />
    </Routes>,
    perm,
    { route: `/educacao/matriculas/${MATRICULA_ID}/diario` },
  );
}

describe('DiarioClasseDetailPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('mostra a situação do diário e a frequência consolidada', async () => {
    mockFetchByUrl();
    renderDetail();

    expect(await screen.findByText('Aberto')).toBeInTheDocument();
    expect(await screen.findByText('Frequência consolidada')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Apurar resultado/i })).toBeInTheDocument();
  });

  it('abre o modal de registro de frequência ao acionar a ação', async () => {
    const user = userEvent.setup();
    mockFetchByUrl();
    renderDetail();

    await screen.findByText('Aberto');
    await user.click(screen.getByRole('button', { name: /Registrar frequência/i }));
    expect(await screen.findByLabelText(/Carga horária da aula/i)).toBeInTheDocument();
  });

  it('esconde as ações de gestão sem a permissão educacao.gerenciar', async () => {
    mockFetchByUrl();
    renderDetail(['educacao.ver']);

    await screen.findByText('Aberto');
    expect(screen.queryByRole('button', { name: /Apurar resultado/i })).not.toBeInTheDocument();
  });
});
