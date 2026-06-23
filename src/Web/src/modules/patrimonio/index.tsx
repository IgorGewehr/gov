// Módulo Patrimonio — Bens (Tombamento/Depreciação), Frota (Veículos) e
// Almoxarifado (Itens de Estoque). Segue o PADRÃO-OURO de src/modules/tributos:
//   *.api.ts                -> DTOs + query keys + hooks TanStack Query
//   <Recurso>ListPage.tsx   -> consulta/lista (DataTable, estados)
//   <Recurso>DetailPage.tsx -> detalhe (useQuery + QueryState)
//   <Recurso>*Modal.tsx     -> formulários/ações (useMutation), renderizados nas páginas
//   index.tsx               -> ModuleDefinition { routes, nav } com páginas em React.lazy
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
// Os *Modal são componentes controlados (open/onClose) renderizados dentro das
// páginas — não são rotas.
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

// Bens patrimoniais (índice do módulo)
const BemPatrimonialListPage = lazy(() =>
  import('./bempatrimonial/BemPatrimonialListPage').then((m) => ({
    default: m.BemPatrimonialListPage,
  })),
);
const BemPatrimonialDetailPage = lazy(() =>
  import('./bempatrimonial/BemPatrimonialDetailPage').then((m) => ({
    default: m.BemPatrimonialDetailPage,
  })),
);

// Frota (veículos)
const VeiculoListPage = lazy(() =>
  import('./veiculo/VeiculoListPage').then((m) => ({ default: m.VeiculoListPage })),
);
const VeiculoDetailPage = lazy(() =>
  import('./veiculo/VeiculoDetailPage').then((m) => ({ default: m.VeiculoDetailPage })),
);

// Almoxarifado (itens de estoque)
const ItemEstoqueListPage = lazy(() =>
  import('./itemestoque/ItemEstoqueListPage').then((m) => ({ default: m.ItemEstoqueListPage })),
);
const ItemEstoqueDetailPage = lazy(() =>
  import('./itemestoque/ItemEstoqueDetailPage').then((m) => ({
    default: m.ItemEstoqueDetailPage,
  })),
);

const MODULE: ModuleDefinition = {
  id: 'patrimonio',
  nav: {
    label: 'Patrimônio',
    path: '/patrimonio',
    icon: 'fas fa-boxes-stacked',
  },
  routes: [
    {
      path: 'patrimonio',
      children: [
        // Bens patrimoniais — lista é o índice do módulo
        { index: true, element: <BemPatrimonialListPage /> },
        { path: 'bens/:id', element: <BemPatrimonialDetailPage /> },
        // Frota — veículos
        { path: 'frota', element: <VeiculoListPage /> },
        { path: 'frota/veiculos/:id', element: <VeiculoDetailPage /> },
        // Almoxarifado — itens de estoque
        { path: 'estoque', element: <ItemEstoqueListPage /> },
        { path: 'estoque/itens/:id', element: <ItemEstoqueDetailPage /> },
      ],
    },
  ],
};

export default MODULE;
