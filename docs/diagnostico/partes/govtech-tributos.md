# GovTech — Bounded Context TRIBUTOS (Pesquisa de Produção)

> Pesquisa de requisitos para um sistema municipal de Tributos em produção no Brasil.
> Foco do piloto: Maximiliano de Almeida/RS → jurisdição **TCE-RS** (SIAPC/PAD).
> Itens não confirmados em fonte oficial estão marcados como **[a confirmar em fonte oficial]**.
> Data da pesquisa: 2026-06.

---

## 1. ESCOPO FUNCIONAL — o que o módulo precisa cobrir

### 1.1 Impostos e Receitas Municipais
- **IPTU** — Imposto Predial e Territorial Urbano. Núcleo: **Cadastro Imobiliário (CIB/inscrição imobiliária)**, **Planta Genérica de Valores (PGV)**, cálculo do Valor Venal (terreno + edificação), progressividade/alíquotas, isenções/imunidades, lançamento anual em lote, carnê/cota única + parcelas, revisão e impugnação.
- **ITBI** — Imposto sobre Transmissão de Bens Imóveis "Inter-Vivos". Guia de avaliação, base de cálculo (maior entre valor de transação e valor venal de referência), declaração do contribuinte, integração com cartórios de registro de imóveis. **[a confirmar]**: STF Tema 1.124 fixou que base do ITBI é o valor da transação, não a base do IPTU/arbitramento prévio.
- **ISS / ISSQN** — Imposto Sobre Serviços. Cadastro Mobiliário (empresas/autônomos), Lista de Serviços (LC 116/2003), ISS próprio, **ISS retido na fonte (substituição tributária)**, ISS Simples Nacional (DAS/DAF607), Declaração Mensal de Serviços, escrituração eletrônica. Fortemente acoplado à **NFS-e** (ver seção 2).
- **Taxas** — Taxa de Coleta de Lixo, Taxa de Iluminação/COSIP (CIP), Taxa de Fiscalização de Funcionamento, Taxas de Poder de Polícia em geral.
- **Alvarás e licenças** — Alvará de Funcionamento, Alvará de Construção/Habite-se, Localização. Acoplados a Protocolo e fiscalização.
- **Contribuição de Melhoria** — quando houver obra pública valorizadora.

### 1.2 Cadastros estruturantes (pré-requisito de tudo)
- **Cadastro Único de Contribuintes (CPF/CNPJ)** — pessoas físicas e jurídicas.
- **Cadastro Imobiliário** (BCI — Boletim de Cadastro Imobiliário) e o novo **CIB** (Cadastro Imobiliário Brasileiro / SINTER da Receita Federal). **[a confirmar prazo de adesão municipal]**.
- **Cadastro Mobiliário** (econômico/atividades — CNAE + lista LC 116).
- **PGV** versionada por exercício (lei municipal anual).

### 1.3 Conta Corrente Fiscal / Lançamento e Arrecadação
- Conta corrente do contribuinte (débitos, créditos, compensações).
- Geração de DAM/guias com **código de barras / PIX QRCode (padrão arrecadação FEBRABAN)**.
- Conciliação bancária do arrecadado (retorno bancário CNAB 240/400). **[a confirmar layout do banco arrecadador]**.
- Parcelamentos, REFIS/anistias, juros/multa/correção (SELIC ou índice municipal), prescrição/decadência.

---

## 2. NFS-e PADRÃO NACIONAL (Sistema Nacional NFS-e + ADN) — OBRIGATÓRIO 2026

### 2.1 Marco regulatório e prazo
- **Convênio Nacional NFS-e** (Receita Federal + ABRASF + municípios). Padronização nacional do leiaute e do ambiente de emissão.
- A partir de **1º de janeiro de 2026**, municípios e o DF estão obrigados a **autorizar seus contribuintes a emitirem a NFS-e Nacional no Ambiente de Dados Nacional (ADN)**. Adesão pode ser **parcial** (município mantém sistema próprio e *compartilha* as notas com o ADN) ou via **Emissor Nacional**.
- Conexão direta com a **Reforma Tributária** (EC 132/2023): a NFS-e Nacional já carrega os campos de IBS/CBS para a transição (ver seção 4).

