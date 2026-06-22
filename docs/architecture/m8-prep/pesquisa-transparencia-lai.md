# Pesquisa — Transparência / LAI completa (M8)

> Escopo: Transparência ATIVA, DADOS ABERTOS, Transparência PASSIVA (e-SIC) e
> relação com o módulo `Transparencia` já existente. Insumo para o M8
> (Portal/App do Cidadão + Portal do Gestor + BI + Transparência).
> Regra §16: nada inventado — cada afirmação tem FONTE; lacunas marcadas `[a confirmar]`.
> Data da pesquisa: 2026-06-22.

---

## 0. Marco legal aplicável (pirâmide normativa)

| Norma | Objeto | Aplica a municípios? |
|---|---|---|
| **CF/88, art. 5º XXXIII, 37 §3º II, 216 §2º** | Direito fundamental de acesso à informação | Sim |
| **Lei 12.527/2011 (LAI)** | Acesso à informação — ativa + passiva | Sim (art. 1º, II — abrange Municípios) |
| **LC 101/2000 (LRF)** + **LC 131/2009 (Lei da Transparência)** | Transparência da gestão fiscal em **tempo real** (art. 48-A) | Sim |
| **Decreto 7.724/2012** | Regulamenta a LAI (âmbito federal — referência/modelo p/ município regulamentar por decreto próprio) | Modelo `[a confirmar regulamento municipal]` |
| **Decreto 10.540/2020 (SIAFIC)** | Padrão mínimo de qualidade do sistema de adm. financeira/contábil — revogou o Decreto 7.185/2010 | Sim |
| **Decreto 8.777/2016** | Política de Dados Abertos do Executivo federal (modelo) | Modelo |
| **Lei 13.709/2018 (LGPD)** | Limite à transparência: dados pessoais/sensíveis | Sim (intersecção crítica) |

FONTES:
- LAI: https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12527.htm
- Decreto 7.724/2012: https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2012/decreto/d7724.htm
- LC 131/2009: https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp131.htm
- Decreto 8.777/2016 (dados abertos): https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2016/decreto/d8777.htm

---

## 1. Transparência ATIVA — o que DEVE ser publicado espontaneamente

**Conceito:** dever do poder público de **divulgar de forma proativa e espontânea**, em sítio
oficial na internet, informação de interesse coletivo, **independentemente de pedido**.
FONTE: LAI art. 8º, *caput*; Senado — https://www12.senado.leg.br/noticias/materias/2015/05/15/entenda-a-lei-de-acesso-a-informacao

### 1.1 Rol mínimo — LAI art. 8º, §1º (incisos I a VI)
1. **I** — registro das competências e **estrutura organizacional**, endereços, telefones e horários de atendimento;
2. **II** — registros de **repasses ou transferências** de recursos financeiros;
3. **III** — registros das **despesas**;
4. **IV** — informações sobre **procedimentos licitatórios** (editais, resultados) e **contratos** celebrados;
5. **V** — dados gerais de **programas, ações, projetos e obras**;
6. **VI** — respostas a perguntas mais frequentes da sociedade.

FONTE: LAI art. 8º §1º (incisos no texto Planalto). Reforço CGU/CAPES: https://www.gov.br/capes/pt-br/acesso-a-informacao/servico-de-informacao-ao-cidadao-sic/a-lei-de-acesso-a-informacao-lai

### 1.2 Itens de transparência fiscal em **tempo real** — LC 131/2009 (art. 48-A da LRF)
- **Despesas:** todos os atos de execução — nº do processo, bem fornecido/serviço prestado, **beneficiário/credor do pagamento**, e dados da licitação/dispensa quando houver (empenho → liquidação → pagamento).
- **Receitas:** lançamento e recebimento de toda a receita, **inclusive extraordinária**.

FONTE: LC 131/2009 art. 48-A; Âmbito Jurídico — https://ambitojuridico.com.br/lei-complementar-131-de-27-05-2009-aumento-da-transparencia-nos-gastos-publicos-como-instrumento-democratico/

