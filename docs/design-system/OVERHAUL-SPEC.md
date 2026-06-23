# OVERHAUL-SPEC — A "nova cara" do Tensorroot.Gov

> **Escopo:** polish visual *dentro* do gov.br DS. NÃO é rebrand, NÃO muda rotas, gating,
> contratos de API nem features. Eleva o frontend ao patamar **govtech/fintech SaaS**:
> limpo, confiável, denso de dados mas organizado — aplicando o gov.br DS com **rigor de
> layout, espaçamento, hierarquia e dataviz**.
> **Base normativa inegociável (CLAUDE.md §13 + Constitution):** gov.br DS + eMAG + WCAG 2.1 AA.
> Tokens = *single source of truth*; zero *magic number*; foco visível; contraste ≥ 4,5:1.
>
> **Como ler:** cada item traz **Queixa → Diagnóstico → Conserto**. Tudo via CSS vars em
> `src/styles/layout.css` (`:root`) + ajustes pontuais nos wrappers em `src/components/ui`.
> Arquivos < 300 linhas, PT-BR.

---

## 0. Diagnóstico (a partir dos prints do dono)

| # | Queixa (print do dono) | Causa-raiz no código atual |
|---|---|---|
| 1 | **"Consultar" torto/desalinhado do input** | `ImovelListPage.tsx`: `div.col-auto.mb-3` ao lado de `FormField` com label em cima. O botão alinha pela base do *bloco campo* (label+input), não pelo **input** → fica "subindo". O `mb-3` é um *hack* manual. |
| 2 | **Botões grandes "jogados na tela"** | Não há `Toolbar`/`ActionBar`. Ações ficam soltas em `PageHeader.actions` ou avulsas, sem agrupamento, sem hierarquia primário/secundário, todas no mesmo tamanho `md`. |
| 3 | **Painel do Gestor "terrível": caixas cinzas chapadas, sem hierarquia/cor** | `.tg-metrica` usa `background: var(--tg-bg)` (cinza) + borda — vira bloco chapado sem elevação. `--tg-bg` igual ao fundo da página → KPIs "somem". Sem barra de acento de status no card. |
| 4 | **Tabela: coluna "Ações" CORTADA na borda** | `.br-table` sem `overflow` controlado nem coluna de ação *sticky*; botão de ação em tamanho `md` força largura; sem `min-width`/coluna fixa à direita. |
| 5 | **"Rgps/Rpps" minúsculo (deveria RGPS/RPPS)** | `CargoDetailPage.tsx:61` renderiza `cargo.regime` cru (enum `"Rgps"`/`"Rpps"`). Falta um *label* de sigla maiúsculo. |
| 6 | **Empty states enormes** | `EmptyState` usa `.app-center` (`min-height: 320px`) + `fa-2x` → domina a tela mesmo dentro de um card. |
| 7 | **Visual amador / pouco profissional** | Tokens crus: 1 cinza só (`--tg-bg`), sem escala neutra; sombras inexistentes (cards "flat"); tipografia sem escala (px soltos: 26/22/17/16/14/13/12...); raios inconsistentes (8/9/100px). |

---

## 1. TOKENS refinados (antes → depois)

Definir em `:root` de `src/styles/layout.css`. Mantemos os nomes `--tg-*` existentes (consumidos
em todo o app) e **adicionamos** a escala que falta. Sempre que houver token gov.br equivalente,
usamos como *fallback right* (ex.: `var(--blue-warm-vivid-70, #1351b4)`).

### 1.1 Paleta neutra — cinza COM PROPÓSITO (mata o "cinza chapado")

```css
/* ANTES: só --tg-bg #f4f6f9, --tg-border #dde2e8, --tg-text, --tg-muted (4 níveis soltos) */
/* DEPOIS: escala neutra de 9 degraus, cada um com função semântica */
--tg-gray-0:  #ffffff;   /* surface elevada (cards)            */
--tg-gray-5:  #f7f9fc;   /* fundo de página (app canvas)       */
--tg-gray-10: #eef2f7;   /* fundo sutil de zebra/hover/inputs  */
--tg-gray-20: #e2e8f0;   /* bordas sutis (hairline)            */
--tg-gray-30: #cbd5e1;   /* bordas de ênfase / divisores fortes*/
--tg-gray-40: #94a3b8;   /* ícones decorativos, placeholders   */
--tg-gray-50: #64748b;   /* texto muted / secundário           */
--tg-gray-70: #334155;   /* texto de corpo                     */
--tg-gray-90: #0f1d2e;   /* títulos / texto forte              */

/* Remapeia os tokens antigos para a nova escala (sem tocar nos consumidores) */
--tg-bg:      var(--tg-gray-5);
--tg-surface: var(--tg-gray-0);
--tg-border:  var(--tg-gray-20);
--tg-text:    var(--tg-gray-90);
--tg-muted:   var(--tg-gray-50);
```

