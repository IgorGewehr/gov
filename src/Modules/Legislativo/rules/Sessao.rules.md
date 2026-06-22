---
modulo: Legislativo
agregado: Sessao
contexto: Legislativo (Processo Legislativo Municipal — sessoes plenarias)
poder: Legislativo
schema: legislativo
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/88 art. 29 (Lei Organica)", "CF/88 art. 29-A (numero de vereadores por faixa populacional)", "CF/88 arts. 59-69 (processo legislativo - simetria)", "Lei Organica Municipal (LOM)", "Regimento Interno (Resolucao)"]
---

# Sessao — Regras-as-Code (Rules-as-Code)

> Reuniao plenaria da Camara Municipal (ordinaria ou extraordinaria), com Expediente
> (comunicacoes) e Ordem do Dia (deliberacao). Instala-se apenas com **quorum de instalacao =
> maioria absoluta** dos membros, controla a **lista de presencas** e organiza as materias a
> deliberar. Garante trilha imutavel de presencas para prova juridica e LAI. Conformidade com
> CF/88 art. 29, a Lei Organica Municipal e o Regimento Interno. Este arquivo e **normativo e
> versionado**; o codigo (agregado `Sessao`, handlers, validators, EF config, testes) e
> consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Sessao (`Sessao`) | Reuniao plenaria da Camara. Raiz de agregado. |
| Tipo de Sessao (`TipoSessao`) | Especie da reuniao (ordinaria / extraordinaria). VO/enum. |
| Sessao Ordinaria (`SessaoOrdinaria`) | Reuniao plenaria regular (calendario). |
| Sessao Extraordinaria (`SessaoExtraordinaria`) | Reuniao convocada fora do calendario ordinario. |
| Data e Hora (`DataHora`) | Momento agendado/realizado da sessao. VO. |
| Expediente (`Expediente`) | Parte da sessao destinada a comunicacoes. |
| Ordem do Dia (`OrdemDoDia` / `IncluirNaOrdemDoDia`) | Parte da sessao destinada a deliberacao das proposicoes. |
| Quorum de Instalacao (`QuorumInstalacao`) | Numero minimo para instalar a sessao = maioria absoluta (> 50% dos membros). VO. |
| Total de Membros (`TotalMembros`) | Numero de vereadores da Camara (base do quorum). |
| Presenca (`Presenca` / `RegistrarPresenca`) | Registro de comparecimento de um vereador. Entidade do agregado. |
| Verificar Quorum (`VerificarQuorum`) | Apuracao do atingimento do quorum de instalacao. |
| Abrir Sessao (`Abrir`) | Instalar a sessao apos verificacao de quorum. |
| Suspender / Reabrir (`Suspender` / `Reabrir`) | Pausa e retomada da sessao em andamento. |
| Encerrar (`Encerrar`) | Encerramento (terminal) da sessao. |
| Ata Eletronica (`AtaEletronica`) | Registro formal do que ocorreu na sessao. |
| Mesa Diretora (`MesaDiretora`) | Orgao de direcao dos trabalhos da sessao. |
| Tenant (`TenantId`) | Ente publico (Camara Municipal) dono do registro. |
| Situacao (`Situacao` : `SituacaoSessao`) | Estado atual da sessao. |

---

## 2. Modelo

