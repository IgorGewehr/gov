import { useContext } from 'react';
import { CidadaoAuthContext } from './CidadaoAuthContext';
import type { CidadaoAuthContextValue } from './CidadaoAuthContext';

/** Acessa a sessao do cidadao. Lanca fora do CidadaoAuthProvider (uso indevido). */
export function useCidadaoAuth(): CidadaoAuthContextValue {
  const ctx = useContext(CidadaoAuthContext);
  if (ctx === null) {
    throw new Error('useCidadaoAuth deve ser usado dentro de <CidadaoAuthProvider>.');
  }
  return ctx;
}
