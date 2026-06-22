---
modulo: RecursosHumanos
agregado: Cargo
contexto: RecursosHumanos (estrutura de cargos públicos e plano de cargos)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 37, II (concurso público para cargo efetivo)", "CF/1988 art. 37, XI (teto remuneratório)", "Lei 8.112/1990 (RJU — supletivo ao estatuto municipal)", "EC 103/2019 (regimes RPPS/RGPS)", "eSocial — S-1005 (estabelecimentos), S-1010 (rubricas), S-1020 (lotação tributária)"]
---

# Cargo — Regras-as-Code (Rules-as-Code)

> Posição na estrutura de pessoal do ente público: **efetivo** (provido por concurso — CF art. 37,
> II), **comissionado** (livre nomeação/exoneração) ou **temporário**. Define vencimento, lotação e
> regime previdenciário associado (efetivo → RPPS; demais → RGPS — EC 103/2019), e sujeita-se ao
> **teto remuneratório** (CF art. 37, XI). Este arquivo é **normativo e versionado**; o código
> (`Cargo.cs`, handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Cargo Público (`Cargo`) | Posição na estrutura de pessoal. Raiz de agregado. |
| Cargo Efetivo (`TipoCargo.Efetivo`) | Provido por concurso público; regime RPPS; gera estabilidade após 3 anos. |
| Cargo Comissionado (`TipoCargo.Comissionado`) | Livre nomeação e exoneração; regime RGPS. |
| Cargo Temporário (`TipoCargo.Temporario`) | Contratação por tempo determinado; regime RGPS. |
| Vencimento (`Vencimento` : `Vencimento`) | Remuneração-base do cargo (VO monetário). |
| Lotação (`Lotacao` : `Lotacao`) | Unidade/estabelecimento de exercício (mapeia S-1005/S-1020). |
| Plano de Cargos (`PlanoDeCargos`) | Entidade-filha que agrupa cargos por carreira/estrutura. |
| Teto Remuneratório (`TetoRemuneratorio`) | Limite constitucional aplicado via abate-teto (CF art. 37, XI). |
| Regime Previdenciário (`RegimePrevidenciario`) | RPPS (efetivo) ou RGPS (comissionado/temporário). |
| Provimento (`Provimento`) | Ato de preenchimento do cargo (nomeação). |
| Vacância (`Vagar` / `Vago`) | Estado do cargo sem ocupante provido. |
| Quantitativo de Vagas (`QuantidadeVagas`) | Número de vagas autorizadas em lei para o cargo. |
| Tenant (`TenantId`) | Ente público (Prefeitura ou Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoCargo`) | Estado atual do cargo. |

---

## 2. Modelo

- **Identidade:** `CargoId` — `readonly record struct CargoId(Guid Value)`; fábrica `CargoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Cargo : AggregateRoot<CargoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Criar(...)`.
- **Entidade-filha:** `PlanoDeCargos` (associação/coleção exposta somente através da raiz).

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Denominacao` | `string` | Nome/denominação legal do cargo. | `private set` |
| `Tipo` | `TipoCargo` (enum) | Efetivo, comissionado ou temporário. | `private set` |
| `Vencimento` | `Vencimento` (VO) | Remuneração-base do cargo. | `private set` |
| `Lotacao` | `Lotacao` (VO) | Lotação/estabelecimento de exercício. | `private set` |
| `Regime` | `RegimePrevidenciario` (enum) | Derivado do `Tipo` (efetivo → RPPS; demais → RGPS). | `private set` |
| `QuantidadeVagas` | `int` | Vagas autorizadas em lei. | `private set` |
| `VagasOcupadas` | `int` | Vagas atualmente providas. | `private set` |
| `LeiCriacao` | `string` | Lei que criou o cargo (referência normativa). | `private set` |
| `Situacao` | `SituacaoCargo` | Situação atual (ativo/vago/extinto). | `private set` |
| `PlanoDeCargosId` | `Guid?` | Plano de cargos a que pertence (opcional). | `private set` |

### Constantes

- `VagasMinimas` = `1` (todo cargo nasce com ao menos 1 vaga autorizada). Constante de domínio.

### Value Objects (referenciados)

- `Vencimento` — valor monetário da remuneração-base; expõe `Valor` (`decimal`). Não nulo (validado em `Criar`).
- `Lotacao` — VO com identificação do estabelecimento/unidade; alinhado a S-1005 (estabelecimento) e S-1020 (lotação tributária).

### Enum `TipoCargo`

| Valor | Numérico | Descrição |
|---|---|---|
| `Efetivo` | 1 | Provido por concurso público (CF art. 37, II); RPPS. |
| `Comissionado` | 2 | Livre nomeação/exoneração; RGPS. |
| `Temporario` | 3 | Contratação por tempo determinado; RGPS. |

### Enum `SituacaoCargo`

| Valor | Numérico | Descrição |
|---|---|---|
| `Ativo` | 1 | Cargo ativo (com ao menos uma vaga ocupável; estado inicial). |
| `Vago` | 2 | Cargo ativo, porém sem ocupante provido. |
| `Extinto` | 3 | Cargo extinto por lei (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Extinto` }.
> - **Provível** (admite provimento) = { `Ativo`, `Vago` } com `VagasOcupadas < QuantidadeVagas`.
> - **Mapeamento Tipo → Regime:** `Efetivo → Rpps`; `Comissionado | Temporario → Rgps`.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A criação exige `Denominacao` não vazia, `Vencimento` não nulo, `Lotacao` não nula e `QuantidadeVagas` ≥ 1 (`ArgumentException`/`ArgumentNullException`).
- **I-2.** O **regime previdenciário** é **derivado** do tipo: `Efetivo → Rpps`; `Comissionado | Temporario → Rgps` (EC 103/2019). Não pode ser informado em contradição ao tipo.
- **I-3.** Cargo **efetivo** só é provido mediante **concurso público** (CF art. 37, II); o provimento de efetivo pressupõe candidato aprovado em concurso (regra orquestrada com `Servidor`).
- **I-4.** O `Vencimento` do cargo está **sujeito ao teto remuneratório** (CF art. 37, XI); o abate-teto é aplicado no cálculo da folha quando os proventos somados ultrapassam o teto (ver módulo `FolhaDePagamento`).
- **I-5.** `VagasOcupadas` nunca excede `QuantidadeVagas`; tentar prover além do quantitativo lança `InvalidOperationException`.
- **I-6.** `VagasOcupadas` é decrementado na vacância e incrementado no provimento; `VagasOcupadas ≥ 0` sempre.
- **I-7.** Reduzir `QuantidadeVagas` para valor **inferior** a `VagasOcupadas` é proibido (`InvalidOperationException`).
- **I-8.** Cargo **extinto** (`Extinto`) é terminal e não admite provimento nem alteração de vencimento/vagas.
- **I-9.** A extinção só é permitida quando `VagasOcupadas == 0` (não há servidor em exercício no cargo).
- **I-10.** A `Lotacao` referencia estabelecimento **vigente** em S-1005 na competência (consistência com eSocial — ver §11).
- **I-11.** Na criação, a situação inicial é `Ativo`, `VagasOcupadas == 0` e é emitido o evento `CargoCriado(id, denominacao, tipo)`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Criar` | `Ativo` | `denominacao` não vazia; `vencimento`/`lotacao` não nulos; `vagas` ≥ 1 | `CargoCriado` |
| `Ativo` \| `Vago` | `Prover` | `Ativo` | `VagasOcupadas < QuantidadeVagas`; (se efetivo) candidato de concurso | `CargoProvido` |
| `Ativo` | `Vagar` | `Vago` | `VagasOcupadas - 1 == 0` (última vaga liberada) | `CargoVago` |
| `Ativo` \| `Vago` | `AlterarVencimento` | (mantém) | situação ∉ {`Extinto`} | `VencimentoAlterado` |
| `Ativo` \| `Vago` | `AlterarQuantidadeVagas` | (mantém) | nova quantidade ≥ `VagasOcupadas` | — |
| `Vago` (ou `Ativo` com `VagasOcupadas == 0`) | `Extinguir` | `Extinto` | `VagasOcupadas == 0` | `CargoExtinto` |

> Observações:
> - `Prover` mantém a situação `Ativo` (apenas incrementa `VagasOcupadas`); só altera o estado nominal quando se sai de `Vago` para `Ativo`.
> - `Vagar` leva a `Vago` apenas quando a **última** vaga ocupada é liberada; com vagas remanescentes, permanece `Ativo` com `VagasOcupadas` decrementado.
> - A derivação `Tipo → Regime` ocorre na criação e não muda no ciclo de vida do cargo.

---

## 5. Comandos (escrita)

### 5.1 CriarCargo

- **Command:** `CriarCargoCommand(string Denominacao, TipoCargo Tipo, decimal Vencimento, LotacaoDto Lotacao, int QuantidadeVagas, string LeiCriacao, Guid? PlanoDeCargosId) : ICommand<Guid>`.
- **Entrada (DTO):** `Denominacao`, `Tipo`, `Vencimento`, `Lotacao`, `QuantidadeVagas`, `LeiCriacao`, `PlanoDeCargosId?`.
- **Dependências do handler:** `ICargoRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pré-condições:**
  - `request` não nulo.
  - `QuantidadeVagas` ≥ 1 (I-1); `Vencimento`/`Lotacao` válidos.
  - `Tipo` válido; regime derivado coerente (I-2).
- **Efeito:** cria via `Cargo.Criar(tenant.TenantId, request.Denominacao, request.Tipo, new Vencimento(request.Vencimento), lotacao, request.QuantidadeVagas, request.LeiCriacao, request.PlanoDeCargosId)`; `cargos.Adicionar(cargo)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Cargo` em situação `Ativo`, `VagasOcupadas == 0`; retorna `cargo.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException`/`ArgumentException` (campos); `InvalidOperationException` (regime/tipo incoerente).
- **Evento de domínio:** `CargoCriado(id, denominacao, tipo)` (emitido no construtor via factory).

### 5.2 ProverCargo

- **Command:** `ProverCargoCommand(Guid CargoId) : ICommand`.
- **Entrada (DTO):** `CargoId`.
- **Dependências do handler:** `ICargoRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; cargo existe, senão `InvalidOperationException("Cargo não encontrado.")`; `VagasOcupadas < QuantidadeVagas` (I-5); situação ∉ {`Extinto`} (I-8).
- **Efeito:** `cargo.Prover()`; `SaveChangesAsync`.
- **Pós-condições:** `VagasOcupadas` incrementado; situação `Ativo`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, sem vaga ou extinto).
- **Evento de domínio:** `CargoProvido(Id)`.

### 5.3 VagarCargo

- **Command:** `VagarCargoCommand(Guid CargoId) : ICommand`.
- **Entrada (DTO):** `CargoId`.
- **Dependências do handler:** `ICargoRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; cargo existe; `VagasOcupadas > 0`; situação ∉ {`Extinto`} (I-8).
- **Efeito:** `cargo.Vagar()`; `SaveChangesAsync`.
- **Pós-condições:** `VagasOcupadas` decrementado; situação `Vago` se zerou, senão `Ativo`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, sem ocupante ou extinto).
- **Evento de domínio:** `CargoVago(Id)` (emitido quando passa a `Vago`).

### 5.4 AlterarVencimento

- **Command:** `AlterarVencimentoCommand(Guid CargoId, decimal NovoVencimento) : ICommand`.
- **Entrada (DTO):** `CargoId`, `NovoVencimento`.
- **Dependências do handler:** `ICargoRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; cargo existe; situação ∉ {`Extinto`} (I-8); `NovoVencimento` > 0.
- **Efeito:** `cargo.AlterarVencimento(new Vencimento(request.NovoVencimento))`; `SaveChangesAsync`.
- **Pós-condições:** `Vencimento` atualizado (sujeito a abate-teto na folha — I-4).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou extinto).
- **Evento de domínio:** `VencimentoAlterado(Id, novoVencimento)`.

### 5.5 ExtinguirCargo

- **Command:** `ExtinguirCargoCommand(Guid CargoId, string LeiExtincao) : ICommand`.
- **Entrada (DTO):** `CargoId`, `LeiExtincao`.
- **Dependências do handler:** `ICargoRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; cargo existe; `VagasOcupadas == 0` (I-9); situação ∉ {`Extinto`} (I-8).
- **Efeito:** `cargo.Extinguir(request.LeiExtincao)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Extinto` (terminal).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, com ocupantes ou já extinto).
- **Evento de domínio:** `CargoExtinto(Id, leiExtincao)`.

> **Comandos de domínio existentes no agregado sem handler de Application dedicado nesta versão:** `AlterarQuantidadeVagas(int)` (método da raiz, exposto para futura orquestração de caso de uso, com guarda I-7).

---

## 6. Consultas (leitura)

### 6.1 ObterCargoPorId

- **Query:** `ObterCargoPorIdQuery(Guid CargoId) : IQuery<CargoDetalhe?>`.
- **Entrada:** `CargoId`.
- **Handler:** `ObterCargoPorIdHandler(ICargoRepository cargos)`; chama `cargos.ObterPorIdAsync(new CargoId(request.CargoId), ct)`.
- **Projeção (DTO):** `CargoDetalhe(Guid Id, string Denominacao, string Tipo, decimal Vencimento, string Lotacao, string Regime, int QuantidadeVagas, int VagasOcupadas, string Situacao, string LeiCriacao)`.
  - `Vencimento` projetado de `cargo.Vencimento.Valor`; `Tipo`/`Regime`/`Situacao` de `ToString()`.
- **Filtros:** por `CargoId`; **sempre tenant-scoped** via Global Query Filter por `TenantId` no DbContext.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarCargosComVagas

- **Query:** `ListarCargosComVagasQuery(TipoCargo? Tipo) : IQuery<IReadOnlyList<CargoResumo>>`.
- **Entrada:** `Tipo?` (filtro opcional por tipo de cargo).
- **Handler:** `ListarCargosComVagasHandler(ICargoRepository cargos)`; chama `cargos.ListarComVagasDisponiveisAsync(request.Tipo, ct)` (situação ∉ {`Extinto`} e `VagasOcupadas < QuantidadeVagas`).
- **Projeção (DTO):** `IReadOnlyList<CargoResumo>` com `(Guid Id, string Denominacao, string Tipo, int VagasDisponiveis)`.
- **Filtros:** vagas disponíveis e (opcional) `Tipo`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...RecursosHumanos.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `CargoCriado` | `(CargoId, string Denominacao, TipoCargo Tipo)` | `Cargo.Criar` (construtor) |
| `CargoProvido` | `(CargoId)` | `Cargo.Prover` |
| `CargoVago` | `(CargoId)` | `Cargo.Vagar` |
| `VencimentoAlterado` | `(CargoId, decimal NovoVencimento)` | `Cargo.AlterarVencimento` |
| `CargoExtinto` | `(CargoId, string LeiExtincao)` | `Cargo.Extinguir` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...RecursosHumanos.Contracts`)

