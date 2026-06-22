# Como uma Prefeitura e uma Câmara Funcionam NA PRÁTICA

> Insumo de produto para o Tensorroot.Gov: descreve o organograma real, hierarquia, competências e **quem
> opera cada módulo do sistema**. Objetivo: alinhar RBAC (`Usuário → Departamento → Roles`, CLAUDE.md §6),
> nomes de telas/perfis e fluxos de aprovação ao mundo real de uma prefeitura/câmara brasileira de **pequeno
> porte** (perfil do piloto: **Maximiliano de Almeida/RS**, ~5 mil hab., jurisdição TCE-RS).
>
> **Lembrete de tenancy (CLAUDE.md §4):** **Prefeitura (Executivo) e Câmara (Legislativo) do mesmo município
> são tenants DISTINTOS (CNPJs distintos).** Este documento trata os dois lados, mas cada um é um tenant.
> Itens não confirmados em fonte oficial estão marcados **[a confirmar]** — a estrutura exata é definida por
> **lei municipal de organização administrativa** e varia por município.
>
> Data: 2026-06-22.

---

## 0. Princípio que organiza tudo: separação de Poderes + fiscalização externa

```
                          ┌─────────────────────────────────────────────┐
                          │  CIDADÃO / ELEITOR (titular do poder)         │
                          └───────────────┬───────────────┬──────────────┘
                                          │ elege          │ elege
                          ┌───────────────▼───────┐  ┌─────▼──────────────────┐
                          │ PODER EXECUTIVO        │  │ PODER LEGISLATIVO      │
                          │ = PREFEITURA           │  │ = CÂMARA DE VEREADORES │
                          │ (administra/executa)   │  │ (legisla + FISCALIZA)  │
                          │  → TENANT A (CNPJ A)   │  │  → TENANT B (CNPJ B)   │
                          └───────────┬────────────┘  └────────┬─────────────┘
                                      │ presta contas           │ presta contas
                                      ▼                          ▼
                          ┌──────────────────────────────────────────────────┐
                          │ CONTROLE EXTERNO: TRIBUNAL DE CONTAS (TCE-RS)      │
                          │ + UNIÃO/STN (SICONFI) + Ministério Público        │
                          └──────────────────────────────────────────────────┘
```

- O **Executivo** (Prefeitura) **arrecada, contrata, paga, presta serviços** (saúde, educação, obras…).
- O **Legislativo** (Câmara) **faz as leis, aprova o orçamento (LOA) e FISCALIZA a Prefeitura**; julga as contas
  do Prefeito (com parecer prévio do TCE).
- Os **dois** prestam contas ao **TCE-RS** (remessas SIAPC/PAD mensais) e à **União/STN** (SICONFI). Por isso
  **ambos os tenants** usam os módulos contábil/fiscal (Financas, Patrimonio, RH, Administracao, Transparencia).
- **Quem fiscaliza por dentro:** a **Controladoria / Controle Interno** (em cada Poder), que dialoga com o TCE.

---

## 1. PODER EXECUTIVO — A PREFEITURA

### 1.1 Organograma típico (pequeno/médio porte)

```
                              PREFEITO(A)  ── (Vice-Prefeito)
                                   │  chefe do Executivo, ordenador de despesa
        ┌──────────────────────────┼───────────────────────────────────────────┐
        │                          │                                            │
  ÓRGÃOS DE ASSESSORAMENTO    SECRETARIAS-FIM (finalísticas)        ÓRGÃOS DE CONTROLE/JURÍDICO
  DIRETO (staff do gabinete)  (entregam serviço ao cidadão)         (vinculados ao Prefeito)
        │                          │                                            │
  • Gabinete do Prefeito      • Sec. de Saúde                        • Procuradoria-Geral (jurídico)
  • Chefia de Gabinete        • Sec. de Educação                     • Controladoria / Controle Interno
  • Sec. de Governo           • Sec. de Assistência Social             (auditoria, conformidade, TCE)
  • Sec. de Planejamento      • Sec. de Obras / Infraestrutura
  • Assessoria de Comunicação • Sec. de Agricultura / Meio Ambiente
                              • (Sec. de Cultura/Esporte/Turismo)
        │
  SECRETARIAS-MEIO (atividade administrativa de suporte a todas)
  • Sec. de Administração (RH, compras/licitações, patrimônio, protocolo, TI)
  • Sec. da Fazenda / Finanças (orçamento, contabilidade, tesouraria, tributação)
```

