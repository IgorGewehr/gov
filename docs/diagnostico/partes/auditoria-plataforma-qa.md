# Auditoria — Plataforma, Segurança e QA

> Auditoria EMPÍRICA (código lido + build/contagem executados em 2026-06-22, Release, .NET 8).
> Escopo: multi-tenant, Outbox, Identidade/RBAC/Auditoria, certificado A1, provisionamento;
> build, testes, fitness functions, CI/CD, deploy Azure, observabilidade.

---

## 🔴 ACHADO CRÍTICO #0 — A SOLUÇÃO NÃO COMPILA

`dotnet build Tensorroot.Gov.sln -c Release` → **FALHA da compilação. 2 Erro(s).**

```
EmpenhoRepository.cs(8,68): error CS0535: "EmpenhoRepository" não implementa
  IEmpenhoRepository.ListarPorDotacaoAsync(DotacaoOrcamentariaId, CancellationToken)
EmpenhoRepository.cs(8,68): error CS0535: "EmpenhoRepository" não implementa
  IEmpenhoRepository.ListarComSaldoAbertoPorExercicioAsync(int, CancellationToken)
```

- Arquivo: `src/Modules/Financas/...Infrastructure/Persistence/Repositories/EmpenhoRepository.cs`
  implementa apenas `Adicionar` e `ObterPorIdAsync`; faltam 2 membros declarados na interface
  (`...Application/Abstractions/IEmpenhoRepository.cs`) e JÁ CONSUMIDOS pelos handlers
  `ObterEmpenho.cs` e `RestosAPagar/EncerrarExercicio.cs`.
- **Impacto direto na maior preocupação do dono (CONTABILIDADE/TCE):** o módulo Finanças
  (empenho→liquidação→pagamento, restos a pagar) não builda.
- **Consequência para o QA inteiro:** o CI (`ci.yml`) roda `dotnet build` antes de `dotnet test --no-build`.
  Com o build quebrado, **o passo de testes NUNCA chega a rodar** — os 575 testes e as fitness
  functions estão, na prática, **não executando hoje**. O CI está vermelho (ou estaria, em `main`).

---

## 1. Multi-Tenancy (banco dedicado + query filter + interceptors)

### O que existe (REAL e profundo)
- **Database-per-tenant real.** `Tenant.ConnectionString` no catálogo da plataforma
  (`PlatformDbContext`, schema `plataforma`). `TenantConnectionResolver` resolve a conexão dedicada
  do tenant atual com **cache singleton** (`TenantConnectionCache` / `ConcurrentDictionary`).
  Cada `DbContext` de módulo é registrado com `ResolveConnectionString()` (visto em `TributosModule`).
- **Global Query Filter por reflexão** (`ModelBuilderExtensions.ApplyTenantQueryFilter`): aplica
  `e.TenantId == context.CurrentTenantId` a **toda** entidade `IMustHaveTenant`, reavaliado por
  consulta (lê `ModuleDbContext.CurrentTenantId` do `ITenantContext`). Aplicado na base `ModuleDbContext`.
- **TenantSaveChangesInterceptor**: carimba `TenantId` em `Added` e **lança exceção** em
  `Modified/Deleted` cross-tenant. Correto e alinhado à constituição §5.
- **Resolução de tenant**: `TenantContext` dá precedência ao `TenantOverride` (jobs/provisionamento)
  e cai para a claim `tenant_id` do JWT. `WorkerTenantContext` para o worker. Bom design.

### Real × stub / lacunas
- ⚠️ **Fallback SQLite por convenção** (`tenant_{guid}.db`) é dev-only — aceitável, mas múltiplos
  módulos compartilham o MESMO arquivo SQLite do tenant (ver `SchemaProvisioner`), o que NÃO é
  "schema isolado por módulo" da constituição §9. Em SqlServer cada módulo tem schema próprio; em
  SQLite todos caem no mesmo arquivo. Divergência dev vs. prod — risco de bugs só aparecerem em prod.
- ⚠️ **Cache de conexão nunca invalida.** `TenantConnectionCache` é populado e jamais limpo;
  `Tenant.DefinirConexao` (rotação de conexão) não invalida o cache → conexão obsoleta até reiniciar.
