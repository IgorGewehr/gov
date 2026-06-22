// Constantes de permissão (claim "perm" do JWT) usadas pelo gating do módulo de
// Administração do Sistema. Centralizadas aqui para evitar strings mágicas
// espalhadas nas páginas e manter alinhamento 1:1 com o catálogo canônico do
// backend (GET /api/identidade/permissoes).
//
// Gating por área:
//   - Usuários e Papéis  -> identidade.usuarios.gerenciar
//   - Configuração de Módulos -> admin.modulos.configurar
//   - Trilha de Auditoria -> admin.auditoria.ver

/** Gerenciar usuários e papéis (RBAC) do tenant. */
export const PERM_USUARIOS_GERENCIAR = 'identidade.usuarios.gerenciar';

/** Ativar/desativar módulos licenciados do tenant. */
export const PERM_MODULOS_CONFIGURAR = 'admin.modulos.configurar';

/** Consultar a trilha de auditoria imutável. */
export const PERM_AUDITORIA_VER = 'admin.auditoria.ver';

/** Conjunto de permissões do módulo admin (útil para iteração/validação). */
export const ADMIN_PERMISSOES = {
  usuarios: PERM_USUARIOS_GERENCIAR,
  papeis: PERM_USUARIOS_GERENCIAR,
  modulos: PERM_MODULOS_CONFIGURAR,
  auditoria: PERM_AUDITORIA_VER,
} as const;