### 1.2 Cores semânticas (gov.br) + primária navy

```css
/* Primária institucional (mantida) */
--tg-primary:      #1351b4;   /* --blue-warm-vivid-70 */
--tg-primary-dark: #0c326f;   /* hover/pressed        */
--tg-navy:         #071d41;   /* topbar/títulos fortes*/
--tg-tint:         #e8f0fb;   /* superfície interativa clara */
--tg-tint-strong:  #d4e5fb;   /* item de menu ativo   */

/* Semânticas (texto/borda escuros = contraste AA; fundo = tint claro) */
--tg-success:     #168821;  --tg-success-bg:  #e3f5e1;  --tg-success-fg: #0d5c16;
--tg-warning:     #b86e00;  --tg-warning-bg:  #fdf3e2;  --tg-warning-fg: #8a4b00; /* texto sobre claro */
--tg-danger:      #c92a2a;  --tg-danger-bg:   #fbe7e7;  --tg-danger-fg:  #9b1c1c;
--tg-info:        #155bcb;  --tg-info-bg:     #e8f0fb;  --tg-info-fg:    #0c326f;
```
> Regra: **status nunca só por cor** — sempre cor + ícone + texto (eMAG/WCAG; Constitution §5/§6).

### 1.3 Espaçamento — escala 4/8pt (mata os `mb-3` manuais)

```css
--tg-space-1:  4px;    --tg-space-2:  8px;    --tg-space-3: 12px;
--tg-space-4: 16px;    --tg-space-5: 20px;    --tg-space-6: 24px;
--tg-space-8: 32px;    --tg-space-10: 40px;   --tg-space-12: 48px;
/* mapeia para o token gov.br: --spacing-scale-1x=8px, 2x=16px, 3x=24px… */
```
Toda margem/padding novo usa estes tokens. Banir px solto em CSS novo.

### 1.4 Tipografia — escala com hierarquia clara

```css
--tg-font-size-display: 26px;  --tg-lh-display: 1.2;   /* h1 página           */
--tg-font-size-h2:      18px;  --tg-lh-h2: 1.3;        /* título de card/seção*/
--tg-font-size-h3:      15px;  --tg-lh-h3: 1.35;       /* sub-bloco           */
--tg-font-size-body:    14px;  --tg-lh-body: 1.5;      /* corpo / tabela      */
--tg-font-size-sm:      13px;  --tg-lh-sm: 1.45;       /* labels, captions    */
--tg-font-size-xs:    11.5px;  --tg-lh-xs: 1.4;        /* overline/eyebrow    */
--tg-font-size-kpi:     24px;  --tg-lh-kpi: 1.15;      /* valor de KPI        */
--tg-weight-regular: 400; --tg-weight-medium: 500; --tg-weight-semibold: 600; --tg-weight-bold: 700;
--tg-tracking-eyebrow: 0.6px;  /* overline maiúsculo  */
```
Hierarquia: **display(700) › h2(700) › h3(600) › body(400/500) › sm(muted) › eyebrow(600 caps muted)**.

### 1.5 Sombras / Bordas / Raios (elevação REAL, não caixa chapada)

```css
--tg-shadow-xs: 0 1px 2px rgb(15 29 46 / 6%);
--tg-shadow-sm: 0 1px 3px rgb(15 29 46 / 8%), 0 1px 2px rgb(15 29 46 / 4%);  /* card padrão */
--tg-shadow-md: 0 4px 12px rgb(15 29 46 / 10%);                              /* hover/menu  */
--tg-shadow-lg: 0 12px 32px rgb(15 29 46 / 16%);                             /* modal       */
--tg-border-hair: 1px solid var(--tg-gray-20);
--tg-radius-sm: 6px;   --tg-radius-md: 10px;   --tg-radius-lg: 14px;   --tg-radius-pill: 999px;
--tg-focus-ring: 0 0 0 3px rgb(19 81 180 / 35%);  /* foco visível consistente */
```
Card = `--tg-surface` + `--tg-border-hair` + `--tg-shadow-sm`. **Nunca** `background: cinza` chapado.

