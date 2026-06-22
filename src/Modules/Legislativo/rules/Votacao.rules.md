---
modulo: Legislativo
agregado: Votacao
contexto: Legislativo (Processo Legislativo Municipal — deliberacao e apuracao)
poder: Legislativo
schema: legislativo
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/88 art. 29 (Lei Organica e maiorias)", "CF/88 arts. 59-69 (processo legislativo - simetria)", "Lei Organica Municipal (LOM)", "Regimento Interno (Resolucao)"]
---

# Votacao — Regras-as-Code (Rules-as-Code)

> Deliberacao do Plenario sobre uma materia (proposicao, veto, parecer), apurada por modalidade
> simbolica, nominal ou secreta. Aplica a **maioria exigida** (simples, absoluta ou qualificada)
> conforme a especie e produz um `Resultado` (aprovado/rejeitado) que alimenta a tramitacao da
> proposicao. Registra cada `Voto` de forma idempotente (painel eletronico) e mantem trilha
> imutavel para prova juridica e LAI. Conformidade com CF/88 art. 29 (maiorias), a Lei Organica
> Municipal e o Regimento Interno. Este arquivo e **normativo e versionado**; o codigo (agregado
> `Votacao`, handlers, validators, EF config, testes) e consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Votacao (`Votacao`) | Deliberacao do Plenario sobre uma materia. Raiz de agregado. |
| Tipo de Votacao (`TipoVotacao`) | Modalidade de apuracao (simbolica / nominal / secreta). VO/enum. |
| Votacao Simbolica (`VotacaoSimbolica`) | Apuracao sem registro individual (placar agregado). |
| Votacao Nominal (`VotacaoNominal`) | Apuracao com registro individual por vereador. |
| Votacao Secreta (`VotacaoSecreta`) | Apuracao com sigilo do voto individual. |
| Maioria Exigida (`MaioriaExigida`) | Criterio de aprovacao (simples / absoluta / qualificada 2/3). VO/enum. |
| Maioria Simples (`Simples`) | > 50% dos presentes (lei ordinaria). |
| Maioria Absoluta (`Absoluta`) | > 50% dos membros (LC, derrubada de veto, Regimento). |
| Maioria Qualificada (`Qualificada`) | 2/3 em dois turnos (Emenda a LOM). |
| Resultado (`Resultado`) | Desfecho apurado da votacao (aprovado / rejeitado). VO/enum. |
| Voto (`Voto` / `RegistrarVoto`) | Manifestacao de um vereador (sim / nao / abstencao). Entidade do agregado. |
| Sentido do Voto (`SentidoVoto`) | Direcao do voto (Sim / Nao / Abstencao). VO/enum. |
| Total de Membros (`TotalMembros`) | Numero de vereadores da Camara (base da maioria absoluta). |
| Presentes (`Presentes`) | Numero de vereadores presentes (base da maioria simples). |
| Iniciar Votacao (`Iniciar`) | Abrir a votacao para registro de votos. |
| Encerrar / Apurar (`Encerrar` / `Apurar`) | Fechar a votacao e calcular o `Resultado`. |
| Painel Eletronico (`votoId`) | Origem dos votos em tempo real; idempotencia por `votoId`. |
| Tenant (`TenantId`) | Ente publico (Camara Municipal) dono do registro. |
| Situacao (`Situacao` : `SituacaoVotacao`) | Estado atual da votacao. |

---

## 2. Modelo

