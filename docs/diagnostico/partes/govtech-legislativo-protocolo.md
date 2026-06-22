# GovTech — Legislativo (Câmara) e Protocolo / Processo Eletrônico

> Diagnóstico de obrigações, formatos, integrações e prazos para os Bounded Contexts **Legislativo** e **Protocolo** do Tensorroot.Gov.
> Foco: Câmaras Municipais (piloto Maximiliano de Almeida/RS) + processo administrativo eletrônico para Prefeituras e Câmaras.
> Data: 2026-06. Itens não confirmados em fonte oficial estão marcados com **[a confirmar em fonte oficial]**.

---

## PARTE A — LEGISLATIVO (Câmara / Processo Legislativo)

### A.1 Obrigações

| # | Obrigação | Base legal / referência |
|---|-----------|------------------------|
| L1 | Gerir o ciclo completo de **proposições** (projeto de lei, projeto de lei complementar, PEC/emenda à LOM, projeto de decreto legislativo, projeto de resolução, requerimento, indicação, moção, emendas, substitutivos, vetos) com tramitação rastreável. | Regimento Interno de cada Câmara + Lei Orgânica Municipal (LOM) |
| L2 | Organizar **sessões plenárias** (ordinárias, extraordinárias, solenes), com pauta/ordem do dia, ata, lista de presença e quórum. | Regimento Interno |
| L3 | Registrar **votações** (nominal, simbólica, secreta nos casos permitidos), apuração de quórum de instalação e de aprovação (maioria simples, absoluta, qualificada/2-3), e resultado por parlamentar. | Regimento Interno + LOM + CF/88 art. 47 (analogia) |
| L4 | Manter **base de leis consolidada** e vigente (leis ordinárias, complementares, decretos legislativos, resoluções), com compilação/atualização de textos articulados. | Lei Complementar 95/1998 (técnica legislativa) |
| L5 | **Transparência legislativa ativa** — publicação de proposições, pautas, atas, resultados de votação, presença, despesas do Legislativo e remuneração dos vereadores. | Lei 12.527/2011 (LAI); LC 131/2009; CF/88 art. 37 |
| L6 | **e-Democracia / participação cidadã** — canais para consulta pública, acompanhamento de proposições e manifestação (recomendável; obrigatório p/ tornar transparência efetiva). | LAI art. 8º; boas práticas Interlegis |
| L7 | Suporte a **sessões remotas/híbridas** (videoconferência + painel eletrônico de votação) quando o Regimento autorizar. | Regimentos pós-2020; modelo SAPL-R |
| L8 | Prestação de contas do Legislativo ao **TCE** (folha dos vereadores e servidores, despesas, repasse duodécimo da Câmara, limites art. 29-A CF). | CF/88 art. 29-A; LRF; layouts do TCE-RS (SIAPC/PAD) — **[a confirmar layout específico Legislativo no TCE-RS]** |

### A.2 Formatos / Layouts / Padrões

- **LexML** — padrão nacional de identificação e interoperabilidade de informação jurídica e legislativa nos 3 poderes e 3 esferas (URN persistente `urn:lex:br;...`, metadados em XML). Recomendado pelo e-PING. Cobre legislação municipal (ex.: São Carlos/SP). Deve ser o padrão de identificação de normas e proposições do módulo Legislativo. Fonte: projeto.lexml.gov.br.
- **SAPL (Sistema de Apoio ao Processo Legislativo)** — referência funcional do Interlegis/Senado, gratuito, open source (GitHub interlegis/sapl). Funcionalidades de referência a espelhar: proposições e tramitação, sessões plenárias, base de leis, mesa diretora/comissões/votações, painel eletrônico, compilação de textos articulados (v3.1), e SAPL-R (sessões remotas com videoconferência + painel). Útil tanto como benchmark quanto como alvo de **migração/importação de dados**.
- **Texto articulado** — estrutura normativa (artigo, parágrafo, inciso, alínea) conforme LC 95/1998; armazenar de forma estruturada para permitir compilação/consolidação automática.
- **Atas e documentos** — gerar/preservar em **PDF/A** (preservação de longo prazo, ver Parte B).
- **Dados abertos** — expor proposições, votações e agenda em formato aberto (CSV/JSON) e, idealmente, com identificadores LexML, para portais de transparência e e-Democracia. Formato aberto exigido pela LAI (art. 8º §3º).

### A.3 Integrações

- **LexML** (federação de informação legislativa) — publicação/indexação de normas municipais via metadados XML/URN.
- **SAPL / Interlegis** — caminho de **migração** de Câmaras que já usam SAPL; mapear entidades (matéria → proposição, norma jurídica → lei).
- **TCE-RS** — envio de prestação de contas do Legislativo (folha dos vereadores, duodécimo, despesas) pelos layouts do TCE (SIAPC/PAD e correlatos). **[a confirmar leiautes exatos para o Poder Legislativo no portal do TCE-RS]**.
- **Portal da Transparência** (BC Transparencia) — feed de pauta, atas, votação, presença e despesas.
- **gov.br / assinatura eletrônica** — assinatura de proposições, pareceres e autógrafos de lei (ver Parte B, Lei 14.063).
- **Diário Oficial do Município** — publicação de leis/atos sancionados/promulgados.

