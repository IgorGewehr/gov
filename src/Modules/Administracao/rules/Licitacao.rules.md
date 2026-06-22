---
modulo: Administracao
agregado: Licitacao
contexto: Administracao (Compras Públicas — seleção do fornecedor sob a NLLC)
poder: Ambos
schema: administracao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "Lei 14.133/2021 (NLLC) — norma central das compras públicas"
  - "Lei 14.133/2021 art. 6º (definições: modalidades, critérios de julgamento)"
  - "Lei 14.133/2021 art. 17 (fases do procedimento licitatório)"
  - "Lei 14.133/2021 art. 28 (modalidades: pregão, concorrência, diálogo competitivo)"
  - "Lei 14.133/2021 art. 33 (critérios de julgamento)"
  - "Lei 14.133/2021 art. 71 (encerramento: homologação, revogação, anulação, fracasso, deserção)"
  - "Lei 14.133/2021 art. 74 (inexigibilidade)"
  - "Lei 14.133/2021 art. 75 (dispensa por valor e hipóteses)"
  - "Lei 14.133/2021 art. 174 (PNCP — condição de eficácia)"
  - "Decreto 10.024/2019 (pregão eletrônico)"
  - "Decreto 11.246/2022 (agentes de contratação)"
  - "Decreto 11.462/2023 (PNCP)"
---

# Licitacao — Regras-as-Code (Rules-as-Code)

