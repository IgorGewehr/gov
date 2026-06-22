// Helper de acessibilidade para os testes (CLAUDE.md §13: WCAG 2.1 AA / eMAG).
// Roda o axe-core sobre um container já renderizado. A regra `color-contrast`
// é desabilitada: o jsdom não tem layout/canvas reais (e os testes rodam com
// `css: false`), então o axe não consegue medir contraste de forma confiável —
// o contraste é garantido pelo gov.br Design System e revisado no navegador.
// Todas as demais regras WCAG 2.1 A/AA permanecem ativas.
import { axe } from 'vitest-axe';
import type { AxeResults } from 'axe-core';

export function checarAcessibilidade(container: Element): Promise<AxeResults> {
  return axe(container, {
    rules: {
      'color-contrast': { enabled: false },
    },
  }) as unknown as Promise<AxeResults>;
}
