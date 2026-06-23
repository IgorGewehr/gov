---
modulo: RecursosHumanos
agregado: Servidor
contexto: RecursosHumanos (ciclo de vida de servidores e empregados públicos)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 37, II (concurso público)", "CF/1988 art. 41 (estabilidade após 3 anos)", "Lei 8.112/1990 (RJU — supletivo ao estatuto municipal)", "EC 103/2019 (RPPS/RGPS)", "CLT (empregado público celetista)", "eSocial — Decreto 8.373/2014 (leiautes S-1.3: S-2200, S-2206, S-2230, S-2299)"]
---

# Servidor — Regras-as-Code (Rules-as-Code)

> Vínculo de pessoal de ente público (Executivo ou Legislativo): servidor estatutário (RPPS)
> ou empregado público celetista (RGPS). Modela o ciclo de vida **Nomeação → Posse → Exercício**,
> a aquisição de **estabilidade** (3 anos de efetivo exercício — CF art. 41), afastamentos e
> desligamento, com transmissão dos eventos não periódicos ao **eSocial** (S-2200, S-2206,
> S-2230, S-2299). Este arquivo é **normativo e versionado**; o código (`Servidor.cs`, handlers,
> validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Servidor (`Servidor`) | Pessoa com vínculo de pessoal no tenant (estatutário ou celetista). Raiz de agregado. |
| Servidor Estatutário (`ServidorEstatutario`) | Vínculo regido por estatuto/RJU; regime previdenciário RPPS. |
| Empregado Público (`EmpregadoPublico`) | Vínculo celetista (CLT); regime previdenciário RGPS. |
| Admissão (`Admitir` / `ServidorAdmitido`) | Ato de registrar o ingresso do servidor a partir de provimento em cargo. |
| Provimento (`Provimento`) | Ato de preenchimento de cargo (nomeação). |
| Posse (`RegistrarPosse` / `PosseRegistrada`) | Aceitação formal das atribuições; caduca se não ocorrer no prazo legal. |
| Exercício (`IniciarExercicio` / `ExercicioIniciado`) | Início efetivo do desempenho das funções. |
| Estabilidade (`ConcederEstabilidade` / `EstabilidadeConcedida`) | Garantia adquirida após 3 anos de efetivo exercício (servidor efetivo — CF art. 41). |
| Afastamento (`RegistrarAfastamento` / `AfastamentoRegistrado`) | Interrupção temporária do exercício (licença, cessão, etc.). |
| Desligamento (`Desligar` / `ServidorDesligado`) | Encerramento do vínculo (exoneração, demissão, aposentadoria, rescisão). |
| RPPS (`RegimePrevidenciario.Rpps`) | Regime Próprio de Previdência Social (servidor efetivo). |
| RGPS (`RegimePrevidenciario.Rgps`) | Regime Geral de Previdência Social (celetista/comissionado/temporário). |
| CPF (`Cpf` : `CPF`) | Cadastro de Pessoa Física; identificador fiscal do servidor. |
| Matrícula (`Matricula` : `Matricula`) | Identificador único do vínculo no tenant. |
| Dados Pessoais (`DadosPessoais` : `DadosPessoais`) | Dados cadastrais do servidor (tratados como sensíveis — LGPD). |
| Dependente (`Dependente`) | Entidade-filha do agregado (dependentes do servidor para fins de IR/benefícios). |
| Tenant (`TenantId`) | Ente público (Prefeitura ou Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoServidor`) | Estado atual do vínculo no ciclo de vida. |

---

## 2. Modelo

- **Identidade:** `ServidorId` — `readonly record struct ServidorId(Guid Value)`; fábrica `ServidorId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Servidor : AggregateRoot<ServidorId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Admitir(...)`.
- **Entidade-filha:** `Dependente` (coleção `IReadOnlyCollection<Dependente> Dependentes`), exposta somente através da raiz.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Cpf` | `CPF` (VO) | CPF do servidor. | `private set` |
| `Matricula` | `Matricula` (VO) | Matrícula única do vínculo no tenant. | `private set` |
| `DadosPessoais` | `DadosPessoais` (VO) | Dados cadastrais sensíveis. | `private set` |
| `CargoId` | `CargoId` (VO/Id) | Cargo provido (associação a `Cargo`). | `private set` |
| `Regime` | `RegimePrevidenciario` (enum) | RPPS (efetivo) ou RGPS (demais). | `private set` |
| `DataNomeacao` | `DateOnly` | Data do provimento/nomeação. | `private set` |
| `DataPosse` | `DateOnly?` | Data da posse (nula antes de registrada). | `private set` |
| `DataExercicio` | `DateOnly?` | Data de início de exercício (nula antes de iniciado). | `private set` |
| `DataEstabilidade` | `DateOnly?` (calculada/concedida) | `DataExercicio + AnosParaEstabilidade`, gravada ao conceder. | `private set` |
| `DataDesligamento` | `DateOnly?` | Data de encerramento do vínculo. | `private set` |
| `Situacao` | `SituacaoServidor` | Situação atual. | `private set` |
| `Dependentes` | `IReadOnlyCollection<Dependente>` | Dependentes do servidor. | coleção encapsulada |

### Constantes

- `AnosParaEstabilidade` = `3` (CF/1988 art. 41). Constante de domínio que parametriza a estabilidade do servidor efetivo.

### Value Objects (referenciados)

- `CPF` — CPF validado (SharedKernel); expõe `Numero` (`string`, 11 dígitos). Não nulo (validado em `Admitir`).
- `Matricula` — `record struct`/VO com `Valor : string`; única por tenant.
- `DadosPessoais` — VO com nome, data de nascimento, etc.; dados sensíveis (LGPD).
- `CargoId` — `record struct` com `Value : Guid` (referência ao agregado `Cargo`).

### Enum `RegimePrevidenciario`

| Valor | Numérico | Descrição |
|---|---|---|
| `Rpps` | 1 | Regime Próprio (servidor efetivo). |
| `Rgps` | 2 | Regime Geral (celetista/comissionado/temporário). |

### Enum `SituacaoServidor`

| Valor | Numérico | Descrição |
|---|---|---|
| `Nomeado` | 1 | Nomeado/admitido (provimento registrado; estado inicial). |
| `Empossado` | 2 | Posse registrada. |
| `EmExercicio` | 3 | Em efetivo exercício. |
| `Estavel` | 4 | Estável (3 anos de efetivo exercício — efetivos). |
| `Afastado` | 5 | Afastado temporariamente. |
| `Desligado` | 6 | Desligado (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Desligado` }.
> - **Em atividade plena** = { `EmExercicio`, `Estavel` } — base para registrar afastamento.
> - **Apto a iniciar exercício** = { `Empossado` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A admissão exige `Cpf`, `Matricula` e `DadosPessoais` não nulos (`ArgumentNullException.ThrowIfNull`).
- **I-2.** Cargo **efetivo** exige aprovação prévia em concurso público; o provimento de efetivo só ocorre via concurso (CF art. 37, II) — regra orquestrada com o agregado `Cargo` (tipo do cargo).
- **I-3.** A sequência legal é **Nomeação → Posse → Exercício**: posse só a partir de `Nomeado`; exercício só a partir de `Empossado`.
- **I-4.** A **posse caduca** se não ocorrer no prazo legal contado da nomeação; expirado o prazo, o provimento é tornado sem efeito e **nenhum** `PosseRegistrada` é emitido.
- **I-5.** A **estabilidade** só é concedida a servidor efetivo (`Regime == Rpps`) após **3 anos** (`AnosParaEstabilidade`) de efetivo exercício; antes disso, `ConcederEstabilidade` lança `InvalidOperationException`.
- **I-6.** O **regime previdenciário** é coerente com o cargo: efetivo → `Rpps` (gera S-1202 na folha); temporário/comissionado/celetista → `Rgps` (gera S-1200).
- **I-7.** O afastamento só pode ser registrado quando o servidor está em atividade plena ({`EmExercicio`,`Estavel`}); caso contrário, `InvalidOperationException`.
- **I-8.** Servidor **desligado** (`Desligado`) é estado terminal e não admite novas transições (posse, exercício, estabilidade, afastamento).
- **I-9.** Na admissão, a situação inicial é `Nomeado`, o evento `ServidorAdmitido(id, matricula, cargoId)` é emitido e o **S-2200** deve ser transmitido **até a véspera** do início do exercício.
- **I-10.** O desligamento emite `ServidorDesligado` e enfileira o **S-2299** (desligamento) ao eSocial.
- **I-11.** `Matricula` é **única por tenant** (índice único `(TenantId, Matricula)`).
- **I-12.** Não excluir/anular eventos de remuneração vinculados (S-1200/S-1202/S-2299) enquanto houver **S-1210** vinculado já transmitido (regra de integração — ver §11).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Admitir` | `Nomeado` | `cpf`, `matricula`, `dadosPessoais` não nulos | `ServidorAdmitido` |
| `Nomeado` | `RegistrarPosse` | `Empossado` | dentro do prazo legal de posse (não caducado) | `PosseRegistrada` |
| `Empossado` | `IniciarExercicio` | `EmExercicio` | situação == `Empossado` | `ExercicioIniciado` |
| `EmExercicio` | `ConcederEstabilidade` | `Estavel` | `Regime == Rpps` && (hoje − `DataExercicio`) ≥ 3 anos | `EstabilidadeConcedida` |
| `EmExercicio` \| `Estavel` | `RegistrarAfastamento` | `Afastado` | situação ∈ {`EmExercicio`,`Estavel`} | `AfastamentoRegistrado` |
| `Afastado` | `RetornarDeAfastamento` | `EmExercicio` | situação == `Afastado` | — |
| ≠ `Desligado` | `Desligar` | `Desligado` | situação ∉ {`Desligado`} | `ServidorDesligado` |

> Observações:
> - A **caducidade da posse** (I-4) não é uma transição de estado de sucesso: é uma guarda que, ao expirar o prazo, torna o provimento sem efeito e impede `PosseRegistrada`.
> - `Desligar` é admitido a partir de qualquer situação não terminal (inclusive `Afastado`).
> - A concessão de estabilidade pressupõe vínculo efetivo (`Rpps`); comissionados/temporários/celetistas não adquirem estabilidade.

---

## 5. Comandos (escrita)

### 5.1 AdmitirServidor

- **Command:** `AdmitirServidorCommand(string Cpf, string Matricula, DadosPessoaisDto DadosPessoais, Guid CargoId, RegimePrevidenciario Regime, DateOnly DataNomeacao) : ICommand<Guid>`.
- **Entrada (DTO):** `Cpf`, `Matricula`, `DadosPessoais`, `CargoId`, `Regime`, `DataNomeacao`.
- **Dependências do handler:** `ICargoRepository`, `IServidorRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Cargo existe (`ICargoRepository.ObterPorIdAsync`), senão `InvalidOperationException("Cargo não encontrado.")`.
  - Regime coerente com o tipo do cargo (efetivo → `Rpps`; demais → `Rgps`) (I-6).
  - `Matricula` não usada no tenant (I-11), senão `InvalidOperationException("Matrícula já existe.")`.
- **Efeito:** cria via `Servidor.Admitir(tenant.TenantId, new CPF(request.Cpf), new Matricula(request.Matricula), dadosPessoais, new CargoId(request.CargoId), request.Regime, request.DataNomeacao)`; `servidores.Adicionar(servidor)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Servidor` em situação `Nomeado`; **S-2200** enfileirado (Outbox/integração) para transmissão até a véspera do exercício; retorna `servidor.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request/campos); `InvalidOperationException` (cargo inexistente, regime incoerente, matrícula duplicada).
- **Evento de domínio:** `ServidorAdmitido(id, matricula, cargoId)` (emitido no construtor via factory).

### 5.2 RegistrarPosse

- **Command:** `RegistrarPosseCommand(Guid ServidorId, DateOnly DataPosse) : ICommand`.
- **Entrada (DTO):** `ServidorId`, `DataPosse`.
- **Dependências do handler:** `IServidorRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo; servidor existe, senão `InvalidOperationException("Servidor não encontrado.")`.
  - Situação == `Nomeado` (I-3); prazo de posse não caducado (I-4).
- **Efeito:** `servidor.RegistrarPosse(request.DataPosse)`; `SaveChangesAsync`.
- **Pós-condições:** `DataPosse` preenchida; situação `Empossado`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, situação ≠ `Nomeado` ou posse caducada).
- **Evento de domínio:** `PosseRegistrada(Id, dataPosse)`.

### 5.3 IniciarExercicio

- **Command:** `IniciarExercicioCommand(Guid ServidorId, DateOnly DataExercicio) : ICommand`.
- **Entrada (DTO):** `ServidorId`, `DataExercicio`.
- **Dependências do handler:** `IServidorRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; servidor existe; situação == `Empossado` (I-3).
- **Efeito:** `servidor.IniciarExercicio(request.DataExercicio)`; `SaveChangesAsync`.
- **Pós-condições:** `DataExercicio` preenchida; situação `EmExercicio`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou situação ≠ `Empossado`).
- **Evento de domínio:** `ExercicioIniciado(Id, dataExercicio)`.

### 5.4 ConcederEstabilidade

- **Command:** `ConcederEstabilidadeCommand(Guid ServidorId) : ICommand`.
- **Entrada (DTO):** `ServidorId`.
- **Dependências do handler:** `IServidorRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; servidor existe; `Regime == Rpps`; ≥ 3 anos de efetivo exercício (I-5).
- **Efeito:** `servidor.ConcederEstabilidade(hoje)`; `SaveChangesAsync`.
- **Pós-condições:** `DataEstabilidade` gravada; situação `Estavel`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, não efetivo ou prazo insuficiente).
- **Evento de domínio:** `EstabilidadeConcedida(Id, dataEstabilidade)`.

### 5.5 RegistrarAfastamento

- **Command:** `RegistrarAfastamentoCommand(Guid ServidorId, DateOnly Inicio, DateOnly? Fim, string Motivo) : ICommand`.
- **Entrada (DTO):** `ServidorId`, `Inicio`, `Fim?`, `Motivo`.
- **Dependências do handler:** `IServidorRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; servidor existe; situação ∈ {`EmExercicio`,`Estavel`} (I-7).
- **Efeito:** `servidor.RegistrarAfastamento(request.Inicio, request.Fim, request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Afastado`; **S-2230** enfileirado ao eSocial.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou situação não permite afastamento).
- **Evento de domínio:** `AfastamentoRegistrado(Id, inicio, fim, motivo)`.

