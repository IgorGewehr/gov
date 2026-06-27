// Teste do EntityPicker (picker genérico de entidade com busca search-as-you-type).
// Exercita: gating por mínimo de caracteres, debounce, lista de resultados, seleção
// (devolve o id via onChange), estado "nenhum resultado", erro e acessibilidade
// (role combobox/listbox/option). A busca é ligada a um hook TanStack Query real sobre
// um `fetch` mockado — espelha o padrão dos testes de página do projeto.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { act, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test/renderWithProviders';
import { EntityPicker } from './EntityPicker';
import type { EntityPickerQuery } from './EntityPicker';

interface Pessoa {
  id: string;
  nome: string;
  documento: string;
}

const PESSOAS: Pessoa[] = [
  { id: 'id-ana', nome: 'Ana Maria', documento: '111.111.111-11' },
  { id: 'id-bruno', nome: 'Bruno Alves', documento: '222.222.222-22' },
];

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });
}

/** Hook de busca real (useQuery) ligado ao fetch mockado: GET /fake?termo= -> Pessoa[]. */
function useBuscarPessoa(termo: string, habilitado: boolean): EntityPickerQuery<Pessoa> {
  const query = useQuery({
    queryKey: ['fake-pessoas', termo],
    queryFn: async () => {
      const r = await fetch(`/fake?termo=${encodeURIComponent(termo)}`);
      return (await r.json()) as Pessoa[];
    },
    enabled: habilitado,
  });
  return { data: query.data, isFetching: query.isFetching, isError: query.isError, error: query.error };
}

/** Harness controlado: mantém o `value` selecionado e expõe a última chamada de onChange. */
function Harness({ onPick }: { onPick: (id: string) => void }) {
  const [value, setValue] = useState('');
  return (
    <EntityPicker<Pessoa>
      value={value}
      onChange={(id) => {
        setValue(id);
        onPick(id);
      }}
      label="Identificador da pessoa"
      debounceMs={10}
      useBuscar={useBuscarPessoa}
      idDe={(p) => p.id}
      rotuloDe={(p) => p.nome}
      detalheDe={(p) => p.documento}
    />
  );
}

describe('EntityPicker', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => vi.restoreAllMocks());

  it('renderiza um combobox acessível com o rótulo associado', () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(PESSOAS));
    renderWithProviders(<Harness onPick={() => {}} />);
    const combo = screen.getByRole('combobox', { name: /Identificador da pessoa/i });
    expect(combo).toBeInTheDocument();
    expect(combo).toHaveAttribute('aria-expanded', 'false');
  });

  it('não busca abaixo do mínimo de caracteres e busca acima dele', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(PESSOAS));
    const user = userEvent.setup();
    renderWithProviders(<Harness onPick={() => {}} />);

    const combo = screen.getByRole('combobox', { name: /Identificador da pessoa/i });
    await user.type(combo, 'a'); // 1 caractere < mínimo (2) -> sem busca
    // Deixa o debounce (10ms) estabilizar dentro de act e confirma que nada foi buscado.
    await act(async () => {
      await new Promise((r) => setTimeout(r, 30));
    });
    expect(fetchSpy).not.toHaveBeenCalled();

    await user.type(combo, 'na'); // agora "ana" (>= 2) -> busca
    expect(await screen.findByRole('option', { name: /Ana Maria/i })).toBeInTheDocument();
    expect(fetchSpy).toHaveBeenCalled();
  });

  it('seleciona um resultado e devolve o id via onChange', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(PESSOAS));
    const onPick = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<Harness onPick={onPick} />);

    await user.type(screen.getByRole('combobox'), 'ana');
    const opcao = await screen.findByRole('option', { name: /Ana Maria/i });
    await user.click(opcao);

    expect(onPick).toHaveBeenLastCalledWith('id-ana');
    // Após selecionar, mostra o rótulo amigável e some a lista.
    await waitFor(() => expect(screen.queryByRole('listbox')).not.toBeInTheDocument());
    expect(screen.getByText('Ana Maria')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Trocar/i })).toBeInTheDocument();
  });

  it('seleciona pelo teclado (seta para baixo + Enter)', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(PESSOAS));
    const onPick = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<Harness onPick={onPick} />);

    const combo = screen.getByRole('combobox');
    await user.type(combo, 'a maria');
    await screen.findByRole('option', { name: /Ana Maria/i });
    await user.keyboard('{ArrowDown}{Enter}');

    expect(onPick).toHaveBeenLastCalledWith('id-ana');
  });

  it('mostra "Nenhum resultado encontrado" quando a busca volta vazia', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse([]));
    const user = userEvent.setup();
    renderWithProviders(<Harness onPick={() => {}} />);

    await user.type(screen.getByRole('combobox'), 'zzz');
    expect(await screen.findByText(/Nenhum resultado encontrado/i)).toBeInTheDocument();
  });

  it('exibe o erro quando a busca falha', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response('erro', { status: 500, headers: { 'content-type': 'application/json' } }),
    );
    const user = userEvent.setup();
    renderWithProviders(<Harness onPick={() => {}} />);

    await user.type(screen.getByRole('combobox'), 'ana');
    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });

  it('permite trocar a seleção (volta a buscar)', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(PESSOAS));
    const onPick = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<Harness onPick={onPick} />);

    await user.type(screen.getByRole('combobox'), 'ana');
    await user.click(await screen.findByRole('option', { name: /Ana Maria/i }));
    expect(screen.getByRole('button', { name: /Trocar/i })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Trocar/i }));
    expect(onPick).toHaveBeenLastCalledWith('');
    // Campo de busca volta a aparecer habilitado.
    expect(screen.getByRole('combobox')).toBeEnabled();
  });
});
