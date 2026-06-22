---
modulo: Protocolo
agregado: Documento
contexto: Protocolo (Processo Administrativo Eletrônico — PAE — paperless; trilha documental)
poder: Ambos
schema: protocolo
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 14.063/2020 (assinaturas SIMPLES/AVANÇADA/QUALIFICADA)", "Decreto 10.543/2020 (uso por criticidade)", "MP 2.200-2/2001 (ICP-Brasil)", "Lei 11.419/2006 (processo eletrônico)", "CONARQ (e-ARQ Brasil/SIGAD, PDF/A)", "LGPD"]
---

# Documento — Regras-as-Code (Rules-as-Code)

> Peça processual (nato-digital ou digitalizada) em **PDF/A**, com **hash SHA-256** e
> **carimbo de tempo**, assinada por nível de criticidade conforme a **Lei 14.063/2020** e o
> **Decreto 10.543/2020** (SIMPLES / AVANÇADA / QUALIFICADA — ICP-Brasil). É **imutável após a
> juntada**: nunca excluída, apenas tornada *sem efeito*, preservando a trilha documental.
> Este arquivo é **normativo e versionado**; o código (`Documento.cs`, handlers, validators, EF
> config, testes) é consequência dele. Marco: **Lei 14.063/2020**, **Decreto 10.543/2020**,
> **MP 2.200-2/2001** (ICP-Brasil), **Lei 11.419/2006** e **CONARQ** (e-ARQ Brasil/SIGAD, PDF/A).

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Documento (`Documento`) | Peça processual (nato-digital ou digitalizada), em PDF/A. Raiz de agregado. |
| Juntada (`Juntar` / `DocumentoJuntado`) | Inserção do documento ao processo (ato que o torna imutável). |
| Hash (`Hash` : `Hash`) | Resumo criptográfico SHA-256 do conteúdo do documento (integridade). |
| Carimbo de Tempo (`CarimboTempo` : `CarimboDeTempo`) | Atestação temporal confiável da assinatura. |
| Assinatura (`Assinatura` : `Assinatura`) | Ato de assinar o documento, com `Tipo` (simples/avançada/qualificada). |
| Assinatura Qualificada (`AssinaturaQualificada`) | ICP-Brasil (certificado e-CPF/e-CNPJ) — MP 2.200-2/2001. |
| Assinatura Avançada (`AssinaturaAvancada`) | Provedor credenciado com vínculo do signatário (Lei 14.063/2020). |
| Assinatura Simples (`AssinaturaSimples`) | Identificação por meio eletrônico (gov.br bronze). |
| Tipo de Assinatura (`TipoAssinatura` : `TipoAssinatura`) | Nível exigido por criticidade (Decreto 10.543/2020). |
| Sem Efeito (`TornarSemEfeito` / `SemEfeito`) | Estado que invalida o documento juntado **sem excluí-lo** (preserva a trilha). |
| Criticidade (`Criticidade` : `CriticidadeAto`) | Grau do ato que determina o nível mínimo de assinatura. |
| Nível de Acesso (`NivelAcesso` : `NivelDeAcesso`) | Visibilidade do documento: público / restrito / sigiloso. |
| Processo (`ProcessoId`) | Processo ao qual o documento é juntado. |
| Signatário (`SignatarioId`) | Sujeito que assina o documento. |
| Tenant (`TenantId`) | Ente público (Prefeitura ou Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoDocumento`) | Estado atual do documento no ciclo de vida. |

---

## 2. Modelo

- **Identidade:** `DocumentoId` — `readonly record struct DocumentoId(Guid Value)`; fábrica `DocumentoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Documento : AggregateRoot<DocumentoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Criar(...)`; torna-se imutável ao `Juntar(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `ProcessoId` | `Guid?` | Processo ao qual está juntado (nulo antes da juntada). | `private set` |
| `Hash` | `Hash` (VO) | SHA-256 do conteúdo — integridade. | `private set` (imutável após juntada) |
| `CarimboTempo` | `CarimboDeTempo?` (VO) | Carimbo de tempo da assinatura (nulo antes de assinar). | `private set` |
| `Criticidade` | `CriticidadeAto` (enum) | Criticidade do ato — determina o nível mínimo de assinatura. | `private set` |
| `NivelAcesso` | `NivelDeAcesso` (enum) | Visibilidade do documento. | `private set` |
| `FormatoPdfA` | `bool` | Indica conformidade com PDF/A (e-ARQ Brasil). | `private set` |
| `SignatarioId` | `Guid?` | Signatário (nulo antes da assinatura). | `private set` |
| `TipoAssinatura` | `TipoAssinatura?` | Tipo da assinatura aplicada (nulo antes de assinar). | `private set` |
| `DataJuntada` | `DateOnly?` | Data da juntada ao processo (nula antes). | `private set` |
| `Situacao` | `SituacaoDocumento` | Situação atual. | `private set` |

### Constantes / Mapa de criticidade (Decreto 10.543/2020)

- `NivelMinimoPorCriticidade` — mapeamento de domínio (não *hardcoded* em fluxo): `Alta → AssinaturaQualificada`, `Media → AssinaturaAvancada`, `Baixa → AssinaturaSimples`.

### Value Objects (referenciados)

- `Hash` — `record` com algoritmo SHA-256 (`Valor : string` hex de 64 caracteres); validado na criação.
- `CarimboDeTempo` — `record` com instante atestado e autoridade de carimbo (atestação temporal confiável).
- `Assinatura` — `record` com `Tipo : TipoAssinatura`, `SignatarioId : Guid` e `CarimboTempo : CarimboDeTempo`.

### Enum `TipoAssinatura`

| Valor | Numérico | Descrição |
|---|---|---|
| `AssinaturaSimples` | 1 | Identificação por meio eletrônico (gov.br bronze). |
| `AssinaturaAvancada` | 2 | Provedor credenciado com vínculo do signatário (gov.br prata; ouro). |
| `AssinaturaQualificada` | 3 | ICP-Brasil (e-CPF/e-CNPJ) — MP 2.200-2/2001. |

### Enum `CriticidadeAto`

| Valor | Numérico | Descrição | Nível mínimo de assinatura |
|---|---|---|---|
| `Baixa` | 1 | Requerimento do cidadão. | `AssinaturaSimples` |
| `Media` | 2 | Ato interno de média criticidade. | `AssinaturaAvancada` |
| `Alta` | 3 | Ato do dirigente máximo / bens imóveis. | `AssinaturaQualificada` |

### Enum `SituacaoDocumento`

| Valor | Numérico | Descrição |
|---|---|---|
| `Rascunho` | 1 | Criado, ainda não juntado (mutável). |
| `Juntado` | 2 | Juntado ao processo (imutável). |
| `Assinado` | 3 | Juntado e assinado conforme criticidade. |
| `SemEfeito` | 4 | Tornado sem efeito (terminal); permanece na trilha. |

> **Conjuntos de referência usados nas guardas:**
> - **Imutável** = { `Juntado`, `Assinado`, `SemEfeito` } (não admite exclusão nem alteração de conteúdo/hash).
> - **Encerrado** = { `SemEfeito` } (não admite novas transições).
> - **Juntado/válido** (admite assinar / tornar sem efeito) = { `Juntado`, `Assinado` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A criação exige `Hash` (SHA-256) não nulo/não vazio e `FormatoPdfA == true` (e-ARQ Brasil, PDF/A); caso contrário, `ArgumentException`/`InvalidOperationException`.
- **I-2.** A juntada vincula o documento a um `ProcessoId`, grava `DataJuntada`, passa a `Juntado` e emite `DocumentoJuntado(id, processoId, hash)`.
- **I-3.** **Documento juntado é imutável**: uma vez em situação ∈ {`Juntado`, `Assinado`, `SemEfeito`}, **não pode ser excluído nem ter conteúdo/hash alterado** (`InvalidOperationException`).
- **I-4.** A exclusão de documento juntado é **proibida**; a única operação permitida é `TornarSemEfeito`, que preserva a trilha (Lei 11.419/2006; e-ARQ Brasil).
- **I-5.** **Assinatura por criticidade** (Decreto 10.543/2020): o `TipoAssinatura` aplicado deve ser **≥** ao nível mínimo da `Criticidade` (`NivelMinimoPorCriticidade`); assinatura de nível inferior é **rejeitada** (`InvalidOperationException`).
- **I-6.** Ato de criticidade `Alta` (dirigente máximo / bens imóveis) exige **AssinaturaQualificada (ICP-Brasil)**; tentativa de assinatura simples/avançada é rejeitada (I-5).
- **I-7.** A assinatura só ocorre sobre documento **juntado/válido** ({`Juntado`, `Assinado`}); ao assinar, grava `SignatarioId`, `TipoAssinatura`, `CarimboTempo` e passa a `Assinado`. Documento `SemEfeito` não pode ser assinado.
- **I-8.** Toda assinatura possui **carimbo de tempo** (`CarimboDeTempo`) confiável (atestação temporal); sem carimbo, a assinatura é inválida.
- **I-9.** `TornarSemEfeito` só ocorre sobre documento ∈ {`Juntado`, `Assinado`}; passa a `SemEfeito` (terminal) e exige motivo; **não** remove o registro.
- **I-10.** Estado terminal (`SemEfeito`) não admite novas transições (juntar/assinar/tornar sem efeito novamente).
- **I-11.** Documento com `NivelAcesso = Sigiloso` segue o mesmo controle do processo: nega acesso não autorizado e **audita a tentativa**.
- **I-12.** O `Hash` SHA-256 garante a integridade: divergência entre o hash armazenado e o conteúdo recalculado invalida a verificação de integridade.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Criar` | `Rascunho` | `Hash` não vazio && `FormatoPdfA == true` | — |
| `Rascunho` | `Juntar` | `Juntado` | `ProcessoId` informado | `DocumentoJuntado` |
| `Juntado` \| `Assinado` | `Assinar` | `Assinado` | `TipoAssinatura` ≥ nível mínimo da `Criticidade`; carimbo de tempo presente | `DocumentoAssinado` |
| `Juntado` \| `Assinado` | `TornarSemEfeito` | `SemEfeito` | motivo informado; situação ∈ {`Juntado`,`Assinado`} | — |

> Observações:
> - `Juntar` torna o documento **imutável** (conteúdo/hash); a partir daí, qualquer tentativa de exclusão é negada (I-3/I-4).
> - `Assinar` chama `GarantirNivelAdequado(criticidade, tipo)` **antes** de gravar a assinatura; rejeita nível inferior ao exigido (I-5/I-6).
> - **Não há comando de exclusão** no agregado: a "remoção" lógica é exclusivamente `TornarSemEfeito` (preserva a trilha).
> - Múltiplas assinaturas são permitidas a partir de `Assinado` (re-assinatura/coassinatura), desde que respeitem o nível mínimo; cada uma emite `DocumentoAssinado`.

---

## 5. Comandos (escrita)

### 5.1 JuntarDocumento

- **Command:** `JuntarDocumentoCommand(Guid ProcessoId, string Hash, CriticidadeAto Criticidade, NivelDeAcesso NivelAcesso, bool FormatoPdfA) : ICommand<Guid>`.
- **Entrada (DTO):** `ProcessoId`, `Hash` (SHA-256 do conteúdo), `Criticidade`, `NivelAcesso`, `FormatoPdfA`.
- **Dependências do handler:** `IDocumentoRepository`, `IProcessoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Processo destino existe (`IProcessoRepository.ObterPorIdAsync`), senão `InvalidOperationException("Processo não encontrado.")`.
  - `FormatoPdfA == true` (I-1); `Hash` não vazio.
- **Efeito:** calcula `hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`; cria via `Documento.Criar(tenant.TenantId, new Hash(request.Hash), request.Criticidade, request.NivelAcesso, request.FormatoPdfA)`; `documento.Juntar(request.ProcessoId, hoje)`; `documentos.Adicionar(documento)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Documento` em situação `Juntado`, vinculado ao processo, imutável; retorna `documento.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (hash vazio); `InvalidOperationException` (processo inexistente ou não PDF/A).
- **Evento de domínio:** `DocumentoJuntado(id, processoId, hash)`.

### 5.2 AssinarDocumento

- **Command:** `AssinarDocumentoCommand(Guid DocumentoId, Guid SignatarioId, TipoAssinatura Tipo) : ICommand`.
- **Entrada (DTO):** `DocumentoId`, `SignatarioId`, `Tipo` (nível de assinatura).
- **Dependências do handler:** `IDocumentoRepository`, `ICarimboDeTempoService` (autoridade de carimbo de tempo), `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Documento existe (`ObterPorIdAsync`), senão `InvalidOperationException("Documento não encontrado.")`.
  - Situação ∈ {`Juntado`, `Assinado`} (I-7).
  - `Tipo` ≥ nível mínimo da `Criticidade` (I-5/I-6).
- **Efeito:** obtém `carimbo = carimboService.Gerar(...)`; `documento.Assinar(request.SignatarioId, request.Tipo, carimbo)`; `SaveChangesAsync`; publica **Integration Event** `DocumentoAssinadoIntegrationEvent` via Outbox.
- **Pós-condições:** `SignatarioId`/`TipoAssinatura`/`CarimboTempo` preenchidos; situação `Assinado`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, situação inválida, sem efeito, ou nível de assinatura insuficiente).
- **Evento de domínio:** `DocumentoAssinado(Id, signatarioId, tipo)`.
- **Evento de integração (publica):** `DocumentoAssinadoIntegrationEvent`.

### 5.3 TornarDocumentoSemEfeito

- **Command:** `TornarDocumentoSemEfeitoCommand(Guid DocumentoId, string Motivo) : ICommand`.
- **Entrada (DTO):** `DocumentoId`, `Motivo`.
- **Dependências do handler:** `IDocumentoRepository`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo.
  - Documento existe, senão `InvalidOperationException("Documento não encontrado.")`.
  - Situação ∈ {`Juntado`, `Assinado`} (I-9).
- **Efeito:** `documento.TornarSemEfeito(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `SemEfeito`; registro **preservado** (não excluído).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (motivo vazio); `InvalidOperationException` (não encontrado ou já sem efeito).
- **Evento de domínio:** — (transição registrada na trilha; sem evento próprio nesta versão).

> **Não há comando de exclusão.** Qualquer pedido de "exclusão" de documento juntado é negado por design (I-4); a operação permitida é exclusivamente `TornarDocumentoSemEfeito`.

---

## 6. Consultas (leitura)

### 6.1 ListarDocumentosDoProcesso

- **Query:** `ListarDocumentosDoProcessoQuery(Guid ProcessoId) : IQuery<IReadOnlyList<DocumentoResumo>>`.
- **Entrada:** `ProcessoId`.
- **Handler:** `ListarDocumentosDoProcessoHandler(IDocumentoRepository documentos)`; chama `documentos.ListarPorProcessoAsync(request.ProcessoId, ct)`.
- **Projeção (DTO):** `DocumentoResumo(Guid Id, string Hash, string Criticidade, string Situacao, string? TipoAssinatura, DateOnly? DataJuntada)`.
  - `Criticidade`/`Situacao`/`TipoAssinatura` projetados de `ToString()`.
- **Filtros:** por `ProcessoId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`. Respeita `NivelDeAcesso`: documentos `Sigilosos` omitidos para não autorizados (com auditoria) (I-11).
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 VerificarIntegridadeDocumento

- **Query:** `VerificarIntegridadeDocumentoQuery(Guid DocumentoId, string HashRecalculado) : IQuery<bool>`.
- **Entrada:** `DocumentoId`, `HashRecalculado` (SHA-256 recalculado do conteúdo).
- **Handler:** `VerificarIntegridadeDocumentoHandler(IDocumentoRepository documentos)`; obtém o documento e compara `documento.Hash.Valor == request.HashRecalculado` (I-12).
- **Projeção (DTO):** `bool` (íntegro / não íntegro).
- **Filtros:** por `DocumentoId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`); documento existente (senão integridade indeterminada/`false`).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Protocolo.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `DocumentoJuntado` | `(DocumentoId, Guid ProcessoId, Hash)` | `Documento.Juntar` |
| `DocumentoAssinado` | `(DocumentoId, Guid SignatarioId, TipoAssinatura Tipo)` | `Documento.Assinar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Protocolo.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `DocumentoAssinadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid DocumentoId, Guid ProcessoId, string TipoAssinatura)` | `AssinarDocumentoHandler` (informa módulos originadores que o ato foi assinado) |

### Integração (consome via `*.Contracts`)

| Evento consumido | Origem | Ação |
|---|---|---|
| `JuntarDocumentoRequested` | Licitações / RH / Licenças | Junta um `Documento` ao processo indicado (PDF/A + hash). |
| `SolicitarAssinaturaRequested` | Outros módulos | Aciona a assinatura por criticidade do documento; rejeita nível insuficiente. |

---

## 8. Validações (FluentValidation)

### JuntarDocumentoValidator (`AbstractValidator<JuntarDocumentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProcessoId` | `NotEmpty()` | (padrão: processo de destino obrigatório) |
| `Hash` | `NotEmpty()` + `Length(64)` | (padrão: hash SHA-256 obrigatório, 64 caracteres hexadecimais) |
| `Criticidade` | `IsInEnum()` | (padrão: criticidade inválida) |
| `NivelAcesso` | `IsInEnum()` | (padrão: nível de acesso inválido) |
| `FormatoPdfA` | `Equal(true)` | (padrão: documento deve estar em PDF/A) |

### AssinarDocumentoValidator (`AbstractValidator<AssinarDocumentoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `DocumentoId` | `NotEmpty()` | (padrão: identificador do documento obrigatório) |
| `SignatarioId` | `NotEmpty()` | (padrão: signatário obrigatório) |
| `Tipo` | `IsInEnum()` | (padrão: tipo de assinatura inválido) |

### TornarDocumentoSemEfeitoValidator (`AbstractValidator<TornarDocumentoSemEfeitoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `DocumentoId` | `NotEmpty()` | (padrão: identificador do documento obrigatório) |
| `Motivo` | `NotEmpty()` + `MaximumLength(500)` | (padrão: motivo obrigatório, máx. 500 caracteres) |

> A adequação do nível de assinatura à criticidade (I-5/I-6) é invariante de **domínio** (não apenas validação de entrada). As queries não possuem validador dedicado (apenas `ArgumentNullException.ThrowIfNull` no handler).

---

## 9. Persistência (EF Core 8)

- **Schema:** `protocolo` (isolado por módulo). **DbContext:** o do módulo Protocolo. **Migrations:** por módulo.
- **Tabela:** `Documento` (raiz de agregado).

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `DocumentoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `ProcessoId` | `Guid?` | processo vinculado; nulável antes da juntada. |
| `Hash` | `nvarchar(64)` | conversor/owned de `Hash` (SHA-256, 64 hex); imutável após juntada. |
| `CarimboTempoInstante` | `datetime2?` | parte do VO `CarimboDeTempo`; nulável antes da assinatura. |
| `CarimboTempoAutoridade` | `nvarchar(120)?` | autoridade do carimbo de tempo; nulável. |
| `Criticidade` | `int` | enum `CriticidadeAto` (persistido por valor numérico). |
| `NivelAcesso` | `int` | enum `NivelDeAcesso` (persistido por valor numérico). |
| `FormatoPdfA` | `bit` | conformidade PDF/A. |
| `SignatarioId` | `Guid?` | signatário; nulável antes da assinatura. |
| `TipoAssinatura` | `int?` | enum `TipoAssinatura`; nulável antes da assinatura. |
| `DataJuntada` | `date?` | `DateOnly`; nulável antes da juntada. |
| `Situacao` | `int` | enum `SituacaoDocumento` (persistido por valor numérico). |

- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, ProcessoId)` para `ListarPorProcessoAsync`.
  - Índice em `(TenantId, Hash)` para verificação de integridade/deduplicação por tenant.
- **Imutabilidade:** após `Juntado`, a configuração do agregado impede alteração de conteúdo/hash; a exclusão física é vedada (apenas transição para `SemEfeito`).
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto Protocolo para `DocumentoAssinadoIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Documento` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas (`ListarDocumentosDoProcesso`, `VerificarIntegridadeDocumento`). Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição. Executivo e Legislativo do mesmo município são tenants **distintos**.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Juntar/assinar/tornar sem efeito: papéis do **setor/autoridade competente** (ex.: `Protocolo.Documento.Gerir`); assinatura `Qualificada` restrita a quem possui certificado ICP-Brasil.
  - Consulta/verificação: papel de **leitura** (ex.: `Protocolo.Documento.Ler`); documentos `Sigilosos` exigem papel adicional (ex.: `Protocolo.Documento.Sigiloso`).
  - Módulo Protocolo é **ativável por tenant** (Ambos os poderes); requisição a tenant sem licença → 404/403 auditado.
- **Certificados A1 (.pfx) / ICP-Brasil:** quando aplicável à assinatura **Qualificada**, os certificados ficam no **Azure Key Vault**, por tenant (nunca no repositório).
- **NivelDeAcesso:** `Publico` / `Restrito` / `Sigiloso` controla visibilidade. Tentativa de acesso a documento **sigiloso** por usuário não autorizado é **negada** e **registrada em auditoria imutável** (I-11).
- **LGPD:** documentos podem conter dados pessoais/sensíveis. Acesso a documento restrito/sigiloso gera **trilha de acesso** (quem leu, quando, por quê); minimização aplicada na projeção `DocumentoResumo`.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Criar`, `Juntar`, `Assinar`, `TornarSemEfeito`) — destinada ao Tribunal de Contas (TCE-RS). A imutabilidade do documento juntado e o "sem efeito" preservam a trilha documental **append-only**.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Assinatura eletrônica (Lei 14.063/2020 + Decreto 10.543/2020):**
  - **gov.br** — bronze → `AssinaturaSimples`; prata → `AssinaturaAvancada`; ouro → `AssinaturaAvancada`/`AssinaturaQualificada`.
  - **ICP-Brasil** (MP 2.200-2/2001) — `AssinaturaQualificada` (e-CPF/e-CNPJ), via certificado A1 do Key Vault.
  - **Provedores de assinatura avançada** credenciados — `AssinaturaAvancada`.
  - Acionada por comando local ou por `SolicitarAssinaturaRequested` de outro módulo; ACL + Polly (retry + circuit breaker) + idempotência por `EventId`.
- **Carimbo de tempo:** autoridade de carimbo de tempo confiável (`ICarimboDeTempoService`) atesta o instante da assinatura (`CarimboDeTempo`); I/O externa idempotente e resiliente.
- **GED/SIGAD (e-ARQ Brasil):** armazenamento em **PDF/A**; o documento juntado integra o Sistema Informatizado de Gestão Arquivística de Documentos (SIGAD) aderente ao e-ARQ Brasil (CONARQ).
- **Saída — módulos originadores (intra-aplicação, via Contracts):** após assinatura, publica `DocumentoAssinadoIntegrationEvent` para que Licitações/RH/Licenças saibam que o ato foi assinado. Nenhum módulo acessa as tabelas do Protocolo diretamente.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Juntada de documento PDF/A**
- **Dado** um documento nato-digital em PDF/A com hash SHA-256 válido e um processo existente
- **Quando** executo `JuntarDocumentoCommand(processoId, hash, CriticidadeAto.Baixa, NivelDeAcesso.Publico, true)`
- **Então** um `Documento` é criado em situação `Juntado`, vinculado ao processo, e o evento `DocumentoJuntado` é emitido.

**Cenário 2 — Documento não-PDF/A é rejeitado**
- **Dado** um documento com `FormatoPdfA = false`
- **Quando** executo `JuntarDocumentoCommand(..., FormatoPdfA: false)`
- **Então** a validação/domínio rejeita (documento deve estar em PDF/A) (I-1).

**Cenário 3 — Imutabilidade de documento juntado**
- **Dado** um `Documento` já juntado a um `Processo`
- **Quando** solicita-se a exclusão
- **Então** a operação é negada e só é permitido `TornarDocumentoSemEfeito`, preservando a trilha (I-3/I-4).

**Cenário 4 — Tornar sem efeito preserva o registro**
- **Dado** um `Documento` em situação `Juntado`
- **Quando** executo `TornarDocumentoSemEfeitoCommand(documentoId, motivo)`
- **Então** a situação passa a `SemEfeito`, o registro **permanece** na trilha e nenhuma linha é excluída.

**Cenário 5 — Assinatura por criticidade (rejeição)**
- **Dado** um `Documento` que é ato do dirigente máximo sobre bem imóvel (`Criticidade = Alta`)
- **Quando** a assinatura **simples** é tentada (`AssinarDocumentoCommand(..., Tipo: AssinaturaSimples)`)
- **Então** o sistema exige **AssinaturaQualificada (ICP-Brasil)** e **rejeita** a simples (`InvalidOperationException`) (I-5/I-6).

**Cenário 6 — Assinatura qualificada válida**
- **Dado** um `Documento` `Juntado` com `Criticidade = Alta`
- **Quando** executo `AssinarDocumentoCommand(documentoId, signatarioId, AssinaturaQualificada)`
- **Então** o documento passa a `Assinado` com `CarimboDeTempo`, e `DocumentoAssinado` + `DocumentoAssinadoIntegrationEvent` são publicados.

**Cenário 7 — Assinatura de média criticidade**
- **Dado** um `Documento` `Juntado` com `Criticidade = Media`
- **Quando** executo `AssinarDocumentoCommand(..., AssinaturaAvancada)`
- **Então** o documento passa a `Assinado` (nível ≥ mínimo exigido).

**Cenário 8 — Assinatura de documento sem efeito**
- **Dado** um `Documento` em situação `SemEfeito`
- **Quando** executo `AssinarDocumentoCommand`
- **Então** ocorre `InvalidOperationException` (documento sem efeito não pode ser assinado) (I-7).

**Cenário 9 — Carimbo de tempo obrigatório**
- **Dado** um `Documento` `Juntado`
- **Quando** a assinatura é aplicada
- **Então** a `Assinatura` carrega um `CarimboDeTempo` confiável; sem carimbo, a assinatura é inválida (I-8).

**Cenário 10 — Acesso a documento sigiloso**
- **Dado** um `Documento` com `NivelDeAcesso = Sigiloso`
- **Quando** usuário sem autorização tenta listá-lo/visualizá-lo
- **Então** o acesso é negado (omitido/403) e a tentativa é registrada em auditoria imutável (I-11).

**Cenário 11 — Verificação de integridade (SHA-256)**
- **Dado** um `Documento` juntado com hash conhecido
- **Quando** executo `VerificarIntegridadeDocumentoQuery(documentoId, hashRecalculado)` com `hashRecalculado` divergente
- **Então** o resultado é `false` (integridade comprometida); se coincidir, `true` (I-12).

**Cenário 12 — Consulta tenant-scoped**
- **Dado** documentos de um processo no tenant A e documentos de outro tenant B
- **Quando** executo `ListarDocumentosDoProcessoQuery(processoId)` no contexto do tenant A
- **Então** retornam **apenas** os documentos do tenant A, projetados em `DocumentoResumo`.

**Cenário 13 — Juntada solicitada por outro módulo**
- **Dado** o módulo Licitações publica `JuntarDocumentoRequested(processoId, hash, ...)`
- **Quando** o Protocolo processa o Integration Event
- **Então** junta o `Documento` ao processo (PDF/A + hash) de forma idempotente por `EventId`.

---

## 13. Casos de Borda

- **B-1.** `Hash` nulo/vazio ou comprimento ≠ 64 em `Criar`/`Juntar` ⇒ `ArgumentException` / rejeição do validator (I-1).
- **B-2.** `FormatoPdfA = false` ⇒ rejeitado (documento deve estar em PDF/A) (I-1).
- **B-3.** Exclusão de documento `Juntado`/`Assinado` ⇒ **negada por design** (não há comando de exclusão) (I-3/I-4).
- **B-4.** `TornarSemEfeito` sobre documento já `SemEfeito` ⇒ falha (estado terminal) (I-10).
- **B-5.** `TornarSemEfeito` sobre `Rascunho` ⇒ falha (só ∈ {`Juntado`,`Assinado`}) (I-9).
- **B-6.** Assinatura de nível inferior à criticidade (ex.: `AssinaturaAvancada` em `Criticidade = Alta`) ⇒ rejeitada (I-5/I-6).
- **B-7.** Assinatura de nível superior ao mínimo (ex.: `AssinaturaQualificada` em `Criticidade = Baixa`) ⇒ permitida (atende o mínimo).
- **B-8.** Assinar documento `Rascunho` (não juntado) ⇒ falha (assinatura só sobre `Juntado`/`Assinado`) (I-7).
- **B-9.** Re-assinatura/coassinatura de documento já `Assinado` ⇒ permitida se respeitar o nível mínimo; cada assinatura emite `DocumentoAssinado` (mantém-se `Assinado`).
- **B-10.** Carimbo de tempo indisponível (falha da autoridade) ⇒ assinatura não conclui; I/O resiliente (Polly), idempotente; nenhum estado parcial persistido (I-8).
- **B-11.** Acesso a documento `Sigiloso` por papel não autorizado ⇒ negado **e** auditado (I-11), inclusive em listagem ampla.
- **B-12.** `JuntarDocumentoRequested`/`SolicitarAssinaturaRequested` reentregues via Outbox ⇒ idempotentes por `EventId`; sem duplicar juntada/assinatura.
- **B-13.** Verificação de integridade de documento inexistente ⇒ resultado `false`/indeterminado (não vaza existência cross-tenant) (I-12).
- **B-14.** Hash duplicado no mesmo tenant (mesmo conteúdo rejuntado) ⇒ detectável pelo índice `(TenantId, Hash)`; política de deduplicação aplicada pelo handler de juntada por Integration Event.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — regras do agregado `Documento` derivadas do README do módulo Protocolo: juntada (PDF/A + hash SHA-256), imutabilidade ("sem efeito"), assinatura por criticidade (Lei 14.063/2020 + Decreto 10.543/2020 — simples/avançada/qualificada ICP-Brasil), carimbo de tempo, níveis de acesso e juntada/assinatura cross-module via Integration Events. |

<!-- manifest
commands: JuntarDocumento, AssinarDocumento, TornarDocumentoSemEfeito
queries: ListarDocumentosDoProcesso, VerificarIntegridadeDocumento
domainEvents: DocumentoJuntado, DocumentoAssinado
integrationEventsPublished: DocumentoAssinadoIntegrationEvent
integrationEventsConsumed: JuntarDocumentoRequested, SolicitarAssinaturaRequested
-->