### 5.6 DesligarServidor

- **Command:** `DesligarServidorCommand(Guid ServidorId, DateOnly DataDesligamento, string Motivo) : ICommand`.
- **Entrada (DTO):** `ServidorId`, `DataDesligamento`, `Motivo`.
- **Dependências do handler:** `IServidorRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; servidor existe; situação ∉ {`Desligado`} (I-8).
- **Efeito:** `servidor.Desligar(request.DataDesligamento, request.Motivo)`; `SaveChangesAsync`; publica **Integration Event** `ServidorDesligadoIntegrationEvent(Guid.NewGuid(), agoraUtc, tenant.TenantId, request.ServidorId, request.DataDesligamento)` via `publisher.Publish`.
- **Pós-condições:** situação `Desligado`; **S-2299** enfileirado ao eSocial; integração de revogação publicada (Acesso/Patrimônio).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou já desligado).
- **Evento de domínio:** `ServidorDesligado(Id, dataDesligamento, motivo)`.
- **Evento de integração (publica):** `ServidorDesligadoIntegrationEvent`.

### 5.7 AdicionarPensaoAlimenticia

- **Command:** `AdicionarPensaoAlimenticiaCommand(Guid ServidorId, string Beneficiario, ModalidadePensao Modalidade, decimal Percentual, BasePensao BaseIncidencia, decimal ValorFixo, string ProcessoJudicial) : ICommand` (P0-1).
- **Entrada (DTO):** `ServidorId`, `Beneficiario`, `Modalidade` (percentual/valor fixo), `Percentual`, `BaseIncidencia`, `ValorFixo`, `ProcessoJudicial`.
- **Dependências do handler:** `IServidorRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; servidor existe, senão `InvalidOperationException("Servidor não encontrado.")`; modalidade/percentual/valor fixo válidos (validator).
- **Efeito:** cria a `PensaoAlimenticia` (`PorPercentual` ou `PorValorFixo`); `servidor.AdicionarPensaoAlimenticia(pensao)`; `SaveChangesAsync`.
- **Pós-condições:** pensão judicial ATIVA vinculada ao servidor — o motor de folha deduz a base do IRRF (Lei 7.713/88 art. 4 II) e gera o desconto/repasse ao beneficiário.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (servidor inexistente); `ArgumentException`/`ArgumentOutOfRangeException` (parâmetros da pensão).

