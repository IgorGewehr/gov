# GovTech — Saúde Municipal (SUS)

> Diagnóstico de obrigações, formatos, integrações e prazos para o Bounded Context **Saude** do Tensorroot.Gov.
> Foco: o que um ERP municipal de saúde precisa **enviar/integrar** com os sistemas federais do SUS.
> Piloto: Maximiliano de Almeida/RS (município pequeno, APS predominante).
> Data da pesquisa: 2026-06-22. Modelo regulatório em rápida transição (Hórus → e-SUS AF; SI-PNI → RNDS).

---

## 1. Visão geral — arquitetura nacional de dados em saúde

O SUS evoluiu de dezenas de sistemas transacionais isolados (cada um com seu layout) para um modelo de **interoperabilidade centralizado na RNDS** (HL7 FHIR R4). Para um ERP municipal, há dois regimes a suportar simultaneamente nos próximos anos:

1. **Regime legado / batch (DATASUS)** — sistemas que ainda exigem arquivos/transmissão própria: e-SUS APS → SISAB, CNES/SCNES, SIA/SIH, SISREG, BNAFAR (via Hórus/e-SUS AF), SI-PNI.
2. **Regime moderno / FHIR (RNDS)** — barramento único que recebe eventos clínicos (imunização, dispensação, resultados laboratoriais, sumário de alta) em **HL7 FHIR** via HTTPS, com certificado digital e-CNPJ/ICP-Brasil.

> Tendência clara: tudo converge para a RNDS. Um ERP novo deve nascer **FHIR-first** e tratar os layouts legados como adaptadores.

---

## 2. Sistemas, obrigações e formatos

### 2.1 e-SUS APS / PEC (Prontuário Eletrônico do Cidadão) → SISAB

- **O que é:** Estratégia e-SUS Atenção Primária à Saúde. Dois softwares coletores gratuitos do MS/UFSC: **PEC** (prontuário completo, online) e **CDS** (Coleta de Dados Simplificada, fichas em lote). Versão atual citada: PEC 5.3 (nov/2024) — [a confirmar versão vigente em fonte oficial].
- **Para onde vai:** os dados alimentam o **SISAB** (Sistema de Informação em Saúde para a APS), base nacional usada para monitoramento e **financiamento federal da APS**.
- **Obrigação ERP:** se o município usa sistema próprio (não o PEC do MS), o ERP precisa exportar a produção da APS no **layout/thrift do e-SUS APS** e transmitir ao SISAB (centralizador). Alternativamente, integrar via RNDS.
- **Formato:** importação/exportação por **arquivos LEDI (Layout e-SUS — Dicionário de Importação)**, protocolo Apache Thrift, fichas CDS (cadastro individual, domiciliar, atendimento individual, procedimentos, atividade coletiva, etc.) [a confirmar nomenclatura LEDI da versão vigente].
- **Impacto financeiro:** produção registrada no SISAB é insumo direto dos indicadores de cofinanciamento (ver §3).

### 2.2 RNDS — Rede Nacional de Dados em Saúde

- **O que é:** plataforma nacional de interoperabilidade (SEIDIGI/MS). **Único padrão aceito: HL7 FHIR.**
- **Integração:** requisições **HTTPS conforme FHIR R4** + adaptações RNDS; autenticação por **certificado digital ICP-Brasil (e-CNPJ)** e credenciamento do estabelecimento/solução.
- **Eventos suportados (RES — Registro Eletrônico de Saúde):** imunização (RNI), resultados de exames laboratoriais (ex.: SARS-CoV-2, obrigatório por Portaria 1.792/2020), dispensação de medicamentos, sumário de alta, registro de atendimento clínico — conforme perfis FHIR publicados no Guia RNDS.
- **Obrigação ERP:** qualificação/padronização dos dados para FHIR; homologação no ambiente da RNDS antes de produção.
- **Tendência:** **federalização da RNDS** com adesão de estados/municípios — o caminho oficial para o futuro.

### 2.3 SISREG — Sistema Nacional de Regulação

