// Tipos do domínio de autenticação. O usuário e o tenant são derivados das claims
// do JWT emitido pelo ApiHost (.NET). Nada sensível é persistido além do token.

export interface AuthenticatedUser {
  /** subject (claim "sub"). */
  id: string;
  nome: string;
  email: string | null;
  /** Identificador do tenant (claim "tenant_id") — base do isolamento multi-tenant. */
  tenantId: string;
  /** Nome amigável do órgão/tenant (claim "tenant_name"), se presente. */
  tenantNome: string | null;
  /** Papéis (claim "role"). */
  roles: string[];
  /** Permissões granulares (claim "perm") — base do gating de UI por permissão. */
  permissions: string[];
}

export interface AuthState {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
}

/** Resposta do endpoint de login da API. */
export interface LoginResponse {
  accessToken: string;
}

export interface LoginRequest {
  email: string;
  senha: string;
}
