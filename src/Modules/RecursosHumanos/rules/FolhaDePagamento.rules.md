---
modulo: RecursosHumanos
agregado: FolhaDePagamento
contexto: RecursosHumanos (folha de pagamento por competência)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 37, XI (teto remuneratório / abate-teto)", "EC 103/2019 (RPPS S-1202 / RGPS S-1200)", "eSocial — S-1200, S-1202, S-1210, S-1299, S-1010 (rubricas), S-1005 (estabelecimentos), totalizadores S-5001/5002/5003", "DCTFWeb — IN RFB 2.005/2021 (obrigatória para entes públicos desde jul/2022)"]
---

# FolhaDePagamento — Regras-as-Code (Rules-as-Code)

> Folha de pagamento de um ente público em uma **competência** (`AAAA-MM`): apura proventos e
> descontos por servidor, aplica **abate-teto** (CF art. 37, XI), separa remuneração **RPPS
> (S-1202)** de **RGPS (S-1200)** (EC 103/2019), e ao fechar (S-1299) gera **pagamentos (S-1210)**,
> **totalizadores (S-5001/5002/5003)** e a **DCTFWeb** (IN RFB 2.005/2021). Este arquivo é
> **normativo e versionado**; o código (`FolhaDePagamento.cs`, handlers, validators, EF config,
> testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Folha de Pagamento (`FolhaDePagamento`) | Conjunto de eventos de remuneração de uma competência. Raiz de agregado. |
| Competência (`Competencia` : `Competencia`) | Mês/ano de referência da folha (`AAAA-MM`). |
| Evento de Folha (`EventoFolha`) | Entidade-filha: lançamento de provento ou desconto de um servidor. |
| Provento (`TipoEvento.Provento`) | Verba de natureza creditícia na folha. |
| Desconto (`TipoEvento.Desconto`) | Verba de natureza debitória na folha. |
| Consignação (`Consignacao`) | Desconto autorizado por terceiro (empréstimo, mensalidade). |
| Rubrica (`Rubrica` : `Rubrica`) | Código que classifica provento/desconto e suas incidências (S-1010). |
| Base de Cálculo (`BaseCalculo` : `BaseCalculo`) | Valor de incidência usado no cálculo de uma rubrica. |
| Líquido a Pagar (`LiquidoAPagar` : `LiquidoAPagar`) | Total de proventos menos total de descontos por servidor. |
| Teto Remuneratório (`TetoRemuneratorio`) | Limite constitucional aplicado via abate-teto (CF art. 37, XI). |
| Abate-Teto (`AplicarAbateTeto` / rubrica de abate-teto) | Rubrica de desconto que reduz o líquido ao limite constitucional. |
| Cálculo (`Calcular` / `FolhaCalculada`) | Apuração de proventos/descontos/líquido de todos os eventos. |
| Fechamento (`Fechar` / `FolhaFechada`) | Encerramento da competência; dispara S-1299, S-1210, totalizadores e DCTFWeb. |
| Pagamento (`EfetuarPagamento` / `PagamentoEfetuado`) | Liquidação financeira do líquido (S-1210). |
| Tenant (`TenantId`) | Ente público (Prefeitura ou Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoFolha`) | Estado atual da folha na competência. |

---

## 2. Modelo

- **Identidade:** `FolhaDePagamentoId` — `readonly record struct FolhaDePagamentoId(Guid Value)`; fábrica `FolhaDePagamentoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `FolhaDePagamento : AggregateRoot<FolhaDePagamentoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Abrir(...)` (uma folha por competência por tenant).
- **Entidade-filha:** `EventoFolha` (coleção `IReadOnlyCollection<EventoFolha> Eventos`), exposta somente através da raiz.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Competencia` | `Competencia` (VO) | Mês/ano de referência (`AAAA-MM`). | `private set` |
| `Situacao` | `SituacaoFolha` | Situação atual (aberta/calculada/fechada/paga). | `private set` |
| `DataCalculo` | `DateOnly?` | Data do último cálculo (nula antes de calcular). | `private set` |
| `DataFechamento` | `DateOnly?` | Data do fechamento (nula antes de fechar). | `private set` |
| `DataPagamento` | `DateOnly?` | Data do pagamento (nula antes de pagar). | `private set` |
| `TotalProventos` | `decimal` (calculado) | Soma dos proventos após cálculo. | `private set` |
| `TotalDescontos` | `decimal` (calculado) | Soma dos descontos (inclui abate-teto) após cálculo. | `private set` |
| `TotalLiquido` | `LiquidoAPagar` (VO, calculado) | `TotalProventos − TotalDescontos`. | `private set` |
| `Eventos` | `IReadOnlyCollection<EventoFolha>` | Proventos/descontos por servidor. | coleção encapsulada |

### Entidade-filha `EventoFolha`

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `Guid` | Identidade local do evento. |
| `ServidorId` | `Guid` | Servidor a que o evento pertence. |
| `Rubrica` | `Rubrica` (VO) | Código/classificação da verba (S-1010). |
| `Tipo` | `TipoEvento` (enum) | Provento ou Desconto. |
| `BaseCalculo` | `BaseCalculo` (VO) | Valor de incidência. |
| `Valor` | `decimal` | Valor apurado da verba. |
| `RegimePrevidenciario` | `RegimePrevidenciario` (enum) | RPPS (S-1202) ou RGPS (S-1200) — herdado do servidor. |

### Constantes

- `MesesPrazoEnvioPeriodico` — eventos periódicos são transmitidos **até o dia 15** do mês seguinte ao da competência (parametrizável por tenant, não *hardcoded*).

### Value Objects (referenciados)

- `Competencia` — `record struct`/VO com `Ano : int` e `Mes : int`; formato `AAAA-MM`; única por tenant.
- `Rubrica` — VO com `Codigo : string` e metadados de incidência; deve existir e estar vigente em S-1010 na competência.
- `BaseCalculo` — VO com `Valor : decimal` (base de incidência).
- `LiquidoAPagar` — VO com `Valor : decimal`; não negativo após abate-teto.

### Enum `TipoEvento`

| Valor | Numérico | Descrição |
|---|---|---|
| `Provento` | 1 | Verba creditícia. |
| `Desconto` | 2 | Verba debitória (inclui abate-teto e consignações). |

### Enum `SituacaoFolha`

| Valor | Numérico | Descrição |
|---|---|---|
| `Aberta` | 1 | Folha aberta, aceitando lançamento de eventos (estado inicial). |
| `Calculada` | 2 | Proventos/descontos/líquido apurados (S-1200/S-1202 prontos). |
| `Fechada` | 3 | Competência fechada (S-1299, S-1210, totalizadores, DCTFWeb). |
| `Paga` | 4 | Líquido pago (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Paga` }.
> - **Editável** (aceita adicionar/remover eventos) = { `Aberta` }.
> - **Calculável** = { `Aberta`, `Calculada` } (recalcular é permitido enquanto não fechada).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A abertura exige `Competencia` válida (`AAAA-MM`) e **única por tenant** (índice único `(TenantId, Competencia)`); duplicar competência lança `InvalidOperationException`.
- **I-2.** Eventos só podem ser adicionados/removidos enquanto a folha está `Aberta` (I editável); após `Calculada`/`Fechada`/`Paga`, qualquer mutação de eventos lança `InvalidOperationException`.
- **I-3.** Cada `EventoFolha` referencia uma `Rubrica` que **existe e está vigente em S-1010** na competência (consistência eSocial — ver §11).
- **I-4.** O `RegimePrevidenciario` do evento é coerente com o do servidor: efetivo → **RPPS (S-1202)**; temporário/comissionado/celetista → **RGPS (S-1200)** (EC 103/2019).
- **I-5.** No cálculo, `TotalProventos = Σ proventos`, `TotalDescontos = Σ descontos`, `TotalLiquido = TotalProventos − TotalDescontos`; `TotalLiquido ≥ 0`.
- **I-6.** **Abate-teto (CF art. 37, XI):** quando a soma de proventos de um servidor excede o `TetoRemuneratorio`, é lançada uma **rubrica de desconto de abate-teto** reduzindo o `LiquidoAPagar` ao limite constitucional.
- **I-7.** O fechamento (`Fechar`) só é permitido a partir de `Calculada`; fechar folha `Aberta` (não calculada) lança `InvalidOperationException`.
- **I-8.** Ao fechar, são gerados **S-1299** (fechamento), **S-1210** (pagamentos), **totalizadores S-5001/5002/5003** e a **DCTFWeb** é produzida a partir dos totalizadores.
- **I-9.** O pagamento (`EfetuarPagamento`) só é permitido a partir de `Fechada`; pagar folha não fechada lança `InvalidOperationException`.
- **I-10.** Folha **paga** (`Paga`) é terminal e não admite novas transições (recálculo, novo fechamento, novo pagamento).
- **I-11.** **Não excluir** S-1200/S-1202/S-2299 enquanto houver **S-1210 vinculado** já transmitido (a exclusão exige a remoção prévia do S-1210).
- **I-12.** Os estabelecimentos referenciados (via servidor/lotação) existem e estão **vigentes em S-1005** na competência.
- **I-13.** Eventos periódicos (S-1200/S-1202/S-1210) são transmitidos **até o dia 15** do mês seguinte ao da competência.
- **I-14.** Na abertura, a situação inicial é `Aberta` e é emitido o evento `FolhaAberta(id, competencia)`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Abrir` | `Aberta` | `Competencia` válida e única no tenant | `FolhaAberta` |
| `Aberta` | `AdicionarEvento` / `RemoverEvento` | `Aberta` | situação == `Aberta`; rubrica vigente (S-1010) | — |
| `Aberta` \| `Calculada` | `Calcular` | `Calculada` | situação ∈ {`Aberta`,`Calculada`} | `FolhaCalculada` |
| `Calculada` | `Fechar` | `Fechada` | situação == `Calculada` | `FolhaFechada` |
| `Fechada` | `EfetuarPagamento` | `Paga` | situação == `Fechada` | `PagamentoEfetuado` |

> Observações:
> - `Calcular` é **idempotente em estado** e pode ser reexecutado enquanto a folha não estiver `Fechada` (recalcula totais e reaplica abate-teto).
> - O **abate-teto** é aplicado **dentro** de `Calcular` (lança/atualiza a rubrica de abate-teto antes de consolidar `TotalLiquido`).
> - Não há transição de "reabertura" de folha `Fechada`/`Paga` nesta versão (encerramento conforme prazos legais).

---

## 5. Comandos (escrita)

### 5.1 AbrirFolha

- **Command:** `AbrirFolhaCommand(int Ano, int Mes) : ICommand<Guid>`.
- **Entrada (DTO):** `Ano`, `Mes` (compõem a `Competencia` `AAAA-MM`).
- **Dependências do handler:** `IFolhaDePagamentoRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pré-condições:**
  - `request` não nulo; `Competencia` válida.
  - Não existe folha para a mesma competência no tenant (I-1), senão `InvalidOperationException("Folha já aberta para a competência.")`.
- **Efeito:** cria via `FolhaDePagamento.Abrir(tenant.TenantId, new Competencia(request.Ano, request.Mes))`; `folhas.Adicionar(folha)`; `SaveChangesAsync`.
- **Pós-condições:** nova `FolhaDePagamento` em situação `Aberta`; retorna `folha.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (competência inválida ou duplicada).
- **Evento de domínio:** `FolhaAberta(id, competencia)` (emitido no construtor via factory).

### 5.2 AdicionarEvento

- **Command:** `AdicionarEventoCommand(Guid FolhaDePagamentoId, Guid ServidorId, string Rubrica, TipoEvento Tipo, decimal BaseCalculo, decimal Valor) : ICommand`.
- **Entrada (DTO):** `FolhaDePagamentoId`, `ServidorId`, `Rubrica`, `Tipo`, `BaseCalculo`, `Valor`.
- **Dependências do handler:** `IFolhaDePagamentoRepository`, `IServidorRepository`, `IRubricaRepository` (ou validação de S-1010), `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo; folha existe e está `Aberta` (I-2).
  - Servidor existe; regime do evento coerente com o do servidor (I-4).
  - `Rubrica` existe e vigente em S-1010 (I-3).
- **Efeito:** `folha.AdicionarEvento(request.ServidorId, new Rubrica(request.Rubrica), request.Tipo, new BaseCalculo(request.BaseCalculo), request.Valor)`; `SaveChangesAsync`.
- **Pós-condições:** novo `EventoFolha` na coleção; totais **não** recalculados até `Calcular`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (folha não `Aberta`, servidor/rubrica inválidos, regime incoerente).
- **Evento de domínio:** — (mutação interna do agregado; consolidação no cálculo).

### 5.3 CalcularFolha

- **Command:** `CalcularFolhaCommand(Guid FolhaDePagamentoId) : ICommand`.
- **Entrada (DTO):** `FolhaDePagamentoId`.
- **Dependências do handler:** `IFolhaDePagamentoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`, `IOptions<ParametrosFolha>` (teto remuneratório parametrizável).
- **Pré-condições:** `request` não nulo; folha existe; situação ∈ {`Aberta`,`Calculada`} (I-7 só impede fechar, não calcular).
- **Efeito:** `folha.Calcular(tetoRemuneratorio, hoje)` — soma proventos/descontos, **aplica abate-teto** por servidor (I-6), consolida `TotalLiquido` (I-5); `SaveChangesAsync`.
- **Pós-condições:** situação `Calculada`; `DataCalculo` preenchida; `TotalProventos`/`TotalDescontos`/`TotalLiquido` atualizados; S-1200/S-1202 prontos para transmissão.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou já fechada/paga).
- **Evento de domínio:** `FolhaCalculada(Id, competencia, totalLiquido)`.

