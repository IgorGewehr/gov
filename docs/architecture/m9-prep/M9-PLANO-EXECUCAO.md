# M9 — PLANO DE EXECUÇÃO (turnkey)

> **Engenheiro-chefe — consolidação executável do M9.** Costura os 5 designs de `m9-prep/`
> (M9-BREAKDOWN, CALENDARIO-DIAS-UTEIS, OBRAS, CONVENIOS-MROSC, P0-SNAPSHOTS) numa sequência
> única de sub-workflows prontos para rodar. **READ-ONLY na produção desta fase** (sem
> `dotnet`/`npm`, sem derrubar a `:5080`). Disciplina CLAUDE.md: domínio rico, cross-module só
> via `*.Contracts`, integrações atrás de ACL+Polly+Outbox, multi-tenant + auditoria imutável,
> prazos parametrizáveis por tenant. Data: 2026-06-23.
>
> **Ritmo de execução (regra fixa para todas as ondas):**
> - **Build serial** — um único build lane; ondas backend nunca buildam em paralelo (race no
>   `obj/bin` + na `:5080`).
> - **`npx` (frontend Vite/React) isolado** — roda em paralelo, nunca toca a `:5080`.
> - **Read-only em paralelo** — pesquisa, leitura de design, escrita de spec/ADR/rules podem
>   correr concorrentes desde que os **alvos sejam disjuntos** (módulos diferentes).
> - **Cada onda backend = edit-only + verify**: editar → **build + test + runtime + ADICIONAR
>   testes** das invariantes/handlers novos. Sem `dotnet ef migrations add` no hot-path (ver §4).
> - **Creds reais (PNCP / SICOE / ACT / Transferegov / Azure Monitor) = M10.** No M9 tudo é
>   testado contra **WireMock + Testcontainers** do harness W9.8.

---

## 1) SEQUÊNCIA DE SUB-WORKFLOWS (ordem oficial, turnkey)

> Ordem: **W9.0(P0 snapshots) → W9.8(QA harness) → W9.1(PNCP+calendário) → Inbox/OTel(W9.7res)
> → W9.5(Legislativo) → W9.4(Protocolo ACT/CONARQ) → W9.3(Obras) → W9.6(Convênios)**.
> (W9.0 é pré-requisito de robustez de migrations destacado do P0-SNAPSHOTS-PLANO — ele blinda
> o `MigrateAsync` que todas as ondas seguintes vão exercer ao adicionar tabelas. Entra antes do
> harness porque a fitness `HasPendingModelChanges` vive no mesmo projeto de testes que o W9.8 amplia.)

