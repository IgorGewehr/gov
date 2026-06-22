---
modulo: Transparencia
agregado: DeclaracaoFiscal
contexto: Transparencia (prestação de contas fiscais ao SICONFI/STN — MSC, RREO, RGF, DCA) — papel CONSUMIDOR
poder: Ambos
schema: transparencia
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "LC 101/2000 (LRF) art. 48 e 48-A (transparência da gestão fiscal; RREO bimestral; RGF quadrimestral)"
  - "LC 101/2000 (LRF) art. 23 §3º (atraso bloqueia transferências voluntárias)"
  - "Lei 4.320/64 (normas gerais de direito financeiro; execução orçamentária)"
  - "Portarias STN (Regras Gerais da MSC; SICONFI; RREO/RGF/DCA; PCASP/MCASP)"
  - "Decretos 7.185/2010 e 10.540/2020 (SIAFIC)"
---

# DeclaracaoFiscal (MSC / RREO / RGF / DCA ao SICONFI) — Regras-as-Code (Rules-as-Code)

> **Fonte da verdade.** Este `*.rules.md` é **normativo e versionado**. O domínio, a aplicação,
> a persistência e os testes do agregado `DeclaracaoFiscal` são **gerados e mantidos a partir
> daqui**. Bug, ajuste ou nova regra ⇒ edita-se **este arquivo**; o código é consequência.
>
> Declaração fiscal periódica transmitida ao **SICONFI** (STN): consolida a **Matriz de Saldos
> Contábeis (MSC)** mensal e dela derivam **RREO** (bimestral), **RGF** (quadrimestral) e **DCA**
> (anual). É montada a partir de saldos contábeis **consumidos** do módulo Finanças via
> Integration Event (`MSCGeradaIntegrationEvent`). Este módulo é **CONSUMIDOR**: não é fonte
> primária; cross-module ocorre **exclusivamente** via Integration Events.

---

## 1. Linguagem Ubíqua

Os identificadores entre parênteses (sem acento, PT-BR no domínio) são **VINCULANTES**: o gerador de código DEVE usá-los.

