---
modulo: Educacao
agregado: DiarioClasse
contexto: Educacao (registro pedagogico — frequencia, notas e apuracao de resultado)
poder: Executivo
schema: educacao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LDB Lei 9.394/1996 (atualizada pela Lei 14.945/2024) — frequencia min. 75%, 200 dias letivos, 800h/1.000h", "Censo Escolar/EducaCenso (INEP) — Situacao do Aluno", "LGPD Lei 13.709/2018 art. 14 (dados de menores)", "CF/1988 arts. 205-214"]
---

# DiarioClasse — Regras-as-Code (Rules-as-Code)

> Registro digital de frequencia, conteudos e notas de uma matricula em uma turma. Apura o
> resultado anual: aprovacao exige frequencia >= 75% da carga horaria; abaixo disso, reprovacao
> por frequencia. Mantem vinculo 1-1 com a `Matricula` e alimenta a Situacao do Aluno (2a etapa
> do Censo). Lancamentos de frequencia/nota tem auditoria imutavel. Este arquivo e **normativo e
> versionado**; o codigo (`DiarioClasse.cs`, handlers, validators, EF config, testes) e
> consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Diario de Classe (`DiarioClasse`) | Registro digital de frequencia, conteudos e notas; raiz de agregado. |
| Registro de Frequencia (`RegistroFrequencia` / `RegistrarFrequencia`) | Entidade-filha: presenca/falta do aluno por aula/dia. |
| Registro de Nota (`RegistroNota` / `LancarNota`) | Entidade-filha: nota de um componente curricular por periodo. |
| Registro de Aula (`RegistroAula` / `RegistrarAula`) | Entidade-filha: conteudo ministrado e dia letivo. |
| Frequencia (`Frequencia` / `PercentualFrequencia`) | Percentual de presenca; minimo 75% da carga horaria para aprovacao. |
| Dia Letivo (`DiaLetivo`) | Dia de efetivo trabalho escolar com aluno (min. 200/ano). |
| Carga Horaria (`CargaHoraria`) | Horas anuais (min. 800h Fundamental / 1.000h Medio). |
| Apuracao de Resultado (`ApurarResultado` / `ResultadoApurado`) | Calculo final de aprovacao/reprovacao do aluno no ano. |
| Resultado (`Resultado` : `ResultadoAluno`) | Aprovado / Reprovado / Reprovado por frequencia. |
| Situacao do Diario (`Situacao` : `SituacaoDiario`) | Estado do diario: Aberto/Apurado. |
| Matricula (`MatriculaId`) | Matricula vinculada (1-1). |
| Componente Curricular (`ComponenteCurricularId`) | Disciplina/unidade de ensino da nota. |
| Tenant (`TenantId`) | Ente municipal (rede de ensino) dono do registro. |

---

## 2. Modelo

- **Identidade:** `DiarioClasseId` — `readonly record struct DiarioClasseId(Guid Value)`; fabrica `DiarioClasseId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `DiarioClasse : AggregateRoot<DiarioClasseId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valido via factory `Abrir(...)`.

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente municipal/rede) dono do registro. | `private set` |
| `MatriculaId` | `MatriculaId` (VO/Id) | Matricula vinculada (1-1). | `private set` |
| `CargaHorariaTotal` | `int` | Carga horaria anual de referencia (800h/1.000h). | `private set` |
| `Situacao` | `SituacaoDiario` (enum) | Situacao atual do diario. | `private set` |
| `Resultado` | `ResultadoAluno?` (enum) | Resultado apurado (nulo ate apuracao). | `private set` |
| `Frequencias` | `IReadOnlyCollection<RegistroFrequencia>` | Registros de frequencia (entidades-filhas). | colecao encapsulada |
| `Notas` | `IReadOnlyCollection<RegistroNota>` | Registros de nota (entidades-filhas). | colecao encapsulada |
| `Aulas` | `IReadOnlyCollection<RegistroAula>` | Registros de aula/dia letivo (entidades-filhas). | colecao encapsulada |

### Constantes

- `FrequenciaMinimaAprovacao` = `0.75` (75% da carga horaria — LDB). Constante de dominio que parametriza a aprovacao por frequencia.
- `DiasLetivosMinimos` = `200` (dias letivos/ano — LDB).

### Entidades-filhas e Value Objects (referenciados)

