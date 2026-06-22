# Red-Team — Isolamento Multi-Tenant (Tensorroot.Gov)

> Avaliação adversarial (caixa-branca) do isolamento entre inquilinos. Foco: escapes do Global
> Query Filter, gravação cross-tenant, `IgnoreQueryFilters` perigoso, cache de conexão, e o
> `TenantOverride` dos workers/outbox vazando tenant. Itens marcados **[a confirmar]** exigem
> validação em runtime (não rodei o processo, só li o código).

**Arquitetura observada:** database-per-tenant (conexão dedicada por tenant resolvida do catálogo
da plataforma) + Global Query Filter por `TenantId` + filtro de UO + `TenantSaveChangesInterceptor`.
O isolamento físico (DB dedicado) é a defesa primária; o GQF é defesa em profundidade. Os achados
abaixo atacam exatamente as costuras onde essas duas camadas se desalinham.

## Resumo

**8 achados** — 2 CRÍTICOS, 2 ALTOS, 2 MÉDIOS, 2 BAIXOS.

| # | Severidade | Local | Essência |
|---|---|---|---|
| 1 | CRÍTICO | `ApiHost/Admin/AdminEndpoints.cs:55-78,37-52` | Admin de tenant A configura licenças de módulo de tenant B (route `tenantId` sem vínculo com o tenant do JWT) |
| 2 | CRÍTICO | `ModelBuilderExtensions.cs:48-49` + `ModuleDbContext.cs:45` | GQF cai para `TenantId == Guid.Empty` quando não há tenant — leitura sem tenant retorna linhas "órfãs" de qualquer tenant em vez de bloquear |
| 3 | ALTO | `AuditSaveChangesInterceptor.cs:145,232-243` + `ConvertDomainEventsToOutboxInterceptor.cs:55` | Cadeia de hash de auditoria e Outbox usam `Guid.Empty` como tenant para entidades sem `IMustHaveTenant`, colidindo cadeias de tenants distintos no mesmo bucket |
| 3b| ALTO | `ModuleIntegrationEventWriter.cs:46-50` | `TenantId` do Integration Event extraído por reflexão de string "TenantId" → `Guid.Empty` silencioso se a convenção mudar → mensagem drenada no tenant errado |
| 4 | MÉDIO | `TenantConnectionResolver.cs:40-53` | Cache singleton de conexão com TTL de 5 min; rotação fora do fluxo `RotacionarConexao` (Key Vault/failover) serve conexão obsoleta — janela de leitura/gravação no banco antigo |
| 5 | MÉDIO | `Identidade/Seguranca/TenantUnidadeContext.cs:57-60` | JWT com `tenant_id` válido mas `sub` não-GUID desliga o filtro de UO (`_deveFiltrar=false`), expondo todas as UOs do tenant |
| 6 | BAIXO | `TenantSaveChangesInterceptor.cs:35-38` | Interceptor de carimbo/bloqueio é no-op quando `!HasTenant` — gravações em escopo sem tenant não são carimbadas nem bloqueadas |
| 7 | BAIXO | `TributosModule.cs:111` / migração com `SistemaTenantContext` | DbContext de migração/seed roda com GQF efetivamente desligado (`Guid.Empty`); qualquer query acidental nesse contexto ignora tenant |

---

## Achado 1 — CRÍTICO: configuração cross-tenant de licenças de módulo

**Arquivo:** `src/ApiHost/Admin/AdminEndpoints.cs:31-78` (grupo `/api/admin/tenants/{tenantId:guid}/modulos`)
**Apoio:** `src/BuildingBlocks/.../Authorization/PermissionAuthorization.cs:27-43`

O grupo é protegido apenas por `RequirePermission("admin.modulos.configurar")`. O handler
`PermissaoHandler` (linha 32-41) verifica **somente a presença da claim `perm`** — não compara o
`tenantId` da rota com o `tenant_id` do JWT do chamador. O `tenantId` da rota é passado **direto**
para serviços que operam no `PlatformDbContext` (banco de CONTROLE, que **não tem** Global Query
Filter — `Tensorroot.Gov.Platform/Tenancy/Tenancy.cs:15-19` documenta "não é isolado por tenant"):

