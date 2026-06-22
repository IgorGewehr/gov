---
modulo: Transparencia
agregado: RemessaTce
contexto: Transparencia (transparência ativa/passiva, dados abertos e prestação de contas ao TCE-RS/SICONFI) — papel CONSUMIDOR
poder: Ambos
schema: transparencia
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "LC 101/2000 (LRF) art. 48 e 48-A (transparência da gestão fiscal)"
  - "LC 101/2000 (LRF) art. 23 §3º (atraso bloqueia transferências voluntárias)"
  - "LC 131/2009 (disponibilização da execução em tempo real)"
  - "Lei 12.527/2011 (LAI) art. 8 §3º (formato aberto, estruturado e legível por máquina)"
  - "Decretos 7.185/2010 e 10.540/2020 (SIAFIC — tempo real até o 1º dia útil)"
  - "Resoluções TCE-RS (SIAPC/PAD, e-Validador, leiautes versionados, RDI/SICOE)"
---

# RemessaTce (Prestação de Contas ao TCE-RS) — Regras-as-Code (Rules-as-Code)

> **Fonte da verdade.** Este `*.rules.md` é **normativo e versionado**. O domínio, a aplicação,
> a persistência e os testes do agregado `RemessaTce` são **gerados e mantidos a partir daqui**.
> Bug, ajuste ou nova regra ⇒ edita-se **este arquivo**; o código é consequência.
>
> Pacote de arquivos por leiaute versionado enviado ao **TCE-RS (SIAPC/PAD)**: gerado a partir
> de itens já materializados na transparência (consolidação de dados consumidos de outros
> módulos), **validado localmente** pelo **e-Validador** (que emite o **RDI**), e só então
> **transmitido**. Imutável e retido com **hash de integridade** verificável. O atraso da
> remessa pode **bloquear transferências voluntárias** (LRF art. 23 §3º). Este módulo é
> **CONSUMIDOR**: não é fonte primária; cross-module ocorre **exclusivamente** via Integration Events.

---

## 1. Linguagem Ubíqua

Os identificadores entre parênteses (sem acento, PT-BR no domínio) são **VINCULANTES**: o gerador de código DEVE usá-los.

