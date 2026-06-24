# Limite de Despesa da Camara — art. 29-A CF/88 — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). W9.5 (item 2): apurar o limite de despesa TOTAL do Poder
> Legislativo municipal (% por faixa populacional sobre a receita do exercicio anterior) + subteto da
> folha <= 70% do repasse (art. 29-A §1); realizado x limite com semaforo; demonstrativo.
> Autoridade: `docs/architecture/m9-prep/M9-BREAKDOWN.md` (W9.5).
> Fonte legal: CF/88 art. 29-A (EC 25/2000, EC 58/2009), EC 109/2021 art. 7º.
> Constituicao: multi-tenant, dominio rico, auditoria imutavel, **sem numero magico** (tudo parametrizavel).

---

## 1. Linguagem ubiqua

- **Base de receita:** receita tributaria + transferencias efetivamente REALIZADAS no **exercicio anterior**
  (caput). Obtida de Financas (Contracts) OU informada (entrada auditada — Camara e tenant distinto).
- **Faixa populacional:** intervalo de habitantes -> percentual-limite (7/6/5/4,5/4/3,5% — parametrizavel).
- **Teto da despesa total:** base x percentual da faixa (caput).
- **Repasse/duodecimo:** valor efetivamente repassado pelo Executivo a Camara no exercicio.
- **Subteto da folha (§1):** folha da Camara <= 70% do repasse (percentual parametrizavel).
- **Semaforo:** Adequado (verde) / Atencao (amarelo, a partir do limiar) / Excedido (vermelho).
- **Demonstrativo:** consolidacao realizado x limite (teto e subteto), com semaforos e parcelas.

---

## 2. Invariantes do agregado `ApuracaoArt29A`

- **A29-1** Uma apuracao por `(TenantId, Exercicio)` (indice unico + verificacao no handler).
- **A29-2** A base de receita e do exercicio ANTERIOR (`BaseReceita.ExercicioReferencia == Exercicio - 1`).
- **A29-3** Populacao, exercicio e percentuais positivos; base (receita + transferencias) positiva; repasse >= 0.
- **A29-4** O percentual da faixa resolve pela populacao (primeira faixa que comporta; ultima faixa aberta
  superiormente). Borda de 100.000 e INCLUSIVE na primeira faixa.
- **A29-5** Teto da despesa total = base x percentual; subteto da folha = repasse x percentual §1.
- **A29-6** Regra temporal EC 109/2021 (art. 7º): inativos/pensionistas so integram o teto a partir do
  `ExercicioCorteInativos` (default 2025). Antes disso sao excluidos do teto e do subteto de folha.
- **A29-7** Despesa total sob teto = soma das parcelas (exceto inativos/pensionistas antes do corte);
  folha = parcelas de pessoal (ativo + inativos elegiveis).
- **A29-8** Semaforo: Excedido se realizado > limite; Atencao se utilizacao >= limiar (parametrizavel);
  senao Adequado. Estouro de qualquer limite => `Irregular` (risco art. 29-A §2/§3).
- **A29-9** Lancamento de despesa so em Rascunho. `Consolidar` (Rascunho -> Consolidada, terminal) congela
  e emite `ApuracaoArt29AConsolidada`.
- **A29-10** Todos os percentuais/faixas/subteto/limiar/corte vem de `Legislativo:LimiteCamara:*` (norma-fonte
  por tenant); nenhum hardcoded no dominio (CLAUDE.md §7).

---

## 3. RBAC

- `legislativo.ver` — consultar o demonstrativo.
- `legislativo.limite-camara.gerenciar` — abrir/consolidar a apuracao.

---

## 4. Cenarios BDD (resumo)

- **Faixa por populacao:** dado municipio de 8.000 hab e faixa ate 100.000 = 7%, quando apurar, entao o
  percentual e 7% e o teto = base x 0,07.
- **Subteto de folha §1:** dado repasse R e §1 = 70%, quando a folha > 0,70R, entao semaforo da folha = Excedido.
- **EC 109 — inativos:** dada despesa de inativos e exercicio < 2025, quando apurar, entao inativos NAO entram
  no teto; com exercicio >= 2025, entram.
- **Estouro do teto:** dada despesa total > teto, entao semaforo do teto = Excedido e `Irregular` = true.
- **Unicidade por exercicio:** dada apuracao do exercicio ja aberta, quando abrir de novo, entao falha.
- **Base do exercicio anterior:** dado exercicio 2026 com base de 2024, quando abrir, entao falha (deve ser 2025).

---

## 5. Fronteira M10

- // TODO(M10): transmissao da prestacao 29-A ao TCE-RS (cert/endpoint). No M9: apurar + demonstrativo local.
- // TODO(M10): leitura cross-tenant da receita do Executivo a partir da Camara (federacao de dados entre os
  tenants do mesmo municipio). No M9: base informada (auditada) ou de Financas do proprio tenant via Contracts.

<!-- manifest
commands: AbrirApuracaoArt29A, ConsolidarApuracaoArt29A
queries: ObterDemonstrativoArt29A
domainEvents: ApuracaoArt29AAberta, ApuracaoArt29AConsolidada
contractsConsumed: IConsultaReceitaParaLimiteLegislativo (Financas)
-->
