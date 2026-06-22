// Tabela de dados gov.br (br-table) tipada e acessível: <caption>, <th scope="col">,
// estados loading/vazio/erro e ordenação por coluna (client-side) com aria-sort.
import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Spinner } from './Spinner';
import { Alert } from './Alert';
import { EmptyState } from './EmptyState';

export interface Column<T> {
  /** Chave estável da coluna. */
  key: string;
  header: ReactNode;
  /** Render da célula. */
  render: (row: T) => ReactNode;
  /** Habilita ordenação; informa o valor comparável da linha. */
  sortAccessor?: (row: T) => string | number;
  /** Alinhamento do conteúdo. */
  align?: 'start' | 'center' | 'end';
}

export interface DataTableProps<T> {
  caption: string;
  columns: Column<T>[];
  rows: T[] | undefined;
  /** Chave única por linha. */
  rowKey: (row: T) => string;
  loading?: boolean;
  /** Mensagem de erro; exibe Alert no lugar da tabela. */
  error?: string | null;
  /** Conteúdo do estado vazio (padrão: EmptyState genérico). */
  empty?: ReactNode;
  /** Ação ao clicar na linha (torna a linha interativa). */
  onRowClick?: (row: T) => void;
}

type SortDirection = 'asc' | 'desc';

export function DataTable<T>({
  caption,
  columns,
  rows,
  rowKey,
  loading = false,
  error = null,
  empty,
  onRowClick,
}: DataTableProps<T>) {
  const [sort, setSort] = useState<{ key: string; direction: SortDirection } | null>(null);

  const sortedRows = useMemo(() => {
    if (!rows || !sort) return rows ?? [];
    const column = columns.find((c) => c.key === sort.key);
    if (!column?.sortAccessor) return rows;
    const factor = sort.direction === 'asc' ? 1 : -1;
    return [...rows].sort((a, b) => {
      const va = column.sortAccessor!(a);
      const vb = column.sortAccessor!(b);
      if (va < vb) return -1 * factor;
      if (va > vb) return 1 * factor;
      return 0;
    });
  }, [rows, sort, columns]);

  function toggleSort(key: string): void {
    setSort((current) => {
      if (current?.key !== key) return { key, direction: 'asc' };
      if (current.direction === 'asc') return { key, direction: 'desc' };
      return null;
    });
  }

  if (error) {
    return <Alert variant="danger">{error}</Alert>;
  }

  if (loading) {
    return (
      <div className="app-center">
        <Spinner label="Carregando dados…" />
      </div>
    );
  }

  if (!sortedRows || sortedRows.length === 0) {
    return <>{empty ?? <EmptyState title="Nenhum registro encontrado." />}</>;
  }

  return (
    <div className="br-table">
      <table>
        <caption>{caption}</caption>
        <thead>
          <tr>
            {columns.map((column) => {
              const sortable = Boolean(column.sortAccessor);
              const active = sort?.key === column.key;
              const ariaSort: React.AriaAttributes['aria-sort'] = active
                ? sort?.direction === 'asc'
                  ? 'ascending'
                  : 'descending'
                : sortable
                  ? 'none'
                  : undefined;
              return (
                <th key={column.key} scope="col" aria-sort={ariaSort} className={`text-${column.align ?? 'start'}`}>
                  {sortable ? (
                    <button
                      type="button"
                      className="br-button tertiary small"
                      onClick={() => toggleSort(column.key)}
                    >
                      {column.header}{' '}
                      <i
                        className={`fas ${active ? (sort?.direction === 'asc' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort'}`}
                        aria-hidden="true"
                      />
                    </button>
                  ) : (
                    column.header
                  )}
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody>
          {sortedRows.map((row) => (
            <tr
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              style={onRowClick ? { cursor: 'pointer' } : undefined}
            >
              {columns.map((column) => (
                <td key={column.key} className={`text-${column.align ?? 'start'}`}>
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