| Termo (identificador-no-código) | Definição |
|---|---|
| **Remessa ao TCE** (`RemessaTce`) | Pacote de arquivos, por leiaute, enviado ao Tribunal de Contas do Estado (TCE-RS) para prestação de contas. Raiz de agregado. |
| **RemessaTceId** (`RemessaTceId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| **TenantId** (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro — isolamento multi-tenant. |
| **Arquivo de Remessa** (`ArquivoRemessa`) | Arquivo componente do pacote (entidade-filha), conforme o leiaute. |
| **Registro de Leiaute** (`RegistroLeiaute`) | Linha/registro estruturado de um arquivo, conforme a especificação do leiaute (entidade-filha). |
| **Resultado de Validação** (`ResultadoValidacao`) | Resultado do e-Validador (o RDI) com erros/avisos por arquivo/registro (entidade-filha). |
| **Período** (`Periodo`) | Intervalo de competência da remessa (mês/bimestre/ano + ano de exercício). Value Object. |
| **Leiaute** (`Leiaute`) | Especificação versionada de layout da remessa (com `Versao`). Value Object. |
| **Hash de Integridade** (`HashIntegridade`) | Resumo criptográfico do conteúdo do pacote, verificável, para imutabilidade/retenção. Value Object. |
| **RDI** (`Rdi` / `ResultadoValidacao`) | Relatório de Dados e Informações: resultado da validação local. |
| **e-Validador** (`EValidador` / `IEValidadorTce`) | Validador local de remessas TCE-RS que gera o RDI. |
| **SIAPC/PAD** (`Siapc` / `Pad`) | Sistema de Informações para Auditoria e Prestação de Contas / Processo de Auditoria a Distância (canal de envio). |
| **SICOE** (`Sicoe`) | Sistema de comunicação/recebimento das remessas no TCE-RS. |
| **Situação** (`Situacao` : `SituacaoRemessaTce`) | Estado atual da remessa no ciclo `Gerada → Validada → Enviada → Homologada/Rejeitada`. |
| **Gerar Remessa** (`GerarRemessa`) | Consolidar os itens e montar o pacote (nasce em `Gerada`). |
| **Validar** (`Validar` / `RegistrarResultadoValidacao`) | Aplicar o RDI; sem erro → `Validada`; com erro → `Rejeitada`. |
| **Enviar ao TCE** (`EnviarTce`) | Transmitir o pacote `Validada` ao SIAPC/PAD (passa a `Enviada`). |
| **Homologar** (`Homologar`) | Registrar a homologação pelo TCE (passa a `Homologada`). |
| **Vencer Prazo** (`VencerPrazo`) | Marcar o vencimento do prazo legal sem envio (emite alerta de risco de bloqueio LRF). |
| **Prazo de Remessa** (`PrazoRemessa` / `DataLimite`) | Data-limite legal/parametrizada por tenant para o envio da remessa do `Periodo`. |
| **EBT** (`Ebt`) | Escala Brasil Transparente (avaliação CGU) — contexto de transparência ativa do módulo. |

---

## 2. Modelo

- **Identidade:** `RemessaTceId` — `readonly record struct RemessaTceId(Guid Value)`; fábrica `RemessaTceId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `RemessaTce : AggregateRoot<RemessaTceId>, IMustHaveTenant` (`sealed`).
- **Construção:** construtor privado sem parâmetros (EF) + construtor privado parametrizado + **factory** `GerarRemessa(...)`. A entidade **nasce válida** e em `Gerada`.

### 2.1 Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `Id` | `RemessaTceId` | Identidade do agregado. | `private set` |
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. `IMustHaveTenant`. | `private set` |
| `Periodo` | `Periodo` (VO) | Competência/exercício da remessa. | `private set` |
| `Leiaute` | `Leiaute` (VO) | Especificação versionada do layout. | `private set` |
| `Situacao` | `SituacaoRemessaTce` (enum) | Estado atual da remessa. | `private set` |
| `HashIntegridade` | `HashIntegridade?` (VO) | Hash do pacote (definido na geração; obrigatório para envio). | `private set` |
| `DataLimite` | `DateOnly` | Prazo legal/parametrizado de envio do `Periodo`. | `private set` |
| `DataGeracao` | `DateOnly` | Data de geração do pacote. | `private set` |
| `DataEnvio` | `DateOnly?` | Data de transmissão ao SIAPC/PAD (nula antes do envio). | `private set` |
| `Arquivos` | `IReadOnlyCollection<ArquivoRemessa>` | Arquivos componentes (entidades-filhas). | coleção encapsulada |
| `ResultadoValidacao` | `ResultadoValidacao?` | RDI mais recente (nulo antes de validar). | `private set` |

Todos os *setters* são `private set`. As mutações ocorrem **apenas** pelos métodos de comportamento (§5), preservando invariantes. A coleção `Arquivos` é exposta como somente-leitura sobre um campo `List<ArquivoRemessa>` privado.

### 2.2 Entidades-filhas

**`ArquivoRemessa` (entidade):**
- `NomeArquivo : string` (não vazio), `Conteudo : ReadOnlyMemory<byte>` (ou referência a blob), `Hash : HashIntegridade`.
- `Registros : IReadOnlyCollection<RegistroLeiaute>` — linhas estruturadas do arquivo conforme o leiaute.

**`RegistroLeiaute` (entidade):**
- `Tipo : string` (tipo de registro do leiaute), `Conteudo : string` (linha conforme a especificação).
- Fatiamento de XMLs/arquivos pesados feito com **`Span<T>`/`Memory<T>`** (sem alocação desnecessária).

**`ResultadoValidacao` (entidade — o RDI):**
- `PossuiErro : bool`, `QuantidadeErros : int`, `QuantidadeAvisos : int`, `ValidadoEm : DateTimeOffset`, `LeiauteVersao : string`.
- `Ocorrencias : IReadOnlyCollection<OcorrenciaValidacao>` (linha, arquivo, severidade, mensagem).

### 2.3 Value Objects

**`Periodo` (`ValueObject`):**
- Propriedades: `Exercicio : int` (>= 1900), `Tipo : TipoPeriodo` (`Mensal`/`Bimestre`/`Quadrimestre`/`Anual`), `Numero : int` (1..12 para mês; 1..6 para bimestre; 1..3 para quadrimestre; ignorado/0 para anual).
- Fábrica: `Periodo.De(int exercicio, TipoPeriodo tipo, int numero)` — `ArgumentOutOfRangeException` se as fronteiras do tipo forem violadas.
- Igualdade por valor: `(Exercicio, Tipo, Numero)`. `ToString()` → ex.: `"2026-B03"`, `"2026-12"` (cultura invariante).

**`Leiaute` (`ValueObject`):**
- Propriedades: `Codigo : string` (não vazio), `Versao : string` (não vazio; ex.: `"2026.1"`).
- Fábrica: `Leiaute.De(string codigo, string versao)` — `ArgumentException` se vazios.
- Igualdade por valor: `(Codigo, Versao)`.

**`HashIntegridade` (`ValueObject`):**
- Propriedades: `Algoritmo : string` (ex.: `"SHA-256"`), `Valor : string` (hex, não vazio).
- Fábrica: `HashIntegridade.De(string algoritmo, string valor)` — `ArgumentException` se vazios.
- Igualdade por valor: `(Algoritmo, Valor)`. `Confere(ReadOnlySpan<byte> conteudo)` → recomputa e compara.

### 2.4 Enum `SituacaoRemessaTce`

| Valor | Numérico | Descrição |
|---|---|---|
| `Gerada` | 1 | Pacote consolidado e montado (estado inicial), ainda não validado. |
| `Validada` | 2 | RDI sem erro e hash íntegro — apta a envio. |
| `Enviada` | 3 | Transmitida ao SIAPC/PAD. |
| `Homologada` | 4 | Homologada pelo TCE-RS (terminal de sucesso). |
| `Rejeitada` | 5 | RDI com erro — envio bloqueado (terminal de falha; admite regeração). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrada** = { `Homologada`, `Rejeitada` }.
> - **Enviável** = { `Validada` } (apenas).
> - **Validável** = { `Gerada` }.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Toda `RemessaTce` pertence a exatamente um tenant (`IMustHaveTenant`); `TenantId` é imutável após a criação.
- **I-2.** `Periodo` e `Leiaute` são obrigatórios e não nulos na geração (`ArgumentNullException.ThrowIfNull`).
- **I-3.** Ao ser gerada (`GerarRemessa`), a remessa nasce em `Gerada`, com `HashIntegridade` calculado sobre o pacote, e emite `RemessaGerada` (LRF arts. 48/48-A).
- **I-4.** A validação só ocorre a partir de `Gerada` (estado **Validável**); caso contrário, `InvalidOperationException`.
- **I-5.** Validação **com erro** no RDI ⇒ transita para `Rejeitada` e emite `RemessaRejeitada`; envio fica **bloqueado** (regra crítica do módulo).
- **I-6.** Validação **sem erro** (RDI limpo) ⇒ transita para `Validada` e emite `RemessaValidada`.
- **I-7.** A remessa **só transita a `Enviada`** após validação **sem erro** (situação == `Validada`) **e** com `HashIntegridade` presente/íntegro — invariante central (README §5). Caso contrário, `InvalidOperationException`.
- **I-8.** A homologação só ocorre a partir de `Enviada`; caso contrário, `InvalidOperationException`. Homologar leva a `Homologada` e emite `RemessaHomologada` (evento de domínio próprio da remessa; não confundir com `DeclaracaoHomologada`, do agregado `DeclaracaoFiscal`).
- **I-9.** `HashIntegridade` torna o pacote **imutável**: alterar `Arquivos`/`Registros` após a geração invalida a remessa (exige regeração) — preserva retenção verificável.
- **I-10.** Estados **terminais** (`Homologada`, `Rejeitada`) não admitem `Validar`/`EnviarTce`/`Homologar`.
- **I-11.** `DataLimite` é **parametrizável por tenant** (nunca *hardcoded*); deriva do `Periodo` conforme a Resolução TCE-RS vigente.
- **I-12.** `VencerPrazo` só se aplica a remessa **não enviada** (situação ∉ {`Enviada`,`Homologada`}) cujo `DataLimite` já passou; emite `PrazoRemessaVencido` (alerta de risco de bloqueio de transferências — LRF art. 23 §3º). É **idempotente** (não duplica o alerta).
- **I-13.** Como módulo **CONSUMIDOR**, o agregado **não** materializa fontes primárias: os itens consolidados provêm de `PublicacaoTransparencia` (já alimentada por Integration Events de outros módulos).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento de domínio emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| _(inexistente)_ | `GerarRemessa` | `Gerada` | `Periodo` e `Leiaute` não nulos; hash calculado | `RemessaGerada` |
| `Gerada` | `Validar` (RDI **sem** erro) | `Validada` | `ResultadoValidacao.PossuiErro == false` | `RemessaValidada` |
| `Gerada` | `Validar` (RDI **com** erro) | `Rejeitada` | `ResultadoValidacao.PossuiErro == true` | `RemessaRejeitada` |
| `Validada` | `EnviarTce` | `Enviada` | situação == `Validada` **e** `HashIntegridade` íntegro | `RemessaEnviadaTce` |
| `Enviada` | `Homologar` | `Homologada` | situação == `Enviada` | `RemessaHomologada` |
| `Gerada` \| `Validada` (não enviada, `hoje > DataLimite`) | `VencerPrazo(hoje)` | _(mantém)_ | não enviada **e** prazo vencido (idempotente) | `PrazoRemessaVencido` |
| `Homologada` \| `Rejeitada` | `Validar` / `EnviarTce` / `Homologar` | — | bloqueado (terminal) → `InvalidOperationException` | — |
| `Gerada` (RDI com erro) | `EnviarTce` | — | bloqueado (situação != `Validada`) → `InvalidOperationException` | — |

> Observações:
> - `EnviarTce` chama a verificação de integridade do hash **antes** da transição.
> - `Rejeitada` é terminal **neste agregado**; a correção gera **nova** `RemessaTce` (regeração), preservando a trilha da remessa rejeitada.
> - `VencerPrazo` não altera a `Situacao` (apenas sinaliza risco), mas registra o evento uma única vez por remessa.

---

## 5. Comandos (escrita)

### 5.1 `GerarRemessaTce` — Consolidar e montar o pacote

- **Comando (DTO):** `GerarRemessaTceCommand : ICommand<Guid>`
  - `Exercicio : int`
  - `TipoPeriodo : TipoPeriodo`
  - `NumeroPeriodo : int`
  - `LeiauteCodigo : string`
  - `LeiauteVersao : string`
- **Dependências do handler:** `IPublicacaoTransparenciaRepository` (itens consolidados), `IRemessaTceRepository`, `ILeiauteCatalogo` (versão vigente/`DataLimite`), `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:**
  - Comando válido segundo `GerarRemessaTceValidator` (§8).
  - Existem itens consolidados em `PublicacaoTransparencia` para o `Periodo` no tenant atual; senão `InvalidOperationException("Não há itens consolidados para o período.")`.
  - Leiaute/versão suportados pelo `ILeiauteCatalogo`; `DataLimite` derivada do `Periodo`.
- **Efeito:** monta `ArquivoRemessa`/`RegistroLeiaute` (fatiamento com `Span<T>`), calcula `HashIntegridade`, cria via `RemessaTce.GerarRemessa(tenant.TenantId, Periodo.De(...), Leiaute.De(...), dataLimite, hoje, arquivos)`; `repo.Adicionar(remessa)`; `SaveChangesAsync`.
- **Pós-condições:** nova `RemessaTce` em `Gerada`, carimbada com `TenantId`; evento `RemessaGerada` registrado; retorna `Guid` = `remessa.Id.Value`.
- **Exceções:** `ValidationException` (pipeline); `InvalidOperationException` (sem itens / leiaute não suportado); `ArgumentOutOfRangeException`/`ArgumentException` (VOs).
- **Evento de domínio:** `RemessaGerada`.

### 5.2 `ValidarRemessaTce` — Aplicar o RDI (e-Validador)

- **Comando (DTO):** `ValidarRemessaTceCommand(Guid RemessaTceId) : ICommand`.
- **Dependências do handler:** `IRemessaTceRepository`, `IEValidadorTce` (ACL local), `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; remessa existe (`ObterPorIdAsync`), senão `InvalidOperationException("Remessa não encontrada.")`; situação == `Gerada` (I-4).
- **Efeito:** invoca `IEValidadorTce.Validar(remessa)` → `ResultadoValidacao` (RDI); chama `remessa.RegistrarResultadoValidacao(rdi)` — transita para `Validada` (sem erro) ou `Rejeitada` (com erro); `SaveChangesAsync`.
- **Pós-condições:** `ResultadoValidacao` anexado; situação `Validada` **ou** `Rejeitada`; evento `RemessaValidada` **ou** `RemessaRejeitada`.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrada ou situação ≠ `Gerada`).
- **Evento de domínio:** `RemessaValidada` | `RemessaRejeitada`.
- **Evento de integração (publica em caso de rejeição):** `RemessaRejeitadaIntegrationEvent`.

