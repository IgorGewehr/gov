# M9 — Pesquisa de Compliance Documental

> Tensorroot.Gov — ERP GovTech municipal. Preparação do marco M9 (Suprimentos avançados + compliance + robustez/QA).
> Escopo desta pesquisa: Protocolo eletrônico (carimbo do tempo ICP-Brasil, assinatura Lei 14.063/2020), GED + temporalidade documental (CONARQ / e-ARQ Brasil) e Legislativo (LexML, art. 29-A CF).
> Disciplina §16: cada afirmação tem **FONTE** ou marcação **[a confirmar]**.
> Data da pesquisa: 2026-06-22.

---

## 1. Protocolo eletrônico — Carimbo do tempo (ACT / ICP-Brasil)

### Achados
- **ACT = Autoridade de Carimbo do Tempo**: entidade credenciada pela AC-Raiz da ICP-Brasil, responsável por emitir carimbos do tempo (Time Stamps). O carimbo é documento eletrônico que comprova que uma informação digital existia em determinada data/hora no passado. **FONTE:** ITI — Autoridades de Carimbo do Tempo (gov.br/iti).
- **Normas técnicas base**: o sistema ICP-Brasil de carimbo do tempo é fundamentado nas normas ICP-Brasil + **RFC 3161** (IETF — *Time-Stamp Protocol / TSP*, ago/2001) + **RFC 3628** (IETF — *Policy Requirements for Time-Stamping Authorities*, nov/2003) + ETSI TS 101861. **FONTE:** DOC-ICP-12 (gov.br/iti) e Resolução ITI 172/2020.
- **Documento normativo principal**: **DOC-ICP-12** ("Requisitos Mínimos para Declarações de Práticas de Carimbo do Tempo da ICP-Brasil"), versão 2.0 aprovada pela **Resolução ITI nº 172, de 17/08/2020**. Há também DOC-ICP-11 ("Visão Geral do Sistema de Carimbos do Tempo"). **FONTE:** repositorio.iti.gov.br/resolucoes/Resolucao172_DOC-ICP-12.htm.
- **Protocolo de requisição/resposta**: usa **TSQ** (Time Stamp Request, contém o *hash* a carimbar) e **TSR/TST** (Time Stamp Response/Token). Falhas devem responder conforme RFC 3161 item 2.4.2 (campo `PKIFailureInfo`, status ≠ 0/1, sem emitir token). **FONTE:** RFC 3161 / DOC-ICP-12.
- **Requisito de hardware**: a ACT deve possuir **HSM** contendo o relógio a partir do qual os carimbos são emitidos; o HSM também executa funções criptográficas (geração de chaves e assinatura). **FONTE:** ITI / DOC-ICP-12.
- **Obrigatoriedade**: o uso do carimbo do tempo **é opcional** na ICP-Brasil — documentos assinados digitalmente com certificado ICP-Brasil são válidos com ou sem carimbo. Para protocolo eletrônico, o carimbo é a forma robusta de provar **tempestividade** (data/hora de protocolo não-repudiável). **FONTE:** ITI — Autoridades de Carimbo do Tempo.
- **ACTs credenciadas** (não-exaustivo, com data de credenciamento): CAIXA (2013), SERPRO (2013), CERTISIGN (2014), VALID (2014), BRY (2014), QUICKSOFT (2014), SAFEWEB (2014), SOLUTI (2019), PRODESP (2021). **FONTE:** ITI — lista de ACTs (gov.br/iti).

### Implicação para Tensorroot.Gov
- O módulo de Protocolo deve registrar o **TST (token de carimbo do tempo)** retornado por uma ACT credenciada como prova de tempestividade do protocolo, vinculado ao *hash* do documento protocolado.
- Padrão de integração: cliente TSP (RFC 3161) → ACT (HTTP/TSP). **[a confirmar]** qual ACT contratar e custo por carimbo (modelo de cobrança por volume).

---

## 2. Protocolo eletrônico — Assinatura eletrônica (Lei 14.063/2020)

### Achados — três níveis de assinatura (Lei nº 14.063, de 23/09/2020)
- **Assinatura eletrônica simples**: identifica o signatário e anexa/associa dados a outros dados em formato eletrônico do signatário. Admitida em interações de **menor impacto** que não envolvam informações protegidas por sigilo. **FONTE:** Lei 14.063/2020 (planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm).
- **Assinatura eletrônica avançada**: usa certificados **não** emitidos pela ICP-Brasil ou outro meio de comprovação de autoria/integridade, desde que admitida pelas partes como válida ou aceita pela pessoa a quem o documento for apresentado. **FONTE:** Lei 14.063/2020 (Planalto).
- **Assinatura eletrônica qualificada**: utiliza **certificado digital ICP-Brasil**, nos termos da **MP 2.200-2, de 24/08/2001**. **FONTE:** Lei 14.063/2020 (Planalto).
- **Regulamentação complementar**: **Decreto nº 10.543, de 13/11/2020** dispõe sobre os níveis mínimos exigidos para assinatura eletrônica em interações com entes públicos. **FONTE:** D10543 (planalto.gov.br/ccivil_03/_ato2019-2022/2020/decreto/d10543.htm).

