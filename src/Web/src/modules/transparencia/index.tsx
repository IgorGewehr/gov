// Módulo Transparência (LAI / TCE-RS SIAPC-PAD / SICONFI-MSC). Segue o PADRÃO-OURO de
// src/modules/tributos: páginas via React.lazy (code-splitting), expondo { routes, nav }
// no ModuleDefinition consumido pelo registry central. Agregados:
//   RemessaTce        -> remessas ao TCE-RS (SIAPC/PAD)
//   DeclaracaoFiscal  -> demonstrativos ao SICONFI (MSC/RREO/RGF/DCA)
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const RemessaListPage = lazy(() =>
  import('./RemessaListPage').then((m) => ({ default: m.RemessaListPage })),
);
const RemessaDetailPage = lazy(() =>
  import('./RemessaDetailPage').then((m) => ({ default: m.RemessaDetailPage })),
);
const DeclaracaoFiscalListPage = lazy(() =>
  import('./DeclaracaoFiscalListPage').then((m) => ({ default: m.DeclaracaoFiscalListPage })),
);
const DeclaracaoFiscalDetailPage = lazy(() =>
  import('./DeclaracaoFiscalDetailPage').then((m) => ({ default: m.DeclaracaoFiscalDetailPage })),
);

const MODULE: ModuleDefinition = {
  id: 'transparencia',
  nav: { label: 'Transparência', path: '/transparencia', icon: 'fas fa-eye' },
  routes: [
    {
      path: 'transparencia',
      children: [
        { index: true, element: <RemessaListPage /> },
        { path: 'remessas-tce/:id', element: <RemessaDetailPage /> },
        { path: 'declaracoes-fiscais', element: <DeclaracaoFiscalListPage /> },
        { path: 'declaracoes-fiscais/:id', element: <DeclaracaoFiscalDetailPage /> },
      ],
    },
  ],
};

/** Re-exporta as rotas e o nav do módulo (contrato { route, nav } consumido pelo registry). */
export const route = MODULE.routes;
export const nav = MODULE.nav;

export default MODULE;