### 1.3 Itens consolidados que um portal municipal precisa expor (rol prático cobrado em rankings)
- **Despesas** (empenho/liquidação/pagamento, credor, classificação funcional-programática)
- **Receitas** (previsão e arrecadação, por natureza)
- **Licitações** (editais, modalidade, resultado) e **Contratos** (objeto, valor, vigência, contratado, aditivos)
- **Servidores / folha** (nome, cargo, lotação, remuneração) — `[atenção LGPD: ver §5]`
- **Diárias e passagens** (favorecido, destino, valor, finalidade)
- **Obras** (acompanhamento físico-financeiro)
- **Estrutura organizacional / agenda / horários**
- **Convênios e transferências**

FONTE (rol cobrado): CGU — Escala Brasil Transparente 360 (ver §4).

### 1.4 Meio e exceção dos pequenos municípios
- Divulgação obrigatória em **sítio oficial na internet** (LAI art. 8º §2º).
- **Exceção** (LAI art. 8º §4º): Municípios com **até 10.000 habitantes** ficam dispensados da divulgação obrigatória **na internet** do rol do art. 8º — **mas NÃO** das obrigações de transparência fiscal da LRF/LC 131. `[implicação de produto: feature flag por porte do município, mas tempo-real fiscal sempre ON]`
FONTE: LAI art. 8º §4º; reforço — câmara Colatina-ES: https://camaracolatina.es.gov.br/pagina/ler/1031/perguntas-frequentes-do-portal-da-transparencia

---

## 2. DADOS ABERTOS — formato e atualização

### 2.1 Requisitos técnicos — LAI art. 8º §3º
O sítio deve atender, entre outros, requisitos de:
- ferramenta de **pesquisa de conteúdo** que permita acesso por diversos critérios;
- possibilitar **gravação de relatórios em formatos eletrônicos diversos, inclusive abertos e não proprietários** (ex.: planilha, texto — CSV, ODS), para facilitar a análise;
- possibilitar **acesso automatizado por sistemas externos em formatos abertos, estruturados e legíveis por máquina** (→ **API** / dados estruturados);
- divulgar em **formato aberto e não proprietário**, com detalhamento da última atualização;
- garantir **autenticidade e integridade**;
- manter atualizadas as informações;
- indicar **local e instruções** para o requerente comunicar falha e obter informação;
- garantir **acessibilidade** a pessoas com deficiência (eMAG `[a confirmar exigência específica]`).

FONTE: LAI art. 8º §3º; Decreto 7.724/2012 (gravação em formatos abertos / acesso automatizado por sistemas externos) — https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2012/decreto/d7724.htm

### 2.2 Tempo real — definição operacional
"Tempo real" = disponibilização da informação em meio eletrônico de amplo acesso público
**até o 1º dia útil seguinte** ao do registro contábil no respectivo sistema (sem prejuízo das
rotinas de segurança operacional). Originalmente Decreto 7.185/2010, **hoje regido pelo Decreto 10.540/2020 (SIAFIC)**.
FONTE: ICO-CE — https://ico.ce.gov.br/informa/222/lei-da-transpar-ncia-lc-131-2009-fiscaliza-o-e-ori ; Decreto 10.540/2020.

### 2.3 Prazos históricos de implantação da LC 131 (referência)
1 ano (União/Estados/DF e Municípios > 100 mil hab); 2 anos (50–100 mil); 4 anos (até 50 mil).
*Já vencidos — todos os municípios estão obrigados hoje.* FONTE: mesma acima.

`[implicação de produto: o M8/BI deve consumir os dados do SIAFIC/contabilidade e publicar
até D+1 útil; expor download CSV/ODS + API REST/JSON de dados abertos com data de última atualização]`

---

## 3. Transparência PASSIVA — e-SIC (pedido de informação)

### 3.1 Pedido (LAI art. 10)
- Qualquer interessado pode pedir; só pode ser exigida **identificação do requerente** (dados básicos) — **vedado exigir motivos** do pedido (art. 10 §3º).
- O órgão deve viabilizar pedido por **meio eletrônico (e-SIC)** (art. 10 §2º).
FONTE: LAI art. 10; WikiLAI/Fiquem Sabendo — https://wikilai.fiquemsabendo.com.br/wiki/Pedido_de_informa%C3%A7%C3%A3o ; gov.br/acessoainformacao — https://www.gov.br/acessoainformacao/pt-br/assuntos/pedidos/prazos