- **Identidade:** `VotacaoId` — `readonly record struct VotacaoId(Guid Value)`; fabrica `VotacaoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Votacao : AggregateRoot<VotacaoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valida via factory `Abrir(...)` (vide `Iniciar`).

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (Camara) dono do registro. | `private set` |
| `SessaoId` | `SessaoId` (VO/Id) | Sessao em que ocorre a votacao. | `private set` |
| `ProposicaoId` | `ProposicaoId` (VO/Id) | Materia votada. | `private set` |
| `Tipo` | `TipoVotacao` (VO/enum) | Simbolica / nominal / secreta. | `private set` |
| `MaioriaExigida` | `MaioriaExigida` (VO/enum) | Simples / absoluta / qualificada. | `private set` |
| `TotalMembros` | `int` | Numero de vereadores (base da maioria absoluta). | `private set` |
| `Presentes` | `int` | Numero de presentes (base da maioria simples). | `private set` |
| `Turno` | `int` | Turno da votacao (1 ou 2; qualificada exige 2 turnos). | `private set` |
| `Situacao` | `SituacaoVotacao` | Situacao atual. | `private set` |
| `Resultado` | `ResultadoVotacao?` | Resultado apurado (nulo antes do encerramento). | `private set` |
| `Votos` | `IReadOnlyList<Voto>` | Votos registrados (trilha imutavel). | colecao encapsulada |

### Constantes / formulas de dominio

- **Maioria simples** = aprovado se `votosSim > Presentes / 2` (> 50% dos presentes).
- **Maioria absoluta** = aprovado se `votosSim >= TotalMembros / 2 + 1` (> 50% dos membros).
- **Maioria qualificada** = aprovado se `votosSim >= ceil(2 * TotalMembros / 3)` **e** aprovado em **2 turnos**.
- `TotalMembros` e `Presentes` parametrizados (derivados da `Sessao`). **Nunca** hardcoded.

### Entidades internas

- **`Voto`** — `VotoId` (origem `votoId` do painel, chave de idempotencia), `VereadorId` (omitido/encriptado em votacao secreta), `Sentido` (`SentidoVoto`), `RegistradoEm`. Vinculado a `VotacaoId`. Trilha **imutavel** (append-only).

### Value Objects (referenciados)

- `TipoVotacao` — modalidade de apuracao.
- `MaioriaExigida` — criterio de aprovacao.
- `SentidoVoto` — direcao do voto.

### Enum `TipoVotacao`

| Valor | Numerico | Descricao |
|---|---|---|
| `Simbolica` | 1 | Apuracao simbolica (placar agregado). |
| `Nominal` | 2 | Apuracao nominal (registro individual). |
| `Secreta` | 3 | Apuracao secreta (sigilo do voto). |

### Enum `MaioriaExigida`

| Valor | Numerico | Descricao |
|---|---|---|
| `Simples` | 1 | > 50% dos presentes (lei ordinaria). |
| `Absoluta` | 2 | > 50% dos membros (LC, derrubada de veto, Regimento). |
| `Qualificada` | 3 | 2/3 em dois turnos (Emenda a LOM). |

### Enum `SentidoVoto`

| Valor | Numerico | Descricao |
|---|---|---|
| `Sim` | 1 | Voto favoravel. |
| `Nao` | 2 | Voto contrario. |
| `Abstencao` | 3 | Abstencao. |

### Enum `SituacaoVotacao`

| Valor | Numerico | Descricao |
|---|---|---|
| `Aberta` | 1 | Aberta para registro de votos (estado inicial). |
| `Encerrada` | 2 | Encerrada e apurada (terminal). |
| `Cancelada` | 3 | Cancelada (terminal). |

### Enum `ResultadoVotacao`

| Valor | Numerico | Descricao |
|---|---|---|
| `Aprovado` | 1 | Materia aprovada (maioria exigida atingida). |
| `Rejeitado` | 2 | Materia rejeitada (maioria nao atingida). |

> **Conjuntos de referencia usados nas guardas:**
> - **Terminal** = { `Encerrada`, `Cancelada` }.
> - **Aberta para votos** = situacao == `Aberta`.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Na abertura (`Iniciar`), `SessaoId`, `ProposicaoId`, `Tipo`, `MaioriaExigida` sao obrigatorios; `TotalMembros > 0` e `Presentes > 0`; a situacao inicial e `Aberta` e e emitido `VotacaoIniciada(id, proposicaoId, tipo, maioriaExigida)`.
- **I-2.** O registro de `Voto` (`RegistrarVoto`) so e permitido com a votacao `Aberta`; caso contrario, `InvalidOperationException`.
- **I-3.** **Idempotencia por `votoId`**: o mesmo `votoId` (origem do painel) gera **apenas um** `Voto` persistido e **apenas um** `VotoRegistrado`; reenvio e no-op (Cenario 6 do README).
- **I-4.** Em votacao **nominal**, cada `VereadorId` vota uma unica vez (alem da idempotencia por `votoId`).
- **I-5.** O encerramento (`Encerrar`/`Apurar`) so ocorre com a votacao `Aberta`; calcula o `Resultado` conforme `MaioriaExigida`, passa a `Encerrada` (terminal) e emite `VotacaoEncerrada(id, resultado)`.
- **I-6.** **Maioria simples** (`Simples`): `Aprovado` sse `votosSim > Presentes / 2` (> 50% dos presentes).
- **I-7.** **Maioria absoluta** (`Absoluta`): `Aprovado` sse `votosSim >= TotalMembros / 2 + 1` (> 50% dos membros) — aplicavel a LC, derrubada de veto e Regimento.
- **I-8.** **Maioria qualificada** (`Qualificada`): `Aprovado` sse `votosSim >= ceil(2 * TotalMembros / 3)` **e** a materia for aprovada nos **dois turnos** (Emenda a LOM — CF art. 29).
- **I-9.** O `Resultado` e nulo enquanto `Aberta`; so e preenchido no encerramento (somente leitura apos definido).
- **I-10.** Estados terminais (`Encerrada`, `Cancelada`) nao admitem novos votos nem reapuracao.
- **I-11.** A trilha de votos e **imutavel** (append-only): votos nao podem ser removidos/alterados (prova juridica + LAI).
- **I-12.** Em votacao **secreta**, o `VereadorId` nao e exposto nas projecoes/consultas (sigilo), preservando apenas o placar agregado.
- **I-13.** Abstencoes nao contam como `Sim`; entram apenas no total de votos registrados, nao no numerador da maioria.

---

## 4. Maquina de Estados

Tabela: Estado origem → comando/metodo → Estado destino | guarda | evento emitido.

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Iniciar` | `Aberta` | `Tipo`, `MaioriaExigida` validos; `TotalMembros > 0`; `Presentes > 0` | `VotacaoIniciada` |
| `Aberta` | `RegistrarVoto` | (inalterada) | `votoId` ainda nao registrado; (nominal) vereador ainda nao votou | `VotoRegistrado` |
| `Aberta` | `Encerrar` / `Apurar` | `Encerrada` | situacao == `Aberta` | `VotacaoEncerrada` |
| `Aberta` | `Cancelar` | `Cancelada` | situacao == `Aberta` | — |

