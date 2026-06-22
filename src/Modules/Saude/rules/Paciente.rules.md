---
modulo: Saude
agregado: Paciente
contexto: Saude (Atenção à Saúde municipal — SUS; PEP/e-SUS APS, regulação, farmácia e imunização)
poder: Executivo
schema: saude
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF arts. 196-200", "Lei 8.080/1990", "Lei 8.142/1990", "PNAB Portaria GM/MS 2.436/2017", "Portaria 1.412/2013 (SISAB/e-SUS APS)", "Portaria 1.434/2020 (RNDS)", "LGPD art. 11, II, f (tutela da saude)"]
---

# Paciente — Regras-as-Code (Rules-as-Code)

> Cidadão usuário do SUS identificado de forma unívoca pelo **CNS** (Cartão Nacional de Saúde),
> com registro clínico longitudinal (condições de saúde e alergias) tratado como **dado pessoal
> sensível** (LGPD art. 11). Raiz de agregado do PEP/e-SUS APS, base para Atendimento, regulação,
> dispensação e imunização. Este arquivo é **normativo e versionado**; o código (`Paciente.cs`,
> handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Paciente (`Paciente`) | Cidadão usuário do SUS, sujeito do registro clínico longitudinal. Raiz de agregado. |
| Cartão Nacional de Saúde / CNS (`Cns` : `Cns`) | Identificador nacional unívoco do usuário SUS na base CADSUS. Chave de negócio. |
| CADSUS (`ValidarCnsNoCadsus`) | Base nacional de usuários do SUS; valida e confirma o CNS (PIX/PDQ). |
| Identificação (`Identificacao` : VO) | Dados civis do paciente (nome, nome social, data de nascimento, sexo, CPF). |
| Endereço (`Endereco` : VO) | Endereço residencial do paciente (logradouro, número, bairro, município, UF, CEP). |
| Condição de Saúde (`CondicaoDeSaude`) | Problema/condição de saúde do paciente (entidade; CID-10/CIAP-2). |
| Alergia (`Alergia`) | Reação alérgica/intolerância registrada do paciente (entidade). |
| Cadastrar (`Cadastrar` / `CadastrarPaciente`) | Ato de cadastrar o paciente no PEP a partir de CNS confirmado no CADSUS. |
| Confirmação CADSUS (`CnsConfirmado` : `bool`) | Indica que o CNS foi validado/confirmado na base nacional. |
| Atualizar Cadastro (`AtualizarCadastro`) | Atualização de Identificacao/Endereco do paciente. |
| Registrar Condição (`RegistrarCondicaoDeSaude`) | Inclusão de uma `CondicaoDeSaude` no histórico do paciente. |
| Registrar Alergia (`RegistrarAlergia`) | Inclusão de uma `Alergia` no histórico do paciente. |
| Inativar (`Inativar` / `Inativado`) | Encerramento administrativo do cadastro (óbito, transferência, duplicidade). |
| Situação (`Situacao` : `SituacaoPaciente`) | Estado atual do cadastro do paciente. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Secretaria de Saúde) dono do registro. |

---

## 2. Modelo

- **Identidade:** `PacienteId` — `readonly record struct PacienteId(Guid Value)`; fábrica `PacienteId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Paciente : AggregateRoot<PacienteId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Cadastrar(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Cns` | `Cns` (VO) | Cartão Nacional de Saúde (chave de negócio). | `private set` |
| `Identificacao` | `Identificacao` (VO/owned) | Dados civis (nome, nome social, nascimento, sexo, CPF). | `private set` |
| `Endereco` | `Endereco` (VO/owned) | Endereço residencial. | `private set` |
| `CnsConfirmado` | `bool` | CNS validado no CADSUS. | `private set` |
| `Situacao` | `SituacaoPaciente` | Situação atual do cadastro. | `private set` |
| `Condicoes` | `IReadOnlyCollection<CondicaoDeSaude>` | Condições de saúde (somente leitura para fora). | coleção encapsulada |
| `Alergias` | `IReadOnlyCollection<Alergia>` | Alergias registradas (somente leitura para fora). | coleção encapsulada |

### Value Objects (referenciados)

