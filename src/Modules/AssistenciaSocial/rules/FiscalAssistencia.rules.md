# FiscalAssistencia — Regras (Rules-as-Code) — M7 ASSISTÊNCIA (A-1 FMAS + A-2 RMA)

> Camada fiscal/integração do SUAS, espelhando o padrão fiscal da Saúde (FMS). Cobre o **Fundo
> Municipal de Assistência Social (FMAS)** por bloco/piso de cofinanciamento (Port. MDS 1.043/2024) e o
> **Registro Mensal de Atendimentos (RMA)** consolidado do Prontuário SUAS (Res. CIT 4/2011 e 20/2013).
> Marco legal: LOAS (Lei 8.742/1993, c/ Lei 12.435/2011); NOB-SUAS/2012; Tipificação Nacional (Res. CNAS
> 109/2009); Portaria MDS 1.043/2024. Sem mínimo % constitucional — vínculo por bloco/piso (CLAUDE.md §16:
> valores/critérios/prazos são parâmetros versionados por tenant+vigência, nunca hardcoded).

## A-1 — Fundo Municipal de Assistência Social (FMAS)
- **I-1.** O FMAS é unidade gestora com execução **segregada por bloco** (PSB, PSE-Média Complexidade,
  PSE-Alta Complexidade, Gestão/IGD — Port. 1.043/2024) e **por piso** (Tipificação Nacional).
- **I-2.** Cada conta amarra **(bloco, piso, fonte de recurso PCASP)** — uma única conta por combinação.
- **I-3.** Recebimento de parcela do FNAS credita a conta do bloco/piso/fonte (valor > 0).
- **I-4.** Execução de despesa debita **apenas** a conta correspondente; **transposição livre entre
  blocos/pisos é vedada** (não se executa além do saldo da própria conta).
- **I-5.** Saldos por bloco e por piso são derivados (recebido − executado); painel de execução por piso.

## A-2 — Registro Mensal de Atendimentos (RMA)
- **I-6.** O RMA é único por **(tenant, unidade, competência)**.
- **I-7.** A consolidação é **derivada do Prontuário SUAS/atendimentos** (sem dupla digitação): conta os
  atendimentos por serviço (PAIF/PAEFI/SCFV) no mês da competência, na unidade.
- **I-8.** Reconsolidar com a mesma fonte é **idempotente** (substitui as linhas, não soma) — reprodutível.
- **I-9.** O fechamento **sela** a competência (terminal); RMA fechado não admite reconsolidação nem novo
  fechamento, e emite o evento de fechamento para envio ao MDS (RMA/SAGI).
- **I-10.** Tudo isolado por tenant (Global Query Filter); volumes agregados — nunca dado sigiloso (LGPD art. 11).

## Cenários (BDD)
**Cenário: Execução segregada por bloco/piso do FMAS**
- **Dado** parcelas do FNAS recebidas em blocos distintos
- **Quando** uma despesa é executada num bloco/piso
- **Então** o saldo é debitado apenas na conta do bloco/piso/fonte e a transposição livre é vedada.

**Cenário: Consolidação do RMA a partir do Prontuário**
- **Dado** atendimentos PAIF registrados nos prontuários de uma unidade na competência
- **Quando** o RMA é consolidado e fechado
- **Então** as quantidades por serviço derivam do prontuário, `RmaFechado` é publicado e a competência fica selada.

<!-- manifest
commands: AbrirFundoMunicipalAssistencia, ReceberParcelaFnas, ExecutarDespesaSuas, ConsolidarRma, FecharRma
queries: ObterExecucaoFmas, ObterRma
domainEvents: RmaFechado
integrationEventsPublished: RmaFechadoIntegrationEvent
integrationEventsConsumed: 
-->