> **Realidade do pequeno porte [a confirmar por lei local]:** em municípios de ~5 mil habitantes as pastas
> são **acumuladas** — frequentemente existe uma única **"Secretaria de Administração e Fazenda"** que
> concentra RH + compras + patrimônio + contabilidade + tributação, e a **Procuradoria** e a **Controladoria**
> podem ser cargos individuais (1 procurador, 1 controlador) e não estruturas com vários setores. O sistema
> deve suportar **um mesmo usuário com múltiplos papéis** e poucos operadores acumulando funções.

### 1.2 Quem é quem — competências e a quem responde

| Órgão / cargo | Faz o quê (competência prática) | Responde a |
|---|---|---|
| **Prefeito(a)** | Chefe do Executivo. **Ordenador de despesa** (autoriza empenho/pagamento), nomeia secretários, sanciona/veta leis, envia PPA/LDO/LOA à Câmara, assina contratos e prestação de contas anual. | Câmara (fiscalização), TCE, eleitor |
| **Vice-Prefeito** | Substitui o Prefeito; muitas vezes acumula uma secretaria. | Prefeito |
| **Chefe de Gabinete / Sec. de Governo** | Coordena a agenda, relações com a Câmara, articulação política, atos do Prefeito. | Prefeito |
| **Sec. de Planejamento** | Elabora **PPA/LDO/LOA** (planejamento orçamentário), monitora metas, projetos/captação. | Prefeito |
| **Sec. da Fazenda / Finanças** | **Núcleo financeiro-contábil.** Sob ela: Contabilidade, Tesouraria, Tributação, às vezes Compras. | Prefeito |
| ↳ **Contabilidade** | Faz **empenho→liquidação→pagamento** (escrituração), PCASP/partidas dobradas, balancetes, **DCASP**, restos a pagar; **gera as remessas TCE-RS/SICONFI**. Núcleo da prioridade nº1 do dono. | Sec. Fazenda |
| ↳ **Tesouraria** | Movimenta o caixa/bancos: paga ordens autorizadas, concilia extratos, fluxo de caixa, arrecadação. | Sec. Fazenda |
| ↳ **Tributação / Fiscal** | Cadastro imobiliário e mobiliário, lança IPTU/ISS/ITBI/taxas, **Dívida Ativa/CDA**, fiscalização, certidões. | Sec. Fazenda |
| **Sec. de Administração** | Atividade-meio: **RH/folha**, **compras/licitações**, **patrimônio/almoxarifado/frota**, **protocolo**, TI, almox. | Prefeito |
| **Sec. de Saúde** | Rede municipal (UBS, vigilância), Fundo Municipal de Saúde, repasses SUS; conselho de saúde. | Prefeito |
| **Sec. de Educação** | Escolas/creches municipais, merenda, transporte escolar, Censo INEP, FUNDEB; conselho FUNDEB/CAE. | Prefeito |
| **Sec. de Assistência Social** | CRAS/CREAS, CadÚnico, benefícios, Fundo Municipal de Assist. Social; conselho de assistência. | Prefeito |
| **Sec. de Obras / Infraestrutura** | Obras públicas, vias, iluminação, fiscalização de obras; mede contratos de obra. | Prefeito |
| **Sec. de Agricultura / Meio Ambiente** | Apoio rural, licenciamento ambiental municipal, estradas rurais. | Prefeito |
| **Procuradoria-Geral do Município** | **Jurídico:** defende o município em juízo, emite pareceres (licitações, contratos, dívida ativa), redige projetos de lei, ajuíza execução fiscal. **Assessora**, não decide gasto. | Prefeito (autonomia técnica) |
| **Controladoria / Controle Interno** | **Fiscaliza por dentro:** confere legalidade dos atos, conformidade dos empenhos/pagamentos, limites da LRF, monitora prazos de remessa, **emite o parecer/relatório de controle interno** que acompanha a prestação de contas ao TCE. Independente das secretarias que audita. | Prefeito (mas reporta tecnicamente ao TCE) |