- `RegistroFrequencia` — `(DateOnly Data, bool Presente, int CargaHorariaAula)`.
- `RegistroNota` — `(ComponenteCurricularId Componente, string Periodo, decimal Valor)`.
- `RegistroAula` — `(DateOnly Data, string Conteudo, bool DiaLetivo)`.
- `MatriculaId` — `record struct` com `Value : Guid`.
- `ComponenteCurricularId` — `record struct` com `Value : Guid`.

### Enum `SituacaoDiario`

| Valor | Numerico | Descricao |
|---|---|---|
| `Aberto` | 1 | Diario aberto, admitindo lancamentos (estado inicial). |
| `Apurado` | 2 | Resultado anual apurado (terminal para lancamentos do periodo). |

### Enum `ResultadoAluno`

| Valor | Numerico | Descricao |
|---|---|---|
| `Aprovado` | 1 | Frequencia >= 75% e medias suficientes. |
| `Reprovado` | 2 | Reprovado por nota/medias insuficientes. |
| `ReprovadoPorFrequencia` | 3 | Frequencia < 75% da carga horaria. |

> **Conjuntos de referencia usados nas guardas:**
> - **Aberto** = { `Aberto` } (admite lancamentos).
> - **Apurado** = { `Apurado` } (resultado fechado).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A frequencia e apurada por periodo; `PercentualFrequencia` = presencas ponderadas pela carga horaria / `CargaHorariaTotal`.
- **I-2.** Frequencia >= `FrequenciaMinimaAprovacao` (75%) e condicao para aprovacao; abaixo disso o resultado e `ReprovadoPorFrequencia` (LDB).
- **I-3.** Lancamentos (`RegistrarFrequencia`, `LancarNota`, `RegistrarAula`) so ocorrem com diario `Aberto`; caso contrario `InvalidOperationException`.
- **I-4.** A apuracao de resultado (`ApurarResultado`) exige diario `Aberto`; ao apurar passa a `Apurado` e emite `ResultadoApurado`.
- **I-5.** Aprovacao exige frequencia >= 75% **e** medias suficientes nos componentes; do contrario `Reprovado` (por nota) ou `ReprovadoPorFrequencia` (por frequencia).
- **I-6.** Diario `Apurado` (terminal de lancamentos) nao admite novos lancamentos do periodo nem nova apuracao.
- **I-7.** O diario mantem vinculo **1-1** com a `Matricula` (uma matricula -> um diario).
- **I-8.** Toda nota lancada referencia um `ComponenteCurricularId` valido e um `Periodo` informado.
- **I-9.** O calendario exige minimo de **200 dias letivos** (`DiasLetivosMinimos`); a contagem de `RegistroAula` com `DiaLetivo = true` e a base da apuracao de cumprimento.
- **I-10.** O resultado apurado alimenta a Situacao do Aluno (2a etapa do Censo) da `Matricula` vinculada.
- **I-11.** Lancamentos de frequencia/nota sao **auditaveis de forma imutavel** (autor, timestamp, valor anterior) e o diario e versionado.
- **I-12.** O professor so lanca na **propria turma** (escopo RBAC aplicado na operacao).

---

## 4. Maquina de Estados

Tabela: Estado origem -> comando/metodo -> Estado destino | guarda | evento emitido.

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Abrir` | `Aberto` | matricula nao nula; cargaHoraria > 0 | — |
| `Aberto` | `RegistrarFrequencia` | `Aberto` | situacao == `Aberto` | `FrequenciaRegistrada` |
| `Aberto` | `LancarNota` | `Aberto` | situacao == `Aberto`; componente/periodo informados | `NotaLancada` |
| `Aberto` | `RegistrarAula` | `Aberto` | situacao == `Aberto` | — |
| `Aberto` | `ApurarResultado` | `Apurado` | situacao == `Aberto` | `ResultadoApurado` |

> Observacoes:
> - `RegistrarFrequencia`, `LancarNota` e `RegistrarAula` nao alteram a situacao; apenas adicionam registros-filhos e (frequencia/nota) emitem evento.
> - `ApurarResultado` calcula `Resultado` (`Aprovado`/`Reprovado`/`ReprovadoPorFrequencia`) e fecha o diario em `Apurado`.

---

## 5. Comandos (escrita)

### 5.1 AbrirDiarioClasse

- **Command:** `AbrirDiarioClasseCommand(Guid MatriculaId, int CargaHorariaTotal) : ICommand<Guid>`.
- **Entrada (DTO):** `MatriculaId`, `CargaHorariaTotal`.
- **Dependencias do handler:** `IDiarioClasseRepository`, `IMatriculaRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Matricula existe e esta `Ativa`, senao `InvalidOperationException("Matricula invalida para abertura de diario.")`.
  - Nao existe diario para a matricula (vinculo 1-1 — I-7), senao `InvalidOperationException("Diario ja existe para a matricula.")`.
  - `CargaHorariaTotal` > 0.
