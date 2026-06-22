// Teste (Vitest + Testing Library) da tela de consulta de Fornecedor (query
// ListarFornecedoresImpedidos). Cobre os estados que toda tela de consulta deve
// ter: sucesso (tabela de impedidos), vazio (nenhum impedido) e erro (falha na
// API). A tela auto-consulta os impedidos na data de hoje ao montar; o fetch
// global e mockado para isolar da rede.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { FornecedorListPage } from './FornecedorListPage';
import type { FornecedorResumo } from './fornecedor.api';

const PERMS = ['administracao.ver', 'administracao.gerenciar'];

function renderWithProviders(ui: Parameters<typeof renderComAuth>[0]) {
  return renderComAuth(ui, PERMS);
}

const FORNECEDOR: FornecedorResumo = {
  id: '44444444-4444-4444-4444-444444444444',
  cnpj: '12.345.678/0001-90',
  razaoSocial: 'Construtora Exemplo Ltda',
  situacao: 'Sancionado',
  nivelCadastralSICAF: 'RegularidadeFiscalNivel3',
};

function mockFetchOnce(body: unknown, status = 200): void {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    }),
  );
}

describe('FornecedorListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista os fornecedores impedidos em tabela', async () => {
    mockFetchOnce([FORNECEDOR]);
    renderWithProviders(<FornecedorListPage />);

    expect(await screen.findByText('Construtora Exemplo Ltda')).toBeInTheDocument();
    expect(screen.getByText('12.345.678/0001-90')).toBeInTheDocument();
    expect(screen.getByText('Sancionado')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando nao ha fornecedores impedidos', async () => {
    mockFetchOnce([]);
    renderWithProviders(<FornecedorListPage />);

    expect(await screen.findByText('Nenhum fornecedor impedido')).toBeInTheDocument();
  });

  it('mostra o estado de erro quando a consulta falha', async () => {
    mockFetchOnce({ title: 'Internal Server Error' }, 500);
    renderWithProviders(<FornecedorListPage />);

    expect(
      await screen.findByText(/Erro ao processar a solicitação|Internal Server Error/i),
    ).toBeInTheDocument();
  });

  it('abre o formulario ao clicar em "Cadastrar fornecedor" (acao gated)', async () => {
    const user = userEvent.setup();
    mockFetchOnce([FORNECEDOR]);
    renderWithProviders(<FornecedorListPage />);

    await user.click(await screen.findByRole('button', { name: /Cadastrar fornecedor/i }));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
  });
});