**Cadeia de decisão de um gasto (fluxo que o sistema precisa refletir):**
Secretaria-fim **solicita** → **Compras/Licitação** (Administração) processa (Lei 14.133 + PNCP) →
**Contabilidade** faz o **empenho** (reserva o orçamento) → entrega/serviço → **Liquidação** (atesta) →
**Tesouraria** **paga** → **Controle Interno** confere → **Contabilidade** gera **remessa ao TCE**.
O **Prefeito é o ordenador** que autoriza nos pontos-chave.

---

## 2. PODER LEGISLATIVO — A CÂMARA DE VEREADORES

### 2.1 Organograma típico

```
                         PLENÁRIO (todos os Vereadores em exercício)
                         órgão máximo de deliberação: vota leis, orçamento, contas
                                          │
                         ┌────────────────┴─────────────────────────────┐
                         │ MESA DIRETORA (eleita pelos pares)            │
                         │ Presidente · Vice-Presidente · 1º e 2º Secret.│
                         │ dirige os trabalhos + administra a Casa       │
                         └────────────────┬─────────────────────────────┘
                                          │
        ┌─────────────────────────────────┼──────────────────────────────────┐
        │                                 │                                    │
  COMISSÕES PERMANENTES            COMISSÕES TEMPORÁRIAS              ADMINISTRAÇÃO DA CASA
  (técnicas, dão parecer)          (CPI, especiais)                  (estrutura-meio da Câmara)
  • Constituição e Justiça (CCJ)   • Comissão de Inquérito           • Diretoria-Geral / Secretaria
  • Finanças e Orçamento           • Comissões especiais             • Contabilidade da Câmara
  • Obras/Serviços, Educação,                                        • RH/Folha dos servidores
    Saúde etc. [varia por LOM]                                       • Compras/Licitação + Patrimônio
                                                                     • Protocolo + Controle Interno
                                                                     • Procuradoria/Assessoria Jurídica
```

### 2.2 Quem é quem — competências e a quem responde

| Órgão / cargo | Faz o quê | Responde a |
|---|---|---|
| **Vereador(a)** | Funções típicas: **legislar** (apresenta proposições) e **fiscalizar** o Executivo. Vota em plenário. | Eleitor; regimento |
| **Presidente da Câmara** | Dirige sessões e **administra a Câmara** (é o ordenador de despesa do Legislativo); representa a Casa; promulga. | Plenário |
| **Mesa Diretora** | Comanda os trabalhos legislativos + gestão administrativa/financeira da Câmara; nomeia comissões. | Plenário |
| **1º/2º Secretário** | Atas, anais, presença/quórum, correspondência, assina atos com o Presidente. | Mesa |
| **Comissões Permanentes** | Analisam proposições e dão **parecer** (CCJ = legalidade; Finanças = orçamento); fiscalizam atos do Executivo; podem convocar secretários. | Mesa/Plenário |
| **Diretoria-Geral / Secretaria da Câmara** | Estrutura-meio: conduz contabilidade, RH, compras, patrimônio, protocolo da própria Câmara. | Mesa/Presidente |
| **Controle Interno da Câmara** | Conformidade e prestação de contas da Câmara ao TCE (a Câmara também é fiscalizada, inclusive no limite de gastos do art. 29-A CF). | Mesa/TCE |

> **A Câmara tem dupla natureza no sistema:** (a) seu lado **legislativo** (módulo **Legislativo**: sessões,
> proposições, comissões, votação) e (b) seu lado **administrativo** — ela é um **órgão público com CNPJ
> próprio** que arrecada duodécimo, paga folha, compra, tem patrimônio e **presta contas ao TCE** como
> qualquer ente. Logo o tenant Câmara usa **Financas, RH, Administracao, Patrimonio, Protocolo, Transparencia**
> além do **Legislativo**. (Tributos/Saúde/Educação/Assistência são **só do Executivo**.)

---

## 3. QUEM OPERA CADA MÓDULO DO TENSORROOT.GOV

> Mapeamento para o RBAC (`Usuário → Departamento → Roles`). "Operador" = quem digita/processa;
> "Aprovador/Ordenador" = quem autoriza; "Consulta" = leitura. Em pequeno porte, **um usuário acumula vários
> papéis** [a confirmar por lei local].

