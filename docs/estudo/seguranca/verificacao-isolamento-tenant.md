# Verificação Cética — Isolamento Multi-Tenant (Tensorroot.Gov)

> Auditoria de confirmação do red-team em `ataque-isolamento-tenant.md`. Cada achado foi confirmado
> NO CÓDIGO (caixa-branca): **VULNERABILIDADE REAL** (com evidência arquivo:linha) ou **FALSO-POSITIVO
> / MITIGADO** (rebaixado, com a mitigação apontada). Itens **[a confirmar]** seguem dependendo de
> runtime e estão marcados como tal.
>
> **Tese de fundo confirmada:** a arquitetura é **database-per-tenant** (conexão dedicada por tenant
> resolvida do catálogo da Plataforma — `TenantConnectionResolver.cs:75-94`). O isolamento FÍSICO é a
> defesa primária; o GQF por `TenantId` é defesa em profundidade. **Nenhum dos achados é um vazamento
> direto A→B de dados de negócio** — o banco dedicado impede isso. O que os achados de `Guid.Empty`
> realmente quebram é o invariante **"nega por padrão"** DENTRO do banco de um mesmo tenant (linhas
> órfãs, mis-roteamento, colisão de cadeia de auditoria). A exceção é o **Achado 1**, que é uma
> escalada horizontal REAL e explorável no **banco de CONTROLE** (Plataforma), que de fato NÃO é
> isolado por tenant.

## Veredito por achado

| # | Sev. original | Veredito | Sev. ajustada | Evidência-chave |
|---|---|---|---|---|
| 1 | CRÍTICO | **REAL — confirmado** | **CRÍTICO** | `AdminEndpoints.cs:37-78` + `PermissionAuthorization.cs:32-41` + `Permissoes.cs:201` |
| 2 | CRÍTICO | **REAL — confirmado** (mas database-per-tenant limita a escopo intra-tenant) | **ALTO** | `ModuleDbContext.cs:45` + `ModelBuilderExtensions.cs:45-50` |
| 3 | ALTO | **PARCIAL** — `Guid.Empty` real; "mistura entre tenants" é FALSO (DB dedicado) | **MÉDIO** | `AuditSaveChangesInterceptor.cs:145` + `AuditoriaReadDbContext.cs` |
| 3b| ALTO | **REAL — confirmado** (convenção não verificada) | **MÉDIO** | `ModuleIntegrationEventWriter.cs:46-50` + `IntegrationEvent.cs` |
| 4 | MÉDIO | **REAL — confirmado** (in-process + TTL 5min; sem A→B) | **MÉDIO** | `TenantConnectionResolver.cs:15-59` + `TenantProvisioningService.cs:98` |
| 5 | MÉDIO | **REAL como gap, NÃO explorável** com token de 1ª parte | **BAIXO (defensivo)** | `TenantUnidadeContext.cs:57-61` + `EmissorToken.cs:55` |
| 6 | BAIXO | **REAL — confirmado** | **BAIXO** | `TenantSaveChangesInterceptor.cs:35-38` |
| 7 | BAIXO | **REAL — latente** (seed atual seguro) | **BAIXO** | `SistemaTenantContext.cs:15-18` + `IdentidadeModule.cs:154-197` |

**Placar: 6 reais (1 crítico, 1 alto, 2 médios, 2 baixos) · 1 parcial (rebaixado de ALTO p/ MÉDIO) ·
0 falsos-positivos puros.** A seção "Pontos NÃO vulneráveis" do red-team foi **confirmada correta**.

---

## Achado 1 — REAL / CRÍTICO (confirmado, prioridade máxima)

**Config cross-tenant de licenças de módulo via `tenantId` de rota.**

Evidência no código:
- `AdminEndpoints.cs:31-34` — o grupo `/api/admin/tenants/{tenantId:guid}/modulos` é protegido
  **apenas** por `.RequirePermission("admin.modulos.configurar")`.
- `AdminEndpoints.cs:55-75` — o handler `PUT /{modulo}` recebe `Guid tenantId` **da rota** e chama
  `provisioningService.DefinirModuloAsync(tenantId, nomeCanonico, requisicao.Ativo, ...)` **sem
  qualquer comparação** com o tenant do JWT. O `GET /` (`:37-43`) idem (`EnabledModulesAsync(tenantId)`).
