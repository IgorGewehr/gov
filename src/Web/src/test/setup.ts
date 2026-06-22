// Setup global do Vitest: matchers do jest-dom + vitest-axe (toHaveNoViolations)
// + limpeza automática do DOM. Acessibilidade (CLAUDE.md §13: WCAG 2.1 AA / eMAG)
// passa a ser verificável nos testes via expect(...).toHaveNoViolations().
import '@testing-library/jest-dom/vitest';
import { afterEach, expect } from 'vitest';
import { cleanup } from '@testing-library/react';
import * as axeMatchers from 'vitest-axe/matchers';

expect.extend(axeMatchers);

afterEach(() => {
  cleanup();
});
