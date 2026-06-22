# M7 — Pesquisa: Educação Municipal (Educacenso/INEP · SIOPE · FNDE/PNAE/PNATE · FUNDEB · BNCC)

> Preparatório (m7-prep). Domínio **Educacao** (módulo já scaffolded). Educação municipal
> alimenta **repasses federais** (fundo a fundo) e tem **mínimo constitucional de 25% MDE**
> (CF art. 212). O Censo Escolar (Educacenso) é o **dado-base** que determina o volume de
> quase todos os repasses (FUNDEB, PNAE, PNATE, PDDE).
>
> **Regra inegociável (CLAUDE.md §16):** nada de inventar leiaute/percentual/per capita —
> tudo com FONTE ou marcado **[a confirmar — obter doc oficial]**. O que é lei/parâmetro
> (per capita FNDE, ponderações FUNDEB, calendário, matriz curricular) é **versionado por
> exercício/vigência e parametrizável por tenant** — zero números hardcoded.
>
> Legenda de confiança: **ALTA** = base legal/fluxo confirmado nas fontes oficiais;
> **MÉDIA** = padrão correto, detalhe numérico/leiaute depende de doc oficial;
> **BAIXA** = bloqueado por leiaute/contrato não obtido (não implementar fiel sem o doc).

---

## 0. Mapa do domínio — o Censo é a raiz de tudo

O número de matrículas/profissionais/turmas declarado no **Educacenso/INEP** vira a base de
cálculo de **FUNDEB, PNAE, PNATE e PDDE** (todos usam o Censo do ano anterior). Logo, no
nosso modelo, **o agregado de matrícula/turma/escola deve ser a fonte canônica** da qual se
derivam: o arquivo de migração do Educacenso, os quantitativos do FUNDEB e os per capita do
PNAE/PNATE. Errar o Censo propaga erro em todos os repasses. **CONFIANÇA: ALTA.**

Fluxo financeiro paralelo: **SIOPE** (despesa/receita de educação, bimestral) comprova o
**mínimo 25% MDE** e é pré-requisito para transferências voluntárias — espelha o que já
fazemos com SICONFI/contabilidade no M3/M4.

---

## 1. Educacenso / Censo Escolar (INEP) — coleta em 2 etapas

**O que é:** levantamento anual, declaratório e **obrigatório** para todas as escolas
públicas e privadas (Decreto 6.425/2008; conduzido pelo INEP). Data de referência = **última
quarta-feira de maio**. **CONFIANÇA: ALTA.**

### 1.1 Primeira etapa — Matrícula Inicial
- Coleta: estabelecimentos, **turmas, alunos, gestores e profissionais escolares em sala de
  aula**. Abertura na data de referência (última quarta de maio); declaração no Sistema
  Educacenso **até 31 de julho** (referência ciclo 2025). **CONFIANÇA: ALTA** (data exata é
  fixada por portaria/cronograma anual → **parametrizar `CalendarioEducacenso` por exercício**).

### 1.2 Segunda etapa — Situação do Aluno
- Coleta ao **fim do ano letivo**: **rendimento** (aprovados/reprovados) e **movimento**
  (transferidos, abandono, falecidos). Declaração **até 30 de março** (do ano seguinte;
  ciclo 2025 reprogramado, 2ª etapa iniciada em fev/2026). **CONFIANÇA: ALTA** (datas anuais
  → parametrizar).

### 1.3 Integração técnica — migração por arquivo TXT (pipe-delimited)
- Escolas/redes com **sistema próprio** (o nosso caso) podem **migrar dados** ao Educacenso
  em vez de digitar. Há dois leiautes: (a) **identificação** (obter o código único INEP de
  alunos/profissionais já cadastrados) e (b) **importação** dos dados da etapa.