- `Cns` — `readonly record struct` com `Valor : string`; valida formato (15 dígitos, dígito verificador do CNS). Não nulo/vazio (validado em `Cadastrar`).
- `Identificacao` — owned type: `Nome`, `NomeSocial?`, `DataNascimento : DateOnly`, `Sexo : Sexo`, `Cpf? : Cpf`.
- `Endereco` — owned type: `Logradouro`, `Numero`, `Bairro`, `Municipio`, `Uf`, `Cep`.

### Entidades do agregado

- `CondicaoDeSaude` — `Id`, `Codigo` (`CID` ou `CIAP`), `Descricao`, `DataRegistro : DateOnly`, `Ativa : bool`.
- `Alergia` — `Id`, `Substancia`, `Gravidade`, `DataRegistro : DateOnly`.

### Enum `SituacaoPaciente`

| Valor | Numérico | Descrição |
|---|---|---|
| `Ativo` | 1 | Cadastro ativo (estado inicial após confirmação CADSUS). |
| `Inativo` | 2 | Cadastro inativado (óbito, transferência, duplicidade) — terminal. |

### Enum `Sexo`

| Valor | Numérico | Descrição |
|---|---|---|
| `Feminino` | 1 | Sexo feminino. |
| `Masculino` | 2 | Sexo masculino. |
| `Ignorado` | 9 | Não informado/ignorado. |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrado** = { `Inativo` }.
> - **Ativo** = { `Ativo` } — única situação que admite registro clínico e atualização cadastral.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** O CNS é obrigatório no cadastro (`ArgumentException.ThrowIfNullOrWhiteSpace(cns)`) e deve ser um `Cns` válido em formato (15 dígitos + DV). (Linguagem ubíqua / CADSUS)
- **I-2.** O cadastro só se conclui com **CNS confirmado no CADSUS** (`CnsConfirmado == true`); sem confirmação, o paciente **não** pode ser referenciado por `Atendimento` (regra crítica: "Atendimento exige CNS válido"). (Cenário 1)
- **I-3.** `Identificacao` é obrigatória na criação (`ArgumentNullException.ThrowIfNull(identificacao)`), incluindo `Nome` não vazio e `DataNascimento` não futura.
- **I-4.** Na criação, a situação inicial é `Ativo` e é emitido o evento `PacienteCadastrado(id, cns)`.
- **I-5.** O CNS é **imutável** após o cadastro; correção exige fluxo administrativo dedicado (não há setter público de `Cns`).
- **I-6.** Paciente **inativado** (`Inativo`) não admite atualização cadastral, registro de condição/alergia nem novos atendimentos (`GarantirAtivo` lança `InvalidOperationException`).
- **I-7.** `RegistrarCondicaoDeSaude` exige `codigo` (CID-10 ou CIAP-2) não vazio e `DataRegistro` não futura; adiciona à coleção `Condicoes`.
- **I-8.** `RegistrarAlergia` exige `substancia` não vazia e `DataRegistro` não futura; adiciona à coleção `Alergias`.
- **I-9.** A unicidade do paciente é por `(TenantId, Cns)` — não pode haver dois cadastros ativos com o mesmo CNS no mesmo tenant (índice único; valida no handler antes de cadastrar).
- **I-10.** `CnsConfirmado` só transiciona `false → true` via `ConfirmarCadastro()` (resultado da validação CADSUS); nunca regride.
- **I-11.** Estado terminal (`Inativo`) não admite novas transições de negócio.
- **I-12.** Dado clínico é **sensível** (LGPD art. 11): coleções `Condicoes`/`Alergias` só são lidas sob trilha de acesso e claim `saude.atender`/`saude.ler` (ver §10).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Cadastrar` | `Ativo` | `cns` válido && `identificacao != null` | `PacienteCadastrado` |
| `Ativo` | `ConfirmarCadastro` | `Ativo` | resultado positivo do CADSUS | `CadastroConfirmadoNoCadsus` |
| `Ativo` | `AtualizarCadastro` | `Ativo` | situação == `Ativo` | `CadastroAtualizado` |
| `Ativo` | `RegistrarCondicaoDeSaude` | `Ativo` | situação == `Ativo`; codigo não vazio | `CondicaoDeSaudeRegistrada` |
| `Ativo` | `RegistrarAlergia` | `Ativo` | situação == `Ativo`; substancia não vazia | `AlergiaRegistrada` |
| `Ativo` | `Inativar` | `Inativo` | situação == `Ativo` | `PacienteInativado` |

> Observações:
> - `AtualizarCadastro`, `RegistrarCondicaoDeSaude`, `RegistrarAlergia` chamam `GarantirAtivo()` antes do efeito.
> - Não há transição de retorno de `Inativo` para `Ativo` nesta versão (reativação exige fluxo administrativo dedicado).
> - `ConfirmarCadastro` não altera a `Situacao`, apenas o flag `CnsConfirmado` (de `false` para `true`).

---

## 5. Comandos (escrita)

### 5.1 CadastrarPaciente

- **Command:** `CadastrarPacienteCommand(string Cns, IdentificacaoDto Identificacao, EnderecoDto Endereco) : ICommand<Guid>`.
- **Entrada (DTO):** `Cns`, `Identificacao` (nome, nome social, data nascimento, sexo, CPF), `Endereco`.
- **Dependências do handler:** `IPacienteRepository`, `ICadsusGateway`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Não existir paciente ativo com mesmo `(TenantId, Cns)` (`IPacienteRepository.ExistePorCnsAsync`), senão `InvalidOperationException("Paciente já cadastrado para este CNS.")` (I-9).
  - Validação CADSUS: `ICadsusGateway.ValidarCnsAsync(cns)` retorna confirmação positiva (senão paciente nasce sem confirmação ou cadastro é rejeitado conforme política — ver I-2).
- **Efeito:** cria via `Paciente.Cadastrar(tenant.TenantId, new Cns(request.Cns), identificacao, endereco)`; se CADSUS positivo, `paciente.ConfirmarCadastro()`; `pacientes.Adicionar(paciente)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Paciente` em situação `Ativo`; `CnsConfirmado` conforme retorno do CADSUS; retorna `paciente.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request/identificacao); `ArgumentException` (cns vazio/inválido); `InvalidOperationException` (CNS duplicado).
- **Evento de domínio:** `PacienteCadastrado(id, cns)` (emitido no construtor via factory); `CadastroConfirmadoNoCadsus(id, cns)` se confirmado.