- **Identidade:** `SessaoId` — `readonly record struct SessaoId(Guid Value)`; fabrica `SessaoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Sessao : AggregateRoot<SessaoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valida via factory `Agendar(...)`.

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (Camara) dono do registro. | `private set` |
| `Tipo` | `TipoSessao` (VO/enum) | Ordinaria / extraordinaria. | `private set` |
| `DataHora` | `DateTimeOffset` (VO `DataHora`) | Momento agendado/realizado. | `private set` |
| `TotalMembros` | `int` | Numero de vereadores (base do quorum). | `private set` |
| `QuorumInstalacao` | `int` (VO/calculado) | Maioria absoluta = `TotalMembros / 2 + 1`. | derivada |
| `Situacao` | `SituacaoSessao` | Situacao atual. | `private set` |
| `Presencas` | `IReadOnlyList<Presenca>` | Registros de presenca (trilha imutavel). | colecao encapsulada |
| `OrdemDoDia` | `IReadOnlyList<ItemOrdemDoDia>` | Proposicoes pautadas para deliberacao. | colecao encapsulada |

### Constantes / formulas de dominio

- `QuorumInstalacao` = **maioria absoluta** = `TotalMembros / 2 + 1` (> 50% dos membros). Constante de regra: quorum de instalacao = maioria absoluta (CF art. 29 / Regimento).
- `TotalMembros` parametrizado por tenant (numero de vereadores conforme faixa populacional — CF art. 29-A). **Nunca** hardcoded.

### Entidades internas

- **`Presenca`** — `PresencaId`, `VereadorId`, `RegistradaEm` (`DateTimeOffset`). Vinculada a `SessaoId`. Trilha **imutavel** (append-only).
- **`ItemOrdemDoDia`** — `ItemId`, `ProposicaoId`, ordem/numero de pauta. Vinculado a `SessaoId`.

### Value Objects (referenciados)

- `DataHora` — momento da sessao; nao default (validado em `Agendar`).
- `TipoSessao` — especie da sessao.

### Enum `TipoSessao`

| Valor | Numerico | Descricao |
|---|---|---|
| `Ordinaria` | 1 | Sessao ordinaria (calendario regular). |
| `Extraordinaria` | 2 | Sessao extraordinaria (convocacao especial). |

### Enum `SituacaoSessao`

| Valor | Numerico | Descricao |
|---|---|---|
| `Agendada` | 1 | Agendada (estado inicial), aguardando instalacao. |
| `Aberta` | 2 | Instalada/aberta (quorum atingido). |
| `Suspensa` | 3 | Suspensa temporariamente. |
| `Encerrada` | 4 | Encerrada (terminal). |
| `Cancelada` | 5 | Cancelada por falta de quorum ou decisao da Mesa (terminal). |

> **Conjuntos de referencia usados nas guardas:**
> - **Terminal** = { `Encerrada`, `Cancelada` }.
> - **Em andamento** = { `Aberta`, `Suspensa` }.
> - **Quorum atingido** = `quantidade de Presencas registradas >= QuorumInstalacao`.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Na agendamento (`Agendar`), `DataHora`, `Tipo` e `TotalMembros` (> 0) sao obrigatorios; a situacao inicial e `Agendada`.
- **I-2.** `QuorumInstalacao` = maioria absoluta = `TotalMembros / 2 + 1` (somente leitura, derivado de `TotalMembros`).
- **I-3.** O registro de `Presenca` (`RegistrarPresenca`) so e permitido enquanto a sessao **nao** for terminal; cada `VereadorId` registra presenca **uma unica vez** (idempotente; duplicata e ignorada/rejeitada).
- **I-4.** `VerificarQuorum` apura se `quantidade de Presencas >= QuorumInstalacao` e emite `QuorumVerificado(id, atingido)`.
- **I-5.** A abertura (`Abrir`) so ocorre a partir de `Agendada` **e** com quorum atingido (`Presencas >= QuorumInstalacao`); passa a `Aberta` e emite `SessaoAberta`. Sem quorum, `InvalidOperationException`.
- **I-6.** A inclusao em Ordem do Dia (`IncluirNaOrdemDoDia`) so ocorre com a sessao `Agendada` ou `Aberta` (nao terminal); pauta uma `Proposicao` por item.
- **I-7.** A suspensao (`Suspender`) exige situacao `Aberta`; a reabertura (`Reabrir`) exige situacao `Suspensa`.
- **I-8.** O encerramento (`Encerrar`) exige situacao `Aberta` ou `Suspensa`; passa a `Encerrada` (terminal) e emite `SessaoEncerrada`.
- **I-9.** O cancelamento (`Cancelar`) exige situacao `Agendada` (ex.: ausencia de quorum no horario); passa a `Cancelada` (terminal).
- **I-10.** Estados terminais (`Encerrada`, `Cancelada`) nao admitem novas transicoes nem novos registros de presenca.
- **I-11.** A trilha de presencas e **imutavel** (append-only): presencas nao podem ser removidas/alteradas apos o registro (prova juridica + LAI).
- **I-12.** `TotalMembros` reflete o numero de vereadores da Camara conforme faixa populacional (CF art. 29-A), parametrizado por tenant.

---

## 4. Maquina de Estados

Tabela: Estado origem → comando/metodo → Estado destino | guarda | evento emitido.

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Agendar` | `Agendada` | `DataHora`, `Tipo` validos; `TotalMembros > 0` | — |
| `Agendada` \| `Aberta` | `IncluirNaOrdemDoDia` | (inalterada) | nao terminal | — |
| ≠ terminal | `RegistrarPresenca` | (inalterada) | `VereadorId` ainda nao presente | — |
| `Agendada` \| `Aberta` \| `Suspensa` | `VerificarQuorum` | (inalterada) | — | `QuorumVerificado` |
| `Agendada` | `Abrir` | `Aberta` | `Presencas >= QuorumInstalacao` | `SessaoAberta` |
| `Aberta` | `Suspender` | `Suspensa` | situacao == `Aberta` | — |
| `Suspensa` | `Reabrir` | `Aberta` | situacao == `Suspensa` | — |
| `Aberta` \| `Suspensa` | `Encerrar` | `Encerrada` | situacao ∈ {`Aberta`,`Suspensa`} | `SessaoEncerrada` |
| `Agendada` | `Cancelar` | `Cancelada` | situacao == `Agendada` | — |