### 5.4 FecharFolha

- **Command:** `FecharFolhaCommand(Guid FolhaDePagamentoId) : ICommand`.
- **Entrada (DTO):** `FolhaDePagamentoId`.
- **Dependências do handler:** `IFolhaDePagamentoRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; folha existe; situação == `Calculada` (I-7).
- **Efeito:** `folha.Fechar(hoje)`; `SaveChangesAsync`; gera S-1299/S-1210/totalizadores e a **DCTFWeb** (I-8, via integração/Outbox); publica **Integration Event** `FolhaFechadaIntegrationEvent(Guid.NewGuid(), agoraUtc, tenant.TenantId, folha.Id.Value, competencia, folha.TotalLiquido.Valor)` via `publisher.Publish`.
- **Pós-condições:** situação `Fechada`; `DataFechamento` preenchida; despesa de pessoal publicada (Contabilidade/Empenho).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou não calculada).
- **Evento de domínio:** `FolhaFechada(Id, competencia)`.
- **Evento de integração (publica):** `FolhaFechadaIntegrationEvent`.

### 5.5 EfetuarPagamento

- **Command:** `EfetuarPagamentoCommand(Guid FolhaDePagamentoId, DateOnly DataPagamento) : ICommand`.
- **Entrada (DTO):** `FolhaDePagamentoId`, `DataPagamento`.
- **Dependências do handler:** `IFolhaDePagamentoRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; folha existe; situação == `Fechada` (I-9).
- **Efeito:** `folha.EfetuarPagamento(request.DataPagamento)`; `SaveChangesAsync`; publica **Integration Event** `PagamentoEfetuadoIntegrationEvent(Guid.NewGuid(), agoraUtc, tenant.TenantId, folha.Id.Value, competencia, folha.TotalLiquido.Valor, request.DataPagamento)` via `publisher.Publish`.
- **Pós-condições:** situação `Paga`; `DataPagamento` preenchida; liquidação financeira publicada (Tesouraria).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou não fechada).
- **Evento de domínio:** `PagamentoEfetuado(Id, competencia, dataPagamento)`.
- **Evento de integração (publica):** `PagamentoEfetuadoIntegrationEvent`.

