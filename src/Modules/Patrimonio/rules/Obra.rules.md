---
modulo: Patrimonio
agregado: Obra
contexto: Patrimonio (Obras — bem patrimonial em formacao; cronograma fisico-financeiro, medicao/RDO, fiscalizacao)
poder: Ambos
schema: patrimonio
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 14.133/2021 (NLLC: art. 46 regimes, art. 94 §3 divulgacao, art. 117 fiscalizacao, art. 125 aditivos)", "Lei 4.320/1964 art. 63 (liquidacao - verificacao do direito do credor)", "MCASP/STN (Obras em Andamento / Imobilizado em Curso)", "NBC TSP 07 (Ativo Imobilizado)"]
---

<!-- manifest
commands: AbrirObra, DefinirCronograma, EmitirOrdemInicio, DesignarFiscal, RegistrarOcorrencia, RegistrarRdo, RegistrarMedicao, AprovarMedicao, RejeitarMedicao, ParalisarObra, ReiniciarObra, ConcluirObra, RescindirObra, VarrerPrazosArt94
queries: ObterObra, BuscarObras
domainEvents: ObraAberta, MedicaoAprovada, ObraConcluida, ObraIncorporada
integrationEventsPublished: MedicaoAprovadaIntegrationEvent, ObraConcluidaIntegrationEvent, ObraPrazoArt94VencendoIntegrationEvent
integrationEventsConsumed: AditivoCeleradoIntegrationEvent
-->


# Obra — Regras-as-Code (Rules-as-Code)

> Obra publica / servico de engenharia (Lei 14.133/2021). **E-um bem patrimonial em formacao**
> (imobilizado em curso — MCASP) que, ao concluir, e **incorporado** ao acervo como `BemPatrimonial`.
> Nasce de um **contrato NLLC** (vinculo por `ContratoId` — Guid, cross-context com Administracao via
> Integration Events) e controla a **execucao fisica**: cronograma fisico-financeiro (curva S),
> medicao/RDO e fiscalizacao (fiscal designado, ocorrencias, paralisacao/reinicio). Raiz de agregado
> (`AggregateRoot<ObraId>`), espelhando o arquetipo do `Veiculo` (composicao com `BemPatrimonialId`
> populado so na incorporacao). Este arquivo e **normativo**; o codigo e consequencia dele.
>
> **Placement (DDD):** o agregado mora em `Patrimonio.Domain/Obras` — o ciclo de vida da obra e
> patrimonial (meses/anos de execucao + depreciacao apos conclusao), nao contratual. Administracao e
> dona do `Contrato`; a obra **referencia** o contrato apenas por ID + eventos.

---

## 1. Linguagem Ubiqua

| Termo (identificador) | Definicao |
|---|---|
| Obra (`Obra`) | Bem patrimonial em formacao; raiz de agregado da execucao fisica de uma obra/servico de engenharia. |
| Contrato de origem (`ContratoId` : Guid) | Vinculo NLLC por identidade (Administracao); chega por `ContratoAssinadoIntegrationEvent`. |
| Bem incorporado (`BemPatrimonialId?` : referencia) | `BemPatrimonial` criado na conclusao; nulo ate la (I-13). |
| Regime de execucao (`RegimeExecucao` : enum) | Art. 46: preco unitario/global, tarefa, integral, contratacao integrada/semi-integrada. |
| Cronograma (`EtapaCronograma`) | Curva S do edital: etapas x peso fisico previsto x valor previsto x datas. |
| Medicao (`Medicao`) | Boletim periodico; aprovada libera a liquidacao (Lei 4.320 art. 63). Linhas em `ItemMedicao` (avanco por etapa). |
| RDO (`RegistroDiarioObra`) | Relatorio Diario de Obra; lastro de fiscalizacao continua das medicoes. |
| Fiscal designado (`FiscalDesignadoId`) | Servidor que fiscaliza (art. 117); aprova medicoes. |
| Paralisacao (`EventoParalisacao`) | Suspensao da execucao (motivo + data); reinicio retoma a execucao. |
| Valor contratado (`ValorContratado`) | Teto da medicao acumulada; reajustado por aditivo (art. 125). |

---

## 2. Maquina de estados

`Planejada` → `EmExecucao` → (`Paralisada` ⇄ `EmExecucao`) → `Concluida` → `Incorporada`; tambem `Rescindida` (terminal antes da conclusao).

- `AbrirObra` → **Planejada**.
- `DefinirCronograma` (I-3) + `DesignarFiscal` (I-10) habilitam `EmitirOrdemInicio` → **EmExecucao**.
- `RegistrarRdo`/`RegistrarMedicao`/`AprovarMedicao` exigem **EmExecucao** (I-11).
- `Paralisar`/`Reiniciar` alternam **Paralisada** ⇄ **EmExecucao** (I-15).
- `Concluir` (I-12, 100% fisico) → **Concluida**; `Incorporar` (I-13) → **Incorporada**.

---

## 3. Invariantes (protegidas no agregado, numeradas)

