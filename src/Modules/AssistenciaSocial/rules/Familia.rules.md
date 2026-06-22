---
modulo: AssistenciaSocial
agregado: Familia
contexto: AssistenciaSocial (SUAS — referenciamento e gestão socioeconômica de famílias)
poder: Executivo
schema: assistenciasocial
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF arts. 203-204", "Lei 8.742/1993 (LOAS, c/ Lei 12.435/2011)", "PNAS/2004 (Res. CNAS 145/2004)", "NOB-SUAS/2012 (Res. CNAS 33/2012)", "Lei 14.601/2023 (PBF)", "LGPD art. 11 (dado sensivel)"]
---

# Familia — Regras-as-Code (Rules-as-Code)

> Raiz de agregado que **referencia e organiza** a família em situação de vulnerabilidade/risco
> dentro do SUAS: calcula a **renda per capita**, vincula a família a um **território/CRAS** e
> projeta o **read model do CadÚnico (MDS)** — base **federal autoritativa** que este módulo
> **consome** e **jamais sobrescreve**. Marco: CF arts. 203-204; Lei 8.742/1993 (LOAS, c/ Lei
> 12.435/2011); PNAS/2004; NOB-SUAS/2012; Lei 14.601/2023 (PBF). Este arquivo é **normativo e
> versionado**; o código (domínio, handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Família (`Familia`) | Núcleo familiar referenciado pelo SUAS; raiz de agregado. Implementa `IMustHaveTenant`. |
| Membro Familiar (`MembroFamiliar`) | Entidade-filha: pessoa que compõe o núcleo familiar (renda, parentesco, condições). |
| Referenciamento (`Referenciar` / `Referenciada`) | Ato de vincular a `Familia` a uma `UnidadeAtendimento` (CRAS) cujo território cobre o endereço. |
| NIS (`Nis` : VO) | Número de Identificação Social — chave do CadÚnico. Mascarado no barramento (`NisMascarado`). |
| CPF (`Cpf` : VO) | Cadastro de Pessoa Física do responsável/membro. Mascarado no barramento. |
| Renda Per Capita (`RendaPerCapita` : VO) | Renda familiar total dividida pelo número de membros; base de elegibilidade. |
| Endereço Territorializado (`EnderecoTerritorializado` : VO) | Endereço com território/área de abrangência de um CRAS. |
| Território (`Territorio`) | Área de cobertura à qual a família pertence (vincula ao CRAS responsável). |
| Unidade de Atendimento (`UnidadeAtendimentoId`) | CRAS/CREAS/CentroPOP que referencia a família. PAIF só em CRAS (porta de entrada PSB). |
| Salário Mínimo Vigente (`SalarioMinimoVigente`) | Parâmetro versionado por vigência/competência; nunca *hardcoded*. |
| Atualização Cadastral (`DataUltimaAtualizacaoCadastral` / `AtualizacaoVencida`) | Marco da última atualização do CadÚnico; obrigatória a cada 24 meses. |
| Vigência Cadastral (`MesesValidadeCadastro`) | Janela de validade do cadastro (24 meses); processada para sinalizar atualização. |
| CadÚnico (`CadUnico`) | Cadastro Único (MDS); base federal de renda/composição. Somente leitura (read model). |
| Resumo CadÚnico (`ResumoCadUnico`) | Read model federal projetado por NIS/CPF (folha resumo, composição, renda). |
| Tenant (`TenantId`) | Ente público (Prefeitura) dono do registro — município. |
| Situação (`Situacao` : `SituacaoFamilia`) | Estado atual da família no ciclo de referenciamento. |

---

## 2. Modelo

- **Identidade:** `FamiliaId` — `readonly record struct FamiliaId(Guid Value)`; fábrica `FamiliaId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Familia : AggregateRoot<FamiliaId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Referenciar(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (município) dono do registro. | `private set` |
| `Nis` | `Nis` (VO) | NIS do responsável familiar (chave do CadÚnico). | `private set` |
| `CpfResponsavel` | `Cpf` (VO) | CPF do responsável familiar. | `private set` |
| `Endereco` | `EnderecoTerritorializado` (VO) | Endereço com território/área de cobertura. | `private set` |
| `UnidadeAtendimentoId` | `Guid` | CRAS de referência (porta de entrada PSB). | `private set` |
| `Territorio` | `string` | Território de cobertura ao qual a família pertence. | `private set` |
| `RendaPerCapita` | `RendaPerCapita` (VO) | Renda per capita calculada a partir dos membros. | `private set` (recalculada) |
| `DataReferenciamento` | `DateOnly` | Data do referenciamento ao CRAS. | `private set` |
| `DataUltimaAtualizacaoCadastral` | `DateOnly` | Marco da última atualização no CadÚnico. | `private set` |
| `Situacao` | `SituacaoFamilia` | Situação atual da família. | `private set` |
| `Membros` | `IReadOnlyCollection<MembroFamiliar>` | Composição familiar (entidades-filhas). | coleção encapsulada |

### Constantes

- `MesesValidadeCadastro` = `24` — vigência cadastral do CadÚnico (atualização obrigatória a cada 24 meses).

> **Parametrização por tenant:** `SalarioMinimoVigente` e os limiares de renda **não** são constantes de domínio: são **versionados por vigência/competência** (aplica-se a regra vigente na competência), nunca *hardcoded* (CLAUDE.md §7; README §5).

### Value Objects (referenciados)

- `Nis` — `record` com número de identificação social válido (dígito verificador); expõe versão mascarada `Mascarado`.
- `Cpf` — `record struct` (SharedKernel); expõe versão mascarada `Mascarado`. Não nulo (validado em `Referenciar`).
- `RendaPerCapita` — `record` com `Valor` (`decimal`) e fator de comparação com `SalarioMinimoVigente` (ex.: `EhAteMeioSalario`, `EhAbaixoUmQuarto`). Não negativa.
- `EnderecoTerritorializado` — `record` com logradouro, município, CEP e `Territorio` (área de cobertura do CRAS).

### Entidade `MembroFamiliar`

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `MembroFamiliarId` (record struct Guid) | Identidade do membro. |
| `Cpf` | `Cpf` (VO) | CPF do membro. |
| `Parentesco` | `Parentesco` (enum) | Relação com o responsável familiar. |
| `DataNascimento` | `DateOnly` | Para apuração de idade (ex.: criança/adolescente, idoso ≥ 65). |
| `RendaIndividual` | `ValorMonetario` (VO) | Renda declarada do membro (soma para renda familiar). |
| `EhPcd` | `bool` | Indicador de pessoa com deficiência (dado sensível, art. 11 LGPD). |

### Enum `SituacaoFamilia`

| Valor | Numérico | Descrição |
|---|---|---|
| `Referenciada` | 1 | Família referenciada a um CRAS (estado inicial). |
| `AtualizacaoVencida` | 2 | Vigência cadastral > 24 meses; sinalizada para atualização. |
| `Regularizada` | 3 | Cadastro atualizado após vencimento; volta a estar apta a novos benefícios. |

> **Conjuntos de referência usados nas guardas:**
> - **Apta a novos benefícios** = { `Referenciada`, `Regularizada` } **e** cadastro dentro da vigência.
> - **Bloqueada para novos benefícios** = { `AtualizacaoVencida` } (condicionada à regularização — README §5, Cenário "Atualização cadastral obrigatória vencida").

### Enum `Parentesco`

| Valor | Numérico | Descrição |
|---|---|---|
| `ResponsavelFamiliar` | 1 | Responsável pela unidade familiar (RF). |
| `Conjuge` | 2 | Cônjuge/companheiro(a). |
| `Filho` | 3 | Filho(a). |
| `Outro` | 9 | Demais membros do núcleo. |

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Nis` é obrigatório e válido no referenciamento (`ArgumentNullException`/validação de dígito); sem NIS válido não há referenciamento (README §8, Cenário "Referenciamento").
- **I-2.** O endereço deve estar **dentro do território** do CRAS informado; referenciar a unidade cujo território não cobre o endereço é rejeitado (`InvalidOperationException`).
- **I-3.** `RendaPerCapita` = soma das `RendaIndividual` dos membros ÷ número de membros; recalculada sempre que a composição/renda muda. Nunca negativa.
- **I-4.** O número de membros usado no cálculo é ≥ 1 (divisão por zero proibida).
- **I-5.** Atualização cadastral vencida ⇔ `hoje > DataUltimaAtualizacaoCadastral + MesesValidadeCadastro` (24 meses); ao processar a vigência, a situação passa a `AtualizacaoVencida`.
- **I-6.** Família com cadastro vencido (`AtualizacaoVencida`) **não está apta** a novos benefícios; a aptidão fica **condicionada à regularização** (README §5; Cenário "Atualização cadastral obrigatória vencida").
- **I-7.** Mudança de endereço/renda/composição exige **atualização imediata** do cadastro (renova `DataUltimaAtualizacaoCadastral`) — README §5 (atualização obrigatória ou imediata).
- **I-8.** No referenciamento, a situação inicial é `Referenciada`, `DataReferenciamento` = data informada e é emitido o evento `FamiliaReferenciada(familiaId, nisMascarado, unidadeId, territorio, dataReferenciamento)`.
- **I-9.** A base **federal (CadÚnico/MDS) é autoritativa**: a `Familia` projeta apenas um **read model**; nenhum comando deste agregado escreve/sobrescreve a base federal (README §1 e §4).
- **I-10.** `Cpf` do responsável é obrigatório e válido no referenciamento.
- **I-11.** Membro `MembroFamiliar` com `Parentesco = ResponsavelFamiliar` é único por família (no máximo um RF).
- **I-12.** Estado `AtualizacaoVencida` só transita para `Regularizada` mediante regularização cadastral (atualização do CadÚnico); não há atalho.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Referenciar` | `Referenciada` | `Nis` válido; `Cpf` válido; endereço ∈ território do CRAS | `FamiliaReferenciada` |
| `Referenciada` \| `Regularizada` | `ProcessarVigenciaCadastral` | `AtualizacaoVencida` | `hoje > DataUltimaAtualizacaoCadastral + 24 meses` | — |
| `AtualizacaoVencida` | `RegularizarCadastro` | `Regularizada` | atualização cadastral aplicada | — |
| qualquer | `AtualizarRendaFamiliar` | (mantém) | composição/renda recalculada; renova `DataUltimaAtualizacaoCadastral` | — |

> Observações:
> - `ProcessarVigenciaCadastral` é idempotente: se já `AtualizacaoVencida`, não reemite transição.
> - `AtualizarRendaFamiliar` recalcula `RendaPerCapita` e (mudança de renda/composição) renova o marco cadastral (I-7), podendo regularizar (`AtualizacaoVencida` → `Regularizada`).
> - Não há transição que escreva na base federal (I-9).

---

## 5. Comandos (escrita)

### 5.1 ReferenciarFamilia

- **Command:** `ReferenciarFamiliaCommand(string Nis, Guid UnidadeAtendimentoId, EnderecoTerritorializadoDto Endereco, IReadOnlyList<MembroFamiliarDto> Membros) : ICommand<Guid>`.
- **Entrada (DTO):** `Nis`, `UnidadeAtendimentoId` (CRAS), `Endereco` (com território), `Membros` (composição + rendas).
- **Dependências do handler:** `IUnidadeAtendimentoRepository`, `IFamiliaRepository`, `ICadUnicoGateway` (ACL, consulta por NIS), `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - CRAS existe e seu `TerritorioCobertura` contém o endereço, senão `InvalidOperationException("Endereço fora do território do CRAS.")` (I-2).
  - NIS válido/consultável no CadÚnico (read model federal), senão `InvalidOperationException("NIS inválido ou não localizado no CadÚnico.")` (I-1, I-9).
- **Efeito:** `hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`; cria via `Familia.Referenciar(tenant.TenantId, nis, cpfResponsavel, endereco, unidadeId, membros, hoje)`; `familias.Adicionar(familia)`; `SaveChangesAsync`.
- **Pós-condições:** nova `Familia` em situação `Referenciada` vinculada ao CRAS; `RendaPerCapita` calculada; retorna `familia.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (NIS inválido ou endereço fora do território).
- **Evento de domínio:** `FamiliaReferenciada(familiaId, nisMascarado, unidadeId, territorio, dataReferenciamento)`.
- **Evento de integração (publica):** `FamiliaReferenciadaIntegrationEvent` (Outbox; NIS mascarado).

### 5.2 AtualizarRendaFamiliar

- **Command:** `AtualizarRendaFamiliarCommand(Guid FamiliaId, IReadOnlyList<MembroFamiliarDto> Membros) : ICommand`.
- **Entrada (DTO):** `FamiliaId`, nova lista de `Membros` (rendas/composição).
- **Dependências do handler:** `IFamiliaRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Família existe (`ObterPorIdAsync`), senão `InvalidOperationException("Família não encontrada.")`.
- **Efeito:** `familia.AtualizarRendaFamiliar(membros, hoje)` — recalcula `RendaPerCapita`, renova `DataUltimaAtualizacaoCadastral` (I-7) e, se vencida, regulariza; `SaveChangesAsync`.
- **Pós-condições:** `RendaPerCapita` recalculada; marco cadastral renovado; situação pode passar de `AtualizacaoVencida` a `Regularizada`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou número de membros = 0 — I-4).
- **Evento de domínio:** — (sem evento nesta versão).

