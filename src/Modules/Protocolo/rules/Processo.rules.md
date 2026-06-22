---
modulo: Protocolo
agregado: Processo
contexto: Protocolo (Processo Administrativo Eletrônico — PAE — paperless)
poder: Ambos
schema: protocolo
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 9.784/1999 (processo administrativo; art. 66 prazos)", "Lei 11.419/2006 (processo eletrônico)", "Decreto 8.539/2015 (SEI/PAE federal)", "CONARQ (TTD, e-ARQ Brasil/SIGAD)", "LGPD"]
---

# Processo — Regras-as-Code (Rules-as-Code)

> Ciclo de vida do **Processo Administrativo Eletrônico (PAE)**: autuação (geração do NUP),
> tramitação entre setores, despacho, sobrestamento e arquivamento conforme a Tabela de
> Temporalidade e Destinação (CONARQ). Bounded Context **cross-cutting**: demais módulos
> (Licitações, RH/Férias, Licenças) **não** implementam protocolo próprio — solicitam autuação,
> juntada e assinatura via Integration Events. Este arquivo é **normativo e versionado**; o
> código (`Processo.cs`, handlers, validators, EF config, testes) é consequência dele.
> Marco legal: **Lei 9.784/1999** (art. 66 — prazos excluem o dia inicial e incluem o final;
> decisão em regra 30 dias prorrogáveis), **Lei 11.419/2006**, **Decreto 8.539/2015** e **CONARQ**.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Processo Administrativo (`Processo`) | Conjunto ordenado de documentos com finalidade, regido pela Lei 9.784/1999. Raiz de agregado. |
| Protocolo (`Protocolo`) | Registro formal de entrada/saída de documento ou requerimento. |
| Autuação (`Autuar` / `ProcessoAutuado`) | Ato de formar o processo, gerando o NUP. |
| NUP — Número Único de Protocolo (`Nup` / `NumeroUnicoProtocolo`) | Identificador imutável e único do processo após a autuação. |
| Classificação (`Classificacao`) | Classe documental do processo (vincula a Tabela de Temporalidade). |
| Nível de Acesso (`NivelAcesso` : `NivelDeAcesso`) | Visibilidade: público / restrito / sigiloso. |
| Prazo (`Prazo` : `Prazo`) | Prazo legal/administrativo do processo (art. 66, Lei 9.784/1999). |
| Despacho (`Despacho` / `Despachar`) | Manifestação/decisão de autoridade no processo (entidade interna). |
| Movimentação (`Movimentacao`) | Registro de tramitação entre setores/responsáveis (entidade interna). |
| Tramitação (`Tramitar` / `ProcessoTramitado`) | Movimentação do processo entre setores/responsáveis. |
| Distribuição (`Distribuir`) | Atribuição inicial do processo a um relator/setor. |
| Sobrestamento (`Sobrestar` / `ProcessoSobrestado`) | Suspensão temporária do andamento. |
| Arquivamento (`Arquivar` / `ProcessoArquivado`) | Encerramento; guarda conforme TTD. |
| Capa do Processo (`CapaDoProcesso`) | Metadados consolidados (NUP, partes, classificação). |
| Setor Atual (`SetorAtualId`) | Setor/unidade responsável pelo processo no momento. |
| Origem (`OrigemModulo` / `OrigemId`) | Módulo originador (ex.: Licitação, RH) e identificador da origem, quando autuado por Integration Event. |
| Tenant (`TenantId`) | Ente público (Prefeitura ou Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoProcesso`) | Estado atual do processo no ciclo de vida do PAE. |

---

## 2. Modelo

- **Identidade:** `ProcessoId` — `readonly record struct ProcessoId(Guid Value)`; fábrica `ProcessoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Processo : AggregateRoot<ProcessoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Autuar(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Nup` | `Nup` (VO) | Número Único de Protocolo — imutável após a autuação. | `private set` (única atribuição na autuação) |
| `Classificacao` | `Classificacao` (VO) | Classe documental (vincula à TTD). | `private set` |
| `NivelAcesso` | `NivelDeAcesso` (enum) | Visibilidade do processo. | `private set` |
| `Prazo` | `Prazo` (VO) | Prazo legal/administrativo (art. 66). | `private set` |
| `SetorAtualId` | `Guid?` | Setor/unidade atualmente responsável. | `private set` |
| `OrigemModulo` | `string?` | Módulo originador (ex.: "Licitacao", "RH"); nulo quando autuado pelo próprio Protocolo. | `private set` |
| `OrigemId` | `Guid?` | Identificador da origem no módulo originador. | `private set` |
| `RequerimentoId` | `Guid?` | Requerimento que originou o processo (quando aplicável). | `private set` |
| `DataAutuacao` | `DateOnly` | Data da autuação do processo. | `private set` |
| `Situacao` | `SituacaoProcesso` | Situação atual. | `private set` |
| `Despachos` | `IReadOnlyList<Despacho>` | Despachos do processo (entidade interna, append-only). | coleção interna |
| `Movimentacoes` | `IReadOnlyList<Movimentacao>` | Tramitações do processo (entidade interna, append-only). | coleção interna |

### Constantes

- `PrazoDecisaoPadraoDias` = `30` (Lei 9.784/1999, decisão em regra 30 dias, prorrogáveis). Constante de domínio que parametriza o prazo padrão de decisão.

### Value Objects (referenciados)

- `Nup` — `record` com o Número Único de Protocolo (`Valor : string`); único e imutável por tenant. Validado na autuação.
- `Classificacao` — `record` com a classe documental (vincula à `TabelaTemporalidade`).
- `Prazo` — `record` com data de início e data final; o cálculo **exclui o dia inicial e inclui o final** (art. 66, Lei 9.784/1999).

### Enum `NivelDeAcesso`

| Valor | Numérico | Descrição |
|---|---|---|
| `Publico` | 1 | Acesso público (LAI). |
| `Restrito` | 2 | Acesso restrito a perfis autorizados. |
| `Sigiloso` | 3 | Sigiloso — nega acesso não autorizado e audita a tentativa. |

### Enum `SituacaoProcesso`

| Valor | Numérico | Descrição |
|---|---|---|
| `Autuado` | 1 | Autuado (estado inicial após geração do NUP). |
| `EmTramitacao` | 2 | Em tramitação entre setores/responsáveis. |
| `Sobrestado` | 3 | Sobrestado (andamento temporariamente suspenso). |
| `Arquivado` | 4 | Arquivado (terminal); guarda conforme TTD. |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Arquivado` }.
> - **Em andamento** (admite tramitação/despacho/sobrestamento) = { `Autuado`, `EmTramitacao` }.
> - **Suspensa** = { `Sobrestado` } (não tramita até reativação).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** O **NUP é único e imutável** após a autuação; uma vez atribuído, `Nup` não pode ser alterado (Decreto 8.539/2015).
- **I-2.** Na autuação, a situação inicial é `Autuado`, o NUP é gerado e é emitido o evento `ProcessoAutuado(id, nup, origemModulo, origemId)` (Lei 9.784/1999).
- **I-3.** A autuação exige `Classificacao` não nula e `Nup` não vazio (`ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`).
- **I-4.** A tramitação só ocorre sobre processo **em andamento** ({`Autuado`, `EmTramitacao`}); caso contrário, `InvalidOperationException`. Ao tramitar, atualiza `SetorAtualId`, registra `Movimentacao` e passa a `EmTramitacao`.
- **I-5.** O sobrestamento só ocorre sobre processo **em andamento**; ao sobrestar, passa a `Sobrestado` (suspende o andamento).
- **I-6.** Processo `Sobrestado` não tramita nem é arquivado até ser reativado (`Reativar` retorna a `EmTramitacao`).
- **I-7.** O arquivamento só ocorre sobre processo **não encerrado** ({≠ `Arquivado`}); ao arquivar, passa a `Arquivado` e emite `ProcessoArquivado`. Guarda/eliminação posterior **somente conforme TTD/CONARQ**.
- **I-8.** Estado terminal (`Arquivado`) não admite novas transições de andamento (tramitar/despachar/sobrestar).
- **I-9.** Processo com `NivelAcesso = Sigiloso` **nega** acesso não autorizado e **audita a tentativa** (registro de auditoria imutável).
- **I-10.** Despachos e movimentações são **append-only** — não são excluídos nem editados; compõem a trilha documental.
- **I-11.** O `Prazo` é calculado conforme art. 66 (exclui o dia inicial, inclui o final); o prazo padrão de decisão é `PrazoDecisaoPadraoDias` (30 dias), prorrogável.
- **I-12.** Quando autuado por outro módulo via Integration Event, `OrigemModulo` e `OrigemId` são obrigatórios e preservados para devolução do NUP ao originador.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Autuar` | `Autuado` | `Classificacao != null` && `Nup` não vazio | `ProcessoAutuado` |
| `Autuado` \| `EmTramitacao` | `Tramitar` | `EmTramitacao` | em andamento; setor destino informado | `ProcessoTramitado` |
| `Autuado` \| `EmTramitacao` | `Despachar` | (inalterado) | em andamento; despacho informado | — |
| `Autuado` \| `EmTramitacao` | `Sobrestar` | `Sobrestado` | em andamento | `ProcessoSobrestado` |
| `Sobrestado` | `Reativar` | `EmTramitacao` | situação == `Sobrestado` | `ProcessoTramitado` |
| ≠ `Arquivado` | `Arquivar` | `Arquivado` | situação ∉ {`Arquivado`} | `ProcessoArquivado` |

> Observações:
> - `Tramitar`, `Despachar` e `Sobrestar` chamam `GarantirEmAndamento()` **antes** da verificação específica de situação.
> - `Despachar` registra um `Despacho` (entidade interna) sem alterar a situação; mantém a trilha.
> - Não há comando que retorne `Arquivado` para andamento nesta versão (estado terminal); desarquivamento, se previsto, exige caso de uso dedicado e auditado.
> - A eliminação física do processo arquivado **não** é transição do agregado `Processo`: depende de `PrazoGuardaExpirado` e autorização conforme `TabelaTemporalidade` (TTD/CONARQ).

---

## 5. Comandos (escrita)

### 5.1 AutuarProcesso

- **Command:** `AutuarProcessoCommand(Guid RequerimentoId, string Classificacao, NivelDeAcesso NivelAcesso, string? OrigemModulo, Guid? OrigemId) : ICommand<Guid>`.
- **Entrada (DTO):** `RequerimentoId` (opcional), `Classificacao`, `NivelAcesso`, `OrigemModulo`/`OrigemId` (quando autuado por outro módulo).
- **Dependências do handler:** `IProcessoRepository`, `INupGenerator` (geração do NUP único), `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - `Classificacao` válida na `TabelaTemporalidade`, senão `InvalidOperationException("Classificação não encontrada na Tabela de Temporalidade.")`.
- **Efeito:** calcula `hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`; gera `nup` via `INupGenerator`; cria via `Processo.Autuar(tenant.TenantId, nup, classificacao, nivelAcesso, requerimentoId, origemModulo, origemId, hoje)`; `processos.Adicionar(processo)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Processo` em situação `Autuado`, com NUP único; retorna `processo.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (classificação/nup vazios); `InvalidOperationException` (classificação inexistente).
- **Evento de domínio:** `ProcessoAutuado(id, nup, origemModulo, origemId)` (emitido no construtor via factory).
- **Evento de integração (publica):** `ProcessoAutuadoIntegrationEvent(NUP, origemModulo, origemId)` quando há módulo originador (devolução do NUP).

### 5.2 TramitarProcesso

- **Command:** `TramitarProcessoCommand(Guid ProcessoId, Guid SetorDestinoId, string? Observacao) : ICommand`.
- **Entrada (DTO):** `ProcessoId`, `SetorDestinoId`, `Observacao` (opcional).
- **Dependências do handler:** `IProcessoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Processo existe (`ObterPorIdAsync`), senão `InvalidOperationException("Processo não encontrado.")`.
  - Situação em andamento (I-4).
- **Efeito:** `processo.Tramitar(request.SetorDestinoId, request.Observacao, hoje)`; `SaveChangesAsync`.
- **Pós-condições:** `SetorAtualId = SetorDestinoId`; nova `Movimentacao` registrada; situação `EmTramitacao`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, sobrestado ou arquivado).
- **Evento de domínio:** `ProcessoTramitado(Id, setorDestinoId)`.
- **Evento de integração (publica):** `ProcessoTramitadoIntegrationEvent`.

