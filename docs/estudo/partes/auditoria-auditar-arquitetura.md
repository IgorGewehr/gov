# Auditoria crítica de arquitetura — Tensorroot.Gov

> Auditor cético. Achados ordenados por severidade, com evidência no código e mitigação.
> Marcações `[a confirmar]` onde a conclusão depende de medição em carga real (CLAUDE.md §16).
> Data: 2026-06-22.

## Sumário

Revisei os ADRs 0005/0007/0010/0011/0013 e o código de `BuildingBlocks.Infrastructure`,
`Platform/Tenancy`, o `OutboxBackgroundService` e a resolução de escopo de autorização.
A base é sólida e algumas decisões frequentemente criticadas (banco-por-tenant, Outbox) estão
bem fundamentadas — em especial **a contabilidade NÃO é eventualmente consistente**: o lançamento
PCASP é síncrono e transacional (domain event in-process na mesma UoW, ADR-0013), e o Outbox só
carrega integration events cross-módulo (MSC→Transparência). Esse é o desenho correto.

Os riscos reais não estão nas grandes decisões, mas na **operação em escala** (despacho de Outbox
sequencial), na **ausência total de cache** (resolução de autorização por requisição via banco,
sync-over-async), na **invalidação inexistente do cache de connection strings**, e em **lacunas de
robustez do Outbox** (sem contador de tentativas / poison messages eternas). O `ScopeDbContextHolder`
é uma fragilidade estrutural conhecida e documentada, mas mitigável.

---

## CRÍTICO

### C1 — Cache de connection strings sem invalidação (rotação de segredo não propaga)
**Onde:** `Platform/Tenancy/TenantConnectionResolver.cs` (`TenantConnectionCache`, singleton) +
`PlatformServiceCollectionExtensions.cs:37` (`AddSingleton<TenantConnectionCache>()`).

```csharp
return cache.Mapa.GetOrAdd(tenantId, id => { /* lê do PlatformDbContext uma única vez */ });
```

**Por que é risco:** `GetOrAdd` num `ConcurrentDictionary` singleton, **sem TTL e sem nenhum caminho
de invalidação** (busquei: a única escrita no mapa é o `GetOrAdd`; nada o limpa). Banco-por-tenant
(ADR-0005) co-loca segredos no Key Vault e prega rotação. Quando a connection string de um tenant
mudar (rotação de credencial, failover de réplica, migração de banco), **toda a instância continua
usando a conexão antiga até reinício do processo** — e instâncias diferentes ficam divergentes.
Num domínio que move verba pública, apontar para um banco obsoleto/derrubado é incidente sério.

**Mitigação:** trocar por `IMemoryCache`/`HybridCache` com TTL curto + sinal de invalidação
(ex.: incrementar uma versão no `PlatformDbContext` lida por cabeçalho, ou pub/sub na rotação).
No mínimo, expor `Invalidar(Guid tenantId)` chamado pelo fluxo de provisionamento/rotação.

### C2 — Despacho de Outbox totalmente sequencial: latência de propagação cresce com N tenants
**Onde:** `ApiHost/Outbox/OutboxBackgroundService.cs`.

Um único `PeriodicTimer` de 30s; dentro, `foreach (tenant)` → `foreach (módulo)`, **sem paralelismo**,
cada módulo num escopo de DI próprio, cada um abrindo conexão ao banco DEDICADO do tenant. Para cada
tenant ainda há **duas aberturas de escopo extra** só para listar módulos licenciados (linhas 64–74).

**Por que é risco:** o tempo de um ciclo é ~`O(N_tenants × M_módulos × RTT_banco)`. Com poucas
centenas de entes (perfil declarado no ADR-0005) e até 13 módulos cada, um ciclo serial pode estourar
o intervalo de 30s — a fila de Outbox de tenants no fim da lista é drenada cada vez mais tarde.
Como a **MSC e a remessa ao TCE dependem do `MSCGeradaIntegrationEvent`** propagado pelo Outbox
(ADR-0011/0013), isso vira **latência de prestação de contas** que degrada com o crescimento da base
— exatamente onde o custo do banco-por-tenant (fan-out, ADR-0005 §consequências) se materializa.
`[a confirmar]` o ponto de saturação exige medição com RTT real e nº de tenants alvo.

**Mitigação:** paralelizar por tenant com `Parallel.ForEachAsync` + grau de concorrência limitado;
pular tenants sem mensagens pendentes (consulta barata `EXISTS`/marcador) antes de abrir escopo por
módulo; e mover a descoberta de módulos licenciados para fora do laço/cacheá-la. Caminho final já
previsto no ADR-0011: trocar polling por broker.

---

## ALTO

### A1 — Resolução de escopo de UO por requisição via banco, com sync-over-async
**Onde:** `Modules/Identidade/.../Seguranca/TenantUnidadeContext.cs:68-70`.

