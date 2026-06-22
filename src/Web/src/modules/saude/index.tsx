// Módulo Saúde — segue o padrão-ouro de src/modules/tributos. Estrutura:
//   api.ts                      -> DTOs + query keys + hooks TanStack Query
//   <Recurso>ListPage.tsx       -> consulta/lista (estados loading/vazio/erro)
//   <Recurso>DetailPage.tsx     -> detalhe (useQuery + QueryState)
//   <Recurso>FormModal.tsx      -> formulário (useMutation + validação por campo)
//   saude.helpers.ts            -> mapeamentos de apresentação (Tag, selects)
//   index.tsx                   -> ModuleDefinition { routes, nav } com páginas em React.lazy
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
//
// Agregados (espelham o backend ...Modules.Saude):
//   Paciente (PEP/CADSUS), Atendimento (e-SUS APS), SolicitacaoRegulacao (SISREG).
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const PacienteListPage = lazy(() =>
  import('./PacienteListPage').then((m) => ({ default: m.PacienteListPage })),
);
const PacienteDetailPage = lazy(() =>
  import('./PacienteDetailPage').then((m) => ({ default: m.PacienteDetailPage })),
);
const AtendimentoDetailPage = lazy(() =>
  import('./AtendimentoDetailPage').then((m) => ({ default: m.AtendimentoDetailPage })),
);
const RegulacaoListPage = lazy(() =>
  import('./RegulacaoListPage').then((m) => ({ default: m.RegulacaoListPage })),
);
const RegulacaoDetailPage = lazy(() =>
  import('./RegulacaoDetailPage').then((m) => ({ default: m.RegulacaoDetailPage })),
);

const MODULE: ModuleDefinition = {
  id: 'saude',
  nav: { label: 'Saúde', path: '/saude', icon: 'fas fa-heart-pulse' },
  routes: [
    {
      path: 'saude',
      children: [
        { index: true, element: <PacienteListPage /> },
        { path: 'pacientes/:pacienteId', element: <PacienteDetailPage /> },
        { path: 'atendimentos/:atendimentoId', element: <AtendimentoDetailPage /> },
        { path: 'regulacao', element: <RegulacaoListPage /> },
        { path: 'regulacao/:solicitacaoId', element: <RegulacaoDetailPage /> },
      ],
    },
  ],
};

export default MODULE;