- Nenhum nesta versão. O agregado `Cargo` é estrutural/interno ao módulo; alterações de estrutura de cargos não disparam Integration Events cross-module nesta versão (a integração de pessoal parte do agregado `Servidor`).

### Integração (consome)

- Nenhum (esta versão não consome Integration Events de outros módulos no agregado Cargo).

---

## 8. Validações (FluentValidation)

### CriarCargoValidator (`AbstractValidator<CriarCargoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Denominacao` | `NotEmpty()` + `MaximumLength(120)` | "Denominação do cargo é obrigatória (máx. 120 caracteres)." |
| `Tipo` | `IsInEnum()` | "Tipo de cargo inválido." |
| `Vencimento` | `GreaterThan(0)` | "Vencimento deve ser maior que zero." |
| `Lotacao` | `NotNull()` | "Lotação é obrigatória." |
| `QuantidadeVagas` | `GreaterThanOrEqualTo(1)` | "Quantidade de vagas deve ser ao menos 1." |
| `LeiCriacao` | `NotEmpty()` + `MaximumLength(80)` | "Lei de criação é obrigatória." |

### AlterarVencimentoValidator (`AbstractValidator<AlterarVencimentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `CargoId` | `NotEmpty()` | "Cargo é obrigatório." |
| `NovoVencimento` | `GreaterThan(0)` | "Vencimento deve ser maior que zero." |