- **O que é:** software web do DATASUS para gestão do Complexo Regulador. Dois módulos: **CMC** (Central de Marcação de Consultas/exames) e **CIH** (Central de Internação Hospitalar).
- **Caráter:** uso **não compulsório**; configuração customizada por cada Secretaria. Administrador Estadual define municípios solicitantes/executantes conforme pactuação.
- **Obrigação ERP:** se o município regula localmente (sistema próprio ou estadual, ex.: complexos reguladores estaduais), o ERP de saúde deve **integrar/encaminhar solicitações** ou ao menos não duplicar o fluxo. Relevante para fila de espera, regulação ambulatorial e leitos.
- **Formato/integração:** SISREG é primariamente operado via navegador; integração programática depende de webservices/estado — [a confirmar disponibilidade de API pública do SISREG III em fonte oficial].

### 2.4 Assistência Farmacêutica — Hórus / e-SUS AF → BNAFAR

- **O que é:** **BNAFAR** (Base Nacional de Dados de Ações e Serviços da Assistência Farmacêutica) consolida estoque, movimentação e dispensação de medicamentos nas 3 esferas. Sistema gestor histórico: **Hórus**.
- **MUDANÇA CRÍTICA (2026):** **Portaria GM/MS nº 11.585 (publicada 16/06/2026)** institui a **substituição do Hórus pelo e-SUS Assistência Farmacêutica (e-SUS AF)**.
  - Janela de fechamento operacional: **180 dias** a partir da publicação para concluir registros de entradas, estoque, saídas e dispensações no legado.
  - Hórus disponível para **consulta (read-only) por até 5 anos** após a janela; depois, acesso encerrado.
  - Módulo especializado (estados): contagem de 180 dias só inicia após conclusão do piloto e aprovação tripartite.
  - **e-SUS AF faz envio automático à BNAFAR e à RNDS.**
- **Obrigações do município:** declarar interesse no novo sistema, fechar inventário/saldo no legado com documentação, garantir integridade dos dados na migração, treinar equipes.
- **Obrigação ERP:** se o município usa sistema próprio de farmácia, deve **enviar à BNAFAR** (layout BNAFAR) e/ou integrar à RNDS via e-SUS AF. Recomenda-se mirar o fluxo e-SUS AF/RNDS, não mais o Hórus.

### 2.5 SI-PNI — Imunizações

- **O que é:** Sistema de Informação do Programa Nacional de Imunizações. **Novo SI-PNI** reformulado e **integrado à RNDS** (alinhado ao Registro Nacional de Vacinação eletrônico / RNVe da OMS).
- **Marco:** em **31/05/2023** foi descontinuada a entrada de dados no SI-PNI web/desktop antigos; desde **01/06/2023** o registro de rotina ocorre no novo SI-PNI integrado à RNDS.
- **Fluxos de registro de dose:**
  1. Direto no novo SI-PNI; ou
  2. Sistema local **integrado à RNDS** → evento de imunização enviado conforme modelo nacional (FHIR); ou
  3. Sistema de APS **sem integração direta** → passa por e-SUS APS/SISAB e migra à RNDS (latência ~30 dias).
- **Obrigação ERP:** registrar dose aplicada e **enviar evento de imunização à RNDS** (perfil FHIR de imunização) — caminho preferencial. Caso contrário, garantir trânsito via SISAB.

### 2.6 CADSUS / CNS — Cartão Nacional de Saúde

- **O que é:** CADSUS gera/atualiza o **CNS** (Cartão Nacional de Saúde), documento de identificação do usuário do SUS. Operado via **CadSUS Web** e webservices.
- **Integração:** **Web Service do CNS / API CADSUS** (catálogo Conecta gov.br, versão citada v5) permite **consultar, inserir e alterar** cadastro de usuários na base nacional, com padrões de identificação do paciente. Gestão de operadores via **SGOP**.
- **Obrigação ERP:** o ERP deve **identificar o cidadão pelo CNS/CPF** e idealmente integrar ao CADSUS para validar/criar cadastro nacional — base para todos os demais envios (APS, imunização, farmácia, regulação). É a "chave de cidadão" de toda a integração SUS.

### 2.7 CNES / SCNES — Cadastro Nacional de Estabelecimentos

- **O que é:** sistema oficial de cadastro de todos os estabelecimentos de saúde e profissionais.
- **Obrigação ERP:** referenciar **código CNES** do estabelecimento e **CBO/CNS dos profissionais** em toda produção enviada. Pré-requisito para SIA/SIH/SISAB.

### 2.8 SIA / SIH — Produção ambulatorial e hospitalar

