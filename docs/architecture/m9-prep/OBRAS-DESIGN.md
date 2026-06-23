# W9.3 (Obras) — DESIGN do agregado `Obra` (M9, agregado NOVO)

> Arquiteto de domínio — obras públicas / engenharia de custos. **READ-ONLY** (sem build/run).
> Autoridade base: `docs/architecture/m9-prep/M9-BREAKDOWN.md` (W9.3-Obras, esforço **G**).
> Disciplina CLAUDE.md: domínio rico (sem anêmico), multi-tenant por padrão, cross-module
> **exclusivamente** via `*.Contracts` (Integration Events + Outbox + ACL), auditoria imutável,
> prazos legais **parametrizáveis por tenant** (nunca hardcoded). Data: 2026-06-23.

---

## 0) TL;DR (decisão + entregáveis)

- **PLACEMENT:** o agregado `Obra` nasce em **`Patrimonio.Domain/Obras`** (mesmo Bounded Context
  da Frota/Bens). A obra **é um bem patrimonial em formação** (imobilizado em curso → vira ativo ao
  concluir). Ela **referencia** o contrato NLLC do outro lado **só por `ContratoId` (Guid)** —
  recebido por Integration Event de Administração (`ContratoAssinadoIntegrationEvent`), nunca por
  referência a entidade. Justificativa completa em §1.
- **AGREGADO `Obra`:** raiz rica com **Cronograma físico-financeiro** (Etapas × valores/percentuais),
  **Medição/RDO** (boletins de medição periódica + Relatório Diário de Obra), **Fiscalização**
  (fiscal designado, ocorrências, paralisação/reinício). Composição espelhando `Veiculo`
  (`BemPatrimonialId` opcional, populado só na incorporação). Invariantes I-1…I-16 em §2.
- **EVENTOS (Contracts):** `MedicaoAprovadaIntegrationEvent` → Finanças (liquidação);
  `ObraConcluidaIntegrationEvent` → Patrimônio incorpora o bem (variação patrimonial aumentativa);
  `ObraPrazoArt94VencendoIntegrationEvent` → Portal do Gestor. SICOE (remessa obras TCE-RS) atrás de
  ACL = **M10**. Detalhe em §3.
- **SEQUÊNCIA:** 6 sub-workflows (W9.3a…W9.3f), backend edit-only + verify; frontend npx. §4.

---

## 1) DECISÃO DE PLACEMENT — `Patrimonio` (com vínculo a `Administracao` via Contracts)

### A pergunta
A obra **nasce de um contrato NLLC** (Administração: licitação → contrato de obra/serviço de
engenharia, Lei 14.133/2021) **e vira bem patrimonial ao concluir** (Patrimônio: imobilizado).
Onde fica o agregado raiz?

### Decisão: **`Patrimonio.Domain/Obras`** (recomendado)

Justificativa (engenharia de domínio + reuso + CLAUDE.md):

1. **O ciclo de vida da obra é patrimonial, não contratual.** O contrato é o *instrumento de origem*
   (um evento de nascimento), mas o que tem ciclo de vida longo — meses/anos de medição, RDO,
   fiscalização, depreciação após conclusão — é o **ativo em formação**. Contabilmente é **Obras em
   Andamento / Imobilizado em Curso (MCASP)**, que ao concluir reclassifica para Imobilizado. A obra
   **continua existindo depois que o contrato encerra** (manutenção, depreciação, baixa). Colocá-la
   em Administração amarraria um ativo de vida longa ao ciclo curto de um contrato.

2. **Reuso direto do padrão `Veiculo`.** `Veiculo` já é "é-um bem patrimonial + gestão operacional"
   (composição com `BemPatrimonial`/`BemPatrimonialId`, situação no ciclo patrimonial, depreciação
   MCASP/NBC TSP 07, Outbox/eventos de Patrimônio). `Obra` é o **mesmo arquétipo**: é-um bem
   patrimonial (em curso) + gestão de execução (cronograma/medição/fiscalização). Reusa
   `SituacaoBemPatrimonial`, `ValorMonetario`, `HistoricoDepreciacao`, o pipeline de incorporação e
   os Domain/Integration Events de Patrimônio. Manda-a para Patrimônio **paga o reuso**; mandá-la
   para Administração duplicaria conceitos patrimoniais lá.

