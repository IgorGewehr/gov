// Teste do toggle da sidebar: alterna colapsada/expandida E persiste a
// preferência do usuário em localStorage (sobrevive a remontagem — ex.: reload).
import { describe, it, expect, beforeEach } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useSidebarColapsada } from './useSidebarColapsada';

const CHAVE = 'tg:sidebar-colapsada';

describe('useSidebarColapsada', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('começa expandida quando não há preferência salva', () => {
    const { result } = renderHook(() => useSidebarColapsada());
    expect(result.current.colapsada).toBe(false);
  });

  it('alterna colapsada e persiste a preferência em localStorage', () => {
    const { result } = renderHook(() => useSidebarColapsada());

    act(() => result.current.alternar());
    expect(result.current.colapsada).toBe(true);
    expect(window.localStorage.getItem(CHAVE)).toBe('1');

    act(() => result.current.alternar());
    expect(result.current.colapsada).toBe(false);
    expect(window.localStorage.getItem(CHAVE)).toBe('0');
  });

  it('restaura a preferência colapsada salva ao montar (persistência entre sessões)', () => {
    window.localStorage.setItem(CHAVE, '1');
    const { result } = renderHook(() => useSidebarColapsada());
    expect(result.current.colapsada).toBe(true);
  });
});