### 5.3 SobrestarProcesso

- **Command:** `SobrestarProcessoCommand(Guid ProcessoId, string Motivo) : ICommand`.
- **Entrada (DTO):** `ProcessoId`, `Motivo`.
- **Dependências do handler:** `IProcessoRepository`, `IUnitOfWork`.
- **Pré-condições:**
  - `request` não nulo.
  - Processo existe, senão `InvalidOperationException("Processo não encontrado.")`.
  - Situação em andamento (I-5).
- **Efeito:** `processo.Sobrestar(request.Motivo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Sobrestado`; andamento suspenso.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou já arquivado/sobrestado).
- **Evento de domínio:** `ProcessoSobrestado(Id, motivo)`.

### 5.4 ArquivarProcesso

- **Command:** `ArquivarProcessoCommand(Guid ProcessoId, string? Motivo) : ICommand`.
- **Entrada (DTO):** `ProcessoId`, `Motivo` (opcional).
- **Dependências do handler:** `IProcessoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Processo existe, senão `InvalidOperationException("Processo não encontrado.")`.
  - Situação não encerrada (I-7).
- **Efeito:** `processo.Arquivar(request.Motivo, hoje)`; `SaveChangesAsync`; publica **Integration Event** `ProcessoArquivadoIntegrationEvent` via Outbox.
- **Pós-condições:** situação `Arquivado`; guarda regida pela `TabelaTemporalidade` (TTD).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado ou já arquivado).
- **Evento de domínio:** `ProcessoArquivado(Id)`.
- **Evento de integração (publica):** `ProcessoArquivadoIntegrationEvent`.

### 5.5 DespacharProcesso

- **Command:** `DespacharProcessoCommand(Guid ProcessoId, string Texto, Guid AutoridadeId) : ICommand`.
- **Entrada (DTO):** `ProcessoId`, `Texto` (conteúdo do despacho), `AutoridadeId`.
- **Dependências do handler:** `IProcessoRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:**
  - `request` não nulo.
  - Processo existe, senão `InvalidOperationException("Processo não encontrado.")`.
  - Situação em andamento (`GarantirEmAndamento`).