### 2.2 Arquitetura técnica (ADN)
- **ADN — Ambiente de Dados Nacional**: infraestrutura central que recebe, valida, processa, armazena e **distribui** os DF-e (Documentos Fiscais eletrônicos) aos municípios de interesse.
- **DPS — Declaração de Prestação de Serviço**: documento que o contribuinte emite; a API valida (regras de negócio) e gera a **NFS-e**.
- **API DF-e**: recepção/distribuição dos documentos compartilhados pelos municípios conveniados.
- **CNC — Cadastro Nacional de Contribuintes** (módulo do sistema nacional).
- **Emissor Público Nacional Web** + Painel Administrativo Municipal.

### 2.3 Formatos / Leiautes
- **Mensagens de API: JSON.** **Leiaute do DF-e (NFS-e e Eventos): XML com assinatura digital.**
- Schemas: **NFSe-ESQUEMAS_XSD v1.01 (fev/2026)**.
- **Anexo I** — Leiautes DPS/NFS-e (SEFIN ADN-DPS NFSe, fev/2026).
- **Anexo II** — Eventos de NFS-e (cancelamento, substituição etc. — modelo de eventos genérico).
- **Anexo A** — códigos IBGE de município e ISO2 de país.
- **Anexo B** — **NBS2** (lista nacional de serviços) — sucede a lista LC 116 no modelo nacional.
- **Anexo C** — **INDOP / indicadores IBS-CBS**.
- Comunicação histórica do padrão ABRASF usava **SOAP / XML Document-Literal** (relevante para municípios em adesão parcial). **[a confirmar versão ABRASF no município]**.

### 2.4 O que o sistema Tributos precisa implementar
1. Cliente da **API ADN** (autenticação por **certificado digital ICP-Brasil**, mTLS).
2. Emissão/recepção de **DPS → NFS-e** e **Eventos** (cancelamento, substituição, manifestação).
3. **Compartilhamento** das notas emitidas no sistema próprio para o ADN (modo adesão parcial).
4. Conciliação NFS-e ↔ ISS lançado/retido na conta corrente fiscal.
5. Suporte aos campos de IBS/CBS (transição 2026).

---

## 3. DÍVIDA ATIVA, CDA, PROTESTO e COBRANÇA

### 3.1 Inscrição em Dívida Ativa
- Dívida Ativa = créditos tributários e não-tributários não pagos no exercício/prazos regulamentares.
- **Requisitos legais da inscrição/CDA — CTN arts. 202 e 203** (obrigatórios):
  1. Nome do devedor e co-responsáveis + domicílio;
  2. Quantia devida e forma de cálculo de juros de mora;
  3. **Origem e natureza do crédito**, com a disposição legal que o fundamenta;
  4. Data da inscrição;
  5. Número do processo administrativo de origem.
- Omissão/erro em qualquer requisito → **nulidade** da inscrição e da cobrança (art. 203 CTN). CDA goza de presunção de **certeza e liquidez** (Lei 6.830/80, art. 3º).

### 3.2 CDA — Certidão de Dívida Ativa
- Título executivo extrajudicial que lastreia a **Execução Fiscal (Lei 6.830/80 — LEF)**.
- Sistema precisa gerar CDA numerada, com todos os requisitos, e permitir substituição/retificação até a sentença de embargos.

### 3.3 Protesto extrajudicial
- Autorizado pela **Lei 9.492/1997, art. 1º, parágrafo único** (incluído pela **Lei 12.767/2012**) — inclui CDA da União, Estados, DF, **Municípios** e autarquias/fundações.
- **STF (ADI 5.135)**: protesto de CDA é **constitucional**. **STJ**: protesto por Fazenda Municipal **não depende de lei local autorizadora**.
- Operacionalização via **CRA — Central de Remessa de Arquivos** (IEPTB / cartórios). Sistema gera **arquivo remessa** (layout CRA) e processa **arquivo retorno** (confirmação, pagamento, irregularidade).
- **[a confirmar]**: layout CRA vigente (CRA 21 / padrão IEPTB-BR) e convênio com o cartório/sindicato local do RS.

### 3.4 Cobrança e negativação
- Execução fiscal (judicial) + cobrança extrajudicial (cartas, protesto, inclusão em serviços de proteção ao crédito).
- Parcelamento de débitos inscritos; suspensão da exigibilidade; emissão de **CND/CPEN** (certidão negativa / positiva com efeito de negativa).

---

