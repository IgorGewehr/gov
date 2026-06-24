---
modulo: Administracao
agregado: Contrato
contexto: Administracao (Compras Públicas — formalização e execução contratual sob a NLLC)
poder: Ambos
schema: administracao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "Lei 14.133/2021 (NLLC) — norma central das compras públicas"
  - "Lei 14.133/2021 art. 89 a 95 (formalização dos contratos)"
  - "Lei 14.133/2021 art. 96 e 98 (garantia de execução: ≤ 5%; até 10% em grande vulto)"
  - "Lei 14.133/2021 art. 105 e 106 (vigência vinculada a crédito orçamentário)"
  - "Lei 14.133/2021 art. 124 a 136 (alterações contratuais e reequilíbrio)"
  - "Lei 14.133/2021 art. 125 (limites de acréscimo/supressão: 25%; até 50% em reforma)"
  - "Lei 14.133/2021 art. 136 (apostilamento — alteração que dispensa termo aditivo)"
  - "Lei 14.133/2021 art. 94 (divulgação no PNCP como CONDIÇÃO DE EFICÁCIA; prazos 20/10 d.u.; §3 obras 25/45 d.u.)"
  - "Lei 14.133/2021 art. 174 (institui o PNCP — sítio oficial de divulgação)"
  - "Lei 14.133/2021 art. 110 / art. 224 CPC (vencimento em dia não útil prorroga para o 1º dia útil)"
  - "LC 101/2000 (LRF — vinculação a dotação/crédito orçamentário)"
  - "Decreto 10.764/2021 (gestão do PNCP; Decreto 11.462/2023 trata de SRP)"
---

# Contrato — Regras-as-Code (Rules-as-Code)