| # | Sub-WF | Escopo (1 linha) | Design de referência | Alvo (backend / src/Web) | Esforço | Em paralelo (npx / read-only) enquanto roda |
|---|---|---|---|---|---|---|
| 0 | **W9.0 — P0 snapshots/Designers** | Fechar drift modelo↔snapshot (Saúde) + 5 Designers órfãos + fitness `HasPendingModelChanges`. | `P0-SNAPSHOTS-PLANO.md` | `tests/…ArchitectureTests` + Designers/snapshot em `Saude/Financas/RH/Identidade.Infrastructure` | **M** | Read-only: pesquisar Manual PNCP v2.5 + leiautes (prep do W9.1); escrever spec BDD do harness W9.8. |
| 1 | **W9.8 — QA harness E2E + WCAG** | Projeto `IntegrationTests` (Testcontainers + WireMock + `WebApplicationFactory` + mock auth) + axe em todas as rotas + E2E licitação→contrato→PNCP→empenho; isolamento cross-tenant nos 11 módulos. | `M9-BREAKDOWN.md §W9.8` | novo `tests/…IntegrationTests`; estende `.a11y.test.tsx` em `src/Web` | **M** | `npx` frontend: estender provas axe nas ~7 telas existentes. Read-only: design dos eventos PNCP (prep W9.1). |
| 2 | **W9.1 — PNCP bloqueante + calendário transversal** | Promover `ICalendarioDiasUteis`+`PrazoLegal` ao SharedKernel/BuildingBlocks; ACL `IPncpGateway` (Polly+idempotência); invariante "sem nº PNCP não empenha"; correções legais art.94/Dec.10.764. | `CALENDARIO-DIAS-UTEIS-DESIGN.md` + `M9-BREAKDOWN.md §W9.1` | `SharedKernel/Tempo`, `BuildingBlocks.*/Tempo`, `Administracao.Infrastructure` (gateway), `Administracao.Domain/Contratos` (invariante) | **G** | `npx`: telas de prazo PNCP / Portal do Gestor. Read-only: pesquisar e-ARQ v2 / DOC-ICP-12 v2.1 (prep W9.4) e LexML Schema v1.0 (prep W9.5). |
| 3 | **W9.7res — Inbox idempotente + OTel multi-tenant** | Inbox (`event_id` por handler+tenant) no consumidor; enrichers OTel (`tenant.id`/`CorrelationId` nos 3 sinais + Baggage do JWT); métricas Outbox/DLQ; fan-out de migrations por banco-por-tenant. | `M9-BREAKDOWN.md §W9.7` | `BuildingBlocks.Infrastructure` (Inbox + OTel), `BuildingBlocks.Application` | **M** | **Pode correr em paralelo a W9.1** desde W9.1-meio (alvos disjuntos: BuildingBlocks vs Administracao). Inbox é pré-req de W9.4/W9.6. Read-only para as ondas seguintes. |
| 4 | **W9.5 — Legislativo LexML + art. 29-A** | VO `UrnLexml` + export XML LexML (Schema v1.0 RC1) + motor art.29-A (teto Câmara, regra temporal EC 109/2021, faixas por população). | `M9-BREAKDOWN.md §W9.5` | `Legislativo.Domain/Application/Infrastructure` | **M** | `npx`: telas LexML/29-A. Read-only: detalhar agregado Obras (prep W9.3) e os dois agregados de Convênios (prep W9.6) — **alvos disjuntos** de Legislativo. |
| 5 | **W9.4 — Protocolo ACT RFC 3161 + CONARQ** | Cliente TSP RFC 3161 (TSQ→TST, `PKIFailureInfo`) atrás de ACL+Polly (substitui o local); motor de temporalidade/destinação e-ARQ v2/CONARQ; fix race do sequencial NUP. | `M9-BREAKDOWN.md §W9.4` | `Protocolo.Domain/Application/Infrastructure` | **M** | `npx`: telas de temporalidade/destinação. Read-only: detalhar relógio §3 da Obra (prep W9.3) — depende do calendário **já entregue** em W9.1. |
| 6 | **W9.3 — Obras (Patrimônio) + RDO + SICOE** | Agregado `Obra` em `Patrimonio.Domain/Obras` (cronograma físico-financeiro, Medição/RDO, fiscalização); relógio art.94 §3 (25/45 d.u.) via `ICalendarioDiasUteis`; mapeador SICOE (transmissão = M10). | `OBRAS-DESIGN.md` | `Patrimonio.Domain/Obras`, `Patrimonio.Application/Infrastructure/Contracts` + consumidor em `Financas.Application` | **G** | `npx`: telas de obra (curva S, medições, RDO, prazos §3). Read-only: detalhar PC/state machines de Convênios (prep W9.6). |
| 7 | **W9.6 — Convênios + MROSC** | Módulo NOVO `Convenios`: dois agregados (`ConvenioRecebido` Dec.11.531/Transferegov + `ParceriaOsc` MROSC Lei 13.019), state machines/prazos próprios, PC parcial/final, gatilho inadimplência→bloqueio. | `CONVENIOS-MROSC-DESIGN.md` | novo `Convenios.{Domain,Application,Infrastructure,Contracts}` + consumo `Financas.Contracts` | **G** | `npx`: telas dos dois fluxos. Read-only: rascunho da auditoria current-state do fim do M9. |

**Por que esta ordem (resumo do encadeamento de dependências reais):**
- **W9.0 antes de tudo:** sem snapshot em sync, qualquer onda que adicione tabela (W9.1 invariante,
  W9.3 Obras, W9.6 Convênios) gera DDL duplicado no próximo `add` e quebra o invariante migrations==modelo.
- **W9.8 logo após W9.0:** cria o harness WireMock/Testcontainers que **W9.1, W9.4, W9.6, W9.3** reusam
  para testar gateways sem credencial. Sem ele, as ondas de integração não têm como provar o ACL.
