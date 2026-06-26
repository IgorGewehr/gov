---
modulo: Administracao
agregado: DispensaEletronica
contexto: Administracao (Compras Públicas — contratação direta por dispensa em razão do valor, na forma eletrônica)
poder: Ambos
schema: administracao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "Lei 14.133/2021 (NLLC) — norma central das compras públicas"
  - "Lei 14.133/2021 art. 14 e art. 156 (sanções; impedimento/inidoneidade — fail-closed)"
  - "Lei 14.133/2021 art. 75, I e II (dispensa por valor: obras/serviços de engenharia; outros serviços e compras)"
  - "Lei 14.133/2021 art. 75 §3º (divulgação em sítio eletrônico oficial — prazo mínimo)"
  - "Lei 14.133/2021 art. 174 (PNCP — condição de eficácia/divulgação)"
  - "Lei 14.133/2021 art. 182 (atualização anual dos valores-limite pelo IPCA-E)"
  - "IN SEGES/ME 67/2021 (Sistema de Dispensa Eletrônica — aviso de contratação direta, lances sucessivos, julgamento, habilitação)"
  - "Decreto 12.807/2025 (atualização dos valores de dispensa, vigência 01/01/2026 — revogou o Decreto 12.343/2024)"
---

# DispensaEletronica — Regras-as-Code (Rules-as-Code)

> Procedimento de **contratação direta por DISPENSA em razão do valor**, conduzido na forma
> **ELETRÔNICA** (Lei 14.133/2021, art. 75, I e II; **IN SEGES/ME 67/2021** — Sistema de Dispensa
> Eletrônica). Promove disputa competitiva por **menor preço** ou **maior desconto** entre
> fornecedores: **aviso de contratação direta** (prazo mínimo de divulgação) → **etapa de lances
> sucessivos** → **julgamento/classificação** → **negociação/habilitação** → **homologação** pela
> autoridade competente. **Fail-closed** quanto ao limite legal: o valor total estimado **não pode
> exceder** o limite de dispensa vigente (parametrizável por tenant — Dec. 12.807/2025 e atualizações
> anuais pelo IPCA-E, art. 182), nem se pode homologar fornecedor com **sanção impeditiva vigente**
> (art. 14/156). Raiz de agregado, **tenant-scoped**. Este arquivo é **normativo e versionado**; o
> código de domínio, aplicação, persistência e testes do agregado `DispensaEletronica` é **gerado e
> mantido a partir daqui**. Bug, ajuste ou nova regra ⇒ edita-se **este arquivo**; o código é
> consequência.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Dispensa Eletrônica (`DispensaEletronica`) | Procedimento de contratação direta por dispensa em razão do valor, na forma eletrônica. Raiz de agregado. |
| Identidade (`DispensaEletronicaId`) | Identidade forte do agregado (`readonly record struct` sobre `Guid`). |
| Objeto (`Objeto` : `string`) | Descrição do objeto da contratação direta. |
| Fundamento (`Fundamento` : `FundamentoDispensaValor`) | Hipótese legal: `ObrasEServicosEngenharia` (art. 75, I) ou `OutrosServicosECompras` (art. 75, II). Enum. |
| Critério de Julgamento (`CriterioJulgamento` : `CriterioJulgamentoDispensa`) | `MenorPreco` ou `MaiorDesconto` (IN 67/2021, art. 1º §2º). Enum. |
| Limite Legal Vigente (`LimiteLegalVigente` : `ValorMonetario`) | Limite de dispensa aplicável ao procedimento, **injetado pela borda** (parâmetro do tenant). Value Object. |
| Norma-Fonte do Limite (`LimiteLegalNormaFonte` : `string`) | Norma que fundamenta o limite aplicado (rastreabilidade da parametrização). |
| ETP — Estudo Técnico Preliminar (`EtpId`) | Estudo que fundamenta a necessidade da contratação (referência por Id, opcional). |
| TR — Termo de Referência (`TermoReferenciaId`) | Documento que define objeto, requisitos e critérios (referência por Id, opcional). |
| Item (`ItemDispensa`) | Item disputado (objeto do aviso). Carrega quantidade e valor unitário estimado. Entidade-filha. |
| Cotação (`CotacaoDispensa`) | Lance/proposta de um fornecedor para um item, na etapa de disputa. Entidade-filha. |
| Aviso (`NumeroAviso`) | Número/identificador do aviso de contratação direta publicado. |
| Abertura da Disputa (`AberturaDisputa` : `DateTimeOffset?`) | Data/hora de abertura da etapa de lances (definida na publicação do aviso). |
| Fornecedor (`FornecedorId`) | Proponente (outra raiz; referência por Id). |
| Cotação Vencedora (`CotacaoVencedoraId`) | Cotação julgada vencedora antes da homologação. |
| PNCP (`NumeroPncp` / `RegistrarPublicacaoPncp`) | Identificador da contratação no Portal Nacional de Contratações Públicas (art. 174). |
| Situação (`Situacao` : `SituacaoDispensa`) | Estado atual do procedimento no ciclo de vida. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |

---

## 2. Modelo

- **Raiz de agregado** `DispensaEletronica : AggregateRoot<DispensaEletronicaId>, IMustHaveTenant`.
- **Entidades-filhas** (coleções privadas, expostas como `IReadOnlyCollection<>`):
  - `ItemDispensa` (`_itens`) — `Numero`, `ItemCatalogoId?`, `Descricao`, `Quantidade`, `ValorUnitarioEstimado`; `ValorTotalEstimado = Quantidade × ValorUnitario`.
  - `CotacaoDispensa` (`_cotacoes`) — `FornecedorId`, `ItemId`, `Valor`, `DataRegistro`, `Sequencia` (monotônica, para desempate determinístico após reidratação), `Classificacao?`, `Situacao`.
- **Value Objects / enums:** `ValorMonetario`; `FundamentoDispensaValor`, `CriterioJulgamentoDispensa`, `SituacaoDispensa`, `SituacaoCotacao`.
- **Propriedade derivada:** `ValorTotalEstimado` = soma do `ValorTotalEstimado` dos itens.
- Construtor privado + *factory* `Abrir(...)`; entidade nasce válida (invariantes no factory).
- **Relógio externo:** `DateTimeOffset` é sempre injetado pelo handler (borda); nunca lido no domínio.

---

## 3. Invariantes

- **I-1.** `Objeto`, `LimiteLegalNormaFonte` não vazios; `LimiteLegalVigente` não nulo (factory).
- **I-2.** `Fundamento` e `CriterioJulgamento` devem ser valores definidos do enum (`ArgumentOutOfRangeException`).
- **I-3.** Itens só podem ser adicionados com a dispensa `Aberta`.
- **I-4. (fail-closed — teto legal)** O valor total estimado **não pode ultrapassar** `LimiteLegalVigente`; inclusão que estouraria o teto é recusada (`InvalidOperationException`) — dispensa acima do limite é ato nulo (exigiria licitação).
- **I-5.** Publicar aviso exige dispensa `Aberta` e **ao menos um item**; `NumeroAviso` não vazio.
- **I-6.** Abrir disputa exige `AvisoPublicado` e `agora >= AberturaDisputa` (prazo mínimo de divulgação).
- **I-7.** Lances só com a disputa `EmDisputa`; o item deve pertencer à dispensa.
- **I-8. (fail-closed — sanção)** Fornecedor com sanção impeditiva vigente **não pode cotar** (art. 14/156; aferido na borda via `fornecedorImpedido`).
- **I-9.** Lance sucessivo do mesmo fornecedor para o mesmo item deve **melhorar** a oferta vigente conforme o critério (menor preço: estritamente menor; maior desconto: estritamente maior).
- **I-10.** Julgamento exige `EmDisputa` e **ao menos uma cotação válida**; vencedora = fornecedor de melhor **total agregado** (proposta global), desempate pela **menor sequência**.
- **I-11.** Homologação exige `EmJulgamento`, `CotacaoVencedoraId` definida, **vencedor habilitado** e **não impedido** (art. 14/156).
- **I-12.** Fracassada exige inexistência de cotação válida; Deserta exige ausência total de cotações.
- **I-13.** Estados terminais (`Homologada`, `Fracassada`, `Deserta`, `Revogada`, `Anulada`) não admitem novas transições.