> Observacoes:
> - `RegistrarVoto` aplica idempotencia por `votoId` **antes** de persistir (Cenario 6); reenvio do mesmo `votoId` => no-op, sem novo `VotoRegistrado`.
> - O encerramento calcula o `Resultado` aplicando a `MaioriaExigida`; o `Resultado` (`Aprovado`/`Rejeitado`) alimenta `AprovarProposicao`/`RejeitarProposicao` no agregado `Proposicao`.
> - O **Painel Eletronico de Votacao** abre/fecha a votacao e exibe o placar nominal em tempo real (WebSocket/event stream).

---

## 5. Comandos (escrita)

### 5.1 IniciarVotacao

- **Command:** `IniciarVotacaoCommand(Guid SessaoId, Guid ProposicaoId, int Tipo, int MaioriaExigida, int TotalMembros, int Presentes, int Turno) : ICommand<Guid>`.
- **Entrada (DTO):** `SessaoId`, `ProposicaoId`, `Tipo` (`TipoVotacao`), `MaioriaExigida`, `TotalMembros`, `Presentes`, `Turno`.
- **Dependencias do handler:** `IVotacaoRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pre-condicoes:** `request` nao nulo; `TotalMembros > 0`; `Presentes > 0`; `Tipo` e `MaioriaExigida` validos (I-1).
- **Efeito:** cria via `Votacao.Iniciar(tenant.TenantId, sessaoId, proposicaoId, tipo, maioriaExigida, totalMembros, presentes, turno)`; `votacoes.Adicionar(votacao)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Votacao` em situacao `Aberta`; retorna `votacao.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `ArgumentException` (`TotalMembros`/`Presentes <= 0` ou enum invalido).
- **Evento de dominio:** `VotacaoIniciada(id, proposicaoId, tipo, maioriaExigida)`.