```csharp
_unidades = resolvedor
    .ResolverUnidadesLegiveisAsync(usuarioGuid, clock.GetUtcNow(), CancellationToken.None)
    .GetAwaiter().GetResult();
```

**Por que é risco:** dois problemas somados. (1) **`.GetAwaiter().GetResult()`** bloqueia uma thread
do pool dentro do caminho quente de toda requisição que toque uma entidade `IMustHaveUnidade` — sob
carga, é receita clássica de thread-pool starvation/deadlock. (2) **Sem cache cross-request** — o
próprio `remarks` admite "ADIADO p/ M1.x: cache cross-request + invalidação". A resolução carrega
papéis + **árvore inteira de UOs do tenant** (`CalculadoraPermissoesEfetivas.ResolverEscopoAsync`:
`unidades.ListarAsync()` + `ArvoreUnidades.Construir`) **a cada requisição**. Em entes grandes
(muitas secretarias/UOs), reconstruir a árvore por request é caro.

**Mitigação:** tornar a resolução assíncrona de ponta a ponta (o `ITenantUnidadeContext` pode expor
um método async resolvido antes da consulta, não via property síncrona); cachear o `EscopoEfetivo`
por (tenant, usuário) com invalidação na revogação/atribuição de papel (invariante I10, já citada
como pendência).

### A2 — Ausência total de cache na plataforma
**Onde:** confirmado por varredura — **nenhuma** ocorrência de `IMemoryCache`, `IDistributedCache`,
`HybridCache` ou `AddMemoryCache` em todo `src/`.

**Por que é risco:** dados de altíssima cardinalidade de leitura e baixíssima volatilidade são lidos
do banco a cada uso: módulos licenciados (`TenantModuleProvider.EnabledModulesAsync`, lido no gating
de runtime e no laço do Outbox), connection string (parcialmente cacheada, ver C1), escopo de
autorização (A1), roteiros contábeis (`EventoContabil` vigente, ADR-0013) e árvore de UOs. Sem
cache, cada um vira roundtrip — e com banco-por-tenant cada roundtrip é a um banco distinto.
`[a confirmar]` o impacto exato por endpoint exige profiling, mas a **lacuna estrutural** é certa.

**Mitigação:** introduzir camada de cache (memória + invalidação por evento) para licenças de módulo,
escopo efetivo e roteiros contábeis. Tratar como item de M9 (robustez/QA) no roadmap.

### A3 — Outbox sem contador de tentativas: poison messages reprocessam para sempre
**Onde:** `SharedKernel/OutboxMessage.cs` (campos: `ProcessedOnUtc`, `Error` — **sem** `AttemptCount`/
`NextAttemptUtc`/dead-letter) + `OutboxPublisher.PublicarPendentesAsync` (grava `Error` e segue).

**Por que é risco:** uma mensagem que falha permanentemente (tipo não resolúvel — `Type.GetType`
retorna null após renomear/remover um evento; JSON incompatível; consumidor com bug) **fica `Pending`
e é reselecionada a cada ciclo de 30s, indefinidamente**. Como `OrderBy(OccurredOnUtc).Take(lote=100)`,
uma poison message **antiga** pode ocupar a cabeça do lote e **bloquear/atrasar** as mensagens válidas
seguintes do mesmo módulo. Não há dead-letter nem alarme. Entrega é at-least-once (correto), mas sem
teto de tentativas vira loop quente silencioso.

**Mitigação:** adicionar `AttemptCount` + `NextAttemptUtc` (backoff exponencial) e mover para
dead-letter após N tentativas, com métrica/alerta. Tornar o `OrderBy` resiliente a cabeça envenenada
(filtrar por `NextAttemptUtc <= now`).

### A4 — `ScopeDbContextHolder`: estado scoped implícito e frágil em multi-módulo no mesmo escopo
**Onde:** `BuildingBlocks.Infrastructure/{ScopeDbContextHolder,ModuleUnitOfWork,ModuleDbContext}.cs`;
ADR-0010 documenta o trade-off honestamente.

**Por que é risco:** o holder guarda **um** `Atual`, populado pelo ctor do `ModuleDbContext`
(`holder?.Definir(this)`). Dois pontos: (1) **last-writer-wins** — se dois `ModuleDbContext` forem
construídos no mesmo escopo, o `ModuleUnitOfWork.SaveChangesAsync` confirma só o **último**, e as
mutações do primeiro são **descartadas silenciosamente** (é exatamente o bug que o ADR-0010 corrige
para o caso comum, mas o caso patológico permanece — o próprio ADR o admite como "limite"). (2)
depende de **toda** subclasse de `ModuleDbContext` herdar e não sobrescrever o registro no holder;
um contexto futuro que esqueça reintroduz a ambiguidade. É invariante de runtime sem rede de proteção
estática. O comentário no `OutboxBackgroundService` (linhas 78–82) mostra que a equipe já contorna
isso manualmente (um escopo por módulo) — sinal de fragilidade real, não teórica.

