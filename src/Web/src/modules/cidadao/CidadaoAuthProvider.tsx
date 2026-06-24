// Provider da sessao do CIDADAO (realm externo). Espelha o AuthProvider do admin, mas
// usa o token/eventos do cidadao e NUNCA interfere na sessao do admin.
// - Inicializa a partir do token do cidadao em sessionStorage (deriva da claim).
// - Sincroniza com CIDADAO_TOKEN_CHANGED (login/logout/401 vindo do cidadaoApi) e entre abas.
import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  CIDADAO_TOKEN_CHANGED,
  clearCidadaoToken,
  getCidadaoToken,
  setCidadaoToken,
} from './cidadaoAuthToken';
import { cidadaoFromToken, cidadaoTokenExpirado } from './cidadaoSession';
import type { CidadaoSessao } from './cidadaoSession';
import { cidadaoApi } from './cidadaoApi';
import type { LoginCidadaoRequest } from './cidadaoApi';
import { CidadaoAuthContext } from './CidadaoAuthContext';
import type { CidadaoAuthContextValue } from './CidadaoAuthContext';

function resolverCidadao(): CidadaoSessao | null {
  const token = getCidadaoToken();
  if (!token || cidadaoTokenExpirado(token)) {
    if (token) clearCidadaoToken();
    return null;
  }
  return cidadaoFromToken(token);
}

export function CidadaoAuthProvider({ children }: { children: ReactNode }) {
  const [cidadao, setCidadao] = useState<CidadaoSessao | null>(() => resolverCidadao());

  // Mantem a sessao em sincronia com o token (login/logout/401/outra aba).
  useEffect(() => {
    const handler = () => setCidadao(resolverCidadao());
    window.addEventListener(CIDADAO_TOKEN_CHANGED, handler);
    window.addEventListener('storage', handler);
    return () => {
      window.removeEventListener(CIDADAO_TOKEN_CHANGED, handler);
      window.removeEventListener('storage', handler);
    };
  }, []);

  const entrar = useCallback(async (credenciais: LoginCidadaoRequest) => {
    const { accessToken } = await cidadaoApi.login(credenciais);
    setCidadaoToken(accessToken);
    setCidadao(cidadaoFromToken(accessToken));
  }, []);

  const sair = useCallback(() => {
    clearCidadaoToken();
    setCidadao(null);
  }, []);

  const value = useMemo<CidadaoAuthContextValue>(
    () => ({ cidadao, isAutenticado: cidadao !== null, entrar, sair }),
    [cidadao, entrar, sair],
  );

  return <CidadaoAuthContext.Provider value={value}>{children}</CidadaoAuthContext.Provider>;
}