```
grupo.MapPut("/{modulo}", ...) => provisioningService.DefinirModuloAsync(tenantId, nomeCanonico, requisicao.Ativo, ...)
grupo.MapGet("/", ...)        => moduleProvider.EnabledModulesAsync(tenantId, ...)
```

A permissão `admin.modulos.configurar` é concedida por administradores **de cada tenant** (faz parte
de `Permissoes.Todas`, semeado no papel "Administrador" de cada tenant — `IdentidadeModule.cs:162`).

**Cenário de exploração:**
1. Atacante é administrador legítimo do tenant A (Câmara de cidade X). Seu JWT contém `tenant_id=A`
   e `perm=admin.modulos.configurar`.
2. `PUT /api/admin/tenants/{GUID_DO_TENANT_B}/modulos/Saude` com `{"ativo": false}`.
3. A autorização passa (claim presente). `DefinirModuloAsync(B, "Saude", false)` desativa a licença
   do módulo Saúde do tenant B (uma Prefeitura concorrente / outro ente).
4. Resultado: DoS direcionado — o gating em `Program.cs:201-206` passa a responder 403 a TODAS as
   rotas `/api/saude/*` do tenant B. Também é possível **ATIVAR** módulos não contratados (impacto
   comercial/billing) e enumerar (via `GET`) quais módulos qualquer tenant licenciou.

Diferente do endpoint `/admin/tenants` (provisionamento), que corretamente exige
`PlataformaTenantsProvisionar` — permissão **fora** de `Permissoes.Todas`, logo nunca concedida a
admin de tenant (`Program.cs:230`) — os endpoints de `/api/admin/tenants/{tenantId}/modulos` usam
uma permissão de tenant para uma operação de plataforma. É escalada horizontal entre tenants.

**Correção:**
- Trocar a permissão por uma de PLATAFORMA (ex.: `plataforma.modulos.configurar`, **fora** de
  `Permissoes.Todas`), espelhando o endpoint de provisionamento; OU
- Se a operação for legitimamente self-service do tenant, **remover `tenantId` da rota** e derivar
  o tenant exclusivamente de `ITenantContext.TenantId` (JWT), rejeitando qualquer id externo;
- Adicionar verificação explícita `route.tenantId == tenantContext.TenantId` (com 403 auditado) como
  defesa em profundidade enquanto a permissão não é segregada.

---

## Achado 2 — CRÍTICO: Global Query Filter degrada para `Guid.Empty` sem tenant

**Arquivos:** `src/BuildingBlocks/.../ModuleDbContext.cs:45` e `.../ModelBuilderExtensions.cs:45-50`

```csharp
public Guid CurrentTenantId => _tenantContext.HasTenant ? _tenantContext.TenantId : Guid.Empty;
// filtro: e.TenantId == context.CurrentTenantId
```

Quando o contexto de tenant **não resolve** (`HasTenant == false`), o filtro não desabilita nem
lança — ele compara `TenantId == Guid.Empty`. Isso transforma "sem tenant" em "tenant zero". Qualquer
linha com `TenantId == Guid.Empty` (e há caminhos que produzem exatamente isso: entidades sem
`IMustHaveTenant` carimbado, dados legados/seed, migrações de dados como `RaizPendente` em
`IdentidadeModule.cs:191`) torna-se visível em **qualquer** escopo sem tenant. Não é o vazamento
clássico A→B (cada tenant tem seu DB), mas é um **vazamento de linhas órfãs e quebra do invariante
"nega por padrão"**: o esperado seria a leitura sem tenant retornar vazio/erro, não um conjunto
pseudo-tenant.

Combinado com o Achado 6 (interceptor no-op sem tenant), um escopo sem tenant pode **gravar** uma
linha com `TenantId == Guid.Empty` e depois **lê-la** de volta — criando um "tenant fantasma"
compartilhável entre quaisquer escopos não-tenantizados (jobs, dispatch mal-roteado, requisição
autenticada cujo token perdeu a claim `tenant_id`).

**Correção:** o filtro deve **bloquear** em vez de degradar. Opções:
- Expor uma flag `DeveFiltrarTenant` análoga a `FiltrarPorUnidade`; quando não há tenant E não é
  contexto de sistema explícito, fazer o predicado retornar `false` para tudo (`1=0`), nunca
  `== Guid.Empty`.
