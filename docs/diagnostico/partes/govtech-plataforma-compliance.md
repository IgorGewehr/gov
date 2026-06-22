# GovTech BR — Requisitos de Plataforma e Compliance (Tensorroot.Gov)

> Diagnóstico de requisitos transversais de plataforma/compliance para ERP GovTech multi-tenant (Prefeituras + Câmaras). Piloto: Maximiliano de Almeida/RS.
> Foco do dono: **Contabilidade** e **Prestação de Contas ao TCE** (TCE-RS / SICONFI).
> Última verificação web: jun/2026. Itens não confirmados em fonte oficial estão marcados **[a confirmar em fonte oficial]**.

---

## 1. LGPD — Proteção de Dados Pessoais (dados sensíveis: Saúde / Assistência Social)

### Obrigações
- **Base legal própria para o Poder Público**: tratamento por entes públicos deve apoiar-se em hipóteses dos arts. 7º e 11 e nos arts. 23 a 30 (tratamento pelo poder público) da Lei 13.709/2018. Para **execução de políticas públicas** o ente é dispensado do consentimento, mas deve dar publicidade às hipóteses de tratamento (art. 23, I).
- **Dados sensíveis** (art. 5º, II): saúde, vida sexual, dado genético/biométrico, origem racial/étnica, convicção religiosa, opinião política, filiação sindical. Tratamento de saúde/assistência social só nas hipóteses do art. 11 (cumprimento de obrigação legal/regulatória, política pública, tutela da saúde por profissional/serviço de saúde, proteção da vida).
- **Vedação** (art. 11, §4º): comunicação/uso compartilhado de dados de saúde com objetivo de obter vantagem econômica é vedado, salvo exceções (portabilidade a pedido do titular, prestação de serviços de saúde/assistência farmacêutica em benefício do titular).
- **Encarregado (DPO)**: indicação obrigatória, com identidade e contato divulgados publicamente (art. 41). No setor público a ANPD pode estabelecer hipóteses de dispensa para órgãos de pequeno porte **[a confirmar regulamento ANPD vigente]**.
- **RIPD (Relatório de Impacto à Proteção de Dados)**: a ANPD pode determinar sua elaboração (art. 38) e, para o Poder Público, é recomendado/exigível especialmente quando há tratamento de grande volume de dados sensíveis (saúde, assistência social). Deve identificar controlador, operador e encarregado, e a hipótese legal motivadora.
- **Direitos do titular** (art. 18): confirmação, acesso, correção, anonimização/bloqueio/eliminação, portabilidade, informação sobre compartilhamento — exigem funcionalidades no produto.
- **Segurança e incidentes**: medidas técnicas/administrativas (art. 46) e **comunicação de incidente** à ANPD e ao titular em prazo razoável (art. 48). A ANPD definiu prazo de comunicação **[a confirmar — Resolução CD/ANPD nº 15/2024 / regulamento de comunicação de incidentes]**.

### Formatos / Layouts
- Registro das operações de tratamento (ROPA / art. 37) por contexto de negócio.
- Política de Privacidade pública por tenant; banner/registro de consentimento quando aplicável.
- Modelo de RIPD seguindo template ANPD (controlador → operador → encarregado → finalidade → riscos → medidas).

### Integrações
- Catálogo de bases legais por entidade/módulo (Saúde, Assistência Social, RH, Tributos).
- Trilha de auditoria imutável de acessos a dados sensíveis (quem/quando/por quê).

### Prazos
- LGPD em vigor desde 2020/2021 (sanções administrativas desde 01/08/2021).
- Comunicação de incidente: prazo definido em regulamento ANPD **[a confirmar]**.

---

## 2. Acessibilidade Digital — gov.br DS + eMAG + WCAG