### 5.3 ProcessarVigenciaCadastral

- **Command:** `ProcessarVigenciaCadastralCommand(Guid FamiliaId) : ICommand`.
- **Entrada (DTO):** `FamiliaId`.
- **Dependências do handler:** `IFamiliaRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Família existe, senão `InvalidOperationException("Família não encontrada.")`.
- **Efeito:** `familia.ProcessarVigenciaCadastral(hoje)` — se `hoje > DataUltimaAtualizacaoCadastral + 24 meses`, passa a `AtualizacaoVencida` (I-5/I-6); `SaveChangesAsync`.
- **Pós-condições:** família sinalizada para atualização quando vencida; elegibilidade a novos benefícios condicionada à regularização.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada).
- **Evento de domínio:** — (sem evento nesta versão).

> **Comandos de domínio existentes no agregado sem handler de Application nesta versão:** `RegularizarCadastro()` (método da raiz, exposto para futura orquestração; também alcançável via `AtualizarRendaFamiliar`).

---

## 6. Consultas (leitura)

### 6.1 ObterFamiliasDoTerritorio

- **Query:** `ObterFamiliasDoTerritorioQuery(string Territorio) : IQuery<IReadOnlyList<FamiliaResumo>>`.
- **Entrada:** `Territorio`.
- **Handler:** `ObterFamiliasDoTerritorioHandler(IFamiliaRepository familias)`; chama `familias.ListarPorTerritorioAsync(request.Territorio, ct)`.
- **Projeção (DTO):** `FamiliaResumo(Guid Id, string NisMascarado, Guid UnidadeAtendimentoId, string Territorio, decimal RendaPerCapita, string Situacao, DateOnly DataReferenciamento, DateOnly DataUltimaAtualizacaoCadastral)`.
  - `NisMascarado` projetado de `familia.Nis.Mascarado` (minimização — não expõe NIS bruto).
- **Filtros:** por `Territorio`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ObterResumoCadUnico

- **Query:** `ObterResumoCadUnicoQuery(Guid FamiliaId) : IQuery<ResumoCadUnico>`.
- **Entrada:** `FamiliaId`.
- **Handler:** `ObterResumoCadUnicoHandler(IFamiliaRepository familias, ICadUnicoReadModel cadUnico)`; localiza a família (tenant-scoped) e projeta o **read model federal** por NIS.
- **Projeção (DTO):** `ResumoCadUnico(string NisMascarado, decimal RendaFamiliarDeclarada, int QuantidadeMembros, DateOnly DataUltimaAtualizacao, bool DentroDaVigencia)` — **somente leitura**, isolado por `TenantId` (README §6).
- **Filtros:** por `FamiliaId`/NIS; tenant-scoped; **nunca** trafega entre tenants (I-9, README §6).
- **Pré-condições:** `request` não nulo; família existe no tenant.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...AssistenciaSocial.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `FamiliaReferenciada` | `(FamiliaId, string NisMascarado, Guid UnidadeId, string Territorio, DateOnly DataReferenciamento)` | `Familia.Referenciar` (factory) |

### Integração (publica via `*.Contracts` + Outbox; assembly `...AssistenciaSocial.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `FamiliaReferenciadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid FamiliaId, string NisMascarado, Guid UnidadeId, string Territorio, DateOnly DataReferenciamento)` | `ReferenciarFamiliaHandler` |