- `PermissionAuthorization.cs:32-41` — `PermissaoHandler` aprova **só pela presença da claim `perm`**;
  não há comparação de tenant em lugar nenhum do handler.
- `Permissoes.cs:201` — `AdminModulosConfigurar` (`"admin.modulos.configurar"`) **PERTENCE a
  `Permissoes.Todas`**, que é semeado no papel "Administrador" de cada tenant. Logo, **todo admin de
  qualquer tenant possui essa claim**.
- `TenantProvisioningService.cs:60-82` — `DefinirModuloAsync` opera direto em
  `PlatformDbContext.TenantModules` (banco de CONTROLE, **sem GQF** — confirmado), gravando pela chave
  `(tenantId da rota, moduleName)`.
- `TenantContext.cs:22` — o tenant do JWT (`tenant_id`) ESTÁ disponível no escopo da requisição; uma
  verificação `rota.tenantId == tenantContext.TenantId` seria trivial — **e não existe**.

Contraste que confirma a intenção do projeto: `Program.cs:230` protege `/admin/tenants`
(provisionamento) com `PlataformaTenantsProvisionar`, escopo **deliberadamente fora** de
`Permissoes.Todas` (`Permissoes.cs:153-166`) — admin de tenant nunca o recebe. Os endpoints de
**módulos** quebram exatamente esse padrão ao usar uma permissão de tenant para uma operação de
plataforma.

**Exploração (credenciais legítimas de qualquer tenant A):**
`PUT /api/admin/tenants/{GUID_DO_TENANT_B}/modulos/Saude {"ativo": false}` → desativa o módulo do
tenant B. O gating em `Program.cs:189-211` passa então a responder **403 a todo `/api/saude/*`** do
tenant B (DoS direcionado). Também é possível **ativar** módulos não contratados (impacto de
billing) e **enumerar** licenças de qualquer tenant via `GET`.

**Por que é o único A→B real:** o banco de controle (Plataforma) NÃO é isolado por tenant — é
justamente o catálogo que mapeia tenants. O database-per-tenant não protege aqui.

**Correção (mantida do red-team):** trocar para permissão de PLATAFORMA fora de `Permissoes.Todas`;
OU remover `tenantId` da rota e derivar de `ITenantContext`; e, como defesa em profundidade imediata,
`rota.tenantId == tenantContext.TenantId` com 403 auditado.

---

## Achado 2 — REAL / rebaixado CRÍTICO → ALTO (escopo intra-tenant)

**GQF degrada para `TenantId == Guid.Empty` quando não há tenant — não bloqueia.**

Evidência:
- `ModuleDbContext.cs:45` — `public Guid CurrentTenantId => _tenantContext.HasTenant ? _tenantContext.TenantId : Guid.Empty;`
- `ModelBuilderExtensions.cs:45-50` — o predicado de tenant é instalado SEMPRE como
  `e.TenantId == context.CurrentTenantId`; **não há flag `DeveFiltrarTenant`** análoga ao
  `FiltrarPorUnidade` (`:52-64`). Quando `!HasTenant`, o filtro vira `e.TenantId == Guid.Empty`.

Confirmado: "sem tenant" vira "tenant zero" em vez de "negar tudo (1=0)". Linhas com
`TenantId == Guid.Empty` (entidades sem `IMustHaveTenant`, dados de seed/legados) ficam visíveis em
qualquer escopo sem tenant. Combinado com o Achado 6, um escopo sem tenant **grava** linha
`Guid.Empty` e a **relê** — "tenant fantasma".

**Rebaixamento justificado (CRÍTICO → ALTO):** por database-per-tenant, esse "tenant zero" é
**confinado ao banco físico de um único tenant** — não há leitura A→B de dados de negócio de outro
tenant. O dano real é a quebra do **deny-by-default** e exposição de linhas órfãs DENTRO do DB de um
tenant (ou em contexto de sistema). Sério (viola invariante I2/§5), mas não é o vazamento
cross-tenant que "CRÍTICO" sugeria.

**Correção (mantida):** o predicado deve retornar `1=0` quando `!HasTenant` e não for contexto de
sistema explícito; para `SistemaTenantContext`, **não instalar** o filtro (ver Achado 7).

