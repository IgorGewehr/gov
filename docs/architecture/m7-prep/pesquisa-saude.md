# M7 — Pesquisa: Saúde municipal (sistemas federais, repasses e mínimo constitucional)

> Preparatório (m7-prep). Tema: o que o **município ENVIA/CONSOME** dos sistemas nacionais
> de saúde do SUS (RNDS, e-SUS APS/SISAB, CNES, SI-PNI, SISREG, HÓRUS/BNAFAR, SIOPS, FNS),
> **periodicidade**, **formato** e o **vínculo com o financiamento** (repasses fundo a fundo
> e mínimo constitucional de 15% ASPS).
>
> **Regra inegociável (CLAUDE.md §16):** nada de inventar formato/leiaute/percentual/código —
> tudo com **FONTE/URL** ou marcado `[a confirmar — obter doc oficial]`. Percentuais/regras que
> são **lei** ficam **parametrizáveis por tenant+vigência**, nunca hardcoded.
>
> **Estado dos módulos:** JÁ EXISTEM scaffolded `Saude`, `Educacao`, `AssistenciaSocial`.
> Esta pesquisa cobre o **domínio Saúde**: o ERP é a fonte transacional municipal e atua como
> **gateway de integração (ACL + Outbox + idempotência)** para os sistemas nacionais — em geral
> o município **NÃO substitui** os sistemas do MS, mas precisa **alimentá-los** (ou consumir
> deles) e, sobretudo, **espelhar a contabilidade do Fundo Municipal de Saúde (FMS)** para o
> mínimo constitucional e a prestação de contas (vínculo direto com o M2/M4 já entregues).
>
> Legenda de confiança: **ALTA** = base legal/fluxo confirmado em fonte oficial; **MÉDIA** =
> conceito correto mas detalhe técnico (XSD/endpoint/código) depende de doc oficial não obtido;
> **BAIXA** = bloqueado por leiaute/contrato não obtido (não implementar fiel sem o doc).

---

## 0. Princípio transversal — o ERP é gateway, e o FMS é o coração contábil

Dois eixos distintos, não confundir:

1. **Eixo assistencial/operacional** (RNDS, e-SUS APS, CNES, SI-PNI, SISREG, HÓRUS): o município
   produz/registra atos de saúde e os **envia** aos sistemas nacionais (ou marca/consome filas).
   Aqui o ERP atua como **integrador**; o registro clínico fino normalmente já vive no e-SUS/PEC.
2. **Eixo financeiro/fiscal** (SIOPS + FNS + Fundo Municipal de Saúde): este é o eixo onde o ERP
   **é a fonte da verdade** — a execução orçamentária do FMS (já modelada no M2 PCASP) precisa
   ser **segregada por bloco de financiamento e por fonte/recurso vinculado** para (a) apurar o
   mínimo de 15% ASPS e (b) prestar contas dos repasses fundo a fundo. **Este eixo é o de maior
   valor e menor risco regulatório para o M7 entregar primeiro.** **CONFIANÇA: ALTA.**

> **Decisão de escopo a validar:** modelar o domínio Saúde primariamente como **gestão do FMS +
> integração de envio**, e tratar os módulos clínicos (prontuário) como **read models / ACLs**
> sobre os sistemas nacionais — evita reconstruir e-SUS. `[a confirmar com o produto/escopo M7]`.

---

## 1. SIOPS — mínimo 15% ASPS (eixo fiscal, prioridade #1)

- **O que é:** Sistema de Informações sobre Orçamentos Públicos em Saúde. Declaração **obrigatória
  e bimestral** de receitas totais e despesas em saúde por ente federado. **CONFIANÇA: ALTA.**
- **Base legal:** **Lei Complementar nº 141/2012** (regulamenta a EC 29/2000). Municípios e DF
  devem aplicar **no mínimo 15%** da receita de impostos + transferências constitucionais em
  **Ações e Serviços Públicos de Saúde (ASPS)** — **ou o percentual maior** fixado na Lei
  Orgânica municipal. **CONFIANÇA: ALTA.**
