// Teste (Vitest + Testing Library) da tela de lista de beneficios de uma familia
// (query ObterBeneficiosDaFamilia). Cobre os estados que TODA tela de lista deve ter:
// pre-consulta, sucesso (tabela por familia) e a guarda de gating do <Can>. A busca
// e sob demanda (por familiaId): o teste preenche o campo e submete o formulario.
// O fetch global e mockado para isolar a UI da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { clearAccessToken } from '../../../api/authToken';
import { BeneficioListPage } from './BeneficioListPage';
import type { BeneficioResumo } from './beneficio.api';

const FAMILIA_ID = '11111111-1111-1111-1111-111111111111';

const BENEFICIOS: BeneficioResumo[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    familiaId: FAMILIA_ID,
    tipo: 'Eventual',
    competencia: '06/2026',
    valor: null,
    situacao: 'Concedida',
    motivoIndeferimento: null,
    dataDecisao: '2026-06-01',
  },
];

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

async function consultarFamilia(): Promise<void> {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText(/Identificador da família/i), FAMILIA_ID);
  await user.click(screen.getByRole('button', { name: /Consultar/i }));
}

describe('BeneficioListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial e a ação de avaliar elegibilidade (com permissão)', () => {
    mockFetch([]);
    renderComAuth(<BeneficioListPage />);

    expect(screen.getByRole('heading', { name: 'Benefícios da família' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Avaliar elegibilidade/i })).toBeInTheDocument();
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('lista os benefícios da família consultada em tabela', async () => {
    mockFetch(BENEFICIOS);
    renderComAuth(<BeneficioListPage />);

    await consultarFamilia();

    const tabela = await screen.findByRole('table');
    const { getByText } = within(tabela);
    expect(getByText('Benefício eventual')).toBeInTheDocument();
    expect(getByText('Concedida')).toBeInTheDocument();
  });

  it('oculta a ação de avaliar quando o usuário não tem assistenciasocial.gerenciar', () => {
    mockFetch([]);
    renderComAuth(<BeneficioListPage />, ['assistenciasocial.ver']);

    expect(
      screen.queryByRole('button', { name: /Avaliar elegibilidade/i }),
    ).not.toBeInTheDocument();
  });
});
