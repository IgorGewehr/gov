---
modulo: AssistenciaSocial
agregado: Beneficio
contexto: AssistenciaSocial (SUAS — elegibilidade e concessão de benefícios BPC/PBF/eventuais)
poder: Executivo
schema: assistenciasocial
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF arts. 203-204", "Lei 8.742/1993 (LOAS, c/ Lei 12.435/2011)", "PNAS/2004 (Res. CNAS 145/2004)", "Lei 14.601/2023 (Programa Bolsa Familia)", "LGPD art. 11 (dado sensivel)"]
---

# Beneficio — Regras-as-Code (Rules-as-Code)

> Raiz de agregado que **avalia a elegibilidade** e **registra a concessão ou o indeferimento**
> de benefícios socioassistenciais: **BPC** (idoso ≥ 65 / PCD), **PBF** (Programa Bolsa Família —
> Lei 14.601/2023) e **benefícios eventuais** (natalidade, funeral, cesta básica). Critérios de
> renda, valor do salário mínimo e prazos são **versionados por vigência/competência** (nunca
> *hardcoded*). Marco: CF arts. 203-204; Lei 8.742/1993 (LOAS, c/ Lei 12.435/2011); Lei
> 14.601/2023 (PBF). Este arquivo é **normativo e versionado**; o código (domínio, handlers,
> validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Benefício (`Beneficio`) | Provisão socioassistencial concedida/indeferida a uma família; raiz de agregado. Implementa `IMustHaveTenant`. |
| Tipo de Benefício (`TipoBeneficio` : enum) | `Bpc`, `Pbf` ou `Eventual` (README §7, payload `BeneficioConcedido`). |
| BPC (`Bpc` / `ConcessaoBPC`) | Benefício de Prestação Continuada — idoso ≥ 65 ou PCD; renda per capita < ¼ SM; não acumula. |
| PBF (`Pbf` / `VinculoPBF`) | Programa Bolsa Família (Lei 14.601/2023); vínculo/condicionalidades. |
| Benefício Eventual (`Eventual` / `BeneficioEventual`) | Provisão temporária e excepcional (natalidade, funeral, cesta básica). |
| Critério de Elegibilidade (`CriterioElegibilidade` : VO) | Conjunto de regras de renda/idade/condição aplicáveis na competência. |
| Avaliação de Elegibilidade (`AvaliarElegibilidade`) | Operação que decide concessão ou indeferimento conforme o critério vigente. |
| Concessão (`Conceder` / `Concedida`) | Deferimento e registro do benefício à família. |
| Indeferimento (`Indeferir` / `Indeferida`) | Negativa fundamentada (motivo registrado). |
| Entrega de Cesta (`EntregarCestaBasica` / `CestaEntregue`) | Entrega física da cesta básica (benefício eventual de cesta). |
| Salário Mínimo Vigente (`SalarioMinimoVigente`) | Parâmetro versionado por vigência/competência; nunca *hardcoded*. |
| Competência (`Competencia` : VO) | Ano/mês de referência da concessão; regra aplicável = a vigente na competência. |
| Renda Per Capita (`RendaPerCapita`) | Renda familiar ÷ membros (projetada da `Familia`/CadÚnico). |
| Avaliação Biopsicossocial (`AvaliacaoBiopsicossocial`) | Requisito do BPC para PCD (deficiência). |
| Acumulação Seguridade (`AcumulaSeguridadeSocial`) | Indicador de benefício concomitante da Seguridade (veda BPC). |
| Tenant (`TenantId`) | Ente público (Prefeitura) dono do registro — município. |
| Situação (`Situacao` : `SituacaoBeneficio`) | Estado atual do benefício no ciclo de avaliação/concessão. |

---

## 2. Modelo

- **Identidade:** `BeneficioId` — `readonly record struct BeneficioId(Guid Value)`; fábrica `BeneficioId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Beneficio : AggregateRoot<BeneficioId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce em avaliação via factory `Solicitar(...)`; concedido/indeferido via métodos de transição.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (município) dono do registro. | `private set` |
| `FamiliaId` | `Guid` | Família beneficiária (vínculo com o agregado `Familia`). | `private set` |
| `Tipo` | `TipoBeneficio` | `Bpc` / `Pbf` / `Eventual`. | `private set` |
| `Competencia` | `Competencia` (VO) | Ano/mês de referência (define a regra vigente). | `private set` |
| `Valor` | `ValorMonetario?` | Valor concedido (nulo até concessão; nulo em cesta básica). | `private set` |
| `Situacao` | `SituacaoBeneficio` | Situação atual. | `private set` |
| `MotivoIndeferimento` | `string?` | Motivo da negativa (preenchido em `Indeferida`). | `private set` |
| `DataDecisao` | `DateOnly?` | Data da concessão/indeferimento. | `private set` |
| `QuantidadeCesta` | `int?` | Quantidade de cestas (apenas `Eventual` de cesta básica). | `private set` |
| `DataEntregaCesta` | `DateOnly?` | Data de entrega da cesta. | `private set` |

### Constantes / parâmetros versionados

- Os limiares de renda **não** são constantes de domínio fixas; são derivados de `SalarioMinimoVigente` **versionado por vigência** (README §5; CLAUDE.md §7). Frações de referência (semântica, não números mágicos):
  - **BPC:** renda per capita **< ¼** do salário mínimo (`FatorRendaBpc`).
  - **Eventual:** renda per capita **≤ ½** do salário mínimo (`FatorRendaEventual`).
  - **Idade mínima BPC idoso:** **65** anos (`IdadeMinimaBpcIdoso`).
- A regra aplicável é **a vigente na `Competencia`** (aplica-se a regra vigente na competência).

### Value Objects (referenciados)

- `Competencia` — `readonly record struct` com `Ano` (`int`) e `Mes` (`int`, 1-12); valida intervalo.
- `ValorMonetario` — valor monetário do benefício; expõe `Valor` (`decimal`). Não negativo.
- `CriterioElegibilidade` — `record` que encapsula os limiares vigentes na competência (renda, idade, vedações) e expõe `Avaliar(contexto)` → resultado (`Elegivel`/`NaoElegivel` + motivo). Nunca *hardcoded*: recebe `SalarioMinimoVigente` da competência.

### Enum `TipoBeneficio`

| Valor | Numérico | Descrição |
|---|---|---|
| `Bpc` | 1 | Benefício de Prestação Continuada (idoso ≥ 65 / PCD). |
| `Pbf` | 2 | Programa Bolsa Família (Lei 14.601/2023). |
| `Eventual` | 3 | Benefício eventual (natalidade, funeral, cesta básica). |

### Enum `SituacaoBeneficio`

| Valor | Numérico | Descrição |
|---|---|---|
| `EmAvaliacao` | 1 | Solicitado; aguardando avaliação de elegibilidade (estado inicial). |
| `Concedida` | 2 | Elegibilidade deferida; benefício concedido (terminal de deferimento). |
| `Indeferida` | 3 | Elegibilidade negada; motivo registrado (terminal de indeferimento). |

> **Conjuntos de referência usados nas guardas:**
> - **Decidida** = { `Concedida`, `Indeferida` } — estados terminais (não admitem nova avaliação/concessão).
> - **Pendente** = { `EmAvaliacao` } — única origem para `Conceder`/`Indeferir`.
> - **Entrega de cesta** só sobre `Concedida` **e** `Tipo == Eventual` de cesta básica.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Competencia` é obrigatória; a regra de elegibilidade aplicada é **a vigente na competência** (nunca *hardcoded*) — README §5; CLAUDE.md §7.
- **I-2.** **BPC** exige (idade ≥ 65 anos **OU** PCD com `AvaliacaoBiopsicossocial`) **E** renda per capita **< ¼** do `SalarioMinimoVigente`. Caso contrário, indefere (README §5).
- **I-3.** **BPC** **não acumula** com outro benefício da Seguridade Social: `AcumulaSeguridadeSocial == true` ⇒ indeferimento obrigatório (README §5).
- **I-4.** **Benefício eventual** exige renda per capita **≤ ½** do `SalarioMinimoVigente`, com **preferência a inscritos no CadÚnico**; é provisão **temporária e excepcional** (README §5).
- **I-5.** Concessão/indeferimento só ocorre a partir de `EmAvaliacao`; benefício já **decidido** (`Concedida`/`Indeferida`) não admite nova decisão (`InvalidOperationException`).
- **I-6.** Ao **conceder**, situação passa a `Concedida`, grava `DataDecisao` e (quando aplicável) `Valor`; emite `BeneficioConcedido(beneficioId, familiaId, tipo, competencia, valor)`.
- **I-7.** Ao **indeferir**, `MotivoIndeferimento` é obrigatório (não nulo/vazio), situação passa a `Indeferida`, grava `DataDecisao` e emite `BeneficioIndeferido(beneficioId, familiaId, tipo, motivoIndeferimento)`.
- **I-8.** **Entrega de cesta básica** só é permitida sobre benefício `Concedida` do tipo `Eventual` (cesta); exige `QuantidadeCesta` ≥ 1 e grava `DataEntregaCesta`; emite `CestaBasicaEntregue(beneficioId, familiaId, quantidade, dataEntrega)`.
- **I-9.** `Valor` quando informado é **não negativo**; benefícios de cesta básica podem ter `Valor` nulo (provisão em espécie).
- **I-10.** A elegibilidade considera a **situação cadastral da família**: família com cadastro **vencido** (`AtualizacaoVencida` no agregado `Familia`) tem novos benefícios **condicionados à regularização** (README §5; Familia I-6).
- **I-11.** `FamiliaId` é obrigatório na solicitação (`ArgumentNullException`); o benefício sempre se vincula a uma família referenciada.
- **I-12.** Nenhum payload de Integration Event carrega dado sensível identificável (NIS/CPF/saúde); somente identificadores e dados estritamente necessários (README §7).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Solicitar` | `EmAvaliacao` | `FamiliaId` informado; `Competencia` válida; `Tipo` informado | — |
| `EmAvaliacao` | `Conceder` (via `AvaliarElegibilidade` elegível) | `Concedida` | critério vigente satisfeito (I-2/I-3/I-4); cadastro regular (I-10) | `BeneficioConcedido` |
| `EmAvaliacao` | `Indeferir` (via `AvaliarElegibilidade` não elegível) | `Indeferida` | `motivoIndeferimento` não vazio | `BeneficioIndeferido` |
| `Concedida` | `EntregarCestaBasica` | `Concedida` (mantém) | `Tipo == Eventual` (cesta); `quantidade ≥ 1` | `CestaBasicaEntregue` |

> Observações:
> - `Concedida` e `Indeferida` são **terminais** quanto à decisão (I-5): não há reabertura de avaliação nesta versão.
> - `AvaliarElegibilidade` é a operação de decisão: encapsula `CriterioElegibilidade.Avaliar(...)` e despacha para `Conceder` ou `Indeferir`.
> - `EntregarCestaBasica` não altera a situação (permanece `Concedida`), apenas registra a entrega e emite o evento.

---

## 5. Comandos (escrita)

### 5.1 AvaliarElegibilidadeBeneficio

- **Command:** `AvaliarElegibilidadeBeneficioCommand(Guid FamiliaId, TipoBeneficio Tipo, Competencia Competencia, DadosElegibilidadeDto Dados) : ICommand<Guid>`.
- **Entrada (DTO):** `FamiliaId`, `Tipo` (`Bpc`/`Pbf`/`Eventual`), `Competencia`, `Dados` (idade, PCD/avaliação biopsicossocial, acumulação Seguridade, inscrição no CadÚnico).
- **Dependências do handler:** `IFamiliaRepository`, `IBeneficioRepository`, `IParametroVigenteProvider` (salário mínimo/critérios por competência), `IUnitOfWork`, `ITenantContext`, `TimeProvider`, `IPublisher`.
- **Pré-condições:**
  - `request` não nulo.
  - Família existe e está **apta** (não `AtualizacaoVencida`), senão indefere por "cadastro desatualizado" (I-10).
  - `SalarioMinimoVigente` e critérios obtidos para a `Competencia` (I-1) — nunca *hardcoded*.
- **Efeito:** cria via `Beneficio.Solicitar(tenant.TenantId, familiaId, tipo, competencia)`; monta `CriterioElegibilidade` vigente; chama `beneficio.AvaliarElegibilidade(criterio, dados, rendaPerCapita)`:
  - **elegível** ⇒ `Conceder(valor?)` → `Concedida` + `BeneficioConcedido`; publica `BeneficioConcedidoIntegrationEvent` (Outbox).
  - **não elegível** ⇒ `Indeferir(motivo)` → `Indeferida` + `BeneficioIndeferido`; publica `BeneficioIndeferidoIntegrationEvent` (Outbox).
  - `beneficios.Adicionar(beneficio)`; `SaveChangesAsync`.
- **Pós-condições:** benefício em `Concedida` ou `Indeferida`; evento de domínio e integração correspondente publicados; retorna `beneficio.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (família inexistente).
- **Eventos de domínio:** `BeneficioConcedido` **ou** `BeneficioIndeferido`.
- **Eventos de integração (publica):** `BeneficioConcedidoIntegrationEvent` **ou** `BeneficioIndeferidoIntegrationEvent`.

### 5.2 EntregarCestaBasica

- **Command:** `EntregarCestaBasicaCommand(Guid BeneficioId, int Quantidade) : ICommand`.
- **Entrada (DTO):** `BeneficioId`, `Quantidade` (≥ 1).
- **Dependências do handler:** `IBeneficioRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`, `IPublisher`.
- **Pré-condições:**
  - `request` não nulo.
  - Benefício existe (`ObterPorIdAsync`), senão `InvalidOperationException("Benefício não encontrado.")`.
  - Situação `Concedida` e `Tipo == Eventual` (cesta) — senão `InvalidOperationException` (I-8).
- **Efeito:** `beneficio.EntregarCestaBasica(request.Quantidade, hoje)`; `SaveChangesAsync`; publica `CestaBasicaEntregueIntegrationEvent(...)` via Outbox.
- **Pós-condições:** `QuantidadeCesta`/`DataEntregaCesta` gravados; situação permanece `Concedida`; `CestaBasicaEntregue` (domínio) + Integration Event publicados.
- **Exceções:** `ArgumentNullException` (request); `InvalidOperationException` (não encontrado, situação ≠ `Concedida` ou tipo incompatível); `ArgumentOutOfRangeException` (quantidade < 1).
- **Evento de domínio:** `CestaBasicaEntregue(Id, familiaId, quantidade, dataEntrega)`.
- **Evento de integração (publica):** `CestaBasicaEntregueIntegrationEvent`.

> **Comandos de domínio existentes no agregado sem handler dedicado nesta versão:** `Conceder()` e `Indeferir()` são acionados **exclusivamente** por `AvaliarElegibilidade` (não expostos como comandos avulsos, para preservar a decisão por critério vigente).

---

## 6. Consultas (leitura)

### 6.1 ObterBeneficiosDaFamilia

- **Query:** `ObterBeneficiosDaFamiliaQuery(Guid FamiliaId) : IQuery<IReadOnlyList<BeneficioResumo>>`.
- **Entrada:** `FamiliaId`.
- **Handler:** `ObterBeneficiosDaFamiliaHandler(IBeneficioRepository beneficios)`; chama `beneficios.ListarPorFamiliaAsync(request.FamiliaId, ct)`.
- **Projeção (DTO):** `BeneficioResumo(Guid Id, Guid FamiliaId, string Tipo, string Competencia, decimal? Valor, string Situacao, string? MotivoIndeferimento, DateOnly? DataDecisao)`.
  - `Tipo`/`Situacao` projetados de `ToString()`; `Valor` de `beneficio.Valor?.Valor`.
- **Filtros:** por `FamiliaId`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ObterConcessoesPorCompetencia

- **Query:** `ObterConcessoesPorCompetenciaQuery(Competencia Competencia) : IQuery<IReadOnlyList<BeneficioResumo>>`.
- **Entrada:** `Competencia`.
- **Handler:** `ObterConcessoesPorCompetenciaHandler(IBeneficioRepository beneficios)`; chama `beneficios.ListarConcedidosPorCompetenciaAsync(request.Competencia, ct)` (apenas `Concedida`).
- **Projeção (DTO):** `BeneficioResumo` (mesma de 6.1).
- **Filtros:** por `Competencia` + `Situacao == Concedida`; tenant-scoped.
- **Pré-condições:** `request` não nulo.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...AssistenciaSocial.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `BeneficioConcedido` | `(BeneficioId, FamiliaId, TipoBeneficio Tipo, Competencia, decimal? Valor)` | `Beneficio.Conceder` |
| `BeneficioIndeferido` | `(BeneficioId, FamiliaId, TipoBeneficio Tipo, string MotivoIndeferimento)` | `Beneficio.Indeferir` |
| `CestaBasicaEntregue` | `(BeneficioId, FamiliaId, int Quantidade, DateOnly DataEntrega)` | `Beneficio.EntregarCestaBasica` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...AssistenciaSocial.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `BeneficioConcedidoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid BeneficioId, Guid FamiliaId, string Tipo, string Competencia, decimal? Valor)` | `AvaliarElegibilidadeBeneficioHandler` |
| `BeneficioIndeferidoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid BeneficioId, Guid FamiliaId, string Tipo, string MotivoIndeferimento)` | `AvaliarElegibilidadeBeneficioHandler` |
| `CestaBasicaEntregueIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid BeneficioId, Guid FamiliaId, int Quantidade, DateOnly DataEntrega)` | `EntregarCestaBasicaHandler` |

> Payloads expõem apenas identificadores e dados estritamente necessários — sem dado sensível identificável (NIS/CPF/saúde) no barramento (README §7). `BeneficioConcedidoIntegrationEvent` pode notificar **Financas** (provisão de recurso) e **Transparencia** (dados agregados/anonimizados) — README §4.

### Integração (consome)

- Nenhum nesta versão (a elegibilidade lê o read model do CadÚnico/`Familia` intra-módulo, não por Integration Event).

---

## 8. Validações (FluentValidation)

### AvaliarElegibilidadeBeneficioValidator (`AbstractValidator<AvaliarElegibilidadeBeneficioCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FamiliaId` | `NotEmpty()` | "Família é obrigatória." |
| `Tipo` | `IsInEnum()` | "Tipo de benefício inválido." |
| `Competencia` | `NotEmpty()` + ano/mês válidos | "Competência (ano/mês) é obrigatória e válida." |
| `Dados` | `NotNull()` | "Dados de elegibilidade são obrigatórios." |

### EntregarCestaBasicaValidator (`AbstractValidator<EntregarCestaBasicaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `BeneficioId` | `NotEmpty()` | "Identificador do benefício é obrigatório." |
| `Quantidade` | `GreaterThanOrEqualTo(1)` | "Quantidade de cestas deve ser ≥ 1." |

> As consultas não possuem validador FluentValidation dedicado nesta versão (proteção por `ArgumentNullException.ThrowIfNull` no handler e tenant-scope na infraestrutura). O `MotivoIndeferimento` é validado no domínio (`ArgumentException.ThrowIfNullOrWhiteSpace` em `Indeferir`).

---

## 9. Persistência (EF Core 8)

- **Schema:** `assistenciasocial` (isolado por módulo). **DbContext:** o do módulo AssistenciaSocial. **Migrations:** por módulo.
- **Tabela:** `Beneficio` (raiz de agregado).

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `BeneficioId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `FamiliaId` | `Guid` | vínculo com a família beneficiária. |
| `Tipo` | `int` | enum `TipoBeneficio` (persistido por valor numérico). |
| `Competencia_Ano` | `int` | owned `Competencia`. |
| `Competencia_Mes` | `int` | owned `Competencia`. |
| `Valor` | `decimal(18,2)?` | owned/conversor de `ValorMonetario` (nulável; nulo em cesta básica). |
| `Situacao` | `int` | enum `SituacaoBeneficio`. |
| `MotivoIndeferimento` | `nvarchar(400)?` | preenchido quando `Indeferida`. |
| `DataDecisao` | `date?` | `DateOnly?`. |
| `QuantidadeCesta` | `int?` | apenas `Eventual` (cesta). |
| `DataEntregaCesta` | `date?` | `DateOnly?`. |

- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, FamiliaId)` para `ListarPorFamiliaAsync`.
  - Índice em `(TenantId, Competencia_Ano, Competencia_Mes, Situacao)` para `ListarConcedidosPorCompetenciaAsync`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto AssistenciaSocial para `BeneficioConcedidoIntegrationEvent`, `BeneficioIndeferidoIntegrationEvent` e `CestaBasicaEntregueIntegrationEvent` (consistência transacional com o estado).
- **Parâmetros versionados:** `SalarioMinimoVigente`/critérios por competência vêm de tabela parametrizável por tenant/vigência (não embutidos no agregado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Beneficio` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT.
- **RBAC (policy-based + RBAC `Usuário → Departamento → Roles`, negar por padrão):**
  - Avaliação/concessão/indeferimento: papéis da **equipe técnica do SUAS** (ex.: `AssistenciaSocial.Beneficio.Conceder`).
  - Entrega de cesta básica: papel operacional (ex.: `AssistenciaSocial.Beneficio.Entregar`).
  - Consulta de benefícios: papel de **leitura socioassistencial** (ex.: `AssistenciaSocial.Beneficio.Ler`).
  - Módulo AssistenciaSocial é **ativável por tenant** (Executivo); requisição a tenant sem licença → 404/403 auditado.
- **LGPD — dado sensível (art. 11):** elegibilidade processa renda, deficiência (PCD/avaliação biopsicossocial) e composição familiar. Base legal: **execução de política pública** (art. 11, II, "b" / art. 23) — **não** consentimento. Princípios de **minimização** (eventos não carregam saúde/NIS/CPF) e finalidade.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em `Solicitar`, `Conceder`, `Indeferir`, `EntregarCestaBasica` — destinada ao controle social/Tribunal de Contas (TCE-RS).
- **Isolamento federal:** dados do CadÚnico usados na elegibilidade **não trafegam entre tenants** nem para módulos não autorizados (README §6).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **CadÚnico / MDS (leitura) — base da elegibilidade:** renda/composição/condicionalidades projetadas como read model (intra-módulo via `Familia`); base **federal autoritativa**, jamais sobrescrita (README §1, §4). Polly + ACL.
- **PBF / SISC:** consulta de vínculos e condicionalidades do Bolsa Família para qualificar `Pbf` (README §4). `HttpClient` tipado + Polly + ACL; idempotente.
- **Saída — Finanças (intra-aplicação, via Contracts):** ao conceder, `BeneficioConcedidoIntegrationEvent` (Outbox) pode notificar **Financas** para provisão de recurso (Lei 4.320). Idempotente por `EventId`.
- **Saída — Transparência (intra-aplicação, via Contracts):** dados **agregados/anonimizados** de concessão para dados abertos (LAI) — **nunca** dado sensível identificável (README §4, §6).
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Indeferimento de BPC por renda** (README §8)
- **Dado** um requerente PCD com renda per capita **≥ ¼ do salário mínimo** (vigente na competência)
- **Quando** executo `AvaliarElegibilidadeBeneficioCommand(familiaId, Bpc, competencia, dados)`
- **Então** a concessão é **indeferida**, `MotivoIndeferimento` é registrado e `BeneficioIndeferido` (domínio) + `BeneficioIndeferidoIntegrationEvent` são publicados (I-2, I-7).

**Cenário 2 — Concessão de BPC a idoso elegível**
- **Dado** um requerente com idade **≥ 65 anos** e renda per capita **< ¼ SM**, sem acumulação da Seguridade
- **Quando** avalio a elegibilidade do BPC
- **Então** o benefício é `Concedida`, `DataDecisao` é gravada e `BeneficioConcedido` + `BeneficioConcedidoIntegrationEvent` são publicados (I-2, I-6).

**Cenário 3 — BPC indeferido por acumulação da Seguridade**
- **Dado** um requerente elegível por idade/renda, **mas** com benefício concomitante da Seguridade Social
- **Quando** avalio a elegibilidade do BPC
- **Então** o benefício é `Indeferida` com motivo de não acumulação (I-3).

**Cenário 4 — Concessão de cesta básica (benefício eventual)** (README §8)
- **Dado** uma família inscrita no CadÚnico com renda **≤ ½ SM** e situação de vulnerabilidade temporária
- **Quando** o benefício eventual é concedido (`AvaliarElegibilidadeBeneficioCommand` tipo `Eventual`) e a cesta é entregue (`EntregarCestaBasicaCommand`)
- **Então** `BeneficioConcedido` e `CestaBasicaEntregue` (domínio) + os Integration Events correspondentes são publicados, com trilha de auditoria (I-4, I-6, I-8).

**Cenário 5 — Eventual indeferido por renda acima de ½ SM**
- **Dado** uma família com renda per capita **> ½ SM**
- **Quando** avalio um benefício eventual
- **Então** é `Indeferida` com motivo de renda (I-4).

**Cenário 6 — Entrega de cesta sobre benefício não concedido**
- **Dado** um benefício em `EmAvaliacao` ou `Indeferida`
- **Quando** executo `EntregarCestaBasicaCommand`
- **Então** ocorre `InvalidOperationException` e nenhum evento de entrega é publicado (I-8).

**Cenário 7 — Entrega de cesta sobre benefício não-eventual**
- **Dado** um benefício `Bpc` concedido
- **Quando** executo `EntregarCestaBasicaCommand`
- **Então** ocorre `InvalidOperationException` (tipo incompatível — só `Eventual` de cesta).

**Cenário 8 — Nova decisão sobre benefício já decidido**
- **Dado** um benefício já `Concedida` (ou `Indeferida`)
- **Quando** se tenta avaliar/decidir novamente
- **Então** ocorre `InvalidOperationException` (estado terminal — I-5).

**Cenário 9 — Elegibilidade bloqueada por cadastro vencido**
- **Dado** uma família com cadastro do CadÚnico vencido (`AtualizacaoVencida`)
- **Quando** avalio a elegibilidade a um novo benefício
- **Então** é `Indeferida`/bloqueada com motivo "cadastro desatualizado", condicionada à regularização (I-10; README §5).

**Cenário 10 — Regra aplicada é a vigente na competência**
- **Dado** competências com salários mínimos diferentes (vigências distintas)
- **Quando** avalio dois benefícios em competências distintas com a mesma renda
- **Então** cada avaliação aplica o `SalarioMinimoVigente` da respectiva competência (I-1).

**Cenário 11 — Concessão tenant-scoped por competência**
- **Dado** benefícios concedidos no tenant A e no tenant B na mesma competência
- **Quando** executo `ObterConcessoesPorCompetenciaQuery(competencia)` no contexto do tenant A
- **Então** retornam **apenas** as concessões do tenant A, projetadas em `BeneficioResumo`.

---

## 13. Casos de Borda

- **B-1.** Renda per capita **exatamente = ¼ SM** no BPC ⇒ **indeferido** (regra é estritamente `< ¼` — I-2).
- **B-2.** Renda per capita **exatamente = ½ SM** no eventual ⇒ **elegível** quanto à renda (regra é `≤ ½` — I-4).
- **B-3.** PCD sem `AvaliacaoBiopsicossocial` ⇒ requisito de deficiência não comprovado; indeferido por critério de BPC (I-2).
- **B-4.** Idade `= 64` no BPC idoso ⇒ não atinge `IdadeMinimaBpcIdoso (65)`; indeferido salvo PCD (I-2).
- **B-5.** `Indeferir` com `MotivoIndeferimento` vazio ⇒ `ArgumentException` (I-7).
- **B-6.** `Conceder` cesta básica com `Valor` nulo ⇒ permitido (provisão em espécie — I-9).
- **B-7.** `EntregarCestaBasica` com `Quantidade = 0` ⇒ `ArgumentOutOfRangeException`/validação `>= 1` (I-8).
- **B-8.** Avaliar benefício para família inexistente ⇒ `InvalidOperationException` (I-11).
- **B-9.** Eventual para família **não inscrita no CadÚnico** com renda ≤ ½ SM ⇒ admissível, porém **sem a preferência** dos inscritos (I-4 — preferência, não exclusividade).
- **B-10.** Reentrega da mesma cesta ⇒ consumidor idempotente por `EventId` (Outbox); domínio registra a última entrega.
- **B-11.** Concessão sem `SalarioMinimoVigente` parametrizado para a competência ⇒ falha de parametrização (não *hardcoded*); avaliação não prossegue (I-1).
- **B-12.** Integration Event não pode conter NIS/CPF/saúde ⇒ payload restrito a identificadores (I-12; README §7).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo AssistenciaSocial (§§1-9). Elegibilidade e concessão/indeferimento de BPC, PBF e benefícios eventuais; entrega de cesta básica; critérios versionados por vigência; eventos para Finanças/Transparência sem dado sensível identificável. |

<!-- manifest
commands: AvaliarElegibilidadeBeneficio, EntregarCestaBasica
queries: ObterBeneficiosDaFamilia, ObterConcessoesPorCompetencia
domainEvents: BeneficioConcedido, BeneficioIndeferido, CestaBasicaEntregue
integrationEventsPublished: BeneficioConcedidoIntegrationEvent, BeneficioIndeferidoIntegrationEvent, CestaBasicaEntregueIntegrationEvent
integrationEventsConsumed: 
-->
