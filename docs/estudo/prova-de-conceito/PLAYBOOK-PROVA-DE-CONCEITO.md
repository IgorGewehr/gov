# PLAYBOOK — Prova de Conceito / Prova de Aderência em Licitações

> **Objetivo:** guia operacional para a Tensorroot.Gov enfrentar PROVAS DE CONCEITO (PoC) /
> provas de aderência / testes de conformidade / amostras em licitações de software de gestão
> pública (Lei 14.133/2021), nos tenants **Executivo (Prefeitura)** e **Legislativo (Câmara)**.
> **Regra:** toda afirmação tem FONTE (URL/edital/acórdão) ou está marcada `[a confirmar]`.
> **Marcação de frequência:** cada requisito é **[RECORRENTE]** (presente em ≥2 editais reais
> independentes — "obrigatório de facto") ou **[OCASIONAL]** (visto em 1 edital ou específico de
> um tipo de objeto).
> Consolidado de: `pesquisa-poc-base-legal-processo.md`, `pesquisa-requisitos-municipal.md`,
> `pesquisa-requisitos-legislativo.md`, `pesquisa-roteiro-pontuacao.md`,
> `verificacao-poc-base-legal-processo.md`, `verificacao-requisitos-municipal.md`,
> `verificacao-requisitos-legislativo.md`. Cruzar prontidão com `AUTOAVALIACAO-POC.md`.
> Data: 2026-06-22.

---

## PARTE 1 — O QUE É, BASE LEGAL, PROCESSO E O QUE REPROVA

### 1.1 Definições

- **Prova de Conceito (PoC):** etapa do certame, regrada **previamente no edital**, na qual se
  aplica metodologia para verificar se a proposta do licitante **classificado em 1º lugar**
  contempla todos os requisitos necessários à satisfação da necessidade pública. Valida na prática
  o que o TR pediu na teoria, evitando contratar objeto inservível. FONTE: TCU 5.4.1.2
  (https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/).
- **Prova de aderência / Teste de Conformidade:** sinônimos práticos de PoC focada no atendimento
  **item a item** das especificações do TR. A Lei usa o verbo "comprovar a aderência às
  especificações". Em editais aparece como "Teste de Conformidade" (Mata/RS §7), "Avaliação dos
  Requisitos" (Riqueza/SC §14.6), "Amostragem" (Carbonita/MG §3.10). `[a confirmar]` — não há
  definição legal autônoma; é nomenclatura de edital.
- **Amostra / homologação de amostras / exame de conformidade:** institutos correlatos (art. 17,
  §3º). PoC é a modalidade típica para software (objeto imaterial). FONTE: TCU 5.4.1.2.

### 1.2 Base legal (Lei 14.133/2021)

| Dispositivo | Conteúdo | Fonte |
|---|---|---|
| **Art. 17, §3º** ✅ **literal conferido** | "Desde que previsto no edital, na fase a que se refere o inciso IV [julgamento], (...) **em relação ao licitante provisoriamente vencedor**, realizar análise e avaliação da conformidade da proposta, mediante **homologação de amostras, exame de conformidade e prova de conceito** (...) de modo a **comprovar sua aderência às especificações** definidas no TR ou projeto básico." (núcleo legal) | Planalto / TCE-SP |
| **Art. 41, II** ✅ **literal conferido** | "exigir **amostra ou prova de conceito do bem** no procedimento de pré-qualificação permanente, **na fase de julgamento das propostas ou de lances**, ou no período de vigência do contrato/ata, **desde que previsto no edital** e **justificada a necessidade**." ⚠️ **o inciso diz "ou de lances", NÃO "habilitação"** (correção da pesquisa). | TCE-SP art. 41 |
| **Art. 41, parágrafo único** ✅ **literal conferido** | "A exigência (...) **restringir-se-á ao licitante provisoriamente vencedor** quando realizada na fase de julgamento das propostas ou de lances." | TCE-SP art. 41 |
| **Art. 42, §§2º-3º** ⏳ `[a confirmar]` | Admite **protótipo** e exame por **instituição especializada** (laboratório/órgão técnico). §/inciso exato a reconferir no Planalto. | TCU 5.4.1.2 |
| **Art. 59** | Desclassificação de proposta em desacordo com especificações/prazos/condições do edital (cláusula geral). | Edital Pato Branco/PR |

FONTE da lei: Planalto (https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm);
TCE-SP legislação comentada art. 41 (https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/41).
**Art. 17 §3º, 41 II e 41 par. único conferidos em fonte primária** (ver `verificacao-poc-base-legal-processo.md`).
**Atenção:** amostra/PoC é instrumento de **julgamento da proposta**, NÃO de habilitação — a Nova Lei
positivou a antiga jurisprudência do TCU de que "PoC não é condição de habilitação".

