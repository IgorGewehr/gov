# Estudo — Mapa Federal Setorial (Saúde, Educação, Assistência, Trabalho)

> **Escopo:** sistemas federais setoriais com os quais um ERP municipal (Tensorroot.Gov) precisa dialogar — direta (integração de dados) ou indiretamente (a aplicação de recursos exige o envio tempestivo a esses sistemas, sob pena de bloqueio de repasse). Para cada eixo: **o que o município envia/consome**, **periodicidade** e **vínculo com repasses/financiamento**.
>
> **Convenções (CLAUDE.md §16):** fontes oficiais sempre que possível; itens incertos marcados **[a confirmar]** — antes de codar qualquer integração, confirmar layout/versão/leiaute vigente no portal oficial do sistema.
>
> **Data da pesquisa:** 2026-06 · **Relação com M7** (Saúde/Educação/Assistência/repasses) e **M5** (RH/eSocial).

---

## 0. Visão geral — dois eixos de relacionamento

1. **Eixo financeiro/fiscal (orçamento e contabilidade):** sistemas que recebem a **execução orçamentária setorial** para verificar pisos constitucionais e habilitar/condicionar transferências — **SIOPS** (saúde) e **SIOPE** (educação). Alimentam-se da contabilidade (PCASP/DCASP — já implementado em M2/M3) e são primos do SICONFI (M3/M4).
2. **Eixo operacional/programático:** sistemas que recebem **produção, cadastros e atendimentos** setoriais que **condicionam o cofinanciamento federal fundo a fundo** — RNDS/SISAB/CNES/SI-PNI (saúde), Educacenso + SiGPC (educação/FNDE), CadÚnico/RMA/Censo SUAS/SUASWeb (assistência) e **eSocial** (trabalho/RH, transversal).

A regra de ouro federal: **repasse fundo a fundo é regular e automático enquanto o ente mantém conselhos, planos e prestação de contas em dia**; a *não* alimentação tempestiva dos sistemas setoriais gera **bloqueio/suspensão de recursos** e inscrição em cadastros restritivos (ex.: CAUC). [SIOPS-MS, FNS-MS, FNDE — ver fontes]

---

## 1. SAÚDE

### 1.1 SIOPS — Sistema de Informações sobre Orçamentos Públicos em Saúde
- **Gestor:** Ministério da Saúde / DATASUS.
- **O que o município envia:** dados da **execução orçamentária e financeira** das despesas com Ações e Serviços Públicos de Saúde (ASPS) — receitas próprias e despesas, para apurar o **mínimo constitucional** (15% da receita de impostos + transferências, art. 198 CF / LC 141/2012).
- **Periodicidade:** **bimestral**, declaração até **30 dias após o encerramento de cada bimestre** (LC 141/2012). A partir de 2013 passou de anual para bimestral. Transmissão eletrônica via aplicativo de autopreenchimento para o banco do DATASUS.
- **Vínculo com repasses/financiamento:** instrumento de **controle do piso da saúde**; o descumprimento sujeita o ente a sanções fiscais e é insumo de fiscalização de TCEs e Ministério Público. Indicadores gerados automaticamente.
- **Para o Tensorroot:** alimentado pela contabilidade (M2/M3); candidato a **exportação automática** análoga ao SICONFI. Confirmar leiaute/aplicativo vigente. **[a confirmar: API/leiaute de importação vs. digitação no aplicativo]**

### 1.2 RNDS — Rede Nacional de Dados em Saúde
- **Gestor:** Ministério da Saúde (SEIDIGI).
- **O que é:** plataforma nacional de **interoperabilidade** (padrão FHIR) para compartilhamento seguro e padronizado de dados clínicos.
- **O que o município envia/consome:** estabelecimentos com **CNES válido** solicitam credencial de acesso às APIs no **Portal de Serviços do DATASUS**; o e-SUS APS (PEC) integra-se à RNDS via certificado. Eventos clínicos (ex.: vacinação, exames) são enviados.
- **Periodicidade:** muitos fluxos são **diários/em tempo quase real**; a obrigação consolidada de envio de relatórios é mensal. **[a confirmar por tipo de evento]**
- **Vínculo com financiamento:** infraestrutura habilitadora; o cofinanciamento depende dos dados que trafegam por ela (ver SISAB/SI-PNI).
- **Para o Tensorroot:** integração de saúde é majoritariamente **escopo de prontuário/e-SUS** (fora do núcleo ERP); o ERP atua na ponta **financeira/repasse** e no CNES como cadastro de unidades. **[a confirmar limite de escopo no M7]**

