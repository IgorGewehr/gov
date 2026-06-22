# Verificação Cética — Requisitos de PoC em Editais de Gestão Municipal

> **Papel:** auditor de fatos. Este documento confere, **afirmação por afirmação**, o conteúdo de
> `pesquisa-requisitos-municipal.md` contra os **editais reais**, lendo os PDFs campo a campo
> (extração via `pdftotext -layout`), e classifica cada item como:
> **CONFIRMADO** (citação localizada no edital) / **PLAUSÍVEL-SEM-FONTE** (coerente, mas sem
> cláusula localizada) / **INCERTO** (fonte não verificável ou alegação não encontrada).
> Data da verificação: 2026-06-22.

---

## 0. Resumo executivo (TL;DR)

- **A tese central da pesquisa está CONFIRMADA por 3 editais independentes de 3 estados** (MG, SC, RS):
  editais de gestão municipal usam **Teste de Conformidade / Amostra / PoC eliminatória**, com **corte
  de 100% nos "requisitos obrigatórios gerais"** e **~90–95% por módulo**, mais **diferimento** do
  resíduo (5%) por prazo contratual.
- **Achado novo (resolve a pendência nº1 da pesquisa):** encontrei e li um **edital real do RS** —
  **Pregão Eletrônico nº 21/2021, Prefeitura de Mata/RS** — que **reproduz o mesmo regime** (100% gerais
  + 95% por módulo + 5%/30 dias). Isso eleva o padrão de "MG+SC" para **MG+SC+RS**, exatamente a UF-alvo.
- **2 das 3 fontes primárias da pesquisa foram verificadas integralmente** (E1 Carbonita/MG, E2 Riqueza/SC).
  A **3ª (E3 PNCP)** é **INCERTA** — o endpoint citado retorna um binário agregado não parseável; não
  sustenta as afirmações específicas atribuídas a ela.
- **Correções factuais menores** (números de item ligeiramente trocados; E2 é **Riqueza/SC**, não Cerro
  Negro) abaixo. Nenhuma delas derruba uma conclusão.

### Veredito por fonte

| Fonte | Status da verificação | Observação |
|---|---|---|
| **E1 — Carbonita/MG** | ✅ **CONFIRMADA campo a campo** | 18.425 linhas extraídas (bate com "~18 mil"). Todas as regras de PoC e itens obrigatórios citados foram localizados. |
| **E2 — Riqueza/SC** | ✅ **CONFIRMADA campo a campo** | Município = **Riqueza/SC** (CNPJ 95.988.309/0001-48). Cerro Negro/SC é o **mesmo TR-base** (template idêntico). |
| **E3 — PNCP CNPJ 08.539.439/0001-07** | ⚠️ **INCERTA** | Endpoint retorna `application/octet-stream` agregado; não foi possível confirmar objeto nem cláusulas. Não usar como fonte das alegações atribuídas. |
| **REF — TCU (Amostra e PoC)** | ✅ **CONFIRMADA** | Base jurídica (Lei 14.133, art. 41 II; só do 1º classificado; critérios objetivos; demais acompanham). |
| **NOVO — Mata/RS (Pregão 21/2021)** | ✅ **CONFIRMADA (RS)** | Teste de Conformidade eliminatório, 100% gerais, 95% módulo, 5%/30 dias, "nativamente em web". |

---

## 1. Verificação das REGRAS DE POC (seção 1 da pesquisa)