---

## 2. Espec componente-a-componente (o que conserta o quê)

### 2.1 `Button` — tamanhos + ícone alinhado *(arquivo: components/ui/Button.tsx + CSS)*
- **Adicionar prop `size?: 'sm' | 'md'`** (default `md`). `sm` → `height: 32px`, `--tg-font-size-sm`; `md` → `40px`, `body`. Mapear `sm` para a classe `small` do gov.br.
- **Variantes:** `primary` (navy sólido), `secondary` (contorno azul), `ghost` (= `tertiary` atual, sem borda) — adicionar alias `ghost`→`tertiary`; `danger`.
- **Ícone alinhado:** o botão vira `inline-flex; align-items:center; gap: var(--tg-space-2)`. Conserta ícone "fora do eixo". Regra: passar ícone como filho `<i className=.. aria-hidden>` — o gap cuida do espaçamento (remover os `{' '}` manuais).
- **Estados (Constitution §3):** default/hover/pressed/focus(`--tg-focus-ring`)/disabled/loading — todos via tokens.
- **CSS** (em layout.css): `.br-button{ gap:var(--tg-space-2); border-radius:var(--tg-radius-sm); font-weight:var(--tg-weight-semibold) } .br-button.small{ height:32px } .br-button:focus-visible{ box-shadow:var(--tg-focus-ring); outline:none }`.

### 2.2 `FormField` + **novo `FormRow`** — MATA o "Consultar torto" *(arquivos: FormField.tsx, novo FormRow.tsx)*
- **Causa:** a ação alinha ao *bloco* (label em cima + input), então sobe junto com o label.
- **Conserto — novo `<FormRow>`:** layout em grid/flex onde **a ação se alinha ao INPUT, não ao label**:
  ```
  FormRow = grid de 2 áreas alinhadas pela BASELINE DO CONTROLE
  ┌───────────────────────────┐ ┌──────────────┐
  │ label                     │ │ (sem label)  │  ← linha do label
  │ [ input.....................] │ [ Consultar ] │  ← controles alinhados
  └───────────────────────────┘ └──────────────┘
  ```
  CSS: `.tg-form-row{ display:flex; gap:var(--tg-space-3); align-items:flex-end }` e a ação recebe
  `margin-bottom` = altura da linha de feedback/erro (`0` quando o input do par não tem erro) — eliminando o `mb-3` chutado.
  **Mais robusto:** a coluna da ação usa um *spacer* invisível com a mesma altura/line-height do `<label>` (`.tg-form-row-acao::before{content:''; display:block; height:calc(var(--tg-font-size-sm)*var(--tg-lh-sm)); margin-bottom:var(--tg-space-1)}`), garantindo que **input e botão fiquem na mesma linha** independ0entemente do label.
- `ImovelListPage` (e similares) troca `div.row.align-items-end > col + col-auto.mb-3` por `<FormRow>`.
- `FormField` permanece para campos isolados; ganha `--tg-font-size-sm`/`weight-semibold` no `<label>` e cor `--tg-gray-70`.

### 2.3 `Card` — elevação real *(arquivo: Card.tsx só usa classes; CSS faz o trabalho)*
- `.br-card{ background:var(--tg-surface); border:var(--tg-border-hair); border-radius:var(--tg-radius-md); box-shadow:var(--tg-shadow-sm) }`.
- `card-header{ padding:var(--tg-space-5) var(--tg-space-6); border-bottom:var(--tg-border-hair) }`, `card-content{ padding:var(--tg-space-6) }`.
- **Acento de status opcional:** `Card` ganha prop `accent?: 'primary'|'success'|'warning'|'danger'` → borda-superior 3px (`box-shadow: inset 0 3px 0 var(--tg-…)`). Usado nos cards do Painel.