> Observacoes:
> - `Abrir` chama `VerificarQuorum` (ou usa a contagem corrente de presencas) **antes** da transicao; quorum insuficiente => `InvalidOperationException` (sessao nao instala).
> - O **Painel Eletronico de Votacao** captura presencas em tempo real, alimentando `RegistrarPresenca` (idempotente por `VereadorId`).

---

## 5. Comandos (escrita)

### 5.1 AgendarSessao

- **Command:** `AgendarSessaoCommand(int Tipo, DateTimeOffset DataHora, int TotalMembros) : ICommand<Guid>`.
- **Entrada (DTO):** `Tipo` (`TipoSessao`), `DataHora`, `TotalMembros`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pre-condicoes:** `request` nao nulo; `TotalMembros > 0`; `DataHora` valida.
- **Efeito:** cria via `Sessao.Agendar(tenant.TenantId, tipo, dataHora, totalMembros)`; `sessoes.Adicionar(sessao)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Sessao` em situacao `Agendada`; retorna `sessao.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `ArgumentException` (`TotalMembros <= 0` ou `DataHora` invalida).
- **Evento de dominio:** — (sem evento dedicado nesta versao).

### 5.2 IncluirNaOrdemDoDia

- **Command:** `IncluirNaOrdemDoDiaCommand(Guid SessaoId, Guid ProposicaoId) : ICommand`.
- **Entrada (DTO):** `SessaoId`, `ProposicaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; sessao nao terminal (I-6).
- **Efeito:** `sessao.IncluirNaOrdemDoDia(new ProposicaoId(request.ProposicaoId))`; `SaveChangesAsync`.
- **Pos-condicoes:** novo `ItemOrdemDoDia` vinculado a sessao.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou terminal).
- **Evento de dominio:** —.

### 5.3 RegistrarPresenca

- **Command:** `RegistrarPresencaCommand(Guid SessaoId, Guid VereadorId) : ICommand`.
- **Entrada (DTO):** `SessaoId`, `VereadorId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; nao terminal; `VereadorId` ainda nao presente (I-3, idempotente).
- **Efeito:** `sessao.RegistrarPresenca(new VereadorId(request.VereadorId), agora)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Presenca` na trilha imutavel (ou no-op se ja registrada).
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou terminal).
- **Evento de dominio:** —.

### 5.4 VerificarQuorum

- **Command:** `VerificarQuorumCommand(Guid SessaoId) : ICommand<bool>`.
- **Entrada (DTO):** `SessaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe.
- **Efeito:** `bool atingido = sessao.VerificarQuorum()`; `SaveChangesAsync`.
- **Pos-condicoes:** retorna se o quorum foi atingido; emite `QuorumVerificado`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada).
- **Evento de dominio:** `QuorumVerificado(Id, atingido)`.