> Instrumento que formaliza a relação entre a Administração e o `Fornecedor` vencedor de uma
> `Licitacao` (ou de contratação direta por `Dispensa`/`Inexigibilidade`), sob a
> **Lei 14.133/2021 (NLLC)**. Admite `Aditivo` (alteração quantitativa/qualitativa/de prazo,
> limitado a **25%** — até **50%** em reforma), `Apostilamento` (reajuste/dotação, sem termo
> aditivo) e `Garantia` de execução (**≤ 5%**, até **10%** em grande vulto). A **divulgação no
> PNCP é condição de eficácia** do contrato e de seus aditivos (**art. 94** — o art. 174 apenas
> institui o PNCP) e sua **vigência** depende de **crédito orçamentário** (art. 105–106; LRF).
> **W9.1 — invariante de bloqueio:** contrato sem **número de controle PNCP** é **ineficaz** e
> **NÃO sustenta empenho** (`PodeEmpenhar()` é fonte única da regra; o módulo Financas consulta
> esse estado via `*.Contracts` ANTES de empenhar — fail-closed). A divulgação é transmitida pela
> ACL `IPncpGateway` (que devolve o número de controle), com **relógio de prazo** do art. 94
> (`PrazoPncp`/`PrazoLegal`/`ICalendarioDiasUteis`, dias úteis com feriados por tenant —
> parametrizável, sem número mágico). Publicação fora do prazo é **intempestiva** mas não impede a
> eficácia (subsídio de auditoria). Este arquivo é **normativo e
> versionado**; o código de domínio, aplicação, persistência e testes do agregado `Contrato`
> é **gerado e mantido a partir daqui**. Bug, ajuste ou nova regra ⇒ edita-se **este arquivo**;
> o código é consequência.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Contrato (`Contrato`) | Instrumento que formaliza a contratação pública. Raiz de agregado. |
| Identidade (`ContratoId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| Licitacao de origem (`LicitacaoId`) | Certame que originou o contrato (referência por Id; nulo se contratação direta). |
| Fornecedor (`FornecedorId`) | Contratado vencedor (outra raiz; referência por Id). |
| Origem da Contratação (`OrigemContratacao` : `OrigemContratacao`) | Fundamento: `Licitacao`, `Dispensa`, `Inexigibilidade`. |
| Objeto (`Objeto` : `string`) | Descrição do objeto contratado. |
| Valor (`ValorContratado` : `ValorMonetario`) | Valor global original do contrato. |
| Valor Atual (`ValorAtual` : `ValorMonetario`) | Valor vigente após aditivos/apostilamentos. |
| Vigência (`VigenciaInicio` / `VigenciaFim` : `DateOnly`) | Período de execução contratual. |
| Empenho (ref) (`EmpenhoRef` : `EmpenhoRef`) | Referência ao ato de Financas que reserva dotação (Value Object). |
| Aditivo (`Aditivo`) | Alteração quantitativa/qualitativa ou de prazo, via termo aditivo. Entidade-filha. |
| Apostilamento (`Apostilamento`) | Registro de alteração que dispensa termo aditivo (reajuste, dotação) (art. 136). Entidade-filha. |
| Reequilíbrio (`ReequilibrioEconomicoFinanceiro`) | Recomposição da equação econômica do contrato. Tipo de `Aditivo`. |
| Garantia (`Garantia`) | Caução contratual de execução (≤ 5%, art. 96). Entidade-filha. |
| Assinar (`AssinarContrato`) | Formalizar o contrato (passa a `Assinado`). |
| Publicar no PNCP (`PublicarContratoNoPncp` / `ContratoPublicadoPncp`) | Publicação oficial — condição de eficácia (art. 174). |
| Celebrar Aditivo (`CelebrarAditivo` / `AditivoCelebrado`) | Registrar termo aditivo respeitando o limite legal. |
| Apostilar (`Apostilar`) | Registrar apostilamento (reajuste/dotação). |
| Prestar Garantia (`PrestarGarantia`) | Registrar a garantia de execução. |
| Iniciar Execução (`IniciarExecucao`) | Iniciar a vigência/execução (exige eficácia e dotação). |
| Encerrar (`EncerrarContrato`) | Encerrar por término da vigência/conclusão do objeto (passa a `Encerrado`). |
| Rescindir (`RescindirContrato`) | Extinguir antecipadamente (passa a `Rescindido`). |
| Situacao (`Situacao` : `SituacaoContrato`) | Estado atual do contrato no ciclo de vida. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |

---

## 2. Modelo

- **Identidade:** `ContratoId` — `readonly record struct ContratoId(Guid Value)`; fábrica `ContratoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Contrato : AggregateRoot<ContratoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válida via factory `Celebrar(...)`.

### 2.1 Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `Id` | `ContratoId` | Identidade do agregado. | `init` |
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `LicitacaoId` | `Guid?` | Licitação de origem (nulo se contratação direta). | `private set` |
| `FornecedorId` | `Guid` | Contratado. | `private set` |
| `OrigemContratacao` | `OrigemContratacao` | Fundamento da contratação. | `private set` |
| `Objeto` | `string` | Descrição do objeto. | `private set` |
| `ValorContratado` | `ValorMonetario` (VO) | Valor global original. | `private set` |
| `ValorAtual` | `ValorMonetario` (VO) | Valor vigente após alterações. | `private set` |
| `VigenciaInicio` | `DateOnly` | Início da vigência. | `private set` |
| `VigenciaFim` | `DateOnly` | Fim da vigência. | `private set` |
| `EmpenhoRef` | `EmpenhoRef?` (VO) | Referência ao empenho (Financas). | `private set` |
| `DotacaoConfirmada` | `bool` | Cobertura orçamentária confirmada (via Integration Event). | `private set` |
| `Situacao` | `SituacaoContrato` | Situação atual. | `private set` |
| `NumeroContratoPncp` | `string?` | Identificador no PNCP, quando publicado. | `private set` |
| `PublicadoNoPncp` | `bool` | Eficácia obtida pela publicação (art. 174). | `private set` |
| `Aditivos` | `IReadOnlyCollection<Aditivo>` | Termos aditivos. | coleção encapsulada |
| `Apostilamentos` | `IReadOnlyCollection<Apostilamento>` | Apostilamentos. | coleção encapsulada |
| `Garantias` | `IReadOnlyCollection<Garantia>` | Garantias de execução. | coleção encapsulada |

### 2.2 Value Objects e enums

- **`ValorMonetario`** (SharedKernel) — valor monetário; expõe `Valor` (`decimal`). Não nulo (validado em `Celebrar`).
- **`EmpenhoRef`** — `readonly record struct EmpenhoRef(Guid EmpenhoId, string NumeroEmpenho)`; referência ao ato de Financas (sem navegação cruzada entre raízes/módulos).
- **`OrigemContratacao`** (enum):

  | Valor | Numérico | Descrição | Base legal |
  |---|---|---|---|
  | `Licitacao` | 1 | Contrato decorrente de certame homologado. | art. 89 |
  | `Dispensa` | 2 | Contratação direta por valor/hipótese. | art. 75 |
  | `Inexigibilidade` | 3 | Contratação direta por inviabilidade de competição. | art. 74 |

### 2.3 Entidades-filhas

- **`Aditivo`** — `AditivoId`, `Numero` (`int`), `Tipo` (`TipoAditivo`: `Acrescimo`, `Supressao`, `Prazo`, `Reequilibrio`, `Qualitativo`), `PercentualSobreValorOriginal` (`decimal`), `ValorDelta` (`ValorMonetario`), `NovaVigenciaFim` (`DateOnly?`), `Justificativa` (`string`), `PublicadoNoPncp` (`bool`), `DataCelebracao` (`DateOnly`).
- **`Apostilamento`** — `ApostilamentoId`, `Numero` (`int`), `Tipo` (`TipoApostilamento`: `Reajuste`, `Dotacao`, `Correcao`), `Descricao` (`string`), `DataRegistro` (`DateOnly`).
- **`Garantia`** — `GarantiaId`, `Modalidade` (`ModalidadeGarantia`: `CaucaoDinheiro`, `SeguroGarantia`, `FiancaBancaria`, `TitulosDividaPublica`), `Percentual` (`decimal`), `Valor` (`ValorMonetario`), `ValidadeFim` (`DateOnly`).

### 2.4 Enum `SituacaoContrato`

| Valor | Numérico | Descrição |
|---|---|---|
| `Assinado` | 1 | Assinado, ainda **sem** eficácia (não publicado no PNCP / sem dotação confirmada). |
| `Eficaz` | 2 | Publicado no PNCP e com cobertura orçamentária — apto a executar. |
| `EmExecucao` | 3 | Vigência iniciada; execução em andamento. |
| `Encerrado` | 4 | Vigência concluída / objeto entregue (terminal). |
| `Rescindido` | 5 | Extinto antecipadamente (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Encerrado** = { `Encerrado`, `Rescindido` }.
> - **Eficaz/operável** = { `Eficaz`, `EmExecucao` }.
> - **Apto a aditivo/apostilamento** = { `Assinado`, `Eficaz`, `EmExecucao` } (≠ encerrado).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** `Objeto` é obrigatório (não nulo/não vazio) na celebração; caso contrário, `ArgumentException`.
- **I-2.** `ValorContratado` é obrigatório (`ArgumentNullException.ThrowIfNull`); na celebração, `ValorAtual = ValorContratado`.
- **I-3.** `VigenciaInicio <= VigenciaFim`; caso contrário, `ArgumentException`.
- **I-4.** Se `OrigemContratacao = Licitacao`, `LicitacaoId` é obrigatório; se `Dispensa`/`Inexigibilidade`, `LicitacaoId` é nulo e exige justificativa do enquadramento.
- **I-5.** **Dispensa por valor** (art. 75): obras/serviços de engenharia até **R$ 119.812,47**; demais bens/serviços até **R$ 59.906,02** (limites **parametrizáveis por tenant**, nunca hardcoded). Acima do limite, `Dispensa` é rejeitada.
- **I-6.** Na celebração, a situação inicial é `Assinado` e é emitido o evento `ContratoAssinado`.
- **I-7.** **Eficácia exige publicação no PNCP** (art. 174): `IniciarExecucao` só ocorre se `PublicadoNoPncp == true`; caso contrário, `InvalidOperationException`.
- **I-8.** **Vigência exige crédito orçamentário** (art. 105–106; LRF): `IniciarExecucao` só ocorre se `DotacaoConfirmada == true` (confirmada por `EmpenhoEmitidoIntegrationEvent`); `DotacaoIndisponivelIntegrationEvent` bloqueia a eficácia/assinatura.
- **I-9.** **Limite de aditivo quantitativo** (art. 125): a soma dos percentuais de `Acrescimo`/`Supressao` não pode exceder **25%** do valor original em bens/serviços; em **reforma de edifício/equipamento**, o acréscimo admite até **50%**. Excedente ⇒ `InvalidOperationException`.
- **I-10.** `CelebrarAditivo` só ocorre sobre contrato **apto a aditivo** ({`Assinado`,`Eficaz`,`EmExecucao`}); atualiza `ValorAtual`/`VigenciaFim` conforme o tipo e emite `AditivoCelebrado`.
- **I-11.** **Aditivo é eficaz somente após publicação no PNCP** (art. 174): a publicação do aditivo é condição de sua eficácia (`AditivoCeleradoIntegrationEvent` dispara a publicação no Outbox).
- **I-12.** **Garantia** (art. 96): `Percentual` ≤ **5%** do valor; em **obras de grande vulto**, até **10%** (art. 98). Acima, `InvalidOperationException`.
- **I-13.** `Apostilar` registra alteração que **dispensa termo aditivo** (reajuste/dotação, art. 136); não altera o limite de 25% nem exige novo certame.
- **I-14.** Contrato **encerrado** ({`Encerrado`,`Rescindido`}) não admite aditivo, apostilamento, garantia, início de execução nem nova publicação.
- **I-15.** `RescindirContrato` exige `motivo` não vazio (motivação do ato administrativo).
- **I-16.** Toda transição é **tenant-scoped**: o agregado nunca é lido/gravado fora do seu `TenantId`.

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Celebrar` | `Assinado` | objeto/valor/vigência válidos; enquadramento da origem (I-4/I-5) | `ContratoAssinado` |
| `Assinado` | `PublicarContratoNoPncp` | `Assinado`→(eficácia parcial) | `numeroContrato` não vazio | `ContratoPublicadoPncp` |
| `Assinado` | `ConfirmarDotacao` (consumo de evento) | `Assinado` | `EmpenhoEmitidoIntegrationEvent` recebido | — |
| `Assinado` | `Tornar Eficaz` (interno) | `Eficaz` | `PublicadoNoPncp && DotacaoConfirmada` | — |
| `Eficaz` | `IniciarExecucao` | `EmExecucao` | eficácia + dotação (I-7/I-8) | — |
| `Assinado`\|`Eficaz`\|`EmExecucao` | `CelebrarAditivo` | (mantém) | ≠ encerrado; limite 25%/50% (I-9) | `AditivoCelebrado` |
| `Assinado`\|`Eficaz`\|`EmExecucao` | `Apostilar` | (mantém) | ≠ encerrado | — |
| `Assinado`\|`Eficaz`\|`EmExecucao` | `PrestarGarantia` | (mantém) | percentual ≤ 5% (10% grande vulto) (I-12) | — |
| `Eficaz`\|`EmExecucao` | `EncerrarContrato` | `Encerrado` | ≠ encerrado | `ContratoEncerrado` |
| `Assinado`\|`Eficaz`\|`EmExecucao` | `RescindirContrato` | `Rescindido` | `motivo` não vazio; ≠ encerrado | `ContratoRescindido` |

> Observações:
> - `PublicarContratoNoPncp` marca `PublicadoNoPncp = true`; quando também `DotacaoConfirmada`, o agregado transita internamente para `Eficaz` (a ordem entre publicação e confirmação de dotação é indiferente).
> - `ConfirmarDotacao` é acionada pelo **consumo** de `EmpenhoEmitidoIntegrationEvent` (handler de integração), não por comando externo do usuário.
> - `DotacaoIndisponivelIntegrationEvent` mantém o contrato **sem eficácia** e impede `IniciarExecucao`.

---

## 5. Comandos (escrita)

Cada comando tem `*Command` + `*Handler` + `*Validator` (FluentValidation). Todos resolvem `ITenantContext` e respeitam o Global Query Filter.

### 5.1 CelebrarContrato

- **Command:** `CelebrarContratoCommand(Guid? LicitacaoId, Guid FornecedorId, OrigemContratacao Origem, string Objeto, decimal Valor, DateOnly VigenciaInicio, DateOnly VigenciaFim, Guid? EmpenhoId, string? NumeroEmpenho, string? JustificativaContratacaoDireta) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; objeto/valor/vigência válidos (I-1/I-2/I-3); coerência origem×`LicitacaoId` (I-4); se `Dispensa`, valor dentro do limite (I-5) e justificativa presente.
- **Efeito:** cria via `Contrato.Celebrar(...)`; `contratos.Adicionar(contrato)`; `SaveChangesAsync`; enfileira `ContratoAssinadoIntegrationEvent` no Outbox (Financas → empenho).
- **Pós-condições:** novo `Contrato` em `Assinado`; retorna `contrato.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException`; `ArgumentException` (objeto/valor/vigência); `InvalidOperationException` (origem incoerente, dispensa acima do limite).
- **Evento de domínio:** `ContratoAssinado(Id, fornecedorId, valor)`.
- **Evento de integração (publica):** `ContratoAssinadoIntegrationEvent`.

### 5.2 PublicarContratoNoPncp

- **Command:** `PublicarContratoNoPncpCommand(Guid ContratoId, string NumeroContratoPncp) : ICommand`.
- **Pré-condições:** `request` não nulo; contrato existe; não encerrado; `NumeroContratoPncp` não vazio.
- **Efeito:** `contrato.PublicarContratoPncp(numero)`; marca `PublicadoNoPncp = true`; se já `DotacaoConfirmada`, torna-se `Eficaz`; `SaveChangesAsync`; enfileira `ContratoPublicadoPncpIntegrationEvent` no Outbox; a publicação efetiva é feita pelo handler do Outbox (cliente PNCP — §11).
- **Pós-condições:** `NumeroContratoPncp` preenchido; eficácia condicionada também à dotação.
- **Exceções:** `ArgumentNullException`; `ArgumentException`; `InvalidOperationException`.
- **Evento de domínio:** `ContratoPublicadoPncp(Id, numero)`.
- **Evento de integração (publica):** `ContratoPublicadoPncpIntegrationEvent`.

### 5.3 IniciarExecucaoContrato

- **Command:** `IniciarExecucaoContratoCommand(Guid ContratoId) : ICommand`.
- **Pré-condições:** `request` não nulo; contrato existe; situação `Eficaz`; `PublicadoNoPncp` (I-7) **e** `DotacaoConfirmada` (I-8).
- **Efeito:** `contrato.IniciarExecucao()`; passa a `EmExecucao`; `SaveChangesAsync`.
- **Pós-condições:** situação `EmExecucao`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (sem eficácia/PNCP/dotação).
- **Evento de domínio:** —.

### 5.4 CelebrarAditivo

- **Command:** `CelebrarAditivoCommand(Guid ContratoId, TipoAditivo Tipo, decimal Percentual, decimal ValorDelta, DateOnly? NovaVigenciaFim, string Justificativa) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; contrato existe; apto a aditivo (≠ encerrado); soma de percentuais ≤ 25% (até 50% em reforma) (I-9); justificativa presente.
- **Efeito:** `contrato.CelebrarAditivo(...)`; atualiza `ValorAtual`/`VigenciaFim`; `SaveChangesAsync`; enfileira `AditivoCeleradoIntegrationEvent` no Outbox (publicação no PNCP — condição de eficácia do aditivo).
- **Pós-condições:** novo `Aditivo` registrado; `ValorAtual` atualizado; retorna `aditivo.AditivoId.Value`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (limite excedido, contrato encerrado).
- **Evento de domínio:** `AditivoCelebrado(Id, aditivoId, percentualAcumulado)`.
- **Evento de integração (publica):** `AditivoCeleradoIntegrationEvent`.

### 5.5 ApostilarContrato

- **Command:** `ApostilarContratoCommand(Guid ContratoId, TipoApostilamento Tipo, string Descricao) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; contrato existe; ≠ encerrado.
- **Efeito:** `contrato.Apostilar(tipo, descricao)`; `SaveChangesAsync` (apostilamento dispensa termo aditivo — art. 136).
- **Pós-condições:** novo `Apostilamento` registrado.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (descrição vazia); `InvalidOperationException` (encerrado).
- **Evento de domínio:** —.

### 5.6 PrestarGarantia

- **Command:** `PrestarGarantiaCommand(Guid ContratoId, ModalidadeGarantia Modalidade, decimal Percentual, decimal Valor, DateOnly ValidadeFim) : ICommand<Guid>`.
- **Pré-condições:** `request` não nulo; contrato existe; ≠ encerrado; `Percentual` ≤ 5% (até 10% em grande vulto) (I-12).
- **Efeito:** `contrato.PrestarGarantia(...)`; `SaveChangesAsync`.
- **Pós-condições:** nova `Garantia` registrada.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (percentual acima do limite, encerrado).
- **Evento de domínio:** —.

### 5.7 EncerrarContrato

- **Command:** `EncerrarContratoCommand(Guid ContratoId) : ICommand`.
- **Pré-condições:** `request` não nulo; contrato existe; situação ∈ {`Eficaz`,`EmExecucao`}.
- **Efeito:** `contrato.Encerrar()`; passa a `Encerrado`; `SaveChangesAsync`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException`.
- **Evento de domínio:** `ContratoEncerrado(Id)`.

### 5.8 RescindirContrato

- **Command:** `RescindirContratoCommand(Guid ContratoId, string Motivo) : ICommand`.
- **Pré-condições:** `request` não nulo; contrato existe; não encerrado; `Motivo` não vazio (I-15).
- **Efeito:** `contrato.Rescindir(motivo)`; passa a `Rescindido`; `SaveChangesAsync`.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (motivo vazio); `InvalidOperationException`.
- **Evento de domínio:** `ContratoRescindido(Id, motivo)`.

> **Handlers de integração (consumo):** `ConfirmarDotacaoQuandoEmpenhoEmitidoHandler` (consome `EmpenhoEmitidoIntegrationEvent` → marca `DotacaoConfirmada`, registra `EmpenhoRef`, eventualmente torna `Eficaz`) e `BloquearEficaciaQuandoDotacaoIndisponivelHandler` (consome `DotacaoIndisponivelIntegrationEvent` → mantém sem eficácia/impede execução). Idempotentes por `EventId`.

---

## 6. Consultas (leitura)

Toda consulta é **tenant-scoped** via Global Query Filter por `TenantId`.

### 6.1 ObterContratoPorId

- **Query:** `ObterContratoPorIdQuery(Guid ContratoId) : IQuery<ContratoDetalhe?>`.
- **Projeção (DTO):** `ContratoDetalhe(Guid Id, Guid? LicitacaoId, Guid FornecedorId, string Origem, string Objeto, decimal ValorContratado, decimal ValorAtual, DateOnly VigenciaInicio, DateOnly VigenciaFim, string Situacao, bool PublicadoNoPncp, bool DotacaoConfirmada, string? NumeroContratoPncp, IReadOnlyList<AditivoResumo> Aditivos, IReadOnlyList<GarantiaResumo> Garantias)`.

### 6.2 ListarContratosVigentes

- **Query:** `ListarContratosVigentesQuery(DateOnly Referencia) : IQuery<IReadOnlyList<ContratoResumo>>`.
- **Projeção (DTO):** `ContratoResumo(Guid Id, Guid FornecedorId, string Objeto, decimal ValorAtual, DateOnly VigenciaFim, string Situacao)`.
- **Filtros:** situação ∈ {`Eficaz`,`EmExecucao`} e `VigenciaFim >= Referencia`; sempre tenant-scoped.

### 6.3 ListarContratosPorFornecedor

- **Query:** `ListarContratosPorFornecedorQuery(Guid FornecedorId) : IQuery<IReadOnlyList<ContratoResumo>>`.
- **Filtros:** por `FornecedorId`; sempre tenant-scoped.

> Todos os handlers de consulta validam `request` não nulo (`ArgumentNullException.ThrowIfNull`).

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Administracao.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `ContratoAssinado` | `(ContratoId, Guid FornecedorId, decimal Valor)` | `Contrato.Celebrar` |
| `ContratoPublicadoPncp` | `(ContratoId, string NumeroContratoPncp)` | `Contrato.PublicarContratoPncp` |
| `AditivoCelebrado` | `(ContratoId, Guid AditivoId, decimal PercentualAcumulado)` | `Contrato.CelebrarAditivo` |
| `ContratoEncerrado` | `(ContratoId)` | `Contrato.Encerrar` |
| `ContratoRescindido` | `(ContratoId, string Motivo)` | `Contrato.Rescindir` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Administracao.Contracts`)

| Evento | Payload | Publicado por | Consumido por |
|---|---|---|---|
| `ContratoAssinadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ContratoId, Guid FornecedorId, decimal Valor, Guid? LicitacaoId)` | `CelebrarContratoHandler` | **Financas** (empenho) |
| `ContratoPublicadoPncpIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ContratoId, string NumeroContratoPncp)` | `PublicarContratoNoPncpHandler` | Transparencia / PNCP (publicidade) |
| `AditivoCeleradoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ContratoId, Guid AditivoId, decimal ValorAtual, DateOnly NovaVigenciaFim)` | `CelebrarAditivoHandler` | **Financas** (reforço de empenho) / PNCP |

### Integração (consome)

| Evento | Payload | Tratado por | Efeito |
|---|---|---|---|
| `EmpenhoEmitidoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ContratoId, Guid EmpenhoId, string NumeroEmpenho, decimal Valor)` (de **Financas**) | `ConfirmarDotacaoQuandoEmpenhoEmitidoHandler` | marca `DotacaoConfirmada`, grava `EmpenhoRef`, pode tornar `Eficaz` (I-8) |
| `DotacaoIndisponivelIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ContratoId, string Motivo)` (de **Financas**) | `BloquearEficaciaQuandoDotacaoIndisponivelHandler` | mantém sem eficácia; impede `IniciarExecucao` |

---

## 8. Validações (FluentValidation)

### CelebrarContratoValidator (`AbstractValidator<CelebrarContratoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `FornecedorId` | `NotEmpty()` | "Fornecedor é obrigatório." |
| `Origem` | `IsInEnum()` | "Origem da contratação inválida." |
| `Objeto` | `NotEmpty()` + `MaximumLength(500)` | "Objeto é obrigatório (máx. 500)." |
| `Valor` | `GreaterThan(0)` | "Valor contratado deve ser positivo." |
| `VigenciaFim` | `GreaterThanOrEqualTo(x => x.VigenciaInicio)` | "Fim da vigência não pode ser anterior ao início." |
| `LicitacaoId` | `Must(CoerenteComOrigem)` | "Licitação obrigatória quando origem = Licitacao; vedada nas contratações diretas." |
| `JustificativaContratacaoDireta` | `NotEmpty().When(Origem != Licitacao)` | "Justificativa é obrigatória na contratação direta." |

### CelebrarAditivoValidator (`AbstractValidator<CelebrarAditivoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ContratoId` | `NotEmpty()` | "Contrato é obrigatório." |
| `Tipo` | `IsInEnum()` | "Tipo de aditivo inválido." |
| `Percentual` | `InclusiveBetween(0, 50)` | "Percentual de aditivo fora do intervalo legal." |
| `Justificativa` | `NotEmpty()` | "Justificativa do aditivo é obrigatória." |

> O **limite acumulado** de 25% (ou 50% em reforma) é invariante de domínio (I-9), pois depende do somatório dos aditivos existentes — verificado no agregado, não apenas no validator.

### PrestarGarantiaValidator / PublicarContratoNoPncpValidator / RescindirContratoValidator / outros

| Comando | Campo | Regra |
|---|---|---|
| `PrestarGarantiaCommand` | `Percentual` | `InclusiveBetween(0, 10)` (limite fino 5%/10% é invariante de domínio — I-12) |
| `PrestarGarantiaCommand` | `Modalidade`, `Valor` | `IsInEnum()`; `GreaterThan(0)` |
| `PublicarContratoNoPncpCommand` | `ContratoId`, `NumeroContratoPncp` | `NotEmpty()`; `MaximumLength(60)` |
| `RescindirContratoCommand` | `ContratoId`, `Motivo` | `NotEmpty()` ambos |
| `ApostilarContratoCommand` | `ContratoId`, `Tipo`, `Descricao` | `NotEmpty()` ids; `IsInEnum()`; `NotEmpty()` descrição |
| `IniciarExecucaoContratoCommand` | `ContratoId` | `NotEmpty()` |
| `EncerrarContratoCommand` | `ContratoId` | `NotEmpty()` |

---

## 9. Persistência (EF Core 8)

- **Schema:** `administracao` (isolado por módulo). **DbContext:** `AdministracaoDbContext`. **Migrations:** por módulo.
- **Tabela:** `Contrato` (raiz de agregado); entidades-filhas em tabelas próprias.

| Coluna (`Contrato`) | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor `ContratoId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `LicitacaoId` | `Guid?` | referência à licitação de origem. |
| `FornecedorId` | `Guid` | referência ao contratado. |
| `OrigemContratacao` | `int` | enum `OrigemContratacao`. |
| `Objeto` | `nvarchar(500)` | obrigatório. |
| `ValorContratado` | `decimal(18,2)` | owned de `ValorMonetario`. |
| `ValorAtual` | `decimal(18,2)` | owned de `ValorMonetario`. |
| `VigenciaInicio` | `date` | `DateOnly`. |
| `VigenciaFim` | `date` | `DateOnly`. |
| `EmpenhoId` | `Guid?` | parte de `EmpenhoRef` (owned). |
| `NumeroEmpenho` | `nvarchar(40)?` | parte de `EmpenhoRef` (owned). |
| `DotacaoConfirmada` | `bit` | confirmação orçamentária. |
| `Situacao` | `int` | enum `SituacaoContrato`. |
| `NumeroContratoPncp` | `nvarchar(60)?` | nulável até publicação. |
| `PublicadoNoPncp` | `bit` | eficácia pela publicação (art. 174). |

- **Tabelas-filhas:** `ContratoAditivo`, `ContratoApostilamento`, `ContratoGarantia` — FK para `Contrato(Id)` e `TenantId` herdado.
- **Índices:**
  - PK em `Id`.
  - Índice em `(TenantId, FornecedorId)` para `ListarContratosPorFornecedor`.
  - Índice em `(TenantId, Situacao, VigenciaFim)` para `ListarContratosVigentes`.
  - Índice único em `(TenantId, NumeroContratoPncp)` quando não nulo.
  - Índice em `ContratoAditivo(TenantId, ContratoId)`.
- **Outbox:** tabela Outbox do contexto Administracao para `ContratoAssinadoIntegrationEvent`, `ContratoPublicadoPncpIntegrationEvent` e `AditivoCeleradoIntegrationEvent`. Consumo de `EmpenhoEmitidoIntegrationEvent`/`DotacaoIndisponivelIntegrationEvent` com **deduplicação por `EventId`** (idempotência do Inbox).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `Contrato` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based + RBAC, negar por padrão) — segregação de funções:**
  - Celebração/assinatura: **ordenador de despesa** (ex.: `Administracao.Contrato.Celebrar`).
  - Aditivos/apostilamentos: ordenador + parecer jurídico (ex.: `Administracao.Contrato.Alterar`).
  - Fiscalização/execução: **fiscal do contrato** (papel distinto do ordenador) (ex.: `Administracao.Contrato.Fiscalizar`).
  - Consulta: **leitura de compras** (ex.: `Administracao.Contrato.Ler`).
  - Módulo Administracao é **ativável por tenant**; requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** dados majoritariamente de pessoa jurídica; representantes pessoa física com minimização; trilha de acesso registrada.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em toda mutação (`Celebrar`, `PublicarContratoPncp`, `IniciarExecucao`, `CelebrarAditivo`, `Apostilar`, `PrestarGarantia`, `Encerrar`, `Rescindir`) — para o Tribunal de Contas (TCE-RS) e transparência ativa (LAI), com registro de **valor** antes/depois nos aditivos.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **PNCP (Decreto 11.462/2023; art. 174 da NLLC) — SAÍDA:** publicação de contratos e aditivos. Cliente REST/JSON credenciado por CNPJ, **resiliente (Polly: retry + circuit breaker + timeout)**, chamado por **handler do Outbox**, **idempotente** por chave da contratação. A publicação é **condição de eficácia** do contrato e dos aditivos. Atrás de **Anti-Corruption Layer**.
- **Financas (intra-aplicação, via Contracts) — SAÍDA/ENTRADA:**
  - Saída: ao celebrar, publica `ContratoAssinadoIntegrationEvent` (Outbox) → Financas emite empenho (Lei 4.320; LRF). Idempotente por `EventId`.
  - Entrada: consome `EmpenhoEmitidoIntegrationEvent` (confirma cobertura orçamentária — `DotacaoConfirmada`) e `DotacaoIndisponivelIntegrationEvent` (bloqueia eficácia/assinatura). Cross-module ocorre **exclusivamente** via estes Integration Events.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de ACL.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Contrato dispara empenho em Financas**
- **Dado** um `Contrato` recém-assinado com dotação informada
- **Quando** `ContratoAssinadoIntegrationEvent` é publicado
- **Então** **Financas** consome o evento e retorna `EmpenhoEmitidoIntegrationEvent`, que marca `DotacaoConfirmada = true`.

**Cenário 2 — Eficácia condicionada ao PNCP**
- **Dado** um `Contrato` assinado mas ainda **não** publicado no PNCP
- **Quando** se tenta `IniciarExecucaoContratoCommand`
- **Então** o sistema bloqueia (`InvalidOperationException`), pois a publicação no PNCP é condição de eficácia (I-7).

**Cenário 3 — Eficácia condicionada à dotação**
- **Dado** um `Contrato` publicado no PNCP mas sem `EmpenhoEmitidoIntegrationEvent`
- **Quando** se tenta `IniciarExecucaoContratoCommand`
- **Então** o sistema bloqueia, pois falta cobertura orçamentária (I-8).

**Cenário 4 — Dispensa válida por valor**
- **Dado** um TR de serviços comuns no valor de R$ 40.000,00
- **Quando** o agente registra `CelebrarContratoCommand` com `Origem = Dispensa`
- **Então** o sistema aceita o enquadramento (≤ R$ 59.906,02) e exige justificativa (I-5).

**Cenário 5 — Dispensa acima do limite**
- **Dado** um serviço comum no valor de R$ 80.000,00
- **Quando** se registra `Origem = Dispensa`
- **Então** o sistema rejeita o enquadramento (acima de R$ 59.906,02) (I-5).

**Cenário 6 — Aditivo acima do limite legal**
- **Dado** um `Contrato` de serviços com aditivos somando 20%
- **Quando** se solicita acréscimo de mais 10% (total 30%)
- **Então** o sistema rejeita o `Aditivo` por exceder 25% (art. 125) (I-9).

**Cenário 7 — Aditivo dentro do limite**
- **Dado** um `Contrato` de serviços com aditivos somando 10%
- **Quando** se celebra acréscimo de mais 10% (total 20%)
- **Então** o `Aditivo` é registrado, `ValorAtual` é atualizado e `AditivoCeleradoIntegrationEvent` é enfileirado.

**Cenário 8 — Reforma admite 50%**
- **Dado** um `Contrato` de reforma de edifício com acréscimos somando 40%
- **Quando** se celebra acréscimo de mais 10% (total 50%)
- **Então** o `Aditivo` é aceito (limite de 50% em reforma) (I-9).

**Cenário 9 — Garantia acima do limite**
- **Dado** um `Contrato` comum
- **Quando** se executa `PrestarGarantiaCommand` com `Percentual = 8%`
- **Então** o sistema rejeita (limite 5% fora de grande vulto) (I-12).

**Cenário 10 — Apostilamento dispensa aditivo**
- **Dado** um `Contrato` `EmExecucao` com reajuste contratual
- **Quando** se executa `ApostilarContratoCommand(Reajuste, ...)`
- **Então** o `Apostilamento` é registrado sem termo aditivo (art. 136) (I-13).

**Cenário 11 — Dotação indisponível bloqueia eficácia**
- **Dado** um `Contrato` assinado
- **Quando** chega `DotacaoIndisponivelIntegrationEvent`
- **Então** o contrato permanece sem eficácia e `IniciarExecucao` falha.

**Cenário 12 — Rescisão exige motivação**
- **Dado** um `Contrato` `EmExecucao`
- **Quando** se executa `RescindirContratoCommand(id, "")`
- **Então** a validação rejeita por `Motivo` vazio (I-15).

**Cenário 13 — Consulta tenant-scoped**
- **Dado** contratos no tenant A e no tenant B
- **Quando** executo `ListarContratosVigentesQuery` no contexto do tenant A
- **Então** retornam **apenas** os contratos do tenant A.

---

## 13. Casos de Borda

- **B-1.** `Objeto` vazio/nulo em `Celebrar` ⇒ `ArgumentException` (I-1).
- **B-2.** `VigenciaFim < VigenciaInicio` ⇒ `ArgumentException` (I-3).
- **B-3.** `Origem = Licitacao` com `LicitacaoId` nulo (ou vice-versa) ⇒ rejeitado (I-4).
- **B-4.** `Dispensa` para obra de engenharia em R$ 130.000,00 ⇒ rejeitada (> R$ 119.812,47) (I-5).
- **B-5.** Limites de dispensa **parametrizáveis por tenant**: alteração de parâmetro reflete sem recompilar (CLAUDE.md §7).
- **B-6.** `IniciarExecucao` com PNCP publicado porém sem dotação ⇒ `InvalidOperationException` (I-8).
- **B-7.** `IniciarExecucao` com dotação porém sem PNCP ⇒ `InvalidOperationException` (I-7).
- **B-8.** Aditivos acumulados exatamente em 25% ⇒ aceito; 25,01% ⇒ rejeitado (I-9).
- **B-9.** Reforma: acréscimo acumulado exatamente em 50% ⇒ aceito; acima ⇒ rejeitado (I-9).
- **B-10.** `Garantia` com 5% (comum) ⇒ aceito; 10% só em **grande vulto** (I-12).
- **B-11.** Aditivo sobre contrato `Encerrado`/`Rescindido` ⇒ `InvalidOperationException` (I-14).
- **B-12.** Publicar contrato no PNCP duas vezes ⇒ idempotente por chave da contratação no Outbox.
- **B-13.** `EmpenhoEmitidoIntegrationEvent` reentregue ⇒ idempotente por `EventId` (não duplica `DotacaoConfirmada`).
- **B-14.** `NumeroContratoPncp` duplicado no mesmo tenant ⇒ violação do índice único `(TenantId, NumeroContratoPncp)`.
- **B-15.** Celebração/assinatura por papel que não seja **ordenador de despesa** ⇒ 403 auditado (segregação de funções).
- **B-16.** Aditivo de `Prazo` sem alteração de valor ⇒ não conta para o limite de 25% (somente quantitativos contam) (I-9).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — derivada do README do módulo Administracao e da Lei 14.133/2021 (formalização, eficácia via PNCP art. 174, aditivos/apostilamento art. 125/136, garantia art. 96/98, vigência vinculada a crédito art. 105–106) e integração com Financas (empenho). |
| 1.1.0 | 2026-06-24 | **W9.1 — PNCP bloqueante + calendário.** CORREÇÃO LEGAL: eficácia da divulgação é do **art. 94** (art. 174 apenas institui o PNCP); gestão do PNCP pelo **Dec. 10.764/2021** (o 11.462/2023 trata de SRP). Adicionados: VO `PrazoPncp` + `DataAssinatura` + prazo de divulgação (20 d.u. — `IPncpParametros`, parametrizável por tenant, usando `ICalendarioDiasUteis`); flag `PublicacaoPncpVencida` (tempestividade art. 94). ACL `IPncpGateway` (M9 SIMULADO/WireMock; transmissão real = M10) chamada pelo handler que enfileira no Outbox. **INVARIANTE DE BLOQUEIO**: contrato sem nº de controle PNCP **não empenha** (`Contrato.PodeEmpenhar`); Financas consulta via `IConsultaContratoParaEmpenho` (cross-module via Contracts, escopo dedicado) antes de empenhar — fail-closed. Eventos `PrazoPncpAVencer`/`PrazoPncpVencido` → Portal do Gestor (varredor `VarrerPrazosPncp`). |

### Cenários W9.1 (BDD)
- **P-1.** Empenho de despesa de contrato **sem** nº de controle PNCP ⇒ **bloqueado** (contrato ineficaz, art. 94). Fail-closed.
- **P-2.** Empenho de contrato **divulgado** (com nº PNCP) e não extinto ⇒ permitido.
- **P-3.** Empenho de contrato **encerrado/rescindido** ⇒ bloqueado (extinto).
- **P-4.** Divulgação no PNCP **após** a data-limite (art. 94) ⇒ `PublicadoNoPncp = true` (eficaz) **e** `PublicacaoPncpVencida = true` (auditável), sem impedir a eficácia.
- **P-5.** Contrato pendente de divulgação dentro da janela de antecedência ⇒ `PrazoPncpAVencerIntegrationEvent` ao Portal do Gestor (idempotente por `EventId`).
- **P-6.** Contrato pendente de divulgação com prazo vencido ⇒ `PrazoPncpVencidoIntegrationEvent` ao Portal do Gestor.

<!-- manifest
commands: CelebrarContrato, PublicarContratoNoPncp, IniciarExecucaoContrato, CelebrarAditivo, ApostilarContrato, PrestarGarantia, EncerrarContrato, RescindirContrato, VarrerPrazosPncp
queries: ObterContratoPorId, ListarContratosVigentes, ListarContratosPorFornecedor
domainEvents: ContratoAssinado, ContratoPublicadoPncp, AditivoCelebrado, ContratoEncerrado, ContratoRescindido
integrationEventsPublished: ContratoAssinadoIntegrationEvent, ContratoPublicadoPncpIntegrationEvent, AditivoCeleradoIntegrationEvent, PrazoPncpAVencerIntegrationEvent, PrazoPncpVencidoIntegrationEvent
integrationEventsConsumed: EmpenhoEmitidoIntegrationEvent, DotacaoIndisponivelIntegrationEvent
contractsPorts: IConsultaContratoParaEmpenho
acl: IPncpGateway (M9 simulado/WireMock; transmissão real = M10)
-->
