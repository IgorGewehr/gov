# Prova de Conceito / Prova de Aderência / Amostra em Licitação de Software de Gestão Pública

> Pesquisa para preparação de PROVAS DE CONCEITO/ADERÊNCIA em licitações (Lei 14.133/2021).
> Contexto Tensorroot.Gov: ERP GovTech municipal multi-tenant (Executivo + Legislativo).
> Regra do estudo: toda afirmação tem FONTE (URL) ou marcação `[a confirmar]`.
> Data: 2026-06-22.

---

## 1. O que é (definições)

- **Prova de Conceito (PoC / "Proof of Concept")**: etapa do certame, regrada previamente no edital, na qual se aplica metodologia para verificar se a proposta do licitante **classificado em 1º lugar** contempla todos os requisitos necessários à satisfação da necessidade pública. Valida na prática as funcionalidades pedidas teoricamente no Termo de Referência (TR), evitando contratar objeto inadequado/inservível. Aplica-se tipicamente a contratações de TIC. FONTE: TCU – Licitações e Contratos, item 5.4.1.2 (https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/); Justen (https://justen.com.br/artigo_pdf_2/a-prova-de-conceito-poc-a-luz-da-eficiencia-e-da-racionalidade-administrativa/).
- **"Prova de aderência"**: termo usado em editais como sinônimo prático de PoC focada em **aderência às especificações** do TR (atendimento item a item). A Lei usa o verbo "comprovar sua aderência às especificações". `[a confirmar]` — não há definição legal autônoma; é nomenclatura de edital. FONTE da expressão legal "aderência às especificações": Lei 14.133/2021, art. 17, §3º (via TCU 5.4.1.2).
- **Amostra / homologação de amostras / exame de conformidade**: institutos correlatos da mesma família (art. 17, §3º). PoC é a modalidade típica para software (objeto imaterial); "amostra" é mais usada para bens físicos.

---

## 2. Base legal (Lei 14.133/2021)

| Dispositivo | Conteúdo |
|---|---|
| **Art. 17, §3º** | Autoriza a Administração, em relação ao licitante provisoriamente vencedor, a realizar análise/avaliação da conformidade da proposta mediante **homologação de amostras, exame de conformidade e prova de conceito**, entre outros testes, para comprovar **aderência às especificações** do TR/Projeto Básico. (núcleo legal da PoC) |
| **Art. 41, II** | Permite, para bens, exigência de amostra/PoC **na fase de julgamento das propostas ou de habilitação**, desde que **prevista no edital** e **justificada a necessidade**. |
| **Art. 42, §§ 2º e 3º** | Admite oferta de **protótipo** e o exame por **instituição especializada** (laboratório/órgão técnico). |
| **Art. 17 + regra geral** | A PoC se insere na fase de julgamento; não é requisito de habilitação. |

FONTE: TCU – Licitações e Contratos 5.4.1.2 (https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/); texto da lei: Planalto (https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm).

`[a confirmar]` redação literal art. 17 §3º e art. 42 §§2º-3º direto no Planalto (validar incisos exatos antes de citar em peça/impugnação).

---

## 3. Como funciona na prática

### 3.1 Quem é convocado e quando
- PoC é exigida **somente do licitante provisoriamente vencedor (1º colocado)**, na **fase de julgamento** — nunca como condição de habilitação para todos. Se reprovado/recusar, desclassifica-se e convoca-se o 2º colocado, **sucessivamente**, até classificar quem atenda integralmente o TR. FONTE: TCU 5.4.1.2; Acórdão 2640/2019-TCU-Plenário (exigência só na classificação, exclusivamente do 1º colocado).

### 3.2 O que o edital DEVE conter (requisitos de validade)
1. **Data, horário e local** de realização/entrega.
2. **Roteiro detalhado da avaliação** (script de demonstração, item a item).
3. **Critérios objetivos de aceitação** (sem subjetividade).
4. **Justificativa formal da necessidade** (caráter excepcional; pode restringir competição).

FONTE: TCU 5.4.1.2; Zênite – "PoC: cautelas necessárias" (https://zenite.blog.br/prova-de-conceito-poc-cautelas-necessarias/).

### 3.3 Comissão / equipe técnica de avaliação
- A PoC é conduzida por **equipe técnica designada** (comissão avaliadora), responsável por aferir o atendimento item a item e emitir **laudo de avaliação**.
- Demais licitantes **podem acompanhar** (publicidade/isonomia), mediante registro formal junto ao pregoeiro. FONTE: TCU 5.4.1.2; Acórdão 1823/2017-TCU (acompanhamento por todos os licitantes é obrigatório).
- Exemplo real de laudo: "Comissão Avaliadora da Prova de Conceito ... emite o presente laudo ... verificado por todos os integrantes da Comissão". FONTE: Laudo PoC Câmara de Pilar do Sul/SP (PDF, item 6 do Anexo I – TR).

### 3.4 Quem demonstra / ambiente
- A **licitante** demonstra (opera o sistema), perante a comissão. Pode ser presencial no órgão ou remoto, conforme edital. `[a confirmar]` regra de ambiente varia por edital (alguns exigem ambiente próprio da licitante; outros, infraestrutura do órgão).

### 3.5 Prazo
- Exemplo de edital: **prazo máximo de 15 (quinze) dias** a contar da convocação, distribuídos entre entrega, preparação e execução. FONTE: resultado de busca em editais (a confirmar edital específico).
- Disponibilização pós-homologação observada em laudo real: **30 (trinta) dias** para entrega em pleno funcionamento. FONTE: Laudo PoC Pilar do Sul/SP.
- `[a confirmar]` — prazos NÃO são padronizados em lei; cada edital define. Coletar faixa típica (5–15 dias úteis para a demonstração).

---

## 4. Percentual de aderência (obrigatórios vs desejáveis)

A Lei **não fixa percentual**; é definido em cada TR. Padrão de mercado observado:

- **Requisitos obrigatórios/mínimos/essenciais**: exige-se em regra **100% de atendimento**; falha desclassifica.
- **Requisitos desejáveis/complementares**: exige-se um **mínimo percentual** (ex.: 80%, 90%), pontuáveis ou de corte.

Exemplos reais coletados:
- Edital com **110 requisitos** = **83 obrigatórios + 27 desejáveis**. FONTE: resultado de busca em editais (`[a confirmar]` edital de origem).
- Laudo real (Câmara de Pilar do Sul/SP): sistema atendeu **100% dos Requisitos Mínimos dos Programas (item 5.2)**, **superou o mínimo de 80% da Especificação dos Programas (item 5.3)** e **100% do item 5.4 SIAFIC**; "ATENDIDO PARCIALMENTE" foi registrado em itens não-mínimos. FONTE: Laudo PoC Pilar do Sul/SP (PDF).

`[a confirmar]` — montar tabela própria a partir de 5–8 editais reais do RS (Executivo e Legislativo) para calibrar o padrão regional de % obrigatório/desejável.

---

## 5. O que DESCLASSIFICA

- **Recusar** a apresentar a PoC/amostra quando convocado (1º colocado).
- **Reprovação**: não atender **requisito obrigatório/mínimo** (em regra, qualquer falha em obrigatório).
- Não atingir o **percentual mínimo** dos requisitos desejáveis quando o edital o estabelece como corte.
- Proposta em **desacordo com especificações, prazos e condições** do edital (cláusula geral de desclassificação — art. 59). FONTE: edital de software (Pato Branco/Bolsa de Licitações) — "Serão desclassificadas as propostas: a) que estejam em desacordo com as especificações, prazos e condições fixados neste Edital".
- Consequência: desclassificado → convoca-se o próximo colocado, sucessivamente. FONTE: TCU 5.4.1.2.

---

## 6. Jurisprudência (TCU)

| Acórdão | Tese |
|---|---|
| **1.113/2009-Plenário** | PoC, quando exigida, **não pode ser condição de habilitação**; limita-se ao 1º colocado provisório. FONTE: resumo TCU/busca. `[a confirmar]` texto. |
| **1.984/2008** (Cedraz) | PoC não conflita com exigências de habilitação (sob Lei 8.666). FONTE: Justen. `[a confirmar]`. |
| **2.763/2013** (Weder) | PoC **não pode ser imposta como condição de habilitação**. FONTE: Justen. `[a confirmar]`. |
| **2.992/2016** | Rejeita PoC de **caráter facultativo**; **proíbe ausência de critérios específicos** de avaliação. FONTE: TCU 5.4.1.2. |
| **2.059/2017-Plenário** (Zymler) | Definição fundamental da finalidade da PoC; **realizar PoC antes de definir especificações mínimas → direcionamento/irregularidade**. FONTE: Justen; Zênite. |
| **1.823/2017** | Deve-se **viabilizar acompanhamento por todos os licitantes** (publicidade). FONTE: TCU 5.4.1.2; Zênite. |
| **529/2018** | Exige **critérios objetivos e motivação detalhada**. FONTE: TCU 5.4.1.2. |
| **2.569/2018-Plenário** | (PoC/amostra em TI) — `[a confirmar]` tese exata. FONTE: https://pesquisa.apps.tcu.gov.br/doc/acordao-completo/2569/2018/Plen%C3%A1rio |
| **339/2019** | Exigir **ferramentas específicas não essenciais** na PoC fere isonomia. FONTE: Zênite. |
| **2.640/2019** | Exigência só na **fase de classificação**, exclusivamente do **1º colocado**. FONTE: TCU 5.4.1.2. |
| **387/2024** | Admite **inversão de fases** com devida justificação. FONTE: TCU 5.4.1.2. |

`[a confirmar]` — abrir cada acórdão em pesquisa.apps.tcu.gov.br e extrair o enunciado literal antes de citar em PoC/impugnação. Verificar também jurisprudência **TCE-RS** (não coberta nesta rodada).

---

## 7. Implicações para o Tensorroot.Gov (síntese)

- A PoC é o ponto de maior risco de desclassificação em prova de aderência: **falha em 1 requisito obrigatório desclassifica**. Priorizar cobertura de 100% dos itens marcados "obrigatório/mínimo/essencial" nos TRs-alvo.
- Preparar **roteiro de demonstração espelhado no padrão de TR** (item a item), com tela/fluxo pronto para cada requisito.
- Garantir **multi-tenant Executivo + Legislativo** demonstrável separadamente (alguns laudos avaliam módulo Legislativo/processo digital à parte — ver Pilar do Sul, Câmara).
- Contabilidade PCASP/SIAFIC e prestação de contas TCE-RS/SICONFI já provados (ver ESTADO-ATUAL.md) cobrem os itens de maior peso recorrente (laudo Pilar do Sul exigiu 100% SIAFIC).

---

## 8. Pendências / próximos passos `[a confirmar]`

1. Validar redação literal de **art. 17 §3º, art. 41 II, art. 42 §§2º-3º** direto no Planalto.
2. Coletar **5–8 editais/TRs reais do RS** (Executivo e Câmaras) via PNCP e portais de transparência → tabela de % obrigatório vs desejável e prazos.
3. Extrair **enunciados literais** dos acórdãos TCU listados (esp. 2569/2018, 2640/2019, 2059/2017) e mapear **jurisprudência TCE-RS** sobre PoC em TI.
4. Levantar **modelos de roteiro de demonstração** e **composição/atribuições de comissão técnica** em editais de software de gestão.
5. Confirmar regra de **ambiente** (infra da licitante vs do órgão) e modo (presencial/remoto) nos editais-alvo.

---

## FONTES (URLs)

- TCU – Licitações e Contratos, 5.4.1.2 Amostra e prova de conceito: https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/
- TCU – 5.4.1 Aceitabilidade e desclassificação: https://licitacoesecontratos.tcu.gov.br/5-4-1-aceitabilidade-e-desclassificacao-2/
- Justen – "A Prova de Conceito (PoC) à luz da eficiência e da racionalidade administrativa": https://justen.com.br/artigo_pdf_2/a-prova-de-conceito-poc-a-luz-da-eficiencia-e-da-racionalidade-administrativa/
- Zênite – "Prova de Conceito (PoC): cautelas necessárias": https://zenite.blog.br/prova-de-conceito-poc-cautelas-necessarias/
- Lei 14.133/2021 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm
- Laudo PoC – Câmara Municipal de Pilar do Sul/SP (software Fiorilli): https://www.pilardosul.sp.gov.br/licitacao/download/6424/
- Edital software gestão de saúde pública – Pato Branco/PR: https://patobranco.pr.gov.br/wp-content/uploads/2023/04/41-SOFTWARE-GESTAO-DE-SAUDE-PUBLICA.pdf
- Edital software gestão pública – Nova Esperança/PR (PE 89/2023): https://www.novaesperanca.pr.gov.br/licitacoes/84411c02024d5bb139d4d7d455fbad1b.pdf
- Anexo I Prova de Conceito – FUNDAJ (PG 33/2021): https://www.gov.br/fundaj/pt-br/acesso-a-informacao/licitacoes-e-contratos/detalhamento-das-licitacoes/abrir-pasta-pg-33-2021-transformacao-digital/anexo-i-prova-de-conceito-fundaj.pdf
- Relatório PoC – IFB (PE 90056/2024): https://ifb.edu.br/attachments/article/39100/Relat%C3%B3rio%20da%20Prova%20de%20Conceito%20-%20Preg%C3%A3o%20Eletr%C3%B4nico%20N%C2%B0%2090056_2024.pdf
- Acórdão 2569/2018-TCU-Plenário: https://pesquisa.apps.tcu.gov.br/doc/acordao-completo/2569/2018/Plen%C3%A1rio
- ETP Software Integrado de Gestão Pública – Timbó/SC: https://www.timbo.sc.gov.br/wp-content/uploads/2024/07/ETP-Software-Integrado-de-Gestao-Publica.pdf