| Afirmação da pesquisa | Veredito | Evidência localizada |
|---|---|---|
| E1 exige **≥95% dos itens de cada módulo** (§3.10.11) | ✅ CONFIRMADO | E1 3.10.11: *"o atendimento de pelo menos 95% dos itens de cada módulo."* |
| E1: itens obrigatórios = **totalidade**, sob pena de desclassificação (§3.10.12) | ✅ CONFIRMADO | E1 3.10.12: *"Os itens presentes nas ESPECIFICAÇÕES TÉCNICAS OBRIGATÓRIAS deverão ser atendidos em sua totalidade, sob pena de desclassificação."* |
| E1: **mínimo 375 pontos** no bloco obrigatório | ✅ CONFIRMADO | E1: *"com pontuação mínima de 375 pontos, anexar nos documentos de Habilitação, sob pena de desclassificação"* (após item 75; "Pontuação Para o Módulo: 375"). |
| E1: matriz Obrig/Desej × Atende/Parcial/Não (**5/4/0** e **3/2/0**) | ✅ CONFIRMADO | E1 3.10.22 (MATRIZ DE PONTUAÇÃO) e 3.10.23: a/b/c = 5/4/0; d/e/f = 3/2/0. |
| E1: **5% diferível em 120 dias** pós-assinatura (§3.10.13) | ✅ CONFIRMADO | E1 3.10.13: *"até o máximo de 5% ... em até 120 (cento e vinte) dias corridos após a data da assinatura do contrato."* |
| E1: equipamentos **intocáveis**; itens não apresentados = ausentes (§3.10.16) | ✅ CONFIRMADO | E1 3.10.16: *"os equipamentos ... deverão manter-se intocáveis ... os itens até então não apresentados, serão considerados como não presentes no sistema."* |
| E1: **ata circunstanciada** + fiscais (§3.10.20) | ✅ CONFIRMADO | E1 3.10.7 (acompanhamento) e 3.10.20 (ata assinada por agente, comissão e licitantes). |
| E2: **Características Gerais Obrigatórias = 100%** (§14.6.4) | ✅ CONFIRMADO | E2 14.6.4: *"deverá atender 100% (cem por cento) dos requisitos ... Características Gerais Obrigatórias (Item 1 e seus subitens do ANEXO III) sob pena de ser reprovada."* |
| E2: falhar nas gerais **bloqueia avaliação de módulos** (§14.6.5) | ✅ CONFIRMADO | E2 14.6.5: *"não se passará a etapa de Avaliação dos Requisitos por módulos ... automaticamente reprovada."* |
| E2: **módulos ≥90%** (§14.6.8) | ✅ CONFIRMADO | E2 14.6.8: *"deve atender no mínimo 90% (noventa por cento) dos requisitos avaliados relacionados aos módulos."* |
| E2: **"sem ajustes e sem contato externo; não desenvolver/editar"** (§14.6.9) | ✅ CONFIRMADO | E2 14.6.9: *"apresentá-los de forma objetiva, sem ajustes e sem contato externo. Não será permitido desenvolver, editar, corrigir ou ajustar os softwares durante a apresentação."* |
| E2: **integrações de terceiros DISPENSADAS na PoC** (§14.6.6) | ✅ CONFIRMADO | E2 14.6.6: *"Aqueles requisitos obrigatórios que dependem da integração com sistemas em uso na Prefeitura não serão avaliados pela Comissão ... poderá depender de algumas customizações ... durante a fase de implantação."* |
| E2: PoC pode ser **remota por videoconferência** | ✅ CONFIRMADO | E2 14.6.23: *"a avaliação poderá ser feita de forma remota, por meio de videoconferência."* |
| TCU: base jurídica da PoC (1º classificado, critérios objetivos) | ✅ CONFIRMADO | TCU/Lei 14.133 art. 41 II; Acórdãos 2640/2019, 529/2018, 1823/2017 (demais licitantes acompanham). |

**Conclusão da seção 1:** **100% das regras de PoC citadas foram localizadas no texto dos editais.** A
pesquisa foi precisa aqui. As regras de corte (100% obrigatórios + 90–95% módulo + diferimento do resíduo)
não são invenção: aparecem **idênticas em estrutura** nos três editais (ver seção 5).

---

## 2. Verificação dos REQUISITOS FUNCIONAIS/TÉCNICOS por item (seções 2–4 da pesquisa)

