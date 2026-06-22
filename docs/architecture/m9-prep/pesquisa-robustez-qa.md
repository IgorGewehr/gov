# M9 — Pesquisa: Robustez e QA (engenharia production-grade)

> Preparatório (m9-prep). Eixo **engenharia** do M9 (menos normativo): endurecer o que já
> existe na base — Outbox com dead-letter/poison, AuditSaveChangesInterceptor, OpenTelemetry
> (traces+metrics já wired em `src/ApiHost/Program.cs`, exportador OTLP/Azure Monitor diferido
> para FASE 5). Foco: tornar resiliência, imutabilidade de auditoria, observabilidade
> multi-tenant, testes E2E e acessibilidade WCAG 2.1 AA / eMAG **production-grade**.
>
> **Regra CLAUDE.md §16:** afirmação técnica de fonte externa leva FONTE/URL; o que ainda
> não foi confirmado em fonte vai marcado **[a confirmar]**. Distinção importante: padrões de
> engenharia (Outbox/OTel/testes) são consolidados na indústria; **acessibilidade e protocolo
> têm base legal brasileira** e exigem fonte oficial (gov.br/planalto).
>
> Legenda de confiança: **ALTA** = padrão consolidado em múltiplas fontes / base legal direta;
> **MÉDIA** = padrão correto, detalhe de implementação a validar contra nosso código;
> **BAIXA** = depende de decisão de arquitetura/infra ainda não tomada.

---

## 0. Mapa do que já existe (ponto de partida — não reinventar)

Detectado no repo (`.NET 8`, `global.json` SDK 8.0.400):

- **Outbox**: `src/BuildingBlocks/.../Infrastructure/Outbox/` — `ConvertDomainEventsToOutboxInterceptor`,
  `OutboxPublisher`, `CurrentScopeOutboxMessageDispatcher`, `ModuleIntegrationEventWriter`.
  Já há cobertura de poison/dead-letter: `tests/.../Financas.Tests/OutboxPoisonTests.cs`,
  `OutboxDispatchIsolationTests.cs`. **→ M9 endurece, não cria do zero.**
- **Auditoria**: `src/BuildingBlocks/.../Infrastructure/Auditing/AuditTrail.cs` +
  `AuditSaveChangesInterceptor.cs`. **→ falta camada WORM/imutabilidade verificável.**
- **OpenTelemetry**: `src/ApiHost/Program.cs` — `AddOpenTelemetry()` com `AddAspNetCoreInstrumentation`
  + `AddHttpClientInstrumentation` (tracing e metrics). **Resource só tem `AddService(...)`;
  falta `tenant.id` correlacionado e exportador OTLP.**
- **Testes**: ~10 projetos `*.Tests` + `ArchitectureTests` (inclui `ModuleUnitOfWorkIsolationTests`).
  **→ falta camada E2E/integração com Testcontainers e Playwright.**
- **Web**: `src/Web/package.json` (SPA) — alvo de acessibilidade WCAG/eMAG.

---

## 1. Outbox resiliente — endurecimento production-grade. CONFIANÇA: ALTA

Já temos Outbox + dead-letter. As lacunas a fechar no M9, com base nas fontes:

1. **Garantia é at-least-once, nunca exactly-once.** O par correto é **Outbox (produtor) +
   Inbox (consumidor idempotente)**. Para nós, "Inbox" = tabela de `event_id` já processados
   por handler/tenant, checada antes de aplicar efeito. Sem Inbox, qualquer redelivery duplica
   lançamento contábil/integração. [event-driven.io; npiontko.pro]
2. **Concorrência de relay sem lock contention** — claim de linhas com
   `FOR UPDATE SKIP LOCKED` (Postgres) ou hints `ROWLOCK, UPDLOCK, READPAST` (SQL Server),
   para múltiplas instâncias do dispatcher não pegarem a mesma linha. **[a confirmar]** qual o
   provider real do nosso `ModuleDbContext`. [npiontko.pro]
3. **Relay separado do request path** — acima de ~5 réplicas, co-hospedar dispatcher na API
   gera pressão de lock no banco; rodar como BackgroundService/worker dedicado. [npiontko.pro]
4. **DLQ só para erro permanente** (schema inválido, regra de negócio definitiva); transitório
   (broker/rede) vai para **retry com backoff exponencial**. Nosso poison-handling já separa —
   validar contadores de tentativa e janela. [dev.to/sagarmaheshwary; medium/melistogan]
