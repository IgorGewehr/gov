# DESIGN — Hardening de Performance e Escala (Tensorroot.Gov)

> Design **pronto-para-implementar** do endurecimento de performance derivado de
> `auditoria.md` (gargalos G1–G5) e da auditoria de arquitetura (H2/H3/H4/M-3/M-4).
> **Princípio inviolável:** melhorar performance **sem** comprometer **correção**,
> **isolamento de tenant** (database-per-tenant + Global Query Filter) ou **auditoria**
> (trilha imutável). Toda chave de cache é **sempre prefixada por `tenantId`**.
> `[a confirmar]` = exige medição sob carga no SGBD de produção (SQL Server; DEV é SQLite).
> Data: 2026-06-22. Stack: .NET 8 / EF Core 8 / MediatR / Outbox.

Ordem por prioridade de implementação: **D2 → D1 → D3 → D5 → D4**
(D2 destrava o caminho quente e habilita o cache de escopo; D1 é a camada que D2/D3 reusam).

---

## D1 — Camada de cache da plataforma (licenças / escopo / roteiros / árvore de UO) com invalidação por evento

**Prioridade: ALTA** (resolve G2; sustenta D2 e D3).

### O que muda

Introduzir uma abstração de cache **única e tenant-aware** em `BuildingBlocks` —
`IPlataformaCache` sobre **`HybridCache`** (.NET 9 `Microsoft.Extensions.Caching.Hybrid`; em
.NET 8 usar `IMemoryCache` com a mesma interface, trocável depois sem mexer nos chamadores).
Decisão de backend `[a confirmar]`: começar **L1 `IMemoryCache` por instância**; só migrar para
L1+L2 (Redis via `HybridCache`) quando houver mais de uma instância de ApiHost — aí a
invalidação precisa ser **distribuída** (ver Risco).

- **Helper de chave obrigatório** que recusa montar chave sem `tenantId`:
  `CacheKeys.Licencas(tenantId)`, `CacheKeys.Escopo(tenantId, usuarioId)`,
  `CacheKeys.ArvoreUO(tenantId)`, `CacheKeys.Roteiro(tenantId, exercicio)`.
  Nenhuma chave global; toda entrada carrega o `tenantId` no prefixo.
- **Quatro consumidores:**
  1. **Licenças de módulo** — `TenantModuleProvider.EnabledModulesAsync` passa a ler do cache
     (`CacheKeys.Licencas(tenantId)`), invalidado em `TenantProvisioningService.DefinirModuloAsync`
     e `ProvisionarAsync` (`TenantProvisioningService.cs:60-82`). TTL de segurança 5 min.
  2. **Árvore de UO** — `CalculadoraPermissoesEfetivas` (`CalculadoraPermissoesEfetivas.cs:50-51`)
     deixa de reconstruir `ArvoreUnidades.Construir(todasUnidades)` por request; a árvore é
     cacheada por `tenantId`, invalidada na criação/movimentação/inativação de UO.
  3. **Escopo efetivo** — ver **D2** (chave por `(tenant, usuário)`; invalidação por revogação/
     rotação de papel — I10).
  4. **Roteiros contábeis (PCASP)** — `MotorContabil` cacheia o roteiro por
     `(tenantId, exercício)`, invalidado na alteração de roteiro do exercício.
- **Invalidação por evento de domínio**, não só TTL: cada mutação relevante chama
  `cache.Invalidar(CacheKeys.X(...))` **dentro da mesma transação** que persiste a mudança
  (ou imediatamente após o `SaveChanges`, antes de retornar). TTL curto é apenas rede de
  segurança contra invalidação perdida.

### Risco de correção / isolamento (e mitigação)

- **Vazamento entre tenants** (servir dado de A para B): mitigado pelo **helper que torna o
  `tenantId` obrigatório** na chave + teste de arquitetura/unidade que falha se alguma chave
  não contiver `tenantId`. O Global Query Filter permanece intacto — o cache guarda o
  **resultado já filtrado por tenant**, nunca uma query crua.
- **Stale após mudança de licença/UO/roteiro/papel** (autorizar a mais = falha de segurança):
  invalidação **síncrona no evento de escrita** fecha a janela; TTL curto a limita se a
  invalidação falhar. **Nunca** cachear negativa de licença por TTL longo.
- **Multi-instância** `[a confirmar]`: com 2+ ApiHosts, invalidar só o L1 local deixa as demais
  instâncias stale até o TTL. Mitigação: ou TTL curto aceito como janela máxima de
  inconsistência (documentar no runbook), ou backplane de invalidação (Redis pub/sub via
  `HybridCache` L2) — decidir por medição de nº de instâncias.