- Formato: **arquivo texto, campos delimitados por pipe `|`**, um arquivo pode conter mais de
  uma escola. Fluxo: *exportação* (baixar do Educacenso no leiaute) → atualizar códigos únicos
  INEP no sistema local → *importação* (enviar TXT ao Educacenso). **CONFIANÇA: MÉDIA**
  (fluxo e delimitador confirmados; **estrutura exata de cada registro/campo do leiaute é
  bloqueador** → ver pendências).
- **Implicação de arquitetura:** criar `EducacensoExportador` (ACL + Outbox + idempotência,
  filosofia do `NfseSincronizador`/sync existentes), gerando o TXT a partir do agregado
  canônico de matrícula. **NÃO há API/webservice de envio automático** comprovado — o envio é
  upload manual no portal Educacenso; modelar **geração de arquivo**, não integração síncrona.
  **CONFIANÇA: MÉDIA** (ausência de API a confirmar).

**Lei/parâmetro a versionar:** cronograma anual (datas das 2 etapas) e o **leiaute do ano**
(o INEP publica caderno de conceitos + leiaute por ciclo) → `CalendarioEducacenso` +
`LeiauteEducacenso` por exercício.

---

## 2. SIOPE (FNDE) — comprovação do mínimo 25% MDE

- **O que é:** sistema eletrônico do FNDE para coleta/disseminação dos **orçamentos de
  educação** (receitas e despesas) de União/Estados/DF/Municípios. **CONFIANÇA: ALTA.**
- **Mínimo constitucional:** **25% da receita de impostos + transferências** em Manutenção e
  Desenvolvimento do Ensino — **MDE** (CF art. 212). O SIOPE calcula o **indicador** e
  identifica entes abaixo do mínimo. **CONFIANÇA: ALTA.**
- **Periodicidade: BIMESTRAL.** Prazo de até **30 dias** após o encerramento de cada bimestre,
  alinhado ao **RREO** (CF art. 165 §3; LRF art. 52; LC 141/2012). Transmissão em **ordem
  cronológica** — não se envia um bimestre sem o anterior. **CONFIANÇA: ALTA.** (atenção:
  o mínimo de 25% é **aferido no encerramento do exercício**; o envio é bimestral.)
- **Consequência de descumprir:** restrição a transferências voluntárias e novos convênios
  federais. **CONFIANÇA: ALTA.**
- **Implicação de arquitetura:** o SIOPE é **alimentado pela contabilidade** (PCASP/execução,
  já no M2/M3), não pelo cadastro escolar. Modelar `IndicadorMde` + `DeclaracaoSiope` como
  **derivação da execução orçamentária da função 12 (Educação)**, com calendário bimestral
  — reutilizar o motor de prestação de contas/RREO do M4. **NÃO duplicar dados contábeis.**
  **CONFIANÇA: MÉDIA** (mapeamento exato de contas→campos SIOPE depende de doc/leiaute SIOPE).

---

## 3. FNDE — PNAE (merenda) e PNATE (transporte)

Ambos são **repasse fundo a fundo automático**, calculados sobre matrículas do **Censo do ano
anterior**, com **conselho de controle social** e **prestação de contas no SIGPC/Contas
Online**. **CONFIANÇA: ALTA** (estrutura); detalhes numéricos = parâmetro versionado.

### 3.1 PNAE — Programa Nacional de Alimentação Escolar (merenda)
- **Cálculo:** `valor = per capita × nº matrículas (Censo ano anterior) × dias de atendimento`.
  Per capita **varia por etapa/modalidade de ensino** (creche, pré-escola, fundamental, EJA,
  etc.). **CONFIANÇA: ALTA** (estrutura); **valores per capita = parâmetro** (Resolução
  CD/FNDE específica) → **[a confirmar — obter tabela vigente]**.
- **Parcelas:** em **até 8 parcelas anuais** entre fev e set (Resolução CD/FNDE 07/2024
  reduziu de 10 → 8 sem mudar o total). **CONFIANÇA: ALTA** (parametrizar nº/calendário).
