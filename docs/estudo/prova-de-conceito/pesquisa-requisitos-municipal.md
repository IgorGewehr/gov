# Pesquisa — Requisitos de Prova de Conceito (PoC) em Editais de Gestão Municipal (Executivo)

> **Objetivo:** mapear, a partir de **editais reais** e Termos de Referência (TR), os requisitos
> FUNCIONAIS e TÉCNICOS que sistemas de gestão pública municipal (Poder Executivo) tipicamente
> precisam **DEMONSTRAR** na prova de conceito / teste de conformidade / amostra (Lei 14.133/2021).
> **Regra deste documento:** toda afirmação tem FONTE (URL/edital) OU está marcada `[a confirmar]`.
> Data da pesquisa: 2026-06-22.

---

## 0. Fontes primárias analisadas (editais reais, lidos campo a campo)

| # | Município / UF | Documento | Regra-chave de PoC | URL |
|---|---|---|---|---|
| **E1** | **Carbonita/MG** | TR — Software de Gestão Pública (ERP completo, Prefeitura + Câmara) | **≥95% de cada módulo**; itens "Especificações Técnicas Obrigatórias" = **100%** sob pena de desclassificação; matriz Obrigatório/Desejável × Atende/Parcial/Não Atende (5/4/0 e 3/2/0); mínimo 375 pts no bloco obrigatório; itens faltantes (até 5%) em **120 dias** pós-contrato | https://sistema.carbonita.mg.gov.br/UpFiles/licitacoes/818/tr___software_gestao_publica.pdf |
| **E2** | **Riqueza/SC** | Pregão Eletrônico (Locação de sistema web integrado de gestão pública) | **Características Gerais Obrigatórias = 100%** (se falhar, nem avalia módulos); **módulos ≥90%**; apresentação "sem ajustes e sem contato externo", proibido desenvolver/editar durante a sessão; pode ser remota por videoconferência | https://cerronegro.sc.gov.br/uploads/sites/409/2024/06/Termo-de-Referencia-RETIFICADO.pdf *(mesmo TR-base SC; ver também)* https://s3cache.dom.sc.gov.br/atos/2024/11/1731530485_lic_965_contratao_de_empresa_para_gesto_de_software__prego_eletronico_servios_1_retificao.pdf |
| **E3** | **Órgão CNPJ 08.539.439/0001-07** (PNCP) | TR publicado no PNCP | Amostra do classificado em 1º; **prazo de entrega**; Termo de Aceite; vista da amostra pelos demais; não apresentar = desclassificação | https://pncp.gov.br/pncp-api/v1/orgaos/08539439000107/compras/2025/10/arquivos/1 |
| ref. | TCU | Orientação oficial sobre Amostra e Prova de Conceito | Base jurídica do uso de PoC em licitações | https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/ |
| ref. | STN | MCASP 10ª ed. (PCASP, DCASP, MSC) | Padrão contábil que os TRs exigem aderência | https://cnm.org.br/storage/noticias/2023/Links/MCASP%2010%C2%AA%20edic%CC%A7a%CC%83o%20(3).pdf |

> **Achado transversal:** os TRs de gestão municipal são **enormes** (E1 tem ~18 mil linhas de
> requisitos) e **listam funcionalidade por funcionalidade** marcando "Obrigatório/Desejável". A PoC
> reproduz essa lista item a item, em base que "simule condições reais de uso" (E2 §14.6.2).

---

## 1. REGRAS DA PROVA DE CONCEITO (como a banca avalia) — citado dos editais

- **Percentual de corte por módulo:** E1 exige **≥95% dos itens de cada módulo** (§3.10.11); E2 exige
  **≥90% dos itens dos módulos** (§14.6.8) **E 100% das "Características Gerais Obrigatórias"** (§14.6.4)
  — se reprovar nas gerais, **não passa** para a avaliação dos módulos (§14.6.5). FONTE: E1, E2.
- **Itens "Obrigatórios" = totalidade:** itens marcados obrigatórios devem ser atendidos integralmente,
  sob pena de desclassificação (E1 §3.10.12). FONTE: E1.
- **Sem desenvolver na hora:** "apresentá-los de forma objetiva, sem ajustes e sem contato externo.
  Não será permitido desenvolver, editar, corrigir ou ajustar os softwares durante a apresentação"
  (E2 §14.6.9). FONTE: E2.