### Obrigações
- **eMAG (Modelo de Acessibilidade em Governo Eletrônico)** é de observância obrigatória em sítios e portais do governo brasileiro — institucionalizado pela **Portaria SLTI/MP nº 3, de 07/05/2007**, no âmbito do SISP.
- Base legal de acessibilidade: **Decreto nº 5.296/2004** (acessibilidade) e **Lei Brasileira de Inclusão — Lei 13.146/2015** (LBI, art. 63: acessibilidade de sites obrigatória).
- eMAG é uma especialização do **WCAG** (W3C) adaptada ao governo BR — não exclui boas práticas do WCAG.
- **gov.br Design System (DS)**: padrão visual/componentes recomendado para serviços digitais gov.br **[obrigatoriedade formal a confirmar — vincula serviços integrados ao gov.br; municípios seguem por adesão]**.

### Formatos / Layouts
- HTML semântico, contraste, navegação por teclado, leitores de tela, descrição de imagens (alt), formulários com labels — alvo mínimo **WCAG 2.1 nível AA** **[a confirmar nível mandatório para municípios]**.
- Componentes/tokens do gov.br DS (cores, tipografia Rawline, grid, ícones).

### Integrações
- Identidade visual gov.br quando integrado a login único gov.br.

### Prazos
- Obrigação contínua (deve estar presente desde a entrega).

---

## 3. Assinatura Eletrônica — Lei 14.063/2020 + ICP-Brasil / Certificado A1

### Obrigações
- **Lei 14.063/2020** classifica em 3 níveis:
  - **Simples** — identifica o signatário e anexa/associa dados; usos de baixo risco.
  - **Avançada** — certificado não-ICP ou outro meio que comprove autoria/integridade, aceito pelas partes.
  - **Qualificada** — certificado digital **ICP-Brasil** (MP 2.200-2/2001); presume autenticidade, **substitui a assinatura manuscrita**.
- **Atos com a Administração Pública, valor patrimonial relevante, atos notariais e processos** exigem **assinatura qualificada (ICP-Brasil)**.
- **Certificado A1** (ICP-Brasil em arquivo, validade ~1 ano) é válido para assinatura qualificada — adequado para assinatura automatizada server-side (ex.: emissão de documentos, NFS-e, atos). **A3** é em token/cartão.

### Formatos / Layouts
- Assinatura padrão **CAdES / PAdES (PDF) / XAdES (XML)** conforme DOC-ICP do ITI **[a confirmar perfil exigido por documento]**.
- Carimbo de tempo (ACT) para documentos de longo prazo / preservação.

### Integrações
- ICP-Brasil (AC credenciadas / ITI); validação via Verificador de Conformidade do ITI.
- Eventual integração com **assinatura gov.br** (avançada) para cidadãos.

### Prazos
- A1 expira anualmente — exige rotina de renovação/rotação de certificado por tenant.

---

## 4. Transparência — LAI, LRF, Dados Abertos

### Obrigações
- **LAI — Lei 12.527/2011**: transparência ativa (publicar independentemente de pedido) e passiva (e-SIC / Fala.BR). Aplica-se a municípios e câmaras.
- **LRF — LC 101/2000** + **LC 131/2009 (Lei da Transparência)** + **LC 156/2016**: Portal da Transparência obrigatório em todos os níveis, com divulgação em tempo real de **receitas e despesas** (empenho, liquidação, pagamento).
- **Decreto 7.185/2010**: requisitos mínimos do sistema integrado de administração financeira e controle para transparência.
- Instrumentos de transparência da gestão fiscal (LRF art. 48): planos, LDO/LOA/PPA, prestações de contas e parecer prévio, **RREO** e **RGF**, versões simplificadas — ampla divulgação inclusive em meio eletrônico.

### Formatos / Layouts
- **Dados abertos**: formato aberto, não-proprietário, legível por máquina (CSV/JSON/XML), licença aberta, com metadados — conforme política de dados abertos (Decreto 8.777/2016 federal; municípios por simetria) **[obrigatoriedade municipal a confirmar em norma local]**.
- Atualização em tempo real / até 24h da despesa.

### Integrações
- Portal da Transparência por tenant; e-SIC/Fala.BR (CGU) para transparência passiva.
- Exposição de datasets contábeis e de pessoal.

### Prazos
- Tempo real para execução orçamentária; publicação periódica de RREO (bimestral) e RGF (quadrimestral/semestral).

---

