# Tensorroot.Gov — Frontend (SPA React)

SPA em **React + TypeScript (Vite)** que consome a API .NET (`ApiHost`), aderente ao
**gov.br Design System** e ao [`docs/design-system/`](../../docs/design-system/README.md)
(*Design System Constitution*).

## Como rodar

```bash
cd src/Web
npm install
npm run dev      # http://localhost:5173 (proxy /api -> http://localhost:5080)
```

## Adesão ao Design System (enforcement)

| Mecanismo | Garante |
|---|---|
| `@govbr-ds/core` | Tokens, componentes e estilos oficiais (fonte única). |
| `eslint-plugin-jsx-a11y` | Acessibilidade no JSX (eMAG / WCAG 2.1 AA) — `npm run lint`. |
| `stylelint` | Bloqueia `!important` e incentiva tokens (sem valores hard-coded). |
| `lang="pt-BR"`, skip-link, `caption`/`scope`, `role`, rótulos | Acessibilidade objetiva. |
| `Intl.NumberFormat('pt-BR')` | Formatação de moeda/locale centralizada. |

## Estrutura

```
src/
  api/apiClient.ts                 cliente da API (JWT + tenant)
  modules/tributos/DividaAtivaPage.tsx   tela de Dívida Ativa
  App.tsx                          shell (header/footer gov.br + breadcrumb)
  main.tsx                         bootstrap + import do gov.br DS
```

> O menu/telas respeitam a **ativação modular por tenant**: só aparecem os módulos
> licenciados para o ente (espelhando `modulosAtivos` da API).