### 5.2 RegistrarVoto

- **Command:** `RegistrarVotoCommand(Guid VotacaoId, Guid VotoId, Guid VereadorId, int Sentido) : ICommand`.
- **Entrada (DTO):** `VotacaoId`, `VotoId` (idempotencia — origem do painel), `VereadorId`, `Sentido` (`SentidoVoto`).
- **Dependencias do handler:** `IVotacaoRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pre-condicoes:** `request` nao nulo; votacao existe; situacao == `Aberta` (I-2); `VotoId` ainda nao registrado (I-3); (nominal) `VereadorId` ainda nao votou (I-4).
- **Efeito:** `votacao.RegistrarVoto(new VotoId(request.VotoId), new VereadorId(request.VereadorId), sentido, agora)`; `SaveChangesAsync`.
- **Pos-condicoes:** novo `Voto` na trilha imutavel (ou no-op se `VotoId` ja existir).
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada, votacao nao `Aberta`, ou vereador ja votou em nominal).
- **Evento de dominio:** `VotoRegistrado(Id, votoId)`.
- **Idempotencia:** por `VotoId` (Cenario 6) — apenas um `VotoRegistrado` por `VotoId`.

### 5.3 EncerrarVotacao

- **Command:** `EncerrarVotacaoCommand(Guid VotacaoId) : ICommand<ResultadoVotacao>`.
- **Entrada (DTO):** `VotacaoId`.
- **Dependencias do handler:** `IVotacaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; votacao existe; situacao == `Aberta` (I-5).
- **Efeito:** `ResultadoVotacao resultado = votacao.Encerrar()` (apura conforme `MaioriaExigida`); `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Encerrada`; `Resultado` preenchido; retorna `Resultado`; alimenta `Aprovar`/`Rejeitar` da `Proposicao`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `Aberta`).
- **Evento de dominio:** `VotacaoEncerrada(Id, resultado)`.

### 5.4 CancelarVotacao

