# Convenções de Engenharia — Tensorroot.Gov

> **Regras ESTRITAS e inegociáveis.** Este documento tem precedência sobre qualquer
> convenção *default*. Toda contribuição DEVE aderir.
> Violar isolamento de módulo, multi-tenancy ou auditoria é **bug crítico**, não preferência.

---

## 0. Missão & Contexto

Tensorroot.Gov é um **ERP GovTech SaaS multi-tenant** para **Prefeituras (Poder Executivo)**
e **Câmaras de Vereadores (Poder Legislativo)** do Brasil. Opera em **produção**, processando
**dinheiro público** e **dados sensíveis** sob escrutínio do **Tribunal de Contas (foco TCE-RS
e padrão nacional)**. Piloto: **Município de Maximiliano de Almeida/RS**.

Requisitos absolutos: **segurança, isolamento multi-tenant, performance/memória, compliance e
auditabilidade**. Não há espaço para atalhos nessas dimensões.

---

## 1. Princípios Inegociáveis

1. **Spec-Driven (BDD-first).** Nenhuma feature sem cenário `Given/When/Then` aprovado **antes** do código.
2. **Isolamento de módulos.** Um módulo **nunca** referencia o interno de outro — apenas `*.Contracts`.
3. **Multi-tenant por padrão.** Toda entidade de negócio é `IMustHaveTenant`. Vazamento entre tenants = falha crítica.
4. **Auditoria imutável.** Toda mutação de estado gera trilha (antes/depois, quem, quando, IP).
5. **Segurança primeiro.** Segredos só no **Azure Key Vault**. Nenhuma credencial/certificado no repositório.
6. **Tipagem estrita.** NRT habilitado, **warnings = errors**, sem `null!` salvo justificado por comentário.
7. **Domínio rico.** Entidades protegem invariantes. **Proibido** modelo anêmico.
8. **Resiliência por padrão.** Toda I/O externa é idempotente, com timeout, retry e circuit breaker (Polly).

---

## 2. Arquitetura

- **Estilo:** Monolito Modular + **Clean Architecture** + **DDD Tático**.
- **Comunicação intra-processo:** **MediatR** — *Domain Events* in-process; *Integration Events* via **Outbox Pattern** (consistência transacional).
- **Regra de dependência por módulo (verificada por NetArchTest):**

  ```
  Domain  ←  Application  ←  Infrastructure
                  ↑
             Contracts  ← (referenciável por outros módulos)
  ```

  | Camada | Pode depender de | NUNCA depende de |
  |---|---|---|
  | **Domain** | SharedKernel | Application, Infrastructure, EF Core, ASP.NET |
  | **Application** | Domain, próprio Contracts, BuildingBlocks.Application, Contracts de OUTROS módulos | Infrastructure, EF Core |
  | **Infrastructure** | Application, BuildingBlocks.Infrastructure | Domain de outros módulos |
  | **Contracts** | SharedKernel | qualquer camada interna |

- **Cross-module:** comunicação **exclusivamente** via `Modules.X.Contracts` (Integration Events + DTOs). Sem chamadas diretas a Handlers/entidades de outro módulo.
- **ApiHost:** *Composition Root* (web). **Workers:** hosts de processo em segundo plano (ex.: `NfseSync`).

---

## 3. Estrutura da Solução

```
Tensorroot.Gov.sln
├── Directory.Build.props        # net8.0, C#12, NRT, warnings-as-errors, XML docs
├── Directory.Packages.props     # Central Package Management (versões centralizadas)
├── global.json                  # SDK .NET 8 (rollForward latestMajor)
├── src/
│   ├── SharedKernel/                         # primitivos: Entity, AggregateRoot, ValueObject, IMustHaveTenant, OutboxMessage, Cnpj, Cpf
│   ├── BuildingBlocks/
│   │   ├── ...Application/                    # pipeline behaviors MediatR (Validation, Logging, UnitOfWork, Idempotency)
│   │   └── ...Infrastructure/                # Outbox, AuditInterceptor, TenantInterceptor, base de DbContext
│   ├── Modules/<Modulo>/
│   │   ├── ...Domain/  ...Application/  ...Infrastructure/  ...Contracts/
│   │   └── README.md                         # spec do Bounded Context (linguagem ubíqua, mapa, BDD)
│   ├── ApiHost/                              # Composition Root + ativação modular por tenant
│   └── Workers/Tensorroot.Gov.Workers.NfseSync/   # sync diária NFS-e do ADN (integração passiva)
├── tests/Tensorroot.Gov.ArchitectureTests/  # Fitness Functions (NetArchTest)
└── docs/{adr,architecture,design-system}/
```