### 5.3 `EnviarRemessaTce` — Transmitir ao SIAPC/PAD

- **Comando (DTO):** `EnviarRemessaTceCommand(Guid RemessaTceId) : ICommand`.
- **Dependências do handler:** `IRemessaTceRepository`, `ISiapcPadGateway` (ACL + Polly), `ICertificadoTenantProvider` (Azure Key Vault, A1 por tenant), `IUnitOfWork`, `IPublisher`/Outbox, `ITenantContext`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; remessa existe; situação == `Validada` **e** `HashIntegridade` íntegro (I-7).
- **Efeito:** assina/transmite o pacote ao SIAPC/PAD via `ISiapcPadGateway` (idempotente por `RemessaTceId`); `remessa.EnviarTce(hoje)` → `Enviada`; `SaveChangesAsync`; publica **Integration Event** `RemessaEnviadaTceIntegrationEvent` (Outbox).
- **Pós-condições:** situação `Enviada`, `DataEnvio` preenchida; `RemessaEnviadaTce` (domínio) + `RemessaEnviadaTceIntegrationEvent` (integração).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrada / situação ≠ `Validada` / hash inválido); erros do gateway mapeados pela ACL.
- **Evento de domínio:** `RemessaEnviadaTce`.
- **Evento de integração (publica):** `RemessaEnviadaTceIntegrationEvent`.

