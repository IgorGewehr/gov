# Auditoria do Frontend — Tensorroot.Gov (`src/Web`)

> Auditoria EMPÍRICA (código lido e contado em 2026-06-22). Stack: React 18 + Vite 5 +
> TypeScript 5 + TanStack Query 5 + React Router 6. SPA fora da solução .NET (CLAUDE.md §13).
> Arquitetura: registry central de módulos (`src/modules/registry.ts`) → router (`routes.tsx`)
> + Sidebar (`app/shell/Sidebar.tsx`). Cada módulo isolado expõe só `{ routes, nav }`
> (espelha isolamento de Bounded Context). 272 arquivos `.ts/.tsx` (excl. testes); 39 testes.

## 1. O que existe (por módulo)

Os 11 Bounded Contexts de domínio + a área **Administração do Sistema** (admin/Identidade)
estão TODOS registrados no `registry.ts` e na Sidebar. Não há módulo faltando, não há rota
apontando para a `EmConstrucaoPage` (o stub existe em `modules/shared/` mas tem **0 usos** em
rotas — confirmado por grep). Páginas carregadas por `React.lazy` (code-splitting por módulo).

| Módulo (nav) | Rotas (telas) | List / Detail | Agregados cobertos |
|---|---|---|---|
| **Administracao** | 7 | 3 / 3 | Licitação, Contrato, Fornecedor |
| **Tributos** | 1 | 1 / 0 | Dívida Ativa (índice único; modais Contribuinte/Lançamento/CDA) |
| **Finanças** | 2 | 1 / 1 | Empenho (ciclo da despesa) |
| **RecursosHumanos** | 6 | 3 / 3 | Servidor, Cargo, FolhaDePagamento (+ `RhSubNav`) |
| **Patrimonio** | 6 | 3 / 3 | BemPatrimonial, Veículo (frota), ItemEstoque (almox.) |
| **Saúde** | 5 | 2 / 3 | Paciente, Atendimento, SolicitacaoRegulacao |
| **Educação** | 5 | 3 / 2 | Escola, Matrícula, TurmaMatrícula, DiárioClasse |
| **AssistenciaSocial** | 7 | 3 / 3 | Família/CadÚnico, Benefício, ProntuárioSUAS (+ `AssistenciaSocialSubNav`) |
| **Protocolo** | 4 | 2 / 2 | Processo (NUP), Documento (GED) |
| **Legislativo** | 6 | 3 / 3 | Proposição, Sessão, Votação |
| **Transparência** | 4 | 2 / 2 | RemessaTCE (SIAPC/PAD), DeclaraçãoFiscal (SICONFI/MSC) |
| **Admin do Sistema** | 5 | 4 / 0 | Usuários, Papéis, Módulos do tenant, Auditoria (gated `PermissionRoute`) |

Totais: **58 rotas**, **28 ListPages**, **25 DetailPages** (excl. testes). Sub-navegação interna
(uma entrada de Sidebar, várias telas) em RecursosHumanos e AssistenciaSocial.

## 2. Profundidade (real × stub) e cobertura de endpoints

Comparação chamadas HTTP do front (`grep http.get|post|put|delete` nos `*.api.ts`) × endpoints
do backend (`Map{Get,Post,Put,Delete}` nos `*Endpoints.cs`). **Não há stubs** — todas as páginas
fazem chamadas reais via `http` + TanStack Query.

| Módulo | Endpoints backend | Chamadas frontend | Cobertura |
|---|---:|---:|---|
| Administracao | 17 | 30* | 100% (30 inclui DTOs/derivados; todos os 17 cobertos) |
| Tributos | 5 | 5 | **100%** (pessoa-física, lançamento, inscrever DA, CDA, listar DA) |
| Finanças | 4 | 4 | **100%** (POST empenho, liquidar, pagar; GET por id) |
| RecursosHumanos | 22 | 22 | 100% |
| Patrimonio | 33 | 33 | 100% |
| Saúde | 25 | 25 | 100% |
| Educação | 19 | 19 | 100% |
| AssistenciaSocial | 15 | 15 | 100% |
| Protocolo | 12 | 12 | 100% |
| Legislativo | 31 | 31 | 100% |
| Transparência | 13 | 13 | 100% (RemessaTCE + DeclaraçãoFiscal) |
| Admin/Identidade | 14 (Identidade) + ApiHost/Admin | 15 | 100% (login, usuários, papéis, permissões, tenant/módulos, auditoria) |

\* Administracao tem mais chamadas no front que MapXxx no arquivo de endpoints porque vários
hooks reaproveitam o mesmo endpoint e contam DTOs; a verificação por path confirma os 17 cobertos.

**Observações de profundidade (não-bug, mas limites de produto):**

