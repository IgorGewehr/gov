# GovTech — RH / Folha do Setor Público + eSocial + Ponto + RPPS

> Diagnóstico de obrigações legais para o módulo **RecursosHumanos** do Tensorroot.Gov.
> Foco: Prefeitura/Câmara (entes públicos), servidores estatutários (RPPS) e celetistas (RGPS).
> Piloto: Maximiliano de Almeida/RS. Versão vigente do eSocial em pesquisa: **S-1.3**.
> Data da pesquisa: 2026-06. Marcações `[a confirmar em fonte oficial]` onde a web não confirmou diretamente.

---

## 1. Obrigações (o que é obrigatório)

### 1.1 eSocial — obrigatório para entes públicos
O eSocial é **obrigatório** para os órgãos públicos (União, Estados, DF e Municípios, incluindo Câmaras e autarquias) desde a inclusão dos entes públicos no cronograma de implantação. O ente público é o **declarante** e transmite:

- Eventos **de tabela** (S-1000 a S-1080): cadastro base, rubricas, lotações, estabelecimentos.
- Eventos **não periódicos** (S-2xxx): admissões, alterações, afastamentos, desligamentos, benefícios previdenciários (RPPS).
- Eventos **periódicos** (S-1200/S-1202/S-1207/S-1210/S-1280/S-1299): folha mensal.
- Eventos de **processos trabalhistas/administrativos** (S-2500/S-2501) e **SST** quando aplicável.

> O cadastro de estabelecimentos/unidades de órgãos públicos (S-1005) só é obrigatório quando houver trabalhadores/benefícios vinculados a eles.

### 1.2 Particularidade do ente público vs. empresa privada
- Servidor **estatutário / RPPS** → remuneração no evento **S-1202** (não S-1200).
- Servidor **celetista / cargo em comissão / temporário (RGPS)** → remuneração no **S-1200**.
- **Benefícios previdenciários do RPPS** (aposentadorias, pensões, complementações) → **S-1207** (periódico) + **S-2400/S-2405/S-2410/S-2416** (cadastro/alteração de benefício — não periódicos para entes públicos).

### 1.3 Controle de jornada / Ponto — Portaria MTP 671/2021
- Empregadores com **mais de 20 trabalhadores** são obrigados a manter controle de jornada (CLT art. 74, §2º) — aplicável aos **celetistas** do ente.
- Servidores **estatutários** seguem o regime de jornada do estatuto/lei municipal, mas o controle eletrônico de ponto é prática consolidada e recomendável para fins de auditoria/TCE. `[a confirmar em fonte oficial: a obrigatoriedade direta da Portaria 671 sobre estatutários depende de lei municipal]`
- Tipos de REP válidos: **REP-C** (convencional, certificação INMETRO obrigatória), **REP-A** (alternativo, exige acordo/convenção coletiva), **REP-P** (por programa/software, registro no INPI).
- Arquivos: **AFD** (Arquivo Fonte de Dados, assinado digitalmente) obrigatório para todos os tipos; **AEJ** (Arquivo Eletrônico de Jornada) passou a ser obrigatório; **AFDT/ACJEF eliminados**.
- Comprovante de marcação (impresso ou PDF assinado), armazenamento redundante e **imutabilidade** das marcações.

### 1.4 RPPS — previdência própria
- Município com RPPS instituído por lei deve gerar/transmitir os eventos de remuneração de segurados (S-1202) e benefícios (S-1207, S-2400+).
- Integração com obrigações previdenciárias correlatas (CADPREV/DRPPS-SPrev) — fora do escopo eSocial mas conexas. `[a confirmar em fonte oficial: layouts CADPREV vigentes]`

---

## 2. Formatos / Layouts (eventos eSocial principais)

| Evento | Descrição | Tipo | Aplicação no ente |
|--------|-----------|------|-------------------|
| **S-1000** | Informações do Empregador/Órgão Público | Tabela | 1º evento; pré-requisito de todos |
| **S-1005** | Tabela de Estabelecimentos/Unidades | Tabela | Quando houver vínculos/benefícios na unidade |
| **S-1010** | Rubricas (verbas de folha) | Tabela | Obrigatório |
| **S-1020** | Lotações tributárias | Tabela | Obrigatório |
| **S-1070** | Processos administrativos/judiciais | Tabela | Quando houver |
| **S-2200** | Admissão/Cadastro inicial do vínculo | Não periódico | Admissão de servidor/empregado |
| **S-2205/S-2206** | Alteração cadastral / contratual | Não periódico | Conforme ocorrência |
| **S-2230** | Afastamento temporário | Não periódico | Licenças, auxílios |
| **S-2299** | Desligamento | Não periódico | Exoneração/rescisão |
| **S-2400/S-2405/S-2410/S-2416** | Cadastro/alteração/início de **benefício** (entes públicos) | Não periódico | Aposentados e pensionistas RPPS |
| **S-1200** | Remuneração trabalhador **RGPS** | Periódico | Celetistas/comissionados/temporários |
| **S-1202** | Remuneração servidor **RPPS** | Periódico | Estatutários e militares |
| **S-1207** | **Benefícios** — Entes Públicos | Periódico | Aposentadorias/pensões RPPS |
| **S-1210** | Pagamentos de rendimentos do trabalho | Periódico | Todos os pagamentos |
| **S-1260/S-1270/S-1280** | Informações complementares aos periódicos | Periódico | Conforme aplicável |
| **S-1299** | **Fechamento** dos eventos periódicos | Periódico | Mensal (encerra a folha) |
| **S-2500/S-2501** | Processo trabalhista / tributos do processo | Processo | Quando houver decisão judicial |

