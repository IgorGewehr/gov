// Helper que padroniza os estados de uma query TanStack: loading -> Spinner,
// error -> Alert (com mensagem do ApiError), sucesso -> children(data).
// Use em páginas de detalhe; para tabelas, o DataTable já trata os estados.
import type { ReactNode } from 'react';
import { Spinner } from './Spinner';
import { Alert } from './Alert';
import { ApiError } from '../../api/problemDetails';

export interface QueryStateProps<T> {
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  data: T | undefined;
  children: (data: T) => ReactNode;
  /** Conteúdo opcional quando data é nullish mas não houve erro. */
  empty?: ReactNode;
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.userMessage;
  if (error instanceof Error) return error.message;
  return 'Ocorreu um erro inesperado. Tente novamente.';
}

export function QueryState<T>({ isLoading, isError, error, data, children, empty }: QueryStateProps<T>) {
  if (isLoading) {
    return (
      <div className="app-center">
        <Spinner label="Carregando…" />
      </div>
    );
  }
  if (isError) {
    return <Alert variant="danger">{errorMessage(error)}</Alert>;
  }
  if (data === undefined || data === null) {
    return <>{empty ?? null}</>;
  }
  return <>{children(data)}</>;
}