- Para contextos de sistema legítimos (migração), exigir `SistemaTenantContext` explícito e, nesse
  caso, **não instalar** o filtro de tenant (em vez de instalá-lo com `Guid.Empty`).
- Proibir, por invariante de domínio, `TenantId == Guid.Empty` em qualquer `IMustHaveTenant`
  persistido (check no interceptor — ver Achado 6).

---

## Achado 3 — ALTO: auditoria e Outbox usam `Guid.Empty` como tenant para entidades sem `IMustHaveTenant`

**Arquivos:**
- `src/BuildingBlocks/.../Auditing/AuditSaveChangesInterceptor.cs:145` (`... : Guid.Empty`) e a cadeia por tenant em `:232-243`
- `src/BuildingBlocks/.../Outbox/ConvertDomainEventsToOutboxInterceptor.cs:55` (`... : Guid.Empty`)

A trilha de auditoria sela uma **cadeia de hash por TenantId** (`HasIndex(TenantId, Sequencia)`,
`ModelBuilderExtensions.cs:106`). Para entidades que **não** implementam `IMustHaveTenant`, o
interceptor grava `TenantId = Guid.Empty`. Num banco compartilhado entre módulos no mesmo arquivo
(modo SQLite/dev — `AuditoriaReadDbContext.cs:9-12` confirma compartilhamento), **todas** as linhas
de auditoria de entidades não-tenantizadas de **qualquer** módulo caem no mesmo bucket
`Guid.Empty`, encadeadas na mesma sequência. Isso:
1. Mistura a trilha imutável de tenants distintos numa única cadeia (problema de compliance/TCE: a
   trilha "por tenant" deixa de ser por tenant para essas linhas).
2. Cria contenção/condição de corrida na sequência `Guid.Empty` quando dois escopos gravam
   "ao mesmo tempo" no arquivo compartilhado **[a confirmar]** — duas linhas podem reivindicar a
   mesma `Sequencia` (o último selo é lido fora de transação serializável).

O mesmo `Guid.Empty` no `ConvertDomainEventsToOutboxInterceptor` significa que um domain event
emitido por agregado sem `IMustHaveTenant` vira uma `OutboxMessage` com `TenantId = Guid.Empty`. No
`OutboxPublisher.cs:72` o despacho usa `mensagem.TenantId` para carimbar o `TenantOverride` do
escopo de handler — ou seja, **o evento seria processado sob "tenant zero"**, e qualquer gravação do
handler herda o Achado 2/6.

**Correção:** falhar-alto quando uma entidade auditável/portadora de evento não expõe tenant em
contexto tenantizado (lançar em vez de cair para `Guid.Empty`); ou derivar o tenant do
`ITenantContext`/`TenantOverride` corrente (que SEMPRE existe nos fluxos legítimos) em vez de da
entidade. Validar `TenantId != Guid.Empty` antes de gravar trilha/outbox.

### Achado 3b — ALTO: tenant do Integration Event por reflexão de string

**Arquivo:** `src/BuildingBlocks/.../Outbox/ModuleIntegrationEventWriter.cs:46-50`

```csharp
var prop = integrationEvent.GetType().GetProperty("TenantId");
return prop?.GetValue(integrationEvent) is Guid id ? id : Guid.Empty;
```

O tenant do Integration Event é obtido por uma busca de propriedade **por nome de string**
(`"TenantId"`). Se um contrato (em `*.Contracts`) usar outro nome (`Tenant`, `IdTenant`), tipar como
`string`/`Guid?`, ou simplesmente esquecer a propriedade — o tenant cai silenciosamente para
`Guid.Empty` (sem warning, NRT não pega reflexão). A mensagem é então drenada sob "tenant zero" e
despachada no DB/escopo errado (`OutboxPublisher.cs:72`). É um acoplamento frágil por convenção não
verificada, num ponto crítico de roteamento de tenant.

**Correção:** definir `IMustHaveTenant` (ou uma interface de contrato `ITenantScopedIntegrationEvent`)
como **obrigatória** na assinatura de todo Integration Event; remover a extração por reflexão. Um
teste de arquitetura (NetArchTest) que reprova qualquer `IIntegrationEvent` sem a propriedade tipada
fecharia a brecha em compile-time.

