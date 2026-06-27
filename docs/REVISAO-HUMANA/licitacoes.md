# Revisão de Licitações — para o Especialista em Licitações

> **O que esta página é.** Tudo que o especialista em licitações precisa conferir sob a **Lei
> 14.133/2021 (NLLC)**: limites de dispensa (com os **valores atuais**), prazo de publicação no PNCP
> (art. 94), credenciamento (art. 79), registro de preços, contratos/aditivos, sanções e a remessa
> LicitaCon ao TCE-RS.
>
> 🟢 = parâmetro/valor ajustável · 🟡 = lógica descrita no `.rules.md` (sinalize se divergir da norma).
>
> Base normativa: [`docs/normas/FONTES-NORMATIVAS.md`](../normas/FONTES-NORMATIVAS.md) (Lei 14.133/2021;
> Manual de Integração PNCP 2.3.5; Decreto de atualização dos valores; Decreto 11.462/2023 SRP; Decreto
> 11.246/2022 agentes; LicitaCon TCE-RS 1.4.010).

---

## ⭐ A. Limites de DISPENSA por valor (parâmetro ajustável 🟢) — comece por aqui

Arquivo de regra: [`src/Modules/Administracao/rules/Dispensa.rules.md`](../../src/Modules/Administracao/rules/Dispensa.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Teto de dispensa por valor** (fail-closed: valor estimado não pode ultrapassar o limite) | **Lei 14.133/2021 art. 75, I e II** + **Decreto 12.807/2025** (vigência 01/01/2026) | 🟢 **parâmetro** (`LimiteLegalVigente`, por tenant) | **Conferir os valores vigentes.** Art. 75, I (obras/serviços de engenharia) = **R$ 130.984,20** · Art. 75, II (demais bens/serviços) = **R$ 65.492,11**. Inclusão acima do teto é recusada. |
| Reajuste anual dos limites pelo IPCA-E | **Lei 14.133/2021 art. 182** | 🟢 parâmetro (`LimiteLegalNormaFonte` registra o decreto) | Confira que o valor reflete o decreto do exercício corrente. |
| Dispensa eletrônica: aviso → lances sucessivos → julgamento → homologação | Lei 14.133/2021 art. 75; **IN SEGES/ME 67/2021** | 🟡 lógica | Confira o fluxo e que cada lance do mesmo fornecedor melhora estritamente a oferta. |
| Impedimento/inidoneidade bloqueia (fail-closed) | Lei 14.133/2021 art. 14 e art. 156 | 🟡 lógica | Confira que fornecedor sancionado não participa. |

> **Nota importante de versão:** o briefing menciona o **Decreto 12.343/2024**; o `.rules.md` já está
> ancorado no **Decreto 12.807/2025** (que atualizou os valores para 2026, vigência 01/01/2026). Os
> valores acima (130.984,20 / 65.492,11) são os de 2026. **Confirme com o decreto vigente** e, se a
> revisão for de exercício diferente, ajuste o parâmetro `LimiteLegalVigente`.

---

## ⭐ B. Publicação no PNCP — prazo do art. 94 (condição de eficácia)

Arquivo de regra: [`src/Modules/Administracao/rules/Contrato.rules.md`](../../src/Modules/Administracao/rules/Contrato.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Divulgação no PNCP é condição de eficácia do contrato** (prazos em **dias úteis**) | **Lei 14.133/2021 art. 94** | 🟢 parâmetro (calendário de dias úteis/feriados por tenant; `IPrazoPncp`, `ICalendarioDiasUteis`) + 🟡 lógica | **Conferir os prazos:** 20 d.u. (bens/serviços) · 10 d.u. (serviços/trabalhos) · obras: 25/45 d.u. (art. 94 §3). Contrato **sem nº de controle PNCP é ineficaz e NÃO sustenta empenho** (trava de bloqueio). |
| Edital obrigatoriamente publicado no PNCP | **Lei 14.133/2021 art. 174** | 🟡 lógica (evento `EditalPublicadoNoPncp`) | Confira que a publicação é exigida para a regularidade do certame. |
| Aditivo conta prazo PNCP próprio (da assinatura) | Lei 14.133/2021 art. 94 | 🟡 lógica (tem `TODO(M10)`) | Confira a contagem de prazo do aditivo (transmissão real pendente de credencial/canal de produção — `TODO(M10)` no código; ver `tce-rs-integracao.md`). |

> **Atenção:** o art. **94** (eficácia por prazo) é diferente do art. **174** (institui o PNCP/obriga
> a publicidade). Os dois aparecem no sistema; confira ambos.

---

## C. Licitação (modalidades e ciclo)

Arquivo de regra: [`src/Modules/Administracao/rules/Licitacao.rules.md`](../../src/Modules/Administracao/rules/Licitacao.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Modalidades (Pregão, Concorrência, Diálogo Competitivo) e critérios de julgamento | **Lei 14.133/2021 art. 28, 33** | 🟡 lógica | Confira as modalidades disponíveis. |
| Pregão admite **só** Menor Preço ou Maior Desconto | Lei 14.133/2021 art. 33 | 🟡 lógica (invariante I-5) | Confira que Pregão rejeita Técnica/Melhor Técnica/Maior Lance. |
| Encerramento (homologação/revogação/anulação/fracasso/deserção) | **Lei 14.133/2021 art. 71** | 🟡 lógica | Confira os desfechos do certame. |

---

## D. Credenciamento (inexigibilidade — art. 79)

Arquivo de regra: [`src/Modules/Administracao/rules/Credenciamento.rules.md`](../../src/Modules/Administracao/rules/Credenciamento.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Chamamento **permanentemente aberto**; inscrição a qualquer tempo | **Lei 14.133/2021 art. 79, parágrafo único** | 🟡 lógica | Confira que o chamamento fica sempre aberto. |
| **Três hipóteses do art. 79** (Paralela Não Excludente; Seleção por Critério do Beneficiário; Mercados Fluidos) | **Lei 14.133/2021 art. 79, I a III** (c/c art. 74 IV e art. 78 I) | 🟡 lógica (invariante I-2) | Confira que a hipótese autorizadora é obrigatória e imutável após a abertura. |

---

## E. Registro de Preços (SRP)

Arquivo de regra: [`src/Modules/Administracao/rules/RegistroPrecos.rules.md`](../../src/Modules/Administracao/rules/RegistroPrecos.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Ata registra fornecedores/preços/quantidades; vigência; saldo; adesão/carona | **Lei 14.133/2021 art. 82-86** + **Decreto 11.462/2023** | 🟢 parâmetro (vigência da ata; saldo por item) + 🟡 lógica | Confira que contratação/adesão **debitam saldo** e não excedem o disponível, dentro da vigência. |
| Prorrogação exige vantajosidade comprovada | **Lei 14.133/2021 art. 84** | 🟡 lógica (invariante I-10) | Confira a regra de prorrogação. |
| Item único por fornecedor na ata | Lei 14.133/2021 art. 82 | 🟡 lógica (invariante I-4) | Confira que o par (item, fornecedor) não se registra duas vezes. |

---

## F. Contratos e Aditivos

Arquivo de regra: [`src/Modules/Administracao/rules/Contrato.rules.md`](../../src/Modules/Administracao/rules/Contrato.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| **Limite de aditivo quantitativo** (acréscimo/supressão) | **Lei 14.133/2021 art. 125** | 🟡 lógica (invariante I-9) | Confira: soma ≤ **25%** (bens/serviços); até **50%** em reforma de edifício/equipamento. A soma acumulada é verificada no agregado. |
| **Garantia de execução** | **Lei 14.133/2021 art. 96/98** | 🟡 lógica (invariante I-12) | Confira: ≤ **5%** (comum); até **10%** em obras de grande vulto. |
| Apostilamento (alteração sem termo aditivo) | **Lei 14.133/2021 art. 136** | 🟡 lógica | Confira reajuste/dotação por apostilamento. |
| Vigência vinculada a crédito orçamentário | **Lei 14.133/2021 art. 105/106**; LRF | 🟡 lógica (invariante I-8) | Confira que `IniciarExecucao` exige dotação confirmada (empenho) **e** publicação no PNCP. |

---

## G. Fornecedores, Catálogo e PCA

| Item | Arquivo / Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Cadastro de fornecedor (CNPJ validado, nível SICAF, sanções) | [`Fornecedor.rules.md`](../../src/Modules/Administracao/rules/Fornecedor.rules.md) — **Lei 14.133/2021 art. 14, 87, 155-156** | 🟡 lógica | **Conferir:** impedimento (art. 156 III) e inidoneidade (art. 156 IV) vigentes **bloqueiam** habilitação/contratação (fail-closed). Advertência/multa não bloqueiam. |
| Catálogo de materiais/serviços (CATMAT/CATSER) | [`Catalogo.rules.md`](../../src/Modules/Administracao/rules/Catalogo.rules.md) — Lei 14.133/2021 (boas práticas) | 🟡 lógica | Confira código único por tenant; item inativo bloqueia novo uso. |
| **Plano de Contratações Anual (PCA)** | [`Pca.rules.md`](../../src/Modules/Administracao/rules/Pca.rules.md) — **Lei 14.133/2021 art. 12, VII** + Decreto 11.246/2022 | 🟡 lógica | Confira um PCA por exercício e o fluxo de revisão com motivo. |

---

## H. Remessa LicitaCon ao TCE-RS

Arquivo de regra: [`src/Modules/Administracao/rules/LicitaCon.rules.md`](../../src/Modules/Administracao/rules/LicitaCon.rules.md)

| Item | Norma | Tipo | O QUE CONFERIR |
|---|---|---|---|
| Remessa LicitaCon **1.4** — 14 arquivos CSV, validada pelo e-Validador | **TCE-RS IN 13/2017**; Leiaute 1.4 (e-Validador); Lei 14.133/2021 | 🟡 lógica | Confira os 14 arquivos (PESSOAS, LICITACAO, LICITANTE, LOTE, ITEM, PROPOSTA, CONTRATO etc.) e a determinismo da geração. Transmissão real pendente de credencial/canal de produção (`TODO(M10)` no código) — detalhe em [`tce-rs-integracao.md`](tce-rs-integracao.md). |

> **Convênios/parcerias (MROSC):** o módulo Convenios não tem `.rules.md` próprio nesta fase, mas tem
> a integração com o Transferegov.br (Lei 13.019/2014) — pontos de transmissão em
> [`tce-rs-integracao.md`](tce-rs-integracao.md).

---

## Pontos de atenção a SINALIZAR

- **Decreto de valores de dispensa:** confirmar o decreto **vigente no exercício** (o sistema usa
  Decreto 12.807/2025 para 2026). Se a revisão for de outro ano, ajustar `LimiteLegalVigente`.
- **PNCP art. 94:** prazos em **dias úteis** (não corridos) — confira o calendário de feriados do tenant.
- **Regulamentação local:** a NLLC delega a regulamentação ao município (decreto municipal próprio,
  agente de contratação local). Verifique o decreto de Maximiliano de Almeida (FONTES-NORMATIVAS,
  lacuna 6) e ajuste parâmetros conforme.

---

## Resumo para o especialista em licitações

- **9 regras de negócio** (`.rules.md`) para revisar no módulo Administracao.
- **Parâmetros ajustáveis diretos (🟢):** os **limites de dispensa** (R$ 130.984,20 / R$ 65.492,11,
  por decreto), o **calendário de dias úteis** do PNCP (art. 94), a **vigência/saldo** das atas SRP.
- **Prioridade de conferência (lógica 🟡):** o teto fail-closed da dispensa, os **prazos do PNCP**
  (20/10/25/45 d.u.) e a trava "sem PNCP não empenha", os **limites de aditivo** (25%/50%) e de
  **garantia** (5%/10%), e o bloqueio por inidoneidade/impedimento.
- **A transmissão** (LicitaCon ao TCE-RS, PNCP, Transferegov) está detalhada em
  [`tce-rs-integracao.md`](tce-rs-integracao.md).
