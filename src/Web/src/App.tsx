// Raiz da SPA: provê o router (que por sua vez monta QueryClient/Auth/Toast no
// elemento RootProviders) — mantém os providers que precisam de hooks de rota
// dentro da árvore do RouterProvider.
import { RouterProvider } from 'react-router-dom';
import { router } from './routes';

export function App() {
  return <RouterProvider router={router} />;
}
