---
modulo: Saude
agregado: SolicitacaoRegulacao
contexto: Saude (Atenção à Saúde municipal — SUS; PEP/e-SUS APS, regulação, farmácia e imunização)
poder: Executivo
schema: saude
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF arts. 196-200", "Lei 8.080/1990", "PNAB Portaria GM/MS 2.436/2017", "Portaria 1.434/2020 (RNDS)", "LGPD art. 11, II, f (tutela da saude)"]
---

# SolicitacaoRegulacao — Regras-as-Code (Rules-as-Code)

> Solicitação de regulação de procedimento (consulta especializada, exame ou leito) para um
> `Paciente`, classificada por `Procedimento` (SIGTAP), `Prioridade` e consumo de `Cota`, que ao ser
> autorizada reserva a vaga no **SISREG**. Materializa o acesso regulado à média/alta complexidade.
> Trata dado pessoal **sensível** (LGPD art. 11). Este arquivo é **normativo e versionado**; o código
> (`SolicitacaoRegulacao.cs`, handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Solicitação de Regulação (`SolicitacaoRegulacao`) | Pedido de procedimento regulado para um paciente. Raiz de agregado. |
| Regulação (`Regular` / regulador) | Análise técnica que autoriza/nega/devolve a solicitação. |
| Procedimento (`Procedimento` : `Procedimento`) | Procedimento da tabela **SIGTAP** (código + descrição). VO. |
| Prioridade (`Prioridade` : `Prioridade`) | Classificação de risco/urgência da solicitação. VO/enum. |
| Cota (`Cota` : `Cota`) | Limite de vagas disponíveis para o procedimento na competência/unidade. VO. |
| SISREG (`ReservarVagaNoSisreg`) | Sistema nacional de regulação; recebe a reserva da vaga ao autorizar. |
| Solicitar (`Solicitar` / `SolicitarRegulacao`) | Ato de abrir a solicitação de regulação. |
| Autorizar (`Autorizar` / `Autorizada`) | Deferimento pelo regulador; reserva vaga e consome cota. |
| Negar (`Negar` / `Negada`) | Indeferimento técnico da solicitação. |
| Devolver (`Devolver` / `Devolvida`) | Devolução ao solicitante para complementação/ajuste. |
| Cancelar (`Cancelar` / `Cancelada`) | Cancelamento da solicitação (pelo solicitante, antes de executada). |
| Executar (`Executar` / `Executada`) | Marca o procedimento como realizado (atendido). |
| AIH / APAC (`TipoAutorizacao`) | Tipo de autorização (internação / procedimento ambulatorial), quando aplicável. |
| Situação (`Situacao` : `SituacaoSolicitacaoRegulacao`) | Estado atual da solicitação no fluxo de regulação. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Secretaria de Saúde) dono do registro. |

---

## 2. Modelo