- **Efeito:** cria via `DiarioClasse.Abrir(tenant.TenantId, matriculaId, cargaHorariaTotal)`; `diarios.Adicionar(diario)`; `SaveChangesAsync`.
- **Pos-condicoes:** novo `DiarioClasse` em situacao `Aberto`; retorna `diario.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (matricula invalida ou diario duplicado).
- **Evento de dominio:** nenhum nesta versao.

### 5.2 RegistrarFrequencia

- **Command:** `RegistrarFrequenciaCommand(Guid DiarioClasseId, DateOnly Data, bool Presente, int CargaHorariaAula) : ICommand`.
- **Entrada (DTO):** `DiarioClasseId`, `Data`, `Presente`, `CargaHorariaAula`.
- **Dependencias do handler:** `IDiarioClasseRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Diario existe (`ObterPorIdAsync`), senao `InvalidOperationException("Diario nao encontrado.")`.
  - Situacao == `Aberto` (I-3).
- **Efeito:** `diario.RegistrarFrequencia(data, presente, cargaHorariaAula)`; `SaveChangesAsync`.
- **Pos-condicoes:** novo `RegistroFrequencia` na colecao; situacao inalterada.
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrado ou diario nao aberto).
- **Evento de dominio:** `FrequenciaRegistrada(Id, data)`.

### 5.3 LancarNota

- **Command:** `LancarNotaCommand(Guid DiarioClasseId, Guid ComponenteCurricularId, string Periodo, decimal Valor) : ICommand`.
- **Entrada (DTO):** `DiarioClasseId`, `ComponenteCurricularId`, `Periodo`, `Valor`.
- **Dependencias do handler:** `IDiarioClasseRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Diario existe, senao `InvalidOperationException("Diario nao encontrado.")`.
  - Situacao == `Aberto` (I-3); `ComponenteCurricularId` valido e `Periodo` informado (I-8).
- **Efeito:** `diario.LancarNota(componenteId, periodo, valor)`; `SaveChangesAsync`.
- **Pos-condicoes:** novo `RegistroNota` na colecao; situacao inalterada.
- **Excecoes:** `ArgumentNullException` (request); `ArgumentException` (componente/periodo invalidos); `InvalidOperationException` (nao encontrado ou diario nao aberto).
- **Evento de dominio:** `NotaLancada(Id, componenteId, periodo)`.

### 5.4 RegistrarAula

- **Command:** `RegistrarAulaCommand(Guid DiarioClasseId, DateOnly Data, string Conteudo, bool DiaLetivo) : ICommand`.
- **Entrada (DTO):** `DiarioClasseId`, `Data`, `Conteudo`, `DiaLetivo`.
- **Dependencias do handler:** `IDiarioClasseRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Diario existe, senao `InvalidOperationException("Diario nao encontrado.")`.
  - Situacao == `Aberto` (I-3).
- **Efeito:** `diario.RegistrarAula(data, conteudo, diaLetivo)`; `SaveChangesAsync`.
- **Pos-condicoes:** novo `RegistroAula` na colecao (conta para os 200 dias letivos quando `DiaLetivo = true` — I-9); situacao inalterada.
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrado ou diario nao aberto).
- **Evento de dominio:** nenhum nesta versao.

### 5.5 ApurarResultado

- **Command:** `ApurarResultadoCommand(Guid DiarioClasseId) : ICommand`.
- **Entrada (DTO):** `DiarioClasseId`.
- **Dependencias do handler:** `IDiarioClasseRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Diario existe, senao `InvalidOperationException("Diario nao encontrado.")`.
  - Situacao == `Aberto` (I-4).
- **Efeito:** `diario.ApurarResultado()` (calcula frequencia e medias -> `Aprovado`/`Reprovado`/`ReprovadoPorFrequencia`); `SaveChangesAsync`; publica **Integration Event** `ResultadoApuradoIntegrationEvent(...)` via `publisher.Publish`.
- **Pos-condicoes:** situacao `Apurado`; `Resultado` preenchido; resultado alimenta a Situacao do Aluno da matricula vinculada (I-10).
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrado ou ja apurado).
- **Evento de dominio:** `ResultadoApurado(Id, resultado)`.
- **Evento de integracao (publica):** `ResultadoApuradoIntegrationEvent`.

---

## 6. Consultas (leitura)

### 6.1 ObterDiarioDaMatricula

- **Query:** `ObterDiarioDaMatriculaQuery(Guid MatriculaId) : IQuery<DiarioClasseResumo?>`.
- **Entrada:** `MatriculaId`.
- **Handler:** `ObterDiarioDaMatriculaHandler(IDiarioClasseRepository diarios)`; chama `diarios.ObterPorMatriculaAsync(new MatriculaId(request.MatriculaId), ct)`.
- **Projecao (DTO):** `DiarioClasseResumo(Guid Id, Guid MatriculaId, string Situacao, decimal PercentualFrequencia, string? Resultado, int DiasLetivosRegistrados)`.
  - `PercentualFrequencia` calculado das frequencias; `Resultado` de `diario.Resultado?.ToString()`.
- **Filtros:** por `MatriculaId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ObterFrequenciaDoDiario