### 3.2 Prazos (LAI art. 11) — críticos para o motor de SLA
| Etapa | Prazo |
|---|---|
| Acesso imediato | quando disponível de imediato |
| Resposta padrão | **20 dias** |
| Prorrogação | **+10 dias**, mediante **justificativa expressa** enviada ao requerente **antes** do fim dos 20 dias |
| Recurso (negativa) à autoridade hierarquicamente superior | **10 dias** para interpor / **5 dias** para decidir |

FONTE: LAI art. 11 e arts. 15–17; gov.br — https://www.gov.br/acessoainformacao/pt-br/assuntos/pedidos/prazos ; MPC-PR — https://www.mpc.pr.gov.br/index.php/municipio-deve-cumprir-prazos-da-lei-de-acesso-a-informacao/
`[a confirmar números exatos de art. 15/16/17 contra texto Planalto — fetch do Planalto falhou por timeout; prazos de recurso 10/5 dias confirmados por fontes secundárias gov.br]`

### 3.3 Requisitos funcionais do e-SIC (cobrados em ranking)
- existência de e-SIC **online** + SIC **físico/presencial**;
- **rastreamento** do andamento do pedido pelo cidadão (protocolo);
- **não exigir identificação excessiva** nem criar barreiras (cadastro complexo) ao pedido;
- **responder no prazo legal**;
- **resposta conforme** o que foi pedido (não evasiva).
FONTE: CGU EBT — https://www.gov.br/cgu/pt-br/assuntos/transparencia-publica/escala-brasil-transparente-360

---

## 4. O que REPROVA num índice de transparência (CGU EBT 360 / TCEs)

**Escala Brasil Transparente – Avaliação 360° (CGU)** — instrumento que ranqueia estados/municípios.
Estrutura: **Transparência Ativa = 50%** + **Transparência Passiva = 50%**.
(Na versão anterior eram 12 quesitos: 25% regulamentação da LAI + 75% existência/funcionamento do SIC.)

**Quesitos de Transparência ATIVA avaliados:** publicação de **receitas e despesas**, **licitações e contratos**, **estrutura administrativa**, **servidores públicos**, **diárias**, **acompanhamento de obras**.

**Quesitos de Transparência PASSIVA avaliados:** divulgação do SIC físico; existência de e-SIC; **possibilidade de rastrear** o pedido; **ausência de pontos que dificultem/impeçam** o pedido; **resposta no prazo legal**; **resposta conforme** o solicitado.

### O que REPROVA / zera pontos (sinais de não conformidade):
- Portal **sem dados de despesa/receita atualizados** ou sem o detalhe credor/empenho;
- **Sem e-SIC** eletrônico, ou e-SIC que **exige cadastro/identificação excessiva** ("ponto que dificulta o pedido");
- Pedido **sem protocolo/rastreio**;
- **Resposta fora do prazo** (>20 dias sem prorrogação justificada) ou **ausência de resposta**;
- Resposta **evasiva / não conforme** o pedido;
- Dados **não baixáveis em formato aberto** / sem API / sem data de atualização;
- **Exigir motivo** do pedido (viola LAI art. 10 §3º);
- LAI **não regulamentada** por norma local.

FONTES: CGU — Metodologia EBT: https://www.gov.br/cgu/pt-br/assuntos/transparencia-publica/escala-brasil-transparente-360 ; Mapa Brasil Transparente: https://mbt.cgu.gov.br/ ; Zênite (notas revisadas EBT 360): https://www.zenite.com.br/noticias/cgu-informa-notas-revisadas-da-escala-brasil-transparente-avaliacao-360/
`[a confirmar metodologia específica do TCE-RS — projeto é RS (M4 cita TCE-RS/SICONFI); cada TCE pode ter índice próprio (ex.: índice de transparência estadual). Pesquisar checklist TCE-RS no M8.]`

---

## 5. Intersecção crítica: Transparência × LGPD (limite)