- **Command:** `CancelarVotacaoCommand(Guid VotacaoId) : ICommand`.
- **Entrada (DTO):** `VotacaoId`.
- **Dependencias do handler:** `IVotacaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; votacao existe; situacao == `Aberta` (I-10 — antes do encerramento).
- **Efeito:** `votacao.Cancelar()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Cancelada` (terminal); sem `Resultado`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `Aberta`).
- **Evento de dominio:** —.

---

## 6. Consultas (leitura)

### 6.1 ObterVotacaoPorId

- **Query:** `ObterVotacaoPorIdQuery(Guid VotacaoId) : IQuery<VotacaoDetalhe>`.
- **Entrada:** `VotacaoId`.
- **Handler:** `ObterVotacaoPorIdHandler(IVotacaoRepository votacoes)`; chama `votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), ct)`.
- **Projecao (DTO):** `VotacaoDetalhe(Guid Id, Guid SessaoId, Guid ProposicaoId, string Tipo, string MaioriaExigida, int TotalMembros, int Presentes, int Turno, string Situacao, string? Resultado, int VotosSim, int VotosNao, int Abstencoes)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`. Em votacao `Secreta`, nao expoe votos individuais (I-12).
- **Pre-condicoes:** `request` nao nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ObterPlacarDaVotacao

- **Query:** `ObterPlacarDaVotacaoQuery(Guid VotacaoId) : IQuery<PlacarVotacao>`.
- **Entrada:** `VotacaoId`.
- **Handler:** `ObterPlacarDaVotacaoHandler(IVotacaoRepository votacoes)`; agrega os votos por sentido (placar em tempo real — painel).
- **Projecao (DTO):** `PlacarVotacao(Guid VotacaoId, int Sim, int Nao, int Abstencao, int TotalVotos, string Situacao)`.
- **Filtros:** por `VotacaoId`; **sempre tenant-scoped**; em `Secreta` mostra apenas o agregado (sem identificacao).
- **Pre-condicoes:** `request` nao nulo.

### 6.3 ListarVotosNominais

- **Query:** `ListarVotosNominaisQuery(Guid VotacaoId) : IQuery<IReadOnlyList<VotoResumo>>`.
- **Entrada:** `VotacaoId`.
- **Handler:** `ListarVotosNominaisHandler(IVotacaoRepository votacoes)`; le a trilha imutavel de votos **apenas** para votacoes `Nominal`/`Simbolica` (nunca `Secreta`).
- **Projecao (DTO):** `VotoResumo(Guid VereadorId, string Sentido, DateTimeOffset RegistradoEm)`.
- **Filtros:** por `VotacaoId`; **sempre tenant-scoped**; **bloqueada** para `Secreta` (I-12); alimenta transparencia (LAI) para votacoes abertas/nominais.
- **Pre-condicoes:** `request` nao nulo.

---

## 7. Eventos

### Dominio (in-process, MediatR; assembly `...Legislativo.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `VotacaoIniciada` | `(VotacaoId, ProposicaoId, TipoVotacao, MaioriaExigida)` | `Votacao.Iniciar` (construtor) |
| `VotoRegistrado` | `(VotacaoId, VotoId)` | `Votacao.RegistrarVoto` |
| `VotacaoEncerrada` | `(VotacaoId, ResultadoVotacao resultado)` | `Votacao.Encerrar` |

### Integracao (publica via `*.Contracts` + Outbox; assembly `...Legislativo.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `ResultadoVotacaoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid VotacaoId, Guid ProposicaoId, string Resultado)` | `EncerrarVotacaoHandler` (transparencia LAI/APIs abertas) |

### Integracao (consome)

- Nenhum (esta versao nao consome Integration Events de outros modulos no agregado `Votacao`).

---

## 8. Validacoes (FluentValidation)

### IniciarVotacaoValidator (`AbstractValidator<IniciarVotacaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `SessaoId` | `NotEmpty()` | (padrao: sessao obrigatoria) |
| `ProposicaoId` | `NotEmpty()` | (padrao: proposicao obrigatoria) |
| `Tipo` | `IsInEnum()` | (padrao: tipo de votacao invalido) |
| `MaioriaExigida` | `IsInEnum()` | (padrao: maioria exigida invalida) |
| `TotalMembros` | `GreaterThan(0)` | (padrao: total de membros deve ser positivo) |
| `Presentes` | `GreaterThan(0)` | (padrao: presentes deve ser positivo) |
| `Turno` | `InclusiveBetween(1, 2)` | (padrao: turno deve ser 1 ou 2) |

### RegistrarVotoValidator (`AbstractValidator<RegistrarVotoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `VotacaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `VotoId` | `NotEmpty()` | (padrao: votoId obrigatorio para idempotencia) |
| `VereadorId` | `NotEmpty()` | (padrao: vereador obrigatorio) |
| `Sentido` | `IsInEnum()` | (padrao: sentido do voto invalido) |

### EncerrarVotacaoValidator / CancelarVotacaoValidator

