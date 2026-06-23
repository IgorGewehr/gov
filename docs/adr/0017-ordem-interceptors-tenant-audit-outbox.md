# ADR-0017 — Ordem dos interceptors Tenant→Audit→Outbox unificada por helper (corrige W0.2)

- **Status:** Aceito (fecha o achado W0.2 — `TenantId=Guid.Empty` na trilha em inserções)
- **Data:** 2026-06-22
- **Código:** `BuildingBlocks.Infrastructure/ModuleInterceptorRegistration.cs`,
  `BuildingBlocks.Infrastructure/Multitenancy/TenantSaveChangesInterceptor.cs`,
  `BuildingBlocks.Infrastructure/Auditing/AuditSaveChangesInterceptor.cs`,
  `BuildingBlocks.Infrastructure/Outbox/ConvertDomainEventsToOutboxInterceptor.cs`,
  call sites em `Modules/*/.../<Modulo>Module.cs`

## Contexto

A base de persistência registra três `SaveChangesInterceptor`: **Tenant** (carimba `TenantId` em
entidades novas e bloqueia gravação cross-tenant), **Audit** (escreve a `AuditTrail` e encadeia o
hash por tenant — ADR-0016) e **Outbox** (materializa eventos na tabela Outbox — ADR-0011). O EF Core
invoca `SavingChanges` **na ordem de registro**. Cada um dos 13 módulos replicava o
`AddInterceptors(...)` em **ordem divergente** (Audit **antes** de Tenant). Resultado (achado
**W0.2**): para entidades cujo *factory* **não** define o `TenantId` explicitamente, a auditoria lia
`entry.Entity.TenantId` **antes** do carimbo do Tenant e gravava **`Guid.Empty`** na trilha —
quebrando o filtro/segregação por tenant da auditoria e fazendo a trilha do TCE **perder inserts**.

## Decisão

Centralizar o registro num **ponto único**, com **ordem fixa Tenant → Audit → Outbox**:

- **`ModuleInterceptorRegistration.AddModuleSaveChangesInterceptors(serviceProvider)`** registra,
  nesta ordem exata: `TenantSaveChangesInterceptor`, depois `AuditSaveChangesInterceptor`, depois
  `ConvertDomainEventsToOutboxInterceptor`.
- **Por que a ordem importa:** (1) Tenant carimba o `TenantId` primeiro; (2) Audit lê o `TenantId`
  **já carimbado** (corrige W0.2) e sela a linha na cadeia de hash por tenant; (3) Outbox materializa
  os eventos por último, já com tenant e auditoria consolidados.
- **Todo módulo chama o helper** no `OnConfiguring`/instalador do seu `DbContext`, herdando a ordem
  correta **por construção** — elimina a divergência replicada e o risco latente de reintroduzi-la.

## Alternativas consideradas

- **Corrigir a ordem manualmente nos 13 módulos:** corrige o sintoma mas mantém 13 cópias do mesmo
  registro — qualquer módulo novo (ou refactor) reintroduz W0.2. Rejeitado em favor do helper único.
- **Tornar o `AuditInterceptor` independente da ordem (resolver o tenant por outra via):** o
  interceptor teria de reimplementar a lógica de carimbo do Tenant — duplicação e acoplamento.
  Rejeitado.
- **Exigir que todo *factory* defina `TenantId`:** frágil (depende de disciplina em cada agregado) e
  contraria o desenho de o `TenantInterceptor` ser a fonte do carimbo. Rejeitado.

## Consequências

- ➕ A trilha registra **sempre o `TenantId` correto**, inclusive para entidades sem tenant
  pré-setado — fecha W0.2 e preserva a segregação por tenant da auditoria (ADR-0016) e o isolamento
  multi-tenant (CLAUDE.md §5).
- ➕ Ordem **garantida por construção** num único lugar; módulos novos a herdam ao chamar o helper.
- ➕ Casa com o ADR-0016 (hash-chain por tenant precisa do `TenantId` já carimbado para selar na
  cadeia certa) e com o ADR-0011 (Outbox materializa por último).
- ➖ **Acoplamento implícito à ordem:** quem escrever um interceptor novo precisa decidir
  conscientemente sua posição no helper; mitigar por teste de regressão que prove `AuditTrail.TenantId`
  correto para entidade sem `TenantId` pré-setado.
- ➖ O helper é o **único** ponto de verdade: um módulo que registre interceptors **por fora** (sem
  chamar o helper) escapa da garantia — cobrir por revisão/teste.
