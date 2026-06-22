---
modulo: Tributos
agregado: NotaFiscalServico
contexto: Tributos (painel fiscal do ISS / ingestao NFS-e)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["NFS-e Nacional / Ambiente de Dados Nacional (ADN) — ADR-0003 (integracao passiva: nao emitimos nem assinamos)", "LC 116/2003 (ISS — Imposto Sobre Servicos de Qualquer Natureza)"]
---

# NotaFiscalServico (NFS-e Nacional) — Regras-as-Code

> **Natureza do agregado.** `NotaFiscalServico` e um **read model** (raiz de agregado) que
> representa uma NFS-e **sincronizada do Ambiente de Dados Nacional (ADN)** da Receita Federal.
> Conforme **ADR-0003** e §8 do CLAUDE.md, a integracao e **PASSIVA**: o sistema **NAO emite nem
> assina** NFS-e — apenas **consome** (importa) o documento ja emitido e assinado no Ambiente
> Nacional, para alimentar o **painel fiscal do ISS** (Tributos) e a **Divida Ativa**.
> Por ser read model, o agregado **nasce importado e e imutavel** (sem maquina de estados de negocio).

---

## 1. Linguagem Ubiqua

> Tabela `Termo (identificador-no-codigo sem acento) — definicao`. Os identificadores sao VINCULANTES.

| Termo (identificador) | Definicao |
|---|---|
| NFS-e (`NotaFiscalServico`) | Nota Fiscal de Servico eletronica, sincronizada do Ambiente Nacional; read model para fiscalizacao do ISS. |
| Importacao (`Importar`) | Ato de registrar (importar) no painel uma NFS-e baixada do ADN. NAO e emissao. |
| Chave de Acesso (`ChaveAcesso`) | Identificador nacional unico da NFS-e (string). Usada para deduplicacao. |
| Prestador (`PrestadorCnpj`) | CNPJ do prestador do servico (emissor da nota). |
| Tomador (`TomadorDocumento`) | Documento (CPF/CNPJ) do tomador do servico, quando informado (opcional). |
| Valor do Servico (`ValorServico`) | Valor monetario do servico prestado (`ValorMonetario`). |
| Valor do ISS (`ValorIss`) | Valor monetario do ISS destacado na nota (`ValorMonetario`), conforme LC 116/2003. |
| Data de Emissao (`DataEmissao`) | Data em que a NFS-e foi emitida no Ambiente Nacional (`DateOnly`). |
| Competencia (`Competencia`) | Competencia fiscal (ano/mes) a que a nota se refere (`Competencia`). |
| ADN (`INfseNacionalGateway`) | Ambiente de Dados Nacional da NFS-e; gateway/ACL de acesso aos documentos nacionais. |
| Documento ADN (`NfseDocumento`) | DTO bruto retornado pelo ADN antes de virar `NotaFiscalServico`. |
| Sincronizador (`INfseSincronizador`) | Servico que baixa, deduplica (por chave) e persiste as NFS-e do tenant. |
| Tenant (`TenantId`) | Ente publico (prefeitura) dono do registro; isolamento multi-tenant obrigatorio. |

---

## 2. Modelo

- **Identidade:** `NotaFiscalServicoId` — `readonly record struct` envolvendo `Guid`.
  - `NotaFiscalServicoId.New()` gera novo identificador (`Guid.NewGuid()`).
  - `ToString()` retorna o GUID subjacente.
- **Raiz de agregado:** `NotaFiscalServico : AggregateRoot<NotaFiscalServicoId>, IMustHaveTenant`.

### Propriedades (`nome : Tipo (VO/enum/primitivo) — descricao`)

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `Id` | `NotaFiscalServicoId` (Id forte) | Identidade do read model. | herdada de `AggregateRoot` |
| `TenantId` | `Guid` (primitivo) | Tenant (ente publico) dono do registro. | `private set` |
| `ChaveAcesso` | `string` (primitivo) | Chave de acesso (identificador nacional unico). | `private set` |
| `PrestadorCnpj` | `string` (primitivo) | CNPJ do prestador (emissor). | `private set` |
| `TomadorDocumento` | `string?` (primitivo, opcional) | Documento do tomador, quando informado. | `private set` |
| `ValorServico` | `ValorMonetario` (VO) | Valor do servico. | `private set` |
| `ValorIss` | `ValorMonetario` (VO) | Valor do ISS destacado. | `private set` |
| `DataEmissao` | `DateOnly` (primitivo) | Data de emissao no Ambiente Nacional. | `private set` |
| `Competencia` | `Competencia` (VO) | Competencia fiscal (ano/mes). | `private set` |