| Campo | Regra | Mensagem |
|---|---|---|
| `VotacaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |

> Consultas (`ObterVotacaoPorId`, `ObterPlacarDaVotacao`, `ListarVotosNominais`) nao possuem validador dedicado; protecao por existencia no repositorio e tenant-scope; sigilo de `Secreta` aplicado na projecao.

---

## 9. Persistencia (EF Core 8)

- **Schema:** `legislativo` (isolado por modulo). **DbContext:** o do modulo Legislativo. **Migrations:** por modulo.
- **Tabela:** `Votacao` (raiz de agregado); tabela filha `Voto`.

| Coluna | Tipo logico | Conversor / observacao |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `VotacaoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `SessaoId` | `Guid` | conversor de `SessaoId` ↔ `Guid`. |
| `ProposicaoId` | `Guid` | conversor de `ProposicaoId` ↔ `Guid`. |
| `Tipo` | `int` | enum `TipoVotacao`. |
| `MaioriaExigida` | `int` | enum `MaioriaExigida`. |
| `TotalMembros` | `int` | base da maioria absoluta. |
| `Presentes` | `int` | base da maioria simples. |
| `Turno` | `int` | 1 ou 2 (qualificada exige 2 turnos). |
| `Situacao` | `int` | enum `SituacaoVotacao`. |
| `Resultado` | `int?` | enum `ResultadoVotacao`; nulo enquanto `Aberta`. |

- **Tabela `Voto`** (trilha imutavel, append-only): `Id`/`VotoId` (PK — origem do painel, chave de idempotencia), `VotacaoId` (FK), `VereadorId` (`Guid`; omitido em projecoes de `Secreta`), `Sentido` (`int`), `RegistradoEm` (`datetimeoffset`).
- **Indices:**
  - PK em `Id`.
  - Indice em `(TenantId, ProposicaoId)` para localizar votacoes de uma proposicao.
  - Indice em `(TenantId, SessaoId)` para votacoes de uma sessao.
  - Indice **unico** em `Voto(VotacaoId, VotoId)` (idempotencia por `votoId` — I-3).
  - Indice **unico** em `Voto(VotacaoId, VereadorId)` quando `Tipo == Nominal` (um voto por vereador — I-4).
- **Conversores (VO/Id):** Fluent API (sem data annotations no dominio).
- **Outbox:** tabela Outbox do contexto Legislativo para `ResultadoVotacaoIntegrationEvent` (consistencia transacional).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** `Votacao` implementa `IMustHaveTenant`. A **Camara e tenant DISTINTO do Executivo**. `TenantId` carimbado na insercao pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravacao cross-tenant **lanca excecao**. `ITenantContext` resolvido do JWT por requisicao.
- **RBAC (policy-based + RBAC `Usuario → Departamento → Roles`, negar por padrao):**
  - Iniciar / encerrar / cancelar votacao: papel da **Mesa Diretora** (ex.: `Legislativo.Votacao.Conduzir`).
  - Registrar voto: papel de **Vereador** ou captura via **Painel Eletronico** autenticado (ex.: `Legislativo.Votacao.Votar`).
  - Consultas de placar/votos: papel de **leitura legislativa** (ex.: `Legislativo.Votacao.Ler`); votos nominais publicos via transparencia (LAI), exceto `Secreta`.
  - Modulo Legislativo e **ativavel por tenant**; requisicao a tenant sem licenca → 404/403 auditado.
- **Sigilo (votacao secreta):** em `Secreta`, o `VereadorId` **nunca** e exposto em consultas/projecoes (I-12); apenas o placar agregado e divulgado.
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuario, IP, timestamp) em toda mutacao (`Iniciar`, `RegistrarVoto`, `Encerrar`, `Cancelar`). A trilha de **votos e imutavel (append-only)** para prova juridica e LAI — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integracoes Governamentais

