---
modulo: Tributos
agregado: Lancamento
contexto: Tributos (Gestão do Crédito Tributário Municipal)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "CTN art. 142 (lançamento — constituição do crédito tributário)"
  - "CTN art. 139 a 141 (crédito tributário; obrigação principal)"
  - "CTN art. 150 (lançamento por homologação)"
  - "CTN art. 201 (constituição da Dívida Ativa após vencimento e não pagamento)"
  - "Lei 4.320/64 (normas gerais de direito financeiro; receita pública e seu lançamento)"
---

# Lancamento (Crédito Tributário) — Regras Normativas

> **Fonte da verdade.** Este `*.rules.md` é **normativo e versionado**. O código de domínio,
> aplicação, persistência e testes do agregado `Lancamento` é **gerado e mantido a partir
> daqui**. Bug, ajuste ou nova regra ⇒ edita-se **este arquivo**; o código é consequência.

---

## 1. Linguagem Ubíqua

Os identificadores (sem acento) são **VINCULANTES**: o gerador de código DEVE usá-los.

| Termo (identificador-no-código) | Definição |
|---|---|
| **Lancamento** | Ato administrativo que constitui o crédito tributário (CTN art. 142), tornando a obrigação líquida, certa e exigível contra o contribuinte. Raiz de agregado. |
| **LancamentoId** | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| **TenantId** | Ente público (prefeitura) dono do registro — isolamento multi-tenant. |
| **ContribuinteId** | Identidade forte do contribuinte devedor (referência por Id; outro agregado). |
| **TipoTributo** | Espécie tributária lançada (IPTU, ISS, ITBI, Taxa, Contribuição de Melhoria, COSIP). |
| **Competencia** | Competência fiscal (ano e mês) a que se refere o lançamento. Value Object. |
| **ValorPrincipal** | Montante principal do crédito tributário, em Reais. Value Object `ValorMonetario`. |
| **Vencimento** | Data-limite para pagamento sem inscrição em Dívida Ativa (`DateOnly`). |
| **Situacao** | Estado do lançamento (`Aberto`, `Pago`, `InscritoEmDividaAtiva`, `Cancelado`). |
| **Lancar** | Constituir o crédito tributário (factory) — nasce em `Aberto`. |
| **RegistrarPagamento** | Registrar a quitação do lançamento (passa a `Pago`). |
| **InscreverEmDividaAtiva** | Inscrever em Dívida Ativa quando vencido e em aberto (passa a `InscritoEmDividaAtiva`). |
| **Cancelar** | Cancelar o lançamento (passa a `Cancelado`), vedado se já pago. |
| **CreditoTributarioLancado** | Evento de domínio emitido na constituição do crédito. |
| **PagamentoRegistrado** | Evento de domínio emitido na quitação. |
| **LancamentoInscritoEmDividaAtiva** | Evento de domínio emitido na inscrição em Dívida Ativa. |

---

## 2. Modelo

- **Identidade:** `LancamentoId` — `readonly record struct LancamentoId(Guid Value)`; fábrica `LancamentoId.New()`.
- **Raiz de agregado:** `Lancamento : AggregateRoot<LancamentoId>, IMustHaveTenant` (`sealed`).
- **Construção:** construtor privado sem parâmetros (EF) + construtor privado parametrizado + **factory** `Lancar(...)`. A entidade **nasce válida** e em `Aberto`.

### 2.1 Propriedades

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `LancamentoId` | Identidade do agregado. |
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. `IMustHaveTenant`. |
| `ContribuinteId` | `ContribuinteId` | Contribuinte devedor. |
| `TipoTributo` | `TipoTributo` (enum) | Espécie tributária. |
| `Competencia` | `Competencia` (VO) | Competência fiscal (ano/mês). |
| `ValorPrincipal` | `ValorMonetario` (VO) | Valor principal lançado. |
| `Vencimento` | `DateOnly` | Data de vencimento. |
| `Situacao` | `SituacaoLancamento` (enum) | Situação atual. |

Todos os *setters* são `private set`. As mutações ocorrem **apenas** pelos métodos de comportamento (§5), preservando invariantes.

### 2.2 Value Objects

**`Competencia` (`ValueObject`):**
- Propriedades: `Ano : int` (>= 1900), `Mes : int` (1..12).
- Fábrica: `Competencia.De(int ano, int mes)` — lança `ArgumentOutOfRangeException` se `ano < 1900`, `mes < 1` ou `mes > 12`.
- Igualdade por valor: `(Ano, Mes)`.
- `ToString()` → `"MM/AAAA"` (cultura invariante, ex.: `03/2026`).

