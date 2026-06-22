# ADR-0014 — Stack de frontend: React + Vite + gov.br DS + TanStack Query + registry de módulos + `<Can>`

- **Status:** Aceito (estende o ADR-0004)
- **Data:** 2026-06-22
- **Código:** `src/Web/` (`package.json`, `src/auth/Can.tsx`, `src/modules/*`)

## Contexto

O ADR-0004 fixou **React (SPA) + gov.br Design System**, fora da solução .NET, consumindo a API
REST. Faltava decidir o **ferramental e a arquitetura interna** do SPA: build, estado de servidor,
roteamento, organização por módulo (espelhando os 11 bounded contexts e a **ativação modular por
tenant**) e o *enforcement* de autorização **na UI** (que precisa refletir RBAC+ABAC do backend —
ADR-0007 — sem ser a fonte de verdade de segurança).

## Decisão

- **Build/dev:** **Vite** (+ `@vitejs/plugin-react`) e **Vitest** para testes — *dev server*
  rápido, build com `tsc && vite build`.
- **UI:** **`@govbr-ds/core`** (v3) + tokens como única fonte de verdade (ADR-0004); React 18.
- **Estado de servidor:** **TanStack Query** (`@tanstack/react-query`) para cache/sincronização das
  chamadas à API — sem reinventar fetching/cache/invalidação.
- **Organização por módulo:** `src/modules/<modulo>/...` espelhando os bounded contexts; um
  **registry** de módulos no frontend permite montar navegação/rotas **apenas dos módulos
  licenciados** para o tenant (espelha `TenantModule` / ADR-0002).
- **Roteamento:** **`react-router-dom` v6** (não TanStack Router).
- **Autorização na UI:** componente **`<Can>`** (`src/auth/Can.tsx`) que mostra/oculta ações por
  permissão efetiva do usuário — **conveniência de UX**, **não** fronteira de segurança (o
  deny-by-default real é no backend; a UI reflete I4 mas a regra é provada no domínio — ADR-0007).
- **Acessibilidade:** eMAG + WCAG 2.1 AA com lint a11y e testes axe (ADR-0004 / Design System
  Constitution).

## Alternativas consideradas

- **Blazor (full .NET, stack única):** **sem wrapper oficial** do gov.br DS; rejeitado já no ADR-0004.
- **Next.js / SSR:** o produto é uma aplicação interna autenticada (não precisa SEO/SSR); SSR
  adicionaria infraestrutura e acoplaria o front a um runtime Node em produção. Vite SPA basta.
- **TanStack Router:** tipagem de rotas atraente, mas `react-router-dom` é maduro e suficiente;
  evitamos churn. **Redux/MobX** para estado de servidor: substituídos por TanStack Query (o estado
  é majoritariamente *server state*). Estado local de UI fica em React puro.
- **Autorização só no backend, sem `<Can>`:** funcional, mas UX ruim (botões que sempre dão 403).
  `<Can>` melhora a UX **sem** virar mecanismo de segurança.

## Consequências

- ➕ DX rápida (Vite/Vitest), *server state* resolvido (TanStack Query), UI gov.br acessível, e
  organização por módulo que **espelha a ativação modular por tenant** (front só mostra o licenciado).
- ➕ `<Can>` dá UX coerente com RBAC+ABAC sem duplicar a regra de segurança.
- ➖ **Duas stacks** (C#/.NET + TS/React) — aceito conscientemente (ADR-0004); exige sincronizar
  contratos (DTOs) e o catálogo de permissões entre back e front.
- ➖ **Risco de a UI virar "falsa fronteira":** se alguém tratar `<Can>` como segurança, abre brecha.
  Mitigação: o backend é deny-by-default e a documentação marca `<Can>` como UX-only.
- ➖ Versionamento do gov.br DS (v3 estável; v4/web components em evolução) e do registry de módulos
  exige acompanhamento.