**Nomenclatura de projeto:** `Tensorroot.Gov.Modules.<Modulo>.<Camada>`. RootNamespace = nome do `.csproj`.

---

## 4. Bounded Contexts (11) — **ativação modular por tenant**

Cada módulo expõe um instalador `IModule` (registro de DI, endpoints e DbContext). O **ApiHost
ativa SOMENTE os módulos licenciados** para o tenant (tabela `TenantModule`). Uma prefeitura
pode operar **só Saúde**, **só Educação**, **só Legislativo**, etc. Requisição a módulo
não-licenciado → **404/403 auditado**.

| # | Módulo | Poder | Escopo |
|---|---|---|---|
| 1 | **Administracao** | Ambos | Compras, Licitações (Lei 14.133/2021), Fornecedores, Contratos, Aditivos, PNCP |
| 2 | **Financas** | Ambos | Orçamento (PPA/LDO/LOA), Empenho→Liquidação→Pagamento (Lei 4.320), Contabilidade PCASP/MCASP, Restos a Pagar |
| 3 | **Tributos** | Executivo | IPTU, ISS, ITBI, Taxas, Alvarás; **ingestão NFS-e via ADN (passiva)**; **Dívida Ativa, CDA, cobrança** |
| 4 | **RecursosHumanos** | Ambos | Folha, Cargos públicos, Ponto (Port. MTP 671/2021), **eSocial** |
| 5 | **Patrimonio** | Ambos | Bens, Tombamento, Depreciação (MCASP), Almoxarifado, **Frota** |
| 6 | **Protocolo** | Ambos | Processo administrativo eletrônico, assinatura (Lei 14.063/2020), GED, temporalidade (CONARQ) |
| 7 | **Saude** | Executivo | UBS, PEP, Regulação (SISREG), Farmácia (HÓRUS), Imunização (SI-PNI), Telessaúde |
| 8 | **Educacao** | Executivo | Escolas, Matrículas, Diário, Matriz/BNCC, Merenda (PNAE), Transporte (PNATE), EducaCenso/INEP |
| 9 | **AssistenciaSocial** | Executivo | SUAS, CRAS/CREAS, **CadÚnico**, Benefícios (BPC/PBF/eventuais), Prontuário SUAS |
| 10 | **Legislativo** | Legislativo | Sessões, Proposições, Comissões, Votação/Painel eletrônico |
| 11 | **Transparencia** | Ambos | Dados abertos (LAI), remessas **TCE-RS (SIAPC/PAD)**, SICONFI/MSC |

> **Executivo e Legislativo do mesmo município são tenants DISTINTOS** (CNPJs distintos).

---

## 5. Multi-Tenancy (Isolamento Extremo)

- Interface `IMustHaveTenant { Guid TenantId { get; } }` em **toda** raiz de agregado/entidade persistida.
- **Global Query Filter** por `TenantId` em **todo** DbContext (aplicado por reflexão na base).
- `ITenantContext` resolvido do **JWT** por requisição; `Workers` resolvem o tenant por iteração explícita.
- `SaveChangesInterceptor` carimba `TenantId` na inserção; gravação cross-tenant **lança exceção**.
- Proibido desabilitar o filtro global "para facilitar". Consultas administrativas usam mecanismo auditado dedicado.

---

## 6. Segurança & Compliance

- **AuthN:** JWT Bearer. **AuthZ:** *policy-based* + RBAC (`Usuário → Departamento → Roles`). Negar por padrão.
- **Auditoria:** `AuditSaveChangesInterceptor` grava *Audit Trail* (JSON antes/depois, usuário, IP, timestamp) — **imutável**, para o Tribunal de Contas.
- **Certificados A1 (.pfx):** **Azure Key Vault**, por tenant. Usados para **eSocial**, **remessas TCE-RS** e **assinatura de documentos no Protocolo**.
  - ⚠️ **NFS-e NÃO é assinada por nós** (nasce e é assinada no Ambiente Nacional — ver §8).
