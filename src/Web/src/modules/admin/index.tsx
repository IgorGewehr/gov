// Módulo Administração do Sistema (/admin) — gestão de usuários, papéis, módulos
// licenciados do tenant e trilha de auditoria. Segue o padrão-ouro de
// src/modules/protocolo/index.tsx:
//   - cada PÁGINA é carregada via React.lazy -> chunk separado (code-splitting);
//   - o <Suspense> do AppLayout cobre o fallback de carregamento;
//   - a landing (/admin) lista cards de atalho, cada um com gating por permissão.
//
// NÃO está registrado no registry.ts ainda: a fase de integração fará o registro
// (e o gating de visibilidade do item de menu por permissão).
import { lazy } from 'react';
import type { ModuleDefinition } from '../types';
import { AdminLandingPage } from './AdminLandingPage';
import { PermissionRoute } from '../../auth/PermissionRoute';
import {
  PERM_USUARIOS_GERENCIAR,
  PERM_MODULOS_CONFIGURAR,
  PERM_AUDITORIA_VER,
} from './admin.permissoes';

/** Permissões que liberam a área administrativa (semântica "OU"). */
const ADMIN_GATE = [PERM_USUARIOS_GERENCIAR, PERM_MODULOS_CONFIGURAR, PERM_AUDITORIA_VER];

const UsuarioListPage = lazy(() =>
  import('./usuarios/UsuarioListPage').then((m) => ({ default: m.UsuarioListPage })),
);
const UnidadesPage = lazy(() =>
  import('./unidades/UnidadesPage').then((m) => ({ default: m.UnidadesPage })),
);
const PapelListPage = lazy(() =>
  import('./papeis/PapelListPage').then((m) => ({ default: m.PapelListPage })),
);
const ModulosConfigPage = lazy(() =>
  import('./modulos/ModulosConfigPage').then((m) => ({ default: m.ModulosConfigPage })),
);
const AuditoriaListPage = lazy(() =>
  import('./auditoria/AuditoriaListPage').then((m) => ({ default: m.AuditoriaListPage })),
);

const MODULE: ModuleDefinition = {
  id: 'admin',
  nav: {
    label: 'Administração do Sistema',
    path: '/admin',
    icon: 'fas fa-gears',
    // Área restrita: o item de menu só aparece para quem tem ALGUMA permissão
    // administrativa (semântica "OU"). A própria landing e cada rota repetem o
    // gating ("negar por padrão", CLAUDE.md §6).
    permissions: ADMIN_GATE,
  },
  routes: [
    {
      path: 'admin',
      // Gating de rota por permissão: sem NENHUMA permissão admin, redireciona
      // para a Home (em vez de expor a área). Cada página repete o gating fino.
      element: <PermissionRoute permissions={ADMIN_GATE} />,
      children: [
        { index: true, element: <AdminLandingPage /> },
        { path: 'usuarios', element: <UsuarioListPage /> },
        { path: 'unidades', element: <UnidadesPage /> },
        { path: 'papeis', element: <PapelListPage /> },
        { path: 'modulos', element: <ModulosConfigPage /> },
        { path: 'auditoria', element: <AuditoriaListPage /> },
      ],
    },
  ],
};

export default MODULE;