### 2.4 `Table` — coluna Ações SEMPRE visível + densidade *(arquivo: DataTable.tsx + CSS)*
- **Wrapper rolável:** `.tg-table-scroll{ overflow-x:auto; border:var(--tg-border-hair); border-radius:var(--tg-radius-md) }`. A `.br-table` vive dentro.
- **Coluna "Ações" sticky à direita** (mata o corte): coluna com `key:'acoes'` recebe `align:'end'` e classe `.tg-col-acoes` → `position:sticky; right:0; background:var(--tg-surface); box-shadow:-8px 0 8px -8px rgb(15 29 46/12%)`. No header idem. **Botões de ação na tabela usam `size="sm"` `variant="ghost"`** (reduz largura, evita corte).
- **Header sticky vertical:** `thead th{ position:sticky; top:0; background:var(--tg-gray-5); z-index:1 }` (em listas longas).
- **Zebra + hover + densidade:** `tbody tr:nth-child(even){ background:var(--tg-gray-5) }`, `tbody tr:hover{ background:var(--tg-tint) }`, `td/th{ padding:var(--tg-space-3) var(--tg-space-4); font-size:var(--tg-font-size-body) }`. Alvo de toque ≥44px no mobile preservado.

### 2.5 `Tabs`/`SubNav` — barra de abas limpa, todas visíveis *(arquivo: SubNav.tsx + CSS)*
- Manter `flex-wrap` (abas nunca escondidas), **mas trocar o visual "botão"** por aba real:
  `.tg-subnav{ border-bottom:var(--tg-border-hair) }`; aba inativa = texto `--tg-gray-50`, fundo transparente; **aba ativa = sublinhado 2px `--tg-primary` + texto `--tg-primary-dark` weight-semibold** (não "pílula azul cheia"). Hover = `--tg-gray-70` + fundo `--tg-gray-5`.
- Em `SubNav.tsx`: trocar a classe `br-button small primary/secondary` por `tg-tab`/`tg-tab.active` (mantém `NavLink`/`aria-current`).

### 2.6 `EmptyState` — compacto, não domina a tela *(arquivo: EmptyState.tsx + CSS)*
- Nova prop `size?: 'inline' | 'page'` (default `inline`). `inline` → padding `var(--tg-space-8)`, ícone `fa-lg` em círculo `--tg-gray-10`, **sem** `min-height:320px`. `page` mantém o centralizado para tela 100% vazia.
- Trocar `.app-center` por `.tg-empty{ display:flex; flex-direction:column; align-items:center; text-align:center; gap:var(--tg-space-3); padding:var(--tg-space-8) }`.

### 2.7 `PageHeader` — título + subtítulo + slot de AÇÕES à direita *(já existe; refino)*
- Manter estrutura; aplicar tokens: título `--tg-font-size-display/weight-bold/gray-90`; subtítulo `--tg-font-size-sm/gray-50/max-width:70ch`; **eyebrow opcional** (overline maiúsculo `--tg-font-size-xs` com o nome do módulo) via prop `eyebrow?`.
- `actions` deve receber **um `<Toolbar>`** (não botões soltos) — ver 2.8. Borda inferior sutil: `padding-bottom:var(--tg-space-5); border-bottom:var(--tg-border-hair); margin-bottom:var(--tg-space-6)`.

### 2.8 **Novo `Toolbar`/`ActionBar`** — agrupa botões, hierarquia 1ª/2ª *(novo: components/ui/Toolbar.tsx)*
- Mata os "botões jogados". Container `inline-flex; gap:var(--tg-space-2); align-items:center`.
- Regra de hierarquia (Constitution §4 — **máx. 1 primário por contexto**): primário à **direita** (último), secundários/ghost à esquerda. Prop `align?: 'start'|'end'` (default `end`).
- Variante `<Toolbar.Group>` para separar grupos (filtros | ações) com divisor `border-left:var(--tg-border-hair)`.
- Usado em: `PageHeader.actions`, topo de listas (filtros + "Novo"), rodapé de `Modal`.

### 2.9 `Metrica`/`MetricaGrade` — KPI com elevação e cor de status *(arquivos: Metrica.tsx + CSS)*
- **Conserta o "cinza chapado":** `.tg-metrica{ background:var(--tg-surface); border:var(--tg-border-hair); border-radius:var(--tg-radius-md); box-shadow:var(--tg-shadow-xs); padding:var(--tg-space-4) var(--tg-space-5) }`.
- **Acento de status à esquerda** (4px) quando `tom!='neutro'`: `box-shadow: inset 3px 0 0 var(--tg-success|warning|danger)`. Valor usa `--tg-font-size-kpi/weight-bold`; label = eyebrow (`--tg-font-size-xs` caps `--tg-gray-50`); secundário `--tg-font-size-sm`.
- `MetricaGrade`: `gap:var(--tg-space-4)` mantém `auto-fit minmax(180px,1fr)`.