- **Controle social:** **CAE** (Conselho de Alimentação Escolar) constituído e em dia é
  condição para repasse. **Mínimo 30%** dos recursos PNAE deve ser usado na **agricultura
  familiar** (Lei 11.947/2009 art. 14). **CONFIANÇA: ALTA** (percentual da AF a reconfirmar na
  redação vigente). Prestação de contas via **CAE → SIGPC**. **CONFIANÇA: MÉDIA.**

### 3.2 PNATE — Programa Nacional de Apoio ao Transporte do Escolar
- **Cálculo:** `valor = per capita × nº alunos da educação básica pública residentes em área
  rural que usam transporte escolar (Censo ano anterior)`. **CONFIANÇA: ALTA.**
- **Per capita não-uniforme:** definido pelo **FNRM — Fator de Necessidade de Recursos do
  Município** (combina % população rural [IBGE], área do município [IBGE], % população abaixo
  da linha de pobreza [IPEADATA] e **IDEB** [INEP]). **CONFIANÇA: ALTA** (composição); valor
  efetivo por município = **parâmetro** → **[a confirmar — obter tabela/Resolução vigente]**.
- **Base legal/parcelas:** Resolução CD/FNDE 05/2020 (critérios); repasse em **2 parcelas**
  (mar e ago) conforme Resolução CD/FNDE 05/2024. **CONFIANÇA: ALTA** (parametrizar).

> **Nota de contexto (2025/2026):** em 2025 o FNDE **antecipou e concluiu** os repasses do
> PNAE (sem novos repasses no ano) e publicou **novas regras de reprogramação de saldos** (a
> partir de 2027: saldo do último dia útil reprogramável até o 10º dia útil de fevereiro;
> créditos novos só em contas zeradas). Relevante para o módulo financeiro/saldo, **a
> confirmar na Resolução específica**. **CONFIANÇA: MÉDIA.**

---

## 4. FUNDEB (novo) — Lei 14.113/2020, EC 108/2020

- **O que é:** fundo contábil **estadual** que redistribui recursos da educação básica por
  **número de matrículas** (Censo), com **complementação da União**. **CONFIANÇA: ALTA.**
- **Distribuição por 3 modalidades de complementação da União:**
  - **VAAF** — Valor Anual por Aluno (Fundos): complementa fundos abaixo do mínimo nacional.
  - **VAAT** — Valor Anual Total por Aluno: considera receita total disponível por aluno.
  - **VAAR** — complementação por **resultado/condicionalidades** (melhoria de indicadores).
  - **CONFIANÇA: ALTA** (existência/papel); valores **VAAF-MIN/VAAT-MIN** = parâmetro anual
    (Portaria Interministerial MEC/MF) → **[a confirmar — obter portaria do exercício]**.
- **Ponderações por matrícula:** a distribuição usa o Censo com **ponderações por etapa,
  modalidade, duração da jornada e tipo de estabelecimento** (fatores definidos anualmente
  pela CIF — Comissão Intergovernamental). **CONFIANÇA: ALTA** (mecanismo); **tabela de
  fatores = parâmetro anual** → **[a confirmar — obter resolução CIF/fatores de ponderação]**.
- **Vínculos de aplicação (críticos p/ TCE-RS):**
  - **≥ 70%** dos recursos do Fundeb na **remuneração de profissionais da educação básica**
    (EC 108 ampliou de "magistério" p/ "profissionais da educação"; rol amplo, incluindo apoio
    técnico/administrativo/operacional). **CONFIANÇA: ALTA** (70%); rol exato de profissionais
    pagáveis = **a confirmar** (há divergência interpretativa TCE/CNM).
  - **Saldo/exercício:** recursos do exercício devem ser aplicados no próprio exercício;
    **até 10%** podem ser usados no 1º trimestre do ano seguinte (regra de saldo da Lei
    14.113/2020) → relevante para encerramento contábil. **CONFIANÇA: MÉDIA** (percentual/prazo
    exatos **[a confirmar — obter art. da Lei 14.113/2020 + Decreto 10.656/2021]**).
