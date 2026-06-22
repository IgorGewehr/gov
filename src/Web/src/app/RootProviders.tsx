// Elemento raiz do router. Fica DENTRO do RouterProvider para que AuthProvider
// possa usar hooks de roteamento (useNavigate). Provê: QueryClient (TanStack),
// autenticação (sessão) e notificações (toast) a toda a árvore de rotas.
import { QueryClientProvider } from '@tanstack/react-query';
import { Outlet } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthProvider';
import { ToastProvider } from '../components/ui';
import { createQueryClient } from '../api/queryClient';

const queryClient = createQueryClient();

export function RootProviders() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ToastProvider>
          <Outlet />
        </ToastProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}
