# Mapa Tópico → Código — Tensorroot.Gov

> Liga cada **tópico de estudo** do `docs/roadmap_team.html` (fundamentos .NET + GovTech +
> suporte/debug na era da IA) ao **arquivo/pasta real** onde ele vive no nosso código.
> Use como índice de navegação: "estudei a teoria X — onde ela está implementada AQUI?".
> Caminhos relativos à raiz do repositório. Atualizado em 2026-06-22.

Legenda de prioridade (do roadmap): **E** = essencial · **I** = importante · **A** = avançado.

---

## Parte I · Fundamentos do stack

| # | Tópico (id) | Pri | Onde vive no nosso código (arquivo/pasta real) |
|---|---|---|---|
| 1 | C# moderno: tipos, imutabilidade, NRT (`cs-tipos`) | E | `Directory.Build.props` (NRT + warnings-as-errors) · VOs em `src/SharedKernel/ValueObjects/` (`Cpf.cs`, `Cnpj.cs`) e primitivos em `src/SharedKernel/Primitives/` (`ValueObject.cs`, `Entity.cs`, `AggregateRoot.cs`) · `record` em Commands/Queries/DTOs sob cada `...Application/` e `...Contracts/` |
| 2 | Pattern matching avançado (`cs-pattern`) | I | Máquinas de estado de domínio: `src/Modules/Financas/...Domain/Empenhos/Empenho.cs`, `.../Liquidacoes/Liquidacao.cs`, `.../Pagamentos/OrdemDePagamento.cs`; status em `src/Modules/Cofre/...Domain/CertificadoStatus.cs` |
| 3 | ASP.NET Core: pipeline & middlewares (`aspnet-pipeline`) | E | `src/ApiHost/Program.cs` — ordem: `UseSerilogRequestLogging` (l.161) → `UseRateLimiter` (l.169) → `UseAuthentication` (l.170) → `UseAuthorization` (l.171) → gating de licença de módulo (l.185-186) |
| 4 | Resiliência de borda: Rate Limiting (`aspnet-rate`) | I | `src/ApiHost/Program.cs` — `AddRateLimiter` (l.99) com `PartitionedRateLimiter`/`FixedWindowRateLimiterOptions` (l.102-107); `app.UseRateLimiter()` (l.169) |
| 5 | DDD tático: Entities, VOs, Agregados (`ddd`) | E | Base em `src/SharedKernel/Primitives/` (`Entity`, `AggregateRoot`, `ValueObject`, `IHasDomainEvents`). Agregados ricos em cada `src/Modules/<X>/...Domain/` — ex.: `Financas/...Domain/Empenhos/Empenho.cs` (saldos + invariantes, factory privada) |
| 6 | CQRS com MediatR + pipeline behaviors (`cqrs`) | E | Handlers em cada `src/Modules/<X>/...Application/` · behaviors em `src/BuildingBlocks/Tensorroot.Gov.BuildingBlocks.Application/Behaviors/` (`ValidationBehavior.cs`, `LoggingBehavior.cs`); transação real via `ModuleUnitOfWork` (ver Parte II) |
| 7 | Outbox Pattern (`outbox`) | E | `src/BuildingBlocks/...Infrastructure/Outbox/ConvertDomainEventsToOutboxInterceptor.cs` (persiste) + `.../Outbox/OutboxPublisher.cs` (despacha via MediatR) + `src/ApiHost/Outbox/OutboxBackgroundService.cs` (coordenador por tenant) · contrato `src/SharedKernel/OutboxMessage.cs` |
| 8 | EF Core 8: Fluent API + Interceptors (`efcore`) | E | Base `src/BuildingBlocks/...Infrastructure/ModuleDbContext.cs` (aplica filtros + mapeia Outbox/AuditTrail) · `IEntityTypeConfiguration` por entidade em cada `...Infrastructure/Persistence/Configurations/` · interceptors em `.../Multitenancy/` e `.../Auditing/` |
| 9 | Tolerância a falhas: Polly (`polly`) | I | Padrão para integrações governamentais atrás de ACL + Outbox: `src/Integracoes/Tensorroot.Gov.Integracoes/` (ex.: `Cnab/`) e `src/Workers/Tensorroot.Gov.Workers.NfseSync/` |
| 10 | Cloud-native: Docker, Azure, Key Vault, Blob (`cloud`) | I | `infra/` (IaC/Bicep) · KEK no Key Vault: `src/Modules/Cofre/...Infrastructure/Cripto/ProvedorKekKeyVault.cs` + `ProvedorKekConfig.cs` · segredos de plataforma consumidos em `src/ApiHost/Program.cs` |
| 11 | Observabilidade: Serilog + App Insights + OTel (`obs`) | I | `src/ApiHost/Program.cs` — `AddOpenTelemetry` (l.37) + Serilog; `UseSerilogRequestLogging` (l.161) |
| 12 | CI/CD: GitHub Actions + migrações idempotentes (`cicd`) | I | `.github/workflows/ci.yml` (testes + fitness functions) e `.github/workflows/deploy.yml` · migrações por módulo em cada `...Infrastructure/Migrations/` |

