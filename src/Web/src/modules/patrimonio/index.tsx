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
const FrotaPainelPage = lazy(() =>
  import('./frota/FrotaPainelPage').then((m) => ({ default: m.FrotaPainelPage })),
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

// Inventários (Lei 4.320 art. 96)
const InventarioListPage = lazy(() =>
  import('./inventario/InventarioListPage').then((m) => ({ default: m.InventarioListPage })),
);
const InventarioDetailPage = lazy(() =>
  import('./inventario/InventarioDetailPage').then((m) => ({ default: m.InventarioDetailPage })),
);

// Requisições de almoxarifado (Onda 3b) — fila por situação/setor + fluxo de aprovação/atendimento
const RequisicaoListPage = lazy(() =>
  import('./requisicao/RequisicaoListPage').then((m) => ({ default: m.RequisicaoListPage })),
);
const RequisicaoDetailPage = lazy(() =>
  import('./requisicao/RequisicaoDetailPage').then((m) => ({ default: m.RequisicaoDetailPage })),
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
        // Frota — veículos + painel de gestão (Onda 3a)
        { path: 'frota', element: <VeiculoListPage /> },
        { path: 'frota/painel', element: <FrotaPainelPage /> },
        { path: 'frota/veiculos/:id', element: <VeiculoDetailPage /> },
        // Almoxarifado — itens de estoque
        { path: 'estoque', element: <ItemEstoqueListPage /> },
        { path: 'estoque/itens/:id', element: <ItemEstoqueDetailPage /> },
        // Inventários — fluxo do levantamento físico × contábil
        { path: 'inventarios', element: <InventarioListPage /> },
        { path: 'inventarios/:id', element: <InventarioDetailPage /> },
        // Requisições de almoxarifado — fila + fluxo de aprovação/atendimento
        { path: 'requisicoes', element: <RequisicaoListPage /> },
        { path: 'requisicoes/:id', element: <RequisicaoDetailPage /> },
      ],
    },
  ],
};

export default MODULE;