- **Periodicidade/prazo:** declaração **bimestral**; o ente declara **até 30 dias após o
  encerramento de cada bimestre** (LC 141/2012). Descumprimento → medidas administrativas
  (restrições, suspensão de transferências). **CONFIANÇA: ALTA** (prazo confirmado em fonte
  oficial/cartilha; **confirmar redação atual no portal SIOPS** antes de codar o calendário).
- **Vínculo com financiamento:** não-cumprimento do 15% e/ou não-alimentação do SIOPS pode
  **bloquear repasses** e gerar ressalva em prestação de contas (TCE-RS) → integra-se diretamente
  ao M4 já entregue. **CONFIANÇA: ALTA** (princípio); detalhe sancionatório `[a confirmar]`.
- **O que o ERP faz:** o M2 (PCASP) + execução do FMS já têm os dados. Precisa de um
  **`ApuradorAsps`** (serviço de domínio) que classifique despesas como ASPS×não-ASPS conforme
  LC 141/2012 (art. 3º despesas que contam; art. 4º o que **não** conta — ex.: saneamento básico,
  inativos/pensionistas, merenda, limpeza urbana), apure o percentual e gere o **demonstrativo
  SIOPS** por bimestre. **Percentual mínimo parametrizável por tenant** (default legal 15%, mas
  Lei Orgânica pode ser maior). **CONFIANÇA: MÉDIA** (regra de classificação ASPS é a parte
  delicada — depende do manual SIOPS vigente).
- **Formato de transmissão:** o SIOPS tem aplicativo próprio de coleta/transmissão. **Verificar
  se há importação por arquivo/web service** ou se é digitação/importação do SIAFI local.
  `[a confirmar — obter manual técnico SIOPS / leiaute de importação]`. **CONFIANÇA: BAIXA** para
  o canal técnico de envio.
- **Docs a obter:** LC 141/2012 (texto integral); Manual do SIOPS / instruções de preenchimento
  vigentes; tabela oficial de classificação ASPS (despesas que computam e que não computam);
  cartilha SIOPS atual; calendário oficial de bimestres/prazos.

---

## 2. FNS — repasses fundo a fundo e blocos de financiamento (eixo fiscal #2)

- **O que é:** Fundo Nacional de Saúde transfere recursos federais aos **Fundos Municipais de
  Saúde (FMS)** na modalidade **fundo a fundo** — repasse **regular e automático**, **sem convênio**,
  desde que o ente mantenha **Conselho de Saúde** e **Plano de Saúde** atualizados. **CONFIANÇA: ALTA.**
- **Base legal:** **Portaria GM/MS nº 204/2007** (regulamentou financiamento em blocos);
  **Portaria GM/MS nº 3.992/2017** reorganizou em **dois blocos**: **Custeio** (Bloco de Manutenção
  das ASPS) e **Investimento** (Bloco de Estruturação da Rede de Serviços Públicos de Saúde).
  **CONFIANÇA: ALTA** (substituiu os antigos 6 blocos: Atenção Básica, MAC, Vigilância, Assistência
  Farmacêutica, Gestão do SUS, Investimentos). **Confirmar vigência/atualizações pós-2017** antes
  de codar a taxonomia. **CONFIANÇA: MÉDIA** sobre o estado atual exato dos blocos.
- **Vínculo com financiamento:** os recursos chegam **carimbados por bloco/ação** e precisam ser
  **executados e prestados contas dentro do bloco** (vedada a transposição livre custeio↔investimento).
  O ERP precisa modelar **conta corrente por recurso/bloco no FMS** — espelha exatamente o conceito
  de **fonte/recurso vinculado** já presente no PCASP (M2). **CONFIANÇA: ALTA.**