### Achados — admissão e obrigatoriedade (art. 5º)
- Cada ente federativo, no âmbito de suas competências, **define o nível mínimo** exigido de assinatura por ato do titular do Poder/órgão autônomo. **FONTE:** Lei 14.063/2020, art. 5º (Planalto).
- O ente público deve **informar em seu site** os requisitos e mecanismos internos de reconhecimento da assinatura avançada. **FONTE:** Lei 14.063/2020, art. 5º.
- A **qualificada é admitida em qualquer interação** com ente público, independentemente de cadastro prévio. **FONTE:** Lei 14.063/2020.
- **Uso obrigatório da qualificada**: para atos assinados por **chefes de Poder, Ministros de Estado ou titulares de Poder/órgão constitucionalmente autônomo** do ente federativo. **FONTE:** Lei 14.063/2020.

> Observação: a numeração exata dos incisos do art. 4º (definições) e do art. 5º (admissão) deve ser conferida diretamente no texto consolidado do Planalto antes de citar em UI/relatório. **[a confirmar]** — fetch do Planalto falhou repetidamente nesta sessão (socket closed); confirmar incisos no DOM oficial.

### Implicação para Tensorroot.Gov
- O Protocolo deve suportar os **3 níveis** e permitir que o município **configure o nível mínimo por tipo de ato/documento** (parametrização administrativa).
- Atos de chefe de Poder (Prefeito, Presidente da Câmara) → forçar **qualificada (ICP-Brasil A1/A3)**. Reaproveita o Key Vault/A1 já implementado em M2.
- Avançada: aceitar certificados não-ICP desde que registrada a política de aceitação. **[a confirmar]** quais provedores de assinatura avançada o município reconhecerá.

---

## 3. GED + Temporalidade documental (CONARQ / e-ARQ Brasil)

### Achados — CONARQ e instrumentos
- **CONARQ** (Conselho Nacional de Arquivos): órgão colegiado vinculado ao Arquivo Nacional, criado pela **Lei nº 8.159/1991**; define a política nacional de arquivos e a orientação normativa de gestão de documentos. **FONTE:** gov.br/conarq.
- **Tabela de Temporalidade e Destinação (TTD)**: instrumento arquivístico resultante da avaliação, que define **prazos de guarda** (fase corrente / intermediária) e a **destinação final** (eliminação ou guarda permanente) dos documentos. **FONTE:** gov.br/arquivonacional — Guia de Gestão de Documentos.
- **Código de Classificação + TTD das atividades-meio**: aprovados pela **Portaria nº 47** do Arquivo Nacional (instrumento para o Poder Executivo Federal, 2020); atividades-fim têm instrumentos próprios aprovados pelo Arquivo Nacional. **FONTE:** gov.br/conarq — Portaria_47_CCD_TTD.
- **Programa de gestão é único** (Resolução CONARQ nº 20/2004): serve para documentos digitais e não-digitais — a gestão deve integrar todos os documentos, independentemente de formato/suporte. **FONTE:** Resolução CONARQ 20/2004 (gov.br/conarq).

### Achados — e-ARQ Brasil (SIGAD)
- **e-ARQ Brasil = Modelo de Requisitos para Sistemas Informatizados de Gestão Arquivística de Documentos (SIGAD)**. **Versão 2** aprovada pela **Resolução CONARQ nº 50, de 06/05/2022** (em vigor desde 16/05/2022); revoga a versão anterior. **FONTE:** gov.br/conarq — Resolução 50/2022; PDF EARQV203MAI2022.
- **Núcleo de requisitos de um SIGAD** inclui grupos: **Organização/Classificação** (plano/código de classificação), **Captura**, **Avaliação** (temporalidade), **Segurança**, **Preservação**, além de metadados. **FONTE:** e-ARQ Brasil v2 (gov.br/conarq).
- **Presunção de autenticidade de documentos digitais**: diretrizes na **Resolução CONARQ nº 37, de 19/04/2012**. O sistema deve manter autenticidade na captura, armazenamento e tramitação nas fases corrente e intermediária. **FONTE:** Resolução CONARQ 37/2012.
- Os requisitos garantem **confiabilidade, autenticidade e acesso** aos documentos pelo tempo necessário; metadados + requisitos são usados para aferir aderência do sistema ao e-ARQ Brasil. **FONTE:** e-ARQ Brasil v2.