- **W9.1 antes de W9.3 e W9.6:** entrega o **calendário transversal** (`ICalendarioDiasUteis`+`PrazoLegal`
  no SharedKernel) que Obras (relógio §3, 25/45 d.u.) e Convênios (60/180/90+30/150/45) **consomem, não recriam**.
- **W9.7res (Inbox) antes de W9.4/W9.6:** o consumo idempotente de eventos de Finanças (Convênios) e o
  carimbo ACT (Protocolo) dependem do Inbox por `EventId`.
- **W9.5 cedo (independente):** alto valor de transparência, sem credencial, alvo disjunto — ótimo para
  rodar enquanto a equipe read-only detalha Obras/Convênios.

---

## 2) GAP-CHECK (completeness critic) — os designs conversam?

> Verificação cruzada das 5 perguntas. Veredito por item + lacuna/ajuste a aplicar antes de codar.

### (a) W9.3 Obras e W9.6 Convênios consomem o MESMO `ICalendarioDiasUteis` do design do calendário?

**Parcialmente — há um descasamento de ASSINATURA que precisa ser conciliado.** Os três designs
concordam na intenção ("W9.1 promove a serviço transversal; este módulo CONSOME, não recria") e na
porta `ICalendarioDiasUteis`. Mas:

- O **CALENDARIO-DIAS-UTEIS-DESIGN** (a autoridade da promoção) define a API ampliada:
  `AdicionarDiasUteis(inicio, n)`, `EhDiaUtil`, `ProximoDiaUtil`, `DiasUteisEntre` — **e renomeia** o
  método de `SomarDiasUteis` (assinatura legada do e-SIC) para `AdicionarDiasUteis`.
- O **CONVENIOS-MROSC-DESIGN** (§1, A.5/B.5) ainda cita a assinatura **legada** `SomarDiasUteis(DateOnly
  inicio, int diasUteis)` — a que existe hoje em `Transparencia.Domain/Esic`.
- O **OBRAS-DESIGN** (§3.3, I-14) cita só "`ICalendarioDiasUteis` … 25/45 d.u." sem fixar nome de método.

> **LACUNA G1 — alinhar nome do método.** Adotar **`AdicionarDiasUteis`** (do CALENDARIO-DESIGN, que é a
> autoridade da promoção) como nome canônico. Tratar `SomarDiasUteis` como assinatura legada a ser
> migrada. **Ação no W9.1:** ao promover, criar `AdicionarDiasUteis` e **não** manter `SomarDiasUteis`
> além de um shim temporário; atualizar os `using` do e-SIC. **Ação documental:** corrigir as menções a
> `SomarDiasUteis` em CONVENIOS-MROSC-DESIGN §1/A.5/B.5 para `AdicionarDiasUteis` (ou registrar que o VO
> `PrazoLegal` é o ponto de consumo — ver G2). Sem isso, W9.6 codaria contra um método que W9.1 renomeou.

> **AJUSTE G2 — preferir o VO `PrazoLegal` como ponto de consumo único.** O CALENDARIO-DESIGN entrega
> `PrazoLegal.Criar(inicio, n, unidade, normaFonte, calendario)` que encapsula d.u. **e** corridos +
> rolagem art.110/224 + `Vencido/AVencer/Prorrogar`. Convênios mistura d.u. e **dias corridos** (60/180
> são corridos na norma; 45/90+30/150 variam por tenant) — chamar `AdicionarDiasUteis` direto **não**
> cobre o caso corrido com rolagem. **Decisão:** Obras e Convênios consomem `PrazoLegal`, não o calendário
> cru, exceto onde só precisam de `AdicionarDiasUteis`/`ProximoDiaUtil` puro. Isso também resolve G1 (o VO
> esconde o nome do método). Anotar nos specs de W9.3c e W9.6-B4.

**Veredito (a): SIM na intenção, com G1+G2 a aplicar.** Mesma porta, mas o nome do método e o nível de
consumo (porta crua vs VO `PrazoLegal`) precisam ser fixados no W9.1 e refletidos nos specs de W9.3/W9.6.

### (b) Conflito de placement (Obras em Administração × Patrimônio) com o que já existe?