> Procedimento de seleção competitiva da proposta mais vantajosa para a Administração,
> conduzido sob a **Lei 14.133/2021 (NLLC)**. Pode assumir as modalidades `Pregao`,
> `Concorrencia` e `DialogoCompetitivo`, ou ser substituído por contratação direta
> (`Dispensa` / `Inexigibilidade`). Encerra-se por **homologação**, **fracasso**, **deserção**,
> **revogação** ou **anulação**. A publicidade no **PNCP** é obrigatória. Este arquivo é
> **normativo e versionado**; o código de domínio, aplicação, persistência e testes do
> agregado `Licitacao` é **gerado e mantido a partir daqui**. Bug, ajuste ou nova regra ⇒
> edita-se **este arquivo**; o código é consequência.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Licitacao (`Licitacao`) | Procedimento de seleção competitiva da proposta mais vantajosa. Raiz de agregado. |
| Identidade (`LicitacaoId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| Modalidade (`Modalidade` : `ModalidadeLicitacao`) | Forma do certame: `Pregao`, `Concorrencia`, `DialogoCompetitivo`, `Dispensa`, `Inexigibilidade` (art. 28/74/75). Value Object/enum. |
| Critério de Julgamento (`CriterioJulgamento` : `CriterioJulgamento`) | Critério de seleção: menor preço, maior desconto, técnica e preço, melhor técnica, maior lance, maior retorno econômico (art. 33). Value Object/enum. |
| ETP — Estudo Técnico Preliminar (`EtpId`) | Estudo que fundamenta a necessidade da contratação (referência por Id). |
| TR — Termo de Referência (`TermoReferenciaId`) | Documento que define objeto, requisitos e critérios (referência por Id). |
| Objeto (`Objeto` : `string`) | Descrição do objeto licitado. |
| Valor Estimado (`ValorEstimado` : `ValorMonetario`) | Valor estimado/orçado da contratação. Value Object. |
| Lote (`Lote`) | Item ou agrupamento de itens disputado isoladamente. Entidade-filha. |
| Proposta (`Proposta`) | Oferta apresentada por um licitante a um lote. Entidade-filha. |
| Habilitacao (`Habilitacao`) | Verificação de aptidão jurídica, fiscal, técnica e econômica do licitante. Entidade-filha. |
| Recurso (`Recurso`) | Impugnação administrativa interposta no certame. Entidade-filha. |
| Fornecedor (`FornecedorId`) | Licitante/proponente (outra raiz; referência por Id). |
| Proposta Vencedora (`PropostaVencedoraId`) | Proposta julgada vencedora antes da homologação. |
| Abrir (`AbrirLicitacao`) | Publicar o edital e iniciar o certame (passa a `Aberta`). |
| Julgar (`JulgarPropostas`) | Classificar propostas e indicar a vencedora (passa a `EmJulgamento`). |
| Habilitar (`HabilitarLicitante`) | Verificar a habilitação do licitante mais bem classificado. |
| Homologar (`HomologarLicitacao`) | Ato da autoridade que valida o resultado (passa a `Homologada`). |
| Fracassar (`DeclararFracassada`) | Encerrar por inexistência de proposta válida/habilitada (passa a `Fracassada`). |
| Desertar (`DeclararDeserta`) | Encerrar por ausência total de interessados (passa a `Deserta`). |
| Revogar (`RevogarLicitacao`) | Encerrar por conveniência/oportunidade (passa a `Revogada`). |
| Anular (`AnularLicitacao`) | Encerrar por ilegalidade (passa a `Anulada`). |
| PNCP (`PublicarEditalPncp` / `EditalPublicadoNoPncp`) | Publicação oficial do edital no Portal Nacional de Contratações Públicas (art. 174). |
| Situacao (`Situacao` : `SituacaoLicitacao`) | Estado atual do certame no ciclo de vida. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |

---

## 2. Modelo

- **Identidade:** `LicitacaoId` — `readonly record struct LicitacaoId(Guid Value)`; fábrica `LicitacaoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Licitacao : AggregateRoot<LicitacaoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Abrir(...)`.

### 2.1 Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `Id` | `LicitacaoId` | Identidade do agregado. | `init` |
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Objeto` | `string` | Descrição do objeto licitado. | `private set` |
| `Modalidade` | `ModalidadeLicitacao` | Modalidade do certame (art. 28/74/75). | `private set` |
| `CriterioJulgamento` | `CriterioJulgamento` | Critério de julgamento (art. 33). | `private set` |
| `ValorEstimado` | `ValorMonetario` (VO) | Valor estimado/orçado. | `private set` |
| `EtpId` | `Guid?` | Referência ao Estudo Técnico Preliminar. | `private set` |
| `TermoReferenciaId` | `Guid?` | Referência ao Termo de Referência. | `private set` |
| `Situacao` | `SituacaoLicitacao` | Situação atual. | `private set` |
| `NumeroEditalPncp` | `string?` | Identificador da contratação no PNCP, quando publicado. | `private set` |
| `PropostaVencedoraId` | `Guid?` | Proposta vencedora indicada no julgamento. | `private set` |
| `Lotes` | `IReadOnlyCollection<Lote>` | Lotes do certame (entidades-filhas). | coleção encapsulada |
| `Propostas` | `IReadOnlyCollection<Proposta>` | Propostas recebidas. | coleção encapsulada |
| `Habilitacoes` | `IReadOnlyCollection<Habilitacao>` | Habilitações verificadas. | coleção encapsulada |
| `Recursos` | `IReadOnlyCollection<Recurso>` | Recursos interpostos. | coleção encapsulada |

### 2.2 Value Objects e enums

- **`ValorMonetario`** (SharedKernel) — valor monetário; expõe `Valor` (`decimal`). Não nulo (validado em `Abrir`).
- **`ModalidadeLicitacao`** (enum):

  | Valor | Numérico | Descrição | Base legal |
  |---|---|---|---|
  | `Pregao` | 1 | Bens/serviços comuns; menor preço/maior desconto. | art. 28, I; art. 6º, XLI |
  | `Concorrencia` | 2 | Bens/serviços especiais e obras. | art. 28, II |
  | `DialogoCompetitivo` | 3 | Soluções inovadoras/complexas, com fase de diálogo. | art. 28, V; art. 32 |
  | `Dispensa` | 4 | Contratação direta por valor/hipótese legal. | art. 75 |
  | `Inexigibilidade` | 5 | Contratação direta por inviabilidade de competição. | art. 74 |

- **`CriterioJulgamento`** (enum):

  | Valor | Numérico | Descrição |
  |---|---|---|
  | `MenorPreco` | 1 | Menor preço (art. 33, I). |
  | `MaiorDesconto` | 2 | Maior desconto (art. 33, II). |
  | `MelhorTecnica` | 3 | Melhor técnica ou conteúdo artístico (art. 33, III). |
  | `TecnicaEPreco` | 4 | Técnica e preço (art. 33, IV). |
  | `MaiorLance` | 5 | Maior lance (leilão) (art. 33, V). |
  | `MaiorRetornoEconomico` | 6 | Maior retorno econômico (art. 33, VI). |

### 2.3 Entidades-filhas

- **`Lote`** — `LoteId`, `Numero` (`int`), `Descricao` (`string`), `ValorEstimado` (`ValorMonetario`).
- **`Proposta`** — `PropostaId`, `FornecedorId` (`Guid`), `LoteId`, `Valor` (`ValorMonetario`), `Classificacao` (`int?`), `Situacao` (`SituacaoProposta`: `Recebida`, `Classificada`, `Desclassificada`, `Vencedora`).
- **`Habilitacao`** — `HabilitacaoId`, `FornecedorId` (`Guid`), `Resultado` (`ResultadoHabilitacao`: `Habilitado`, `Inabilitado`), `Motivo` (`string?`).
- **`Recurso`** — `RecursoId`, `FornecedorId` (`Guid`), `Fundamentacao` (`string`), `DataInterposicao` (`DateOnly`), `Provido` (`bool?`).

### 2.4 Enum `SituacaoLicitacao`

| Valor | Numérico | Descrição |
|---|---|---|
| `Aberta` | 1 | Edital publicado; certame em andamento (estado inicial). |
| `EmJulgamento` | 2 | Propostas em classificação/julgamento. |
| `Homologada` | 3 | Resultado homologado pela autoridade (terminal de sucesso). |
| `Fracassada` | 4 | Sem proposta válida/habilitada (terminal). |
| `Deserta` | 5 | Sem interessados (terminal). |
| `Revogada` | 6 | Encerrada por conveniência/oportunidade (terminal). |
| `Anulada` | 7 | Encerrada por ilegalidade (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Homologada`, `Fracassada`, `Deserta`, `Revogada`, `Anulada` }.
> - **Em andamento** = { `Aberta`, `EmJulgamento` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Objeto` é obrigatório (não nulo/não vazio) na abertura; caso contrário, `ArgumentException`.
- **I-2.** `ValorEstimado` é obrigatório na abertura (`ArgumentNullException.ThrowIfNull(valorEstimado)`).
- **I-3.** A abertura define `Modalidade` e `CriterioJulgamento` válidos do enum; caso contrário, `ArgumentException`.
- **I-4.** Na abertura, a situação inicial é `Aberta` e é emitido o evento `LicitacaoAberta`.
- **I-5.** A modalidade `Pregao` admite somente `CriterioJulgamento` ∈ {`MenorPreco`, `MaiorDesconto`} (art. 6º, XLI / art. 28, I).
- **I-6.** O julgamento (`JulgarPropostas`) exige situação `Aberta`; passa a `EmJulgamento` e exige ao menos uma `Proposta` classificada como vencedora; caso contrário, `InvalidOperationException`.
- **I-7.** A habilitação (`HabilitarLicitante`) só ocorre sobre certame `Aberta` ou `EmJulgamento`; um licitante com sanção de impedimento/inidoneidade vigente DEVE resultar em `Inabilitado` (regra reforçada pelo agregado `Fornecedor`).
- **I-8.** A homologação (`HomologarLicitacao`) exige situação `EmJulgamento` **e** `PropostaVencedoraId` definida **e** licitante vencedor `Habilitado`; caso contrário, `InvalidOperationException`.
- **I-9.** A homologação leva a `Homologada`, emite `LicitacaoHomologada` e enfileira `LicitacaoHomologadaIntegrationEvent` no Outbox.
- **I-10.** `DeclararFracassada` / `DeclararDeserta` / `RevogarLicitacao` / `AnularLicitacao` só ocorrem sobre certame **não encerrado**; caso contrário, `InvalidOperationException`.
- **I-11.** `DeclararDeserta` exige ausência total de `Proposta` recebida; `DeclararFracassada` exige inexistência de proposta válida/habilitada (todas desclassificadas/inabilitadas).
- **I-12.** Certame **encerrado** ({`Homologada`,`Fracassada`,`Deserta`,`Revogada`,`Anulada`}) não admite novas transições.
- **I-13.** A publicação no PNCP (`PublicarEditalPncp`) grava `NumeroEditalPncp` (não vazio) e emite `EditalPublicadoNoPncp`; a publicidade é condição de regularidade do certame (art. 174).
- **I-14.** Toda transição é **tenant-scoped**: o agregado nunca é lido/gravado fora do seu `TenantId`.
- **I-15.** `RevogarLicitacao` e `AnularLicitacao` exigem `motivo` não vazio (motivação do ato administrativo).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Abrir` | `Aberta` | `objeto` não vazio; `valorEstimado != null`; modalidade/critério válidos | `LicitacaoAberta` |
| `Aberta` | `PublicarEditalPncp` | `Aberta` | `numeroEdital` não vazio | `EditalPublicadoNoPncp` |
| `Aberta` | `JulgarPropostas` | `EmJulgamento` | ≥1 proposta classificada vencedora | — |
| `Aberta` \| `EmJulgamento` | `HabilitarLicitante` | (mantém) | licitante não impedido/inidôneo | — |
| `EmJulgamento` | `HomologarLicitacao` | `Homologada` | `PropostaVencedoraId` definida; vencedor `Habilitado` | `LicitacaoHomologada` |
| `Aberta` \| `EmJulgamento` | `DeclararFracassada` | `Fracassada` | sem proposta válida/habilitada | `LicitacaoFracassada` |
| `Aberta` | `DeclararDeserta` | `Deserta` | nenhuma proposta recebida | `LicitacaoDeserta` |
| `Aberta` \| `EmJulgamento` | `RevogarLicitacao` | `Revogada` | `motivo` não vazio | `LicitacaoRevogada` |
| `Aberta` \| `EmJulgamento` | `AnularLicitacao` | `Anulada` | `motivo` não vazio | `LicitacaoAnulada` |

> Observações:
> - As transições de encerramento chamam a guarda de "não encerrada" **antes** da verificação específica.
> - `PublicarEditalPncp` e `HabilitarLicitante` não alteram a `Situacao` (efeitos colaterais no agregado), mas emitem evento/registram entidade-filha.
> - Não há transição que reabra um certame encerrado (estados terminais).

---

## 5. Comandos (escrita)

Cada comando tem `*Command` + `*Handler` + `*Validator` (FluentValidation). Todos resolvem `ITenantContext` e respeitam o Global Query Filter.

### 5.1 AbrirLicitacao

- **Command:** `AbrirLicitacaoCommand(string Objeto, ModalidadeLicitacao Modalidade, CriterioJulgamento CriterioJulgamento, decimal ValorEstimado, Guid? EtpId, Guid? TermoReferenciaId) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; `Objeto` não vazio; modalidade/critério válidos; combinação modalidade×critério permitida (I-5).
- **Efeito:** cria via `Licitacao.Abrir(tenant.TenantId, objeto, modalidade, criterio, ValorMonetario(valorEstimado), etpId, trId)`; `licitacoes.Adicionar(licitacao)`; `SaveChangesAsync`.
- **Pós-condições:** nova `Licitacao` em situação `Aberta`; retorna `licitacao.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (objeto/valor); `InvalidOperationException` (combinação inválida).
- **Evento de domínio:** `LicitacaoAberta(Id, modalidade)`.

### 5.2 PublicarEditalNoPncp

- **Command:** `PublicarEditalNoPncpCommand(Guid LicitacaoId, string NumeroEditalPncp) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; situação `Aberta`; `NumeroEditalPncp` não vazio.
- **Efeito:** `licitacao.PublicarEditalPncp(numero)`; `SaveChangesAsync`. A publicação efetiva no Portal é feita pelo handler do Outbox (cliente PNCP resiliente — §11).
- **Pós-condições:** `NumeroEditalPncp` preenchido.
- **Exceções:** `ArgumentNullException` (request); `ArgumentException` (número vazio); `InvalidOperationException` (não encontrada).
- **Evento de domínio:** `EditalPublicadoNoPncp(Id, numero)`.

### 5.3 JulgarPropostas

- **Command:** `JulgarPropostasCommand(Guid LicitacaoId, Guid PropostaVencedoraId) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; situação `Aberta`; proposta vencedora pertence ao certame e está classificada (I-6).
- **Efeito:** `licitacao.JulgarPropostas(propostaVencedoraId)`; passa a `EmJulgamento`; `SaveChangesAsync`.
- **Pós-condições:** situação `EmJulgamento`; `PropostaVencedoraId` definida.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (situação ≠ `Aberta` ou proposta inválida).
- **Evento de domínio:** — (sem evento dedicado nesta versão).

### 5.4 HabilitarLicitante

- **Command:** `HabilitarLicitanteCommand(Guid LicitacaoId, Guid FornecedorId, ResultadoHabilitacao Resultado, string? Motivo) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; situação ∈ {`Aberta`,`EmJulgamento`}; fornecedor sem inidoneidade/impedimento vigente para resultar `Habilitado` (I-7).
- **Efeito:** `licitacao.HabilitarLicitante(fornecedorId, resultado, motivo)`; `SaveChangesAsync`.
- **Pós-condições:** `Habilitacao` registrada para o fornecedor.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (situação inválida ou tentativa de habilitar fornecedor inidôneo).
- **Evento de domínio:** —.

### 5.5 HomologarLicitacao

- **Command:** `HomologarLicitacaoCommand(Guid LicitacaoId) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; situação `EmJulgamento`; `PropostaVencedoraId` definida; vencedor `Habilitado` (I-8). RBAC: papel de **ordenador de despesa** (segregação de funções).
- **Efeito:** `licitacao.Homologar()`; passa a `Homologada`; `SaveChangesAsync`; enfileira `LicitacaoHomologadaIntegrationEvent` no Outbox.
- **Pós-condições:** situação `Homologada`; resultado publicável a outros módulos (Administracao→Contrato; Patrimonio se aquisição de bem).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (situação ≠ `EmJulgamento`, sem vencedor ou vencedor não habilitado).
- **Evento de domínio:** `LicitacaoHomologada(Id, propostaVencedoraId, fornecedorVencedorId)`.
- **Evento de integração (publica):** `LicitacaoHomologadaIntegrationEvent` e, se aquisição de bem permanente, `AquisicaoBemHomologadaIntegrationEvent`.

### 5.6 DeclararLicitacaoFracassada

- **Command:** `DeclararLicitacaoFracassadaCommand(Guid LicitacaoId, string Motivo) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; não encerrada; sem proposta válida/habilitada (I-11).
- **Efeito:** `licitacao.DeclararFracassada(motivo)`; passa a `Fracassada`; `SaveChangesAsync`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException`.
- **Evento de domínio:** `LicitacaoFracassada(Id, motivo)`.

### 5.7 DeclararLicitacaoDeserta

- **Command:** `DeclararLicitacaoDesertaCommand(Guid LicitacaoId) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; situação `Aberta`; nenhuma `Proposta` recebida (I-11).
- **Efeito:** `licitacao.DeclararDeserta()`; passa a `Deserta`; `SaveChangesAsync`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException`.
- **Evento de domínio:** `LicitacaoDeserta(Id)`.

### 5.8 RevogarLicitacao

- **Command:** `RevogarLicitacaoCommand(Guid LicitacaoId, string Motivo) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; não encerrada; `Motivo` não vazio (I-15).
- **Efeito:** `licitacao.Revogar(motivo)`; passa a `Revogada`; `SaveChangesAsync`.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (motivo vazio); `InvalidOperationException`.
- **Evento de domínio:** `LicitacaoRevogada(Id, motivo)`.

### 5.9 AnularLicitacao

- **Command:** `AnularLicitacaoCommand(Guid LicitacaoId, string Motivo) : ICommand`.
- **Pré-condições:** `request` não nulo; licitação existe; não encerrada; `Motivo` não vazio (ilegalidade — I-15).
- **Efeito:** `licitacao.Anular(motivo)`; passa a `Anulada`; `SaveChangesAsync`.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (motivo vazio); `InvalidOperationException`.
- **Evento de domínio:** `LicitacaoAnulada(Id, motivo)`.

---

## 6. Consultas (leitura)

Toda consulta é **tenant-scoped** via Global Query Filter por `TenantId`.

### 6.1 ObterLicitacaoPorId

- **Query:** `ObterLicitacaoPorIdQuery(Guid LicitacaoId) : IQuery<LicitacaoDetalhe?>`.
- **Projeção (DTO):** `LicitacaoDetalhe(Guid Id, string Objeto, string Modalidade, string CriterioJulgamento, decimal ValorEstimado, string Situacao, string? NumeroEditalPncp, Guid? PropostaVencedoraId, IReadOnlyList<LoteResumo> Lotes, IReadOnlyList<PropostaResumo> Propostas)`.

### 6.2 ListarLicitacoesPorSituacao

- **Query:** `ListarLicitacoesPorSituacaoQuery(SituacaoLicitacao Situacao) : IQuery<IReadOnlyList<LicitacaoResumo>>`.
- **Projeção (DTO):** `LicitacaoResumo(Guid Id, string Objeto, string Modalidade, string Situacao, decimal ValorEstimado, string? NumeroEditalPncp)`.
- **Filtros:** por `Situacao`; sempre tenant-scoped.

### 6.3 ListarPropostasDaLicitacao

- **Query:** `ListarPropostasDaLicitacaoQuery(Guid LicitacaoId) : IQuery<IReadOnlyList<PropostaResumo>>`.
- **Projeção (DTO):** `PropostaResumo(Guid PropostaId, Guid FornecedorId, Guid LoteId, decimal Valor, int? Classificacao, string Situacao)`.
- **Filtros:** por `LicitacaoId`; sempre tenant-scoped.

> Todos os handlers de consulta validam `request` não nulo (`ArgumentNullException.ThrowIfNull`) e não omitem o tenant (aplicado pela infraestrutura).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Administracao.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `LicitacaoAberta` | `(LicitacaoId, ModalidadeLicitacao Modalidade)` | `Licitacao.Abrir` |
| `EditalPublicadoNoPncp` | `(LicitacaoId, string NumeroEditalPncp)` | `Licitacao.PublicarEditalPncp` |
| `LicitacaoHomologada` | `(LicitacaoId, Guid PropostaVencedoraId, Guid FornecedorVencedorId)` | `Licitacao.Homologar` |
| `LicitacaoFracassada` | `(LicitacaoId, string Motivo)` | `Licitacao.DeclararFracassada` |
| `LicitacaoDeserta` | `(LicitacaoId)` | `Licitacao.DeclararDeserta` |
| `LicitacaoRevogada` | `(LicitacaoId, string Motivo)` | `Licitacao.Revogar` |
| `LicitacaoAnulada` | `(LicitacaoId, string Motivo)` | `Licitacao.Anular` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Administracao.Contracts`)

| Evento | Payload | Publicado por | Consumido por |
|---|---|---|---|
| `LicitacaoHomologadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid LicitacaoId, Guid FornecedorVencedorId, decimal ValorAdjudicado)` | `HomologarLicitacaoHandler` | Administracao (formalização do `Contrato`) |
| `AquisicaoBemHomologadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid LicitacaoId, string Objeto, decimal Valor)` | `HomologarLicitacaoHandler` (quando aquisição de bem permanente) | **Patrimonio** (tombamento) |

### Integração (consome)

- Nenhum no agregado `Licitacao` nesta versão (o consumo de `EmpenhoEmitidoIntegrationEvent` e `DotacaoIndisponivelIntegrationEvent` ocorre no agregado `Contrato`).

---

## 8. Validações (FluentValidation)

### AbrirLicitacaoValidator (`AbstractValidator<AbrirLicitacaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Objeto` | `NotEmpty()` + `MaximumLength(500)` | "Objeto é obrigatório (máx. 500 caracteres)." |
| `Modalidade` | `IsInEnum()` | "Modalidade inválida." |
| `CriterioJulgamento` | `IsInEnum()` | "Critério de julgamento inválido." |
| `ValorEstimado` | `GreaterThan(0)` | "Valor estimado deve ser positivo." |
| (`Modalidade`,`CriterioJulgamento`) | `Must(PregaoExigeMenorPrecoOuDesconto)` | "Pregão admite apenas menor preço ou maior desconto." |

### PublicarEditalNoPncpValidator (`AbstractValidator<PublicarEditalNoPncpCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `LicitacaoId` | `NotEmpty()` | "Licitação é obrigatória." |
| `NumeroEditalPncp` | `NotEmpty()` + `MaximumLength(60)` | "Número do edital no PNCP é obrigatório (máx. 60)." |