---

## Achado 3 — PARCIAL / rebaixado ALTO → MÉDIO (a premissa cross-tenant é FALSA)

**Auditoria/Outbox usam `Guid.Empty` como tenant para entidades sem `IMustHaveTenant`.**

Confirmado o fato base:
- `AuditSaveChangesInterceptor.cs:145` — `var tenantId = entry.Entity is IMustHaveTenant tenant ? tenant.TenantId : Guid.Empty;`
- `ConvertDomainEventsToOutboxInterceptor.cs:55` — idêntico para domain events.
- `ModelBuilderExtensions.cs:106` — cadeia de hash indexada por `(TenantId, Sequencia)`.
- `OutboxPublisher.cs:72` — despacho usa `mensagem.TenantId` → mensagem `Guid.Empty` roda sob
  "tenant zero" (herda Achados 2/6).

**FALSO na parte cross-tenant:** o red-team afirma que "todas as linhas de auditoria de entidades
não-tenantizadas de **qualquer** módulo caem no mesmo bucket `Guid.Empty`" misturando tenants
distintos. **A trilha é gravada no banco DEDICADO de cada tenant** — `AuditoriaReadDbContext` lê a
tabela `AuditTrail` do **arquivo/conexão do tenant** (resolvida do JWT). O comentário em
`AuditoriaReadDbContext.cs:9-12` fala de compartilhamento **entre MÓDULOS dentro do MESMO banco do
tenant** (SQLite dev), **não entre tenants**. Logo, **não há mistura de cadeia entre tenants A e B** —
a premissa de compliance "a trilha por tenant deixa de ser por tenant" é incorreta no eixo
cross-tenant.

**O que sobra como real (MÉDIO):** dentro do DB de **um** tenant, linhas de auditoria de entidades
não-`IMustHaveTenant` (infra/legado) caem num bucket `Guid.Empty` separado da cadeia do tenant — uma
**segunda cadeia** no mesmo arquivo. Não corrompe a cadeia do tenant real, mas cria uma cadeia órfã e
mensagens de Outbox mis-roteadas (tenant zero). A condição de corrida na `Sequencia` `Guid.Empty`
permanece **[a confirmar]** em runtime (último selo lido fora de transação serializável —
`AuditSaveChangesInterceptor.cs:232-243`).

Observação de mitigação: o comentário em `:143-144` é correto — com a ordem Tenant→Audit, entidades
`IMustHaveTenant` JÁ chegam carimbadas, então o bucket `Guid.Empty` só recebe entidades de infra.

**Correção (mantida):** falhar-alto / validar `TenantId != Guid.Empty` em contexto tenantizado, ou
derivar do `TenantOverride`/`ITenantContext` corrente.

### Achado 3b — REAL / rebaixado ALTO → MÉDIO

**Tenant do Integration Event por reflexão de string "TenantId".**

Evidência:
- `ModuleIntegrationEventWriter.cs:26-28` — tenta primeiro `integrationEvent is IMustHaveTenant`
  (mitigação parcial), depois cai em `ExtrairTenantId`.
- `ModuleIntegrationEventWriter.cs:46-50` — `GetProperty("TenantId")` → `... is Guid id ? id : Guid.Empty`.
- `IntegrationEvent.cs:9-21` — `IIntegrationEvent`/`IntegrationEvent` **NÃO** declaram `TenantId`;
  **nenhum** contrato implementa `IMustHaveTenant` (grep confirmou 0 ocorrências). Logo o caminho de
  reflexão é o **único** efetivamente usado em produção.
- Verificado: os ~50 contratos `: IntegrationEvent(...)` hoje **têm** um `Guid TenantId` posicional
  (convenção respeitada — ex. `MatriculaEncerradaIntegrationEvent.cs:18`,
  `MSCGeradaIntegrationEvent.cs:67`), então **funciona HOJE**.
- **Não há enforcement:** o único teste que toca o tema (`SpecCodeConsistency.cs:147-151`) só
  inventaria eventos para consistência de spec — **não exige** a propriedade `TenantId`. Um contrato
  futuro com `Tenant`/`IdTenant`/`Guid?`, ou sem a propriedade, cai **silenciosamente** em
  `Guid.Empty` (NRT não pega reflexão) → mis-roteamento no Outbox.