**`ValorMonetario` (`ValueObject`):**
- Propriedade: `Valor : decimal` (Reais, **não-negativo**, arredondado a **2 casas**, `MidpointRounding.AwayFromZero`).
- Constante: `ValorMonetario.Zero`.
- Fábrica: `ValorMonetario.De(decimal valor)` — lança `ArgumentOutOfRangeException` se `valor < 0`.
- Operações: `Somar(ValorMonetario)`, `AplicarPercentual(decimal)` (percentual não-negativo).
- Igualdade por valor: `(Valor)`.
- `ToString()` → `"0.00"` (cultura invariante).

### 2.3 Enums

**`TipoTributo`** (espécie tributária):

| Valor | Nome | Significado |
|---|---|---|
| 1 | `Iptu` | Imposto Predial e Territorial Urbano. |
| 2 | `Iss` | Imposto Sobre Serviços de Qualquer Natureza. |
| 3 | `Itbi` | Imposto sobre Transmissão de Bens Imóveis. |
| 4 | `Taxa` | Taxa (poder de polícia ou serviço). |
| 5 | `ContribuicaoMelhoria` | Contribuição de Melhoria. |
| 6 | `Cosip` | Contribuição para Custeio da Iluminação Pública. |

**`SituacaoLancamento`** (estado):

| Valor | Nome | Significado |
|---|---|---|
| 1 | `Aberto` | Em aberto (exigível, ainda não pago). Estado inicial. |
| 2 | `Pago` | Pago/quitado (estado terminal). |
| 3 | `InscritoEmDividaAtiva` | Inscrito em Dívida Ativa (estado terminal neste agregado). |
| 4 | `Cancelado` | Cancelado (estado terminal). |

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Todo `Lancamento` pertence a exatamente um tenant (`IMustHaveTenant`); `TenantId` é imutável após a criação.
- **I-2.** `Competencia` é obrigatória e não pode ser `null` na constituição (`ArgumentNullException.ThrowIfNull`).
- **I-3.** `ValorPrincipal` é obrigatório e não pode ser `null` na constituição (`ArgumentNullException.ThrowIfNull`).
- **I-4.** `Competencia` exige `Ano >= 1900` e `Mes` entre 1 e 12 (CTN art. 142 — competência líquida e certa).
- **I-5.** `ValorPrincipal` é **não-negativo** e arredondado a 2 casas decimais (`ValorMonetario`).
- **I-6.** Ao ser constituído (`Lancar`), o lançamento nasce na situação `Aberto` e emite `CreditoTributarioLancado` (CTN art. 142).
- **I-7.** Só é possível **registrar pagamento** de um lançamento na situação `Aberto`; caso contrário lança `InvalidOperationException`.
- **I-8.** Só é possível **inscrever em Dívida Ativa** um lançamento na situação `Aberto`; caso contrário lança `InvalidOperationException`.
- **I-9.** A inscrição em Dívida Ativa exige lançamento **vencido**: `hoje > Vencimento`. Se `hoje <= Vencimento`, lança `InvalidOperationException` ("ainda não está vencido") — CTN art. 201.
- **I-10.** É **vedado cancelar** um lançamento já `Pago`; lança `InvalidOperationException`. Lançamentos em qualquer outra situação podem ser cancelados.
- **I-11.** `Pago`, `InscritoEmDividaAtiva` e `Cancelado` são estados **terminais** neste agregado: não admitem novas transições de negócio.

---

## 4. Máquina de Estados

