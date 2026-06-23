# Auditoria de Performance e Escala — Tensorroot.Gov

> Auditoria do **código** de `src/` (Platform/Tenancy, autorização, Outbox, GQF) focada nos
> gargalos REAIS de escala apontados pela auditoria de arquitetura (H2/H3/H4/M-3/M-4).
> Princípio: melhorar performance **sem** comprometer correção, isolamento de tenant ou auditoria.
> Cada achado traz **evidência (arquivo:linha)** + **impacto em escala** + **magnitude estimada**.
> `[a confirmar]` = depende de medição sob carga no SGBD de produção (DEV é SQLite).
> Data: 2026-06-22.

---

## Sumário executivo

Já estão **corrigidos** dois achados que a auditoria original listava como CRÍTICO/ALTO:
- **K6 (connection string sem invalidação)** → `TenantConnectionCache` hoje tem **TTL de 5 min +
  `Invalidar(tenantId)`** acionado por `RotacionarConexaoAsync`
  (`TenantConnectionResolver.cs:15-62`, `TenantProvisioningService.cs:85-99`). **Não é mais gargalo.**
- **H1 (Outbox poison eterna)** → `OutboxMessage` tem `AttemptCount`/`NextAttemptUtc`/dead-letter
  com backoff exponencial; a seleção filtra `NextAttemptUtc <= agora` e há índice de drenagem
  (`OutboxPublisher.cs:49-118`, `ModelBuilderExtensions.cs:88-89`). **Robustez OK.**

Restam **cinco gargalos de escala** reais, em ordem de severidade:

| # | Gargalo | Evidência | Severidade |
|---|---|---|---|
| **G1** | Autorização por requisição via banco + `sync-over-async`; reconstrução da árvore de UO por request; **sem cache cross-request** | `TenantUnidadeContext.cs:68-70`, `CalculadoraPermissoesEfetivas.cs:50-51`, `IdentidadeRepositories.cs:134-138` | **ALTO** (H2) |
| **G2** | **Ausência total de cache** na plataforma (licenças de módulo, escopo efetivo, árvore de UO, roteiros lidos do banco dedicado a cada uso) | zero `IMemoryCache`/`HybridCache` em `src/`; `TenantModuleProvider.cs:33-38` | **ALTO** (H3) |
| **G3** | **Despacho serial do Outbox** — `O(N_tenants × M_módulos × RTT)`, pode estourar o intervalo de 30 s | `OutboxBackgroundService.cs:60-104` | **ALTO** (H4) |
| **G4** | Custo operacional do **banco-por-tenant**: fan-out de migrations serial, pooling, teto de tenants/instância | `TenantProvisioner.cs:43-51`, `FinancasModule.cs:39-55` | **MÉDIO** (M-4) |
| **G5** | **Plan-cache pollution**: filtro de UO gera `IN(@p0..@pN)` de tamanho variável; injeta `Expression.Constant(context)` | `ModelBuilderExtensions.cs:30,58-66` | **MÉDIO** (M-3) |

---

## G1 — Autorização resolvida por requisição via banco + `sync-over-async` (H2)

### Evidência

`TenantUnidadeContext.Resolver()` (escopo de requisição) resolve o escopo de UO **bloqueando a
thread** sobre uma chamada async:

```
src/Modules/Identidade/.../Seguranca/TenantUnidadeContext.cs:68-70
    _unidades = resolvedor
        .ResolverUnidadesLegiveisAsync(usuarioGuid, clock.GetUtcNow(), CancellationToken.None)
        .GetAwaiter().GetResult();
```

Esse `.GetAwaiter().GetResult()` está no **caminho quente**: é disparado preguiçosamente pelo Global
Query Filter na **primeira leitura** de qualquer entidade `IMustHaveUnidade`
(`ModuleDbContext.cs:51-54` expõe `FiltrarPorUnidade`/`UnidadesPermitidas`, lidos pelo predicado em
`ModelBuilderExtensions.cs:55-62`). Praticamente toda query de negócio com escopo de UO passa por
aqui.

