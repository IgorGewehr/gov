# DCASP — Demonstrações Contábeis Aplicadas ao Setor Público

> **Spec oficial** — insumo Rules-as-Code das Demonstrações DCASP + MSC.
> **Princípio (CONVENCOES-ENGENHARIA.md §8):** layouts/quadros/contas oficiais NÃO são inventados. Onde o
> documento-fonte estava em PDF comprimido (FlateDecode) e não pôde ser lido linha-a-linha,
> a estrutura confirmada por fonte é descrita e os detalhes faltantes ficam marcados
> `[obter arquivo oficial]` com nome/URL na seção *Pendências de fonte oficial*.
> **Piloto:** Maximiliano de Almeida/RS — jurisdicionado TCE-RS (SIAPC/PAD).

---

## 1. Base normativa vigente

| Documento | Edição/Norma | Vigência | Observação |
|---|---|---|---|
| **MCASP — Parte V (DCASP)** | **11ª edição** | exercício **2025** em diante | Aprovada pela **Portaria STN/MF nº 2.016, de 18/12/2024** (Partes Geral, II, III, IV e V). PCO (Parte I) pela Port. Conjunta STN/SOF nº 26/2024. |
| **Lei nº 4.320/1964** | arts. 101 a 105; Anexos 12 a 15 | vigente | Define as demonstrações obrigatórias e os Anexos 12/13/14/15. |
| **Portaria STN nº 438/2012** | altera Anexos 12–15 da Lei 4.320 | vigente | Atualizou estrutura/quadros dos Anexos 12 a 15 para o padrão PCASP. |
| **NBC TSP 11 / 12 / 13** | CFC | vigente | Base conceitual: TSP 11 (apresentação das DC), TSP 12 (DFC), TSP 13 (informação orçamentária nas DC). |
| **TCE-RS — Resolução nº 1.134/2020** | substitui Res. 1.099/2018 e 1.052/2015 | vigente | Prazos/documentos/informações da prestação de contas municipal (contas de governo e de gestão) em formato eletrônico. |
| **TCE-RS — IN nº 4/2021** | critérios RVE/RREO/RGF via PAD | vigente | Relatórios gerados de forma eletrônica e automática pelo PAD a partir do SIAPC. |

**Mapa de classes PCASP** (origem dos valores nas DCASP):
`1` Ativo · `2` Passivo + Patrimônio Líquido · `3` Variações Patrimoniais Diminutivas (VPD) ·
`4` Variações Patrimoniais Aumentativas (VPA) · `5` controle orçamentário (previsão/dotação/fixação) ·
`6` controle orçamentário (execução: empenho/liquidação/pagamento/realização) · `7`/`8` controles (atos potenciais, restos a pagar, etc.).

---

## 2. As demonstrações obrigatórias (Parte V + Lei 4.320)

| # | Demonstração | Anexo Lei 4.320 | Enfoque | Origem (classes PCASP) |
|---|---|---|---|---|
| 1 | **Balanço Orçamentário (BO)** | Anexo 12 | Orçamentário | 5 e 6 |
| 2 | **Balanço Financeiro (BF)** | Anexo 13 | Financeiro | 6 (execução), 1/2 (disponibilidades), 7/8 |
| 3 | **Balanço Patrimonial (BP)** | Anexo 14 | Patrimonial | 1, 2, 7/8 |
| 4 | **Demonstração das Variações Patrimoniais (DVP)** | Anexo 15 | Patrimonial | 3 e 4 |
| 5 | **Demonstração dos Fluxos de Caixa (DFC)** | — (MCASP/NBC TSP 12) | Financeiro | 1 (caixa e equiv.), 6, 3/4 |
| 6 | **Demonstração das Mutações do Patrimônio Líquido (DMPL)** | — (MCASP) | Patrimonial | 2 (PL), 3/4 |
| 7 | **Notas Explicativas** | — | todos | qualitativo |
| 8 | **Informações comparativas** (exercício anterior) | — | todos | — |

> DMPL é **obrigatória apenas para entes que recebem aporte de recursos para formação de capital de empresas estatais dependentes** (e facultativa para os demais); para o piloto municipal típico, foco em BO, BF, BP, DVP, DFC + Notas.