3. **A incorporação do bem é local (sem cross-module).** Ao concluir, a obra **vira** um
   `BemPatrimonial` no MESMO contexto — incorporação **in-process** (Domain Event), não Integration
   Event de ida-e-volta. Se a obra morasse em Administração, a incorporação seria cross-module
   Administração→Patrimônio: mais acoplamento, mais latência, mais um ACL. Mantê-la em Patrimônio
   torna o caminho mais quente (conclusão→imobilizado) puramente interno.

4. **Administração permanece o dono do contrato** (já está). Administração já tem `Contrato`/`Aditivo`
   /`Garantia` ricos (Lei 14.133) e já **emite** `ContratoAssinadoIntegrationEvent`. Não há ganho em
   mover a obra para perto do contrato — o vínculo é **fraco e por ID**, exatamente o que Integration
   Events resolvem. Administração continua responsável pela eficácia (PNCP/W9.1), aditivos de valor,
   sanções; Patrimônio é responsável pela **execução física** (medição/RDO/fiscalização) e pelo ativo.

5. **CLAUDE.md §2/§5.** Cross-module só por `*.Contracts`. O vínculo `Obra ↔ Contrato` é
   precisamente um **acoplamento por identificador + evento**, que é a fronteira correta entre dois
   Bounded Contexts. Colocar a obra em Patrimônio deixa a regra "a medição libera pagamento" como um
   evento Patrimônio→Finanças (já existe esse padrão: Patrimônio→Finanças para depreciação/ingresso),
   e mantém a fiscalização (RDO/fiscal designado) longe do contexto de compras.

### Como cada lado referencia o outro (sem violar isolamento)

```
 Administracao (contexto contratual)            Patrimonio (contexto do ativo)
 ──────────────────────────────────             ──────────────────────────────
 Contrato (NLLC)  ── ContratoAssinado ─────►  Obra.AbrirObra(ContratoId, ...)   [vínculo por Guid]
 Aditivo (valor)  ── AditivoCelebrado ─────►  Obra.AplicarAditivoValor(...)     [reajusta teto contratado]
                                              Obra (raiz) ── BemPatrimonialId? (preenchido na conclusão)
 Financas  ◄── MedicaoAprovada ────────────  Obra.AprovarMedicao(...)          [libera liquidação]
 Patrimonio (interno) ◄── ObraConcluida ───  Obra.Concluir(...)                [incorpora BemPatrimonial]
```

- **Obra → Contrato:** apenas `ContratoId : Guid` (FK lógica entre contextos). Validação de
  existência/valor do contrato chega **pelo evento** (`Valor`, `FornecedorId`), não por consulta
  síncrona a Administração. A obra guarda seu **próprio** `ValorContratado` (snapshot do evento,
  ajustado por aditivos via evento).
- **Contrato → Obra:** Administração **não conhece** `Obra`. Se precisar do estado de execução
  (para PNCP/relatório), consome eventos de Patrimônio (`MedicaoAprovada`/`ObraConcluida`) — nunca
  referencia a entidade.
- **Consumo do `ContratoAssinadoIntegrationEvent`:** reusa o padrão já existente em
  `Patrimonio.Application/Integracoes/ReceberContratoAssinadoHandler` (handler fino que **não**
  cria a obra automaticamente — registra a elegibilidade; a obra é aberta por `AbrirObraCommand`
  com os dados de engenharia completos, igual ao par "ContratoAssinado → IncorporarBemCommand").

> **Rejeitado (Administração):** colocaria a execução física + o ativo + a depreciação dentro do
> contexto de compras; quebraria o reuso do `Veiculo`; transformaria a incorporação em cross-module.
> A obra-como-contrato confunde *instrumento* com *coisa*.

---

## 2) AGREGADO `Obra` (rico) — entidades-chave e invariantes

Localização: `Patrimonio.Domain/Obras`. Raiz: `Obra : AggregateRoot<ObraId>, IMustHaveTenant`
(`readonly record struct ObraId(Guid Value)`, espelhando `VeiculoId`). Construtor privado +
factory `AbrirObra`; entidade nasce válida (CLAUDE.md §7).

### 2.1 Identificação & vínculos (raiz)

