// Teste dos modais de ação do Servidor. Render + 1 interação (validação do campo data
// de posse obrigatório ao submeter vazio).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { DesligarServidorModal, RegistrarPosseModal } from './ServidorAcaoModais';

describe('ServidorAcaoModais', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renderiza o modal de desligamento com aviso terminal', () => {
    renderWithProviders(<DesligarServidorModal open onClose={() => {}} servidorId="s-1" />);
    expect(screen.getByRole('dialog', { name: /Desligar servidor/i })).toBeInTheDocument();
  });

  it('valida a data de posse obrigatória ao submeter vazio (interação)', async () => {
    const user = userEvent.setup();
    const fetchSpy = vi.spyOn(globalThis, 'fetch');
    renderWithProviders(<RegistrarPosseModal open onClose={() => {}} servidorId="s-1" />);

    await user.click(screen.getByRole('button', { name: /^Registrar$/i }));

    expect(await screen.findByText('Informe a data de posse.')).toBeInTheDocument();
    expect(fetchSpy).not.toHaveBeenCalled();
  });
});
