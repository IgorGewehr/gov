# ADR-0002 — Multi-tenancy e Ativação Modular por Tenant

- **Status:** Aceito
- **Data:** 2026-06-20

## Contexto

O sistema atende **Executivo e Legislativo do mesmo município como tenants distintos** (CNPJs
diferentes) e diferentes prefeituras. Cada tenant **licencia um subconjunto dos 11 módulos** —
uma prefeitura pode usar **só Saúde**, **só Educação**, **só Legislativo**, etc. O isolamento de
dados é requisito de compliance (dinheiro público, LGPD, Tribunal de Contas).

## Decisão

**Isolamento de tenant (linhas):**
- `IMustHaveTenant { Guid TenantId }` em toda raiz de agregado persistida.
- **Global Query Filter** por `TenantId` em todos os DbContexts (aplicado por reflexão na base).
- `ITenantContext` resolvido do **JWT**; Workers iteram tenants explicitamente.
- `SaveChangesInterceptor` carimba `TenantId`; gravação cross-tenant **lança**.
- Modelo: **banco DEDICADO por tenant (database-per-tenant)** — isolamento físico exigido por
  auditorias de dinheiro público (TCE) e por editais. Um **DB central de plataforma** guarda o
  catálogo de tenants, as connection strings dedicadas e as licenças de módulo. Dentro do banco de
  cada tenant mantém-se **schema por módulo**. O `ITenantConnectionResolver` resolve a conexão do
  tenant corrente em runtime; o Global Query Filter por `TenantId` e os interceptors permanecem
  como **defesa em profundidade** (revisão sobre o modelo shared-DB inicial).

**Ativação modular:**
- Cada módulo expõe `IModule` (registro de DI, endpoints, DbContext).
- Tabela **`TenantModule`** define os módulos ativos por tenant.
- O `ApiHost` registra/roteia **apenas** os módulos licenciados; acesso a módulo inativo → **403/404 auditado**.

## Consequências

- ➕ Um único código-base serve N tenants; **billing por módulo**.
- ➕ Onboarding de prefeitura = configurar tenant + módulos licenciados.
- ➖ Risco de vazamento entre tenants — mitigado por filtros globais + **testes de isolamento obrigatórios**.
- ➖ Filtro global nunca pode ser desabilitado "para facilitar"; consultas administrativas usam mecanismo dedicado e auditado.
