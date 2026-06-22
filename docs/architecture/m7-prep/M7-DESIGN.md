# M7 — DESIGN pronto-para-implementar: Saúde + Educação + Assistência Social

> Arquiteto: design de implementação dos 3 setores que recebem **repasses federais fundo a fundo**
> e respondem a **mínimos constitucionais** (Saúde 15% ASPS · Educação 25% MDE + 70% FUNDEB).
> Baseado em `pesquisa-*.md` + `verificacao-*.md` (auditoria cética, 2026-06-22) e nos módulos
> **já scaffolded** `Saude`/`Educacao`/`AssistenciaSocial`.
>
> **Premissa central (corrigida pela verificação):** os módulos já têm domínio **clínico/pedagógico/
> socioassistencial** rico (Paciente, Atendimento, Regulação / Escola, Matrícula, Diário / Família,
> Prontuário, Benefício). O **gap do M7 é a CAMADA FISCAL-DE-INTEGRAÇÃO**: (a) espelhar a execução
> do **fundo setorial** (FMS/FME-FUNDEB/FMAS) por **bloco/piso**; (b) apurar os **mínimos
> constitucionais**; (c) **gerar/exportar** os envios aos sistemas nacionais; (d) **registrar o
> parecer do conselho**; (e) **ligar à contabilidade/M4** (RREO/SIAPC-PAD/SICONFI). Esse eixo é o de
> **maior valor e menor risco regulatório** — implementar primeiro.
>
> **Regra §16 (inegociável):** nenhum percentual/valor/leiaute/prazo hardcoded. Tudo que é lei/portaria
> é **parâmetro versionado por tenant+vigência** (reusar o padrão `ParametroVigente` já existente em
> `AssistenciaSocial.Infrastructure`). Toda integração nasce **atrás de ACL + Outbox + idempotência**,
> com **versão de contrato explícita**, e fica **BLOQUEADA-SEM-LEIAUTE** até obter o doc oficial.
>
> **Legenda de CONFIANÇA:** **ALTA** = base legal/fluxo confirmado em fonte oficial primária na
> verificação; **MÉDIA** = conceito correto, detalhe técnico (XSD/endpoint/contas→campos) depende de
> doc não obtido; **BAIXA** = bloqueado por leiaute/contrato não obtido — não implementar fiel sem o doc.

---

## 0. Princípio arquitetural transversal — três setores, um mesmo esqueleto fiscal

A verificação confirmou que os 3 domínios têm **estrutura idêntica com nomes diferentes**. O M7 deve
**fatorar esse esqueleto** em vez de reimplementar 3×:

| Eixo | Saúde | Educação | Assistência Social |
|---|---|---|---|
| Fundo federal → municipal | FNS → **FMS** | FNDE → **FME / conta FUNDEB** | FNAS → **FMAS** |
| Segregação do recurso | **bloco** (Custeio/Investimento) | **conta FUNDEB / MDE** | **bloco/piso** (PSB/PSE/Gestão/PBF) |
| Mínimo constitucional | **15% ASPS** (LC 141/2012) | **25% MDE** (CF 212) + **70% FUNDEB** folha | — (cofinanciamento por piso; sem % constitucional) |
| Sistema setorial de contas | **SIOPS** (bimestral) | **SIOPE** (bimestral) | **AgilizaSUAS** (contínuo, 1º/mar) |
| Anexo RREO espelho | **Anexo 12** (ASPS) | **Anexo 8** (MDE) | — |
| Conselho + instrumento | **CMS** → RAG (DigiSUS DGMP) | **CACS-FUNDEB**/CME → parecer | **CMAS** → parecer (Demonstrativo) |

**Blocos compartilhados a criar (núcleo M7, em BuildingBlocks ou SharedKernel de domínio fiscal):**