> **Comandos de domínio existentes no agregado sem handler de Application dedicado nesta versão:** `RemoverEvento(Guid eventoId)` (método da raiz, exposto para correção de lançamentos enquanto `Aberta`).

---

## 6. Consultas (leitura)

### 6.1 ObterFolhaPorCompetencia

- **Query:** `ObterFolhaPorCompetenciaQuery(int Ano, int Mes) : IQuery<FolhaResumo?>`.
- **Entrada:** `Ano`, `Mes`.
- **Handler:** `ObterFolhaPorCompetenciaHandler(IFolhaDePagamentoRepository folhas)`; chama `folhas.ObterPorCompetenciaAsync(new Competencia(request.Ano, request.Mes), ct)`.
- **Projeção (DTO):** `FolhaResumo(Guid Id, string Competencia, string Situacao, decimal TotalProventos, decimal TotalDescontos, decimal TotalLiquido, DateOnly? DataFechamento)`.
  - `TotalLiquido` projetado de `folha.TotalLiquido.Valor`; `Situacao`/`Competencia` de `ToString()`.
- **Filtros:** por `Competencia`; **sempre tenant-scoped** via Global Query Filter por `TenantId` no DbContext.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ObterContrachequeDoServidor

- **Query:** `ObterContrachequeDoServidorQuery(Guid FolhaDePagamentoId, Guid ServidorId) : IQuery<ContrachequeDto?>`.
- **Entrada:** `FolhaDePagamentoId`, `ServidorId`.
- **Handler:** `ObterContrachequeDoServidorHandler(IFolhaDePagamentoRepository folhas)`; carrega a folha e projeta os `EventoFolha` do servidor.
- **Projeção (DTO):** `ContrachequeDto(Guid ServidorId, string Competencia, IReadOnlyList<LinhaContracheque> Linhas, decimal TotalProventos, decimal TotalDescontos, decimal LiquidoAPagar)`; `LinhaContracheque(string Rubrica, string Tipo, decimal Valor)`.
- **Filtros:** por `FolhaDePagamentoId` e `ServidorId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...RecursosHumanos.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `FolhaAberta` | `(FolhaDePagamentoId, Competencia)` | `FolhaDePagamento.Abrir` (construtor) |
| `FolhaCalculada` | `(FolhaDePagamentoId, Competencia, decimal TotalLiquido)` | `FolhaDePagamento.Calcular` |
| `FolhaFechada` | `(FolhaDePagamentoId, Competencia)` | `FolhaDePagamento.Fechar` |
| `PagamentoEfetuado` | `(FolhaDePagamentoId, Competencia, DateOnly DataPagamento)` | `FolhaDePagamento.EfetuarPagamento` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...RecursosHumanos.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `FolhaFechadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid FolhaDePagamentoId, string Competencia, decimal TotalLiquido)` | `FecharFolhaHandler` (para Contabilidade/Empenho — despesa de pessoal) |
| `PagamentoEfetuadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid FolhaDePagamentoId, string Competencia, decimal TotalLiquido, DateOnly DataPagamento)` | `EfetuarPagamentoHandler` (para Tesouraria — liquidação financeira) |

### Integração (consome)

- Nenhum (esta versão não consome Integration Events de outros módulos no agregado FolhaDePagamento).

---

## 8. Validações (FluentValidation)

### AbrirFolhaValidator (`AbstractValidator<AbrirFolhaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Ano` | `InclusiveBetween(2000, 2100)` | "Ano da competência inválido." |
| `Mes` | `InclusiveBetween(1, 12)` | "Mês da competência deve estar entre 1 e 12." |

### AdicionarEventoValidator (`AbstractValidator<AdicionarEventoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FolhaDePagamentoId` | `NotEmpty()` | "Folha é obrigatória." |
| `ServidorId` | `NotEmpty()` | "Servidor é obrigatório." |
| `Rubrica` | `NotEmpty()` + `MaximumLength(30)` | "Rubrica é obrigatória (máx. 30 caracteres)." |
| `Tipo` | `IsInEnum()` | "Tipo de evento inválido (Provento/Desconto)." |
| `BaseCalculo` | `GreaterThanOrEqualTo(0)` | "Base de cálculo não pode ser negativa." |
| `Valor` | `GreaterThan(0)` | "Valor do evento deve ser maior que zero." |

### FecharFolhaValidator (`AbstractValidator<FecharFolhaCommand>`) / EfetuarPagamentoValidator (`AbstractValidator<EfetuarPagamentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FolhaDePagamentoId` | `NotEmpty()` | "Folha é obrigatória." |
| `DataPagamento` (somente pagamento) | `NotEmpty()` | "Data de pagamento é obrigatória." |

> `CalcularFolhaCommand` apoia-se em invariantes de domínio (estado calculável, abate-teto) e checagem de existência no handler; consultas (`ObterFolhaPorCompetencia`, `ObterContrachequeDoServidor`) não possuem validador dedicado.

---

## 9. Persistência (EF Core 8)

- **Schema:** `recursoshumanos` (isolado por módulo). **DbContext:** o do módulo RecursosHumanos. **Migrations:** por módulo.
- **Tabela:** `FolhaDePagamento` (raiz de agregado); `EventoFolha` (entidade-filha).

| Coluna (`FolhaDePagamento`) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `FolhaDePagamentoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Competencia` | `nvarchar(7)` ou (owned `Ano`,`Mes`) | VO `Competencia` (`AAAA-MM`). |
| `Situacao` | `int` | enum `SituacaoFolha` (valor numérico). |
| `DataCalculo` | `date?` | `DateOnly?` nulável. |
| `DataFechamento` | `date?` | `DateOnly?` nulável. |
| `DataPagamento` | `date?` | `DateOnly?` nulável. |
| `TotalProventos` | `decimal` | total apurado. |
| `TotalDescontos` | `decimal` | total apurado (inclui abate-teto). |
| `TotalLiquido` | `decimal` | owned de `LiquidoAPagar` (campo `Valor`). |

| Coluna (`EventoFolha`) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | identidade local. |
| `FolhaDePagamentoId` | `Guid` (FK) | relação 1:N com a raiz. |
| `ServidorId` | `Guid` | servidor do lançamento. |
| `Rubrica` | `nvarchar(30)` | owned de `Rubrica` (campo `Codigo`); referência a S-1010. |
| `Tipo` | `int` | enum `TipoEvento`. |
| `BaseCalculo` | `decimal` | owned de `BaseCalculo` (campo `Valor`). |
| `Valor` | `decimal` | valor apurado. |
| `RegimePrevidenciario` | `int` | enum (RPPS/RGPS — herdado do servidor). |

- **Não persistidas (calculadas):** totais derivam do recálculo, porém são **materializados** após `Calcular` para auditoria/desempenho da consulta.
- **Índices:**
  - PK em `Id` (folha) e `Id` (evento).
  - **Índice único** em `(TenantId, Competencia)` (I-1 — uma folha por competência por tenant).
  - Índice em `(FolhaDePagamentoId, ServidorId)` para o contracheque.
  - Índice em `(TenantId, Situacao)` para painéis de folha.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto RecursosHumanos para `FolhaFechadaIntegrationEvent` e `PagamentoEfetuadoIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `FolhaDePagamento` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição. **Executivo e Legislativo do mesmo município são tenants distintos** (folhas independentes).
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Abertura/lançamento/cálculo/fechamento/pagamento da folha: papéis de **Folha/RH** com competência financeira (ex.: `RecursosHumanos.Folha.Gerir`); o **pagamento** pode exigir papel adicional de **Tesouraria/financeiro**.
  - Consulta de folha e contracheque: papel de **leitura de RH** (ex.: `RecursosHumanos.Folha.Ler`); contracheque acessível ao próprio servidor (autosserviço).
  - Módulo RecursosHumanos é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** valores de remuneração e contracheques são **dados pessoais sensíveis** — minimização, controle de acesso por perfil; acesso a contracheque gera **trilha de acesso** (quem leu, quando, por quê). Remuneração nominal exposta apenas a perfis autorizados (e ao próprio servidor).
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Abrir`, `AdicionarEvento`, `RemoverEvento`, `Calcular`, `Fechar`, `EfetuarPagamento`) — destinada ao Tribunal de Contas (TCE-RS). Recibos/protocolos eSocial (S-1200/S-1202/S-1210/S-1299) e a DCTFWeb gerada preservados como prova fiscal.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **eSocial (periódicos) — Decreto 8.373/2014, leiautes S-1.3:**
  - **S-1200** (remuneração RGPS) e **S-1202** (remuneração RPPS — entes públicos): gerados a partir do **cálculo** (I-4 separa por regime).
  - **S-1210** (pagamentos) e **S-1299** (fechamento): gerados no **fechamento** (I-8).
  - **Totalizadores S-5001/5002/5003:** retornados pelo eSocial após processamento; base da DCTFWeb.
  - Transmissão: **web service SOAP**, **XML assinado com certificado A1** (Azure Key Vault, por tenant); ambientes **Produção Restrita** e **Produção**; cada lote retorna protocolo e cada evento gera **recibo persistido**.
- **DCTFWeb (IN RFB 2.005/2021):** **gerada automaticamente após o fechamento** (S-1299) a partir dos totalizadores; obrigatória para entes públicos desde jul/2022 (I-8).
- **Dependência de tabelas (eSocial):** rubricas vigentes em **S-1010** (I-3) e estabelecimentos vigentes em **S-1005** (I-12) na competência.
- **Prazo legal:** eventos periódicos transmitidos **até o dia 15** do mês seguinte ao da competência (I-13).
- **Regra de exclusão (I-11):** **não excluir** S-1200/S-1202/S-2299 enquanto houver **S-1210** vinculado já transmitido (exige exclusão prévia do S-1210).
- **Resiliência:** integração externa **idempotente** (por `EventId`/recibo), com timeout, **retry e circuit breaker (Polly)** sobre o SOAP; envio assíncrono via **Outbox**; mapeamento explícito de erros atrás de **Anti-Corruption Layer (ACL)**.
- **Saída intra-aplicação (via Contracts):** `FolhaFechadaIntegrationEvent` (Contabilidade/Empenho — despesa de pessoal, Lei 4.320) e `PagamentoEfetuadoIntegrationEvent` (Tesouraria — liquidação), publicados pelo Outbox; consumo via ACL.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Abertura de folha por competência**
- **Dado** um tenant sem folha na competência `2026-06`
- **Quando** executo `AbrirFolhaCommand(2026, 6)`
- **Então** é criada uma `FolhaDePagamento` em situação `Aberta` e o evento `FolhaAberta` é emitido.

**Cenário 2 — Competência duplicada**
- **Dado** uma folha já aberta para `2026-06`
- **Quando** executo `AbrirFolhaCommand(2026, 6)` novamente
- **Então** ocorre `InvalidOperationException` (competência duplicada — I-1).

**Cenário 3 — Lançamento de evento em folha aberta**
- **Dado** uma folha `Aberta` e uma rubrica vigente em S-1010
- **Quando** executo `AdicionarEventoCommand` (provento)
- **Então** o `EventoFolha` é adicionado à folha.

**Cenário 4 — Lançamento bloqueado após cálculo**
- **Dado** uma folha `Calculada`
- **Quando** executo `AdicionarEventoCommand`
- **Então** ocorre `InvalidOperationException` (eventos só em `Aberta` — I-2).

**Cenário 5 — Cálculo consolida totais**
- **Dada** uma folha `Aberta` com proventos e descontos
- **Quando** executo `CalcularFolhaCommand`
- **Então** situação = `Calculada`, `TotalLiquido = TotalProventos − TotalDescontos` e `FolhaCalculada` é emitido (I-5).

**Cenário 6 — Abate-teto**
- **Dado** um servidor cujos proventos somados ultrapassam o teto remuneratório
- **Quando** a folha é calculada (`CalcularFolhaCommand`)
- **Então** é lançada rubrica de **abate-teto** reduzindo o `LiquidoAPagar` ao limite constitucional (I-6 / CF art. 37, XI).

**Cenário 7 — Fechamento com RPPS gera S-1202 e DCTFWeb**
- **Dada** uma `FolhaDePagamento` `Calculada` na competência `2026-06` com servidores efetivos
- **Quando** executo `FecharFolhaCommand`
- **Então** situação = `Fechada`, `FolhaFechada` é emitido, **S-1202** é gerado para os efetivos, **S-1299/S-1210/totalizadores** são produzidos, a **DCTFWeb** é gerada e `FolhaFechadaIntegrationEvent` é publicado (I-8).

**Cenário 8 — Fechamento sem cálculo**
- **Dada** uma folha `Aberta` (não calculada)
- **Quando** executo `FecharFolhaCommand`
- **Então** ocorre `InvalidOperationException` (fechamento exige `Calculada` — I-7).

**Cenário 9 — Exclusão bloqueada de evento periódico**
- **Dado** um **S-1200** com **S-1210** vinculado já transmitido
- **Quando** solicito a exclusão do S-1200
- **Então** a operação é rejeitada, exigindo a exclusão prévia do S-1210 (I-11).

**Cenário 10 — Pagamento após fechamento**
- **Dada** uma folha `Fechada`
- **Quando** executo `EfetuarPagamentoCommand(folhaId, data)`
- **Então** situação = `Paga`, `PagamentoEfetuado` é emitido e `PagamentoEfetuadoIntegrationEvent` é publicado (Tesouraria — I-9).

**Cenário 11 — Pagamento sem fechamento**
- **Dada** uma folha `Calculada` (não fechada)
- **Quando** executo `EfetuarPagamentoCommand`
- **Então** ocorre `InvalidOperationException` (pagamento exige `Fechada` — I-9).

**Cenário 12 — Separação RPPS/RGPS**
- **Dada** uma folha com servidor efetivo (RPPS) e comissionado (RGPS)
- **Quando** executo `CalcularFolhaCommand`
- **Então** a remuneração do efetivo compõe **S-1202** e a do comissionado compõe **S-1200** (I-4).

**Cenário 13 — Consulta tenant-scoped de folha**
- **Dado** folhas da competência `2026-06` no tenant A e no tenant B
- **Quando** executo `ObterFolhaPorCompetenciaQuery(2026, 6)` no contexto do tenant A
- **Então** retorna **apenas** a folha do tenant A, projetada em `FolhaResumo`.

---

## 13. Casos de Borda

- **B-1.** `Mes` fora de 1–12 ou `Ano` fora da faixa ⇒ rejeitado pelo validator (`AbrirFolhaValidator`).
- **B-2.** Adicionar evento com `Valor <= 0` ⇒ rejeitado (`GreaterThan(0)`).
- **B-3.** Adicionar evento com rubrica inexistente/expirada em S-1010 ⇒ `InvalidOperationException` (I-3).
- **B-4.** Adicionar evento de regime incoerente com o servidor ⇒ `InvalidOperationException` (I-4).
- **B-5.** Abate-teto exatamente no limite (proventos == teto) ⇒ **nenhuma** rubrica de abate-teto lançada (apenas quando excede — I-6).
- **B-6.** Recalcular folha `Calculada` ⇒ permitido; reaplica abate-teto e atualiza totais (estado permanece `Calculada`).
- **B-7.** Calcular folha já `Fechada`/`Paga` ⇒ `InvalidOperationException` (não calculável).
- **B-8.** Fechar folha duas vezes ⇒ a 2ª falha (situação já `Fechada` ≠ `Calculada`).
- **B-9.** Pagar folha já `Paga` ⇒ `InvalidOperationException` (terminal — I-10).
- **B-10.** `TotalLiquido` resultaria negativo ⇒ impedido; abate-teto/descontos consolidam `TotalLiquido ≥ 0` (I-5).
- **B-11.** Estabelecimento não vigente em S-1005 na competência ⇒ rejeitado na validação de referência (I-12).
- **B-12.** Transmissão fora do prazo (após dia 15) ⇒ alerta/registro de prazo (I-13); transmissão segue com penalidade fiscal, fora do bloqueio do agregado.
- **B-13.** `FolhaFechadaIntegrationEvent`/`PagamentoEfetuadoIntegrationEvent` devem ser idempotentes no consumidor por `EventId` (reentrega via Outbox).
- **B-14.** Transmissão eSocial falha/timeout ⇒ retry/circuit breaker (Polly); evento permanece no Outbox até confirmação de recibo.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo RecursosHumanos (folha por competência: abertura, lançamento de eventos, cálculo com abate-teto, separação RPPS/RGPS, fechamento com S-1299/S-1210/totalizadores e DCTFWeb, pagamento; integrações Contabilidade/Empenho e Tesouraria). |

<!-- manifest
commands: AbrirFolha, AdicionarEvento, ApurarDescontosLegais, CalcularFolha, FecharFolha, EfetuarPagamento
queries: ObterFolhaPorCompetencia, ObterContrachequeDoServidor
domainEvents: FolhaAberta, FolhaCalculada, FolhaFechada, PagamentoEfetuado
integrationEventsPublished: FolhaFechadaIntegrationEvent, PagamentoEfetuadoIntegrationEvent, FolhaResumoRemessaTceIntegrationEvent, RemuneracaoMagisterioApuradaIntegrationEvent, DespesaPessoalApuradaIntegrationEvent
integrationEventsConsumed: 
-->