| Campo | Tipo | Observação |
|---|---|---|
| `Id` | `ObraId` | identidade forte |
| `TenantId` | `Guid` | `IMustHaveTenant`; Global Query Filter |
| `ContratoId` | `Guid` | **vínculo NLLC por ID** (cross-context); origem via `ContratoAssinado` |
| `FornecedorId` | `Guid` | snapshot do evento (contratada/executora) |
| `BemPatrimonialId` | `BemPatrimonialId?` | **nulo até a conclusão** — populado na incorporação (igual ao `Veiculo` quando passa por incorporação) |
| `Objeto` | `string` | descrição da obra/serviço de engenharia |
| `Localizacao` | `LocalizacaoObra` (VO) | logradouro + município/UF + lat/long opcional + `GeoCodigo`/SICOE |
| `RegimeExecucao` | `RegimeExecucao` (enum) | `EmpreitadaPorPrecoUnitario`, `EmpreitadaPorPrecoGlobal`, `Tarefa`, `EmpreitadaIntegral`, `ContratacaoIntegrada`, `ContratacaoSemiIntegrada` (Lei 14.133 art. 46) |
| `ValorContratado` | `ValorMonetario` | snapshot do evento; teto da medição acumulada (I-1); reajustado por `AditivoCelebrado` (I-2) |
| `DataAssinaturaContrato` | `DateOnly` | base do relógio art. 94 §3 (25 d.u.) |
| `DataInicioOrdemServico` | `DateOnly?` | emissão da Ordem de Início de Serviço |
| `DataConclusao` | `DateOnly?` | base do relógio art. 94 §3 (45 d.u.) |
| `Situacao` | `SituacaoObra` (enum) | `Planejada` → `EmExecucao` → `Paralisada` ⇄ `EmExecucao` → `Concluida` → `Incorporada`; também `Rescindida` |
| `PercentualFisicoAcumulado` | `decimal` (0–100) | derivado das etapas concluídas/medidas (I-5/I-6) |
| `ValorMedidoAcumulado` | `ValorMonetario` | soma das medições aprovadas (I-1) |

### 2.2 CRONOGRAMA físico-financeiro — `EtapaCronograma` (entidade filha)

Planilha de etapas/marcos × percentuais físicos × valores (a "curva S" do edital).

| Campo | Tipo |
|---|---|
| `Id` (`EtapaCronogramaId`), `Ordem` (`int`) | sequência |
| `Descricao` | `string` (ex.: "Fundação", "Estrutura", "Acabamento") |
| `PercentualFisicoPrevisto` | `decimal` (peso da etapa no total) |
| `ValorPrevisto` | `ValorMonetario` |
| `DataPrevistaInicio` / `DataPrevistaFim` | `DateOnly` |
| `PercentualFisicoExecutado` | `decimal` (atualizado pelas medições) |
| `ValorMedido` | `ValorMonetario` (acumulado por etapa) |
| `Situacao` | `Prevista`/`EmAndamento`/`Concluida` |

Coleção privada `List<EtapaCronograma> _etapas` exposta como `IReadOnlyCollection` (padrão `Veiculo`).

### 2.3 MEDIÇÃO / boletim — `Medicao` + `RegistroDiarioObra` (RDO)

- **`Medicao`** (boletim de medição periódica → libera pagamento):
  `Id` (`MedicaoId`), `Numero` (sequencial monotônico por obra), `Competencia` (mês/ano),
  `PeriodoInicio`/`PeriodoFim` (`DateOnly`), `ValorMedido` (`ValorMonetario`),
  `PercentualFisicoNoPeriodo` (`decimal`), `Situacao` (`Rascunho`/`Aprovada`/`Rejeitada`),
  `FiscalAprovadorId` (`Guid?`), `DataAprovacao` (`DateOnly?`). A **medição aprovada** é o gatilho
  da liquidação em Finanças (Lei 4.320 art. 63 — verificação do direito do credor).
- **`RegistroDiarioObra` (RDO)** — Relatório Diário de Obra (item de fiscalização contínua):
  `Id`, `Data` (`DateOnly`, único por dia/obra — I-9), `Clima`/`CondicaoTempo`, `EfetivoMaoDeObra`
  (`int`), `EquipamentosMobilizados` (`string`), `AtividadesExecutadas` (`string`),
  `Ocorrencias` (`string?`), `ResponsavelTecnicoId` (`Guid`). Os RDOs do período **embasam** a
  medição (I-8): só medição com RDOs cobrindo o período pode ser aprovada.

### 2.4 FISCALIZAÇÃO — fiscal designado + ocorrências + paralisação