- **Identidade:** `SolicitacaoRegulacaoId` — `readonly record struct SolicitacaoRegulacaoId(Guid Value)`; fábrica `SolicitacaoRegulacaoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `SolicitacaoRegulacao : AggregateRoot<SolicitacaoRegulacaoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Solicitar(...)`.
- **Referências cross-aggregate por Id** (sem navegação): `PacienteId`, `EstabelecimentoSolicitanteId`, `ProfissionalSolicitanteId`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `PacienteId` | `PacienteId` (VO/Id) | Paciente da solicitação (referência por Id). | `private set` |
| `EstabelecimentoSolicitanteId` | `EstabelecimentoId` (VO/Id) | Unidade solicitante (CNES). | `private set` |
| `ProfissionalSolicitanteId` | `ProfissionalId` (VO/Id) | Profissional solicitante. | `private set` |
| `Procedimento` | `Procedimento` (VO) | Procedimento SIGTAP (código + descrição). | `private set` |
| `Prioridade` | `Prioridade` | Classificação de risco/urgência. | `private set` |
| `Cota` | `Cota` (VO) | Cota/limite de vagas aplicável. | `private set` |
| `Justificativa` | `string` | Justificativa clínica do pedido. | `private set` |
| `DataSolicitacao` | `DateOnly` | Data de abertura da solicitação. | `private set` |
| `DataAutorizacao` | `DateOnly?` | Data da autorização (nula até autorizar). | `private set` |
| `ProtocoloSisreg` | `string?` | Protocolo da reserva no SISREG (nulo até autorizar). | `private set` |
| `Situacao` | `SituacaoSolicitacaoRegulacao` | Situação atual. | `private set` |

### Value Objects (referenciados)

- `Procedimento` — `readonly record struct` com `CodigoSigtap : string`, `Descricao : string`. Código SIGTAP validado (formato e existência via ACL).
- `Cota` — `readonly record struct` com `Disponivel : int`, `Total : int`; método `TemDisponibilidade()` => `Disponivel > 0`.

### Enum `Prioridade`

| Valor | Numérico | Descrição |
|---|---|---|
| `Eletiva` | 1 | Sem urgência (fila eletiva). |
| `Prioritaria` | 2 | Prioridade clínica intermediária. |
| `Urgente` | 3 | Urgência. |
| `Emergencia` | 4 | Emergência (máxima prioridade). |

### Enum `SituacaoSolicitacaoRegulacao`

| Valor | Numérico | Descrição |
|---|---|---|
| `Solicitada` | 1 | Aguardando regulação (estado inicial). |
| `Autorizada` | 2 | Deferida; vaga reservada no SISREG, cota consumida. |
| `Negada` | 3 | Indeferida pelo regulador — terminal. |
| `Devolvida` | 4 | Devolvida ao solicitante para complementação. |
| `Executada` | 5 | Procedimento realizado — terminal. |
| `Cancelada` | 6 | Cancelada pelo solicitante — terminal. |

> **Conjuntos de referência usados nas guardas:**
> - **Em análise** = { `Solicitada`, `Devolvida` } — situações que admitem `Autorizar`/`Negar`.
> - **Encerrada** = { `Negada`, `Executada`, `Cancelada` } — não admite novas transições.
> - **Cancelável** = { `Solicitada`, `Devolvida`, `Autorizada` } — admite `Cancelar` (antes de `Executada`).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A solicitação exige `Procedimento` SIGTAP válido (código não vazio) e `Justificativa` não vazia na abertura (`ArgumentException` caso contrário).
- **I-2.** A solicitação exige paciente com **CNS válido/confirmado** (`CnsConfirmado == true`); validado no handler antes de `Solicitar` (coerência com a regra crítica do módulo).
- **I-3.** A autorização (`Autorizar`) exige situação **em análise** ({`Solicitada`,`Devolvida`}) **e** `Cota` com disponibilidade (`Cota.TemDisponibilidade()`); caso contrário `InvalidOperationException`. (Cenário 6)
- **I-4.** Ao autorizar, a `Cota` é **decrementada** (consumo de uma vaga), `DataAutorizacao` é gravada, a situação passa a `Autorizada`, a vaga é reservada no **SISREG** (`ProtocoloSisreg` preenchido) e o evento `SolicitacaoAutorizada` é emitido. (Cenário 6)
- **I-5.** `Negar` exige situação em análise; passa a `Negada` (terminal) com `Motivo` registrado.
- **I-6.** `Devolver` exige situação `Solicitada`; passa a `Devolvida` com `Motivo`, permitindo reanálise posterior.
- **I-7.** `Cancelar` só é permitido enquanto a solicitação for **cancelável** ({`Solicitada`,`Devolvida`,`Autorizada`}) e **antes** de `Executada`; ao cancelar uma solicitação `Autorizada`, a `Cota` reservada é **devolvida** (incrementada).
- **I-8.** `Executar` exige situação `Autorizada`; passa a `Executada` (terminal).
- **I-9.** Situações **encerradas** ({`Negada`,`Executada`,`Cancelada`}) não admitem novas transições.
- **I-10.** Na criação, a situação inicial é `Solicitada` e é emitido o evento `RegulacaoSolicitada(id, pacienteId, procedimento)`.
- **I-11.** `Prioridade` é obrigatória e válida (`IsInEnum`); ordena a fila de regulação (não altera a guarda de cota, mas a ordem de atendimento).
- **I-12.** Dado é **sensível** (LGPD art. 11): leitura de solicitações de um paciente registra **trilha de acesso** e exige claim `saude.regular`/`saude.ler`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Solicitar` | `Solicitada` | procedimento/justificativa válidos; CNS confirmado | `RegulacaoSolicitada` |
| `Solicitada` \| `Devolvida` | `Autorizar` | `Autorizada` | em análise && `Cota.TemDisponibilidade()` | `SolicitacaoAutorizada` |
| `Solicitada` \| `Devolvida` | `Negar` | `Negada` | em análise | `SolicitacaoNegada` |
| `Solicitada` | `Devolver` | `Devolvida` | situação == `Solicitada` | `SolicitacaoDevolvida` |
| `Autorizada` | `Executar` | `Executada` | situação == `Autorizada` | `ProcedimentoExecutado` |
| `Solicitada` \| `Devolvida` \| `Autorizada` | `Cancelar` | `Cancelada` | cancelável (≠ `Executada`/encerrada) | `SolicitacaoCancelada` |

