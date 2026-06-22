// Chamadas de autenticação à API .NET (módulo Identidade).
import { http } from '../api/http';
import type { LoginRequest, LoginResponse } from './types';

/**
 * Autentica no backend real: POST /api/identidade/login {email, senha} → {accessToken}.
 * O token JWT é assinado pelo módulo Identidade; o login resolve o tenant pelo índice
 * central (email→tenant) e emite o token com as permissões efetivas do usuário.
 */
export function login(credentials: LoginRequest): Promise<LoginResponse> {
  return http.post<LoginResponse>('/identidade/login', credentials);
}