> Payload expõe apenas identificadores e território; **NIS mascarado** — sem dado sensível identificável no barramento (README §7). **Nunca** trafega dado do CadÚnico federal entre tenants/módulos.

### Integração (consome)

- Nenhum nesta versão (o agregado `Familia` não consome Integration Events de outros módulos).

---

## 8. Validações (FluentValidation)

### ReferenciarFamiliaValidator (`AbstractValidator<ReferenciarFamiliaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Nis` | `NotEmpty()` + validador de NIS (11 dígitos + DV) | "NIS é obrigatório e deve ser válido." |
| `UnidadeAtendimentoId` | `NotEmpty()` | "Unidade de atendimento (CRAS) é obrigatória." |
| `Endereco.Territorio` | `NotEmpty()` | "Território do endereço é obrigatório." |
| `Membros` | `NotEmpty()` (≥ 1 membro) | "A família deve ter ao menos um membro." |
| `Membros[].Cpf` | CPF válido por item | "CPF de membro inválido." |

### AtualizarRendaFamiliarValidator (`AbstractValidator<AtualizarRendaFamiliarCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FamiliaId` | `NotEmpty()` | "Identificador da família é obrigatório." |
| `Membros` | `NotEmpty()` (≥ 1) | "A família deve ter ao menos um membro." |