- ⚠️ Não há **fitness function** que verifique que toda entidade persistida implementa `IMustHaveTenant`
  (a regra §5 "toda entidade é IMustHaveTenant" é confiada à revisão humana, não automatizada).

## 2. Outbox Pattern

### O que existe (REAL)
- `ConvertDomainEventsToOutboxInterceptor`: materializa os domain events como `OutboxMessage` na
  **mesma transação** do SaveChanges (consistência transacional). Correto.
- `OutboxPublisher`: lê pendentes (`ProcessedOnUtc == null`), ordena por `OccurredOnUtc`, desserializa
  por `AssemblyQualifiedName`, publica via MediatR, marca processado. Resiliente por mensagem
  (try/catch grava `Error`, não interrompe lote). `OutboxBackgroundService` drena a cada **30s**,
  por tenant ativo, por módulo licenciado, com `TenantOverride`.

### Lacunas
- ⚠️ **Sem retry/back-off nem dead-letter.** Mensagem que falha grava `Error` e é **reprocessada
  indefinidamente** a cada ciclo, sem contador de tentativas nem limite. Um evento "veneno" reprocessa
  para sempre. Não há coluna `RetryCount`/`NextAttemptUtc`.
- ⚠️ **Sem idempotência no consumidor** materializada (a constituição §10 fala em behavior de
  Idempotency; ver §6 abaixo — não encontrado).
- ⚠️ Publicação é **in-process MediatR**, não broker. Consistente com "monolito modular", mas não há
  garantia de entrega além do próprio processo.

## 3. Identidade / RBAC / Auditoria

### O que existe (REAL)
- **JWT auto-emitido (HS256)** — `EmissorToken`: claims `sub/jti/name/email/tenant_id/tenant_name` +
  **uma claim `perm` por permissão efetiva** + `role`. `MapInboundClaims=false`, `ClockSkew=1min`,
  valida issuer/audience/lifetime/signing-key. Sólido.
- **RBAC negar-por-padrão** real: `PermissionPolicyProvider` materializa políticas dinâmicas
  `perm:<escopo>`; `PermissaoHandler` aprova só com a claim correspondente; helper `RequirePermission`.
  Catálogo canônico de permissões em `Permissoes.cs`.
- **Senha BCrypt** (`SenhaHasher`, work factor 12, verificação em tempo constante, trata hash legado).
- **AutenticarHandler** com defesa contra **enumeração de contas por timing** (hash sintético quando
  usuário inexistente) e mensagem genérica uniforme. Maduro.
- **Índice central email→tenant** (`UsuarioTenantIndex` no banco de controle, PK = email global) para
  resolver o tenant no login, já que usuários vivem no banco dedicado. Bom design multi-tenant.
- **AuditSaveChangesInterceptor**: grava `AuditTrail` (old/new em JSON, colunas afetadas, UserId, IP,
  timestamp UTC) por mutação. `CurrentUser` lê `sub/name/email/IP` do HttpContext.
- **Visualizador de auditoria admin** (`AdminEndpoints` `/api/admin/auditoria`) paginado e filtrável,
  protegido por `admin.auditoria.ver`, em DbContext somente-leitura (`AuditoriaReadDbContext`).

### Bugs / lacunas (relevantes para o TCE)
- 🔴 **Ordem dos interceptors quebra o TenantId da auditoria em INSERTs.** Em `TributosModule` os
  interceptors são registrados na ordem `Audit, Tenant, Outbox`. Os interceptors rodam **em ordem de
  registro**, então o `AuditSaveChangesInterceptor` lê `entity.TenantId` ANTES de o
  `TenantSaveChangesInterceptor` carimbá-lo. Para entidades `Added`, `AuditTrail.TenantId` fica
  **`Guid.Empty`**. Como o visualizador filtra por `TenantId`, **as criações podem não aparecer na
  trilha do tenant** — exatamente a evidência que o TCE pede. (O `ConvertDomainEventsToOutbox` tem o
  mesmo padrão de leitura, mesmo risco no `OutboxMessage.TenantId`.)