- **Banca técnica dedicada (CTA / Comissão Especial):** estabelece prazo; quem não cumpre é
  "imediatamente desclassificada" (E1 §3.10.9). FONTE: E1.
- **"Equipamentos intocáveis":** interromper a amostragem mexendo no equipamento → encerramento e
  itens não apresentados contam como ausentes (E1 §3.10.16). FONTE: E1.
- **Diferimento de 5%:** itens não demonstrados (até 5%) podem ser entregues em **120 dias** após
  assinatura (E1 §3.10.13). FONTE: E1. — *relevante: dá fôlego para o que ainda é stub.*
- **Integrações com sistemas de terceiros podem ser dispensadas na PoC** porque dependem de
  customização na implantação (E2 §14.6.6). FONTE: E2. — *abranda o risco TCE/eSocial na PoC, mas
  vira obrigação contratual.*
- **Sessão pública, ata circunstanciada, fiscais dos concorrentes** (1 por licitante) (E1 §3.10.6/3.10.20).

---

## 2. CHECKLIST — REQUISITOS FUNCIONAIS POR MÓDULO (o que demonstrar)

### 2.1 Contábil / Orçamentário (PCASP, ciclo da despesa, DCASP) — núcleo da PoC

- [ ] **PPA / LDO / LOA** com histórico de alterações e legislação de autorização (E1 itens 141; cap.
  Contabilidade/Orçamento). Habilitação técnica E2 lista PPA, LDO, LOA, Contabilidade Pública como
  "módulos de maior relevância". FONTE: E1, E2.
- [ ] **Ciclo Lei 4.320:** Empenho → Liquidação → Pagamento; **anulação de empenho com motivo**
  (E1 item 144); bloqueio/desbloqueio de dotação (art. 9º LRF) (E1 item 143). FONTE: E1.
- [ ] **Aderência PCASP/MCASP/NBC TSP e SIAFIC** — integração entre módulos "nos moldes do SIAFIC,
  Sistema Único e Integrado, conforme MCASP da STN e NBC TSP vigentes" (E1 item 73, **Obrigatório**).
  FONTE: E1.
- [ ] **Balancete mensal e balanço anual** (anexos Lei 4.320, LRF e Resolução do TCE) (E1 item 146);
  razões analíticas dos sistemas Financeiro/Patrimonial/Compensação (item 147); **Livro Diário** com
  termo de abertura/encerramento (item 188). FONTE: E1.
- [ ] **RREO** (Port. STN) e **RGF** (E1 itens 190/191); Síntese da Execução Orçamentária (Res. 78/98
  Senado) (item 184); anexos da dívida fundada/flutuante (16/17) (itens 185–187). FONTE: E1.
- [ ] **Restos a Pagar** (processado/não processado) com extratos e demonstrativo mensal (E1 itens 157,
  170). FONTE: E1.
- [ ] **Demonstrativos de limites constitucionais** — gastos com Saúde, Educação, Pessoal (LRF arts.
  19/20; art. 29-A CF); **gráficos** do percentual aplicado (E1 item 51, **Obrigatório**). FONTE: E1.
- [ ] **SIOPS / SIOPE** no mesmo formato (E1 itens 158/159). FONTE: E1.
- [ ] **Adiantamentos, convênios, precatórios, PPP, empréstimos** com contabilização automática e
  prestação de contas conforme normativa do TCE (E1 itens 142, 171–183). FONTE: E1.

### 2.2 Tesouraria / Financeiro

- [ ] **Conciliação bancária automática** ("Fluxo Monetário") — listada como módulo de relevância (E2
  habilitação técnica). FONTE: E2.
- [ ] **Borderô eletrônico OBN / CNAB** para envio e recebimento de pagamentos via gerenciador
  financeiro, conforme convênio bancário (E1 item 68, **Obrigatório**). FONTE: E1.
- [ ] **Guias com QR Code de arrecadação integrado a PIX via API/webservice**, pagamentos/recebimentos
  instantâneos sem troca manual de arquivos (E1 item 70, **Obrigatório**); E2 cita "cobrança
  registrada/pix". FONTE: E1, E2.
