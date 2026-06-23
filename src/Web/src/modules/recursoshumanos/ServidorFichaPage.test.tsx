// Teste (Vitest + Testing Library) da FICHA FUNCIONAL do servidor.
// Cobre sucesso (dados + vínculo + timeline + histórico de folhas/ponto) e o
// estado "não encontrado" (404 -> null). Fetch global mockado para isolar a rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { ServidorFichaPage } from './ServidorFichaPage';
import type { FichaFuncional } from './api';

const SERVIDOR_ID = '11111111-1111-1111-1111-111111111111';

const FICHA: FichaFuncional = {
  id: SERVIDOR_ID,
  dadosPessoais: {
    cpf: '***.456.789-**',
    nome: 'Maria da Silva',
    dataNascimento: '1985-03-10',
  },
  vinculo: {
    matricula: 'MAT-001',
    regime: 'Rpps',
    situacao: 'EmExercicio',
    cargoId: '22222222-2222-2222-2222-222222222222',
    cargo: 'Analista Administrativo',
    tipoCargo: 'Efetivo',
    vencimento: 4500,
    lotacao: 'Secretaria de Administração',
  },
  timeline: [
    { evento: 'Nomeacao', data: '2024-02-01' },
    { evento: 'Posse', data: '2024-02-05' },
    { evento: 'Exercicio', data: '2024-02-15' },
  ],
  dependentes: [{ nome: 'João da Silva', parentesco: 'Filho', dataNascimento: '2015-06-01' }],
  folhas: [
    {
      folhaId: '33333333-3333-3333-3333-333333333333',
      ano: 2026,
      mes: 5,
      tipo: 'Mensal',
      situacao: 'Paga',
      totalProventos: 5000,
      totalDescontos: 800,
      liquido: 4200,
    },
  ],
  ponto: [
    {
      apuracaoId: '44444444-4444-4444-4444-444444444444',
      ano: 2026,
      mes: 5,
      situacao: 'Fechada',
      minutosExtras: 120,
      minutosFalta: 0,
      saldoBancoHorasMinutos: 120,
    },
  ],
};

function renderFicha() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/recursoshumanos/servidores/:servidorId/ficha"
        element={<ServidorFichaPage />}
      />
    </Routes>,
    { route: `/recursoshumanos/servidores/${SERVIDOR_ID}/ficha` },
  );
}

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
    Promise.resolve(
      new Response(body === null ? 'null' : JSON.stringify(body), {
        status,
        headers: { 'content-type': 'application/json' },
      }),
    ),
  );
}

describe('ServidorFichaPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe os dados, vínculo, timeline e históricos da ficha', async () => {
    mockFetch(FICHA);
    renderFicha();

    expect(await screen.findByText('Maria da Silva')).toBeInTheDocument();
    expect(screen.getByText('Analista Administrativo')).toBeInTheDocument();
    expect(screen.getByText('Secretaria de Administração')).toBeInTheDocument();
    expect(screen.getByText('Nomeacao')).toBeInTheDocument();
    expect(screen.getByText('João da Silva')).toBeInTheDocument();
    expect(
      screen.getByRole('table', { name: /Histórico de folhas do servidor/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('table', { name: /Histórico de apurações de ponto do servidor/i }),
    ).toBeInTheDocument();
  });

  it('mostra "não encontrado" quando a ficha é nula (404)', async () => {
    mockFetch(null);
    renderFicha();

    await waitFor(() =>
      expect(screen.getByText('Servidor não encontrado')).toBeInTheDocument(),
    );
  });
});
