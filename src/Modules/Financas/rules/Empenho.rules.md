---
modulo: Financas
agregado: Empenho
contexto: Financas (Despesa Pública — ciclo Empenho → Liquidação → Pagamento)
poder: Ambos
schema: financas
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "Lei 4.320/64, art. 58 a 65 (estágios da despesa: empenho, liquidação e pagamento)"
  - "Lei 4.320/64, art. 60 (tipos de empenho: ordinário, estimativo e global)"
  - "Lei 4.320/64, art. 62 (pagamento exige liquidação prévia)"
  - "Lei 4.320/64, art. 63 (liquidação da despesa)"
  - "LC 101/2000 (Lei de Responsabilidade Fiscal — LRF)"
---

## 1. Linguagem Ubíqua

Identificadores **sem acento** e **vinculantes** (devem aparecer no código exatamente assim).

| Termo (identificador-no-código) | Definição |
|---|---|
| Empenho (`Empenho`) | 1º estágio da despesa pública (Lei 4.320/64). Ato emanado de autoridade competente que cria para o ente a obrigação de pagamento, pendente ou não de implemento de condição. Raiz de agregado. |
| Empenhar (`Empenhar`) | Ato/fábrica que cria o empenho na situação `Empenhado` (1º estágio). |
| Liquidar (`Liquidar`) | 2º estágio da despesa: verificação do direito adquirido pelo credor. Move `Empenhado` → `Liquidado`. |
| Pagar (`Pagar`) | 3º estágio da despesa: quitação. Exige liquidação prévia. Move `Liquidado` → `Pago`. |
| Anular (`Anular`) | Cancelamento do empenho. Vedado se já `Pago`. |
| Credor (`Credor`) | Pessoa física ou jurídica titular do direito ao recebimento. |
| Numero do empenho (`Numero`) | Número identificador do empenho no exercício. |
| Valor empenhado (`Valor`) | Montante reservado/comprometido, expresso como `ValorMonetario`. |
| Tipo de empenho (`TipoEmpenho`) | Classificação Lei 4.320/64 art. 60: `Ordinario`, `Estimativo` ou `Global`. |
| Situacao (`Situacao`) | Estágio atual da despesa (`SituacaoEmpenho`). |
| Exercicio (`Exercicio`) | Exercício orçamentário (ano) ao qual o empenho pertence. |
| Data do empenho (`DataEmpenho`) | Data em que o empenho foi emitido (`DateOnly`). |
| Tenant (`TenantId`) | Ente público (Prefeitura ou Câmara) dono do registro. |
| Despesa empenhada (`DespesaEmpenhada`) | Evento de domínio emitido na criação do empenho. |
| Despesa liquidada (`DespesaLiquidada`) | Evento de domínio emitido na liquidação. |
| Despesa paga (`DespesaPaga`) | Evento de domínio emitido no pagamento. |

## 2. Modelo

- **Identidade:** `EmpenhoId` — `readonly record struct EmpenhoId(Guid Value)`; fábrica `EmpenhoId.New()`.
- **Raiz de agregado:** `Empenho : AggregateRoot<EmpenhoId>, IMustHaveTenant`. `sealed`. Construtores privados; instância criada pela fábrica estática `Empenhar`.

### Propriedades (todas com setter privado)

| Propriedade | Tipo (VO/enum/primitivo) | Descrição |
|---|---|---|
| `TenantId` | `Guid` (primitivo) | Tenant (ente público) dono do registro. |
| `Numero` | `string` (primitivo) | Número do empenho. |
| `Credor` | `string` (primitivo) | Credor. |
| `Valor` | `ValorMonetario` (VO) | Valor empenhado. |
| `TipoEmpenho` | `TipoEmpenho` (enum) | Tipo de empenho. |
| `Exercicio` | `int` (primitivo) | Exercício orçamentário. |
| `DataEmpenho` | `DateOnly` (primitivo) | Data do empenho. |
| `Situacao` | `SituacaoEmpenho` (enum) | Situação (estágio) atual. |