| Origem | Comando/Método | Destino | Guarda | Evento emitido |
|---|---|---|---|---|
| _(inexistente)_ | `Lancar` | `Aberto` | `Competencia` e `ValorPrincipal` não nulos; competência válida | `CreditoTributarioLancado` |
| `Aberto` | `RegistrarPagamento` | `Pago` | situação == `Aberto` | `PagamentoRegistrado` |
| `Aberto` | `InscreverEmDividaAtiva(hoje)` | `InscritoEmDividaAtiva` | situação == `Aberto` **e** `hoje > Vencimento` | `LancamentoInscritoEmDividaAtiva` |
| `Aberto` | `Cancelar` | `Cancelado` | situação != `Pago` | _(nenhum)_ |
| `InscritoEmDividaAtiva` | `Cancelar` | `Cancelado` | situação != `Pago` | _(nenhum)_ |
| `Cancelado` | `Cancelar` | `Cancelado` | situação != `Pago` (idempotente) | _(nenhum)_ |
| `Pago` / `InscritoEmDividaAtiva` / `Cancelado` | `RegistrarPagamento` | — | bloqueado (situação != `Aberto`) → `InvalidOperationException` | — |
| `Pago` / `InscritoEmDividaAtiva` / `Cancelado` | `InscreverEmDividaAtiva` | — | bloqueado (situação != `Aberto`) → `InvalidOperationException` | — |
| `Pago` | `Cancelar` | — | bloqueado (situação == `Pago`) → `InvalidOperationException` | — |

> Observação: `Cancelar` a partir de `Cancelado` é tecnicamente idempotente (permanece `Cancelado`, sem evento), pois a única guarda é "não estar `Pago`".

---

## 5. Comandos (escrita)

### 5.1 `LancarCredito` — Constituir o crédito tributário (CTN art. 142)

- **Comando (DTO):** `LancarCreditoCommand : ICommand<Guid>`
  - `ContribuinteId : Guid`
  - `TipoTributo : TipoTributo`
  - `Ano : int`
  - `Mes : int`
  - `ValorPrincipal : decimal`
  - `Vencimento : DateOnly`
- **Pré-condições:**
  - Comando válido segundo `LancarCreditoValidator` (§8).
  - Existe um `Contribuinte` com o `ContribuinteId` informado **no tenant atual** (`ITenantContext.TenantId`, respeitando o Global Query Filter).
- **Efeito:**
  - Resolve o `Contribuinte` via `IContribuinteRepository.ObterPorIdAsync`; se ausente, lança `InvalidOperationException("Contribuinte não encontrado.")`.
  - Cria o agregado via `Lancamento.Lancar(tenant.TenantId, contribuinte.Id, TipoTributo, Competencia.De(Ano, Mes), ValorMonetario.De(ValorPrincipal), Vencimento)`.
  - `ILancamentoRepository.Adicionar(lancamento)`; persiste via `IUnitOfWork.SaveChangesAsync`.
- **Pós-condições:**
  - Novo `Lancamento` persistido na situação `Aberto`, carimbado com `TenantId` do contexto.
  - Evento de domínio `CreditoTributarioLancado(LancamentoId, ContribuinteId, ValorPrincipal)` registrado.
  - Retorna `Guid` = `lancamento.Id.Value`.
- **Exceções:**
  - `ValidationException` (pipeline FluentValidation) para comando inválido.
  - `InvalidOperationException("Contribuinte não encontrado.")` se o contribuinte não existir no tenant.
  - `ArgumentOutOfRangeException` (em `Competencia.De` / `ValorMonetario.De`) se valores escaparem das fronteiras dos VOs.
- **Evento:** `CreditoTributarioLancado` (domínio, in-process via MediatR).
- **Handler:** `LancarCreditoHandler(IContribuinteRepository, ILancamentoRepository, IUnitOfWork, ITenantContext)`.

### 5.2 Métodos de comportamento do agregado (invocados por comandos futuros)

> Os métodos abaixo já existem no domínio (`Lancamento.cs`) e definem a máquina de estados.
> Ainda **não** há `*Command/*Handler` dedicados além de `LancarCredito`; quando criados,
> devem reusar exatamente estes métodos (não duplicar regra).

- **`RegistrarPagamento()`** — Pré: situação == `Aberto`. Pós: `Pago` + evento `PagamentoRegistrado(Id)`. Exceção: `InvalidOperationException` se não estiver `Aberto`.
- **`InscreverEmDividaAtiva(DateOnly hoje)`** — Pré: situação == `Aberto` **e** `hoje > Vencimento`. Pós: `InscritoEmDividaAtiva` + evento `LancamentoInscritoEmDividaAtiva(Id, ContribuinteId)`. Exceções: `InvalidOperationException` se não estiver `Aberto` ou se ainda não vencido.
- **`Cancelar()`** — Pré: situação != `Pago`. Pós: `Cancelado` (sem evento). Exceção: `InvalidOperationException` se já `Pago`.

---

## 6. Consultas (leitura)

Nenhuma consulta (`*Query`/`*Handler`) está definida para o agregado `Lancamento` nesta versão.