### Value Objects e enums

- `ValorMonetario` (VO do modulo Tributos — `Domain.ValueObjects`) — encapsula valor monetario. Usado em `ValorServico` e `ValorIss`.
- `Competencia` (VO do modulo Tributos — `Domain.ValueObjects`) — competencia fiscal (ano + mes). Construida no Application a partir de `Ano` e `Mes` (ver §5).
- **Enums:** o agregado **nao possui enums** (read model imutavel, sem situacao/estado de negocio).

> **Construcao:** construtor privado sem parametros (EF) + construtor privado completo + **factory** `Importar(...)`.
> A entidade **nasce valida** (invariantes garantidas na factory) e **nasce importada** (evento de dominio no construtor).

---

## 3. Invariantes

> Lista NUMERADA. Cada item I-n vira `[Fact] Invariante_n_*`.

- **I-1.** `ChaveAcesso` e obrigatoria e nao pode ser nula nem vazia/branca (`ArgumentException.ThrowIfNullOrWhiteSpace`).
- **I-2.** `PrestadorCnpj` e obrigatorio e nao pode ser nulo nem vazio/branco (`ArgumentException.ThrowIfNullOrWhiteSpace`).
- **I-3.** `ValorServico` e obrigatorio e nao pode ser nulo (`ArgumentNullException.ThrowIfNull`).
- **I-4.** `ValorIss` e obrigatorio e nao pode ser nulo (`ArgumentNullException.ThrowIfNull`) — destaque do ISS (LC 116/2003).
- **I-5.** `Competencia` e obrigatoria e nao pode ser nula (`ArgumentNullException.ThrowIfNull`).
- **I-6.** `TomadorDocumento` e **opcional** (pode ser `null`); quando ausente, a nota permanece valida.
- **I-7.** Toda NFS-e importada pertence a exatamente um `TenantId` (`IMustHaveTenant`); o registro nasce vinculado ao tenant informado na importacao.
- **I-8.** A importacao bem-sucedida gera **sempre** o evento de dominio `NotaFiscalServicoImportada(Id, ChaveAcesso)` (emitido no construtor).
- **I-9.** O identificador e gerado internamente via `NotaFiscalServicoId.New()` na factory — nao e fornecido externamente.
- **I-10.** Unicidade de negocio por **chave de acesso** dentro do tenant: a deduplicacao e responsabilidade do fluxo de sincronizacao (`INotaFiscalServicoRepository.ExistePorChaveAsync` + indice unico — ver §9/§11). O agregado em si nao revalida a unicidade.
- **I-11.** Read model imutavel: apos a importacao, **nenhum** estado de negocio do agregado e alterado (nao ha comandos de mutacao subsequentes — ver §4 e §5).

---

## 4. Maquina de Estados

> Tabela: Estado origem → comando → Estado destino | guarda | evento emitido.

Por ser **read model imutavel**, o agregado **nao possui maquina de estados de negocio** com
multiplas transicoes. Existe apenas o **fato de criacao/ingestao**:

| Origem | Comando/Operacao | Destino | Guarda | Evento |
|---|---|---|---|---|
| (inexistente) | `Importar(...)` (factory) | `Importada` (registro persistido, imutavel) | I-1..I-5, I-9 satisfeitas | `NotaFiscalServicoImportada` |
| `Importada` | — | `Importada` (terminal) | nao ha transicoes subsequentes | — |

> Observacao: nao ha comandos de cancelamento, alteracao ou reemissao no agregado — a nota e
> reflexo fiel do documento nacional (ADR-0003).

---

## 5. Comandos (escrita)

> Para cada comando: entrada (DTO), pre-condicoes, efeito, pos-condicoes, excecoes, evento.

> **Importante (estado atual do codigo).** As fontes fornecidas (`NotaFiscalServico.cs` e
> `NfseContratos.cs`) **NAO definem `*Command`/`*Handler` MediatR**. A escrita ocorre por:
> (a) a **factory de dominio** `NotaFiscalServico.Importar(...)`, e (b) o **servico de aplicacao**
> `INfseSincronizador.SincronizarAsync(...)`, que orquestra gateway + repositorio. Esta secao
> documenta fielmente essas operacoes de escrita. **Nao ha comando MediatR a inventar** (ver manifesto).

### 5.1 Operacao de dominio — `NotaFiscalServico.Importar`

- **Entrada (parametros):** `tenantId : Guid`, `chaveAcesso : string`, `prestadorCnpj : string`,
  `tomadorDocumento : string?`, `valorServico : ValorMonetario`, `valorIss : ValorMonetario`,
  `dataEmissao : DateOnly`, `competencia : Competencia`.
