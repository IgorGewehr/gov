# Estudo — Funções de um Sistema Público Municipal Completo e Obrigações Legais Transversais

> **Escopo:** insumo de pesquisa para o estudo de produto do Tensorroot.Gov (ERP GovTech municipal multi-tenant; piloto Maximiliano de Almeida/RS).
> **Método:** fontes oficiais (Planalto, STN/SICONFI, gov.br, TCU, ANPD) priorizadas; pontos não confirmados em fonte primária estão marcados **[a confirmar]** conforme CLAUDE.md §16.
> **Data da pesquisa:** 2026-06-22.

---

## 1. Funções de um sistema público municipal completo

Um ERP de gestão pública municipal cobre o ciclo administrativo completo do município (Poder Executivo, e por vezes Câmara e autarquias). As funções abaixo são as observadas em soluções consolidadas de mercado (e-Cidade/Software Público, IPM, Atende.Net, GeoPixel, SmarAPD) e refletem o que o piloto precisa endereçar ao longo do roadmap.

### 1.1 Núcleo financeiro-orçamentário (já coberto/em curso no Tensorroot.Gov — M2/M3/M4)
- **Planejamento orçamentário:** PPA, LDO, LOA; programação financeira e cronograma de desembolso.
- **Contabilidade pública (PCASP):** plano de contas aplicado ao setor público, lançamentos automáticos, conformidade MCASP.
- **Execução orçamentária da despesa:** empenho → liquidação → pagamento (ciclo da Lei 4.320).
- **Execução da receita:** previsão, lançamento, arrecadação, recolhimento.
- **Tesouraria/financeiro:** conciliação bancária, fluxo de caixa, restos a pagar.
- **Demonstrações contábeis (DCASP):** Balanço Orçamentário, Financeiro, Patrimonial e DVP (Anexos 12–15 da Lei 4.320).
- **Prestação de contas:** geração de artefatos para TCE-RS e SICONFI/STN (MSC, RREO, RGF).

### 1.2 Arrecadação e tributos (roadmap M6)
- Cadastro imobiliário (IPTU) e mobiliário (ISS/alvará); ITBI; taxas; contribuição de melhoria; COSIP.
- Dívida ativa e cobrança (administrativa e judicial — execução fiscal).
- Nota Fiscal de Serviço eletrônica (NFS-e) e integração ao padrão nacional.
- Atendimento ao contribuinte e parcelamentos (REFIS).

### 1.3 Recursos Humanos e folha (roadmap M5)
- Cadastro funcional, lotação, cargos/carreiras; folha de pagamento; férias, benefícios, consignações.
- Ponto/frequência; gestão de afastamentos; previdência (RPPS/RGPS).
- Integração **eSocial** e demais obrigações trabalhistas-previdenciárias.

### 1.4 Suprimentos / compras / contratos (roadmap M9, parcialmente transversal)
- Licitações em todas as modalidades (Lei 14.133) e contratação direta.
- Gestão de contratos, atas de registro de preços, fiscalização contratual.
- Almoxarifado, patrimônio (bens móveis/imóveis), frota.
- Integração obrigatória ao **PNCP**.

### 1.5 Áreas sociais e serviços ao cidadão (roadmap M7/M8)
- **Saúde:** prontuário/agendamento, medicamentos, leitos, integração à rede; repasses do Fundo Municipal de Saúde.
- **Educação:** cadastro de alunos/professores, matrícula, frequência, transporte e merenda escolar; repasses (FUNDEB).
- **Assistência social:** cadastro único, programas, benefícios eventuais; repasses do Fundo Municipal de Assistência Social.
- **Portal do cidadão / autoatendimento:** 2ª via de tributos, protocolo, ouvidoria, agendamentos.

### 1.6 Funções transversais de governança digital
- **Protocolo / processo administrativo eletrônico** (digitalização do trâmite).
- **Portal da Transparência** (transparência ativa — ver §3).
- **Serviço de Informação ao Cidadão (e-SIC)** (transparência passiva — ver §3).
- **Ouvidoria.**
- **Trilha de auditoria imutável, RBAC/ABAC, multi-tenancy, assinatura eletrônica** (já implementado: M1/A1).

---

## 2. Obrigações legais transversais — visão consolidada