---

## 4. Máquina de Estados (`SituacaoDispensa`)

```
Aberta ──PublicarAviso──▶ AvisoPublicado ──AbrirDisputa──▶ EmDisputa ──EncerrarDisputaEJulgar──▶ EmJulgamento ──Homologar──▶ Homologada (terminal)
   │                                                            │
   │                                                            └── (sem cotação válida) ──DeclararFracassada──▶ Fracassada (terminal)
   │
   ├── (não-terminal) ──DeclararDeserta──▶ Deserta (terminal, sem cotações)
   ├── (não-terminal) ──Revogar──▶ Revogada (terminal — conveniência/oportunidade)
   └── (não-terminal) ──Anular──▶ Anulada (terminal — ilegalidade)
```

Valores: `Aberta=1`, `AvisoPublicado=2`, `EmDisputa=3`, `EmJulgamento=4`, `Homologada=5`, `Fracassada=6`, `Deserta=7`, `Revogada=8`, `Anulada=9`.

---

## 5. Comandos (escrita)

| Comando (`*Command`) | Método de domínio | Efeito |
|---|---|---|
| `AbrirDispensa` (→ `Guid`) | `DispensaEletronica.Abrir` | Cria a dispensa em `Aberta`. Limite legal + norma-fonte injetados pela borda. Emite `DispensaAberta`. |
| `AdicionarItemDispensa` (→ `Guid`) | `AdicionarItem` | Adiciona item (I-3, I-4). |
| `PublicarAvisoDispensa` | `PublicarAviso` | Publica o aviso (I-5); → `AvisoPublicado`. Emite `AvisoDispensaPublicado`. |
| `AbrirDisputaDispensa` | `AbrirDisputa` | Abre lances (I-6); → `EmDisputa`. Emite `DisputaDispensaAberta`. |
| `RegistrarLanceDispensa` (→ `Guid`) | `RegistrarLance` | Registra lance (I-7, I-8, I-9). |
| `EncerrarDisputaDispensa` | `EncerrarDisputaEJulgar` | Encerra e julga (I-10); → `EmJulgamento`. |
| `HomologarDispensa` | `Homologar` | Homologa (I-11); → `Homologada`. Emite `DispensaHomologada`. |
| `DeclararDispensaFracassada` | `DeclararFracassada` | → `Fracassada` (I-12). Emite `DispensaFracassada`. |
| `DeclararDispensaDeserta` | `DeclararDeserta` | → `Deserta` (I-12). Emite `DispensaDeserta`. |
| `RevogarDispensa` | `Revogar` | → `Revogada`. Emite `DispensaRevogada`. |
| `AnularDispensa` | `Anular` | → `Anulada`. Emite `DispensaAnulada`. |

> A publicação no PNCP é registrada por `RegistrarPublicacaoPncp(numeroPncp)` no domínio (rastreabilidade do `NumeroPncp`); não há comando dedicado nesta versão.

---

## 6. Consultas (leitura)

| Consulta (`*Query`) | Retorno | Uso |
|---|---|---|
| `ObterDispensaPorId` | `DispensaDetalhe?` | Detalhe da dispensa (itens, cotações, situação). |
| `ListarDispensasPorSituacao` | `IReadOnlyList<DispensaResumo>` | Lista resumida filtrada por `SituacaoDispensa`. |

---

## 7. Eventos

**Domain Events** (in-process):
`DispensaAberta`, `AvisoDispensaPublicado`, `DisputaDispensaAberta`, `DispensaHomologada`,
`DispensaFracassada`, `DispensaDeserta`, `DispensaRevogada`, `DispensaAnulada`.