> **Comandos de domínio existentes no agregado sem handler de Application dedicado nesta versão:** `RetornarDeAfastamento()` (método da raiz, exposto para futura orquestração de caso de uso).

---

## 6. Consultas (leitura)

### 6.1 ObterServidorPorMatricula

- **Query:** `ObterServidorPorMatriculaQuery(string Matricula) : IQuery<ServidorResumo?>`.
- **Entrada:** `Matricula`.
- **Handler:** `ObterServidorPorMatriculaHandler(IServidorRepository servidores)`; chama `servidores.ObterPorMatriculaAsync(new Matricula(request.Matricula), ct)`.
- **Projeção (DTO):** `ServidorResumo(Guid Id, string Cpf, string Matricula, string NomeServidor, Guid CargoId, string Regime, string Situacao, DateOnly DataNomeacao, DateOnly? DataExercicio)`.
  - `Cpf` projetado **mascarado** (LGPD) de `servidor.Cpf`; `Situacao`/`Regime` de `ToString()`.
- **Filtros:** por `Matricula`; **sempre tenant-scoped** via Global Query Filter por `TenantId` no DbContext.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarServidoresAtivos

- **Query:** `ListarServidoresAtivosQuery() : IQuery<IReadOnlyList<ServidorResumo>>`.
- **Entrada:** nenhuma (escopo do tenant).
- **Handler:** `ListarServidoresAtivosHandler(IServidorRepository servidores)`; chama `servidores.ListarAtivosAsync(ct)` (situação ∉ {`Desligado`}).
- **Projeção (DTO):** `IReadOnlyList<ServidorResumo>` (mesma projeção da 6.1, CPF mascarado).
- **Filtros:** situação ativa; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...RecursosHumanos.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `ServidorAdmitido` | `(ServidorId, Matricula, CargoId)` | `Servidor.Admitir` (construtor) |
| `PosseRegistrada` | `(ServidorId, DateOnly DataPosse)` | `Servidor.RegistrarPosse` |
| `ExercicioIniciado` | `(ServidorId, DateOnly DataExercicio)` | `Servidor.IniciarExercicio` |
| `EstabilidadeConcedida` | `(ServidorId, DateOnly DataEstabilidade)` | `Servidor.ConcederEstabilidade` |
| `AfastamentoRegistrado` | `(ServidorId, DateOnly Inicio, DateOnly? Fim, string Motivo)` | `Servidor.RegistrarAfastamento` |
| `ServidorDesligado` | `(ServidorId, DateOnly DataDesligamento, string Motivo)` | `Servidor.Desligar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...RecursosHumanos.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `ServidorAdmitidoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ServidorId, string Matricula, Guid CargoId)` | `AdmitirServidorHandler` (para Cadastro/Patrimônio — alocação) |
| `ServidorDesligadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ServidorId, DateOnly DataDesligamento)` | `DesligarServidorHandler` (para Acesso/Patrimônio — revogação) |

### Integração (consome)

- Nenhum (esta versão não consome Integration Events de outros módulos no agregado Servidor).

---

## 8. Validações (FluentValidation)

### AdmitirServidorValidator (`AbstractValidator<AdmitirServidorCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Cpf` | `NotEmpty()` + validação de CPF (11 dígitos, DV válido) | "CPF inválido." |
| `Matricula` | `NotEmpty()` + `MaximumLength(20)` | "Matrícula é obrigatória (máx. 20 caracteres)." |
| `DadosPessoais` | `NotNull()` + validação aninhada (nome, data de nascimento) | "Dados pessoais são obrigatórios." |
| `CargoId` | `NotEmpty()` | "Cargo é obrigatório." |
| `Regime` | `IsInEnum()` | "Regime previdenciário inválido." |
| `DataNomeacao` | `NotEmpty()` | "Data de nomeação é obrigatória." |

### RegistrarPosseValidator (`AbstractValidator<RegistrarPosseCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ServidorId` | `NotEmpty()` | "Servidor é obrigatório." |
| `DataPosse` | `NotEmpty()` | "Data de posse é obrigatória." |

### DesligarServidorValidator (`AbstractValidator<DesligarServidorCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ServidorId` | `NotEmpty()` | "Servidor é obrigatório." |
| `DataDesligamento` | `NotEmpty()` | "Data de desligamento é obrigatória." |
| `Motivo` | `NotEmpty()` + `MaximumLength(200)` | "Motivo do desligamento é obrigatório (máx. 200 caracteres)." |

> `IniciarExercicioCommand`, `ConcederEstabilidadeCommand` e `RegistrarAfastamentoCommand` apoiam-se em invariantes de domínio e checagens de existência no handler; consultas (`ObterServidorPorMatricula`, `ListarServidoresAtivos`) não possuem validador dedicado.

---

## 9. Persistência (EF Core 8)

- **Schema:** `recursoshumanos` (isolado por módulo). **DbContext:** o do módulo RecursosHumanos. **Migrations:** por módulo.
- **Tabela:** `Servidor` (raiz de agregado); `Dependente` (entidade-filha, owned/relacionada).

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `ServidorId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Cpf` | `nvarchar(11)` | conversor/owned de `CPF` (campo `Numero`); mascarado em logs. |
| `Matricula` | `nvarchar(20)` | conversor de `Matricula` ↔ `string`. |
| `DadosPessoais` | (owned) | VO `DadosPessoais` mapeado como owned type (colunas próprias). |
| `CargoId` | `Guid` | conversor de `CargoId` ↔ `Guid`. |
| `Regime` | `int` | enum `RegimePrevidenciario` (valor numérico). |
| `DataNomeacao` | `date` | `DateOnly`. |
| `DataPosse` | `date?` | `DateOnly?` nulável. |
| `DataExercicio` | `date?` | `DateOnly?` nulável. |
| `DataEstabilidade` | `date?` | `DateOnly?` nulável (gravada ao conceder). |
| `DataDesligamento` | `date?` | `DateOnly?` nulável. |
| `Situacao` | `int` | enum `SituacaoServidor` (valor numérico). |

- **Entidade-filha `Dependente`:** tabela `Dependente` com FK `ServidorId`; colunas próprias (nome, parentesco, data de nascimento); dados sensíveis (LGPD).
- **Índices:**
  - PK em `Id`.
  - **Índice único** em `(TenantId, Matricula)` (I-11 — unicidade da matrícula por tenant).
  - Índice em `(TenantId, Cpf)` para localização por CPF.
  - Índice em `(TenantId, Situacao)` para `ListarServidoresAtivos`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto RecursosHumanos para `ServidorAdmitidoIntegrationEvent` e `ServidorDesligadoIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Servidor` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição. **Executivo e Legislativo do mesmo município são tenants distintos.**
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Admissão/posse/exercício/estabilidade/afastamento/desligamento: papéis de **Gestão de Pessoal/RH** (ex.: `RecursosHumanos.Servidor.Gerir`).
  - Consulta de servidores: papel de **leitura de RH** (ex.: `RecursosHumanos.Servidor.Ler`).
  - Módulo RecursosHumanos é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** `DadosPessoais`, `Cpf` e `Dependente` são **dados pessoais sensíveis** — minimização, controle de acesso por perfil e **mascaramento de CPF em logs/projeções**. Acesso à ficha do servidor gera **trilha de acesso** (quem leu, quando, por quê).
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Admitir`, `RegistrarPosse`, `IniciarExercicio`, `ConcederEstabilidade`, `RegistrarAfastamento`, `Desligar`) — destinada ao Tribunal de Contas (TCE-RS). Recibos/protocolos eSocial (S-2200/S-2206/S-2230/S-2299) preservados como prova fiscal.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **eSocial (não periódicos) — Decreto 8.373/2014, leiautes S-1.3:**
  - **S-2200** (admissão) — enfileirado na `Admitir`; prazo: **até a véspera** do início do exercício.
  - **S-2206** (alteração contratual) — em mudanças de cargo/dados contratuais.
  - **S-2230** (afastamento) — enfileirado na `RegistrarAfastamento`.
  - **S-2299** (desligamento) — enfileirado na `Desligar`.
  - Transmissão: **web service SOAP**, **XML assinado com certificado A1** (Azure Key Vault, por tenant); ambientes **Produção Restrita** e **Produção**; cada lote retorna protocolo e cada evento gera **recibo persistido**.