1. **`CalendarioFederal`** (parâmetro versionado por tenant+exercício): prazos de SIOPS/SIOPE
   bimestrais, remessa APS mensal, parcelas PNAE/PNATE, campanhas AVE/REV, janela Censo SUAS, prazo
   AgilizaSUAS. **RISCO #1 das 3 verificações:** prazos/rótulos federais envelhecem em meses → **zero
   prazo no domínio**. **CONFIANÇA: ALTA** (necessidade); valores = parâmetro.
2. **`FonteRecursoVinculado`** — extensão da tabela de fontes/destinação do PCASP (M2) para carimbar
   cada repasse por bloco/piso. **Não duplicar dado contábil** — o M2/M3 é a fonte da verdade; o M7
   **classifica e deriva**. **CONFIANÇA: ALTA.**
3. **`ApuradorMinimo`** (serviço de domínio genérico) — recebe (receita-base, despesas computáveis,
   percentual-meta-vigente) e devolve indicador + status. Especializações: `ApuradorAsps` (15%),
   `ApuradorMde` (25%), `IndicadorAplicacaoFundeb` (70% folha). **CONFIANÇA: MÉDIA** (a classificação
   de despesa computável é a parte delicada — depende do manual vigente de cada anexo RREO).
4. **`EnvioGatewayBase`** (ACL + Outbox + idempotência + versão-de-contrato) — base de todos os
   gateways de envio (RNDS, SISAB/SIAPS, BNAFAR, Educacenso, AgilizaSUAS, RMA). Reusa o padrão
   `IRndsGateway`/`ICadUnicoGateway` já existentes + Polly. **CONFIANÇA: ALTA** (padrão); contratos
   concretos = BAIXA até leiaute.
5. **`ParecerConselho`** (agregado) — registra parecer do CMS/CACS/CMAS (data, resolução,
   aprovado/ressalva/rejeitado) como evidência imutável de prestação. **CONFIANÇA: ALTA.**
6. **Ligação M4:** o M7 **estende** o M4 (que já gera RREO/RGF/MSC/DCA/SIAPC-PAD) com os **vínculos
   setoriais** e as **exportações setoriais**. **Não é envio separado ao TCE** — os Anexos 8/12 e os
   dados de função 10/12/08 saem no **mesmo PAD mensal**. Consome `DespesaEmpenhada`/`PagamentoEfetuado`
   (Financas.Contracts, já existem) para alimentar a apuração. **CONFIANÇA: ALTA.**

---

## 1. SAÚDE — entidades, integrações, mínimo, contábil

### 1.1 Novas entidades núcleo (camada fiscal/integração — o clínico já existe)
- **`FundoMunicipalSaude` (FMS)** [raiz · `IMustHaveTenant`] — conta-corrente por **bloco** (Custeio /
  Investimento, Port. GM/MS 3.992/2017) e por **componente APS** (Port. 3.493/2024). Espelha parcelas
  recebidas do FNS e a execução por bloco. **CONFIANÇA: ALTA** (estrutura 2 blocos confirmada);
  códigos de blocos/componentes **[a confirmar — Portaria de Consolidação GM/MS nº 6/2017 consolidada]**.
- **`DeclaracaoSiops`** [raiz] por `(Tenant, Bimestre)` — deriva do PCASP via `ApuradorAsps`; gera o
  **Anexo 12 do RREO**. Status de transmissão. **CONFIANÇA: MÉDIA** (canal técnico SIOPS = BAIXA).
- **`MonitorEnvioSisabSiaps`** [read model] — status da última remessa de produção APS por unidade/
  equipe; alerta de risco de perda de repasse. **CONFIANÇA: MÉDIA.**
- **`RelatorioAnualGestao` (RAG)** [raiz] — dados financeiros (execução por bloco, %ASPS, RAP saúde) +
  `ParecerConselho` do CMS. **CONFIANÇA: ALTA** (instrumento); import no DGMP = MÉDIA.