- **Auditoria:** cache é só de **leitura**; nenhuma escrita/trilha passa por cache. Sem impacto.

---

## D2 — Autorização async ponta-a-ponta + cache de `EscopoEfetivo` por `(tenant, usuário)`

**Prioridade: CRÍTICA** (resolve G1 — o pior gargalo, no caminho quente de toda leitura com escopo de UO).

### O que muda

**(a) Eliminar o `sync-over-async`.** Hoje `TenantUnidadeContext.Resolver()`
(`TenantUnidadeContext.cs:68-70`) bloqueia a thread com `.GetAwaiter().GetResult()` porque o
Global Query Filter lê propriedades **síncronas** (`FiltrarPorUnidade` / `UnidadesPermitidas`,
`ModuleDbContext.cs:51-54`). Solução: **pré-resolver o escopo de forma async ANTES de qualquer
query**, deixando o GQF apenas **ler um valor já materializado** (sem I/O no getter).

- Novo **middleware de pipeline** `EscopoUnidadeResolutionMiddleware` (ApiHost), posicionado
  **depois** da autenticação JWT e da resolução de `ITenantContext`, e **antes** dos endpoints
  de módulo. Para requisições com `UserId` no JWT, ele chama
  `await resolvedor.ResolverUnidadesLegiveisAsync(...)` (já async) e **popula** o
  `TenantUnidadeContext` (scoped) com o resultado.
- `TenantUnidadeContext` deixa de resolver preguiçosamente: ganha
  `Preencher(IReadOnlyCollection<Guid> unidades)` chamado pelo middleware; os getters viram
  leitura pura do campo (`_resolvido` já `true`). O caminho preguiçoso síncrono é **removido**.
- **Workers / Outbox / provisionamento** (sem sujeito): o middleware não roda; `_resolvido`
  permanece `false` ⇒ `FiltrarPorUnidade = false` — **comportamento idêntico ao atual**
  (deny-by-default não se aplica a jobs de sistema; ver `ModuleDbContext.cs:25-28`).

**(b) Cachear o `EscopoEfetivo`** por `CacheKeys.Escopo(tenantId, usuarioId)` (via D1). O
middleware consulta o cache antes de ir ao banco; só recalcula em miss. Resolve os
`2+ round-trips/request` (papéis + **todas** as UOs, `CalculadoraPermissoesEfetivas.cs:44-53`) e
a reconstrução `O(U)` da árvore por request.

### Risco de correção / isolamento (e mitigação)

- **Ordem de pipeline** (escopo lido antes de resolvido): se algum endpoint disparasse query
  antes do middleware, o GQF leria escopo vazio ⇒ **deny-by-default** (não vaza dado a mais —
  falha fechada, não aberta). Mitigação: registrar o middleware **antes** do roteamento de
  módulos; teste de integração que confirma escopo preenchido em toda rota autenticada.
- **Stale por revogação/rotação de papel (I10):** o cache de escopo **precisa** ser invalidado
  em qualquer mudança de `AtribuicaoDePapel`, `Papel.Permissoes` ou UO. Invalidar
  `CacheKeys.Escopo(tenantId, usuarioId)` no comando de revogação/atribuição; quando a mudança
  é na árvore/papel (afeta vários usuários), invalidar por prefixo `tenant` (limpar escopos do
  tenant) — mais barato e correto que rastrear usuários afetados. **TTL curto** como rede de
  segurança. Sem invalidação confiável, um papel revogado continuaria autorizando ⇒ tratar a
  invalidação como **requisito de correção**, não otimização.
- **Isolamento:** chave por `(tenant, usuário)`; um usuário só existe em um tenant (CNPJs
  distintos são tenants distintos — CLAUDE.md §4). Sem cruzamento possível.
- **Auditoria:** inalterada — autorização não escreve trilha; o command-side fino por
  permissão+UO (guards) permanece e não usa este cache de leitura.
- **`[a confirmar]`** ponto de saturação do thread-pool antes/depois (medir p99 sob carga
  concorrente com I/O de banco lento).

---

## D3 — Despacho paralelo do Outbox (concorrência limitada) + skip de tenants sem pendências

**Prioridade: ALTA** (resolve G3 — `O(N×M×RTT)` serial, risco de estourar o ciclo de 30 s).

### O que muda

Em `OutboxBackgroundService.DrenarTodosAsync` (`OutboxBackgroundService.cs:47-105`):