### Implicação para Tensorroot.Gov
- O GED do módulo de Protocolo/Patrimônio deve implementar: **plano de classificação** configurável, **captura com metadados** arquivísticos, **motor de temporalidade** (cálculo de prazos de guarda + destinação) e **trilha de preservação/autenticidade** (hash, assinatura, carimbo do tempo — conexão direta com §1 e §2).
- Mapear os **metadados obrigatórios** do e-ARQ Brasil v2 no schema de documentos. **[a confirmar]** — lista completa de metadados da v2 (consultar PDF EARQV203MAI2022, ~225 itens).
- A TTD do município (atividades-meio) pode partir da **Portaria 47 / instrumentos do Arquivo Nacional** como base; atividades-fim municipais → **[a confirmar]** se há instrumento aprovado pelo arquivo público estadual (RS) ou se o município define o seu.

---

## 4. Legislativo — LexML (padrão de normas)

### Achados
- **LexML Brasil**: padrão aberto para estruturação/identificação de normas jurídicas e legislativas, integrando as três esferas (federal, estadual, **municipal**) e os três Poderes. Lançado oficialmente em 30/06/2009; recomendação **e-PING**. **FONTE:** projeto.lexml.gov.br/institucional/historia.
- **URN LexML**: cada documento legislativo/jurídico tem um **identificador unívoco e persistente (URN)** — referência estável que não gera link quebrado e não é ambígua. **FONTE:** projeto.lexml.gov.br.
- **Esquema XML**: as normas são modeladas em **XML Schema** (LexML XML Schema, Parte 3). O padrão estrutura artigos, parágrafos, incisos, etc. **FONTE:** projeto.lexml.gov.br/documentacao/Parte-3-XML-Schema.pdf; "Esquema XML para Normas Jurídicas" (lexml_brasil.pdf).
- **API**: o LexML oferece **API de pesquisa via URL** com resultado em **XML**; há acervo em dados abertos do Senado. **FONTE:** www12.senado.leg.br/dados-abertos — Acervo do portal LexML.

### Implicação para Tensorroot.Gov
- O módulo Legislativo deve **gerar/armazenar URN LexML** para cada norma municipal (leis, decretos, resoluções) e exportar em **XML LexML** para interoperabilidade.
- Estrutura editorial das normas deve seguir a hierarquia LexML (artigo > parágrafo > inciso > alínea) para parsing e referência cruzada.
- **[a confirmar]** — versão atual do LexML XML Schema (doc encontrado é v1.0 RC1, de 2009; verificar revisões posteriores) e formato canônico da URN para esfera municipal/RS.

---

## 5. Legislativo — Limite de despesa da Câmara (art. 29-A CF)

### Achados
- **Art. 29-A CF/88** (incluído pela **EC 25/2000**, com redação por **EC 58/2009** e alterações pela **EC 109/2021**): o **total da despesa do Poder Legislativo municipal** (incluídos subsídios de vereadores e **outras despesas com inativos e pensionistas** — texto da EC 109/2021), **excluídos os gastos com inativos** na redação original, **não pode ultrapassar** percentuais sobre o **somatório da receita tributária e das transferências** previstas no art. 153, §5º e arts. 158 e 159 da CF, **efetivamente realizado no exercício anterior**. **FONTE:** Constituição Federal art. 29-A (planalto.gov.br) + EC 58/2009 + EC 109/2021.
- **Percentuais por faixa populacional** (EC 58/2009):
  - **7%** — municípios com **até 100.000** habitantes;
  - **6%** — de **100.001 a 300.000** habitantes;
  - **5%** — de **300.001 a 500.000** habitantes;
  - **4,5%** — de **500.001 a 3.000.000** habitantes;
  - **4%** — de **3.000.001 a 8.000.000** habitantes;
  - **3,5%** — **acima de 8.000.001** habitantes.
  - **FONTE:** EC 58/2009 (planalto.gov.br/ccivil_03/constituicao/emendas/emc/emc58.htm); CF art. 29-A, incisos I a VI.
- **§1º — limite de folha**: a Câmara Municipal **não gastará mais de 70%** de sua receita com **folha de pagamento**, incluído o gasto com subsídio dos vereadores. **FONTE:** CF art. 29-A, §1º.
- **§2º e §3º**: constituem **crime de responsabilidade** do **Prefeito** (efetuar repasse acima dos limites / a menor / fora dos prazos — §2º) e do **Presidente da Câmara** (gastar mais do que o repasse, desrespeitar o §1º, etc. — §3º). **FONTE:** CF art. 29-A, §§2º e 3º.

