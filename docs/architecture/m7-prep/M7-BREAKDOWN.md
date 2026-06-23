# M7 — BREAKDOWN DE IMPLEMENTAÇÃO: Saúde + Educação + Assistência Social

> **Para quem executa.** Este documento transforma o `M7-DESIGN.md` (+ `pesquisa-*.md` /
> `verificacao-*.md`, auditoria de 2026-06-22) numa **sequência de sub-workflows pequenos e
> verificáveis**, prontos para rodar em sub-marcos. **Não re-pesquise** — a base legal e os fluxos
> já foram confirmados; o que falta é leiaute/contrato técnico (marcado `[a confirmar]`) e credenciais
> do dono (RNDS etc.). Cada sub-workflow declara: **entidades-núcleo de domínio**, **fluxos**,
> **integrações/envios oficiais**, **painel de mínimo** (quando há), **o que REUSA do que já existe**,
> e **tamanho/risco honestos**.
>
> **Regra §16 (inegociável):** nenhum percentual/valor/leiaute/prazo hardcoded. Tudo que é lei/portaria
> é **parâmetro versionado por tenant+vigência** (padrão `ParametroVigente`). Toda integração nasce
> **atrás de ACL + Outbox + idempotência + versão de contrato**, e fica **BLOQUEADA-SEM-LEIAUTE** até o
> doc oficial.
>
> **Legenda de tamanho:** **P** = 1 sub-workflow curto (1 agregado/serviço + testes); **M** = vários
> arquivos coordenados; **G** = grande/arriscado (quebrar em iterações). **Risco** reflete confiança do
> design + dependência externa.

---

## 0. Estado real do que já existe (apurado no código — corrige premissas do design)

Antes da sequência, três fatos do código que **mudam o plano** e que o executor precisa saber:

1. **REUSO confirmado e pronto (não re-fazer):** os 3 módulos (`Saude`/`Educacao`/`AssistenciaSocial`)
   **já têm Outbox + resiliência wired** (migração `OutboxResiliencia`, 2026-06-22, com `AttemptCount`/
   `DeadLetteredOnUtc`/`NextAttemptUtc` + dispatcher + background drain a cada 30s), **audit trail imutável
   com hash-chain** (`AuditSaveChangesInterceptor`, BuildingBlocks), **tenant isolation por reflexão**
   (`IMustHaveTenant` + Global Query Filter na base) e **Polly** (`AddStandardResilienceHandler`, padrão
   do Tributos/Transparencia). Toda entidade nova do M7 herda isso de graça.
2. **A1/Key Vault NÃO está "no M2" — está no módulo `Cofre`** e já é **genérico por tenant**:
   `IServicoAssinaturaDigital` + `ICofreCertificadoRepository.ObterAtivoAsync` (DEK envelopado AES-256-GCM,
   KEK no Key Vault via Managed Identity, auditado). **RNDS mTLS e assinaturas reusam o Cofre** — sem
   copiar nada. (Onde o design diz "A1/Key Vault do M2", leia **Cofre**.)
3. **⚠️ Os contratos de Finanças são RASOS — esta é a dependência real do M7.0.**
   `DespesaEmpenhadaIntegrationEvent` carrega só `ClassificacaoOrcamentaria` como **texto concatenado**
   (`orgao.unidade.funcional.categoria.fonte`); `PagamentoEfetuadoIntegrationEvent` nem fonte tem (tem TODO
   de SIAPC). O VO `InformacoesComplementaresMsc` (Finanças) **tem** `FonteRecurso`/`NaturezaDespesa`/
   `FuncaoSubfuncao`, **mas** o próprio código diz que esses campos **só são preenchidos a partir do M3.x/M4**.
   **Consequência:** o `ApuradorMinimo` **não consegue** classificar despesa computável só com os eventos de
   hoje. **M7.0 precisa decidir e implementar a ligação de dados** (uma das duas vias do §A abaixo) — isto é
   trabalho real, não "reuso gratuito". **É o primeiro risco de execução do marco.**

### §A — A decisão de arquitetura que destrava tudo (resolver no M7.0)
O eixo fiscal inteiro depende de saber **qual despesa pertence a qual bloco/piso/função**. Duas vias:

- **Via A1 (preferida) — enriquecer o contrato de Finanças.** Acrescentar a `DespesaEmpenhada`/
  `PagamentoEfetuado` os campos decompostos (`FonteRecurso`, `FuncaoSubfuncao`, `NaturezaDespesa`,
  `Competencia`) — **versão nova de contrato**, retrocompatível (campos opcionais). O M7 consome eventos
  já carimbados. Acopla M7 ao roadmap de Finanças (M3.x), mas é a fonte da verdade única.
- **Via A2 (fallback) — `FonteRecursoVinculado` no M7** como **projeção/classificador próprio**: o M7
  mantém uma tabela de mapeamento `(fonte PCASP, função) → bloco/piso setorial` por tenant+vigência e
  deriva a apuração cruzando os eventos rasos com essa tabela. Não duplica o dado contábil (Finanças
  continua a fonte), mas **deriva** a classificação setorial. Funciona sem esperar Finanças.

> **Recomendação:** começar por **A2** (não bloqueia o M7 no roadmap de Finanças) e migrar para **A1**
> quando Finanças entregar os campos. O `ApuradorMinimo` deve depender de uma **abstração de fonte de
> dados** (`IExecucaoSetorialReadModel`) para que a troca A2→A1 não toque o domínio.

---

## 1. Núcleo fiscal compartilhado (M7.0) — **fazer primeiro, os 3 setores dependem**

> Um só esqueleto fiscal para os 3 setores (o design provou que são "a mesma coisa com nomes diferentes").
> Mora em SharedKernel de domínio fiscal / BuildingBlocks (sem vazar entre módulos).

| # | Sub-workflow | Entidades-núcleo / serviços | Reusa | Tam. | Risco |
|---|---|---|---|---|---|
| **0.0** | **Decisão A1×A2 + abstração de fonte de dados** (§A). Definir `IExecucaoSetorialReadModel` (receita-base, despesas por fonte/função/competência) e implementar a via escolhida. **Destrava o resto.** | `IExecucaoSetorialReadModel`; (A2) `FonteRecursoVinculado` map tenant+vigência | Contratos Finanças (`DespesaEmpenhada`/`PagamentoEfetuado`); `ParametroVigente` | **M** | **ALTO** (premissa furada do design; ver §0.3) |
| **0.1** | **`CalendarioFederal`** — prazos/rótulos federais (SIOPS/SIOPE bimestres, remessa APS mensal 10º dia útil, janela quadrimestral de indicadores, parcelas PNAE/PNATE, AVE/REV, janela Censo SUAS, AgilizaSUAS 1º/mar). **Zero prazo no domínio.** | `CalendarioFederal` (parâmetro tenant+exercício) | `ParametroVigente` (mesmo padrão chave+vigência) | **P** | BAIXO (estrutura) / o conteúdo é parâmetro |
| **0.2** | **`ParecerConselho`** (agregado genérico) — parecer de CMS/CACS-FUNDEB/CMAS (data, resolução, aprovado/ressalva/rejeitado), evidência imutável. | `ParecerConselho` | audit trail imutável; Outbox | **P** | BAIXO |
| **0.3** | **`ApuradorMinimo`** (serviço de domínio base) — entra (receita-base, despesas computáveis, %-meta vigente) → sai (indicador + status). Especializações são 1.x/2.x. **Separar SEMPRE indicador-de-acompanhamento de aferição-de-conformidade.** | `ApuradorMinimo` base + `IndicadorMinimo`/`AferimentoMinimo` | `IExecucaoSetorialReadModel` (0.0); `ParametroVigente` p/ % | **M** | MÉDIO (classificação de despesa computável depende do manual de cada anexo) |
| **0.4** | **`EnvioGatewayBase`** (ACL + Outbox + idempotência + **versão de contrato explícita**) — base de TODOS os gateways de envio. Não envia nada ainda; é o contrato + plumbing. | `EnvioGatewayBase`, `ContratoVersao` | Outbox (pronto); Polly `AddStandardResilienceHandler`; padrão `IRndsGateway`/`ICadUnicoGateway` | **P/M** | BAIXO (padrão) — contratos concretos vêm depois |