5. **Ordenação** — para eventos que exigem ordem (ex.: empenho→liquidação→pagamento) usar
   version/sequence no payload + partição por agregado; não confiar em ordem de polling. [npiontko.pro]
6. **Crescimento silencioso** — limpar outbox de despachados imediatamente; reter inbox só a
   janela de retry (horas/dias). Tabelas de outbox "crescem em background" e degradam. [npiontko.pro]
7. **Observabilidade obrigatória da fila** (amarra com §3): idade média do evento não-enviado,
   taxa entrada vs. saída, erros do relay, profundidade da DLQ. "Nova fila ⇒ novo monitoramento." [npiontko.pro]

**Para o M9:** (a) adicionar tabela/serviço **Inbox idempotente** por handler+tenant;
(b) métricas de outbox/DLQ no OTel; (c) confirmar estratégia de lock conforme o provider real.

---

## 2. WORM / imutabilidade de auditoria — tamper-evident. CONFIANÇA: ALTA

Temos `AuditTrail` + interceptor, mas gravar audit numa tabela mutável **não é** prova de
não-adulteração. Padrão production-grade (relevante para ERP público, onde a trilha pode ser
contestada em TCE/MP):

1. **Append-only + hash chaining** — cada entrada carrega o hash da anterior
   (`hash_n = H(payload_n || hash_{n-1})`); alteração retroativa quebra a cadeia e é detectável.
   Usar **HMAC** (chave no Key Vault — já temos KV do M2/A1) para impedir recomputação da cadeia
   por quem só tem acesso de leitura ao banco. [tracehold.ai; emergentmind; mattermost]
2. **Não-repúdio** — encadeamento criptográfico atende requisitos do tipo PCI-DSS 10.3.3
   (logs em mídia WORM/servidor central) e dá **prova externamente verificável** de
   não-alteração pós-arquivamento. [hubifi; luthor.ai]
3. **WORM físico no destino frio** — arquivamento imutável via **Azure Immutable Blob Storage
   (time-based retention)** — coerente com a stack Azure já adotada; nem o root pode apagar
   antes do prazo. (Equivalente AWS: S3 Object Lock em Compliance mode.) [devsecopsschool; emergentmind]
4. **Separação de papéis** — quem escreve a auditoria não pode ter permissão de UPDATE/DELETE
   nela; idealmente schema/conexão separada com grant só de INSERT. [mattermost — "compliance by design"]
5. **Merkle tree / âncora periódica** para verificação eficiente de grandes volumes e checkpoint
   assinado (selo periódico do topo da cadeia). [emergentmind]

**Para o M9:** evoluir `AuditTrail` para entrada **append-only com hash-chain HMAC**
(chave no KV existente), grant de INSERT-only no banco do tenant, e job de arquivamento para
Immutable Blob com retenção. **Amarra com CONARQ/protocolo** (eixo normativo do M9): a trilha
imutável é o que dá fé à preservação digital. **[a confirmar]** prazo de retenção legal por
tipo de documento (tabela de temporalidade) — obter da pesquisa CONARQ/protocolo.

---

## 3. OpenTelemetry multi-tenant (traces/metrics com TenantId). CONFIANÇA: ALTA

Base já tem OTel wired; falta o eixo **multi-tenant** e o exportador:

1. **`tenant.id` em todos os 3 sinais** — Baggage **não** vira atributo automaticamente; é
   preciso ler do Baggage e **copiar explicitamente** para span attributes / métrica / log.
   Implementar um **SpanProcessor / Enricher** que injeta `tenant.id` (e `tenant.tier` se houver)
   a partir do `ITenantConnectionResolver`/contexto já existente. [opentelemetry.io/baggage; oneuptime multi-tenancy]
2. **Propagação cross-service** — setar Baggage **cedo, no entrypoint** (middleware), via header
   **W3C Baggage**; downstream lê e re-tagueia. Validar/sanitizar Baggage de entrada (não confiar
   em header externo para `tenant.id` — derivar do JWT/tenant resolver). [oneuptime baggage; opentelemetry.io]
