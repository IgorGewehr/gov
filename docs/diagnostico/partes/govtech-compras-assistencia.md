# Diagnóstico GovTech — Compras/Licitações e Assistência Social

> Pesquisa para Tensorroot.Gov (ERP GovTech multi-tenant, .NET 8 + React/Vite).
> Foco: obrigações legais, formatos/layouts, integrações, prazos e fontes oficiais.
> Piloto: Maximiliano de Almeida/RS. Preocupações do dono: Contabilidade e Prestação de Contas ao TCE.
> Data da pesquisa: 2026-06.

---

## PARTE 1 — COMPRAS / LICITAÇÕES (Lei 14.133/2021)

A Nova Lei de Licitações (Lei nº 14.133/2021) é o eixo central do Bounded Context **Administracao** (compras/contratos) e tem reflexos diretos em **Financas** (empenho/liquidação/pagamento) e **Transparencia**. Desde 30/12/2023 a Lei 8.666/93, a Lei do Pregão (10.520/02) e o RDC foram revogados; a 14.133 é o único regime.

### 1.1 Obrigações

| # | Obrigação | Base legal | Impacto no sistema |
|---|-----------|-----------|--------------------|
| 1 | **Divulgação centralizada e obrigatória no PNCP** de todos os atos exigidos pela lei (editais, atas de registro de preços, contratos e aditivos, contratações diretas — dispensa/inexigibilidade). | Art. 174 e Art. 54 da Lei 14.133 | Integração via API com o PNCP é requisito de produto. |
| 2 | **Publicidade no PNCP é condição de eficácia** do contrato e de seus aditivos. Sem publicação, o ato não produz efeitos. | Art. 94, §1º e Art. 91 | O sistema deve bloquear execução financeira (empenho/pagamento) enquanto não houver número de controle PNCP confirmado. |
| 3 | **Cadastramento unificado de fornecedores (SICAF)** — exigência de cadastro para licitações, dispensas (eletrônicas e não eletrônicas), inexigibilidades e contratações não onerosas. | Art. 87 e regulamentação federal | Para municípios não integrados ao Compras.gov.br federal, é admitido cadastro próprio equivalente; consultar SICAF para habilitação. |
| 4 | **Manutenção do conteúdo íntegro** do edital e anexos no PNCP durante toda a fase de divulgação. | Art. 54 | Armazenar e versionar documentos; publicar arquivos (PDF) via API. |
| 5 | **Publicação adicional em Diário Oficial** (União/Estado/Município) e jornal de grande circulação para o extrato do edital — sem prejuízo do PNCP. | Art. 54, §1º (facultativo conforme regulamento local) | [a confirmar regulamento municipal de Maximiliano de Almeida/RS] |
| 6 | **Plano de Contratações Anual (PCA)** e sua divulgação no PNCP. | Art. 12, VII | Módulo de planejamento de compras alimentando o PNCP. |
| 7 | **Portal de transparência próprio integrado ao PNCP** — sites oficiais do ente devem estar integrados ao PNCP, sob pena de a publicação não atender ao requisito legal. | Art. 174 c/c Decreto 10.764/2021 | Reforça que a integração PNCP é obrigatória, não opcional. |

### 1.2 Formatos / Layouts

- **Protocolo:** HTTP/1.1, payloads em **JSON (UTF-8)**. Não há entrada manual de dados no PNCP — toda inserção/alteração/exclusão é por **integração de sistema (API)**.
- **Documentação técnica:**
  - Swagger de manutenção (envio): `https://pncp.gov.br/api/pncp/swagger-ui/index.html`
  - Swagger de consulta (leitura pública): `https://pncp.gov.br/api/consulta/swagger-ui/index.html`
  - Manual de Integração PNCP (versão de referência) — define módulos: compras/editais/avisos, atas de registro de preços, contratos.
- **Autenticação:** serviços de manutenção exigem **JWT** obtido por login/senha fornecidos no credenciamento da plataforma junto ao Ministério (Gestão). **Token válido por 1 hora** — renovação obrigatória após expiração.
- **Módulos da API:** (a) Compras/Editais/Avisos; (b) Atas de Registro de Preços; (c) Contratos e aditivos. Há serviço de consulta de ARPs publicadas por período.
- **SICAF** — opera em **níveis progressivos**: Nível I (Credenciamento — CNPJ/dados básicos); Nível II (Habilitação Jurídica — contrato social, inscrição em cadastro de contribuintes); demais níveis (regularidade fiscal/trabalhista, qualificação econômico-financeira, qualificação técnica). Integrado ao **Compras.gov.br** com validação automática de certidões.