### Value Objects

- `ValorMonetario` (VO compartilhado em `Domain.ValueObjects`) — encapsula o montante; criado via `ValorMonetario.De(decimal)`; expõe a propriedade `Valor` (decimal subjacente).

### Enums (com valores)

`TipoEmpenho` (Lei 4.320/64, art. 60):

| Valor | Nome | Significado |
|---|---|---|
| 1 | `Ordinario` | Valor fixo e pagamento único. |
| 2 | `Estimativo` | Montante indeterminado (água, energia, combustível). |
| 3 | `Global` | Valor conhecido sujeito a parcelamento. |

`SituacaoEmpenho` (estágios da despesa):

| Valor | Nome | Significado |
|---|---|---|
| 1 | `Empenhado` | Empenhado (1º estágio). |
| 2 | `Liquidado` | Liquidado (2º estágio). |
| 3 | `Pago` | Pago (3º estágio). |
| 4 | `Anulado` | Anulado. |

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Empenho recém-criado nasce sempre na situação `Empenhado` (1º estágio da despesa — Lei 4.320/64).
- **I-2.** `Numero` é obrigatório: não pode ser nulo, vazio ou somente espaços em branco (`ArgumentException.ThrowIfNullOrWhiteSpace`).
- **I-3.** `Credor` é obrigatório: não pode ser nulo, vazio ou somente espaços em branco (`ArgumentException.ThrowIfNullOrWhiteSpace`).
- **I-4.** `Valor` é obrigatório e não nulo (`ArgumentNullException.ThrowIfNull`).
- **I-5.** A liquidação só é permitida quando a situação atual é `Empenhado`; caso contrário lança `InvalidOperationException` ("Apenas despesa empenhada pode ser liquidada. Situação atual: {Situacao}.").
- **I-6.** O pagamento exige liquidação prévia (Lei 4.320/64, art. 62): só é permitido quando a situação é `Liquidado`; caso contrário lança `InvalidOperationException` ("O pagamento exige liquidação prévia (Lei 4.320/64). Situação atual: {Situacao}.").
- **I-7.** A anulação é vedada quando a situação é `Pago`; nesse caso lança `InvalidOperationException` ("Não é possível anular despesa já paga.").
- **I-8.** O ciclo de estágios é estritamente sequencial e sem saltos: `Empenhado` → `Liquidado` → `Pago`. Não há transição direta de `Empenhado` para `Pago`.
- **I-9.** Toda criação de empenho emite o evento de domínio `DespesaEmpenhada(Id, Valor.Valor)`.
- **I-10.** A liquidação emite o evento de domínio `DespesaLiquidada(Id)` e o pagamento emite `DespesaPaga(Id)`.
- **I-11.** Toda raiz de agregado carrega `TenantId` (`IMustHaveTenant`); o registro pertence a um único tenant.

## 4. Máquina de Estados

| Origem | Comando (método de domínio) | Destino | Guarda | Evento emitido |
|---|---|---|---|---|
| (inexistente) | `Empenhar` | `Empenhado` | `Numero`/`Credor` não vazios; `Valor` não nulo | `DespesaEmpenhada` |
| `Empenhado` | `Liquidar` | `Liquidado` | situação atual = `Empenhado` | `DespesaLiquidada` |
| `Liquidado` | `Pagar` | `Pago` | situação atual = `Liquidado` (liquidação prévia) | `DespesaPaga` |
| `Empenhado` \| `Liquidado` | `Anular` | `Anulado` | situação atual ≠ `Pago` | — (nenhum evento) |
| `Pago` | `Anular` | (erro) | — | — (lança `InvalidOperationException`) |

> Observações: não existem transições a partir de `Pago` nem de `Anulado` no código atual (estados terminais). `Anular` não emite evento de domínio.

## 5. Comandos (escrita)