- **Dependência de tabelas (eSocial):** rubricas em **S-1010** e estabelecimentos em **S-1005** devem estar **vigentes na competência**; lotação tributária em **S-1020**.
- **Regra de exclusão (I-12):** não excluir/anular S-1200/S-1202/S-2299 enquanto houver **S-1210** vinculado já transmitido (exige exclusão prévia do S-1210).
- **Resiliência:** integração externa **idempotente** (por `EventId`/recibo), com timeout, **retry e circuit breaker (Polly)** sobre o SOAP; envio assíncrono via **Outbox**; mapeamento explícito de erros atrás de **Anti-Corruption Layer (ACL)**.
- **Saída intra-aplicação (via Contracts):** `ServidorAdmitidoIntegrationEvent` (Cadastro/Patrimônio) e `ServidorDesligadoIntegrationEvent` (Acesso/Patrimônio), publicados pelo Outbox; consumo via ACL.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Admissão de servidor efetivo**
- **Dado** um candidato aprovado em concurso e nomeado para cargo efetivo
- **Quando** registro a admissão antes da véspera do exercício via `AdmitirServidorCommand`
- **Então** é criado um `Servidor` em situação `Nomeado`, o evento `ServidorAdmitido` é emitido e o **S-2200** é enfileirado para o eSocial.

