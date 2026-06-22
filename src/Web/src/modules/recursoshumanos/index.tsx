// Módulo Recursos Humanos — segue o PADRÃO-OURO de src/modules/tributos:
//   api.ts                      -> DTOs + query keys + hooks TanStack Query
//   <Agregado>ListPage.tsx      -> consulta/lista (DataTable/QueryState, estados)
//   <Agregado>DetailPage.tsx    -> detalhe (useQuery + QueryState)
//   <Acao>FormModal.tsx         -> formulário (useMutation + validação por campo)
//   index.tsx                   -> ModuleDefinition { routes, nav } com páginas em React.lazy
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
//
// Agregados: Servidor (índice), Cargo e FolhaDePagamento. A navegação entre eles
// é interna (RhSubNav), mantendo UMA entrada de módulo na Sidebar.
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const ServidoresListPage = lazy(() =>
  import('./ServidoresListPage').then((m) => ({ default: m.ServidoresListPage })),
);
const ServidorDetailPage = lazy(() =>
  import('./ServidorDetailPage').then((m) => ({ default: m.ServidorDetailPage })),
);
const CargosListPage = lazy(() =>
  import('./CargosListPage').then((m) => ({ default: m.CargosListPage })),
);
const CargoDetailPage = lazy(() =>
  import('./CargoDetailPage').then((m) => ({ default: m.CargoDetailPage })),
);
const RubricasListPage = lazy(() =>
  import('./RubricasListPage').then((m) => ({ default: m.RubricasListPage })),
);
const TabelasLegaisPage = lazy(() =>
  import('./TabelasLegaisPage').then((m) => ({ default: m.TabelasLegaisPage })),
);
const FolhaListPage = lazy(() =>
  import('./FolhaListPage').then((m) => ({ default: m.FolhaListPage })),
);
const FolhaDetailPage = lazy(() =>
  import('./FolhaDetailPage').then((m) => ({ default: m.FolhaDetailPage })),
);
const PontoListPage = lazy(() =>
  import('./PontoListPage').then((m) => ({ default: m.PontoListPage })),
);
const PontoServidorPage = lazy(() =>
  import('./PontoServidorPage').then((m) => ({ default: m.PontoServidorPage })),
);

const MODULE: ModuleDefinition = {
  id: 'recursoshumanos',
  nav: {
    label: 'Recursos Humanos',
    path: '/recursoshumanos',
    icon: 'fas fa-users',
  },
  routes: [
    {
      path: 'recursoshumanos',
      children: [
        { index: true, element: <ServidoresListPage /> },
        { path: 'servidores/:matricula', element: <ServidorDetailPage /> },
        { path: 'cargos', element: <CargosListPage /> },
        { path: 'cargos/:id', element: <CargoDetailPage /> },
        { path: 'rubricas', element: <RubricasListPage /> },
        { path: 'tabelas-legais', element: <TabelasLegaisPage /> },
        { path: 'folhas', element: <FolhaListPage /> },
        { path: 'folhas/:folhaId', element: <FolhaDetailPage /> },
        { path: 'ponto', element: <PontoListPage /> },
        { path: 'ponto/:servidorId', element: <PontoServidorPage /> },
      ],
    },
  ],
};

export default MODULE;
