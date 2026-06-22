---
modulo: Educacao
agregado: Matricula
contexto: Educacao (ciclo de matricula — vinculo aluno-turma-escola e Censo)
poder: Executivo
schema: educacao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LDB Lei 9.394/1996 (atualizada pela Lei 14.945/2024)", "Censo Escolar/EducaCenso (INEP) — Matricula Inicial e Situacao do Aluno", "FUNDEB EC 108/2020 + Lei 14.113/2020 (matriculas ponderadas)", "CF/1988 arts. 205-214 (ensino fundamental obrigatorio)", "LGPD Lei 13.709/2018 art. 14 (dados de menores)"]
---

# Matricula — Regras-as-Code (Rules-as-Code)

> Vinculo do aluno a uma turma de uma escola na data de referencia do Censo (Matricula Inicial).
> Percorre os estados `Ativa -> Transferida/Concluida/Abandono` e e a base das matriculas ponderadas
> do FUNDEB e da Situacao do Aluno (2a etapa do Censo). Mantem vinculo 1-1 com o `DiarioClasse`.
> Este arquivo e **normativo e versionado**; o codigo (`Matricula.cs`, handlers, validators, EF config,
> testes) e consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Matricula (`Matricula`) | Vinculo aluno-turma-escola; raiz de agregado. |
| Matricula Inicial (`MatricularAluno` / `DataReferencia`) | 1a etapa do Censo: vinculo aluno-turma-escola na data de referencia. |
| Situacao do Aluno (`RegistrarSituacaoDoAluno`) | 2a etapa do Censo: rendimento (aprovado/reprovado) + movimento (transferido/abandono/falecido). |
| Situacao da Matricula (`Situacao` : `SituacaoMatricula`) | Estado do vinculo: Ativa/Transferida/Concluida/Abandono. |
| Data de Referencia (`DataReferencia` : `DateOnly`) | Data de referencia do Censo (Matricula Inicial). |
| Rematricula (`Rematricular`) | Renovacao de matricula do aluno apto ao ano seguinte. |
| Transferencia (`Transferir` / `Transferida`) | Movimento do aluno para outra escola/turma. |
| Conclusao (`Concluir` / `Concluida`) | Encerramento por conclusao da etapa/ano. |
| Abandono (`RegistrarAbandono` / `Abandono`) | Movimento de abandono escolar. |
| Aluno (`AlunoId`) | Aluno vinculado (sujeito de dados, menor — LGPD art. 14). |
| Turma (`TurmaId`) | Turma de enturmacao do aluno. |
| Escola (`EscolaId`) | Escola da matricula. |
| Matricula Ponderada (`MatriculaPonderada`) | Matricula multiplicada pelo fator FUNDEB. |
| Codigo INEP (`CodigoInep`) | Identificador nacional do aluno/turma para o Censo. |
| Tenant (`TenantId`) | Ente municipal (rede de ensino) dono do registro. |

---

## 2. Modelo