- [ ] **Programação financeira / cotas de desembolso** (art. 9º LRF) e cotas de receita (E1 itens 166/167);
  controle automático de saldos com alerta de estouro (item 156). FONTE: E1.

### 2.3 Tributos / Arrecadação

- [ ] **IPTU** (emissão de carnê/IPTU listado em E2 habilitação técnica e no "WEB Cidadão" de E1).
  FONTE: E1, E2.
- [ ] **ISS / NFS-e (ABRASF 2.0) / Declaração de ISS Digital** — módulos de "Declaração Eletrônica de
  Serviços" e "NFS-e ABRASF 2.0" (E1 módulos; E2 habilitação técnica). FONTE: E1, E2.
  - ⚠️ Nosso CLAUDE.md define NFS-e como **ingestão passiva via ADN** (não emitimos/assinamos). Editais
    de outros municípios pedem **emissão ABRASF** — `[a confirmar]` se o edital-alvo (Maximiliano de
    Almeida/RS) exige emissão ou só consulta. Risco de aderência.
- [ ] **Dívida Ativa, CDA, cobrança, CND / validação de CND** (E1 "WEB Cidadão: IPTU, CND, Validação
  de CND"). FONTE: E1.
- [ ] **ITBI, Taxas, Alvarás** — `[a confirmar]` nos TRs específicos (presentes no nosso escopo; E1/E2
  citam "Tributação e Receitas" de forma agregada). FONTE parcial: E1/E2.

### 2.4 Folha / RH / eSocial

- [ ] **Folha mensal, férias, 13º, rescisão, extra-folha**; concurso público e atos legais; Portal do
  Servidor; **contracheque online** (E1 "MODULO FOLHA"; E2 habilitação técnica). FONTE: E1, E2.
- [ ] **eSocial integrado à folha** — módulo dedicado **inteiramente Obrigatório** em E1 (itens 548–569):
  ambiente produção e produção restrita; importar folha sem digitação; **visualizar/salvar XML** dos
  eventos; **certificado digital A1 e A3**; envio em lote independente de hierarquia; consultar retorno
  e erros; histórico; logs/auditoria por ação e período; faseamento do eSocial. FONTE: E1.
- [ ] **Qualificação cadastral** (geração/recebimento individual ou em lote, exigência da Receita) (E1
  item 63, **Obrigatório**); **EFD-REINF** (E1 item 75, **Obrigatório**). FONTE: E1.
- [ ] **Dispositivo visual de status do eSocial dentro da folha** (E1 item 62, **Obrigatório**). FONTE: E1.
- [ ] Controle de parcelas de plano de saúde por tabela/contrato com operadora (E1 item 69). FONTE: E1.
- [ ] **RPPS** — relatórios do regime próprio (Port. 916 MPS) (E1 item 162). FONTE: E1.
- [ ] **Ponto eletrônico** (Port. MTP 671/2021) — no nosso escopo; `[a confirmar]` se é item de PoC nos
  TRs-alvo (E1/E2 não detalham ponto na amostra lida).

### 2.5 Compras / Licitações / Contratos / PNCP

- [ ] **Licitações Lei 14.133/2021, Pregão Eletrônico, Contratos, Aditivos, Obras, Editais** (E1 módulo;
  E2 "Licitações e Contratos com pregão eletrônico e Integrado do PNCP"). FONTE: E1, E2.
- [ ] **Integração PNCP e PCA** (módulo dedicado em E1; obrigação legal art. 176 Lei 14.133 — E2 cita
  adoção pelo Município). FONTE: E1, E2.
- [ ] **Integração com ≥5 plataformas de pregão eletrônico** (E1 item 60, **Obrigatório**). FONTE: E1.
- [ ] **Importação de cotação/processo licitatório de exercício anterior** (E1 item 64, **Obrigatório**).
  FONTE: E1.
- [ ] **Saga orçamento↔contrato** (empenho amarrado ao contrato) — implícito no SIAFIC/integração. `[a confirmar]`

### 2.6 Patrimônio / Frota / Almoxarifado

- [ ] **Patrimônio** com tombamento e **depreciação MCASP**; **Almoxarifado**; **Frota** (módulos
  listados em E1 e E2). FONTE: E1, E2.
- [ ] **Cadastro único de credores/fornecedores** compartilhado, demonstrável entre Contabilidade,
  Pessoal, Compras/Licitação, Almoxarifado, Frota, Tributos e Patrimônio (E1 item 57, **Obrigatório**).
  FONTE: E1.
- [ ] **Leitor de código de barras** para inserir NF na liquidação (E1 item 66, **Obrigatório**). FONTE: E1.

### 2.7 Protocolo / Processo Eletrônico

- [ ] **Processo administrativo eletrônico / Processos Digitais com assinatura eletrônica nativa**
  (E2 habilitação técnica "Processos Digitais — Assinatura eletrônica Nativa"; E1 "Gestão de Processos
  e Documentos"). FONTE: E1, E2.
- [ ] **Workflow BPMN** (raias, eventos, atividades), versionamento e histórico de processos (E2,
  ferramenta de Workflow). FONTE: E2.
- [ ] **Protocolo do cidadão / Central de Atendimento** e **Ouvidoria (Lei 13.460/2017)** (E1 módulos).
  FONTE: E1.

### 2.8 Transparência / LAI

- [ ] **Portal da Transparência LAI** — publicação automática e diária (LC 101, LC 131, Lei 12.527,
  Dec. 7.724) (E1 item 1528, **Obrigatório**). FONTE: E1.
- [ ] **e-SIC** (Serviço de Informação ao Cidadão) (E1 item 1530, **Obrigatório**). FONTE: E1.
- [ ] **Consultas obrigatórias:** receita/tributos arrecadados, orçamentos, despesas, **pessoal**
  (histórico financeiro de servidores, filtros, exportação .json/.csv/.xml/.pdf) (E1 itens 1532–1543).
  FONTE: E1.

### 2.9 Portal do Cidadão (transversal — Executivo)

- [ ] **WEB Cidadão:** IPTU, CND, Contracheque, Protocolo, Situação do credor, Validação de CND;
  **Aplicativo de Mobilidade** (E1; E2 "Aplicativo de Mobilidade"). FONTE: E1, E2.

---

## 3. CHECKLIST — INTEGRAÇÕES GOVERNAMENTAIS (demonstrar ou comprovar)

- [ ] **TCE estadual (SICOM/MG em E1; "Informações Automatizadas TCE/SC" em E2)** — geração, validação
  e transferência de dados. **No nosso alvo: TCE-RS (SIAPC/PAD).** FONTE: E1, E2. — *no nosso sistema
  ainda é stub (ver ESTADO-ATUAL §Integrações).* `[a confirmar leiaute TCE-RS atual]`
- [ ] **SICONFI (STN)** — adaptado para geração e transmissão (E1 item 74, **Obrigatório**). FONTE: E1.
- [ ] **eSocial** — A1/A3, ambientes produção e restrita, lote, retorno/erros (E1 §2.4). FONTE: E1.
- [ ] **EFD-REINF** (E1 item 75, **Obrigatório**). FONTE: E1.
- [ ] **PNCP** (art. 176 Lei 14.133) (E1, E2). FONTE: E1, E2.
- [ ] **PIX / arrecadação registrada** via API bancária (E1 item 70). FONTE: E1.
- [ ] **OBN / CNAB** convênio bancário (E1 item 68). FONTE: E1.
- [ ] **SIOPS / SIOPE** (E1 itens 158/159). FONTE: E1.
- [ ] Federais diversos (Emprega Brasil, INPC, etc.) no portal (E1 cap. 8240+). FONTE: E1.
- [ ] **Certificado Digital para assinaturas** nos envios do eSocial e demais obrigações (E1 item 61,
  **Obrigatório**). FONTE: E1. — *nosso A1/Key Vault ainda não fiado em runtime.*

> **Mitigação útil (E2 §14.6.6):** integrações com sistemas de terceiros podem ser **dispensadas da
> avaliação na PoC** (dependem de customização na implantação). Isso reduz o risco de reprovar por
> TCE/eSocial **na demonstração**, mas torna-os **obrigação contratual** com prazo.

---

## 4. CHECKLIST — REQUISITOS TÉCNICOS (arquitetura, segurança, LGPD, infra)

### 4.1 Arquitetura / Web (frequentemente "Característica Geral Obrigatória" = 100%)

- [ ] **100% Web, multicamadas ("n camadas"), banco único** hospedado em nuvem/data center (E1 item 1,
  **Obrigatório**; E2 §1). FONTE: E1, E2.
- [ ] **Vedação explícita a desktop cliente-servidor emulado** em navegador/área de trabalho remota,
  por performance/banda/segurança (E2 §1.b). FONTE: E2. — *atenção: bancas técnicas verificam isso;
  nossa SPA React real é vantagem competitiva direta.*
- [ ] **Front-end leve** (tráfego mínimo, JSON), validações no cliente (CPF/CNPJ), navegadores padrão
  (Chrome/Firefox/Edge/Safari versões recentes), **sem plugins/applets NPAPI** (E2 §1). FONTE: E2.
- [ ] **Multiusuário sem limite de acessos simultâneos** (E1 item 42, **Obrigatório**). FONTE: E1.
- [ ] **Bloqueio mensal escalonado** entre módulos (só abre mês se Contabilidade abriu) (E1 item 59,
  **Obrigatório**). FONTE: E1. — *implicação de arquitetura: fechamento contábil governa os demais.*
- [ ] **Integração/cadastro único — informação alimentada uma única vez** (E1 item 72, **Obrigatório**).
  FONTE: E1.
- [ ] Interface padronizada, PT-BR, manual do usuário, teclas de atalho padrão em todos os módulos,
  gerador de relatórios (txt/xls/pdf/html) (E1 itens 39–55, 65). FONTE: E1.

### 4.2 Segurança da Informação

- [ ] **HTTPS obrigatório** com certificado SSL válido, redirecionamento automático de HTTP→HTTPS,
  validação periódica por terceiro (E1 itens 11–13, **Obrigatório**). FONTE: E1.
- [ ] **Firewall NGFW de borda redundante** (Gartner MQ), IPS/IDS, WAF nativo, SD-WAN, GeoIP, filtro de
  URL, inspeção SSL, SOC, logs (E1 itens 10, 14–36, **Obrigatório**). FONTE: E1. — *exigência de
  **infraestrutura de hospedagem**, não de software; atende-se via provedor de nuvem/data center.*
- [ ] **Proteção contra SQL Injection e DDoS na camada de aplicação**; enlace eBGP com ≥2 operadoras
  (alta disponibilidade) (E2 §2.5). FONTE: E2.
- [ ] **RBAC / permissões individualizadas por usuário e função**, senhas, total segurança contra acesso
  indevido (E1 item 71, **Obrigatório**). FONTE: E1.
- [ ] **Log de acesso/transações/erros** por módulo, operação (Inclusão/Alteração/Exclusão/Consulta),
  entrada e saída por usuário, **auditoria em tempo real** (E1 itens 48/49, **Obrigatório**). FONTE: E1.
  — *alinhado à nossa AuditTrail (que precisa de imutabilidade real + trilha de leitura).*

### 4.3 LGPD (Lei 13.709/2018)

- [ ] **Gestão de Termos e Condições de Uso** (interno e cidadão), por perfil e por serviço (E2 §LGPD.a).
  FONTE: E2.
- [ ] **Inventário/RoPA de tratamentos de dados pessoais** com hipótese legal cadastrada (E2 §LGPD.b/c).
  FONTE: E2.
- [ ] **Área do titular:** ver tratamentos, solicitar relatório de usos (transparência ativa/passiva),
  relatório de vínculos do cidadão (E2 §LGPD.d/e). FONTE: E2.
- [ ] **Consentimento** verificável quando não houver interesse público; **webservice** para terceiros
  consultarem consentimento (E2 §LGPD.f/j). FONTE: E2.
- [ ] **Controlador e Encarregado (DPO)** publicados no portal da transparência; **aceite de política/
  cookies no 1º acesso** registrado para auditoria (E2 §LGPD.g/h/i). FONTE: E2.
- [ ] Cláusulas contratuais LGPD, base legal arts. 7/11/14, plano de resposta a incidentes art. 48
  (E2 cap. 6 — Cumprimento da LGPD). FONTE: E2.

### 4.4 Backup / Continuidade / Banco de Dados

- [ ] **SGBD relacional com controle transacional**, backup e recovery garantindo integridade (E1 item 37,
  **Obrigatório**). FONTE: E1.
- [ ] **Backup diário** pela CONTRATADA, cópia disponível à CONTRATANTE sob solicitação; ferramenta de
  backup para o administrador (E1 itens 38/52, **Obrigatório**). FONTE: E1.
- [ ] **Recuperação de falha** preservando a última transação com êxito (E1 item 45, **Obrigatório**).
  FONTE: E1.
- [ ] Consistência de dados campo a campo (entrada e gravação), integridade referencial (E1 item 9,
  **Obrigatório**). FONTE: E1.
- [ ] **Redimensionamento elástico** de CPU/RAM/disco/link conforme demanda (E1 itens 3–8). FONTE: E1.

---

## 5. Leitura para o Tensorroot.Gov (gap rápido vs. ESTADO-ATUAL)

> Cruzamento informal — detalhamento fica para o estudo de aderência, não é fonte de edital.

- **Vantagem forte:** ser **SPA React + API .NET nativa web multicamadas** ataca diretamente a vedação a
  desktop emulado (E2 §1.b) — diferencial técnico explícito de edital. Multi-tenant/RBAC/auditoria já
  reais.
- **Risco alto na PoC (módulos ≥90–95%):** **Contabilidade PCASP/MCASP ausente** (ESTADO-ATUAL §2) é o
  módulo mais cobrado e mais incompleto; **bloqueio mensal escalonado** (E1 item 59) exige fechamento
  contábil governando os demais — ainda não existe.
- **Risco médio:** **eSocial** (módulo inteiro Obrigatório em E1) e **EFD-REINF** ausentes; **A1/Key Vault**
  não fiados (exigidos para eSocial/TCE/assinatura).
- **Alívio:** §3.10.13 (E1, 5%/120 dias) e §14.6.6 (E2, integrações de terceiros dispensáveis na PoC)
  dão margem para o que ainda é stub — desde que os **módulos centrais funcionais** estejam de pé.

---

## 6. Pendências `[a confirmar]` (próximos passos de pesquisa)

1. `[a confirmar]` **Edital-alvo concreto** para Maximiliano de Almeida/RS (ou município gêmeo no RS):
   buscar no PNCP e no portal do TCE-RS um TR de gestão municipal **com regras de PoC** específicas.
2. `[a confirmar]` **Leiaute oficial atual SIAPC/PAD do TCE-RS** e versão MSC/SICONFI vigente (2026).
3. `[a confirmar]` Se o TR-alvo exige **emissão NFS-e ABRASF** (conflita com nosso modelo de ingestão
   passiva ADN) ou apenas consulta/dívida ativa.
4. `[a confirmar]` Exigência de **ponto eletrônico (Port. MTP 671/2021)** como item de PoC.
5. `[a confirmar]` Percentual de corte e **matriz de pontuação** típicos no RS (E1=95%, E2=90%; varia).
6. `[a confirmar]` Saúde/Educação/Assistência: editais costumam licitar **em separado** (E1 lista Gestão
   Educacional e Saúde/e-SUS como blocos próprios) — verificar se o TR-alvo agrega ou separa esses módulos.

---

## Fontes

- [TR Software de Gestão Pública — Carbonita/MG (E1)](https://sistema.carbonita.mg.gov.br/UpFiles/licitacoes/818/tr___software_gestao_publica.pdf)
- [Pregão Locação de Sistema Web de Gestão — Riqueza/SC (E2)](https://s3cache.dom.sc.gov.br/atos/2024/11/1731530485_lic_965_contratao_de_empresa_para_gesto_de_software__prego_eletronico_servios_1_retificao.pdf)
- [TR de gestão municipal publicado no PNCP (E3)](https://pncp.gov.br/pncp-api/v1/orgaos/08539439000107/compras/2025/10/arquivos/1)
- [TCU — Amostra e Prova de Conceito (orientação)](https://licitacoesecontratos.tcu.gov.br/5-4-1-2-amostra-e-prova-de-conceito/)
- [STN — MCASP 10ª edição (PCASP/DCASP/MSC)](https://cnm.org.br/storage/noticias/2023/Links/MCASP%2010%C2%AA%20edic%CC%A7a%CC%83o%20(3).pdf)
- [TR Software Gestão Pública — Cerro Negro/SC (TR-base SC, mesmo padrão de E2)](https://cerronegro.sc.gov.br/uploads/sites/409/2024/06/Termo-de-Referencia-RETIFICADO.pdf)