| Norma | Objeto | O que gera | Destino | Prazo / periodicidade |
|---|---|---|---|---|
| **LC 101/2000 (LRF)** | Responsabilidade fiscal | RREO | SICONFI / STN | até **30 dias** após cada **bimestre** |
| **LC 101/2000 (LRF)** | Responsabilidade fiscal | RGF | SICONFI / STN | até **30 dias** após cada **quadrimestre** (ou semestral p/ municípios < 50 mil hab. que optarem — art. 63) |
| **LC 101/2000 (LRF) art. 48 §2º** | Consolidação contábil | **MSC** (Matriz de Saldos Contábeis) | SICONFI / STN | **mensal** — até o último dia do mês seguinte ao de referência |
| **Lei 4.320/1964** | Orçamento e balanços | Balanços anuais (Anexos 12–15) e ciclo empenho/liquidação/pagamento | TCE-RS / prestação de contas anual | **anual** (exercício = ano civil) |
| **Lei 12.527/2011 (LAI)** | Transparência ativa | Portal da Transparência (receitas, despesas, contratos, etc.) | Internet (cidadão) | **tempo real / contínuo** |
| **Lei 12.527/2011 (LAI)** | Transparência passiva | Resposta a pedido via e-SIC | Cidadão solicitante | **20 dias**, prorrogáveis por **+10 dias** mediante justificativa |
| **Lei 13.709/2018 (LGPD)** | Proteção de dados | Indicação de Encarregado (DPO); RIPD quando aplicável | ANPD / titulares | Encarregado **obrigatório** no poder público; prazo do RIPD **[a confirmar]** (regulamentação ANPD) |
| **Lei 14.133/2021** | Licitações e contratos | Editais, contratos e aditamentos | **PNCP** | Contrato: **20 dias úteis** (licitação) / **10 dias úteis** (contratação direta), da assinatura — condição de eficácia |
| **Lei 14.063/2020** | Assinatura eletrônica | Assinatura simples/avançada/qualificada em atos eletrônicos | Validade do ato perante o ente público | nível conforme criticidade do ato |
| **Lei 13.146/2015 (LBI) art. 63 + eMAG/WCAG** | Acessibilidade digital | Portais e sistemas acessíveis | Cidadão (PcD) | obrigatório de forma contínua |

---

## 3. Detalhamento por norma

### 3.1 LRF — Lei Complementar nº 101/2000
Base do regime fiscal. O **art. 48** trata da transparência da gestão fiscal; o **art. 48 §2º** fundamenta o envio da **Matriz de Saldos Contábeis (MSC)** ao SICONFI.

- **RREO (Relatório Resumido de Execução Orçamentária):** bimestral, entrega ao SICONFI até **30 dias** após o fim de cada bimestre, a partir do 1º bimestre (jan–fev).
- **RGF (Relatório de Gestão Fiscal):** quadrimestral, entrega até **30 dias** após o fim de cada quadrimestre. Municípios com **menos de 50 mil habitantes** podem optar pela publicação **semestral** (art. 63 da LRF), registrando a opção no SICONFI.
- **MSC:** mensal, enviada até o **último dia do mês seguinte** ao mês de referência.

