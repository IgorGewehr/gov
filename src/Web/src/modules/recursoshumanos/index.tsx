// Módulo Recursos Humanos — segue o PADRÃO-OURO de src/modules/tributos:
//   api.ts                      -> DTOs + query keys + hooks TanStack Query
//   <Agregado>ListPage.tsx      -> consulta/lista (DataTable/QueryState, estados)
//   <Agregado>DetailPage.tsx    -> detalhe (useQuery + QueryState)
//   <Acao>FormModal.tsx         -> formulário (useMutation + validação por campo)
//   index.tsx                   -> ModuleDefinition { routes, nav } com páginas em React.lazy
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
//
// Agregados: Servidor (índice), Cargo e FolhaDePagamento. A navegação entre eles
// é interna (RhSubNav), mantendo UMA entrada de módulo na Sidebar.
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const ServidoresListPage = lazy(() =>
  import('./ServidoresListPage').then((m) => ({ default: m.ServidoresListPage })),
);
const ServidorDetailPage = lazy(() =>
  import('./ServidorDetailPage').then((m) => ({ default: m.ServidorDetailPage })),
);
const ServidorFichaPage = lazy(() =>
  import('./ServidorFichaPage').then((m) => ({ default: m.ServidorFichaPage })),
);
const AfastamentosServidorPage = lazy(() =>
  import('./AfastamentosServidorPage').then((m) => ({ default: m.AfastamentosServidorPage })),
);
const ConsignacoesServidorPage = lazy(() =>
  import('./ConsignacoesServidorPage').then((m) => ({ default: m.ConsignacoesServidorPage })),
);
const CargosListPage = lazy(() =>
  import('./CargosListPage').then((m) => ({ default: m.CargosListPage })),
);
const CargoDetailPage = lazy(() =>
  import('./CargoDetailPage').then((m) => ({ default: m.CargoDetailPage })),
);
const RubricasListPage = lazy(() =>
  import('./RubricasListPage').then((m) => ({ default: m.RubricasListPage })),
);
const TabelasLegaisPage = lazy(() =>
  import('./TabelasLegaisPage').then((m) => ({ default: m.TabelasLegaisPage })),
);
const FolhaListPage = lazy(() =>
  import('./FolhaListPage').then((m) => ({ default: m.FolhaListPage })),
);
const FolhaDetailPage = lazy(() =>
  import('./FolhaDetailPage').then((m) => ({ default: m.FolhaDetailPage })),
);
const CicloAnualPage = lazy(() =>
  import('./CicloAnualPage').then((m) => ({ default: m.CicloAnualPage })),
);
const PontoListPage = lazy(() =>
  import('./PontoListPage').then((m) => ({ default: m.PontoListPage })),
);
const PontoServidorPage = lazy(() =>
  import('./PontoServidorPage').then((m) => ({ default: m.PontoServidorPage })),
);
const ESocialPainelPage = lazy(() =>
  import('./ESocialPainelPage').then((m) => ({ default: m.ESocialPainelPage })),
);
const MinhaFolhaPage = lazy(() =>
  import('./MinhaFolhaPage').then((m) => ({ default: m.MinhaFolhaPage })),
);
const RelatoriosPage = lazy(() =>
  import('./RelatoriosPage').then((m) => ({ default: m.RelatoriosPage })),
);
const PortariasListPage = lazy(() =>
  import('./PortariasListPage').then((m) => ({ default: m.PortariasListPage })),
);
const PortariaDetailPage = lazy(() =>
  import('./PortariaDetailPage').then((m) => ({ default: m.PortariaDetailPage })),
);
const PasepPage = lazy(() => import('./PasepPage').then((m) => ({ default: m.PasepPage })));
const SstServidorPage = lazy(() =>
  import('./SstServidorPage').then((m) => ({ default: m.SstServidorPage })),
);
const BancoDeHorasServidorPage = lazy(() =>
  import('./BancoDeHorasServidorPage').then((m) => ({ default: m.BancoDeHorasServidorPage })),
);
const CertidoesTempoServidorPage = lazy(() =>
  import('./CertidoesTempoServidorPage').then((m) => ({ default: m.CertidoesTempoServidorPage })),
);
const CertidaoDetailPage = lazy(() =>
  import('./CertidaoDetailPage').then((m) => ({ default: m.CertidaoDetailPage })),
);
const PlanosCarreiraListPage = lazy(() =>
  import('./PlanosCarreiraListPage').then((m) => ({ default: m.PlanosCarreiraListPage })),
);
const PlanoCarreiraDetailPage = lazy(() =>
  import('./PlanoCarreiraDetailPage').then((m) => ({ default: m.PlanoCarreiraDetailPage })),
);
const ProcessosTrabalhistasListPage = lazy(() =>
  import('./ProcessosTrabalhistasListPage').then((m) => ({
    default: m.ProcessosTrabalhistasListPage,
  })),
);
const RemessasSicapListPage = lazy(() =>
  import('./RemessasSicapListPage').then((m) => ({ default: m.RemessasSicapListPage })),
);
const RemessaSicapDetailPage = lazy(() =>
  import('./RemessaSicapDetailPage').then((m) => ({ default: m.RemessaSicapDetailPage })),
);