- **LGPD:** dados sensíveis (Saúde, Assistência Social, menores na Educação) com base legal explícita, minimização e **trilha de acesso** (quem leu o quê, quando, por quê).
- **Anti-SQLi:** sempre parametrizado/EF. **Proibido** SQL concatenado com entrada do usuário.

---

## 7. Padrões de Código (C# 12 / .NET 8)

- `record` / `readonly record struct` para **Value Objects** e DTOs; `sealed` por padrão.
- **Construtores privados + *factory methods*** ou `init`; entidade nasce válida (invariantes no construtor/factory).
- **NRT** ligado; **zero warnings**; `GenerateDocumentationFile` com XML docs nos tipos públicos do domínio.
- **`Span<T>` / `Memory<T>`** para fatiar XMLs pesados (remessas TCE, lotes do ADN) sem alocação desnecessária.
- Falhas de negócio → **exceções de domínio** ou `Result`; **não** usar exceções para fluxo trivial.
- `async/await` ponta a ponta; `CancellationToken` sempre propagado.
- **Sem números mágicos**: constantes nomeadas / `IOptions`. Regras fiscais e prazos são **parametrizáveis por tenant** (nunca *hardcoded*).

---

## 8. Integrações Governamentais

- **NFS-e Nacional (ADN) — integração PASSIVA.** O Worker `NfseSync` baixa **diariamente** os XMLs do
  **Ambiente de Dados Nacional / Receita Federal** para os CNPJs do tenant, deduplica por chave de acesso,
  persiste e alimenta o **painel fiscal (Tributos)** e a **Dívida Ativa**.
  **Nós NÃO emitimos nem assinamos NFS-e.** Emissão/recepção padrão ABRASF está **fora** do escopo.
- **eSocial / remessas TCE-RS (SIAPC/PAD) / SICONFI (MSC):** XML assinado (A1) + SOAP/REST, com **Polly**
  (retry + circuit breaker) e **Anti-Corruption Layer**. Eventos via **Outbox**.
- Toda integração externa: **idempotente, resiliente, atrás de ACL**, com mapeamento explícito de erros.
- Em dúvida sobre regra fiscal/legal ou layout de integração: **pesquisar a fonte oficial** (planalto,
  gov.br, STN, TCE-RS) e **confirmar layout/versão atual** antes de implementar — nunca inventar.

---

## 9. Persistência (EF Core 8)

- **Um `DbContext` por módulo**, **schema isolado** (`administracao`, `tributos`, `saude`, …).
- **Fluent API** (sem *data annotations* no domínio). **Migrations por módulo**.
- Tabela **Outbox** por contexto; interceptors de **auditoria** e **tenant** registrados na base.

---

## 10. CQRS / Mensageria

- **Commands** (mutação) e **Queries** (leitura) via MediatR. *Pipeline behaviors* na ordem:
  **Validation** (FluentValidation) → **Logging** → **Transaction/UnitOfWork** → **Idempotency**.
- **Domain Events** in-process; **Integration Events** publicados via **Outbox** (transacional com o estado).

---

## 11. Resiliência & Observabilidade

- **Polly** (`Microsoft.Extensions.Http.Resilience`) em toda chamada externa.
- **Serilog** (logs estruturados) + **OpenTelemetry** (traces/metrics), sempre com `TenantId` e `CorrelationId`.

---

## 12. Testes & Fitness Functions

- **Specs BDD (`.md`) antes do código.** xUnit + FluentAssertions.
- **NetArchTest** bloqueia: (a) `Domain → Infrastructure`; (b) módulo → interno de outro módulo;
  (c) qualquer referência cross-module fora de `*.Contracts`.
- Cobertura obrigatória das **regras de negócio críticas** (invariantes, prazos legais, isolamento de tenant).

---

## 13. UI/UX

- **Frontend:** **React** (SPA em `src/Web`) consumindo a API — fora da solução .NET.
- **Padrão visual ÚNICO e obrigatório:** **gov.br Design System** + **eMAG** + **WCAG 2.1 AA**.
- Regras detalhadas e *enforcement* em **`docs/design-system/`** (*Design System Constitution*). Nenhuma tela foge do padrão.

---

## 14. Convenções

- **Idioma:** linguagem ubíqua/domínio em **PT-BR** (identificadores **sem acento**: `DividaAtiva`, `Empenho`);
  termos técnicos em EN. Comentários e documentação em **PT-BR**.
- **Commits:** Conventional Commits. Branch por feature. PR com checklist (segurança, tenant, auditoria, testes).