> Observações:
> - `Autorizar`/`Negar` chamam `GarantirEmAnalise()` antes do efeito; `Autorizar` ainda exige `Cota.TemDisponibilidade()` (I-3).
> - `Cancelar` sobre situação `Autorizada` **devolve a cota** reservada (I-7).
> - Não há transição de retorno de estados encerrados (`Negada`/`Executada`/`Cancelada`).

---

## 5. Comandos (escrita)

### 5.1 SolicitarRegulacao

- **Command:** `SolicitarRegulacaoCommand(Guid PacienteId, Guid EstabelecimentoSolicitanteId, Guid ProfissionalSolicitanteId, string CodigoSigtap, string DescricaoProcedimento, Prioridade Prioridade, string Justificativa) : ICommand<Guid>`.
- **Entrada (DTO):** paciente, unidade/profissional solicitante, procedimento SIGTAP, prioridade, justificativa.
- **Dependências do handler:** `IPacienteRepository`, `ISolicitacaoRegulacaoRepository`, `ICotaRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe com `CnsConfirmado == true`, senão `InvalidOperationException("Paciente sem CNS válido/confirmado.")` (I-2).
  - `CodigoSigtap` e `Justificativa` não vazios (I-1).
  - `Cota` aplicável ao procedimento obtida (`ICotaRepository.ObterCotaAsync`).
- **Efeito:** `hoje = DateOnly.FromDateTime(timeProvider...)`; cria via `SolicitacaoRegulacao.Solicitar(tenant.TenantId, pacienteId, estabelecimentoId, profissionalId, procedimento, prioridade, cota, justificativa, hoje)`; `solicitacoes.Adicionar(solicitacao)`; `SaveChangesAsync`.
- **Pós-condições:** nova `SolicitacaoRegulacao` em situação `Solicitada`; retorna `solicitacao.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (procedimento/justificativa vazios); `InvalidOperationException` (CNS inválido).
- **Evento de domínio:** `RegulacaoSolicitada(id, pacienteId, procedimento)` (emitido no construtor via factory).

### 5.2 AutorizarSolicitacaoRegulacao

- **Command:** `AutorizarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId) : ICommand`.
- **Entrada (DTO):** `SolicitacaoRegulacaoId`.
- **Dependências do handler:** `ISolicitacaoRegulacaoRepository`, `ISisregGateway`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo; solicitação existe (senão `InvalidOperationException("Solicitação não encontrada.")`).
  - Situação em análise ({`Solicitada`,`Devolvida`}); `Cota.TemDisponibilidade()` (I-3).
