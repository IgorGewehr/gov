# ADR-0015 — Despacho do Outbox isolado por mensagem (escopo de DI por mensagem) + reuso na assinatura A1

- **Status:** Aceito (fecha o achado H5 exposto pela guarda do `ScopeDbContextHolder`)
- **Data:** 2026-06-22
- **Código:** `BuildingBlocks.Infrastructure/Outbox/{IOutboxMessageDispatcher,OutboxPublisher}.cs`,
  `ApiHost/Outbox/{ScopedOutboxMessageDispatcher,OutboxBackgroundService}.cs`,
  `BuildingBlocks.Application/Assinatura/IAssinaturaEmEscopoDedicado.cs`,
  `ApiHost/Assinatura/AssinaturaEmEscopoDedicado.cs`

## Contexto

O `ScopeDbContextHolder` (ADR-0010) mantém **um único `ModuleDbContext` por escopo de DI** e a
guarda **H5** (`ScopeDbContextHolder.Definir`) **lança** quando um segundo contexto **divergente**
é resolvido no mesmo escopo — em vez de aplicar *last-writer-wins* e descartar gravações em
silêncio. A drenagem do Outbox (ADR-0011) republica cada mensagem aos seus handlers via MediatR.
Como **uma única mensagem pode ter handlers de módulos distintos** (ex.: o domain event de Finanças
e o integration event consumido por Administração na mesma drenagem), o `OutboxPublisher`, ao
publicar **todas as mensagens do lote no mesmo escopo do contexto de leitura**, faria o MediatR
resolver **dois `ModuleDbContext` distintos** nesse escopo — **acionando a guarda H5** e abortando a
contabilização. O mesmo padrão patológico aparece fora do Outbox: um handler que já resolveu o
`ModuleDbContext` do seu módulo e precisa **assinar com o A1 do tenant** (módulo Cofre) resolveria
o `CofreDbContext` lado a lado — de novo dois contextos no mesmo escopo.

## Decisão

Isolar **cada mensagem** (e cada operação cross-módulo análoga) em seu **próprio escopo de DI**:

- **`IOutboxMessageDispatcher`** (porta) declara `DespacharAsync(evento, tenantId, ct)`; a
  implementação **`ScopedOutboxMessageDispatcher`** (no `ApiHost`, onde existe `IServiceScopeFactory`)
  abre um **escopo novo por mensagem** (`CreateAsyncScope`), **reaplica o tenant corrente**
  (`TenantOverride`) e só então publica via `IPublisher`. A **leitura do lote**, a marcação de
  processado/*dead-letter*, o `AttemptCount` e o `SaveChanges` permanecem **sempre** no contexto de
  leitura recebido; só o **despacho** é isolado.
- O `OutboxBackgroundService` já usa um **escopo dedicado por (tenant, módulo)** para a *leitura* do
  Outbox — então a leitura também nunca colide; o isolamento por mensagem cobre o **despacho**.
- O **mesmo padrão** é reusado na assinatura: **`IAssinaturaEmEscopoDedicado`** + 
  `AssinaturaEmEscopoDedicado` capturam o tenant, abrem escopo próprio (onde o único `ModuleDbContext`
  é o do Cofre) e assinam (CMS/XML). Reusável por AFD/AEJ (ponto), eSocial e remessas TCE.

## Alternativas consideradas

- **Relaxar a guarda H5 (voltar a *last-writer-wins*):** reintroduziria o descarte silencioso de
  gravações — exatamente o bug que o ADR-0010 corrige. Rejeitado.
- **Despachar o lote inteiro num único escopo:** é o estado que **causa** a colisão H5 quando a
  mensagem cruza módulos. Rejeitado.
- **Forçar todo handler de integration event a não tocar `DbContext`:** quebra casos legítimos
  (contabilização, confirmação de dotação reagindo a evento). Rejeitado.
- **Chamar o serviço de assinatura direto do handler de outro módulo:** resolve o `CofreDbContext`
  no escopo do chamador → H5. Rejeitado em favor da porta em escopo dedicado.

## Consequências

- ➕ Handlers de **módulos distintos** nunca compartilham escopo no despacho → a guarda H5 não
  dispara; a contabilização e demais reações ao evento confirmam.
- ➕ **Tenant preservado** no novo escopo (`TenantOverride`): Global Query Filters e isolamento
  multi-tenant continuam valendo na republicação assíncrona.
- ➕ **Padrão único e reusável** (Outbox e assinatura A1 compartilham a mesma forma), reduzindo a
  chance de recriar o anti-padrão em integrações futuras (eSocial, TCE).
- ➖ **Custo por mensagem:** abrir um escopo de DI por mensagem tem overhead; aceitável dado o
  volume de drenagem assíncrona e a criticidade da consistência.
- ➖ A porta vive no `ApiHost` (precisa de `IServiceScopeFactory`); `BuildingBlocks` só declara a
  interface — mantém o layering, mas exige que cada host componha a implementação.
- 🔗 Depende do ADR-0010 (guarda H5) e do ADR-0011 (Outbox); a separação leitura×despacho é o que
  torna o *at-least-once* idempotente seguro mesmo com handlers multi-módulo.