- **Formato técnico:** XML assinado com certificado ICP-Brasil, validado contra **esquemas XSD** publicados por versão. Versão atual: **S-1.3** (eventos periódicos a partir do período de apuração 01/2025 — S-1200, S-1202, S-1207, S-1210, S-2500, S-2501 — na S-1.3).
- **Ordem de envio:** S-1000 primeiro → tabelas → não periódicos (S-2200 etc.) → periódicos → S-1299. S-1207 deve preceder S-1299.

---

## 3. Integrações

- **Transmissão eSocial:** WebService (envio de lote de eventos XML + consulta de retorno por protocolo). Endpoints separados para **Produção** e **Produção Restrita** (testes, sem efeito legal).
- **Certificado digital:** e-CNPJ (e-PJ) ou e-CPF (e-PF), tipo **A1** ou **A3**, padrão ICP-Brasil. A1 (arquivo) exige serviço de transmissão instalado; A3 (token/cartão). Usado para assinar cada evento e autenticar a conexão TLS. `[a confirmar em fonte oficial: novo padrão de segurança/certificado do eSocial previsto para 2026]`
- **Produção Restrita:** ambiente de teste com limite de trabalhadores por declarante, sem validação cruzada com sistemas externos e **sem efeito legal** — usar para homologar a integração antes de produção.
- **Ponto eletrônico (Portaria 671):** geração de **AFD** (assinado) e **AEJ**; integração coletor → ARP (armazenamento) → programa de tratamento → folha. REP-P exige registro INPI do software.
- **RPPS/Previdência:** dados de S-1202/S-1207 alimentam apuração previdenciária; correlação com CADPREV/DRPPS. `[a confirmar em fonte oficial]`
- **TCE (prestação de contas):** a folha e os recolhimentos previdenciários/encargos compõem a prestação de contas — a base de RH deve ser auditável e reconciliável com Finanças/Contabilidade (relevante para a preocupação do dono com TCE).

---

## 4. Prazos

| Obrigação | Prazo |
|-----------|-------|
| Eventos periódicos da folha (S-1200/S-1202/S-1207/S-1210/S-1260/S-1270/S-1280) | Até o **dia 15 do mês seguinte** ao período de apuração, ou antes do S-1299 |
| S-1299 (fechamento) | Mesmo prazo mensal; fecha a competência |
| Apurações anuais (13º salário, gratificação natalina) | Até **20 de dezembro** ou antes do S-1299 correspondente |
| S-2200 (admissão) | **Até o dia anterior** ao início da atividade do trabalhador `[a confirmar em fonte oficial]` |
| S-2299 (desligamento) | Até **10 dias** após o desligamento `[a confirmar em fonte oficial]` |
| S-2230 (afastamento) | Conforme tipo/duração do afastamento `[a confirmar em fonte oficial]` |
| AFD do ponto (Portaria 671) | Disponibilizar à fiscalização quando exigido; manter pelos prazos legais |

> Prazos podem ter postergações por Nota Técnica/Resolução do Comitê Gestor do eSocial — sempre confirmar no portal oficial por competência.

---

## 5. Fontes / URLs

- Portal eSocial — Documentação técnica (Leiautes/Manual S-1.3): https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-3-consolidada-ate-a-no-s-1-3-07-2026.pdf
- Leiautes eSocial S-1.2 (gov.br): https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-v-1-2-nt-02-2024/index.html
- Ambiente de Produção Restrita (eSocial): https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita
- FAQ Produção Empresas / Produção Restrita (eSocial): https://www.gov.br/esocial/pt-br/acesso-ao-sistema/cronograma-de-implantacao/perguntas-frequentes-producao-empresas-e-producao-restrita
- Manual WEB Geral eSocial (gov.br): https://www.gov.br/esocial/pt-br/empresas/manual-web-geral
- S-1000 (Empregador/Órgão Público) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/tabelas/s-1000.htm
- S-1005 (Estabelecimentos/Unidades de Órgãos Públicos) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/tabelas/s-1005.htm
- S-1200 (Remuneração RGPS) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/periodicos/s-1200.htm
- S-1202 (Remuneração servidor RPPS) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/periodicos/s-1202.htm
- S-1207 (Benefícios — Entes Públicos) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/periodicos/s-1207.htm
- S-1210 (Pagamentos) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/periodicos/s-1210.htm
- S-2410 (Cadastro de Benefício — Entes Públicos) — Senior: https://documentacao.senior.com.br/gestao-de-pessoas-hcm/esocial/leiautes/nao-periodicos/s-2410.htm
- S-2400 (Cadastro de Benefícios Previdenciários RPPS) — Contador Perito: https://www.contadorperito.com/materia/44990/esocial-evento-s-2400-cadastro-de-beneficios-previdenciarios-rpps
- DRPPS — envio de eventos eSocial pelos RPPS: https://pauseperin.adv.br/noticia/atualizacao-do-drpps-sobre-envio-de-eventos-do-esocial-pelos-rpps
- Reunião eSocial OPP — Aspectos Tecnológicos (SERPRO, gov.br/previdencia): https://www.gov.br/previdencia/pt-br/assuntos/rpps/esocial/arquivos/ReunioVirtualdoeSocialparaOPPAspectosTecnolgicosSerpro.pdf
- Portaria MTP 671/2021 — análise (TOPDATA): https://www.topdata.com.br/portaria-671-do-ministerio-do-trabalho/
- Portaria MTP 671/2021 — Perguntas e Respostas REP (gov.br): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
- Portaria 671/2021 — Espaço Legislação TOTVS: https://espacolegislacao.totvs.com/portaria-671/
- eSocial — atualização de certificado/padrão de segurança 2026: https://www.rsdata.com.br/atualizacao-certificado-esocial-2026-padrao-seguranca/
