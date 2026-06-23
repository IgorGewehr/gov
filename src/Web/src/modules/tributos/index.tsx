// Módulo Tributos — PADRÃO-OURO. Estrutura replicada pelos demais módulos:
//   api.ts                  -> DTOs + query keys + hooks TanStack Query
//   <Recurso>ListPage.tsx   -> consulta/lista (DataTable, estados)
//   <Recurso>FormModal.tsx  -> formulário (useMutation + validação por campo)
//   <Recurso>Modal.tsx      -> ações/commands específicos (EmitirCda)
//   index.tsx               -> ModuleDefinition { routes, nav } com páginas em React.lazy
//
// Cobertura REAL do backend (TributosEndpoints.cs /api/tributos):
//   - Contribuinte: cadastrar pessoa física (modal em DividaAtivaListPage)         [gerenciar]
//   - Lancamento:   lançar crédito + inscrever em Dívida Ativa (modal)             [gerenciar]
//   - DividaAtiva:  listar por contribuinte (GET) + emitir CDA (modal por linha)   [ver / gerenciar]
//
// As páginas são carregadas via React.lazy -> o módulo vira um chunk separado
// (code-splitting). O <Suspense> do AppLayout cobre o fallback de carregamento.
// Os FormModals/AcaoModals são embutidos na própria página (sem rota própria),
// igual ao padrão de Protocolo. A leitura é gated por "tributos.ver" e toda ação
// por "tributos.gerenciar" (<Can> dentro das páginas/modais).
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

const DividaAtivaListPage = lazy(() =>
  import('./DividaAtivaListPage').then((m) => ({ default: m.DividaAtivaListPage })),
);
const ImovelListPage = lazy(() =>
  import('./ImovelListPage').then((m) => ({ default: m.ImovelListPage })),
);
const ApurarIptuPage = lazy(() =>
  import('./ApurarIptuPage').then((m) => ({ default: m.ApurarIptuPage })),
);
const IptuParametrosPage = lazy(() =>
  import('./IptuParametrosPage').then((m) => ({ default: m.IptuParametrosPage })),
);
const ApurarIssPage = lazy(() =>
  import('./ApurarIssPage').then((m) => ({ default: m.ApurarIssPage })),
);
const TransmitirItbiPage = lazy(() =>
  import('./TransmitirItbiPage').then((m) => ({ default: m.TransmitirItbiPage })),
);
const LancarTaxaPage = lazy(() =>
  import('./LancarTaxaPage').then((m) => ({ default: m.LancarTaxaPage })),
);
const ApurarCosipPage = lazy(() =>
  import('./ApurarCosipPage').then((m) => ({ default: m.ApurarCosipPage })),
);
const EmitirAlvaraPage = lazy(() =>
  import('./EmitirAlvaraPage').then((m) => ({ default: m.EmitirAlvaraPage })),
);
const MelhoriaPage = lazy(() =>
  import('./MelhoriaPage').then((m) => ({ default: m.MelhoriaPage })),
);

const MODULE: ModuleDefinition = {
  id: 'tributos',
  nav: {
    label: 'Tributos',
    path: '/tributos',
    icon: 'fas fa-file-invoice-dollar',
  },
  routes: [
    {
      path: 'tributos',
      children: [
        { index: true, element: <DividaAtivaListPage /> },
        { path: 'imoveis', element: <ImovelListPage /> },
        { path: 'imoveis/:id/iptu', element: <ApurarIptuPage /> },
        { path: 'iptu/parametros', element: <IptuParametrosPage /> },
        { path: 'iss', element: <ApurarIssPage /> },
        { path: 'itbi', element: <TransmitirItbiPage /> },
        { path: 'taxas', element: <LancarTaxaPage /> },
        { path: 'cosip', element: <ApurarCosipPage /> },
        { path: 'alvaras', element: <EmitirAlvaraPage /> },
        { path: 'melhoria', element: <MelhoriaPage /> },
      ],
    },
  ],
};

export default MODULE;