> Quando criadas, DEVEM ser **tenant-scoped** (Global Query Filter por `TenantId`), projetar para DTO
> dedicado (sem expor a entidade) e respeitar a paginação padrão do módulo. O repositório
> `ILancamentoRepository` hoje expõe `ObterPorIdAsync(LancamentoId, CancellationToken)` para uso interno.

---

## 7. Eventos

### 7.1 Domínio (in-process, MediatR — assembly `...Tributos.Domain`)

| Evento | Payload | Emitido em |
|---|---|---|
| `CreditoTributarioLancado` | `(LancamentoId LancamentoId, ContribuinteId ContribuinteId, decimal ValorPrincipal)` | `Lancar` (constituição). |
| `PagamentoRegistrado` | `(LancamentoId LancamentoId)` | `RegistrarPagamento`. |
| `LancamentoInscritoEmDividaAtiva` | `(LancamentoId LancamentoId, ContribuinteId ContribuinteId)` | `InscreverEmDividaAtiva`. |

Todos implementam `IDomainEvent` e são `sealed record`.

### 7.2 Integração (via `Modules.Tributos.Contracts`, Outbox)

Nenhum Integration Event é **publicado** nem **consumido** por este agregado nesta versão.

> Nota de design: `LancamentoInscritoEmDividaAtiva` é um evento de **domínio** consumido in-process
> pelo próprio módulo Tributos para originar a `DividaAtiva`. Caso, no futuro, a inscrição deva
> notificar outros módulos, criar um Integration Event dedicado em `*.Contracts` (não reutilizar o
> evento de domínio cross-module).

---

## 8. Validações (FluentValidation)

`LancarCreditoValidator : AbstractValidator<LancarCreditoCommand>`:

| Campo | Regra | Mensagem (padrão FluentValidation) |
|---|---|---|
| `ContribuinteId` | `NotEmpty()` | "'Contribuinte Id' must not be empty." |
| `TipoTributo` | `IsInEnum()` | "'Tipo Tributo' has a range of values which does not include ..." |
| `Ano` | `GreaterThanOrEqualTo(1900)` | "'Ano' must be greater than or equal to '1900'." |
| `Mes` | `InclusiveBetween(1, 12)` | "'Mes' must be between 1 and 12." |
| `ValorPrincipal` | `GreaterThan(0m)` | "'Valor Principal' must be greater than '0'." |

> As fronteiras de `Ano`/`Mes`/`ValorPrincipal` são **redundantemente** garantidas pelos Value Objects
> `Competencia`/`ValorMonetario` (defesa em profundidade). O validador rejeita cedo no pipeline; os VOs
> rejeitam no domínio. `ValorPrincipal` no comando é **estritamente positivo** (`> 0`), embora
> `ValorMonetario` aceite zero — o invariante de negócio do lançamento exige montante positivo.

---

## 9. Persistência

EF Core 8, **um `DbContext` por módulo** com **schema isolado**.

- **DbContext:** `TributosDbContext` — `Schema => "tributos"`.
- **Tabela:** `tributos.Lancamentos`.
- **Configuração:** `LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>` (Fluent API; sem data annotations no domínio).
- **Chave primária:** `Id` (`HasKey`), com conversor `LancamentoId <-> Guid`, `ValueGeneratedNever()`.

### 9.1 Colunas e conversores

| Coluna | Tipo de coluna | Conversor / mapeamento |
|---|---|---|
| `Id` | `uniqueidentifier` | `id => id.Value` / `value => new LancamentoId(value)`; gerado nunca. |
| `TenantId` | `uniqueidentifier` | primitivo (carimbado pelo TenantInterceptor). |
| `ContribuinteId` | `uniqueidentifier` | `id => id.Value` / `value => new ContribuinteId(value)`. |
| `TipoTributo` | `nvarchar(30)` | `HasConversion<string>()` (enum como string). |
| `Competencia` | `int` | `competencia => (Ano*100)+Mes` / `valor => Competencia.De(valor/100, valor%100)`. |
| `ValorPrincipal` | `decimal(18,2)` | `valor => valor.Valor` / `valor => ValorMonetario.De(valor)`. |
| `Situacao` | `nvarchar(30)` | `HasConversion<string>()` (enum como string). |
| `Vencimento` | `date` | `DateOnly` (mapeamento nativo EF Core 8). |

### 9.2 Índices