- `FiscalDesignadoId` (`Guid?` na raiz) + histórico `List<DesignacaoFiscal>` (quem, desde quando,
  ato de designação) — Lei 14.133 art. 117 (fiscal/gestor do contrato).
- `OcorrenciaFiscalizacao` (entidade filha): `Data`, `Tipo` (`Notificacao`/`Advertencia`/
  `RegistroTecnico`), `Descricao`, `RegistradaPorId`.
- **Paralisação/reinício:** `Paralisar(motivo, data)` → `Situacao = Paralisada`, registra
  `EventoParalisacao` (`MotivoParalisacao` enum: `OrdemFiscalizacao`, `FaltaProjeto`, `Clima`,
  `Orcamentaria`, `Judicial`); `Reiniciar(data)` → volta a `EmExecucao`. Período de paralisação
  **suspende a contagem** do relógio art. 94 §3 quando aplicável (parametrizável por tenant).

### 2.5 INVARIANTES (numeradas — protegidas no agregado, NUNCA no handler)

> Falhas de invariante → exceção de domínio (CLAUDE.md §7). Prazos/percentuais que dependem de
> norma são **parâmetros por tenant** (CLAUDE.md §7/§16), passados ao agregado (porta de domínio),
> não constantes.

- **I-1 — Teto da medição acumulada.** `ValorMedidoAcumulado` (soma das medições **aprovadas**)
  **≤ `ValorContratado`**. Aprovar medição que ultrapasse o contratado → rejeição (exige aditivo).
- **I-2 — Aditivo respeita o instrumento.** `ValorContratado` só muda por `AplicarAditivoValor`
  (disparado por `AditivoCelebradoIntegrationEvent`); o aditivo de valor de obra observa o limite
  legal (25% / 50% reforma — art. 125) **validado em Administração** (não reduplicar a regra aqui;
  Patrimônio só aplica o novo teto recebido).
- **I-3 — Coerência físico-financeira do cronograma.** Σ `PercentualFisicoPrevisto` das etapas = 100
  (tolerância de arredondamento parametrizável); Σ `ValorPrevisto` das etapas = `ValorContratado`.
- **I-4 — Percentual físico monotônico e limitado.** `PercentualFisicoAcumulado` ∈ [0,100], não
  decrescente entre medições aprovadas (não há "des-execução" sem ocorrência/retrabalho explícito).
- **I-5 — Coerência física × financeira por etapa.** `ValorMedido` de uma etapa ≤ `ValorPrevisto`
  da etapa; medição não pode reportar 100% físico de uma etapa com 0% financeiro medido sem
  justificativa (configurável: regime por preço unitário vs global).
- **I-6 — % físico derivado, não digitado.** `PercentualFisicoAcumulado` é **calculado** a partir
  das etapas/medições aprovadas (ponderado pelo peso da etapa), nunca atribuído diretamente.
- **I-7 — Medição numerada e sequencial.** `Medicao.Numero` é monotônico crescente por obra
  (sem buracos), e os períodos de medição não se sobrepõem (I-7b: `PeriodoInicio`/`Fim` disjuntos).
- **I-8 — Medição exige RDO.** Uma `Medicao` só é aprovada se houver RDOs cobrindo o período
  medido (lastro de fiscalização contínua).
- **I-9 — RDO único por dia.** Não há dois `RegistroDiarioObra` para a mesma `Data` na mesma obra.
- **I-10 — Aprovação por fiscal designado.** Só `AprovarMedicao` quando `FiscalDesignadoId` está
  preenchido e o aprovador é o fiscal vigente (art. 117).
- **I-11 — Transições de estado válidas.** `AprovarMedicao`/`RegistrarRdo` exigem
  `Situacao ∈ {EmExecucao}`; bloqueadas em `Paralisada`/`Concluida`/`Rescindida`/`Planejada`
  (espelha `GarantirAtivoNoAcervo` do `Veiculo`).
- **I-12 — Conclusão exige execução completa.** `Concluir` só com `PercentualFisicoAcumulado` = 100
  (tolerância) e `ValorMedidoAcumulado` consistente; emite `ObraConcluida` (Domain Event interno).
- **I-13 — Incorporação única.** `BemPatrimonialId` é atribuído **uma só vez**, na transição
  `Concluida → Incorporada`; reincorporar é no-op (idempotência, espelha `Depreciar` do `Veiculo`).
