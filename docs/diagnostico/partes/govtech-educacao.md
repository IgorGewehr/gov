# GovTech — Educação Municipal (Bounded Context: Educacao)

> Pesquisa para Tensorroot.Gov — ERP GovTech multi-tenant. Piloto: Maximiliano de Almeida/RS.
> Foco do dono: contabilidade e prestação de contas ao TCE. Educação concentra repasses federais
> vinculados (FUNDEB, PNAE, PNATE) cujo descumprimento de obrigações **suspende repasses e
> bloqueia transferências voluntárias / operações de crédito** — risco fiscal direto.
> Última atualização: 2026-06-22.

---

## 1. Obrigações (visão geral)

| Obrigação | Órgão | Periodicidade | Natureza | Consequência do não cumprimento |
|---|---|---|---|---|
| **Educacenso / Censo Escolar** | INEP (MEC) | Anual (2 etapas) | Declaratória (matrícula, turmas, profissionais, infraestrutura, situação do aluno) | Afeta cálculo do FUNDEB e dos repasses PNAE/PNATE (baseados em nº de alunos); responsabilidade solidária dos gestores pela veracidade |
| **SIOPE** | FNDE | **Bimestral** (6 bimestres/ano) | Contábil/orçamentária (gastos em educação, MDE — Manutenção e Desenvolvimento do Ensino, art. 212 CF) | **Suspensão de transferências voluntárias da União**, impedimento de operações de crédito e de convênios federais |
| **Prestação de contas PNAE** | FNDE (SiGPC/Contas Online) | Anual | Contábil + parecer do CAE (Conselho de Alimentação Escolar) via Sigecon | Suspensão dos recursos do PNAE a partir do 1º dia do mês seguinte à inadimplência |
| **Prestação de contas PNATE** | FNDE (SiGPC) | Mensal no SiGPC, consolidada anual + parecer anual do CACS/FUNDEB | Contábil | Responsabilização civil/penal/administrativa do gestor e do presidente do CACS/FUNDEB; suspensão de repasses futuros |
| **BNCC** | MEC/CNE | Currículo permanente | Normativa pedagógica (alinhamento curricular) | Não é "envio", mas baliza Educacenso, materiais e avaliações (SAEB) |

> Observação: PNAE e PNATE são **transferências automáticas (obrigatórias)** calculadas com base
> no nº de alunos do Censo Escolar do ano anterior — o Educacenso é, portanto, a "fonte de verdade"
> de toda a cadeia de financiamento da educação. [a confirmar nas resoluções vigentes do FNDE]

---

## 2. Detalhamento por obrigação

### 2.1 Educacenso / Censo Escolar (INEP)
- **Obrigatório** para todas as escolas públicas e privadas. Coordenado pelo INEP; gestores das redes
  municipais/estaduais treinam agentes, acompanham a coleta e respondem **solidariamente** pela
  veracidade dos dados.
- **Duas etapas:**
  - **1ª etapa — Matrícula Inicial:** data de referência **27/05/2026**; coleta de **27/05 a 31/07/2026**.
    Declara matrículas, turmas, profissionais escolares e infraestrutura.
  - **2ª etapa — Situação do Aluno:** coleta de **01/02 a 12/03/2027** (aprovação, reprovação, abandono, transferência).
- **Marcos 2026/2027:** dados preliminares ao MEC em 27/08/2026 (DOU); reabertura ~30 dias para correção;
  dados finais ao FNDE em 11/12/2026; resultados finais 01/02/2027; indicadores da 2ª etapa em 14/05/2027.

### 2.2 SIOPE — Sistema de Informações sobre Orçamentos Públicos em Educação (FNDE)
- **Registro bimestral obrigatório** dos gastos em educação. Prazo de **até 30 dias** após o
  fechamento de cada bimestre (ex.: 6º bimestre encerra **30/01** do ano seguinte). Prazos **não flexíveis**.
- **Validação obrigatória** dos dados pelo **Secretário de Educação** e pelo **Presidente do CACS/FUNDEB**
  no módulo **SIOPE-MAVS** (Módulo de Adesão, Validação e Solicitação).
- Acompanha o cumprimento dos mínimos constitucionais de aplicação em MDE (art. 212 CF) e em FUNDEB.

### 2.3 PNAE — Programa Nacional de Alimentação Escolar (merenda)
- Prestação de contas no **SiGPC — Contas Online** (FNDE). Prazo histórico **15/02**, com
  **prorrogações frequentes** (ex.: até 30/04). [confirmar data exata do exercício no FNDE]
- **CAE (Conselho de Alimentação Escolar)** emite parecer (aprovar/rejeitar) no **Sigecon** — prazo
  típico ~45 dias após o prazo do gestor.
- Reajuste per capita 2026 noticiado em ~14,3% [a confirmar em resolução oficial FNDE].
- Exige aplicação mínima de **30% em agricultura familiar** (Lei 11.947/2009).

### 2.4 PNATE — Programa Nacional de Apoio ao Transporte Escolar
- Instituído pela **Lei nº 10.880/2004**. Foco: transporte de alunos da educação básica da **zona rural**.
- Prestação de contas **mensal no SiGPC**, consolidada anualmente com o FNDE.
- **Não tem conselho próprio**: o **CACS/FUNDEB** acompanha e emite o parecer anual.

### 2.5 BNCC — Base Nacional Comum Curricular
- Documento normativo (MEC/CNE). **Obrigatória desde 2020** para Educação Infantil e Ensino Fundamental
  em todas as redes públicas e privadas. Norteia currículos municipais, materiais e matrizes do SAEB.
- **BNCC Computação:** implementação obrigatória a partir de **2026** (pensamento computacional, cultura
  digital e programação, transversal e progressivo).

---

## 3. Formatos / Layouts