### 5.2 ConfirmarCadastroNoCadsus

- **Command:** `ConfirmarCadastroNoCadsusCommand(Guid PacienteId) : ICommand`.
- **Entrada (DTO):** `PacienteId`.
- **Dependências do handler:** `IPacienteRepository`, `ICadsusGateway`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe (`ObterPorIdAsync`), senão `InvalidOperationException("Paciente não encontrado.")`.
  - CADSUS confirma o CNS (`ValidarCnsAsync`).
- **Efeito:** `paciente.ConfirmarCadastro()`; `SaveChangesAsync`.
- **Pós-condições:** `CnsConfirmado == true`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou CADSUS negativo).
- **Evento de domínio:** `CadastroConfirmadoNoCadsus(Id, Cns)`.

### 5.3 AtualizarCadastroPaciente

- **Command:** `AtualizarCadastroPacienteCommand(Guid PacienteId, IdentificacaoDto Identificacao, EnderecoDto Endereco) : ICommand`.
- **Entrada (DTO):** `PacienteId`, `Identificacao`, `Endereco`.
- **Dependências do handler:** `IPacienteRepository`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe, senão `InvalidOperationException("Paciente não encontrado.")`.
  - Situação == `Ativo` (I-6).
- **Efeito:** `paciente.AtualizarCadastro(identificacao, endereco)`; `SaveChangesAsync`.
- **Pós-condições:** `Identificacao`/`Endereco` atualizados.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou inativo).
- **Evento de domínio:** `CadastroAtualizado(Id)`.

### 5.4 RegistrarCondicaoDeSaude

