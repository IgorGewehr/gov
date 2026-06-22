// Teste do modal de efetivação de pagamento. Render + 1 interação (validação do campo
// obrigatório de data ao submeter vazio).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { EfetuarPagamentoFormModal } from './EfetuarPagamentoFormModal';

describe('EfetuarPagamentoFormModal', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe o título e o aviso de fechamento', () => {
    renderWithProviders(
      <EfetuarPagamentoFormModal open onClose={() => {}} folhaId="f-1" />,
    );
    expect(
      screen.getByRole('dialog', { name: /Efetuar pagamento da folha/i }),
    ).toBeInTheDocument();
  });

  it('valida a data de pagamento obrigatória ao submeter vazio (interação)', async () => {
    const user = userEvent.setup();
    const fetchSpy = vi.spyOn(globalThis, 'fetch');
    renderWithProviders(
      <EfetuarPagamentoFormModal open onClose={() => {}} folhaId="f-1" />,
    );

    await user.click(screen.getByRole('button', { name: /Efetuar pagamento/i }));

    expect(await screen.findByText('Informe a data do pagamento.')).toBeInTheDocument();
    expect(fetchSpy).not.toHaveBeenCalled();
  });
});