A resolução, por sua vez, **vai ao banco dedicado** e **reconstrói a árvore de UO inteira por
request**:

```
src/Modules/Identidade/.../Internal/CalculadoraPermissoesEfetivas.cs:44-53
    var dosPapeis        = await papeis.ObterPorIdsAsync(...);      // round-trip
    var todasUnidades    = await unidades.ListarAsync(...);        // round-trip: TODAS as UOs
    var arvore           = ArvoreUnidades.Construir(todasUnidades);// O(U) por request
    return EscopoEfetivo.Calcular(...);                            // expansão de subárvore
```

```
src/Modules/Identidade/.../Persistence/Repositories/IdentidadeRepositories.cs:134-138
    public async Task<IReadOnlyList<UnidadeOrganizacional>> ListarAsync(...)
        => await context.Unidades.OrderBy(u => u.Codigo).ToListAsync(...);  // varre a tabela de UO
```

O cache cross-request foi **explicitamente adiado** (comentário no próprio arquivo): *"ADIADO p/
M1.x: cache cross-request + invalidacao na revogacao (I10)"* (`TenantUnidadeContext.cs:19`). O
`TenantUnidadeContext` é `AddScoped` (`IdentidadeModule.cs:84`) — o cache vive **só dentro da
requisição**.

### Impacto em escala

1. **Thread-pool starvation:** sob carga, `sync-over-async` consome uma thread do pool por requisição
   enquanto espera o I/O do banco. Quando o pool satura, novas requisições enfileiram e a latência
   dispara de forma não-linear (efeito "joelho"). Em ambiente com banco-por-tenant (latência de rede
   por tenant distinta), a janela de bloqueio é maior.
2. **2+ round-trips por requisição** ao banco dedicado, só para autorizar: papéis + **todas as UOs**.
   Reconstruir a árvore (`O(U)` CPU + alocação de 2 dicionários + listas de filhos) a cada request
   é trabalho repetido para dado quase-estático (a árvore de UO de um ente muda raramente).

### Magnitude estimada

- Com `T` tenants ativos, `R` req/s por tenant e `U` UOs por ente: o sistema paga
  `~2·T·R` round-trips/s de autorização + `T·R` reconstruções de árvore `O(U)`/s — **puro overhead**,
  pois o resultado é idêntico entre requisições do mesmo usuário até uma mudança de papel.
- O `sync-over-async` é o multiplicador perigoso: a partir de algumas centenas de req/s concorrentes
  com I/O de banco lento, o tempo de fila no thread-pool pode **dominar** o tempo de resposta.
  `[a confirmar]` ponto exato de saturação por medição sob carga.

### Caminho (sem comprometer isolamento)

1. **Remover o `sync-over-async`:** tornar a resolução async ponta-a-ponta. O obstáculo é que o
   predicado do GQF lê propriedades **síncronas** (`FiltrarPorUnidade`/`UnidadesPermitidas`). Pré-
   resolver o `EscopoEfetivo` async num middleware/behavior **antes** de a query rodar (já há
   `UserId` no JWT) e popular o contexto de escopo; o GQF passa a ler valor já materializado.
2. **Cachear `EscopoEfetivo` por `(tenantId, usuarioId)`** em `IMemoryCache`/`HybridCache` com
   invalidação no evento de revogação/alteração de papel (I10) — mantém correção (deny-by-default
   continua; invalidação fecha a janela). Isola por tenant na chave.

---

## G2 — Ausência total de cache na plataforma (H3)

### Evidência

**Zero** `IMemoryCache`/`HybridCache`/`MemoryCache` em todo o `src/` (grep recursivo: nenhuma
ocorrência). Tudo que é quase-estático é relido do **banco dedicado** a cada uso:

- **Licenças de módulo** — lidas a cada drenagem de Outbox e a cada gating de request:
  ```
  src/Platform/.../Tenancy/TenantModuleProvider.cs:33-38
      => await context.TenantModules.Where(v => v.TenantId == tenantId && v.Ativo)
             .Select(v => v.ModuleName).ToListAsync(...);
  ```
  Consumido em loop no Outbox: `OutboxBackgroundService.cs:67-69` (uma query por tenant por ciclo).
- **Escopo efetivo / árvore de UO** — relidos por request (ver G1).
- **Roteiros contábeis (PCASP)** — `MotorContabil` resolve o roteiro do evento a cada
  contabilização (`src/Modules/Financas/.../Contabilidade/Motor/MotorContabil.cs`), dado que muda
  por exercício, não por lançamento.

Cada um desses é um round-trip a um **banco distinto** (database-per-tenant), não a uma tabela
compartilhada em cache de plano local.

### Impacto em escala

- O custo de leitura cresce **linearmente com o tráfego** para dados que mudam raramente. Em
  database-per-tenant, cada leitura é uma conexão/round-trip ao banco daquele tenant — sem
  amortização entre tenants.
- O loop do Outbox relê as licenças de **todos** os tenants a cada 30 s (`N` queries/ciclo) mesmo
  quando nada mudou (ver também G3).

### Magnitude estimada

- Licenças de módulo no Outbox: `N_tenants` queries a cada 30 s = `N/30` qps **constantes** de puro
  overhead, escalando com o número de tenants ativos independentemente de haver pendências.
- Roteiros/escopo/árvore: 1+ round-trip por operação de negócio relevante — proporcional ao
  throughput, eliminável por cache com hit-rate alto (dados quase-imutáveis).

### Caminho

Camada de cache (memória + invalidação **por evento de domínio**, não só TTL):
- **Licenças de módulo:** cache por `tenantId`, invalidado em `DefinirModuloAsync`
  (`TenantProvisioningService.cs:60-82`).
- **Escopo efetivo / árvore de UO:** ver G1 (invalidação em mudança de papel/UO).
- **Roteiros contábeis:** cache por `(tenantId, exercício)`, invalidado na alteração de roteiro.
Manter chave **sempre prefixada por `tenantId`** para não vazar entre tenants. TTL curto como rede
de segurança além da invalidação por evento.

---

## G3 — Despacho serial do Outbox (H4)

### Evidência

`DrenarTodosAsync` percorre **serialmente** todos os tenants, e dentro de cada tenant, serialmente
todos os módulos licenciados:

```
src/ApiHost/Outbox/OutboxBackgroundService.cs:60-104
    foreach (var tenantId in tenants)                 // serial por tenant
    {
        ... EnabledModulesAsync(tenantId) ...          // 1 query/tenant/ciclo (ver G2)
        foreach (var nomeModulo in nomesModulos)       // serial por módulo
        {
            await module.DrenarOutboxAsync(...);       // abre/migra contexto + lê Outbox + despacha
        }
    }
```

Cada `DrenarOutboxAsync` abre um escopo, resolve o `ModuleDbContext` daquele banco dedicado e lê o
Outbox (`FinancasModule.cs:102-108` → `OutboxPublisher.PublicarPendentesAsync`, lote 100). **Não há
skip de tenants sem pendências** — todo tenant ativo paga ao menos a query de licenças + uma leitura
de Outbox por módulo licenciado, mesmo vazia. O intervalo é fixo em **30 s**
(`OutboxBackgroundService.cs:21`).

### Impacto em escala

- O ciclo custa `~O(N_tenants × M_módulos_licenciados × RTT)`. Com banco-por-tenant, o `RTT` é a
  latência de abrir contexto + round-trip ao banco daquele tenant. Quando
  `N × M × RTT > 30 s`, ciclos começam a **se atrasar e empilhar**, aumentando a **latência de
  propagação** de `MSCGeradaIntegrationEvent` → atraso na prestação de contas (Transparência).