### A.4 Prazos (operacionais — definidos por Regimento/LOM de cada Câmara)

- Prazos de tramitação (apresentação, pareceres de comissão, prazos de emenda, regime de urgência) são **regimentais** — devem ser **parametrizáveis por tenant**. **[a confirmar prazos específicos no Regimento Interno da Câmara de Maximiliano de Almeida/RS]**.
- Sanção/veto do Executivo: prazo definido pela LOM (analogia CF/88 art. 66: 15 dias úteis p/ sanção/veto; 48h p/ promulgação) — **parametrizável**.
- Publicação de atos no prazo da LAI e do Diário Oficial.

---

## PARTE B — PROTOCOLO / PROCESSO ADMINISTRATIVO ELETRÔNICO

### B.1 Obrigações

| # | Obrigação | Base legal / referência |
|---|-----------|------------------------|
| P1 | **Protocolo eletrônico** de documentos e processos com número/identificador único, autuação, juntada, tramitação entre setores e encerramento, com trilha de auditoria. | Lei 9.784/1999 (processo administrativo, subsidiária); normas locais |
| P2 | **Assinatura eletrônica** nos documentos públicos, observando os 3 níveis e exigências por tipo de ato. | **Lei 14.063/2020** + Decreto 10.543/2020 |
| P3 | **GED / SIGAD** — gestão arquivística do documento digital do nascimento à destinação final, com captura, classificação, tramitação, avaliação e preservação. | e-ARQ Brasil (CONARQ); Lei 8.159/1991 |
| P4 | **Classificação e temporalidade** — aplicar Plano de Classificação (atividades-meio e atividades-fim) e **Tabela de Temporalidade e Destinação de Documentos (TTDD)** com prazos de guarda e destinação (eliminação/guarda permanente). | CONARQ; Lei 8.159/1991; Portaria AN/MGI nº 174/2024 (instrumentos federais, referência) |
| P5 | **Preservação digital de longo prazo** — repositório digital confiável e formatos de preservação. | CONARQ Resolução 39/2014 (alt. pela Res. 43/2015) — RDC-Arq |
| P6 | **Interoperabilidade** entre sistemas/órgãos. | e-PING; e-ARQ Brasil (requisitos não-funcionais de interoperabilidade) |
| P7 | **Acesso à informação / e-SIC** — receber e responder pedidos de acesso e disponibilizar transparência ativa. | Lei 12.527/2011 (LAI); Decreto 7.724/2012 |
| P8 | **Proteção de dados pessoais** no tratamento de documentos. | LGPD (Lei 13.709/2018) |

### B.2 Lei 14.063/2020 — níveis de assinatura eletrônica (núcleo do Protocolo)

| Nível | Tecnologia | Presunção legal | Uso típico |
|-------|-----------|-----------------|-----------|
| **Simples** | identifica o signatário + anexa/associa dados (ex.: login gov.br nível bronze). | nenhuma presunção legal forte. | atos de **baixo risco** sem sigilo: requerimentos, marcação de consulta, perícias. |
| **Avançada** | certificado **não** ICP-Brasil ou outro meio que comprove autoria/integridade, aceito pelas partes (ex.: gov.br nível prata/ouro). | comprova autoria/integridade quando aceita. | interações de **médio risco** com entes públicos. |
| **Qualificada** | certificado digital **ICP-Brasil** (e-CPF / e-CNPJ). | **presunção legal de autenticidade**; equivale à assinatura física. | atos de **alto risco**, e exigida em hipóteses específicas (ex.: atos que transfiram patrimônio, e por entes públicos quando a lei exigir). É **admitida sempre**, em qualquer interação, independentemente de cadastro prévio. |

> Implicação para o produto: o módulo Protocolo deve suportar os 3 níveis, integrar **gov.br** (simples/avançada) e **ICP-Brasil** (qualificada), e permitir **parametrizar o nível mínimo exigido por tipo de documento/ato**. Fonte: Lei 14.063/2020 (Planalto) + Decreto 10.543/2020.

### B.3 Formatos / Layouts / Padrões

- **e-ARQ Brasil** (CONARQ — versão atualizada 2022, PDF oficial gov.br/conarq) — **modelo de requisitos para SIGAD**. Distinção crítica para a arquitetura:
  - **SIGAD** = controla o ciclo de vida arquivístico (produção → destinação) com princípios arquivísticos (autenticidade, cadeia de custódia). É o alvo de conformidade.
  - **GED** = digitalização, workflow, indexação, repositório — subconjunto tecnológico, **não** garante por si só gestão arquivística.
  - Requisitos não-funcionais cobrem: armazenamento, funções administrativas, conformidade legal, usabilidade, **interoperabilidade** e disponibilidade.