**Cenário 2 — Posse caducada**
- **Dado** um servidor `Nomeado` sem posse dentro do prazo legal
- **Quando** o prazo de posse expira e tento `RegistrarPosseCommand`
- **Então** o provimento é tornado sem efeito e **nenhum** `PosseRegistrada` é emitido (`InvalidOperationException`).

**Cenário 3 — Sequência Nomeação → Posse → Exercício**
- **Dado** um servidor `Nomeado` dentro do prazo
- **Quando** executo `RegistrarPosseCommand` e depois `IniciarExercicioCommand`
- **Então** a situação evolui para `Empossado` e então `EmExercicio`, emitindo `PosseRegistrada` e `ExercicioIniciado`.

**Cenário 4 — Exercício sem posse**
- **Dado** um servidor `Nomeado` (sem posse)
- **Quando** executo `IniciarExercicioCommand`
- **Então** ocorre `InvalidOperationException` (exercício exige `Empossado`).

**Cenário 5 — Estabilidade após 3 anos (efetivo)**
- **Dado** um servidor efetivo (`Rpps`) `EmExercicio` há 3 anos ou mais
- **Quando** executo `ConcederEstabilidadeCommand`
- **Então** a situação passa a `Estavel`, `DataEstabilidade` é gravada e `EstabilidadeConcedida` é emitido.