- **I-14 — Relógio art. 94 §3 (gancho W9.1).** O prazo de publicação/registro vinculado à obra é
  **calculado** via `ICalendarioDiasUteis` (porta de domínio promovida a serviço transversal — ver
  §3): **25 dias úteis após `DataAssinaturaContrato`** e **45 dias úteis após `DataConclusao`**
  (Lei 14.133 art. 94 §3). Os números (25/45) e os feriados são **parâmetros por tenant**. Períodos
  de paralisação suspendem a contagem quando o tenant assim parametrizar.
- **I-15 — Paralisação não retroage medição.** Paralisar não apaga medições aprovadas nem reduz o
  `ValorMedidoAcumulado`; só interrompe novas medições/RDOs.
- **I-16 — Multi-tenant / auditoria.** Todas as filhas herdam `TenantId` da raiz (gravação
  cross-tenant lança exceção via interceptor); toda mutação gera trilha imutável (CLAUDE.md §5/§6).

### 2.6 Comportamentos (factory + métodos ricos)

`AbrirObra(tenantId, contratoId, fornecedorId, objeto, localizacao, regime, valorContratado,
dataAssinaturaContrato)` · `DefinirCronograma(etapas)` (valida I-3) · `EmitirOrdemInicio(data)` ·
`RegistrarRdo(...)` (I-9/I-11) · `RegistrarMedicao(...)` (cria em `Rascunho`) ·
`AprovarMedicao(medicaoId, fiscalId, calendario, …)` (I-1/I-5/I-7/I-8/I-10/I-11 → `RaiseDomainEvent`)
· `RejeitarMedicao(...)` · `DesignarFiscal(...)` (art. 117) · `RegistrarOcorrencia(...)` ·
`Paralisar(...)`/`Reiniciar(...)` (I-15) · `AplicarAditivoValor(novoTeto)` (I-2) · `Concluir(data,
calendario)` (I-12 → `ObraConcluida`) · `Incorporar(bemPatrimonialId)` (I-13).

---

## 3) Eventos de integração (Contracts) — wiring cross-module

> Todos `sealed record … : IntegrationEvent(EventId, OccurredOnUtc)`, publicados via **Outbox**
> (transacional com o estado), **idempotentes por `EventId`** no consumidor (Inbox — W9.7).

### 3.1 Entrada (Patrimônio consome de Administração)

- **`ContratoAssinadoIntegrationEvent`** (já existe em `Administracao.Contracts`): handler fino
  reusando `ReceberContratoAssinadoHandler` — registra elegibilidade da obra; a obra é aberta por
  `AbrirObraCommand` (dados de engenharia completos). Para distinguir obra de aquisição comum,
  parametrizar por categoria/objeto do contrato (a abertura é deliberada, não automática).
- **`AditivoCeleradoIntegrationEvent`** (já existe; nome com typo preservado): handler aplica
  `Obra.AplicarAditivoValor` (I-2) quando o aditivo é de valor e referencia o contrato da obra.

### 3.2 Saída (Patrimônio publica em `Patrimonio.Contracts` — NOVOS)

- **`MedicaoAprovadaIntegrationEvent`** → **Finanças** (liquidação, Lei 4.320 art. 63).
  Campos: `EventId, OccurredOnUtc, TenantId, ObraId, ContratoId, MedicaoId, NumeroMedicao,
  ValorMedido, Competencia, FornecedorId`. Finanças reusa o padrão de consumo de eventos
  (`Financas.Application/Integracoes`) para gerar a **liquidação** atrelada ao empenho do contrato
  (correlaciona por `ContratoId`/`EmpenhoRef`). Fecha o laço **medição → liquidação → pagamento**
  (complementa `DespesaLiquidadaIntegrationEvent`/`PagamentoEfetuadoIntegrationEvent`).
- **`ObraConcluidaIntegrationEvent`** → **Patrimônio (interno) + consumidores**. A incorporação do
  bem é **in-process** (Domain Event `ObraConcluida` → cria `BemPatrimonial.Incorporar` →
  `Obra.Incorporar(bemId)`), emitindo o já-existente `BemIncorporadoIntegrationEvent` para Finanças
  (variação patrimonial aumentativa, MCASP). O `ObraConcluidaIntegrationEvent` público notifica
  Portal do Gestor/Transparência. Campos: `EventId, OccurredOnUtc, TenantId, ObraId, ContratoId,
  BemPatrimonialId, ValorFinal, DataConclusao`.