- **Query:** `ObterFrequenciaDoDiarioQuery(Guid DiarioClasseId) : IQuery<FrequenciaConsolidada>`.
- **Entrada:** `DiarioClasseId`.
- **Handler:** `ObterFrequenciaDoDiarioHandler(IDiarioClasseRepository diarios)`; chama `diarios.ObterPorIdAsync(...)` e consolida as frequencias.
- **Projecao (DTO):** `FrequenciaConsolidada(Guid DiarioClasseId, decimal PercentualFrequencia, int AulasComputadas, bool AtingiuMinimo)` (`AtingiuMinimo` = `PercentualFrequencia >= 0.75`).
- **Filtros:** por `DiarioClasseId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo.

---

## 7. Eventos

### Dominio (in-process, MediatR; assembly `...Educacao.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `FrequenciaRegistrada` | `(DiarioClasseId, DateOnly data)` | `DiarioClasse.RegistrarFrequencia` |
| `NotaLancada` | `(DiarioClasseId, ComponenteCurricularId componente, string periodo)` | `DiarioClasse.LancarNota` |
| `ResultadoApurado` | `(DiarioClasseId, ResultadoAluno resultado)` | `DiarioClasse.ApurarResultado` |

### Integracao (publica via `*.Contracts` + Outbox; assembly `...Educacao.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `ResultadoApuradoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid DiarioClasseId, Guid MatriculaId, string Resultado)` | `ApurarResultadoHandler` |

> Consumido por **Transparencia** (e demais modulos interessados na movimentacao academica). Idempotente por `EventId`.

### Integracao (consome)

- Nenhum diretamente no agregado DiarioClasse nesta versao. (No modulo, eventos de **Administracao** (`ContratoAssinado`) e **Patrimonio** (veiculos) sao consumidos pelos agregados de merenda/transporte.)

---

## 8. Validacoes (FluentValidation)

### AbrirDiarioClasseValidator (`AbstractValidator<AbrirDiarioClasseCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `MatriculaId` | `NotEmpty()` | "Matricula obrigatoria." |
| `CargaHorariaTotal` | `GreaterThan(0)` | "Carga horaria total deve ser maior que zero." |

### RegistrarFrequenciaValidator (`AbstractValidator<RegistrarFrequenciaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `DiarioClasseId` | `NotEmpty()` | "Identificador do diario obrigatorio." |
| `Data` | `NotEmpty()` | "Data da frequencia obrigatoria." |
| `CargaHorariaAula` | `GreaterThan(0)` | "Carga horaria da aula deve ser maior que zero." |

### LancarNotaValidator (`AbstractValidator<LancarNotaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `DiarioClasseId` | `NotEmpty()` | "Identificador do diario obrigatorio." |
| `ComponenteCurricularId` | `NotEmpty()` | "Componente curricular obrigatorio." |
| `Periodo` | `NotEmpty()` + `MaximumLength(20)` | "Periodo obrigatorio (max. 20 caracteres)." |
| `Valor` | `InclusiveBetween(0, 10)` | "Nota deve estar entre 0 e 10." |

### ApurarResultadoValidator (`AbstractValidator<ApurarResultadoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `DiarioClasseId` | `NotEmpty()` | "Identificador do diario obrigatorio." |

> `RegistrarAulaCommand` reusa validacao minima (`DiarioClasseId`/`Data` `NotEmpty`); demais protecoes por invariantes de dominio. As queries nao possuem validador.

---

## 9. Persistencia (EF Core 8)