- Mesmo sem nenhuma mensagem pendente, o sistema paga `N` queries de licença + `N×M` leituras de
  Outbox por ciclo (trabalho desperdiçado, agrava G2).

### Magnitude estimada

- Exemplo conservador: `RTT ≈ 20 ms` (licença + leitura de Outbox por módulo). Com `M ≈ 6` módulos:
  `~120 ms/tenant`. Em **250 tenants** → **~30 s/ciclo**, no limite do intervalo, **sem fazer
  trabalho útil**. Acima disso, o despacho atrasa monotonicamente. `[a confirmar]` ponto de
  saturação real depende do `RTT` do SGBD de produção e do `M` médio.

### Caminho

1. **Skip barato de tenants sem pendências:** antes de abrir contexto por módulo, um `EXISTS`
   (mensagem elegível) ou um flag/coluna "tem-pendência" no catálogo — corta o `N×M` para
   `N_com_pendência`.
2. **Paralelizar** com `Parallel.ForEachAsync` + concorrência limitada (`MaxDegreeOfParallelism`) por
   tenant e/ou por módulo — respeitando o isolamento de escopo já existente (cada módulo já roda em
   escopo próprio, `OutboxBackgroundService.cs:85`).
3. **Cachear a descoberta de módulos licenciados** (G2) para remover a query por tenant por ciclo.
4. Caminho final estrutural: **broker** (ADR-0011) substitui o polling.

---

## G4 — Custo operacional do banco-por-tenant (M-4)

### Evidência

Provisionamento migra **serialmente** o banco dedicado de cada módulo licenciado:

```
src/ApiHost/Provisioning/TenantProvisioner.cs:43-51
    foreach (var module in modules.Where(m => modulos.Contains(m.Name, ...)))
    {
        await using var escopoModulo = scopeFactory.CreateAsyncScope();
        await module.MigrarBancoAsync(conexao, provider, tenantId, ...);  // serial por módulo
    }
```

Cada módulo abre seu **próprio** `DbContext`/conexão contra o banco do tenant
(`FinancasModule.cs:39-55`, provider por config). Não há orquestração em lote, observabilidade por
tenant, nem reconhecimento de teto de tenants/instância no código.

### Impacto em escala

- **Fan-out de migrations:** uma alteração de schema precisa rodar em `N_tenants × M_módulos`
  bancos. Numa janela de deploy, isso é serial por tenant (no provisionamento) e precisa de
  orquestração própria para o rollout de migrations sobre a base instalada.
- **Pooling:** cada banco dedicado mantém seu próprio pool de conexões; o número total de conexões
  cresce com `N_tenants`, podendo esbarrar no teto do servidor de banco/instância da aplicação.
- **Teto prático de tenants/instância:** não modelado — risco de descobrir o limite em produção.

### Magnitude estimada

- Migration: `N_tenants × M_módulos` execuções por mudança de schema; com 250 tenants e 6 módulos =
  **~1.500 migrações** por release — exige paralelismo controlado + observabilidade por tenant para
  caber na janela. `[a confirmar]` duração por medição.
- Conexões: pico ≈ `Σ_tenants (pool_min por módulo ativo)`; orçar `Max Pool Size` e um **teto de
  tenants por instância** explícito.

### Caminho

- Orquestrador de migrations em **lote, paralelo e idempotente**, com métrica/log por tenant e
  retomada após falha parcial.
- Pooling consciente (`Min/Max Pool Size` por banco), runbook de provisionamento/rotação, e um
  **teto declarado de tenants/instância** com sharding horizontal acima dele.

---

## G5 — Plan-cache pollution pelo `IN(@p0..@pN)` variável (M-3)

### Evidência

O filtro de UO traduz `UnidadesPermitidas.Contains(e.UnidadeId)` para um `IN(...)` cujo número de
parâmetros **varia com a cardinalidade** do escopo do usuário:

```
src/BuildingBlocks/.../ModelBuilderExtensions.cs:58-62
    var permitidas = Expression.Property(contextoConstante, nameof(ModuleDbContext.UnidadesPermitidas));
    var contains   = Expression.Call(ContainsMethod, permitidas, unidadeProperty);  // -> IN(@p0..@pN)
```

Além disso, o filtro injeta `Expression.Constant(context)` no modelo:

```
src/BuildingBlocks/.../ModelBuilderExtensions.cs:30
    var contextoConstante = Expression.Constant(context);
```

### Impacto em escala

- **Plan-cache:** cada cardinalidade distinta de `UnidadesPermitidas` gera uma query SQL textual
  diferente (`IN` com `N` parâmetros) → **um plano por cardinalidade**. Em entes grandes (muitas UOs
  por usuário) isso polui o plan cache do SGBD e aumenta compilações de plano. O `Contains` sobre
  coleção parametrizada também pode disparar comportamento de cardinalidade dinâmica no EF 8.
- `Expression.Constant(context)`: o filtro depende de propriedades reavaliadas do contexto; é o
  mecanismo correto para reavaliação por query, mas reforça que o predicado não é uma constante
  estável (cada execução reflete o escopo atual).

### Magnitude estimada

- Impacto **depende do SGBD de produção** (DEV é SQLite, que não sofre o mesmo plan-cache que o SQL
  Server). Em SQL Server, `IN` de tamanho variável é causa conhecida de plan-cache bloat. Magnitude
  proporcional à variedade de cardinalidades de escopo entre usuários. `[a confirmar]` por medição.

### Caminho

- Em produção (SQL Server): usar **TVP** ou **`OPENJSON`** para passar a lista de UOs como um único
  parâmetro estruturado (1 plano, independente da cardinalidade), **ou** impor teto + paginação do
  conjunto. Medir sob carga antes de decidir; não alterar a semântica do filtro (correção/isolamento
  intactos).

---

## Os 3 MAIORES gargalos + caminho

1. **G1 — Autorização `sync-over-async` + reconstrução de árvore de UO por request, sem cache.**
   É o pior porque está no **caminho quente de toda leitura com escopo de UO** e o `sync-over-async`
   (`TenantUnidadeContext.cs:68-70`) ameaça **thread-pool starvation** — degradação não-linear sob
   carga. **Caminho:** tornar a resolução async ponta-a-ponta (pré-resolver em middleware antes do
   GQF ler valores síncronos) + cachear `EscopoEfetivo` por `(tenant, usuário)` com invalidação na
   revogação de papel (I10). Correção e deny-by-default preservados.

2. **G3 — Despacho serial do Outbox `O(N×M×RTT)`.**
   Escala mal com `N_tenants` e atrasa a **propagação da MSC** (latência de prestação), com risco de
   estourar o intervalo de 30 s. **Caminho:** skip de tenants sem pendências (`EXISTS`) +
   `Parallel.ForEachAsync` com concorrência limitada + cache da descoberta de módulos; broker como
   destino final (ADR-0011).

3. **G2 — Ausência total de cache na plataforma.**
   Cada licença/escopo/roteiro/árvore é um round-trip a um banco **dedicado** a cada uso; amplifica
   G1 e G3. **Caminho:** camada de cache (memória + invalidação por evento de domínio, chave sempre
   prefixada por `tenantId`) para licenças de módulo, escopo efetivo, árvore de UO e roteiros
   contábeis.

> **Já resolvidos (não recontar como gargalo):** invalidação do cache de connection string (K6) e
> resiliência do Outbox com dead-letter/backoff (H1). **Médios para endurecimento:** custo
> operacional do banco-por-tenant (G4) e plan-cache do `IN` variável (G5), ambos `[a confirmar]` por
> medição no SGBD de produção.