| # | Módulo | Poder | Setor/órgão que OPERA | Perfil operador | Aprova / autoriza | Consulta/fiscaliza |
|---|---|---|---|---|---|---|
| 1 | **Administracao** (compras, licitações 14.133, contratos, PNCP) | Ambos | Setor de Compras/Licitações (Sec. Administração) | Pregoeiro/agente de contratação, comprador | Prefeito/Presidente (homologa); Procuradoria (parecer) | Controle Interno, TCE |
| 2 | **Financas** (orçamento PPA/LDO/LOA, empenho→liquidação→pagamento, **PCASP/contabilidade**, restos a pagar) | Ambos | **Contabilidade** (empenho/liquidação/escrituração) + **Tesouraria** (pagamento) + **Planejamento** (PPA/LDO/LOA) | Contador, técnico de contabilidade, tesoureiro | **Prefeito/Presidente = ordenador**; Sec. Fazenda | Controle Interno, **TCE**, Câmara (Comissão de Finanças) |
| 3 | **Tributos** (IPTU/ISS/ITBI/taxas, Dívida Ativa/CDA, NFS-e/ADN) | **Executivo** | Setor de **Tributação/Fiscal** (Sec. Fazenda); Procuradoria (execução fiscal) | Fiscal de tributos, cadastrista, atendente | Sec. Fazenda | Controle Interno, TCE |
| 4 | **RecursosHumanos** (folha, cargos, ponto MTP 671, eSocial) | Ambos | Setor de **RH/Pessoal** (Sec. Administração / Diretoria da Câmara) | Analista de RH/folha | Prefeito/Presidente (nomeações); Sec. Administração | Controle Interno, TCE (Res. 1099/2018) |
| 5 | **Patrimonio** (bens, depreciação MCASP, almoxarifado, frota) | Ambos | Setor de **Patrimônio/Almoxarifado** (Sec. Administração) | Almoxarife, responsável por patrimônio | Sec. Administração | Contabilidade (contrapartida), Controle Interno, TCE |
| 6 | **Protocolo** (processo eletrônico, assinatura 14.063, GED, LAI) | Ambos | **Protocolo central** (recepção) + todos os setores como tramitadores | Protocolista; servidores que tramitam | Autoridade do ato (assina conforme nível) | Cidadão (e-SIC), Controle Interno |
| 7 | **Saude** (UBS, PEP, regulação, farmácia, imunização, SISAB/RNDS) | **Executivo** | **Sec. de Saúde** / Fundo Municipal de Saúde; UBS; vigilância | Recepção UBS, enfermeiro, médico, farmacêutico, regulador | Secretário de Saúde | Conselho de Saúde, Controle Interno, TCE |
| 8 | **Educacao** (escolas, matrículas, diário, merenda PNAE, transporte PNATE, Censo INEP) | **Executivo** | **Sec. de Educação** + secretarias escolares | Secretário escolar, professor (diário), nutricionista (merenda) | Secretário de Educação | Conselho FUNDEB/CAE, Controle Interno, TCE |
| 9 | **AssistenciaSocial** (SUAS, CRAS/CREAS, CadÚnico, benefícios) | **Executivo** | **Sec. de Assistência Social**; equipes CRAS/CREAS; Fundo Mun. Assist. Social | Técnico SUAS (assist. social/psicólogo), cadastrador CadÚnico | Secretário de Assistência | Conselho de Assistência, Controle Interno, TCE |
| 10 | **Legislativo** (sessões, proposições, comissões, votação/painel) | **Legislativo** | **Câmara**: Secretaria legislativa + Mesa + comissões | Servidor legislativo (pauta/ata), vereador (proposição/voto) | Mesa Diretora / Plenário | Cidadão, Controle Interno da Câmara |
| 11 | **Transparencia** (dados abertos LAI, **remessas TCE-RS SIAPC/PAD**, SICONFI/MSC) | Ambos | **Contabilidade** (gera remessa) + **Controle Interno** (valida/assina) + Comunicação (portal) | Contador (gera/transmite), controlador (confere) | **Controle Interno + ordenador** | **TCE**, STN, cidadão |
| — | **Identidade / Admin** (usuários, papéis, tenants, auditoria) | Plataforma | **TI / Administrador do tenant** (Sec. Administração) | Administrador de sistema do município | — | Auditoria (CLAUDE.md §6), TCE |

### Implicações de RBAC sugeridas (departamentos → roles)