- **Efeito:** `processo.Despachar(request.Texto, request.AutoridadeId, hoje)`; `SaveChangesAsync`.
- **Pós-condições:** novo `Despacho` (append-only) registrado; situação inalterada.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, sobrestado ou arquivado).
- **Evento de domínio:** — (registro interno; não emite evento próprio nesta versão).

> **Comando de domínio existente no agregado:** `Reativar()` (sai de `Sobrestado` para `EmTramitacao`), exposto para futura orquestração de caso de uso, sem handler de Application dedicado nesta versão.

---

## 6. Consultas (leitura)

### 6.1 ObterProcessoPorNup

- **Query:** `ObterProcessoPorNupQuery(string Nup) : IQuery<ProcessoDetalhe?>`.
- **Entrada:** `Nup`.
- **Handler:** `ObterProcessoPorNupHandler(IProcessoRepository processos)`; chama `processos.ObterPorNupAsync(new Nup(request.Nup), ct)`.
- **Projeção (DTO):** `ProcessoDetalhe(Guid Id, string Nup, string Classificacao, string NivelAcesso, string Situacao, DateOnly DataAutuacao, Guid? SetorAtualId, string? OrigemModulo)`.
  - `NivelAcesso`/`Situacao` projetados de `ToString()`.
- **Filtros:** por `Nup`; **sempre tenant-scoped** via Global Query Filter por `TenantId`. Aplica controle de `NivelDeAcesso`: processo `Sigiloso` retorna `null` (e audita) para quem não tem autorização (I-9).
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarProcessosDoSetor

- **Query:** `ListarProcessosDoSetorQuery(Guid SetorId) : IQuery<IReadOnlyList<ProcessoResumo>>`.
- **Entrada:** `SetorId`.
- **Handler:** `ListarProcessosDoSetorHandler(IProcessoRepository processos)`; chama `processos.ListarPorSetorAtualAsync(request.SetorId, ct)`.
- **Projeção (DTO):** `ProcessoResumo(Guid Id, string Nup, string Classificacao, string Situacao, DateOnly DataAutuacao)`.
- **Filtros:** por `SetorAtualId`; **sempre tenant-scoped** via Global Query Filter; respeita `NivelDeAcesso` (sigilosos omitidos para não autorizados, com auditoria da tentativa de listagem ampla).
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Protocolo.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `ProcessoAutuado` | `(ProcessoId, Nup, string? OrigemModulo, Guid? OrigemId)` | `Processo.Autuar` (construtor) |
| `ProcessoTramitado` | `(ProcessoId, Guid SetorDestinoId)` | `Processo.Tramitar` / `Processo.Reativar` |
| `ProcessoSobrestado` | `(ProcessoId, string Motivo)` | `Processo.Sobrestar` |
| `ProcessoArquivado` | `(ProcessoId)` | `Processo.Arquivar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Protocolo.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `ProcessoAutuadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, string Nup, string? OrigemModulo, Guid? OrigemId)` | `AutuarProcessoHandler` (devolve o NUP ao módulo originador) |
| `ProcessoTramitadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, string Nup, Guid SetorDestinoId)` | `TramitarProcessoHandler` |
| `ProcessoArquivadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, string Nup)` | `ArquivarProcessoHandler` |

### Integração (consome via `*.Contracts`)

| Evento consumido | Origem | Ação |
|---|---|---|
| `AutuarProcessoRequested` | Licitações / RH / Licenças | Autua um `Processo` (origem preservada) e responde com `ProcessoAutuadoIntegrationEvent` carregando o NUP. |
| `TramitarProcessoRequested` | Outros módulos | Tramita o processo correspondente ao setor solicitado. |
| `ArquivarProcessoRequested` | Outros módulos | Arquiva o processo originado pelo módulo. |

---

## 8. Validações (FluentValidation)

### AutuarProcessoValidator (`AbstractValidator<AutuarProcessoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Classificacao` | `NotEmpty()` + `MaximumLength(60)` | (padrão: classificação obrigatória, máx. 60 caracteres) |
| `NivelAcesso` | `IsInEnum()` | (padrão: nível de acesso inválido) |
| `OrigemId` | `NotEmpty().When(x => x.OrigemModulo != null)` | (padrão: origem obrigatória quando há módulo originador) |

### TramitarProcessoValidator (`AbstractValidator<TramitarProcessoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProcessoId` | `NotEmpty()` | (padrão: identificador do processo obrigatório) |
| `SetorDestinoId` | `NotEmpty()` | (padrão: setor de destino obrigatório) |

### SobrestarProcessoValidator (`AbstractValidator<SobrestarProcessoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProcessoId` | `NotEmpty()` | (padrão: identificador do processo obrigatório) |
| `Motivo` | `NotEmpty()` + `MaximumLength(500)` | (padrão: motivo do sobrestamento obrigatório, máx. 500 caracteres) |

### ArquivarProcessoValidator (`AbstractValidator<ArquivarProcessoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProcessoId` | `NotEmpty()` | (padrão: identificador do processo obrigatório) |

> `DespacharProcessoCommand` é protegido por invariantes de domínio e checagem de existência no handler. As queries não possuem validador dedicado (apenas `ArgumentNullException.ThrowIfNull` no handler).

---

## 9. Persistência (EF Core 8)

- **Schema:** `protocolo` (isolado por módulo). **DbContext:** o do módulo Protocolo. **Migrations:** por módulo.
- **Tabela:** `Processo` (raiz de agregado); `Despacho` e `Movimentacao` como tabelas filhas (owned/entidades internas).

| Coluna (`Processo`) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `ProcessoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Nup` | `nvarchar(30)` | conversor/owned de `Nup` (campo `Valor`); imutável. |
| `Classificacao` | `nvarchar(60)` | conversor/owned de `Classificacao`. |
| `NivelAcesso` | `int` | enum `NivelDeAcesso` (persistido por valor numérico). |
| `PrazoInicio` | `date` | parte do VO `Prazo` (`DateOnly`). |
| `PrazoFim` | `date` | parte do VO `Prazo` (`DateOnly`). |
| `SetorAtualId` | `Guid?` | setor responsável atual; nulável. |
| `OrigemModulo` | `nvarchar(40)?` | módulo originador; nulável. |
| `OrigemId` | `Guid?` | identificador da origem; nulável. |
| `RequerimentoId` | `Guid?` | requerimento de origem; nulável. |
| `DataAutuacao` | `date` | `DateOnly`. |
| `Situacao` | `int` | enum `SituacaoProcesso` (persistido por valor numérico). |

- **Tabela `Movimentacao`** (filha, append-only): `Id` (PK), `ProcessoId` (FK), `SetorOrigemId?`, `SetorDestinoId`, `Observacao?`, `DataMovimentacao` (`date`).
- **Tabela `Despacho`** (filha, append-only): `Id` (PK), `ProcessoId` (FK), `Texto`, `AutoridadeId`, `DataDespacho` (`date`).
- **Índices:**
  - PK em `Id`.
  - **Índice único** em `(TenantId, Nup)` — unicidade e imutabilidade do NUP por tenant (I-1).
  - Índice em `(TenantId, SetorAtualId)` para `ListarPorSetorAtualAsync`.
  - Índice em `(TenantId, OrigemModulo, OrigemId)` para correlação com o módulo originador.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto Protocolo para `ProcessoAutuadoIntegrationEvent`, `ProcessoTramitadoIntegrationEvent` e `ProcessoArquivadoIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Processo` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas (inclusive `ObterProcessoPorNup` e `ListarProcessosDoSetor`). Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição. Executivo e Legislativo do mesmo município são tenants **distintos**.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Autuar/tramitar/sobrestar/arquivar/despachar: papéis do **setor de protocolo/autoridade** (ex.: `Protocolo.Processo.Gerir`).
  - Consulta de processos: papel de **leitura** (ex.: `Protocolo.Processo.Ler`); processos `Sigilosos` exigem papel adicional (ex.: `Protocolo.Processo.Sigiloso`).
  - Módulo Protocolo é **ativável por tenant** (Ambos os poderes); requisição a tenant sem licença → 404/403 auditado.
- **NivelDeAcesso:** `Publico` / `Restrito` / `Sigiloso` controla visibilidade. Tentativa de acesso a processo **sigiloso** por usuário não autorizado é **negada** e **registrada em auditoria imutável** (I-9).
- **LGPD:** processos podem conter dados pessoais (requerente, partes). Acesso a processo restrito/sigiloso gera **trilha de acesso** (quem leu, quando, por quê); minimização aplicada nas projeções `ProcessoResumo`/`ProcessoDetalhe`.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Autuar`, `Tramitar`, `Sobrestar`, `Arquivar`, `Despachar`) — destinada ao Tribunal de Contas (TCE-RS). Movimentações e despachos formam trilha documental **append-only**.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Entrada — autuação cross-module (intra-aplicação, via Contracts):** Licitações, RH (Férias) e Licenças publicam `AutuarProcessoRequested(origem, ...)`; o Protocolo autua o `Processo` e responde com `ProcessoAutuadoIntegrationEvent` carregando o NUP de volta ao originador. Idempotente por `EventId`; nenhum módulo acessa tabelas do Protocolo diretamente.
- **Motor BPMN (cross-module):** fluxos de férias, licenças e licitação são orquestrados com raias por setor; a tramitação do `Processo` reflete as transições do fluxo. ACL entre o motor e o agregado.
- **Arquivística (CONARQ / e-ARQ Brasil / SIGAD):** a guarda e a destinação do processo arquivado seguem a `TabelaTemporalidade` (TTD). A eliminação ocorre apenas após `PrazoGuardaExpirado` e autorização, com termo de eliminação registrado (ver agregado `TabelaTemporalidade`).
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Autuação de requerimento do cidadão**
- **Dado** um `Requerimento` válido recebido pelo portal
- **Quando** executo `AutuarProcessoCommand(requerimentoId, classificacao, NivelDeAcesso.Publico, null, null)`
- **Então** um `Processo` é autuado em situação `Autuado`, um NUP único é gerado e o evento `ProcessoAutuado` é emitido.