> `ProcessarVigenciaCadastralCommand` não possui validador dedicado nesta versão; a proteção é por invariantes de domínio e checagem de existência no handler. As consultas não possuem validador FluentValidation.

---

## 9. Persistência (EF Core 8)

- **Schema:** `assistenciasocial` (isolado por módulo). **DbContext:** o do módulo AssistenciaSocial. **Migrations:** por módulo.
- **Tabela:** `Familia` (raiz de agregado) + `MembroFamiliar` (entidade-filha, owned/relacionada).

| Coluna (Familia) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `FamiliaId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Nis` | `nvarchar(11)` | conversor de `Nis` ↔ `string` (Value Converter). |
| `CpfResponsavel` | `nvarchar(11)` | conversor de `Cpf` ↔ `string`. |
| `Territorio` | `nvarchar(120)` | território de cobertura. |
| `UnidadeAtendimentoId` | `Guid` | CRAS de referência. |
| `RendaPerCapita` | `decimal(18,2)` | owned/conversor de `RendaPerCapita` (campo `Valor`). |
| `DataReferenciamento` | `date` | `DateOnly`. |
| `DataUltimaAtualizacaoCadastral` | `date` | `DateOnly`. |
| `Situacao` | `int` | enum `SituacaoFamilia` (persistido por valor numérico). |
| `Endereco_*` | (owned) | `EnderecoTerritorializado` como owned type (logradouro, CEP, município, território). |