- **Efeito:** reserva vaga no SISREG (`ISisregGateway.ReservarVagaAsync(...)` → `protocolo`); `solicitacao.Autorizar(protocolo, hoje)` (decrementa cota); `SaveChangesAsync`; (Outbox publica `SolicitacaoAutorizadaIntegrationEvent`, se aplicável).
- **Pós-condições:** situação `Autorizada`; `DataAutorizacao` e `ProtocoloSisreg` preenchidos; `Cota.Disponivel` decrementada.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada, fora de análise ou cota esgotada).
- **Evento de domínio:** `SolicitacaoAutorizada(Id, protocoloSisreg)`.

### 5.3 NegarSolicitacaoRegulacao

- **Command:** `NegarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId, string Motivo) : ICommand`.
- **Entrada (DTO):** `SolicitacaoRegulacaoId`, `Motivo`.
- **Dependências do handler:** `ISolicitacaoRegulacaoRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; solicitação existe; situação em análise (I-5).
- **Efeito:** `solicitacao.Negar(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Negada` (terminal).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (motivo vazio); `InvalidOperationException` (não encontrada ou fora de análise).
- **Evento de domínio:** `SolicitacaoNegada(Id, motivo)`.

### 5.4 DevolverSolicitacaoRegulacao

- **Command:** `DevolverSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId, string Motivo) : ICommand`.
- **Entrada (DTO):** `SolicitacaoRegulacaoId`, `Motivo`.
- **Dependências do handler:** `ISolicitacaoRegulacaoRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; solicitação existe; situação == `Solicitada` (I-6).
- **Efeito:** `solicitacao.Devolver(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Devolvida` (admite reanálise).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (motivo vazio); `InvalidOperationException` (não encontrada ou ≠ `Solicitada`).
- **Evento de domínio:** `SolicitacaoDevolvida(Id, motivo)`.

### 5.5 ExecutarSolicitacaoRegulacao

