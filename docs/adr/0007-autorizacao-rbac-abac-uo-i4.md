# ADR-0007 — Autorização: RBAC dinâmico + ABAC organizacional (UO) + regra I4, com separação de poderes por tenant

- **Status:** Aceito
- **Data:** 2026-06-22
- **Fonte:** `docs/planejamento/MODELO-AUTORIZACAO-ORGANIZACIONAL.md`

## Contexto

O RBAC inicial é **plano e tenant-scoped**: `Usuario.HashSet<PapelId>` → `Papel.HashSet<string>`
(catálogo canônico de escopos `modulo.ver`/`modulo.gerenciar`), claims `perm` no JWT,
deny-by-default na borda. Esse modelo tem três limites para o setor público:

1. **Sem dimensão organizacional:** quem tem `financas.gerenciar` gerencia o ente inteiro; não
   existe "empenho só da Secretaria de Saúde". Mas empenho, dotação e patrimônio são **por
   Unidade Orçamentária/Gestora** (alinhamento exigido por SIAPC/PAD e MSC/SICONFI).
2. **Sem ABAC:** nada condiciona o acesso por atributo do recurso (UO dona, sensibilidade LGPD)
   nem do sujeito (lotação).
3. **Sem delegação com escopo:** `identidade.usuarios.gerenciar` é tudo-ou-nada — viola a regra
   do dono **"não delega o que não tem"**.

## Decisão

Evoluir para **RBAC dinâmico + ABAC organizacional**, mantendo o catálogo de permissões canônico:

- **RBAC dinâmico:** `PermissionPolicyProvider` materializa políticas `perm:<escopo>` sob demanda
  (não há política estática por permissão); deny-by-default por presença de claim na borda.
- **ABAC organizacional (nova dimensão):**
  - `UnidadeOrganizacional` — árvore de Secretarias/Departamentos/Setores do ente (auto-FK),
    com ponte opcional à Unidade Gestora contábil (alinha autorização e prestação de contas).
  - `AtribuicaoDePapel` substitui o `HashSet<PapelId>` plano: `(Usuario, Papel, UnidadeId,
    IncluiSubunidades, Vigencia, Origem)`. Permissão efetiva passa a ser **(permissão → conjunto
    de UOs)**, não um set global — o `EscopoEfetivo` do sujeito.
  - **Filtro de UO** (espelha o Global Query Filter de tenant) via marcador `IMustHaveUnidade`,
    restringindo leituras às UOs do escopo; guards de command-side no handler.
  - `NivelSensibilidade` (`Publico < Interno < Restrito < SensivelLGPD`) como ABAC do recurso +
    trilha de leitura LGPD.
- **Regra-mãe I4 — "não delega o que não tem":** toda concessão ⊆ permissões efetivas do
  concedente no escopo, **provada no domínio** (`ConcessaoDelegacao`, algoritmo D1–D6), não só na UI.
- **Separação de poderes por tenant (invariante dura):** Executivo e Legislativo do mesmo
  município são tenants distintos (CNPJs distintos); **nenhuma** concessão cross-tenant; não
  existe super-admin global de cliente. Único acoplamento legítimo = troca de artefatos
  auditados (ex.: duodécimo via Outbox), nunca leitura cruzada de banco.

## Alternativas consideradas

- **Permanecer no RBAC plano:** insuficiente para "empenho da Saúde só na dotação da Saúde" e
  para a SoD exigida pelo TCE. Rejeitado.
- **ABAC puro (policy engine externo, ex. OPA/Rego):** poderoso, mas tira a regra do domínio,
  adiciona dependência e dificulta provar I4/SoD como invariante. Rejeitado — preferimos
  **ABAC modelado no domínio** (entidades + invariantes provadas).
- **Escopo de UO no JWT:** inflaria o token em entes com muitas UOs e impediria revogação
  imediata. Decidido **resolver o escopo server-side por requisição** (cache + invalidação),
  mantendo o token enxuto (alinha com ADR-0006).

## Consequências

- ➕ Autorização e prestação de contas passam a falar a **mesma dimensão organizacional**
  (UO↔UG), o que é exigência de qualidade do TCE.
- ➕ Delegação seletiva por secretaria com a garantia matemática do I4; SoD (segregação de
  funções) modelável (quem liquida ≠ quem paga).
- ➕ Separação de poderes é estrutural (fronteira de tenant), não política de aplicação.
- ➖ **Alto esforço:** UO, `AtribuicaoDePapel`, filtro de UO e delegação **não existem** hoje;
  exige migração de dados (cada par `(Usuario, PapelId)` vira `AtribuicaoDePapel` na UO raiz
  com `IncluiSubunidades=true`, preservando o comportamento global no go-live).
- ➖ Várias dimensões (tenant × permissão × UO × sensibilidade) aumentam a superfície de teste —
  invariantes I1–I10 viram testes obrigatórios.
- ➖ Pontos jurídicos `[a confirmar]` antes de congelar: matriz oficial de SoD do TCE-RS, registro
  de base legal LGPD, verbos finos de atos com efeito legal, política de subdelegação.
- 🔗 Faseado: M1 = UO + `AtribuicaoDePapel` + filtro de UO + RBAC no `/admin/tenants` + verbos SoD
  de Finanças; M1.x/M2 = delegação D1–D6, sensibilidade/clearance e trilha de leitura LGPD plenas.