- **`ObraPrazoArt94VencendoIntegrationEvent`** → **Portal do Gestor** (alerta de prazo §3,
  espelha `PrazoPncpAVencer` de W9.1). Campos: `…, ObraId, TipoPrazo (Assinatura25|Conclusao45),
  DataLimite, DiasUteisRestantes`.

### 3.3 Relógio / calendário (gancho W9.1)

- **Reuso obrigatório:** `ICalendarioDiasUteis` (hoje em `Transparencia.Domain/Esic`). W9.1 a
  **promove a serviço transversal compartilhado** (provável `SharedKernel` ou
  `BuildingBlocks.Application`). `Obra` consome essa **mesma** porta para I-14 — **não duplicar**.
  Se W9.1 ainda não a promoveu quando W9.3 entrar, abrir uma porta `ICalendarioDiasUteis` em
  Patrimônio com a **mesma assinatura** e convergir depois (anotar dívida).
- O cálculo dos prazos §3 fica **dentro da invariante do agregado** (porta passada nos métodos),
  com 25/45 d.u. e feriados como parâmetro por tenant.

### 3.4 SICOE (remessa de obras ao TCE-RS) — **M10, atrás de ACL**

- No **M9**: gerar o **artefato/leiaute SICOE** (reusando o pipeline `Transparencia/RemessaTce` —
  `RemessaTce` + motor de leiaute `EmissorRegistroSiapc`/`MontadorArquivo`), com mapeador
  `Obra → registros SICOE`, **testado contra WireMock** (W9.8). A geração é local; o agregado
  Obra é a **fonte** dos dados.
- No **M10**: **transmissão real** ao TCE-RS (cert A1/Key Vault, endpoint SICOE) — atrás de ACL +
  Polly + Outbox, igual às demais remessas. Fora do recorte do M9 (fronteira de credencial).

---

## 4) SEQUÊNCIA de implementação — sub-workflows (W9.3a … W9.3f)

> Backend: **edit-only** + verify (sem rodar `dotnet`/derrubar :5080 nesta fase de design).
> Frontend: **npx** (Vite/React, gov.br DS). BDD-first: spec `Given/When/Then` antes do código
> (CLAUDE.md §1/§12). Cada WF: entidades → invariantes → migration → config/repo → DI/endpoints →
> testes verdes → rules.md.

1. **W9.3a — Núcleo do agregado `Obra` + Cronograma (Domain).** `Obra`, `ObraId`, `EtapaCronograma`,
   VOs `LocalizacaoObra`/`RegimeExecucao`/`SituacaoObra`; factory `AbrirObra` + `DefinirCronograma`;
   invariantes **I-1…I-7, I-11, I-16**. Reuso: arquétipo `Veiculo` (composição, `SituacaoBemPatrimonial`,
   `ValorMonetario`). Domain Events em `Patrimonio.Domain/Events`. `rules/Obra.rules.md`.
   *Verify:* testes de invariante (xUnit/FluentAssertions), sem I/O.

2. **W9.3b — Medição + RDO + Fiscalização (Domain + Application).** `Medicao`, `RegistroDiarioObra`,
   `OcorrenciaFiscalizacao`, `DesignacaoFiscal`; métodos `RegistrarRdo`/`RegistrarMedicao`/
   `AprovarMedicao`/`DesignarFiscal`/`Paralisar`/`Reiniciar`; invariantes **I-8, I-9, I-10, I-15**.
   Handlers Application: `AbrirObra`, `DefinirCronograma`, `RegistrarRdo`, `RegistrarMedicao`,
   `AprovarMedicao`, `DesignarFiscal`, `Paralisar`/`Reiniciar`, + queries (`ObterObra`,
   `ListarObras`, `ObterCronograma`, `ListarMedicoes`, `ListarRdo`). *Verify:* testes de handler.

3. **W9.3c — Relógio art. 94 §3 (I-14) + alerta de prazo.** Consumir `ICalendarioDiasUteis`
   transversal (W9.1); `Obra` calcula deadlines 25/45 d.u. (parâmetro por tenant); job/handler que
   emite `ObraPrazoArt94VencendoIntegrationEvent`. **Depende de W9.1** (calendário promovido).
   *Verify:* testes determinísticos do cálculo de prazo (sem relógio real).