- **O que o ERP faz:** (a) **conciliar** os créditos recebidos do FNS no FMS (importar extrato/
  parcelas por bloco/ação — fonte: portais FNS, ex.: Consulta FNS / painéis InvestSUS); (b)
  segregar a execução por bloco; (c) gerar o **Relatório de Gestão / prestação de contas** do FMS
  (vinculado ao Conselho de Saúde) e alimentar o M4. **CONFIANÇA: MÉDIA** (princípio ALTA; leiaute
  de importação dos repasses `[a confirmar]`).
- **Docs a obter:** Portaria 204/2007 e 3.992/2017 (íntegra + alterações vigentes); manual de
  consulta/extração do FNS (há API/arquivo de parcelas?); regras de prestação de contas fundo a
  fundo (SISAUDE/Relatório Anual de Gestão — RAG / SARGSUS).

---

## 3. RNDS — interoperabilidade nacional (padrão FHIR)

- **O que é:** Rede Nacional de Dados em Saúde — **plataforma oficial de interoperabilidade** do MS
  (barramento nacional). Recebe **apenas dados no padrão HL7 FHIR**; o emissor deve qualificar/
  padronizar antes de enviar. **CONFIANÇA: ALTA.**
- **Identificador:** o MS oficializou a RNDS como plataforma do SUS e adotou **CPF como
  identificador nacional** do cidadão (além do CNS). **CONFIANÇA: ALTA** (confirmar implicação
  técnica no manual — CNS ainda usado no header). 
- **Autenticação (crítico p/ ACL):** `POST /token` com **certificado digital ICP-Brasil tipo
  **A1** (.pfx/.p12) e-CNPJ/e-CPF** — **A3 (token físico) NÃO é suportado** para integração de API.
  Token tem **validade de 30 min** e é exigido em todas as demais chamadas; o **CNS** vai no header
  `Authorization` em todos os contatos (exceto na obtenção do token). **CONFIANÇA: ALTA** (modelo
  confirmado; confirmar nomes exatos de headers no manual vigente). Implica: o ERP precisa do
  **e-CNPJ A1 do ente** (já temos infra Key Vault/A1 do M2) e de um **gateway RNDS** com renovação
  automática de token + Polly/retry.
- **Ambientes:** **homologação** e **produção** (ativação testa em homologação antes de migrar).
  **CONFIANÇA: ALTA.**
- **O que o município ENVIA (perfis/cenários documentados — confirmar lista vigente):** resultado
  de **exame laboratorial** (ex.: COVID-19, obrigatório por Portaria 1.792 — verificar vigência),
  **RAC (Registro de Atendimento Clínico)**, **Sumário de Alta**, **imunização** (via SI-PNI, ver §5).
  **CONFIANÇA: MÉDIA** (cenários existem; **a lista completa de perfis/Bundles FHIR e seus
  XSD/StructureDefinitions** deve ser obtida do Guia RNDS — seção "Modelos").
- **O que o município CONSOME:** dados clínicos do cidadão de outros entes (continuidade de
  cuidado) e terminologias. **CONFIANÇA: BAIXA** sobre escopo/permissão exatos `[a confirmar]`.
- **Docs a obter (bloqueador de fidelidade):** **Manual de Integração da RNDS (Barramento)** —
  versão vigente (há PDF DATASUS); **Guia RNDS** (rnds-guia.saude.gov.br) seção Modelos com os
  **perfis FHIR / Bundles** suportados; lista de cenários obrigatórios atuais; especificação do
  Conector RNDS. **CONFIANÇA do conjunto de perfis: BAIXA até obter o guia.**

> **Recomendação de arquitetura:** modelar `RndsGateway` (ACL) + `RndsEnvioOutbox` com idempotência
> por evento; **não** inventar Bundles FHIR — gerar a partir das StructureDefinitions oficiais
> (gerar tipos a partir do Guia, não escrever campos à mão).

---

## 4. e-SUS APS / SISAB — atenção primária e o financiamento da APS