> **Relevância p/ Tensorroot.Gov:** M3 já produz MSC + DCASP; M4 fecha os artefatos de prestação de contas (RREO/RGF + TCE-RS). Confirmar **layout/versão vigente** das Regras Gerais SICONFI antes de codar cada integração (CLAUDE.md §16).
>
> Fontes: [SICONFI — Regras Gerais RREO 2025](https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2025_Regras_Gerais_e_Instrucoes_de_preenchimento_RREO.pdf), [SICONFI — Regras Gerais RGF 2026](https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Regras_Gerais_e_Instrucoes_de_preenchimento_RGF_04032026.pdf), [Tesouro Transparente — LRF](https://www.tesourotransparente.gov.br/temas/execucao-orcamentaria-e-financeira/lei-de-responsabilidade-fiscal-lrf), [Cartilha MSC](https://www.abipem.org.br/wp-content/uploads/2019/05/Cartilha_Matriz_de_Saldos_Contabeis.pdf)

### 3.2 Lei 4.320/1964 — Normas gerais de direito financeiro
Define o **exercício financeiro** (coincide com o ano civil) e o ciclo da despesa: **empenho** (art. 58–60), **liquidação** (art. 63), **pagamento** (art. 62–65). Os resultados do exercício são demonstrados nos **balanços** — Orçamentário, Financeiro, Patrimonial e DVP (**Anexos 12, 13, 14 e 15**), além das tabelas dos Anexos 1, 6–11, 16 e 17. Estes alimentam a **prestação de contas anual** ao **TCE-RS**.

> Fontes: [Planalto — Lei 4.320/1964](https://www.planalto.gov.br/ccivil_03/leis/l4320.htm), [Lei 4.320 comentada — Título IV (Exercício Financeiro)](https://www.jusbrasil.com.br/doutrina/secao/art-34-titulo-iv-do-exercicio-financeiro-orcamentos-publicos-a-lei-4320-1964-comentada/1250396852)

### 3.3 LAI — Lei nº 12.527/2011 (Acesso à Informação)
- **Transparência ativa (art. 8):** dever de divulgar proativamente informações de interesse coletivo na internet, em **tempo real** (receitas, despesas, contratos, repasses, estrutura, etc.). Municípios com até **10.000 habitantes** estão dispensados da divulgação obrigatória na internet, **mantida** a obrigatoriedade de divulgação em tempo real **[a confirmar redação exata do dispositivo aplicável]**.
- **Transparência passiva (art. 11):** resposta a pedido do cidadão (e-SIC) em **20 dias**, prorrogáveis por **mais 10 dias** mediante justificativa expressa.

> Fontes: [Planalto — Lei 12.527/2011](https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12527.htm), [Cartilha LAI — Senado](https://www12.senado.leg.br/transparencia/indice-de-transparencia-dos-portais-legislativos/arquivos/sobre/cartilha-lai)

### 3.4 LGPD — Lei nº 13.709/2018
Aplica-se ao tratamento de dados pelo Poder Público.
- **Encarregado (DPO):** indicação **obrigatória** na Administração Pública; pode haver mais de um, conforme volume/complexidade. Atua como canal entre controlador, titulares e **ANPD**.
- **RIPD (Relatório de Impacto à Proteção de Dados Pessoais):** documento do controlador descrevendo o tratamento, riscos e mitigação. Prazo/forma de apresentação **a ser regulamentado pela ANPD [a confirmar]**.
- **Princípio da responsabilização (accountability):** a administração não pode invocar despreparo institucional para ausência de governança de dados.

> **Relevância p/ Tensorroot.Gov:** multi-tenancy + auditoria imutável + cofre (A1) já endereçam segurança/registro; faltará formalizar papel de Encarregado e RIPD por tenant.
>
> Fontes: [Planalto — Lei 13.709/2018](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm), [ANPD — Guia Tratamento de Dados pelo Poder Público (v1.0, 2022)](https://www.ifsp.edu.br/images/reitoria/docs/guia-poder-publico-anpd-versao-final.pdf)

### 3.5 Lei nº 14.133/2021 — Licitações e Contratos + PNCP
- **PNCP (art. 174):** sítio oficial único para divulgação **centralizada e obrigatória** dos atos da lei (editais e seus anexos, contratos, atas de registro de preços, etc.).
- **Divulgação de contratos (art. 94):** condição **indispensável à eficácia** do contrato e seus aditamentos; prazos contados da assinatura:
  - **20 dias úteis** — no caso de licitação;
  - **10 dias úteis** — no caso de contratação direta.
- **Editais:** publicidade pelo inteiro teor no PNCP + extrato em Diário Oficial e jornal de grande circulação.
- **Municípios com até 20.000 habitantes:** prazo estendido para obrigatoriedade plena do PNCP (até **31/03/2027**) **[a confirmar fundamento normativo vigente]** — relevante para o piloto, que deve ser dimensionado.

> Fontes: [Planalto — Lei 14.133/2021](https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm), [TCU — Divulgação do edital](https://licitacoesecontratos.tcu.gov.br/5-1-divulgacao-do-edital/), [Effecti — Contratos na Lei 14.133 (2026)](https://effecti.com.br/contratos-na-nova-lei-de-licitacoes/)

### 3.6 Lei nº 14.063/2020 — Assinaturas eletrônicas
Regula o uso de assinaturas eletrônicas nas **interações com entes públicos** e **entre órgãos públicos**. Três níveis:
1. **Simples** — identifica o signatário, anexa/associa dados.
2. **Avançada** — certificados não-ICP-Brasil ou outro meio que comprove autoria/integridade aceito pelas partes.
3. **Qualificada** — baseada em certificado **ICP-Brasil** (e-CPF/e-CNPJ); presunção de autenticidade; admitida em **qualquer** interação com ente público, independentemente de cadastro prévio.

O nível exigido cresce com a criticidade do ato (atos de maior repercussão tendem a exigir avançada/qualificada).

> **Relevância p/ Tensorroot.Gov:** A1 (assinatura + cofre por envelope encryption) já implementado; mapear quais atos exigem qualificada (ICP-Brasil) vs avançada.
>
> Fontes: [Planalto — Lei 14.063/2020](https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm), [gov.br — Assinatura eletrônica](https://www.gov.br/governodigital/pt-br/identidade/assinatura-eletronica/saiba-mais-sobre-a-assinatura-eletronica)

### 3.7 Acessibilidade — Lei nº 13.146/2015 (LBI) art. 63 + eMAG/WCAG
- **Art. 63 da LBI:** obrigatória a acessibilidade nos sites mantidos por órgãos governamentais e por empresas com sede/representação no Brasil, garantindo acesso de pessoas com deficiência conforme melhores práticas e diretrizes internacionais.
- **Padrões:** **eMAG** (Modelo de Acessibilidade em Governo Eletrônico) para sites de governo + **WCAG** como referência internacional; **ABNT NBR 17225** como norma técnica nacional.
- **Status regulatório:** decorridos ~10 anos da LBI, o art. 63 ainda carece de ato normativo federal que fixe parâmetros técnicos obrigatórios — a obrigação legal existe, a regulamentação técnica vinculante segue pendente **[a confirmar atualizações 2026]**.

> **Relevância p/ Tensorroot.Gov:** Portal do cidadão (M8) deve nascer eMAG/WCAG-aware desde o design.
>
> Fontes: [Planalto — Lei 13.146/2015](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2015/lei/l13146.htm), [eMAG — Governo Eletrônico (cartilha de contratação)](https://emag.governoeletronico.gov.br/cartilha-contratacao/), [gov.br — Acessibilidade (legislação)](https://www.gov.br/governodigital/pt-br/legislacao/acessibilidade)

---

## 4. Síntese "quem envia o quê, para quem e quando"

- **Mensal →** MSC ao SICONFI/STN (último dia do mês seguinte).
- **Bimestral →** RREO ao SICONFI/STN (30 dias após o bimestre).
- **Quadrimestral (ou semestral se < 50 mil hab.) →** RGF ao SICONFI/STN (30 dias após o período).
- **Anual →** Balanços (Lei 4.320) e prestação de contas ao TCE-RS.
- **Por evento (assinatura de contrato) →** PNCP, em 20 dias úteis (licitação) / 10 dias úteis (contratação direta).
- **Contínuo / tempo real →** Transparência ativa (Portal da Transparência) ao cidadão (LAI art. 8).
- **Sob demanda →** Transparência passiva via e-SIC, 20 + 10 dias (LAI art. 11).
- **Permanente →** Encarregado LGPD como canal com ANPD; acessibilidade (LBI/eMAG); assinatura eletrônica (Lei 14.063) conforme criticidade do ato.

---

## 5. Pontos a confirmar (CLAUDE.md §16)
1. Layout/versão **vigente** de cada leiaute SICONFI (RREO, RGF, MSC) — confirmar nas Regras Gerais do ano-exercício antes de codar.
2. Prazo de apresentação do **RIPD** (pendente de regulamentação ANPD).
3. Redação exata e atualização do **art. 8 §4º LAI** (dispensa para municípios ≤ 10 mil hab.).
4. Fundamento normativo vigente do **prazo estendido de PNCP** para municípios ≤ 20 mil hab. (até 31/03/2027).
5. Atualizações **2026** sobre regulamentação técnica do **art. 63 LBI** (ABNT NBR 17225 / ato vinculante).
6. Obrigações específicas do **TCE-RS** (leiautes próprios de prestação de contas, ex.: sistemas estaduais) — pesquisar fonte oficial do TCE-RS para M4.