## 4. REFORMA TRIBUTÁRIA (EC 132/2023 + LC 214/2025) — IMPACTO NO MÓDULO

- **2026**: ano de **teste**. NFS-e/notas devem **destacar** alíquotas-teste: **CBS 0,9%** e **IBS 0,1%** (caráter meramente informativo, sem recolhimento, desde que cumpridas as obrigações acessórias).
- **2027**: cobrança efetiva da **CBS**; entra o **Imposto Seletivo (IS)**; extinção de PIS/Cofins.
- **2029–2032**: transição progressiva do **IBS** (substitui ICMS e **ISS**).
- **2033**: extinção total de ICMS e **ISS**; IBS/CBS plenos.
- **Consequência para Tributos municipais**: o **ISS será extinto e substituído pelo IBS** (partilhado União/Estados/Municípios, não-cumulativo, padronizado nacionalmente). O módulo deve estar preparado para coexistência ISS↔IBS (2026–2032) e migração. **IPTU e ITBI permanecem** municipais (não afetados pela reforma do consumo). **[a confirmar regras de partilha/comitê gestor do IBS]**.
- Base legal: **EC 132/2023**, **LC 214/2025** (regulamentação, ex-PLP 68/2024).

---

## 5. OBRIGAÇÕES ACESSÓRIAS / PRESTAÇÃO DE CONTAS (FOCO TCE-RS)

### 5.1 TCE-RS — SIAPC / PAD (a maior preocupação do dono)
- **SIAPC** — Sistema de Informações para Auditoria e Prestação de Contas; **PAD** — Programa Autenticador de Dados (gera os arquivos/relatórios autenticados).
- **Instrução Normativa nº 8/2025 (23/09/2025)** — revoga a IN 5/2024; novas regras para relatórios fiscais municipais gerados pelo PAD a partir do SIAPC.
- **Remessa mensal**: até **30 dias corridos** após o encerramento do período (regra vigente desde jan/2019). Entrega **somente eletrônica** (sem papel).
- Layouts oficiais: **"Resumo do Leiaute dos Dados à Disposição – SIAPC (MT Vol. V)"**, **Elenco de Contas Padrão (MT Vol. IV)**. O módulo Tributos alimenta a **Receita** (arrecadada/lançada/dívida ativa) que compõe os relatórios fiscais (LRF). **[a confirmar campos exatos de receita tributária no leiaute vigente]**.

### 5.2 Obrigações federais correlatas (contabilidade/pessoal — interface)
- **SICONFI** (STN) — DCA, RGF, RREO, MSC (Matriz de Saldos Contábeis). Receita tributária entra na MSC.
- **eSocial + EFD-Reinf → DCTFWeb** — tributos sobre folha/retenções (interface com RH, mas o ISS retido de prestadores pode tocar EFD-Reinf). **[a confirmar escopo do ISS na EFD-Reinf]**.
- **SIOPE / SIOPS** — aplicação em educação/saúde (recebem percentuais de receita de impostos — vínculo constitucional 25%/15%).

### 5.3 Prazos-chave (resumo)
| Obrigação | Periodicidade | Prazo |
|---|---|---|
| NFS-e Nacional (autorizar contribuintes/ADN) | Contínuo | Obrigatório desde 01/01/2026 |
| Remessa SIAPC/PAD (TCE-RS) | Mensal | Até 30 dias corridos após o mês |
| SICONFI MSC | Mensal | Conforme cronograma STN **[a confirmar]** |
| RREO / RGF (LRF) | Bimestral / Quadrimestral | Conforme LRF/STN |
| Destaque IBS/CBS na nota (teste) | Por documento | Durante 2026 |

---

## 6. INTEGRAÇÕES NECESSÁRIAS

- **ADN / Sistema Nacional NFS-e** (Receita Federal) — API DF-e, certificado ICP-Brasil.
- **Bancos arrecadadores** — geração DAM (código de barras FEBRABAN), **PIX QRCode**, retorno **CNAB 240/400**.
- **CRA / Cartórios de Protesto** (IEPTB-RS) — remessa/retorno de protesto de CDA.
- **TCE-RS** — geração de arquivos SIAPC/PAD.
- **SICONFI / SINTER / SERPRO** — MSC, CIB (Cadastro Imobiliário Brasileiro), validação CPF/CNPJ.
- **Cartórios de Registro de Imóveis** — ITBI / averbações.
- **Simples Nacional (PGDAS-D / DAF607)** — repasse de ISS do Simples.
- **Poder Judiciário (PJe / execução fiscal)** — petições e CDA. **[a confirmar via API/peticionamento]**.
- **Procuradoria municipal** — fluxo Dívida Ativa → CDA → protesto/execução.
- Internos (Constituição Tensorroot.Gov): **Financas** (conta corrente/contabilidade), **Protocolo** (impugnações/processos), **Transparencia** (portal), **Identidade** (RBAC/multi-tenant).