Real, porém o impacto é **MÉDIO** (latente, depende de erro futuro de contrato, e confinado ao DB do
tenant origem). **Correção (mantida):** tornar `IMustHaveTenant`/`ITenantScopedIntegrationEvent`
obrigatório na assinatura + teste de arquitetura que reprove qualquer `IIntegrationEvent` sem o tenant
tipado.

---

## Achado 4 — REAL / MÉDIO (confirmado)

**Cache de conexão singleton com TTL de 5 min serve conexão obsoleta.**

Evidência:
- `TenantConnectionResolver.cs:21` — `TtlPadrao = 5 min`; `:15` cache é singleton
  (`ConcurrentDictionary<Guid, Entrada>`), chave = `tenantId`, valor = connection string.
- `:40-53` — `ObterOuAdicionar` só recalcula quando ausente OU expirado pelo TTL.
- `:56-59` — `Invalidar`/`InvalidarTodos` são **in-process** (mexem só no `_mapa` desta instância).
- `TenantProvisioningService.cs:98` — `cacheInvalidator.Invalidar(tenantId)` só é chamado dentro de
  `RotacionarConexaoAsync`. Rotação fora desse fluxo (Key Vault, outro processo/instância, restore)
  **não** invalida → até 5 min de janela; e **outras réplicas nunca recebem** a invalidação
  (in-process). **[a confirmar em runtime multi-instância]**

Confirmado MÉDIO. **Sem A→B**: a chave é o próprio `tenantId`; o pior caso é servir o **banco antigo
do MESMO tenant** (divergência de dados / banco reciclado), não o banco de outro tenant.
**Correção (mantida):** invalidação distribuída (pub-sub) disparada pela rotação no Key Vault;
fingerprint de tenant na conexão pós-failover.

---

## Achado 5 — REAL como gap / rebaixado MÉDIO → BAIXO (não explorável com token de 1ª parte)

**`sub` não-GUID desliga o filtro de Unidade Organizacional.**

Evidência:
- `TenantUnidadeContext.cs:57-61` — `if (!Guid.TryParse(currentUser.UserId, out var usuarioGuid)) { _deveFiltrar = false; return; }`.
  Um principal autenticado com `sub` não-GUID é tratado como "sistema" → `_deveFiltrar=false` → todas
  as UOs do tenant ficam visíveis.
- `CurrentUser.cs:13-15` — `UserId` vem de `Claim("sub") ?? Claim(ClaimTypes.NameIdentifier)`.

**Por que NÃO é explorável hoje:** o **único emissor de token** do sistema
(`EmissorToken.cs:55`) sempre escreve `sub = usuarioId.Value.ToString()` — **sempre um GUID**. Não há
fluxo de 1ª parte que produza `sub` textual. O caminho só dispara com **emissor externo/federado/SSO
futuro** emitindo `sub` não-GUID, ou token legado.

Rebaixado para **BAIXO (defensivo)**: gap de design real (distinguir "job de sistema" deveria ser por
marca explícita, não por parseabilidade do `sub`), mas **inalcançável** com a emissão atual.
**Correção (mantida):** para principal autenticado com `sub` inválido → **negar** (filtrar com
conjunto vazio), nunca abrir tudo; distinguir sistema por ausência de identidade autenticada.

---

## Achado 6 — REAL / BAIXO (confirmado)

**Interceptor de carimbo/bloqueio é no-op sem tenant.**

Evidência:
- `TenantSaveChangesInterceptor.cs:33-38` — `if (context is null || !tenantContext.HasTenant) { return; }`.
  Sem tenant: não carimba `TenantId` em `Added` (`:47-49`) nem bloqueia `Modified/Deleted`
  cross-tenant (`:50-56`). O XML-doc da classe (`:8-11`) promete "BLOQUEIA qualquer gravação
  cross-tenant", mas a guarda só vale **com** tenant.

Confirmado. É o complemento de gravação do Achado 2: escopo sem tenant grava `IMustHaveTenant` com
`TenantId` default (`Guid.Empty`) sem qualquer guarda. **BAIXO** porque, em fluxo legítimo, sempre há
tenant (JWT) ou `TenantOverride` (workers/outbox/assinatura — confirmados corretos abaixo).
**Correção (mantida):** quando `!HasTenant` e não for `SistemaTenantContext`, **lançar** ao detectar
`IMustHaveTenant` em `Added/Modified/Deleted`.

