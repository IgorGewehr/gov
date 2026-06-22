# Design System Constitution — Tensorroot.Gov

> **Padrão visual ÚNICO e OBRIGATÓRIO de toda a aplicação.** Estas regras são *fitness functions*
> de UI: aplicadas por lint, testes e *Definition of Done* — **nenhuma tela foge do padrão**.
> Base normativa: **gov.br Design System (DS)** + **eMAG** + **WCAG 2.1 AA**.
> Stack: **React (SPA)** com `@govbr-ds` ([ADR-0004](../adr/0004-frontend-react-govbr-ds.md)).

---

## 1. Fundamentos & Marco

O gov.br DS (mantido pelo SERPRO/Governo Digital) é o **padrão obrigatório** para serviços públicos
federais e referência consolidada para estados e municípios. Conformidade legal cumulativa:

- **gov.br DS** — identidade visual, design tokens e componentes.
- **eMAG 3.1** — Modelo de Acessibilidade em Governo Eletrônico (**todas** as recomendações são exigíveis).
- **WCAG 2.1 nível AA** — referência internacional adotada pelo eMAG.

**Princípios transversais:** *mobile-first*; **conteúdo antes do *chrome*** (HTML do conteúdo precede o
menu); **linguagem cidadã**; **densidade controlada para ERP** (tabelas, formulários longos, dashboards).

---

## 2. Design Tokens — *single source of truth*

Fonte única: **`@govbr-ds/core`** + **`@govbr-ds/tokens`**, expostos como CSS custom properties.
**Proibido valor hard-coded** (cor, tamanho, espaçamento, raio) — bloqueado por `stylelint`.

| Categoria | Regra |
|---|---|
| **Cor** | Paleta institucional (`--blue-warm-*`, escala `--gray-*`) + semânticas `--*-success/warning/danger/info`. Tokens de interação: `--interactive`, `--hover`, `--pressed`, `--focus`, `--visited`. |
| **Tipografia** | **Rawline** (principal), **Raleway** + sans-serif como fallback. Base **16px**; escala `--font-size-scale-*`; pesos 400/500/600/700; `line-height` por token. |
| **Espaçamento** | Base **8px**: `--spacing-scale-half` (4), `--spacing-scale-1x` (8), `2x` (16), `3x` (24)… Padding/margin sempre por token. |
| **Grid** | **12 colunas** (desktop) → proporcional (tablet) → foco vertical (mobile). Container com *gutter* por token. |
| **Breakpoints** | **sm 576px · md 992px · lg 1280px** (xl acima). |
| **Raio / Sombra** | `--surface-rounder-sm/md/lg/pill` · `--surface-shadow-sm/md/lg`. |
| **z-index / Motion** | Escala semântica (dropdown/modal/tooltip). `--duration` + easing por token; respeitar `prefers-reduced-motion`. |

---

## 3. Biblioteca de Componentes

Usar **exclusivamente** componentes do gov.br DS (via `@govbr-ds/webcomponents-react`): Botão, Input,
Textarea, Select/Multi-select, Checkbox, Radio, Switch, **Message/Feedback** (success/warning/danger/info),
Modal, Card, **Tabela** (com `caption`/`scope`), Breadcrumb, Menu, **Header e Footer padrão gov.br**,
Paginação, Loading, Tooltip, Tag, Tabs, Collapse, Date/Time Picker, Upload, Notification, Avatar, Cookie Bar.

> **Não criar componente próprio** quando existir equivalente no DS. Novos componentes só via *wrapper*
> documentado no Storybook, usando tokens.

**Estados obrigatórios** em todo elemento interativo: `default`, `hover`, `focus` (**foco visível**),
`active/pressed`, `disabled` (+ `error` e `loading`/`selected` quando aplicável). Inputs **sempre** com
`label` associado, estado de erro com **mensagem + ícone**.

---

## 4. Layout & Navegação

- **Header e Footer gov.br** padronizados em **todas** as telas (assinatura institucional, login, busca, avatar).
- **Breadcrumb obrigatório** em telas internas (eMAG 3.4 — informar localização).
- Estrutura semântica: `header` · `nav` · `main` · `footer`.
- **Densidade de ERP:** tabelas com cabeçalho fixo e **paginação server-side**; densidade compacta opcional
  (sem violar alvo de toque **≥ 44px** no mobile); formulários longos seccionados em `fieldset`/abas;
  dashboards em grid de cards. **Máximo 1 botão primário por contexto**; ações primárias à direita.

---

## 5. Acessibilidade (regras objetivas — eMAG / WCAG 2.1 AA)