- **O que é:** **e-SUS APS** = sistema de registro da Atenção Primária (módulos **PEC** —
  Prontuário Eletrônico do Cidadão, e **CDS** — Coleta de Dados Simplificada). **SISAB** = base
  nacional que centraliza esses dados. **Desde 2017 o registro da APS é exclusivo no e-SUS APS**
  (ou sistemas próprios/terceiros **integrados** que enviam ao SISAB). **CONFIANÇA: ALTA.**
- **O que o município ENVIA:** produção da APS (atendimentos, procedimentos, indicadores) ao
  **Centralizador Nacional (SISAB)** — direto do e-SUS ou via sistema próprio integrado. O MS vem
  **fortalecendo o e-SUS como sistema exclusivo da APS**. **CONFIANÇA: ALTA.**
- **Periodicidade:** envio **regular** com **fechamentos quadrimestrais** para fins de
  financiamento (cálculo a cada 4 meses considerando os 4 meses anteriores). **CONFIANÇA: ALTA**
  (cadência quadrimestral confirmada; confirmar janela/prazo exato de envio no manual SISAB).
- **Vínculo com financiamento (forte):** o **financiamento federal da APS** é diretamente atrelado
  aos dados enviados ao SISAB. Programa **Previne Brasil**, **substituído/reformulado pela
  Portaria GM/MS nº 3.493/2024** (novo modelo de cofinanciamento da APS):
  - **Componente fixo** — valor mensal por equipe (eSF/eAP), conforme **classificação do município
    pelo IED (Índice de Equidade e Dimensionamento)**.
  - **Componente de vínculo e acompanhamento territorial** — valor mensal por equipe baseado em
    critérios demográficos (crianças <5, idosos >60) e indicadores de vulnerabilidade; **a partir
    da parcela 5/12 de 2025** passa a depender da **classificação de desempenho** das equipes.
  - Modelo dividido em **6 componentes**. Fase de transição: **12 parcelas mai/24→abr/25**.
  - **CONFIANÇA: ALTA** quanto à existência/estrutura; **valores R$/equipe, fórmulas e indicadores
    são parametrizáveis e mudam por portaria → `[a confirmar — obter Portaria 3.493/2024 e anexos]`.**
- **SIAPS / novos indicadores:** há um **SIAPS** (Sistema de Informação da APS) e novos
  **indicadores de cofinanciamento** a serem registrados. **CONFIANÇA: MÉDIA** (mencionado em
  fontes CONASEMS; **obter doc oficial do SIAPS e lista de indicadores vigentes**).
- **O que o ERP faz:** provavelmente **NÃO substituir o e-SUS PEC**. Modelar (a) **monitor de
  envio ao SISAB** (status/última remessa por unidade/equipe — risco de perda de repasse se parar
  de enviar); (b) **read model dos indicadores Previne/SIAPS** para o Portal do Gestor (M8) prever
  o repasse; (c) conciliar o repasse APS recebido (FNS) com o desempenho. **CONFIANÇA: MÉDIA.**
- **Docs a obter:** Portaria GM/MS 3.493/2024 + anexos (valores, IED, componentes); manual técnico
  de integração SISAB (formato de remessa e-SUS / thrift/XML?); lista de indicadores SIAPS vigentes
  e periodicidade de apuração; manual e-SUS APS (PEC/CDS).

---

## 5. SI-PNI — imunização

- **O que é:** Sistema de Informação do Programa Nacional de Imunizações. Registra doses,
  calendário vacinal, campanhas, cobertura, distribuição de imunobiológicos e ESAVI. **CONFIANÇA: ALTA.**
- **Integração RNDS:** **desde 01/06/2023** o novo SI-PNI (módulo de rotina) opera **integrado à
  RNDS**, alinhado ao **Registro Nacional de Vacinação eletrônico (RNVe)** da OMS. **CONFIANÇA: ALTA.**