Itens **Obrigatórios** de E1 citados pela pesquisa — todos localizados com o **texto verbatim**:

| Item E1 | Afirmação | Veredito | Texto localizado (resumo) |
|---|---|---|---|
| 1–9 | 100% web, n-camadas, redimensionamento elástico, consistência campo a campo | ✅ CONFIRMADO | Itens 2–9: dimensionamento de infra, redimensionamento por MB/GB/CPU, integridade referencial (item 9). |
| 10,14–36 | Firewall NGFW (Gartner MQ), IPS, WAF, SD-WAN, GeoIP | ✅ CONFIRMADO | *"NGFW ... estar no Gartner Magic Quadrant"*, IPS, SD-WAN, GeoIP, WAF localizados. |
| 11–13 | HTTPS + SSL + redirecionamento automático | ✅ CONFIRMADO | *"comunicação segura HTTPS"*, *"redirecionados de forma automática e transparente para o protocolo HTTPS"*, *"certificado digital SSL"*. |
| 42 | Multiusuário **sem limite** de acessos | ✅ CONFIRMADO | Item 42: *"diversos usuários ao mesmo tempo, sem limitação de [acessos]."* |
| 57 | **Cadastro único** de credores/fornecedores (lista 7 módulos) | ✅ CONFIRMADO | Item 57: *"Cadastro Único ... Contabilidade, Pessoal, Compras e Licitação, Almoxarifado, Controle de frotas, Tributos e Patrimônio."* |
| 59 | **Bloqueio mensal escalonado** (módulos só abrem se contabilidade abriu) | ✅ CONFIRMADO | Item 59: *"os demais módulos só podem abrir o mês caso a contabilidade esteja com o referente mês aberto."* |
| 60 | Integração com **≥5 plataformas** de pregão eletrônico | ✅ CONFIRMADO | Item 60: *"Integração com no mínimo 5 plataformas diferentes."* |
| 61 | **Certificado digital** para assinaturas (eSocial e demais) | ✅ CONFIRMADO | Item 61: *"integração com certificado Digital para assinaturas ... nos envios do ESocial."* |
| 62 | **Status visual do eSocial** dentro da folha | ✅ CONFIRMADO | Item 62: *"dispositivo informando visualmente os dados [do] Esocial."* |
| 63 | **Qualificação cadastral** (individual/lote, Receita) | ✅ CONFIRMADO | Item 63: *"geração e recebimento dos arquivos de qualificação cadastral seja individual ou por lote."* |
| 64 | **Importação de cotação/processo** de exercício anterior | ✅ CONFIRMADO | Item 64: *"importação dos dados, seja de uma cotação ou de um processo licitatório de exercício anterior."* |
| 66 | **Leitor de código de barras** na liquidação de NF | ✅ CONFIRMADO | Item 66: *"leitor de código de barras para inserção dos dados de Nota fiscal na Liquidação."* |
| 68 | **Borderô OBN/CNAB** (convênio bancário) | ✅ CONFIRMADO | Item 68: *"Borderaux eletrônico OBN para envio e recebimento de pagamentos."* |
| 70 | **QR Code de arrecadação + PIX via API/webservice** | ✅ CONFIRMADO | Item 70: *"guias com o QR code de arrecadação integrada ao pix via API/webservice ... sem ... transferências de arquivos de forma manual."* |
| 71 | **RBAC / permissões individualizadas** por usuário e função | ✅ CONFIRMADO | Item 71: *"permissões de acesso individualizadas por usuário e função."* |
| 72 | **Cadastro único / informação alimentada uma única vez** | ✅ CONFIRMADO | Item 72: *"a informação seja alimentada uma única vez."* |
| 73 | Integração **nos moldes do SIAFIC** + MCASP/STN + NBC TSP | ✅ CONFIRMADO | Item 73: *"nos moldes do SIAFIC ... MCASP ... STN ... NBC TSP vigentes."* |
| 74 | Adaptado a **SICOM/TCE + SICONFI/STN** | ✅ CONFIRMADO **(com ressalva)** | Item 74 localizado, **mas o TCE é o de MG (SICOM/TCEMG)**, não SICOM genérico. SICONFI confirmado. |
| 75 | **EFD-REINF** | ✅ CONFIRMADO | Item 75: *"Escrituração Fiscais de retenções (EFD-REINF)"* + módulo dedicado (itens 326–347, eventos R-1000…R-9000, A1/A3). |
| 548–569 (eSocial) | Bloco eSocial **inteiro obrigatório** (prod./restrita, XML, lote, faseamento) | ✅ CONFIRMADO **(números diferem)** | Bloco localizado em **itens 549–565** (não 548–569): *"Ambiente de produção"*, *"produção restrita"*, *"salvar o XML dos eventos"* (555), *"faseamento do eSocial"* (565). Conteúdo bate; numeração da pesquisa está deslocada. |
| 1528–1543 (Transparência) | LAI/e-SIC, publicação automática, export .json/.csv/.xml/.pdf | ✅ CONFIRMADO **(números diferem)** | Localizado em **itens ~1529–1541**: e-SIC (1530), Lei 12.527 + Dec. 7.724, *"exportar ... .json, .csv, .xml e .pdf"* (1539). |

