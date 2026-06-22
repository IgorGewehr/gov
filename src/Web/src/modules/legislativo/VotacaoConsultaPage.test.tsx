// Teste (Vitest + Testing Library) da tela de consulta de Votacao. Cobre o estado
// inicial e a interacao de abrir o formulario de inicio de votacao (gated por <Can>).
import { describe, it, expect, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { renderComAuth } from '../../test/renderComAuth';
import { VotacaoConsultaPage } from './VotacaoConsultaPage';

const PERMS = ['legislativo.ver', 'legislativo.gerenciar'];

describe('VotacaoConsultaPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exibe o estado inicial pedindo o identificador', () => {
    renderComAuth(<VotacaoConsultaPage />, PERMS);
    expect(screen.getByText('Consulte uma votação')).toBeInTheDocument();
  });

  it('abre o formulário de início de votação ao clicar na ação', async () => {
    const user = userEvent.setup();
    renderComAuth(<VotacaoConsultaPage />, PERMS);

    await user.click(screen.getByRole('button', { name: /Iniciar votação/i }));

    expect(await screen.findByRole('dialog')).toHaveAccessibleName(/Iniciar votação/i);
  });

  it('não exibe a ação de iniciar sem a permissão de gerenciar', () => {
    renderComAuth(<VotacaoConsultaPage />, ['legislativo.ver']);
    expect(screen.queryByRole('button', { name: /Iniciar votação/i })).not.toBeInTheDocument();
  });
});