---

## 7. FONTES (URLs)

**NFS-e Nacional / ADN**
- Documentação técnica oficial (gov.br/nfse): https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual
- Manual dos Contribuintes — Emissor Público (out/2025): https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual/manual-contribuintes-emissor-publico-api-sistema-nacional-nfs-e-v1-2-out2025.pdf
- Manual Municípios APIs ADN (SN NFS-e): https://www.dinamicasistemas.com.br/upload/files/Manual%20Munic%C3%ADpios%20APIs%20ADN%20-%20Sistema%20Nacional%20NFS-e%20v1_2%20out21-025-1.pdf
- Como funciona o ADN (TecnoSpeed): https://blog.tecnospeed.com.br/ambiente-de-dados-nacional-da-nfs-e/
- NFS-e Nacional — prazos/impactos (TecnoSpeed): https://blog.tecnospeed.com.br/nfse-nacional-tudo/
- Migração das prefeituras p/ ambiente nacional (Bling): https://ajuda.bling.com.br/hc/pt-br/articles/36949961808663

**Dívida Ativa / CDA / Protesto / Execução Fiscal**
- Lei 9.492/1997 (Planalto): https://www.planalto.gov.br/ccivil_03/leis/l9492.htm
- Protesto de CDA da União (PGFN): https://www.gov.br/pgfn/pt-br/servicos/orientacoes-contribuintes/protesto-de-certidao-da-divida-ativa-da-uniao
- STJ — protesto municipal não exige lei local: https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/19082021-Protesto-de-divida-pela-Fazenda-Publica-municipal-nao-depende-de-lei-local-autorizadora--decide-Primeira-Turma.aspx
- Cartilha execuções fiscais municipais (TJSP): https://www.tjsp.jus.br/Download/GeraisIntranet/SPI/CartilhaExecucoesFiscaisLeitura.pdf
- Dívida Ativa — PBH: https://prefeitura.pbh.gov.br/fazenda/divida-ativa
- Serviços Dívida Ativa — PGM São Paulo: https://www.prefeitura.sp.gov.br/cidade/secretarias/procuradoria_geral/servicos/index.php?p=309683

**TCE-RS / SIAPC-PAD**
- Portal SIAPC TCE-RS: http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- Sistemas de controle externo (TCE-RS): https://tcers.tc.br/sistemas-de-controle-externo/
- IN 8/2025 (resumo INLEGIS): https://inlegis.com.br/alerta-tce-rs-publica-instrucao-normativa-no-8-2025-com-novas-regras-para-relatorios-fiscais-dos-municipios/
- Resumo Leiaute Dados SIAPC (MT Vol. V): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf
- Elenco de Contas Padrão (MT Vol. IV): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_IV_Elenco%20de%20Contas%20Padrao.pdf

**Reforma Tributária (IBS/CBS/IS)**
- Receita Federal — Entenda a Reforma do Consumo: https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/reforma-consumo/entenda
- Senado — testes 2026, transição até 2033: https://www12.senado.leg.br/noticias/materias/2024/12/16/novos-tributos-comecam-a-ser-testados-em-2026-e-transicao-vai-ate-2033
- Câmara — fase de transição/testes 2026: https://www.camara.leg.br/noticias/1237089-reforma-tributaria-comeca-fase-de-transicao-com-testes-de-novos-impostos-em-2026/
- Cronograma 2026–2033 (Jettax): https://www.jettax.com.br/blog/cronograma-e-fases-da-reforma-tributaria-de-2026-a-2033/

**Obrigações acessórias federais (interface)**
- Manual DCTFWeb (Receita Federal, jan/2025): https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/manuais/manual-dctfweb/manual-dctfweb-atualizacao-janeiro2025_versao_final.pdf
