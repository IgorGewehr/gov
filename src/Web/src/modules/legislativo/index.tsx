// Modulo Legislativo (Camara de Vereadores) — segue o PADRAO-OURO de Tributos.
// Agregados: Proposicao, Sessao, Votacao. Cada pagina e carregada via React.lazy
// (code-splitting); o <Suspense> do AppLayout cobre o fallback de carregamento.
//
//   api.ts                  -> DTOs + query keys + hooks TanStack Query
//   <Recurso>ListPage.tsx   -> consulta/lista (DataTable, estados)
//   <Recurso>DetailPage.tsx -> detalhe (useQuery + QueryState)
//   <Recurso>FormModal.tsx  -> formulario (useMutation + validacao por campo)
//   index.tsx               -> ModuleDefinition { routes, nav } com paginas em React.lazy
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const ProposicaoListPage = lazy(() =>
  import('./ProposicaoListPage').then((m) => ({ default: m.ProposicaoListPage })),
);
const ProposicaoDetailPage = lazy(() =>
  import('./ProposicaoDetailPage').then((m) => ({ default: m.ProposicaoDetailPage })),
);
const SessaoListPage = lazy(() =>
  import('./SessaoListPage').then((m) => ({ default: m.SessaoListPage })),
);
const SessaoDetailPage = lazy(() =>
  import('./SessaoDetailPage').then((m) => ({ default: m.SessaoDetailPage })),
);
const VotacaoConsultaPage = lazy(() =>
  import('./VotacaoConsultaPage').then((m) => ({ default: m.VotacaoConsultaPage })),
);
const VotacaoDetailPage = lazy(() =>
  import('./VotacaoDetailPage').then((m) => ({ default: m.VotacaoDetailPage })),
);
const VereadorListPage = lazy(() =>
  import('./VereadorListPage').then((m) => ({ default: m.VereadorListPage })),
);
const PainelAoVivoPage = lazy(() =>
  import('./PainelAoVivoPage').then((m) => ({ default: m.PainelAoVivoPage })),
);
const AtaView = lazy(() => import('./AtaView').then((m) => ({ default: m.AtaView })));
const NormaListPage = lazy(() =>
  import('./NormaListPage').then((m) => ({ default: m.NormaListPage })),
);
const NormaDetailPage = lazy(() =>
  import('./NormaDetailPage').then((m) => ({ default: m.NormaDetailPage })),
);
const DiarioListPage = lazy(() =>
  import('./DiarioListPage').then((m) => ({ default: m.DiarioListPage })),
);
const EdicaoDetailPage = lazy(() =>
  import('./EdicaoDetailPage').then((m) => ({ default: m.EdicaoDetailPage })),
);
const TribunaPage = lazy(() => import('./TribunaPage').then((m) => ({ default: m.TribunaPage })));
const ComissaoListPage = lazy(() =>
  import('./ComissaoListPage').then((m) => ({ default: m.ComissaoListPage })),
);
const ComissaoDetailPage = lazy(() =>
  import('./ComissaoDetailPage').then((m) => ({ default: m.ComissaoDetailPage })),
);

const MODULE: ModuleDefinition = {
  id: 'legislativo',
  nav: { label: 'Legislativo', path: '/legislativo', icon: 'fas fa-landmark' },
  routes: [
    {
      path: 'legislativo',
      children: [
        { index: true, element: <ProposicaoListPage /> },
        { path: 'proposicoes/:id', element: <ProposicaoDetailPage /> },
        { path: 'sessoes', element: <SessaoListPage /> },
        { path: 'sessoes/:id', element: <SessaoDetailPage /> },
        { path: 'votacoes', element: <VotacaoConsultaPage /> },
        { path: 'votacoes/:id', element: <VotacaoDetailPage /> },
        { path: 'vereadores', element: <VereadorListPage /> },
        { path: 'painel', element: <PainelAoVivoPage /> },
        { path: 'ata', element: <AtaView /> },
        { path: 'normas', element: <NormaListPage /> },
        { path: 'normas/:id', element: <NormaDetailPage /> },
        { path: 'diario', element: <DiarioListPage /> },
        { path: 'diario/:id', element: <EdicaoDetailPage /> },
        { path: 'tribuna', element: <TribunaPage /> },
        { path: 'comissoes', element: <ComissaoListPage /> },
        { path: 'comissoes/:id', element: <ComissaoDetailPage /> },
      ],
    },
  ],
};

export default MODULE;