### 5.4 `HomologarRemessaTce` — Registrar homologação do TCE

- **Comando (DTO):** `HomologarRemessaTceCommand(Guid RemessaTceId) : ICommand`.
- **Dependências do handler:** `IRemessaTceRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; remessa existe; situação == `Enviada` (I-8).
- **Efeito:** `remessa.Homologar()` → `Homologada`; `SaveChangesAsync`.
- **Pós-condições:** situação `Homologada`; evento `RemessaHomologada`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrada / situação ≠ `Enviada`).
- **Evento de domínio:** `RemessaHomologada`.

### 5.5 `VencerPrazoRemessaTce` — Sinalizar prazo vencido (acionado por Worker)

- **Comando (DTO):** `VencerPrazoRemessaTceCommand(Guid RemessaTceId) : ICommand`.
- **Dependências do handler:** `IRemessaTceRepository`, `IUnitOfWork`, `IPublisher`/Outbox, `TimeProvider`.
- **Pré-condições:** `request` não nulo; remessa existe; situação ∉ {`Enviada`,`Homologada`} **e** `hoje > DataLimite`; alerta ainda não emitido (idempotência — I-12).
- **Efeito:** `remessa.VencerPrazo(hoje)`; `SaveChangesAsync`; publica **Integration Event** `PrazoRemessaVencidoIntegrationEvent` (Outbox).
- **Pós-condições:** evento `PrazoRemessaVencido` (domínio) + `PrazoRemessaVencidoIntegrationEvent` (integração) — alerta de risco de bloqueio de transferências voluntárias (LRF art. 23 §3º).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrada / prazo não vencido / já enviada / alerta já emitido).
- **Evento de domínio:** `PrazoRemessaVencido`.
- **Evento de integração (publica):** `PrazoRemessaVencidoIntegrationEvent`.

---

## 6. Consultas (leitura)

### 6.1 `ObterRemessaTcePorId`

- **Query:** `ObterRemessaTcePorIdQuery(Guid RemessaTceId) : IQuery<RemessaTceDetalhe?>`.
- **Handler:** `ObterRemessaTcePorIdHandler(IRemessaTceRepository)`.
- **Projeção (DTO):** `RemessaTceDetalhe(Guid Id, string Periodo, string Leiaute, string Situacao, string? HashIntegridade, DateOnly DataLimite, DateOnly DataGeracao, DateOnly? DataEnvio, bool PossuiErroValidacao, int QuantidadeErros)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 `ListarRemessasTcePorPeriodo`

