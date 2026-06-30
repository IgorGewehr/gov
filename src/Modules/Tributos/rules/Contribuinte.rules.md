---
modulo: Tributos
agregado: Contribuinte
contexto: Tributos (cadastro de sujeitos passivos da obrigacao tributaria municipal)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CTN (Lei 5.172/66) art. 121 (sujeito passivo)", "CTN (Lei 5.172/66) art. 122 (responsavel)", "LC 116/2003 (ISS)"]
---

> **Documento normativo.** Esta especificacao e a fonte da verdade do agregado
> `Contribuinte` (modulo Tributos). O codigo de Dominio, Application, Infrastructure e
> testes e gerado e mantido a partir deste arquivo. Engenheiros alteram **apenas** este
> `.rules.md`; a IA (re)gera o codigo e se autoaudita contra estas regras.
>
> **Escopo desta versao (1.0.0):** o codigo existente cobre **somente** o cadastro de
> contribuinte **pessoa fisica** como caso de uso de escrita exposto. O agregado tambem
> oferece a fabrica `PessoaJuridica` no Dominio, porem **sem** comando/handler de
> Application correspondente nesta versao. As secoes abaixo refletem fielmente o que esta
> implementado e marcam explicitamente o que ainda nao foi exposto.

---

## 1. Linguagem Ubigua