- **Command:** `ExecutarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId) : ICommand`.
- **Entrada (DTO):** `SolicitacaoRegulacaoId`.
- **Dependências do handler:** `ISolicitacaoRegulacaoRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; solicitação existe; situação == `Autorizada` (I-8).
- **Efeito:** `solicitacao.Executar(hoje)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Executada` (terminal).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou ≠ `Autorizada`).
- **Evento de domínio:** `ProcedimentoExecutado(Id)`.

### 5.6 CancelarSolicitacaoRegulacao

- **Command:** `CancelarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId, string Motivo) : ICommand`.
- **Entrada (DTO):** `SolicitacaoRegulacaoId`, `Motivo`.
- **Dependências do handler:** `ISolicitacaoRegulacaoRepository`, `ISisregGateway`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; solicitação existe; situação **cancelável** ({`Solicitada`,`Devolvida`,`Autorizada`}) (I-7).
- **Efeito:** se `Autorizada`, libera a reserva no SISREG e o agregado **devolve a cota**; `solicitacao.Cancelar(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Cancelada` (terminal); cota devolvida se havia reserva.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (motivo vazio); `InvalidOperationException` (não encontrada ou encerrada/`Executada`).
- **Evento de domínio:** `SolicitacaoCancelada(Id, motivo)`.

---

## 6. Consultas (leitura)

### 6.1 ObterSolicitacaoRegulacaoPorId

- **Query:** `ObterSolicitacaoRegulacaoPorIdQuery(Guid SolicitacaoRegulacaoId) : IQuery<SolicitacaoRegulacaoDetalhe?>`.
- **Entrada:** `SolicitacaoRegulacaoId`.
- **Handler:** `ObterSolicitacaoRegulacaoPorIdHandler(ISolicitacaoRegulacaoRepository solicitacoes)`; chama `solicitacoes.ObterPorIdAsync(...)`.
- **Projeção (DTO):** `SolicitacaoRegulacaoDetalhe(Guid Id, Guid PacienteId, string CodigoSigtap, string DescricaoProcedimento, string Prioridade, string Situacao, DateOnly DataSolicitacao, DateOnly? DataAutorizacao, string? ProtocoloSisreg)`.
- **Filtros:** por `SolicitacaoRegulacaoId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo.
- **Segurança:** claim `saude.regular`/`saude.ler`; **trilha de acesso** (dado sensível — LGPD art. 11).

### 6.2 ListarFilaDeRegulacao

- **Query:** `ListarFilaDeRegulacaoQuery(string? CodigoSigtap, Prioridade? Prioridade) : IQuery<IReadOnlyList<SolicitacaoRegulacaoResumo>>`.
- **Entrada:** filtro opcional por procedimento e prioridade; retorna solicitações **em análise** ({`Solicitada`,`Devolvida`}).
- **Handler:** `ListarFilaDeRegulacaoHandler(ISolicitacaoRegulacaoRepository solicitacoes)`; chama `solicitacoes.ListarPendentesAsync(...)` ordenando por `Prioridade` desc e `DataSolicitacao` asc.
- **Projeção (DTO):** `SolicitacaoRegulacaoResumo(Guid Id, Guid PacienteId, string CodigoSigtap, string Prioridade, string Situacao, DateOnly DataSolicitacao)`.
- **Filtros:** por procedimento/prioridade + situação pendente; **sempre tenant-scoped**.
- **Pré-condições:** `request` não nulo.
- **Segurança:** claim `saude.regular`; **trilha de acesso** registra quem, quando.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Saude.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `RegulacaoSolicitada` | `(SolicitacaoRegulacaoId, PacienteId, Procedimento)` | `SolicitacaoRegulacao.Solicitar` (construtor) |
| `SolicitacaoAutorizada` | `(SolicitacaoRegulacaoId, string protocoloSisreg)` | `SolicitacaoRegulacao.Autorizar` |
| `SolicitacaoNegada` | `(SolicitacaoRegulacaoId, string motivo)` | `SolicitacaoRegulacao.Negar` |
| `SolicitacaoDevolvida` | `(SolicitacaoRegulacaoId, string motivo)` | `SolicitacaoRegulacao.Devolver` |
| `ProcedimentoExecutado` | `(SolicitacaoRegulacaoId)` | `SolicitacaoRegulacao.Executar` |
| `SolicitacaoCancelada` | `(SolicitacaoRegulacaoId, string motivo)` | `SolicitacaoRegulacao.Cancelar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Saude.Contracts`)

- Nenhum **obrigatório** nesta versão pelo README (a lista de Integration Events publicados do módulo — §7 — não inclui regulação). A reserva da vaga é feita por **gateway síncrono** (SISREG), não por Integration Event. Caso a analítica para **Transparencia** exija, poderá ser adicionado `SolicitacaoAutorizadaIntegrationEvent` (agregado/anonimizado) — fora do escopo desta versão.

### Integração (consome)

- Nenhum diretamente no agregado `SolicitacaoRegulacao`.

---

## 8. Validações (FluentValidation)

### SolicitarRegulacaoValidator (`AbstractValidator<SolicitarRegulacaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `PacienteId` | `NotEmpty()` | "Paciente é obrigatório." |
| `EstabelecimentoSolicitanteId` | `NotEmpty()` | "Estabelecimento solicitante é obrigatório." |
| `ProfissionalSolicitanteId` | `NotEmpty()` | "Profissional solicitante é obrigatório." |
| `CodigoSigtap` | `NotEmpty()` + `MaximumLength(10)` | "Código SIGTAP é obrigatório (máx. 10)." |
| `Prioridade` | `IsInEnum()` | "Prioridade inválida." |
| `Justificativa` | `NotEmpty()` + `MaximumLength(2000)` | "Justificativa é obrigatória (máx. 2000)." |