1. **Skip barato (EXISTS) antes de abrir contexto pesado.** Hoje todo tenant ativo paga
   `query de licenças + leitura de Outbox por módulo`, mesmo vazia. Acrescentar, por módulo, um
   **`AnyAsync`** de mensagem elegível (mesmo predicado de `OutboxPublisher`: `ProcessedOnUtc ==
   null && DeadLetteredOnUtc == null && (NextAttemptUtc == null || NextAttemptUtc <= agora)`) —
   já há índice de drenagem (`ModelBuilderExtensions.cs:88-89`) que torna o `EXISTS` barato. Só
   abre o caminho de despacho se houver pendência. Corta `N×M` para `N_com_pendência`.
2. **Paralelizar por tenant** com `Parallel.ForEachAsync` + `MaxDegreeOfParallelism` configurável
   (`IOptions`, default conservador `[a confirmar]`, ex. 8). Cada tenant continua resolvendo
   **seu próprio escopo de DI** (`scopeFactory.CreateAsyncScope()` + `TenantOverride`,
   `OutboxBackgroundService.cs:64-87`) — o isolamento já existente por escopo é preservado;
   apenas vários escopos rodam concorrentemente. **Dentro** de um tenant, manter serial por
   módulo (o `ScopeDbContextHolder` mantém um contexto por escopo — guarda H5).
3. **Cachear a descoberta de módulos licenciados** (via D1) — remove a query
   `EnabledModulesAsync` por tenant por ciclo (`OutboxBackgroundService.cs:67-69`).
4. **Destino estrutural:** broker (ADR-0011) substitui o polling; este design é a ponte até lá.

### Risco de correção / isolamento (e mitigação)

- **Cross-tenant em paralelo:** o risco seria um escopo "vazar" o `TenantOverride` para outro.
  Mitigação: cada iteração do `Parallel.ForEachAsync` cria **seu próprio `CreateAsyncScope()`**
  e define o `TenantOverride` **dentro** dele (nunca compartilhado) — exatamente o padrão atual,
  só que concorrente. Confirmar que `TenantOverride` é scoped (não singleton) — é resolvido por
  escopo no código atual.
- **Ordem de entrega:** o Outbox já não garante ordem global; o paralelismo é **entre tenants**
  (independentes por design — bancos dedicados). Dentro do tenant a ordem por `OccurredOnUtc`
  no `Take` (`OutboxPublisher`) é preservada (serial por módulo). Sem regressão de ordering.
- **Saturação de conexões:** paralelismo alto multiplica conexões simultâneas a bancos
  distintos ⇒ acoplar `MaxDegreeOfParallelism` ao orçamento de pool de D4. `[a confirmar]` o
  grau ótimo por medição (RTT do SQL Server de produção).
- **EXISTS pode perder corrida** (mensagem inserida logo após o check): aceitável — é drenada no
  **próximo ciclo**; o Outbox é eventualmente consistente por definição. Sem perda de mensagem.
- **Auditoria:** despacho não altera a trilha; mensagens falhas seguem o backoff/dead-letter
  existente (`OutboxPublisher` — H1 já resolvido). Sem impacto.

---

## D4 — Orçamento operacional do banco-por-tenant (migrations, pooling, runbook)

**Prioridade: MÉDIA** (resolve G4 — custo operacional do database-per-tenant).

### O que muda

1. **Orquestrador de migrations em lote, paralelo e idempotente.** Hoje o provisionamento migra
   serialmente por módulo (`TenantProvisioner.cs:43-51`) e não há rollout sobre a base já
   instalada. Extrair um `MigrationOrchestrator` que, dado um conjunto de tenants:
   - aplica migrations com **paralelismo limitado** (`MaxDegreeOfParallelism`, mesmo orçamento
     de pool que D3), **idempotente** (EF `MigrateAsync` é seguro re-rodar),
   - emite **métrica/log por tenant** (`TenantId`, módulo, duração, resultado) via Serilog/OTel,
   - **retoma após falha parcial** (registra tenants migrados; re-execução pula concluídos).
   - Migração de schema em produção: `N_tenants × M_módulos` execuções por release
     (`[a confirmar]` duração por medição) — paralelizar para caber na janela de deploy.
2. **Pooling consciente.** Definir `Min/Max Pool Size` por banco na connection string
   (`FinancasModule.cs:45` `UseSqlServer`), parametrizável por config. Orçar o **pico total de
   conexões** ≈ `Σ_tenants (pool por módulo ativo)` contra o teto do servidor SQL.
