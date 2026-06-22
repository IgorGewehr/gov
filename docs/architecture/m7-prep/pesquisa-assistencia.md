# M7 — Pesquisa: Assistência Social (SUAS)

> Preparatório (m7-prep). Tema: o que o município **envia ao MDS** no SUAS — CadÚnico,
> RMA, Censo SUAS, SUASWeb/AgilizaSUAS, Prontuário SUAS, benefícios (BPC/PBF/eventuais),
> IGD — periodicidade, formato, e o vínculo com o **cofinanciamento federal FNAS fundo a fundo**.
>
> **CLAUDE.md §16:** nada de inventar formato/leiaute/percentual — tudo com FONTE/URL ou
> marcado `[a confirmar — obter doc oficial]`. Lei municipal (ex.: valores de benefícios
> eventuais) é **parametrizável por tenant**. Os módulos `AssistenciaSocial` já existem
> scaffolded; este doc é base de design, não implementação.
>
> Legenda de confiança: **ALTA** = base legal/fluxo confirmado em fonte oficial;
> **MÉDIA** = padrão correto, detalhe numérico/leiaute depende de doc; **BAIXA** = bloqueado
> por leiaute/contrato não obtido.

---

## 0. Princípio transversal — CadÚnico é a base de tudo

No SUAS, **quase todo benefício e acompanhamento parte do Cadastro Único** (CadÚnico).
PBF, benefícios eventuais, tarifa social, BPC (em parte), priorização de serviços — todos
referenciam a família no CadÚnico. O município **não envia "o CadÚnico" como arquivo periódico**;
ele **opera o cadastro continuamente** no aplicativo do MDS/Dataprev (inclusão, atualização,
averiguação, revisão). O "envio" é a operação online + as **ações de qualificação cadastral**
com prazo. Modelar CadÚnico como **read/sync model + fila de pendências de atualização**, não
como sistema-fonte que substitui o do MDS. **CONFIANÇA: ALTA.**

---

## 1. CadÚnico (Cadastro Único)

- **Base legal:** Lei 8.742/1993 (LOAS), art. 6º-F; regulamentado pelo **Decreto nº 11.016/2022**.
  Operado pela **Caixa/Dataprev** (aplicativo `cadunico.dataprev.gov.br`); gestão federal MDS/SAGICAD.
- **Periodicidade de atualização da família:** a cada **2 anos** (contados da inclusão/última
  atualização/revalidação) — Decreto 11.016/2022. Famílias em programas podem ter exigência mais
  frequente. **CONFIANÇA: ALTA.**
- **Averiguação e Revisão Cadastral:** ação anual de qualificação. Em 2025 composta por
  **AVE25** (averiguação — indícios de inconsistência na composição/renda) e **REV25** (revisão —
  cadastros desatualizados). O **município é o responsável** por tratar as inconsistências via
  atualização cadastral, dentro do calendário federal. Repercussão (bloqueio/cancelamento de
  benefícios) segue cronograma do MDS. **CONFIANÇA: ALTA.**
- **Formato/leiaute:** o cadastro é preenchido no sistema do MDS; há **extrato/relatórios** e
  arquivos de retorno de averiguação/revisão para o município. **Leiaute exato dos arquivos de
  importação/retorno** `[a confirmar — obter doc oficial: layout V7/V8 do CadÚnico, manual SAGICAD]`.
  **CONFIANÇA: MÉDIA** (fluxo ALTA; leiaute BAIXA).
- **Design Tensorroot:** sync model `FamiliaCadUnico` (chave = código familiar/NIS responsável),
  `IMustHaveTenant`; agregado de **pendências** (averiguação/revisão a tratar) com SLA do calendário
  federal; ACL para o app Dataprev (não reimplementar cadastro). Auditoria imutável de toda alteração.
- **Docs a obter:** Decreto 11.016/2022 (texto Planalto); manual de instruções CadÚnico (SAGICAD);
  layout de arquivos AVE/REV.