**Conclusão da seção 2:** todos os itens funcionais/técnicos citados foram **localizados verbatim** em E1.
Há **dois deslocamentos de numeração** (eSocial 549–565 ≠ "548–569"; transparência ~1529–1541 ≠ "1528–1543")
e **uma imprecisão de escopo** (item 73/74 da pesquisa diz "SIAFIC/SICONFI" sem destacar que o TCE de E1 é
**TCEMG**, irrelevante para o nosso alvo RS). Nenhuma altera a substância.

---

## 3. Verificação dos requisitos de E2 (Riqueza/SC) — Web, LGPD, Segurança

| Afirmação (E2) | Veredito | Texto localizado |
|---|---|---|
| **100% web, multicamadas, SaaS, sem limite de usuários** | ✅ CONFIRMADO | E2 §1: *"solução que seja 100% web"*, *"modelo SaaS ... 100% por meio da internet, sem limite de usuários"*; camadas Apresentação/Aplicação/Dados. |
| **Vedação a desktop cliente-servidor emulado** (§1.b) | ✅ CONFIRMADO | E2 §1.b: *"Fica vedado o uso de aplicações tradicionais desktop cliente-servidor (2 camadas) emuladas para serem executadas através de navegador ou por outros meios como área de trabalho remota."* |
| **Sem plugins/applets NPAPI**, navegadores padrão | ✅ CONFIRMADO | E2: *"não poderá ser exigida ... instalação local de runtimes e plugins"*; veda *"recurso NPAPI ... como Applets Java, por questão de segurança."* |
| **LGPD — Termos e Condições de Uso** por perfil/serviço (a) | ✅ CONFIRMADO | E2 LGPD.a (verbatim). |
| **LGPD — Inventário/RoPA de tratamentos com hipótese legal** (b/c) | ✅ CONFIRMADO | E2 LGPD.b: *"inventário dos Tratamentos de Dados Pessoais ... hipótese(s) previstas em lei."* |
| **LGPD — Área do titular** (transparência ativa/passiva, vínculos) (d/e) | ✅ CONFIRMADO | E2 LGPD.d/e (verbatim). |
| **LGPD — Consentimento** verificável + **webservice** para terceiros (f/j) | ✅ CONFIRMADO | E2 LGPD.f e LGPD.j (web-service para verificar consentimento). |
| **LGPD — Controlador + Encarregado (DPO)** no portal; **cookies no 1º acesso** auditado (g/h/i) | ✅ CONFIRMADO | E2 LGPD.g/h/i (verbatim). |
| **Segurança — SQL Injection + DDoS na app; eBGP ≥2 operadoras** | ✅ CONFIRMADO | E2 §2.5.a: *"Enlace eBGP ... com no mínimo 2"* operadoras; §2.5.b: inibir *"SQL Injection e Negação de Serviço."* |
| **Cláusulas LGPD contratuais** (arts. 7/11/14; art. 48 incidentes) | ✅ CONFIRMADO | E2 cap. 6: bases legais arts. 7º/11/14; *"providências dispostas no art. 48 da LGPD."* |