- **Pre-condicoes:** I-1 (chave nao vazia), I-2 (CNPJ prestador nao vazio), I-3 (valor servico nao nulo),
  I-4 (valor ISS nao nulo), I-5 (competencia nao nula).
- **Efeito:** gera `NotaFiscalServicoId.New()`, cria a instancia com todos os campos e carimba o estado.
- **Pos-condicoes:** registro valido e imutavel; `TenantId` definido; evento de dominio enfileirado.
- **Excecoes:** `ArgumentException` (chave/CNPJ vazios — I-1/I-2); `ArgumentNullException`
  (valorServico/valorIss/competencia nulos — I-3/I-4/I-5).
- **Evento:** `NotaFiscalServicoImportada(NotaFiscalServicoId, ChaveAcesso)` (dominio, in-process).

### 5.2 Operacao de aplicacao — `INfseSincronizador.SincronizarAsync`

- **Entrada:** `cnpjsPrestadores : IReadOnlyList<string>`, `desde : DateOnly`, `cancellationToken : CancellationToken`.
- **Pre-condicoes:** tenant atual resolvido (worker itera tenants); lista de CNPJs do tenant.
- **Efeito:** para cada CNPJ, chama `INfseNacionalGateway.ObterNotasEmitidasAsync(cnpj, desde, ct)`,
  **deduplica por chave de acesso** via `INotaFiscalServicoRepository.ExistePorChaveAsync(chave, ct)`,
  mapeia `NfseDocumento` → `ValorMonetario`/`Competencia(Ano, Mes)` e chama
  `NotaFiscalServico.Importar(...)` + `INotaFiscalServicoRepository.Adicionar(nota)`.
- **Pos-condicoes:** apenas notas **novas** (chave inexistente no tenant) sao persistidas;
  retorna a **quantidade de notas novas importadas** (`int`).
- **Excecoes:** propaga falhas do gateway (tratadas com resiliencia/ACL — ver §11);
  `OperationCanceledException` em cancelamento.
- **Evento:** indiretamente, cada nota nova emite `NotaFiscalServicoImportada` (via factory).

---

## 6. Consultas (leitura)

> Nome, entrada, projecao (DTO), filtros (sempre tenant-scoped).

> **Estado atual do codigo.** As fontes fornecidas **NAO definem `*Query`/`*Handler` MediatR**
> para este agregado. A unica leitura presente nos contratos e um **predicado de existencia**
> usado na deduplicacao (nao e uma Query de projeicao). Documentado fielmente; **nenhuma Query a inventar**
> (ver manifesto — `queries` vazio).

| Operacao | Entrada | Projecao | Filtro |
|---|---|---|---|
| `INotaFiscalServicoRepository.ExistePorChaveAsync` | `chaveAcesso : string`, `ct` | `bool` (existe?) | **tenant-scoped** (no tenant atual, via Global Query Filter) |

> Consultas de projeicao para o painel fiscal (ex.: listar NFS-e por competencia/prestador) **nao existem
> nestas fontes** e, portanto, nao sao especificadas aqui — seriam adicionadas em versao futura das regras.

---

## 7. Eventos

> - Dominio: Nome(payload)
> - Integracao (publica/consome via *.Contracts): Nome(payload)

- **Dominio (in-process, MediatR):**
  - `NotaFiscalServicoImportada(NotaFiscalServicoId NotaFiscalServicoId, string ChaveAcesso)` — emitido no construtor ao importar a nota. Implementa `IDomainEvent`.
- **Integracao (publica via `*.Contracts`):** **nenhum** evento de integracao e publicado por este agregado nas fontes fornecidas.
- **Integracao (consome):** **nenhum** — a ingestao parte do **gateway ADN** (`INfseNacionalGateway`), nao de Integration Events de outros modulos.

---

## 8. Validacoes (FluentValidation)

> Campo → regra → mensagem.

> As fontes fornecidas garantem as invariantes via **guard clauses** na factory de dominio
> (`ArgumentException`/`ArgumentNullException`), nao via `AbstractValidator`. Caso um `*Command`
> seja introduzido no futuro, o validador correspondente deve refletir exatamente estas regras:

| Campo | Regra | Mensagem (sugerida) |
|---|---|---|
| `ChaveAcesso` | `NotEmpty` | "Chave de acesso da NFS-e e obrigatoria." |
| `PrestadorCnpj` | `NotEmpty` (CNPJ valido) | "CNPJ do prestador e obrigatorio e deve ser valido." |
| `TomadorDocumento` | opcional; quando informado, documento valido | "Documento do tomador invalido." |
| `ValorServico` | `NotNull` (>= 0) | "Valor do servico e obrigatorio." |
| `ValorIss` | `NotNull` (>= 0) | "Valor do ISS e obrigatorio." |
| `DataEmissao` | data valida | "Data de emissao invalida." |
| `Competencia` (`Ano`/`Mes`) | `NotNull`; `Mes` em 1..12; `Ano` plausivel | "Competencia fiscal invalida." |

---

## 9. Persistencia

> Tabela, colunas, conversores (VO/Id), indices (incl. unicos), schema.

- **Schema:** `tributos` (DbContext do modulo Tributos; isolamento por schema — §9 do CLAUDE.md).
- **Tabela (sugerida):** `tributos.NotasFiscaisServico` (uma linha por NFS-e importada).
- **Colunas / mapeamento:**

| Coluna | Origem | Conversor / Mapeamento |
|---|---|---|
| `Id` | `NotaFiscalServicoId` | ValueConverter `NotaFiscalServicoId ⇄ Guid` |
| `TenantId` | `Guid` | direto (Global Query Filter + TenantInterceptor) |
| `ChaveAcesso` | `string` | direto (obrigatorio) |
| `PrestadorCnpj` | `string` | direto (obrigatorio) |
| `TomadorDocumento` | `string?` | direto (nullable) |
| `ValorServico` | `ValorMonetario` | conversor/owned do VO `ValorMonetario` |
| `ValorIss` | `ValorMonetario` | conversor/owned do VO `ValorMonetario` |
| `DataEmissao` | `DateOnly` | conversor `DateOnly ⇄ date` |
| `Competencia` | `Competencia` | conversor/owned do VO `Competencia` (ano/mes) |

- **Indices:**
  - **Unico:** `(TenantId, ChaveAcesso)` — garante deduplicacao por chave de acesso dentro do tenant (suporta I-10 / `ExistePorChaveAsync`).
  - **Consulta:** `(TenantId, Competencia)` e `(TenantId, PrestadorCnpj)` para o painel fiscal.
- **Fluent API** (sem data annotations no dominio). **Migrations por modulo** (Tributos).

---

## 10. Seguranca, Tenant e Auditoria

- **IMustHaveTenant:** `NotaFiscalServico` implementa `IMustHaveTenant` (`TenantId`). **Global Query Filter**
  por `TenantId` aplicado no DbContext; gravacao cross-tenant **lanca excecao** (TenantInterceptor).
- **RBAC (policies/roles):** acesso ao painel fiscal de NFS-e restrito a papeis do modulo Tributos
  (ex.: fiscal/auditor do ISS). Negar por padrao.
- **LGPD:** `PrestadorCnpj`/`TomadorDocumento` sao dados de identificacao fiscal; quando o tomador for
  pessoa fisica (CPF), aplicar minimizacao e **trilha de acesso** (quem leu, quando, por que). Sem dados
  sensiveis de saude/assistencia neste agregado.
- **Auditoria:** a importacao (insercao) e registrada pelo `AuditSaveChangesInterceptor` (JSON antes/depois,
  usuario/worker, IP/origem, timestamp) — trilha **imutavel** para o Tribunal de Contas.
- **Workers:** o `NfseSync` resolve o tenant por **iteracao explicita** (nao via JWT), preservando o
  isolamento ao chamar `Importar`/`Adicionar` no escopo do tenant corrente.

---

## 11. Integracoes Governamentais

- **Sistema:** **NFS-e Nacional / Ambiente de Dados Nacional (ADN)** — Receita Federal.
- **Natureza:** **PASSIVA** (ADR-0003 / §8 CLAUDE.md). **Nao emitimos nem assinamos** NFS-e; apenas
  consumimos os documentos ja emitidos e assinados no Ambiente Nacional. Padrao **ABRASF: REMOVIDO** do escopo.
- **ACL / Gateway:** `INfseNacionalGateway.ObterNotasEmitidasAsync(cnpjPrestador, desde, ct)` retorna
  `IReadOnlyList<NfseDocumento>` (DTO bruto), isolando o dominio do contrato externo.
- **Orquestracao:** `INfseSincronizador.SincronizarAsync(cnpjsPrestadores, desde, ct)` baixa, **deduplica
  por chave de acesso** (`ExistePorChaveAsync`) e persiste (`Adicionar`), retornando a quantidade de notas novas.
