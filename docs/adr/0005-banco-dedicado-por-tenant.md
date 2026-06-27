# ADR-0005 — Banco DEDICADO por tenant (database-per-tenant), não schema-por-módulo

- **Status:** Aceito (aprofunda e firma a decisão esboçada no ADR-0002)
- **Data:** 2026-06-22

## Contexto

O Tensorroot.Gov processa **dinheiro público** e dados sensíveis sob escrutínio do
Tribunal de Contas (TCE-RS) e da LGPD. O isolamento entre tenants — onde Executivo e
Legislativo do mesmo município são tenants distintos — é requisito de compliance, não
preferência. A `CONVENCOES-ENGENHARIA.md` (§9) descreve "um `DbContext` por módulo, **schema
isolado**", o que num modelo *shared-database* deixaria todos os tenants partilhando a
mesma base física, com isolamento apenas lógico (Global Query Filter por `TenantId`).

Editais públicos e auditorias de contas frequentemente exigem **isolamento físico** dos
dados de um ente (backup/restore independente, extração íntegra para o TCE, possibilidade
de export/portabilidade por ente). Um vazamento cross-tenant num sistema que move verba
pública é falha crítica de difícil recuperação reputacional.

## Decisão

Adotar **um banco de dados DEDICADO por tenant** (*database-per-tenant*):

- Um **DB central de plataforma** guarda o catálogo de tenants, as *connection strings*
  dedicadas (segredos no Key Vault) e as licenças de módulo (`TenantModule`).
- `ITenantConnectionResolver` (em `BuildingBlocks.Application`, impl em
  `Platform/Tenancy/TenantConnectionResolver.cs`) resolve, em runtime, a conexão do tenant
  corrente a partir do `ITenantContext` (JWT) ou, nos Workers, da iteração explícita.
- Dentro do banco de cada tenant mantém-se **schema por módulo** (`financas`, `tributos`,
  `saude`, …) — a separação por módulo continua, mas **dentro** da fronteira física do tenant.
- O **Global Query Filter** por `TenantId` e o `TenantSaveChangesInterceptor` permanecem
  como **defesa em profundidade** (não como mecanismo primário de isolamento).
- Em DEV/local: **SQLite + `SchemaProvisioner`** (um arquivo por tenant), com o mesmo
  resolver — paridade conceitual com PROD.

## Alternativas consideradas

- **Shared-database, schema-per-module (modelo original da §9):** isolamento só lógico
  por `TenantId`. Mais simples e barato, mas um bug no filtro global vaza dados de dinheiro
  público entre entes. Rejeitado como modelo primário.
- **Schema-per-tenant (um schema por ente no mesmo banco):** isolamento intermediário, mas
  não dá backup/restore nem export independentes por ente, e colide com "schema por módulo".
- **Banco-por-módulo-por-tenant:** isolamento máximo, mas explode o número de bancos
  (11 módulos × N tenants) e quebra transações locais entre módulos do mesmo ente. Rejeitado.

## Consequências

- ➕ Isolamento **físico** — atende auditoria/editais; backup, restore, export e
  *portabilidade* por ente são triviais; "blast radius" de um incidente fica contido a um tenant.
- ➕ Custódia de segredos do tenant (ex.: certificado A1 cifrado — ver ADR-0009) co-localizada
  com a trilha de auditoria do próprio ente.
- ➖ **Custo operacional maior**: provisionamento, migrations e monitoração por banco; *fan-out*
  de operações administrativas. Mitigado por automação (`SchemaProvisioner`, migrations por módulo).
- ➖ **Sem JOIN cross-tenant** nem consultas administrativas globais triviais — consolidações
  (ex.: relatórios de plataforma) exigem agregação explícita e auditada, nunca leitura cruzada.
- ➖ **Diverge da `CONVENCOES-ENGENHARIA.md` §9** ("um DbContext por módulo, schema isolado" lido como
  shared-DB): esta decisão re-significa "schema isolado" como *schema por módulo dentro do
  banco dedicado do tenant*. As Convenções de Engenharia devem ser lidas sob esta ADR.
- 🔗 Trade-off honesto: ganhamos isolamento e compliance ao preço de operação mais cara — para
  o perfil (poucas centenas de entes, dado regulado) o isolamento vale o custo.