- **Query:** `ListarRemessasTcePorPeriodoQuery(int Exercicio, TipoPeriodo? Tipo, SituacaoRemessaTce? Situacao) : IQuery<IReadOnlyList<RemessaTceResumo>>`.
- **Handler:** `ListarRemessasTcePorPeriodoHandler(IRemessaTceRepository)`.
- **Projeção (DTO):** `RemessaTceResumo(Guid Id, string Periodo, string LeiauteVersao, string Situacao, DateOnly DataLimite, DateOnly? DataEnvio)`.
- **Filtros:** por `Exercicio` (+ `Tipo`/`Situacao` opcionais); **tenant-scoped**; paginação padrão do módulo.
- **Pré-condições:** `request` não nulo.

---

## 7. Eventos

### 7.1 Domínio (in-process, MediatR — assembly `...Transparencia.Domain`)

| Evento | Payload | Emitido em |
|---|---|---|
| `RemessaGerada` | `(RemessaTceId RemessaTceId, string Periodo, string LeiauteVersao)` | `GerarRemessa`. |
| `RemessaValidada` | `(RemessaTceId RemessaTceId)` | `RegistrarResultadoValidacao` (RDI sem erro). |
| `RemessaRejeitada` | `(RemessaTceId RemessaTceId, int QuantidadeErros)` | `RegistrarResultadoValidacao` (RDI com erro). |
| `RemessaEnviadaTce` | `(RemessaTceId RemessaTceId, DateOnly DataEnvio)` | `EnviarTce`. |
| `RemessaHomologada` | `(RemessaTceId RemessaTceId)` | `Homologar`. |
| `PrazoRemessaVencido` | `(RemessaTceId RemessaTceId, DateOnly DataLimite)` | `VencerPrazo`. |

Todos implementam `IDomainEvent` e são `sealed record`.

### 7.2 Integração — **publica** (via `Modules.Transparencia.Contracts`, Outbox)

| Evento | Payload | Publicado por |
|---|---|---|
| `RemessaEnviadaTceIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid RemessaTceId, string Periodo, DateOnly DataEnvio)` | `EnviarRemessaTceHandler`. |
| `RemessaRejeitadaIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid RemessaTceId, string Periodo, int QuantidadeErros)` | `ValidarRemessaTceHandler` (rejeição). |
| `PrazoRemessaVencidoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid RemessaTceId, string Periodo, DateOnly DataLimite)` | `VencerPrazoRemessaTceHandler` (alerta de risco LRF art. 23 §3º). |

### 7.3 Integração — **consome**

Nenhum Integration Event é **consumido diretamente** pelo agregado `RemessaTce`. Os itens que compõem a remessa são consolidados em `PublicacaoTransparencia` a partir dos eventos assinados pelo módulo (Finanças, Administração, RecursosHumanos, Tributos, Patrimônio) — ver README §7; a composição é externa a este agregado.