- **Worker:** `Tensorroot.Gov.Workers.NfseSync` — sincronizacao **diaria** por tenant.
- **Idempotencia:** garantida pela deduplicacao por `ChaveAcesso` + indice unico `(TenantId, ChaveAcesso)`;
  reexecucao da janela nao gera duplicatas.
- **Resiliencia (Polly):** a chamada ao ADN deve ter timeout + retry + circuit breaker (`Microsoft.Extensions.Http.Resilience`),
  com mapeamento explicito de erros, conforme §8/§11 do CLAUDE.md.
- **Base legal:** **LC 116/2003** (ISS) fundamenta a fiscalizacao a partir do `ValorIss` destacado.

---

## 12. Cenarios BDD

> Given/When/Then. Cada cenario vira teste de integracao.

- **Cenario 1 — Importacao valida emite evento.**
  - **Dado** dados validos de NFS-e (chave, CNPJ prestador, valores, competencia) para um tenant,
  - **Quando** `NotaFiscalServico.Importar(...)` e chamado,
  - **Entao** o registro nasce com os campos preenchidos e o evento `NotaFiscalServicoImportada` e emitido.

- **Cenario 2 — Chave de acesso vazia e rejeitada.**
  - **Dado** uma NFS-e com `chaveAcesso` vazio/branco,
  - **Quando** `Importar(...)` e chamado,
  - **Entao** lanca `ArgumentException` (I-1) e nada e persistido.

- **Cenario 3 — Valor do ISS nulo e rejeitado.**
  - **Dado** uma NFS-e com `valorIss` nulo,
  - **Quando** `Importar(...)` e chamado,
  - **Entao** lanca `ArgumentNullException` (I-4).

- **Cenario 4 — Deduplicacao por chave na sincronizacao.**
  - **Dado** que ja existe no tenant uma NFS-e com a chave `K` (`ExistePorChaveAsync(K)=true`),
  - **Quando** `SincronizarAsync` reprocessa um lote contendo `K`,
  - **Entao** `K` **nao** e reimportada e a contagem de novas notas a ignora.

- **Cenario 5 — Tomador opcional.**
  - **Dado** uma NFS-e sem `tomadorDocumento` (null),
  - **Quando** `Importar(...)` e chamado,
  - **Entao** a nota e importada com `TomadorDocumento = null` (I-6).

- **Cenario 6 — Isolamento de tenant.**
  - **Dado** notas importadas para o tenant A,
  - **Quando** o tenant B consulta `ExistePorChaveAsync` com a mesma chave,
  - **Entao** o filtro global retorna `false` (a chave de A nao vaza para B).

---

## 13. Casos de Borda

> Enumerados (cada um vira teste).

- **B-1.** `ChaveAcesso` com apenas espacos em branco → `ArgumentException` (I-1).
- **B-2.** `PrestadorCnpj` vazio/branco → `ArgumentException` (I-2).
- **B-3.** `valorServico` ou `valorIss` nulo → `ArgumentNullException` (I-3/I-4).
- **B-4.** `competencia` nula → `ArgumentNullException` (I-5).
- **B-5.** `ValorIss` igual a zero (servico isento/imune) → importacao permitida (apenas nao-nulo e exigido).
- **B-6.** `ValorIss` maior que `ValorServico` → o agregado **nao** valida coerencia de valores; importado como veio do ADN (fidelidade ao documento nacional).
- **B-7.** Mesma chave recebida em duas execucoes (`SincronizarAsync`) → importada uma unica vez (dedup + indice unico).
- **B-8.** Lote do ADN vazio para um CNPJ → `SincronizarAsync` retorna `0` para aquele CNPJ.
- **B-9.** Cancelamento via `CancellationToken` durante a sincronizacao → `OperationCanceledException`; nenhuma nota parcial inconsistente.
- **B-10.** `Competencia(Ano, Mes)` com mes fora de 1..12 vindo do ADN → rejeitado pelo VO `Competencia` ao mapear `NfseDocumento`.
- **B-11.** Falha/timeout do `INfseNacionalGateway` → tratada com resiliencia (Polly) + ACL; nao corrompe o estado local.

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial das regras do agregado `NotaFiscalServico` (read model NFS-e/ADN), fiel a `NotaFiscalServico.cs` e `NfseContratos.cs`: factory `Importar`, evento de dominio `NotaFiscalServicoImportada`, contratos `INfseNacionalGateway`/`INotaFiscalServicoRepository`/`INfseSincronizador`. Integracao passiva (ADR-0003), ISS (LC 116/2003). |

<!-- manifest
commands: 
queries: 
domainEvents: NotaFiscalServicoImportada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