### NegarSolicitacaoRegulacaoValidator (`AbstractValidator<NegarSolicitacaoRegulacaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `SolicitacaoRegulacaoId` | `NotEmpty()` | "Solicitação é obrigatória." |
| `Motivo` | `NotEmpty()` + `MaximumLength(1000)` | "Motivo da negativa é obrigatório (máx. 1000)." |

### DevolverSolicitacaoRegulacaoValidator (`AbstractValidator<DevolverSolicitacaoRegulacaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `SolicitacaoRegulacaoId` | `NotEmpty()` | "Solicitação é obrigatória." |
| `Motivo` | `NotEmpty()` + `MaximumLength(1000)` | "Motivo da devolução é obrigatório (máx. 1000)." |

### CancelarSolicitacaoRegulacaoValidator (`AbstractValidator<CancelarSolicitacaoRegulacaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `SolicitacaoRegulacaoId` | `NotEmpty()` | "Solicitação é obrigatória." |
| `Motivo` | `NotEmpty()` + `MaximumLength(1000)` | "Motivo do cancelamento é obrigatório (máx. 1000)." |

> `AutorizarSolicitacaoRegulacaoCommand` e `ExecutarSolicitacaoRegulacaoCommand` validam ao menos `SolicitacaoRegulacaoId` `NotEmpty`; demais proteções por invariantes de domínio (incl. checagem de cota) e checagem de existência/estado no handler. Queries não possuem validador além de `ArgumentNullException`.

---

## 9. Persistência (EF Core 8)

- **Schema:** `saude` (isolado por módulo). **DbContext:** o do módulo Saude. **Migrations:** por módulo.
- **Tabela:** `SolicitacaoRegulacao` (raiz de agregado).

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `SolicitacaoRegulacaoId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo interceptor; alvo do Global Query Filter. |
| `PacienteId` | `Guid` | conversor de `PacienteId` ↔ `Guid` (referência por Id). |
| `EstabelecimentoSolicitanteId` | `Guid` | conversor de `EstabelecimentoId` ↔ `Guid`. |
| `ProfissionalSolicitanteId` | `Guid` | conversor de `ProfissionalId` ↔ `Guid`. |
| `Procedimento_CodigoSigtap` | `nvarchar(10)` | owned/VO `Procedimento` (CodigoSigtap). |
| `Procedimento_Descricao` | `nvarchar(200)` | owned/VO `Procedimento` (Descricao). |
| `Prioridade` | `int` | enum `Prioridade`. |
| `Cota_Disponivel` / `Cota_Total` | `int` | owned/VO `Cota` (snapshot no momento; a fonte de verdade de cota pode ser tabela própria). |
| `Justificativa` | `nvarchar(2000)` | texto. |
| `DataSolicitacao` | `date` | `DateOnly`. |
| `DataAutorizacao` | `date?` | `DateOnly?` (nulo até autorizar). |
| `ProtocoloSisreg` | `nvarchar(40)?` | nulável; protocolo da reserva. |
| `Situacao` | `int` | enum `SituacaoSolicitacaoRegulacao`. |

- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, Situacao, Prioridade, DataSolicitacao)` para a fila de regulação (`ListarPendentesAsync`).
  - Índice em `(TenantId, PacienteId)` para listar solicitações do paciente.
  - Índice em `(TenantId, Procedimento_CodigoSigtap)` para consumo/gestão de cota por procedimento.