### HomologarLicitacaoValidator / Revogar / Anular / DeclararFracassada / DeclararDeserta / JulgarPropostas / HabilitarLicitante

| Comando | Campo | Regra |
|---|---|---|
| `HomologarLicitacaoCommand` | `LicitacaoId` | `NotEmpty()` |
| `RevogarLicitacaoCommand` | `LicitacaoId`, `Motivo` | `NotEmpty()` ambos |
| `AnularLicitacaoCommand` | `LicitacaoId`, `Motivo` | `NotEmpty()` ambos |
| `DeclararLicitacaoFracassadaCommand` | `LicitacaoId`, `Motivo` | `NotEmpty()` ambos |
| `DeclararLicitacaoDesertaCommand` | `LicitacaoId` | `NotEmpty()` |
| `JulgarPropostasCommand` | `LicitacaoId`, `PropostaVencedoraId` | `NotEmpty()` ambos |
| `HabilitarLicitanteCommand` | `LicitacaoId`, `FornecedorId`, `Resultado` | `NotEmpty()` ids; `IsInEnum()` resultado |

---

## 9. Persistência (EF Core 8)

- **Schema:** `administracao` (isolado por módulo). **DbContext:** `AdministracaoDbContext`. **Migrations:** por módulo.
- **Tabela:** `Licitacao` (raiz de agregado); entidades-filhas em tabelas próprias (composição owned/related).