**Sem conflito — decisão resolvida e coerente com o código atual.** O OBRAS-DESIGN §1 decide
**`Patrimonio.Domain/Obras`** (obra = bem em formação; reusa o arquétipo `Veiculo`/`BemPatrimonial`;
incorporação interna). Isso bate com o M9-BREAKDOWN que lista a decisão como "pendente de produto" mas a
recomenda no mesmo lugar. Confirmado contra o código já entregue:

- `Patrimonio.Application/Integracoes/ReceberContratoAssinadoHandler` **já existe** e já consome
  `ContratoAssinadoIntegrationEvent` — exatamente o handler que OBRAS-DESIGN §3.1 reusa.
- `Veiculo` (composto com `BemPatrimonial`) já é o arquétipo "é-um bem + gestão operacional" que Obra espelha.
- Administração já **emite** `ContratoAssinado`/`AditivoCelerado` (typo preservado no código) e não conhece Patrimônio.

> **LACUNA B1 (menor) — confirmar a decisão de produto.** O M9-BREAKDOWN §94 ainda marca "submódulo de
> Obras (Administração × Patrimonio)" como **decisão de produto a confirmar antes de codar**. O OBRAS-DESIGN
> já a tomou (Patrimônio). **Ação:** registrar um ADR curto fixando "Obra em Patrimônio" antes de abrir
> W9.3a, fechando a pendência do BREAKDOWN. Não é bloqueio técnico — é higiene de governança.

> **LACUNA B2 (typo no nome do evento) — `AditivoCelerado` vs `AditivoCelebrado`.** OBRAS-DESIGN §3.1
> registra que o evento existente tem o typo `AditivoCeleradoIntegrationEvent` ("nome com typo preservado")
> mas o M9-BREAKDOWN §18 fala em `AditivoCelebrado`. **Ação:** o W9.3e DEVE consumir **o nome real que está
> no código** (`AditivoCelerado` se for esse o typo já mergeado) — não renomear na onda de Obras (mudaria
> Contracts já publicado). Verificar o nome exato em `Administracao.Contracts` no início do W9.3e.

**Veredito (b): SEM conflito de placement.** Patrimônio é o destino correto e o código já tem os ganchos.
Pendências B1 (ADR de governança) e B2 (usar o nome real do evento, typo incluso).

### (c) O calendário precisa existir ANTES de W9.1/W9.3/W9.6 — a ordem reflete isso?

**Sim, e a ordem está correta — com uma sutileza.** O calendário **é entregue DENTRO do W9.1** (não é uma
onda separada anterior). A sequência W9.1 → W9.3 → W9.6 garante que Obras e Convênios encontrem o
`ICalendarioDiasUteis` já promovido ao SharedKernel.

- **W9.3 (Obras)** depende explicitamente: OBRAS-DESIGN §4 diz "W9.3c depende de **W9.1**" e §3.3 prevê o
  fallback ("se W9.1 ainda não promoveu, abrir porta local com a MESMA assinatura e convergir depois").
- **W9.6 (Convênios)** depende explicitamente: CONVENIOS-DESIGN §3 lista como pré-req transversal
  "vem de W9.1: `ICalendarioDiasUteis` promovido + `IConveniosParametros`".
- **W9.1 (PNCP)** é o **primeiro consumidor E o promotor** — ele cria o serviço que consome. Não há ciclo:
  a promoção é a primeira tarefa do W9.1, antes da invariante de bloqueio.

> **AJUSTE C1 — ordem interna do W9.1: promover o calendário PRIMEIRO.** Dentro do W9.1, a sub-tarefa
> "promover `ICalendarioDiasUteis`+`PrazoLegal`+`FeriadosMoveis` ao SharedKernel/BuildingBlocks + migrar
> e-SIC" deve ser a **W9.1-A**, antes da ACL PNCP. Assim, mesmo que W9.1 estoure prazo, o ativo
> transversal (que W9.3/W9.6 bloqueiam) já está entregue. Anotar no spec do W9.1.

> **OBSERVAÇÃO C2 — o fallback do OBRAS-DESIGN não deve ser exercido.** Como a ordem oficial coloca W9.1
> inteiramente antes de W9.3, a "porta local em Patrimônio + convergir depois" do §3.3 é dívida técnica
> evitável. Regra: **só ativar o fallback se W9.1-A for adiado**; caso contrário, Obras importa direto do
> SharedKernel. Não criar a porta duplicada por precaução.