### ExtinguirCargoValidator (`AbstractValidator<ExtinguirCargoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `CargoId` | `NotEmpty()` | "Cargo é obrigatório." |
| `LeiExtincao` | `NotEmpty()` + `MaximumLength(80)` | "Lei de extinção é obrigatória." |

> `ProverCargoCommand` e `VagarCargoCommand` apoiam-se em invariantes de domínio e checagens de existência no handler; consultas (`ObterCargoPorId`, `ListarCargosComVagas`) não possuem validador dedicado.

---

## 9. Persistência (EF Core 8)

- **Schema:** `recursoshumanos` (isolado por módulo). **DbContext:** o do módulo RecursosHumanos. **Migrations:** por módulo.
- **Tabela:** `Cargo` (raiz de agregado); `PlanoDeCargos` (entidade-filha/relacionada).

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `CargoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Denominacao` | `nvarchar(120)` | obrigatória. |
| `Tipo` | `int` | enum `TipoCargo` (valor numérico). |
| `Vencimento` | `decimal` | conversor/owned de `Vencimento` (campo `Valor`). |
| `Lotacao` | (owned) | VO `Lotacao` mapeado como owned type (colunas próprias); alinhado a S-1005/S-1020. |
| `Regime` | `int` | enum `RegimePrevidenciario` (derivado do tipo). |
| `QuantidadeVagas` | `int` | vagas autorizadas. |
| `VagasOcupadas` | `int` | vagas providas (≥ 0, ≤ `QuantidadeVagas`). |
| `LeiCriacao` | `nvarchar(80)` | referência normativa. |
| `Situacao` | `int` | enum `SituacaoCargo` (valor numérico). |
| `PlanoDeCargosId` | `Guid?` | FK opcional ao `PlanoDeCargos`. |

