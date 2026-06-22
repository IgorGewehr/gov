# M7 — Repasses federais e prestação de contas: Saúde, Educação, Assistência Social

> Tensorroot.Gov — ERP GovTech municipal (RS). Pesquisa de preparação do M7.
> Domínios: **Saúde**, **Educação**, **Assistência Social** — os três que recebem
> repasses federais **fundo a fundo** e estão sujeitos a **mínimos constitucionais**.
> Data da pesquisa: 2026-06-22.
>
> **REGRA (CLAUDE.md §16):** nenhum percentual, prazo, leiaute ou nomenclatura abaixo
> deve ser hard-coded sem FONTE. Onde o valor é lei (mínimos, %), é **parametrizável por
> tenant** com default igual à lei vigente; onde há `[a confirmar]`, obter doc oficial
> antes de implementar.

---

## 0. Visão geral — como os 3 setores se ligam a repasses e prestação

Cada um dos três domínios tem o **mesmo padrão estrutural**, com nomes diferentes:

| Eixo | Saúde | Educação | Assistência Social |
|---|---|---|---|
| Fundo federal | **FNS** (Fundo Nacional de Saúde) | **FNDE** (Fundo Nacional de Desenvolvimento da Educação) | **FNAS** (Fundo Nacional de Assistência Social) |
| Fundo municipal (recebe) | FMS (Fundo Municipal de Saúde) | FME / conta FUNDEB municipal | FMAS (Fundo Municipal de Assistência Social) |
| Sistema de orçamento/contas setorial | **SIOPS** | **SIOPE** | **SuasWeb** (Demonstrativo Sintético) |
| Mínimo constitucional | **15% ASPS** (municípios) | **25% MDE** + **70% Fundeb** p/ profissionais | (não há mínimo % constitucional — cofinanciamento por piso) |
| Conselho (controle social) | **CMS** (Conselho Municipal de Saúde) | **CACS-FUNDEB** + Conselho Mun. de Educação | **CMAS** (Conselho Municipal de Assistência Social) |
| Instrumento que o conselho aprova | **RAG** (Relatório Anual de Gestão), via DigiSUS | parecer sobre prestação Fundeb | **Parecer** sobre Demonstrativo Sintético |
| Base legal do mínimo/repasse | LC 141/2012; EC 29 | Lei 14.113/2020; CF art. 212; EC 108 | Lei 8.742/1993 (LOAS); Dec. 7.788/2012 |

**Fluxo geral comum aos três:** União (fundo federal) → transferência **regular e automática
fundo a fundo** → fundo municipal (conta vinculada específica) → execução contábil no **PCASP**
(já temos no M2/M3) → demonstrativos de mínimos no **RREO** (anexos) → espelho nos sistemas
setoriais (**SIOPS/SIOPE/SuasWeb**) → aprovação pelo **conselho** → consolidação no **SICONFI**
(federal) e no **SIAPC/PAD** (TCE-RS, estadual). O ERP precisa: (a) **segregar fonte/recurso**
por bloco/piso, (b) **calcular os mínimos**, (c) **gerar os anexos do RREO**, (d) **alimentar/
exportar** para os sistemas setoriais, (e) **registrar o parecer do conselho**, (f) **fechar**
com SICONFI e SIAPC/PAD.

---

## 1. SAÚDE

### 1.1 Repasse fundo a fundo (FNS → FMS)
- Transferências do **Fundo Nacional de Saúde** aos fundos municipais por **blocos de
  financiamento** (estrutura reorganizada). Há o **Bloco de Manutenção das Ações e Serviços
  Públicos de Saúde** (custeio) e bloco de **investimento**. Atenção Primária reformulada pela
  **Portaria GM/MS nº 3.493/2024** (nova metodologia de cofinanciamento do Piso de Atenção
  Primária — componentes per capita, etc.), com efeitos financeiros a partir de mai/2024.
  → FONTE: https://bvsms.saude.gov.br/bvs/saudelegis/gm/2024/prt3493_11_04_2024.html
- Ressarcimento interfederativo e emendas parlamentares regulados em portarias próprias
  (ex.: Portaria GM/MS nº 6.212/2024; emendas: Portaria GM/MS nº 6.904/2025).
  → FONTE (emendas 2025): https://www.conass.org.br/conass-informa-n-48-2025-publicada-a-portaria-gm-n-6-904-...