### 5.5 AbrirSessao

- **Command:** `AbrirSessaoCommand(Guid SessaoId) : ICommand`.
- **Entrada (DTO):** `SessaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; situacao == `Agendada`; quorum atingido (`Presencas >= QuorumInstalacao`) (I-5).
- **Efeito:** `sessao.Abrir()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Aberta`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada, situacao ≠ `Agendada` ou sem quorum).
- **Evento de dominio:** `SessaoAberta(Id)`.

### 5.6 SuspenderSessao

- **Command:** `SuspenderSessaoCommand(Guid SessaoId) : ICommand`.
- **Entrada (DTO):** `SessaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; situacao == `Aberta` (I-7).
- **Efeito:** `sessao.Suspender()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Suspensa`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `Aberta`).
- **Evento de dominio:** —.

### 5.7 ReabrirSessao

- **Command:** `ReabrirSessaoCommand(Guid SessaoId) : ICommand`.
- **Entrada (DTO):** `SessaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; situacao == `Suspensa` (I-7).
- **Efeito:** `sessao.Reabrir()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Aberta`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `Suspensa`).
- **Evento de dominio:** —.

### 5.8 EncerrarSessao

- **Command:** `EncerrarSessaoCommand(Guid SessaoId) : ICommand`.
- **Entrada (DTO):** `SessaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; situacao ∈ {`Aberta`,`Suspensa`} (I-8).
- **Efeito:** `sessao.Encerrar()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Encerrada` (terminal); habilita geracao da ata eletronica.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao invalida).
- **Evento de dominio:** `SessaoEncerrada(Id)`.

### 5.9 CancelarSessao

- **Command:** `CancelarSessaoCommand(Guid SessaoId) : ICommand`.
- **Entrada (DTO):** `SessaoId`.
- **Dependencias do handler:** `ISessaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; sessao existe; situacao == `Agendada` (I-9).
- **Efeito:** `sessao.Cancelar()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Cancelada` (terminal).
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `Agendada`).
- **Evento de dominio:** —.

---

## 6. Consultas (leitura)

### 6.1 ObterSessaoPorId

- **Query:** `ObterSessaoPorIdQuery(Guid SessaoId) : IQuery<SessaoDetalhe>`.
- **Entrada:** `SessaoId`.
- **Handler:** `ObterSessaoPorIdHandler(ISessaoRepository sessoes)`; chama `sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), ct)`.
- **Projecao (DTO):** `SessaoDetalhe(Guid Id, string Tipo, DateTimeOffset DataHora, int TotalMembros, int QuorumInstalacao, int Presentes, string Situacao, IReadOnlyList<ItemOrdemDoDiaResumo> OrdemDoDia)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarSessoesAgendadas

- **Query:** `ListarSessoesAgendadasQuery() : IQuery<IReadOnlyList<SessaoResumo>>`.
- **Entrada:** (sem parametros; tenant-scoped).
- **Handler:** `ListarSessoesAgendadasHandler(ISessaoRepository sessoes)`; chama `sessoes.ListarPorSituacaoAsync(SituacaoSessao.Agendada, ct)`.
- **Projecao (DTO):** `SessaoResumo(Guid Id, string Tipo, DateTimeOffset DataHora, string Situacao)`.
- **Filtros:** situacao `Agendada`; **sempre tenant-scoped**.
- **Pre-condicoes:** `request` nao nulo.

### 6.3 ObterPresencasDaSessao