**Cenário 2 — Autuação solicitada por outro módulo**
- **Dado** o módulo Licitações publica `AutuarProcessoRequested(origem="Licitacao")`
- **Quando** o Protocolo processa o Integration Event
- **Então** autua o `Processo` (preservando `OrigemModulo`/`OrigemId`) e responde com `ProcessoAutuadoIntegrationEvent` contendo o NUP.

**Cenário 3 — Imutabilidade do NUP**
- **Dado** um `Processo` já autuado com NUP gerado
- **Quando** se tenta alterar o NUP
- **Então** a operação é negada (o NUP é imutável após a autuação) e o índice único `(TenantId, Nup)` impede duplicidade.

**Cenário 4 — Tramitação entre setores**
- **Dado** um `Processo` em situação `Autuado`
- **Quando** executo `TramitarProcessoCommand(processoId, setorDestinoId)`
- **Então** `SetorAtualId = setorDestinoId`, situação = `EmTramitacao`, uma `Movimentacao` é registrada e `ProcessoTramitado` é emitido.

**Cenário 5 — Tramitação de processo arquivado**
- **Dado** um `Processo` em situação `Arquivado`
- **Quando** executo `TramitarProcessoCommand`
- **Então** ocorre `InvalidOperationException` (processo encerrado não tramita).

