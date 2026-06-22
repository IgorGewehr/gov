---
modulo: Educacao
agregado: Escola
contexto: Educacao (rede municipal de ensino — cadastro de escolas e EducaCenso/INEP)
poder: Executivo
schema: educacao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 arts. 205-214 (direito a educacao; min. 25% MDE)", "LDB Lei 9.394/1996 (atualizada pela Lei 14.945/2024)", "Censo Escolar/EducaCenso (INEP) — leiaute posicional anual", "FUNDEB EC 108/2020 + Lei 14.113/2020", "LGPD Lei 13.709/2018 art. 14"]
---

# Escola — Regras-as-Code (Rules-as-Code)

> Unidade escolar da rede municipal de ensino, identificada nacionalmente pelo `CodigoINEP` —
> chave de integracao com o Censo Escolar/EducaCenso. Reune dados de credenciamento,
> dependencia administrativa, endereco e infraestrutura, e e a origem dos formularios Escola/Gestor
> exportados ao INEP. Este arquivo e **normativo e versionado**; o codigo (`Escola.cs`, handlers,
> validators, EF config, testes) e consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Escola (`Escola`) | Unidade escolar da rede municipal; raiz de agregado. |
| Codigo INEP (`CodigoInep` : `CodigoInep`) | Identificador unico nacional da escola — chave de integracao com o Censo. |
| Dependencia Administrativa (`DependenciaAdministrativa` : enum) | Federal/Estadual/Municipal/Privada. |
| Endereco (`Endereco` : VO) | Endereco e localizacao (com coordenadas georreferenciadas, GeoJSON). |
| Infraestrutura (`Infraestrutura` : VO) | Recursos fisicos da escola (salas, dependencias, acessibilidade). |
| Credenciamento (`Credenciar` / `Credenciada`) | Ato que habilita a escola a operar na rede de ensino. |
| Dados do Censo (`AtualizarDadosCenso` / `DadosCensoAtualizados`) | Atualizacao dos dados cadastrais exigidos pelo EducaCenso. |
| Situacao (`Situacao` : `SituacaoEscola`) | Estado atual da escola no ciclo de operacao. |
| Educacao Basica (`EducacaoBasica`) | Etapas Infantil, Fundamental e Medio ofertadas pela escola. |
| Rede de Ensino (`TenantId`) | Ente municipal dono do registro (tenant). |
| Tenant (`TenantId`) | Ente municipal (rede de ensino) dono do registro. |

---

## 2. Modelo

- **Identidade:** `EscolaId` — `readonly record struct EscolaId(Guid Value)`; fabrica `EscolaId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Escola : AggregateRoot<EscolaId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valida via factory `Credenciar(...)`.

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente municipal/rede) dono do registro. | `private set` |
| `CodigoInep` | `CodigoInep` (VO) | Codigo INEP unico nacional. | `private set` |
| `Nome` | `string` | Nome da unidade escolar. | `private set` |
| `DependenciaAdministrativa` | `DependenciaAdministrativa` (enum) | Federal/Estadual/Municipal/Privada. | `private set` |
| `Endereco` | `Endereco` (VO) | Endereco e georreferenciamento. | `private set` |
| `Infraestrutura` | `Infraestrutura` (VO) | Recursos fisicos/acessibilidade. | `private set` |
| `Situacao` | `SituacaoEscola` (enum) | Situacao atual. | `private set` |

### Value Objects (referenciados)

- `CodigoInep` — `record struct` com `Value : string`; nao nulo/nao vazio (validado em `Credenciar`).
- `Endereco` — VO com logradouro, municipio, UF, CEP e coordenadas (latitude/longitude, GeoJSON).
- `Infraestrutura` — VO com numero de salas, dependencias e itens de acessibilidade.

### Enum `DependenciaAdministrativa`

| Valor | Numerico | Descricao |
|---|---|---|
| `Federal` | 1 | Dependencia administrativa federal. |
| `Estadual` | 2 | Dependencia administrativa estadual. |
| `Municipal` | 3 | Dependencia administrativa municipal. |
| `Privada` | 4 | Dependencia administrativa privada. |

### Enum `SituacaoEscola`

| Valor | Numerico | Descricao |
|---|---|---|
| `EmCadastro` | 1 | Em cadastro (ainda nao credenciada). |
| `Credenciada` | 2 | Credenciada e apta a operar. |
| `Desativada` | 3 | Desativada (terminal). |

> **Conjuntos de referencia usados nas guardas:**
> - **Ativa** = { `Credenciada` }.
> - **Encerrada** = { `Desativada` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** O `CodigoInep` e obrigatorio no credenciamento e deve ser unico por rede (`ArgumentException.ThrowIfNullOrWhiteSpace`).
- **I-2.** O `Nome` da escola e obrigatorio (nao nulo/nao vazio).
- **I-3.** A `DependenciaAdministrativa` e obrigatoria e pertence ao enum (Federal/Estadual/Municipal/Privada).
- **I-4.** No credenciamento a situacao inicial e `Credenciada` e e emitido o evento `EscolaCredenciada(id, codigoInep)`.
- **I-5.** A atualizacao de dados do Censo so ocorre sobre escola nao desativada; emite `DadosCensoAtualizados`.
- **I-6.** A geracao do leiaute EducaCenso exige `CodigoInep` valido (idempotencia por codigo INEP).
- **I-7.** O isolamento e por tenant = ente municipal; escopo por `CodigoInep` da rede; dados nao cruzam tenants.
- **I-8.** Escola `Desativada` (terminal) nao admite novas transicoes de operacao.
- **I-9.** Apenas matriculas validas no Censo geram repasse FUNDEB; a escola e a referencia de agregacao dessas matriculas (EC 108/2020 + Lei 14.113/2020).

---

## 4. Maquina de Estados

Tabela: Estado origem -> comando/metodo -> Estado destino | guarda | evento emitido.

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Credenciar` | `Credenciada` | `codigoInep` nao vazio; `nome` nao vazio | `EscolaCredenciada` |
| `Credenciada` | `AtualizarDadosCenso` | `Credenciada` | situacao != `Desativada` | `DadosCensoAtualizados` |
| `Credenciada` | `Desativar` | `Desativada` | situacao == `Credenciada` | — |