**Conclusão da seção 3:** o capítulo LGPD da pesquisa (§a–j) é **cópia fiel** do edital de Riqueza/SC.
A "vedação a desktop emulado" — apontada como nosso diferencial competitivo — é **cláusula real e literal**,
e **reaparece no edital do RS** ("sem o uso de emuladores", "desenvolvidos nativamente em web" — seção 5).

---

## 4. O que NÃO se sustenta / precisa de ressalva

| Item | Status | Detalhe |
|---|---|---|
| **E3 — TR no PNCP (CNPJ 08.539.439/0001-07)** | ⚠️ **INCERTO** | O endpoint `pncp-api/.../arquivos/1` devolve **binário agregado** (`octet-stream`), não um TR limpo. **Não consegui confirmar** objeto, "prazo de entrega", "Termo de Aceite" nem "vista da amostra pelos demais" atribuídos a E3. **Recomendo remover E3 como fonte** ou substituí-lo por edital parseável (ver Mata/RS). As regras que E3 supostamente sustentava **já estão cobertas por E1/E2/Mata**, então a tese não depende dele. |
| **NFS-e ABRASF (emissão) no edital-alvo** | 🟡 **PLAUSÍVEL-SEM-FONTE / risco real** | E1 exige *"escriture as NFS-e prestadas no município"* (item 1375, Obrigatório) e tem **módulo NFS-e** — mas E1 é MG. **Mata/RS lista "nota fiscal eletrônica de serviços" e "escrita fiscal eletrônica"** como área de relevância. Isso **tensiona** nosso modelo de **ingestão passiva via ADN** (CLAUDE.md §8): vários editais presumem **emissão/escrituração ativa**, não só consulta. **Continua `[a confirmar]` no edital-alvo concreto**, mas o risco de aderência é **confirmado como recorrente**, não hipotético. |
| **Item 74 = "SICOM/TCE"** | 🟢 CONFIRMADO com ressalva | É **TCEMG (SICOM)**. Para o nosso alvo, o equivalente é **TCE-RS (SIAPC/PAD)** — não testado por E1. Mata/RS cita *"padrões exigidos pelo TCE-RS"* genericamente, **sem leiaute SIAPC/PAD detalhado** no corpo lido. |
| **"E2 = Cerro Negro/SC"** | 🟢 Correção factual | E2 é **Riqueza/SC** (CNPJ 95.988.309/0001-48). Cerro Negro/SC usa o **mesmo TR-base** (template idêntico), o que na prática **reforça** o argumento de recorrência (mesmo texto reaproveitado por vários municípios). |
| **Numeração eSocial (548–569) e Transparência (1528–1543)** | 🟢 Correção menor | Conteúdo confirmado; **ranges reais 549–565 e ~1529–1541**. Ajustar citações. |

---

## 5. ACHADO PRINCIPAL — Edital REAL do RS confirma o regime (resolve pendência nº1)

**Pregão Eletrônico nº 21/2021 — Prefeitura Municipal de Mata/RS** (Estado do RS, CNPJ/portal mata.rs.gov.br).
Lido campo a campo (9.033 linhas). **Seção 7 — TESTE DE CONFORMIDADE:**

- **§7.1 — caráter ELIMINATÓRIO:** *"A fase de demonstração dos módulos possui caráter eliminatório,
  portanto, ocorrerá a desclassificação da licitante."*