- **Identidade:** `MatriculaId` — `readonly record struct MatriculaId(Guid Value)`; fabrica `MatriculaId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Matricula : AggregateRoot<MatriculaId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valida via factory `MatricularAluno(...)`.

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente municipal/rede) dono do registro. | `private set` |
| `AlunoId` | `AlunoId` (VO/Id) | Aluno vinculado. | `private set` |
| `TurmaId` | `TurmaId` (VO/Id) | Turma de enturmacao. | `private set` |
| `EscolaId` | `EscolaId` (VO/Id) | Escola da matricula. | `private set` |
| `DataReferencia` | `DateOnly` | Data de referencia do Censo (Matricula Inicial). | `private set` |
| `Situacao` | `SituacaoMatricula` (enum) | Situacao atual da matricula. | `private set` |
| `SituacaoDoAluno` | `SituacaoDoAluno?` (VO) | Rendimento + movimento (2a etapa do Censo), nulo ate o registro. | `private set` |

### Value Objects (referenciados)

- `AlunoId` — `record struct` com `Value : Guid`.
- `TurmaId` — `record struct` com `Value : Guid`.
- `EscolaId` — `record struct` com `Value : Guid`.
- `SituacaoDoAluno` — VO com `Rendimento` (aprovado/reprovado) e `Movimento` (transferido/abandono/falecido) para a 2a etapa do Censo.

### Enum `SituacaoMatricula`

| Valor | Numerico | Descricao |
|---|---|---|
| `Ativa` | 1 | Matricula ativa (estado inicial). |
| `Transferida` | 2 | Aluno transferido (terminal). |
| `Concluida` | 3 | Etapa/ano concluido (terminal). |
| `Abandono` | 4 | Abandono escolar (terminal). |

> **Conjuntos de referencia usados nas guardas:**
> - **Ativa** = { `Ativa` }.
> - **Encerrada** = { `Transferida`, `Concluida`, `Abandono` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Na matricula a situacao inicial e `Ativa` e e emitido o evento `AlunoMatriculado(id, alunoId, turmaId, escolaId)`.
- **I-2.** `AlunoId`, `TurmaId`, `EscolaId` e `DataReferencia` sao obrigatorios na matricula (`ArgumentNullException.ThrowIfNull`).
- **I-3.** Um aluno **nao pode ter duas matriculas ativas conflitantes** no mesmo periodo/turno (vedacao de vinculo conflitante).
- **I-4.** A Matricula Inicial reflete a **data de referencia** do Censo (`DataReferencia`).
- **I-5.** Transicoes (`Transferir`, `Concluir`, `RegistrarAbandono`) exigem situacao `Ativa`; caso contrario `InvalidOperationException`.
- **I-6.** Transferencia leva a `Transferida` e emite `AlunoTransferido`.
- **I-7.** Conclusao leva a `Concluida` e emite `MatriculaEncerrada`.
- **I-8.** Abandono leva a `Abandono` e emite `MatriculaEncerrada`.
- **I-9.** Estados terminais (`Transferida`, `Concluida`, `Abandono`) nao admitem novas transicoes.
- **I-10.** O encerramento do ano letivo exige `SituacaoDoAluno` preenchida para todos os matriculados (Situacao do Aluno — 2a etapa do Censo).
- **I-11.** A enturmacao respeita a invariante da `Turma`: matriculados <= vagas (controlada na operacao de matricula).
- **I-12.** Apenas matriculas validas no Censo geram repasse FUNDEB (matricula ponderada por fator).
- **I-13.** A matricula mantem vinculo 1-1 com o `DiarioClasse` (uma matricula -> um vinculo no diario).

---

## 4. Maquina de Estados

Tabela: Estado origem -> comando/metodo -> Estado destino | guarda | evento emitido.

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `MatricularAluno` | `Ativa` | aluno/turma/escola/data nao nulos; sem matricula ativa conflitante | `AlunoMatriculado` |
| `Ativa` | `Transferir` | `Transferida` | situacao == `Ativa` | `AlunoTransferido` |
| `Ativa` | `Concluir` | `Concluida` | situacao == `Ativa` | `MatriculaEncerrada` |
| `Ativa` | `RegistrarAbandono` | `Abandono` | situacao == `Ativa` | `MatriculaEncerrada` |
| `Ativa` | `RegistrarSituacaoDoAluno` | `Ativa` | situacao == `Ativa`; rendimento+movimento informados | — |

> Observacoes:
> - `RegistrarSituacaoDoAluno` registra a 2a etapa do Censo sem alterar a situacao da matricula.
> - `Rematricular` cria uma **nova** `Matricula` `Ativa` para o ano seguinte (aluno apto), nao e transicao da matricula atual.
> - Nao ha movimento de "falecido" como estado de matricula nesta versao; e capturado no VO `SituacaoDoAluno` (movimento).

---

## 5. Comandos (escrita)

### 5.1 MatricularAluno

- **Command:** `MatricularAlunoCommand(Guid AlunoId, Guid TurmaId, Guid EscolaId, DateOnly DataReferencia) : ICommand<Guid>`.
- **Entrada (DTO):** `AlunoId`, `TurmaId`, `EscolaId`, `DataReferencia`.
- **Dependencias do handler:** `IMatriculaRepository`, `ITurmaRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Turma existe e possui vaga (`matriculados < vagas`), senao `InvalidOperationException("Turma sem vaga.")` (I-11).
  - Aluno sem matricula ativa conflitante no mesmo periodo/turno (I-3), senao `InvalidOperationException("Aluno ja possui matricula ativa conflitante.")`.