**Cenário 6 — Sobrestamento e reativação**
- **Dado** um `Processo` `EmTramitacao`
- **Quando** executo `SobrestarProcessoCommand(processoId, motivo)` e depois `Reativar()`
- **Então** a situação passa a `Sobrestado` (emitindo `ProcessoSobrestado`) e, ao reativar, retorna a `EmTramitacao`.

**Cenário 7 — Tentativa de tramitar processo sobrestado**
- **Dado** um `Processo` em situação `Sobrestado`
- **Quando** executo `TramitarProcessoCommand`
- **Então** ocorre `InvalidOperationException` (processo sobrestado não tramita até reativação).

**Cenário 8 — Arquivamento conforme TTD**
- **Dado** um `Processo` em situação `EmTramitacao`
- **Quando** executo `ArquivarProcessoCommand(processoId)`
- **Então** situação = `Arquivado`, `ProcessoArquivado` é emitido e `ProcessoArquivadoIntegrationEvent` é publicado; a guarda passa a reger-se pela `TabelaTemporalidade`.

**Cenário 9 — Acesso a processo sigiloso**
- **Dado** um `Processo` com `NivelDeAcesso = Sigiloso`
- **Quando** usuário sem autorização tenta visualizá-lo via `ObterProcessoPorNupQuery`
- **Então** o acesso é negado (retorna `null`/403) e a tentativa é registrada em auditoria imutável.