---

## 8. Validações (FluentValidation)

### `GerarRemessaTceValidator : AbstractValidator<GerarRemessaTceCommand>`

| Campo | Regra | Mensagem (padrão FluentValidation) |
|---|---|---|
| `Exercicio` | `GreaterThanOrEqualTo(1900)` | "'Exercicio' must be greater than or equal to '1900'." |
| `TipoPeriodo` | `IsInEnum()` | "'Tipo Periodo' has a range of values which does not include ..." |
| `NumeroPeriodo` | `GreaterThanOrEqualTo(0)` (fronteiras finais validadas no VO `Periodo`) | "'Numero Periodo' must be greater than or equal to '0'." |
| `LeiauteCodigo` | `NotEmpty()` + `MaximumLength(40)` | "'Leiaute Codigo' must not be empty." |
| `LeiauteVersao` | `NotEmpty()` + `MaximumLength(20)` | "'Leiaute Versao' must not be empty." |

### `ValidarRemessaTceValidator : AbstractValidator<ValidarRemessaTceCommand>`

| Campo | Regra | Mensagem |
|---|---|---|
| `RemessaTceId` | `NotEmpty()` | "'Remessa Tce Id' must not be empty." |

### `EnviarRemessaTceValidator` / `HomologarRemessaTceValidator` / `VencerPrazoRemessaTceValidator`

| Campo | Regra | Mensagem |
|---|---|---|
| `RemessaTceId` | `NotEmpty()` | "'Remessa Tce Id' must not be empty." |

> A consistência de `NumeroPeriodo` com `TipoPeriodo` (1..12 mês, 1..6 bimestre, 1..3 quadrimestre) é garantida no Value Object `Periodo` (defesa em profundidade), além do validador.

---

## 9. Persistência (EF Core 8)

- **DbContext:** `TransparenciaDbContext` — `Schema => "transparencia"`. **Migrations por módulo.**
- **Tabela raiz:** `transparencia.RemessasTce`. **Filhas:** `transparencia.ArquivosRemessa`, `transparencia.RegistrosLeiaute`, `transparencia.ResultadosValidacao` (owned/has-many com `OnDelete(Cascade)` dentro do agregado).
- **Configuração:** `RemessaTceConfiguration : IEntityTypeConfiguration<RemessaTce>` (Fluent API; sem data annotations no domínio).

| Coluna (raiz) | Tipo de coluna | Conversor / mapeamento |
|---|---|---|
| `Id` | `uniqueidentifier` (PK) | `id => id.Value` / `value => new RemessaTceId(value)`; `ValueGeneratedNever()`. |
| `TenantId` | `uniqueidentifier` | primitivo (carimbado pelo TenantInterceptor); alvo do Global Query Filter. |
| `Periodo` | owned (`Exercicio:int`, `Tipo:nvarchar(20)`, `Numero:int`) | `OwnsOne` (VO `Periodo`). |
| `Leiaute` | owned (`Codigo:nvarchar(40)`, `Versao:nvarchar(20)`) | `OwnsOne` (VO `Leiaute`). |
| `Situacao` | `nvarchar(20)` | `HasConversion<string>()` (enum como string). |
| `HashIntegridade` | owned (`Algoritmo:nvarchar(20)`, `Valor:nvarchar(128)`), nulável | `OwnsOne` (VO `HashIntegridade`). |
| `DataLimite` | `date` | `DateOnly` (nativo EF Core 8). |
| `DataGeracao` | `date` | `DateOnly`. |
| `DataEnvio` | `date?` | `DateOnly?`. |

- **Índices:**
  - PK em `Id`.
  - `IX_RemessasTce_TenantId_Exercicio_Tipo_Numero` em `(TenantId, Periodo_Exercicio, Periodo_Tipo, Periodo_Numero)` para a consulta por período.
  - Índice **único** `UX_RemessasTce_Tenant_Periodo_Leiaute` em `(TenantId, Periodo_Exercicio, Periodo_Tipo, Periodo_Numero, Leiaute_Codigo, Leiaute_Versao)` desconsiderando remessas `Rejeitada` (uma remessa vigente por período/leiaute/tenant).
  - `IX_RemessasTce_TenantId_Situacao_DataLimite` em `(TenantId, Situacao, DataLimite)` para a varredura de prazos pelo Worker.
- **Conversores (VO/Id):** Fluent API. **Imutabilidade:** arquivos/registros são gravados na geração; o `HashIntegridade` é persistido para verificação/retenção.
- **Outbox / Auditoria:** `TransparenciaDbContext` herda a base com tabela **Outbox**, `AuditSaveChangesInterceptor` e `TenantInterceptor`.

---

## 10. Segurança, Tenant e Auditoria