### 1.3 Integrações

| Sistema | Papel | Forma de integração |
|---------|-------|---------------------|
| **PNCP** (gov.br/pncp) | Repositório nacional obrigatório de atos de contratação | API REST/JSON + JWT; credenciamento da plataforma junto à Gestão da Rede Nacional de Contratações Públicas |
| **SICAF** (via Compras.gov.br) | Cadastro/habilitação de fornecedores | Consulta de regularidade; cadastro próprio municipal aceito se equivalente |
| **Diário Oficial municipal/estadual** | Publicação de extratos | Geração de matéria/PDF; integração depende do veículo do ente |
| **TCE-RS (módulo Financas)** | Prestação de contas — empenhos/contratos vinculados às compras | Ver diagnóstico de Contabilidade/TCE (LICOM/SIAPC-PAD) — contratos de compra alimentam dados contábeis |
| **Comitê Gestor da Rede Nacional de Contratações Públicas (CGRNCP)** | Governança do PNCP | Regulado pelo Decreto Federal nº 10.764/2021 |

> Observação de arquitetura: o contrato/empenho gerado em **Administracao** deve disparar a publicação no PNCP e, em paralelo, alimentar **Financas** (empenho → liquidação → pagamento), que por sua vez alimenta a remessa ao TCE-RS. O número de controle PNCP deve ser persistido como chave de rastreabilidade.

### 1.4 Prazos

- **Eficácia de contratos e aditivos no PNCP:**
  - **20 dias úteis** (regra geral, licitação) — [a confirmar literal do Art. 94 em fonte oficial Planalto].
  - **10 dias úteis** para **contratação direta** (dispensa/inexigibilidade), contados da assinatura.
- **Edital:** divulgação e manutenção íntegra no PNCP durante toda a fase; a contagem de prazo de propostas inicia da publicação no PNCP.
- **JWT da API PNCP:** expira em **1 hora**.
- **PCA:** divulgação anual.

### 1.5 Fontes — URLs