### 5.1 `EmpenharCommand`
- **Entrada (DTO):** `EmpenharCommand(string Numero, string Credor, decimal Valor, TipoEmpenho TipoEmpenho, int Exercicio, DateOnly DataEmpenho) : ICommand<Guid>`.
- **Pré-condições:** validação FluentValidation (ver §8); `request` não nulo; tenant resolvido por `ITenantContext`.
- **Efeito:** cria `Empenho` via `Empenho.Empenhar(tenant.TenantId, Numero, Credor, ValorMonetario.De(Valor), TipoEmpenho, Exercicio, DataEmpenho)`; persiste via `IEmpenhoRepository.Adicionar` + `IUnitOfWork.SaveChangesAsync`.
- **Pós-condições:** empenho persistido na situação `Empenhado`; retorna `Guid` (`empenho.Id.Value`).
- **Exceções:** `ValidationException` (pipeline) se validação falhar; `ArgumentException` se `Numero`/`Credor` vazios; `ArgumentNullException` se `Valor`/`request` nulos.
- **Evento:** `DespesaEmpenhada(EmpenhoId, valor)`.
- **Handler:** `EmpenharHandler(IEmpenhoRepository, IUnitOfWork, ITenantContext) : ICommandHandler<EmpenharCommand, Guid>`.

### 5.2 `LiquidarEmpenhoCommand`
- **Entrada (DTO):** `LiquidarEmpenhoCommand(Guid EmpenhoId) : ICommand`.
- **Pré-condições:** `request` não nulo; empenho existente; situação = `Empenhado`.
- **Efeito:** carrega via `IEmpenhoRepository.ObterPorIdAsync(new EmpenhoId(...))`; invoca `empenho.Liquidar()`; persiste via `IUnitOfWork.SaveChangesAsync`.
- **Pós-condições:** situação passa a `Liquidado`.
- **Exceções:** `InvalidOperationException` "Empenho não encontrado." se inexistente; `InvalidOperationException` (I-5) se situação ≠ `Empenhado`; `ArgumentNullException` se `request` nulo.
- **Evento:** `DespesaLiquidada(EmpenhoId)`.
- **Handler:** `LiquidarEmpenhoHandler(IEmpenhoRepository, IUnitOfWork) : ICommandHandler<LiquidarEmpenhoCommand>`.

### 5.3 `PagarEmpenhoCommand`
- **Entrada (DTO):** `PagarEmpenhoCommand(Guid EmpenhoId) : ICommand`.
- **Pré-condições:** `request` não nulo; empenho existente; situação = `Liquidado` (liquidação prévia — Lei 4.320/64, art. 62).
- **Efeito:** carrega via `IEmpenhoRepository.ObterPorIdAsync(new EmpenhoId(...))`; invoca `empenho.Pagar()`; persiste via `IUnitOfWork.SaveChangesAsync`.
- **Pós-condições:** situação passa a `Pago`.
- **Exceções:** `InvalidOperationException` "Empenho não encontrado." se inexistente; `InvalidOperationException` (I-6) se situação ≠ `Liquidado`; `ArgumentNullException` se `request` nulo.
- **Evento:** `DespesaPaga(EmpenhoId)`.
- **Handler:** `PagarEmpenhoHandler(IEmpenhoRepository, IUnitOfWork) : ICommandHandler<PagarEmpenhoCommand>`.

> Nota: o método de domínio `Empenho.Anular()` existe e é especificado na máquina de estados/invariantes, porém **não há comando/handler de aplicação** correspondente no código atual; não é exposto como caso de uso.

## 6. Consultas (leitura)

