// Helper de testes: renderiza a UI dentro dos providers necessários (QueryClient,
// MemoryRouter, AuthProvider, ToastProvider). QueryClient com retry desligado para
// testes determinísticos. O AuthProvider entra aqui porque componentes transversais
// (ex.: SubNav, que faz gating de abas por permissão via useAuth) são montados por
// muitas telas; sem ele, qualquer página com sub-navegação quebraria nos testes. Ele
// é seguro em teste: deriva a sessão do token em memória (default: anônimo) e não faz
// rede no mount. Quem precisa de sessão autenticada usa renderComAuth (que define o
// token antes de renderizar).
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { render } from '@testing-library/react';
import type { ReactElement, ReactNode } from 'react';
import { AuthProvider } from '../auth/AuthProvider';
import { ToastProvider } from '../components/ui';

export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  });
}

export interface RenderOptions {
  route?: string;
  queryClient?: QueryClient;
}

export function renderWithProviders(ui: ReactElement, options: RenderOptions = {}) {
  const queryClient = options.queryClient ?? createTestQueryClient();
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter
          initialEntries={[options.route ?? '/']}
          future={{ v7_startTransition: true, v7_relativeSplatPath: true }}
        >
          <AuthProvider>
            <ToastProvider>{children}</ToastProvider>
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  }
  return { queryClient, ...render(ui, { wrapper: Wrapper }) };
}
