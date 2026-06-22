---
modulo: Administracao
agregado: Fornecedor
contexto: Administracao (Compras Públicas — cadastro e sanção de fornecedores sob a NLLC)
poder: Ambos
schema: administracao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "Lei 14.133/2021 (NLLC) — norma central das compras públicas"
  - "Lei 14.133/2021 art. 87 (Registro Cadastral / SICAF)"
  - "Lei 14.133/2021 art. 14 (vedações à participação)"
  - "Lei 14.133/2021 art. 155 a 156 (infrações e sanções administrativas)"
  - "Lei 14.133/2021 art. 156, IV (declaração de inidoneidade)"
  - "Lei 14.133/2021 art. 156, III (impedimento de licitar e contratar)"
  - "Decreto 11.246/2022 (agentes de contratação)"
---

# Fornecedor — Regras-as-Code (Rules-as-Code)

> Pessoa jurídica/física apta a contratar com a Administração, identificada por **CNPJ** válido
> (validado na **Receita** na criação), com **nível cadastral** no **SICAF** e eventual histórico
> de **sanções** (advertência, multa, impedimento, inidoneidade — art. 156). Um fornecedor com
> **inidoneidade** (ou **impedimento**) **vigente** **não** pode ser habilitado nem contratado.
> Este arquivo é **normativo e versionado**; o código de domínio, aplicação, persistência e
> testes do agregado `Fornecedor` é **gerado e mantido a partir daqui**. Bug, ajuste ou nova
> regra ⇒ edita-se **este arquivo**; o código é consequência.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Fornecedor (`Fornecedor`) | Pessoa jurídica/física apta a contratar com a Administração. Raiz de agregado. |
| Identidade (`FornecedorId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| CNPJ (`Cnpj` : `Cnpj`) | Cadastro Nacional da Pessoa Jurídica, validado (14 dígitos + DV). Value Object (SharedKernel). |
| Razão Social (`RazaoSocial` : `string`) | Denominação do fornecedor. |
| Nível Cadastral SICAF (`NivelCadastralSICAF` : `NivelCadastralSICAF`) | Nível de cadastramento/regularidade no SICAF (art. 87). Value Object/enum. |
| Sanção (`Sancao`) | Penalidade administrativa aplicada (art. 156). Entidade-filha. |
| Tipo de Sanção (`TipoSancao` : `TipoSancao`) | `Advertencia`, `Multa`, `Impedimento`, `Inidoneidade`. |
| Inidoneidade (`Inidoneidade`) | Sanção mais grave; impede licitar/contratar com toda a Administração Pública (art. 156, IV). |
| Impedimento (`Impedimento`) | Impedimento de licitar e contratar (art. 156, III). |
| Sanção Vigente (`PossuiSancaoImpeditivaVigente` / `EstaImpedido`) | Sanção de impedimento/inidoneidade dentro do período de vigência. |
| Cadastrar (`CadastrarFornecedor`) | Criar o fornecedor (CNPJ validado na Receita) — nasce `Ativo`. |
| Atualizar Nível SICAF (`AtualizarNivelSicaf`) | Atualizar o nível cadastral consultado no SICAF. |
| Aplicar Sanção (`AplicarSancao` / `FornecedorSancionado`) | Registrar uma sanção administrativa. |
| Reabilitar (`ReabilitarFornecedor`) | Encerrar/cumprir a sanção e restabelecer a aptidão. |
| Inativar (`InativarFornecedor`) | Tornar o cadastro inativo (passa a `Inativo`). |
| Situacao (`Situacao` : `SituacaoFornecedor`) | Estado cadastral do fornecedor. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |

---

## 2. Modelo

- **Identidade:** `FornecedorId` — `readonly record struct FornecedorId(Guid Value)`; fábrica `FornecedorId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Fornecedor : AggregateRoot<FornecedorId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Cadastrar(...)`.

### 2.1 Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `Id` | `FornecedorId` | Identidade do agregado. | `init` |
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Cnpj` | `Cnpj` (VO) | CNPJ validado (sem máscara). | `private set` |
| `RazaoSocial` | `string` | Denominação do fornecedor. | `private set` |
| `NivelCadastralSICAF` | `NivelCadastralSICAF` | Nível cadastral no SICAF. | `private set` |
| `Situacao` | `SituacaoFornecedor` | Situação cadastral. | `private set` |
| `Sancoes` | `IReadOnlyCollection<Sancao>` | Histórico de sanções. | coleção encapsulada |

### 2.2 Value Objects e enums

- **`Cnpj`** (SharedKernel, `ValueObject`) — `Digitos` (14 dígitos sem máscara), fábricas `Cnpj.Create(valor)` / `Cnpj.TryCreate(valor, out cnpj)`, `IsValid(digitos)` (DV conferidos), `Formatar()`. Lança `ArgumentException` quando inválido.
- **`NivelCadastralSICAF`** (enum):

  | Valor | Numérico | Descrição |
  |---|---|---|
  | `NaoCadastrado` | 0 | Sem cadastro no SICAF. |
  | `CredenciamentoNivel1` | 1 | Credenciamento (nível I). |
  | `HabilitacaoJuridicaNivel2` | 2 | Habilitação jurídica (nível II). |
  | `RegularidadeFiscalNivel3` | 3 | Regularidade fiscal e trabalhista (nível III). |
  | `QualificacaoEconomicaNivel4` | 4 | Qualificação econômico-financeira (nível IV). |
  | `QualificacaoTecnicaNivel5` | 5 | Qualificação técnica (nível V). |

- **`TipoSancao`** (enum):

  | Valor | Numérico | Descrição | Base legal | Impeditiva? |
  |---|---|---|---|---|
  | `Advertencia` | 1 | Advertência. | art. 156, I | Não |
  | `Multa` | 2 | Multa. | art. 156, II | Não |
  | `Impedimento` | 3 | Impedimento de licitar e contratar (ente/esfera). | art. 156, III | **Sim** |
  | `Inidoneidade` | 4 | Declaração de inidoneidade (toda a Administração). | art. 156, IV | **Sim** |

### 2.3 Entidade-filha

- **`Sancao`** — `SancaoId`, `Tipo` (`TipoSancao`), `DataInicio` (`DateOnly`), `DataFim` (`DateOnly?`; nulo = sem termo final definido), `ProcessoAdministrativo` (`string`), `Fundamentacao` (`string`), `ValorMulta` (`ValorMonetario?`; quando `Multa`).
  - **`EstaVigente(hoje)`**: `DataInicio <= hoje && (DataFim == null || hoje <= DataFim)`.

### 2.4 Enum `SituacaoFornecedor`

| Valor | Numérico | Descrição |
|---|---|---|
| `Ativo` | 1 | Apto a participar/contratar (estado inicial). |
| `Sancionado` | 2 | Com sanção **impeditiva** vigente (impedimento/inidoneidade). |
| `Inativo` | 3 | Cadastro inativo (terminal administrativo). |

> **Conjuntos de referência usados nas guardas:**
> - **Impeditivos** = { `Impedimento`, `Inidoneidade` }.
> - **Apto** = `Situacao == Ativo` **e** sem `Sancao` impeditiva vigente.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Cnpj` é obrigatório e **válido** na criação (DV conferidos via `Cnpj.Create`); CNPJ inválido ⇒ `ArgumentException`.
- **I-2.** O `Cnpj` é validado na **Receita** na criação (consulta de existência/situação cadastral ativa); CNPJ inexistente/baixado ⇒ rejeitado pelo handler.
- **I-3.** `RazaoSocial` é obrigatória (não nula/não vazia); caso contrário, `ArgumentException`.
- **I-4.** `(TenantId, Cnpj)` é **único**: não há dois fornecedores com o mesmo CNPJ no mesmo tenant.
- **I-5.** Na criação, a situação inicial é `Ativo`, `NivelCadastralSICAF` default = `NaoCadastrado`, e é emitido o evento `FornecedorCadastrado`.
- **I-6.** `AplicarSancao` registra uma `Sancao`; se o tipo for **impeditivo** ({`Impedimento`,`Inidoneidade`}) e vigente, a situação passa a `Sancionado` e emite `FornecedorSancionado`.
- **I-7.** Sanção de **Inidoneidade** vigente torna `EstaImpedido == true` e **impede habilitação e contratação** (art. 14/156, IV) — regra consumida pelos agregados `Licitacao` e `Contrato`.
- **I-8.** Sanção de **Impedimento** vigente também torna `EstaImpedido == true` no âmbito do ente (art. 156, III).
- **I-9.** `Multa` exige `ValorMulta` não nulo e positivo; demais tipos não exigem valor.
- **I-10.** `Sancao` exige `ProcessoAdministrativo` e `Fundamentacao` não vazios (devido processo legal/motivação).
- **I-11.** `ReabilitarFornecedor` só é permitido quando **não há** sanção impeditiva vigente (todas cumpridas/encerradas); restabelece `Situacao = Ativo`.
- **I-12.** `EstaImpedido(hoje)` é verdadeiro se existe ao menos uma `Sancao` impeditiva com `EstaVigente(hoje) == true`.
- **I-13.** Fornecedor `Inativo` não admite `AplicarSancao` de forma a habilitá-lo; pode receber registro histórico, mas não participa de certames.
- **I-14.** Toda transição é **tenant-scoped**: o agregado nunca é lido/gravado fora do seu `TenantId`.
- **I-15.** `AtualizarNivelSicaf` apenas altera `NivelCadastralSICAF` (valor válido do enum); não altera situação nem sanções.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Cadastrar` | `Ativo` | CNPJ válido (Receita); razão social não vazia | `FornecedorCadastrado` |
| `Ativo`\|`Sancionado` | `AtualizarNivelSicaf` | (mantém) | nível válido do enum | — |
| `Ativo`\|`Sancionado` | `AplicarSancao` (impeditiva vigente) | `Sancionado` | tipo ∈ {`Impedimento`,`Inidoneidade`} e vigente | `FornecedorSancionado` |
| `Ativo`\|`Sancionado` | `AplicarSancao` (não impeditiva) | (mantém) | tipo ∈ {`Advertencia`,`Multa`} | `FornecedorSancionado` |
| `Sancionado` | `ReabilitarFornecedor` | `Ativo` | sem sanção impeditiva vigente | `FornecedorReabilitado` |
| `Ativo`\|`Sancionado` | `InativarFornecedor` | `Inativo` | — | `FornecedorInativado` |

> Observações:
> - `AplicarSancao` sempre emite `FornecedorSancionado`; a mudança para `Sancionado` ocorre apenas quando a sanção é **impeditiva** e **vigente**.
> - `EstaImpedido(hoje)` é calculado a partir do histórico de `Sancao`; uma sanção que expira (passa `DataFim`) deixa de impedir, mas a situação só volta a `Ativo` via `ReabilitarFornecedor`.

---

## 5. Comandos (escrita)

Cada comando tem `*Command` + `*Handler` + `*Validator` (FluentValidation). Todos resolvem `ITenantContext` e respeitam o Global Query Filter.

### 5.1 CadastrarFornecedor

- **Command:** `CadastrarFornecedorCommand(string Cnpj, string RazaoSocial) : ICommand<Guid>`.
- **Dependências do handler:** `IFornecedorRepository`, `IUnitOfWork`, `ITenantContext`, `IReceitaCnpjGateway` (validação na Receita).
- **Pré-condições:** `request` não nulo; `Cnpj` válido (`Cnpj.Create`) (I-1); CNPJ confirmado/ativo na Receita (I-2); `RazaoSocial` não vazia (I-3); inexistência de fornecedor com o mesmo `(TenantId, Cnpj)` (I-4).
- **Efeito:** cria via `Fornecedor.Cadastrar(tenant.TenantId, Cnpj.Create(cnpj), razaoSocial)`; `fornecedores.Adicionar(fornecedor)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Fornecedor` em `Ativo`, `NivelCadastralSICAF = NaoCadastrado`; retorna `fornecedor.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (CNPJ/razão social inválidos); `InvalidOperationException` (CNPJ duplicado no tenant ou inexistente na Receita).
- **Evento de domínio:** `FornecedorCadastrado(Id, Cnpj)`.

### 5.2 AtualizarNivelSicaf

- **Command:** `AtualizarNivelSicafCommand(Guid FornecedorId, NivelCadastralSICAF Nivel) : ICommand`.
- **Pré-condições:** `request` não nulo; fornecedor existe; `Nivel` válido do enum (I-15).
- **Efeito:** `fornecedor.AtualizarNivelSicaf(nivel)`; `SaveChangesAsync`. O valor pode provir de consulta ao SICAF/Compras.gov.br (§11).
- **Pós-condições:** `NivelCadastralSICAF` atualizado.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado).
- **Evento de domínio:** —.

### 5.3 AplicarSancao

- **Command:** `AplicarSancaoCommand(Guid FornecedorId, TipoSancao Tipo, DateOnly DataInicio, DateOnly? DataFim, string ProcessoAdministrativo, string Fundamentacao, decimal? ValorMulta) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; fornecedor existe; `ProcessoAdministrativo`/`Fundamentacao` não vazios (I-10); se `Multa`, `ValorMulta` positivo (I-9); `DataFim == null || DataFim >= DataInicio`.
- **Efeito:** `fornecedor.AplicarSancao(...)`; se impeditiva e vigente, passa a `Sancionado`; `SaveChangesAsync`; enfileira `FornecedorSancionadoIntegrationEvent` no Outbox.
- **Pós-condições:** nova `Sancao` registrada; situação `Sancionado` se impeditiva vigente; retorna `sancao.SancaoId.Value`.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (campos obrigatórios/valor); `InvalidOperationException`.
- **Evento de domínio:** `FornecedorSancionado(Id, sancaoId, tipo, dataInicio, dataFim)`.
- **Evento de integração (publica):** `FornecedorSancionadoIntegrationEvent`.

### 5.4 ReabilitarFornecedor

- **Command:** `ReabilitarFornecedorCommand(Guid FornecedorId) : ICommand`.
- **Pré-condições:** `request` não nulo; fornecedor existe; situação `Sancionado`; **sem** sanção impeditiva vigente (I-11).
- **Efeito:** `fornecedor.Reabilitar()`; passa a `Ativo`; `SaveChangesAsync`.
- **Pós-condições:** situação `Ativo`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (ainda impedido ou não `Sancionado`).
- **Evento de domínio:** `FornecedorReabilitado(Id)`.

### 5.5 InativarFornecedor

- **Command:** `InativarFornecedorCommand(Guid FornecedorId) : ICommand`.
- **Pré-condições:** `request` não nulo; fornecedor existe; situação ≠ `Inativo`.
- **Efeito:** `fornecedor.Inativar()`; passa a `Inativo`; `SaveChangesAsync`.
- **Pós-condições:** situação `Inativo`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException`.
- **Evento de domínio:** `FornecedorInativado(Id)`.

---

## 6. Consultas (leitura)

Toda consulta é **tenant-scoped** via Global Query Filter por `TenantId`.

### 6.1 ObterFornecedorPorId

- **Query:** `ObterFornecedorPorIdQuery(Guid FornecedorId) : IQuery<FornecedorDetalhe?>`.
- **Projeção (DTO):** `FornecedorDetalhe(Guid Id, string Cnpj, string RazaoSocial, string NivelCadastralSICAF, string Situacao, bool EstaImpedido, IReadOnlyList<SancaoResumo> Sancoes)`.
  - `Cnpj` projetado de `fornecedor.Cnpj.Formatar()`; `EstaImpedido` de `fornecedor.EstaImpedido(hoje)`.

### 6.2 ObterFornecedorPorCnpj

- **Query:** `ObterFornecedorPorCnpjQuery(string Cnpj) : IQuery<FornecedorDetalhe?>`.
- **Filtros:** por `Cnpj` (normalizado via `Cnpj.Create`); sempre tenant-scoped.

### 6.3 ListarFornecedoresImpedidos

- **Query:** `ListarFornecedoresImpedidosQuery(DateOnly Referencia) : IQuery<IReadOnlyList<FornecedorResumo>>`.
- **Projeção (DTO):** `FornecedorResumo(Guid Id, string Cnpj, string RazaoSocial, string Situacao, string NivelCadastralSICAF)`.
- **Filtros:** fornecedores com `Sancao` impeditiva vigente em `Referencia`; sempre tenant-scoped.

> Todos os handlers de consulta validam `request` não nulo (`ArgumentNullException.ThrowIfNull`).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Administracao.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `FornecedorCadastrado` | `(FornecedorId, Cnpj)` | `Fornecedor.Cadastrar` |
| `FornecedorSancionado` | `(FornecedorId, Guid SancaoId, TipoSancao Tipo, DateOnly DataInicio, DateOnly? DataFim)` | `Fornecedor.AplicarSancao` |
| `FornecedorReabilitado` | `(FornecedorId)` | `Fornecedor.Reabilitar` |
| `FornecedorInativado` | `(FornecedorId)` | `Fornecedor.Inativar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Administracao.Contracts`)

| Evento | Payload | Publicado por | Consumido por |
|---|---|---|---|
| `FornecedorSancionadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid FornecedorId, string Cnpj, string TipoSancao, DateOnly DataInicio, DateOnly? DataFim)` | `AplicarSancaoHandler` | Administracao (`Licitacao`/`Contrato` — bloqueio) e Transparencia |

### Integração (consome)

- Nenhum no agregado `Fornecedor` nesta versão.

---

## 8. Validações (FluentValidation)

### CadastrarFornecedorValidator (`AbstractValidator<CadastrarFornecedorCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Cnpj` | `NotEmpty()` + `Must(Cnpj.IsValid após extrair dígitos)` | "CNPJ inválido." |
| `RazaoSocial` | `NotEmpty()` + `MaximumLength(200)` | "Razão social é obrigatória (máx. 200)." |

### AplicarSancaoValidator (`AbstractValidator<AplicarSancaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FornecedorId` | `NotEmpty()` | "Fornecedor é obrigatório." |
| `Tipo` | `IsInEnum()` | "Tipo de sanção inválido." |
| `ProcessoAdministrativo` | `NotEmpty()` + `MaximumLength(60)` | "Processo administrativo é obrigatório." |
| `Fundamentacao` | `NotEmpty()` | "Fundamentação é obrigatória." |
| `DataFim` | `GreaterThanOrEqualTo(x => x.DataInicio).When(DataFim != null)` | "Data fim não pode ser anterior ao início." |
| `ValorMulta` | `GreaterThan(0).When(Tipo == Multa)` | "Valor da multa deve ser positivo." |

### AtualizarNivelSicafValidator / ReabilitarFornecedorValidator / InativarFornecedorValidator

| Comando | Campo | Regra |
|---|---|---|
| `AtualizarNivelSicafCommand` | `FornecedorId`, `Nivel` | `NotEmpty()`; `IsInEnum()` |
| `ReabilitarFornecedorCommand` | `FornecedorId` | `NotEmpty()` |
| `InativarFornecedorCommand` | `FornecedorId` | `NotEmpty()` |

---

## 9. Persistência (EF Core 8)

- **Schema:** `administracao` (isolado por módulo). **DbContext:** `AdministracaoDbContext`. **Migrations:** por módulo.
- **Tabela:** `Fornecedor` (raiz de agregado); `Sancao` em tabela-filha.

| Coluna (`Fornecedor`) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor `FornecedorId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Cnpj` | `nvarchar(14)` | conversor/owned de `Cnpj` (campo `Digitos`, sem máscara). |
| `RazaoSocial` | `nvarchar(200)` | obrigatório. |
| `NivelCadastralSICAF` | `int` | enum `NivelCadastralSICAF` (valor numérico). |
| `Situacao` | `int` | enum `SituacaoFornecedor` (valor numérico). |

| Coluna (`FornecedorSancao`) | Tipo lógico | Observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor `SancaoId` ↔ `Guid`. |
| `FornecedorId` | `Guid` (FK) | FK para `Fornecedor(Id)`; `TenantId` herdado. |
| `Tipo` | `int` | enum `TipoSancao`. |
| `DataInicio` | `date` | `DateOnly`. |
| `DataFim` | `date?` | `DateOnly?` (nulo = sem termo final). |
| `ProcessoAdministrativo` | `nvarchar(60)` | obrigatório. |
| `Fundamentacao` | `nvarchar(max)` | obrigatório. |
| `ValorMulta` | `decimal(18,2)?` | owned de `ValorMonetario` (quando `Multa`). |

- **Índices:**
  - PK em `Id`.
  - **Índice único** em `(TenantId, Cnpj)` (unicidade do CNPJ por tenant — I-4).
  - Índice em `(TenantId, Situacao)` para `ListarFornecedoresImpedidos`.
  - Índice em `FornecedorSancao(TenantId, FornecedorId, Tipo, DataFim)` para apurar vigência impeditiva.
- **Outbox:** tabela Outbox do contexto Administracao para `FornecedorSancionadoIntegrationEvent`.

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Fornecedor` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC, negar por padrão):**
  - Cadastro/atualização SICAF: **agente de contratação** / cadastro (ex.: `Administracao.Fornecedor.Gerir`).
  - Aplicação/reabilitação de sanção: autoridade competente + parecer jurídico (ex.: `Administracao.Fornecedor.Sancionar`) — ato sujeito ao devido processo (art. 156).
  - Consulta: **leitura de compras** (ex.: `Administracao.Fornecedor.Ler`).
  - Módulo Administracao é **ativável por tenant**; requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** quando o fornecedor é **pessoa física** (CPF de microempreendedor/profissional), aplica-se base legal explícita, minimização e **trilha de acesso**; dados de sanção são de interesse público (transparência ativa — LAI), preservada a finalidade.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Cadastrar`, `AtualizarNivelSicaf`, `AplicarSancao`, `Reabilitar`, `Inativar`) — para o Tribunal de Contas (TCE-RS), com registro do **ato sancionatório** (ator, data, fundamentação).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Receita (CNPJ) — ENTRADA (consulta):** validação de existência/situação cadastral do CNPJ na **criação** do `Fornecedor` (I-2). Cliente resiliente (Polly: retry + circuit breaker + timeout), **idempotente** por CNPJ, atrás de **Anti-Corruption Layer**; falha de disponibilidade não corrompe o agregado (CNPJ formal já é validado localmente por `Cnpj.Create`).
- **SICAF / Compras.gov.br — ENTRADA (consulta):** obtenção do **nível cadastral** e regularidade do fornecedor (art. 87), alimentando `AtualizarNivelSicaf`. Resiliente e idempotente, atrás de ACL.
- **Transparencia / cadastro de sanções — SAÍDA:** `FornecedorSancionadoIntegrationEvent` (Outbox) alimenta a transparência ativa (LAI) e o bloqueio de habilitação/contratação nos agregados `Licitacao`/`Contrato`.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de ACL.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Cadastro com CNPJ válido**
- **Dado** um CNPJ válido e ativo na Receita
- **Quando** executo `CadastrarFornecedorCommand("11.222.333/0001-81", "Fornecedora X Ltda")`
- **Então** é criado um `Fornecedor` em situação `Ativo` com `NivelCadastralSICAF = NaoCadastrado` e o evento `FornecedorCadastrado` é emitido.

**Cenário 2 — Cadastro com CNPJ inválido**
- **Dado** um CNPJ com dígitos verificadores incorretos
- **Quando** executo `CadastrarFornecedorCommand`
- **Então** a validação rejeita ("CNPJ inválido") / `ArgumentException` no VO `Cnpj` (I-1).

**Cenário 3 — CNPJ duplicado no tenant**
- **Dado** um `Fornecedor` já cadastrado com determinado CNPJ no tenant A
- **Quando** tento cadastrá-lo novamente no tenant A
- **Então** ocorre `InvalidOperationException` (violação de `(TenantId, Cnpj)`) (I-4).

**Cenário 4 — Aplicação de inidoneidade**
- **Dado** um `Fornecedor` `Ativo`
- **Quando** executo `AplicarSancaoCommand(Inidoneidade, hoje, hoje+5anos, "PA-2026-1", "...", null)`
- **Então** a situação passa a `Sancionado`, `EstaImpedido(hoje) == true`, o evento `FornecedorSancionado` é emitido e `FornecedorSancionadoIntegrationEvent` é enfileirado (I-6/I-7).

**Cenário 5 — Habilitação de fornecedor inidôneo é negada**
- **Dado** um `Fornecedor` com `Sancao` de inidoneidade vigente apresentando proposta
- **Quando** o agregado `Licitacao` avalia a `Habilitacao`
- **Então** a habilitação é negada automaticamente (consome a regra `EstaImpedido`) (I-7).

**Cenário 6 — Advertência não impede**
- **Dado** um `Fornecedor` `Ativo`
- **Quando** executo `AplicarSancaoCommand(Advertencia, ...)`
- **Então** o evento `FornecedorSancionado` é emitido, mas a situação permanece `Ativo` e `EstaImpedido == false` (I-6).

**Cenário 7 — Multa exige valor**
- **Dado** um `Fornecedor` `Ativo`
- **Quando** executo `AplicarSancaoCommand(Multa, ..., ValorMulta = null)`
- **Então** a validação rejeita por valor de multa ausente (I-9).

**Cenário 8 — Reabilitação após cumprimento**
- **Dado** um `Fornecedor` `Sancionado` cuja sanção impeditiva já expirou (`DataFim` no passado)
- **Quando** executo `ReabilitarFornecedorCommand`
- **Então** a situação volta a `Ativo` e o evento `FornecedorReabilitado` é emitido (I-11).

**Cenário 9 — Reabilitação bloqueada com sanção vigente**
- **Dado** um `Fornecedor` `Sancionado` com inidoneidade ainda vigente
- **Quando** executo `ReabilitarFornecedorCommand`
- **Então** ocorre `InvalidOperationException` (ainda impedido) (I-11).

**Cenário 10 — Vigência da sanção pela data**
- **Dado** uma `Sancao` de impedimento com `DataInicio` e `DataFim`
- **Quando** avalio `EstaImpedido(hoje)`
- **Então** retorna `true` enquanto `DataInicio <= hoje <= DataFim`, e `false` após `DataFim` (I-12).

**Cenário 11 — Consulta tenant-scoped**
- **Dado** fornecedores no tenant A e no tenant B
- **Quando** executo `ListarFornecedoresImpedidosQuery(hoje)` no contexto do tenant A
- **Então** retornam **apenas** os fornecedores do tenant A.

---

## 13. Casos de Borda

- **B-1.** CNPJ com máscara é aceito e normalizado para 14 dígitos via `Cnpj.Create` (I-1).
- **B-2.** CNPJ formalmente válido mas inexistente/baixado na Receita ⇒ rejeitado pelo handler (I-2); indisponibilidade da Receita não corrompe o agregado (ACL).
- **B-3.** Cadastro duplicado por `(TenantId, Cnpj)` ⇒ violação do índice único (I-4).
- **B-4.** `Sancao` impeditiva sem `DataFim` (nulo) ⇒ considerada **vigente indefinidamente** até reabilitação (I-12).
- **B-5.** `EstaImpedido(hoje)` com `hoje == DataFim` ⇒ `true` (inclusivo no termo final).
- **B-6.** Múltiplas sanções; basta **uma** impeditiva vigente para `EstaImpedido == true` (I-12).
- **B-7.** `Advertencia`/`Multa` não alteram a situação para `Sancionado` (I-6).
- **B-8.** `Multa` com `ValorMulta` ≤ 0 ⇒ rejeitado (I-9).
- **B-9.** `Sancao` sem `ProcessoAdministrativo`/`Fundamentacao` ⇒ rejeitado (I-10).
- **B-10.** Reabilitar fornecedor que não está `Sancionado` ⇒ `InvalidOperationException` (I-11).
- **B-11.** Aplicar sanção a fornecedor `Inativo` ⇒ registra histórico, mas não o habilita a participar (I-13).
- **B-12.** `FornecedorSancionadoIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).
- **B-13.** Sanção aplicada por papel sem competência sancionatória ⇒ 403 auditado.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo Administracao e da Lei 14.133/2021 (registro cadastral/SICAF art. 87, sanções art. 156, vedações art. 14) e validação de CNPJ na Receita. |

<!-- manifest
commands: CadastrarFornecedor, AtualizarNivelSicaf, AplicarSancao, ReabilitarFornecedor, InativarFornecedor
queries: ObterFornecedorPorId, ObterFornecedorPorCnpj, ListarFornecedoresImpedidos
domainEvents: FornecedorCadastrado, FornecedorSancionado, FornecedorReabilitado, FornecedorInativado
integrationEventsPublished: FornecedorSancionadoIntegrationEvent
integrationEventsConsumed: 
-->