| Índice | Colunas | Único | Finalidade |
|---|---|---|---|
| `IX_Lancamentos_TenantId_ContribuinteId` | `(TenantId, ContribuinteId)` | Não | Consulta de lançamentos por contribuinte dentro do tenant. |

> Não há índice único de negócio nesta versão (um contribuinte pode ter múltiplos lançamentos por
> competência/tributo — ex.: parcelas/refaturamentos).

### 9.3 Outbox / Auditoria

`TributosDbContext` herda a base com tabela **Outbox**, `AuditSaveChangesInterceptor` e `TenantInterceptor` (carimbo de `TenantId` na inserção; gravação cross-tenant lança exceção).

---

## 10. Segurança, Tenant e Auditoria

- **`IMustHaveTenant`:** `Lancamento` implementa; `TenantId` é obrigatório e carimbado na inserção pelo `TenantInterceptor`. **Global Query Filter** por `TenantId` aplicado no DbContext — toda leitura é automaticamente restrita ao tenant da requisição.
- **`ITenantContext`:** resolvido do JWT (ApiHost) ou por iteração explícita (Workers). O handler usa `tenant.TenantId` para constituir o lançamento; jamais aceita `TenantId` vindo do cliente.
- **RBAC / AuthZ:** *policy-based*, negar por padrão. Constituir/alterar lançamento exige papel fiscal autorizado (`Usuário → Departamento → Roles`). Requisição a módulo não-licenciado para o tenant → 404/403 auditado.
- **Auditoria imutável:** toda mutação (constituição, pagamento, inscrição, cancelamento) gera trilha (JSON antes/depois, usuário, IP, timestamp) via `AuditSaveChangesInterceptor`, para o Tribunal de Contas.
- **LGPD:** o agregado referencia o contribuinte por `ContribuinteId` (não duplica dados pessoais). Dados pessoais residem no agregado `Contribuinte`; o acesso é minimizado e trilhado.
- **Anti-SQLi:** acesso exclusivamente via EF parametrizado; proibido SQL concatenado com entrada do usuário.

---

## 11. Integrações Governamentais

Este agregado **não** realiza chamada externa síncrona própria.

- **Origem dos lançamentos (ISS):** o painel fiscal alimentado pela **ingestão passiva de NFS-e** (Worker `NfseSync` / Ambiente de Dados Nacional — ADN/Receita Federal) pode subsidiar a constituição de lançamentos de ISS. Essa ingestão é **idempotente** (dedup por chave de acesso) e está fora do agregado.
- **Jusante (Dívida Ativa):** a inscrição em Dívida Ativa (`InscreverEmDividaAtiva`) origina, no próprio módulo, o agregado `DividaAtiva`, de onde partem CDA, protesto e execução fiscal (CTN art. 201; Lei 6.830/80) — tratados nas regras de `DividaAtiva`.
- **Resiliência (Polly), ACL e idempotência** aplicam-se às integrações de origem/destino, não a este agregado diretamente.

---

## 12. Cenários BDD

Cada cenário Given/When/Then vira um teste de integração.

**Cenário 1 — Constituição do crédito (caminho feliz).**
- **Dado** um contribuinte existente no tenant
- **Quando** executo `LancarCredito` com `TipoTributo=Iptu`, `Ano=2026`, `Mes=3`, `ValorPrincipal=1234,56`, `Vencimento=2026-04-10`
- **Então** é criado um `Lancamento` na situação `Aberto`, carimbado com o `TenantId` do contexto, e é emitido `CreditoTributarioLancado(LancamentoId, ContribuinteId, 1234,56)`; o comando retorna o `Guid` do lançamento.

**Cenário 2 — Contribuinte inexistente.**
- **Dado** um `ContribuinteId` que não existe no tenant
- **Quando** executo `LancarCredito`
- **Então** ocorre `InvalidOperationException("Contribuinte não encontrado.")` e nada é persistido.

**Cenário 3 — Comando inválido (valor não positivo).**
- **Dado** um comando com `ValorPrincipal = 0`
- **Quando** ele entra no pipeline de validação
- **Então** ocorre `ValidationException` ("must be greater than '0'") e o handler não é executado.

**Cenário 4 — Comando inválido (mês fora do intervalo).**
- **Dado** um comando com `Mes = 13`
- **Quando** ele entra no pipeline de validação
- **Então** ocorre `ValidationException` ("must be between 1 and 12").

