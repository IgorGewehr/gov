// Estado da sidebar de módulos: colapsada (rail só de ícones) x expandida.
// Persiste a preferência do usuário em localStorage e auto-colapsa em telas
// estreitas (md: 992px do gov.br DS). Em telas estreitas a sidebar vira
// off-canvas (controlada pelo AppLayout), então o "colapsar" só vale no desktop.
import { useCallback, useEffect, useState } from 'react';

const CHAVE = 'tg:sidebar-colapsada';
const BREAKPOINT_MD = 992;

function lerPreferencia(): boolean {
  try {
    return window.localStorage.getItem(CHAVE) === '1';
  } catch {
    return false;
  }
}

/**
 * Controla o estado colapsada/expandida da sidebar no desktop, com persistência.
 * Retorna o estado e um alternador. Em telas < md a sidebar é off-canvas, então
 * o rail colapsado não se aplica (o AppLayout cuida do off-canvas).
 */
export function useSidebarColapsada(): { colapsada: boolean; alternar: () => void } {
  const [colapsada, setColapsada] = useState<boolean>(lerPreferencia);

  const alternar = useCallback(() => {
    setColapsada((atual) => {
      const proximo = !atual;
      try {
        window.localStorage.setItem(CHAVE, proximo ? '1' : '0');
      } catch {
        // localStorage indisponível (modo privado) — preferência só em memória.
      }
      return proximo;
    });
  }, []);

  // Sincroniza entre abas/janelas (storage event) — preferência única do usuário.
  useEffect(() => {
    function aoMudarStorage(evento: StorageEvent): void {
      if (evento.key === CHAVE) setColapsada(evento.newValue === '1');
    }
    window.addEventListener('storage', aoMudarStorage);
    return () => window.removeEventListener('storage', aoMudarStorage);
  }, []);

  return { colapsada, alternar };
}

export { BREAKPOINT_MD };