- **Query:** `ObterPresencasDaSessaoQuery(Guid SessaoId) : IQuery<IReadOnlyList<PresencaResumo>>`.
- **Entrada:** `SessaoId`.
- **Handler:** `ObterPresencasDaSessaoHandler(ISessaoRepository sessoes)`; le a trilha imutavel de presencas.
- **Projecao (DTO):** `PresencaResumo(Guid VereadorId, DateTimeOffset RegistradaEm)`.
- **Filtros:** por `SessaoId`; **sempre tenant-scoped**; alimenta transparencia (LAI).
- **Pre-condicoes:** `request` nao nulo.

---

## 7. Eventos

### Dominio (in-process, MediatR; assembly `...Legislativo.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `QuorumVerificado` | `(SessaoId, bool atingido)` | `Sessao.VerificarQuorum` |
| `SessaoAberta` | `(SessaoId)` | `Sessao.Abrir` |
| `SessaoEncerrada` | `(SessaoId)` | `Sessao.Encerrar` |

### Integracao (publica via `*.Contracts` + Outbox; assembly `...Legislativo.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `SessaoRealizadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid SessaoId, string Tipo, DateTimeOffset DataHora)` | `AbrirSessaoHandler` / `EncerrarSessaoHandler` (transparencia LAI/APIs abertas) |

### Integracao (consome)

- Nenhum (esta versao nao consome Integration Events de outros modulos no agregado `Sessao`).

---

## 8. Validacoes (FluentValidation)

### AgendarSessaoValidator (`AbstractValidator<AgendarSessaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Tipo` | `IsInEnum()` | (padrao: tipo de sessao invalido) |
| `DataHora` | `NotEmpty()` | (padrao: data/hora obrigatoria) |
| `TotalMembros` | `GreaterThan(0)` | (padrao: total de membros deve ser positivo) |

### IncluirNaOrdemDoDiaValidator (`AbstractValidator<IncluirNaOrdemDoDiaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `SessaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `ProposicaoId` | `NotEmpty()` | (padrao: proposicao obrigatoria) |

### RegistrarPresencaValidator (`AbstractValidator<RegistrarPresencaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `SessaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `VereadorId` | `NotEmpty()` | (padrao: vereador obrigatorio) |

### VerificarQuorumValidator / AbrirSessaoValidator / SuspenderSessaoValidator / ReabrirSessaoValidator / EncerrarSessaoValidator / CancelarSessaoValidator