3. **Teto declarado de tenants/instância** + runbook: documentar o limite prático
   (`[a confirmar]` por carga) e o gatilho de **sharding horizontal** (nova instância de
   ApiHost / novo servidor de banco) acima dele. Runbook de provisionamento, rotação de conexão
   (já suportada por `RotacionarConexaoAsync` + invalidação K6) e failover.

### Risco de correção / isolamento (e mitigação)

- **Migration parcial deixa schema inconsistente:** mitigado por idempotência + retomada +
  log por tenant; nenhum tenant é considerado "pronto" até a migration concluir. Provisionamento
  só libera o tenant após sucesso.
- **Isolamento:** cada migration roda na conexão **dedicada** do tenant (escopo por módulo,
  `TenantProvisioner.cs:47`); paralelizar não cruza bancos. Sem risco de isolamento.
- **Pool exaustão sob paralelismo:** `Max Pool Size` + `MaxDegreeOfParallelism` coordenados
  evitam estouro; medir antes de elevar o grau.
- **Auditoria:** migrations não tocam a trilha de dados; runbook documenta janelas.

---

## D5 — Plan-cache: parâmetro estruturado (TVP/OPENJSON) ou teto para o filtro de UO

**Prioridade: MÉDIA** (resolve G5 — `IN(@p0..@pN)` de cardinalidade variável; impacto só em SQL Server).

### O que muda

O filtro de UO traduz `UnidadesPermitidas.Contains(e.UnidadeId)` em `IN(...)` cujo nº de
parâmetros varia com a cardinalidade do escopo (`ModelBuilderExtensions.cs:58-62`), gerando **um
plano por cardinalidade** no SQL Server. Opções (decidir por medição):

1. **`OPENJSON` / TVP**: passar a lista de UOs como **um único parâmetro estruturado** ⇒ **um
   plano** independente da cardinalidade. Em EF 8, `Contains` sobre coleção parametrizada já
   pode usar `OPENJSON` em SQL Server — **medir** se o tradutor atual já o faz para esta coleção
   antes de customizar. Se não, materializar o predicado de UO via consulta com parâmetro
   estruturado (sem alterar a **semântica** do filtro — correção/isolamento idênticos).
2. **Teto + paginação do conjunto de UOs**: se a cardinalidade for sempre pequena, impor um teto
   estabiliza o nº de planos. Menos geral que (1).

O `Expression.Constant(context)` (`ModelBuilderExtensions.cs:30`) é o mecanismo **correto** de
reavaliação por query (reflete o escopo atual a cada execução) — **não** alterar.

### Risco de correção / isolamento (e mitigação)

- **Semântica do filtro:** qualquer mudança deve produzir **exatamente** o mesmo conjunto
  filtrado (mesma união de UOs). Mitigação: testes de isolamento/UO existentes (CLAUDE.md §12)
  como gate; o filtro de UO **e** o de tenant permanecem combinados por `AND`
  (`ModelBuilderExtensions.cs:63`).
- **DEV vs PROD:** SQLite (DEV) não sofre o plan-cache bloat ⇒ **`[a confirmar]` por medição no
  SQL Server**; não otimizar prematuramente sem evidência. Manter o `IN` se a medição mostrar
  impacto desprezível.
- **Auditoria/isolamento:** inalterados — é otimização de **plano**, não de dados.

---

## Resumo de prioridades

| ID | Gargalo | Mudança principal | Prioridade | Medição? |
|----|---------|-------------------|------------|----------|
| **D2** | G1 | Async ponta-a-ponta (middleware) + cache de `EscopoEfetivo` | **CRÍTICA** | `[a confirmar]` saturação |
| **D1** | G2 | Camada de cache tenant-aware + invalidação por evento | **ALTA** | `[a confirmar]` L1 vs L1+L2 |
| **D3** | G3 | Outbox paralelo (limitado) + skip por EXISTS | **ALTA** | `[a confirmar]` grau ótimo |
| **D5** | G5 | TVP/OPENJSON ou teto no filtro de UO | **MÉDIA** | `[a confirmar]` no SQL Server |
| **D4** | G4 | Orquestração de migrations + pooling + runbook | **MÉDIA** | `[a confirmar]` duração/teto |

**Invariantes preservadas em todos os itens:** isolamento de tenant (chave de cache sempre com
`tenantId`; escopos de DI dedicados por tenant), Global Query Filter intacto, deny-by-default na
autorização (miss/escopo vazio falha **fechado**), trilha de auditoria imutável não atravessa
cache. Itens marcados `[a confirmar]` exigem medição sob carga no SGBD de produção antes da
calibração final.