- 🔴 **AuditTrail "imutável" só no nome.** É uma classe com props `init`, gravada na MESMA tabela do
  banco do tenant, sem trigger/append-only/WORM, sem hash-chain. Qualquer um com acesso ao banco
  (ou um bug com o filtro desligado) pode alterar/apagar. Para escrutínio de Tribunal de Contas isso é
  insuficiente — não há garantia técnica de imutabilidade.
- ⚠️ **Auditoria de LEITURA (LGPD §6) ausente.** A constituição exige "trilha de acesso: quem leu o
  quê, quando, por quê" para Saúde/Assistência/menores. O interceptor só captura mutações
  (`Added/Modified/Deleted`); **não há trilha de leitura** de dados sensíveis.
- ⚠️ Sem **refresh token / revogação**; token de 60min sem blacklist. Logout é client-side.
- ⚠️ **Credenciais default versionadas**: `admin@tensorroot.gov / Mudar@123` em
  `IdentidadeModule` (constantes) e em `appsettings.Development.json`. Risco se vazar para prod.

## 4. Certificado A1 / Azure Key Vault

### Estado: PROVISIONADO NA INFRA, NÃO INTEGRADO NO CÓDIGO (stub)
- ✅ Bicep cria **Key Vault** (RBAC, soft-delete) e dá role *Key Vault Secrets User* à identidade
  gerenciada do App Service; injeta `KeyVault__Uri` como app setting.
- ✅ Pacotes presentes em `Directory.Packages.props`: `Azure.Identity`,
  `Azure.Security.KeyVault.Secrets`, `Azure.Security.KeyVault.Certificates`.
- 🔴 **Mas `Program.cs` NUNCA chama `AddAzureKeyVault` / `SecretClient` / `DefaultAzureCredential`.**
  `grep` em todo `src` por `AddAzureKeyVault|SecretClient|DefaultAzureCredential|X509|.pfx` →
  **nenhuma ocorrência de uso real**; só comentários "em produção virá do Key Vault". Os segredos
  (`Jwt:Secret`) são lidos de configuração plana. **A integração com Key Vault não está fiada.**