### 1.2 Integrações/envios (cada uma atrás de ACL + Outbox + versão de contrato)
| Integração | Direção | Periodicidade | CONFIANÇA | Docs oficiais a obter | Parametrizável |
|---|---|---|---|---|---|
| **SIOPS** | ENVIA (Anexo 12) | **Bimestral, até 30d após bimestre** | canal **BAIXA** | Manual técnico SIOPS + **tabela classificação ASPS (LC 141 arts. 3º/4º)** + canal (arquivo/API/digitação) + calendário bimestres | %ASPS (default 15%, Lei Orgânica pode ser maior); calendário |
| **FNS fundo a fundo** | CONSOME + presta contas | Mensal (parcelas) | **MÉDIA** | Port. 204/2007 + 3.992/2017 vigentes; arquivo/API de parcelas por bloco; RAG/SARGSUS→**DigiSUS DGMP** | taxonomia blocos/componentes por vigência |
| **RNDS** | ENVIA + CONSOME | Por evento (tempo real) | perfis **BAIXA** | Manual Barramento (vigente) + Guia RNDS "Modelos" (Bundles FHIR R4) + headers exatos | ambiente homol/prod; cenários obrigatórios |
| **e-SUS APS → SISAB/SIAPS** | ENVIA | **Remessa MENSAL (10º dia útil)**; quadrimestre = janela de média dos indicadores | **MÉDIA** | **Port. 7.639/2025 (SIAPS substitui SISAB)** + manual SIAPS + Port. 3.493/2024 anexos (IED, 6 componentes, indicadores) | sistema-alvo (SISAB→SIAPS), valores R$/equipe, indicadores, calendário |
| **SI-PNI** | ENVIA (via RNDS/SISAB) | Por evento | **MÉDIA** | perfil FHIR Immunization (RNDS); manual novo SI-PNI | mapeamento imunobiológicos |
| **CNES** | ENVIA | **Mensal (mín.)** ou a cada alteração | **MÉDIA** | Port. Consolidação 01/2017 arts. 371/372; leiaute/import SCNES | tabelas (tipos estab., CBO, INE) |
| **SISREG** | CONSOME (regula) | Filas ~24h | **BAIXA** | confirmar **se existe API** + manuais III | — |
| **HÓRUS→e-SUS AF / BNAFAR** | ENVIA | **Diária** | XSD **BAIXA** | **Port. 5.713/2024** + manual web service BNAFAR (XSD/endpoints) + cronograma e-SUS AF | RENAME/CATMAT |

**Correções da verificação que viram requisito de engenharia (não negociar):**
- **RNDS: token vale 15 min** (não 30) e autenticação é **Two-way SSL / mTLS** — o A1 é o **client
  certificate do handshake TLS**, configurado no `HttpClientHandler`, não credencial no corpo. Reusar
  A1+Key Vault do M2; renovar token com margem < 15 min. **CONFIANÇA: ALTA (corrigido).**
- **SISAB está sendo substituído por SIAPS (Port. 7.639/2025)** — **sistema-alvo de financiamento é o
  SIAPS**, não SISAB. Modelar o destino como **parâmetro versionado**, nunca hardcoded.
- **Remessa APS é MENSAL (10º dia útil)**; o quadrimestre é só a janela de média dos indicadores dos
  Componentes de Vínculo/Qualidade. Separar `PrazoRemessaMensal` de `JanelaIndicadorQuadrimestral`.

### 1.3 Mínimo constitucional — painel 15% ASPS
- `ApuradorAsps`: receita de impostos+transferências × despesas ASPS computáveis (LC 141 art. 3º
  computam; art. 4º **não** computam — saneamento, inativos/pensionistas, merenda, limpeza urbana) →
  % aplicado vs. **15% parametrizável**. Gera **Anexo 12 RREO**. **CONFIANÇA: MÉDIA** (regra de
  classificação depende do Manual SIOPS vigente — bloqueador de fidelidade).
- **Painel:** indicador bimestral **informativo** + aferição que o TCE/SIOPS consideram (LC 141 é mais
  rígida: aferição com regras próprias). Default 15%, **parametrizável por tenant+vigência**.