### 1.3 SISAB / e-SUS APS — Atenção Primária
- **Gestor:** Ministério da Saúde / SAPS (Secretaria de Atenção Primária).
- **O que o município envia:** **cadastros vinculados** de população às equipes (eSF/eAP), produção e indicadores da APS, via e-SUS APS (PEC) → SISAB.
- **Periodicidade:** envio contínuo; consolidação **mensal**; os **cadastros são analisados por trimestre** (capitação recalculada a cada 4 quadrimestres/períodos). **[a confirmar nomenclatura de ciclos do modelo vigente — pós Previne Brasil/novo cofinanciamento]**
- **Vínculo com financiamento:** **direto e crítico.** O modelo de cofinanciamento federal da APS usa **capitação ponderada** (população cadastrada no SISAB ponderada por vulnerabilidade socioeconômica, perfil etário e classificação geográfica IBGE), **incentivo por desempenho/qualidade** e ações estratégicas. Valor **transferido mensalmente** e recalculado periodicamente. Requisitos: credenciamento das equipes pelo MS + registro no **SCNES** pelo gestor municipal.
- **Para o Tensorroot:** **fora do núcleo ERP** (é sistema clínico-assistencial); relevante apenas como **fonte do montante de repasse** a ser contabilizado e prestado contas. **[a confirmar]**

### 1.4 CNES — Cadastro Nacional de Estabelecimentos de Saúde
- **Gestor:** Ministério da Saúde / DATASUS.
- **O que o município envia:** cadastro e manutenção de **estabelecimentos, equipes e profissionais** de saúde.
- **Periodicidade:** atualização **mensal** (competência); pré-requisito permanente.
- **Vínculo com financiamento:** **condicionante** de quase todo repasse SUS — equipes precisam estar registradas no SCNES para gerar cofinanciamento (APS, MAC etc.).

### 1.5 SI-PNI — Sistema de Informação do Programa Nacional de Imunizações
- **Gestor:** Ministério da Saúde.
- **O que o município envia:** **doses aplicadas** (imunização), hoje preferencialmente via e-SUS APS / nova plataforma integrada à RNDS.
- **Periodicidade:** registro **contínuo**; consolidação mensal.
- **Vínculo com financiamento:** coberturas vacinais compõem **indicadores de desempenho** que afetam incentivos da APS e ações específicas de vigilância. **[a confirmar peso no cofinanciamento vigente]**

### 1.6 FNS — Fundo Nacional de Saúde (repasse fundo a fundo)
- **Gestor:** Ministério da Saúde / FNS.
- **Modalidade:** **fundo a fundo**, do FNS para o **Fundo Municipal de Saúde**, regular e automática, **sem convênio**.
- **Blocos de financiamento (desde 2017):** **Bloco de Custeio** (APS, vigilância, assistência farmacêutica, MAC) e **Bloco de Investimento** (obras/reformas/equipamentos).
- **Condicionantes:** ente deve manter **Conselho de Saúde** e **Plano de Saúde** atualizados; a aplicação é prestada contas no **RAG — Relatório Anual de Gestão** (Lei 8.142/1990; Dec. 1.651/1995), apreciado pelo Conselho de Saúde. Transferência pactuada na CIT e aprovada pelo CNS.
- **Para o Tensorroot:** o ERP **contabiliza o ingresso** (receita de transferência), **vincula a despesa ao bloco/fonte** e **gera insumos para o RAG e a prestação de contas**. Forte interseção com M2/M3/M4.

---

## 2. EDUCAÇÃO

### 2.1 SIOPE — Sistema de Informações sobre Orçamentos Públicos em Educação
- **Gestor:** **FNDE** (operação) / **INEP** (apoio técnico) — sistema do FNDE.
- **O que o município envia:** **execução orçamentária e financeira** das despesas com Manutenção e Desenvolvimento do Ensino (MDE) e do **FUNDEB**, para apurar o **mínimo constitucional** (25% — art. 212 CF) e a aplicação do FUNDEB.
- **Periodicidade:** **bimestral** (espelho do SIOPS). **[a confirmar prazo exato de cada bimestre no manual FNDE vigente]**
- **Vínculo com repasses/financiamento:** monitora aplicação de recursos do **FUNDEB** (>70% dos recursos da educação básica) e o piso constitucional; **condição de regularidade** para transferências. Subsidia controle social e fiscalização.
- **Para o Tensorroot:** **gêmeo do SIOPS** — alimentado pela contabilidade (M2/M3); candidato a exportação automática. **[a confirmar leiaute/importação]**