| Coluna (`Licitacao`) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor `LicitacaoId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Objeto` | `nvarchar(500)` | obrigatório. |
| `Modalidade` | `int` | enum `ModalidadeLicitacao` (valor numérico). |
| `CriterioJulgamento` | `int` | enum `CriterioJulgamento` (valor numérico). |
| `ValorEstimado` | `decimal(18,2)` | conversor/owned de `ValorMonetario`. |
| `EtpId` | `Guid?` | referência ao ETP. |
| `TermoReferenciaId` | `Guid?` | referência ao TR. |
| `Situacao` | `int` | enum `SituacaoLicitacao` (valor numérico). |
| `NumeroEditalPncp` | `nvarchar(60)?` | nulável até a publicação no PNCP. |
| `PropostaVencedoraId` | `Guid?` | proposta vencedora. |

- **Tabelas-filhas:** `LicitacaoLote`, `LicitacaoProposta`, `LicitacaoHabilitacao`, `LicitacaoRecurso` — todas com FK para `Licitacao(Id)` e `TenantId` herdado.
- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, Situacao)` para `ListarLicitacoesPorSituacao`.
  - Índice único em `(TenantId, NumeroEditalPncp)` quando não nulo (unicidade da contratação no PNCP por tenant).
  - Índice em `LicitacaoProposta(TenantId, LicitacaoId)`.
- **Outbox:** tabela Outbox do contexto Administracao para `LicitacaoHomologadaIntegrationEvent` e `AquisicaoBemHomologadaIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Licitacao` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão) — segregação de funções (art. 7º/NLLC):**
  - Abertura/condução/julgamento: **agente de contratação** / **pregoeiro** (ex.: `Administracao.Licitacao.Conduzir`).
  - Homologação: **ordenador de despesa** (ex.: `Administracao.Licitacao.Homologar`) — papel distinto do agente de contratação.
  - Consulta: **leitura de compras** (ex.: `Administracao.Licitacao.Ler`).
  - Módulo Administracao é **ativável por tenant** (Executivo e Legislativo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** dados majoritariamente de pessoa jurídica (fornecedores). Representantes pessoa física tratados com minimização; trilha de acesso registrada.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Abrir`, `PublicarEditalPncp`, `JulgarPropostas`, `HabilitarLicitante`, `Homologar`, `DeclararFracassada`, `DeclararDeserta`, `Revogar`, `Anular`) — destinada ao Tribunal de Contas (TCE-RS) e à transparência ativa (LAI).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **PNCP (Decreto 11.462/2023; art. 174 da NLLC) — SAÍDA:** publicação de editais. Cliente REST/JSON credenciado por CNPJ, **resiliente (Polly: retry + circuit breaker + timeout)**, chamado por **handler do Outbox**, **idempotente** por chave da contratação. A publicação é condição de regularidade do certame e de eficácia do contrato dele decorrente. Atrás de **Anti-Corruption Layer**.