**Cenário 10 — Despacho preserva a trilha**
- **Dado** um `Processo` `EmTramitacao`
- **Quando** executo `DespacharProcessoCommand(processoId, texto, autoridadeId)`
- **Então** um `Despacho` é registrado (append-only), a situação permanece `EmTramitacao` e a trilha documental é preservada.

**Cenário 11 — Consulta tenant-scoped**
- **Dado** processos de um setor no tenant A e processos de outro tenant B
- **Quando** executo `ListarProcessosDoSetorQuery(setorId)` no contexto do tenant A
- **Então** retornam **apenas** os processos do tenant A, projetados em `ProcessoResumo`.

**Cenário 12 — Prazo (art. 66, Lei 9.784/1999)**
- **Dado** um `Processo` autuado em uma data
- **Quando** calculo o `Prazo` de decisão padrão (`PrazoDecisaoPadraoDias` = 30)
- **Então** o prazo **exclui o dia inicial e inclui o final**, resultando na data correta de vencimento.

---

## 13. Casos de Borda

- **B-1.** `Classificacao` nula/vazia em `Autuar` ⇒ `ArgumentException`/`ArgumentNullException` (I-3).
- **B-2.** `Nup` vazio gerado pelo `INupGenerator` ⇒ `ArgumentException` (I-3); jamais persistido.
- **B-3.** Autuar dois processos que resultem no mesmo NUP no mesmo tenant ⇒ a 2ª falha pelo índice único `(TenantId, Nup)` (I-1).
- **B-4.** Tramitar processo `Sobrestado` ⇒ falha (`GarantirEmAndamento` lança); precisa `Reativar` antes.
- **B-5.** Arquivar processo já `Arquivado` ⇒ falha (situação encerrada).
- **B-6.** Sobrestar processo já `Sobrestado` ⇒ falha (não está em andamento).
- **B-7.** Reativar processo que não está `Sobrestado` ⇒ falha (`InvalidOperationException`).
- **B-8.** Despachar processo `Arquivado` ⇒ falha (não está em andamento); despachos só em andamento.
- **B-9.** Acesso a processo `Sigiloso` por papel não autorizado ⇒ negado **e** auditado (I-9), mesmo em listagem ampla.
- **B-10.** Autuação por Integration Event sem `OrigemModulo`/`OrigemId` ⇒ rejeitada pelo validador (`OrigemId` obrigatório quando há `OrigemModulo`) (I-12).
- **B-11.** `AutuarProcessoRequested` reentregue via Outbox ⇒ idempotente por `EventId`; não cria processo duplicado.
- **B-12.** Eliminação física do processo arquivado ⇒ **não** é transição do agregado `Processo`; depende de `PrazoGuardaExpirado` + autorização conforme `TabelaTemporalidade` (TTD/CONARQ).
- **B-13.** Movimentação/despacho jamais excluídos ou editados ⇒ trilha append-only (I-10).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — regras do agregado `Processo` (PAE) derivadas do README do módulo Protocolo: autuação/NUP, tramitação, despacho, sobrestamento/reativação, arquivamento (TTD), níveis de acesso, autuação cross-module via Integration Events. |

<!-- manifest
commands: AutuarProcesso, TramitarProcesso, SobrestarProcesso, ArquivarProcesso, DespacharProcesso
queries: ObterProcessoPorNup, ListarProcessosDoSetor
domainEvents: ProcessoAutuado, ProcessoTramitado, ProcessoSobrestado, ProcessoArquivado
integrationEventsPublished: ProcessoAutuadoIntegrationEvent, ProcessoTramitadoIntegrationEvent, ProcessoArquivadoIntegrationEvent
integrationEventsConsumed: AutuarProcessoRequested, TramitarProcessoRequested, ArquivarProcessoRequested
-->