---

## 3. Balanço Orçamentário (BO) — Anexo 12

**Objetivo:** confrontar receita prevista × realizada e despesa fixada (dotação) × executada; apurar resultado orçamentário (superávit/déficit) e o cumprimento das metas da LOA.

**Composição (3 quadros):**
- **a) Quadro Principal** — Receita e Despesa orçamentárias.
- **b) Quadro de Execução de Restos a Pagar Não Processados.**
- **c) Quadro de Execução de Restos a Pagar Processados.**

### 3.1 Quadro Principal — RECEITAS
Colunas: **Previsão Inicial · Previsão Atualizada · Receitas Realizadas · Saldo** (Realizada − Atualizada).

| Linha (estrutura) | Origem PCASP |
|---|---|
| Receitas Correntes / de Capital (por categoria econômica, origem) | Previsão: classe **5** (5.2.1 previsão da receita / atualizações). Realizada: classe **6** (6.2.1.x realização da receita). |
| Recursos Arrecadados em Exercícios Anteriores (RPPS, quando aplicável) | classe 6 |
| **Subtotal das Receitas** | — |
| Operações de Crédito / Refinanciamento (se houver) | classe 5/6 |
| **Déficit** (quando despesa executada > receita realizada) | linha de fechamento |
| **TOTAL** | — |

### 3.2 Quadro Principal — DESPESAS
Colunas: **Dotação Inicial · Dotação Atualizada · Despesas Empenhadas · Despesas Liquidadas · Despesas Pagas · Saldo da Dotação** (Atualizada − Empenhada).

| Linha (estrutura) | Origem PCASP |
|---|---|
| Despesas Correntes / de Capital (categoria econômica, GND) | Dotação: classe **5** (5.2.2 fixação/crédito disponível). Empenhada/Liquidada/Paga: classe **6** (6.2.2.x — empenhada, liquidada/em liquidação, paga). |
| Reserva de Contingência | classe 5 |
| **Subtotal das Despesas** | — |
| Amortização da Dívida / Refinanciamento (se houver) | 5/6 |
| **Superávit** (quando receita realizada > despesa executada) | linha de fechamento |
| **TOTAL** | — |

**Resultado orçamentário:** Receitas Realizadas − Despesas Empenhadas (o MCASP usa o empenhado como execução da despesa para o confronto). Superávit/déficit aparece como linha de equilíbrio no lado oposto do quadro.

### 3.3 Quadro de Execução de Restos a Pagar (NP e P)
Para **RP Não Processados**: Inscritos em Exercícios Anteriores · Inscritos no Exercício Anterior · Liquidados · Pagos · Cancelados · Saldo.
Para **RP Processados** (e Não Processados Liquidados): Inscritos · Pagos · Cancelados · Saldo.
Origem: controles de **classe 6 / 7-8** (RP — `6.3.x` execução de RP; controles de `7/8`). **[obter arquivo oficial]** para a relação exata de colunas e contas.

---

## 4. Balanço Financeiro (BF) — Anexo 13

**Objetivo:** evidenciar a movimentação financeira (receitas/despesas orçamentárias + ingressos/dispêndios extraorçamentários) e os saldos em espécie de abertura e encerramento.

**Composição:** **quadro único** com colunas **Ingressos** e **Dispêndios** (e colunas de exercício atual e anterior — informação comparativa).

| Bloco | Lado | Detalhamento | Origem PCASP |
|---|---|---|---|
| **Receita Orçamentária** por destinação de recurso | Ingressos | Recursos Não Vinculados · Vinculados (exceto RPPS) · Vinculados RPPS | classe **6** (realização da receita) |
| **Despesa Orçamentária** por destinação de recurso | Dispêndios | mesma segregação por destinação | classe **6** (execução da despesa) |
| **Transferências Financeiras Recebidas** | Ingressos | repasses/sub-repasses, devolução | classe **4** (VPA — interna) |
| **Transferências Financeiras Concedidas** | Dispêndios | repasses/sub-repasses, devolução | classe **3** (VPD — interna) |
| **Recebimentos Extraorçamentários** | Ingressos | depósitos restituíveis, valores vinculados, RP inscritos | classes **1/2** + **7/8** |
| **Pagamentos Extraorçamentários** | Dispêndios | mesma natureza | classes **1/2** + **7/8** |
| **Saldo em Espécie do Exercício Anterior** | Ingressos | Caixa e Equivalentes (exceto RPPS) + Caixa RPPS | classe **1** |
| **Saldo em Espécie para o Exercício Seguinte** | Dispêndios | idem | classe **1** |