- **Controle social:** **CACS-Fundeb** (conselho de acompanhamento e controle social).
  **CONFIANÇA: ALTA.**
- **Implicação de arquitetura:** FUNDEB é primariamente **contábil/financeiro** (receita de
  transferência + vínculo de 70% na folha de educação) → integra com M2/M3 e com a **folha
  (M5)** para aferir o 70%. O **insumo de matrículas ponderadas** vem do agregado de Censo
  (§1). Modelar `DistribuicaoFundeb` (insumo Censo+ponderações, informativo/conciliação) e
  `IndicadorAplicacaoFundeb` (70% folha) — **não** recalculamos a cota (quem rateia é o ente
  estadual/FNDE), conciliamos e comprovamos aplicação. **CONFIANÇA: MÉDIA.**

---

## 5. BNCC / Matriz Curricular

- **BNCC** (MEC) = documento **normativo** das aprendizagens essenciais por etapa/modalidade;
  organiza componentes por **áreas do conhecimento** (Linguagens, Matemática, Ciências da
  Natureza, Ciências Humanas). **CONFIANÇA: ALTA.**
- **Carga horária mínima** (LDB Lei 9.394/96 art. 24): **800 horas / 200 dias letivos** anuais
  no ensino fundamental (anos iniciais: 800h em 40 semanas). **CONFIANÇA: ALTA.**
- **Matriz municipal:** a BNCC define **mínimos**; o **currículo/matriz curricular** é
  elaborado pela **rede/secretaria municipal** (autonomia para realidade local) — logo, no
  modelo, **a matriz é parâmetro por tenant** (não há leiaute nacional). **CONFIANÇA: ALTA.**
- **Implicação de arquitetura:** modelar `MatrizCurricular` (por etapa/série/ano, componentes
  e carga horária) como **agregado parametrizável por tenant+vigência**, validando os mínimos
  da LDB (800h/200 dias) como invariante. Não é repasse — é **gestão pedagógica/escolar** que
  alimenta turmas → Censo. **CONFIANÇA: MÉDIA** (escopo exato no M7 a definir no DESIGN).

---

## 6. Pendências consolidadas — [a confirmar — obter doc oficial]

1. **Leiaute Educacenso** (caderno de conceitos + estrutura de registros/campos do TXT pipe)
   do ciclo vigente — INEP. **(bloqueador do exportador fiel; o gerador compila sem ele.)**
2. **Existência/ausência de API de envio** ao Educacenso (vs. upload manual) — INEP.
3. **Tabelas per capita PNAE** por etapa/modalidade (Resolução CD/FNDE vigente) + **% mínimo
   agricultura familiar** na redação atual.
4. **Per capita/FNRM PNATE** vigente (Resolução CD/FNDE 05/2020 e atualizações) + calendário
   de parcelas do exercício.
5. **Fatores de ponderação FUNDEB** (resolução CIF) + **VAAF-MIN/VAAT-MIN** (Portaria
   Interministerial MEC/MF do exercício).
6. **Regra de saldo/aplicação FUNDEB** (% e prazo do 1º trimestre seguinte) — Lei 14.113/2020
   + Decreto 10.656/2021; **rol de profissionais** pagáveis no 70% (alinhar c/ TCE-RS).
7. **Leiaute/mapeamento de contas SIOPE** (contábil → campos SIOPE) — FNDE.
8. **Regras de reprogramação de saldos PNAE/PNATE/PDDE** (vigência a partir de 2027) — FNDE.
9. **Calendário anual** (Educacenso, SIOPE bimestral, parcelas PNAE/PNATE) → popular
   `CalendarioEducacao` por exercício/tenant.

---

## Fontes (oficiais e de apoio)

