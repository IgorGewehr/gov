// QueryClient central do TanStack Query. Defaults conservadores para ERP:
// não refetch ao focar a janela (evita ruído), 1 retry e staleTime curto.
import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './problemDetails';

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Não reintentar erros de cliente (4xx, inclusive 401/403/404).
          if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false;
          return failureCount < 1;
        },
      },
      mutations: {
        retry: false,
      },
    },
  });
}