**Fontes:** [Decreto 11.016/2022 — Planalto](https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2022/decreto/d11016.htm) ·
[CadÚnico — MDS](https://www.gov.br/mds/pt-br/acoes-e-programas/cadastro-unico) ·
[CadÚnico — Dataprev](https://cadunico.dataprev.gov.br/) ·
[Legislação CadÚnico — MDS](https://www.gov.br/mds/pt-br/acoes-e-programas/cadastro-unico/legislacao)

---

## 2. RMA — Registro Mensal de Atendimentos

- **O que é:** registro **mensal** do volume de atendimentos e serviços ofertados por
  **CRAS, CREAS e Centro POP**. Insumo central da **Vigilância Socioassistencial**.
- **Base legal:** **Resolução CIT nº 4/2011** (institui RMA para CRAS/CREAS) e
  **Resolução CIT nº 20/2013** (estende ao Centro POP). **CONFIANÇA: ALTA.**
- **Periodicidade/prazo:** mensal; dados do mês de referência devem ser inseridos no sistema
  online **em até 30 dias após o mês de referência**. **CONFIANÇA: ALTA.**
- **Conteúdo:** volume de famílias/indivíduos atendidos, novas inclusões em acompanhamento
  **PAIF** (CRAS) / **PAEFI** (CREAS), serviço especializado a pessoas em situação de rua
  (Centro POP), encaminhamentos, etc. **Conjunto exato de campos por questionário (CRAS/CREAS/POP)**
  `[a confirmar — obter doc oficial: Manual RMA CRAS/CREAS, MDS/SAGI]`. **CONFIANÇA: MÉDIA.**
- **Para onde envia:** sistema **RMA/SAGI** do MDS (`aplicacoes.mds.gov.br/sagi/atendimento`),
  autenticação **SAA** vinculada ao CPF do técnico. **Não há contrato de API pública confirmado** —
  hoje é preenchimento web. **CONFIANÇA: ALTA** (sistema); integração programática **BAIXA**.
- **Design Tensorroot:** o sistema deve **gerar o RMA a partir do Prontuário/atendimentos** (§5),
  evitando dupla digitação. Agregado `RegistroMensalAtendimento` por `(Unidade, Competencia)`, fechável
  e auditável; exportação no formato que o sistema MDS aceita (web ou eventual arquivo).
  Liga RMA ↔ cofinanciamento (PAIF é o serviço financiado pelo Piso Básico Fixo — §4).
- **Docs a obter:** Manual de Instruções RMA CRAS; Manual RMA CREAS; Resoluções CIT 4/2011 e 20/2013.

**Fontes:** [RMA — MDS](https://www.gov.br/mds/pt-br/acoes-e-programas/suas/gestao-do-suas/vigilancia-socioassistencial-1/registro-mensal-de-atendimentos-2013-rma) ·
[Sistema RMA/SAGI](https://aplicacoes.mds.gov.br/sagi/atendimento/auth/index.php?doc=1) ·
[Manual RMA CREAS 2018 (PDF)](https://aplicacoes.mds.gov.br/sagi/atendimento/doc/Manual_RMA_CREAS2018.pdf) ·
[Manual Instruções CRAS (PDF)](https://aplicacoes.mds.gov.br/sagi/atendimento/doc/Manual_de_Instrucoes_-_CRAS.pdf)

---

## 3. Censo SUAS

- **O que é:** coleta **anual** de dados sobre a rede socioassistencial — CRAS, CREAS, Centro POP,
  unidades de acolhimento, órgãos gestores (municipal/estadual), conselhos (CMAS/CEAS) e rede privada.
  Realizado desde 2007. Regulamentado por **Decreto nº 7.334/2010** `[a confirmar — citar decreto/portaria anual no doc oficial]`.
- **Periodicidade:** **anual**. Coleta tipicamente **set–nov**, com **dez** para verificação/retificação
  por estados e municípios (varia por ano — confirmar portaria do ano vigente). **CONFIANÇA: ALTA** (anual);
  janela exata **MÉDIA** (definida em portaria anual).
- **Conteúdo (por questionário):** identificação, estrutura física, serviços ofertados, gestão do
  território, articulação, recursos humanos — questionários distintos por tipo de unidade.
  **Campos exatos por ano** `[a confirmar — obter questionário Censo SUAS do ano vigente, MDS/SAGI]`.
- **Para onde envia:** sistema **Censo SUAS/SAGI** do MDS, preenchimento web. **CONFIANÇA: ALTA.**
- **Design Tensorroot:** muitos campos do Censo (estrutura física, RH, serviços) são **cadastrais
  estáveis** — o ERP pode **pré-popular** o questionário a partir do cadastro de unidades/lotação RH
  (integra com módulo de RH do M5) e do que o RMA já mede. Agregado `CensoSuas` por `(Unidade, AnoBase)`.
- **Docs a obter:** portaria anual do Censo SUAS; questionários CRAS/CREAS/POP/Acolhimento do ano vigente.

**Fontes:** [Metadados Censo SUAS — IBGE/CES](https://ces.ibge.gov.br/base-de-dados/metadados/mds/metadados-do-censo-suas.html) ·
[Portarias Censo SUAS — IBGE/CES](https://ces.ibge.gov.br/apresentacao/portarias?id=3581) ·
[Manual Censo SUAS 2022 CRAS (PDF)](https://aplicacoes.mds.gov.br/sagi/dicivip_datain/ckfinder/userfiles/files/Manual_Censo_SUAS_2022_CRAS.pdf)

---

## 4. Cofinanciamento FNAS fundo a fundo + SUASWeb → AgilizaSUAS (prestação de contas)

> **MUDANÇA CRÍTICA RECENTE (2024/2025):** a **Portaria MDS nº 1.043, de 24/12/2024** **revoga a
> Portaria 113/2015** e altera todo o fluxo de transferência, execução e prestação de contas fundo a fundo.

- **Blocos de financiamento** (recursos transferidos fundo a fundo FNAS → FMAS):
  **(1)** Proteção Social Básica; **(2)** Proteção Social Especial; **(3)** Gestão do SUAS;
  **(4)** Gestão do Bolsa Família e do Cadastro Único. **CONFIANÇA: ALTA.**
- **Pisos / serviços tipificados:** os serviços são tipificados pela **Resolução CNAS nº 109/2009**
  (Tipificação Nacional). Cofinanciamento da PSB = **Piso Básico Fixo** (PAIF/CRAS) + **Piso Básico
  Variável**; PSE de média complexidade = **Piso Fixo de Média Complexidade** (PAEFI, Abordagem Social,
  serviço a população de rua); alta complexidade = pisos de acolhimento. **Valores de referência por
  piso** `[a confirmar — obter doc oficial: tabela de pisos vigente / NOB-SUAS]`. **CONFIANÇA: ALTA**
  (estrutura); valores **pendência documental**.
- **SUASWeb (Rede SUAS):** sistema que registra as transferências fundo a fundo; módulos públicos
  (Relatório de Parcelas Pagas, Saldos em Conta) e restritos (Plano de Ação, Demonstrativo).
  → **Plano de Ação foi DESCONTINUADO** (obsoleto; permanece apenas o Plano de Assistência Social).
  **CONFIANÇA: ALTA.**
- **Prestação de contas — NOVO instrumento `AgilizaSUAS`:** o **Demonstrativo Sintético Anual da
  Execução Físico-Financeira** (antes no SUASWeb) **é substituído pelo AgilizaSUAS** (complementado pelo
  **BB Gestão Ágil** para a parte financeira). **CONFIANÇA: ALTA.**
- **Periodicidade da prestação de contas (regra nova):** a partir do **exercício 2025**, lançamento
  **ao longo do exercício, concomitante à execução**, com prazo final em **1º de março do exercício
  subsequente**. Transição p/ ano-base 2024: gestores até **30/09/2025**; Conselhos (parecer CMAS)
  até **31/12/2025**. **CONFIANÇA: ALTA.**
- **Design Tensorroot:** o módulo deve **acompanhar saldos em conta por bloco** (espelhar Parcelas
  Pagas/Saldos do SUASWeb via importação), classificar a **execução por bloco/serviço** ligada à
  contabilidade PCASP (M2/M3) e ao FMAS como fundo, e **produzir os dados do Demonstrativo/AgilizaSUAS**
  de forma contínua (não anual de última hora). Reprogramação de saldo facilitada pela 1.043/2024.
  **Contrato/API do AgilizaSUAS** `[a confirmar — obter Manual Operacional AgilizaSUAS; hoje provável preenchimento web]`.
- **Docs a obter:** **Portaria MDS 1.043/2024** (íntegra DOU) + cartilha anotada; Manual Operacional
  AgilizaSUAS 2024/2025; NOB-SUAS; tabela de pisos vigente; Resolução CNAS 109/2009.

**Fontes:** [Portaria 1.043/2024 — FNAS](https://fnas.mds.gov.br/portaria-mds-no-1-043-de-24-de-dezembro-de-2024-nova-regulacao-para-transferencias-fundo-a-fundo-no-suas/) ·
[Portaria 1.043/2024 íntegra DOU (PDF)](https://rondonia.ro.gov.br/wp-content/uploads/2025/03/PORTARIA-MDS-No-1.043-DE-24-DE-DEZEMBRO-2024-PORTARIA-MDS-No-1.043-DE-24-DE-DEZEMBRO-2024-DOU-Imprensa-Nacional.pdf) ·
[Manual AgilizaSUAS — FNAS](https://fnas.mds.gov.br/fnas-disponibiliza-manual-agilizasuas-e-reforca-nova-sistematica-de-prestacao-de-contas-no-suas/) ·
[Portaria 113/2015 (revogada, p/ histórico)](https://www.mds.gov.br/webarquivos/legislacao/assistencia_social/portarias/2015/portaria1132015-10122015-blocos.pdf) ·
[Manual Demonstrativo SUASWeb (PDF)](https://fnas.mds.gov.br/wp-content/uploads/2019/09/Manual-Demonstrativo-2018-Final-revisada.pdf) ·
[Tipificação Nacional CNAS 109/2009 (PDF)](https://www.mds.gov.br/webarquivos/publicacao/assistencia_social/Normativas/tipificacao.pdf)

---

## 5. Prontuário SUAS

- **O que é:** instrumento **nacional padronizado** para registro detalhado do acompanhamento
  familiar no **PAIF (CRAS)** e **PAEFI (CREAS)**. Versão física, eletrônica ou online.
  Preenchido por **profissionais de nível superior** da equipe de referência. **CONFIANÇA: ALTA.**
- **Regra-chave:** quando a família é acompanhada **simultaneamente por PAIF e PAEFI**, **cada unidade
  abre seu próprio prontuário**. Modelar `Prontuario` por `(Familia, Unidade/Servico)`, não único global.
  **CONFIANÇA: ALTA.**
- **Vínculo:** o Prontuário **alimenta o RMA** (§2) e a Vigilância Socioassistencial. É a fonte primária
  de atendimento → o ERP deve gerar RMA a partir dele. **CONFIANÇA: ALTA.**
- **Formato:** **não há envio periódico ao MDS** do prontuário em si (é registro local/instrumental).
  O MDS publica **modelo/manual de campos**; o eletrônico é local. Campos exatos
  `[a confirmar — obter Manual do Prontuário SUAS (versão eletrônica), MDS/SAGI]`. **CONFIANÇA: MÉDIA.**
- **Design Tensorroot:** **dado sensível LGPD** (saúde, vulnerabilidade, violência) → controle de
  acesso por papel/unidade, trilha de auditoria reforçada, sem exposição cruzada PAIF×PAEFI.
  Agregado `Prontuario` rico com `PlanoAcompanhamentoFamiliar`, evoluções datadas, vínculo ao CadÚnico.
- **Docs a obter:** Manual do Prontuário SUAS (eletrônico); orientações de preenchimento (MDS).

**Fontes:** [Manual Prontuário SUAS — versão preliminar (PDF)](https://aplicacoes.mds.gov.br/sagi/dicivip_datain/ckfinder/userfiles/files/Manual_Prontuario_SUAS_VERSAO_PRELIMINAR.pdf) ·
[Orientações preenchimento Prontuário — RS (PDF)](https://social.rs.gov.br/upload/arquivos/202205/03120530-orientacoes-para-o-preenchimento.pdf) ·
[Orientação técnica Prontuário SUAS — PR (PDF)](https://www.justica.pr.gov.br/sites/default/arquivos_restritos/files/documento/2020-09/orientacao_tecnica_03_prontuario_suas.pdf)

---

## 6. Benefícios — BPC, PBF (Bolsa Família) e Benefícios Eventuais

### 6.1 BPC — Benefício de Prestação Continuada
- **Base legal:** LOAS (Lei 8.742/1993). **Operacionalizado pelo INSS** (pagamento/concessão);
  **gestão pelo MDS/SNBA**. **O município NÃO paga o BPC.** Critério de renda referenciado ao CadÚnico.
- **Papel do município/CRAS:** **informar/orientar** sobre acesso e requerimento, e **acompanhar
  famílias** com beneficiários BPC (BPC na Escola, busca ativa). **CONFIANÇA: ALTA.**
- **Design Tensorroot:** read model de beneficiários BPC do território (para acompanhamento e busca
  ativa); **não** gerir concessão/pagamento. Integração informativa.

### 6.2 PBF — Programa Bolsa Família
- **Base legal:** **Lei nº 14.601/2023**, regulamentada pelo **Decreto nº 12.064/2024**;
  condicionalidades reguladas pela **Portaria MDS nº 1.058/2025**. **CONFIANÇA: ALTA.**
- **Papel do município:** **gestão de condicionalidades** (saúde + educação) articulada com as
  secretarias municipais; oferta de Trabalho Social com Famílias; gestão de perfis no **SICON/SIGPBF**.
  Pagamento é federal (Caixa). **CONFIANÇA: ALTA.**
- **Condicionalidades (referência — parametrizável):** frequência escolar mín. **60%** (4–6 anos) /
  **75%** (6–18 anos); calendário vacinal; acompanhamento nutricional de menores de 7 anos; pré-natal
  de gestantes. **Percentuais conforme Decreto 12.064/2024 / Portaria 1.058/2025** — confirmar antes de
  fixar. **CONFIANÇA: MÉDIA** (são lei federal, mas validar versão vigente; tratar como parâmetro).
- **Sistemas/envio:** condicionalidades de saúde (Sistema PBF na Saúde / e-SUS) e educação
  (Sistema Presença/MEC) → consolidadas pelo MDS no **SICON**. **CONFIANÇA: ALTA** (fluxo);
  **integração programática** do ERP a esses sistemas `[a confirmar — contratos/APIs SICON/SIGPBF/Presença]`.
- **Design Tensorroot:** acompanhamento de condicionalidades e busca ativa de descumprimento (read/sync
  do SICON); **não** reimplementar concessão. Liga ao IGD (§7) e ao bloco 4 de cofinanciamento (§4).

### 6.3 Benefícios Eventuais
- **Base legal:** LOAS art. 22 (auxílio natalidade, auxílio funeral e outros por vulnerabilidade
  temporária). **Definidos e custeados majoritariamente pelo MUNICÍPIO** (lei/decreto municipal +
  Conselho), em geral para renda per capita < **1/4 do salário mínimo** (parâmetro de referência LOAS).
  **CONFIANÇA: ALTA.**
- **Design Tensorroot — LEI MUNICIPAL (parametrizar por tenant):** tipos de benefício eventual, valores,
  critérios de renda, documentação e fluxo de concessão são **definidos por lei/decreto municipal** →
  **nada hardcoded** (§16). Agregado `BeneficioEventual` parametrizável por tenant+vigência; concessão
  com trilha auditável e vínculo ao CadÚnico/Prontuário. Valores de Maximiliano de Almeida/RS
  `[a confirmar — obter lei municipal de benefícios eventuais do tenant piloto]`.

**Fontes:** [BPC — MDS](https://www.gov.br/mds/pt-br/acoes-e-programas/suas/beneficios-assistenciais/beneficio-assistencial-ao-idoso-e-a-pessoa-com-deficiencia-bpc) ·
[LOAS — Lei 8.742/1993 (Planalto)](https://www.planalto.gov.br/ccivil_03/leis/l8742.htm) ·
[Portaria MDS 1.058/2025 — condicionalidades PBF](https://aplicacoes.mds.gov.br/snas/regulacao/visualizar.php?codigo=6892) ·
[Decreto 12.064/2024 — regulamenta PBF](https://aplicacoes.mds.gov.br/snas/regulacao/visualizar.php?codigo=6777) ·
[Guia integração SICON (PDF)](https://www.mds.gov.br/webarquivos/publicacao/bolsa_familia/Guias_Manuais/GuiaNav_App_integracaoSicon.pdf)

---

## 7. IGD — Índice de Gestão Descentralizada

- **O que é:** indicador **mensal** que mede a qualidade da gestão do **PBF + CadÚnico** (IGD-M/IGD-PBF)
  e do **SUAS** (IGD-SUAS) e **compõe a base de cálculo dos recursos transferidos** ao município
  (incentivo de gestão, bloco 3/4). **CONFIANÇA: ALTA.**
- **Fórmula IGD-M:** `IGD-M = FatorI × FatorII × FatorIII × FatorIV`, onde os fatores consideram
  **taxa de atualização cadastral** e **taxas de acompanhamento das condicionalidades** de saúde e
  educação (FatorI/II) e fatores de adesão/comprovação (FatorIII/IV). **CONFIANÇA: ALTA** (estrutura);
  **pesos/fórmula numérica exata** `[a confirmar — obter IN MDS sobre IGD-M (ex. IN 091 SAGICAD) e portaria vigente]`.
- **Requisitos para receber IGD-PBF:** adesão formal ao PBF e ao SUAS; estar **quite com o lançamento
  anual da comprovação de gastos no SUASWeb** (→ migrar p/ AgilizaSUAS, §4); **aprovação total da
  comprovação pelo CMAS**. **CONFIANÇA: ALTA.**
- **Vínculo direto:** IGD ↔ CadÚnico (atualização) ↔ PBF (condicionalidades) ↔ prestação de contas
  (SUASWeb/AgilizaSUAS) ↔ CMAS (aprovação). É o **indicador que amarra todos os módulos acima**.
- **Design Tensorroot:** painel/serviço que **estima o IGD** a partir dos dados internos (taxa de
  atualização cadastral, acompanhamento de condicionalidades, status de prestação de contas e parecer
  do CMAS) — ferramenta de **gestão preditiva de receita**, não cálculo oficial (o oficial é do MDS).
- **Docs a obter:** IN do IGD-M (SAGICAD); orientações IGD para Conselhos; portaria de valores/teto.

**Fontes:** [IGD — MDS](https://www.gov.br/mds/pt-br/igd) ·
[IGD Bolsa Família — MDS](https://www.gov.br/mds/pt-br/acoes-e-programas/bolsa-familia/igd) ·
[IN 091 IGD-M-PBF — SAGICAD](https://wiki-sagi.mds.gov.br/home/DS/NBF/I/IN091) ·
[Orientações prestação de contas IGD (PDF)](https://www.mds.gov.br/webarquivos/publicacao/bolsa_familia/Guias_Manuais/Orientacoes_prestacao_contas_IGD.pdf)

---

## 8. Mapa de envios ao MDS (resumo)

| Item | Sistema MDS | Periodicidade | Município envia? | Confiança |
|---|---|---|---|---|
| CadÚnico | App CadÚnico (Dataprev) | Contínuo; atualização família 2 anos; AVE/REV anual | Opera online (não arquivo) | ALTA |
| RMA | RMA/SAGI | Mensal (até 30 dias após o mês) | Sim (web) | ALTA |
| Censo SUAS | Censo SUAS/SAGI | Anual (~set–nov) | Sim (web) | ALTA |
| Prestação de contas FNAS | **AgilizaSUAS** (+BB Gestão Ágil) | Contínua, prazo 1º/mar do ano seguinte | Sim | ALTA |
| Prontuário SUAS | Local/eletrônico | Contínuo (não envia ao MDS) | Não (instrumental) | ALTA |
| PBF condicionalidades | SICON/SIGPBF (+Saúde/Presença) | Por vigência (semestral saúde / bimestral educação `[a confirmar]`) | Sim | MÉDIA |
| IGD | Calculado pelo MDS | Mensal | Não envia (é resultado) | ALTA |

---

## 9. Pendências consolidadas `[a confirmar — obter doc oficial]`

1. **Portaria MDS 1.043/2024 íntegra** + Manual Operacional **AgilizaSUAS** — fluxo/contrato exato da
   nova prestação de contas (substitui Demonstrativo SUASWeb). **Bloqueador** para integração de prestação de contas.
2. **Leiautes de arquivo CadÚnico** (importação/extrato/retorno AVE/REV; layout V7/V8) — para qualquer sync.
3. **Manuais RMA (CRAS/CREAS/POP)** — campos exatos por questionário, para gerar RMA do Prontuário.
4. **Questionários Censo SUAS do ano vigente** — campos por tipo de unidade.
5. **Tabela de pisos / NOB-SUAS** — valores de referência de cofinanciamento por piso/serviço (não hardcodar).
6. **Decreto 12.064/2024 + Portaria 1.058/2025** — percentuais/regras vigentes das condicionalidades PBF.
7. **IN do IGD-M** — fórmula/pesos exatos dos fatores.
8. **Lei/decreto municipal de Benefícios Eventuais do tenant piloto** (Maximiliano de Almeida/RS) —
   tipos, valores, critérios (parametrizar por tenant+vigência, §16).
9. **APIs/contratos** SICON/SIGPBF, Sistema Presença (MEC), PBF na Saúde — viabilidade de integração programática
   (hoje provável só web).