- **SICAF / Compras.gov.br — CONSULTA:** apoio à `Habilitacao` (regularidade e nível cadastral do fornecedor). Idempotente, com timeout/retry; mapeamento de erros via ACL. (O cadastro do fornecedor e seu nível SICAF pertencem ao agregado `Fornecedor`.)
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de ACL.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Abertura de pregão**
- **Dado** um TR e valor estimado de R$ 200.000,00
- **Quando** executo `AbrirLicitacaoCommand(objeto, Pregao, MenorPreco, 200000, etpId, trId)`
- **Então** é criada uma `Licitacao` em situação `Aberta` e o evento `LicitacaoAberta` é emitido.

**Cenário 2 — Pregão com critério incompatível**
- **Dado** uma tentativa de abrir `Pregao` com `CriterioJulgamento = TecnicaEPreco`
- **Quando** executo `AbrirLicitacaoCommand`
- **Então** a validação rejeita ("Pregão admite apenas menor preço ou maior desconto") (I-5).

**Cenário 3 — Homologação publica integration event**
- **Dado** uma `Licitacao` na modalidade `Pregao` em `EmJulgamento`, com proposta vencedora de licitante `Habilitado`
- **Quando** o ordenador executa `HomologarLicitacaoCommand`
- **Então** situação = `Homologada`, `LicitacaoHomologada` é registrado e `LicitacaoHomologadaIntegrationEvent` é enfileirado no Outbox.