**Cenário 5 — Registro de pagamento.**
- **Dado** um lançamento na situação `Aberto`
- **Quando** chamo `RegistrarPagamento()`
- **Então** a situação passa a `Pago` e é emitido `PagamentoRegistrado(Id)`.

**Cenário 6 — Pagamento bloqueado.**
- **Dado** um lançamento já `Pago` (ou `InscritoEmDividaAtiva`/`Cancelado`)
- **Quando** chamo `RegistrarPagamento()`
- **Então** ocorre `InvalidOperationException` informando a situação atual.

**Cenário 7 — Inscrição em Dívida Ativa (vencido).**
- **Dado** um lançamento `Aberto` com `Vencimento = 2026-04-10`
- **Quando** chamo `InscreverEmDividaAtiva(hoje = 2026-04-11)`
- **Então** a situação passa a `InscritoEmDividaAtiva` e é emitido `LancamentoInscritoEmDividaAtiva(Id, ContribuinteId)`.

**Cenário 8 — Inscrição bloqueada (ainda não vencido).**
- **Dado** um lançamento `Aberto` com `Vencimento = 2026-04-10`
- **Quando** chamo `InscreverEmDividaAtiva(hoje = 2026-04-10)`
- **Então** ocorre `InvalidOperationException("O lançamento ainda não está vencido.")`.

**Cenário 9 — Cancelamento permitido.**
- **Dado** um lançamento `Aberto` (ou `InscritoEmDividaAtiva`)
- **Quando** chamo `Cancelar()`
- **Então** a situação passa a `Cancelado` (sem evento).

**Cenário 10 — Cancelamento vedado.**
- **Dado** um lançamento `Pago`
- **Quando** chamo `Cancelar()`
- **Então** ocorre `InvalidOperationException("Não é possível cancelar um lançamento já pago.")`.

---

## 13. Casos de Borda

Cada item vira um teste.

- **CB-1.** `Competencia.De(1899, 6)` → `ArgumentOutOfRangeException` (ano < 1900).
- **CB-2.** `Competencia.De(2026, 0)` e `Competencia.De(2026, 13)` → `ArgumentOutOfRangeException` (mês fora de 1..12).
- **CB-3.** `ValorMonetario.De(-0,01)` → `ArgumentOutOfRangeException` (negativo).
- **CB-4.** Arredondamento: `ValorMonetario.De(1,005)` → `1,01` (`AwayFromZero`); persiste como `decimal(18,2)`.
- **CB-5.** `Lancar` com `competencia == null` ou `valorPrincipal == null` → `ArgumentNullException`.
- **CB-6.** `InscreverEmDividaAtiva` exatamente no dia do vencimento (`hoje == Vencimento`) → bloqueado (exige `hoje > Vencimento`).
- **CB-7.** `Cancelar` sobre lançamento já `Cancelado` → permanece `Cancelado` (idempotente, sem erro).
- **CB-8.** `RegistrarPagamento`/`InscreverEmDividaAtiva` sobre lançamento `Cancelado` → `InvalidOperationException` (situação != `Aberto`).
- **CB-9.** `ValorPrincipal = 0` aceito por `ValorMonetario`, mas **rejeitado** pelo validador do comando (`GreaterThan(0)`) — divergência intencional documentada (§8).
- **CB-10.** Mapeamento de `Competencia` ⇄ `int`: round-trip `(2026, 3)` → `202603` → `Competencia.De(2026, 3)` (ano = `valor/100`, mês = `valor%100`).
- **CB-11.** Isolamento de tenant: tentar obter/gravar lançamento de outro tenant é barrado pelo Global Query Filter e pelo TenantInterceptor (não vaza entre tenants).
- **CB-12.** Enum persistido como string (`nvarchar(30)`): valores novos de `TipoTributo`/`SituacaoLancamento` não colidem com inteiros legados.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Retrofit inicial do agregado `Lancamento` (módulo Tributos) para Rules-as-Code, fiel ao código existente (`Lancamento.cs`, `LancarCredito.cs`, VOs `Competencia`/`ValorMonetario`, `TributosDomainEvents.cs`, `LancamentoConfiguration.cs`). |

<!-- manifest
commands: LancarCredito
queries: 
domainEvents: CreditoTributarioLancado, PagamentoRegistrado, LancamentoInscritoEmDividaAtiva
integrationEventsPublished: 
integrationEventsConsumed: 
-->