- **Implicação ERP:** cada bloco/componente é uma **fonte/recurso vinculado** distinto no
  PCASP; o sistema deve rastrear saldo por bloco para o demonstrativo SIOPS e para a
  prestação ao FNS. `[a confirmar — lista atual e códigos dos blocos/componentes na Portaria de Consolidação GM/MS nº 6/2017 consolidada]`

### 1.2 Mínimo constitucional — 15% ASPS
- Municípios devem aplicar **mínimo de 15%** da receita de impostos e transferências em
  **Ações e Serviços Públicos de Saúde (ASPS)** — **LC 141/2012** (regulamenta EC 29).
  Percentual **parametrizável** (default 15% município).
  → FONTE: https://www.gov.br/saude/pt-br/acesso-a-informacao/siops
- Apuração e monitoramento pelo **SIOPS**.

### 1.3 SIOPS (orçamento/contas de saúde)
- **Sistema de Informações sobre Orçamentos Públicos em Saúde** — alimentação
  **obrigatória e bimestral**; declaração até **30 dias após o fim de cada bimestre**
  (LC 141/2012). Descumprimento → medidas administrativas (inclusive sobre transferências).
  → FONTE: https://www.gov.br/saude/pt-br/acesso-a-informacao/siops
  → Cartilha SIOPS 2025: http://siops.datasus.gov.br/Documentacao/cartilha_2025.pdf
- **Espelho no RREO:** o **Anexo 12** do RREO (Demonstrativo da Receita de Impostos e das
  Despesas Próprias com Saúde / ASPS) é o demonstrativo enviado ao SIOPS / Sistema.
  → FONTE: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2023_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf
- **Implicação ERP:** gerar Anexo 12 a partir do PCASP e **exportar/transmitir** ao SIOPS.
  `[a confirmar — formato de importação/transmissão do SIOPS (DATASUS): arquivo, API ou digitação]`

### 1.4 Conselho Municipal de Saúde (CMS) + RAG
- O **Relatório Anual de Gestão (RAG)** é o principal instrumento de prestação de contas do
  SUS (LC 141/2012). Elaborado no **DigiSUS Gestor — Módulo Planejamento (DGMP)** (substituiu
  o **SARGSUS**, obrigatório desde 2018). Cabe ao **CMS analisar e aprovar** o RAG e emitir
  **parecer conclusivo**.
  → FONTE: https://conselho.saude.gov.br/relatorios-anuais-de-gestao
  → DF (descrição RAG/LC 141): https://info.saude.df.gov.br/transparencia-e-prestacao-de-contas/relatorio-anual-de-gestao-rag/
- **Implicação ERP:** o sistema é fonte dos **dados financeiros** que alimentam o RAG (execução
  por bloco, % ASPS, restos a pagar de saúde) e deve **armazenar o parecer do CMS** (data,
  resolução, status aprovado/com ressalvas/rejeitado) como evidência de prestação.
  `[a confirmar — se DGMP aceita import de dados financeiros ou se é digitação manual]`

---

## 2. EDUCAÇÃO

### 2.1 FUNDEB (Lei 14.113/2020)
- Fundo de natureza contábil, distribuição por **matrículas** (Censo Escolar/INEP), com
  valores por aluno: **VAAF**, **VAAT** e **VAAR**. Complementação da União **≥ 23%** do total
  do fundo (VAAF + VAAT + VAAR).
  → FONTE (lei): http://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14113.htm
  → Nota técnica CNM VAAF/VAAT/VAAR ed. 03/2024: https://cnm.org.br/biblioteca/download/15227
- **Mínimo de 70%** dos recursos do Fundeb para **remuneração dos profissionais da educação
  básica** (subiu de 60% para 70% com o novo Fundeb). **Parametrizável** (default 70%).
  → FONTE: https://www.cenpec.org.br/noticias/fundeb-complementacoes-uniao
- **Implicação ERP:** conta/fonte **vinculada Fundeb** específica; cálculo do **% pago a
  profissionais** sobre receita do fundo no exercício; rastrear VAAT (que tem subvínculos de
  aplicação). `[a confirmar — subvínculos VAAT (ex.: % mínimo em educação infantil) e regras de superávit/saldo até 10% no exercício seguinte, art. 25 da Lei 14.113]`