- **Painel Eletronico de Votacao:** abre/fecha a votacao e exibe o **placar nominal em tempo real** (WebSocket/event stream); origem dos votos. **Idempotencia por `votoId`** (Cenario 6 do README): o mesmo `votoId` produz apenas um `Voto`/`VotoRegistrado`. ACL + resiliencia.
- **Transmissao + ata eletronica:** resultados de votacao integram a ata e a transmissao da sessao.
- **SAPL/Interlegis:** interoperacao e exportacao/importacao de resultados (ACL + idempotencia).
- **APIs abertas (LAI):** publica `ResultadoVotacaoIntegrationEvent` e placares para o portal de transparencia (respeitando o sigilo de `Secreta`).
- **Resiliencia:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explicito de erros atras de Anti-Corruption Layer.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Inicio de votacao**
- **Dado** uma proposicao em Ordem do Dia e uma sessao aberta com 11 membros e 9 presentes
- **Quando** executo `IniciarVotacaoCommand(sessaoId, proposicaoId, Simbolica, Simples, 11, 9, 1)`
- **Entao** e criada uma `Votacao` em situacao `Aberta` e o evento `VotacaoIniciada` e emitido.

**Cenario 2 — Aprovacao por maioria simples (README Cenario 2)**
- **Dado** uma `VotacaoSimbolica` com `MaioriaExigida = Simples` e 9 presentes
- **Quando** sao registrados 5 votos `Sim` e executo `EncerrarVotacaoCommand(id)`
- **Entao** `Resultado = Aprovado` (5 > 9/2) e o evento `VotacaoEncerrada` e emitido.

**Cenario 3 — Rejeicao por maioria simples**
- **Dado** uma votacao `Simples` com 9 presentes
- **Quando** sao registrados 4 votos `Sim` e 5 `Nao` e encerro a votacao
- **Entao** `Resultado = Rejeitado` (4 nao supera 50% dos presentes).

**Cenario 4 — Idempotencia do painel (README Cenario 6)**
- **Dado** uma `Votacao` `Aberta`
- **Quando** o mesmo `votoId` chega duas vezes via `RegistrarVotoCommand`
- **Entao** apenas um `Voto` e persistido e apenas um `VotoRegistrado` e emitido.

**Cenario 5 — Maioria absoluta (PLC / Regimento)**
- **Dado** uma `VotacaoNominal` com `MaioriaExigida = Absoluta` e 11 membros
- **Quando** sao registrados 6 votos `Sim` e encerro a votacao
- **Entao** `Resultado = Aprovado` (6 >= 11/2 + 1).

**Cenario 6 — Derrubada de veto por maioria absoluta (README Cenario 5)**
- **Dado** uma `VotacaoNominal` para derrubar um veto, com `MaioriaExigida = Absoluta` e 11 membros
- **Quando** a votacao alcanca 6 votos pela rejeicao do veto e encerro
- **Entao** `Resultado = Aprovado` e a materia segue a promulgacao.

**Cenario 7 — Maioria qualificada para Emenda a LOM (2/3 em dois turnos)**
- **Dado** uma `VotacaoNominal` com `MaioriaExigida = Qualificada`, 11 membros, no 1o turno
- **Quando** sao registrados 8 votos `Sim` (>= ceil(2*11/3) = 8) e encerro
- **Entao** `Resultado = Aprovado` no turno 1 (a aprovacao final ainda exige o turno 2).

**Cenario 8 — Voto fora de votacao aberta**
- **Dado** uma votacao `Encerrada`
- **Quando** executo `RegistrarVotoCommand(...)`
- **Entao** ocorre `InvalidOperationException` (votacao nao esta aberta).

**Cenario 9 — Encerramento de votacao ja encerrada**
- **Dado** uma votacao `Encerrada`
- **Quando** executo `EncerrarVotacaoCommand(id)`
- **Entao** ocorre `InvalidOperationException` (situacao ≠ `Aberta`).

**Cenario 10 — Voto duplo do mesmo vereador (nominal)**
- **Dado** uma `VotacaoNominal` aberta em que um vereador ja votou
- **Quando** o mesmo `VereadorId` tenta votar de novo com outro `votoId`
- **Entao** ocorre `InvalidOperationException` / viola o indice unico `(VotacaoId, VereadorId)`.