**Saída do M7.0:** esqueleto que compila e tem testes de unidade do `ApuradorMinimo` com dados sintéticos.
**Nada de integração externa ainda.** Maior valor, menor risco regulatório.

---

## 2. SAÚDE

> O clínico (Paciente/Atendimento/Regulação) **já existe** no módulo `Saude`. O M7 adiciona a **camada
> fiscal/integração**: FMS por bloco, 15% ASPS, RAG, e os envios (RNDS/SIAPS/CNES/SI-PNI/BNAFAR/SIOPS).
> **Falta crítica de LGPD:** `Saude` **não tem read-trail** (Assistência tem `AcessoProntuario`). Dado
> clínico é sensível → **replicar a trilha antes de qualquer leitura de PEP entrar em produção.**

### Sequência recomendada (SAÚDE)

| # | Sub-workflow | Entidades-núcleo / fluxos | Integração/envio | Painel mínimo | Reusa | Tam. | Risco |
|---|---|---|---|---|---|---|---|
| **S-0** | **Read-trail clínico (LGPD)** — `AcessoRegistroSaude` append-only (quem/quando/motivo-base-legal/o-que-leu), espelhando o padrão da Assistência, sobre Paciente/Atendimento. **Pré-requisito de qualquer leitura sensível.** | trilha de acesso imutável | — | — | **copiar `AcessoProntuario` da Assistência** (mesmo padrão LG-1: user do principal, não do cliente) | **P** | BAIXO (padrão pronto) — **mas bloqueante** |
| **S-1** | **`ApuradorAsps` (15% ASPS) + Anexo 12** — receita impostos+transf. × despesas ASPS computáveis (LC 141 art. 3º computa / art. 4º não) → % vs 15% parametrizável; gera **Anexo 12 do RREO**. Indicador bimestral **informativo** + aferição de conformidade. | apura execução; emite Anexo 12 ao M4 | **SIOPS** (ENVIA Anexo 12, bimestral até 30d) | **15% ASPS** (default, parametrizável) | `ApuradorMinimo` (0.3); `IExecucaoSetorialReadModel` (0.0); `ParecerConselho` | **M** | MÉDIO (classificação ASPS = Manual SIOPS `[a confirmar]`); **canal SIOPS = BAIXO `[a confirmar]`** |
| **S-2** | **`FundoMunicipalSaude` (FMS)** — conta por **2 blocos** (Custeio/Investimento, Port. 3.992/2017) + componentes APS (Port. 3.493/2024). Espelha parcelas do FNS e execução por bloco. | recebe/concilia parcelas; saldo por bloco | FNS fundo a fundo (CONSOME parcelas — leiaute do extrato `[a confirmar]`) | alimenta S-1 | `FonteRecursoVinculado` (0.0/A2); PCASP via contratos Finanças | **M** | MÉDIO (códigos de blocos/componentes `[a confirmar]` — Port. Consolidação 6/2017) |
| **S-3** | **`RelatorioAnualGestao` (RAG)** — dados financeiros (execução/bloco, %ASPS, RAP saúde) + `ParecerConselho` do CMS. | consolida + anexa parecer CMS | **DigiSUS DGMP** (ENVIA/import — aceita import? `[a confirmar]`) | usa S-1 | `ParecerConselho` (0.2); execução de S-1/S-2 | **M** | MÉDIO (fluxo ALTA; import DGMP `[a confirmar]`) |
| **S-4** | **`MonitorEnvioSisabSiaps`** (read model) — status da última remessa de produção APS por unidade/equipe; alerta de risco de perda de repasse. **Destino = SIAPS (parâmetro), não SISAB fixo.** | monitora janela mensal + janela quadrimestral de indicadores | **e-SUS APS → SISAB/SIAPS** (ENVIA, mensal 10º dia útil) | risco de repasse APS | `CalendarioFederal` (0.1) separando prazo mensal × janela quadrimestral | **M** | MÉDIO (Port. 7.639/2025 SIAPS substitui SISAB — **destino parametrizado**) |
| **S-5** | **RNDS (envio FHIR R4)** — `IRndsGateway` já existe como ACL. Implementar concreto: **mTLS (Two-way SSL), token 15 min**, A1 do **Cofre**, headers exatos, homologação antes de produção. Cenários (COVID 24h obrigatório etc.). | envia Bundle por evento (tempo real) | **RNDS** (ENVIA+CONSOME) — **dep. cred. dono + manual** | — | **Cofre** (`IServicoAssinaturaDigital`/A1); `EnvioGatewayBase` (0.4); `IRndsGateway` (scaffold) | **G** | **ALTO** (mTLS/15min/headers/Bundles = `[a confirmar]`; **cred. do dono**) |
| **S-6** | **CNES + SI-PNI + BNAFAR/e-SUS AF** (envios condicionantes de repasse) — CNES mensal (arts. 371/372); SI-PNI via RNDS (Immunization); BNAFAR **diária** (Port. 5.713/2024, XSD). | envios periódicos/por evento | **CNES / SI-PNI / BNAFAR** — leiautes/XSD `[a confirmar]` | risco de bloqueio de repasse | `EnvioGatewayBase` (0.4); S-5 (RNDS p/ SI-PNI) | **G** | **ALTO** (XSD/leiaute/endpoints `[a confirmar]`; quebram em produção e bloqueiam repasse) |