> Observacoes:
> - `AtualizarDadosCenso` nao altera a situacao; apenas registra os dados e emite evento.
> - Estado `Desativada` e terminal: bloqueia novas transicoes de operacao.

---

## 5. Comandos (escrita)

### 5.1 CredenciarEscola

- **Command:** `CredenciarEscolaCommand(string CodigoInep, string Nome, DependenciaAdministrativa Dependencia, Endereco Endereco, Infraestrutura Infraestrutura) : ICommand<Guid>`.
- **Entrada (DTO):** `CodigoInep`, `Nome`, `Dependencia`, `Endereco`, `Infraestrutura`.
- **Dependencias do handler:** `IEscolaRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - `CodigoInep` nao vazio e ainda nao existente na rede (senao `InvalidOperationException("Codigo INEP ja cadastrado.")`).
  - `Nome` nao vazio; `Dependencia` valida.
- **Efeito:** cria via `Escola.Credenciar(tenant.TenantId, codigoInep, nome, dependencia, endereco, infraestrutura)`; `escolas.Adicionar(escola)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Escola` em situacao `Credenciada`; retorna `escola.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `ArgumentException` (codigoInep/nome vazios); `InvalidOperationException` (codigo INEP duplicado).
- **Evento de dominio:** `EscolaCredenciada(id, codigoInep)` (emitido no construtor via factory).

### 5.2 AtualizarDadosCenso

- **Command:** `AtualizarDadosCensoCommand(Guid EscolaId, Endereco Endereco, Infraestrutura Infraestrutura) : ICommand`.
- **Entrada (DTO):** `EscolaId`, `Endereco`, `Infraestrutura`.
- **Dependencias do handler:** `IEscolaRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Escola existe (`ObterPorIdAsync`), senao `InvalidOperationException("Escola nao encontrada.")`.
  - Situacao != `Desativada` (I-5).
- **Efeito:** `escola.AtualizarDadosCenso(endereco, infraestrutura)`; `SaveChangesAsync`.
- **Pos-condicoes:** dados cadastrais do Censo atualizados; situacao inalterada.
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrada ou desativada).
- **Evento de dominio:** `DadosCensoAtualizados(Id)`.

### 5.3 DesativarEscola

- **Command:** `DesativarEscolaCommand(Guid EscolaId) : ICommand`.
- **Entrada (DTO):** `EscolaId`.
- **Dependencias do handler:** `IEscolaRepository`, `IUnitOfWork`.
- **Pre-condicoes:**
  - `request` nao nulo.
  - Escola existe, senao `InvalidOperationException("Escola nao encontrada.")`.
  - Situacao == `Credenciada`.
- **Efeito:** `escola.Desativar()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Desativada`.
- **Excecoes:** `ArgumentNullException` (request); `InvalidOperationException` (nao encontrada ou ja desativada).
- **Evento de dominio:** nenhum nesta versao.

---

## 6. Consultas (leitura)

### 6.1 ObterEscolaPorCodigoInep

- **Query:** `ObterEscolaPorCodigoInepQuery(string CodigoInep) : IQuery<EscolaResumo?>`.
- **Entrada:** `CodigoInep`.
- **Handler:** `ObterEscolaPorCodigoInepHandler(IEscolaRepository escolas)`; chama `escolas.ObterPorCodigoInepAsync(new CodigoInep(request.CodigoInep), ct)`.
- **Projecao (DTO):** `EscolaResumo(Guid Id, string CodigoInep, string Nome, string Dependencia, string Situacao)`.
- **Filtros:** por `CodigoInep`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarEscolasDaRede

- **Query:** `ListarEscolasDaRedeQuery() : IQuery<IReadOnlyList<EscolaResumo>>`.
- **Entrada:** nenhuma (escopo do tenant atual).
- **Handler:** `ListarEscolasDaRedeHandler(IEscolaRepository escolas)`; chama `escolas.ListarAsync(ct)`.
- **Projecao (DTO):** `EscolaResumo(...)` (mesma projecao de 6.1).
- **Filtros:** **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo.

---

## 7. Eventos

### Dominio (in-process, MediatR; assembly `...Educacao.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `EscolaCredenciada` | `(EscolaId, CodigoInep codigoInep)` | `Escola.Credenciar` (construtor) |
| `DadosCensoAtualizados` | `(EscolaId)` | `Escola.AtualizarDadosCenso` |

