// Módulo Finanças — ciclo da despesa pública orçamentária (Lei 4.320/64):
//   financas.api.ts + <agregado>.api.ts -> DTOs + query keys + hooks TanStack Query
//   <Agregado>ListPage.tsx              -> consulta/lista (DataTable/QueryState, estados)
//   <Agregado>DetailPage.tsx            -> detalhe (useQuery + QueryState) + ações
//   <Acao>FormModal.tsx / ValorAcaoModal-> formulários e ações (useMutation + validação)
//   FinancasSubNav.tsx                  -> navegação interna entre agregados
//   index.tsx                           -> ModuleDefinition { routes, nav } com React.lazy
//
// Agregados: Dotações (índice), Empenhos, Liquidações, Pagamentos e Restos a Pagar, mais a
// Contabilidade (PCASP): Plano de Contas, Balancete, Lançamentos e Razão por conta. A
// navegação entre eles é interna (FinancasSubNav), mantendo UMA entrada na Sidebar.
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const DotacaoListPage = lazy(() =>
  import('./DotacaoListPage').then((m) => ({ default: m.DotacaoListPage })),
);
const DotacaoDetailPage = lazy(() =>
  import('./DotacaoDetailPage').then((m) => ({ default: m.DotacaoDetailPage })),
);
const EmpenhoListPage = lazy(() =>
  import('./EmpenhoListPage').then((m) => ({ default: m.EmpenhoListPage })),
);
const EmpenhoDetailPage = lazy(() =>
  import('./EmpenhoDetailPage').then((m) => ({ default: m.EmpenhoDetailPage })),
);
const LiquidacaoListPage = lazy(() =>
  import('./LiquidacaoListPage').then((m) => ({ default: m.LiquidacaoListPage })),
);
const LiquidacaoDetailPage = lazy(() =>
  import('./LiquidacaoDetailPage').then((m) => ({ default: m.LiquidacaoDetailPage })),
);
const PagamentoListPage = lazy(() =>
  import('./PagamentoListPage').then((m) => ({ default: m.PagamentoListPage })),
);
const PagamentoDetailPage = lazy(() =>
  import('./PagamentoDetailPage').then((m) => ({ default: m.PagamentoDetailPage })),
);
const RestosAPagarListPage = lazy(() =>
  import('./RestosAPagarListPage').then((m) => ({ default: m.RestosAPagarListPage })),
);
const PlanoDeContasPage = lazy(() =>
  import('./contabilidade/PlanoDeContasPage').then((m) => ({ default: m.PlanoDeContasPage })),
);
const BalancetePage = lazy(() =>
  import('./contabilidade/BalancetePage').then((m) => ({ default: m.BalancetePage })),
);
const LancamentosPage = lazy(() =>
  import('./contabilidade/LancamentosPage').then((m) => ({ default: m.LancamentosPage })),
);
const RazaoContaPage = lazy(() =>
  import('./contabilidade/RazaoContaPage').then((m) => ({ default: m.RazaoContaPage })),
);
const RazaoAnaliticoPage = lazy(() =>
  import('./contabilidade/RazaoAnaliticoPage').then((m) => ({ default: m.RazaoAnaliticoPage })),
);
const DiarioPage = lazy(() =>
  import('./contabilidade/DiarioPage').then((m) => ({ default: m.DiarioPage })),
);
const ContasFinanceirasPage = lazy(() =>
  import('./tesouraria/ContasFinanceirasPage').then((m) => ({ default: m.ContasFinanceirasPage })),
);
const ExtratoContaPage = lazy(() =>
  import('./tesouraria/ExtratoContaPage').then((m) => ({ default: m.ExtratoContaPage })),
);
const BoletimCaixaBancoPage = lazy(() =>
  import('./tesouraria/BoletimCaixaBancoPage').then((m) => ({ default: m.BoletimCaixaBancoPage })),
);
const DemonstracoesPage = lazy(() =>
  import('./contabilidade/DemonstracoesPage').then((m) => ({ default: m.DemonstracoesPage })),
);
const MscPage = lazy(() =>
  import('./contabilidade/MscPage').then((m) => ({ default: m.MscPage })),
);

const MODULE: ModuleDefinition = {
  id: 'financas',
  nav: { label: 'Finanças', path: '/financas', icon: 'fas fa-coins' },
  routes: [
    {
      path: 'financas',
      children: [
        { index: true, element: <DotacaoListPage /> },
        { path: 'dotacoes/:id', element: <DotacaoDetailPage /> },
        { path: 'empenhos', element: <EmpenhoListPage /> },
        { path: 'empenhos/:id', element: <EmpenhoDetailPage /> },
        { path: 'liquidacoes', element: <LiquidacaoListPage /> },
        { path: 'liquidacoes/:id', element: <LiquidacaoDetailPage /> },
        { path: 'pagamentos', element: <PagamentoListPage /> },
        { path: 'pagamentos/:id', element: <PagamentoDetailPage /> },
        { path: 'restos-a-pagar', element: <RestosAPagarListPage /> },
        { path: 'tesouraria/contas', element: <ContasFinanceirasPage /> },
        { path: 'tesouraria/contas/:contaId', element: <ExtratoContaPage /> },
        { path: 'tesouraria/boletim', element: <BoletimCaixaBancoPage /> },
        { path: 'contabilidade/plano-de-contas', element: <PlanoDeContasPage /> },
        { path: 'contabilidade/balancete', element: <BalancetePage /> },
        { path: 'contabilidade/lancamentos', element: <LancamentosPage /> },
        { path: 'contabilidade/contas/:contaId/razao', element: <RazaoContaPage /> },
        { path: 'contabilidade/contas/:contaId/razao-analitico', element: <RazaoAnaliticoPage /> },
        { path: 'contabilidade/diario', element: <DiarioPage /> },
        { path: 'contabilidade/demonstracoes', element: <DemonstracoesPage /> },
        { path: 'contabilidade/msc', element: <MscPage /> },
      ],
    },
  ],
};

export default MODULE;