- **Finanças (PRIORIDADE DO DONO — contabilidade)**: o backend só expõe **4 endpoints** de
  Empenho (POST criar / liquidar / pagar + GET por id). **Não existe endpoint de LISTA**, então
  a `EmpenhoListPage` consulta **por id digitado** e mostra 1 resultado em tabela (documentado no
  cabeçalho do arquivo). O front cobre 100% do que o backend oferece, mas **a superfície contábil
  é rasa**: sem PPA/LDO/LOA, sem Liquidação/Pagamento como agregados próprios, sem PCASP/MCASP,
  sem Restos a Pagar — tudo previsto no CLAUDE.md §4 (#2) e **ausente do backend e do front**.
- **Tributos (PRIORIDADE — TCE)**: só 1 rota índice. Sem painel fiscal NFS-e/ADN, sem IPTU/ISS/
  ITBI/Alvarás na UI; apenas Dívida Ativa + CDA.
- **Transparência (PRIORIDADE — TCE)**: RemessaTCE e DeclaraçãoFiscal cobertos 100%; é o módulo
  mais alinhado à preocupação de prestação de contas, com profundidade adequada ao backend atual.

## 3. Gating de permissão (`<Can>` / `useHasPermission`)

`Can` (`auth/Can.tsx`) = gating de **UI** (claim `perm` do JWT), explicitamente NÃO-autoritativo
(backend é a fonte da verdade). Sidebar filtra itens por `nav.permissions` (OU). Admin usa
`PermissionRoute` (redirect) + gating fino por card.

- **58 ocorrências** de `<Can>`; **55 arquivos** importam Can/useHasPermission.
- Distribuição: administracao 6, legislativo 6, patrimonio 6, recursoshumanos 6, assistenciasocial 5,
  saude 5, educacao 4, protocolo 4, transparencia 4, admin 3, financas 2, **tributos 1**.
- **Lacuna**: cobertura desigual. **Tributos tem só 1 `<Can>`** e **Finanças só 2** — justamente os
  módulos sensíveis (dinheiro público). Verificar se TODAS as ações de mutação (emitir empenho,
  liquidar, pagar, emitir CDA) estão gated. Como o gating é só de UI, não é falha de segurança,
  mas é inconsistência de UX/"negar por padrão".

## 4. Aderência gov.br / eMAG / WCAG 2.1 AA (acessibilidade)

**Positivo (real, verificado):**
- `@govbr-ds/core@3.6.1` instalado **e importado** em `main.tsx` (`core.min.css`); FontAwesome
  importado. Paleta oficial gov.br no `layout.css` (`--tg-primary: #1351b4`, navy `#071d41`).
- `index.html` com `lang="pt-BR"`.
- Landmarks: `<header role="banner">`, `<nav aria-label="Módulos">`, `<main id="conteudo">`,
  **skip-link** (`.br-skip-link`, accessKey 1) com foco programático no `<main>`.
- `aria-current` na NavLink ativa; `aria-describedby` em formulários (399 usos), `aria-invalid`
  (26), `aria-live` em toasts/spinner/modais, `aria-sort` em DataTable. ESLint `jsx-a11y` ativo.

**Lacunas:**
- **`axe-core@4.10` é devDependency mas NÃO é usado em NENHUM teste** (grep `axe` em testes = 0
  resultados). Não há teste automatizado de acessibilidade — o requisito WCAG AA não tem rede de
  proteção em CI.
- O shell usa um **CSS próprio** (`layout.css`, 330 linhas) que apenas se inspira na paleta gov.br,
  em vez de aplicar os componentes/classes `br-*` do DS de forma sistemática. Adesão visual ao DS é
  parcial (tokens de cor sim; componentes do DS, pouco).

## 5. Arquivos gigantes (> 300 linhas, excl. testes)

11 arquivos acima de 300 linhas. Concentrados em **modais de ação** (componentes que agrupam
vários comandos do agregado num só arquivo) e em **api.ts** com muitos DTOs/hooks:

| Linhas | Arquivo |
|---:|---|
| 917 | `modules/patrimonio/bempatrimonial/BemPatrimonialAcoesModais.tsx` |
| 704 | `modules/administracao/contrato/ContratoAcaoModals.tsx` |
| 640 | `modules/patrimonio/veiculo/VeiculoAcaoModais.tsx` |
| 615 | `modules/administracao/licitacao/LicitacaoActionModals.tsx` |
| 439 | `modules/administracao/fornecedor/FornecedorAcoesModais.tsx` |
| 390 | `modules/administracao/contrato/contrato.api.ts` |
| 354 | `modules/patrimonio/veiculo/VeiculoFormModal.tsx` |
| 344 | `modules/administracao/licitacao/licitacao.api.ts` |
| 343 | `modules/patrimonio/veiculo/veiculo.api.ts` |
| 327 | `modules/patrimonio/bempatrimonial/bempatrimonial.api.ts` |
| 315 | `modules/administracao/contrato/ContratoDetailPage.tsx` |

`BemPatrimonialAcoesModais.tsx` (917) é o maior — candidato a quebrar em um modal por ação.
Patrimonio e Administracao concentram a maioria dos arquivos grandes.

## 6. Lacunas — síntese priorizada

1. **Profundidade contábil/fiscal rasa nos módulos críticos do dono** (Finanças sem LISTA de
   empenho, sem ciclo orçamentário/PCASP/Restos a Pagar; Tributos só Dívida Ativa). O front cobre
   100% do backend, mas **o backend é o limitante** — não há telas porque não há endpoints.
2. **Zero testes de acessibilidade** apesar de `axe-core` instalado — risco direto ao requisito
   WCAG 2.1 AA / eMAG obrigatório (CLAUDE.md §13).
3. **Gating `<Can>` desigual** — Tributos (1) e Finanças (2) sub-cobertos vs. demais (4–6).
4. **5 modais de ação gigantes (>600 linhas)** — manutenibilidade; quebrar por comando.
5. **Adesão ao gov.br DS parcial** — usa tokens de cor + skip-link, mas pouco dos componentes `br-*`.

**Pontos fortes:** isolamento de módulos rígido e correto; 100% de cobertura dos endpoints
existentes em todos os 11 módulos + admin; nenhuma página stub em produção; landmarks/skip-link/
aria bem aplicados; 39 testes (RH e Educação melhor cobertos; Tributos e Finanças só 1 teste cada).