> Invariante de fechamento: **Total de Ingressos = Total de Dispêndios** (incluindo saldos de abertura/encerramento).

---

## 5. Balanço Patrimonial (BP) — Anexo 14

**Objetivo:** evidenciar qualitativa e quantitativamente Ativo, Passivo e Patrimônio Líquido em data determinada.

**Composição (4 quadros):**
- **a) Quadro Principal** — Ativo, Passivo e Patrimônio Líquido.
- **b) Quadro dos Ativos e Passivos Financeiros e Permanentes** (segregação financeiro/permanente da Lei 4.320, art. 105).
- **c) Quadro das Contas de Compensação** — atos potenciais ativos e passivos.
- **d) Quadro do Superávit/Déficit Financeiro** — apuração por fonte/destinação de recurso.

### 5.1 Quadro Principal
| Lado Ativo | Origem | Lado Passivo + PL | Origem |
|---|---|---|---|
| **Ativo Circulante** (caixa/equiv., créditos, estoques, VPD pagas antec.) | classe **1.1** | **Passivo Circulante** (obrigações curto prazo, fornecedores, RP, provisões) | classe **2.1** |
| **Ativo Não Circulante** (realizável LP, investimentos, imobilizado, intangível) | classe **1.2** | **Passivo Não Circulante** (obrigações LP, provisões LP) | classe **2.2** |
| | | **Patrimônio Líquido** (patrimônio social/capital, resultados acumulados, ajustes) | classe **2.3** |
| **TOTAL DO ATIVO** | | **TOTAL DO PASSIVO + PL** | |

### 5.2 Quadro dos Ativos e Passivos Financeiros e Permanentes
Segrega Ativo e Passivo em **Financeiro** e **Permanente** (atributo de cada conta no PCASP — "indicador de superávit financeiro"). Base para apurar o superávit financeiro da Lei 4.320 (fonte de abertura de créditos adicionais).

### 5.3 Quadro das Contas de Compensação (atos potenciais)
Atos potenciais **ativos** e **passivos** (garantias, direitos/obrigações contratadas, etc.). Origem: classes **7 e 8** (controles devedores/credores).

### 5.4 Quadro do Superávit/Déficit Financeiro
**Superávit Financeiro = Ativo Financeiro − Passivo Financeiro**, apurado **por fonte/destinação de recurso**. Origem: atributo financeiro das contas das classes 1 e 2.

---

## 6. Demonstração das Variações Patrimoniais (DVP) — Anexo 15

**Objetivo:** evidenciar as variações **quantitativas** (aumentativas e diminutivas) do patrimônio, dependentes e independentes da execução orçamentária, apurando o **Resultado Patrimonial do Período**.

| Bloco | Origem PCASP |
|---|---|
| **Variações Patrimoniais Aumentativas (VPA)** — Impostos/Taxas/Contribuições; Transferências e Delegações Recebidas; Exploração e Venda de Bens/Serviços; Variações com Ativos (ganhos); Valorização/Ganhos com Ativos; Outras VPA | classe **4** |
| **Variações Patrimoniais Diminutivas (VPD)** — Pessoal e Encargos; Benefícios Previdenciários; Uso de Bens/Serviços e Consumo de Capital Fixo (depreciação/amortização/exaustão); Transferências e Delegações Concedidas; Desvalorização/Perdas; Tributárias; Custo de Mercadorias/Produtos/Serviços; Outras VPD | classe **3** |
| **RESULTADO PATRIMONIAL DO PERÍODO** = Σ VPA − Σ VPD | fechamento → transita ao PL (classe 2.3) |