- **`IMustHaveTenant`:** `RemessaTce` implementa; `TenantId` carimbado na inserção pelo `TenantInterceptor`. **Global Query Filter** por `TenantId` em todas as leituras. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT (ApiHost) ou por iteração explícita (Workers); jamais aceito do cliente.
- **RBAC (policy-based, negar por padrão):** geração/validação/envio/homologação exigem o papel `transparencia.remeter`; consultas exigem leitura de transparência. Módulo Transparencia é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **Certificado A1:** assinatura/transmissão ao SIAPC/PAD usa certificado **A1 (.pfx)** do **Azure Key Vault**, **por tenant** (`ICertificadoTenantProvider`); **nenhum** segredo/certificado no repositório.
- **Auditoria imutável:** toda mutação (`GerarRemessa`, `Validar`, `EnviarTce`, `Homologar`, `VencerPrazo`) gera trilha (JSON antes/depois, usuário, IP, timestamp) via `AuditSaveChangesInterceptor`, para o **Tribunal de Contas (TCE-RS)**. Arquivos de remessa preservados com `HashIntegridade` para auditoria.
- **LGPD:** itens consolidados que contenham dado pessoal (ex.: remuneração de servidores) devem estar **anonimizados** conforme a política do módulo antes de comporem publicações abertas; a remessa ao TCE segue base legal de prestação de contas. Acesso a dados pré-anonimização registra base legal e finalidade.
- **Anti-SQLi:** acesso exclusivamente via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Sistema:** TCE-RS — **SIAPC/PAD** (envio) + **SICOE** (recebimento); validação local pelo **e-Validador** (gera o **RDI**).
- **Layout/versão:** remessa por **leiaute versionado** (`Leiaute(Codigo, Versao)`), conforme **Resolução TCE-RS** vigente; confirmar a versão atual antes de gerar (CLAUDE.md §16).
- **Prazo legal:** `DataLimite` parametrizada por tenant a partir do `Periodo` (Resoluções TCE-RS); atraso ⇒ alerta `PrazoRemessaVencido` (risco de bloqueio de transferências voluntárias — LRF art. 23 §3º).
- **Resiliência (Polly):** envio com **timeout, retry e circuit breaker** via `ISiapcPadGateway`; **idempotente** por `RemessaTceId` (reenvio não duplica).
- **ACL (Anti-Corruption Layer):** `IEValidadorTce` e `ISiapcPadGateway` isolam o domínio dos contratos externos; mapeamento explícito de erros do TCE para resultados de domínio.
- **Outbox:** Integration Events (`RemessaEnviadaTce`, `RemessaRejeitada`, `PrazoRemessaVencido`) publicados transacionalmente com o estado.
- **Certificado:** **A1** por tenant via **Azure Key Vault** (assinatura do pacote).
- **Performance:** fatiamento de XMLs pesados com **`Span<T>`/`Memory<T>`** (sem alocação desnecessária) na montagem/validação.

---

## 12. Cenários BDD

Cada cenário Given/When/Then vira um teste de integração (alinhado ao README §8).

**Cenário 1 — Geração da remessa.**
- **Dado** itens consolidados em `PublicacaoTransparencia` para o `Periodo` no tenant
- **Quando** executo `GerarRemessaTce(Exercicio=2026, TipoPeriodo=Bimestre, NumeroPeriodo=3, LeiauteCodigo="SIAPC", LeiauteVersao="2026.1")`
- **Então** é criada uma `RemessaTce` em `Gerada`, com `HashIntegridade` calculado, e o evento `RemessaGerada` é emitido; o comando retorna o `Guid`.

**Cenário 2 — Remessa com erro de validação (README Cenário 2).**
- **Dada** uma `RemessaTce` em `Gerada`
- **Quando** o e-Validador retorna o RDI com erros e executo `ValidarRemessaTce`
- **Então** a remessa transita para `Rejeitada`, `RemessaRejeitada` (domínio) e `RemessaRejeitadaIntegrationEvent` são publicados e o envio é **bloqueado**.

**Cenário 3 — Remessa válida enviada (README Cenário 3).**
- **Dada** uma `RemessaTce` `Validada` com RDI sem erro e hash íntegro
- **Quando** o Worker transmite ao SIAPC/PAD via `EnviarRemessaTce`
- **Então** a remessa vira `Enviada`, `DataEnvio` é preenchida e `RemessaEnviadaTce` (domínio) + `RemessaEnviadaTceIntegrationEvent` são publicados.

**Cenário 4 — Envio bloqueado sem validação limpa.**
- **Dada** uma `RemessaTce` em `Gerada` (ou `Rejeitada`)
- **Quando** executo `EnviarRemessaTce`
- **Então** ocorre `InvalidOperationException` (situação ≠ `Validada`) e nada é transmitido.