- **Formatos de preservação**: **PDF/A** para documentos textuais; preferir formatos abertos/padronizados para preservação de longo prazo.
- **RDC-Arq** (Repositório Digital Confiável) — CONARQ Res. 39/2014 (alt. Res. 43/2015) para transferência/recolhimento e preservação dos documentos digitais arquivísticos.
- **Cadeia de custódia** SIGAD → RDC-Arq — manter metadados de autenticidade e trilha desde a captura.
- **Classificação/Temporalidade**: adotar Código de Classificação + TTDD; referência federal atualizada pela **Portaria AN/MGI nº 174/2024**. Municípios podem ter instrumentos próprios aprovados por seu arquivo/comissão de avaliação. **[a confirmar instrumento adotado no município piloto]**.

### B.4 Integrações

- **gov.br** (login + assinatura simples/avançada) — autenticação e assinatura de cidadãos e servidores.
- **ICP-Brasil** — assinatura qualificada (e-CPF/e-CNPJ; token/certificado A1/A3).
- **e-SIC / LAI** — recepção e resposta de pedidos de acesso, integrado ao protocolo.
- **RDC-Arq / Arquivo** — destinação para repositório de preservação ao fim da temporalidade.
- **Diário Oficial / Transparência** — publicação de atos.
- **Interoperabilidade e-PING** — APIs/padrões para troca entre Prefeitura ↔ Câmara ↔ órgãos externos (alinhado ao requisito de interoperabilidade do e-ARQ).

### B.5 Prazos

- **Guarda / temporalidade**: definidos por TTDD por tipo documental (correntes, intermediários, permanentes) — **parametrizável por tabela**; eliminação somente após cumprimento de prazo e respeitada a guarda permanente. (CONARQ / Lei 8.159/1991).
- **LAI / e-SIC**: resposta em até **20 dias**, prorrogáveis por **+10 dias** mediante justificativa (Lei 12.527/2011 art. 11).
- **Transparência financeira em "tempo real"**: disponibilização até o **1º dia útil subsequente** ao registro contábil (Decreto 7.185/2010).
- **Retenção de dados de transparência**: registros mantidos no portal por **no mínimo 5 anos** a contar da aprovação das contas; conteúdo retirado deve ser arquivado digitalmente e mantido de forma permanente para atender a LAI.
- **Penalidade** por descumprimento da transparência: impedimento de receber **transferências voluntárias** (LC 131/2009).

---

## FONTES (URLs)

### Legislativo
- LexML — projeto: https://projeto.lexml.gov.br/documentacao/destaques-lexml
- LexML — Câmara dos Deputados (Rede de Informação Legislativa): https://www2.camara.leg.br/atividade-legislativa/legislacao/destaques/lexml-legislacao-integrada
- LexML — apresentação/integração 3 poderes: https://github.com/dadosgovbr/catalogos-dados-brasil/issues/1
- SAPL — Interlegis/Senado: https://www12.senado.leg.br/interlegis/produtos/sapl
- SAPL — Interlegis (produtos/serviços): https://www.interlegis.leg.br/produtos-servicos/sapl
- SAPL — código-fonte: https://github.com/interlegis/sapl
- SAPL-R (sessões remotas): https://www12.senado.leg.br/interlegis/produtos/sapl-r

### Protocolo / Processo eletrônico
- Lei 14.063/2020 (Planalto): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm
- Assinatura eletrônica avançada — Lei 14.063 + Decreto 10.543 (RedGEALC): https://www.redgealc.org/site/assets/files/15034/assinatura_eletronica_avancada_-_21-03-2022.pdf
- e-ARQ Brasil — Modelo de Requisitos (CONARQ, v2 2022): https://www.gov.br/conarq/pt-br/centrais-de-conteudo/publicacoes/EARQV203MAI2022.pdf
- Arquivo Nacional — Código de Classificação e TTDD (Portaria AN/MGI nº 174/2024): https://www.gov.br/arquivonacional/pt-br/canais_atendimento/imprensa/copy_of_noticias/arquivo-nacional-atualiza-codigo-de-classificacao-e-tabela-de-temporalidade-e-destinacao-de-documentos-de-arquivo
- Cadeia de custódia: do SIGAD ao RDC-Arq (UFES): https://arquivologia.ufes.br/sites/arquivologia.ufes.br/files/field/anexo/cadeia_de_custodia_dos_documentos_arquivisticos_digitais-_do_sigad_ao_rdc-arq.pdf

### Transparência / LAI
- LC 131/2009 (Planalto): https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp131.htm
- LAI — Lei 12.527/2011: https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12527.htm
- CNM — Lei da Transparência (perguntas/respostas): http://www.leidatransparencia.cnm.org.br/pergunta-resposta.php