- **Efeito:** cria via `Matricula.MatricularAluno(tenant.TenantId, alunoId, turmaId, escolaId, dataReferencia)`; `matriculas.Adicionar(matricula)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Matricula` em situacao `Ativa`; vinculo 1-1 com `DiarioClasse` criado; retorna `matricula.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (turma sem vaga ou matricula conflitante).
- **Evento de dominio:** `AlunoMatriculado(id, alunoId, turmaId, escolaId)` (emitido no construtor via factory).
- **Evento de integracao (publica):** `AlunoMatriculadoIntegrationEvent`.

### 5.2 TransferirAluno

- **Command:** `TransferirAlunoCommand(Guid MatriculaId) : ICommand`.
- **Entrada (DTO):** `MatriculaId`.
- **Dependencias do handler:** `IMatriculaRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Matricula existe (`ObterPorIdAsync`), senao `InvalidOperationException("Matricula nao encontrada.")`.
  - Situacao == `Ativa` (I-5).
- **Efeito:** `matricula.Transferir()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Transferida`.
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrada ou nao ativa).
- **Evento de dominio:** `AlunoTransferido(Id)`.

### 5.3 EncerrarMatricula

- **Command:** `EncerrarMatriculaCommand(Guid MatriculaId, MotivoEncerramento Motivo) : ICommand`.
- **Entrada (DTO):** `MatriculaId`, `Motivo` (`Conclusao` | `Abandono`).
- **Dependencias do handler:** `IMatriculaRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Matricula existe, senao `InvalidOperationException("Matricula nao encontrada.")`.
  - Situacao == `Ativa` (I-5).
- **Efeito:** `matricula.Concluir()` ou `matricula.RegistrarAbandono()` conforme `Motivo`; `SaveChangesAsync`; publica **Integration Event** `MatriculaEncerradaIntegrationEvent(...)` via `publisher.Publish`.
- **Pos-condicoes:** situacao `Concluida` ou `Abandono`; encerramento publicado aos demais modulos (Transparencia).
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrada ou nao ativa).
- **Evento de dominio:** `MatriculaEncerrada(Id)`.
- **Evento de integracao (publica):** `MatriculaEncerradaIntegrationEvent`.

### 5.4 RegistrarSituacaoDoAluno

- **Command:** `RegistrarSituacaoDoAlunoCommand(Guid MatriculaId, Rendimento Rendimento, Movimento Movimento) : ICommand`.
- **Entrada (DTO):** `MatriculaId`, `Rendimento` (aprovado/reprovado), `Movimento` (transferido/abandono/falecido).
- **Dependencias do handler:** `IMatriculaRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Matricula existe, senao `InvalidOperationException("Matricula nao encontrada.")`.
  - Situacao == `Ativa` (I-5).
- **Efeito:** `matricula.RegistrarSituacaoDoAluno(rendimento, movimento)`; `SaveChangesAsync`.
- **Pos-condicoes:** `SituacaoDoAluno` preenchida (pre-requisito para o encerramento do ano letivo — I-10).
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrada ou nao ativa).
- **Evento de dominio:** nenhum nesta versao.

### 5.5 RematricularAluno

- **Command:** `RematricularAlunoCommand(Guid MatriculaAnteriorId, Guid TurmaDestinoId, DateOnly DataReferencia) : ICommand<Guid>`.
- **Entrada (DTO):** `MatriculaAnteriorId`, `TurmaDestinoId`, `DataReferencia`.
- **Dependencias do handler:** `IMatriculaRepository`, `ITurmaRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Matricula anterior existe e aluno apto ao ano seguinte (situacao `Concluida` ou regra de aprovacao).
  - Turma destino com vaga (I-11).
- **Efeito:** cria **nova** `Matricula.MatricularAluno(...)` (Rematricula) para a turma destino; `matriculas.Adicionar(novaMatricula)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Matricula` `Ativa`; retorna `novaMatricula.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (aluno inapto, turma sem vaga ou matricula conflitante).
- **Evento de dominio:** `AlunoMatriculado(...)`.
- **Evento de integracao (publica):** `AlunoMatriculadoIntegrationEvent`.

---

## 6. Consultas (leitura)

### 6.1 ObterMatriculasDoAluno