---

## Achado 4 — MÉDIO: cache de conexão singleton com TTL serve conexão obsoleta

**Arquivo:** `src/Platform/Tensorroot.Gov.Platform/Tenancy/TenantConnectionResolver.cs:15-62`

O `TenantConnectionCache` é singleton, chaveado por `tenantId`, com TTL de 5 min. A invalidação só
ocorre no caminho `RotacionarConexaoAsync` (`TenantProvisioningService.cs:98`). Qualquer rotação de
segredo/failover que **não** passe por esse método (rotação no Key Vault, mudança direta no catálogo
por outro processo/instância, restore de banco) deixa **até 5 minutos** de janela em que a instância
serve a connection string antiga. Em database-per-tenant, isso pode significar continuar lendo/gravando
no banco **antigo** do tenant (pós-migração) — divergência de dados, e potencialmente apontar para um
banco que foi reciclado/reatribuído. Não é vazamento A→B direto, mas é **resolução de tenant para o
recurso físico errado**, que é a mesma classe de risco.

Observação adicional: a chave do cache é só `tenantId` e o valor é a connection string; não há
verificação de que a conexão resolvida pertence ao tenant esperado após failover. O TTL "curto" é
mitigação, não garantia.

**Correção:** publicar invalidação **multi-instância** (a invalidação atual é in-process; outras
réplicas mantêm o cache obsoleto) via canal de cache distribuído / pub-sub; e tornar a rotação no
Key Vault um evento que dispara `Invalidar`/`InvalidarTodos` em todas as instâncias. Considerar TTL
ainda menor para o catálogo de conexões ou validação de "tenant fingerprint" na conexão.

---

## Achado 5 — MÉDIO: `sub` não-GUID desliga o filtro de Unidade Organizacional

**Arquivo:** `src/Modules/Identidade/.../Seguranca/TenantUnidadeContext.cs:57-60`

```csharp
if (!Guid.TryParse(currentUser.UserId, out var usuarioGuid)) { _deveFiltrar = false; return; }
```

A intenção é "sem sujeito (job/sistema) → não filtra por UO". Mas a condição usada é "UserId não é um
GUID parseável". Um principal **autenticado** (com `tenant_id` válido, dentro do banco do tenant
correto) cujo claim `sub` não seja um GUID — token legado, integração que emite `sub` textual, ou
`sub` ausente caindo para outra claim — passa a ser tratado como sistema: `_deveFiltrar = false`,
**expondo todas as UOs do tenant** em vez de só as que o sujeito pode ler (MODELO §5/§10.3). É escape
do filtro de UO (camada de isolamento intra-tenant), não cross-tenant, mas viola o deny-by-default
intra-tenant que o próprio código diz aplicar (I2).

**Correção:** distinguir "principal autenticado" de "job de sistema" por uma marca explícita
(ex.: ausência de identidade autenticada / claim de tipo de principal), não pela parseabilidade do
`sub`. Para principal autenticado com `sub` inválido → **negar** (`_deveFiltrar = true` com conjunto
vazio), nunca abrir tudo.

---

## Achado 6 — BAIXO: interceptor de carimbo/bloqueio cross-tenant é no-op sem tenant

**Arquivo:** `src/BuildingBlocks/.../Multitenancy/TenantSaveChangesInterceptor.cs:35-38`

```csharp
if (context is null || !tenantContext.HasTenant) { return; }
```

Quando não há tenant resolvido, o interceptor **não carimba** `TenantId` em entidades novas **nem
bloqueia** gravações cross-tenant. Combinado com o Achado 2, um `SaveChanges` num escopo sem tenant
grava entidades com `TenantId` default (`Guid.Empty`) sem qualquer guarda, e o bloqueio de
modificação cross-tenant (linha 50-56) nunca roda. O comentário da classe promete "BLOQUEIA qualquer
gravação cross-tenant", mas a guarda só vale quando já há tenant.