- **Conversores (VO/Id):** Fluent API; owned types para `Procedimento`/`Cota`.
- **Outbox:** tabela Outbox do contexto Saude (não utilizada nesta versão pelo agregado — sem Integration Event publicado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `SolicitacaoRegulacao` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId`; **nunca aceito do cliente**. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Abrir solicitação: claim do solicitante (ex.: `saude.atender`/`saude.solicitar`).
  - Autorizar/Negar/Devolver/Executar/Cancelar: claim **`saude.regular`** (regulador).
  - Leitura da fila / solicitação: claim `saude.regular`/`saude.ler`.
  - Módulo Saude **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** dado pessoal **sensível** (art. 11). Base legal: **tutela da saúde** (art. 11, II, "f"), com minimização. Leitura de solicitações vinculadas a paciente gera **trilha de acesso** (quem, quando, qual CNS) além do *audit trail* imutável.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Solicitar`, `Autorizar`, `Negar`, `Devolver`, `Executar`, `Cancelar`) — destinada ao Tribunal de Contas (TCE-RS). A decisão de regulação (autorizar/negar) é especialmente auditável.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **SISREG (saída — síncrona, ACL):** ao `Autorizar`, `ISisregGateway.ReservarVagaAsync` reserva a vaga e retorna o `ProtocoloSisreg`; ao `Cancelar` uma autorizada, libera a reserva. Idempotente por `SolicitacaoRegulacaoId`, com **timeout, retry e circuit breaker (Polly)** e **Anti-Corruption Layer**.
- **SIGTAP (master data):** validação do `CodigoSigtap` do `Procedimento` contra a tabela unificada (ACL/cache).
- **CADSUS (pré-condição):** paciente deve ter CNS confirmado (validado no agregado Paciente; aqui exige-se `CnsConfirmado == true` — I-2).
- **RNDS (contexto):** procedimentos executados podem compor o RES do paciente via `Atendimento`; o agregado de regulação não publica RES diretamente.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); ACL por integração; a reserva no SISREG e o consumo de cota ocorrem na mesma transação lógica de `Autorizar` (compensação no cancelamento).

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Solicitação de regulação autorizada (cota disponível)**
- **Dada** uma `SolicitacaoRegulacao` com `Procedimento` SIGTAP e `Cota` disponível
- **Quando** o regulador autoriza (`AutorizarSolicitacaoRegulacaoCommand`)
- **Então** `SolicitacaoAutorizada` é publicada, a `Cota` é decrementada e a vaga é reservada no **SISREG** (`ProtocoloSisreg` preenchido) (I-3/I-4).

**Cenário 2 — Autorização sem cota disponível**
- **Dada** uma `SolicitacaoRegulacao` cuja `Cota` está esgotada (`Disponivel == 0`)
- **Quando** o regulador autoriza
- **Então** ocorre `InvalidOperationException` (sem cota) e a situação permanece `Solicitada` (I-3).

**Cenário 3 — Solicitação exige CNS válido**
- **Dado** um `Paciente` sem CNS confirmado
- **Quando** executo `SolicitarRegulacaoCommand`
- **Então** a operação é rejeitada (`InvalidOperationException`) e nenhuma solicitação é criada (I-2).

**Cenário 4 — Negativa pelo regulador**
- **Dada** uma `SolicitacaoRegulacao` `Solicitada`
- **Quando** executo `NegarSolicitacaoRegulacaoCommand(id, "Sem indicação clínica")`
- **Então** situação = `Negada` e `SolicitacaoNegada` é emitida (I-5).

**Cenário 5 — Devolução para complementação e reanálise**
- **Dada** uma `SolicitacaoRegulacao` `Solicitada`
- **Quando** executo `DevolverSolicitacaoRegulacaoCommand(id, "Anexar exame")` e depois `AutorizarSolicitacaoRegulacaoCommand(id)`
- **Então** a solicitação passa por `Devolvida` e, com cota disponível, chega a `Autorizada` (I-6/I-3).

**Cenário 6 — Execução do procedimento autorizado**
- **Dada** uma `SolicitacaoRegulacao` `Autorizada`
- **Quando** executo `ExecutarSolicitacaoRegulacaoCommand(id)`
- **Então** situação = `Executada` e `ProcedimentoExecutado` é emitido (I-8).

**Cenário 7 — Cancelamento de autorizada devolve cota**
- **Dada** uma `SolicitacaoRegulacao` `Autorizada` (cota consumida e vaga reservada)
- **Quando** executo `CancelarSolicitacaoRegulacaoCommand(id, "Paciente desistiu")`
- **Então** situação = `Cancelada`, a reserva no SISREG é liberada e a `Cota` é **devolvida** (incrementada) (I-7).