**Cenário 6 — Estabilidade negada antes de 3 anos**
- **Dado** um servidor efetivo `EmExercicio` há menos de 3 anos (ou não efetivo)
- **Quando** executo `ConcederEstabilidadeCommand`
- **Então** ocorre `InvalidOperationException` (prazo insuficiente ou regime ≠ RPPS).

**Cenário 7 — Afastamento de servidor em exercício**
- **Dado** um servidor `EmExercicio` (ou `Estavel`)
- **Quando** executo `RegistrarAfastamentoCommand`
- **Então** a situação passa a `Afastado`, `AfastamentoRegistrado` é emitido e o **S-2230** é enfileirado.

**Cenário 8 — Afastamento bloqueado fora de atividade**
- **Dado** um servidor `Nomeado` ou `Empossado`
- **Quando** executo `RegistrarAfastamentoCommand`
- **Então** ocorre `InvalidOperationException` (afastamento exige `EmExercicio`/`Estavel`).

**Cenário 9 — Desligamento publica integração**
- **Dado** um servidor em situação não terminal
- **Quando** executo `DesligarServidorCommand`
- **Então** a situação passa a `Desligado`, `ServidorDesligado` é emitido, o **S-2299** é enfileirado e `ServidorDesligadoIntegrationEvent` é publicado (revogação Acesso/Patrimônio).