- **§7.11 — 100% dos REQUISITOS OBRIGATÓRIOS GERAIS DA TECNOLOGIA** (TR item 5): *"O não atendimento de
  qualquer destes requisitos, ensejará a desclassificação imediata."* → **mesma trava de E1/E2.**
- **§7.13 — 95% por módulo + diferimento do resíduo:** *"o atendimento deverá ser em 95%. A adaptação
  total da ferramenta (5% desconformidade, se houver) deverá se dar em até 30 (trinta) dias após a Emissão
  da Ordem de Início."* → **mesmo mecanismo de E1 (5%), prazo 30 dias (vs 120 de E1).**
- **§7.9 — "desenvolvidos nativamente em web"** + **linha 354: "funcionamento sem o uso de emuladores"** →
  **mesma vedação a desktop emulado de E2 §1.b.** Confirma nosso diferencial SPA React em UF-alvo.
- **§7.16 — avaliadores não respondem dúvidas** durante a demo → mesmo espírito de "sem contato externo".
- **Módulos de relevância (habilitação técnica):** Planejamento e orçamento, escrituração contábil, execução
  financeira, **folha de pagamento**, compras e licitações, **patrimônio, frota/combustíveis**, portal da
  transparência, portal de serviços/autoatendimento, **processo digital**, **app Android/iOS**, escrita
  fiscal eletrônica, **NFS-e**, arrecadação, **tributos (IPTU, ITBI, ISS), dívida ativa**; + bloco **Saúde**.
- **PCASP/Lei 4.320:** ciclo empenho→em liquidação→liquidação→pagamento (§6.1.x), regras contábeis PCASP
  (§6.1.40), TCE-RS citado (§2.21–2.22).

> **Implicação:** a tese da pesquisa **não era extrapolação de MG/SC** — o **RS pratica o mesmo modelo**.
> Para a banca de PoC no RS, valem: **100% dos requisitos gerais (eliminatório)**, **95% por módulo**,
> resíduo de 5% diferido (Mata: 30 dias), **web nativo sem emulador**.

---

## 6. REQUISITOS RECORRENTES ("obrigatórios de facto" — aparecem em ≥2 editais independentes)

Critério: presente em **≥2 das 3 fontes verificadas** (E1-MG, E2-SC, Mata-RS). Esses são os que **NÃO se
pode falhar** numa PoC. Ordenados por força de recorrência:

### 6.1 Regras de avaliação (universais — nos 3)
1. **PoC/Teste de Conformidade ELIMINATÓRIO**, só do 1º classificado, banca técnica nomeada — **E1, E2, Mata, TCU**.
2. **100% dos "requisitos obrigatórios gerais"** sob pena de desclassificação imediata — **E1 (3.10.12), E2 (14.6.4), Mata (7.11)**.
3. **Corte percentual por módulo (90–95%)** — **E1 (95%), E2 (90%), Mata (95%)**.
4. **Diferimento do resíduo de 5%** para prazo pós-contrato — **E1 (120 d), Mata (30 d)**.
5. **Sem desenvolver/ajustar/contato externo durante a demo** — **E2 (14.6.9), Mata (7.16)**.

### 6.2 Arquitetura (universais — nos 3)
6. **100% web nativo + vedação a desktop/emulador** — **E2 (§1.b), Mata (7.9 + "sem emuladores"), E1 (web)**.
   → **Diferencial direto do Tensorroot.Gov (SPA React + API .NET).** Confirmado como cláusula real e repetida.
7. **Hospedagem em datacenter/nuvem com escalabilidade** — **E1 (itens 1–8), Mata (§5), E2 (SaaS)**.
8. **Sem limite de usuários simultâneos** — **E1 (item 42), E2, Mata**.
9. **Multi-navegador padrão (Chrome/Firefox/Edge/Safari), sem plugins** — **E2, Mata (7.5)**.