**Cenario 11 — Sigilo da votacao secreta**
- **Dado** uma `VotacaoSecreta` encerrada
- **Quando** executo `ListarVotosNominaisQuery(id)`
- **Entao** a consulta e bloqueada/retorna apenas o placar agregado, sem identificar vereadores (I-12).

**Cenario 12 — Placar em tempo real**
- **Dado** uma votacao `Aberta` com votos sendo registrados
- **Quando** executo `ObterPlacarDaVotacaoQuery(id)`
- **Entao** retorna o agregado `Sim`/`Nao`/`Abstencao` corrente, tenant-scoped.

**Cenario 13 — Cancelamento de votacao**
- **Dado** uma votacao `Aberta`
- **Quando** executo `CancelarVotacaoCommand(id)`
- **Entao** situacao = `Cancelada` e sem `Resultado`.

**Cenario 14 — Isolamento entre tenants**
- **Dado** votacoes do tenant A (Camara) e de outro tenant B
- **Quando** executo `ObterVotacaoPorIdQuery(id)` no contexto do tenant A para um id do tenant B
- **Entao** nao retorna a votacao do tenant B (isolamento por Global Query Filter).

---

## 13. Casos de Borda

- **B-1.** `TotalMembros <= 0` ou `Presentes <= 0` em `Iniciar` ⇒ `ArgumentException`/validator (I-1).
- **B-2.** Maioria simples com empate (`votosSim == Presentes / 2`) ⇒ `Rejeitado` (exige estritamente > 50%).
- **B-3.** Maioria absoluta com `votosSim == TotalMembros / 2` (sem o +1) ⇒ `Rejeitado`.
- **B-4.** Maioria qualificada com `votosSim == ceil(2*TotalMembros/3) - 1` ⇒ `Rejeitado` no turno.
- **B-5.** Abstencoes nao contam como `Sim` (I-13); muitas abstencoes podem levar a `Rejeitado` por falta de maioria.
- **B-6.** Reenvio do mesmo `votoId` ⇒ no-op idempotente (I-3); indice unico `(VotacaoId, VotoId)` previne duplicacao.
- **B-7.** Voto com `Sentido` invalido (fora do enum) ⇒ validator rejeita.
- **B-8.** `RegistrarVoto` em votacao `Cancelada`/`Encerrada` ⇒ `InvalidOperationException` (I-2/I-10).
- **B-9.** `Encerrar` votacao sem nenhum voto ⇒ `Resultado = Rejeitado` (zero `Sim` nao atinge maioria).
- **B-10.** `Cancelar` votacao ja `Encerrada` ⇒ `InvalidOperationException` (so a partir de `Aberta`).
- **B-11.** `ListarVotosNominais` sobre `Secreta` ⇒ bloqueada (I-12), preservando sigilo.
- **B-12.** Votacao qualificada exige aprovacao em **dois turnos**; aprovacao apenas no turno 1 nao conclui a Emenda a LOM (decisao de turno 2 e externa a esta votacao).
- **B-13.** `ResultadoVotacaoIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).
- **B-14.** Consultas em tenant da Camara nao retornam dados do tenant do Executivo (isolamento por Global Query Filter).

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial — derivada do README do modulo Legislativo (linguagem ubiqua, mapa de dominio, regras criticas, integracoes, cenarios BDD e fontes legais) para o agregado `Votacao`. |

<!-- manifest
commands: IniciarVotacao, RegistrarVoto, EncerrarVotacao, CancelarVotacao
queries: ObterVotacaoPorId, ObterPlacarDaVotacao, ListarVotosNominais, ListarVotacoes, ObterPainelDaVotacao
domainEvents: VotacaoIniciada, VotoRegistrado, VotacaoEncerrada
integrationEventsPublished: ResultadoVotacaoIntegrationEvent
integrationEventsConsumed: 
-->
