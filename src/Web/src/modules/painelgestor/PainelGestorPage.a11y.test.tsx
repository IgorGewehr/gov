// Teste de ACESSIBILIDADE (CLAUDE.md §13: gov.br DS + eMAG + WCAG 2.1 AA) da
// PainelGestorPage — PÁGINA PRINCIPAL do módulo Painel do Gestor (dashboard
// executivo). Valida a faixa-resumo + os 5 cards de KPI carregados. O fetch é
// mockado; um JWT com a claim "painel.ver" libera a leitura.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { checarAcessibilidade } from '../../test/axe';
import { setAccessToken, clearAccessToken } from '../../api/authToken';
import { AuthProvider } from '../../auth/AuthProvider';
import { PainelGestorPage } from './PainelGestorPage';
import { SituacaoLimite } from './api';
import type { PainelGestorDto } from './api';

const PAINEL: PainelGestorDto = {
  exercicio: 2026,
  execucaoOrcamentaria: {
    dotacaoAtualizada: 10_000_000,
    empenhado: 6_000_000,
    liquidado: 5_000_000,
    pago: 4_500_000,
    percentualEmpenhado: 0.6,
    percentualLiquidado: 0.5,
    percentualPago: 0.45,
  },
  minimos: [
    {
      setor: 'Saude',
      receitaBase: 10_000_000,
      aplicado: 1_700_000,
      percentualAplicado: 0.17,
      percentualMinimo: 0.15,
      situacao: 'Atingido',
    },
    {
      setor: 'Educacao',
      receitaBase: 10_000_000,
      aplicado: 2_600_000,
      percentualAplicado: 0.26,
      percentualMinimo: 0.25,
      situacao: 'Atingido',
    },
  ],
  arrecadacao: {
    arrecadacaoTributaria: 3_200_000,
    dividaAtivaSaldoInscrito: 1_100_000,
    dividaAtivaSaldoAjuizado: 400_000,
    dividaAtivaRecuperada: 250_000,
  },
  pessoalLrf: {
    despesaPessoal: 4_800_000,
    receitaCorrenteLiquida: 10_000_000,
    percentualDaRcl: 0.48,
    limiteLegal: 0.54,
    limitePrudencial: 0.513,
    limiteAlerta: 0.486,
    situacao: SituacaoLimite.Adequado,
  },
  prestacaoContas: {
    remessasEnviadas: 12,
    remessasComPrazoVencido: 0,
    emDia: true,
    situacao: SituacaoLimite.Adequado,
  },
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
  const payload = toBase64Url({ sub: 'u-1', name: 'Gestor', tenant_id: 't-1', perm });
  return `${header}.${payload}.`;
}

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

function renderPagina() {
  setAccessToken(fakeToken(['painel.ver']));
  return renderWithProviders(
    <AuthProvider>
      <PainelGestorPage />
    </AuthProvider>,
  );
}

describe('PainelGestorPage (acessibilidade)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('não tem violações com os 5 KPIs carregados', async () => {
    mockFetch(PAINEL);
    const { container } = renderPagina();

    expect(await screen.findByText('Execução paga')).toBeInTheDocument();
    expect(await checarAcessibilidade(container)).toHaveNoViolations();
  });
});