- **Command:** `RegistrarCondicaoDeSaudeCommand(Guid PacienteId, string Codigo, string Descricao) : ICommand`.
- **Entrada (DTO):** `PacienteId`, `Codigo` (CID-10/CIAP-2), `Descricao`.
- **Dependências do handler:** `IPacienteRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe, senão `InvalidOperationException("Paciente não encontrado.")`.
  - Situação == `Ativo` (I-6); `codigo` não vazio (I-7).
- **Efeito:** `paciente.RegistrarCondicaoDeSaude(request.Codigo, request.Descricao, hoje)`; `SaveChangesAsync`.
- **Pós-condições:** nova `CondicaoDeSaude` na coleção `Condicoes`.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (codigo vazio); `InvalidOperationException` (não encontrado ou inativo).
- **Evento de domínio:** `CondicaoDeSaudeRegistrada(Id, codigo)`.

### 5.5 RegistrarAlergia

- **Command:** `RegistrarAlergiaCommand(Guid PacienteId, string Substancia, string Gravidade) : ICommand`.
- **Entrada (DTO):** `PacienteId`, `Substancia`, `Gravidade`.
- **Dependências do handler:** `IPacienteRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe, senão `InvalidOperationException("Paciente não encontrado.")`.
  - Situação == `Ativo` (I-6); `substancia` não vazia (I-8).
- **Efeito:** `paciente.RegistrarAlergia(request.Substancia, request.Gravidade, hoje)`; `SaveChangesAsync`.
- **Pós-condições:** nova `Alergia` na coleção `Alergias`.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (substancia vazia); `InvalidOperationException` (não encontrado ou inativo).
- **Evento de domínio:** `AlergiaRegistrada(Id, substancia)`.

### 5.6 InativarPaciente

- **Command:** `InativarPacienteCommand(Guid PacienteId, string Motivo) : ICommand`.
- **Entrada (DTO):** `PacienteId`, `Motivo` (óbito/transferência/duplicidade).
- **Dependências do handler:** `IPacienteRepository`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo.
  - Paciente existe, senão `InvalidOperationException("Paciente não encontrado.")`.
  - Situação == `Ativo` (I-11).
- **Efeito:** `paciente.Inativar(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Inativo`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou já inativo).
- **Evento de domínio:** `PacienteInativado(Id, motivo)`.

---

## 6. Consultas (leitura)

### 6.1 ObterPacientePorCns

- **Query:** `ObterPacientePorCnsQuery(string Cns) : IQuery<PacienteResumo?>`.
- **Entrada:** `Cns`.
- **Handler:** `ObterPacientePorCnsHandler(IPacienteRepository pacientes)`; chama `pacientes.ObterPorCnsAsync(new Cns(request.Cns), ct)`.
- **Projeção (DTO):** `PacienteResumo(Guid Id, string Cns, string Nome, string? NomeSocial, DateOnly DataNascimento, string Sexo, bool CnsConfirmado, string Situacao)`.
- **Filtros:** por `Cns`; **sempre tenant-scoped** via Global Query Filter por `TenantId` no DbContext.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).
- **Segurança:** requer claim de leitura (`saude.ler`); acesso registra **trilha de acesso ao prontuário** (quem leu, quando, qual CNS).

### 6.2 ObterHistoricoClinicoDoPaciente

- **Query:** `ObterHistoricoClinicoDoPacienteQuery(Guid PacienteId) : IQuery<HistoricoClinicoDto>`.
- **Entrada:** `PacienteId`.
- **Handler:** `ObterHistoricoClinicoDoPacienteHandler(IPacienteRepository pacientes)`; carrega `Condicoes` e `Alergias`.
- **Projeção (DTO):** `HistoricoClinicoDto(Guid PacienteId, IReadOnlyList<CondicaoDto> Condicoes, IReadOnlyList<AlergiaDto> Alergias)`.
- **Filtros:** por `PacienteId`; **sempre tenant-scoped**.
- **Pré-condições:** `request` não nulo.
- **Segurança:** **dado sensível** (LGPD art. 11); claim `saude.atender`/`saude.ler`; **trilha de acesso** obrigatória (quem leu, quando, qual CNS).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Saude.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `PacienteCadastrado` | `(PacienteId, Cns)` | `Paciente.Cadastrar` (construtor) |
| `CadastroConfirmadoNoCadsus` | `(PacienteId, Cns)` | `Paciente.ConfirmarCadastro` |
| `CadastroAtualizado` | `(PacienteId)` | `Paciente.AtualizarCadastro` |
| `CondicaoDeSaudeRegistrada` | `(PacienteId, string codigo)` | `Paciente.RegistrarCondicaoDeSaude` |
| `AlergiaRegistrada` | `(PacienteId, string substancia)` | `Paciente.RegistrarAlergia` |
| `PacienteInativado` | `(PacienteId, string motivo)` | `Paciente.Inativar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Saude.Contracts`)

