// Módulo AssistenciaSocial — segue o PADRÃO-OURO de src/modules/tributos/index.tsx.
// Cada página dos agregados (Familia, Beneficio, ProntuarioSuas) é carregada via
// React.lazy -> o módulo vira um chunk separado (code-splitting). O <Suspense> do
// AppLayout cobre o fallback de carregamento. Os FormModals/Modais NÃO têm rota
// própria: são embutidos nas páginas (List/Detail), igual ao padrão de Tributos.
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';

// --- Familia ---
const FamiliaListPage = lazy(() =>
  import('./familia/FamiliaListPage').then((m) => ({ default: m.FamiliaListPage })),
);
const FamiliaDetailPage = lazy(() =>
  import('./familia/FamiliaDetailPage').then((m) => ({ default: m.FamiliaDetailPage })),
);

// --- Beneficio ---
const BeneficioListPage = lazy(() =>
  import('./beneficio/BeneficioListPage').then((m) => ({ default: m.BeneficioListPage })),
);
const BeneficioDetailPage = lazy(() =>
  import('./beneficio/BeneficioDetailPage').then((m) => ({ default: m.BeneficioDetailPage })),
);
const ConcessoesPorCompetenciaPage = lazy(() =>
  import('./beneficio/ConcessoesPorCompetenciaPage').then((m) => ({
    default: m.ConcessoesPorCompetenciaPage,
  })),
);

// --- ProntuarioSuas ---
const ProntuarioSuasListPage = lazy(() =>
  import('./prontuariosuas/ProntuarioSuasListPage').then((m) => ({
    default: m.ProntuarioSuasListPage,
  })),
);
const ProntuarioSuasDetailPage = lazy(() =>
  import('./prontuariosuas/ProntuarioSuasDetailPage').then((m) => ({
    default: m.ProntuarioSuasDetailPage,
  })),
);

const MODULE: ModuleDefinition = {
  id: 'assistenciasocial',
  nav: {
    label: 'Assistência Social',
    path: '/assistenciasocial',
    icon: 'fas fa-hands-helping',
  },
  routes: [
    {
      path: 'assistenciasocial',
      children: [
        // Lista principal do módulo (Familias / CadÚnico)
        { index: true, element: <FamiliaListPage /> },
        { path: 'familias/:id', element: <FamiliaDetailPage /> },
        { path: 'familias/:familiaId/beneficios/:id', element: <BeneficioDetailPage /> },
        // Beneficios
        { path: 'beneficios', element: <BeneficioListPage /> },
        { path: 'beneficios/concessoes', element: <ConcessoesPorCompetenciaPage /> },
        // Prontuario SUAS
        { path: 'prontuarios', element: <ProntuarioSuasListPage /> },
        { path: 'prontuarios/:id', element: <ProntuarioSuasDetailPage /> },
      ],
    },
  ],
};

export default MODULE;