**SISREG:** deixar por último / fora do M7 inicial — **`[a confirmar]` se existe API**; não modelar push sem contrato.

---

## 3. EDUCAÇÃO

> **O Censo é a raiz de tudo** (matrícula/turma/escola já existem no módulo `Educacao`): dele derivam
> Educacenso, FUNDEB, PNAE e PNATE. Errar o Censo propaga erro em todos os repasses. Educação **não tem
> read-trail crítico** (menores: minimização sim, mas não PEP) — não bloqueia como Saúde/Assistência.

### Sequência recomendada (EDUCAÇÃO)

| # | Sub-workflow | Entidades-núcleo / fluxos | Integração/envio | Painel mínimo | Reusa | Tam. | Risco |
|---|---|---|---|---|---|---|---|
| **E-1** | **`ApuradorMde` (25% MDE) + Anexo 8** — execução da **função 12** → % vs 25% parametrizável; gera **Anexo 8 do RREO**. **`AferimentoMdeAnual` (conformidade) separado de `IndicadorMdeBimestral` (acompanhamento)** — evita alarme falso nos bimestres iniciais. Base legal do prazo = **Port. Interm. 424/2016** (NÃO LC 141). | apura; emite Anexo 8 ao M4 | **SIOPE** (ENVIA Anexo 8, bimestral, ordem cronológica) | **25% MDE** (default, parametrizável) | `ApuradorMinimo` (0.3); `IExecucaoSetorialReadModel` (0.0) | **M** | MÉDIO (mapeamento contas→campos SIOPE `[a confirmar]`) |
| **E-2** | **`IndicadorAplicacaoFundeb` (70% folha)** — % pago a profissionais da educação básica sobre receita FUNDEB; rol de profissionais **parametrizável** (divergência TCE/CNM). Cruza com folha do **M5 (RH)**. | apura 70%; concilia cota | **CACS-FUNDEB/SISCACS** (registra parecer) | **70% FUNDEB** (default, parametrizável) | `ApuradorMinimo` (0.3); **contratos do M5/RH** (folha); `ParecerConselho` (0.2) | **M** | MÉDIO (rol 70% `[a confirmar]`; depende de contrato de folha do RH) |
| **E-3** | **`DistribuicaoFundeb`** (informativo/conciliação) — insumo = matrículas ponderadas (Censo + ponderações CIF). **Não recalcula a cota** (quem rateia é o estado/FNDE) — **concilia**. | concilia recebido × esperado | FUNDEB (CONSOME + comprova; anual + saldo) | liga a E-2 | matrícula (agregado existente); `CalendarioFederal` (0.1) | **P/M** | MÉDIO (fatores CIF + VAAF/VAAT/VAAR + saldo 10%/1º tri `[a confirmar]`) |
| **E-4** | **`ExportacaoEducacenso`** — gera o arquivo de migração a partir do agregado de matrícula, parametrizado por **`LeiauteEducacenso`** (contrato externo versionado por ciclo). 1ª etapa (Matrícula Inicial) + 2ª etapa (Situação do Aluno). **BLOQUEADO-SEM-LEIAUTE** (delimitador/estrutura NÃO confirmados; **não declarar "ausência de API" como fato**). | gera arquivo; valida contra layout | **Educacenso/INEP** (ENVIA migração; anual 2 etapas) | — | matrícula (existente); `LeiauteEducacenso` (contrato versionado); `EnvioGatewayBase` (0.4) | **G** | **ALTO** (leiaute = `[a confirmar]`; Censo é raiz → erro propaga a FUNDEB/PNAE/PNATE) |
| **E-5** | **`RepasseFnde` (PNAE/PNATE)** — read model + conciliação: parcelas, saldo por programa, per capita aplicado. **PNAE: agricultura familiar = 45% (Res. 02/03/2026), NÃO 30% — parâmetro versionado** (30%→45%); 85% in natura; até 8 parcelas (fev–set). **PNATE: 2 parcelas (mar/ago); per capita/FNRM.** | concilia parcelas; valida %AF | **PNAE/PNATE/FNDE** (CONSOME + presta contas) | — | `CalendarioFederal` (0.1); `ParametroVigente` (per capita, %AF) | **M** | MÉDIO (per capita/%AF/FNRM = `[a confirmar]`; **45% hardcoded = falso "conforme" → TCE**) |

