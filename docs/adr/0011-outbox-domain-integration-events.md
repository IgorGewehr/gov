# ADR-0011 — Outbox Pattern para integration events (domain events assíncronos → integration events)

- **Status:** Aceito
- **Data:** 2026-06-22
- **Código:** `BuildingBlocks.Infrastructure/Outbox/`, `ApiHost/Outbox/OutboxBackgroundService.cs`

## Contexto

O monolito modular (ADR-0001) comunica-se **dentro** do módulo por **Domain Events** in-process
(MediatR) e **entre** módulos **só** via `*.Contracts` (Integration Events + DTOs). Publicar um
integration event diretamente num broker dentro do command criaria o clássico **dual-write**: se
o `SaveChanges` confirma mas a publicação falha (ou vice-versa), estado e mensagens divergem —
inaceitável num sistema que move verba pública (ex.: lançamento contábil x MSC gerada).

## Decisão

Adotar o **Outbox Pattern** com escrita transacional:

- Quando um command muta o estado e precisa emitir um **integration event**, o evento é gravado
  numa **tabela Outbox no mesmo `DbContext`/transação** do estado (`ModuleIntegrationEventWriter`),
  na **mesma** unidade de trabalho do `ModuleUnitOfWork` (ADR-0010). Estado e intenção de
  publicar **commitam juntos ou não commitam**.
- Um **`OutboxBackgroundService`** (no `ApiHost`) faz *polling* da tabela e publica os eventos de
  forma **assíncrona e idempotente** aos consumidores (outros módulos / Workers).
- **Domain Events** permanecem **in-process** (MediatR), dentro do agregado/módulo; só o que cruza
  a fronteira de módulo vira **integration event** via Outbox.

## Alternativas consideradas

- **Publicar direto no broker dentro do command (dual-write):** sem garantia transacional —
  rejeitado por risco de divergência estado×mensagem.
- **Transação distribuída (2PC) entre banco e broker:** complexidade e acoplamento operacional
  altos; mal suportado com bancos por tenant (ADR-0005). Rejeitado.
- **CDC (change data capture) no log do banco:** evita a tabela Outbox, mas amarra a publicação ao
  fornecedor de banco e ao SQLite de DEV; menos portável. Rejeitado nesta fase.
- **Apenas domain events in-process para tudo:** não resolve consistência cross-módulo com
  consumidores assíncronos (ex.: Transparência consumindo `MSCGeradaIntegrationEvent`).

## Consequências

- ➕ **Consistência transacional** estado↔evento (sem dual-write); entrega *at-least-once* com
  consumidores **idempotentes** (CLAUDE.md §8).
- ➕ Desacopla produtor de consumidor; casa com isolamento de módulo (só `Contracts` cruzam) e com
  integrações governamentais resilientes (Polly/ACL).
- ➕ Habilita o acoplamento legítimo **inter-tenant** auditado (ex.: repasse de duodécimo) sem
  leitura cruzada de banco.
- ➖ **Latência** de propagação (polling) e necessidade de **idempotência** no consumidor —
  *exactly-once* não é garantido; o consumidor deduplica.
- ➖ Uma **tabela Outbox por contexto** + serviço de despacho a operar e monitorar (mensagens
  presas, *poison messages*). Com banco-por-tenant, o despacho precisa varrer os tenants.
- 🔗 Caminho de evolução: trocar polling por broker real (ex.: fila) sem mudar o contrato do
  produtor (continua gravando na Outbox).
