// Augmentação de tipos do matcher de acessibilidade para o Vitest 2.x. O pacote
// `vitest-axe@0.1.0` só declara o matcher no namespace global `Vi` (Vitest 0.x),
// que o Vitest 2.x não usa para `expect(...)`. Aqui estendemos a interface
// `Assertion` do módulo `vitest` para que `toHaveNoViolations()` seja tipado.
import type { AxeResults } from 'axe-core';

interface AxeMatchers<R = unknown> {
  toHaveNoViolations(): R;
}

/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars, @typescript-eslint/no-empty-object-type */
declare module 'vitest' {
  interface Assertion<T = any> extends AxeMatchers {}
  interface AsymmetricMatchersContaining extends AxeMatchers {}
}
/* eslint-enable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars, @typescript-eslint/no-empty-object-type */

// Garante que o tipo de retorno do `axe()` esteja disponível para o consumidor.
export type { AxeResults };