- **Departamentos** mapeiam secretarias/setores: `Gabinete`, `Fazenda.Contabilidade`, `Fazenda.Tesouraria`,
  `Fazenda.Tributacao`, `Administracao.Compras`, `Administracao.RH`, `Administracao.Patrimonio`, `Protocolo`,
  `Saude`, `Educacao`, `AssistenciaSocial`, `Controladoria`, `Procuradoria` / (Câmara) `Mesa`,
  `Camara.Legislativo`, `Camara.Administrativo`.
- **Segregação de funções (exigência de controle/TCE):** **quem empenha ≠ quem paga ≠ quem confere.** O perfil
  da **Tesouraria** não deve poder empenhar; o **Controle Interno** é **somente leitura + validação**, nunca
  operador da despesa que audita. Modelar como roles distintas no mesmo módulo Financas.
- **Ordenador de despesa** (Prefeito/Presidente) é uma role de **autorização** de alto nível, separada do
  operador. Em pequeno porte um secretário pode ser ordenador por delegação [a confirmar por lei/decreto local].
- **Acesso cross-módulo é por necessidade:** um técnico do CRAS vê só Assistência; o contador vê Financas +
  Patrimonio (contrapartida) + Transparencia; o controlador vê (leitura) quase tudo.

---

## 4. Incertezas a confirmar antes de modelar perfis

- **[a confirmar]** Estrutura administrativa exata e nomes das secretarias do **piloto (Maximiliano de
  Almeida/RS)** — depende da **lei municipal de organização administrativa**; o desenho acima é o típico de
  pequeno porte e deve ser **parametrizável por tenant**, não hard-coded.
- **[a confirmar]** Se a Procuradoria e a Controladoria são órgãos com setores ou cargos individuais no piloto.
- **[a confirmar]** Quais secretarias-fim existem de fato (ex.: pode não haver Meio Ambiente/Turismo separados).
- **[a confirmar]** Regras de **delegação de ordenação de despesa** a secretários (decreto municipal).
- **[a confirmar]** Composição das **comissões permanentes** e quóruns na Câmara (definidos no **Regimento
  Interno** + **Lei Orgânica Municipal**) — já tratados como parametrizáveis no módulo Legislativo.
- **[a confirmar]** Em pequeno porte, grau de acúmulo de papéis por usuário (impacta diretamente o RBAC).

---

## Fontes

- [Funções dos Vereadores e competências da Mesa Diretora — Câmara de Nazareno/MG](https://camaranazareno.mg.gov.br/funcoes-dos-vereadores/)
- [Competências e Atribuições — Câmara de Marechal Cândido Rondon/PR](https://www.marechalcandidorondon.pr.leg.br/institucional/funcao-e-definicao)
- [Atribuições das Comissões — Câmara de Linhares/ES](https://www.linhares.es.leg.br/vereadores/comissoes/atribuicoes-das-comissoes)
- [Mesa Diretora — Câmara Municipal de Salvador/BA](https://www.cms.ba.gov.br/mesa-diretora)
- [Secretaria Municipal da Fazenda — Prefeitura de Pinheiro Machado/RS](https://www.pinheiromachado.rs.gov.br/portal-transparencia/estrutura-administrativa/fazenda/)
- [Tesouraria — Prefeitura de Caldas Novas/GO](https://www.caldasnovas.go.gov.br/estrutura/secretaria-municipal-de-fazenda-e-gestao-publica/tesouraria/)
- [Estrutura administrativa / Organograma — Prefeitura de Ariranha do Ivaí/PR](https://www.ariranhadoivai.pr.gov.br/documentos/Estrutura%20Administrativa%20-%20Organograma%20Prefeitura%20Ariranha%20do%20Iva%C3%ADPR.pdf)
- [O papel do Controle Interno municipal — Prefeitura de Ritápolis/MG](https://www.ritapolis.mg.gov.br/pagina/9187/Controladoria%20Geral)
- [A organização do sistema de controle interno municipal — CRCRS](https://www.crcrs.org.br/arquivos/livros/livro_cont_int_mun.PDF)
- [Controladoria Geral do Município (competências) — Prefeitura de São Paulo](https://prefeitura.sp.gov.br/web/controladoria_geral/legislacao)
- Documentos internos: `docs/diagnostico/REQUISITOS-GOVTECH.md`, `docs/diagnostico/ESTADO-ATUAL.md`, `CLAUDE.md` §4–§6.