**INEP / Educacenso**
- [Censo Escolar — INEP](https://www.gov.br/inep/pt-br/areas-de-atuacao/pesquisas-estatisticas-e-indicadores/censo-escolar)
- [Começou a coleta da 2ª etapa do Censo Escolar 2025 — INEP](https://www.gov.br/inep/pt-br/centrais-de-conteudo/noticias/censo-escolar/comecou-a-coleta-da-2a-etapa-do-censo-escolar-2025)
- [Publicado cronograma do Censo Escolar 2025 — INEP](https://www.gov.br/inep/pt-br/centrais-de-conteudo/noticias/censo-escolar/publicado-cronograma-do-censo-escolar-2025)
- [Caderno de Conceitos e Orientações do Censo Escolar 2025 — 1ª etapa (PDF)](https://download.inep.gov.br/publicacoes/institucionais/estatisticas_e_indicadores/cadernos_de_conceitos_2025.pdf)
- [Etapas e Instruções Gerais para a Migração — 2ª etapa 2025 (PDF)](https://download.inep.gov.br/educacao_basica/educacenso/migracao/2025/etapas_e_instrucoes_gerais_para_a_migracao_sistema_segunda_etapa_2025_aluno_18_02_26.pdf)
- [Sistema Educacenso](https://educacenso.inep.gov.br/)

**FNDE / SIOPE**
- [SIOPE — FNDE (gov.br)](https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope)
- [SIOPE — o que é](https://www.fnde.gov.br/siope/o_que_e.jsp)
- [Notas Técnicas SIOPE — FNDE](https://www.gov.br/fnde/pt-br/assuntos/sistemas/siope/notas-tecnicas)
- [Prazo declaração receitas/despesas 2024 — FNDE](https://www.gov.br/fnde/pt-br/assuntos/noticias/prazo-para-declaracao-de-receitas-e-despesas-de-2024-termina-em-30-01)

**FNDE / PNAE / PNATE**
- [Recursos Financeiros do PNAE — FNDE](https://www.gov.br/fnde/pt-br/acesso-a-informacao/acoes-e-programas/programas/pnae/recursos-financeiros-do-pnae)
- [Política Nacional de Transporte Escolar (PNATE) — FNDE](https://www.gov.br/fnde/pt-br/acesso-a-informacao/transparencia-e-prestacao-de-contas/relatorio-de-gestao-1/relatorio-de-gestao-2021/resultados-da-gestao-1/programas-para-a-educacao-basica-1/politica-nacional-de-transporte-escolar)
- [PNATE — perguntas frequentes — FNDE](https://www.fnde.gov.br/index.php/programas/pnate/perguntas-frequentes-pnate)
- [1ª parcela PNATE 2026 — FNDE](https://www.gov.br/fnde/pt-br/assuntos/noticias/primeira-parcela-do-pnate-2026-beneficia-estados-e-mais-de-5-mil-municipios)
- [Reprogramação de saldos PNAE/PNATE — Conviva Educação](https://convivaeducacao.org.br/fique_atento/5880)

**FUNDEB**
- [Fundeb — FNDE](https://www.gov.br/fnde/pt-br/acesso-a-informacao/acoes-e-programas/financiamento/fundeb)
- [Ponderação das Matrículas — MEC](https://www.gov.br/mec/pt-br/financiamento-da-educacao-basica/ponderacao-das-matriculas)
- [Nota Técnica VAAF, VAAT e VAAR (CNM)](https://cnm.org.br/biblioteca/download/15227)
- [Portaria aumenta estimativa de complementação Fundeb 2025 — Undime](https://undime.org.br/noticia/09-05-2025-10-13-portaria-aumenta-estimativa-de-complementacao-para-o-fundeb)
- [Lei 14.113/2020 — Planalto](http://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14113.htm)
- [Decreto 10.656/2021 — Planalto](https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/decreto/d10656.htm)

**BNCC**
- [A Base — BNCC/MEC](https://basenacionalcomum.mec.gov.br/a-base)
- [BNCC — versão final (PDF)](https://basenacionalcomum.mec.gov.br/8_versaofinal_site.pdf)
