// Guarda de rota por permissão granular (claim "perm"). Complementa o
// ProtectedRoute (que exige apenas sessão): aqui, mesmo autenticado, o usuário
// precisa de ALGUMA das permissões informadas (semântica "OU") para acessar a
// subárvore de rotas. Sem nenhuma delas, redireciona para a Home ("/") — evita
// expor áreas restritas como a Administração do Sistema. É gating de UI ("negar
// por padrão", CLAUDE.md §6); a autorização real continua no backend.
import { Navigate, Outlet } from 'react-router-dom';
import { useHasAnyPermission } from './Can';

export interface PermissionRouteProps {
  /** Permissões que liberam a subárvore (basta UMA). */
  permissions: string[];
  /** Destino do redirect quando faltam todas as permissões. */
  redirectTo?: string;
}

export function PermissionRoute({ permissions, redirectTo = '/' }: PermissionRouteProps) {
  const permitido = useHasAnyPermission(permissions);
  if (!permitido) {
    return <Navigate to={redirectTo} replace />;
  }
  return <Outlet />;
}