- Nenhum nesta versão. O `Paciente` é referenciado por `Atendimento` por Id (sem navegação cross-aggregate); a publicação analítica/anonimizada para **Transparencia** ocorre a partir de `Atendimento`/`Imunizacao`, não do cadastro do paciente.

### Integração (consome)

- Nenhum diretamente no agregado `Paciente`. A validação do CNS no **CADSUS** é feita por gateway síncrono (ACL), não por Integration Event.

---

## 8. Validações (FluentValidation)

### CadastrarPacienteValidator (`AbstractValidator<CadastrarPacienteCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Cns` | `NotEmpty()` + `Length(15)` + `Must(SerCnsValido)` | "CNS é obrigatório e deve ser válido (15 dígitos)." |
| `Identificacao.Nome` | `NotEmpty()` + `MaximumLength(120)` | "Nome do paciente é obrigatório (máx. 120)." |
| `Identificacao.DataNascimento` | `LessThanOrEqualTo(hoje)` | "Data de nascimento não pode ser futura." |
| `Identificacao.Cpf` | `Must(SerCpfValido).When(cpf != null)` | "CPF inválido." |
| `Endereco.Uf` | `Length(2).When(endereco != null)` | "UF deve ter 2 caracteres." |

### RegistrarCondicaoDeSaudeValidator (`AbstractValidator<RegistrarCondicaoDeSaudeCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `PacienteId` | `NotEmpty()` | "Paciente é obrigatório." |
| `Codigo` | `NotEmpty()` + `MaximumLength(10)` | "Código CID-10/CIAP-2 é obrigatório (máx. 10)." |

### RegistrarAlergiaValidator (`AbstractValidator<RegistrarAlergiaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `PacienteId` | `NotEmpty()` | "Paciente é obrigatório." |
| `Substancia` | `NotEmpty()` + `MaximumLength(120)` | "Substância da alergia é obrigatória (máx. 120)." |

> `ConfirmarCadastroNoCadsusCommand`, `AtualizarCadastroPacienteCommand` e `InativarPacienteCommand` validam ao menos `PacienteId` `NotEmpty`; demais proteções por invariantes de domínio e checagem de existência no handler. Queries não possuem validador além de `ArgumentNullException`.

---

## 9. Persistência (EF Core 8)

- **Schema:** `saude` (isolado por módulo). **DbContext:** o do módulo Saude. **Migrations:** por módulo.
- **Tabela:** `Paciente` (raiz de agregado) + tabelas filhas `CondicaoDeSaude`, `Alergia` (owned/entidades do agregado).

| Coluna (Paciente) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `PacienteId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Cns` | `nvarchar(15)` | conversor de `Cns` ↔ `string`. |
| `Identificacao_*` | (owned) | owned type `Identificacao` (Nome, NomeSocial, DataNascimento, Sexo, Cpf). |
| `Endereco_*` | (owned) | owned type `Endereco` (Logradouro, Numero, Bairro, Municipio, Uf, Cep). |
| `CnsConfirmado` | `bit` | `bool`. |
| `Situacao` | `int` | enum `SituacaoPaciente` (persistido por valor numérico). |

- **Tabelas filhas:** `CondicaoDeSaude(Id, PacienteId FK, Codigo, Descricao, DataRegistro, Ativa)`; `Alergia(Id, PacienteId FK, Substancia, Gravidade, DataRegistro)`.
- **Índices:**
  - PK em `Id`.
  - **Índice único** em `(TenantId, Cns)` (unicidade do paciente por tenant — I-9).
  - FKs `CondicaoDeSaude.PacienteId` e `Alergia.PacienteId` com índice por `PacienteId`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio); owned types para `Identificacao`/`Endereco`.