- **SIA** (ambulatorial) capta via **BPA-C** (consolidado), **BPA-I** (individualizado), **APAC** (alta complexidade) e **RAAS**. Tabela mensal **BDSIA** atualiza os aplicativos.
- **SIH** (hospitalar): **AIH**.
- **Obrigação ERP:** municípios com produção de média/alta complexidade ou unidades próprias devem gerar **BPA/APAC/AIH** nos layouts DATASUS para faturamento SUS.

---

## 3. Financiamento (FNS) e seu vínculo com os dados

- **FNS** (Fundo Nacional de Saúde) repassa recursos **fundo a fundo** (Portaria 204/2007 — blocos de financiamento) a municípios.
- **Cofinanciamento federal da APS — novo modelo:** **Portaria GM/MS nº 3.493, de 10/04/2024** revogou o **Previne Brasil** e instituiu nova metodologia (efeitos financeiros desde maio/2024). Componentes:
  1. **Fixo** — manutenção de eSF/eAP + recursos de implantação (eSF, eAP, eSB, eMulti);
  2. **Vínculo e acompanhamento territorial** — eSF/eAP;
  3. **Qualidade** — eSF, eAP, eSB, eMulti;
  4. **Implantação/manutenção** de programas, serviços e composições de equipe;
  5. **Atenção à Saúde Bucal**;
  6. **Per capita** com base populacional.
- **Indicadores de qualidade:** a partir do **2º quadrimestre de 2025**, 15 indicadores em 3 blocos (Saúde Bucal; eMulti; eSF/APS) monitorados via SISAB (Anexo V da portaria). Atualizações posteriores citadas: **Portaria GM/MS nº 6.796/2025** [a confirmar escopo].
- **CONSEQUÊNCIA DIRETA PARA O ERP:** o repasse federal depende da **produção registrada no SISAB** e dos indicadores. **Qualidade do dado clínico = receita do município.** Este é o argumento de venda mais forte do módulo Saúde para o gestor.
- **Suspensão de repasse:** **Portaria nº 708/2007** torna obrigatório o **envio mensal de CNES, SIA e SIHD**; a falta de envio por **3 meses consecutivos ou alternados** suspende imediatamente as transferências. Acompanhamento de execução/teto via **SISMAC** e Portal FNS.

---

## 4. Prazos e cadência de envio

| Obrigação | Periodicidade | Prazo |
|---|---|---|
| CNES/SCNES, SIA, SIHD | Mensal | até ~5º dia útil do mês seguinte (cronograma DATASUS); falta de 3 meses → suspensão de repasse (Port. 708/2007) |
| Produção APS → SISAB | Quadrimestral (avaliação) / contínuo (envio) | janelas de fechamento por quadrimestre para cálculo do cofinanciamento |
| Imunização → SI-PNI/RNDS | Contínuo / por dose | registro no ato; latência via SISAB ~30 dias se sem integração direta |
| Assistência Farmacêutica → BNAFAR | Mensal | migração Hórus→e-SUS AF: janela de **180 dias** desde 16/06/2026 (Port. 11.585) |
| Resultados laboratoriais (ex.: SARS-CoV-2) → RNDS | Por evento | conforme Port. 1.792/2020 |

> Datas exatas de fechamento mensal seguem o **cronograma anual do DATASUS** — [a confirmar cronograma vigente em fonte oficial].

---

## 5. Recomendações de arquitetura para o BC Saude (Tensorroot.Gov)

1. **CNS como identidade clínica:** integrar CADSUS Web Service (API v5) na Identidade do paciente; CNS/CPF como chave de todos os envios.
2. **FHIR-first + adaptadores legados:** núcleo de eventos clínicos em HL7 FHIR R4 para RNDS; adaptadores para LEDI/e-SUS APS, BPA/APAC/AIH e BNAFAR enquanto persistirem.
3. **Certificado digital ICP-Brasil (e-CNPJ)** como requisito de infra para falar com RNDS.
4. **Não reimplementar o Hórus** — mirar e-SUS AF/RNDS para farmácia (a transição já está em curso).
5. **Imunização via evento RNDS direto** (evitar latência de 30 dias do trânsito por SISAB).
6. **Painel de cofinanciamento APS:** expor ao gestor o status dos 15 indicadores de qualidade e da produção SISAB — é o que protege a receita do município (alinhado à preocupação do dono com prestação de contas).
7. **Monitor de obrigatoriedade de envio** (CNES/SIA/SIH mensal) com alerta antes do risco de suspensão de repasse (Port. 708/2007).

