// EntityPicker — picker GENÉRICO e reutilizável de entidade (substitui o GUID digitado
// à mão por busca search-as-you-type + seleção). Não conhece nenhum módulo: o consumidor
// injeta a função de busca (`buscar`, normalmente um hook TanStack Query já existente) e
// os extratores de rótulo. Expõe o id selecionado via `onChange`; o formulário continua
// enviando o Id, sem mudança de contrato.
//
// Acessibilidade (gov.br DS + eMAG / WCAG 2.1 AA):
//   - combobox ARIA (role="combobox" + aria-expanded/-controls/-activedescendant) sobre a
//     lista de resultados (role="listbox" / role="option");
//   - navegação por teclado (↑/↓ percorre, Enter seleciona, Esc fecha/limpa);
//   - estados anunciados via aria-live (carregando / nenhum resultado / erro);
//   - rótulo, ajuda e erro associados via FormField (aria-describedby / aria-invalid).
//
// Padroniza os pickers bespoke já existentes (FornecedorPicker, EstabelecimentoPicker,
// MatriculaPickers, …) num único componente.
import { useId, useMemo, useRef, useState } from 'react';
import type { KeyboardEvent, ReactNode } from 'react';
import { FormField } from './FormField';
import { Input } from './Input';
import { Spinner } from './Spinner';
import { Tag } from './Tag';
import { errorMessage } from './QueryState';
import { useDebouncedValue } from './useDebouncedValue';

/** Estado de uma consulta de busca (compatível com TanStack Query `UseQueryResult`). */
export interface EntityPickerQuery<T> {
  /** Itens retornados (vazio enquanto não resolve). */
  data: T[] | undefined;
  isFetching: boolean;
  isError: boolean;
  error: unknown;
}

export interface EntityPickerProps<T> {
  /** Id da entidade selecionada (ou '' quando nada selecionado). */
  value: string;
  /** Notifica o id selecionado (ou '' ao limpar). O formulário segue enviando o Id. */
  onChange: (id: string) => void;
  /** Rótulo do campo. */
  label: ReactNode;

  /**
   * Hook de busca: recebe o termo (já com debounce e trim) e um flag `habilitado`
   * (false quando o termo é curto ou já há seleção) e devolve o estado da consulta.
   * É CHAMADO COMO HOOK (uma vez por render, em posição estável) — normalmente um hook
   * TanStack Query do módulo (ex.: `useCredores`). O nome `useBuscar` satisfaz as Rules
   * of Hooks do React/eslint.
   */
  useBuscar: (termo: string, habilitado: boolean) => EntityPickerQuery<T>;
  /** Extrai o id de um item. */
  idDe: (item: T) => string;
  /** Rótulo amigável (nome/inscrição) exibido na lista e ao selecionar. */
  rotuloDe: (item: T) => ReactNode;
  /** Texto secundário opcional (ex.: documento/inscrição) à direita do rótulo. */
  detalheDe?: (item: T) => ReactNode;

  required?: boolean;
  error?: string;
  disabled?: boolean;
  /** Texto de ajuda sob o campo. */
  help?: ReactNode;
  /** Placeholder do campo de busca. */
  placeholder?: string;
  /** Mínimo de caracteres para disparar a busca (default: 2). */
  minCaracteres?: number;
  /** Atraso do debounce em ms (default: 300). */
  debounceMs?: number;
}

/**
 * Picker genérico de entidade com busca search-as-you-type. Mantém o contrato do
 * formulário (envia o Id), apenas troca a digitação do GUID por busca + seleção.
 */