**Veredito (c): SIM, a ordem reflete a dependência.** Reforçar com C1 (promover calendário como primeira
sub-tarefa do W9.1) e C2 (não exercer o fallback de porta local).

### (d) Entidade/Contract duplicada entre designs?

**Riscos de duplicação identificados — nenhum é colisão de tipo, mas há conceitos a unificar:**

1. **`Repasse`** aparece como entidade filha **dentro de cada agregado** de Convênios (§A.2 em
   `ConvenioRecebido`, §B.2 em `ParceriaOsc`) — são tipos **distintos com semântica distinta** (entra vs
   sai). **Não é duplicação ilegal** (vivem em namespaces/agregados diferentes do mesmo módulo), mas
   **devem ter nomes ou namespaces que os distingam** para não confundir o EF mapping. **Ação W9.6-B2/B3/B5:**
   nomear `RepasseRecebido` (A) e `RepasseOsc`/`ParcelaRepasse` (B), ou mantê-los em
   `Convenios.Domain.Recebidos` vs `Convenios.Domain.Mrosc`. Anotar.

2. **`RendimentoAplicacaoFinanceira` / `RendimentoAplicacao`** e **`Vigencia`** — citados nos dois fluxos
   de Convênios (A.2 e B.2). `Vigencia` (VO) e `IndiceDevolucao` (VO Selic) **podem e devem ser
   compartilhados** dentro do módulo `Convenios` (são genéricos). **Ação:** colocá-los em
   `Convenios.Domain.Comum` (VOs compartilhados pelos dois agregados), não duplicar. `Rendimento` idem se
   a estrutura for idêntica.

3. **`PrazoLegal` / VOs de prazo** — o CALENDARIO-DESIGN entrega `PrazoLegal` genérico no SharedKernel.
   CONVENIOS-DESIGN define `PrazoConvenioRecebido`/`PrazoParceriaOsc` (A.5/B.5) e OBRAS implica
   parâmetros 25/45. **Risco de reimplementar lógica de prazo.** **Ação (reforça G2):** `PrazoConvenio*`/
   `PrazoParceriaOsc` devem ser **wrappers de parâmetro** (`Quantidade`/`Unidade`/`NormaFonte`) que
   ALIMENTAM `PrazoLegal.Criar(...)` — não reimplementar `Vencido/AVencer`. O cálculo mora só no
   `PrazoLegal` do SharedKernel.

4. **Eventos de Finanças consumidos** — `DespesaEmpenhadaIntegrationEvent`,
   `DespesaLiquidadaIntegrationEvent`, `PagamentoEfetuadoIntegrationEvent` são consumidos por **Obras**
   (W9.3e: medição→liquidação) **e** por **Convênios** (W9.6-B8: espelho empenho/liquidação/pagamento).
   São **os mesmos contratos de `Financas.Contracts`** — consumo legítimo, não duplicação. **Ação:** ambos
   reusam o padrão `ReceberContratoAssinadoHandler` + Inbox por `EventId`; confirmar que os Contracts já
   existem em `Financas.Contracts` antes de W9.3e/W9.6-B8 (se faltar `ReceitaConvenioReconhecida`, é stub).

5. **`ICalendarioDiasUteis`** — hoje existe em `Transparencia.Domain/Esic`. Após W9.1, passa a viver no
   SharedKernel. **Risco:** durante a transição, dois tipos com o mesmo nome (Transparência legado +
   SharedKernel novo). **Ação (do CALENDARIO-DESIGN §1):** remover/shimar o de Transparência no mesmo W9.1;
   não deixar os dois coexistirem além da onda.

> **LACUNA D1 — `IConveniosParametros` vs `IObrasParametros` vs `IPncpParametros`.** O CALENDARIO-DESIGN
> §3 cita as três portas de parâmetros por tenant (uma por fluxo). São **portas distintas por módulo** (ok,
> não duplicação), mas todas seguem o **mesmo shape** (`Quantidade`/`Unidade`/`NormaFonte`). **Ação:**
> considerar um VO base `ParametroPrazoTenant` no SharedKernel que as três portas retornam, evitando três
> records idênticos. Decisão não-bloqueante — registrar como melhoria de manutenibilidade.

