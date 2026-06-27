# Revisão Contábil — para o Contador Municipal

> **O que esta página é.** Tudo que o contador precisa conferir na **contabilidade pública**
> (Finanças) e na **prestação de contas** (Transparência): plano de contas PCASP, ciclo da despesa,
> encerramento de exercício, MSC/SICONFI, RREO/RGF, retenções na fonte e mínimos constitucionais.
>
> 🟢 = parâmetro ajustável direto (muda o valor → muda o sistema) · 🟡 = lógica descrita no
> `.rules.md` (se estiver errada vs a norma, sinalize que o dev ajusta).
>
> Base normativa consolidada: [`docs/normas/FONTES-NORMATIVAS.md`](../normas/FONTES-NORMATIVAS.md)
> (MCASP 11ª ed.; Portaria STN 642/2019 — MSC; MDF 15ª ed. — RREO/RGF; Lei 4.320/64; LRF LC 101/2000;
> Decreto 10.540/2020 — SIAFIC).

---

## A. Contabilidade — Plano de Contas e Lançamentos (PCASP/MCASP)

Arquivo de regra: [`src/Modules/Financas/rules/Contabilidade.rules.md`](../../src/Modules/Financas/rules/Contabilidade.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Plano de Contas PCASP (conta segmentada `C.G.SG.T.ST.IT.SI`, natureza, F/P) | MCASP 11ª ed. (STN); spec em `docs/architecture/specs-oficiais/pcasp-plano-de-contas.md` | 🟡 lógica + 🟢 seed | Confira se o plano de contas semeado reflete o PCASP estendido vigente (atenção às alterações do PCASP com vigência 2026 — pendência registrada em FONTES-NORMATIVAS A1). |
| Partida dobrada (ΣDébitos = ΣCréditos por lançamento) | MCASP | 🟡 lógica (invariante 1) | Toda escrituração precisa fechar débito = crédito; confira que nenhum lançamento manual quebra isso. |
| Homogeneidade de natureza da informação | MCASP §3 | 🟡 lógica (invariante 2) | Todas as partidas de um lançamento devem ter a mesma natureza (orçamentária OU patrimonial OU controle). |
| Roteiros de contabilização (evento do ciclo → partidas) | MCASP eventos contábeis; spec `mcasp-eventos-contabeis.md` | 🟢 parametrizável por tenant (`EventoContabil`/`LinhaRoteiro`) | Confira o mapa Empenho/Liquidação/Pagamento/Receita → contas de débito e crédito (tabela "Mapa evento → partidas" no `.rules.md`). É aqui que a "amarração" contábil pode ser ajustada. |
| Estorno preserva o lançamento original | Auditoria imutável (TCE) | 🟡 lógica (invariante 5) | Estorno gera lançamento inverso, nunca apaga o original. |

---

## B. Ciclo da Despesa — Empenho → Liquidação → Pagamento

Arquivo de regra: [`src/Modules/Financas/rules/Empenho.rules.md`](../../src/Modules/Financas/rules/Empenho.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Estágios sequenciais sem salto (Empenhado → Liquidado → Pago) | Lei 4.320/64 art. 58–65 | 🟡 lógica (invariante I-8) | Não pode pagar sem liquidar; confira que o sistema bloqueia o salto de estágio. |
| Pagamento exige liquidação prévia | **Lei 4.320/64 art. 62** | 🟡 lógica (invariante I-6) | Confira a trava: pagar sem liquidação prévia deve ser recusado. |
| Tipos de empenho (Ordinário / Estimativo / Global) | **Lei 4.320/64 art. 60** | 🟡 lógica (enum) | Confira se a classificação está disponível e correta. |
| Saldos por valor (não-empenhar acima da dotação; não-liquidar acima do empenho; não-pagar acima do liquidado) | Lei 4.320/64; LRF | 🟡 lógica | Confira os controles de saldo da `DotacaoOrcamentaria`. **Atenção:** o `.rules.md` tem `TODO(revisao-contabil)` no changelog — a prosa descreve modelo simplificado, mas o código já tem saldos por valor. Vale uma conferência. |
| Anulação vedada após pagamento | Lei 4.320/64 | 🟡 lógica (invariante I-7) | Empenho pago não pode ser anulado. |

---

## C. Planejamento Orçamentário — PPA / LDO / LOA + Créditos

Arquivo de regra: [`src/Modules/Financas/rules/Planejamento.rules.md`](../../src/Modules/Financas/rules/Planejamento.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Compatibilidade LOA ⊆ LDO ⊆ PPA | CF arts. 165/166/167; **Lei 4.320/64 arts. 2-15, 40-46**; LRF arts. 4-5 | 🟡 lógica (invariante 3) | Toda despesa fixada na LOA deve existir/estar vigente no PPA e priorizada na LDO. |
| Equilíbrio orçamentário (Σ receita prevista ≥ Σ despesa fixada) | **Lei 4.320/64 art. 2º** | 🟡 lógica (invariante 4) | Confira a validação no momento de aprovar a LOA. |
| Limite de suplementação por decreto | **CF 167, V; Lei 4.320 art. 7º** | 🟢 parâmetro (`LimiteSuplementacaoPercentual`) | Confira o percentual-limite de suplementações autorizado na LOA — é parametrizável e deve refletir a LOA do município. |
| Crédito extraordinário dispensa autorizador/fonte | **CF 167 §3º** | 🟡 lógica (invariante 7) | Confira que suplementar exige dotação alvo; extraordinário não. |

---

## D. Encerramento de Exercício (PCASP/MCASP)

Arquivo de regra: [`src/Modules/Financas/rules/Encerramento.rules.md`](../../src/Modules/Financas/rules/Encerramento.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Apuração do resultado patrimonial (zera VPA/VPD classes 3/4 contra `2.3.7.1.1.01.00`) | STN **IPC 03 §§21-23** | 🟡 lógica (invariante 4) | Confira a conta de apuração e a pré-condição (a conta `2.3.7.1.1.01.00` deve estar zerada antes da apuração patrimonial). |
| Apuração do resultado orçamentário (zera classes 5/6) | Lei 4.320/64 | 🟡 lógica | Confira a sequência de fases (só "para frente", uma a uma). |
| Inscrição de Restos a Pagar | **Lei 4.320/64 art. 36** | 🟡 lógica | Confira a inscrição de RaP processados/não processados no encerramento. |
| MSC de Encerramento (mês 13) alimenta a DCA | **LRF art. 51**; Portaria STN 642/2019 | 🟡 lógica | Confira que a matriz anual de encerramento é a base do rascunho da DCA. |
| Reprodutibilidade e idempotência (sem relógio; reexecutar fase concluída é no-op) | DESIGN encerramento | 🟡 lógica (invariantes 1,2,3) | Confira que reexecutar não duplica lançamentos e que exercício encerrado fica congelado. |

> Autoridade adicional: `docs/architecture/encerramento-exercicio/DESIGN.md`.

---

## E. Retenções na Fonte (consignações extra-orçamentárias)

Arquivo de regra: [`src/Modules/Financas/rules/Retencoes.rules.md`](../../src/Modules/Financas/rules/Retencoes.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Tabela de IRRF sobre pagamentos a PJ** (alíquotas e códigos DARF) | **IN RFB 1.234/2012, Anexo I**, alterada pela **IN RFB 2.145/2023** (estende a Estados/Municípios) | 🟢 **parâmetro ajustável** | **Confira as alíquotas e códigos de receita.** Arquivo: [`src/Modules/Financas/.../Retencoes/TabelaIrrfServicosCatalogo.cs`](../../src/Modules/Financas/Tensorroot.Gov.Modules.Financas.Application/Retencoes/TabelaIrrfServicosCatalogo.cs). Valores atuais: 1,2% cód. 6147 (mercadorias/serviços com materiais/hospitalar) · 0,24% cód. 9060 (álcool/biodiesel) · 2,4% cód. 6175 (transporte de passageiros) · 2,4% cód. 6188 (financeiros/seguros) · 4,8% cód. 6190 (demais serviços). Vigência padrão: 01/01/2024. |
| **Dispensa de retenção por DARF mínimo** | **IN RFB 1.234/2012 art. 3º §6º** | 🟢 **parâmetro** (`ValorMinimoRetencaoPadrao = 10.00`) | Confira o valor mínimo de R$ 10,00 por DARF abaixo do qual a retenção é zero. Mesmo arquivo (`TabelaIrrfServicosCatalogo.cs`). |
| Consignação não é despesa orçamentária (ingresso/dispêndio extra-orçamentário) | **Lei 4.320/64 art. 3º par. único e art. 11** | 🟡 lógica (invariante I-1) | Confira que a retenção e o recolhimento transitam como extra-orçamentário, sem reduzir a dotação. |
| Retenção ≤ valor do pagamento; líquido = bruto − retenções | MCASP (passivo "valores a recolher") | 🟡 lógica (invariante I-2) | Confira o cálculo do líquido ao credor. |
| Guia de recolhimento baixa o passivo no recolhimento | MCASP | 🟡 lógica (invariantes I-4/I-5) | Confira a contabilização da baixa da consignação no recolhimento. |

> **Atenção (revisão tributária federal):** a **DIRF foi extinta** — as retenções migram para EFD-Reinf
> R-4000 + eSocial S-1210 (ver `rh-folha.md`). Se aparecer geração de DIRF, sinalize.

---

## F. Tesouraria (Caixa-Banco) e Remessa Bancária

| Item | Arquivo / Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Movimento de caixa, transferência entre contas, Boletim de Caixa/Receita/Despesa | [`src/Modules/Financas/rules/Tesouraria.rules.md`](../../src/Modules/Financas/rules/Tesouraria.rules.md) — Lei 4.320/64 | 🟡 lógica + 🟢 limite de saldo por conta | Confira o fechamento diário e o limite parametrizado de saldo negativo por conta. A data usa o relógio civil do tenant (não UTC). Import de extrato OFX/CNAB é `TODO(M10)`. |
| Conciliação bancária (casamento com extrato) | Tesouraria.rules.md | 🟡 lógica | Confira a marcação de movimento conciliado; o import do extrato ainda depende de credencial/layout do banco (M10). |
| **Remessa de pagamento CNAB240 (FEBRABAN, serviço 20)** | [`src/Modules/Financas/rules/RemessaCnab.rules.md`](../../src/Modules/Financas/rules/RemessaCnab.rules.md) — layout FEBRABAN CNAB240 | 🟡 lógica | Confira que cada registro tem exatamente 240 posições e que os trailers somam valores/contam registros. Transmissão ao banco é diferida (M10). |
| Receita Arrecadada (reconhecimento via evento de Tributos) | [`src/Modules/Financas/rules/ReceitaArrecadada.rules.md`](../../src/Modules/Financas/rules/ReceitaArrecadada.rules.md) — **Lei 4.320/64 art. 35 e art. 39** | 🟡 lógica | A receita é reconhecida ao quitar dívida ativa (Tributos). **Ponto de atenção do `.rules.md`:** dedup/idempotência por origem ainda não está explícita no handler (CB-4) e não há regra de sinal sobre o valor (CB-5). Vale sinalizar. |

---

## G. Credor (cadastro de fornecedores/credores de Finanças)

Arquivo de regra: [`src/Modules/Financas/rules/Credor.rules.md`](../../src/Modules/Financas/rules/Credor.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Cadastro de credor (CPF/CNPJ válido) para empenho/pagamento | Lei 4.320/64 (controle da despesa) | 🟡 lógica | Confira validação de documento e vínculo do credor ao empenho. |

---

## H. Prestação de Contas Fiscal — MSC / RREO / RGF / DCA (SICONFI)

Arquivo de regra: [`src/Modules/Transparencia/rules/DeclaracaoFiscal.rules.md`](../../src/Modules/Transparencia/rules/DeclaracaoFiscal.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| MSC consolida saldos contábeis e dela derivam RREO/RGF/DCA | **Portarias STN / Regras Gerais MSC** (Portaria STN 642/2019); **LRF arts. 48/48-A** | 🟡 lógica | A MSC vem da contabilidade de Finanças (evento `MSCGeradaIntegrationEvent`); o módulo é consumidor, não recalcula. |
| Matriz de saldos **balanceada** (TotalDébitos == TotalCréditos) | PCASP partidas dobradas | 🟡 lógica (invariante I-7) | Confira que matriz desbalanceada é recusada na consolidação. |
| Coerência período × tipo (MSC=mês; RREO=bimestre; RGF=quadrimestre; DCA=ano) | MDF 15ª ed.; LRF | 🟡 lógica (invariante I-3) | Confira os períodos: RREO bimestral, RGF quadrimestral (municípios). |
| **Prazos de transmissão** | **LRF** (MSC: último dia do mês subsequente; RREO/RGF: até 30 dias após o período) | 🟢 parâmetro (`DataLimite` por tenant, via `ICalendarioFiscal`) | Confira as datas-limite parametrizadas; atraso gera alerta de risco de bloqueio de transferências (**LRF art. 23 §3º**). |

---

## I. Remessa ao TCE-RS (SIAPC/PAD)

Arquivo de regra: [`src/Modules/Transparencia/rules/RemessaTce.rules.md`](../../src/Modules/Transparencia/rules/RemessaTce.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Remessa por leiaute versionado, validada pelo e-Validador (RDI) antes de enviar | **Resoluções TCE-RS** (SIAPC/PAD, e-Validador); leiautes versionados | 🟡 lógica | Confira o ciclo Gerada → Validada → Enviada → Homologada/Rejeitada; remessa com erro no RDI tem envio bloqueado. (Detalhes de leiaute/versão estão em `tce-rs-integracao.md`.) |
| Prazo de remessa parametrizável; atraso bloqueia transferências voluntárias | **LRF art. 23 §3º**; Resolução TCE-RS 1099/2018 | 🟢 parâmetro (`DataLimite` por tenant) | Confira a data-limite por período/exercício. |
| Imutabilidade por hash de integridade | Retenção/auditoria TCE | 🟡 lógica (invariante I-9) | Confira que alterar arquivos após a geração invalida a remessa (exige regeração). |

---

## J. Mínimos Constitucionais — Saúde 15% e Educação 25%

Arquivo de regra: [`src/Modules/Transparencia/rules/NucleoFiscalMinimos.rules.md`](../../src/Modules/Transparencia/rules/NucleoFiscalMinimos.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Percentual mínimo da Saúde (15% ASPS)** | **LC 141/2012** (regulamenta a EC 29) | 🟢 **parâmetro por tenant+vigência** (`ParametroMinimo`, default 15%) | Confira o percentual vigente da saúde (o sistema permite sobrepor o default por município). |
| **Percentual mínimo da Educação (25% MDE)** | **CF art. 212** | 🟢 **parâmetro por tenant+vigência** (default 25%) | Confira o percentual vigente da educação (MDE). |
| O que **computa** no mínimo (classificação por função/fonte) | **LC 141 art. 4º** (saneamento/inativos/merenda/limpeza NÃO computam em Saúde); Portaria MOG 42/1999 (funções 10/12); Portaria STN 710/2021 (FR) | 🟢 **parâmetro** (`FonteRecursoVinculado` por tenant+vigência) + 🟡 lógica | **Confira as regras de classificação:** qual despesa por (função, fonte) entra no mínimo e qual não entra. É o ponto mais sensível da apuração — regras cadastradas erradas distorcem o índice. |
| Apuração reprodutível e sem divisão por zero | — | 🟡 lógica (invariantes I-5/I-6/I-8) | Confira que receita-base zero → 0% e que "atingido" = % aplicado ≥ % mínimo (arredondado a 4 casas). |

> Calendários federais (SIOPS/SIOPE/RMA) e pareceres de conselho (CMS/CME/CACS-FUNDEB/CMAS) também
> são modelados aqui como prazos parametrizáveis e evidências imutáveis para o TCE.

---

## Resumo para o contador

- **13 regras de negócio** (`.rules.md`) para revisar: 9 em Finanças (Contabilidade, Empenho,
  Planejamento, Encerramento, Retenções, Tesouraria, Receita Arrecadada, Credor, Remessa CNAB) +
  4 em Transparência (Declaração Fiscal/MSC, Remessa TCE, Núcleo Fiscal Mínimos, Portal/ESIC).
- **Parâmetros ajustáveis diretos (🟢):** a **tabela de IRRF/PJ** (alíquotas + códigos DARF + dispensa
  R$ 10), os **percentuais dos mínimos** (saúde 15% / educação 25%, por tenant), as **regras de
  classificação** do que computa no mínimo, o **limite de suplementação** da LOA e os **prazos**
  de MSC/RREO/RGF e de remessa ao TCE.
- **Pontos de atenção a sinalizar:** o `TODO(revisao-contabil)` na prosa do Empenho; a falta de
  dedup explícita em Receita Arrecadada; e a pendência do PCASP 2026 (alterações da STN).
- **A revisão da transmissão** (versão de leiaute SIAPC/MSC/folha-TCE) está detalhada em
  [`tce-rs-integracao.md`](tce-rs-integracao.md).
