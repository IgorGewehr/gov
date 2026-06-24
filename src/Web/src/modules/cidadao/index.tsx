// Portal do Cidadao (ator EXTERNO). NAO entra no registry de modulos do back-office:
// aqueles renderizam dentro do AppLayout (admin), sob o AuthProvider/ProtectedRoute do
// admin. O cidadao tem realm PROPRIO (token, login e shell separados). Por isso este
// modulo expoe um conjunto de RouteObject de NIVEL SUPERIOR (montado direto em routes.tsx,
// fora do ProtectedRoute do admin), e NAO um ModuleDefinition.
//
// Estrutura de rotas (todas sob /portal-cidadao, dentro do CidadaoAuthProvider):
//   /portal-cidadao                 -> CidadaoLoginPage (publica do realm cidadao)
//   /portal-cidadao (CidadaoLayout) -> gated por sessao de cidadao
//        ├ meus-debitos             -> MeusDebitosPage (+ 2a via de DAM)
//        ├ minha-divida-ativa       -> MinhaDividaAtivaPage
//        └ meus-processos           -> MeusProcessosPage
import { lazy } from 'react';
import type { RouteObject } from 'react-router-dom';
import { CidadaoRoot } from './CidadaoRoot';

const CidadaoLoginPage = lazy(() =>
  import('./CidadaoLoginPage').then((m) => ({ default: m.CidadaoLoginPage })),
);
const CidadaoLayout = lazy(() => import('./CidadaoLayout').then((m) => ({ default: m.CidadaoLayout })));
const MeusDebitosPage = lazy(() =>
  import('./MeusDebitosPage').then((m) => ({ default: m.MeusDebitosPage })),
);
const MinhaDividaAtivaPage = lazy(() =>
  import('./MinhaDividaAtivaPage').then((m) => ({ default: m.MinhaDividaAtivaPage })),
);
const MeusProcessosPage = lazy(() =>
  import('./MeusProcessosPage').then((m) => ({ default: m.MeusProcessosPage })),
);

/** Rotas de nivel superior do Portal do Cidadao (sibling de /login, fora do admin). */
export const cidadaoRoutes: RouteObject[] = [
  {
    path: '/portal-cidadao',
    // A raiz do realm cidadao (QueryClient/Toast/sessao proprios) envolve TODAS as rotas
    // do portal (login + area gated), isolada do realm do admin.
    element: <CidadaoRoot />,
    children: [
      { index: true, element: <CidadaoLoginPage /> },
      {
        element: <CidadaoLayout />,
        children: [
          { path: 'meus-debitos', element: <MeusDebitosPage /> },
          { path: 'minha-divida-ativa', element: <MinhaDividaAtivaPage /> },
          { path: 'meus-processos', element: <MeusProcessosPage /> },
        ],
      },
    ],
  },
];