### 2.2 Mínimo constitucional — 25% MDE
- **Mínimo de 25%** da receita de impostos e transferências em **Manutenção e Desenvolvimento
  do Ensino (MDE)** — **CF art. 212**. **Parametrizável** (default 25%).
  → FONTE: https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope

### 2.3 SIOPE (orçamento/contas de educação)
- **Sistema de Informações sobre Orçamentos Públicos em Educação** — operado pelo **FNDE**,
  institucionalizado pela **Portaria MEC nº 844/2008**. Alimentação **bimestral**.
  → FONTE: https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope
- **Espelho no RREO:** **Anexo 8** do RREO (Demonstrativo das Receitas e Despesas com MDE) é
  o que se envia ao SIOPE.
  → FONTE: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2023_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf
- SIOPE integra com STN, INEP, FNDE; interface com SICONFI (Matriz) **em estudo** — ou seja, o
  ente ainda transmite ao SIOPE.
  → FONTE: https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope
- **Implicação ERP:** gerar Anexo 8 do PCASP e **transmitir** ao SIOPE.
  `[a confirmar — formato/leiaute de import do SIOPE: arquivo ou digitação; recibo bimestral]`

### 2.4 Conselhos de Educação
- **CACS-FUNDEB** (Conselho de Acompanhamento e Controle Social do FUNDEB) — acompanha
  distribuição, transferência e aplicação dos recursos do fundo; registra atuação no **SISCACS**
  (FNDE). Há também o **Conselho Municipal de Educação**.
  → FONTE (SISCACS): https://www.gov.br/fnde/pt-br/assuntos/sistemas/cacs-fundeb
- **Implicação ERP:** fornecer ao CACS os demonstrativos de execução do Fundeb e **armazenar o
  parecer** do conselho. `[a confirmar — se há instrumento anual formal de parecer do CACS análogo ao RAG]`

---

## 3. ASSISTÊNCIA SOCIAL

### 3.1 Repasse fundo a fundo (FNAS → FMAS)
- **SUAS**: cofinanciamento **União–Estado–Município** por **transferências regulares e
  automáticas fundo a fundo** (LOAS — Lei 8.742/1993; Lei 9.604/1998; **Decreto 7.788/2012**).
  Recursos organizados por **pisos** (Proteção Social **Básica** e **Especial** de média/alta
  complexidade), calculados por critérios de oferta de serviços. Há ainda **IGD-SUAS** e
  **IGD-PBF** (índices de gestão descentralizada que condicionam repasses de apoio à gestão).
  → FONTE: https://www.gov.br/mds/pt-br/acoes-e-programas/suas/gestao-do-suas/financiamento-1/fundo-a-fundo
  → FONTE (recursos FNAS): https://www.gov.br/sri/pt-br/.../recursos-do-fundo-nacional-de-assistencia-social-2013-fnas
- **Sem mínimo constitucional %** (diferente de saúde/educação) — o vínculo é por **piso** e
  pela contrapartida do cofinanciamento. `[a confirmar — pisos e blocos vigentes e códigos (Proteção Básica / Especial MC / Especial AC; Programa Primeira Infância no SUAS; etc.)]`
- **Implicação ERP:** cada **piso/bloco** = fonte vinculada no PCASP do FMAS; rastrear saldo,
  aplicação e **saldo reprogramado** (recursos não usados podem ser reprogramados, não devolvidos
  automaticamente). `[a confirmar — regra de reprogramação de saldo e parecer do conselho exigido]`

### 3.2 Prestação de contas — Demonstrativo Sintético (SuasWeb)
- A prestação é o **Demonstrativo Sintético Anual da Execução Físico-Financeira**, preenchido no
  sistema **SuasWeb** pelo gestor e com **Parecer do Conselho** (CMAS).
  → FONTE (manual FNAS): https://fnas.mds.gov.br/wp-content/uploads/2024/10/Manual-de-Preenchimento-do-Demonstrativo-Sintetico-4.pdf
- Fluxo de prazos (exemplo do exercício 2022, **Portaria SNAS nº 95/2023**): **gestor** preenche
  até 31/01 do ano seguinte+1; **conselho** registra **parecer** ~1 mês depois.
  → FONTE: https://suasfacil.com.br/demonstrativo-sintetico-da-execucao-fisico-financeira-exercicio-2022/
  `[a confirmar — prazos vigentes para o exercício corrente; portaria anual que fixa as datas]`