---

## 4. ASSISTÊNCIA SOCIAL

> CadÚnico é a base, **operado online no app do MDS/Dataprev** — o ERP é **read/sync model + fila de
> pendências**, nunca substitui o cadastro federal. O operacional (`Familia`/`Prontuario`/`Beneficio`/
> `ParametroVigente`/`ICadUnicoGateway`) **já existe**, **incluindo o read-trail** (`AcessoProntuario`).
> **Sem mínimo % constitucional** — o painel é de **execução por piso + saldos + estimativa IGD**.
> **Correção jurídica obrigatória:** benefício eventual **não** tem teto de "1/4 SM da LOAS" (revogado pela
> Lei 12.435/2011) — critério de renda é **100% lei/decreto municipal + CMAS**, parâmetro por tenant, **sem
> default federal embutido**. O README atual ainda diz "≤ ½ SM"/"BPC < ¼ SM" → **rever como parâmetro**.

### Sequência recomendada (ASSISTÊNCIA)

| # | Sub-workflow | Entidades-núcleo / fluxos | Integração/envio | Painel | Reusa | Tam. | Risco |
|---|---|---|---|---|---|---|---|
| **A-0** | **Correção do critério de benefício eventual** — remover teto federal "1/4 SM"; elegibilidade 100% derivada de parâmetro municipal (CMAS+LOA) por tenant+vigência. Ajustar README + `CriterioElegibilidade`. **Risco jurídico de indeferir indevidamente.** | regra de elegibilidade parametrizada | — | — | `ParametroVigente` (já existe); `Beneficio` (existente) | **P** | BAIXO (mas **corrige risco jurídico ALTO** se não feito) |
| **A-1** | **`FundoMunicipalAssistencia` (FMAS)** — conta por **4 blocos** (PSB/PSE/Gestão SUAS/Gestão PBF-CadÚnico, Port. 1.043/2024) + por **piso** (Básico Fixo/Variável, Fixo MC, Acolhimento). Espelha Parcelas Pagas/Saldos (do SUASWeb) + **reprogramação de saldo** (facilitada pela 1.043/2024). | concilia parcelas; saldo por piso/bloco | FNAS fundo a fundo (CONSOME) | execução por piso | `FonteRecursoVinculado` (0.0/A2); PCASP (contratos Finanças) | **M** | MÉDIO (valores de pisos/tabela NOB-SUAS `[a confirmar]`) |
| **A-2** | **`RegistroMensalAtendimento` (RMA)** — gerado **a partir do Prontuário/atendimentos** (já existe), evita dupla digitação; fechável/auditável por `(Unidade, Competencia)`. | deriva do prontuário; fecha competência | **RMA → SAGI/MDS** (ENVIA, mensal até 30d) | — | `ProntuarioSuas`/atendimentos (existentes); `CalendarioFederal` (0.1) | **M** | MÉDIO (campos por questionário `[a confirmar]`; API `[a confirmar]`) |
| **A-3** | **`CensoSuas`** — **pré-populado** do cadastro de unidades/lotação RH (**M5**) + RMA, por `(Unidade, AnoBase)`. | pré-popula; exporta | **Censo SUAS → SAGI** (ENVIA, anual 2º sem.) | — | RMA (A-2); contratos M5/RH; `CalendarioFederal` (0.1) | **M** | MÉDIO (campos por ano `[a confirmar]`) |
| **A-4** | **`PrestacaoContasAgilizaSuas`** — Demonstrativo Sintético **contínuo** (substitui SUASWeb; + BB Gestão Ágil), prazo **1º/mar** do exercício seguinte, + `ParecerConselho` do CMAS. **Prazos prorrogados (Port. SNAS 132/2025) → `CalendarioFederal`, nunca hardcoded.** | consolida execução; anexa parecer CMAS | **AgilizaSUAS (+BB Gestão Ágil)** (ENVIA/presta contas, contínuo 1º/mar) | — | `ParecerConselho` (0.2); A-1 (execução/piso); `CalendarioFederal` | **M** | MÉDIO (fluxo ALTA; contrato/API AgilizaSUAS = BAIXO `[a confirmar]`) |
| **A-5** | **`EstimativaIgd`** (read model) — estima IGD-M/IGD-SUAS dos dados internos (taxa de atualização cadastral, condicionalidades, status prestação, parecer CMAS) = `FatorI×II×III×IV`. **Gestão preditiva de receita, não cálculo oficial.** | estima fatores | — (deriva de CadÚnico/PBF/prestação) | estimativa IGD | A-4; `ICadUnicoGateway`/`ICadUnicoReadModel` (existentes) | **P/M** | MÉDIO (pesos exatos = IN 091 `[a confirmar]`) |
| **A-6** | **PBF condicionalidades** (acompanha, não paga) — Saúde semestral / Educação bimestral; frequências **60%/75%** (Decreto 12.064/2024 + Port. 1.058/2025) **parametrizáveis**. BPC = read model (não paga). | acompanha condicionalidades | **SICON/SIGPBF/Presença** (acompanha) | — | `ParametroVigente` (frequências); `EnvioGatewayBase` (0.4) | **M** | MÉDIO (APIs SICON/Presença = `[a confirmar]`) |