### 1.4 Ligação com contabilidade/repasses
- Repasse FNS entra como **receita orçamentária com fonte/recurso vinculado por bloco** no PCASP (M2).
- `ApuradorAsps` consome execução (Financas.Contracts) — **não duplica** contas.
- Anexo 12 sai no **PAD mensal do M4** (TCE-RS) + transmissão ao SIOPS. RAG armazena parecer do CMS.

---

## 2. EDUCAÇÃO — entidades, integrações, mínimos, contábil

> **O Censo é a raiz de tudo:** matrícula/turma/escola (agregado já existente) é a **fonte canônica**
> da qual derivam Educacenso, FUNDEB, PNAE e PNATE. Errar o Censo propaga erro em todos os repasses.

### 2.1 Novas entidades núcleo
- **`ExportacaoEducacenso`** [raiz] por `(Tenant, Ciclo, Etapa)` — gera o **arquivo de migração** a
  partir do agregado de matrícula, parametrizado por **`LeiauteEducacenso`** (contrato externo
  versionado por ciclo). 1ª etapa (Matrícula Inicial) e 2ª etapa (Situação do Aluno). **CONFIANÇA:
  MÉDIA** — leiaute (delimitador/estrutura de registro) é **bloqueador** (a verificação NÃO confirmou
  pipe `|`; tratar como contrato versionado, gerador parametrizado pelo layout).
- **`DeclaracaoSiope`** [raiz] por `(Tenant, Bimestre)` — deriva da execução da **função 12**; gera
  **Anexo 8 RREO**. **CONFIANÇA: MÉDIA** (mapeamento contas→campos SIOPE = bloqueador).
- **`RepasseFnde`** (PNAE/PNATE) [read model + conciliação] — parcelas recebidas, saldo por programa,
  per capita aplicado. **CONFIANÇA: MÉDIA.**
- **`DistribuicaoFundeb`** [informativo/conciliação] — insumo = matrículas ponderadas (Censo +
  ponderações CIF); **não recalcula a cota** (quem rateia é o ente estadual/FNDE) — **concilia**.