**Cenário 4 — Homologação sem vencedor habilitado**
- **Dado** uma `Licitacao` em `EmJulgamento` cujo vencedor está `Inabilitado`
- **Quando** executo `HomologarLicitacaoCommand`
- **Então** ocorre `InvalidOperationException` (I-8).

**Cenário 5 — Habilitação de fornecedor inidôneo**
- **Dado** um `Fornecedor` com `Sancao` de inidoneidade vigente apresentando proposta
- **Quando** executo `HabilitarLicitanteCommand(..., Resultado=Habilitado)`
- **Então** o resultado é forçado a `Inabilitado` (ou ocorre `InvalidOperationException`) (I-7).

**Cenário 6 — Licitação deserta**
- **Dado** uma `Licitacao` `Aberta` sem nenhuma `Proposta` recebida
- **Quando** executo `DeclararLicitacaoDesertaCommand`
- **Então** situação = `Deserta` e o evento `LicitacaoDeserta` é emitido (I-11).

**Cenário 7 — Licitação fracassada**
- **Dado** uma `Licitacao` cujas propostas foram todas desclassificadas/inabilitadas
- **Quando** executo `DeclararLicitacaoFracassadaCommand(motivo)`
- **Então** situação = `Fracassada` e o evento `LicitacaoFracassada` é emitido.