- **Educacenso:** plataforma web `educacenso.inep.gov.br`; coleta online com possibilidade de
  **migração/importação de dados** a partir de sistemas próprios das redes (layout de importação INEP).
  O ERP deve gerar o arquivo no layout do migrador do Educacenso. [confirmar layout/manual do migrador vigente]
- **SIOPE:** sistema web FNDE; entrada de dados orçamentário-contábeis bimestrais. Demanda
  conciliação com a contabilidade pública (PCASP/MSC) — ponto de integração crítico com o BC de Contabilidade/Finanças.
- **PNAE/PNATE:** **SiGPC — Contas Online** (prestação de contas) e **Sigecon** (parecer de conselhos).
- Não há, nas fontes consultadas, um layout de arquivo único padronizado entre os sistemas — cada um
  tem entrada própria (web) e/ou importação específica. Tratar como **integrações por sistema**.

---

## 4. Integrações (impacto no Tensorroot.Gov)

1. **Educacao ↔ Identidade/Cadastro:** base única de alunos, escolas, turmas, profissionais — é a fonte
   que alimenta o Educacenso.
2. **Educacao ↔ Contabilidade/Finanças:** os gastos exportados ao **SIOPE** precisam vir conciliados com
   o empenho/liquidação/pagamento (PCASP). Esta é a integração de maior risco fiscal e a que mais interessa
   ao dono (prestação de contas ao TCE + mínimos constitucionais de MDE/FUNDEB).
3. **Educacao ↔ Transparência/TCE:** os mesmos dados de aplicação em educação compõem os relatórios ao TCE-RS
   (ex.: SIAPC/PAD-RS) — verificar derivação dos dados de MDE/FUNDEB. [a confirmar no TCE-RS]
4. **Conselhos (CAE / CACS-FUNDEB):** fluxo de validação/parecer — o sistema deve registrar e dar suporte
   à validação eletrônica (SIOPE-MAVS, Sigecon).
5. **PNAE — agricultura familiar:** controle do mínimo de 30% (compras), integrável a Compras/Licitações.

---

## 5. Prazos (calendário-resumo)

| Item | Prazo / janela | Frequência |
|---|---|---|
| Educacenso 1ª etapa (matrícula inicial) 2026 | 27/05 a 31/07/2026 (ref. 27/05) | Anual |
| Educacenso dados finais ao FNDE | 11/12/2026 | Anual |
| Educacenso 2ª etapa (situação do aluno) | 01/02 a 12/03/2027 | Anual |
| SIOPE — cada bimestre | até 30 dias após o bimestre (6º bim. = 30/01) | Bimestral |
| PNAE — prestação de contas (SiGPC) | ~15/02, sujeito a prorrogação (ex. 30/04) | Anual |
| PNAE — parecer CAE (Sigecon) | ~45 dias após prazo do gestor | Anual |
| PNATE — prestação de contas | mensal no SiGPC + consolidação anual | Mensal/Anual |
| BNCC Computação — implementação | a partir de 2026 | Permanente |

---

## 6. Fontes (URLs)

- INEP — Censo Escolar: https://www.gov.br/inep/pt-br/areas-de-atuacao/pesquisas-estatisticas-e-indicadores/censo-escolar
- INEP — Cronograma Censo Escolar 2026: https://www.gov.br/inep/pt-br/centrais-de-conteudo/noticias/censo-escolar/inep-divulga-cronograma-do-censo-escolar-da-educacao-basica-2026
- INEP — Educacenso (plataforma): https://educacenso.inep.gov.br/
- FNDE — Relatório de situação de entrega (SIOPE): https://www.fnde.gov.br/siope/situacaoEntregaMunicipio.do
- CNM — Prazo SIOPE 6º bimestre (30/01): https://cnm.org.br/comunicacao/noticias/siope-prazo-para-envio-dos-dados-do-6-bimestre-encerra-dia-30-de-janeiro
- CNM — Suspensão de repasses por não envio do SIOPE: https://cnm.org.br/comunicacao/noticias/cnm-alerta-que-prazo-para-envio-dos-dados-no-siope-encerra-em-30-de-janeiro
- FNDE — Recursos financeiros do PNAE: https://www.gov.br/fnde/pt-br/acesso-a-informacao/acoes-e-programas/programas/pnae/recursos-financeiros-do-pnae
- FNDE — Prestação de contas (SiGPC): https://www.fnde.gov.br/prestacao-de-contas/prestacao-de-contas-espaco-sigpc/material-de-apoio
- CNM — Prazo PNAE prorrogado (30/04): https://cnm.org.br/comunicacao/noticias/prazo-para-gestores-municipais-prestarem-contas-da-merenda-escolar-foi-prorrogado-para-30-de-abril
- FNDE — PNATE (home): https://www.gov.br/fnde/pt-br/acesso-a-informacao/acoes-e-programas/programas/pnate
- FNDE — Cartilha prestação de contas PNATE: https://www.fnde.gov.br/phocadownload/programas/transporte_escolar/manuais_material_apoio/cartilhas2019/08%20-%20Prestao%20de%20Contas%20do%20Pnate%20e%20do%20Caminho%20da%20Escola.pdf
- UNDIME — Reprogramação de saldos PNAE/PNATE: https://undime.org.br/noticia/24-12-2025-06-54-reprogramacao-de-saldos-do-pnae-e-do-pnate-o-que-os-municipios-precisam-saber
- MEC/CNE — BNCC: https://www.gov.br/mec/pt-br/cne/base-nacional-comum-curricular-bncc
- Portal da Base — BNCC: https://basenacionalcomum.mec.gov.br/
- Fundação Lemann — BNCC Computação obrigatória 2026: https://fundacaolemann.org.br/noticias/bncc-computacao/