const MODULE: ModuleDefinition = {
  id: 'recursoshumanos',
  nav: {
    label: 'Recursos Humanos',
    path: '/recursoshumanos',
    icon: 'fas fa-users',
  },
  routes: [
    {
      path: 'recursoshumanos',
      children: [
        { index: true, element: <ServidoresListPage /> },
        { path: 'servidores/:servidorId/ficha', element: <ServidorFichaPage /> },
        { path: 'servidores/:servidorId/afastamentos', element: <AfastamentosServidorPage /> },
        { path: 'servidores/:servidorId/consignacoes', element: <ConsignacoesServidorPage /> },
        { path: 'servidores/:servidorId/sst', element: <SstServidorPage /> },
        { path: 'servidores/:servidorId/banco-de-horas', element: <BancoDeHorasServidorPage /> },
        { path: 'servidores/:servidorId/certidoes-tempo', element: <CertidoesTempoServidorPage /> },
        { path: 'certidoes-tempo/:certidaoId', element: <CertidaoDetailPage /> },
        { path: 'servidores/:matricula', element: <ServidorDetailPage /> },
        { path: 'cargos', element: <CargosListPage /> },
        { path: 'cargos/:id', element: <CargoDetailPage /> },
        { path: 'portarias', element: <PortariasListPage /> },
        { path: 'portarias/:portariaId', element: <PortariaDetailPage /> },
        { path: 'pasep', element: <PasepPage /> },
        { path: 'planos-carreira', element: <PlanosCarreiraListPage /> },
        { path: 'planos-carreira/:planoId', element: <PlanoCarreiraDetailPage /> },
        { path: 'processos-trabalhistas', element: <ProcessosTrabalhistasListPage /> },
        { path: 'sicap-pessoal', element: <RemessasSicapListPage /> },
        { path: 'sicap-pessoal/:remessaId', element: <RemessaSicapDetailPage /> },
        { path: 'rubricas', element: <RubricasListPage /> },
        { path: 'tabelas-legais', element: <TabelasLegaisPage /> },
        { path: 'folhas', element: <FolhaListPage /> },
        { path: 'folhas/:folhaId', element: <FolhaDetailPage /> },
        { path: 'ciclo-anual', element: <CicloAnualPage /> },
        { path: 'ponto', element: <PontoListPage /> },
        { path: 'ponto/:servidorId', element: <PontoServidorPage /> },
        { path: 'esocial', element: <ESocialPainelPage /> },
        { path: 'minha-folha', element: <MinhaFolhaPage /> },
        { path: 'relatorios', element: <RelatoriosPage /> },
      ],
    },
  ],
};

export default MODULE;