3. **Cardinalidade** — `tenant.id` como **tag de métrica** explode séries temporais. Mitigar com:
   tag de tenant **só em traces/logs** (alta dimensionalidade aceitável) e em métricas usar
   `tenant.tier` ou agregação; ou aplicar **rate-limit/sampling por tenant** (evitar "noisy
   neighbor"). [oneuptime cardinality; opentelemetry.io]
4. **Amostragem por tier/tenant** e rate-limit em múltiplos níveis (SDK, Collector, backend). [oneuptime]
5. **Exportador** — completar `Program.cs` com OTLP → **Azure Monitor** (já marcado FASE 5 no
   código), garantindo versão sem NU1902 (nota já no comentário do código). **[a confirmar]**
   backend final (Azure Monitor vs. Collector self-hosted).
6. **PII / LGPD** — não colocar dado pessoal de cidadão em atributos de span/log; `tenant.id`
   é id de órgão, não de pessoa — ok. **[a confirmar]** política de masking para campos sensíveis
   em payloads logados (amarra LGPD do projeto).

**Para o M9:** Enricher de `tenant.id` nos 3 sinais + middleware de Baggage no entrypoint +
métricas de Outbox/DLQ (§1.7) + decisão de cardinalidade (tenant em trace, tier em métrica).

---

## 4. Testes E2E / integração. CONFIANÇA: ALTA

Temos unit + ArchitectureTests; falta integração realista e E2E de UI:

1. **`WebApplicationFactory<T>`** (`Microsoft.AspNetCore.Mvc.Testing`) sobe o app in-memory com
   pipeline/DI/rotas reais — base dos testes de integração de API. [learn.microsoft.com; antondevtips]
2. **Testcontainers** para dependências reais (Postgres/Redis) — **nunca hardcodar connection
   string** (portas dinâmicas): injetar via `ConfigureWebHost`/`UseSetting`; ciclo de vida com
   `IAsyncLifetime`. Dá isolamento real e estado limpo por suíte. [milanjovanovic.tech; testcontainers.com]
3. **Playwright for .NET** para E2E de SPA (Chromium/Firefox/WebKit) — cobre o `src/Web`. [dotnet.testcontainers; bool.dev]
4. **Isolar serviços externos** (PNCP, SICONFI, eSocial) com **WireMock** — não bater em
   endpoint gov real em CI. [oneuptime integration testing]
5. **Mock de autenticação** (handler de auth de teste) para exercitar autorização organizacional
   (UO/escopo do M1) sem JWT real. [antondevtips]
6. **Multi-tenant em teste** — cada teste de integração deve provisionar/derrubar tenant isolado
   (já temos `TenantProvisioningService`) para validar isolamento de dados — amarra com
   `ModuleUnitOfWorkIsolationTests` existente.

**Para o M9:** projeto `*.IntegrationTests` com Testcontainers (Postgres) + WireMock para
integrações gov + suíte Playwright E2E mínima dos fluxos críticos de suprimentos (licitação→
contrato→empenho).

---

## 5. Acessibilidade WCAG 2.1 AA / eMAG — base legal BR. CONFIANÇA: ALTA (normativa)

Eixo com **obrigatoriedade legal**, não só boa prática:

1. **eMAG é obrigatório** para sítios/portais do governo brasileiro — institucionalizado pela
   **Portaria SLTI/MP nº 3, de 7 de maio de 2007**, no âmbito do SISP. Versão vigente publicada:
   **eMAG 3.1** (abr/2014), alinhada à **WCAG 2.0** (e referência base para a 2.1 AA). [gov.br; emag.governoeletronico.gov.br]
2. **eMAG ≠ níveis A/AA/AAA** — por ser voltado a páginas de governo, **não admite exceções**:
   trata as recomendações como cumprimento integral, não opcional por prioridade. [emag.governoeletronico.gov.br]
3. **Normas ABNT recentes** citadas pelo gov.br: **ABNT NBR 17225** (acessibilidade digital em
   conteúdo/aplicações web) e **ABNT NBR 17060** (dispositivos móveis) — referência para o
   `src/Web` e qualquer app mobile futuro. [gov.br/governodigital]
4. **Base legal de fundo: Lei Brasileira de Inclusão (LBI) nº 13.146/2015** torna a acessibilidade
   de sítios obrigatória para órgãos de governo. **[a confirmar — citar artigo exato da LBI
   (arts. 63 e 53) direto do planalto.gov.br]** — a página gov.br consultada não trouxe o artigo.

**Para o M9 (engenharia da acessibilidade):**
- **Lint/CI automatizado**: integrar **axe-core** (ou `@axe-core/playwright`) na suíte Playwright
  (§4) — gate de CI para WCAG 2.1 AA. [a confirmar versão; padrão de indústria]
- **Componentes acessíveis por construção** no `src/Web`: foco visível, navegação por teclado,
  contraste AA, labels/ARIA, idioma `lang=pt-BR`, ordem de leitura, mensagens de erro associadas.
- **Página de acessibilidade + VLibras** (padrão gov.br) — **[a confirmar]** obrigatoriedade do
  VLibras no contexto municipal.

---

## 6. Síntese — backlog de engenharia M9 (robustez/QA)

| # | Item | Onde toca | Confiança |
|---|------|-----------|-----------|
| 1 | Inbox idempotente (event_id por handler+tenant) | BuildingBlocks/Outbox | ALTA |
| 2 | Lock de claim do relay (`SKIP LOCKED`/`READPAST`) + relay como worker | Outbox | MÉDIA ([a confirmar] provider) |
| 3 | Audit append-only com hash-chain HMAC (chave no KV) + INSERT-only grant | Auditing | ALTA |
| 4 | Arquivamento WORM em Azure Immutable Blob c/ retenção | Auditing/infra | MÉDIA ([a confirmar] prazo CONARQ) |
| 5 | Enricher `tenant.id` nos 3 sinais + middleware Baggage | ApiHost/OTel | ALTA |
| 6 | Exportador OTLP→Azure Monitor (FASE 5) sem NU1902 | ApiHost | MÉDIA ([a confirmar] backend) |
| 7 | Métricas de Outbox/DLQ (idade, taxa, profundidade) | OTel + Outbox | ALTA |
| 8 | `*.IntegrationTests` Testcontainers + WireMock (gov) | tests | ALTA |
| 9 | Playwright E2E + axe-core gate WCAG 2.1 AA em CI | tests + Web | ALTA |
| 10 | Hardening acessibilidade eMAG/WCAG no `src/Web` | Web | ALTA (normativa) |

---

## 7. Pendências (resolver antes/durante M9)

- **[a confirmar]** Provider de banco real do `ModuleDbContext` (Postgres vs SQL Server) → define
  sintaxe de lock do relay (item 2) e do INSERT-only grant (item 3).
- **[a confirmar]** Prazo de retenção/temporalidade da trilha de auditoria por tipo documental —
  **obter da pesquisa CONARQ/protocolo do M9** (amarra §2 ↔ eixo normativo).
- **[a confirmar]** Backend de observabilidade definitivo (Azure Monitor vs OTel Collector
  self-hosted) e versão do pacote OTLP sem NU1902.
- **[a confirmar]** Artigo exato da LBI 13.146/2015 (arts. 53/63) — citar de planalto.gov.br.
- **[a confirmar]** Obrigatoriedade de VLibras / página de acessibilidade no contexto municipal.
- **Decisão**: cardinalidade de métricas — `tenant.id` só em traces/logs e `tenant.tier`
  (ou agregado) em métricas, para não explodir séries.

---

## Fontes

**Outbox / resiliência**
- https://www.npiontko.pro/2025/05/19/outbox-pattern
- https://event-driven.io/en/outbox_inbox_patterns_and_delivery_guarantees_explained/
- https://dev.to/sagarmaheshwary/transactional-outbox-with-rabbitmq-part-2-handling-retries-dead-letter-queues-and-observability-4h19
- https://medium.com/@melistogan6/idempotency-dlq-and-the-outbox-pattern-in-kafka-a-practical-guide-to-consistent-streams-b5e7620ea80d
- https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html

**WORM / auditoria imutável**
- https://tracehold.ai/blog/immutable-audit-log-hmac-hash-chain/
- https://www.emergentmind.com/topics/immutable-audit-log
- https://mattermost.com/blog/compliance-by-design-18-tips-to-implement-tamper-proof-audit-logs/
- https://www.hubifi.com/blog/immutable-audit-log-guide
- https://www.luthor.ai/guides/worm-vs-audit-trail-17a-4-storage-method-2025-architecture
- https://devsecopsschool.com/blog/worm-storage/

**OpenTelemetry multi-tenant**
- https://oneuptime.com/blog/post/2026-01-24-multi-tenancy-opentelemetry/view
- https://opentelemetry.io/docs/concepts/signals/baggage/
- https://oneuptime.com/blog/post/2026-02-06-tenant-aware-telemetry-routing-multi-tenant/view

**Testes E2E / integração**
- https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- https://www.milanjovanovic.tech/blog/testcontainers-best-practices-dotnet-integration-testing
- https://dotnet.testcontainers.org/modules/playwright/
- https://antondevtips.com/blog/asp-net-core-integration-testing-best-practises

**Acessibilidade eMAG / WCAG (fontes oficiais)**
- https://emag.governoeletronico.gov.br/
- https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/acessibilidade-digital/modelo-de-acessibilidade
- https://emag.governoeletronico.gov.br/emag-3.pdf