**Correção:** quando `!HasTenant`, em contexto que **não** seja `SistemaTenantContext` explícito,
**lançar** ao detectar qualquer `IMustHaveTenant` em `Added/Modified/Deleted` (em vez de retornar
silenciosamente). Migrações/seed legítimos usam `SistemaTenantContext` e podem ser isentados por
tipo, de forma explícita.

---

## Achado 7 — BAIXO: contexto de migração roda com GQF efetivamente desligado

**Arquivos:** `TributosModule.cs:111`, `IdentidadeModule.cs:123` e similares (todos os módulos),
via `SistemaTenantContext` (`Multitenancy/SistemaTenantContext.cs`).

O DbContext de migração/seed é construído com `SistemaTenantContext.Instancia` (`HasTenant=false`,
`TenantId=Guid.Empty`). Por design, o filtro de tenant fica em `Guid.Empty` (Achado 2). Durante o
seed do admin (`IdentidadeModule.cs:153-199`) o código corretamente usa `IgnoreQueryFilters()` +
predicado explícito de `TenantId` — bom. Mas **qualquer** query futura adicionada nesse contexto que
esqueça o `IgnoreQueryFilters()`/predicado explícito vai operar sob `Guid.Empty` silenciosamente
(podendo retornar 0 linhas e tomar decisões erradas, ou colidir com linhas órfãs). É uma armadilha
latente, não uma exploração imediata.

**Correção:** ao usar `SistemaTenantContext`, **não instalar** o filtro de tenant (Achado 2), de modo
que o contexto de sistema veja tudo do banco físico (correto para DDL/migração) e o filtro
`== Guid.Empty` nunca exista para induzir resultados silenciosamente vazios.

---

## Pontos verificados que NÃO são vulneráveis (para evitar falso-positivo)

- **Login (`IdentidadeEndpoints.cs:29-63`)**: resolve tenant pelo índice central no banco de
  controle, seta `TenantOverride` ANTES de `AutenticarCommand`, e `ObterParaAutenticacaoAsync`
  (`IdentidadeRepositories.cs:46-60`) usa `IgnoreQueryFilters()` **mas reaplica o predicado
  `usuario.TenantId == tenantId` explicitamente** sobre o DB já dedicado. Defesa em profundidade
  correta. Falha de login indistinguível (anti-enumeração) ok.
- **Visualizador/verificador de auditoria (`AdminEndpoints.cs:87-150`)**: conexão resolvida do JWT
  (`ITenantConnectionResolver`) E filtro por `tenant.TenantId` (JWT) — consistente, sem uso do
  `tenantId` de rota. Seguro (≠ Achado 1, que é o grupo de módulos).
- **Outbox cross-módulo (`ScopedOutboxMessageDispatcher.cs` + `OutboxBackgroundService.cs`)**: cada
  mensagem/módulo em escopo de DI próprio com `TenantOverride` re-setado; `ScopeDbContextHolder`
  falha-alto (H5) se dois `ModuleDbContext` colidirem no mesmo escopo. O tenant do laço de drenagem
  vem do catálogo (`Tenants.Where(Ativo)`), por tenant, antes de abrir o escopo. Padrão correto — a
  ressalva é o Achado 3 (tenant `Guid.Empty` na origem da mensagem), não o roteamento.
- **Provisionamento (`/admin/tenants`)**: exige `PlataformaTenantsProvisionar`, fora de
  `Permissoes.Todas` — admin de tenant não a possui. Seguro.
- **Assinatura em escopo dedicado (`AssinaturaEmEscopoDedicado.cs`)**: captura o tenant ANTES de
  abrir o novo escopo e o reaplica no `TenantOverride` — correto.

---

## Recomendação de prioridade

1. **Achado 1** (config cross-tenant de módulos) — corrigir já: é escalada horizontal explorável com
   credenciais legítimas de qualquer tenant.
2. **Achado 2 + 3 + 6** (semântica `Guid.Empty`) — corrigir em conjunto: transformar "sem tenant" em
   "negar/lançar" em vez de "tenant zero" elimina uma classe inteira de vazamento de linhas órfãs,
   colisão de cadeia de auditoria e mis-roteamento de Outbox.
3. **Achado 3b / 5 / 4** — fechar as convenções frágeis (reflexão de tenant, parse de `sub`,
   invalidação multi-instância de conexão).
