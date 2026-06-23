---
modulo: Tributos
agregado: DividaAtiva
contexto: Tributos (Dívida Ativa, CDA e cobrança)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CTN art. 174 (prescrição 5 anos)", "Lei 6.830/80 (LEF)", "Lei 9.492/97 (protesto)"]
---

# DividaAtiva — Regras-as-Code (Rules-as-Code)

> Crédito tributário inscrito em Dívida Ativa: título exigível com prazo prescricional de
> 5 anos (CTN art. 174), passível de emissão de CDA, protesto extrajudicial (Lei 9.492/97),
> execução fiscal (Lei 6.830/80 — LEF) e parcelamento (REFIS). Este arquivo é **normativo e
> versionado**; o código (`DividaAtiva.cs`, handlers, validators, EF config, testes) é
> consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Dívida Ativa (`DividaAtiva`) | Crédito tributário vencido e não pago, inscrito como título exigível pela Fazenda Pública. Raiz de agregado. |
| Inscrição (`Inscrever` / `InscreverEmDividaAtiva`) | Ato de constituir a Dívida Ativa a partir de um lançamento em aberto e vencido. |
| CDA — Certidão de Dívida Ativa (`Cda` / `EmitirCda` / `NumeroCda`) | Documento que aparelha a cobrança (administrativa, protesto e execução fiscal). |
| Protesto (`Protestar` / `Protestada`) | Protesto extrajudicial da CDA em cartório (Lei 9.492/97). |
| Execução Fiscal (`AjuizarExecucaoFiscal` / `EmExecucaoFiscal`) | Ajuizamento da cobrança judicial (Lei 6.830/80 — LEF). |
| Parcelamento / REFIS (`FirmarParcelamento` / `Parcelada`) | Acordo que suspende a exigibilidade e interrompe a prescrição. |
| Quitação (`Quitar` / `Quitada`) | Extinção do crédito por pagamento integral. |
| Cancelamento (`Cancelada`) | Estado terminal de encerramento sem pagamento (ex.: anistia, decisão administrativa). |
| Prescrição (`DataPrescricao` / `EstaPrescrita` / `AnosPrescricao`) | Perda da exigibilidade pelo decurso de 5 anos da inscrição (CTN art. 174). |
| Exigibilidade (`GarantirExigivel`) | Estado em que o crédito pode sofrer atos de cobrança (não suspenso nem encerrado). |
| Valor Inscrito (`ValorInscrito` : `ValorMonetario`) | Montante do crédito inscrito. |
| Contribuinte (`ContribuinteId`) | Sujeito passivo devedor. |
| Lançamento (`LancamentoId`) | Crédito tributário de origem (constituição do crédito). |
| Tenant (`TenantId`) | Ente público (Prefeitura) dono do registro. |
| Situação (`Situacao` : `SituacaoDividaAtiva`) | Estado atual do título no ciclo de cobrança. |

---

## 2. Modelo