- **Outbox:** tabela Outbox do contexto Saude (não usada nesta versão pelo agregado Paciente — sem Integration Event publicado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Paciente` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. `TenantId` **nunca aceito do cliente**. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Cadastro/atualização/inativação do paciente: papel de cadastro da APS (claim `saude.atender` ou `saude.cadastrar`).
  - Leitura de histórico clínico (condições/alergias): claim `saude.atender`/`saude.ler`.
  - Módulo Saude é **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** dado pessoal **sensível** (art. 11). Base legal: **tutela da saúde** (art. 11, II, "f") — sem necessidade de consentimento, com **minimização**. As coleções `Condicoes`/`Alergias` e a leitura do `Cns` geram **trilha de acesso ao prontuário** (quem leu, quando, qual CNS). Projeções (`PacienteResumo`) aplicam minimização.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Cadastrar`, `ConfirmarCadastro`, `AtualizarCadastro`, `RegistrarCondicaoDeSaude`, `RegistrarAlergia`, `Inativar`) — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **CADSUS (validação de CNS — síncrona, ACL):** `ICadsusGateway.ValidarCnsAsync` consulta a base nacional de usuários (PIX/PDQ) para validar/confirmar o CNS. Idempotente, com **timeout, retry e circuit breaker (Polly)** e mapeamento explícito de erros atrás de **Anti-Corruption Layer**. Falha de disponibilidade não confirma o cadastro (mantém `CnsConfirmado == false`).
- **e-SUS APS / SISAB (produção — passiva/saída):** o cadastro do cidadão (PEC) integra a base que, via `Atendimento`, alimenta o envio de produção ao SISAB na competência (Portaria 1.412/2013). O agregado Paciente não envia produção diretamente.
- **RNDS (interoperabilidade):** o paciente é identificado por CNS nos Bundles **HL7 FHIR R4** gerados a partir de `Atendimento` (mTLS + ICP-Brasil). O agregado Paciente fornece a identificação; não publica RES por si.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); ACL para o CADSUS.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Cadastro com CNS confirmado no CADSUS**
- **Dado** um cidadão com CNS válido confirmado no CADSUS
- **Quando** executo `CadastrarPacienteCommand(cns, identificacao, endereco)`
- **Então** é criado um `Paciente` em situação `Ativo` com `CnsConfirmado = true` e o evento `PacienteCadastrado` é emitido.

**Cenário 2 — Atendimento exige CNS válido (paciente sem confirmação)**
- **Dado** um `Paciente` sem CNS confirmado no CADSUS (`CnsConfirmado = false`)
- **Quando** um `Atendimento` tenta referenciar esse paciente
- **Então** a operação de atendimento é rejeitada e nenhum evento é publicado (I-2).

**Cenário 3 — CNS duplicado no mesmo tenant**
- **Dado** um `Paciente` já cadastrado com determinado CNS no tenant A
- **Quando** executo `CadastrarPacienteCommand` com o mesmo CNS no tenant A
- **Então** ocorre `InvalidOperationException("Paciente já cadastrado para este CNS.")` (I-9).

**Cenário 4 — Mesmo CNS em tenants distintos**
- **Dado** o mesmo CNS em tenant A e tenant B (entes distintos)
- **Quando** cadastro o paciente em cada tenant
- **Então** ambos os cadastros são válidos (unicidade é por `(TenantId, Cns)`).

**Cenário 5 — Registro de condição de saúde**
- **Dado** um `Paciente` `Ativo`
- **Quando** executo `RegistrarCondicaoDeSaudeCommand(pacienteId, "E11", "Diabetes tipo 2")`
- **Então** a coleção `Condicoes` passa a conter o registro e o evento `CondicaoDeSaudeRegistrada` é emitido.

**Cenário 6 — Registro de alergia**
- **Dado** um `Paciente` `Ativo`
- **Quando** executo `RegistrarAlergiaCommand(pacienteId, "Penicilina", "Grave")`
- **Então** a coleção `Alergias` passa a conter o registro e o evento `AlergiaRegistrada` é emitido.

