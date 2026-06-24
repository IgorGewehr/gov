// Módulo Protocolo — processo administrativo eletrônico (CLAUDE.md §4, #6).
// Segue FIELMENTE o padrão-ouro de src/modules/tributos/index.tsx:
//   - cada PÁGINA é carregada via React.lazy -> chunk separado (code-splitting);
//   - o <Suspense> do AppLayout cobre o fallback de carregamento;
//   - os FormModals/AcaoModals são renderizados DENTRO das próprias páginas,
//     portanto não recebem rota própria.
//
// Agregados: Processo (lista principal + detalhe por NUP) e Documento (lista por
// processo + detalhe documentoId aninhado em processos/:processoId/documentos/).
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const ProcessoListPage = lazy(() =>
  import('./processo/ProcessoListPage').then((m) => ({ default: m.ProcessoListPage })),
);
const ProcessoDetailPage = lazy(() =>
  import('./processo/ProcessoDetailPage').then((m) => ({ default: m.ProcessoDetailPage })),
);
const DocumentoListPage = lazy(() =>
  import('./documento/DocumentoListPage').then((m) => ({ default: m.DocumentoListPage })),
);
const DocumentoDetailPage = lazy(() =>
  import('./documento/DocumentoDetailPage').then((m) => ({ default: m.DocumentoDetailPage })),
);
const PlanoClassificacaoPage = lazy(() =>
  import('./arquivistica/PlanoClassificacaoPage').then((m) => ({ default: m.PlanoClassificacaoPage })),
);
const TabelaTemporalidadePage = lazy(() =>
  import('./arquivistica/TabelaTemporalidadePage').then((m) => ({ default: m.TabelaTemporalidadePage })),
);
const DestinacaoPage = lazy(() =>
  import('./arquivistica/DestinacaoPage').then((m) => ({ default: m.DestinacaoPage })),
);

const MODULE: ModuleDefinition = {
  id: 'protocolo',
  nav: {
    label: 'Protocolo',
    path: '/protocolo',
    icon: 'fas fa-folder-open',
  },
  routes: [
    {
      path: 'protocolo',
      children: [
        { index: true, element: <ProcessoListPage /> },
        { path: 'processos/:nup', element: <ProcessoDetailPage /> },
        { path: 'documentos', element: <DocumentoListPage /> },
        {
          path: 'processos/:processoId/documentos/:documentoId',
          element: <DocumentoDetailPage />,
        },
        { path: 'plano-classificacao', element: <PlanoClassificacaoPage /> },
        { path: 'temporalidade', element: <TabelaTemporalidadePage /> },
        { path: 'destinacao', element: <DestinacaoPage /> },
      ],
    },
  ],
};

export default MODULE;