**CadÚnico (sync/AVE-REV):** o gateway/read-model já existem; o **sync de arquivo fica BLOQUEADO-SEM-LEIAUTE**
(layout V7/V8 AVE/REV `[a confirmar]`; rótulo AVE/REV é **calendário anual**, não constante).

---

## 5. Ordem geral recomendada (qual setor primeiro, e por quê)

> Princípio: **eixo fiscal compartilhado primeiro** (maior valor, menor risco, reusa Finanças/M4/Cofre);
> **integrações de envio por último e BLOQUEADAS-SEM-LEIAUTE** (gerador compila; transmissão fiel só com o doc).

**Fase 1 — Núcleo (transversal, destrava os 3):** `M7.0` inteiro (0.0→0.4). **Começar por 0.0** (decisão
A1×A2): sem a fonte de dados de execução classificada, nenhum apurador funciona.

**Fase 2 — Mínimos constitucionais (a entrega de maior valor):**
1. **Saúde S-1 (15% ASPS / Anexo 12)** e **Educação E-1 (25% MDE / Anexo 8)** em paralelo — mesmo
   `ApuradorMinimo`, dois %; ambos só **estendem o M4** (Anexos no mesmo PAD mensal, sem envio separado).
2. **Educação E-2 (70% FUNDEB)** — depende de contrato de folha do **M5/RH**; encaixar quando o RH expuser.
> Assistência **não entra aqui** (sem mínimo %) — por isso o eixo de mínimos é **Saúde+Educação primeiro**.

**Fase 3 — Fundos por bloco/piso + conselhos:** `S-2` (FMS), `A-1` (FMAS), `E-3` (FUNDEB conciliação);
depois prestação/conselhos `S-3` (RAG), `A-4` (AgilizaSUAS), `E-2`/CACS. Tudo reusa `ParecerConselho` (0.2).
**Fazer a correção jurídica `A-0` o quanto antes** (é P e tira um risco jurídico).