A transparência **não é absoluta**: dados **pessoais** (sobretudo sensíveis — saúde, biometria,
CPF completo, endereço residencial de servidor, dados de beneficiários de assistência) têm
publicidade restrita (LGPD + LAI arts. 31 e 23 — informações pessoais e classificadas/sigilosas).
- Folha de servidores: publica-se **nome, cargo, lotação, remuneração bruta** — mas **não** CPF, conta, endereço.
- Beneficiários de saúde/educação/assistência (módulo M7): **não** expor identificação nominal de pessoa vulnerável.
FONTE: LAI arts. 23 e 31; LGPD Lei 13.709/2018. `[a confirmar — detalhar regras de anonimização por dataset no M8]`

---

## 6. Relação com o módulo `Transparencia` já existente

`[a confirmar no código — grep encontrou referências em docs (RUNBOOK, PLANO-MESTRE, ADRs)
mas não foi inspecionado o módulo de implementação nesta pesquisa]`

Hipótese de arquitetura para o M8 (a validar contra o módulo existente):
- O módulo `Transparencia` deve ser **consumidor** dos módulos-fonte (contabilidade/finanças → despesa/receita; protocolo/licitações → contratos; RH/folha → servidores/diárias) — **não** duplicar dados.
- **Camada de publicação:** seção de transparência ativa (HTML navegável) + **export aberto** (CSV/ODS) + **API de dados abertos** (REST/JSON), com carimbo de "última atualização" e cadência D+1 útil (SIAFIC).
- **e-SIC:** módulo de protocolo de pedidos com **motor de SLA** (20+10 dias, recurso 10/5), rastreio por protocolo, painel do gestor (Portal do Gestor) com pedidos a vencer.
- **BI:** o item "Portal do Gestor + BI" do M8 deve incluir um **painel de conformidade EBT** (autoavaliação dos quesitos do §4) — diferencial de produto.
- **LGPD gate:** pipeline de anonimização/mascaramento antes da publicação (§5).

---

## 7. Pendências / próximos passos (M8)

1. `[a confirmar]` Inspecionar o módulo `Transparencia` existente no código (contrato de dados, o que já publica).
2. `[a confirmar]` Texto literal dos arts. 8º (incisos), 11, 15–17 da LAI direto no Planalto (fetch falhou por timeout — usar mirror ou download).
3. `[a confirmar]` Checklist/índice de transparência **do TCE-RS** especificamente (projeto é RS).
4. `[a confirmar]` Existência e conteúdo do **decreto municipal** regulamentador da LAI (modelo cliente).
5. `[a confirmar]` Requisitos de **acessibilidade** (eMAG/WCAG) exigíveis no portal.
6. `[a confirmar]` Detalhes do **Decreto 10.540/2020 (SIAFIC)** quanto a campos mínimos e periodicidade de publicação.

---

### Todas as FONTES (URLs)
- LAI: https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12527.htm
- Decreto 7.724/2012: https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2012/decreto/d7724.htm
- LC 131/2009: https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp131.htm
- Decreto 8.777/2016: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2016/decreto/d8777.htm
- CGU EBT 360: https://www.gov.br/cgu/pt-br/assuntos/transparencia-publica/escala-brasil-transparente-360
- CGU Mapa Brasil Transparente: https://mbt.cgu.gov.br/
- gov.br Prazos LAI: https://www.gov.br/acessoainformacao/pt-br/assuntos/pedidos/prazos
- gov.br/CAPES — LAI: https://www.gov.br/capes/pt-br/acesso-a-informacao/servico-de-informacao-ao-cidadao-sic/a-lei-de-acesso-a-informacao-lai
- Senado — Entenda a LAI: https://www12.senado.leg.br/noticias/materias/2015/05/15/entenda-a-lei-de-acesso-a-informacao
- MPC-PR — prazos LAI municípios: https://www.mpc.pr.gov.br/index.php/municipio-deve-cumprir-prazos-da-lei-de-acesso-a-informacao/
- ICO-CE — LC 131 / tempo real: https://ico.ce.gov.br/informa/222/lei-da-transpar-ncia-lc-131-2009-fiscaliza-o-e-ori
- WikiLAI (Fiquem Sabendo): https://wikilai.fiquemsabendo.com.br/wiki/Pedido_de_informa%C3%A7%C3%A3o