- **O que o município ENVIA / fluxo (importante p/ arquitetura):** o evento de imunização pode ser
  registrado **direto no novo SI-PNI** OU em **sistemas locais integrados**:
  - se o sistema local é **integrado direto à RNDS** → a dose vai à RNDS pelos modelos nacionais;
  - se o registro ocorre no **e-SUS APS sem integração direta** → passa por **SISAB e depois RNDS**,
    com latência de **~30 dias**. **CONFIANÇA: ALTA** (fluxo confirmado em fonte oficial/COSEMS).
- **Vínculo com financiamento:** cobertura vacinal compõe **indicadores da APS (Previne/SIAPS)** →
  impacta repasse indiretamente. **CONFIANÇA: MÉDIA** `[a confirmar quais indicadores vacinais
  entram no cofinanciamento vigente]`.
- **O que o ERP faz:** se o município registra vacina no ERP, enviar **evento de imunização FHIR à
  RNDS** (reusa `RndsGateway`); caso contrário, **monitorar** a integração SI-PNI/e-SUS. **CONFIANÇA: MÉDIA.**
- **Docs a obter:** modelo de informação de imunização da RNDS (perfil FHIR Immunization);
  manual do novo SI-PNI; mapeamento de imunobiológicos/lotes.

---

## 6. CNES — estabelecimentos, equipes e profissionais

- **O que é:** Cadastro Nacional de Estabelecimentos de Saúde — base nacional de estabelecimentos,
  profissionais, equipes, leitos, equipamentos e serviços. **CONFIANÇA: ALTA.**
- **O que o município ENVIA / periodicidade:** atualização cadastral **eletrônica com frequência
  mínima MENSAL**, ou sempre que houver alteração — **arts. 371 e 372 da Portaria de Consolidação
  nº 01/GM/MS/2017**. Ciclo de coleta→publicação é mensal (DATASUS); base permite envios diários
  dentro da competência aberta. **CONFIANÇA: ALTA.**
- **Vínculo com financiamento (estrutural):** o CNES é **pré-condição** de quase tudo — **equipes
  cadastradas no CNES** definem o repasse da APS (eSF/eAP no Previne), e o CNES é base para MAC,
  produção (BPA/AIH/APAC) e teto financeiro. **Sem CNES correto, não há repasse.** **CONFIANÇA: ALTA.**
- **O que o ERP faz:** manter cadastro de unidades/equipes/profissionais do município e **conciliar
  com o CNES** (o ERP de RH — M5 — já tem servidores/vínculos; aqui cruza com lotação em unidade de
  saúde + CBO + carga horária). Provável **NÃO substituir** o aplicativo CNES; modelar **espelho +
  monitor de competência/atualização**. **CONFIANÇA: MÉDIA.**
- **Docs a obter:** Portaria de Consolidação nº 01/2017 (arts. 371–372 e correlatos); manual/leiaute
  de importação CNES (há arquivo/integração ou só app SCNES?); tabelas oficiais (tipos de
  estabelecimento, CBO, tipos de equipe).

---

## 7. SISREG — regulação (filas, marcação, leitos) — eixo de CONSUMO

- **O que é:** Sistema de Regulação do MS/DATASUS, usado por secretarias municipais/estaduais para
  gerir acesso a consultas especializadas, exames e cirurgias eletivas. **Três módulos:**
  **Ambulatorial** (consultas/exames), **Internação Hospitalar** (leitos), e **APAC** (alta
  complexidade/custo). **CONFIANÇA: ALTA.**
- **O que o município faz:** disponibiliza **ofertas** (cotas de consultas/exames/leitos), regula a
  **fila** e marca; painéis públicos mostram tempo médio de espera, **atualizados ~a cada 24h**.
  **CONFIANÇA: ALTA.**
- **Vínculo com financiamento:** indireto (produção regulada vira BPA/APAC/AIH → MAC). **CONFIANÇA: MÉDIA.**
- **O que o ERP faz:** mais **CONSUMO** que envio — integrar para refletir filas/agendamentos do
  cidadão no Portal do Cidadão (M8) e cruzar oferta×demanda no BI. **Verificar se há API pública do
  SISREG** ou só interface web. **CONFIANÇA: BAIXA** sobre o canal de integração `[a confirmar —
  obter manual SISREG / verificar existência de API]`.