---

## 6. Fontes (URLs oficiais e de referência)

- e-SUS APS / PEC / SISAB — Painel e-SUS APS (MS): http://sisaps.saude.gov.br/esus/
- Manual CDS (MS): http://sisaps.saude.gov.br/sistemas/esusaps/docs/manual/CDS/CDS_01/
- PEC 5.3 lançamento (MS): https://www.gov.br/saude/pt-br/assuntos/noticias/2024/novembro/lancada-versao-5-3-do-prontuario-eletronico-do-cidadao-software-disponibilizado-pela-saude
- RNDS (SEIDIGI/MS): https://www.gov.br/saude/pt-br/composicao/seidigi/rnds
- Guia de Integração RNDS (FHIR): https://rnds-guia.saude.gov.br/
- RNDS no Catálogo Conecta gov.br: https://www.gov.br/conecta/catalogo/apis/rnds-rede-nacional-de-dados-em-saude
- Federalização da RNDS (SES-PR): https://www.saude.pr.gov.br/Pagina/Federalizacao-da-Rede-Nacional-de-Dados-em-Saude-RNDS
- SISREG (Wiki DATASUS): https://wiki.saude.gov.br/SISREG/index.php/P%C3%A1gina_principal
- SISREG (CONASS): https://www.conass.org.br/guiainformacao/o-sisreg/
- HÓRUS (MS): https://www.gov.br/saude/pt-br/composicao/sectics/daf/horus
- BNAFAR (MS): https://www.gov.br/saude/pt-br/composicao/sectics/daf/bnafar
- e-SUS AF substitui Hórus — Port. 11.585 (CONASS Informa 114/2026): https://www.conass.org.br/conass-informa-n-114-2026-publicada-a-portaria-gm-n-11-585-que-altera-a-portaria-de-consolidacao-gm-ms-no-1-de-28-de-setembro-de-2017-para-dispor-sobre-a-substituicao-do-sistema-hor/
- e-SUS AF lançamento (MS, abr/2026): https://www.gov.br/saude/pt-br/assuntos/noticias/2026/abril/ministerio-da-saude-apresenta-novo-sistema-de-assistencia-farmaceutica-no-sus
- SI-PNI novo (MS): https://www.gov.br/saude/pt-br/assuntos/noticias/2023/junho/entenda-o-novo-sistema-de-informacao-do-programa-nacional-de-imunizacoes
- SI-PNI / integração RNDS (Nota Técnica DATASUS, CEVS-RS): https://www.cevs.rs.gov.br/upload/arquivos/202404/29095847-nota-tecnica-conjunta-datasus-envio-de-dados-para-a-rnds-de-sistemas-proprios.pdf
- CADSUS / CNS API (Catálogo Conecta): https://www.gov.br/conecta/catalogo/apis/cadsus-cadastro-de-usuarios-do-sus
- Manual CADSUS Web (MS): https://cadastrohm.saude.gov.br/cadsusweb/manual.pdf
- CNES/SCNES (Wiki DATASUS): https://wiki.saude.gov.br/cnes/index.php/P%C3%A1gina_principal
- SIA (Wiki DATASUS): https://wiki.saude.gov.br/sia/index.php/P%C3%A1gina_principal
- FNS — Fundo a Fundo (MS): https://portalfns.saude.gov.br/fundo-a-fundo/
- Portaria GM/MS nº 3.493/2024 (cofinanciamento APS): https://bvsms.saude.gov.br/bvs/saudelegis/gm/2024/prt3493_11_04_2024.html
- FAQ Novo Modelo de Cofinanciamento APS (MS): https://www.gov.br/saude/pt-br/composicao/saps/esf/faq-novo-modelo-de-cofinanciamento-federal-da-aps
- Portaria GM/MS nº 6.796/2025 (cofinanciamento APS — análise): https://p2saude.com.br/portaria-gm-ms-no-6-796-2025-cofinanciamento-da-atencao-primaria-em-saude/
- Obrigatoriedade de envio mensal (Port. 708/2007) e SISMAC: https://draca.cubatao.sp.gov.br/index.php/2025/08/21/gestao-de-recursos/

> Itens marcados **[a confirmar em fonte oficial]** devem ser validados na documentação técnica vigente (Guia RNDS, LEDI e-SUS APS, cronograma DATASUS) antes da implementação.