## 5. Prestação de Contas ao TCE / Contabilidade Pública (PRIORIDADE DO DONO)

### Obrigações
- **SICONFI (STN)** — envio da **Matriz de Saldos Contábeis (MSC)** em cumprimento ao art. 48, §2º da LRF. MSC **agregada mensal** e **MSC de encerramento**. Falha no envio impede **transferências voluntárias**.
- **TCE-RS — SIAPC/PAD** (Sistema de Informações para Auditoria e Prestação de Contas): remessas mensais de dados contábeis, orçamentários e de **folha de pagamento** via **PAD (Programa Autenticador de Dados)** + **MCI (Manifestação Conclusiva do Controle Interno)**. Sistema próprio do TCE-RS — **fonte primária para o piloto RS**.
- **SIOPE (Educação)** e **SIOPS (Saúde)** — prestação de contas dos mínimos constitucionais de educação e saúde.
- Contabilidade conforme **PCASP** (Plano de Contas Aplicado ao Setor Público), **MCASP** e **NBC TSP**.

### Formatos / Layouts
- **MSC**: PCASP estendido + Plano de Receita + Plano de Despesa + Fonte de Recursos padronizados pela STN (não usa catálogos do TCE). Submissão via SICONFI (XBRL / arquivo conforme Regras Gerais MSC — Portaria STN nº 642 e atualizações anuais).
- **TCE-RS PAD**: leiaute de **arquivos texto (.TXT)** padronizados — ex. `TCE_4810.TXT` para folha; layout publicado a cada versão do PAD (ex.: versão 20.0.0.0). Validações lógico-contábeis mensais antes do envio. **Layout exato a confirmar no Manual SIAPC/PAD da versão vigente.**

### Integrações
- SICONFI (API/upload STN).
- TCE-RS SIAPC/PAD (upload de arquivos validados + PAD desktop/autenticador).
- SIOPE / SIOPS (FNDE / Min. Saúde).

### Prazos
- **MSC agregada**: até o último dia do mês seguinte ao mês de referência.
- **TCE-RS PAD**: remessas **mensais** (prazos conforme resolução TCE-RS vigente) **[a confirmar calendário anual TCE-RS]**.
- RREO bimestral; RGF quadrimestral (municípios <50k hab. podem semestral).

---

## 6. Segurança e Hospedagem (Nuvem Governamental)

### Obrigações
- **GSI/PR — IN nº 5/2021** e **IN nº 8/2025**: critérios de segurança para computação em nuvem no governo. Dados classificados (reservado/secreto) só em **nuvem privada ou comunitária**, em **datacenters em território nacional**, providos por fornecedores habilitados/auditados. Nuvem **pública e híbrida vedadas** para dados classificados; **ultrassecreto vedado em nuvem**.
- Requisitos técnicos: isolamento de ambientes, **criptografia com algoritmo de Estado**, gestão de chaves exclusiva pelo órgão, **MFA**, monitoramento contínuo, controle de acesso rigoroso.
- **Nota**: a maioria dos dados de prefeitura/câmara **não é classificada** no grau reservado/secreto — porém dados sensíveis LGPD (saúde/assistência) exigem rigor equivalente de segurança (art. 46 LGPD). Hospedagem em nuvem pública é admissível para dados não classificados, observando boas práticas. **[Confirmar enquadramento exato dos dados municipais por classificação.]**

### Formatos / Layouts (certificações do provedor)
- **ABNT NBR ISO/IEC 27001** (SGSI), **27017** (nuvem), **27018** (PII em nuvem), **27701** (privacidade), **22237** (datacenter).
- Provedor com estabelecimento no Brasil e situação cadastral ativa.

### Integrações
- KMS/HSM para gestão de chaves; logging/SIEM; backup e DR em território nacional.

### Prazos
- Conformidade contínua; IN 8/2025 publicada em 07/10/2025.

---

## 7. Resumo de Impacto no Produto (checklist transversal)