- **Docs a obter:** manuais SISREG III (operador/regulador); existência e contrato de API; wiki
  SISREG (wiki.saude.gov.br).

---

## 8. HÓRUS / e-SUS AF / BNAFAR — assistência farmacêutica

- **O que é:** **HÓRUS** = Sistema Nacional de Gestão da Assistência Farmacêutica (estoque,
  entradas/saídas, dispensação). **BNAFAR** = Base Nacional de dados da Assistência Farmacêutica —
  consolida posição de estoque, entradas, saídas, avaliações e **dispensações** de medicamentos da
  **RENAME** e Programa Farmácia Popular (componentes Básico, Especializado e Estratégico).
  Instituída em **24/10/2017**. **CONFIANÇA: ALTA.**
- **TRANSIÇÃO IMPORTANTE (2024–2026):** **Portaria GM/MS nº 5.713/2024** atualiza regras da BNAFAR;
  e o MS apresentou (abr/2026) o **novo e-SUS AF** (e-SUS Assistência Farmacêutica) que **substitui
  o HÓRUS**, com **transmissão automática à BNAFAR e à RNDS**. Transmissão diária de dados às bases
  nacionais segue obrigatória. **CONFIANÇA: ALTA** (mudança recente — **codar contra e-SUS AF/BNAFAR,
  tratar HÓRUS como legado**). 
- **O que o município ENVIA / periodicidade:** dados de estoque/dispensação à **BNAFAR**, via HÓRUS,
  **e-SUS AF**, ou **SICLOM** — transmissão **diária**. **CONFIANÇA: ALTA** (canal); periodicidade
  exata/janela `[a confirmar no manual BNAFAR/Portaria 5.713/2024]`.
- **Vínculo com financiamento:** alimentar a BNAFAR é **condição** para recursos do **Componente
  Básico da Assistência Farmacêutica (CBAF)** e ressarcimentos — não enviar pode bloquear repasse
  farmacêutico. **CONFIANÇA: MÉDIA** `[a confirmar regra sancionatória vigente]`.
- **O que o ERP faz:** se o município gerencia farmácia no ERP, modelar estoque/dispensação +
  **gateway de envio à BNAFAR** (webservice BNAFAR existe — ver SIGAF/Ajuda SAF). Reusar terminologia
  RENAME/CATMAT. **CONFIANÇA: MÉDIA.**
- **Docs a obter:** **Portaria GM/MS 5.713/2024** (íntegra); manual do **web service BNAFAR**
  (leiaute/XSD/endpoints); doc do **novo e-SUS AF** (substituição do HÓRUS, calendário de migração);
  RENAME vigente + CATMAT.

---

## 9. Mapa-resumo (ENVIA/CONSOME · periodicidade · formato · financiamento)