> Variações **qualitativas** (permutativas, ex.: liquidação de empenho que troca passivo por caixa) **não** aparecem como resultado na DVP — apenas as quantitativas.

---

## 7. Demonstração dos Fluxos de Caixa (DFC) — MCASP / NBC TSP 12

**Objetivo:** avaliar a capacidade de gerar caixa e equivalentes e o uso desses recursos. **Método direto.**

| Grupo de fluxo | Conteúdo | Origem PCASP |
|---|---|---|
| **Fluxos das Operações** | Ingressos (receita orçamentária corrente, transf. recebidas, ingressos extraorç.) − Desembolsos (pessoal, benefícios, fornecedores, transf. concedidas, juros, desembolsos extraorç.) | classe **1** (caixa) + **6** + **3/4** |
| **Fluxos dos Investimentos** | Aquisição/alienação de ativo não circulante (imobilizado, investimentos), concessão/recebimento de empréstimos | classe **1** + **6** |
| **Fluxos dos Financiamentos** | Operações de crédito (ingresso), amortização da dívida (desembolso) | classe **1** + **6** |
| **Geração Líquida de Caixa e Equivalentes** | soma dos três grupos | — |
| **Caixa e Equivalentes — inicial / final** | conciliação com o BP | classe **1.1.1** |

> Invariante: Caixa Final = Caixa Inicial + Geração Líquida; Caixa Final deve **conciliar com o BP** e com o saldo para o exercício seguinte do BF.

---

## 8. Periodicidade e prazos

- **Periodicidade das DCASP completas:** **anual** (encerramento do exercício). BO, BF, BP, DVP, DFC + Notas Explicativas compõem o **Balanço Geral / prestação de contas anual**.
- **TCE-RS (SIAPC/PAD):**
  - Entrega **bimestral** dos dados (arquivos Lei 4.320 / RVE — Relatório de Validação e Encaminhamento) gerados automaticamente pelo PAD a partir do SIAPC; **RGF** quadrimestral ou semestral conforme porte do município (IN TCE-RS nº 4/2021).
  - **Balanço Financeiro e DFC** passam a ser elaborados pelo SIAPC/PAD a partir do **encerramento do exercício de 2026** (ajuste anunciado pelo TCE-RS — confirmar redação na norma).
  - Prazos das contas anuais (governo/gestão) e relação de documentos: **Resolução TCE-RS nº 1.134/2020**. **[obter arquivo oficial]** para a data-limite exata (tipicamente até 31/03 do exercício seguinte — **confirmar na Resolução 1.134/2020**).
- **União (SICONFI):** DCA (Declaração de Contas Anuais) anual; MSC mensal/bimestral. (detalhado em outra spec — INTEGRACOES-PRESTACAO-DE-CONTAS.md).

---

## 9. Implicações para o sistema (Rules-as-Code)

1. Toda DCASP é um **read model derivado do Balancete** (saldos por conta/período por classe PCASP) — não há digitação manual.
2. Cada linha de quadro = agregação de saldos de um **conjunto de contas PCASP** filtradas por classe + atributos (natureza da informação, indicador financeiro/permanente, destinação de recurso). O mapa conta→linha deve ser **parametrizável por exercício** (segue versão do PCASP/MCASP), nunca hardcoded.
3. Invariantes de fechamento a validar automaticamente:
   - BO: TOTAL Receita (lado) = TOTAL Despesa (lado) com superávit/déficit equilibrando.
   - BF: Σ Ingressos = Σ Dispêndios.
   - BP: ATIVO = PASSIVO + PL.
   - DVP: Resultado Patrimonial = Σ VPA − Σ VPD → integra mutação do PL.
   - DFC: Caixa Final concilia com BP (1.1.1) e com saldo p/ exercício seguinte do BF.

---

## 10. Pendências de fonte oficial — `[obter arquivo oficial]`

