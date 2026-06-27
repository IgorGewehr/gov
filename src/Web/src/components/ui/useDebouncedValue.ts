// Hook utilitário: retorna `valor` com atraso (debounce). Útil para search-as-you-type
// — evita disparar uma requisição a cada tecla. Cancela o timer pendente a cada mudança
// (e no unmount), então só o último valor "estabilizado" é propagado.
import { useEffect, useState } from 'react';

/** Retorna `valor` após `atrasoMs` sem mudanças (debounce). Default: 300ms. */
export function useDebouncedValue<T>(valor: T, atrasoMs = 300): T {
  const [debounced, setDebounced] = useState(valor);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(valor), atrasoMs);
    return () => clearTimeout(timer);
  }, [valor, atrasoMs]);

  return debounced;
}
