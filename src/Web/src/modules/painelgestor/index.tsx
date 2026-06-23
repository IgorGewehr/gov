// Módulo PAINEL DO GESTOR (/painel-gestor) — dashboard executivo (visão do
// prefeito/gestor) com os 5 KPIs do exercício. Segue o padrão-ouro dos demais
// módulos:
//   - a PÁGINA é carregada via React.lazy -> chunk separado (code-splitting);
//   - o <Suspense> do AppLayout cobre o fallback de carregamento;
//   - leitura gated por "painel.ver": item de menu visível e rota acessível só a
//     quem tem a permissão (PermissionRoute). O backend é a fonte da verdade.
//
//   api.ts                  -> DTOs (espelham PainelGestorDto) + key + hook
//   painelgestor.helpers.ts -> formatação + mapeamento de semáforo (cores gov.br)
//   PainelGestorCards.tsx   -> os 5 cards de KPI
//   PainelGestorPage.tsx    -> seletor de exercício + estados + composição
//   index.tsx               -> ModuleDefinition { routes, nav } (este arquivo)
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';
import { PermissionRoute } from '../../auth/PermissionRoute';
import { PERM_PAINEL_VER } from './api';

const PainelGestorPage = lazy(() =>
  import('./PainelGestorPage').then((m) => ({ default: m.PainelGestorPage })),
);

const MODULE: ModuleDefinition = {
  id: 'painelgestor',
  nav: {
    label: 'Painel do Gestor',
    path: '/painel-gestor',
    icon: 'fas fa-chart-line',
    // Item visível só para o perfil de gestor (claim "painel.ver").
    permissions: [PERM_PAINEL_VER],
  },
  routes: [
    {
      path: 'painel-gestor',
      // Gating de rota: sem "painel.ver" redireciona para a Home (não expõe a área).
      element: <PermissionRoute permissions={[PERM_PAINEL_VER]} />,
      children: [{ index: true, element: <PainelGestorPage /> }],
    },
  ],
};

export default MODULE;