**Fase 4 — Censo/produção (base dos repasses, BLOQUEADO-SEM-LEIAUTE):** `E-4` (Educacenso), `A-2` (RMA),
`A-3` (Censo SUAS), `S-4` (monitor SIAPS). Geradores parametrizados pelo leiaute; compilam sem o doc.

**Fase 5 — Integrações de envio em tempo real / barramentos (mais frágil, depende de cred. do dono):**
`S-5` (RNDS), `S-6` (CNES/SI-PNI/BNAFAR), `A-6` (SICON/Presença), `E-5` (PNAE/PNATE conciliação fina).
**Só com manual vigente + homologação + testes de contrato.**

**Por que Saúde "primeiro" dentro dos mínimos, mas Assistência "primeiro" para tirar risco:**
o **mínimo** (maior valor) puxa **Saúde+Educação**; mas a **correção `A-0`** (Assistência) é barata e remove
risco jurídico — então roda cedo, em paralelo, fora da fila de mínimos.

---

## 6. O que depende de CREDENCIAIS DO DONO (não dá pra terminar sem)

- **RNDS (S-5, e SI-PNI em S-6):** certificado **A1 e-CNPJ ICP-Brasil do município** no **Cofre/Key Vault**,
  + acesso ao **ambiente de homologação** da RNDS (mTLS, token 15 min). Sem isso, só simulado.
- **SIOPS / SIOPE / AgilizaSUAS / SICON-Presença / Educacenso:** credenciais de acesso aos sistemas
  federais do município **+ o leiaute/manual técnico de cada um** (a maioria `[a confirmar]`) — alguns
  podem ser **só web manual** (sem API), o que muda o sub-workflow de "envio" para "geração + upload".
- **CadÚnico/Dataprev:** acesso ao app federal + **layout V7/V8 AVE/REV** para qualquer sync de arquivo.
- **TCE-RS (já no M4):** A1 do município (Cofre) — reuso; os Anexos 8/12 saem no **mesmo PAD mensal**.
- **Lei/decreto municipal** do tenant piloto (**Maximiliano de Almeida/RS**): tabela de **benefícios
  eventuais** (`A-0`) e quaisquer %/critérios locais — é **parâmetro do tenant**, fornecido pelo dono.

---

## 7. Bloqueadores de fidelidade `[a confirmar — obter doc oficial]` (resumo por setor)

- **Saúde:** Manual SIOPS + tabela classificação ASPS (LC 141 arts. 3º/4º) + canal SIOPS; códigos de
  blocos/componentes (Port. Consolidação 6/2017 + 3.992/2017 + 3.493/2024); Manual Barramento RNDS + Guia
  "Modelos" (Bundles FHIR) + headers; Port. 7.639/2025 (SIAPS) + manual SIAPS; CNES (Port. 01/2017 arts.
  371/372); web service BNAFAR (XSD, Port. 5.713/2024); DigiSUS DGMP aceita import?; existência de API SISREG.
- **Educação:** **leiaute Educacenso do ciclo** (delimitador/estrutura) + confirmar API; leiaute SIOPE
  (contas→campos); **Res. PNAE 02/03/2026 (45% AF / 85% in natura)** + tabela per capita; per capita/FNRM
  PNATE (Res. 05/2024); fatores CIF + VAAF/VAAT-MIN + saldo 10% (art. 25 Lei 14.113) + rol 70%; instrumento
  de parecer do CACS.
- **Assistência:** Port. 1.043/2024 + Manual AgilizaSUAS + Port. SNAS 132/2025 (prazos); layout V7/V8
  CadÚnico; Manuais RMA; questionários Censo SUAS do ano; tabela de pisos/NOB-SUAS; Decreto 12.064/2024 +
  Port. 1.058/2025 (PBF); IN 091 (IGD-M); **lei municipal de benefícios eventuais do tenant**; APIs
  SICON/SIGPBF/Presença.

> **Engenharia (não negociar, das verificações):** RNDS = **mTLS + token 15 min** (não 30); remessa APS =
> **mensal 10º dia útil** (≠ janela quadrimestral de indicadores); PNAE AF = **45%** (≠ 30%); benefício
> eventual **sem teto 1/4 SM**; **sistema-destino sempre parametrizado** (SISAB→SIAPS, HÓRUS→e-SUS AF).