**Cenário 5 — Homologação pelo TCE.**
- **Dada** uma `RemessaTce` `Enviada`
- **Quando** executo `HomologarRemessaTce`
- **Então** a situação passa a `Homologada` e `RemessaHomologada` é emitido.

**Cenário 6 — Prazo de remessa vencido (README Cenário 6).**
- **Dada** uma `RemessaTce` não enviada após o `DataLimite` do `Periodo`
- **Quando** o Worker verifica os vencimentos e executa `VencerPrazoRemessaTce`
- **Então** `PrazoRemessaVencido` (domínio) + `PrazoRemessaVencidoIntegrationEvent` são publicados, alertando risco de bloqueio de transferências voluntárias (LRF art. 23 §3º).

**Cenário 7 — Integridade do pacote.**
- **Dada** uma `RemessaTce` `Validada`
- **Quando** o conteúdo do pacote não confere com o `HashIntegridade` registrado
- **Então** `EnviarRemessaTce` lança `InvalidOperationException` (hash inválido) e o envio é bloqueado.

**Cenário 8 — Isolamento de tenant.**
- **Dadas** remessas do tenant A e do tenant B
- **Quando** executo `ListarRemessasTcePorPeriodo` no contexto do tenant A
- **Então** retornam **apenas** as remessas do tenant A.

---

## 13. Casos de Borda

Cada item vira um teste.

- **CB-1.** `Periodo.De(2026, Bimestre, 7)` → `ArgumentOutOfRangeException` (bimestre 1..6).
- **CB-2.** `Periodo.De(2026, Mensal, 13)` → `ArgumentOutOfRangeException` (mês 1..12).
- **CB-3.** `Leiaute.De("", "2026.1")` ou versão vazia → `ArgumentException`.
- **CB-4.** `GerarRemessaTce` sem itens consolidados no período → `InvalidOperationException("Não há itens consolidados para o período.")`.
- **CB-5.** `ValidarRemessaTce` sobre remessa `Validada`/`Enviada`/`Homologada`/`Rejeitada` → `InvalidOperationException` (situação ≠ `Gerada`).
- **CB-6.** RDI sem erro porém com avisos (`QuantidadeAvisos > 0`, `PossuiErro == false`) → transita para `Validada` (avisos não bloqueiam).
- **CB-7.** `EnviarRemessaTce` com `HashIntegridade == null` → `InvalidOperationException` (pacote sem hash).
- **CB-8.** `Homologar` sobre remessa `Validada` (não enviada) → `InvalidOperationException` (situação ≠ `Enviada`).
- **CB-9.** `VencerPrazo(hoje == DataLimite)` → não dispara (exige `hoje > DataLimite`).
- **CB-10.** `VencerPrazo` chamado duas vezes → emite `PrazoRemessaVencido` **uma única vez** (idempotência — I-12).
- **CB-11.** `VencerPrazo` sobre remessa já `Enviada`/`Homologada` → `InvalidOperationException` (não há risco de bloqueio).
- **CB-12.** Reenvio do mesmo pacote ao SIAPC/PAD → idempotente por `RemessaTceId` (não duplica no TCE).
- **CB-13.** Correção de remessa `Rejeitada` → cria **nova** `RemessaTce` (regeração); a rejeitada permanece para trilha/auditoria.
- **CB-14.** Tentar gravar/obter remessa de outro tenant → barrado pelo Global Query Filter e pelo `TenantInterceptor`.
- **CB-15.** XML pesado: a montagem/validação não aloca cópias integrais (uso de `Span<T>`/`Memory<T>`); round-trip de `HashIntegridade.Confere` retorna `true` para conteúdo íntegro.

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — especificação Rules-as-Code do agregado `RemessaTce` (módulo Transparencia) derivada do README do Bounded Context e das fontes legais (LRF arts. 48/48-A e 23 §3º, LC 131/2009, LAI art. 8 §3º, SIAFIC, Resoluções TCE-RS SIAPC/PAD/e-Validador). Define ciclo `Gerada → Validada → Enviada → Homologada/Rejeitada`, validação local (RDI), imutabilidade por hash, alerta de prazo e Integration Events publicados. |

<!-- manifest
commands: GerarRemessaTce, ValidarRemessaTce, EmpacotarRemessaTce, RegistrarProtocoloTce, HomologarRemessaTce, VencerPrazoRemessaTce
queries: ObterRemessaTcePorId, ListarRemessasTcePorPeriodo, BaixarArquivoRemessaTce, ObterCriticasRemessaTce
domainEvents: RemessaGerada, RemessaValidada, RemessaRejeitada, RemessaProntaParaTransmissao, RemessaEnviadaTce, RemessaHomologada, PrazoRemessaVencido
integrationEventsPublished: RemessaEnviadaTceIntegrationEvent, RemessaRejeitadaIntegrationEvent, PrazoRemessaVencidoIntegrationEvent
integrationEventsConsumed: 
-->
