// Teste (Vitest + Testing Library) da pagina de lista de familias (SUAS).
// Cobre os estados que TODA tela de lista deve ter: pre-consulta, sucesso (tabela)
// e vazio. A busca e sob demanda (por territorio), entao o teste preenche o campo
// e submete o formulario. O fetch global e mockado para isolar a UI da rede
// (espelha o teste do PADRAO-OURO de Tributos/RH).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderComAuth } from '../../../test/renderComAuth';
import { clearAccessToken } from '../../../api/authToken';
import { FamiliaListPage } from './FamiliaListPage';
import type { FamiliaResumo } from './familia.api';

const FAMILIAS: FamiliaResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    nisMascarado: '***.*****.**-1',
    unidadeAtendimentoId: '22222222-2222-2222-2222-222222222222',
    territorio: 'Território Centro',
    rendaPerCapita: 218.5,
    situacao: 'Referenciada',
    dataReferenciamento: '2025-02-01',
    dataUltimaAtualizacaoCadastral: '2025-02-01',
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

async function consultarTerritorio(): Promise<void> {
  const user = userEvent.setup();
  await user.type(
    screen.getByLabelText(/Território de cobertura/i),
    'Território Centro',
  );
  await user.click(screen.getByRole('button', { name: /Consultar/i }));
}

describe('FamiliaListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('exibe o estado inicial (faça uma consulta) e o cabeçalho com a ação de referenciar', () => {
    mockFetch([]);
    renderComAuth(<FamiliaListPage />);

    expect(screen.getByRole('heading', { name: 'Famílias (SUAS)' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Referenciar família/i })).toBeInTheDocument();
    expect(screen.getByText('Faça uma consulta')).toBeInTheDocument();
  });

  it('lista as famílias do território consultado em tabela', async () => {
    mockFetch(FAMILIAS);
    renderComAuth(<FamiliaListPage />);

    await consultarTerritorio();

    expect(await screen.findByText('***.*****.**-1')).toBeInTheDocument();
    const tabela = screen.getByRole('table');
    expect(tabela).toHaveAccessibleName(/Famílias do território/i);
    // "Referenciada" aparece no filtro (option) e na Tag da linha: escopo na tabela.
    const { getByText: getByTextNaTabela } = within(tabela);
    expect(getByTextNaTabela('Referenciada')).toBeInTheDocument();
  });

  it('mostra o estado vazio quando o território não tem famílias', async () => {
    mockFetch([]);
    renderComAuth(<FamiliaListPage />);

    await consultarTerritorio();

    await waitFor(() =>
      expect(screen.getByText('Nenhuma família encontrada')).toBeInTheDocument(),
    );
  });

  it('oculta a ação de referenciar quando o usuário não tem assistenciasocial.gerenciar', () => {
    mockFetch([]);
    // Apenas leitura: o botão de comando deve ser escondido pelo <Can>.
    renderComAuth(<FamiliaListPage />, ['assistenciasocial.ver']);

    expect(screen.queryByRole('button', { name: /Referenciar família/i })).not.toBeInTheDocument();
  });
});