- **Entidade-filha `PlanoDeCargos`:** tabela `PlanoDeCargos` com `Id`, `TenantId`, denominação da carreira; relação 1:N com `Cargo`.
- **Índices:**
  - PK em `Id`.
  - **Índice único** em `(TenantId, Denominacao, LeiCriacao)` (unicidade do cargo por denominação/lei no tenant).
  - Índice em `(TenantId, Tipo, Situacao)` para `ListarCargosComVagas`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto RecursosHumanos disponível; sem Integration Events publicados por este agregado nesta versão.

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Cargo` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição. **Executivo e Legislativo do mesmo município são tenants distintos** (estruturas de cargos independentes).
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Criação/alteração/extinção/provimento/vacância de cargos: papéis de **Gestão de Pessoal/RH** com competência sobre estrutura (ex.: `RecursosHumanos.Cargo.Gerir`).
  - Consulta de cargos: papel de **leitura de RH** (ex.: `RecursosHumanos.Cargo.Ler`).
  - Módulo RecursosHumanos é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** o agregado `Cargo` é estrutural e **não armazena dados pessoais** de servidores (vínculo está em `Servidor`); minimização aplicada na projeção `CargoResumo`/`CargoDetalhe`.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Criar`, `Prover`, `Vagar`, `AlterarVencimento`, `AlterarQuantidadeVagas`, `Extinguir`) — destinada ao Tribunal de Contas (TCE-RS); alterações de vencimento e de quantitativo de vagas são especialmente relevantes para o controle externo.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **eSocial (tabelas) — coerência da Lotação:**
  - A `Lotacao` do cargo referencia **estabelecimento** declarado em **S-1005** e, quando aplicável, **lotação tributária** em **S-1020**, ambos **vigentes na competência** (I-10).
  - **Rubricas** aplicáveis à remuneração do cargo existem em **S-1010** (vigentes na competência) — consumidas pelo agregado `FolhaDePagamento`.