---

## Achado 7 — REAL / BAIXO (latente; seed atual seguro)

**Contexto de migração roda com GQF efetivamente desligado (`Guid.Empty`).**

Evidência:
- `SistemaTenantContext.cs:15-18` — `TenantId => Guid.Empty`, `HasTenant => false` → `CurrentTenantId`
  vira `Guid.Empty` e o GQF é instalado como `== Guid.Empty` (não desabilitado).
- **Mitigação atual confirmada:** o seed do admin usa corretamente `IgnoreQueryFilters()` + predicado
  explícito de `TenantId` em **todas** as consultas — `IdentidadeModule.cs:154-155`, `:170-171`,
  `:196-197`. Logo, hoje **não** há exploração.

Latente: qualquer query FUTURA adicionada num contexto `SistemaTenantContext` que esqueça
`IgnoreQueryFilters()`/predicado vai operar sob `Guid.Empty` silenciosamente (0 linhas ou colisão com
órfãs). Armadilha, não exploração. **Correção (mantida):** ao usar `SistemaTenantContext`, **não
instalar** o filtro de tenant (resolve junto com Achado 2).

---

## Pontos "NÃO vulneráveis" do red-team — CONFIRMADOS corretos

- **Login** (`IdentidadeRepositories.cs:46-57`): `ObterParaAutenticacaoAsync` usa
  `.IgnoreQueryFilters()` **e reaplica** `usuario.TenantId == tenantId && usuario.Email == email`
  sobre o DB já dedicado. Defesa em profundidade correta. **Confirmado seguro.**
- **Visualizador/verificador de auditoria** (`AdminEndpoints.cs:87-150`): conexão e filtro derivados
  de `ITenantContext` (JWT) — `consulta.Where(item => item.TenantId == tenant.TenantId)` (`:100`) e
  `verificador.VerificarAsync(..., tenant.TenantId, ...)` (`:143-144`). **Não** usa o `tenantId` de
  rota. **Confirmado seguro** (≠ Achado 1, que é só o grupo de módulos).
- **Provisionamento** (`/admin/tenants`, `Program.cs:230`): exige `PlataformaTenantsProvisionar`,
  fora de `Permissoes.Todas` (`Permissoes.cs:153-166`). **Confirmado seguro.**
- **Outbox/dispatch e assinatura em escopo dedicado** (`ScopedOutboxMessageDispatcher.cs:32`,
  `OutboxBackgroundService.cs:66,87`, `AssinaturaEmEscopoDedicado.cs:37,54`): cada escopo re-seta
  `TenantOverride` com o tenant correto antes de abrir o escopo. **Confirmado correto** — a ressalva
  é a ORIGEM da mensagem `Guid.Empty` (Achado 3/3b), não o roteamento.

---

## Conclusão

- **0 falsos-positivos puros.** A análise do red-team está tecnicamente bem-fundamentada.
- **1 correção de premissa importante:** Achado 3 **não** mistura cadeias de auditoria entre tenants
  (database-per-tenant impede); rebaixado de ALTO para MÉDIO. Achado 2 idem rebaixado de CRÍTICO para
  ALTO pelo mesmo motivo (sem A→B de dados de negócio).
- **O único A→B real** é o **Achado 1** (banco de CONTROLE da Plataforma, sem isolamento por tenant) —
  **escalada horizontal explorável com credenciais legítimas**. Corrigir já.
- **Família `Guid.Empty` (2 + 6 + 3 + 3b + 7):** corrigir em conjunto trocando "sem tenant" por
  "negar/lançar" — elimina a classe de linhas órfãs, cadeia órfã e mis-roteamento de Outbox.
- **Achado 5** rebaixado para defensivo (emissor de 1ª parte sempre emite `sub` GUID); fechar antes de
  qualquer integração SSO/federada.
- **Itens [a confirmar] em runtime:** corrida na `Sequencia` `Guid.Empty` (Achado 3); janela de cache
  obsoleto em cenário multi-instância (Achado 4).