## Parte II · Como o Tensorroot.Gov faz (e por quê)

| # | Tópico (id) | Pri | Onde vive no nosso código (arquivo/pasta real) |
|---|---|---|---|
| 13 | Multi-tenant: BANCO DEDICADO por tenant (`adr-dbpertenant`) | E | `src/Platform/Tensorroot.Gov.Platform/Tenancy/TenantConnectionResolver.cs` (impl) + contrato `src/BuildingBlocks/...Application/Abstractions/ITenantConnectionResolver.cs` · carimbo `src/BuildingBlocks/...Infrastructure/Multitenancy/TenantSaveChangesInterceptor.cs` · banco de controle `src/Platform/Tensorroot.Gov.Platform/Persistence/PlatformDbContext.cs` · Global Query Filter em `ModuleDbContext.cs` |
| 14 | ScopeDbContextHolder / IUnitOfWork — bug sutil (`adr-uow`) | E | `src/BuildingBlocks/...Infrastructure/ScopeDbContextHolder.cs` + `.../ModuleUnitOfWork.cs` (o DbContext ativo se registra no escopo; o UoW confirma esse — corrigiu o bug de 12 módulos registrando IUnitOfWork) |
| 15 | Autorização: RBAC dinâmico + ABAC organizacional (`adr-authz`) | E | `src/BuildingBlocks/...Infrastructure/Authorization/` (`PermissionAuthorization.cs`, `AuthorizationExtensions.cs` — provedor de políticas `perm:<escopo>`) · modelo organizacional em `src/Modules/Identidade/...Domain/Unidades/UnidadeOrganizacional.cs`, `.../Usuarios/AtribuicaoDePapel.cs`, `EscopoEfetivo.cs`, `AutorizacaoDeConcessao.cs` (regra I4) · filtro de UO em `ModuleDbContext.ApplyTenantAndUnidadeQueryFilters` · doc `docs/architecture/autorizacao/MODELO-AUTORIZACAO-ORGANIZACIONAL.md` |
| 16 | Contabilidade PCASP → MSC → TCE/SICONFI (`adr-contabilidade`) | E | `src/Modules/Financas/...Domain/Contabilidade/` (`Lancamentos/LancamentoContabil.cs`, `PlanoDeContas/ContaContabil.cs`, `Events/ContabilidadeDomainEvents.cs`) · motor + handlers de partida dobrada AUTOMÁTICA em `...Application/Contabilidade/Motor/MotorContabil.cs` e `...Application/Contabilidade/Handlers/Contabilizar{Empenho,Liquidacao,Pagamento,Receita}Handler.cs` + `ProjetarBalanceteHandler.cs` · MSC em `...Application/Contabilidade/Msc/` · specs `docs/architecture/contabilidade-pcasp-tce.md`, `docs/architecture/specs-oficiais/` |
| 17 | NFS-e é PASSIVA — não emitimos (`adr-nfse`) | I | `src/Workers/Tensorroot.Gov.Workers.NfseSync/` (`NfseSyncWorker.cs`, `WorkerComponents.cs`, `Program.cs`) — baixa do ADN, deduplica por chave, alimenta Tributos/Dívida Ativa · `CLAUDE.md §8` |
| 18 | Certificado A1 no banco do tenant, cifrado (`adr-a1`) | I | Módulo `src/Modules/Cofre/` — `...Domain/CertificadoA1Cofre.cs`, `MaterialCifrado.cs`, `AssinaturaAuditLog.cs` · envelope encryption em `...Infrastructure/Cripto/AesGcmEnvelope.cs`, KEK em `ProvedorKekKeyVault.cs` · assinatura `...Infrastructure/ServicoAssinaturaDigital.cs` + `Cripto/AssinadorCms.cs` · custódia `ServicoCustodiaCertificado.cs` |
| 19 | Fábrica Rules-as-Code + Fitness Functions (`adr-rulesascode`) | E | Specs `src/Modules/<X>/rules/*.rules.md` (ex.: `Financas/rules/Contabilidade.rules.md`, `Empenho.rules.md`) · testes em `tests/Tensorroot.Gov.ArchitectureTests/` (`FitnessFunctions.cs`, `SpecCodeConsistency.cs`, `MaintainabilityTests.cs`) |
| 20 | Frontend: React + Vite + gov.br DS (`adr-frontend`) | E | `src/Web/src/modules/registry.ts` (code-splitting por módulo) · `src/Web/src/auth/` (`jwt.ts`, `AuthProvider.tsx`, `ProtectedRoute.tsx`, `PermissionRoute.tsx`) + `src/Web/src/auth/Can.tsx` (gating) · cliente `src/Web/src/api/http.ts` · padrão por entidade em `src/Web/src/modules/<X>/` (`<ent>.api.ts` + `<Ent>ListPage.tsx` + `<Ent>FormModal.tsx` + `.test.tsx`) |
| 21 | Auditoria imutável para o TCE (`adr-auditoria`) | E | `src/BuildingBlocks/...Infrastructure/Auditing/AuditSaveChangesInterceptor.cs` + entidade `.../Auditing/AuditTrail.cs` (gravada no banco do tenant) · ordem Tenant→Audit registrada na base `ModuleDbContext.cs`; visualizador admin em `src/ApiHost/Admin/` |

