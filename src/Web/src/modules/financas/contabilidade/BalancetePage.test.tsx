// Teste do Balancete: estado inicial, consulta com tabela e destaque de desbalanceamento
// (ΣDébitos ≠ ΣCréditos).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { BalancetePage } from './BalancetePage';
import type { LinhaBalancete } from './contabilidade.api';

const PERMS = ['financas.ver'];

function linha(over: Partial<LinhaBalancete>): LinhaBalancete {
  return {
    contaId: '11111111-1111-1111-1111-111111111111',
    codigoConta: '1.1.1',
    titulo: 'Caixa',
    naturezaSaldo: 'Devedora',
    naturezaInformacao: 'Patrimonial',
    saldoAnterior: 0,
    totalDebitos: 0,
    totalCreditos: 0,
    saldoAtual: 0,
    ...over,
  };
}

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } }),
  );
}

describe('BalancetePage', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('exibe o estado inicial pedindo uma consulta', () => {
    renderComAuth(<BalancetePage />, PERMS);
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('consulta e exibe o balancete em tabela quando balanceado', async () => {
    const user = userEvent.setup();
    mockFetchOnce([
      linha({ contaId: 'a', codigoConta: '1.1.1', totalDebitos: 100, totalCreditos: 0 }),
      linha({ contaId: 'b', codigoConta: '2.1.1', totalDebitos: 0, totalCreditos: 100 }),
    ]);
    renderComAuth(<BalancetePage />, PERMS);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    expect(await screen.findByRole('table')).toHaveAccessibleName(/Balancete de/i);
    expect(screen.queryByText('Balancete desbalanceado')).not.toBeInTheDocument();
  });

  it('destaca quando a soma de débitos difere da soma de créditos', async () => {
    const user = userEvent.setup();
    mockFetchOnce([
      linha({ contaId: 'a', codigoConta: '1.1.1', totalDebitos: 100, totalCreditos: 0 }),
      linha({ contaId: 'b', codigoConta: '2.1.1', totalDebitos: 0, totalCreditos: 80 }),
    ]);
    renderComAuth(<BalancetePage />, PERMS);

    await user.click(screen.getByRole('button', { name: /^Consultar$/i }));

    await waitFor(() => expect(screen.getByText('Balancete desbalanceado')).toBeInTheDocument());
  });
});