### Integracao (publica via `*.Contracts` + Outbox; assembly `...Educacao.Contracts`)

- Nenhum nesta versao. A escola e a referencia de agregacao de matriculas que alimentam FUNDEB/PNAE/PNATE, porem os eventos de integracao do modulo (`AlunoMatriculado`, `MatriculaEncerrada`, `ResultadoApurado`, `MatrizCurricularPublicada`, `ChamadaPublicaHomologada`) sao publicados pelos respectivos agregados (Matricula, DiarioClasse, MatrizCurricular, Cardapio).

### Integracao (consome)

- Nenhum diretamente no agregado Escola. No modulo, eventos de **Administracao** (`ContratoAssinado`) e **Patrimonio** (veiculos) sao consumidos pelos agregados de merenda/transporte, nao pela Escola.

---

## 8. Validacoes (FluentValidation)

### CredenciarEscolaValidator (`AbstractValidator<CredenciarEscolaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `CodigoInep` | `NotEmpty()` + `MaximumLength(8)` | "Codigo INEP obrigatorio (8 digitos)." |
| `Nome` | `NotEmpty()` + `MaximumLength(150)` | "Nome da escola obrigatorio (max. 150 caracteres)." |
| `Dependencia` | `IsInEnum()` | "Dependencia administrativa invalida." |

### AtualizarDadosCensoValidator (`AbstractValidator<AtualizarDadosCensoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `EscolaId` | `NotEmpty()` | "Identificador da escola obrigatorio." |

> `DesativarEscolaCommand` nao possui validador FluentValidation dedicado nesta versao; a protecao e feita por invariantes de dominio e checagens de existencia no handler. As queries nao possuem validador.

---

## 9. Persistencia (EF Core 8)

- **Schema:** `educacao` (isolado por modulo). **DbContext:** o do modulo Educacao. **Migrations:** por modulo.
- **Tabela:** `Escola` (raiz de agregado).