| Campo | Regra | Mensagem |
|---|---|---|
| `SessaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |

> Consultas (`ObterSessaoPorId`, `ListarSessoesAgendadas`, `ObterPresencasDaSessao`) nao possuem validador dedicado; protecao por existencia no repositorio e tenant-scope.

---

## 9. Persistencia (EF Core 8)

- **Schema:** `legislativo` (isolado por modulo). **DbContext:** o do modulo Legislativo. **Migrations:** por modulo.
- **Tabela:** `Sessao` (raiz de agregado); tabelas filhas `Presenca`, `ItemOrdemDoDia`.

| Coluna | Tipo logico | Conversor / observacao |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `SessaoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Tipo` | `int` | enum `TipoSessao`. |
| `DataHora` | `datetimeoffset` | owned/conversor de `DataHora`. |
| `TotalMembros` | `int` | base do quorum. |
| `Situacao` | `int` | enum `SituacaoSessao`. |

- **Nao persistidas (calculadas):** `QuorumInstalacao` (derivada de `TotalMembros / 2 + 1`).
- **Tabela `Presenca`** (trilha imutavel, append-only): `Id` (PK), `SessaoId` (FK), `VereadorId` (`Guid`), `RegistradaEm` (`datetimeoffset`).
- **Tabela `ItemOrdemDoDia`:** `Id` (PK), `SessaoId` (FK), `ProposicaoId` (`Guid`), `Ordem` (`int`).
- **Indices:**
  - PK em `Id`.
  - Indice em `(TenantId, Situacao)` para `ListarSessoesAgendadas`.
  - Indice em `(TenantId, DataHora)` para consulta por data.
  - Indice **unico** em `Presenca(SessaoId, VereadorId)` (uma presenca por vereador por sessao — I-3).
  - Indices em `ItemOrdemDoDia(SessaoId)`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no dominio).
- **Outbox:** tabela Outbox do contexto Legislativo para `SessaoRealizadaIntegrationEvent` (consistencia transacional).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** `Sessao` implementa `IMustHaveTenant`. A **Camara e tenant DISTINTO do Executivo**. `TenantId` carimbado na insercao pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravacao cross-tenant **lanca excecao**. `ITenantContext` resolvido do JWT por requisicao.
- **RBAC (policy-based + RBAC `Usuario → Departamento → Roles`, negar por padrao):**
  - Agendar / abrir / suspender / reabrir / encerrar / cancelar / pautar: papel da **Mesa Diretora** ou **Secretaria Legislativa** (ex.: `Legislativo.Sessao.Conduzir`).
  - Registrar presenca: papel de **Vereador** ou captura via **Painel Eletronico** autenticado (ex.: `Legislativo.Sessao.RegistrarPresenca`).
  - Consultas: papel de **leitura legislativa** (ex.: `Legislativo.Sessao.Ler`); presencas e pautas publicas via transparencia (LAI).
  - Modulo Legislativo e **ativavel por tenant**; requisicao a tenant sem licenca → 404/403 auditado.
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuario, IP, timestamp) em toda mutacao (`Agendar`, `IncluirNaOrdemDoDia`, `RegistrarPresenca`, `VerificarQuorum`, `Abrir`, `Suspender`, `Reabrir`, `Encerrar`, `Cancelar`). A trilha de **presencas e imutavel (append-only)** para prova juridica e LAI — destinada ao Tribunal de Contas (TCE-RS).
- **LAI/Transparencia:** sessoes, presencas e ordem do dia sao dados publicos (transmissao + ata eletronica).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integracoes Governamentais

- **Painel Eletronico de Votacao:** captura **presenca** em tempo real (WebSocket/event stream), alimentando `RegistrarPresenca`. Idempotencia por `VereadorId` (uma presenca por vereador por sessao). ACL + resiliencia.
- **Transmissao + ata eletronica:** registro e publicacao da sessao.
- **SAPL/Interlegis:** interoperacao e importacao de pautas/sessoes (ACL + idempotencia).
- **APIs abertas (LAI):** publica `SessaoRealizadaIntegrationEvent` e resultados para o portal de transparencia.
- **Resiliencia:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explicito de erros atras de Anti-Corruption Layer.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Instalacao de sessao (README Cenario 1)**
- **Dado** uma `SessaoOrdinaria` agendada com 11 membros (`QuorumInstalacao` = 6)
- **Quando** comparecerem 6 vereadores (`RegistrarPresenca` x6) e executo `AbrirSessaoCommand(id)`
- **Entao** o quorum de instalacao (maioria absoluta) e atingido, situacao = `Aberta` e o evento `SessaoAberta` e emitido.

**Cenario 2 — Falta de quorum**
- **Dado** uma sessao agendada com 11 membros e apenas 5 presencas
- **Quando** executo `AbrirSessaoCommand(id)`
- **Entao** ocorre `InvalidOperationException` (quorum insuficiente) e a sessao permanece `Agendada`.

**Cenario 3 — Verificacao de quorum**
- **Dado** uma sessao com presencas registradas
- **Quando** executo `VerificarQuorumCommand(id)`
- **Entao** retorna `true`/`false` conforme `Presencas >= QuorumInstalacao` e o evento `QuorumVerificado` e emitido.

**Cenario 4 — Idempotencia de presenca**
- **Dado** uma sessao agendada
- **Quando** o mesmo `VereadorId` registra presenca duas vezes
- **Entao** apenas uma `Presenca` e persistida (idempotente; viola/ignora o indice unico).

**Cenario 5 — Inclusao em Ordem do Dia**
- **Dado** uma sessao `Agendada` ou `Aberta`
- **Quando** executo `IncluirNaOrdemDoDiaCommand(sessaoId, proposicaoId)`
- **Entao** a proposicao e pautada na Ordem do Dia da sessao.

**Cenario 6 — Suspensao e reabertura**
- **Dado** uma sessao `Aberta`
- **Quando** executo `SuspenderSessaoCommand(id)` e depois `ReabrirSessaoCommand(id)`
- **Entao** a situacao passa por `Suspensa` e retorna a `Aberta`.

**Cenario 7 — Encerramento**
- **Dado** uma sessao `Aberta` (ou `Suspensa`)
- **Quando** executo `EncerrarSessaoCommand(id)`
- **Entao** situacao = `Encerrada` e o evento `SessaoEncerrada` e emitido.

**Cenario 8 — Cancelamento por falta de quorum no horario**
- **Dado** uma sessao `Agendada` sem quorum
- **Quando** executo `CancelarSessaoCommand(id)`
- **Entao** situacao = `Cancelada`.

**Cenario 9 — Registro de presenca em sessao terminal**
- **Dado** uma sessao `Encerrada`
- **Quando** executo `RegistrarPresencaCommand(id, vereadorId)`
- **Entao** ocorre `InvalidOperationException` (sessao terminal).

**Cenario 10 — Consulta de presencas (transparencia)**
- **Dado** uma sessao com varias presencas
- **Quando** executo `ObterPresencasDaSessaoQuery(id)` no contexto do tenant
- **Entao** retorna a trilha imutavel de presencas, tenant-scoped.

**Cenario 11 — Isolamento entre tenants**
- **Dado** sessoes do tenant A (Camara) e sessoes de outro tenant B
- **Quando** executo `ListarSessoesAgendadasQuery()` no contexto do tenant A
- **Entao** retornam **apenas** as sessoes do tenant A.

---

## 13. Casos de Borda

- **B-1.** `TotalMembros <= 0` em `Agendar` ⇒ `ArgumentException`/validator (I-1).
- **B-2.** `QuorumInstalacao` para `TotalMembros` impar (ex.: 11) = 6; para par (ex.: 10) = 6 (maioria absoluta = metade + 1).
- **B-3.** `Abrir` com presencas exatamente iguais ao quorum (`Presentes == QuorumInstalacao`) ⇒ permitido (>=).
- **B-4.** `Abrir` com `Presentes == QuorumInstalacao - 1` ⇒ falha (quorum insuficiente).
- **B-5.** `Abrir` sobre sessao ja `Aberta`/`Encerrada` ⇒ `InvalidOperationException` (so a partir de `Agendada`).
- **B-6.** `Suspender` sobre sessao `Agendada` ou `Encerrada` ⇒ falha (so a partir de `Aberta`).
- **B-7.** `Reabrir` sobre sessao `Aberta` ⇒ falha (so a partir de `Suspensa`).
- **B-8.** `Cancelar` sobre sessao `Aberta`/`Suspensa`/`Encerrada` ⇒ falha (so a partir de `Agendada`).
- **B-9.** `Encerrar` sobre sessao `Agendada` ⇒ falha (exige `Aberta`/`Suspensa`).
- **B-10.** Presenca duplicada do mesmo `VereadorId` ⇒ idempotente; indice unico `(SessaoId, VereadorId)` previne registro duplo.
- **B-11.** `IncluirNaOrdemDoDia` sobre sessao terminal ⇒ `InvalidOperationException`.
- **B-12.** `SessaoRealizadaIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).
- **B-13.** Consultas em tenant da Camara nao retornam dados do tenant do Executivo (isolamento por Global Query Filter).

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial — derivada do README do modulo Legislativo (linguagem ubiqua, mapa de dominio, regras criticas, integracoes, cenarios BDD e fontes legais) para o agregado `Sessao`. |

<!-- manifest
commands: AgendarSessao, IncluirNaOrdemDoDia, RegistrarPresenca, VerificarQuorum, AbrirSessao, SuspenderSessao, ReabrirSessao, EncerrarSessao, CancelarSessao
queries: ObterSessaoPorId, ListarSessoesAgendadas, ObterPresencasDaSessao
domainEvents: QuorumVerificado, SessaoAberta, SessaoEncerrada
integrationEventsPublished: SessaoRealizadaIntegrationEvent
integrationEventsConsumed: 
-->