**Cenário 8 — Revogação exige motivação**
- **Dado** uma `Licitacao` `Aberta`
- **Quando** executo `RevogarLicitacaoCommand(id, "")`
- **Então** a validação rejeita por `Motivo` vazio (I-15).

**Cenário 9 — Transição sobre certame encerrado**
- **Dado** uma `Licitacao` `Homologada` (ou `Revogada`/`Anulada`)
- **Quando** executo qualquer comando de transição
- **Então** ocorre `InvalidOperationException` (I-12).

**Cenário 10 — Publicação no PNCP**
- **Dado** uma `Licitacao` `Aberta`
- **Quando** executo `PublicarEditalNoPncpCommand(id, "PNCP-2026-0001")`
- **Então** `NumeroEditalPncp = "PNCP-2026-0001"`, o evento `EditalPublicadoNoPncp` é emitido e o handler do Outbox publica no PNCP de forma idempotente.

**Cenário 11 — Consulta tenant-scoped**
- **Dado** licitações no tenant A e no tenant B
- **Quando** executo `ListarLicitacoesPorSituacaoQuery(Aberta)` no contexto do tenant A
- **Então** retornam **apenas** as licitações do tenant A.

---

## 13. Casos de Borda

- **B-1.** `Objeto` vazio/nulo em `Abrir` ⇒ `ArgumentException` (I-1).
- **B-2.** `ValorEstimado` ≤ 0 ⇒ rejeitado pelo validator (I-2).
- **B-3.** `Modalidade`/`CriterioJulgamento` fora do enum ⇒ `IsInEnum` rejeita (I-3).
- **B-4.** `Pregao` + `TecnicaEPreco`/`MelhorTecnica`/`MaiorLance`/`MaiorRetornoEconomico` ⇒ rejeitado (I-5).
- **B-5.** `JulgarPropostas` com `PropostaVencedoraId` inexistente no certame ⇒ `InvalidOperationException` (I-6).
- **B-6.** `Homologar` sem `PropostaVencedoraId` definida ⇒ `InvalidOperationException` (I-8).
- **B-7.** `DeclararDeserta` com ≥1 proposta recebida ⇒ `InvalidOperationException` (I-11).
- **B-8.** `DeclararFracassada` com ao menos uma proposta válida/habilitada ⇒ `InvalidOperationException` (I-11).
- **B-9.** Publicar PNCP duas vezes ⇒ idempotente por chave da contratação no handler do Outbox (não duplica publicação).
- **B-10.** Homologar por papel que não seja **ordenador de despesa** ⇒ 403 auditado (segregação de funções).
- **B-11.** `NumeroEditalPncp` duplicado no mesmo tenant ⇒ violação do índice único `(TenantId, NumeroEditalPncp)`.
- **B-12.** Revogar/Anular sem `Motivo` ⇒ `ArgumentException`/validação (I-15).
- **B-13.** `LicitacaoHomologadaIntegrationEvent` deve ser idempotente no consumidor por `EventId` (reentrega via Outbox).
- **B-14.** Aquisição de bem permanente homologada ⇒ também publica `AquisicaoBemHomologadaIntegrationEvent` para o Patrimonio (tombamento).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo Administracao e da Lei 14.133/2021 (modalidades, critérios, ciclo de homologação/fracasso/deserção/revogação, PNCP). |

<!-- manifest
commands: AbrirLicitacao, PublicarEditalNoPncp, JulgarPropostas, HabilitarLicitante, HomologarLicitacao, DeclararLicitacaoFracassada, DeclararLicitacaoDeserta, RevogarLicitacao, AnularLicitacao
queries: ObterLicitacaoPorId, ListarLicitacoesPorSituacao, ListarPropostasDaLicitacao
domainEvents: LicitacaoAberta, EditalPublicadoNoPncp, LicitacaoHomologada, LicitacaoFracassada, LicitacaoDeserta, LicitacaoRevogada, LicitacaoAnulada
integrationEventsPublished: LicitacaoHomologadaIntegrationEvent, AquisicaoBemHomologadaIntegrationEvent
integrationEventsConsumed: 
-->