- [ ] Multi-tenant com isolamento de dados + criptografia em repouso/trânsito.
- [ ] Trilha de auditoria imutável (LGPD + TCE).
- [ ] DPO/encarregado configurável por tenant + RIPD para Saúde/Assistência.
- [ ] Assinatura qualificada ICP-Brasil **A1** server-side (PAdES/XAdES/CAdES) + renovação anual.
- [ ] Acessibilidade WCAG 2.1 AA / eMAG / gov.br DS no frontend.
- [ ] Geração e envio: **MSC (SICONFI)**, **SIAPC/PAD (TCE-RS, .TXT)**, SIOPE, SIOPS.
- [ ] Portal da Transparência (LRF/LAI) + dados abertos (CSV/JSON) por tenant.
- [ ] Hospedagem em datacenter nacional + certificações ISO 27001/27017/27018/27701.

---

## Fontes (URLs)

**LGPD / ANPD**
- Lei 13.709/2018 (LGPD) — Planalto: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm
- L13709 compilado: http://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709compilado.htm
- ANPD — RIPD: https://www.gov.br/anpd/pt-br/canais_atendimento/agente-de-tratamento/relatorio-de-impacto-a-protecao-de-dados-pessoais-ripd
- LGPD na saúde pública (ANAPE): https://anape.org.br/publicacoes/artigos/lei-geral-de-protecao-de-dados-e-reflexos-na-saude-publica
- LGPD — MDS (assistência social): https://www.gov.br/mds/pt-br/acesso-a-informacao/governanca/integridade/campanhas/lgpd

**Acessibilidade**
- eMAG — Governo Eletrônico: https://emag.governoeletronico.gov.br/
- Modelo de Acessibilidade — Governo Digital: https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/acessibilidade-digital/modelo-de-acessibilidade
- Cartilha contratação acessível (PDF): https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/acessibilidade-digital/Cartilhaversao1.0.pdf

**Assinatura Eletrônica / ICP-Brasil**
- Lei 14.063/2020 — Planalto: https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm
- Assinatura eletrônica — Governo Digital: https://www.gov.br/governodigital/pt-br/identidade/assinatura-eletronica/saiba-mais-sobre-a-assinatura-eletronica
- Lei 14.063 e entes públicos (Valid): https://validcertificadora.com.br/blogs/usos-do-certificado-digital/saiba-o-que-diz-a-lei-14-063-sobre-assinaturas-digitais-com-entes-publicos

**Transparência (LAI / LRF / Dados Abertos)**
- Portal da Transparência — Legislação: https://portaldatransparencia.gov.br/sobre/legislacao
- LAI (Wikipédia/visão geral): https://pt.wikipedia.org/wiki/Lei_de_Acesso_%C3%A0_Informa%C3%A7%C3%A3o
- Acesso à Informação — gov.br: https://www.gov.br/acessoainformacao/pt-br
- Tesouro Transparente — dados abertos/legislação: https://www.tesourotransparente.gov.br/sobre/dados-abertos/legislacao

**Prestação de Contas / Contabilidade (TCE / SICONFI)**
- MSC — Tesouro Transparente: https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/matriz-de-saldos-contabeis-msc
- MSC Regras Gerais 2024 (Portaria STN 642) PDF: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2024_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- Cartilha MSC (ABIPEM) PDF: https://www.abipem.org.br/wp-content/uploads/2019/05/Cartilha_Matriz_de_Saldos_Contabeis.pdf
- TCE-RS — Sistemas de controle externo (SIAPC): https://tcers.tc.br/sistemas-de-controle-externo/
- TCE-RS — SIAPC (portal): http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- Manual SIAPC/PAD (MPC-RS) PDF: https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf

**Segurança / Nuvem Governamental**
- GSI/PR — norma nuvem para informações classificadas (IN 8/2025): https://www.gov.br/gsi/pt-br/centrais-de-conteudo/noticias/2025/gsi-pr-publica-norma-para-uso-de-computacao-em-nuvel-para-tratamento-de-informacoes-classificadas
- Computação em Nuvem no Governo Federal — Governo Digital: https://www.gov.br/governodigital/pt-br/estrategias-e-governanca-digital/estrategias-e-politicas-digitais/computacao-em-nuvem