### 6.3 Módulos/funcionalidades (nos 3, com nomes quase idênticos)
10. **Contábil PCASP/MCASP + ciclo Lei 4.320 (empenho→liquidação→pagamento)** — **E1, E2, Mata**. *Núcleo da PoC.*
11. **Folha de pagamento + eSocial** — **E1, E2, Mata**.
12. **Compras/Licitações Lei 14.133 + Contratos** — **E1, E2, Mata**.
13. **Tributos (IPTU, ISS, ITBI) + Dívida Ativa** — **E1, E2, Mata**.
14. **Patrimônio + Almoxarifado + Frota** — **E1, E2, Mata**.
15. **Portal da Transparência (LAI) + Portal do Cidadão/autoatendimento** — **E1, E2, Mata**.
16. **Processo digital / protocolo eletrônico com assinatura** — **E1, E2, Mata**.
17. **Cadastro único de credores/fornecedores compartilhado** — **E1 (item 57), Mata (integração de módulos)**.

### 6.4 Compliance / Segurança (nos 3)
18. **LGPD operacional** (termos de uso, inventário/RoPA, consentimento, DPO no portal) — **E2 (a–j), Mata (LGPD), E1**.
19. **RBAC + auditoria/log de transações (I/A/E/C) por usuário** — **E1 (itens 48/49/71), E2, Mata**.
20. **HTTPS/SSL + segurança de borda** — **E1 (11–13), E2 (eBGP/SQLi/DDoS)**.

### 6.5 Integrações (recorrentes, mas frequentemente DISPENSÁVEIS na PoC — E2 §14.6.6)
21. **TCE estadual** (SICOM/MG; "TCE/SC"; **TCE-RS** em Mata) — **E1, E2, Mata**. *No alvo: SIAPC/PAD.*
22. **SICONFI/STN** — **E1 (item 74)**; presumido nas demais.
23. **PNCP (art. 176 Lei 14.133)** — **E1, E2**.
24. **eSocial + EFD-REINF** — **E1 (blocos inteiros)**; folha+eSocial citada em E2/Mata.
25. **PIX/arrecadação registrada, OBN/CNAB** — **E1 (itens 68/70)**.

---

## 7. REQUISITOS RAROS / específicos de UM edital (peso menor, mas podem cair)

- **Firewall NGFW no Gartner Magic Quadrant, SD-WAN, GeoIP, SOC** — **só E1**. É exigência de **infra de
  hospedagem** (atende-se via provedor de nuvem), não de software. Raro como item de software.
- **Integração com ≥5 plataformas de pregão** — **só E1 (item 60)**.
- **Leitor de código de barras na liquidação** — **só E1 (item 66)**.
- **Borderô OBN específico** — **só E1 (item 68)**; demais citam "cobrança registrada/PIX" genérico.
- **eBGP com ≥2 operadoras** — **só E2 (infra do provedor)**.
- **Bloqueio mensal escalonado contábil** — **só E1 (item 59)**, mas é **arquiteturalmente importante**
  (fechamento contábil governa os demais módulos) e **PLAUSÍVEL** de reaparecer em editais maduros.
- **Ponto eletrônico (Port. MTP 671/2021)** — **não localizado como item de PoC** em E1/E2/Mata lidos →
  permanece **INCERTO** como exigência de amostra (está no nosso escopo, mas sem fonte de PoC).
- **Leiaute SIAPC/PAD do TCE-RS detalhado** — **não detalhado** em Mata (cita TCE-RS genérico) →
  permanece **`[a confirmar]`** (pendência nº2 da pesquisa, ainda aberta).

---

## 8. Pendências da pesquisa — status após verificação