> **[a confirmar]** — a numeração de incisos I a VI e o texto literal dos §§ devem ser conferidos no texto consolidado do Planalto antes de hard-coding na UI (fetch do constituicaocompilado.htm falhou nesta sessão; valores acima vêm de EC 58/2009 + buscas convergentes).

### Implicação para Tensorroot.Gov
- O BI/Portal do Gestor (M8) + módulo Legislativo devem calcular automaticamente:
  - **base de cálculo** = receita tributária + transferências (art. 153 §5º, 158, 159) realizada no exercício anterior;
  - **percentual-teto** conforme faixa populacional do município (parametrizável pelo IBGE);
  - **% da folha** vs. teto de 70% (§1º);
  - **alertas de crime de responsabilidade** quando repasse/gasto exceder limites (§§2º/3º).
- Integra com a base contábil PCASP (M2) e prestação de contas TCE-RS (M4).

---

## 6. Pendências consolidadas ([a confirmar])

1. **Lei 14.063/2020** — conferir numeração exata dos incisos do art. 4º e art. 5º no texto consolidado do Planalto (fetch falhou; usar fonte oficial antes de citar literalmente).
2. **Art. 29-A CF** — conferir texto literal dos incisos I-VI e §§1º-3º no `constituicaocompilado.htm` (Planalto).
3. **e-ARQ Brasil v2** — extrair a lista completa de **metadados obrigatórios** do PDF EARQV203MAI2022 (~225 requisitos) para o schema de documentos.
4. **TTD municipal** — verificar se há instrumento de temporalidade de atividades-fim aprovado pelo Arquivo Público do RS aplicável ao município, ou se o município define o seu.
5. **LexML XML Schema** — confirmar versão vigente (encontrado v1.0 RC1/2009) e formato canônico da URN para normas municipais (esfera `br;rs;<municipio>`).
6. **ACT** — definir qual Autoridade de Carimbo do Tempo credenciada contratar e o modelo de cobrança por carimbo.
7. **Decreto 10.543/2020** — detalhar os níveis mínimos por tipo de interação para parametrização do Protocolo.

---

## Fontes (URLs)

- ITI — Autoridades de Carimbo do Tempo: https://www.gov.br/iti/pt-br/assuntos/icp-brasil/autoridades-de-carimbo-do-tempo
- ITI — DOC-ICP-12 v1.3: https://www.gov.br/iti/pt-br/assuntos/legislacao/documentos-principais/doc-icp-12versao-1-3.pdf
- ITI — Resolução 172/2020 (DOC-ICP-12 v2.0): https://repositorio.iti.gov.br/resolucoes/Resolucao172_DOC-ICP-12.htm
- ITI — DOC-ICP-11 (Visão Geral Carimbo do Tempo): https://www.gov.br/iti/pt-br/central-de-conteudo/doc-icp-11-v-1-0-pdf
- Lei 14.063/2020 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm
- Decreto 10.543/2020 (Planalto): https://planalto.gov.br/ccivil_03/_ato2019-2022/2020/decreto/d10543.htm
- CONARQ — Resolução 50/2022 (e-ARQ Brasil v2): https://www.gov.br/conarq/pt-br/legislacao-arquivistica/resolucoes-do-conarq/resolucao-no-50-de-06-de-maio-de-2022
- e-ARQ Brasil v2 (PDF): https://www.gov.br/conarq/pt-br/centrais-de-conteudo/publicacoes/EARQV203MAI2022.pdf
- Arquivo Nacional — Guia de Gestão de Documentos: https://www.gov.br/arquivonacional/pt-br/servicos/publicacoes/Guiadegestaodedocumentos.pdf
- Arquivo Nacional — Portaria 47 (CCD/TTD): https://www.gov.br/conarq/pt-br/centrais-de-conteudo/publicacoes/Portaria_47_CCD_TTD_poder_executivo_federal_2020_instrumento.pdf
- LexML — História: https://projeto.lexml.gov.br/institucional/historia
- LexML — Parte 3 XML Schema: https://projeto.lexml.gov.br/documentacao/Parte-3-XML-Schema.pdf
- LexML — Esquema XML para Normas Jurídicas: https://projeto.lexml.gov.br/Members/joaolima/lexml_brasil.pdf
- Senado — Acervo do portal LexML (dados abertos): https://www12.senado.leg.br/dados-abertos/legislativo/legislacao/acervo-do-portal-lexml
- CF art. 29-A / EC 58/2009 (Planalto): http://planalto.gov.br/ccivil_03/constituicao/emendas/emc/emc58.htm
- CF art. 29-A / EC 25/2000 (Planalto): https://www.planalto.gov.br/ccivil_03/constituicao/emendas/emc/emc25.htm
- CF compilada (Planalto): https://www.planalto.gov.br/ccivil_03/constituicao/constituicaocompilado.htm