### 6.1 `ObterEmpenhoQuery`
- **Nome:** `ObterEmpenhoQuery(Guid EmpenhoId) : IQuery<EmpenhoResumo?>`.
- **Entrada:** `EmpenhoId` (Guid).
- **Projeção (DTO):** `EmpenhoResumo(Guid Id, string Numero, string Credor, decimal Valor, string Situacao, int Exercicio)`.
- **Filtros:** busca por id via `IEmpenhoRepository.ObterPorIdAsync(new EmpenhoId(...))`; **tenant-scoped** pelo Global Query Filter por `TenantId` aplicado no DbContext do módulo.
- **Retorno:** `null` quando inexistente; caso contrário mapeia `Id.Value`, `Numero`, `Credor`, `Valor.Valor`, `Situacao.ToString()`, `Exercicio`.
- **Handler:** `ObterEmpenhoHandler(IEmpenhoRepository) : IQueryHandler<ObterEmpenhoQuery, EmpenhoResumo?>`.

## 7. Eventos

- **Domínio** (em `Domain.Events`, emitidos via `RaiseDomainEvent`):
  - `DespesaEmpenhada(EmpenhoId, decimal valor)` — na criação.
  - `DespesaLiquidada(EmpenhoId)` — na liquidação.
  - `DespesaPaga(EmpenhoId)` — no pagamento.
- **Integração** (publica/consome via `*.Contracts`): nenhum no código atual.

## 8. Validações (FluentValidation)

`EmpenharValidator : AbstractValidator<EmpenharCommand>`:

| Campo | Regra | Mensagem |
|---|---|---|
| `Numero` | `NotEmpty()` `MaximumLength(30)` | padrão FluentValidation (obrigatório; máx. 30 caracteres) |
| `Credor` | `NotEmpty()` `MaximumLength(200)` | padrão FluentValidation (obrigatório; máx. 200 caracteres) |
| `Valor` | `GreaterThan(0m)` | padrão FluentValidation (deve ser maior que zero) |
| `TipoEmpenho` | `IsInEnum()` | padrão FluentValidation (valor de enum válido) |
| `Exercicio` | `GreaterThanOrEqualTo(2000)` | padrão FluentValidation (≥ 2000) |

> `LiquidarEmpenhoCommand` e `PagarEmpenhoCommand` não possuem validador dedicado no código atual; as guardas são impostas pelas invariantes do domínio (I-5/I-6).

## 9. Persistência

- **Contexto:** DbContext do módulo `Financas`, **schema `financas`** (um DbContext por módulo, schema isolado).
- **Repositório:** `IEmpenhoRepository` com `Adicionar(Empenho)` e `ObterPorIdAsync(EmpenhoId, CancellationToken)`.
- **Tabela:** `Empenho` (agregado), colunas correspondentes às propriedades:

| Coluna | Origem | Conversor |
|---|---|---|
| `Id` | `EmpenhoId` | conversor de `EmpenhoId` ⇄ `Guid` |
| `TenantId` | `Guid` | — |
| `Numero` | `string` | — (máx. 30) |
| `Credor` | `string` | — (máx. 200) |
| `Valor` | `ValorMonetario` | conversor/owned de `ValorMonetario` ⇄ `decimal` |
| `TipoEmpenho` | enum `TipoEmpenho` | inteiro (1/2/3) |
| `Exercicio` | `int` | — |
| `DataEmpenho` | `DateOnly` | — |
| `Situacao` | enum `SituacaoEmpenho` | inteiro (1/2/3/4) |

