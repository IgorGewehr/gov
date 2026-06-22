// Gating de UI por permissão granular (claim "perm" do JWT). Espelha o RBAC do
// backend (CLAUDE.md §6: "Negar por padrão") no nível de apresentação: NÃO é
// controle de autorização real — o backend é a fonte da verdade —, apenas evita
// exibir ações/áreas para as quais o usuário não tem permissão.
//
// IMPORTANTE: esconder a UI não substitui o gating server-side. Toda mutação
// continua autorizada pelo ApiHost; isto é puramente experiência do usuário.
import type { ReactNode } from 'react';
import { useAuth } from './useAuth';

/** Hook: true se o usuário atual possui a permissão informada. */
export function useHasPermission(permission: string): boolean {
  const { hasPermission } = useAuth();
  return hasPermission(permission);
}

/**
 * Hook: true se o usuário possui ALGUMA das permissões informadas (semântica
 * "OU"). Lista vazia/ausente => true (sem restrição). Usado para gating de áreas
 * cujo acesso é liberado por qualquer uma de várias permissões (ex.: o item de
 * menu da Administração do Sistema).
 */
export function useHasAnyPermission(permissions?: string[]): boolean {
  const { hasPermission } = useAuth();
  if (!permissions || permissions.length === 0) return true;
  return permissions.some((p) => hasPermission(p));
}

export interface CanProps {
  /** Permissão exigida (ex.: "identidade.usuarios.gerenciar"). */
  permission: string;
  /** Conteúdo renderizado SOMENTE se o usuário possuir a permissão. */
  children: ReactNode;
  /** Conteúdo alternativo opcional quando a permissão estiver ausente. */
  fallback?: ReactNode;
}

/**
 * Renderiza `children` apenas se o usuário possuir `permission`; caso contrário
 * renderiza `fallback` (padrão: nada). Deve ser usado dentro de <AuthProvider>.
 */
export function Can({ permission, children, fallback = null }: CanProps) {
  const permitido = useHasPermission(permission);
  return <>{permitido ? children : fallback}</>;
}