| Sistema | Direção | Periodicidade | Formato/canal | Vínculo financiamento | Confiança canal técnico |
|---|---|---|---|---|---|
| **SIOPS** | ENVIA (declara) | **Bimestral** (até 30 dias após bimestre) | App SIOPS / `[importação a confirmar]` | **Mín. 15% ASPS** (LC 141/2012); descumpre→bloqueio | BAIXA `[a confirmar]` |
| **FNS (fundo a fundo)** | CONSOME (recebe) + presta contas | Mensal (parcelas) | Extrato/painéis FNS `[arquivo/API a confirmar]` | É o próprio repasse; blocos Custeio/Investimento (Port. 3.992/2017) | MÉDIA |
| **RNDS** | ENVIA + CONSOME | Por evento (tempo real) | **HL7 FHIR**, REST, **ICP-Brasil A1**, token 30min | Habilitador de saúde digital | MÉDIA (perfis: BAIXA) |
| **e-SUS APS / SISAB** | ENVIA | Regular; **fechamento quadrimestral** | Remessa e-SUS→SISAB `[formato a confirmar]` | **Previne/Port. 3.493/2024** (forte) | MÉDIA |
| **SI-PNI** | ENVIA (via RNDS/SISAB) | Por evento (latência ~30d se indireto) | FHIR (RNDS) / SI-PNI | Indicadores APS (indireto) | MÉDIA |
| **CNES** | ENVIA | **Mensal** (mín.) ou a cada alteração | App SCNES / `[importação a confirmar]` | **Pré-condição de todo repasse** (equipes/leitos) | MÉDIA |
| **SISREG** | CONSOME (regula) | Filas ~24h | Web / `[API a confirmar]` | Produção regulada→MAC (indireto) | BAIXA |
| **HÓRUS→e-SUS AF / BNAFAR** | ENVIA | **Diária** | Web service BNAFAR `[XSD a confirmar]` | CBAF / farmácia (condição) | MÉDIA |

---

## 10. Pendências consolidadas `[a confirmar — obter doc oficial]`

**Bloqueadores de fidelidade (não implementar fiel sem o doc):**
1. **SIOPS:** manual técnico + tabela oficial de classificação ASPS (LC 141 arts. 3º/4º) + canal de
   transmissão (importação por arquivo? web service?) + calendário de bimestres.
2. **FNS:** Portarias 204/2007 e 3.992/2017 atualizadas + existência de API/arquivo de parcelas por
   bloco/ação + regras de prestação de contas fundo a fundo (RAG/SARGSUS).
3. **RNDS:** Manual de Integração do Barramento (vigente) + Guia RNDS seção "Modelos" com a **lista
   completa de perfis/Bundles FHIR** + cenários obrigatórios atuais + nomes exatos de headers.
4. **e-SUS APS/SISAB + Previne:** **Portaria GM/MS 3.493/2024 + anexos** (valores R$/equipe, IED,
   6 componentes, indicadores) + manual de integração SISAB (formato de remessa) + doc **SIAPS**.
5. **CNES:** Portaria de Consolidação nº 01/2017 (arts. 371–372) + leiaute/importação CNES.
6. **SISREG:** confirmar **se existe API** (ou apenas interface web) + manuais III.
7. **HÓRUS/e-SUS AF:** **Portaria GM/MS 5.713/2024** + manual web service **BNAFAR** (XSD/endpoints)
   + cronograma de substituição HÓRUS→e-SUS AF.

**Parametrizável por tenant (é lei/portaria — nunca hardcoded):**
- Percentual mínimo ASPS (default legal 15%, Lei Orgânica pode ser maior) — por tenant+vigência.
- Valores/fórmulas/indicadores do cofinanciamento APS (mudam por portaria) — por vigência.
- Calendário de prazos (bimestres SIOPS, quadrimestres SISAB) — parametrizável.
- Taxonomia de blocos/ações FNS e classificação ASPS — versionada por vigência.

**Decisões de escopo a validar com produto:**
- Saúde M7 = primariamente **gestão do FMS + integração de envio** (não reconstruir e-SUS/prontuário).
- Reuso de infra existente: **e-CNPJ A1 + Key Vault (M2)** para RNDS; **PCASP/fonte vinculada (M2)**
  para FMS/SIOPS; **RH (M5)** para profissionais×CNES; **prestação de contas (M4)** para RAG/SIOPS.

---

## Fontes (oficiais e de apoio)