| Termo (identificador-no-código) | Definição |
|---|---|
| **Declaração Fiscal** (`DeclaracaoFiscal`) | Demonstrativo fiscal periódico (MSC/RREO/RGF/DCA) transmitido ao SICONFI. Raiz de agregado. |
| **DeclaracaoFiscalId** (`DeclaracaoFiscalId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| **TenantId** (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro — isolamento multi-tenant. |
| **Tipo de Declaração** (`TipoDeclaracao` : `TipoDeclaracaoFiscal`) | Espécie do demonstrativo (`Msc`, `Rreo`, `Rgf`, `Dca`). |
| **Matriz de Saldos** (`MatrizSaldos`) | Conjunto consolidado de saldos contábeis do período (entidade-filha; a MSC). |
| **Linha Contábil** (`LinhaContabil`) | Linha da matriz: conta PCASP, atributos e valor (entidade-filha). |
| **Competência** (`Competencia`) | Competência mensal (ano/mês) da MSC. Value Object. |
| **Bimestre** (`Bimestre`) | Período bimestral do RREO (ano + número 1..6). Value Object. |
| **Quadrimestre** (`Quadrimestre`) | Período quadrimestral do RGF (ano + número 1..3). Value Object. |
| **MSC** (`Msc`) | Matriz de Saldos Contábeis enviada ao SICONFI. |
| **RREO** (`Rreo`) | Relatório Resumido da Execução Orçamentária (bimestral). |
| **RGF** (`Rgf`) | Relatório de Gestão Fiscal (quadrimestral). |
| **DCA** (`Dca`) | Declaração de Contas Anuais. |
| **SICONFI** (`Siconfi` / `ISiconfiGateway`) | Sistema de Informações Contábeis e Fiscais do Setor Público (STN), destino da transmissão. |
| **Situação** (`Situacao` : `SituacaoDeclaracaoFiscal`) | Estado atual da declaração (`Consolidada → Transmitida → Homologada/Rejeitada`). |
| **Data-Limite** (`DataLimite`) | Prazo legal/parametrizado de transmissão do período. |
| **Consolidar Matriz** (`ConsolidarMatriz`) | Montar a `MatrizSaldos`/declaração a partir dos saldos recebidos (nasce em `Consolidada`). |
| **Transmitir ao SICONFI** (`TransmitirSiconfi`) | Enviar a declaração consolidada ao SICONFI (passa a `Transmitida`). |
| **Homologar** (`Homologar`) | Registrar a homologação pelo SICONFI/STN (passa a `Homologada`). |
| **Rejeitar** (`Rejeitar`) | Registrar a rejeição pelo SICONFI (passa a `Rejeitada`). |
| **MSC Enviada** (`MscEnviadaSiconfi`) | Evento de domínio da transmissão da MSC. |
| **Declaração Homologada** (`DeclaracaoHomologada`) | Evento de domínio da homologação pelo SICONFI. |

---

## 2. Modelo

- **Identidade:** `DeclaracaoFiscalId` — `readonly record struct DeclaracaoFiscalId(Guid Value)`; fábrica `DeclaracaoFiscalId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `DeclaracaoFiscal : AggregateRoot<DeclaracaoFiscalId>, IMustHaveTenant` (`sealed`).
- **Construção:** construtor privado sem parâmetros (EF) + construtor privado parametrizado + **factory** `ConsolidarMatriz(...)`. A entidade **nasce válida** e em `Consolidada`.

### 2.1 Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `Id` | `DeclaracaoFiscalId` | Identidade do agregado. | `private set` |
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. `IMustHaveTenant`. | `private set` |
| `TipoDeclaracao` | `TipoDeclaracaoFiscal` (enum) | Espécie (`Msc`/`Rreo`/`Rgf`/`Dca`). | `private set` |
| `Competencia` | `Competencia` (VO) | Competência mensal (preenchida quando MSC). | `private set` |
| `Bimestre` | `Bimestre?` (VO) | Período bimestral (preenchido quando RREO). | `private set` |
| `Quadrimestre` | `Quadrimestre?` (VO) | Período quadrimestral (preenchido quando RGF). | `private set` |
| `Exercicio` | `int` | Ano de exercício (sempre presente; base do DCA). | `private set` |
| `Situacao` | `SituacaoDeclaracaoFiscal` (enum) | Estado atual. | `private set` |
| `DataLimite` | `DateOnly` | Prazo legal/parametrizado de transmissão do período. | `private set` |
| `DataConsolidacao` | `DateOnly` | Data de consolidação. | `private set` |
| `DataTransmissao` | `DateOnly?` | Data de transmissão ao SICONFI (nula antes do envio). | `private set` |
| `Matriz` | `MatrizSaldos` | Matriz de saldos consolidada (entidade-filha). | composição |
| `ProtocoloSiconfi` | `string?` | Protocolo retornado pelo SICONFI na transmissão. | `private set` |

Todos os *setters* são `private set`. As mutações ocorrem **apenas** pelos métodos de comportamento (§5), preservando invariantes.

### 2.2 Entidades-filhas

**`MatrizSaldos` (entidade — a MSC):**
- `Linhas : IReadOnlyCollection<LinhaContabil>` — somente-leitura sobre `List<LinhaContabil>` privado.
- `TotalDebitos : ValorMonetario`, `TotalCreditos : ValorMonetario` (derivados/validados).
- Invariante de balanceamento (ver I-7).

**`LinhaContabil` (entidade):**
- `ContaPcasp : string` (conta do PCASP, não vazia), `NaturezaSaldo : NaturezaSaldo` (`Devedor`/`Credor`), `Valor : ValorMonetario`, atributos da MSC (`InformacaoComplementar : string?`).

### 2.3 Value Objects

**`Competencia` (`ValueObject`):**
- Propriedades: `Ano : int` (>= 1900), `Mes : int` (1..12).
- Fábrica: `Competencia.De(int ano, int mes)` — `ArgumentOutOfRangeException` se `ano < 1900`, `mes < 1` ou `mes > 12`.
- Igualdade por valor: `(Ano, Mes)`. `ToString()` → `"MM/AAAA"` (cultura invariante).

**`Bimestre` (`ValueObject`):**
- Propriedades: `Ano : int` (>= 1900), `Numero : int` (1..6).
- Fábrica: `Bimestre.De(int ano, int numero)` — `ArgumentOutOfRangeException` fora das fronteiras.
- Igualdade por valor: `(Ano, Numero)`. `ToString()` → ex.: `"2026-B03"`.

**`Quadrimestre` (`ValueObject`):**
- Propriedades: `Ano : int` (>= 1900), `Numero : int` (1..3).
- Fábrica: `Quadrimestre.De(int ano, int numero)` — `ArgumentOutOfRangeException` fora das fronteiras.
- Igualdade por valor: `(Ano, Numero)`. `ToString()` → ex.: `"2026-Q02"`.

**`ValorMonetario` (`ValueObject`):**
- Propriedade: `Valor : decimal` (Reais, arredondado a 2 casas, `MidpointRounding.AwayFromZero`).
- Fábrica: `ValorMonetario.De(decimal valor)`. Constante `ValorMonetario.Zero`. Operação `Somar(ValorMonetario)`.

### 2.4 Enums

**`TipoDeclaracaoFiscal`:**

| Valor | Nome | Significado |
|---|---|---|
| 1 | `Msc` | Matriz de Saldos Contábeis (mensal). |
| 2 | `Rreo` | Relatório Resumido da Execução Orçamentária (bimestral). |
| 3 | `Rgf` | Relatório de Gestão Fiscal (quadrimestral). |
| 4 | `Dca` | Declaração de Contas Anuais. |

**`SituacaoDeclaracaoFiscal`:**

| Valor | Nome | Significado |
|---|---|---|
| 1 | `Consolidada` | Matriz/declaração consolidada (estado inicial), ainda não transmitida. |
| 2 | `Transmitida` | Transmitida ao SICONFI (protocolo recebido). |
| 3 | `Homologada` | Homologada pelo SICONFI/STN (terminal de sucesso). |
| 4 | `Rejeitada` | Rejeitada pelo SICONFI (terminal de falha; admite reconsolidação). |

**`NaturezaSaldo`:** `Devedor` (1), `Credor` (2).

> **Conjuntos de referência:**
> - **Encerrada** = { `Homologada`, `Rejeitada` }.
> - **Transmissível** = { `Consolidada` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Toda `DeclaracaoFiscal` pertence a exatamente um tenant (`IMustHaveTenant`); `TenantId` é imutável após a criação.
- **I-2.** `TipoDeclaracao`, `Exercicio` e a `MatrizSaldos` são obrigatórios na consolidação (`ArgumentNullException.ThrowIfNull` para a matriz; `Exercicio >= 1900`).
- **I-3.** Coerência período × tipo: `Msc` exige `Competencia`; `Rreo` exige `Bimestre`; `Rgf` exige `Quadrimestre`; `Dca` usa apenas `Exercicio`. Período incompatível ⇒ `ArgumentException`.
- **I-4.** Ao consolidar (`ConsolidarMatriz`), a declaração nasce em `Consolidada` (LRF arts. 48/48-A; Lei 4.320/64).
- **I-5.** A transmissão só ocorre a partir de `Consolidada` (estado **Transmissível**); caso contrário, `InvalidOperationException`. Transmitir leva a `Transmitida`.
- **I-6.** A transmissão de uma declaração do tipo `Msc` emite `MscEnviadaSiconfi`; demais tipos emitem `DeclaracaoTransmitida` (evento de domínio genérico de transmissão).
- **I-7.** A `MatrizSaldos` deve estar **balanceada**: `TotalDebitos == TotalCreditos` (partidas dobradas, PCASP). Matriz desbalanceada ⇒ `InvalidOperationException` na consolidação.
- **I-8.** A homologação só ocorre a partir de `Transmitida`; leva a `Homologada` e emite `DeclaracaoHomologada`. Fora disso ⇒ `InvalidOperationException`.
- **I-9.** A rejeição só ocorre a partir de `Transmitida`; leva a `Rejeitada`. Estado terminal; correção exige **nova** consolidação.
- **I-10.** Estados **terminais** (`Homologada`, `Rejeitada`) não admitem `TransmitirSiconfi`/`Homologar`/`Rejeitar`.
- **I-11.** `DataLimite` é **parametrizável por tenant** (nunca *hardcoded*): **MSC** até o **último dia do mês subsequente** à `Competencia`; **RGF/RREO** até **30 dias** após o quadrimestre/bimestre; atraso enseja risco de bloqueio de transferências (LRF art. 23 §3º).
- **I-12.** Como módulo **CONSUMIDOR**, a `MatrizSaldos` provém dos saldos recebidos via `MSCGeradaIntegrationEvent` (Finanças); o agregado **não** calcula contabilidade primária.
- **I-13.** Idempotência: o consumo do mesmo `MSCGeradaIntegrationEvent` (por `EventId`/competência) **não** cria declaração duplicada para a mesma `(TipoDeclaracao, Competencia/Bimestre/Quadrimestre, Tenant)`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento de domínio emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| _(inexistente)_ | `ConsolidarMatriz` | `Consolidada` | tipo×período coerente; matriz balanceada | _(nenhum)_ |
| `Consolidada` | `TransmitirSiconfi` (tipo `Msc`) | `Transmitida` | situação == `Consolidada` | `MscEnviadaSiconfi` |
| `Consolidada` | `TransmitirSiconfi` (tipo ≠ `Msc`) | `Transmitida` | situação == `Consolidada` | `DeclaracaoTransmitida` |
| `Transmitida` | `Homologar` | `Homologada` | situação == `Transmitida` | `DeclaracaoHomologada` |
| `Transmitida` | `Rejeitar` | `Rejeitada` | situação == `Transmitida` | _(nenhum)_ |
| `Homologada` \| `Rejeitada` | `TransmitirSiconfi` / `Homologar` / `Rejeitar` | — | bloqueado (terminal) → `InvalidOperationException` | — |
| `Consolidada` | `Homologar` / `Rejeitar` | — | bloqueado (situação != `Transmitida`) → `InvalidOperationException` | — |

> Observações:
> - `ConsolidarMatriz` não emite Domain Event próprio nesta versão (a consolidação é interna); o primeiro evento relevante ocorre na **transmissão**.
> - Correção de declaração `Rejeitada` ⇒ **nova** `DeclaracaoFiscal` (reconsolidação), preservando a trilha da rejeitada.

---

## 5. Comandos (escrita)

### 5.1 `ConsolidarDeclaracaoFiscal` — Montar a MSC/declaração (consumo da MSC de Finanças)

- **Comando (DTO):** `ConsolidarDeclaracaoFiscalCommand : ICommand<Guid>`
  - `TipoDeclaracao : TipoDeclaracaoFiscal`
  - `Exercicio : int`
  - `Mes : int?` (MSC)
  - `NumeroBimestre : int?` (RREO)
  - `NumeroQuadrimestre : int?` (RGF)
  - `Linhas : IReadOnlyList<LinhaContabilDto>` (saldos recebidos)
- **Dependências do handler:** `IDeclaracaoFiscalRepository`, `ICalendarioFiscal` (deriva `DataLimite` por tenant), `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:** comando válido (§8); período coerente com o tipo (I-3); matriz balanceada (I-7); inexistência de declaração para a mesma `(Tipo, período, tenant)` ou idempotência por `EventId` (I-13).
- **Efeito:** constrói `MatrizSaldos`/`LinhaContabil`; cria via `DeclaracaoFiscal.ConsolidarMatriz(tenant.TenantId, tipo, exercicio, competencia?, bimestre?, quadrimestre?, dataLimite, hoje, matriz)`; `repo.Adicionar(declaracao)`; `SaveChangesAsync`.
- **Pós-condições:** nova `DeclaracaoFiscal` em `Consolidada`, carimbada com `TenantId`; retorna `Guid` = `declaracao.Id.Value`.
- **Exceções:** `ValidationException`; `ArgumentException` (período×tipo); `InvalidOperationException` (matriz desbalanceada / duplicidade); `ArgumentOutOfRangeException` (VOs).
- **Evento de domínio:** _(nenhum nesta etapa)_.

> **Gatilho usual:** este comando é disparado pelo **handler de consumo** do `MSCGeradaIntegrationEvent` (Finanças) — ver §7.3 e §11.

### 5.2 `TransmitirDeclaracaoFiscal` — Enviar ao SICONFI

- **Comando (DTO):** `TransmitirDeclaracaoFiscalCommand(Guid DeclaracaoFiscalId) : ICommand`.
- **Dependências do handler:** `IDeclaracaoFiscalRepository`, `ISiconfiGateway` (ACL + Polly), `ICertificadoTenantProvider` (A1 por tenant, Azure Key Vault, quando exigido), `IUnitOfWork`, `IPublisher`/Outbox, `TimeProvider`.
- **Pré-condições:** `request` não nulo; declaração existe; situação == `Consolidada` (I-5).
- **Efeito:** transmite ao SICONFI via `ISiconfiGateway` (idempotente por `DeclaracaoFiscalId`), obtém `ProtocoloSiconfi`; `declaracao.TransmitirSiconfi(hoje, protocolo)` → `Transmitida`; `SaveChangesAsync`; se tipo `Msc`, publica **Integration Event** `MscEnviadaSiconfiIntegrationEvent` (Outbox).
- **Pós-condições:** situação `Transmitida`, `DataTransmissao`/`ProtocoloSiconfi` preenchidos; evento de domínio `MscEnviadaSiconfi` (tipo `Msc`) ou `DeclaracaoTransmitida`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrada / situação ≠ `Consolidada`); erros do gateway mapeados pela ACL.
- **Evento de domínio:** `MscEnviadaSiconfi` | `DeclaracaoTransmitida`.
- **Evento de integração (publica, tipo `Msc`):** `MscEnviadaSiconfiIntegrationEvent`.

### 5.3 `HomologarDeclaracaoFiscal` — Registrar homologação do SICONFI

- **Comando (DTO):** `HomologarDeclaracaoFiscalCommand(Guid DeclaracaoFiscalId) : ICommand`.
- **Dependências do handler:** `IDeclaracaoFiscalRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; declaração existe; situação == `Transmitida` (I-8).
- **Efeito:** `declaracao.Homologar()` → `Homologada`; `SaveChangesAsync`.
- **Pós-condições:** situação `Homologada`; evento `DeclaracaoHomologada`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrada / situação ≠ `Transmitida`).
- **Evento de domínio:** `DeclaracaoHomologada`.

### 5.4 `RejeitarDeclaracaoFiscal` — Registrar rejeição do SICONFI

- **Comando (DTO):** `RejeitarDeclaracaoFiscalCommand(Guid DeclaracaoFiscalId, string Motivo) : ICommand`.
- **Dependências do handler:** `IDeclaracaoFiscalRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; declaração existe; situação == `Transmitida` (I-9).
- **Efeito:** `declaracao.Rejeitar(request.Motivo)` → `Rejeitada`; `SaveChangesAsync`.
- **Pós-condições:** situação `Rejeitada`; motivo registrado para trilha.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrada / situação ≠ `Transmitida`).
- **Evento de domínio:** _(nenhum)_.

---

## 6. Consultas (leitura)

### 6.1 `ObterDeclaracaoFiscalPorId`

- **Query:** `ObterDeclaracaoFiscalPorIdQuery(Guid DeclaracaoFiscalId) : IQuery<DeclaracaoFiscalDetalhe?>`.
- **Handler:** `ObterDeclaracaoFiscalPorIdHandler(IDeclaracaoFiscalRepository)`.
- **Projeção (DTO):** `DeclaracaoFiscalDetalhe(Guid Id, string TipoDeclaracao, int Exercicio, string? Competencia, string? Bimestre, string? Quadrimestre, string Situacao, DateOnly DataLimite, DateOnly? DataTransmissao, string? ProtocoloSiconfi, decimal TotalDebitos, decimal TotalCreditos)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 `ListarDeclaracoesFiscaisPorExercicio`

- **Query:** `ListarDeclaracoesFiscaisPorExercicioQuery(int Exercicio, TipoDeclaracaoFiscal? Tipo, SituacaoDeclaracaoFiscal? Situacao) : IQuery<IReadOnlyList<DeclaracaoFiscalResumo>>`.
- **Handler:** `ListarDeclaracoesFiscaisPorExercicioHandler(IDeclaracaoFiscalRepository)`.
- **Projeção (DTO):** `DeclaracaoFiscalResumo(Guid Id, string TipoDeclaracao, int Exercicio, string Periodo, string Situacao, DateOnly DataLimite, DateOnly? DataTransmissao)`.
- **Filtros:** por `Exercicio` (+ `Tipo`/`Situacao` opcionais); **tenant-scoped**; paginação padrão do módulo.
- **Pré-condições:** `request` não nulo.

---

## 7. Eventos

### 7.1 Domínio (in-process, MediatR — assembly `...Transparencia.Domain`)

| Evento | Payload | Emitido em |
|---|---|---|
| `MscEnviadaSiconfi` | `(DeclaracaoFiscalId DeclaracaoFiscalId, string Competencia, string Protocolo)` | `TransmitirSiconfi` (tipo `Msc`). |
| `DeclaracaoTransmitida` | `(DeclaracaoFiscalId DeclaracaoFiscalId, string TipoDeclaracao, string Periodo)` | `TransmitirSiconfi` (tipos `Rreo`/`Rgf`/`Dca`). |
| `DeclaracaoHomologada` | `(DeclaracaoFiscalId DeclaracaoFiscalId, string TipoDeclaracao)` | `Homologar`. |

Todos implementam `IDomainEvent` e são `sealed record`.

### 7.2 Integração — **publica** (via `Modules.Transparencia.Contracts`, Outbox)

| Evento | Payload | Publicado por |
|---|---|---|
| `MscEnviadaSiconfiIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid DeclaracaoFiscalId, string Competencia, string Protocolo, DateOnly DataTransmissao)` | `TransmitirDeclaracaoFiscalHandler` (tipo `Msc`). |

### 7.3 Integração — **consome** (via `Modules.Financas.Contracts`)

| Evento | Origem | Reação |
|---|---|---|
| `MSCGeradaIntegrationEvent` | **Finanças** | Handler de consumo (`MSCGeradaIntegrationEventHandler`) dispara `ConsolidarDeclaracaoFiscalCommand`, montando a `MatrizSaldos` do mês. **Idempotente** por `EventId`/competência (I-13). |

> Os demais Integration Events assinados pelo módulo (despesas, contratos, folha, receita, bens — README §7) alimentam `PublicacaoTransparencia`/transparência ativa e **não** entram diretamente na `DeclaracaoFiscal`, cuja única fonte é a MSC de Finanças.

---

## 8. Validações (FluentValidation)

### `ConsolidarDeclaracaoFiscalValidator : AbstractValidator<ConsolidarDeclaracaoFiscalCommand>`

| Campo | Regra | Mensagem (padrão FluentValidation) |
|---|---|---|
| `TipoDeclaracao` | `IsInEnum()` | "'Tipo Declaracao' has a range of values which does not include ..." |
| `Exercicio` | `GreaterThanOrEqualTo(1900)` | "'Exercicio' must be greater than or equal to '1900'." |
| `Mes` | `InclusiveBetween(1, 12)` **When** `TipoDeclaracao == Msc` | "'Mes' must be between 1 and 12." |
| `NumeroBimestre` | `InclusiveBetween(1, 6)` **When** `TipoDeclaracao == Rreo` | "'Numero Bimestre' must be between 1 and 6." |
| `NumeroQuadrimestre` | `InclusiveBetween(1, 3)` **When** `TipoDeclaracao == Rgf` | "'Numero Quadrimestre' must be between 1 and 3." |
| `Linhas` | `NotEmpty()` + `RuleForEach` (conta PCASP não vazia; valor presente) | "'Linhas' must not be empty." |

### `TransmitirDeclaracaoFiscalValidator` / `HomologarDeclaracaoFiscalValidator` / `RejeitarDeclaracaoFiscalValidator`

| Campo | Regra | Mensagem |
|---|---|---|
| `DeclaracaoFiscalId` | `NotEmpty()` | "'Declaracao Fiscal Id' must not be empty." |
| `Motivo` (apenas Rejeitar) | `NotEmpty()` + `MaximumLength(500)` | "'Motivo' must not be empty." |

> O balanceamento da matriz (`TotalDebitos == TotalCreditos`) é invariante **de domínio** (I-7), validado na consolidação — não no validador do comando (defesa em profundidade).

---

## 9. Persistência (EF Core 8)

- **DbContext:** `TransparenciaDbContext` — `Schema => "transparencia"`. **Migrations por módulo.**
- **Tabela raiz:** `transparencia.DeclaracoesFiscais`. **Filhas:** `transparencia.MatrizesSaldos`, `transparencia.LinhasContabeis` (has-many com `OnDelete(Cascade)` dentro do agregado).
- **Configuração:** `DeclaracaoFiscalConfiguration : IEntityTypeConfiguration<DeclaracaoFiscal>` (Fluent API; sem data annotations no domínio).

| Coluna (raiz) | Tipo de coluna | Conversor / mapeamento |
|---|---|---|
| `Id` | `uniqueidentifier` (PK) | `id => id.Value` / `value => new DeclaracaoFiscalId(value)`; `ValueGeneratedNever()`. |
| `TenantId` | `uniqueidentifier` | primitivo (carimbado pelo TenantInterceptor); alvo do Global Query Filter. |
| `TipoDeclaracao` | `nvarchar(10)` | `HasConversion<string>()` (enum como string). |
| `Competencia` | owned (`Ano:int`, `Mes:int`), nulável | `OwnsOne` (VO `Competencia`). |
| `Bimestre` | owned (`Ano:int`, `Numero:int`), nulável | `OwnsOne` (VO `Bimestre`). |
| `Quadrimestre` | owned (`Ano:int`, `Numero:int`), nulável | `OwnsOne` (VO `Quadrimestre`). |
| `Exercicio` | `int` | primitivo. |
| `Situacao` | `nvarchar(20)` | `HasConversion<string>()` (enum como string). |
| `DataLimite` | `date` | `DateOnly` (nativo EF Core 8). |
| `DataConsolidacao` | `date` | `DateOnly`. |
| `DataTransmissao` | `date?` | `DateOnly?`. |
| `ProtocoloSiconfi` | `nvarchar(60)?` | nulável. |

| Coluna (`LinhaContabil`) | Tipo | Mapeamento |
|---|---|---|
| `ContaPcasp` | `nvarchar(30)` | não nula. |
| `NaturezaSaldo` | `nvarchar(10)` | `HasConversion<string>()`. |
| `Valor` | `decimal(18,2)` | conversor `ValorMonetario`. |
| `InformacaoComplementar` | `nvarchar(255)?` | nulável. |

- **Índices:**
  - PK em `Id`.
  - Índice **único** `UX_DeclaracoesFiscais_Tenant_Tipo_Periodo` em `(TenantId, TipoDeclaracao, Exercicio, Competencia_Mes, Bimestre_Numero, Quadrimestre_Numero)` desconsiderando `Rejeitada` (uma declaração vigente por tipo/período/tenant — apoia a idempotência I-13).
  - `IX_DeclaracoesFiscais_TenantId_Situacao_DataLimite` em `(TenantId, Situacao, DataLimite)` para varredura de prazos.
- **Outbox / Auditoria:** `TransparenciaDbContext` herda a base com tabela **Outbox**, `AuditSaveChangesInterceptor` e `TenantInterceptor`.

---

## 10. Segurança, Tenant e Auditoria

- **`IMustHaveTenant`:** `DeclaracaoFiscal` implementa; `TenantId` carimbado na inserção pelo `TenantInterceptor`. **Global Query Filter** por `TenantId` em todas as leituras. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT (ApiHost) ou por iteração explícita (Workers — consumo do `MSCGeradaIntegrationEvent`); jamais aceito do cliente.
- **RBAC (policy-based, negar por padrão):** consolidação/transmissão/homologação/rejeição exigem o papel `transparencia.remeter`; consultas exigem leitura de transparência. Módulo Transparencia é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **Certificado A1:** transmissão ao SICONFI usa, quando exigido, certificado **A1 (.pfx)** do **Azure Key Vault**, **por tenant** (`ICertificadoTenantProvider`); **nenhum** segredo no repositório.
- **Auditoria imutável:** toda mutação (`ConsolidarMatriz`, `TransmitirSiconfi`, `Homologar`, `Rejeitar`) gera trilha (JSON antes/depois, usuário, IP, timestamp) via `AuditSaveChangesInterceptor`, para o **Tribunal de Contas (TCE-RS)** e a STN.
- **LGPD:** a MSC é composta de saldos contábeis agregados (sem dado pessoal direto). Eventuais informações complementares são minimizadas; o demonstrativo segue base legal de prestação de contas (LRF).
- **Anti-SQLi:** acesso exclusivamente via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Sistema:** **SICONFI** (STN) — destino da MSC (mensal) e dos demonstrativos derivados (RREO/RGF/DCA).
- **Layout/versão:** conforme as **Portarias STN / Regras Gerais da MSC** vigentes; confirmar a versão atual antes de transmitir (CLAUDE.md §16). PCASP/MCASP regem as contas das `LinhaContabil`.
- **Prazo legal (parametrizável por tenant):** **MSC** até o **último dia do mês subsequente** à competência; **RREO** até **30 dias** após o bimestre; **RGF** até **30 dias** após o quadrimestre; **DCA** conforme calendário anual STN. Atraso ⇒ risco de bloqueio de transferências voluntárias (LRF art. 23 §3º).
- **Consumo (entrada):** `MSCGeradaIntegrationEvent` de **Finanças** alimenta a consolidação da `MatrizSaldos` (idempotente por `EventId`/competência) — README §7 e Cenário 5.
- **Resiliência (Polly):** transmissão com **timeout, retry e circuit breaker** via `ISiconfiGateway`; **idempotente** por `DeclaracaoFiscalId` (retransmissão não duplica).
- **ACL (Anti-Corruption Layer):** `ISiconfiGateway` isola o domínio dos contratos SOAP/REST do SICONFI; mapeamento explícito de erros para resultados de domínio.
- **Outbox:** `MscEnviadaSiconfiIntegrationEvent` publicado transacionalmente com o estado.

---

## 12. Cenários BDD

Cada cenário Given/When/Then vira um teste de integração (alinhado ao README §8, Cenário 5).

**Cenário 1 — Consolidação a partir da MSC de Finanças.**
- **Dado** o recebimento de `MSCGeradaIntegrationEvent` de **Finanças** com os saldos do mês
- **Quando** o handler de consumo dispara `ConsolidarDeclaracaoFiscal(TipoDeclaracao=Msc, Exercicio=2026, Mes=5, Linhas=...)`
- **Então** é criada uma `DeclaracaoFiscal` em `Consolidada`, com a `MatrizSaldos` balanceada e `DataLimite` = último dia do mês subsequente.

**Cenário 2 — Envio da MSC ao SICONFI (README Cenário 5).**
- **Dada** uma `DeclaracaoFiscal` (`Msc`) em `Consolidada`
- **Quando** executo `TransmitirDeclaracaoFiscal`
- **Então** a MSC é transmitida ao SICONFI, a situação passa a `Transmitida`, `MscEnviadaSiconfi` (domínio) e `MscEnviadaSiconfiIntegrationEvent` são publicados, com `Protocolo` registrado.

**Cenário 3 — Transmissão bloqueada fora de Consolidada.**
- **Dada** uma `DeclaracaoFiscal` `Transmitida` (ou `Homologada`/`Rejeitada`)
- **Quando** executo `TransmitirDeclaracaoFiscal`
- **Então** ocorre `InvalidOperationException` (situação ≠ `Consolidada`).

**Cenário 4 — Matriz desbalanceada.**
- **Dada** uma lista de linhas cujos débitos ≠ créditos
- **Quando** executo `ConsolidarDeclaracaoFiscal`
- **Então** ocorre `InvalidOperationException` (matriz desbalanceada) e nada é persistido.

**Cenário 5 — Homologação pelo SICONFI.**
- **Dada** uma `DeclaracaoFiscal` `Transmitida`
- **Quando** executo `HomologarDeclaracaoFiscal`
- **Então** a situação passa a `Homologada` e `DeclaracaoHomologada` é emitido.

**Cenário 6 — Rejeição pelo SICONFI.**
- **Dada** uma `DeclaracaoFiscal` `Transmitida`
- **Quando** executo `RejeitarDeclaracaoFiscal(Motivo="inconsistência X")`
- **Então** a situação passa a `Rejeitada` e o motivo é registrado para trilha.

**Cenário 7 — Coerência período × tipo.**
- **Dado** um comando `TipoDeclaracao=Rgf` sem `NumeroQuadrimestre` (ou com `Mes`)
- **Quando** executo `ConsolidarDeclaracaoFiscal`
- **Então** ocorre `ValidationException`/`ArgumentException` (período incompatível com o tipo).

**Cenário 8 — Idempotência do consumo.**
- **Dado** o reprocessamento do mesmo `MSCGeradaIntegrationEvent` (mesmo `EventId`/competência)
- **Quando** o handler de consumo executa novamente
- **Então** **não** é criada uma segunda `DeclaracaoFiscal` para a mesma `(Tipo, período, tenant)`.

**Cenário 9 — Isolamento de tenant.**
- **Dadas** declarações do tenant A e do tenant B
- **Quando** executo `ListarDeclaracoesFiscaisPorExercicio` no contexto do tenant A
- **Então** retornam **apenas** as declarações do tenant A.

---

## 13. Casos de Borda

Cada item vira um teste.

- **CB-1.** `Competencia.De(2026, 13)` → `ArgumentOutOfRangeException` (mês 1..12).
- **CB-2.** `Bimestre.De(2026, 7)` → `ArgumentOutOfRangeException` (bimestre 1..6).
- **CB-3.** `Quadrimestre.De(2026, 4)` → `ArgumentOutOfRangeException` (quadrimestre 1..3).
- **CB-4.** `ConsolidarDeclaracaoFiscal` com `Linhas` vazias → `ValidationException` (`NotEmpty`).
- **CB-5.** `Msc` sem `Mes` → `ValidationException` (`InclusiveBetween` com `When`).
- **CB-6.** `Rreo` informando `Mes` em vez de `NumeroBimestre` → `ArgumentException` (período×tipo) na consolidação.
- **CB-7.** Matriz com `TotalDebitos == TotalCreditos == 0` (vazia balanceada) → bloqueada por `Linhas` `NotEmpty` no validador, antes do domínio.
- **CB-8.** `TransmitirDeclaracaoFiscal` do tipo `Rreo` → emite `DeclaracaoTransmitida` (e **não** `MscEnviadaSiconfi`); **não** publica `MscEnviadaSiconfiIntegrationEvent`.
- **CB-9.** `Homologar` sobre declaração `Consolidada` (não transmitida) → `InvalidOperationException` (situação ≠ `Transmitida`).
- **CB-10.** `Rejeitar` sobre declaração `Homologada` → `InvalidOperationException` (terminal).
- **CB-11.** Retransmissão da mesma declaração ao SICONFI → idempotente por `DeclaracaoFiscalId` (não duplica protocolo).
- **CB-12.** Correção de declaração `Rejeitada` → cria **nova** `DeclaracaoFiscal` (reconsolidação); a rejeitada permanece para trilha/auditoria.
- **CB-13.** `DataLimite` da MSC = último dia do mês subsequente; do RGF/RREO = +30 dias do período — derivada por `ICalendarioFiscal`, parametrizável por tenant (nunca *hardcoded*).
- **CB-14.** Tentar gravar/obter declaração de outro tenant → barrado pelo Global Query Filter e pelo `TenantInterceptor`.
- **CB-15.** Arredondamento contábil: `ValorMonetario.De(1,005)` → `1,01` (`AwayFromZero`); persiste como `decimal(18,2)`.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — especificação Rules-as-Code do agregado `DeclaracaoFiscal` (módulo Transparencia) derivada do README do Bounded Context e das fontes legais (LRF arts. 48/48-A e 23 §3º, Lei 4.320/64, Portarias STN/MSC/SICONFI, SIAFIC). Define ciclo `Consolidada → Transmitida → Homologada/Rejeitada`, consumo idempotente de `MSCGeradaIntegrationEvent` (Finanças), balanceamento da matriz, prazos parametrizáveis e publicação de `MscEnviadaSiconfiIntegrationEvent`. |

<!-- manifest
commands: ConsolidarDeclaracaoFiscal, TransmitirDeclaracaoFiscal, HomologarDeclaracaoFiscal, RejeitarDeclaracaoFiscal, GerarMsc, ReconciliarSiconfi
queries: ObterDeclaracaoFiscalPorId, ListarDeclaracoesFiscaisPorExercicio
domainEvents: MscEnviadaSiconfi, DeclaracaoTransmitida, DeclaracaoHomologada
integrationEventsPublished: MscEnviadaSiconfiIntegrationEvent
integrationEventsConsumed: MSCGeradaIntegrationEvent
-->
