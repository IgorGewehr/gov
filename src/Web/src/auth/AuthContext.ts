// Contexto de autenticação. Separado do provider (fast-refresh friendly).
import { createContext } from 'react';
import type { AuthState, LoginRequest } from './types';

export interface AuthContextValue extends AuthState {
  /** Autentica via API e persiste o token; lança ApiError em falha. */
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
  hasRole: (role: string) => boolean;
  /** true se o usuário possui a permissão granular informada (claim "perm"). */
  hasPermission: (permissao: string) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
