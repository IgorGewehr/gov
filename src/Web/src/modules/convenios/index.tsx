// Modulo Convenios — DOIS fluxos do Bounded Context Convenios (CLAUDE.md §1.2):
//   (A) Convenios federais RECEBIDOS (transferencias voluntarias — Dec. 11.531/2023);
//   (B) Parcerias com OSC / MROSC (Lei 13.019/2014).
// Segue o padrao-ouro (tributos/protocolo): cada PAGINA via React.lazy -> chunk
// separado; o <Suspense> do AppLayout cobre o fallback. A sub-navegacao (2 abas)
// vive em ConveniosSubNav, mantendo UMA entrada na Sidebar (gated por convenios.ver).
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const ConvenioListPage = lazy(() =>
  import('./recebidos/ConvenioListPage').then((m) => ({ default: m.ConvenioListPage })),
);
const ConvenioDetailPage = lazy(() =>
  import('./recebidos/ConvenioDetailPage').then((m) => ({ default: m.ConvenioDetailPage })),
);
const ParceriaListPage = lazy(() =>
  import('./parcerias/ParceriaListPage').then((m) => ({ default: m.ParceriaListPage })),
);
const ParceriaDetailPage = lazy(() =>
  import('./parcerias/ParceriaDetailPage').then((m) => ({ default: m.ParceriaDetailPage })),
);

const MODULE: ModuleDefinition = {
  id: 'convenios',
  nav: {
    label: 'Convênios',
    path: '/convenios',
    icon: 'fas fa-handshake',
    // Visibilidade do item na Sidebar gated por convenios.ver (gating de UI; o
    // backend continua a fonte da verdade da autorizacao — CLAUDE.md §6).
    permissions: ['convenios.ver'],
  },
  routes: [
    {
      path: 'convenios',
      children: [
        // Fluxo A — Convenios federais RECEBIDOS.
        { index: true, element: <ConvenioListPage /> },
        { path: 'recebidos/:id', element: <ConvenioDetailPage /> },
        // Fluxo B — Parcerias OSC (MROSC).
        { path: 'parcerias', element: <ParceriaListPage /> },
        { path: 'parcerias/:id', element: <ParceriaDetailPage /> },
      ],
    },
  ],
};

export default MODULE;
