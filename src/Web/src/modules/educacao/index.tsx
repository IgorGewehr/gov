// Módulo Educação — segue o padrão-ouro de src/modules/protocolo. Estrutura:
//   <agregado>.api.ts            -> DTOs + query keys + hooks TanStack Query
//   <Recurso>ListPage.tsx        -> consulta/lista (DataTable, estados, ações)
//   <Recurso>DetailPage.tsx      -> detalhe (useQuery + QueryState + ações)
//   <Recurso>FormModal.tsx       -> formulário de criação (useMutation + validação)
//   <Recurso>AcaoModais.tsx      -> modais de comando/transição do agregado
//   index.tsx                    -> ModuleDefinition { routes, nav } com páginas em React.lazy
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
// Os FormModals/AcaoModais são renderizados DENTRO das próprias páginas (sem rota).
//
// Agregados/operações (espelham 100% de /api/educacao em EducacaoEndpoints.cs):
//   Escola      -> lista da rede, detalhe por INEP, credenciar, atualizar Censo, desativar
//   Matricula   -> consulta por aluno, Matrícula Inicial por turma, matricular, rematricular,
//                  transferir, encerrar, registrar Situação do Aluno
//   DiarioClasse-> diário/frequência da matrícula, abrir, frequência, nota, aula, apuração
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const EscolaListPage = lazy(() =>
  import('./EscolaListPage').then((m) => ({ default: m.EscolaListPage })),
);
const EscolaDetailPage = lazy(() =>
  import('./EscolaDetailPage').then((m) => ({ default: m.EscolaDetailPage })),
);
const AlunoListPage = lazy(() =>
  import('./AlunoListPage').then((m) => ({ default: m.AlunoListPage })),
);
const TurmaListPage = lazy(() =>
  import('./TurmaListPage').then((m) => ({ default: m.TurmaListPage })),
);
const MatriculaListPage = lazy(() =>
  import('./MatriculaListPage').then((m) => ({ default: m.MatriculaListPage })),
);
const TurmaMatriculaListPage = lazy(() =>
  import('./TurmaMatriculaListPage').then((m) => ({ default: m.TurmaMatriculaListPage })),
);
const DiarioClasseDetailPage = lazy(() =>
  import('./DiarioClasseDetailPage').then((m) => ({ default: m.DiarioClasseDetailPage })),
);
const FiscalEducacaoPainelPage = lazy(() =>
  import('./FiscalEducacaoPainelPage').then((m) => ({ default: m.FiscalEducacaoPainelPage })),
);

const MODULE: ModuleDefinition = {
  id: 'educacao',
  nav: {
    label: 'Educação',
    path: '/educacao',
    icon: 'fas fa-graduation-cap',
  },
  routes: [
    {
      path: 'educacao',
      children: [
        { index: true, element: <EscolaListPage /> },
        { path: 'escolas/:codigoInep', element: <EscolaDetailPage /> },
        { path: 'alunos', element: <AlunoListPage /> },
        { path: 'turmas', element: <TurmaListPage /> },
        { path: 'matriculas', element: <MatriculaListPage /> },
        { path: 'turmas/matricula-inicial', element: <TurmaMatriculaListPage /> },
        { path: 'matriculas/:matriculaId/diario', element: <DiarioClasseDetailPage /> },
        { path: 'fiscal', element: <FiscalEducacaoPainelPage /> },
      ],
    },
  ],
};

export const route = MODULE.routes;
export const nav = MODULE.nav;

export default MODULE;