### 2.2 Educacenso / Censo Escolar — INEP
- **Gestor:** **INEP**, de forma descentralizada (União + estados + municípios).
- **O que o município envia:** dados de **Escola, Turma, Aluno e Profissional Escolar** das redes municipais, via sistema web **Educacenso**.
- **Periodicidade:** **anual.** Data de referência: **última quarta-feira de maio** (no Censo 2026, **27 de maio**); coleta vai até ~**31 de julho** (1ª etapa). Há 2ª etapa (situação do aluno) no início do ano seguinte. **[a confirmar datas exatas do ciclo vigente]**
- **Vínculo com repasses/financiamento:** **base de cálculo do FUNDEB** e de praticamente todos os programas do FNDE (matrículas determinam coeficientes). Censo subnotificado = **menos recursos**. Insumo de PNAE, PNATE, PDDE, transporte etc.
- **Para o Tensorroot:** depende de um **módulo de gestão escolar/educação** (matrícula, lotação). Núcleo ERP atua no **vínculo profissional** (RH/lotação — M5) e na **conversão matrícula→coeficiente de repasse**. **[a confirmar fronteira módulo educacional vs. ERP no M7]**

### 2.3 FNDE — PNAE, PNATE, PDDE e SiGPC (prestação de contas)
- **Gestor:** **FNDE**.
- **Programas:**
  - **PNAE** — Programa Nacional de Alimentação Escolar (repasse fundo a fundo, baseado em matrículas do Censo).
  - **PNATE** — Programa Nacional de Apoio ao Transporte do Escolar (idem).
  - **PDDE** — Programa Dinheiro Direto na Escola (repasse às unidades executoras).
- **O que o município envia:** **prestação de contas** anual da aplicação no **SiGPC — Contas Online** (e ferramentas correlatas como SIGECON/BB Ágil). **[a confirmar consolidação de sistemas vigente]**
- **Periodicidade:** **anual** — prazo usual **30 de abril** do exercício seguinte (sujeito a prorrogações por portaria MEC/FNDE).
- **Vínculo com repasses/financiamento:** **crítico.** O não envio da prestação de contas **inscreve o município no CAUC** e **bloqueia transferências voluntárias** e a continuidade dos repasses. Repasses do PNAE/PNATE têm **base no Censo Escolar**.
- **Para o Tensorroot:** o ERP **contabiliza ingressos/aplicações por programa/fonte** e gera os **demonstrativos da prestação de contas SiGPC** (forte interseção M4). **[a confirmar se há importação de leiaute no SiGPC ou só digitação]**

---

## 3. ASSISTÊNCIA SOCIAL (MDS)

### 3.1 CadÚnico — Cadastro Único para Programas Sociais
- **Gestor:** **MDS** (operação técnica Caixa).
- **O que o município faz:** **cadastra e atualiza** famílias de baixa renda (operação descentralizada pela gestão municipal); ações de cadastramento/atualização contam como **atendimentos** no RMA do CRAS.
- **Periodicidade:** atualização cadastral **a cada 2 anos** (no mínimo), além de **Averiguação (AVE)** e **Revisão (REV)** cadastrais anuais. **[a confirmar regras 2026 — houve alterações no funcionamento do CadÚnico]**
- **Vínculo com repasses/financiamento:** base do **Bolsa Família** e de dezenas de programas (tarifa social, BPC etc.). A **qualidade/atualidade do cadastro** alimenta a **Taxa de Atualização Cadastral (TAC)**, componente do **IGD-PBF**.