**Cenário 7 — Atualização cadastral de paciente ativo**
- **Dado** um `Paciente` `Ativo`
- **Quando** executo `AtualizarCadastroPacienteCommand` com novo endereço
- **Então** `Endereco` é atualizado e o evento `CadastroAtualizado` é emitido.

**Cenário 8 — Operações sobre paciente inativado**
- **Dado** um `Paciente` `Inativo`
- **Quando** executo `AtualizarCadastroPacienteCommand` / `RegistrarCondicaoDeSaudeCommand` / `RegistrarAlergiaCommand`
- **Então** ocorre `InvalidOperationException` (paciente inativo não admite alterações — I-6).

**Cenário 9 — Inativação de paciente**
- **Dado** um `Paciente` `Ativo`
- **Quando** executo `InativarPacienteCommand(pacienteId, "Óbito")`
- **Então** situação = `Inativo` e o evento `PacienteInativado` é emitido.

**Cenário 10 — Consulta tenant-scoped por CNS**
- **Dado** pacientes no tenant A e no tenant B com CNS distintos
- **Quando** executo `ObterPacientePorCnsQuery(cns)` no contexto do tenant A
- **Então** retorna apenas o paciente do tenant A (ou nulo), projetado em `PacienteResumo`.

**Cenário 11 — Trilha de acesso ao histórico clínico**
- **Dado** um `Paciente` `Ativo` com condições e alergias
- **Quando** um profissional com claim `saude.atender` executa `ObterHistoricoClinicoDoPacienteQuery(pacienteId)`
- **Então** o histórico é retornado e uma **trilha de acesso ao prontuário** (quem, quando, qual CNS) é registrada.

---

## 13. Casos de Borda

- **B-1.** `cns` nulo/vazio/espaços em `Cadastrar` ⇒ `ArgumentException` (I-1).
- **B-2.** `cns` com formato inválido (≠ 15 dígitos / DV inválido) ⇒ rejeitado pelo VO `Cns` e pelo validator.
- **B-3.** `identificacao` nula em `Cadastrar` ⇒ `ArgumentNullException` (I-3).
- **B-4.** `DataNascimento` futura ⇒ rejeitada pelo validator.
- **B-5.** CADSUS indisponível no cadastro ⇒ paciente nasce `Ativo` com `CnsConfirmado = false`; não pode ser atendido até confirmação (I-2).
- **B-6.** Confirmar cadastro já confirmado (`CnsConfirmado == true`) ⇒ idempotente (permanece `true`, sem erro — I-10).
- **B-7.** `codigo` vazio em `RegistrarCondicaoDeSaude` ⇒ `ArgumentException` (I-7).
- **B-8.** `substancia` vazia em `RegistrarAlergia` ⇒ `ArgumentException` (I-8).
- **B-9.** Inativar paciente já `Inativo` ⇒ `InvalidOperationException` (I-11).
- **B-10.** Atualizar/registrar em paciente `Inativo` ⇒ `InvalidOperationException` (`GarantirAtivo`).
- **B-11.** Tentativa de gravar `Paciente` com `TenantId` do cliente ⇒ ignorado/lançado; `TenantId` vem do interceptor (nunca do cliente).
- **B-12.** Leitura de histórico clínico sem claim `saude.atender`/`saude.ler` ⇒ negado por padrão (403 auditado).
- **B-13.** `DataRegistro` (condição/alergia) futura ⇒ rejeitada (não futura).
- **B-14.** Duas condições com o mesmo `Codigo` ⇒ permitido (histórico admite reincidências datadas); diferenciadas por `DataRegistro`/`Id`.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo Saude (mapa de domínio: Paciente raiz, chave CNS, entidades CondicaoDeSaude/Alergia, VOs Identificacao/Endereco; regras críticas de CNS/CADSUS e LGPD sensível). |

<!-- manifest
commands: CadastrarPaciente, ConfirmarCadastroNoCadsus, AtualizarCadastroPaciente, RegistrarCondicaoDeSaude, RegistrarAlergia, InativarPaciente
queries: ObterPacientePorCns, ObterHistoricoClinicoDoPaciente
domainEvents: PacienteCadastrado, CadastroConfirmadoNoCadsus, CadastroAtualizado, CondicaoDeSaudeRegistrada, AlergiaRegistrada, PacienteInativado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
