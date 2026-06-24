// Contexto da sessao do cidadao (realm externo, independente do AuthContext do admin).
import { createContext } from 'react';
import type { CidadaoSessao } from './cidadaoSession';
import type { LoginCidadaoRequest } from './cidadaoApi';

export interface CidadaoAuthContextValue {
  cidadao: CidadaoSessao | null;
  isAutenticado: boolean;
  entrar: (credenciais: LoginCidadaoRequest) => Promise<void>;
  sair: () => void;
}

export const CidadaoAuthContext = createContext<CidadaoAuthContextValue | null>(null);