### 3.2 IGD-PBF e IGD-SUAS — Índices de Gestão Descentralizada
- **O que medem:** desempenho da gestão municipal do CadÚnico/Bolsa Família (**IGD-PBF**: TAC + TAFE — frequência escolar + TAAS — agenda de saúde) e da gestão do SUAS (**IGD-SUAS**).
- **Vínculo com repasses:** **recurso fundo a fundo** do FNAS ao Fundo Municipal de Assistência Social proporcional ao índice; **atingir índices mínimos é condição de repasse**. Valor de referência IGD-PBF ajustado por portaria (ex.: **Portaria MDS 1.151/2026** atualizou referência **[a confirmar valor vigente]**).
- **Periodicidade:** apuração **mensal** dos índices; repasse correspondente.

### 3.3 RMA — Registro Mensal de Atendimentos
- **Gestor:** MDS / Vigilância Socioassistencial (Resoluções CIT 04/2011 e 20/2013).
- **O que o município envia:** volume e tipo de atendimentos de **CRAS, CREAS e Centro POP**.
- **Periodicidade:** **mensal.**
- **Vínculo com financiamento:** alimenta a Vigilância Socioassistencial e **compõe o IGD-SUAS / condicionantes de cofinanciamento**. **[a confirmar peso exato]**

### 3.4 Censo SUAS
- **Gestor:** MDS.
- **O que o município envia:** caracterização anual das **unidades, serviços, recursos humanos e conselhos** do SUAS (CRAS, CREAS, Centro POP, unidades de acolhimento, Conselho, Fundo, gestão).
- **Periodicidade:** **anual** (questionários eletrônicos). **[a confirmar janela de coleta vigente]**
- **Vínculo com financiamento:** insumo de **planejamento, pactuação e cofinanciamento** do SUAS.

### 3.5 SUASWeb — execução e prestação de contas fundo a fundo
- **Gestor:** MDS / Rede SUAS.
- **O que o município faz:** registra a **prestação de contas (Demonstrativo Sintético da Execução Físico-Financeira)** dos recursos transferidos fundo a fundo; o **Conselho Municipal de Assistência Social** registra a **apreciação** no mesmo sistema.
- **Blocos de financiamento SUAS:** Proteção Social Básica; Proteção Social Especial; Gestão do SUAS; Gestão do Bolsa Família e do CadÚnico.
- **Periodicidade:** demonstrativo **anual** (por exercício); recursos repassados de forma regular.
- **Vínculo com repasses/financiamento:** **condição de continuidade** dos repasses fundo a fundo do FNAS; sem demonstrativo aprovado, o repasse pode ser bloqueado.
- **Para o Tensorroot:** o ERP **contabiliza ingressos/aplicações por bloco/fonte** (Fundo Municipal de Assistência Social) e gera o **demonstrativo físico-financeiro** (interseção M4). **[a confirmar importação vs. digitação no SUASWeb]**

---

## 4. TRABALHO / RH — eSocial (setor público)

### 4.1 eSocial — Grupo 4 (entes federativos / órgãos públicos)
- **Gestor:** Comitê Gestor do eSocial (RFB/MTE/INSS/ME).
- **O que o município envia:** eventos de **tabelas** (cadastros do empregador/órgão e rubricas), **não periódicos** (admissões/vínculos de servidores, alterações, desligamentos), **periódicos** (folha de pagamento — remunerações e apurações) e **SST** (Saúde e Segurança do Trabalho).
- **Faseamento (Grupo 4 — órgãos públicos):**
  - **21/07/2021** — Fase 1: eventos de tabela (cadastro do empregador).
  - **22/11/2021** — Fase 2: eventos não periódicos (servidores e vínculos).
  - **22/08/2022** — Fase 3: eventos periódicos (**folha de pagamento**, a partir da competência agosto/2022).
  - **01/01/2023** — Fase 4: eventos de **SST**.
- **Periodicidade:** **eventos periódicos mensais** (folha); **não periódicos** no prazo do fato gerador (ex.: admissão antes do início; desligamento conforme prazo legal).
- **Envio descentralizado:** órgãos da administração direta, autárquica e fundacional podem enviar **de forma descentralizada** (cada UG/órgão como unidade administrativa). Relevante para a modelagem **multi-UO** já existente (M1).
- **Vínculo com repasses/financiamento:** **indireto, mas crítico** — o eSocial unifica obrigações acessórias e o recolhimento de **contribuições previdenciárias (RGPS/RPPS) e FGTS**; irregularidade afeta **CND/regularidade fiscal** (e, por consequência, transferências voluntárias) e expõe o ente a autuações.
- **Para o Tensorroot:** **núcleo do M5 (RH/folha + eSocial + ponto).** O ERP é a **fonte** dos eventos; precisa do leiaute vigente do **MOS — Manual de Orientação do eSocial** e dos XSDs da versão corrente. **[a confirmar versão de leiaute vigente — confirmar S-1.x atual antes de codar]**