- RNDS — Ministério da Saúde: https://www.gov.br/saude/pt-br/composicao/seidigi/rnds
- Guia RNDS (introdução): https://rnds-guia.saude.gov.br/docs/introducao/
- Manual de Integração da RNDS (Barramento) — DATASUS: https://datasus.saude.gov.br/wp-content/uploads/2020/04/SOA-RNDS_ManualIntegracaoBarramento_vSite.pdf
- Catálogo de APIs gov.br — RNDS: https://www.gov.br/conecta/catalogo/apis/rnds-rede-nacional-de-dados-em-saude
- COSEMS/SP — RNDS oficializada / CPF como identificador: https://www.cosemssp.org.br/noticias/ministerio-da-saude-oficializa-rnds-como-plataforma-do-sus-e-adota-cpf-como-identificador-nacional/
- SISAB / e-SUS APS: https://sisab.saude.gov.br/ · https://sisaps.saude.gov.br/sistemas/sisab/
- Financiamento APS (componentes) — SAPS/MS: https://aps.saude.gov.br/gestor/financiamento/componentesfinanciamento/
- Novo modelo de cofinanciamento APS (Portaria 3.493/2024) — FAQ MS: https://www.gov.br/saude/pt-br/composicao/saps/esf/faq-novo-modelo-de-cofinanciamento-federal-da-aps
- Portaria 3.493/2024 (apresentação): https://www.cosemssp.org.br/wp-content/uploads/2024/07/PT-3493-MARINA-MELO.pdf
- SIAPS / indicadores de cofinanciamento — CONASEMS: https://portal.conasems.org.br/noticias/1266_entenda-o-siaps-e-como-registrar-os-novos-indicadores-de-cofinanciamento-da-aps
- SIOPS — Ministério da Saúde: https://www.gov.br/saude/pt-br/acesso-a-informacao/siops
- SIOPS — FAQ MS: https://www.gov.br/saude/pt-br/acesso-a-informacao/siops/faq
- SIOPS — cartilha COSEMS/SP 2023: https://www.cosemssp.org.br/wp-content/uploads/2023/10/cartilha-SIOPS-2023.pdf
- FNS — Fundo a Fundo: https://portalfns.saude.gov.br/fundo-a-fundo/
- FNS — Legislação (Port. 204/2007, 3.992/2017): https://portalfns.saude.gov.br/legislacao/
- FNS — Regras gerais financiamento (CONASEMS): https://conasems-ava-prod.s3.sa-east-1.amazonaws.com/institucional/orientacoes/regras-gerais-para-financiamento-e-movimentacao-recursos-federais-1-1695049745.pdf
- CNES — DATASUS: http://cnes.datasus.gov.br/ · Wiki CNES: https://wiki.saude.gov.br/cnes/
- CNES — atualização cadastral (arts. 371/372 Port. Consolidação 01/2017): https://www.cosemssp.org.br/noticias/atualizacao-de-dados-cadastro-nacional-de-estabelecimento-de-saude-cnes/
- SI-PNI — DATASUS: http://pni.datasus.gov.br/ · novo SI-PNI/RNDS (MS): https://www.gov.br/saude/pt-br/assuntos/noticias/2023/junho/entenda-o-novo-sistema-de-informacao-do-programa-nacional-de-imunizacoes
- SI-PNI integração RNDS/SISAB (COSEMS/SP): https://www.cosemssp.org.br/wp-content/uploads/2024/09/APRESENTACAO-JOSE-RNDS.pdf
- SISREG — CONASS: https://www.conass.org.br/guiainformacao/o-sisreg/ · Wiki SISREG: https://wiki.saude.gov.br/SISREG/
- BNAFAR — Ministério da Saúde: https://www.gov.br/saude/pt-br/composicao/sectics/daf/bnafar
- HÓRUS → novo e-SUS AF (MS, abr/2026): https://www.gov.br/saude/pt-br/assuntos/noticias/2026/abril/ministerio-da-saude-apresenta-novo-sistema-de-assistencia-farmaceutica-no-sus
- Portaria GM/MS 5.713/2024 (BNAFAR): https://bvsms.saude.gov.br/bvs/saudelegis/gm/2024/prt5713_17_12_2024.html
- BNAFAR — web service (Ajuda SAF/SIGAF): https://ajudasaf.saude.mg.gov.br/54273-2/sigaf/webservice/bnafar/