- **Índices:** índice por `TenantId`; índice por `Exercicio`. Recomenda-se único composto `(TenantId, Exercicio, Numero)` para garantir unicidade do número de empenho por exercício/ente (não evidenciado nos fontes lidos; confirmar na configuração EF).
- **Outbox:** tabela Outbox por contexto; eventos de domínio despachados in-process via MediatR.

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Empenho` implementa `IMustHaveTenant`; `TenantId` carimbado na inserção pelo `SaveChangesInterceptor`; **Global Query Filter** por `TenantId` no DbContext. Gravação cross-tenant lança exceção. `EmpenharHandler` obtém o tenant de `ITenantContext` (JWT).
- **RBAC:** comandos/consultas sob *policy-based authorization* (negar por padrão). Políticas sugeridas (a vincular nos endpoints): `financas:empenho:empenhar`, `financas:empenho:liquidar`, `financas:empenho:pagar`, `financas:empenho:ler`.
- **LGPD:** o agregado não contém dados pessoais sensíveis (categorias especiais); `Credor` é dado cadastral de contraparte. Sem campos de saúde/assistência. Minimização aplicada.
- **Auditoria:** toda mutação de estado (criação, liquidação, pagamento) gera trilha imutável (antes/depois, usuário, IP, timestamp) via `AuditSaveChangesInterceptor`, para o Tribunal de Contas. Os eventos de domínio (`DespesaEmpenhada`, `DespesaLiquidada`, `DespesaPaga`) registram a progressão dos estágios.

## 11. Integrações Governamentais

- Nenhuma integração externa direta neste agregado no código atual.
- **Aplicável (LRF/Lei 4.320/64):** os estágios da despesa alimentam, por outros agregados/módulos, remessas ao **TCE-RS (SIAPC/PAD)** e **SICONFI (MSC)** e o módulo **Transparencia** (LAI). Quando houver integração, deve ser **idempotente, resiliente (Polly: retry + circuit breaker)** e atrás de **Anti-Corruption Layer**, com publicação via **Outbox**. (Não há contrato de integração publicado por este agregado nesta versão.)

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Empenhar despesa**
- **Dado** um tenant válido e dados de empenho (`Numero`="2026NE000123", `Credor`="Fornecedor X", `Valor`=1000,00, `TipoEmpenho`=`Ordinario`, `Exercicio`=2026)
- **Quando** `EmpenharCommand` é executado
- **Então** o empenho é criado na situação `Empenhado`, o evento `DespesaEmpenhada` é emitido e o handler retorna o `Guid` do empenho.

**Cenário 2 — Liquidar despesa empenhada**
- **Dado** um empenho na situação `Empenhado`
- **Quando** `LiquidarEmpenhoCommand` é executado
- **Então** a situação passa a `Liquidado` e o evento `DespesaLiquidada` é emitido.

**Cenário 3 — Pagar despesa liquidada**
- **Dado** um empenho na situação `Liquidado`
- **Quando** `PagarEmpenhoCommand` é executado
- **Então** a situação passa a `Pago` e o evento `DespesaPaga` é emitido.

**Cenário 4 — Pagar sem liquidação prévia (vedado)**
- **Dado** um empenho na situação `Empenhado`
- **Quando** `PagarEmpenhoCommand` é executado
- **Então** lança `InvalidOperationException` ("O pagamento exige liquidação prévia (Lei 4.320/64). ...") e a situação permanece `Empenhado`.

**Cenário 5 — Liquidar despesa não empenhada (vedado)**
- **Dado** um empenho na situação `Liquidado` (ou `Pago`/`Anulado`)
- **Quando** `LiquidarEmpenhoCommand` é executado
- **Então** lança `InvalidOperationException` ("Apenas despesa empenhada pode ser liquidada. ...").

**Cenário 6 — Anular despesa já paga (vedado)**
- **Dado** um empenho na situação `Pago`
- **Quando** `Anular()` é invocado
- **Então** lança `InvalidOperationException` ("Não é possível anular despesa já paga.").

**Cenário 7 — Obter empenho inexistente**
- **Dado** um `EmpenhoId` que não existe (ou pertence a outro tenant)
- **Quando** `ObterEmpenhoQuery` é executado
- **Então** retorna `null`.

**Cenário 8 — Comando sobre empenho inexistente**
- **Dado** um `EmpenhoId` inexistente
- **Quando** `LiquidarEmpenhoCommand` (ou `PagarEmpenhoCommand`) é executado
- **Então** lança `InvalidOperationException` ("Empenho não encontrado.").

## 13. Casos de Borda

Cada item vira teste.

- **B-1.** `Numero` em branco/espaços → `ArgumentException` na fábrica (I-2).
- **B-2.** `Credor` em branco/espaços → `ArgumentException` na fábrica (I-3).
- **B-3.** `Valor` nulo → `ArgumentNullException` na fábrica (I-4).
- **B-4.** `Valor` ≤ 0 → rejeitado pelo `EmpenharValidator` (`GreaterThan(0m)`) antes de chegar ao domínio.
- **B-5.** `Numero` com mais de 30 caracteres → rejeitado pelo validador (`MaximumLength(30)`).
- **B-6.** `Credor` com mais de 200 caracteres → rejeitado pelo validador (`MaximumLength(200)`).
- **B-7.** `TipoEmpenho` fora do enum → rejeitado pelo validador (`IsInEnum`).
- **B-8.** `Exercicio` < 2000 → rejeitado pelo validador (`GreaterThanOrEqualTo(2000)`).
- **B-9.** Pular estágio (`Empenhado` → `Pagar`) → bloqueado (I-6/I-8).
- **B-10.** Liquidar duas vezes (segunda chamada com situação `Liquidado`) → `InvalidOperationException` (I-5).
- **B-11.** Pagar duas vezes (segunda chamada com situação `Pago`) → `InvalidOperationException` (I-6).
- **B-12.** Anular empenho `Empenhado` ou `Liquidado` → permitido, situação vira `Anulado`, sem evento.
- **B-13.** Operar sobre empenho de outro tenant → não encontrado pelo Global Query Filter (comportamento equivalente a inexistente).
- **B-14.** `request` nulo em qualquer handler → `ArgumentNullException` (guard `ThrowIfNull`).

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — retrofit fiel ao código existente do agregado `Empenho` (Domain `Empenho.cs`; Application `Empenhar.cs`, `MovimentarEmpenho.cs`, `ObterEmpenho.cs`). Ciclo Empenho → Liquidação → Pagamento (Lei 4.320/64) e LRF (LC 101/2000). |
| 2.0.0 | 2026-06-22 | Sincronização do manifesto Rules-as-Code com o código rico do módulo Finanças: agregados `DotacaoOrcamentaria`, `Empenho` (saldos: empenhado/liquidado/pago com controle por valor), `Liquidacao`, `OrdemDePagamento` e `RestoAPagar`. Manifesto consolidado dos comandos, consultas, eventos de domínio e integration events do módulo. **TODO(revisao-contabil):** a prosa das seções 1–13 ainda descreve o modelo simplificado anterior (Situacao `Pago`, `Credor` como string) e deve ser reescrita para refletir os saldos por valor (não-empenhar-acima-da-dotação, não-liquidar-acima-do-empenho, não-pagar-acima-do-liquidado). |

<!-- manifest
commands: Empenhar, AnularEmpenho, LiquidarDespesa, EstornarLiquidacao, EmitirOrdemDePagamento, EfetuarPagamento, CancelarOrdemDePagamento, CriarDotacao, ReforcarDotacao, AnularCreditoDotacao, EncerrarExercicio, PagarRestoAPagar, CancelarRestoAPagar
queries: ObterEmpenho, ListarEmpenhosPorDotacao, ObterDotacao, ListarDotacoesPorExercicio, ObterLiquidacao, ListarLiquidacoesPorEmpenho, ObterOrdemDePagamento, ListarRestosAPagarPorExercicio
domainEvents: DotacaoCriada, CreditoReforcado, CreditoAnulado, EmpenhoEmitido, EmpenhoAnulado, DespesaLiquidada, LiquidacaoEstornada, PagamentoEfetuado, EmpenhoInscritoEmRestosAPagar, RestoAPagarLiquidado, RestoAPagarPago, RestoAPagarInscrito
integrationEventsPublished: DespesaEmpenhadaIntegrationEvent, PagamentoEfetuadoIntegrationEvent, DespesaLiquidadaIntegrationEvent, DotacaoOrcamentariaPublicadaIntegrationEvent
integrationEventsConsumed: 
-->