- **Schema:** `educacao` (isolado por modulo). **DbContext:** o do modulo Educacao. **Migrations:** por modulo.
- **Tabela:** `DiarioClasse` (raiz de agregado) + tabelas-filhas `RegistroFrequencia`, `RegistroNota`, `RegistroAula` (owned/relacionadas a raiz).

| Coluna (DiarioClasse) | Tipo logico | Conversor / observacao |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `DiarioClasseId` <-> `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `MatriculaId` | `Guid` | conversor de `MatriculaId` <-> `Guid`. |
| `CargaHorariaTotal` | `int` | carga horaria anual de referencia. |
| `Situacao` | `int` | enum `SituacaoDiario` (persistido por valor numerico). |
| `Resultado` | `int?` | enum `ResultadoAluno` (nulo ate apuracao). |

- **Tabelas-filhas:**
  - `RegistroFrequencia(Id, DiarioClasseId, Data, Presente, CargaHorariaAula)`.
  - `RegistroNota(Id, DiarioClasseId, ComponenteCurricularId, Periodo, Valor)`.
  - `RegistroAula(Id, DiarioClasseId, Data, Conteudo, DiaLetivo)`.
- **Indices:**
  - PK em `Id`.
  - Indice **unico** em `(TenantId, MatriculaId)` — vinculo 1-1 diario<->matricula (I-7).
  - Indice em `(TenantId, DiarioClasseId, Data)` nas tabelas-filhas para consolidacao de frequencia/dias letivos.
- **Nao persistidas (calculadas):** `PercentualFrequencia` (derivada das frequencias e `CargaHorariaTotal`).
- **Conversores (VO/Id):** Fluent API (sem data annotations no dominio).
- **Outbox:** tabela Outbox do contexto Educacao para `ResultadoApuradoIntegrationEvent` (consistencia transacional com o estado).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** `DiarioClasse` implementa `IMustHaveTenant`. `TenantId` carimbado na insercao pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Isolamento por **tenant = ente municipal**; dados nao cruzam tenants; gravacao cross-tenant **lanca excecao**. `ITenantContext` resolvido do JWT por requisicao.
- **RBAC (policy-based + RBAC `Usuario -> Departamento -> Roles`, negar por padrao):**
  - Lancar frequencia/nota/aula e apurar resultado: papel de **professor**, que **lanca apenas a propria turma** (I-12) (ex.: `Educacao.Diario.Lancar`).
  - Consulta de diario/frequencia: papeis de **secretaria escolar**/**gestor da rede** (ex.: `Educacao.Diario.Ler`).
  - Modulo Educacao e **ativavel por tenant** (Executivo); requisicao a tenant sem licenca -> 404/403 auditado.
- **LGPD (art. 14 — menores):** frequencia, notas e resultado sao dados de crianca/adolescente; tratamento no **melhor interesse da crianca**; minimizacao (coletar so o necessario ao Censo/registro pedagogico); base legal de cumprimento de obrigacao legal e politica publica. Acesso a diario/notas gera **trilha de acesso** (quem leu, quando, por que).
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha imutavel de **lancamentos de frequencia/nota** (autor, timestamp, **valor anterior**) e **versionamento do diario** (I-11), alem dos logs de exportacao ao INEP/FNDE — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integracoes Governamentais

- **EducaCenso/INEP (saida):** o resultado apurado alimenta a **Situacao do Aluno** (2a etapa do Censo) da matricula vinculada; o cumprimento de **200 dias letivos** e da **carga horaria** (800h/1.000h) e apurado dos `RegistroAula`. Validacao e reconciliacao por `CodigoInep`; **idempotencia por codigo INEP**; geracao versionada por ano-base.
- **Transparencia (saida, via Contracts):** `ResultadoApuradoIntegrationEvent` publicado via Outbox; idempotente por `EventId`.
- **Resiliencia:** integracoes externas resilientes (Polly: retry + circuit breaker) atras de **Anti-Corruption Layer**; eventos via Outbox; idempotencia por `EventId`.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Aprovacao por frequencia e nota**
- **Dado** um aluno com 75% de frequencia e medias suficientes
- **Quando** executo `ApurarResultadoCommand(diarioId)`
- **Entao** a situacao registrada e `Aprovado`, o evento `ResultadoApurado` e emitido e `ResultadoApuradoIntegrationEvent` e publicado.

**Cenario 2 — Reprovacao por frequencia**
- **Dado** um aluno com frequencia de 70%
- **Quando** executo `ApurarResultadoCommand(diarioId)`
- **Entao** o resultado e `ReprovadoPorFrequencia` (I-2).

**Cenario 3 — Reprovacao por nota**
- **Dado** um aluno com frequencia >= 75% mas medias insuficientes
- **Quando** executo `ApurarResultadoCommand(diarioId)`
- **Entao** o resultado e `Reprovado` (por nota).

**Cenario 4 — Lancamento em diario aberto**
- **Dado** um diario `Aberto`
- **Quando** executo `RegistrarFrequenciaCommand(diarioId, data, true, cargaHorariaAula)`
- **Entao** o registro e adicionado e o evento `FrequenciaRegistrada` e emitido.

**Cenario 5 — Lancamento em diario apurado**
- **Dado** um diario `Apurado`
- **Quando** executo `RegistrarFrequenciaCommand` / `LancarNotaCommand` / `RegistrarAulaCommand`
- **Entao** ocorre `InvalidOperationException` (diario apurado nao admite lancamentos — I-6).

**Cenario 6 — Nova apuracao bloqueada**
- **Dado** um diario `Apurado`
- **Quando** executo `ApurarResultadoCommand` novamente
- **Entao** ocorre `InvalidOperationException` (situacao != `Aberto`).

**Cenario 7 — Vinculo 1-1 com a matricula**
- **Dado** uma matricula que ja possui diario
- **Quando** executo `AbrirDiarioClasseCommand(matriculaId, cargaHoraria)`
- **Entao** ocorre `InvalidOperationException("Diario ja existe para a matricula.")` (I-7).

**Cenario 8 — Lancamento de nota fora da faixa**
- **Dado** um diario `Aberto`
- **Quando** executo `LancarNotaCommand(diarioId, componenteId, "1Bim", 11)`
- **Entao** a validacao rejeita (`InclusiveBetween(0,10)`).

**Cenario 9 — Consulta tenant-scoped**
- **Dado** diarios do tenant A e do tenant B
- **Quando** executo `ObterDiarioDaMatriculaQuery(matriculaId)` no contexto do tenant A
- **Entao** retorna **apenas** o diario do tenant A, projetado em `DiarioClasseResumo`.

---

## 13. Casos de Borda

- **B-1.** `CargaHorariaTotal <= 0` em `Abrir` => rejeitado por `GreaterThan(0)` no validator.
- **B-2.** `CargaHorariaAula <= 0` em `RegistrarFrequencia` => rejeitado por `GreaterThan(0)` no validator.
- **B-3.** Frequencia exatamente em 75% => `Aprovado` por frequencia (limite inclusivo: `>= 0.75`).
- **B-4.** Frequencia em 74,9% => `ReprovadoPorFrequencia` (estritamente abaixo de 75%).
- **B-5.** `Valor` de nota = 0 ou = 10 => aceito (limites inclusivos); 10,1 ou negativo => rejeitado.
- **B-6.** Lancar/apurar em diario inexistente => `InvalidOperationException("Diario nao encontrado.")`.
- **B-7.** Abrir diario para matricula nao `Ativa` => `InvalidOperationException` (matricula invalida).
- **B-8.** Apurar com menos de 200 dias letivos registrados => apuracao sinaliza nao cumprimento do calendario (I-9) conforme regra do calendario.
- **B-9.** Professor tentando lancar em turma que nao e a sua => bloqueado pelo RBAC (I-12).
- **B-10.** Edicao de uma nota ja lancada => registra trilha imutavel com valor anterior (I-11).
- **B-11.** `ResultadoApuradoIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).
- **B-12.** Acessar diario de outro tenant => bloqueado pelo Global Query Filter / excecao cross-tenant.

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial — derivada do README do modulo Educacao (mapa de dominio: agregado DiarioClasse com `RegistroFrequencia`/`RegistroNota`/`RegistroAula`, eventos `FrequenciaRegistrada`/`NotaLancada`/`ResultadoApurado`; regras de frequencia >= 75%, 200 dias letivos, 800h/1.000h; Integration Event `ResultadoApurado` a Transparencia). |

<!-- manifest
commands: AbrirDiarioClasse, RegistrarFrequencia, LancarNota, RegistrarAula, ApurarResultado, RegistrarFrequenciaTurma, LancarNotasTurma
queries: ObterDiarioDaMatricula, ObterFrequenciaDoDiario, ObterDiarioDaTurma, ObterBoletim, ObterHistoricoEscolar
domainEvents: FrequenciaRegistrada, NotaLancada, ResultadoApurado
integrationEventsPublished: ResultadoApuradoIntegrationEvent
integrationEventsConsumed: 
-->
