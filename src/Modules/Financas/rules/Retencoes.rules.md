# Retenções, Consignações e Guias de Recolhimento — Rules-as-Code (Finanças)

> Retenção na fonte (consignação extra-orçamentária) apurada na liquidação/pagamento e o subsequente
> **recolhimento** do valor retido ao ente competente (DARF/GPS/guia municipal). Base legal: Lei 4.320/1964
> (ingressos e dispêndios **extra-orçamentários** — art. 3º, par. único e art. 11); MCASP (passivo
> "valores a recolher" / consignações); **IN RFB 1.234/2012** (IRRF sobre pagamentos a pessoa jurídica,
> tabela de serviços e dispensa por DARF mínimo — art. 3º, §6º).

<!-- manifest
commands: AdicionarRetencao, EmitirGuiaRecolhimento, RecolherGuia, CancelarGuia, SemearTabelaIrrfServicos
queries: ConsultarTabelaIrrf, ListarGuiasRecolhimento
domainEvents: RetencaoApurada, GuiaRecolhimentoEmitida, RecolhimentoEfetuado
integrationEventsPublished:
integrationEventsConsumed:
-->

---

## 1. Linguagem ubíqua

| Termo | Definição |
|---|---|
| **Retenção / Consignação** | Parcela retida na fonte de um pagamento (IRRF, INSS, ISS, etc.). Nasce na liquidação e baixa o líquido a pagar ao credor; gera passivo extra-orçamentário "a recolher". |
| **Natureza da retenção** (`NaturezaRetencao`) | Tributo/encargo retido: IRRF, INSS, ISS, e demais. |
| **Tabela IRRF de serviços** (`TabelaIrrfServicos`) | Tabela do IRRF sobre pagamentos a PJ (IN RFB 1.234/2012), **distinta** da tabela progressiva da folha. Parametrizável por tenant, com vigência e faixas. |
| **Guia de Recolhimento** (`GuiaRecolhimento`) | Documento que reúne retenções de mesma natureza/código de receita e formaliza o recolhimento (dispêndio extra-orçamentário que baixa o passivo). |
| **Recolhimento** | Pagamento efetivo da guia ao ente competente; baixa o passivo "a recolher". |

---

## 2. Invariantes (Rules-as-Code)

- **I-1 — Consignação não é despesa orçamentária.** A retenção e seu recolhimento transitam como
  **ingresso/dispêndio extra-orçamentário** (Lei 4.320/1964), nunca reduzem a dotação.
- **I-2 — Retenção ≤ valor do pagamento.** O somatório das retenções de uma liquidação não excede o
  valor liquidado; o líquido ao credor = bruto − retenções.
- **I-3 — IRRF/PJ com dispensa por DARF mínimo.** Se o IRRF apurado for inferior ao mínimo da tabela
  vigente (IN RFB 1.234/2012, art. 3º, §6º), retém **zero** (parametrizável, não hardcoded).
- **I-4 — Guia só recolhe o que reuniu.** Guia sem itens não pode ser recolhida; o valor total da guia =
  soma dos itens. Recolher e Cancelar exigem situação **Emitida** (transições terminais distintas).
- **I-5 — Recolhimento é idempotente na contabilização.** A baixa contábil da consignação é derivada
  por origem determinística (ordem + natureza), evitando lançamento duplicado.

---

## 3. Contabilização

- Na **liquidação/pagamento**, a retenção registra o passivo extra-orçamentário "valores a recolher".
- No **recolhimento** (guia recolhida), baixa-se o passivo contra a saída financeira — lançamento de
  partida dobrada gerado pelo handler de contabilização, ancorado no Plano de Contas (PCASP).

---

## 4. Cenários BDD (resumo)

1. **IRRF retido na liquidação** → o pagamento gera a consignação e um **lançamento extra-orçamentário**
   de "a recolher"; o líquido ao credor é o bruto menos o IRRF.
2. **DARF mínimo** → IRRF apurado abaixo do mínimo da tabela vigente ⇒ retenção zero (I-3).
3. **Emissão e recolhimento da guia** → reúne retenções da natureza, recolhe e baixa o passivo (I-4).
4. **Cancelamento** → guia emitida sem recolher pode ser cancelada, reabrindo as retenções vinculadas.

> Cobertura em `tests/Tensorroot.Gov.Modules.Financas.Tests/`.
