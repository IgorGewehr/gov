// Teste (Vitest + Testing Library) da página de lista/busca de servidores do RH.
// Cobre os estados que TODA tela de lista deve ter: sucesso (tabela paginada) e vazio.
// O fetch global é mockado para isolar a UI da rede (espelha o teste do PADRÃO-OURO).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test/renderWithProviders';
import { AuthProvider } from '../../auth/AuthProvider';
import { ServidoresListPage } from './ServidoresListPage';
import type { PaginaResultado, ServidorResumo } from './api';

function renderPage() {
  return renderWithProviders(
    <AuthProvider>
      <ServidoresListPage />
    </AuthProvider>,
  );
}

const SERVIDORES: ServidorResumo[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    cpf: '***.456.789-**',
    matricula: 'MAT-001',
    nomeServidor: 'Maria da Silva',
    cargoId: '22222222-2222-2222-2222-222222222222',
    regime: 'Rpps',
    situacao: 'EmExercicio',
    dataNomeacao: '2024-02-01',
    dataExercicio: '2024-02-15',
  },
];

function pagina(itens: ServidorResumo[]): PaginaResultado<ServidorResumo> {
  return { itens, total: itens.length, pagina: 1, tamanho: 20 };
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

// Mock por URL: a página de lista também monta o modal de admissão (mesmo fechado),
// que consulta /cargos/com-vagas — esse precisa devolver array; /servidores devolve o
// envelope paginado.
function mockFetch(servidores: PaginaResultado<ServidorResumo>): void {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = typeof input === 'string' ? input : (input as Request).url;
    if (url.includes('/cargos/com-vagas')) {
      return Promise.resolve(json([]));
    }
    return Promise.resolve(json(servidores));
  });
}

describe('ServidoresListPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('lista os servidores em tabela paginada', async () => {
    mockFetch(pagina(SERVIDORES));
    renderPage();

    expect(await screen.findByText('Maria da Silva')).toBeInTheDocument();
    expect(screen.getByText('MAT-001')).toBeInTheDocument();
    expect(screen.getByText('EmExercicio')).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName(/Resultado da busca de servidores/i);
  });

  it('mostra o estado vazio quando a busca não retorna servidores', async () => {
    mockFetch(pagina([]));
    renderPage();

    await waitFor(() =>
      expect(screen.getByText('Nenhum servidor encontrado')).toBeInTheDocument(),
    );
  });

  it('exibe o cabeçalho e os filtros de busca', async () => {
    mockFetch(pagina([]));
    renderPage();

    expect(screen.getByRole('heading', { name: 'Servidores' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Buscar/i })).toBeInTheDocument();
  });
});