**Veredito (d): sem colisão de tipo, mas 5 conceitos a unificar/distinguir** (Repasse A vs B; Vigencia/
Rendimento/IndiceDevolucao em Comum; PrazoLegal como único motor; eventos de Finanças confirmados; calendário
sem coexistência). Aplicar nas sub-ondas indicadas.

### (e) O que cada design difere a M10 está consistente?

**Sim — a fronteira M10 é consistente e bem traçada em todos os designs.** Conferido cruzado:

| Onda | M9 entrega (sem credencial) | M10 (credencial/decisão oficial) | Consistente? |
|---|---|---|---|
| **W9.1 PNCP** | ACL `IPncpGateway` + invariante bloqueio + relógio + correções legais, testado em WireMock | transmissão real ao `pncp.gov.br` (JWT) + validação `treina.pncp.gov.br` | ✅ BREAKDOWN §35/§88 |
| **W9.3 Obras** | agregado + medição/RDO + **gera artefato SICOE** (reusa `RemessaTce`) testado em WireMock | **transmissão real** SICOE ao TCE-RS (cert A1/endpoint) | ✅ OBRAS §3.4 = BREAKDOWN §41/§89 |
| **W9.4 Protocolo** | ACL TSP RFC 3161 testável (WireMock) + motor temporalidade CONARQ + fix NUP | **carimbo ACT real** (ACT contratada + custo/carimbo) + assinatura qualificada prod | ✅ BREAKDOWN §47/§90 |
| **W9.5 Legislativo** | URN + export LexML + motor 29-A (local) | transmissão prestação 29-A ao TCE-RS | ✅ BREAKDOWN §59/§91 |
| **W9.6 Convênios** | dois agregados + state machines + PC + ACL `ITransferegovGateway` (WireMock; leitura DTPAR) | PC real no **Transferegov.br** (órgãos integrados) | ✅ CONVENIOS A.7/B.7 = BREAKDOWN §53/§92 |
| **W9.7res** | Inbox + enrichers OTel + fan-out migrations | exportador OTLP → **Azure Monitor** (infra) | ✅ BREAKDOWN §68/§93 |

> **OBSERVAÇÃO E1 — assimetria M10 entre os dois fluxos de Convênios (intencional e correta).** Fluxo (A)
> Recebidos tem alta dependência M10 (Transferegov real); fluxo (B) MROSC é "praticamente todo M9" (só
> portal/assinatura tocam prod). CONVENIOS §B.7 e a tabela §2 explicitam isso — consistente, não é lacuna.

> **OBSERVAÇÃO E2 — W9.2/W9.9/W9.10 estão fora do recorte deste plano.** O BREAKDOWN §70 declara que DF-e
> (W9.2), SADIPEM/CDP (W9.9) e SIAFIC (W9.10) são sub-ondas oficiais de M9 mas **fora do recorte** (são
> Tributos/Finanças). Este plano executa exatamente as 7 do enunciado. **Ação:** rastrear W9.2/9.9/9.10
> como pendência separada — não estão cobertas aqui e não devem ser esquecidas no fechamento do M9.

**Veredito (e): CONSISTENTE.** Todos diferem a M10 a mesma coisa — **transmissão/assinatura oficial real
com credencial**, nunca o domínio/contrato/spec. E1/E2 são observações, não conflitos.

### Resumo das lacunas/ajustes achados (checklist pré-código)

