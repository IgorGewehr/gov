// Teste dos modais de ação do Cargo. Render + 1 interação (validação do campo lei de
// extinção obrigatório). Cobre também o render do modal de provimento (sem payload).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { ExtinguirCargoModal, ProverCargoModal } from './CargoAcaoModais';

describe('CargoAcaoModais', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renderiza o modal de provimento com a confirmação', () => {
    renderWithProviders(<ProverCargoModal open onClose={() => {}} cargoId="c-1" />);
    expect(screen.getByRole('dialog', { name: /Prover vaga do cargo/i })).toBeInTheDocument();
  });

  it('valida a lei de extinção obrigatória ao submeter vazio (interação)', async () => {
    const user = userEvent.setup();
    const fetchSpy = vi.spyOn(globalThis, 'fetch');
    renderWithProviders(<ExtinguirCargoModal open onClose={() => {}} cargoId="c-1" />);

    await user.click(screen.getByRole('button', { name: /^Extinguir$/i }));

    expect(
      await screen.findByText('Informe a lei de extinção do cargo.'),
    ).toBeInTheDocument();
    expect(fetchSpy).not.toHaveBeenCalled();
  });
});