export function EntityPicker<T>({
  value,
  onChange,
  label,
  useBuscar,
  idDe,
  rotuloDe,
  detalheDe,
  required,
  error,
  disabled,
  help,
  placeholder = 'Buscar (mín. 2 caracteres)…',
  minCaracteres = 2,
  debounceMs = 300,
}: EntityPickerProps<T>) {
  const [termo, setTermo] = useState('');
  const [selecionado, setSelecionado] = useState<T | null>(null);
  const [ativo, setAtivo] = useState(-1); // índice destacado para navegação por teclado
  const listboxId = useId();
  const inputRef = useRef<HTMLInputElement>(null);

  const termoDebounced = useDebouncedValue(termo, debounceMs).trim();
  const temSelecao = value !== '';
  const habilitado = !disabled && !temSelecao && termoDebounced.length >= minCaracteres;

  const query = useBuscar(termoDebounced, habilitado);
  const itens = useMemo(() => query.data ?? [], [query.data]);
  const aberto = habilitado;

  function selecionar(item: T): void {
    setSelecionado(item);
    setAtivo(-1);
    onChange(idDe(item));
  }

  function limpar(): void {
    setSelecionado(null);
    setAtivo(-1);
    setTermo('');
    onChange('');
    inputRef.current?.focus();
  }

  function aoDigitar(novo: string): void {
    setTermo(novo);
    setAtivo(-1);
    // Digitar invalida a seleção corrente (o usuário está trocando de entidade).
    if (value !== '') {
      setSelecionado(null);
      onChange('');
    }
  }

  function aoTeclar(e: KeyboardEvent<HTMLInputElement>): void {
    if (temSelecao) {
      if (e.key === 'Escape' || e.key === 'Backspace') {
        e.preventDefault();
        limpar();
      }
      return;
    }
    if (!aberto || itens.length === 0) return;
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setAtivo((i) => (i + 1) % itens.length);
        break;
      case 'ArrowUp':
        e.preventDefault();
        setAtivo((i) => (i <= 0 ? itens.length - 1 : i - 1));
        break;
      case 'Enter':
        if (ativo >= 0 && ativo < itens.length) {
          e.preventDefault();
          selecionar(itens[ativo]);
        }
        break;
      case 'Escape':
        e.preventDefault();
        setTermo('');
        setAtivo(-1);
        break;
      default:
        break;
    }
  }

  const semResultado = aberto && !query.isFetching && !query.isError && itens.length === 0;

  return (
    <FormField label={label} required={required} error={error} help={help}>
      {({ id, describedBy, invalid }) => (
        <>
          <Input
            ref={inputRef}
            id={id}
            role="combobox"
            aria-expanded={aberto}
            aria-controls={listboxId}
            aria-autocomplete="list"
            aria-activedescendant={
              aberto && ativo >= 0 ? `${listboxId}-opt-${ativo}` : undefined
            }
            aria-describedby={describedBy}
            invalid={invalid}
            autoComplete="off"
            disabled={disabled || temSelecao}
            value={temSelecao && selecionado ? '' : termo}
            placeholder={placeholder}
            onChange={(e) => aoDigitar(e.target.value)}
            onKeyDown={aoTeclar}
          />

          {/* Estado SELECIONADO: rótulo amigável + ação de trocar (limpar). `selecionado`
              pode ser null se o `value` foi definido externamente (sem passar pelo picker);
              nesse caso mostramos um rótulo neutro, mantendo a ação de trocar. */}
          {temSelecao && (
            <span
              className="mt-1 d-flex align-items-center"
              style={{ gap: '0.5rem' }}
              aria-live="polite"
            >
              <Tag variant="success">
                {selecionado ? rotuloDe(selecionado) : 'Item selecionado'}
              </Tag>
              {selecionado && detalheDe && (
                <span className="text-down-01 text-secondary">{detalheDe(selecionado)}</span>
              )}
              <button
                type="button"
                className="br-button secondary small"
                disabled={disabled}
                onClick={limpar}
              >
                Trocar
              </button>
            </span>
          )}

          {/* Lista de resultados (combobox/listbox ARIA). */}
          {aberto && (
            <div
              className="br-list mt-1"
              id={listboxId}
              role="listbox"
              aria-label="Resultados da busca"
            >
              {query.isFetching && (
                <span className="d-inline-flex align-items-center p-2" aria-live="polite">
                  <Spinner /> <span className="ml-2">Buscando…</span>
                </span>
              )}
              {query.isError && (
                <span className="d-block p-2 text-danger" role="alert">
                  {errorMessage(query.error)}
                </span>
              )}
              {semResultado && (
                <span className="d-block p-2 text-secondary" aria-live="polite">
                  Nenhum resultado encontrado.
                </span>
              )}
              {!query.isFetching &&
                !query.isError &&
                itens.map((item, indice) => (
                  <button
                    key={idDe(item)}
                    id={`${listboxId}-opt-${indice}`}
                    type="button"
                    role="option"
                    aria-selected={indice === ativo}
                    className={`br-item d-flex align-items-center justify-content-between w-100 text-left${
                      indice === ativo ? ' active' : ''
                    }`}
                    onMouseEnter={() => setAtivo(indice)}
                    onClick={() => selecionar(item)}
                  >
                    <span>{rotuloDe(item)}</span>
                    {detalheDe && (
                      <span className="text-down-01 text-secondary">{detalheDe(item)}</span>
                    )}
                  </button>
                ))}
            </div>
          )}
        </>
      )}
    </FormField>
  );
}