| ID | Tipo | Lacuna/Ajuste | Onde aplicar |
|---|---|---|---|
| **G1** | Descasamento de assinatura | `SomarDiasUteis` (legado, citado em Convênios) → `AdicionarDiasUteis` (canônico). Corrigir menções. | W9.1-A + doc CONVENIOS §1/A.5/B.5 |
| **G2** | Nível de consumo | Obras/Convênios consomem o VO `PrazoLegal` (cobre corridos+rolagem), não o calendário cru. | W9.1 + specs W9.3c/W9.6-B4 |
| **B1** | Governança | ADR fixando "Obra em Patrimônio" (fecha pendência §94 do BREAKDOWN). | antes de W9.3a |
| **B2** | Nome real de Contract | Consumir o nome existente do evento de aditivo (`AditivoCelerado` se for o typo mergeado), não renomear. | início de W9.3e |
| **C1** | Ordem interna | Promover calendário como **W9.1-A** (antes da ACL PNCP). | W9.1 |
| **C2** | Dívida evitável | Não criar porta `ICalendarioDiasUteis` local em Patrimônio se W9.1-A já entregou. | W9.3c |
| **D1** | Manutenibilidade | VO base `ParametroPrazoTenant` no SharedKernel para as 3 portas `*Parametros`. | W9.1 (opcional) |
| **D2** | Naming | Distinguir `Repasse` (A) de `Repasse` (B); pôr `Vigencia`/`Rendimento`/`IndiceDevolucao` em `Convenios.Domain.Comum`. | W9.6-B2/B3/B5 |
| **E2** | Escopo/rastreio | W9.2/W9.9/W9.10 fora deste plano — rastrear como pendência de fechamento do M9. | governança |

---

## 3) PRIMEIRO PASSO CONCRETO — W9.8 (QA harness, a fundação reusada)

> O W9.8 é a fundação que **W9.1, W9.4, W9.6 e W9.3** reusam para testar gateways (PNCP, TSP/ACT,
> Transferegov, SICOE) sem credencial real. Antes dele, criar a fitness `HasPendingModelChanges`
> (W9.0) — ela vive no mesmo `tests/…ArchitectureTests` e é o oráculo do drift de snapshot. O "primeiro
> passo" do harness em si:

**Passo 0 (W9.0, pré-harness):** no `tests/Tensorroot.Gov.ArchitectureTests/` (já existe), criar
`PendingModelChangesFitness.cs` — `[Theory]` por `ModuleDbContext` que falha se
`Database.HasPendingModelChanges()` for `true` (provider Sqlite in-memory, sem abrir conexão, `TenantContextFake`
com `HasTenant=false`). Esboço completo em `P0-SNAPSHOTS-PLANO.md §3`. Esse teste deve **falhar para Saúde**
antes do fix (prova que pega o drift) e ficar verde após reconstruir o snapshot + 4 Designers.

**Passo 1 (W9.8 — criar o projeto de testes de integração):**

1. **Criar `tests/Tensorroot.Gov.IntegrationTests/`** (novo csproj `net8.0`, NRT, warnings-as-errors;
   referenciar `ApiHost` + os `*.Infrastructure` necessários; registrar no `.sln`). Pacotes (via Central
   Package Management em `Directory.Packages.props`):
   - `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<TEntryPoint>`).
   - `Testcontainers.MsSql` (SQL Server real efêmero — bate com o provider de PROD, ao contrário do Sqlite
     da fitness; valida migrations/WORM/hash-chain de verdade num banco relacional descartável).
   - `WireMock.Net` (stub HTTP dos gateways externos: PNCP, TSP/ACT, Transferegov, SICOE).
   - `xunit` + `FluentAssertions` (já no padrão do repo).
   - `Respawn` (opcional — reset de estado entre testes no container).

2. **`CustomWebApplicationFactory : WebApplicationFactory<Program>`** com:
   - **Mock auth** — esquema de teste que injeta um `ClaimsPrincipal` com `tenant_id`/roles via
     `AuthenticationHandler` fake (`TestAuthHandler`), para exercitar AuthZ policy-based sem JWT real e
     para **provar isolamento cross-tenant** (trocar o `tenant_id` do principal e assertar 404/403 + zero
     vazamento de dados).
   - **Override do connection string** apontando para o container Testcontainers (substitui o
     `appsettings` de PROD no `ConfigureWebHost`).
   - **Override dos `HttpClient` dos gateways** (PNCP/TSP/Transferegov) para a base URL do WireMock — é o
     ponto de extensão que W9.1/W9.4/W9.6 plugam ao adicionar cada ACL. Registrar uma `IWireMockServer`
     compartilhada na fixture.

3. **Fixture de coleção (`IClassFixture`/`ICollectionFixture`)** que sobe **um** container SQL +
   **um** WireMock por classe/coleção (custo de boot amortizado), roda `MigrateAsync` (exercita as
   migrations reais — sinergia com W9.0) e expõe `HttpClient` + `IWireMockServer` aos testes.