4. **W9.3d — Infra/persistência (Infrastructure).** `ObraConfiguration` (Fluent API, owned types
   p/ VOs, coleções como tabelas filhas, schema `patrimonio`), repositório `IObraRepository`,
   **migration** `ObrasFisicoFinanceiroMedicaoRdo`, Outbox no contexto de Patrimônio (já existe),
   registro no `PatrimonioModule` (DI) + endpoints minimal API. *Verify:* build do módulo
   (edit-only verify), Global Query Filter por tenant aplicado.

5. **W9.3e — Integração cross-module (Contracts).** Publicar `MedicaoAprovadaIntegrationEvent`,
   `ObraConcluidaIntegrationEvent`, `ObraPrazoArt94VencendoIntegrationEvent` em
   `Patrimonio.Contracts`; consumir `ContratoAssinado`/`AditivoCelerado` (handlers finos, reuso de
   `ReceberContratoAssinadoHandler`); wiring da incorporação interna `ObraConcluida →
   BemPatrimonial.Incorporar → BemIncorporadoIntegrationEvent`; consumidor em **Finanças**
   (medição → liquidação). + **mapeador SICOE** (gera artefato, reusa `RemessaTce`; transmissão = M10).
   *Verify:* testes de integração com **WireMock** (harness de W9.8); Inbox idempotente (W9.7).

6. **W9.3f — Frontend (React/Vite, gov.br DS — npx).** Telas: lista/busca de Obras (Onda 0 de
   navegabilidade), ficha da obra (cronograma "curva S", medições, RDOs, fiscalização/ocorrências,
   prazos §3), abrir obra, registrar RDO, registrar/aprovar medição, paralisar/reiniciar. Gating
   `<Can>` por permissão; acessibilidade WCAG 2.1 AA / eMAG + provas `.a11y.test.tsx` (W9.8);
   modais > 600 linhas quebrados. *Verify:* `npx vitest` + axe-core nas rotas novas.

**Ordem de dependência:** W9.3a → W9.3b → (W9.3c depende de **W9.1**) → W9.3d → W9.3e → W9.3f.
No PLANO-MESTRE, W9.3-Obras vem **após W9.1** (consome o relógio §3) e **após W9.8** (consome o
harness WireMock que valida medição→liquidação e o leiaute SICOE).

---

## 5) Resumo executivo

- **Placement:** `Obra` mora em **`Patrimonio.Domain/Obras`** (é-um bem em formação; reusa o
  arquétipo `Veiculo`/`BemPatrimonial`; incorporação interna). Vínculo ao **Contrato NLLC só por
  `ContratoId` (Guid) + Integration Events** (`ContratoAssinado`/`AditivoCelerado` de Administração);
  Administração nunca conhece `Obra`.
- **Entidades-chave:** raiz `Obra` (identificação, objeto, `ContratoId`, `BemPatrimonialId?`,
  localização, regime de execução, situação) + `EtapaCronograma` (físico-financeiro) + `Medicao` +
  `RegistroDiarioObra` (RDO) + `DesignacaoFiscal`/`OcorrenciaFiscalizacao` + paralisação.
- **Invariantes:** I-1 (medição acumulada ≤ contratado), I-3/I-5/I-6 (coerência físico-financeira),
  I-7/I-8/I-9 (medição numerada com lastro de RDO), I-10/I-11 (fiscal designado + transições),
  I-12/I-13 (conclusão→incorporação única), **I-14 (relógio art. 94 §3 via `ICalendarioDiasUteis`,
  25/45 d.u. parametrizáveis)**, I-15/I-16 (paralisação + multi-tenant/auditoria).
- **Eventos:** `MedicaoAprovada` → Finanças (liquidação); `ObraConcluida` → incorporação do bem
  (Patrimônio) + Finanças (MCASP); `ObraPrazoArt94Vencendo` → Portal do Gestor. **SICOE = M10**
  (gera artefato no M9 reusando `RemessaTce`; transmite no M10 atrás de ACL+A1).
- **Sequência:** W9.3a (núcleo+cronograma) → W9.3b (medição/RDO/fiscalização) → W9.3c (relógio §3,
  **depende de W9.1**) → W9.3d (infra/migration) → W9.3e (Contracts/integração + mapeador SICOE) →
  W9.3f (frontend gov.br DS). Vem após **W9.1** e **W9.8** no roadmap M9.