**Integration Events PUBLICADOS** (via Outbox, em `*.Contracts`):
`DispensaHomologadaIntegrationEvent` (`EventId`, `OccurredOnUtc`, `TenantId`, `DispensaId`,
`FornecedorVencedorId`, `ValorAdjudicado`) — habilita a formalização do contrato/empenho decorrente
(consumível, p.ex., por Finanças). Idempotente por `EventId` no consumidor.

**Integration Events CONSUMIDOS:** nenhum nesta versão.

---

## 8. Segurança, Tenant e Auditoria

- `IMustHaveTenant`: toda operação é tenant-scoped; gravação cross-tenant lança exceção (interceptor da base).
- Limite legal e prazos **parametrizáveis por tenant** (`IDispensaParametros` / `DispensaOptions`); nunca *hardcoded* (CLAUDE.md §7).
- Aferição de **impedimento/inidoneidade** do fornecedor (art. 14/156) ocorre na **borda/handler** e entra no domínio como flag `fornecedorImpedido` / `fornecedorVencedorImpedido` (fail-closed).
- Toda mutação gera trilha de auditoria imutável (interceptor da base).

---

## 9. Integrações Governamentais

- **PNCP (art. 174):** divulgação da contratação direta; `NumeroPncp` registrado para rastreabilidade.
- **Limites de dispensa:** Dec. 12.807/2025 (vigência 2026; revogou o Dec. 12.343/2024) e atualização anual pelo IPCA-E (art. 182) — parâmetro do tenant. Valores 2026: art. 75, I = R$ 130.984,20; II = R$ 65.492,11.

---

## 10. Casos de Borda

- **B-1.** Adicionar item que estoura `LimiteLegalVigente` ⇒ `InvalidOperationException` (I-4).
- **B-2.** Publicar aviso sem itens ⇒ `InvalidOperationException` (I-5).
- **B-3.** Abrir disputa antes de `AberturaDisputa` ⇒ `InvalidOperationException` (I-6).
- **B-4.** Lance que não melhora a oferta vigente ⇒ `InvalidOperationException` (I-9).
- **B-5.** Lance/homologação com fornecedor impedido ⇒ `InvalidOperationException` (I-8/I-11).
- **B-6.** Julgar/Homologar fora da situação exigida ⇒ `InvalidOperationException`.
- **B-7.** Transição sobre dispensa em estado terminal ⇒ `InvalidOperationException` (I-13).
- **B-8.** `DispensaHomologadaIntegrationEvent` idempotente no consumidor por `EventId` (reentrega via Outbox).

---

## 11. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-26 | Versão inicial — derivada do código do agregado `DispensaEletronica` e da Lei 14.133/2021 art. 75 + IN SEGES/ME 67/2021 (Sistema de Dispensa Eletrônica): aviso, lances sucessivos, julgamento por proposta global, homologação, fracasso/deserção/revogação/anulação, teto legal fail-closed, PNCP. |
| 1.0.1 | 2026-06-26 | Conformidade (L7/L8): atualização dos limites do art. 75 ao **Dec. 12.807/2025** (vigência 2026; revoga o Dec. 12.343/2024) — I = R$ 130.984,20; II = R$ 65.492,11. O teto fail-closed (I-4) passa a valer também na **celebração do contrato por dispensa em razão do valor** (`CelebrarContrato`), não só no agregado da dispensa eletrônica. |

<!-- manifest
commands: AbrirDispensa, AdicionarItemDispensa, PublicarAvisoDispensa, AbrirDisputaDispensa, RegistrarLanceDispensa, EncerrarDisputaDispensa, HomologarDispensa, DeclararDispensaFracassada, DeclararDispensaDeserta, RevogarDispensa, AnularDispensa
queries: ObterDispensaPorId, ListarDispensasPorSituacao
domainEvents: DispensaAberta, AvisoDispensaPublicado, DisputaDispensaAberta, DispensaHomologada, DispensaFracassada, DispensaDeserta, DispensaRevogada, DispensaAnulada
integrationEventsPublished: DispensaHomologadaIntegrationEvent
integrationEventsConsumed: 
-->