- **Query:** `ObterMatriculasDoAlunoQuery(Guid AlunoId) : IQuery<IReadOnlyList<MatriculaResumo>>`.
- **Entrada:** `AlunoId`.
- **Handler:** `ObterMatriculasDoAlunoHandler(IMatriculaRepository matriculas)`; chama `matriculas.ListarPorAlunoAsync(new AlunoId(request.AlunoId), ct)`.
- **Projecao (DTO):** `MatriculaResumo(Guid Id, Guid AlunoId, Guid TurmaId, Guid EscolaId, string Situacao, DateOnly DataReferencia)`.
- **Filtros:** por `AlunoId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarMatriculasInicialDaTurma

- **Query:** `ListarMatriculasInicialDaTurmaQuery(Guid TurmaId, DateOnly DataReferencia) : IQuery<IReadOnlyList<MatriculaResumo>>`.
- **Entrada:** `TurmaId`, `DataReferencia`.
- **Handler:** `ListarMatriculasInicialDaTurmaHandler(IMatriculaRepository matriculas)`; chama `matriculas.ListarPorTurmaEDataReferenciaAsync(...)`.
- **Projecao (DTO):** `MatriculaResumo(...)` (mesma projecao de 6.1) — base da Matricula Inicial do Censo.
- **Filtros:** por `TurmaId` e `DataReferencia`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo.

---

## 7. Eventos

### Dominio (in-process, MediatR; assembly `...Educacao.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `AlunoMatriculado` | `(MatriculaId, AlunoId, TurmaId, EscolaId)` | `Matricula.MatricularAluno` (construtor) |
| `AlunoTransferido` | `(MatriculaId)` | `Matricula.Transferir` |
| `MatriculaEncerrada` | `(MatriculaId)` | `Matricula.Concluir` / `Matricula.RegistrarAbandono` |

### Integracao (publica via `*.Contracts` + Outbox; assembly `...Educacao.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `AlunoMatriculadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid MatriculaId, Guid AlunoId, Guid TurmaId, Guid EscolaId)` | `MatricularAlunoHandler` / `RematricularAlunoHandler` |
| `MatriculaEncerradaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid MatriculaId, string Motivo)` | `EncerrarMatriculaHandler` |