| Coluna (MembroFamiliar) | Tipo lógico | Observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `MembroFamiliarId`. |
| `FamiliaId` | `Guid` (FK) | pertence à família (cascade). |
| `Cpf` | `nvarchar(11)` | conversor de `Cpf`. |
| `Parentesco` | `int` | enum `Parentesco`. |
| `DataNascimento` | `date` | `DateOnly`. |
| `RendaIndividual` | `decimal(18,2)` | owned `ValorMonetario`. |
| `EhPcd` | `bit` | dado sensível (art. 11 LGPD) — minimização. |

- **Não persistidas (calculadas):** nenhuma derivada além de `RendaPerCapita`, que é **materializada** ao recalcular.
- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, Territorio)` para `ListarPorTerritorioAsync`.
  - Índice **único** em `(TenantId, Nis)` (uma família por NIS por tenant).
  - Índice em `MembroFamiliar(FamiliaId)`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto AssistenciaSocial para `FamiliaReferenciadaIntegrationEvent` (consistência transacional com o estado).
- **Read model CadÚnico:** projeção isolada por `TenantId`; **não** mantém a base federal (somente leitura).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Familia` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas (inclusive `ObterFamiliasDoTerritorio` e `ObterResumoCadUnico`). Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Referenciamento/atualização cadastral: papéis da **equipe de referência do CRAS** (ex.: `AssistenciaSocial.Familia.Gerir`).
  - Consulta de famílias/resumo CadÚnico: papel de **leitura socioassistencial** (ex.: `AssistenciaSocial.Familia.Ler`).
  - Módulo AssistenciaSocial é **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD — dado sensível (art. 11):** NIS, CPF, composição familiar, `EhPcd` (deficiência) e presença de crianças/adolescentes. Base legal: **execução de política pública** (art. 11, II, "b" / art. 23) — **não** consentimento. Princípios de **minimização** (projeções mascaram NIS/CPF) e finalidade.