### 2.10 Sigla de regime — "Rgps/Rpps" → **RGPS/RPPS** *(arquivo: recursosHumanos.helpers.ts + CargoDetailPage.tsx)*
- Adicionar helper `formatarRegimePrev(v: string): string` que mapeia o enum cru (`'Rgps'|'Rpps'`) para a **sigla em maiúsculas** (`'RGPS'|'RPPS'`), com `<abbr title="Regime Geral de Previdência Social">` na 1ª ocorrência (Constitution §6).
- `CargoDetailPage.tsx:61` passa a usar `formatarRegimePrev(cargo.regime)` em vez do valor cru. (Não muda o contrato de API — só apresentação.)

---

## 3. Layout patterns (compor com os componentes acima)

### 3.1 Página de FORM (ex.: modais de cadastro, ApurarIptu)
```
PageHeader (eyebrow=módulo · título · subtítulo · Toolbar[ação primária à dir.])
SubNav (abas sublinhadas)
Card (elevado)
  └ fieldset/seções com <CardSecao> ; campos em grid 12-col (col-12 / col-6 / col-4)
  └ par campo+ação usa <FormRow> (alinhado ao input)
  └ rodapé: <Toolbar align=end>[Cancelar(ghost) · Salvar(primary)]
```
Larguras de campo por token de grid gov.br; nunca botão de submit "solto" — sempre em Toolbar.

### 3.2 Página de LISTA (toolbar + tabela + paginação) — *padrão Tributos/Finanças*
```
PageHeader (título · subtítulo · Toolbar[ "Novo …" primary ])
SubNav
Card (filtros)        → <FormRow> de busca + <Toolbar>[Limpar(ghost) · Consultar(primary)]
<tg-table-scroll>     → <DataTable> (header sticky, zebra/hover, coluna Ações sticky-right)
Toolbar/paginação     → contagem à esquerda, paginação gov.br à direita
```
Estado vazio = `<EmptyState size="inline">` dentro do scroll (compacto). Estado "pré-consulta" = inline também.

### 3.3 DASHBOARD (Painel do Gestor) — *mata o "terrível"*
```
PageHeader (eyebrow="Painel do Gestor" · título · período/exercício · Toolbar[filtro de exercício])
Faixa de KPIs principais  → <MetricaGrade> com cards elevados + acento de status
Grid de <CardSecao> 2-col (lg) / 1-col (mobile):
  • Execução orçamentária   • Mínimos constitucionais (semáforo)
  • Arrecadação/Dívida Ativa • Pessoal LRF (Tag de situação)  • Prestação de contas TCE
Cada CardSecao: header (h2 + subtítulo) · MetricaGrade interna · nota-caption (fundamento legal)
```
Cor = conformidade (sucesso/alerta/perigo), sempre com Tag (cor+ícone+texto). Elevação dá a "cara SaaS".

---

## Resumo da direção (12 linhas)

1. **Não é rebrand** — é o gov.br DS aplicado com rigor: a "cara nova" vem de disciplina de tokens, espaçamento e elevação, não de cores novas.
2. **Escala neutra de 9 cinzas com função** substitui o cinza único chapado — cards passam a "flutuar" sobre o canvas.
3. **Elevação real** (sombras sutis `xs/sm/md/lg` + hairline + raios consistentes) acaba com o visual "flat amador".
4. **Tipografia em escala** (display→eyebrow) cria hierarquia clara em toda tela.
5. **Espaçamento 4/8pt em tokens** elimina os `mb-3` chutados e o desalinhamento.
6. **Novo `FormRow`** alinha a ação ao **input** (não ao label) — conserta o "Consultar torto" na raiz.
7. **Novo `Toolbar`/`ActionBar`** agrupa ações com hierarquia (1 primário à direita) — fim dos "botões jogados".
8. **DataTable**: scroll controlado + **coluna Ações sticky-right** (nunca corta) + zebra/hover/header sticky + ações `sm/ghost`.
9. **SubNav vira abas sublinhadas** limpas (não pílulas azuis), todas visíveis via flex-wrap.
10. **EmptyState compacto** (`inline`) deixa de dominar a tela.
11. **Métricas/Painel do Gestor**: cards elevados com acento de status — o dashboard vira govtech/fintech.
12. **Correções pontuais:** `RGPS/RPPS` em maiúsculas (`formatarRegimePrev`), botões com ícone alinhado por `gap`, foco visível consistente — tudo dentro de eMAG/WCAG AA.

**Arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/design-system/OVERHAUL-SPEC.md`