- **Identidade:** `DividaAtivaId` — `readonly record struct DividaAtivaId(Guid Value)`; fábrica `DividaAtivaId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `DividaAtiva : AggregateRoot<DividaAtivaId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Inscrever(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `ContribuinteId` | `ContribuinteId` (VO/Id) | Contribuinte devedor. | `private set` |
| `LancamentoId` | `LancamentoId` (VO/Id) | Lançamento de origem. | `private set` |
| `ValorInscrito` | `ValorMonetario` (VO) | Valor inscrito. | `private set` |
| `DataInscricao` | `DateOnly` | Data de inscrição em Dívida Ativa. | `private set` |
| `NumeroCda` | `string?` | Número da CDA, quando emitida (nulo antes da emissão). | `private set` |
| `Situacao` | `SituacaoDividaAtiva` | Situação atual. | `private set` |
| `DataPrescricao` | `DateOnly` (calculada) | `DataInscricao.AddYears(AnosPrescricao)` — somente leitura. | derivada |

### Constantes

- `AnosPrescricao` = `5` (CTN art. 174). Constante de domínio que parametriza a prescrição.

### Value Objects (referenciados)

- `ValorMonetario` — valor monetário do crédito; expõe `Valor` (`decimal`). Não nulo (validado em `Inscrever`).
- `ContribuinteId` — `record struct` com `Value : Guid`.
- `LancamentoId` — `record struct` com `Value : Guid`.

### Enum `SituacaoDividaAtiva`

| Valor | Numérico | Descrição |
|---|---|---|
| `Inscrita` | 1 | Inscrita em Dívida Ativa (estado inicial). |
| `CdaEmitida` | 2 | Com CDA emitida. |
| `Protestada` | 3 | Protestada em cartório. |
| `EmExecucaoFiscal` | 4 | Em execução fiscal (Lei 6.830/80). |
| `Parcelada` | 5 | Parcelada (REFIS) — exigibilidade suspensa. |
| `Quitada` | 6 | Quitada (terminal). |
| `Cancelada` | 7 | Cancelada (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Quitada`, `Cancelada` }.
> - **Não exigível** (`GarantirExigivel` lança) = { `Quitada`, `Cancelada`, `Parcelada` }.
> - **Exigível** = qualquer situação que **não** esteja em "Não exigível".

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Prazo prescricional = `DataInscricao + AnosPrescricao` (5 anos), exposto por `DataPrescricao` (CTN art. 174).
- **I-2.** A CDA só pode ser emitida a partir da situação `Inscrita` (estado recém-inscrito). Caso contrário, `InvalidOperationException`.
- **I-3.** A emissão de CDA exige `numeroCda` não nulo/não vazio (`ArgumentException.ThrowIfNullOrWhiteSpace`); ao emitir, grava `NumeroCda` e passa a `CdaEmitida`.
- **I-4.** O protesto exige situação `CdaEmitida` **e** crédito exigível. Caso contrário, `InvalidOperationException`.
- **I-5.** A execução fiscal exige situação `CdaEmitida` **ou** `Protestada` **e** crédito exigível. Caso contrário, `InvalidOperationException` (Lei 6.830/80).
- **I-6.** O parcelamento (REFIS) só ocorre sobre crédito exigível; ao firmar, passa a `Parcelada` (suspende exigibilidade e interrompe prescrição).
- **I-7.** A quitação é proibida sobre dívida já **encerrada** (`Quitada`/`Cancelada`); caso contrário, `InvalidOperationException`. Quitação válida leva a `Quitada`.
- **I-8.** Crédito **não exigível** ({`Quitada`,`Cancelada`,`Parcelada`}) não admite protesto, execução fiscal nem parcelamento (`GarantirExigivel` lança).
- **I-9.** `EstaPrescrita(hoje)` é verdadeiro somente se a situação **não** estiver em {`Quitada`, `Parcelada`, `Cancelada`} **e** `hoje > DataPrescricao`.
- **I-10.** `ValorInscrito` é obrigatório na inscrição (`ArgumentNullException.ThrowIfNull(valorInscrito)`).
- **I-11.** Na inscrição, a situação inicial é `Inscrita` e é emitido o evento `DividaAtivaInscrita(id, contribuinteId, lancamentoId)`.
- **I-12.** Estados terminais (`Quitada`, `Cancelada`) não admitem novas transições de cobrança.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Inscrever` | `Inscrita` | `valorInscrito != null` | `DividaAtivaInscrita` |
| `Inscrita` | `EmitirCda` | `CdaEmitida` | `numeroCda` não vazio; situação == `Inscrita` | `CdaEmitida` |
| `CdaEmitida` | `Protestar` | `Protestada` | exigível && situação == `CdaEmitida` | — |
| `CdaEmitida` \| `Protestada` | `AjuizarExecucaoFiscal` | `EmExecucaoFiscal` | exigível && situação ∈ {`CdaEmitida`,`Protestada`} | — |
| exigível (qualquer não encerrada/não suspensa) | `FirmarParcelamento` | `Parcelada` | exigível | `ParcelamentoFirmado` |
| ≠ encerrada ({`Quitada`,`Cancelada`}) | `Quitar` | `Quitada` | situação ∉ {`Quitada`,`Cancelada`} | `DividaQuitada` |

> Observações:
> - `Protestar` e `AjuizarExecucaoFiscal` chamam `GarantirExigivel()` **antes** da verificação específica de situação.
> - Não há método de transição para `Cancelada` no agregado atual (estado previsto no enum, sem comando de mudança implementado nesta versão).
> - `FirmarParcelamento` aceita qualquer situação exigível (inclusive `Inscrita`, `CdaEmitida`, `Protestada`, `EmExecucaoFiscal`).

---

## 5. Comandos (escrita)

### 5.1 InscreverEmDividaAtiva

- **Command:** `InscreverEmDividaAtivaCommand(Guid LancamentoId) : ICommand<Guid>`.
- **Entrada (DTO):** `LancamentoId` (lançamento de origem).
- **Dependências do handler:** `ILancamentoRepository`, `IDividaAtivaRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Lançamento existe (`ILancamentoRepository.ObterPorIdAsync`), senão `InvalidOperationException("Lançamento não encontrado.")`.
  - Regra de domínio do lançamento: `lancamento.InscreverEmDividaAtiva(hoje)` — só inscreve se em aberto e vencido (invariante do agregado `Lancamento`).
- **Efeito:** calcula `hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`; cria via `DividaAtiva.Inscrever(tenant.TenantId, lancamento.ContribuinteId, lancamento.Id, lancamento.ValorPrincipal, hoje)`; `dividas.Adicionar(divida)`; `SaveChangesAsync`.
- **Pós-condições:** nova `DividaAtiva` em situação `Inscrita`; retorna `divida.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (lançamento inexistente ou não inscritível).
- **Evento de domínio:** `DividaAtivaInscrita(id, contribuinteId, lancamentoId)` (emitido no construtor via factory).

### 5.2 EmitirCda

- **Command:** `EmitirCdaCommand(Guid DividaAtivaId, string NumeroCda) : ICommand`.
- **Entrada (DTO):** `DividaAtivaId`, `NumeroCda`.
- **Dependências do handler:** `IDividaAtivaRepository`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo.
  - Dívida existe (`ObterPorIdAsync`), senão `InvalidOperationException("Dívida ativa não encontrada.")`.
  - Situação == `Inscrita` (I-2); `numeroCda` não vazio (I-3).
- **Efeito:** `divida.EmitirCda(request.NumeroCda)`; `SaveChangesAsync`.
- **Pós-condições:** `NumeroCda` preenchido; situação `CdaEmitida`.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (numeroCda vazio); `InvalidOperationException` (não encontrada ou situação ≠ `Inscrita`).
- **Evento de domínio:** `CdaEmitida(Id, numeroCda)`.

### 5.3 QuitarDivida

- **Command:** `QuitarDividaCommand(Guid DividaAtivaId) : ICommand`.
- **Entrada (DTO):** `DividaAtivaId`.
- **Dependências do handler:** `IDividaAtivaRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Dívida existe, senão `InvalidOperationException("Dívida ativa não encontrada.")`.
  - Situação não encerrada (I-7).
- **Efeito:** `divida.Quitar()`; `SaveChangesAsync`; publica **Integration Event** `ReceitaArrecadadaIntegrationEvent(Guid.NewGuid(), agoraUtc, tenant.TenantId, request.DividaAtivaId, divida.ValorInscrito.Valor, DateOnly.FromDateTime(agoraUtc))` via `publisher.Publish`.
- **Pós-condições:** situação `Quitada`; receita arrecadada publicada aos demais módulos (Finanças).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou já encerrada).
- **Evento de domínio:** `DividaQuitada(Id)`.
- **Evento de integração (publica):** `ReceitaArrecadadaIntegrationEvent`.

> **Comandos de domínio existentes no agregado sem handler de Application nesta versão:** `Protestar()`, `AjuizarExecucaoFiscal()`, `FirmarParcelamento()` (métodos da raiz, expostos para futura orquestração de casos de uso).

---

## 6. Consultas (leitura)

### 6.1 ObterDividasAtivasDoContribuinte

- **Query:** `ObterDividasAtivasDoContribuinteQuery(Guid ContribuinteId) : IQuery<IReadOnlyList<DividaAtivaResumo>>`.
- **Entrada:** `ContribuinteId`.
- **Handler:** `ObterDividasAtivasDoContribuinteHandler(IDividaAtivaRepository dividas)`; chama `dividas.ListarPorContribuinteAsync(new ContribuinteId(request.ContribuinteId), ct)`.
- **Projeção (DTO):** `DividaAtivaResumo(Guid Id, Guid ContribuinteId, decimal ValorInscrito, string Situacao, DateOnly DataInscricao, DateOnly DataPrescricao, string? NumeroCda)`.
  - `ValorInscrito` projetado de `divida.ValorInscrito.Valor`; `Situacao` de `divida.Situacao.ToString()`.
- **Filtros:** por `ContribuinteId`; **sempre tenant-scoped** via Global Query Filter por `TenantId` no DbContext (não há filtro de tenant explícito no handler — é aplicado pela infraestrutura).
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Tributos.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `DividaAtivaInscrita` | `(DividaAtivaId, ContribuinteId, LancamentoId)` | `DividaAtiva.Inscrever` (construtor) |
| `CdaEmitida` | `(DividaAtivaId, string numeroCda)` | `DividaAtiva.EmitirCda` |
| `ParcelamentoFirmado` | `(DividaAtivaId)` | `DividaAtiva.FirmarParcelamento` |
| `DividaQuitada` | `(DividaAtivaId)` | `DividaAtiva.Quitar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Tributos.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `ReceitaArrecadadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid DividaAtivaId, decimal Valor, DateOnly DataArrecadacao)` | `QuitarDividaHandler` |

### Integração (consome)

- Nenhum (esta versão não consome Integration Events de outros módulos no agregado DividaAtiva).

---

## 8. Validações (FluentValidation)

### EmitirCdaValidator (`AbstractValidator<EmitirCdaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `DividaAtivaId` | `NotEmpty()` | (padrão FluentValidation: identificador obrigatório) |
| `NumeroCda` | `NotEmpty()` + `MaximumLength(40)` | (padrão: número da CDA obrigatório, máx. 40 caracteres) |

> `InscreverEmDividaAtivaCommand` e `QuitarDividaCommand` não possuem validador FluentValidation dedicado nesta versão; a proteção é feita por invariantes de domínio e checagens de existência no handler (`ArgumentNullException`, `InvalidOperationException`). `ObterDividasAtivasDoContribuinteQuery` não possui validador.

---

## 9. Persistência (EF Core 8)

- **Schema:** `tributos` (isolado por módulo). **DbContext:** o do módulo Tributos. **Migrations:** por módulo.
- **Tabela:** `DividaAtiva` (raiz de agregado).

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `DividaAtivaId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `ContribuinteId` | `Guid` | conversor de `ContribuinteId` ↔ `Guid`. |
| `LancamentoId` | `Guid` | conversor de `LancamentoId` ↔ `Guid`. |
| `ValorInscrito` | `decimal` | conversor/owned de `ValorMonetario` (campo `Valor`). |
| `DataInscricao` | `date` | `DateOnly`. |
| `NumeroCda` | `nvarchar(40)?` | nulável; comprimento alinhado ao validator (máx. 40). |
| `Situacao` | `int` | enum `SituacaoDividaAtiva` (persistido por valor numérico). |

- **Não persistidas (calculadas):** `DataPrescricao` (derivada de `DataInscricao + AnosPrescricao`).
- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, ContribuinteId)` para a consulta `ListarPorContribuinteAsync`.
  - Recomendado índice único em `(TenantId, NumeroCda)` quando `NumeroCda` não nulo (unicidade da CDA por tenant).
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto Tributos para o `ReceitaArrecadadaIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `DividaAtiva` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas (inclusive `ObterDividasAtivasDoContribuinte`). Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Inscrição/emissão de CDA/quitação: papéis da **Procuradoria/Dívida Ativa** (ex.: `Tributos.DividaAtiva.Gerir`).
  - Consulta de dívidas do contribuinte: papel de **leitura fiscal** (ex.: `Tributos.DividaAtiva.Ler`).
  - Módulo Tributos é **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** vincula `ContribuinteId` (sujeito passivo). Acesso à lista de dívidas de um contribuinte gera **trilha de acesso** (quem leu, quando, por quê). Sem dados sensíveis de saúde/assistência aqui; minimização aplicada na projeção `DividaAtivaResumo`.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Inscrever`, `EmitirCda`, `Protestar`, `AjuizarExecucaoFiscal`, `FirmarParcelamento`, `Quitar`) — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Saída — Finanças (intra-aplicação, via Contracts):** ao quitar, publica `ReceitaArrecadadaIntegrationEvent` (Outbox) para reconhecimento de receita pelo módulo Finanças (Lei 4.320). Idempotente por `EventId`.
- **Origem dos créditos — NFS-e/ADN (passiva):** lançamentos que alimentam a Dívida Ativa podem derivar da ingestão diária de NFS-e do Ambiente de Dados Nacional (Worker `NfseSync`). Não emitimos nem assinamos NFS-e.
- **Cobrança externa (previsto, fora do escopo desta versão de código):**
  - **Protesto (Lei 9.492/97):** integração com Central de Protesto/cartório quando `Protestar` for orquestrado por caso de uso. ACL + Polly (retry + circuit breaker) + idempotência.
  - **Execução Fiscal (Lei 6.830/80):** geração de petição/CDA para ajuizamento. ACL + idempotência.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Inscrição de lançamento vencido**
- **Dado** um lançamento em aberto e vencido na data de hoje
- **Quando** executo `InscreverEmDividaAtivaCommand(lancamentoId)`
- **Então** é criada uma `DividaAtiva` em situação `Inscrita`, com `DataPrescricao = DataInscricao + 5 anos`, e o evento `DividaAtivaInscrita` é emitido.

**Cenário 2 — Lançamento inexistente**
- **Dado** um `LancamentoId` que não existe
- **Quando** executo `InscreverEmDividaAtivaCommand`
- **Então** ocorre `InvalidOperationException("Lançamento não encontrado.")`.

**Cenário 3 — Emissão de CDA a partir de Inscrita**
- **Dado** uma dívida em situação `Inscrita`
- **Quando** executo `EmitirCdaCommand(dividaId, "CDA-2026-0001")`
- **Então** `NumeroCda = "CDA-2026-0001"`, situação = `CdaEmitida` e o evento `CdaEmitida` é emitido.

**Cenário 4 — Emissão de CDA fora de Inscrita**
- **Dado** uma dívida em situação `CdaEmitida` (ou qualquer ≠ `Inscrita`)
- **Quando** executo `EmitirCdaCommand`
- **Então** ocorre `InvalidOperationException` indicando que a CDA só pode ser emitida para dívida recém-inscrita.

**Cenário 5 — CDA com número vazio**
- **Dado** uma dívida `Inscrita`
- **Quando** executo `EmitirCdaCommand(dividaId, "")`
- **Então** a validação rejeita (`NumeroCda` `NotEmpty`) / `ArgumentException` no domínio.

**Cenário 6 — Protesto exige CDA emitida**
- **Dado** uma dívida em situação `Inscrita`
- **Quando** chamo `Protestar()`
- **Então** ocorre `InvalidOperationException("O protesto requer CDA emitida.")`.

**Cenário 7 — Protesto válido**
- **Dado** uma dívida `CdaEmitida` e exigível
- **Quando** chamo `Protestar()`
- **Então** situação = `Protestada`.

**Cenário 8 — Execução fiscal a partir de CdaEmitida/Protestada**
- **Dado** uma dívida `CdaEmitida` (ou `Protestada`) e exigível
- **Quando** chamo `AjuizarExecucaoFiscal()`
- **Então** situação = `EmExecucaoFiscal`.

**Cenário 9 — Execução fiscal sem CDA**
- **Dado** uma dívida `Inscrita`
- **Quando** chamo `AjuizarExecucaoFiscal()`
- **Então** ocorre `InvalidOperationException` (requer CDA emitida/protestada).

**Cenário 10 — Parcelamento suspende exigibilidade**
- **Dado** uma dívida exigível (ex.: `CdaEmitida`)
- **Quando** chamo `FirmarParcelamento()`
- **Então** situação = `Parcelada` e o evento `ParcelamentoFirmado` é emitido; novos atos de cobrança passam a falhar por não exigibilidade.

**Cenário 11 — Atos sobre dívida não exigível**
- **Dado** uma dívida `Parcelada` (ou `Quitada`/`Cancelada`)
- **Quando** chamo `Protestar()` / `AjuizarExecucaoFiscal()` / `FirmarParcelamento()`
- **Então** ocorre `InvalidOperationException("A dívida não está exigível. ...")`.

**Cenário 12 — Quitação publica receita**
- **Dado** uma dívida em qualquer situação não encerrada
- **Quando** executo `QuitarDividaCommand(dividaId)`
- **Então** situação = `Quitada`, o evento `DividaQuitada` é emitido e `ReceitaArrecadadaIntegrationEvent` é publicado com `Valor = ValorInscrito.Valor` e `DataArrecadacao = hoje`.

**Cenário 13 — Quitação de dívida já encerrada**
- **Dado** uma dívida `Quitada` (ou `Cancelada`)
- **Quando** executo `QuitarDividaCommand`
- **Então** ocorre `InvalidOperationException("Dívida já encerrada. ...")`.

**Cenário 14 — Consulta tenant-scoped**
- **Dado** dívidas de um contribuinte no tenant A e dívidas de outro tenant B
- **Quando** executo `ObterDividasAtivasDoContribuinteQuery(contribuinteId)` no contexto do tenant A
- **Então** retornam **apenas** as dívidas do tenant A, projetadas em `DividaAtivaResumo`.

**Cenário 15 — Prescrição (CTN art. 174)**
- **Dado** uma dívida `Inscrita` cuja `DataPrescricao` já passou
- **Quando** avalio `EstaPrescrita(hoje)` com `hoje > DataPrescricao`
- **Então** o resultado é `true`; se a dívida estiver `Quitada`/`Parcelada`/`Cancelada`, o resultado é `false`.

---

## 13. Casos de Borda

- **B-1.** `valorInscrito` nulo em `Inscrever` ⇒ `ArgumentNullException` (I-10).
- **B-2.** `numeroCda` nulo/vazio/espaços em `EmitirCda` ⇒ `ArgumentException` (I-3); via command, `MaximumLength(40)` no validator.
- **B-3.** `NumeroCda` com 40 caracteres é aceito; 41+ é rejeitado pelo validator.
- **B-4.** Emitir CDA duas vezes ⇒ a 2ª falha (situação já `CdaEmitida` ≠ `Inscrita`).
- **B-5.** Protestar dívida `EmExecucaoFiscal` ⇒ falha (situação ≠ `CdaEmitida`), embora exigível.
- **B-6.** Ajuizar execução fiscal sobre dívida já `EmExecucaoFiscal` ⇒ falha (situação ∉ {`CdaEmitida`,`Protestada`}).
- **B-7.** `FirmarParcelamento` sobre dívida `Inscrita` (sem CDA) ⇒ permitido (apenas exigência de exigibilidade).
- **B-8.** Parcelar dívida já `Parcelada` ⇒ falha por não exigibilidade (`GarantirExigivel`).
- **B-9.** Quitar dívida `Parcelada` ⇒ permitido (parcelamento não é estado encerrado para `Quitar`); resulta em `Quitada` + receita publicada.
- **B-10.** `EstaPrescrita` com `hoje == DataPrescricao` ⇒ `false` (estritamente `hoje > DataPrescricao`).
- **B-11.** Quitação de dívida prescrita mas não encerrada ⇒ ainda permitida pelo agregado (a prescrição não bloqueia `Quitar` nesta versão); decisão de cobrar/extinguir é externa ao agregado.
- **B-12.** `DividaAtivaId`/`ContribuinteId` vazios na consulta/comando ⇒ `EmitirCdaValidator` exige `DividaAtivaId` `NotEmpty`; demais dependem da existência no repositório.
- **B-13.** Estado `Cancelada` não é atingível por comando nesta versão (sem método de transição); bloqueia `Quitar` e atos de cobrança caso seja definido por outra via.
- **B-14.** `ReceitaArrecadadaIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — retrofit fiel do código existente (`DividaAtiva.cs`, `InscreverEmDividaAtiva`, `EmitirCda`, `QuitarDivida`, `ObterDividasAtivasDoContribuinte`). |
| 2.0.0 | 2026-06-23 | M6 parte 4 — DA→CDA→cobrança/protesto→execução→prescrição. Inscrição enriquecida (origem/natureza, fundamento legal, valor originário, constituição definitiva, número de inscrição, regra de encargos parametrizável multa/juros/correção — lei municipal). CDA com requisitos legais obrigatórios (LEF art. 2º §5º I–VI / CTN art. 202) via VO `CertidaoDividaAtiva` que RECUSA se faltar requisito (`CdaRequisitoAusenteException`). Protesto extrajudicial (Lei 9.492/97; STF ADI 5.135; STJ REsp 1.895.557) como ato auditável `RemessaProtesto` atrás de ACL `IProtestoCraGateway` versionada por CRA (`GerarRemessaProtesto`/`ProcessarRetornoProtesto`). Gancho de execução fiscal (`AjuizarExecucaoFiscal`). Prescrição real (CTN art. 174): termo inicial = constituição definitiva ou última interrupção (`InterromperPrescricao`, parcelamento), prazo parametrizável; `AvaliarPrescricaoDivida` sinaliza prescrita e apura encargos. Renomeia `ValorInscrito`→`ValorOriginario`. // TODO(validar-oficial): leiaute CRA-RS, índice de correção do CTM, art. 40 LEF pós-Lei 14.195/2021. |

<!-- manifest
commands: InscreverEmDividaAtiva, EmitirCda, QuitarDivida, GerarRemessaProtesto, ProcessarRetornoProtesto, AjuizarExecucaoFiscal
queries: ObterDividasAtivasDoContribuinte, AvaliarPrescricaoDivida
domainEvents: DividaAtivaInscrita, CdaEmitida, ParcelamentoFirmado, DividaQuitada, RemessaProtestoGerada, RetornoProtestoProcessado, ExecucaoFiscalAjuizada, PrescricaoInterrompida
integrationEventsPublished: ReceitaArrecadadaIntegrationEvent, PosicaoDividaAtivaIntegrationEvent
integrationEventsConsumed: 
-->