- **I-1** Teto da medicao: `ValorMedidoAcumulado` (medicoes aprovadas) ≤ `ValorContratado`. Exceder exige aditivo.
- **I-2** Aditivo: `ValorContratado` so muda por `AplicarAditivoValor` (evento `AditivoCelerado`); limite legal (25%/50%) validado em Administracao. Novo teto nao pode cair abaixo do ja medido.
- **I-3** Coerencia do cronograma: Σ peso fisico das etapas = 100% e Σ valor previsto = `ValorContratado` (tolerancia de arredondamento parametrizavel).
- **I-4** Percentual fisico ∈ [0,100], nao decrescente entre medicoes aprovadas.
- **I-5** Coerencia por etapa: `ValorMedido` da etapa ≤ `ValorPrevisto`; fisico executado da etapa ≤ 100%.
- **I-6** % fisico **derivado** (Σ executado_etapa × peso_etapa / 100), nunca digitado.
- **I-7** Medicao numerada (monotonica, sem buracos) e periodos disjuntos (I-7b).
- **I-8** Medicao so aprova com RDOs cobrindo todos os dias do periodo medido.
- **I-9** RDO unico por dia/obra.
- **I-10** Aprovacao so pelo fiscal designado vigente (art. 117).
- **I-11** RDO/medicao exigem `EmExecucao`; bloqueadas em Planejada/Paralisada/Concluida/Rescindida.
- **I-12** Conclusao exige 100% fisico (tolerancia); emite `ObraConcluida`.
- **I-13** Incorporacao unica e idempotente (`BemPatrimonialId` atribuido uma vez; reincorporar e no-op).
- **I-14** Relogio art. 94 §3 via `ICalendarioDiasUteis` (W9.1): **25 d.u. apos assinatura** / **45 d.u. apos conclusao**; quantidade/unidade/feriados **parametrizaveis por tenant** (`IParametrosObraProvider`), nunca hardcoded.
- **I-15** Paralisar nao apaga medicoes aprovadas nem reduz o acumulado; so interrompe novas medicoes/RDOs.
- **I-16** Multi-tenant/auditoria: filhas herdam `TenantId`; toda mutacao gera trilha imutavel.

---

## 4. Eventos de integracao (Contracts)

- **Entrada (de Administracao):** `ContratoAssinado` (registra elegibilidade; abertura deliberada via `AbrirObraCommand`); `AditivoCelerado` → `AplicarAditivoValor` (I-2).
- **Saida (Patrimonio.Contracts):**
  - `MedicaoAprovadaIntegrationEvent` → **Financas** (liquidacao, Lei 4.320 art. 63) — fecha medicao → liquidacao → pagamento.
  - `ObraConcluidaIntegrationEvent` → Portal/Transparencia; incorporacao do bem in-process emite `BemIncorporadoIntegrationEvent` → **Financas** (MCASP).
  - `ObraPrazoArt94VencendoIntegrationEvent` → **Portal do Gestor** (alerta de prazo §3).
- **SICOE (remessa de obras ao TCE-RS) = M10** (// TODO): no M9 gera-se o artefato reusando `Transparencia/RemessaTce`; a transmissao real (cert A1/Key Vault) e do M10, atras de ACL.

---

## 5. Cenarios BDD

### CT-1 — Abertura a partir de contrato
```
Dado um contrato NLLC assinado (ContratoId, Fornecedor, Valor)
Quando a obra e aberta com objeto, localizacao, regime e valor contratado
Entao a obra nasce Planejada, com valor medido zero e evento ObraAberta
```

### CT-2 — Cronograma coerente (I-3)
```
Dada uma obra planejada com valor contratado de R$ 1.000.000
Quando se define o cronograma cuja soma de pesos != 100% ou soma de valores != contratado
Entao a definicao e rejeitada (coerencia fisico-financeira)
```

### CT-3 — Medicao com lastro de RDO (I-8/I-9) e aprovacao por fiscal (I-10)
```
Dada uma obra em execucao com fiscal designado
Quando se aprova uma medicao sem RDO cobrindo o periodo OU por quem nao e o fiscal vigente
Entao a aprovacao e rejeitada
E havendo RDO no periodo e o fiscal designado, a aprovacao compoe o ValorMedidoAcumulado,
   deriva o percentual fisico (I-6) e emite MedicaoAprovada -> Financas
```

### CT-4 — Teto da medicao (I-1) e aditivo (I-2)
```
Dada uma obra com valor contratado de R$ 1.000.000 ja medido em R$ 1.000.000
Quando se tenta aprovar nova medicao sem aditivo
Entao a aprovacao e rejeitada (excede o contratado)
E apos AplicarAditivoValor (novo teto), a medicao adicional e aceita
```

### CT-5 — Conclusao -> incorporacao (I-12/I-13)
```
Dada uma obra com 100% fisico executado
Quando se conclui a obra
Entao transita para Concluida, emite ObraConcluida
E ao incorporar, cria o BemPatrimonial, transita para Incorporada e fixa BemPatrimonialId (uma unica vez)
```

### CT-6 — Relogio art. 94 §3 (I-14)
```
Dada uma obra assinada em 05/01/2026 e o parametro 25 d.u. do tenant
Quando se calcula o prazo de assinatura via calendario de dias uteis
Entao o vencimento e 25 dias uteis apos a assinatura (sem numero magico no agregado)
```