---

## 5. Síntese — periodicidade × vínculo de repasse

| Sistema | Eixo | Periodicidade | Vínculo com repasse | Relação Tensorroot |
|---|---|---|---|---|
| **SIOPS** | Fiscal/saúde | Bimestral | Piso 15% saúde (LC 141) | M2/M3 → export. automática |
| **RNDS** | Operacional/saúde | Contínuo/mensal | Habilitador | Fora do núcleo (e-SUS) |
| **SISAB / e-SUS APS** | Operacional/saúde | Mensal; cadastro trimestral | Capitação ponderada (APS) | Fonte do montante de repasse |
| **CNES/SCNES** | Cadastral/saúde | Mensal | Condicionante de quase todo repasse SUS | Cadastro de unidades |
| **SI-PNI** | Operacional/saúde | Contínuo/mensal | Indicadores de desempenho APS | Fora do núcleo |
| **FNS (fundo a fundo)** | Financeiro/saúde | Regular; RAG anual | Custeio + Investimento; RAG/Conselho | M2/M3/M4 |
| **SIOPE** | Fiscal/educação | Bimestral | Piso 25% MDE + FUNDEB | M2/M3 → export. automática |
| **Educacenso** | Cadastral/educação | Anual (ref. maio) | Base do FUNDEB e do FNDE | Módulo educação + RH |
| **PNAE/PNATE/PDDE (SiGPC)** | Financeiro/educação | Prest. contas anual (~30/abr) | CAUC se atrasar; base Censo | M4 (prestação de contas) |
| **CadÚnico** | Cadastral/assistência | Atualização ≥ 2 anos + AVE/REV | Base Bolsa Família; TAC→IGD-PBF | Módulo assistência |
| **IGD-PBF / IGD-SUAS** | Financeiro/assistência | Apuração mensal | Repasse proporcional ao índice | M2/M3/M4 |
| **RMA** | Operacional/assistência | Mensal | Vigilância; compõe condicionantes | Módulo assistência |
| **Censo SUAS** | Cadastral/assistência | Anual | Planejamento/pactuação/cofinanciamento | Módulo assistência |
| **SUASWeb** | Financeiro/assistência | Demonstrativo anual | Continuidade do fundo a fundo | M4 |
| **eSocial (G4)** | Trabalho/RH | Periódicos mensais; não periódicos por fato gerador | CND/regularidade; previdência/FGTS | M5 (núcleo) |

---

## 6. Implicações para o Tensorroot.Gov

1. **Padrão "espelho do SICONFI":** SIOPS e SIOPE são contábeis e bimestrais — reusar a fábrica de **exportadores derivados da contabilidade** (M2/M3) e as fitness functions já existentes. Prioridade no M7.
2. **Repasses fundo a fundo (FNS/FNAS/FNDE):** o ERP deve **modelar fonte/bloco/programa** no PCASP para rastrear ingresso → aplicação → prestação de contas (SUASWeb, SiGPC, RAG). Interseção forte com **M4**.
3. **Sistemas operacionais clínicos/assistenciais (RNDS, SISAB, SI-PNI)** ficam, em regra, **fora do núcleo ERP** — o ERP consome deles o **valor do repasse** e o vincula contabilmente. Definir fronteira no design do M7. **[a confirmar]**
4. **eSocial é M5** e exige confirmar o **leiaute/versão vigente do MOS** e a modelagem **multi-UO/descentralizada** (já suportada por M1).
5. **Risco de bloqueio de recursos** (CAUC) é o driver de negócio: a tempestividade de SiGPC/SUASWeb/SIOPS/SIOPE/eSocial é **requisito não funcional crítico** (alertas/calendário de obrigações).
6. **Antes de codar qualquer integração:** confirmar leiaute, versão e modalidade (API vs. digitação) no portal oficial — vários sistemas só aceitam **digitação** (não importação), o que muda o desenho do conector. **[a confirmar caso a caso]**

---

## Fontes