- **Isolamento federal:** dados do **CadÚnico federal NÃO trafegam entre tenants** nem para módulos não autorizados; read model isolado por `TenantId` (README §6).
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em `Referenciar`, `AtualizarRendaFamiliar`, `ProcessarVigenciaCadastral`, `RegularizarCadastro` — destinada ao controle social/Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **CadÚnico / MDS (consulta por NIS/CPF) — passiva/leitura:** folha resumo, composição familiar, renda e condicionalidades. A **base federal é a fonte autoritativa**; o módulo apenas **projeta um read model** (I-9). `HttpClient` tipado + **Polly** (retry exponencial + jitter, circuit breaker, timeout) atrás de **ACL** que traduz o contrato federal para o domínio (README §4). Idempotente; nunca sobrescreve a base federal.
- **PBF / SISC (consulta):** vínculos e condicionalidades do Bolsa Família para qualificar a composição familiar (consumido pelo agregado `Beneficio`).
- **Saída — cross-module via Contracts:** `FamiliaReferenciadaIntegrationEvent` (Outbox) disponível a módulos autorizados (ex.: Transparência — dados agregados/anonimizados). **Nunca** trafega dado sensível identificável (NIS/CPF mascarados).
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Referenciamento de família ao CRAS** (README §8)
- **Dado** uma família com NIS válido no CadÚnico e endereço dentro do território de um CRAS
- **Quando** executo `ReferenciarFamiliaCommand(nis, crasId, endereco, membros)`
- **Então** a `Familia` é vinculada à `UnidadeAtendimento`, fica em situação `Referenciada`, `RendaPerCapita` é calculada e `FamiliaReferenciada` (domínio) + `FamiliaReferenciadaIntegrationEvent` (com NIS mascarado) são publicados.

**Cenário 2 — Endereço fora do território do CRAS**
- **Dado** um CRAS cujo território **não** cobre o endereço informado
- **Quando** executo `ReferenciarFamiliaCommand`
- **Então** ocorre `InvalidOperationException("Endereço fora do território do CRAS.")` e nenhum evento é publicado (I-2).

**Cenário 3 — NIS inválido / não localizado**
- **Dado** um NIS inválido ou ausente no CadÚnico
- **Quando** executo `ReferenciarFamiliaCommand`
- **Então** ocorre `InvalidOperationException` e a família não é referenciada (I-1, I-9).

**Cenário 4 — Cálculo de renda per capita**
- **Dado** uma família com membros somando renda familiar conhecida e N membros
- **Quando** referencio/atualizo a família
- **Então** `RendaPerCapita = renda familiar ÷ N` e nunca é negativa (I-3, I-4).

**Cenário 5 — Atualização cadastral obrigatória vencida** (README §8)
- **Dado** uma família com última atualização do CadÚnico há **mais de 24 meses**
- **Quando** executo `ProcessarVigenciaCadastralCommand`
- **Então** a situação passa a `AtualizacaoVencida`, a família é sinalizada para atualização e a elegibilidade a novos benefícios fica condicionada à regularização (I-5, I-6).