| Item | Documento a baixar | URL |
|---|---|---|
| Estrutura linha-a-linha e contas exatas de **todos os quadros** (BO/BF/BP/DVP/DFC), incluindo RP | **MCASP 11ª ed. — Parte V (DCASP)**, PDF oficial STN (PDF lido só parcialmente — comprimido) | https://thot-arquivos.tesouro.gov.br/publicacao/51045 (manual completo) — extrair Parte V |
| Modelos/quadros oficiais dos **Anexos 12, 13, 14 e 15** atualizados | **Portaria STN nº 438/2012** (anexos) | https://www.tesourotransparente.gov.br/ — buscar Portaria STN 438/2012 e anexos |
| Quadros de **Execução de Restos a Pagar** (colunas e contas) do BO | MCASP 11ª ed. Parte V, seção Balanço Orçamentário | mesma fonte STN acima |
| **Leiaute SIAPC/PAD** — arquivos Lei 4.320 a disposição do TCE-RS (estrutura dos registros das demonstrações) | **TCE-RS — Manual Técnico SIAPC Vol. V, "Arquivos a Disposição do TCE — Lei 4.320"** (PDF comprimido, não lido) | http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf |
| **Resumo do Leiaute** SIAPC v2.0 | TCE-RS | http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf |
| **Prazos exatos** das contas anuais municipais (data-limite, assinaturas) | **TCE-RS — Resolução nº 1.134/2020** (texto integral) | https://atosoficiais.com.br/tcers/resolucao-n-1134-2020 |
| Confirmar vigência 2026 de **BF e DFC pelo PAD** | TCE-RS — orientação técnica SIAPC 26ª edição | https://tcers.tc.br/noticia/tce-rs-realiza-26a-edicao-do-evento-do-siapc-com-orientacoes-tecnicas-para-gestores-municipais/ |

---

## 11. Fontes consultadas (URLs)

- MCASP — STN (página oficial / edição vigente 11ª): https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/manuais/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp-1
- MCASP online (estrutura DCASP e quadros) — contas.cnt.br: https://contas.cnt.br/mcasp/manual-de-contabilidade-aplicada-ao-setor-publico/demonstracoes-contabeis-aplicadas-ao-setor-publico-dcasp/
- Balanço Financeiro (estrutura + classes PCASP) — contas.cnt.br: https://contas.cnt.br/mcasp/3-balanco-financeiro/
- DCASP objetivos e resultados — contas.cnt.br: https://www.contas.cnt.br/demonstracoes-contabeis-aplicadas-ao-setor-publico-dcasp-objetivos-e-resultados/
- 11ª edição MCASP (Portaria STN/MF 2.016/2024) — contas.cnt.br: https://www.contas.cnt.br/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp-10a-edicao/
- Lei 4.320/1964 (arts. dos balanços; Anexos 12–15) — Câmara: https://www2.camara.leg.br/legin/fed/lei/1960-1969/lei-4320-17-marco-1964-376590-normaatualizada-pl.pdf
- Balanço Orçamentário Anexo 12 — Contabilidade RO: https://alemdosnumeros.contabilidade.ro.gov.br/api/public/posts/documents/1733498705071.pdf
- DCASP (BP/DVP/DFC/DMPL) — AMOSC: https://amosc.org.br/wp-content/uploads/2023/01/359591_DCASP___BP_DVP_DFC_DMPL_DRE_Consolidacao.pdf
- "Descomplicando as DCASP" — CRC-SC: https://www.crcsc.org.br/uploads/evento/9653/hL2jZ6qzw5GwTwHpiaa-0P5ruO5iMLyg.pdf
- ENAP — Preenchimento de BV, DVP, DFC, DMPL, BO e BF: https://repositorio.enap.gov.br/bitstream/1/8002/5/M%C3%B3dulo%205%20-%20Apresenta%C3%A7%C3%A3o%20de%20Preenchimento%20de%20BV,%20DVP,%20DFC,%20DMPL,%20BO%20e%20BF.pdf
- TCE-RS SIAPC (portal): http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- TCE-RS Resolução 1.134/2020: https://atosoficiais.com.br/tcers/resolucao-n-1134-2020
- TCE-RS IN 4/2021 (RVE/RREO/RGF via PAD): https://atosoficiais.com.br/tcers/instrucao-normativa-n-4-2021