**Cenário 10 — Transição bloqueada após desligamento**
- **Dado** um servidor `Desligado`
- **Quando** executo qualquer comando de transição (posse, exercício, estabilidade, afastamento, desligamento)
- **Então** ocorre `InvalidOperationException` (estado terminal).

**Cenário 11 — Matrícula duplicada no tenant**
- **Dado** uma `Matricula` já usada no tenant
- **Quando** executo `AdmitirServidorCommand` com a mesma matrícula
- **Então** a operação é rejeitada (índice único `(TenantId, Matricula)` / `InvalidOperationException`).

**Cenário 12 — Consulta tenant-scoped**
- **Dado** servidores no tenant A e no tenant B
- **Quando** executo `ListarServidoresAtivosQuery` no contexto do tenant A
- **Então** retornam **apenas** os servidores ativos do tenant A, com CPF mascarado em `ServidorResumo`.

---

## 13. Casos de Borda

- **B-1.** `Cpf`/`Matricula`/`DadosPessoais` nulos em `Admitir` ⇒ `ArgumentNullException` (I-1).
- **B-2.** CPF com DV inválido ⇒ rejeitado pelo VO `CPF`/validator.
- **B-3.** Regime `Rpps` para cargo comissionado/temporário (ou `Rgps` para efetivo) ⇒ `InvalidOperationException` (I-6).
- **B-4.** Posse no exato limite do prazo legal ⇒ aceita; um dia além ⇒ caduca (I-4).
- **B-5.** Conceder estabilidade com exatamente 3 anos completos ⇒ permitido; com 2 anos e 364 dias ⇒ negado (I-5).
- **B-6.** Conceder estabilidade a empregado público (`Rgps`) ⇒ negado (não adquire estabilidade — I-5).
- **B-7.** Registrar afastamento com `Fim < Inicio` ⇒ rejeitado por validação de período.
- **B-8.** Retornar de afastamento quando situação ≠ `Afastado` ⇒ `InvalidOperationException`.
- **B-9.** Desligar servidor `Afastado` ⇒ permitido (afastado não é estado terminal); resulta em `Desligado` + S-2299.
- **B-10.** Admitir sem cargo existente ⇒ `InvalidOperationException("Cargo não encontrado.")`.
- **B-11.** `ServidorAdmitidoIntegrationEvent`/`ServidorDesligadoIntegrationEvent` devem ser idempotentes no consumidor por `EventId` (reentrega via Outbox).
- **B-12.** Tentar excluir S-2299 com S-1210 vinculado já transmitido ⇒ rejeitado (I-12 — exige exclusão prévia do S-1210).
- **B-13.** Transmissão eSocial falha/timeout ⇒ retry/circuit breaker (Polly); evento permanece no Outbox até confirmação de recibo.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo RecursosHumanos (ciclo de vida do Servidor: admissão, posse, exercício, estabilidade, afastamento, desligamento; integração eSocial S-2200/S-2206/S-2230/S-2299). |

<!-- manifest
commands: AdmitirServidor, RegistrarPosse, IniciarExercicio, ConcederEstabilidade, RegistrarAfastamento, DesligarServidor, AdicionarPensaoAlimenticia
queries: ObterServidorPorMatricula, ListarServidoresAtivos, BuscarServidores, ObterFichaFuncional
domainEvents: ServidorAdmitido, PosseRegistrada, ExercicioIniciado, EstabilidadeConcedida, AfastamentoRegistrado, ServidorDesligado
integrationEventsPublished: ServidorAdmitidoIntegrationEvent, ServidorDesligadoIntegrationEvent
integrationEventsConsumed: 
-->