- **Teto remuneratório (CF art. 37, XI):** o `Vencimento` é insumo do cálculo da folha; o **abate-teto** é aplicado em `FolhaDePagamento` quando os proventos somados ultrapassam o teto.
- **Sem chamadas externas diretas:** o agregado `Cargo` não realiza I/O com sistemas governamentais; sua consistência com o eSocial é garantida por validação de referência (S-1005/S-1010/S-1020) no momento do uso pela folha. Quando houver orquestração que toque sistemas externos, aplica-se **ACL + Polly (retry/circuit breaker) + idempotência**.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Criação de cargo efetivo deriva RPPS**
- **Dado** os dados de um cargo do tipo `Efetivo`
- **Quando** executo `CriarCargoCommand`
- **Então** é criado um `Cargo` em situação `Ativo` com `Regime == Rpps`, `VagasOcupadas == 0`, e o evento `CargoCriado` é emitido.

**Cenário 2 — Criação de cargo comissionado deriva RGPS**
- **Dado** os dados de um cargo do tipo `Comissionado`
- **Quando** executo `CriarCargoCommand`
- **Então** o `Cargo` nasce com `Regime == Rgps`.

**Cenário 3 — Criação inválida sem vagas**
- **Dado** um `CriarCargoCommand` com `QuantidadeVagas == 0`
- **Quando** executo o comando
- **Então** a validação rejeita (`GreaterThanOrEqualTo(1)`) / `ArgumentException` no domínio (I-1).

**Cenário 4 — Provimento dentro do quantitativo**
- **Dado** um cargo `Ativo` com `VagasOcupadas < QuantidadeVagas`
- **Quando** executo `ProverCargoCommand`
- **Então** `VagasOcupadas` é incrementado e o evento `CargoProvido` é emitido.

**Cenário 5 — Provimento além do quantitativo**
- **Dado** um cargo com `VagasOcupadas == QuantidadeVagas`
- **Quando** executo `ProverCargoCommand`
- **Então** ocorre `InvalidOperationException` (sem vaga disponível — I-5).