**Cenário 6 — Regularização por atualização de renda/composição**
- **Dado** uma família `AtualizacaoVencida`
- **Quando** executo `AtualizarRendaFamiliarCommand` (nova composição/renda)
- **Então** `RendaPerCapita` é recalculada, `DataUltimaAtualizacaoCadastral` é renovada e a situação passa a `Regularizada` (I-7, I-12).

**Cenário 7 — Vigência ainda válida (idempotência)**
- **Dado** uma família com atualização dentro de 24 meses
- **Quando** executo `ProcessarVigenciaCadastralCommand`
- **Então** a situação permanece `Referenciada`/`Regularizada` e nenhuma transição é emitida.

**Cenário 8 — Consulta tenant-scoped por território**
- **Dado** famílias no tenant A e famílias em outro tenant B no mesmo território nominal
- **Quando** executo `ObterFamiliasDoTerritorioQuery(territorio)` no contexto do tenant A
- **Então** retornam **apenas** as famílias do tenant A, projetadas em `FamiliaResumo` com NIS mascarado.

**Cenário 9 — Read model do CadÚnico não cruza tenants**
- **Dado** uma família referenciada no tenant A
- **Quando** executo `ObterResumoCadUnicoQuery(familiaId)` no contexto do tenant B
- **Então** a consulta não retorna dados (isolamento por `TenantId`); o CadÚnico federal não trafega entre tenants (I-9, README §6).

**Cenário 10 — Unicidade de NIS por tenant**
- **Dado** uma família já referenciada com determinado NIS no tenant A
- **Quando** tento referenciar outra família com o **mesmo NIS** no tenant A
- **Então** a operação falha por índice único `(TenantId, Nis)` (I duplicado proibido).

---

## 13. Casos de Borda

- **B-1.** `Nis` nulo/vazio/inválido em `Referenciar` ⇒ `ArgumentException`/validação (I-1).
- **B-2.** `Membros` vazio ⇒ validação `NotEmpty` e/ou `InvalidOperationException` (divisão por zero proibida — I-4).
- **B-3.** Endereço com `Territorio` divergente do `TerritorioCobertura` do CRAS ⇒ `InvalidOperationException` (I-2); nenhum evento.
- **B-4.** Dois membros com `Parentesco = ResponsavelFamiliar` ⇒ rejeitado (I-11).
- **B-5.** `hoje == DataUltimaAtualizacaoCadastral + 24 meses` ⇒ **não** vencida (estritamente `hoje > marco + 24 meses` — I-5).
- **B-6.** `ProcessarVigenciaCadastral` sobre família já `AtualizacaoVencida` ⇒ idempotente (sem reemitir transição).
- **B-7.** `AtualizarRendaFamiliar` que reduz membros a 0 ⇒ rejeitado (I-4).
- **B-8.** Tentativa de gravar/alterar dado do CadÚnico federal pelo agregado ⇒ não existe comando que o faça (I-9); apenas read model.
- **B-9.** Renda familiar total = 0 ⇒ `RendaPerCapita = 0` (válida, não negativa) — família tipicamente apta a benefícios por baixa renda.
- **B-10.** Mudança de endereço para outro território ⇒ exige novo referenciamento/atualização imediata do cadastro (I-7); não silenciosamente migra de CRAS.
- **B-11.** Consulta `ObterFamiliasDoTerritorio` deve expor **NIS mascarado** — projeção bruta de NIS é proibida (minimização, LGPD art. 11).
- **B-12.** `FamiliaReferenciadaIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo AssistenciaSocial (§§1-9). Referenciamento ao CRAS, cálculo de renda per capita, vigência cadastral (24 meses), read model CadÚnico (somente leitura, autoritativa federal) e isolamento multi-tenant. |

<!-- manifest
commands: ReferenciarFamilia, AtualizarRendaFamiliar, ProcessarVigenciaCadastral
queries: ObterFamiliasDoTerritorio, ObterResumoCadUnico
domainEvents: FamiliaReferenciada
integrationEventsPublished: FamiliaReferenciadaIntegrationEvent
integrationEventsConsumed: 
-->
