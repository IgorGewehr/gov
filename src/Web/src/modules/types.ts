// Contrato de um módulo do Tensorroot.Gov no frontend. Cada módulo é isolado:
// expõe SOMENTE { route, nav } no seu index.tsx. O router central e a Sidebar
// consomem o registry — nenhum módulo importa o interno de outro (espelha o
// isolamento de Bounded Context do backend, CLAUDE.md §1.2).
//
// A `route` é montada como children do AppLayout no router central. O `path` é
// RELATIVO à raiz do módulo (ex.: "tributos", "tributos/dividas-ativas/:id"),
// pois o router já a aninha sob o layout. O index do módulo usa `index: true`.
import type { RouteObject } from 'react-router-dom';

export interface ModuleNav {
  /** Rótulo exibido na navegação (PT-BR, linguagem cidadã). */
  label: string;
  /** Caminho absoluto da raiz do módulo, ex.: "/tributos". */
  path: string;
  /** Ícone Font Awesome (padrão gov.br) — decorativo. */
  icon?: string;
  /**
   * Permissões granulares (claim "perm") que liberam a visibilidade do item na
   * Sidebar. Semântica "OU" (basta UMA). Ausente/vazio => visível para qualquer
   * sessão autenticada (módulos de domínio comuns). Usado por áreas restritas
   * como Administração do Sistema. Isto é gating de UI ("negar por padrão",
   * CLAUDE.md §6); o backend continua sendo a fonte da verdade da autorização.
   */
  permissions?: string[];
}

export interface ModuleDefinition {
  /** Chave estável do módulo (igual ao diretório). */
  id: string;
  /** Rotas do módulo (uma ou mais RouteObject) montadas sob o AppLayout. */
  routes: RouteObject[];
  /** Entrada de menu na Sidebar. */
  nav: ModuleNav;
}