- **`IndicadorAplicacaoFundeb`** — % pago a profissionais sobre receita FUNDEB (cruza com folha M5).
- **`AferimentoMdeAnual`** + **`IndicadorMdeBimestral`** — **separados** (verificação RISCO #3): o
  mínimo de 25% é **aferido no encerramento do exercício**; o bimestral é só acompanhamento (evita
  alarme falso nos bimestres iniciais). **CONFIANÇA: ALTA (corrigido).**

### 2.2 Integrações/envios
| Integração | Direção | Periodicidade | CONFIANÇA | Docs oficiais a obter | Parametrizável |
|---|---|---|---|---|---|
| **Educacenso/INEP** | ENVIA (migração) | Anual, 2 etapas (ref. última quarta de maio) | leiaute **BAIXA** | **Leiaute/caderno de conceitos do ciclo** (estrutura de registro/delimitador) + **confirmar existência de API** (não afirmar ausência) | calendário 2 etapas + leiaute por exercício |
| **SIOPE/FNDE** | ENVIA (Anexo 8) | **Bimestral, ordem cronológica** | **MÉDIA** | leiaute SIOPE (contas→campos) + recibo bimestral; base legal = **Port. Interm. 424/2016** (NÃO LC 141 — RISCO #3) | %MDE (default 25%); calendário |
| **PNAE (merenda)** | CONSOME + presta contas | até **8 parcelas** (fev–set) | estrutura **ALTA**; valores parâmetro | **Resolução PNAE vigente (02/03/2026)**: confirmar **45% agricultura familiar** (era 30% — RISCO #1) + 85% in natura; **tabela per capita** por etapa; SIGPC | per capita; %AF (30%→**45%** por vigência); nº/calendário parcelas; CAE |
| **PNATE (transporte)** | CONSOME + presta contas | **2 parcelas** (mar/ago) | estrutura **ALTA**; valores parâmetro | Res. CD/FNDE 05/2024; **per capita/FNRM** vigente (pop. rural+área+pobreza+IDEB) | per capita/FNRM; calendário |
| **FUNDEB** | CONSOME + comprova | Anual + saldo | estrutura **ALTA**; fatores parâmetro | **fatores de ponderação CIF** + VAAF-MIN/VAAT-MIN (Port. Interm. MEC/MF) + art. 25 Lei 14.113 (saldo **até 10% no 1º trimestre seguinte**) + rol de profissionais do 70% | ponderações; VAAF/VAAT/VAAR; %folha (default **70%**); regra de saldo |
| **CACS-FUNDEB / SISCACS** | registra parecer | conforme FNDE | **MÉDIA** | confirmar instrumento anual de parecer do CACS | — |

**Correções da verificação (requisito):**
- **Agricultura familiar PNAE = 45% (Res. 02/03/2026), não 30%** — **parâmetro versionado** (30%→45%
  por vigência). Hardcodar 30% gera **falso "conforme"** → apontamento TCE. **CONFIANÇA: ALTA (corrigido).**
- **Base legal do prazo SIOPE = Port. Interm. 424/2016 + LRF art. 52 + CF 165§3** (LC 141 é da saúde).
- **Leiaute Educacenso não confirmado** (delimitador/estrutura) → `LeiauteEducacenso` versionado;
  **não declarar "ausência de API"** como fato.

### 2.3 Mínimos — painéis 25% MDE e 70% FUNDEB
- `ApuradorMde` (25%, default parametrizável) → Anexo 8 RREO; **aferição anual** + acompanhamento
  bimestral. `IndicadorAplicacaoFundeb` (70% folha de profissionais da educação básica; rol
  parametrizável — divergência interpretativa TCE/CNM). **CONFIANÇA: ALTA** (estrutura) / **MÉDIA**
  (classificação computável).

### 2.4 Ligação contábil
- FUNDEB/MDE entram como **fonte/recurso vinculado** (conta FUNDEB + MDE) no PCASP; 70% cruza com a
  **folha (M5)**; Anexo 8 sai no **PAD mensal (M4)** + transmissão SIOPE; saldo FUNDEB (10%/1º tri)
  relevante no encerramento contábil.

---

## 3. ASSISTÊNCIA SOCIAL — entidades, integrações, contábil (sem mínimo %)

> CadÚnico é a base de tudo, **operado online no app do MDS/Dataprev** — o ERP é **read/sync model +
> fila de pendências**, nunca substitui o cadastro federal. (O módulo já tem `ICadUnicoGateway`,
> `ICadUnicoReadModel`, `Familia`, `Prontuario`, `Beneficio`, `ParametroVigente`.)

### 3.1 Novas entidades núcleo (camada de envio/fiscal — operacional já existe)
- **`FundoMunicipalAssistencia` (FMAS)** [raiz] — conta por **4 blocos** (PSB / PSE / Gestão SUAS /
  Gestão PBF-CadÚnico, Port. 1.043/2024) e por **piso** (Básico Fixo/Variável, Fixo de Média
  Complexidade, Acolhimento). Espelha **Parcelas Pagas/Saldos** (importadas do SUASWeb). **CONFIANÇA:
  ALTA** (4 blocos); valores de pisos **[a confirmar — tabela de pisos/NOB-SUAS]**.
- **`RegistroMensalAtendimento` (RMA)** [raiz] por `(Unidade, Competencia)` — **gerado a partir do
  Prontuário/atendimentos** (já existe), evita dupla digitação; fechável e auditável. **CONFIANÇA: ALTA.**
- **`CensoSuas`** [raiz] por `(Unidade, AnoBase)` — **pré-populado** do cadastro de unidades/lotação RH
  (M5) + RMA. **CONFIANÇA: ALTA** (anual; Decreto 7.334/2010 confirmado); campos por ano = MÉDIA.
- **`PrestacaoContasAgilizaSuas`** [raiz] — Demonstrativo Sintético **contínuo** (substitui SUASWeb;
  + BB Gestão Ágil para o financeiro), prazo **1º/mar do exercício seguinte**, + `ParecerConselho`
  do CMAS. **CONFIANÇA: ALTA** (fluxo); contrato/API = BAIXA (provável web).
- **`EstimativaIgd`** [read model] — estima IGD-M/IGD-SUAS dos dados internos (taxa de atualização
  cadastral, condicionalidades, status prestação, parecer CMAS) — **gestão preditiva de receita**, não
  cálculo oficial. **CONFIANÇA: ALTA** (estrutura `FatorI×II×III×IV`); pesos exatos = MÉDIA (IN 091).

### 3.2 Integrações/envios
| Integração | Direção | Periodicidade | CONFIANÇA | Docs oficiais a obter | Parametrizável |
|---|---|---|---|---|---|
| **CadÚnico (Dataprev)** | opera online + sync | Contínuo; atualização 2 anos; **AVE/REV anual** | leiaute **BAIXA** | Decreto 11.016/2022; manual SAGICAD; **layout V7/V8** AVE/REV | calendário AVE/REV (rótulo anual, não "AVE25" fixo) |
| **RMA → SAGI/MDS** | ENVIA | **Mensal, até 30d após o mês** | **MÉDIA** | Manuais RMA (CRAS/CREAS/POP); Res. CIT 4/2011, 20/2013; confirmar API | campos por questionário |
| **Censo SUAS → SAGI** | ENVIA | **Anual** (2º semestre) | **MÉDIA** | questionários do ano vigente | janela + campos por ano |
| **AgilizaSUAS (+BB Gestão Ágil)** | ENVIA/presta contas | Contínuo, **1º/mar** | contrato **BAIXA** | **Port. 1.043/2024** + Manual Operacional AgilizaSUAS; **prazos prorrogados (Port. SNAS 132/2025)** | calendário federal por exercício |
| **PBF — SICON/SIGPBF/Presença** | acompanha condicionalidades | Saúde **semestral** / Educação **bimestral** | fluxo **ALTA**; API **BAIXA** | Decreto 12.064/2024 + Port. 1.058/2025; contratos SICON/Presença | frequência (60%/75%), vigências |
| **BPC** | read model (não paga) | — | **ALTA** | — | — |

**Correções da verificação (requisito):**
- **Benefício eventual: remover "1/4 SM como referência LOAS"** — a Lei 12.435/2011 **retirou** o teto
  fixo da LOAS art. 22; critério de renda é **100% lei/decreto municipal + CMAS** → parametrizável por
  tenant+vigência, **sem default federal embutido**. Risco jurídico de indeferir indevidamente. (O
  README atual ainda diz "renda ≤ ½ SM" e "BPC < ¼ SM" — **rever como parâmetro municipal/federal
  vigente**, não constante.) **CONFIANÇA: ALTA (corrigido).**
- **Prazos de transição AgilizaSUAS prorrogados (Port. SNAS 132/2025)** → `CalendarioFederal`, nunca
  hardcoded.
- **AVE/REV é rótulo de campanha anual** (AVE26/REV26…) → parâmetro de calendário.

### 3.3 Sem mínimo constitucional %
A assistência **não tem mínimo % constitucional** — o vínculo é por **piso/cofinanciamento** e parecer
do CMAS. O painel é de **execução por piso + saldos + estimativa IGD**, não de "% mínimo".

### 3.4 Ligação contábil
- Cada **piso/bloco** = fonte vinculada no PCASP do FMAS; rastrear saldo, aplicação e **reprogramação
  de saldo** (facilitada pela 1.043/2024). Demonstrativo alimenta AgilizaSUAS; parecer CMAS imutável.

---

## 4. Painéis de mínimos constitucionais (consolidado, alimenta M8)

- **15% ASPS** (Saúde) e **25% MDE** + **70% FUNDEB** (Educação): indicador **bimestral informativo** +
  **aferição anual de conformidade** (a que vale para TCE/SICONFI). Percentuais **parametrizáveis por
  tenant+vigência** (default legal). Saída: Anexo 12 (SIOPS) e Anexo 8 (SIOPE) do RREO, no **PAD mensal
  do M4**. Assistência: **sem %** — painel de execução por piso + estimativa IGD.
- **Alertas** (Outbox → Transparência/Portal do Gestor M8): "abaixo do mínimo projetado", "remessa
  setorial vencida", "CNES/SIOPS/BNAFAR não alimentado → risco de bloqueio de repasse".
- **CONFIANÇA: ALTA** (necessidade/estrutura); **MÉDIA** na classificação de despesa computável (depende
  do manual vigente de cada anexo) — bloqueador de fidelidade.

---

## 5. Ordem de implementação (sub-marcos)

> Estratégia: **eixo fiscal primeiro** (maior valor, menor risco, reusa M2/M3/M4); integrações de
> envio por último e **BLOQUEADAS-SEM-LEIAUTE** (ACL/gerador compila; transmissão fiel só com o doc).

- **M7.0 — Núcleo fiscal compartilhado.** `CalendarioFederal` + `FonteRecursoVinculado` (extensão PCASP)
  + `ApuradorMinimo` base + `ParecerConselho` + `EnvioGatewayBase` (ACL/Outbox/idempotência/versão de
  contrato). Reusa `ParametroVigente`, A1/Key Vault, contratos Financas. **CONFIANÇA: ALTA.**
- **M7.1 — Mínimos constitucionais (Saúde 15% + Educação 25%/70%).** `ApuradorAsps`, `ApuradorMde`,
  `IndicadorAplicacaoFundeb`, `AferimentoMdeAnual`/`IndicadorMdeBimestral`; geração **Anexo 12 / Anexo 8**;
  integração ao **PAD mensal (M4)**. **Entrega de maior valor.** **CONFIANÇA: ALTA** (estrutura).
- **M7.2 — Fundos setoriais por bloco/piso.** `FundoMunicipalSaude` (2 blocos + APS),
  `FundoMunicipalAssistencia` (4 blocos + pisos), conta FUNDEB/MDE; conciliação de parcelas recebidas
  (importação de extrato — leiaute a obter). **CONFIANÇA: MÉDIA.**
- **M7.3 — Prestação de contas + conselhos.** RAG (DigiSUS DGMP), `PrestacaoContasAgilizaSuas`,
  parecer CACS-FUNDEB; pareceres CMS/CMAS/CACS imutáveis. **CONFIANÇA: ALTA** (fluxo) / contratos BAIXA.
- **M7.4 — Censo/produção (base dos repasses).** `ExportacaoEducacenso` (gerador parametrizado por
  `LeiauteEducacenso`), `RMA` (do Prontuário), `CensoSuas` (pré-populado), `MonitorEnvioSisabSiaps`.
  **BLOQUEADO-SEM-LEIAUTE.** **CONFIANÇA: MÉDIA/BAIXA.**
- **M7.5 — Integrações de envio em tempo real / barramentos.** RNDS (mTLS+15min, homologação antes de
  produção), SI-PNI, BNAFAR/e-SUS AF (diária), CNES, SISAB→**SIAPS**. **Mais frágil e menos confirmada**
  (RISCO #2) — só com manual vigente + testes de contrato. **CONFIANÇA: BAIXA até leiaute.**
- **M7.6 — Repasses FNAS/PNAE/PNATE/FUNDEB (conciliação fina + saldos/reprogramação).** read models de
  parcelas, saldos, %AF (45%), saldo FUNDEB 10%. **CONFIANÇA: MÉDIA.**

---

## 6. Bloqueadores de fidelidade consolidados (`[a confirmar — obter doc oficial]`)

**Saúde:** Manual SIOPS + tabela classificação ASPS (LC 141 arts. 3º/4º) + canal SIOPS; Port. 204/2007
e 3.992/2017 + arquivo/API de parcelas FNS; Manual Barramento RNDS + Guia "Modelos" (Bundles FHIR) +
headers; **Port. 7.639/2025 (SIAPS)** + manual SIAPS + Port. 3.493/2024 anexos; Port. Consolidação
01/2017 arts. 371/372 (CNES); existência de API SISREG; **Port. 5.713/2024** + web service BNAFAR
(XSD); DigiSUS DGMP (aceita import?).
**Educação:** **Leiaute Educacenso do ciclo** + confirmar API; leiaute SIOPE (contas→campos); **Res.
PNAE 02/03/2026 (45% AF / 85% in natura)** + tabela per capita; per capita/FNRM PNATE + Res. 05/2024;
fatores CIF FUNDEB + VAAF/VAAT-MIN + art. 25 Lei 14.113 (saldo) + rol 70%; instrumento parecer CACS.
**Assistência:** Port. 1.043/2024 + Manual AgilizaSUAS (+ Port. SNAS 132/2025 prazos); layout V7/V8
CadÚnico AVE/REV; Manuais RMA; questionários Censo SUAS do ano; tabela de pisos/NOB-SUAS; Decreto
12.064/2024 + Port. 1.058/2025 (condicionalidades PBF); IN 091 (IGD-M); **lei municipal de benefícios
eventuais do tenant piloto (Maximiliano de Almeida/RS)**; APIs SICON/SIGPBF/Presença.

---

## 7. Riscos arquiteturais (das 3 verificações) e mitigação no design

1. **Janela móvel regulatória** (SISAB→SIAPS, Previne→3.493/2024, HÓRUS→e-SUS AF, BNAFAR 5.713/2024,
   AgilizaSUAS, PNAE 45%): 4+ domínios em transição 2024–2026. **Mitigação:** TUDO (sistema-alvo,
   %, valores R$/equipe, indicadores, calendários, rótulos AVE/REV) = **configuração versionada por
   tenant+vigência** (`ParametroVigente`/`CalendarioFederal`); ACL/Outbox com **versão de contrato
   explícita**; nunca hardcodar o sistema-destino.
2. **Transmissão é a parte mais frágil e menos confirmada** (RNDS mTLS+15min, BNAFAR XSD, import
   SIOPS/CNES, API SISREG, leiaute Educacenso): erro de token/header/mTLS quebra em produção e, como
   CNES/SIOPS/BNAFAR são condicionantes, **bloqueia repasse**. **Mitigação:** **BLOQUEADO-SEM-LEIAUTE**;
   homologação RNDS antes de produção; testes de contrato; gerador compila sem o doc, transmissão fiel não.
3. **Fonte secundária virando regra hardcoded** (token 30 vs 15 min; "fechamento quadrimestral" como
   cadência; AF 30% vs 45%; 1/4 SM revogado): número errado hardcoded = **falha silenciosa/apontamento
   TCE/risco jurídico**. **Mitigação:** toda constante temporal/percentual/financeira cita **fonte
   primária**; valor de terceiro fica `[a confirmar]` até o doc; parametrizar por vigência.

---

## 8. Decisões de escopo a validar com o produto (não-bloqueantes para M7.0–M7.3)
- Saúde M7 = primariamente **gestão do FMS + mínimo + integração de envio** (não reconstruir e-SUS/PEC);
  módulos clínicos como read models/ACL sobre os sistemas nacionais.
- Reuso confirmado: **A1/Key Vault (M2)** p/ RNDS; **PCASP/fonte vinculada (M2/M3)** p/ FMS/FMAS/FUNDEB;
  **RH (M5)** p/ profissionais×CNES e 70% FUNDEB; **prestação de contas/RREO/SIAPC-PAD (M4)** p/
  RAG/SIOPS/SIOPE/AgilizaSUAS; **Financas.Contracts** (`DespesaEmpenhada`/`PagamentoEfetuado`) p/ apuração.
- Conformidade com §16 e §5 (isolamento de módulo, multi-tenant, auditoria imutável, NRT/warnings=errors)
  preservada — cross-module só via `*.Contracts`/Integration Events; dado sensível não trafega no barramento.