**Mitigação:** o `ModuleUnitOfWork` deveria **falhar alto** se mais de um contexto se registrar no
escopo (detectar segundo `Definir` com instância diferente e lançar), em vez de silenciosamente
escolher um; e cobrir com teste de arquitetura/integração que force o cenário multi-contexto.

---

## MÉDIO

### M1 — Filtro de UO gera `IN(...)` de tamanho variável → poluição de plan cache
**Onde:** `ModelBuilderExtensions.ApplyTenantAndUnidadeQueryFilters` (usa
`Enumerable.Contains(UnidadesPermitidas, e.UnidadeId)` → traduz para `WHERE UnidadeId IN (@p0..@pN)`).

**Por que é risco:** o nº de parâmetros varia com a quantidade de UOs do sujeito, gerando **uma query
plan distinta por cardinalidade** no SGBD — pressão sobre o plan cache em entes grandes. Além disso o
filtro injeta `Expression.Constant(context)` no modelo: funciona no EF8 (reavalia a property), mas
acopla o predicado à instância e dificulta paginação eficiente. `[a confirmar]` o impacto depende do
SGBD de produção (não definido no código; DEV é SQLite).

**Mitigação:** em PROD considerar TVP/`OPENJSON` para a lista de UOs, ou restringir o filtro a um
teto e paginar; medir plan cache sob carga real.

### M2 — Custo operacional do banco-por-tenant (decisão correta, custo subestimado no código)
**Onde:** ADR-0005 (decisão) + `TenantProvisioningService` / `SchemaProvisioner` (automação parcial).

**Por que é risco:** a decisão de isolamento físico é **bem justificada** (compliance TCE/LGPD,
blast radius contido) e eu a endosso para o perfil. Mas o custo operacional — migrations por banco,
monitoração por banco, fan-out de qualquer operação administrativa (visível em C2 e A2) — é real e
**aparece como gargalo prático** em vários pontos do código já hoje. Não é um defeito; é um custo a
orçar explicitamente (pool de conexões por tenant, limites do provedor de banco, janela de migration).

**Mitigação:** orquestração de migrations em lote com observabilidade por tenant; pooling consciente
do nº de bancos; runbook de provisionamento/rotação. Reconhecer o teto prático de tenants por
instância.

### M3 — Complexidade ABAC/UO vs. benefício
**Onde:** ADR-0007; `EscopoEfetivo`, `AutorizacaoDeConcessao`, filtro de UO, invariantes I1–I10.

**Por que é risco:** a dimensão extra (tenant × permissão × UO × sensibilidade) é **justificada** pela
exigência do TCE (autorização e prestação de contas na mesma dimensão organizacional UO↔UG) e pela SoD.
O risco não é a decisão, é a **superfície de teste e o custo de runtime** (ver A1): cada dimensão a mais
multiplica casos a provar e a resolver por requisição. Modelar ABAC no domínio (em vez de OPA externo)
foi a escolha certa para provar I4/SoD, mas concentra a complexidade no caminho quente.

**Mitigação:** garantir os testes dos invariantes I1–I10 como gate de CI (ADR-0007 já prevê) e resolver
A1/A2 para que a riqueza do modelo não vire custo por requisição.

---

## O que está BEM (achados negativos — risco menor do que a premissa do briefing sugeria)

- **Contabilidade NÃO é eventualmente consistente.** O briefing levanta "Outbox assíncrono p/
  contabilidade — risco?". O código e o ADR-0013 mostram o oposto: lançamento PCASP é **síncrono,
  na mesma UoW do fato** (domain event in-process); Σdébitos=Σcréditos é invariante de domínio. O
  Outbox só transporta **integration events cross-módulo** (MSC→Transparência), onde consistência
  eventual + consumidor idempotente é aceitável. Decisão correta.
- **Outbox transacional sem dual-write** (`ConvertDomainEventsToOutboxInterceptor` materializa as
  mensagens na mesma transação do estado) — desenho de livro-texto, correto.
- **Defesa em profundidade de tenant** mantida mesmo com banco dedicado: Global Query Filter +
  `TenantSaveChangesInterceptor` que **lança** em gravação cross-tenant. Correto.

---

## Prioridade de correção

1. **C1** (invalidação de connection string) e **A3** (poison messages) — robustez/correção, baixo esforço.
2. **A1 + A2** (cache + remover sync-over-async no caminho de autorização) — performance estrutural.
3. **C2** (paralelizar despacho do Outbox) — escala da prestação de contas.
4. **A4** (holder falhar-alto) e **M1** (plan cache) — endurecimento.