Identificadores **sem acento** e **vinculantes** (devem aparecer literalmente no codigo).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Contribuinte (`Contribuinte`) | Sujeito passivo da obrigacao tributaria municipal — pessoa fisica ou juridica sujeita a tributos (CTN art. 121). Raiz de agregado. |
| ContribuinteId (`ContribuinteId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| Tenant (`TenantId`) | Ente publico (prefeitura) dono do registro. Toda operacao e tenant-scoped. |
| Tipo de pessoa (`TipoPessoa`) | Natureza juridica do contribuinte: `Fisica` ou `Juridica`. |
| Pessoa fisica (`Fisica`) | Contribuinte identificado por CPF. |
| Pessoa juridica (`Juridica`) | Contribuinte identificado por CNPJ. |
| Documento (`Documento`) | CPF ou CNPJ do contribuinte, armazenado **sem mascara** (somente digitos). |
| CPF (`Cpf`) | Value Object do SharedKernel; 11 digitos validados (digitos verificadores conferidos). |
| CNPJ (`Cnpj`) | Value Object do SharedKernel; identificador de pessoa juridica. |
| Nome / Razao social (`Nome`) | Nome completo (PF) ou razao social (PJ). |
| Inscricao municipal (`InscricaoMunicipal`) | Inscricao mobiliaria do contribuinte no ente, **opcional**. |
| Cadastrar contribuinte PF (`CadastrarContribuintePessoaFisica`) | Comando que cadastra um contribuinte pessoa fisica. |
| Contribuinte cadastrado (`ContribuinteCadastrado`) | Evento de dominio emitido na criacao do agregado. |

---

## 2. Modelo

- **Identidade:** `ContribuinteId` — `public readonly record struct ContribuinteId(Guid Value)`.
  - `ContribuinteId.New()` gera novo `Guid`.
  - `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Contribuinte : AggregateRoot<ContribuinteId>, IMustHaveTenant` — `sealed`.
- **Construcao:** construtor privado sem parametros (EF) + construtor privado completo;
  **fabricas estaticas** `PessoaFisica(...)` e `PessoaJuridica(...)`. O agregado **nasce valido**.

### Propriedades

| Propriedade | Tipo | Acesso | Descricao |
|---|---|---|---|
| `Id` | `ContribuinteId` | herdado | Identidade do agregado. |
| `TenantId` | `Guid` | `get; private set;` | Tenant (ente publico) dono do registro. |
| `TipoPessoa` | `TipoPessoa` (enum) | `get; private set;` | Natureza juridica. |
| `Documento` | `string` | `get; private set;` | CPF ou CNPJ **sem mascara** (somente digitos). |
| `Nome` | `string` | `get; private set;` | Nome (PF) ou razao social (PJ). |
| `InscricaoMunicipal` | `string?` | `get; private set;` | Inscricao municipal, **opcional** (pode ser nula). |

### Enums

```csharp
public enum TipoPessoa
{
    Fisica = 1,   // pessoa fisica (CPF)
    Juridica = 2, // pessoa juridica (CNPJ)
}
```

### Value Objects (SharedKernel, reutilizados)

- `Cpf` — `sealed class : ValueObject`. Propriedade `Digitos` (11 digitos sem mascara).
  - `Cpf.Create(string)` lanca `ArgumentException` se invalido.
  - `Cpf.TryCreate(string?, out Cpf?)` retorna `bool` sem lancar.
  - `Cpf.IsValid(string)`, `Formatar()` (mascara `000.000.000-00`), igualdade por `Digitos`.
- `Cnpj` — `sealed class : ValueObject`. Propriedade `Digitos`. Usado pela fabrica `PessoaJuridica`
  (sem comando exposto nesta versao).

### Fabricas (Dominio)

```csharp
public static Contribuinte PessoaFisica(Guid tenantId, Cpf cpf, string nome, string? inscricaoMunicipal = null);
public static Contribuinte PessoaJuridica(Guid tenantId, Cnpj cnpj, string razaoSocial, string? inscricaoMunicipal = null);
```

---

## 3. Invariantes

Lista **numerada**. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Documento` e armazenado **somente com digitos** (sem mascara). Para PF e gravado
  `cpf.Digitos`; para PJ, `cnpj.Digitos`. (Tributos: cadastro normalizado.)
- **I-2.** Para `TipoPessoa = Fisica`, o `Documento` provem de um `Cpf` **valido** (11 digitos,
  digitos verificadores conferidos por `Cpf.IsValid`). CPF invalido **impede** a criacao do agregado.
- **I-3.** Para `TipoPessoa = Juridica`, o `Documento` provem de um `Cnpj` **valido**
  (validado pelo Value Object `Cnpj`).
- **I-4.** `Nome` (ou razao social) e **obrigatorio** e nao pode ser nulo, vazio ou somente
  espacos — `ArgumentException.ThrowIfNullOrWhiteSpace(nome)` na fabrica.
- **I-5.** O Value Object de documento (`Cpf`/`Cnpj`) passado a fabrica nao pode ser nulo —
  `ArgumentNullException.ThrowIfNull(...)`.
- **I-6.** `TenantId` e atribuido na construcao e e **imutavel** apos a criacao (sem setter publico).
  Todo `Contribuinte` pertence a exatamente um tenant (`IMustHaveTenant`).
- **I-7.** `InscricaoMunicipal` e **opcional** (`string?`); ausencia e representada por `null`.
- **I-8.** A criacao de um `Contribuinte` **sempre** emite o evento de dominio
  `ContribuinteCadastrado(Id, TenantId)` (exatamente um por criacao).
- **I-9.** O agregado **nasce valido**: nao existe caminho publico de construcao que produza um
  `Contribuinte` com documento invalido ou nome em branco (construtores privados + fabricas).

---

## 4. Maquina de Estados

O agregado `Contribuinte`, nesta versao, **nao possui estado de ciclo de vida** (nao ha enum de
situacao nem transicoes). Existe apenas o evento de **criacao**.

| Origem | Comando | Destino | Guarda | Evento emitido |
|---|---|---|---|---|
| (inexistente) | `CadastrarContribuintePessoaFisica` | `Contribuinte` (cadastrado) | CPF valido + nome preenchido + tenant resolvido | `ContribuinteCadastrado` |

> Transicoes de estado (ex.: inativacao, atualizacao cadastral) **nao** fazem parte desta
> versao e serao introduzidas em revisoes futuras deste `.rules.md`.

---

## 5. Comandos (escrita)

### 5.1. CadastrarContribuintePessoaFisica

- **Tipo:** `CadastrarContribuintePessoaFisicaCommand : ICommand<Guid>`.
- **Entrada (DTO):**
  - `Cpf : string` — CPF com ou sem mascara.
  - `Nome : string` — nome completo.
  - `InscricaoMunicipal : string?` — inscricao municipal opcional.
- **Pre-condicoes:**
  - Tenant resolvido no `ITenantContext` (JWT no ApiHost).
  - `request` nao nulo (`ArgumentNullException.ThrowIfNull(request)`).
  - `Cpf` valido (`Cpf.TryCreate`/`Cpf.Create`).
  - `Nome` nao vazio e com no maximo 200 caracteres.
- **Efeito:**
  - Cria `Cpf` via `Cpf.Create(request.Cpf)`.
  - Cria o agregado `Contribuinte.PessoaFisica(tenant.TenantId, cpf, request.Nome, request.InscricaoMunicipal)`.
  - `contribuintes.Adicionar(contribuinte)` (repositorio).
  - `await unitOfWork.SaveChangesAsync(cancellationToken)` (persiste + despacha eventos via Outbox/pipeline).
- **Pos-condicoes:**
  - Existe um `Contribuinte` persistido com `TipoPessoa = Fisica`, `Documento = cpf.Digitos`,
    `Nome = request.Nome`, `InscricaoMunicipal = request.InscricaoMunicipal`, `TenantId = tenant.TenantId`.
  - Evento de dominio `ContribuinteCadastrado(Id, TenantId)` enfileirado.
  - **Retorno:** `Guid` = `contribuinte.Id.Value`.
- **Excecoes:**
  - `ValidationException` (pipeline FluentValidation) se CPF/nome invalidos.
  - `ArgumentException` se `Cpf.Create` receber CPF invalido (defesa de dominio).
  - `ArgumentNullException` se `request` for nulo.
  - `InvalidOperationException` se nao houver tenant resolvido (`ITenantContext.TenantId`).
- **Evento:** `ContribuinteCadastrado`.

> **Nao exposto nesta versao:** cadastro de contribuinte **pessoa juridica**. A fabrica de
> dominio `Contribuinte.PessoaJuridica(...)` existe, mas **nao ha** `Command`/`Handler`/`Validator`
> correspondente. Quando introduzido, devera constar nesta secao e no manifesto.

---

## 6. Consultas (leitura)

**Nenhuma consulta** (`*Query`/`*Handler`) esta implementada para este agregado nesta versao.

> Quando introduzidas, toda consulta sera **tenant-scoped** (filtrada por `TenantId` via Global
> Query Filter) e projetara DTOs dedicados (ex.: `ObterContribuintePorId`, `ListarContribuintes`).
> O manifesto `queries:` permanece **vazio** ate la.

---

## 7. Eventos

### 7.1. Eventos de Dominio (in-process, MediatR)

- `ContribuinteCadastrado(ContribuinteId ContribuinteId, Guid TenantId) : IDomainEvent`
  - Emitido no construtor do agregado (`RaiseDomainEvent`).
  - Payload: identidade do contribuinte + tenant.

### 7.2. Eventos de Integracao (`*.Contracts`, via Outbox)

- **Publicados:** nenhum nesta versao.
- **Consumidos:** nenhum nesta versao.

> O agregado `Contribuinte` ainda **nao** publica nem consome Integration Events. Comunicacao
> cross-module futura ocorrera **exclusivamente** via `Tensorroot.Gov.Modules.Tributos.Contracts`.

---

## 8. Validacoes (FluentValidation)

`CadastrarContribuintePessoaFisicaValidator : AbstractValidator<CadastrarContribuintePessoaFisicaCommand>`.

| Campo | Regra | Mensagem |
|---|---|---|
| `Cpf` | `NotEmpty()` | (mensagem padrao FluentValidation) |
| `Cpf` | `Must(cpf => Cpf.TryCreate(cpf, out _))` | `"CPF invalido."` |
| `Nome` | `NotEmpty()` | (mensagem padrao FluentValidation) |
| `Nome` | `MaximumLength(200)` | (mensagem padrao FluentValidation) |

> `InscricaoMunicipal` **nao** possui regra de validacao (opcional).

---

## 9. Persistencia

`ContribuinteConfiguration : IEntityTypeConfiguration<Contribuinte>` (EF Core 8, Fluent API).

- **Schema:** `tributos` (aplicado no `DbContext` do modulo).
- **Tabela:** `Contribuintes`.
- **Chave primaria:** `Id`.

| Coluna | Tipo/Config | Observacao |
|---|---|---|
| `Id` | conversor `ContribuinteId <-> Guid`; `ValueGeneratedNever()` | identidade gerada no dominio |
| `TipoPessoa` | `HasConversion<string>()`, `HasMaxLength(20)` | enum persistido como string |
| `Documento` | `HasMaxLength(14)` | CPF (11) ou CNPJ (14), sem mascara |
| `Nome` | `HasMaxLength(200)` | nome ou razao social |
| `InscricaoMunicipal` | `HasMaxLength(30)`, anulavel | opcional |

- **Indices:**
  - `HasIndex(c => new { c.TenantId, c.Documento })` — indice **composto (nao unico)** por tenant + documento.

- **Conversores de Value Object/Id:**
  - `ContribuinteId` <-> `Guid` (lambda no `Property(Id)`).
  - `TipoPessoa` <-> `string`.
  - Observacao: `Documento` e persistido como `string` simples (extraido de `Cpf.Digitos`/`Cnpj.Digitos`
    na fabrica) — **nao** ha conversor de VO para a coluna `Documento`.

> **Nota de fidelidade:** o indice `(TenantId, Documento)` **nao** e unico no codigo atual; portanto
> a unicidade de documento por tenant **nao** e garantida pela camada de persistencia nesta versao.

---

## 10. Seguranca, Tenant e Auditoria

- **Multi-tenant:** `Contribuinte` implementa `IMustHaveTenant`. `TenantId` e carimbado a partir de
  `ITenantContext.TenantId` (resolvido do JWT no ApiHost; iteracao explicita nos Workers).
  Global Query Filter por `TenantId` aplicado no `DbContext` do modulo; gravacao cross-tenant **lanca excecao**.
- **RBAC:** acesso ao comando de cadastro sujeito a policy do modulo Tributos (negar por padrao).
  Requisicao a modulo nao-licenciado para o tenant => **404/403 auditado**.
- **Auditoria:** a criacao do agregado gera trilha imutavel via `AuditSaveChangesInterceptor`
  (antes/depois, usuario, IP, timestamp), para o Tribunal de Contas.
- **LGPD:** `Documento` (CPF) e `Nome` sao dados pessoais. Tratamento com base legal tributaria
  (CTN art. 121/122; LC 116/2003), minimizacao e trilha de acesso. CPF armazenado **sem mascara**,
  apenas digitos; exibicao mascarada (`Cpf.Formatar()`) quando aplicavel na UI.
- **Anti-SQLi:** acesso exclusivamente via EF Core parametrizado; proibido SQL concatenado com entrada do usuario.

---

## 11. Integracoes Governamentais

**Nenhuma integracao governamental direta** neste agregado nesta versao.

> Contexto do modulo: o Worker `NfseSync` ingere NFS-e do Ambiente de Dados Nacional (ADN) de forma
> **passiva** e pode, no futuro, **referenciar** contribuintes por documento (CPF/CNPJ) ao alimentar
> o painel fiscal e a Divida Ativa. Qualquer acoplamento sera idempotente, resiliente (Polly) e
> atras de **Anti-Corruption Layer**, via `*.Contracts`. **Nao** ha tal fluxo implementado para
> `Contribuinte` nesta versao.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Cadastro de pessoa fisica com CPF valido**
- **Dado** um tenant resolvido no contexto
- **E** um comando `CadastrarContribuintePessoaFisicaCommand` com CPF valido "529.982.247-25" e nome "Maria Silva"
- **Quando** o handler processa o comando
- **Entao** um `Contribuinte` e persistido com `TipoPessoa = Fisica`, `Documento = "52998224725"` (sem mascara) e `Nome = "Maria Silva"`
- **E** o evento de dominio `ContribuinteCadastrado` e emitido
- **E** o retorno e o `Guid` do contribuinte criado

**Cenario 2 — CPF invalido e rejeitado**
- **Dado** um comando com CPF "111.111.111-11" (sequencia repetida, invalida)
- **Quando** a validacao do pipeline executa
- **Entao** uma `ValidationException` e lancada com a mensagem "CPF invalido."
- **E** nenhum `Contribuinte` e persistido

**Cenario 3 — Nome em branco e rejeitado**
- **Dado** um comando com CPF valido e `Nome` vazio
- **Quando** a validacao do pipeline executa
- **Entao** uma `ValidationException` e lancada (regra `NotEmpty` em `Nome`)
- **E** nenhum `Contribuinte` e persistido

**Cenario 4 — Inscricao municipal opcional ausente**
- **Dado** um comando com CPF valido, nome preenchido e `InscricaoMunicipal = null`
- **Quando** o handler processa o comando
- **Entao** o `Contribuinte` e criado com `InscricaoMunicipal = null` (sem erro)

**Cenario 5 — Documento armazenado sem mascara**
- **Dado** um comando com CPF informado **com** mascara "529.982.247-25"
- **Quando** o handler processa o comando
- **Entao** o `Documento` persistido contem **somente digitos** ("52998224725")

**Cenario 6 — Tenant carimbado a partir do contexto**
- **Dado** um `ITenantContext` com `TenantId = T`
- **Quando** um contribuinte PF e cadastrado
- **Entao** o `Contribuinte.TenantId` persistido e igual a `T`
- **E** o evento `ContribuinteCadastrado` carrega `TenantId = T`

---

## 13. Casos de Borda

Cada item vira teste.

- **CB-1.** CPF com mascara e pontuacao — deve ser normalizado para 11 digitos (`DigitoUtil.ExtractDigits`).
- **CB-2.** CPF com digitos verificadores incorretos — rejeitado (`Cpf.IsValid` => `false`).
- **CB-3.** CPF de digitos repetidos ("00000000000", "11111111111") — rejeitado (`AllSameDigit`).
- **CB-4.** CPF com numero de digitos diferente de 11 — rejeitado.
- **CB-5.** `Nome` somente com espacos — rejeitado na fabrica (`ThrowIfNullOrWhiteSpace`) e na validacao (`NotEmpty`).
- **CB-6.** `Nome` com exatamente 200 caracteres — aceito; com 201 — rejeitado (`MaximumLength(200)`).
- **CB-7.** `InscricaoMunicipal = null` — aceito (opcional).
- **CB-8.** `request` nulo no handler — `ArgumentNullException`.
- **CB-9.** Ausencia de tenant resolvido — `InvalidOperationException` ao ler `ITenantContext.TenantId`.
- **CB-10.** Documentos iguais para o mesmo tenant — **nao** ha unicidade imposta pela persistencia
  (indice nao unico); duplicidade nao e bloqueada nesta versao.
- **CB-11.** Mesmo documento em **tenants distintos** — permitido (isolamento por tenant).
- **CB-12.** Fabrica de dominio `PessoaFisica` chamada com `cpf = null` — `ArgumentNullException` (I-5).

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial. Retrofit do codigo existente: agregado `Contribuinte` (PF/PJ no dominio), comando `CadastrarContribuintePessoaFisica`, evento `ContribuinteCadastrado`, validacao FluentValidation e mapeamento EF (schema `tributos`, tabela `Contribuintes`). PJ presente no dominio mas sem comando exposto; sem consultas; sem Integration Events. |

<!-- manifest
commands: CadastrarContribuintePessoaFisica
queries: BuscarContribuintes
domainEvents: ContribuinteCadastrado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
