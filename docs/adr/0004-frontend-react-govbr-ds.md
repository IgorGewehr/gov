# ADR-0004 — Frontend React + gov.br Design System

- **Status:** Aceito
- **Data:** 2026-06-20

## Contexto

O produto exige um **padrão visual único, rigoroso e acessível** em toda a aplicação, aderente ao
**gov.br Design System (DS)** e às exigências legais de acessibilidade (**eMAG** + **WCAG 2.1 AA**).
A pesquisa de stacks apontou que o gov.br DS oferece **wrappers oficiais `@govbr-ds`** para
**React, Angular e Vue**, sendo o de **React o mais maduro**; **Blazor não tem wrapper oficial**.

## Decisão

- **Frontend = React (SPA)** em `src/Web`, **fora** da solução .NET, consumindo a **API REST** do `ApiHost`.
- Base de UI: **`@govbr-ds/core`** (CSS/JS) + **`@govbr-ds/tokens`** + **`@govbr-ds/webcomponents-react`**.
- **Design tokens como única fonte de verdade** (proibido valor hard-coded).
- **Enforcement** automatizado: `stylelint`, `eslint-plugin-jsx-a11y`, **Storybook** (todos os estados),
  testes **axe** (jest-axe/Playwright-axe) e **Definition of Done de UI**.
- Regras completas no **Design System Constitution** (`docs/design-system/`).

## Consequências

- ➕ Wrapper oficial **maduro**, ecossistema amplo, separação limpa frontend/backend (API-first).
- ➕ Acessibilidade e identidade gov.br garantidas por tokens + componentes oficiais + lint.
- ➖ **Duas stacks** (C#/.NET no backend, TypeScript/React no frontend) — aceito conscientemente.
- ➖ Versionamento do gov.br DS exige acompanhamento (v3 estável; v4/web components em evolução).