- **Implicação ERP:** consolidar execução física+financeira por piso/serviço para alimentar o
  Demonstrativo e **armazenar o parecer do CMAS**.

### 3.3 Conselho Municipal de Assistência Social (CMAS)
- O **CMAS** emite **parecer** sobre o Demonstrativo Sintético (passo obrigatório da prestação no
  SuasWeb). Controle social do SUAS.
  → FONTE: manual FNAS (acima).

---

## 4. RELAÇÃO COM A CONTABILIDADE / PCASP (já temos — M2/M3)

- Os três repasses entram como **receita orçamentária com fonte/recurso vinculado**. O PCASP
  (M2) precisa de **classificação por fonte** suficiente para segregar: blocos de saúde, conta
  Fundeb e MDE, pisos do SUAS. **Mecanismo já existente** a estender: tabela de
  fontes/destinação de recursos por tenant.
- Os **mínimos** (15% ASPS, 25% MDE, 70% Fundeb-profissionais) são **cálculos derivados da
  execução** — devem ser computados sobre a base de receitas de impostos/transferências e
  despesas liquidadas/empenhadas conforme as regras de cada anexo do RREO. **Parametrizar %**.
- Os **anexos do RREO** que materializam os mínimos:
  - **Anexo 8** — MDE → vai ao **SIOPE**.
  - **Anexo 12** — ASPS/Saúde → vai ao **SIOPS**.
  → FONTE: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2023_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf
- **RREO** é **bimestral**, publicado em **até 30 dias** após o bimestre (municípios < 50 mil hab.
  podem optar por parte semestral). **DCA** (Declaração de Contas Anuais) ao SICONFI até **30/04**.
  → FONTE: https://www.gov.br/sri/pt-br/backup-secretaria-de-governo/portalfederativo/guiainicio/prefeito/trilhas-100-dias-de-governo/rgf-e-rreo
  → FONTE (DCA 30/04): https://cnm.org.br/comunicacao/noticias/municipios-tem-ate-30-de-abril-para-enviar-declaracao-de-contas-anuais
- **Reuso:** o M4 (prestação TCE-RS + SICONFI) já gera RREO/RGF/MSC/DCA. O M7 **estende** o M4
  acrescentando os **vínculos setoriais** e as **exportações para SIOPS/SIOPE/SuasWeb**, mais o
  **registro de pareceres dos conselhos**.

---

## 5. O QUE VAI AO TCE-RS (SIAPC / PAD)

- O TCE-RS recebe dados via **SIAPC** (Sistema de Informações para Auditoria e Prestação de
  Contas); o **PAD** (Programa Autenticador de Dados) gera o **RVE** (Relatório de Validação e
  Encaminhamento) com **RREO** e **RGF**. Desde 2019, remessa **mensal** (Resolução TCE-RS
  nº 1099/2018); o pacote tem ~27 arquivos (orçamentário, financeiro, contábil, patrimonial,
  diário contábil, folha).
  → FONTE: https://tcers.tc.br/noticia/sistema-desenvolvido-pelo-tce-rs-qualifica-contas-dos-municipios-gauchos/
  → FONTE (leiaute SIAPC, MT Vol. V): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
  → FONTE (IN TCE-RS 13/2021 — RREO/RGF via PAD): atosoficiais.com.br/tcers/instrucao-normativa-n-13-2021-...
- **Mínimos saúde/educação** são acompanhados pelo TCE-RS a partir desses dados (RREO Anexos 8 e
  12 dentro do pacote SIAPC). `[a confirmar — campos específicos do leiaute SIAPC/PAD que carregam aplicação em saúde, educação e Fundeb; conferir MT Vol. V vigente para o exercício corrente]`
- **Implicação ERP:** o **M4 já produz o pacote SIAPC/PAD**; o M7 deve garantir que os dados de
  saúde/educação/assistência (fontes, despesas por função 10/12/08, vínculos) saiam corretos no
  mesmo pacote. **Não é um envio separado ao TCE para cada setor** — é o mesmo PAD mensal.

---

## 6. MATRIZ "ONDE / QUANDO ENVIAR"

