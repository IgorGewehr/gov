// Raiz do realm CIDADAO (sibling de RootProviders do admin). Provê o stack do portal SEM
// o AuthProvider do back-office: QueryClient + Toast PROPRIOS + a sessao do cidadao
// (CidadaoAuthProvider) + Suspense para as paginas lazy. Mantem os dois realms isolados
// (token, cache de query e notificacoes separados) — um nao derruba o outro.
import { Suspense } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { Outlet } from 'react-router-dom';
import { ToastProvider, Spinner } from '../../components/ui';
import { createQueryClient } from '../../api/queryClient';
import { CidadaoAuthProvider } from './CidadaoAuthProvider';

const queryClient = createQueryClient();

export function CidadaoRoot() {
  return (
    <QueryClientProvider client={queryClient}>
      <CidadaoAuthProvider>
        <ToastProvider>
          <Suspense
            fallback={
              <div className="app-center" style={{ minHeight: '60vh' }}>
                <Spinner label="Carregando…" />
              </div>
            }
          >
            <Outlet />
          </Suspense>
        </ToastProvider>
      </CidadaoAuthProvider>
    </QueryClientProvider>
  );
}
