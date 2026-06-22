// Router central. Consome o registry de módulos (cada módulo expõe suas RouteObject
// com páginas em React.lazy). Estrutura:
//   /  (RootProviders: QueryClient + Auth + Toast)
//     ├ /login                    -> público (LoginPage)
//     └ (ProtectedRoute)          -> AppLayout
//          ├ index                -> HomePage
//          ├ <módulos do registry>
//          └ *                    -> NotFoundPage
import { createBrowserRouter } from 'react-router-dom';
import type { RouteObject } from 'react-router-dom';
import { RootProviders } from './app/RootProviders';
import { AppLayout } from './app/shell/AppLayout';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { LoginPage } from './app/pages/LoginPage';
import { HomePage } from './app/pages/HomePage';
import { NotFoundPage } from './app/pages/NotFoundPage';
import { modules } from './modules/registry';

const moduleRoutes: RouteObject[] = modules.flatMap((m) => m.routes);

export const router = createBrowserRouter(
  [
    {
      element: <RootProviders />,
      children: [
        { path: '/login', element: <LoginPage /> },
        {
          element: <ProtectedRoute />,
          children: [
            {
              path: '/',
              element: <AppLayout />,
              children: [
                { index: true, element: <HomePage /> },
                ...moduleRoutes,
                { path: '*', element: <NotFoundPage /> },
              ],
            },
          ],
        },
      ],
    },
  ],
  {
    // Opt-in antecipado ao comportamento do React Router v7.
    future: { v7_relativeSplatPath: true },
  },
);