- **Contraste mínimo 4,5:1** (texto normal); **3:1** (texto grande e ícones de UI).
- **100% operável por teclado**, sem armadilha de foco; ordem de tabulação lógica.
- **Foco sempre visível** — **proibido** `outline: none` sem substituto equivalente.
- **Accesskeys gov.br:** conteúdo = **1**, menu = **2**, busca = **3**; primeiro link **salta para o conteúdo**.
- `<html lang="pt-BR">`; trechos em outro idioma marcados com `lang`.
- **`alt`** em imagem informativa; `alt=""` em decorativa; descrição longa para gráficos/dashboards.
- Tabelas de dados com `<caption>` e `<th scope>`/`headers`; **nunca** tabela para layout.
- Título de página descritivo: **"[Assunto] — Tensorroot.Gov"**.
- **ARIA** só quando o HTML semântico não bastar: `aria-live` para feedback, `aria-invalid` + `aria-describedby` em erros.
- Sem *auto-refresh*/redirect inesperado; nada piscando **> 3×/s**; conteúdo em movimento pausável; respeitar `prefers-reduced-motion`.

---

## 6. Consistência, Conteúdo & i18n

- **Linguagem cidadã:** frases curtas, voz ativa, sem jargão jurídico/burocrático; siglas expandidas na 1ª ocorrência (`<abbr>`).
- **Locale pt-BR:** data `dd/mm/aaaa`, moeda `R$`, números — via **i18n centralizado** (proibida string solta no JSX).
- **Ícones:** Font Awesome (padrão gov.br) com `aria-hidden` + texto/label.
- **Mensagens de erro padronizadas** (componente Message/Feedback): tom respeitoso, explicam **causa + ação corretiva**,
  próximas ao campo, com **cor + ícone + texto** (**nunca só cor**).

---

## 7. Stack & Estrutura (`src/Web`)

```
src/Web/
├── package.json            # React + TypeScript (Vite)
├── src/
│   ├── design-system/      # wrappers @govbr-ds/webcomponents-react + tokens
│   ├── i18n/               # pt-BR centralizado
│   ├── modules/<Modulo>/   # telas por Bounded Context (ativadas conforme tenant)
│   ├── api/                # client da API .NET (Axios + React Query)
│   └── app/                # shell (Header/Footer gov.br, rotas, auth)
└── .storybook/             # documentação viva de componentes
```

- Dependências base: **`@govbr-ds/core`** (CSS/JS) + **`@govbr-ds/tokens`** + **`@govbr-ds/webcomponents-react`**.
- **TypeScript estrito**; tokens importados como CSS vars; **zero** *magic numbers*.
- Telas respeitam a **ativação modular por tenant**: o menu só exibe módulos licenciados (espelha o backend).

---

## 8. Enforcement (como a regra é cumprida)

| Mecanismo | O que garante |
|---|---|
| **stylelint** | Bloqueia cores/medidas hard-coded — força uso de tokens. |
| **eslint + eslint-plugin-jsx-a11y** | Build **falha** em violação de acessibilidade no JSX. |
| **Storybook** (addon-a11y) | Cada componente documentado com **todos os estados** + snapshot de acessibilidade. |
| **jest-axe / Playwright-axe** | Testes automatizados: **zero** violação crítica do axe-core. |
| **Lighthouse a11y ≥ 90** | Gate de acessibilidade no CI. |
| **Definition of Done de UI** | Checklist obrigatório no PR (abaixo). |

### Definition of Done de UI (checklist de PR)

- [ ] Usa componente do **gov.br DS** (sem componente próprio redundante).
- [ ] **Tokens** para toda cor/tamanho/espaçamento (zero *magic number*).
- [ ] **5 estados** implementados (default/hover/focus/active/disabled) + erro/loading quando aplicável.
- [ ] **Foco visível** e navegação 100% por teclado.
- [ ] **Contraste ≥ 4,5:1**; `lang`, `alt`, `label`, `caption`/`scope` corretos.
- [ ] Testado em **sm/md/lg**; alvo de toque ≥ 44px no mobile.
- [ ] **axe sem violações críticas**; Lighthouse a11y ≥ 90.
- [ ] Texto em **linguagem cidadã** e via **i18n** (sem string solta).
- [ ] Menu/rotas respeitam a **ativação modular por tenant**.

---

## 9. Fontes

- gov.br DS — Fundamentos visuais: https://www.gov.br/ds/fundamentos-visuais/tipografia · /grid · /espacamento
- gov.br DS — Design Tokens (v4): https://next-ds.estaleiro.serpro.gov.br/fundamentos/design-token
- eMAG: https://emag.governoeletronico.gov.br/ · https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/acessibilidade-digital
- WCAG 2.1: https://www.w3.org/TR/WCAG21/
- Pacotes: https://www.npmjs.com/~govbrds (`@govbr-ds/core`, `@govbr-ds/tokens`, `@govbr-ds/webcomponents-react`)