**Saúde**
- SIOPS — Ministério da Saúde: https://www.gov.br/saude/pt-br/acesso-a-informacao/siops
- SIOPS — Nova SAGE/MS: https://novasage.saude.gov.br/informacoes-orcamentarias/sistema-de-informacoes-sobre-orcamentos-publicos-em-saude-siops
- SIOPS — DATASUS: http://siops.datasus.gov.br/Documentacao/cartilha_2020_2ed.pdf
- RNDS — Ministério da Saúde (SEIDIGI): https://www.gov.br/saude/pt-br/composicao/seidigi/rnds
- RNDS — FAQ WEB Atendimento SUS: https://webatendimento.saude.gov.br/faq/rnds
- Portal de Serviços do DATASUS: https://servicos-datasus.saude.gov.br/
- SISAB / Financiamento APS — manual MS: https://sisaps.saude.gov.br/sistemas/sisab/docs/manual/utilizando-sistema/
- Cofinanciamento federal da APS — MS/SAPS: https://aps.saude.gov.br/gestor/financiamento/componentesfinanciamento/
- FAQ novo modelo de cofinanciamento APS — MS: https://www.gov.br/saude/pt-br/composicao/saps/esf/faq-novo-modelo-de-cofinanciamento-federal-da-aps
- CNES — DATASUS: http://cnes.datasus.gov.br/
- FNS — Fundo a Fundo (MS): https://portalfns.saude.gov.br/fundo-a-fundo/
- FNS — Sobre o FNS (MS): https://portalfns.saude.gov.br/sobre-o-fns/

**Educação**
- SIOPE — FNDE: https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope
- SIOPE — "O que é" (FNDE): https://www.fnde.gov.br/siope/o_que_e.jsp
- Censo Escolar — INEP: https://www.gov.br/inep/pt-br/areas-de-atuacao/pesquisas-estatisticas-e-indicadores/censo-escolar
- Cronograma Censo Escolar 2026 — INEP: https://www.gov.br/inep/pt-br/centrais-de-conteudo/noticias/censo-escolar/inep-divulga-cronograma-do-censo-escolar-da-educacao-basica-2026
- Educacenso (sistema): https://educacenso.inep.gov.br/
- FNDE — Prestação de contas (SiGPC): https://www.fnde.gov.br/prestacao-de-contas/prestacao-de-contas-espaco-sigpc/material-de-apoio
- PNAE — Prestação de contas (FNDE): https://www.fnde.gov.br/programas/pnae/pnae-prestacao-de-contas

**Assistência Social (MDS)**
- RMA — MDS: https://www.gov.br/mds/pt-br/acoes-e-programas/suas/gestao-do-suas/vigilancia-socioassistencial-1/registro-mensal-de-atendimentos-2013-rma
- IGD — MDS: https://www.gov.br/mds/pt-br/cadunico/igd-indice-de-gestao-descentralizada-1
- CadÚnico (metadados) — IBGE/MDS: https://ces.ibge.gov.br/base-de-dados/metadados/mds/cadastro-unico-dos-programas-sociais-cadunico.html
- Recursos do FNAS — Portal Federativo: https://www.gov.br/sri/pt-br/backup-secretaria-de-governo/portalfederativo/agenda-do-prefeito-brasil/guiatermino/areas-tecnicas/assistencia-social/recursos-do-fundo-nacional-de-assistencia-social-2013-fnas
- SUASWeb — Manual Demonstrativo (FNAS/MDS): https://fnas.mds.gov.br/wp-content/uploads/2019/09/Manual-Demonstrativo-2018-Final-revisada.pdf
- Novas regras CadÚnico/Bolsa Família 2026 (MDS): https://www.gov.br/mds/pt-br/noticias-e-conteudos/desenvolvimento-social

**Trabalho / eSocial**
- eSocial — Implantação para Órgãos Públicos (gov.br): https://www.gov.br/esocial/pt-br/noticias/implantado-o-esocial-para-os-orgaos-publicos
- eSocial — Manual de Orientação (MOS), gov.br: https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-1-consolidada-ate-a-no-s-1-1-02-2023.pdf

> **Observação metodológica:** itens **[a confirmar]** referem-se a prazos exatos de ciclo, versões de leiaute e modalidade de entrada (API vs. digitação), que mudam por portaria. Antes de implementar qualquer conector no M5/M7, validar no portal oficial do sistema-alvo a versão vigente (CLAUDE.md §16).