### 1.3 Processo (como funciona na prática)

1. **Convocação só do 1º colocado provisório**, na fase de julgamento — nunca como habilitação de
   todos. Se reprovado/recusar → desclassifica e convoca o 2º, **sucessivamente**. FONTE: TCU
   5.4.1.2; Acórdão 2640/2019-TCU-Plenário.
2. **O edital DEVE conter** (requisitos de validade): (a) data/horário/local; (b) **roteiro
   detalhado** item a item; (c) **critérios objetivos** de aceitação; (d) **justificativa** da
   necessidade. FONTE: TCU 5.4.1.2; Zênite (https://zenite.blog.br/prova-de-conceito-poc-cautelas-necessarias/).
3. **Banca técnica nomeada** (Comissão Avaliadora / CTA) afere item a item e emite **laudo/relatório**.
   Ex.: SEFAZ-MS com 6 servidores nominados; IFB com Equipe de Apoio + Equipe Técnica. FONTE: SEFAZ-MS
   Roteiro PoC; IFB Relatório PoC.
4. **Demais licitantes podem acompanhar** (publicidade/isonomia), mediante registro formal junto ao
   pregoeiro (1 fiscal por licitante). Negar acompanhamento é ilegal. FONTE: Acórdão 1823/2017-TCU;
   Carbonita/MG §3.10.6.
5. **Ambiente:** presencial na sede do órgão é o padrão (SEFAZ-MS); **remoto/gravado é aceito** (IFB
   por Conferência Web RNP; Riqueza/SC §14.6.23 admite videoconferência). A licitante opera o sistema.
   FONTE: SEFAZ-MS; IFB; Riqueza/SC.
6. **Prazo:** sem padronização legal; típico **até 15 dias** entre convocação/preparação/execução
   (IFB §7.21). Sessão real medida: IFB ~4h40 para ~45 requisitos (~6 min/item). Prazo exíguo (48h)
   restringe competição — Acórdão 6638/2015. FONTE: IFB; TCU.
7. **Resultado:** laudo de conforme/não-conforme → vencedor ou desclassificado. Alguns editais
   admitem **ressalva + correção em 3 dias úteis** (IFB §7.27-7.28); outros são **binários puros sem
   ressalva** (SEFAZ-MS §11.5.2.6). **A regra muda por edital — ler sempre a cláusula.** FONTE: IFB; SEFAZ-MS.

### 1.4 Percentual de aderência (obrigatórios vs desejáveis)

A Lei **não fixa percentual**; cada TR define. Padrão de mercado **confirmado em 3 editais
independentes (MG + SC + RS)**:

- **Requisitos obrigatórios / "gerais da tecnologia" / técnicos = 100%** — qualquer falha
  desclassifica (eliminatório). FONTE: Carbonita/MG §3.10.12; Riqueza/SC §14.6.4; **Mata/RS §7.11**.
- **Módulos = 90–95%** — corte por módulo. FONTE: Carbonita/MG §3.10.11 (95%); Riqueza/SC §14.6.8
  (90%); **Mata/RS §7.13 (95%)**.
- **Diferimento do resíduo (até 5%)** entregável em prazo pós-contrato: Carbonita/MG **120 dias**;
  Mata/RS **30 dias**. FONTE: Carbonita/MG §3.10.13; Mata/RS §7.13.
- **Modelo SEFAZ-MS (mais robusto):** técnicos **100% sem falha** + funcionais **≤5% de ajuste**
  desde que já parcialmente implementados; estourar 5% ou falhar 1 técnico = desclassificação.
  FONTE: SEFAZ-MS §11.5.2.7-11.5.2.9.
- **Julgamento binário** (Atende/Não Atende), sem notas/pesos/gradação no modelo SEFAZ-MS; a coluna
  "atende parcialmente" só vale em habilitação técnica, não na PoC. FONTE: SEFAZ-MS §11.5.2.6.
  - ⚠️ "100% dos obrigatórios + 80% dos desejáveis" aparece em síntese de busca **sem edital-fonte
    nominado** → `[a confirmar]`. O que protege o edital não é o número, mas listar objetivamente
    quais itens são 100% e quais admitem tolerância. FONTE: GestGov/NELCA.

### 1.5 O que REPROVA o fornecedor

1. **Depender de customização/código na hora** — o requisito tem de ser **nativo/parametrizável**.
   "Não será permitido desenvolver, editar, corrigir ou ajustar durante a apresentação." FONTE:
   Riqueza/SC §14.6.9; SEFAZ-MS §11.5.2.5-11.5.2.6; Mata/RS §7.16.
2. **Falhar 1 requisito obrigatório/técnico** ou **estourar a tolerância** (>5% funcionais). FONTE:
   Carbonita/MG §3.10.12; SEFAZ-MS §11.5.2.9; Mata/RS §7.11.
3. **Não atingir o corte por módulo** (90–95%). FONTE: Riqueza/SC §14.6.8; Mata/RS §7.13.
4. **Mexer em equipamento intocável** durante a amostragem → encerramento; itens não apresentados =
   ausentes. FONTE: Carbonita/MG §3.10.16.
5. **Não comparecer / recusar / fora das condições do TR** → proposta não aceita. FONTE: IFB §7.30; TCU.
6. **Demonstrar tela/módulo diferente do ofertado na proposta** → vedado aceitar produto diferente da
   amostra. FONTE: Acórdão 2611/2016-TCU.

### 1.6 O que ANULA/vicia a PoC (base para impugnação preventiva — a nosso favor)

- **PoC facultativa** ou **sem especificar pontos avaliados** → Acórdãos 3355/2024, 2992/2016.
- **Critérios vagos** ("todos os testes necessários") → Acórdão 529/2018.
- **Exigir PoC de vários licitantes** (não só o 1º) → Acórdãos 2640/2019, 2096/2015.
- **Sem data/horário/local** → Acórdão 2796/2013. **Não divulgar resultado / não permitir
  acompanhamento** → Acórdãos 2401/2019, 1823/2017. **Prazo exíguo (48h)** → Acórdão 6638/2015.
  **Dispensar amostra prevista** → Acórdão 1948/2019. **PoC antes de definir especificações mínimas**
  (direcionamento) → Acórdão 2059/2017.
- FONTE: TCU 5.4.1.2. `[a confirmar]` enunciado literal de cada acórdão em pesquisa.apps.tcu.gov.br
  antes de usar em peça; verificar também jurisprudência **TCE-RS** (não coberta nesta rodada).

---

## PARTE 2 — CHECKLIST: GESTÃO MUNICIPAL (EXECUTIVO)

> Base verificada campo a campo: **E1 Carbonita/MG** (18.425 linhas), **E2 Riqueza/SC**, **Mata/RS
> PE 21/2021** (9.033 linhas, fonte do RS — UF-alvo). Frequência: [RECORRENTE] = em ≥2 dessas fontes.

### 2.1 Regras de avaliação (universais nos 3)
- [ ] **[RECORRENTE]** PoC/Teste de Conformidade **eliminatório**, só do 1º classificado, banca
  nomeada. FONTE: Carbonita §3.10; Riqueza §14.6; Mata §7.1.
- [ ] **[RECORRENTE]** **100% dos requisitos obrigatórios gerais** sob pena de desclassificação.
  FONTE: Carbonita §3.10.12; Riqueza §14.6.4; Mata §7.11.
- [ ] **[RECORRENTE]** **Corte 90–95% por módulo** + **5% diferível** (120 d Carbonita / 30 d Mata).
  FONTE: Carbonita §3.10.11/3.10.13; Riqueza §14.6.8; Mata §7.13.

### 2.2 Arquitetura / técnico (frequentemente "obrigatório geral" = 100%)
- [ ] **[RECORRENTE]** **100% web nativo, multicamadas, banco único, SaaS, sem limite de usuários
  simultâneos**. FONTE: Carbonita item 1/42; Riqueza §1; Mata §5.
- [ ] **[RECORRENTE]** **Vedação a desktop cliente-servidor emulado** em navegador/área de trabalho
  remota → **DIFERENCIAL DIRETO da nossa SPA React + API .NET**. FONTE: Riqueza §1.b; Mata §7.9 +
  "funcionamento sem o uso de emuladores".
- [ ] **[RECORRENTE]** **Multi-navegador padrão (Chrome/Firefox/Edge/Safari), sem plugins/applets
  NPAPI**, front-end leve (JSON). FONTE: Riqueza §1; Mata §7.5.
- [ ] **[OCASIONAL]** **Bloqueio mensal escalonado** (módulos só abrem o mês se a contabilidade
  abriu) — só Carbonita item 59, mas **arquiteturalmente importante** (fechamento contábil governa
  os demais). FONTE: Carbonita item 59.
- [ ] **[RECORRENTE]** **Cadastro único** / informação alimentada uma única vez, compartilhado entre
  Contabilidade, Pessoal, Compras, Almoxarifado, Frota, Tributos, Patrimônio. FONTE: Carbonita item
  57/72; Mata (integração de módulos).
- [ ] **[OCASIONAL]** Redimensionamento elástico CPU/RAM/disco/link. FONTE: Carbonita itens 3-8.

### 2.3 Contábil / Orçamentário (NÚCLEO da PoC — módulo mais cobrado)
- [ ] **[RECORRENTE]** **PPA / LDO / LOA** com histórico e legislação de autorização. FONTE:
  Carbonita; Riqueza (habilitação técnica); Mata.
- [ ] **[RECORRENTE]** **Ciclo Lei 4.320:** Empenho → Em Liquidação → Liquidação → Pagamento;
  anulação de empenho com motivo; bloqueio/desbloqueio de dotação (art. 9º LRF). FONTE: Carbonita
  itens 143-144; Mata §6.1.x.
- [ ] **[RECORRENTE]** **Aderência PCASP/MCASP/NBC TSP nos moldes do SIAFIC** (sistema único e
  integrado). FONTE: Carbonita item 73; Mata §6.1.40.
- [ ] **[OCASIONAL]** Balancete mensal, balanço anual (anexos Lei 4.320/LRF/TCE), razões analíticas,
  Livro Diário com termos. FONTE: Carbonita itens 146-188.
- [ ] **[OCASIONAL]** **RREO, RGF, Restos a Pagar** (processado/não processado), limites
  constitucionais (Saúde/Educação/Pessoal — LRF 19/20, art. 29-A CF) com gráficos. FONTE: Carbonita
  itens 51, 157, 170, 190-191.
- [ ] **[OCASIONAL]** SIOPS / SIOPE; adiantamentos, convênios, precatórios, PPP. FONTE: Carbonita
  itens 158/159, 142, 171-183.

### 2.4 Tesouraria / Financeiro
- [ ] **[OCASIONAL]** **Conciliação bancária automática** ("Fluxo Monetário"). FONTE: Riqueza
  (habilitação técnica).
- [ ] **[OCASIONAL]** **Borderô OBN / CNAB** (convênio bancário) — só Carbonita item 68.
- [ ] **[RECORRENTE]** **Guias com QR Code + PIX via API/webservice** (cobrança registrada). FONTE:
  Carbonita item 70; Riqueza (cobrança registrada/pix); Mata (arrecadação).
- [ ] **[OCASIONAL]** Programação financeira / cotas de desembolso (art. 9º LRF). FONTE: Carbonita
  itens 166/167.

### 2.5 Tributos / Arrecadação
- [ ] **[RECORRENTE]** **IPTU, ISS, ITBI, Taxas** + emissão de carnê. FONTE: Carbonita; Riqueza; Mata.
- [ ] **[RECORRENTE]** **Dívida Ativa, CDA, cobrança, CND / validação de CND**. FONTE: Carbonita
  (WEB Cidadão); Mata (dívida ativa).
- [ ] **[RECORRENTE]** ⚠️ **NFS-e / escrituração fiscal / Declaração de ISS** — **RISCO DE ADERÊNCIA
  ALTO**: Carbonita (item 1375, escriturar NFS-e) e Mata ("NFS-e", "escrita fiscal eletrônica")
  **presumem emissão/escrituração ATIVA**, enquanto nosso modelo é **ingestão passiva via ADN**
  (CLAUDE.md §8). `[a confirmar]` se o edital-alvo exige emissão ABRASF ou só consulta/dívida ativa.
  FONTE: Carbonita item 1375; Mata; verificacao-requisitos-municipal §4.

### 2.6 RH / Folha / eSocial
- [ ] **[RECORRENTE]** **Folha mensal, férias, 13º, rescisão, extra-folha**, Portal do Servidor,
  contracheque online, subsídios. FONTE: Carbonita; Riqueza; Mata.
- [ ] **[RECORRENTE]** **eSocial integrado** — bloco inteiro **obrigatório** em Carbonita (itens
  **549-565**, não "548-569"): ambientes produção/restrita, importar folha, salvar XML dos eventos
  (item 555), A1/A3, lote, retorno/erros, logs, faseamento (item 565). FONTE: Carbonita itens 549-565
  (numeração corrigida na verificação); Mata/Riqueza (folha+eSocial).
- [ ] **[OCASIONAL]** **Status visual do eSocial na folha**; **qualificação cadastral** (individual/
  lote); **EFD-REINF** (eventos R-1000…R-9000). FONTE: Carbonita itens 62, 63, 75, 326-347.
- [ ] **[OCASIONAL]** RPPS (Port. 916 MPS). FONTE: Carbonita item 162.
- [ ] **[INCERTO/`a confirmar`]** **Ponto eletrônico (Port. MTP 671/2021)** — **não localizado como
  item de PoC** em Carbonita/Riqueza/Mata. Está no nosso escopo, mas sem fonte de amostra. FONTE:
  verificacao-requisitos-municipal §7.

### 2.7 Compras / Licitações / Contratos / PNCP
- [ ] **[RECORRENTE]** **Licitações Lei 14.133, Pregão Eletrônico, Contratos, Aditivos, Obras**.
  FONTE: Carbonita; Riqueza; Mata.
- [ ] **[RECORRENTE]** **Integração PNCP** (art. 176 Lei 14.133) + PCA. FONTE: Carbonita; Riqueza.
- [ ] **[OCASIONAL]** Integração com **≥5 plataformas** de pregão; importação de cotação/processo de
  exercício anterior. FONTE: Carbonita itens 60, 64.

### 2.8 Patrimônio / Frota / Almoxarifado
- [ ] **[RECORRENTE]** **Patrimônio** com tombamento + **depreciação MCASP**, **Almoxarifado**,
  **Frota/combustíveis**. FONTE: Carbonita; Riqueza; Mata.
- [ ] **[OCASIONAL]** **Leitor de código de barras** na liquidação de NF. FONTE: Carbonita item 66.

### 2.9 Protocolo / Processo digital
- [ ] **[RECORRENTE]** **Processo administrativo eletrônico / Processos Digitais com assinatura
  eletrônica nativa** (Lei 14.063/2020). FONTE: Carbonita; Riqueza; Mata.
- [ ] **[OCASIONAL]** Workflow BPMN (raias/eventos), versionamento, histórico. FONTE: Riqueza.
- [ ] **[OCASIONAL]** Ouvidoria (Lei 13.460/2017), Central de Atendimento. FONTE: Carbonita.

### 2.10 Transparência / Portal do Cidadão
- [ ] **[RECORRENTE]** **Portal da Transparência LAI** (publicação automática/diária — LC 101/131,
  Lei 12.527, Dec. 7.724) + **e-SIC**. FONTE: Carbonita itens 1529-1541 (numeração corrigida);
  Riqueza; Mata.
- [ ] **[RECORRENTE]** **Portal do Cidadão / autoatendimento** (IPTU, CND, contracheque, protocolo) +
  **app mobile**. FONTE: Carbonita (WEB Cidadão); Riqueza (app mobilidade); Mata (app Android/iOS).
- [ ] **[OCASIONAL]** Exportação .json/.csv/.xml/.pdf. FONTE: Carbonita item 1539.

### 2.11 Compliance / Segurança / LGPD
- [ ] **[RECORRENTE]** **RBAC / permissões individualizadas** por usuário e função; **log de
  transações (Inclusão/Alteração/Exclusão/Consulta)** por usuário; auditoria em tempo real. FONTE:
  Carbonita itens 48/49/71; Riqueza; Mata.
- [ ] **[RECORRENTE]** **LGPD operacional**: termos de uso por perfil/serviço, inventário/RoPA com
  hipótese legal, área do titular, consentimento + webservice, DPO no portal, cookies no 1º acesso
  auditado, plano de incidentes (art. 48). FONTE: Riqueza LGPD a-j; Mata (LGPD); Carbonita.
- [ ] **[OCASIONAL]** **HTTPS/SSL** com redirecionamento automático; anti-SQLi + anti-DDoS na app;
  eBGP ≥2 operadoras. FONTE: Carbonita itens 11-13; Riqueza §2.5.
- [ ] **[OCASIONAL]** Firewall NGFW (Gartner MQ), IPS/WAF/SD-WAN/GeoIP/SOC — **exigência de
  infra de hospedagem**, atende-se via provedor de nuvem, não é software. Só Carbonita. FONTE:
  Carbonita itens 10, 14-36.
- [ ] **[RECORRENTE]** **SGBD transacional + backup diário + recovery** preservando última transação.
  FONTE: Carbonita itens 37/38/45.

### 2.12 Integrações governamentais (recorrentes, mas frequentemente DISPENSÁVEIS na PoC)
> ⚠️ **Mitigação:** Riqueza §14.6.6 dispensa da avaliação na PoC os requisitos que dependem de
> integração com sistemas em uso (viram obrigação contratual de implantação). Reduz risco na demo.
- [ ] **[RECORRENTE]** **TCE estadual** (SICOM/MG; "TCE/SC"; **TCE-RS** em Mata). **No alvo: SIAPC/PAD.**
  `[a confirmar]` leiaute SIAPC/PAD detalhado — nenhum edital lido o traz; buscar manual no portal
  TCE-RS. FONTE: Carbonita item 74; Riqueza; Mata §2.21-2.22.
- [ ] **[OCASIONAL]** **SICONFI/STN (MSC)**; **PNCP**; **PIX/OBN/CNAB**; SIOPS/SIOPE; certificado
  digital A1/A3 para assinaturas. FONTE: Carbonita itens 68/70/74, 158/159, 61.

---

## PARTE 3 — CHECKLIST: GESTÃO LEGISLATIVA (CÂMARA)

> Base verificada: **E2 Vitória/ES** (TR votação, 1.603 linhas), **E3 Curitiba/PR** (ETP, 1.866
> linhas), **SAPL/Interlegis** (baseline), **NOVO-A CMBelacruz/CE** (TR ERP de Câmara), **NOVO-B
> Salto/SP** (PoC real de Câmara). E1 Matão/SP confirmado só pela página oficial (Memorial não
> extraído). Frequência: [RECORRENTE] = em ≥2 dessas fontes.

### 3.1 Processo Legislativo (NÚCLEO)
- [ ] **[RECORRENTE]** **Proposições + tramitação + protocolo web** (PL, requerimento, indicação,
  moção, emenda — `[a confirmar]` tipos nominais). FONTE: Matão (protocolo web); Curitiba (SPL);
  SAPL.
- [ ] **[RECORRENTE]** **Comissões: cadastro de membros efetivos/suplentes + atas/pautas de reunião**.
  FONTE: Vitória l.329 (literal "3.13"); Curitiba l.649; SAPL.
- [ ] **[OCASIONAL]** **Pareceres / instruções** de comissões. FONTE: Curitiba l.649. `[a confirmar]`
  exigência explícita em Matão.
- [ ] **[RECORRENTE]** **Assinatura / certificado digital** nas peças (Lei 14.063/2020). FONTE: Matão.
- [ ] **[OCASIONAL]** Emendas a proposições — só baseline SAPL, sem item textual em edital. FONTE: SAPL.

### 3.2 Sessões (pauta / ordem do dia / ata / presença) — NÚCLEO
- [ ] **[RECORRENTE]** **Gerenciamento de sessão** (todas as etapas) + **pauta editável** + **geração
  automática da Ordem do Dia** via tramitação. FONTE: Vitória l.76/91/981.
- [ ] **[RECORRENTE]** **Registro de presença + recomposição de quórum** (cancelar quórum anterior,
  novo registro). FONTE: Vitória l.512-513 (literal "3.44"); Curitiba.
- [ ] **[RECORRENTE]** **Geração e emissão automática da Ata Sintética** ao fim da sessão (e reuniões
  de comissão). FONTE: Vitória l.149 (literal "2.1.8"), l.926-927.
- [ ] **[RECORRENTE]** **Cronômetro** de tribuna/apartes + **inscrição de oradores** (tribuna,
  apartes, questão de ordem, grande expediente). FONTE: Vitória l.462/730; Curitiba l.75/80.
- [ ] **[OCASIONAL]** Tipos de sessão: preparatórias, ordinárias, extraordinárias, solenes, especiais.
  FONTE: Curitiba l.64-66.

### 3.3 Votação (painel / nominal / simbólica)
- [ ] **[RECORRENTE]** **Votação eletrônica nominal** com **abrir/fechar/cancelar** + resultado em
  painel; adicionar/remover votação da pauta. FONTE: Vitória l.474-479; Curitiba l.222-223 (literal);
  Matão.
- [ ] **[RECORRENTE]** **Painel apregoador/eletrônico**: presentes/ausentes/licenciados (cor
  configurável), orador/aparteante, resultado, totalizadores, multimídia/TV Câmara. FONTE: Vitória
  l.126/730; Curitiba.
- [ ] **[OCASIONAL]** Identificar **parlamentares impedidos de votar**. FONTE: Vitória l.509 (literal).
- [ ] **[OCASIONAL — NÃO É GAP]** **Votação simbólica:** **explicitamente NÃO informatizada** em
  Curitiba (l.501). Não tratar como requisito faltante. FONTE: Curitiba l.501.
- [ ] **[OCASIONAL — diferencial, não mínimo]** **Deliberação remota** (presença/voto nominal/tribuna
  online em tempo real) — forte em Curitiba (l.219-224), mas demanda de Câmaras grandes; **não
  universal**. FONTE: Curitiba l.219-224.
- [ ] **[OCASIONAL — condicional ao objeto]** **Terminais físicos + hot-swap + painel LED multimídia**
  — isso é **licitação de hardware+software de plenário** (Vitória), escopo diferente de "software de
  processo legislativo". Nem toda PoC exige integração com terminais. FONTE: Vitória l.820-822.

### 3.4 Transparência legislativa / Portal
- [ ] **[RECORRENTE]** **Portal do cidadão** (Mesa, Comissões, Parlamentares, Ordem do Dia, Sessões,
  Proposições, Normas) + **acompanhamento em tempo real**. FONTE: SAPL; Vitória; Curitiba.
- [ ] **[OCASIONAL]** **Diário Oficial eletrônico**. FONTE: Matão.
- [ ] **[OCASIONAL]** **Transmissão das sessões** (TV Câmara/internet) integrada ao painel. FONTE:
  Vitória l.764-768; Curitiba l.1601.
- [ ] **[OCASIONAL]** Relatórios: presenças por reunião, votações com data/nº, pauta. FONTE: Vitória
  l.519.
- [ ] **[INCERTO/`a confirmar`]** Exigência textual LAI 12.527 / LC 131 — não localizada nos PDFs.

### 3.5 Normas Jurídicas / Legislação
- [ ] **[RECORRENTE]** **Base de leis/normas consultável** + manutenção. FONTE: SAPL; Curitiba l.646.
- [ ] **[OCASIONAL]** Compilação/consolidação de normas, vínculo norma↔proposição. FONTE: SAPL 3.1.
- [ ] **[RARO — REMOVER do caminho crítico]** **LexML**: padrão nacional real, mas **não aparece em
  texto de edital** (Matão/Vitória/Curitiba/SAPL). Não tratar como obrigatório de facto. FONTE:
  verificacao-requisitos-legislativo §2-5.

### 3.6 Contabilidade própria + Prestação ao TCE + Folha/eSocial da Câmara
> A Câmara é **tenant/UO próprio** (CNPJ distinto). Editais de "sistema integrado de Câmara" juntam
> contabilidade + folha + processo legislativo no mesmo objeto/PoC.
- [ ] **[RECORRENTE no segmento ERP de Câmara]** **Contabilidade PCASP própria + prestação de contas**
  (SICONFI/STN, SIOPE/FNDE, SIOPS/MS, MSC, depreciação) + **folha de pagamento + subsídios**.
  CONFIRMADO por edital real (CMBelacruz/CE, l.90/174/194/207/212) e **PoC real** (Salto/SP, módulo
  "gestão de pessoal e folha"). **Vantagem nossa: PCASP/TCE-RS/SICONFI já provados.** FONTE:
  CMBelacruz/CE; Salto/SP.
- [ ] **[PLAUSÍVEL-SEM-FONTE]** **eSocial em Câmara** — comum no mercado, mas **não citado
  nominalmente** nos TRs verificados. `[a confirmar]` item textual.
- [ ] **[a confirmar]** **TCE-RS (SIAPC/PAD) nominal em TR de Câmara gaúcha** — as fontes verificadas
  são SICONFI/STN federais e TCE-SC/SP. Buscar no Licitacon-RS/PNCP.

---

## PARTE 4 — COMO SE PREPARAR

### 4.1 Dados de demonstração (massa de teste)
- **Dois tenants distintos populados:** Prefeitura (Executivo) **e** Câmara (Legislativo) com CNPJs
  distintos, sem vazamento de escopo (ABAC/UO). O isolamento multi-tenant pode virar item técnico de
  "atende/não atende". FONTE: pesquisa-roteiro-pontuacao §8.4.
- **Massa que exercita cada [RECORRENTE]:** exercício orçamentário completo (PPA/LDO/LOA →
  empenho→liquidação→pagamento), cadastros tributários (IPTU/ISS/ITBI + dívida ativa), folha mensal
  fechada, processos de licitação, patrimônio com depreciação, processo digital assinado, e — no
  Legislativo — proposições em tramitação, sessão com pauta/votação/ata.
- **Dados fictícios são a norma**; massa fornecida pelo órgão aparece quando o requisito é migração/
  integração. `[a confirmar]` se o edital-alvo entrega massa padronizada. FONTE: SEFAZ-MS RF.1.01;
  pesquisa-roteiro-pontuacao §4.
- **Regra de ouro:** tudo que será demonstrado **tem de existir na build** (nativo/parametrizável).
  Nada de "codamos na implantação". FONTE: SEFAZ-MS §11.5.2.5-11.5.2.6.

### 4.2 Roteiro de demonstração
- **Mapear cada item do TR → tela/fluxo existente** antes de aceitar a PoC (planilha espelhada no
  quadro do edital: `# | CÓD | MÓDULO | REQUISITO | NATIVO | ATENDE/NÃO`). FONTE:
  pesquisa-roteiro-pontuacao §1.1.
- **Podemos escolher a ordem** de demonstração (não precisa seguir a do edital). FONTE: SEFAZ-MS.
- **Ensaiar sessão cronometrada:** ~5-7 min/requisito; sessão de ~4-5h para ~45 itens. FONTE: IFB
  (~4h40 / ~45 itens).
- **Priorizar 100% dos [RECORRENTE] obrigatórios/técnicos** (falha = eliminação) antes dos
  [OCASIONAL].

### 4.3 Riscos prioritários (do diagnóstico vs editais)
1. **🔴 ALTO — Contábil PCASP/MCASP + ciclo da despesa:** módulo **mais cobrado e universal** (3
   editais) e **bloqueio mensal escalonado** (Carbonita item 59) impõe fechamento contábil governando
   os demais. Falhar = reprovação eliminatória. Status M2/M3/M4 concluídos (ver tasks) — **validar em
   demo**. FONTE: verificacao-requisitos-municipal §9.
2. **🔴 ALTO — NFS-e ativa/escrituração:** aparece em MG **e** RS presumindo emissão/escrituração; nosso
   modelo é **ingestão passiva ADN**. **Validar no edital concreto antes de prometer aderência.** FONTE:
   verificacao-requisitos-municipal §9; CLAUDE.md §8.
3. **🟡 MÉDIO — eSocial + EFD-REINF + A1/A3:** bloco inteiro obrigatório em Carbonita; certificado
   A1/Key Vault precisa estar fiado em runtime. (M5 RH pendente.) FONTE: Carbonita itens 549-565.
4. **🟡 MÉDIO — TCE-RS SIAPC/PAD:** leiaute detalhado não consta nos editais; **mitigado** por
   Riqueza §14.6.6 (integrações de terceiros dispensáveis na PoC, viram obrigação contratual).
5. **🟢 DIFERENCIAL — web nativo sem emulador:** cláusula **literal e repetida** (Riqueza §1.b, Mata
   §7.9). Nossa SPA React + API .NET ataca diretamente — **citável, não marketing**.
6. **Legislativo — gaps a tratar:** comissões+pareceres, normas+Diário Oficial, **Ata Sintética
   automática** (Vitória literal). Deliberação remota e terminais físicos = diferencial/condicional,
   não mínimo. FONTE: verificacao-requisitos-legislativo §5.

### 4.4 Defesa / impugnação preventiva
- Montar **dossiê de impugnação** contra editais com armadilhas pró-incumbente (prazo 48h, critério
  vago, PoC facultativa, sem data/local) usando os acórdãos da §1.6. FONTE: pesquisa-roteiro §6.2.
- Exigir **prazo razoável** (≥5-15 dias) e **acompanhamento** da nossa PoC quando formos o desafiante.
- `[a confirmar]` enunciado literal dos acórdãos TCU e jurisprudência **TCE-RS** antes de protocolar.

---

## Pendências `[a confirmar]` ainda abertas
1. Leiaute oficial atual **SIAPC/PAD do TCE-RS** + versão MSC/SICONFI 2026 (manual técnico no portal TCE-RS).
2. Edital-alvo concreto exige **emissão NFS-e ABRASF** ou só consulta/dívida ativa?
3. **Ponto eletrônico** (Port. MTP 671/2021) como item de PoC — não localizado nas fontes.
4. TR de **Câmara gaúcha** exigindo **SIAPC/PAD** e **eSocial** nominalmente (Licitacon-RS/PNCP).
5. Redação literal de art. 17 §3º / 41 II / 42 §§2º-3º e enunciados dos acórdãos TCU/TCE-RS.
6. Memorial Descritivo de Matão/SP (download bloqueado) para itens numerados de proposições/normas.

## Fontes principais (verificadas campo a campo)
- TCU — Amostra e Prova de Conceito: https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/
- Lei 14.133/2021 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm
- **Mata/RS — PE 21/2021 (fonte do RS)**: https://mata.rs.gov.br/wp-content/uploads/2025/08/PREGAO-ELETRONICO-N-21-2021-SOFTWARE.pdf
- Carbonita/MG — TR Software de Gestão Pública: https://sistema.carbonita.mg.gov.br/UpFiles/licitacoes/818/tr___software_gestao_publica.pdf
- Riqueza/SC — Pregão Sistema Web de Gestão: https://s3cache.dom.sc.gov.br/atos/2024/11/1731530485_lic_965_contratao_de_empresa_para_gesto_de_software__prego_eletronico_servios_1_retificao.pdf
- SEFAZ-MS — Convocação e Roteiro da PoC (modelo 100% técnico / ≤5% funcional): https://www.sefaz.ms.gov.br/wp-content/uploads/2023/05/Convocacao-e-Roteiro-da-POC.pdf
- IFB — Relatório da PoC (tempos/fluxo reais): https://ifb.edu.br/attachments/article/39100/Relat%C3%B3rio%20da%20Prova%20de%20Conceito%20-%20Preg%C3%A3o%20Eletr%C3%B4nico%20N%C2%B0%2090056_2024.pdf
- Vitória/ES — TR Sistema de Votação: https://www.cmv.es.gov.br/uploads/licitacao/2962-termo-de-referencia-e-justificativa-1748288531.pdf
- Curitiba/PR — ETP PA 00264/2024: https://mid-transparencia.curitiba.pr.gov.br/contratos/licitacoes/2024/CMC_2024_PE_19_221519_59219.pdf
- CMBelacruz/CE — TR ERP de Câmara: https://www.cmbelacruz.ce.gov.br/arquivos_download/licitacao/22/114
- Salto/SP — Convocação PoC de Câmara: https://www.camarasalto.sp.gov.br/noticias/3831-convocacao-para-prova-de-conceito
- SAPL/Interlegis: https://www12.senado.leg.br/interlegis/produtos/sapl