4. **Primeiro teste-âncora (prova que o harness funciona end-to-end):** o E2E do fluxo crítico do
   BREAKDOWN §63 — **licitação → contrato → PNCP → empenho** — com o WireMock respondendo o nº de controle
   PNCP, assertando que **sem nº PNCP o empenho é bloqueado** (a invariante central do W9.1) e que **com**
   nº PNCP o `EmpenhoEmitido` flui. Esse teste nasce *vermelho* (a invariante ainda não existe até W9.1) —
   serve de **contrato executável** que W9.1 fará passar. É a costura W9.8↔W9.1.

5. **Esqueleto de isolamento cross-tenant (os 11 módulos):** um teste parametrizado que, para cada módulo
   licenciado, cria dado no tenant A e tenta lê-lo autenticado como tenant B → espera **vazio/403/404** e
   nenhuma linha cross-tenant (valida o Global Query Filter sob HTTP real, não só unit). Reusa o padrão de
   `OutboxDispatchIsolationTests` já existente, agora sobre `WebApplicationFactory`.

6. **Acessibilidade (frontend, `npx` em paralelo):** estender as ~7 provas `.a11y.test.tsx` (axe-core/
   vitest-axe) para **todas** as rotas do `src/Web`, com gate de WCAG 2.1 AA / eMAG no CI. Roda isolado da
   `:5080`.

**Entregável do primeiro passo:** projeto `IntegrationTests` compilando, fixture SQL+WireMock+mock-auth de
pé, o teste-âncora PNCP em vermelho (contrato para W9.1) e o esqueleto de isolamento cross-tenant. A partir
daí cada onda de integração **adiciona** seus stubs WireMock e seus testes de ACL sobre a mesma fixture —
sem recriar infraestrutura.

---

## RESPOSTA (resumo executável)

**Sequência turnkey:** `W9.0(P0 snapshots+fitness) → W9.8(QA harness IntegrationTests/WireMock/Testcontainers)
→ W9.1(promove calendário+PrazoLegal ao SharedKernel, depois ACL PNCP+invariante bloqueio art.94) →
W9.7res(Inbox idempotente + OTel multi-tenant, em paralelo desde W9.1-meio) → W9.5(Legislativo LexML+29-A)
→ W9.4(Protocolo ACT RFC3161+CONARQ, depende do Inbox) → W9.3(Obras em Patrimônio+RDO+SICOE, depende do
calendário de W9.1) → W9.6(Convênios+MROSC, módulo novo, depende do calendário)`. Build serial, `npx`
frontend e read-only de alvos disjuntos em paralelo; creds reais = M10.

**Lacunas achadas (9, todas pré-código):** G1 (renomear `SomarDiasUteis`→`AdicionarDiasUteis`),
G2 (consumir o VO `PrazoLegal`, não o calendário cru — cobre dias corridos+rolagem de Convênios),
B1 (ADR fixando "Obra em Patrimônio"), B2 (usar o nome real do evento `AditivoCelerado`/typo),
C1 (promover calendário como primeira sub-tarefa W9.1-A), C2 (não criar porta local em Patrimônio),
D1 (VO base `ParametroPrazoTenant` opcional), D2 (distinguir `Repasse` A vs B + VOs comuns em
`Convenios.Domain.Comum`), E2 (W9.2/W9.9/W9.10 fora do recorte — rastrear). Placement de Obras e
fronteira M10 estão **consistentes** entre os designs.

**Primeiro passo W9.8:** criar `tests/Tensorroot.Gov.IntegrationTests/` (MsSql Testcontainers + WireMock.Net
+ `WebApplicationFactory<Program>` + `TestAuthHandler` com `tenant_id`), uma fixture de coleção que sobe
1 SQL + 1 WireMock e roda `MigrateAsync`, e o **teste-âncora vermelho** licitação→contrato→PNCP→empenho
(prova "sem nº PNCP não empenha" — contrato executável que W9.1 fará passar) + esqueleto de isolamento
cross-tenant nos 11 módulos. Antes dele, a fitness `HasPendingModelChanges` (W9.0) no
`ArchitectureTests` existente. Cada onda de integração reusa essa fixture sem recriar infra.