| Coluna | Tipo logico | Conversor / observacao |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `EscolaId` <-> `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `CodigoInep` | `varchar(8)` | conversor de `CodigoInep` <-> `string`. |
| `Nome` | `nvarchar(150)` | nao nulo. |
| `DependenciaAdministrativa` | `int` | enum `DependenciaAdministrativa` (persistido por valor numerico). |
| `Endereco_*` | owned | VO `Endereco` (owned), incluindo latitude/longitude (GeoJSON). |
| `Infraestrutura_*` | owned | VO `Infraestrutura` (owned). |
| `Situacao` | `int` | enum `SituacaoEscola` (persistido por valor numerico). |

- **Indices:**
  - PK em `Id`.
  - Indice **unico** em `(TenantId, CodigoInep)` — unicidade do codigo INEP por rede (I-1).
  - Indice em `(TenantId, DependenciaAdministrativa)` para listagem e agregacao por dependencia.
- **Conversores (VO/Id):** Fluent API (sem data annotations no dominio).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** `Escola` implementa `IMustHaveTenant`. `TenantId` carimbado na insercao pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Isolamento por **tenant = ente municipal**, escopo por `CodigoInep` da rede; gravacao cross-tenant **lanca excecao**. `ITenantContext` resolvido do JWT por requisicao.
- **RBAC (policy-based + RBAC `Usuario -> Departamento -> Roles`, negar por padrao):**
  - Credenciar/atualizar/desativar escola: papeis da **gestao da rede** (ex.: `Educacao.Escola.Gerir`).
  - Consulta de escolas: papel de **leitura** (ex.: `Educacao.Escola.Ler`).
  - Modulo Educacao e **ativavel por tenant** (Executivo); requisicao a tenant sem licenca -> 404/403 auditado.
- **LGPD (art. 14 — menores):** o cadastro de escola nao trata dados de menores diretamente; minimizacao aplicada (coletar so o necessario ao Censo/cadastro). A geolocalizacao da escola e restrita ao estritamente necessario para otimizacao/auditoria de rotas.
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuario, IP, timestamp) em toda mutacao (`Credenciar`, `AtualizarDadosCenso`, `Desativar`) e nos logs de exportacao ao INEP/FNDE — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integracoes Governamentais

- **EducaCenso/INEP (saida):** a Escola e origem dos formularios **Escola** e **Gestor** do leiaute posicional anual. Validacao e reconciliacao por `CodigoInep`; **idempotencia por codigo INEP**; geracao versionada por ano-base.
- **FNDE/FUNDEB (saida indireta):** as matriculas agregadas por escola alimentam os coeficientes do FUNDEB (EC 108/2020 + Lei 14.113/2020); prestacao de contas via SiGPC/Contas Online.
- **Georreferenciamento:** coordenadas da escola (GeoJSON) para otimizacao e auditoria de rotas de transporte.
- **Resiliencia:** integracoes externas resilientes (Polly: retry + circuit breaker) atras de **Anti-Corruption Layer**; eventos via Outbox; idempotencia por `CodigoInep`/`EventId`.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Credenciamento de escola**
- **Dado** uma rede sem a escola de codigo INEP "43000001"
- **Quando** executo `CredenciarEscolaCommand("43000001", "EMEF Central", Municipal, endereco, infraestrutura)`
- **Entao** e criada uma `Escola` em situacao `Credenciada` e o evento `EscolaCredenciada` e emitido.

**Cenario 2 — Codigo INEP duplicado na rede**
- **Dado** uma escola ja credenciada com codigo INEP "43000001"
- **Quando** executo `CredenciarEscolaCommand` com o mesmo codigo
- **Entao** ocorre `InvalidOperationException("Codigo INEP ja cadastrado.")`.

**Cenario 3 — Atualizacao de dados do Censo**
- **Dado** uma escola `Credenciada`
- **Quando** executo `AtualizarDadosCensoCommand(escolaId, novoEndereco, novaInfraestrutura)`
- **Entao** os dados sao atualizados e o evento `DadosCensoAtualizados` e emitido.

**Cenario 4 — Atualizacao sobre escola desativada**
- **Dado** uma escola `Desativada`
- **Quando** executo `AtualizarDadosCensoCommand`
- **Entao** ocorre `InvalidOperationException` (escola desativada nao admite atualizacao).

**Cenario 5 — Geracao do EducaCenso exige INEP valido**
- **Dado** o fechamento da Matricula Inicial com escolas credenciadas
- **Quando** o leiaute do EducaCenso (formularios Escola/Gestor) e gerado
- **Entao** todas as escolas tem `CodigoInep` valido e o arquivo passa na validacao (idempotente por codigo INEP).

**Cenario 6 — Consulta tenant-scoped**
- **Dado** escolas da rede A e escolas da rede B
- **Quando** executo `ListarEscolasDaRedeQuery()` no contexto do tenant A
- **Entao** retornam **apenas** as escolas do tenant A, projetadas em `EscolaResumo`.

---

## 13. Casos de Borda

- **B-1.** `codigoInep` nulo/vazio/espacos em `Credenciar` => `ArgumentException` (I-1); via command, `MaximumLength(8)` no validator.
- **B-2.** `nome` nulo/vazio em `Credenciar` => `ArgumentException` (I-2).
- **B-3.** `Dependencia` fora do enum => rejeitada por `IsInEnum()` no validator (I-3).
- **B-4.** Credenciar a mesma escola (mesmo codigo INEP) duas vezes => a 2a falha (unicidade por rede).
- **B-5.** Atualizar dados do Censo de escola inexistente => `InvalidOperationException("Escola nao encontrada.")`.
- **B-6.** Desativar escola ja `Desativada` => falha (situacao != `Credenciada`).
- **B-7.** Acessar/credenciar escola de outro tenant => bloqueado pelo Global Query Filter / excecao cross-tenant.
- **B-8.** Geracao de leiaute com escola sem `CodigoInep` valido => bloqueada na validacao (I-6).

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial — derivada do README do modulo Educacao (mapa de dominio: agregado Escola, eventos `EscolaCredenciada`/`DadosCensoAtualizados`; integracao EducaCenso/INEP por `CodigoINEP`). |

<!-- manifest
commands: CredenciarEscola, AtualizarDadosCenso, DesativarEscola
queries: ObterEscolaPorCodigoInep, ListarEscolasDaRede
domainEvents: EscolaCredenciada, DadosCensoAtualizados
integrationEventsPublished: 
integrationEventsConsumed: 
-->