| # | Pendência original | Status |
|---|---|---|
| 1 | Edital-alvo concreto no RS com regras de PoC | ✅ **RESOLVIDA** — Mata/RS (Pregão 21/2021): 100% gerais + 95% módulo + 5%/30 d + web nativo. |
| 2 | Leiaute SIAPC/PAD do TCE-RS e versão MSC/SICONFI 2026 | ⏳ **ABERTA** — nenhum edital lido traz o leiaute detalhado. Buscar manual técnico no portal TCE-RS. |
| 3 | Edital-alvo exige **emissão NFS-e ABRASF**? | 🟡 **RISCO CONFIRMADO COMO RECORRENTE** — E1 (escrituração NFS-e) e Mata (NFS-e/escrita fiscal) presumem ativo; conflito real com nosso modelo passivo ADN. Confirmar no edital concreto. |
| 4 | **Ponto eletrônico** como item de PoC | ⏳ **ABERTA/INCERTA** — não localizado como item de amostra nas 3 fontes. |
| 5 | Percentual de corte típico no RS | ✅ **RESOLVIDA** — RS (Mata) usa **95% por módulo + 100% gerais**, alinhado a E1. |
| 6 | Saúde/Educação licitadas em separado? | 🟡 **PARCIAL** — Mata licita Prefeitura **e** Saúde no mesmo objeto (blocos distintos de relevância). E1 separa Saúde/Educação em módulos próprios. Varia por município. |

---

## 9. Veredito final para o produto (sem floreio)

- **A pesquisa é majoritariamente sólida e bem-fundada.** As regras de PoC e os requisitos por item **estão
  no texto dos editais** — não são suposição. Apenas **E3 (PNCP)** não se sustenta e deve ser substituído
  (já o fiz por **Mata/RS**, que é melhor: é do **RS** e parseável).
- **O risco nº1 da pesquisa é o risco nº1 real:** **Contabilidade PCASP/MCASP + ciclo da despesa** é o
  módulo **mais cobrado e universal** (nos 3 editais), e o **bloqueio mensal escalonado** (E1 item 59) impõe
  que o fechamento contábil **governe os demais módulos**. Falhar aqui = reprovação eliminatória.
- **Risco nº2 confirmado e mais sério do que a pesquisa sugeria:** **NFS-e ativa/escrituração** aparece em
  MG **e** RS. Nosso modelo de **ingestão passiva ADN** pode **não satisfazer** um requisito obrigatório de
  emissão/escrituração — **validar no edital concreto antes de prometer aderência**.
- **Diferencial confirmado e citável:** **web nativo sem emulador** é cláusula **literal e repetida** (E2 §1.b,
  Mata §7.9 + "sem emuladores"). Nossa SPA React + API .NET ataca isso diretamente — **vantagem real**, não
  marketing.

---

## Fontes (verificadas campo a campo)

- **E1 — TR Software de Gestão Pública, Carbonita/MG** — https://sistema.carbonita.mg.gov.br/UpFiles/licitacoes/818/tr___software_gestao_publica.pdf *(18.425 linhas, lido integralmente)*
- **E2 — Pregão Locação de Sistema Web, Riqueza/SC** — https://s3cache.dom.sc.gov.br/atos/2024/11/1731530485_lic_965_contratao_de_empresa_para_gesto_de_software__prego_eletronico_servios_1_retificao.pdf *(lido integralmente; CNPJ 95.988.309/0001-48)*
- **NOVO — Pregão Eletrônico nº 21/2021, Prefeitura de Mata/RS** — https://mata.rs.gov.br/wp-content/uploads/2025/08/PREGAO-ELETRONICO-N-21-2021-SOFTWARE.pdf *(9.033 linhas, lido integralmente — fonte do RS)*
- **TCU — Amostra e Prova de Conceito** — https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/
- **E3 (PNCP) — NÃO VERIFICÁVEL** — https://pncp.gov.br/pncp-api/v1/orgaos/08539439000107/compras/2025/10/arquivos/1 *(retorna binário agregado; descartado como fonte)*
- Outros editais com mesmo padrão (busca, não lidos campo a campo): TR Cerrito/SC, Nova Trento/SC, Cerro Negro/SC (mesmo TR-base de E2).
