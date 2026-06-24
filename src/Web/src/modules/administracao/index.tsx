// Módulo Administracao — Compras, Licitações (Lei 14.133/2021), Fornecedores e
// Contratos. Segue o PADRÃO-OURO de src/modules/tributos/index.tsx:
//   <agregado>.api.ts        -> DTOs + query keys + hooks TanStack Query
//   <Agregado>ListPage.tsx   -> consulta/lista (DataTable, estados)
//   <Agregado>DetailPage.tsx -> detalhe (useQuery + QueryState)
//   <Agregado>FormModal.tsx  -> formulário (controlado, renderizado nas páginas)
//   index.tsx                -> ModuleDefinition { routes, nav } com páginas lazy
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
// Os *FormModal/*ActionModals são componentes controlados (open/onClose) usados
// DENTRO das List/Detail pages — não são rotas próprias.
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const LicitacaoListPage = lazy(() =>
  import('./licitacao/LicitacaoListPage').then((m) => ({ default: m.LicitacaoListPage })),
);
const LicitacaoDetailPage = lazy(() =>
  import('./licitacao/LicitacaoDetailPage').then((m) => ({ default: m.LicitacaoDetailPage })),
);

const ContratoListPage = lazy(() =>
  import('./contrato/ContratoListPage').then((m) => ({ default: m.ContratoListPage })),
);
const ContratoDetailPage = lazy(() =>
  import('./contrato/ContratoDetailPage').then((m) => ({ default: m.ContratoDetailPage })),
);

const FornecedorListPage = lazy(() =>
  import('./fornecedor/FornecedorListPage').then((m) => ({ default: m.FornecedorListPage })),
);
const FornecedorDetailPage = lazy(() =>
  import('./fornecedor/FornecedorDetailPage').then((m) => ({ default: m.FornecedorDetailPage })),
);

const CatalogoListPage = lazy(() =>
  import('./catalogo/CatalogoListPage').then((m) => ({ default: m.CatalogoListPage })),
);
const AtaListPage = lazy(() => import('./ata/AtaListPage').then((m) => ({ default: m.AtaListPage })));
const AtaDetailPage = lazy(() => import('./ata/AtaDetailPage').then((m) => ({ default: m.AtaDetailPage })));
const PcaPage = lazy(() => import('./pca/PcaPage').then((m) => ({ default: m.PcaPage })));

const MODULE: ModuleDefinition = {
  id: 'administracao',
  nav: {
    label: 'Compras e Licitações',
    path: '/administracao',
    icon: 'fas fa-file-signature',
  },
  routes: [
    {
      path: 'administracao',
      children: [
        { index: true, element: <LicitacaoListPage /> },
        { path: 'licitacoes', element: <LicitacaoListPage /> },
        { path: 'licitacoes/:id', element: <LicitacaoDetailPage /> },
        { path: 'contratos', element: <ContratoListPage /> },
        { path: 'contratos/:id', element: <ContratoDetailPage /> },
        { path: 'fornecedores', element: <FornecedorListPage /> },
        { path: 'fornecedores/:id', element: <FornecedorDetailPage /> },
        { path: 'catalogo', element: <CatalogoListPage /> },
        { path: 'atas', element: <AtaListPage /> },
        { path: 'atas/:id', element: <AtaDetailPage /> },
        { path: 'pca', element: <PcaPage /> },
      ],
    },
  ],
};

export default MODULE;