**Cenário 6 — Vacância da última vaga**
- **Dado** um cargo `Ativo` com `VagasOcupadas == 1`
- **Quando** executo `VagarCargoCommand`
- **Então** `VagasOcupadas == 0`, situação passa a `Vago` e `CargoVago` é emitido.

**Cenário 7 — Alteração de vencimento**
- **Dado** um cargo `Ativo`
- **Quando** executo `AlterarVencimentoCommand` com novo valor
- **Então** `Vencimento` é atualizado e `VencimentoAlterado` é emitido (sujeito a abate-teto na folha).

**Cenário 8 — Redução de vagas abaixo do ocupado**
- **Dado** um cargo com `VagasOcupadas == 3`
- **Quando** chamo `AlterarQuantidadeVagas(2)`
- **Então** ocorre `InvalidOperationException` (nova quantidade < ocupadas — I-7).

**Cenário 9 — Extinção com cargo ocupado**
- **Dado** um cargo com `VagasOcupadas > 0`
- **Quando** executo `ExtinguirCargoCommand`
- **Então** ocorre `InvalidOperationException` (extinção exige `VagasOcupadas == 0` — I-9).

**Cenário 10 — Extinção de cargo vago**
- **Dado** um cargo `Vago` com `VagasOcupadas == 0`
- **Quando** executo `ExtinguirCargoCommand`
- **Então** a situação passa a `Extinto` e `CargoExtinto` é emitido.

**Cenário 11 — Operação sobre cargo extinto**
- **Dado** um cargo `Extinto`
- **Quando** executo `ProverCargoCommand` / `AlterarVencimentoCommand`
- **Então** ocorre `InvalidOperationException` (estado terminal — I-8).

**Cenário 12 — Consulta tenant-scoped de cargos com vagas**
- **Dado** cargos com vagas no tenant A e no tenant B
- **Quando** executo `ListarCargosComVagasQuery` no contexto do tenant A
- **Então** retornam **apenas** os cargos com vagas do tenant A em `CargoResumo`.

---

## 13. Casos de Borda

- **B-1.** `Denominacao` vazia ⇒ rejeitada (`NotEmpty`) / `ArgumentException` (I-1).
- **B-2.** Informar `Regime` em contradição ao `Tipo` ⇒ ignorado/sobrescrito pela derivação ou `InvalidOperationException` (I-2).
- **B-3.** `QuantidadeVagas` negativa ⇒ rejeitada (I-1).
- **B-4.** Prover cargo `Vago` ⇒ permitido; volta a `Ativo` com `VagasOcupadas == 1`.
- **B-5.** Vagar cargo com `VagasOcupadas == 0` ⇒ `InvalidOperationException` (sem ocupante).
- **B-6.** Vagar com `VagasOcupadas > 1` ⇒ decrementa mas permanece `Ativo` (não emite `CargoVago`).
- **B-7.** `AlterarQuantidadeVagas` para exatamente `VagasOcupadas` ⇒ permitido (limite inferior aceito).
- **B-8.** Extinguir cargo já `Extinto` ⇒ `InvalidOperationException` (já terminal — I-8).
- **B-9.** Alterar vencimento de cargo `Extinto` ⇒ `InvalidOperationException` (I-8).
- **B-10.** `Lotacao` apontando estabelecimento não vigente em S-1005 ⇒ rejeitada na validação de referência ao ser usada pela folha (I-10).
- **B-11.** Cargo efetivo provido sem candidato de concurso ⇒ rejeitado na orquestração com `Servidor` (I-3 / CF art. 37, II).
- **B-12.** Denominação/lei duplicadas no tenant ⇒ rejeitadas pelo índice único `(TenantId, Denominacao, LeiCriacao)`.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo RecursosHumanos (estrutura de cargos: tipo efetivo/comissionado/temporário, derivação de regime RPPS/RGPS, vencimento/lotação, quantitativo de vagas, provimento/vacância/extinção; coerência com S-1005/S-1010/S-1020 e teto remuneratório). |

<!-- manifest
commands: CriarCargo, ProverCargo, VagarCargo, AlterarVencimento, ExtinguirCargo
queries: ObterCargoPorId, ListarCargosComVagas
domainEvents: CargoCriado, CargoProvido, CargoVago, VencimentoAlterado, CargoExtinto
integrationEventsPublished: 
integrationEventsConsumed: 
-->
