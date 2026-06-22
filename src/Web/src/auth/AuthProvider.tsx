// Provider de autenticação. Fonte da verdade do estado de sessão na SPA.
// - Inicializa a partir do token em sessionStorage (deriva o usuário das claims).
// - Registra o handler global de 401 do http client para forçar logout + navegação.
// - Sincroniza entre abas/instâncias via evento AUTH_TOKEN_CHANGED.
import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { AUTH_TOKEN_CHANGED, clearAccessToken, getAccessToken, setAccessToken } from '../api/authToken';
import { setUnauthorizedHandler } from '../api/http';
import { isTokenExpired, userFromToken } from './jwt';
import { login as loginRequest } from './api';
import { AuthContext } from './AuthContext';
import type { AuthContextValue } from './AuthContext';
import type { AuthenticatedUser, LoginRequest } from './types';

function resolveUser(): AuthenticatedUser | null {
  const token = getAccessToken();
  if (!token || isTokenExpired(token)) {
    if (token) clearAccessToken();
    return null;
  }
  return userFromToken(token);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [user, setUser] = useState<AuthenticatedUser | null>(() => resolveUser());

  // 401 vindo do http client: limpa sessão e leva ao login.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      setUser(null);
      navigate('/login', { replace: true });
    });
    return () => setUnauthorizedHandler(null);
  }, [navigate]);

  // Mantém o estado em sincronia com o token (logout em outra aba etc.).
  useEffect(() => {
    const handler = () => setUser(resolveUser());
    window.addEventListener(AUTH_TOKEN_CHANGED, handler);
    window.addEventListener('storage', handler);
    return () => {
      window.removeEventListener(AUTH_TOKEN_CHANGED, handler);
      window.removeEventListener('storage', handler);
    };
  }, []);

  const login = useCallback(async (credentials: LoginRequest) => {
    const { accessToken } = await loginRequest(credentials);
    setAccessToken(accessToken);
    setUser(userFromToken(accessToken));
  }, []);

  const logout = useCallback(() => {
    clearAccessToken();
    setUser(null);
    navigate('/login', { replace: true });
  }, [navigate]);

  const hasRole = useCallback((role: string) => user?.roles.includes(role) ?? false, [user]);

  const hasPermission = useCallback(
    (permissao: string) => user?.permissions.includes(permissao) ?? false,
    [user],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, logout, hasRole, hasPermission }),
    [user, login, logout, hasRole, hasPermission],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