- PNCP — Portal oficial: https://www.gov.br/pncp/pt-br/pncp
- Swagger manutenção (API de envio): https://pncp.gov.br/api/pncp/swagger-ui/index.html
- Swagger consulta (API pública): https://pncp.gov.br/api/consulta/swagger-ui/index.html
- Manual de Integração PNCP (v1.0.0, repositório): https://repositorio.ufsc.br/bitstream/item/51718abf-525e-4867-a7dd-035657231915/Manual%20de%20Integra%C3%A7%C3%A3o%20PNCP%20%E2%80%93%20Vers%C3%A3o%201.0.0.pdf
- Lei 14.133/2021 (texto na íntegra — Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm
- TCU — Licitações e Contratos (divulgação): https://licitacoesecontratos.tcu.gov.br/5-11-7-divulgacao/
- SICAF e a Nova Lei (TRT4): https://www.trt4.jus.br/portais/trt4/modulos/noticias/631150
- Divulgação do edital no PNCP e contagem de prazo: https://www.licitacaoecontrato.com.br/leccomenta/a-divulgacao-edital-licitacao-pncp-contagem-prazo-regime-nova-lei-licitacoes-25022022.php

---

## PARTE 2 — ASSISTÊNCIA SOCIAL (SUAS / Rede SUAS)

Eixo do Bounded Context **AssistenciaSocial**. O SUAS (Sistema Único de Assistência Social) é gerido pelo **MDS** (Ministério do Desenvolvimento e Assistência Social, Família e Combate à Fome). A **Rede SUAS** é o conjunto de sistemas que organiza produção, armazenamento, processamento e disseminação de dados — base do cofinanciamento federal e do controle social.

### 2.1 Obrigações

| # | Obrigação | Sistema | Consequência do descumprimento |
|---|-----------|---------|--------------------------------|
| 1 | **Registro Mensal de Atendimentos (RMA)** de CRAS e CREAS (e unidades de acolhimento). | Aplicativo RMA (Rede SUAS) | Impacto no monitoramento e no cofinanciamento. |
| 2 | **Prontuário Eletrônico do SUAS** — registro de atendimentos/acompanhamentos de famílias e indivíduos por nome e NIS. | Prontuário Eletrônico SUAS (MDS) | Base qualitativa que substitui consolidação meramente quantitativa do RMA Formulário 1. |
| 3 | **Censo SUAS** (anual) — registro da existência e funcionamento das unidades (CRAS, CREAS, Centro POP, Acolhimento, Conselhos, Gestão). | Censo SUAS | **Suspensão do cofinanciamento federal** até comprovação de funcionamento da unidade. |
| 4 | **Plano de Ação e Demonstrativo Sintético** — planejamento e prestação de contas dos recursos federais. | SUASWeb (módulos Plano de Ação + Demonstrativo Sintético) | Abertura anual; aprovação pelo Conselho Municipal de Assistência Social. |
| 5 | **Gestão do Cadastro Único (CadÚnico)** — identificar, cadastrar, manter atualizado e analisar inconsistências das famílias de baixa renda. | Sistema do Cadastro Único (Dataprev) / CECAD 2.0 | Uso obrigatório para todos os programas sociais federais voltados à baixa renda (exceto previdenciários). |
| 6 | **Atualização do SISC** (Serviço de Convivência e Fortalecimento de Vínculos — SCFV). | SISC | Não atualizar nos prazos impacta/suspende cofinanciamento do SCFV. |
| 7 | **CadSUAS** — cadastro nacional do SUAS (unidades, órgãos gestores, conselhos, trabalhadores, rede). | CadSUAS | Pré-requisito para uso dos demais sistemas e repasses. |

### 2.2 Formatos / Layouts

- **CadÚnico (novo sistema, Dataprev):** plataforma integrada com módulos de entrada/alimentação de dados, capacitação de operadores, gestão de risco e fraude, gestão de acesso e relatórios analíticos. Cadastro/atualização **por entrevista domiciliar** (regra geral), salvo exceções regulamentadas.
  - **NIS → CPF:** o **CPF** passa a ser o código identificador da pessoa (o NIS continua registrado na base, mas deixa de ser a chave).
- **Prontuário Eletrônico SUAS:** registro por **nome + NIS** da família/indivíduo. Novo prontuário (2024/2025) moderniza atendimento, reduz retrabalho, integra à vigilância socioassistencial.
- **RMA:** dividido em **RMA – Unidade** e **RMA – Família**; quantifica atendimentos mensais, cobertura, perfil das famílias, e número de beneficiários do Bolsa Família e do BPC.
- **SUASWeb:** módulos **Plano de Ação** e **Demonstrativo Sintético** (formato web, preenchimento e aprovação eletrônica).
- **Censo SUAS:** questionários eletrônicos por tipo de unidade (CRAS, CREAS, Centro POP, Acolhimento, Conselho, Gestão).
- Formatos de exportação/integração específicos (layout de arquivo para importação em sistema próprio municipal) — **[a confirmar em fonte oficial MDS/SAGI]**; a maioria dos sistemas da Rede SUAS é de preenchimento direto no portal MDS, mas sistemas de gestão municipal costumam exportar para o Prontuário/CadÚnico.

### 2.3 Integrações

| Sistema | Papel | Órgão / portal |
|---------|-------|----------------|
| **CadÚnico** (novo sistema) | Cadastro de famílias de baixa renda; porta de entrada de benefícios | Dataprev — https://cadunico.dataprev.gov.br/ |
| **CECAD 2.0** | Consulta/extração analítica de dados do CadÚnico | https://cecad.cidadania.gov.br/ |
| **Prontuário Eletrônico SUAS** | Registro de atendimentos CRAS/CREAS/Acolhimento | https://aplicacoes.mds.gov.br/prontuario/ |
| **RMA** | Registro mensal de atendimentos | Rede SUAS / MDS |
| **Censo SUAS** | Recenseamento anual das unidades | https://censo-suas.cidadania.gov.br/ |
| **SUASWeb** | Plano de Ação + Demonstrativo Sintético (cofinanciamento) | Rede SUAS / MDS |
| **SISC** | Gestão do SCFV e cálculo de cofinanciamento | Rede SUAS / MDS |
| **SISJOVEM** | Monitoramento do Projovem Adolescente (módulo do SUASWeb) | Rede SUAS / MDS |
| **CadSUAS** | Cadastro nacional do SUAS (unidades/órgãos/trabalhadores) | Rede SUAS / MDS |
| **Bolsa Família** | Benefício vinculado ao CadÚnico | MDS / Dataprev |

> Observação de arquitetura: o módulo **AssistenciaSocial** do Tensorroot.Gov pode atuar como sistema municipal de gestão (atendimentos, agenda CRAS/CREAS, controle de benefícios eventuais) que **espelha/exporta** dados para o Prontuário Eletrônico e consulta o CadÚnico/CECAD. A integração em tempo real com sistemas MDS é limitada (muitos são preenchimento manual no portal); priorizar **importação/exportação por layout** e consulta ao CadÚnico. O número **NIS/CPF** é a chave de vínculo entre cidadão (Identidade), benefícios e atendimentos.

### 2.4 Prazos

- **RMA:** registro **mensal** (até o prazo definido pelo MDS no mês subsequente — [a confirmar dia exato em fonte oficial]).
- **Censo SUAS:** abertura **anual** do sistema a partir de **16 de outubro** (referência); preenchimento obrigatório sob pena de suspensão de cofinanciamento.
- **SUASWeb (Plano de Ação / Demonstrativo Sintético):** abertura **anual** para preenchimento pelos gestores e aprovação pelos conselhos.
- **CadÚnico:** atualização cadastral **a cada 2 anos** (regra geral) ou sempre que houver mudança na composição/renda familiar — [a confirmar regra vigente do novo sistema].
- **SISC:** atualização **periódica** (mensal/conforme calendário MDS) para manutenção do cofinanciamento do SCFV.

### 2.5 Fontes — URLs

- Cadastro Único (MDS): https://www.gov.br/mds/pt-br/acoes-e-programas/cadastro-unico
- Novo sistema do Cadastro Único (FAQ MDS): https://www.gov.br/mds/pt-br/noticias-e-conteudos/desenvolvimento-social/noticias-desenvolvimento-social/tire-suas-duvidas-sobre-o-novo-cadastro-unico
- CadÚnico — Dataprev: https://cadunico.dataprev.gov.br/
- CECAD 2.0: https://cecad.cidadania.gov.br/
- Prontuário Eletrônico do SUAS: https://aplicacoes.mds.gov.br/prontuario/
- Novo Prontuário (notícia MDS): https://www.gov.br/mds/pt-br/noticias-e-conteudos/desenvolvimento-social/noticias-desenvolvimento-social/novo-prontuario-eletronico-do-suas-moderniza-atendimentos-e-fortalece-o-acompanhamento-das-familias
- Censo SUAS: https://censo-suas.cidadania.gov.br/censocidadania/index.php
- Instruções Censo SUAS (SAGI/MDS): https://aplicacoes.mds.gov.br/sagi/portal/index.php?grupo=61
- Sistemas do MDS (lista oficial): https://www.gov.br/mds/pt-br/servicos/sistemas
- Sistemas da Rede SUAS (resumo): https://blog.gesuas.com.br/sistemas-gestao-mds-suas/
- Prontuário SUAS e relação com RMA/Vigilância (orientação técnica PR): https://www.justica.pr.gov.br/sites/default/arquivos_restritos/files/documento/2020-09/orientacao_tecnica_03_prontuario_suas.pdf

---

## Itens a confirmar em fonte oficial (pendências)

1. Prazos literais de eficácia de contratos no PNCP (Art. 94 da Lei 14.133) — verificar texto no Planalto.
2. Regulamento municipal de Maximiliano de Almeida/RS sobre publicação em Diário Oficial.
3. Layouts de exportação/importação de arquivo para Prontuário Eletrônico e CadÚnico (sistema municipal → MDS).
4. Dia exato de fechamento mensal do RMA e periodicidade vigente de atualização do CadÚnico no novo sistema Dataprev.
5. Cruzamento Administracao (contratos/PNCP) × Financas (empenho) × TCE-RS — ver diagnóstico de Contabilidade/TCE.