| Dado | Sistema destino | Esfera | Periodicidade | Quem aprova |
|---|---|---|---|---|
| Execução ASPS (Anexo 12 RREO) | **SIOPS** (DATASUS/MS) | Federal | Bimestral, até 30 dias após bimestre | — (gestor) |
| Execução MDE (Anexo 8 RREO) | **SIOPE** (FNDE/MEC) | Federal | Bimestral | — (gestor) |
| Execução físico-financeira SUAS | **SuasWeb** (FNAS/MDS) | Federal | Anual + parecer | **CMAS** |
| RAG (saúde) | **DigiSUS DGMP** | Federal | Anual | **CMS** |
| Fundeb (acompanhamento) | **SISCACS** / SIOPE | Federal | conforme FNDE | **CACS-FUNDEB** |
| RREO / RGF / MSC / DCA | **SICONFI** (STN) | Federal | RREO bimestral; DCA até 30/04 | — |
| RREO/RGF + 27 arquivos (inclui mínimos) | **SIAPC/PAD** | Estadual (TCE-RS) | Mensal (desde 2019) | TCE-RS (auditoria) |

`[a confirmar — datas exatas de cada periodicidade no exercício 2026 e eventuais portarias anuais que as fixam (especialmente SUAS e emendas SUS)]`

---

## 7. PENDÊNCIAS CONSOLIDADAS [a confirmar — obter doc oficial]

1. **Blocos/componentes de financiamento da Saúde** vigentes e seus códigos (Portaria de
   Consolidação GM/MS nº 6/2017, texto consolidado pós-Portaria 3.493/2024).
2. **Formato de transmissão** SIOPS (DATASUS) e **SIOPE** (FNDE): arquivo/leiaute, API ou
   digitação manual; estrutura do recibo bimestral.
3. **Subvínculos do VAAT** e regra de **saldo/superávit Fundeb** (art. 25, Lei 14.113/2020).
4. **Pisos/blocos do SUAS** vigentes e códigos; regra de **reprogramação de saldo**.
5. **Prazos vigentes (2026)** do Demonstrativo Sintético SuasWeb e da portaria SNAS anual.
6. **Campos do leiaute SIAPC/PAD (MT Vol. V vigente)** que carregam aplicação em saúde,
   educação e Fundeb — para garantir geração correta no pacote do M4.
7. **DigiSUS DGMP**: aceita import de dados financeiros do ERP ou é digitação manual?
8. **Instrumento formal de parecer do CACS-FUNDEB** (existe documento anual análogo ao RAG?).

---

## Fontes principais (URLs)

- Portaria GM/MS nº 3.493/2024 (APS/cofinanciamento): https://bvsms.saude.gov.br/bvs/saudelegis/gm/2024/prt3493_11_04_2024.html
- SIOPS (MS): https://www.gov.br/saude/pt-br/acesso-a-informacao/siops
- Cartilha SIOPS 2025: http://siops.datasus.gov.br/Documentacao/cartilha_2025.pdf
- RAG / Conselho Nacional de Saúde: https://conselho.saude.gov.br/relatorios-anuais-de-gestao
- Lei 14.113/2020 (FUNDEB): http://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14113.htm
- Nota técnica CNM VAAF/VAAT/VAAR 03/2024: https://cnm.org.br/biblioteca/download/15227
- SIOPE (FNDE): https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope
- SISCACS / CACS-FUNDEB (FNDE): https://www.gov.br/fnde/pt-br/assuntos/sistemas/cacs-fundeb
- SUAS fundo a fundo (MDS): https://www.gov.br/mds/pt-br/acoes-e-programas/suas/gestao-do-suas/financiamento-1/fundo-a-fundo
- Manual Demonstrativo Sintético (FNAS): https://fnas.mds.gov.br/wp-content/uploads/2024/10/Manual-de-Preenchimento-do-Demonstrativo-Sintetico-4.pdf
- Regras RREO (SICONFI/STN): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2023_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf
- DCA até 30/04 (CNM): https://cnm.org.br/comunicacao/noticias/municipios-tem-ate-30-de-abril-para-enviar-declaracao-de-contas-anuais
- SIAPC/PAD leiaute (TCE-RS, MT Vol. V): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- SIAPC remessa mensal (TCE-RS): https://tcers.tc.br/noticia/sistema-desenvolvido-pelo-tce-rs-qualifica-contas-dos-municipios-gauchos/