- 🔴 **Assinatura A1 100% simulada.** Toda referência a A1/certificado é comentário ("ACL + Polly +
  A1 na Infrastructure") ou **stub**: `SimuladoSiapcPadGateway`, `SimuladoSiconfiGateway`,
  `AssinaturaIcpBrasilService` simulado em Saúde (`AssinaturaDigital.De(...)` devolve dados sem
  assinar nada). **Nenhum carregamento de `.pfx`, nenhum `X509Certificate2`, nenhuma assinatura
  XMLDSig real.** Isto é exatamente o que a remessa TCE-RS (SIAPC/PAD) e o eSocial exigem.

## 5. Provisionamento de Tenant

### O que existe (REAL)
- `/admin/tenants` (POST) + `TenantProvisioner`: grava catálogo (`TenantProvisioningService`) e
  **migra o banco dedicado de cada módulo licenciado** (`IModule.MigrarBancoAsync` → `SchemaProvisioner`).
- `IdentidadeModule.MigrarBancoAsync` **semeia o administrador** (papel + usuário admin) e popula o
  índice central email→tenant. Onboarding funcional ponta-a-ponta.
- **Gating de licenciamento** real no pipeline (`Program.cs`): requisição a `/api/<modulo>` de módulo
  não licenciado → **403** (`ITenantModuleProvider.IsModuleEnabledAsync`). Admin endpoints para
  ligar/desligar módulos por tenant. Alinhado à constituição §4.
- Seed de DEV provisiona a Prefeitura de Maximiliano de Almeida/RS com todos os módulos.

### Lacunas
- 🔴 O endpoint `/admin/tenants` usa só `.RequireAuthorization()` (qualquer usuário autenticado),
  **sem permissão RBAC específica** — diferente de `AdminEndpoints`, que exige `admin.modulos.configurar`.
  Qualquer token válido de qualquer tenant pode **provisionar novos tenants**. Escalonamento.
- ⚠️ Módulo não licenciado retorna **403, nunca 404** (constituição pede "404/403 auditado"); e a
  decisão de bloqueio **não é auditada** (sem trilha do acesso negado).
- ⚠️ Bootstrap da plataforma usa `EnsureCreatedAsync` em SQLite e `MigrateAsync` só em SqlServer
  (divergência dev/prod já citada).

## 6. Pipeline MediatR / Behaviors

- ✅ `ValidationBehavior` (FluentValidation) e `LoggingBehavior` existem e são registrados
  (`AddApplicationPipeline`).
- 🔴 **Faltam 2 dos 4 behaviors prometidos** (constituição §10): **não há `TransactionBehavior`/
  UnitOfWork behavior** nem **`IdempotencyBehavior`** na pasta `Behaviors/` (só Validation e Logging).
  Existe `IUnitOfWork`/`ModuleUnitOfWork` + `ScopeDbContextHolder`, mas a transação/idempotência **não
  está no pipeline** como behavior. A ordem "Validation→Logging→Transaction→Idempotency" não se cumpre.

## 7. Build, Testes e Fitness Functions

| Item | Medição empírica |
|---|---|
| **Build Release** | 🔴 **FALHA** — 2 erros em Finanças (ver Achado #0). 0 warnings nos projetos que compilam. |
| **Arquivos de teste** | 51 arquivos `*.cs` de teste (excl. base/gerados) |
| **Métodos de teste** | **575** atributos `[Fact]`/`[Theory]` (contados via grep) |
| **`[InlineData]`** | apenas 8 — quase tudo é `[Fact]`, pouco data-driven |
| **Fitness Functions** | `FitnessFunctions.cs` (4): Domain↛Infra/EFCore/AspNet; App↛Infra/EFCore; módulo↛interno de outro módulo; SharedKernel↛EFCore/AspNet. NetArchTest. ✅ cobrem §2/§12. |
| **Maintainability** | `MaintainabilityTests.cs`: nenhum `.cs` de produção > 500 linhas (anti-god-file). ✅ |
| **Spec-Code Consistency** | `SpecCodeConsistency.cs`: manifestos `*.rules.md` devem bater com commands/queries/eventos do código. ✅ governança Rules-as-Code real. |

### Observações de QA
- ⚠️ **Nenhum teste de integração HTTP de verdade** sobre o `WebApplicationFactory` (o `Program` expõe
  `partial class Program` para isso, mas não há projeto de testes E2E/API consumindo-o). Os 575 testes
  são quase todos de domínio/handler in-memory.
- ⚠️ **Sem cobertura medida** (nenhum coverlet/relatório de cobertura no CI; só `trx` publicado).
- ⚠️ Multi-tenant isolation testado **só na Identidade** (`IsolamentoTenantTests`, 5 testes) — não há
  teste de vazamento cross-tenant nos módulos de negócio (Finanças/Tributos/Saúde).
- ⚠️ Como o build quebra, **não foi possível confirmar que os 575 passam**; estão verdes apenas no
  histórico anterior ao bug de Finanças.

## 8. CI/CD + Deploy Azure (Bicep)

### O que existe (REAL)
- `ci.yml`: restore → build Release (NuGetAudit como warning-as-error) → `dotnet list --vulnerable`
  (informativo) → `dotnet test` (inclui fitness functions) → publica `.trx`. Estrutura correta.
- `deploy.yml`: 3 jobs — publish ApiHost → provision Bicep (OIDC, `azure/arm-deploy`) → deploy App
  Service. Login **OIDC sem segredo** (id-token). Bom.
- `main.bicep`: App Service Linux .NET 8 (`httpsOnly`, `minTlsVersion 1.2`, `ftpsState Disabled`,
  identidade gerenciada), Azure SQL (S0, TLS 1.2), Storage (sem blob público, TLS 1.2), Key Vault
  (RBAC + soft-delete), role assignment. Sólido como base.

### Lacunas
- 🔴 **CI quebrado na prática** pelo build de Finanças (#0). O pipeline pararia no passo Build.
- 🔴 SQL firewall **`AllowAllAzureIps` (0.0.0.0)** — abre o SQL para qualquer IP do Azure.
  `publicNetworkAccess: 'Enabled'`. Sem Private Endpoint/VNet. Frágil para dado público sensível.
- ⚠️ App Service **B1 / SQL S0** — sku de desenvolvimento, não de produção com carga real.
- ⚠️ **`deploy.yml` dispara em todo push para `main`** (sem gate/aprovação/ambiente protegido) —
  deploy automático em produção a cada merge. Arriscado para sistema que processa dinheiro público.
- ⚠️ Bicep **não cria o NfseSync Worker** (nenhum recurso para o Worker host; só o ApiHost).
- ⚠️ SQL admin login/senha como parâmetros/secrets do pipeline; não há AAD-only auth no SQL.

## 9. Observabilidade (Serilog / OpenTelemetry)

### O que existe
- ✅ **Serilog** fiado (`AddSerilog`, console, `UseSerilogRequestLogging`, `Enrich.FromLogContext`).
- ✅ **OpenTelemetry** wired: tracing + metrics com instrumentação AspNetCore + HttpClient,
  `ConfigureResource(AddService(...))`.

### Lacunas
- 🔴 **Nenhum exportador** configurado (sem OTLP/Azure Monitor) — os traces/metrics **não saem do
  processo**. O próprio comentário em `Program.cs` admite: "exportador será configurado na FASE 5".
  Hoje a telemetria é coletada e descartada.
- 🔴 **`TenantId`/`CorrelationId` NÃO enriquecem os logs** (constituição §11 exige "sempre com TenantId
  e CorrelationId"). Não há enricher/middleware que injete essas propriedades. Em incidente
  multi-tenant é impossível filtrar por ente.
- ⚠️ Serilog só escreve em Console (sem sink durável/estruturado para prod; depende do App Service).
- ⚠️ Sem health check de dependências (o `/health` é vazio — `AddHealthChecks()` sem checks de SQL/KV).

---

## Resumo: Real × Pendente

| Capacidade | Estado |
|---|---|
| Database-per-tenant + query filter + tenant interceptor | ✅ Real e sólido |
| Outbox (transacional + drain por tenant/módulo) | ✅ Real (sem retry/back-off/dead-letter) |
| JWT + RBAC negar-por-padrão + BCrypt + anti-timing | ✅ Real e maduro |
| Auditoria (gravação de mutações + visualizador) | 🟡 Real, mas TenantId zerado em INSERT + sem imutabilidade real + sem trilha de leitura LGPD |
| Provisionamento + gating de licença | ✅ Real (endpoint /admin/tenants sem RBAC = falha) |
| Pipeline MediatR (Validation/Logging) | 🟡 2 dos 4 behaviors faltam (Transaction, Idempotency) |
| Certificado A1 / Key Vault (uso em runtime) | 🔴 Stub/simulado — pacotes e Bicep prontos, integração NÃO fiada |
| Fitness functions / maintainability / spec-code | ✅ Reais e bons (mas não rodam hoje, build quebrado) |
| CI/CD + Bicep | 🟡 Estrutura correta; CI vermelho por #0; SQL aberto a todo Azure; deploy auto em main |
| Observabilidade (Serilog+OTel) | 🟡 Wired, mas sem exportador e sem TenantId/CorrelationId nos logs |
| **Build da solução** | 🔴 **NÃO COMPILA (Finanças/Empenho)** |

### Prioridades (ordem)
1. 🔴 Corrigir `EmpenhoRepository` (2 métodos) — destrava build, CI e os 575 testes. Bloqueador total.
2. 🔴 Corrigir ordem dos interceptors (Tenant ANTES de Audit/Outbox) ou carimbar TenantId no Audit a
   partir do `ITenantContext` — senão a trilha do TCE perde os INSERTs.
3. 🔴 Fiar Key Vault no `Program.cs` (`AddAzureKeyVault`) e implementar assinatura A1 real (X509 +
   XMLDSig) para SIAPC/PAD e eSocial — hoje é tudo stub.
4. 🔴 Exigir permissão RBAC no `/admin/tenants`; restringir SQL firewall; gate no deploy de prod.
5. 🟡 Imutabilidade real da AuditTrail (append-only/WORM/hash-chain) + trilha de leitura LGPD.
6. 🟡 Exportador OTel + enrichers TenantId/CorrelationId; retry/dead-letter no Outbox; behaviors
   Transaction/Idempotency no pipeline.
