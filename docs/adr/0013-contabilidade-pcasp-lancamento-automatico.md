# ADR-0013 — Contabilidade PCASP com lançamento de partida dobrada AUTOMÁTICO via domain events

- **Status:** Aceito
- **Data:** 2026-06-22
- **Fonte:** `docs/architecture/contabilidade-pcasp-tce.md` + `Financas/rules/Contabilidade.rules.md`

## Contexto

A meta nº1 do produto é **prestação de contas correta** (TCE-RS / SICONFI). O princípio é que a
**contabilidade é a FONTE**: sem lançamento contábil correto, a MSC e a remessa ao TCE nascem ocas.
O ciclo da despesa (Dotação → Empenho → Liquidação → Pagamento → Restos) e a receita acontecem nos
módulos de negócio (Finanças, Tributos). A questão é **como** esses fatos viram lançamentos PCASP.

Lançar contabilidade **à mão** após cada fato é inviável em volume, propenso a erro e impossível de
auditar (quem garante que todo empenho gerou o lançamento certo?). A partida dobrada
(Σdébitos = Σcréditos) e a homogeneidade de natureza (MCASP §3) são invariantes que não podem
depender de digitação.

## Decisão

**Lançamento contábil automático, dirigido por domain events**, na mesma unidade de trabalho:

- Cada fato do ciclo emite um **Domain Event** (`EmpenhoEmitido`, `DespesaLiquidada`,
  `PagamentoEfetuado`, `ReceitaArrecadada`, …).
- Um handler resolve o **`EventoContabil` vigente** (roteiro fato→partidas, **parametrizável por
  tenant**, vigência por exercício — nunca hardcoded) e gera o `LancamentoContabil` correspondente
  na **MESMA** unidade de trabalho/Outbox do fato (ADR-0010/0011).
- Invariantes provados no domínio: **Σdébitos = Σcréditos**; homogeneidade de natureza; lançamento
  só em conta **analítica e ativa**; período aberto; **imutável** após registro (estorno por
  lançamento inverso, preservando o original — auditoria).
- O `Balancete` (read model) deriva dos lançamentos e é a base da **MSC**, publicada como
  `MSCGeradaIntegrationEvent` para a Transparência (SICONFI + insumo da remessa TCE-RS).
- Códigos/contas exatos do PCASP e roteiros oficiais ficam marcados `// TODO(validar-leiaute-oficial)`
  até a fonte oficial (STN/MCASP) — modelamos pela **natureza** das contas, sem inventar (CONVENCOES-ENGENHARIA.md §8).

## Alternativas consideradas

- **Lançamento manual/contador digita:** não escala, não audita, viola a integridade exigida pelo
  TCE. Rejeitado para o fluxo automático (entrada manual fica só para ajustes excepcionais auditados).
- **Lançar de forma síncrona acoplando Finanças ao motor contábil por chamada direta:** quebraria
  o isolamento de módulo. O acoplamento é por **evento** (in-process domain event no mesmo módulo
  Finanças; integration event só para cruzar para Transparência).
- **Calcular a MSC direto dos fatos, sem livro contábil:** perderia a partida dobrada e a
  rastreabilidade exigida pelo TCE; a MSC seria não-auditável. Rejeitado — a contabilidade é a fonte.

## Consequências

- ➕ Contabilidade **consistente por construção** (partida dobrada garantida no domínio), rastreável
  do fato ao balancete à MSC à remessa — exatamente o que o TCE audita.
- ➕ Roteiros parametrizáveis por tenant/exercício acomodam mudanças normativas sem recompilar.
- ➕ Mesma unidade de trabalho fato↔lançamento↔Outbox → sem divergência entre operação e contabilidade.
- ➖ **Dependência forte do `EventoContabil` correto:** um roteiro mal parametrizado contamina toda a
  cadeia. Mitigado por `*.rules.md` com BDD dos invariantes e validação contra spec oficial.
- ➖ **Bloqueado por fonte oficial:** Plano de Contas PCASP vigente, tabela de eventos contábeis,
  leiaute MSC — sem eles, congelamos só o conceito, não os códigos.
- 🔗 Abrange o ciclo da despesa, o PCASP com lançamento automático, o Balancete e a MSC, e a remessa
  ao TCE (leiaute, e-Validador, A1, empacotamento — ver ADR-0009).