**Cenário 8 — Transição sobre solicitação encerrada**
- **Dada** uma `SolicitacaoRegulacao` `Executada` (ou `Negada`/`Cancelada`)
- **Quando** executo `Autorizar`/`Negar`/`Cancelar`
- **Então** ocorre `InvalidOperationException` (situação encerrada — I-9).

**Cenário 9 — Fila ordenada por prioridade**
- **Dadas** solicitações `Urgente` e `Eletiva` pendentes
- **Quando** executo `ListarFilaDeRegulacaoQuery`
- **Então** a `Urgente` aparece antes da `Eletiva` (ordenação por `Prioridade` desc, depois `DataSolicitacao` asc) (I-11).

**Cenário 10 — Fila tenant-scoped**
- **Dadas** solicitações pendentes no tenant A e no tenant B
- **Quando** executo `ListarFilaDeRegulacaoQuery` no contexto do tenant A
- **Então** retornam **apenas** as solicitações do tenant A.

---

## 13. Casos de Borda

- **B-1.** `CodigoSigtap`/`Justificativa` vazios em `Solicitar` ⇒ `ArgumentException` (I-1).
- **B-2.** Paciente sem CNS confirmado ⇒ `InvalidOperationException` (I-2).
- **B-3.** `Autorizar` com `Cota.Disponivel == 0` ⇒ `InvalidOperationException` (I-3); situação inalterada.
- **B-4.** `Autorizar` solicitação já `Autorizada`/`Negada`/`Executada` ⇒ `InvalidOperationException` (fora de análise).
- **B-5.** `Devolver` solicitação `Autorizada` ⇒ `InvalidOperationException` (devolução só de `Solicitada` — I-6).
- **B-6.** `Executar` solicitação não `Autorizada` ⇒ `InvalidOperationException` (I-8).
- **B-7.** `Cancelar` solicitação `Executada` ⇒ `InvalidOperationException` (encerrada — I-7/I-9).
- **B-8.** `Cancelar` solicitação `Solicitada`/`Devolvida` (sem reserva) ⇒ permitido; **não** há cota a devolver (não houve consumo).
- **B-9.** `Cancelar` solicitação `Autorizada` ⇒ devolve cota e libera SISREG (I-7).
- **B-10.** SISREG indisponível em `Autorizar` (após retries Polly) ⇒ a autorização falha (não decrementa cota nem grava `ProtocoloSisreg`); situação permanece `Solicitada`/`Devolvida`.
- **B-11.** Reserva idempotente: reautorizar/recancelar a mesma solicitação não duplica reserva nem cota (idempotência por `SolicitacaoRegulacaoId`).
- **B-12.** `Prioridade` inválida ⇒ rejeitada pelo validator (`IsInEnum`) (I-11).
- **B-13.** Leitura da fila/solicitação sem claim `saude.regular` ⇒ negado por padrão (403 auditado) (I-12).
- **B-14.** Gravar com `TenantId` do cliente ⇒ ignorado/lançado; `TenantId` vem do interceptor.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo Saude (mapa de domínio: SolicitacaoRegulacao raiz; VOs Procedimento/Prioridade/Cota; evento SolicitacaoAutorizada; Cenário 6 BDD; integração SISREG/SIGTAP; cota e reserva de vaga). |

<!-- manifest
commands: SolicitarRegulacao, AutorizarSolicitacaoRegulacao, NegarSolicitacaoRegulacao, DevolverSolicitacaoRegulacao, ExecutarSolicitacaoRegulacao, CancelarSolicitacaoRegulacao
queries: ObterSolicitacaoRegulacaoPorId, ListarFilaDeRegulacao
domainEvents: RegulacaoSolicitada, SolicitacaoAutorizada, SolicitacaoNegada, SolicitacaoDevolvida, ProcedimentoExecutado, SolicitacaoCancelada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