## Parte III · Suporte, bugs e testes/debug na era da IA

| # | Tópico (id) | Pri | Onde vive no nosso código (arquivo/pasta real) |
|---|---|---|---|
| 22 | Verifique, não confie (no verde nem na IA) (`ia-verifique`) | E | Disciplina de runtime, não um arquivo: provisionamento dev `src/BuildingBlocks/...Infrastructure/Multitenancy/SchemaProvisioner.cs` (SQLite) + subir `src/ApiHost/Program.cs` e conferir o dado · testes de regressão dos bugs reais: `tests/Tensorroot.Gov.Modules.Tributos.Tests/OutboxTests.cs`, `OutboxPublisherTests.cs` |
| 23 | Correção de bug disciplinada — TDD de bug (`ia-tddbug`) | E | Invariantes virando regressão: `tests/Tensorroot.Gov.Modules.Financas.Tests/` (ΣD=ΣC, não pagar acima do liquidado) · `tests/Tensorroot.Gov.Modules.Cofre.Tests/AuditTrailVazaMaterialTests.cs` |
| 24 | IA como copiloto de manutenção, com guarda-corpo (`ia-copiloto`) | E | Fábrica de geração a partir de specs: `src/Modules/<X>/rules/*.rules.md` + `tools/workflows/` · guarda-corpo = `tests/Tensorroot.Gov.ArchitectureTests/` + suítes em `tests/` |
| 25 | Fitness functions como rede de segurança (`ia-fitness`) | I | `tests/Tensorroot.Gov.ArchitectureTests/FitnessFunctions.cs` (regras de dependência/isolamento), `SpecCodeConsistency.cs` (código⇄regras), `MaintainabilityTests.cs` (gate de tamanho de arquivo) |
| 26 | Debug guiado por observabilidade (`ia-observabilidade`) | I | Serilog + CorrelationId + OTel wired em `src/ApiHost/Program.cs` (l.37, l.161); sempre logar com TenantId/CorrelationId · EF Core query logging em dev |
| 27 | Ferramentas de teste/debug (.NET e front) (`ia-ferramentas`) | I | Back: suítes `tests/*.Tests/` (xUnit) rodadas com `dotnet test --filter` · Front: Vitest/Testing Library — ex.: `src/Web/src/auth/Can.test.tsx`, `PermissionRoute.test.tsx`, `src/Web/src/modules/<X>/<Ent>ListPage.test.tsx`; pasta `src/Web/src/test/` |
| 28 | Mudanças e migrações seguras (`ia-migracoes`) | I | Migrações por módulo em cada `...Infrastructure/Migrations/` (ex.: `src/Modules/Cofre/...Infrastructure/Migrations/`) · em dev `SchemaProvisioner.cs` cria/atualiza schema · script idempotente no `.github/workflows/ci.yml` |
| 29 | Processo de suporte e incidentes (`ia-suporte`) | I | Processo/documentação, não código: `docs/governanca/`, `docs/progresso/`, `docs/RUNBOOK.md` (runbook, severidade, post-mortem → vira teste de regressão em `tests/`) |

---

## Tópicos sem âncora de código direta (processo/disciplina)

Os tópicos #22 (verifique), #29 (suporte/incidentes) e parte do #28 são **processos**, não
arquivos; mapeamos a evidência mais próxima (testes de regressão dos bugs reais, docs de
governança, provisionamento de schema). Todos os demais têm âncora de código verificada no repo.

**Total: 29 tópicos mapeados** (12 na Parte I + 9 na Parte II + 8 na Parte III).