> Consumidos por **Transparencia** e por **Financas**/**Patrimonio** (matriculas alimentam FUNDEB/PNAE/PNATE — coeficientes e estoque da merenda/combustivel das rotas).

### Integracao (consome)

- Nenhum diretamente no agregado Matricula nesta versao. (No modulo, eventos de **Administracao** (`ContratoAssinado`) e **Patrimonio** (veiculos) sao consumidos pelos agregados de merenda/transporte.)

---

## 8. Validacoes (FluentValidation)

### MatricularAlunoValidator (`AbstractValidator<MatricularAlunoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `AlunoId` | `NotEmpty()` | "Aluno obrigatorio." |
| `TurmaId` | `NotEmpty()` | "Turma obrigatoria." |
| `EscolaId` | `NotEmpty()` | "Escola obrigatoria." |
| `DataReferencia` | `NotEmpty()` | "Data de referencia do Censo obrigatoria." |

### EncerrarMatriculaValidator (`AbstractValidator<EncerrarMatriculaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `MatriculaId` | `NotEmpty()` | "Identificador da matricula obrigatorio." |
| `Motivo` | `IsInEnum()` | "Motivo de encerramento invalido." |

### RegistrarSituacaoDoAlunoValidator (`AbstractValidator<RegistrarSituacaoDoAlunoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `MatriculaId` | `NotEmpty()` | "Identificador da matricula obrigatorio." |
| `Rendimento` | `IsInEnum()` | "Rendimento invalido." |
| `Movimento` | `IsInEnum()` | "Movimento invalido." |

> `TransferirAlunoCommand` e `RematricularAlunoCommand` reusam validacao minima (`MatriculaId`/identificadores `NotEmpty`); demais protecoes por invariantes de dominio. As queries nao possuem validador.

---

## 9. Persistencia (EF Core 8)

- **Schema:** `educacao` (isolado por modulo). **DbContext:** o do modulo Educacao. **Migrations:** por modulo.
- **Tabela:** `Matricula` (raiz de agregado).

| Coluna | Tipo logico | Conversor / observacao |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `MatriculaId` <-> `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `AlunoId` | `Guid` | conversor de `AlunoId` <-> `Guid`. |
| `TurmaId` | `Guid` | conversor de `TurmaId` <-> `Guid`. |
| `EscolaId` | `Guid` | conversor de `EscolaId` <-> `Guid`. |
| `DataReferencia` | `date` | `DateOnly` (data de referencia do Censo). |
| `Situacao` | `int` | enum `SituacaoMatricula` (persistido por valor numerico). |
| `SituacaoDoAluno_Rendimento` | `int?` | owned `SituacaoDoAluno` (rendimento), nulo ate registro. |
| `SituacaoDoAluno_Movimento` | `int?` | owned `SituacaoDoAluno` (movimento), nulo ate registro. |

- **Indices:**
  - PK em `Id`.
  - Indice em `(TenantId, AlunoId)` para `ListarPorAlunoAsync`.
  - Indice em `(TenantId, TurmaId, DataReferencia)` para a Matricula Inicial do Censo.
  - Indice **unico** parcial em `(TenantId, AlunoId, DataReferencia, TurnoTurma)` onde `Situacao = Ativa` — impede duas matriculas ativas conflitantes no mesmo periodo/turno (I-3).
- **Conversores (VO/Id):** Fluent API (sem data annotations no dominio).
- **Outbox:** tabela Outbox do contexto Educacao para `AlunoMatriculadoIntegrationEvent` e `MatriculaEncerradaIntegrationEvent` (consistencia transacional com o estado).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** `Matricula` implementa `IMustHaveTenant`. `TenantId` carimbado na insercao pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Isolamento por **tenant = ente municipal**; dados nao cruzam tenants; gravacao cross-tenant **lanca excecao**. `ITenantContext` resolvido do JWT por requisicao.
- **RBAC (policy-based + RBAC `Usuario -> Departamento -> Roles`, negar por padrao):**
  - Matricular/transferir/encerrar/rematricular: papeis de **secretaria escolar** e **gestor da rede** (ex.: `Educacao.Matricula.Gerir`).
  - Consulta de matriculas: papel de **leitura** (ex.: `Educacao.Matricula.Ler`).
  - Modulo Educacao e **ativavel por tenant** (Executivo); requisicao a tenant sem licenca -> 404/403 auditado.
- **LGPD (art. 14 — menores):** a matricula vincula `AlunoId` (crianca/adolescente). Tratamento no **melhor interesse da crianca**; minimizacao (coletar so o necessario ao Censo/matricula); base legal de cumprimento de obrigacao legal e politica publica. Acesso a matriculas de um aluno gera **trilha de acesso** (quem leu, quando, por que).
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuario, IP, timestamp) em toda mutacao (`MatricularAluno`, `Transferir`, `Concluir`, `RegistrarAbandono`, `RegistrarSituacaoDoAluno`, `Rematricular`) e nos logs de exportacao ao INEP/FNDE — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integracoes Governamentais

- **EducaCenso/INEP (saida):** a matricula alimenta a **Matricula Inicial** (1a etapa) e a **Situacao do Aluno** (2a etapa) do leiaute posicional anual (formulario Aluno). Validacao e reconciliacao por `CodigoInep`; **idempotencia por codigo INEP**; geracao versionada por ano-base. O encerramento do ano letivo exige Situacao do Aluno preenchida para todos os matriculados (I-10).
- **FNDE/FUNDEB (saida indireta):** matriculas validas no Censo geram repasse (matricula ponderada por fator FUNDEB — EC 108/2020 + Lei 14.113/2020); alimentam tambem PNAE/PNATE; prestacao de contas via SiGPC/Contas Online.
- **Transparencia (saida, via Contracts):** `AlunoMatriculadoIntegrationEvent` e `MatriculaEncerradaIntegrationEvent` publicados via Outbox; idempotentes por `EventId`.
- **Resiliencia:** integracoes externas resilientes (Polly: retry + circuit breaker) atras de **Anti-Corruption Layer**; eventos via Outbox; idempotencia por `CodigoInep`/`EventId`.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Matricula inicial em turma com vaga**
- **Dado** uma turma com vaga disponivel e um aluno sem matricula ativa conflitante
- **Quando** executo `MatricularAlunoCommand(alunoId, turmaId, escolaId, dataReferencia)`
- **Entao** e criada uma `Matricula` em situacao `Ativa`, o evento `AlunoMatriculado` e emitido e `AlunoMatriculadoIntegrationEvent` e publicado.

**Cenario 2 — Turma sem vaga**
- **Dado** uma turma cujos matriculados ja igualam as vagas
- **Quando** executo `MatricularAlunoCommand`
- **Entao** ocorre `InvalidOperationException("Turma sem vaga.")` (I-11).

**Cenario 3 — Matricula ativa conflitante**
- **Dado** um aluno ja com matricula `Ativa` no mesmo periodo/turno
- **Quando** executo `MatricularAlunoCommand` para o mesmo periodo/turno
- **Entao** ocorre `InvalidOperationException` (duas matriculas ativas conflitantes vedadas — I-3).

**Cenario 4 — Transferencia de aluno**
- **Dado** uma matricula `Ativa`
- **Quando** executo `TransferirAlunoCommand(matriculaId)`
- **Entao** situacao = `Transferida` e o evento `AlunoTransferido` e emitido.

**Cenario 5 — Encerramento por conclusao**
- **Dado** uma matricula `Ativa`
- **Quando** executo `EncerrarMatriculaCommand(matriculaId, Conclusao)`
- **Entao** situacao = `Concluida`, o evento `MatriculaEncerrada` e emitido e `MatriculaEncerradaIntegrationEvent` e publicado.

**Cenario 6 — Encerramento por abandono**
- **Dado** uma matricula `Ativa`
- **Quando** executo `EncerrarMatriculaCommand(matriculaId, Abandono)`
- **Entao** situacao = `Abandono`, o evento `MatriculaEncerrada` e emitido e `MatriculaEncerradaIntegrationEvent` e publicado.

**Cenario 7 — Transicao sobre matricula encerrada**
- **Dado** uma matricula `Transferida` (ou `Concluida`/`Abandono`)
- **Quando** executo `TransferirAlunoCommand` / `EncerrarMatriculaCommand`
- **Entao** ocorre `InvalidOperationException` (estado terminal nao admite transicao — I-9).

**Cenario 8 — Registro da Situacao do Aluno**
- **Dado** uma matricula `Ativa` ao fim do ano letivo
- **Quando** executo `RegistrarSituacaoDoAlunoCommand(matriculaId, Aprovado, SemMovimento)`
- **Entao** `SituacaoDoAluno` fica preenchida (pre-requisito do encerramento do ano letivo — I-10).

**Cenario 9 — Geracao do EducaCenso (Matricula Inicial)**
- **Dado** o fechamento da Matricula Inicial
- **Quando** o leiaute do EducaCenso (formulario Aluno) e gerado
- **Entao** todos os alunos tem `CodigoInep` valido e o arquivo passa na validacao.

**Cenario 10 — Consulta tenant-scoped**
- **Dado** matriculas de um aluno no tenant A e matriculas de outro tenant B
- **Quando** executo `ObterMatriculasDoAlunoQuery(alunoId)` no contexto do tenant A
- **Entao** retornam **apenas** as matriculas do tenant A, projetadas em `MatriculaResumo`.

---

## 13. Casos de Borda

- **B-1.** `AlunoId`/`TurmaId`/`EscolaId`/`DataReferencia` nulos em `MatricularAluno` => `ArgumentNullException` (I-2); via command, `NotEmpty()` no validator.
- **B-2.** Matricular na turma cujo ultimo lugar acabou de ser preenchido por outra matricula concorrente => 2a falha por `matriculados >= vagas` (I-11).
- **B-3.** Aluno com matricula `Ativa` em turno diferente (sem conflito) => matricula permitida (conflito so no mesmo periodo/turno).
- **B-4.** Transferir/encerrar matricula inexistente => `InvalidOperationException("Matricula nao encontrada.")`.
- **B-5.** Encerrar matricula ja `Concluida`/`Abandono`/`Transferida` => falha (situacao != `Ativa`).
- **B-6.** `Motivo` fora de {`Conclusao`,`Abandono`} em `EncerrarMatricula` => rejeitado por `IsInEnum()` no validator.
- **B-7.** Encerramento do ano letivo com algum matriculado sem `SituacaoDoAluno` => bloqueado (I-10).
- **B-8.** Rematricular aluno inapto (reprovado/abandono) => `InvalidOperationException` (aluno inapto).
- **B-9.** `MatriculaEncerradaIntegrationEvent`/`AlunoMatriculadoIntegrationEvent` devem ser idempotentes no consumidor por `EventId` (reentrega via Outbox).
- **B-10.** Acessar matricula de outro tenant => bloqueado pelo Global Query Filter / excecao cross-tenant.

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial — derivada do README do modulo Educacao (mapa de dominio: agregado Matricula, estados `Ativa -> Transferida/Concluida/Abandono`, eventos `AlunoMatriculado`/`AlunoTransferido`/`MatriculaEncerrada`; Integration Events publicados a Transparencia/Financas). |

<!-- manifest
commands: MatricularAluno, TransferirAluno, EncerrarMatricula, RegistrarSituacaoDoAluno, RematricularAluno
queries: ObterMatriculasDoAluno, ListarMatriculasInicialDaTurma
domainEvents: AlunoMatriculado, AlunoTransferido, MatriculaEncerrada
integrationEventsPublished: AlunoMatriculadoIntegrationEvent, MatriculaEncerradaIntegrationEvent
integrationEventsConsumed: 
-->
