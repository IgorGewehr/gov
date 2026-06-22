# M7 — Verificação de Fatos: pesquisa-educacao.md (auditoria cética)

> Auditoria independente de `pesquisa-educacao.md` contra **fontes oficiais** (gov.br, INEP,
> FNDE, Planalto, STF, MEC). Cada afirmação verificável foi classificada:
> **CONFIRMADO** (fonte oficial bate) · **PLAUSÍVEL-SEM-FONTE** (consistente, mas sem
> confirmação numérica/leiaute oficial obtido) · **INCERTO** (não confirmado, divergência ou
> desatualização detectada).
>
> Data da verificação: **2026-06**. Atenção: várias normas FNDE foram **atualizadas em 2026**.

---

## Tabela de veredictos

| # | Afirmação na pesquisa | Veredicto | Fonte / observação |
|---|---|---|---|
| 1 | Censo Escolar/Educacenso é anual, declaratório, obrigatório (Dec. 6.425/2008, INEP) | **CONFIRMADO** | INEP / gov.br |
| 2 | Data de referência = última quarta-feira de maio | **CONFIRMADO** | INEP (Censo 2025: 28/05, última quarta) |
| 3 | 1ª etapa (Matrícula Inicial): coleta estab./turmas/alunos/gestores/profissionais; declaração até 31/jul (ciclo 2025) | **CONFIRMADO** | INEP / Undime — coleta até 31/07/2025 |
| 4 | 2ª etapa (Situação do Aluno): rendimento+movimento ao fim do ano; declaração até ~30/mar (2025 reprogramado, iniciou fev/2026) | **CONFIRMADO (com correção de data)** | INEP/Agência Brasil: coleta 2ª etapa do ciclo 2025 foi **02/02 a 13/03/2026** — ver Risco/observação. "Até 30/03" não bate exatamente; **parametrizar por exercício** |
| 5 | Migração por arquivo TXT, campos delimitados por pipe `\|`; fluxo exportação→atualizar código INEP→importação | **PLAUSÍVEL-SEM-FONTE** | Fluxo e existência da migração são oficiais (INEP publica "Etapas e Instruções para a Migração"); **delimitador pipe e estrutura de registro NÃO foram confirmados campo-a-campo** — leiaute é bloqueador |
| 6 | NÃO há API/webservice de envio automático ao Educacenso (upload manual) | **INCERTO** | Não confirmado nem refutado por fonte oficial. Tratar como **a confirmar** — não afirmar ausência de API como fato |
| 7 | SIOPE coleta orçamentos de educação (FNDE); calcula indicador e identifica entes abaixo do mínimo | **CONFIRMADO** | FNDE |
| 8 | Mínimo 25% MDE (impostos+transferências) — CF art. 212 | **CONFIRMADO** | CF art. 212 (STF/Planalto). 25% mín. p/ Estados/DF/Municípios |
| 9 | SIOPE periodicidade BIMESTRAL, prazo até 30 dias após o bimestre, alinhado ao RREO (CF 165§3; LRF art. 52; LC 141/2012) | **CONFIRMADO** | Portaria Interministerial 424/2016 + CNM/Undime. Base legal LC 141/2012 é da **saúde (SIOPS)**; para SIOPE a base é Port. Interm. 424/2016 + LRF/CF — ver Risco |
| 10 | Transmissão em ordem cronológica (não envia bimestre sem o anterior) | **CONFIRMADO** | FNDE/CNM |
| 11 | Descumprir → restrição a transferências voluntárias/convênios (pendência no CAUC) | **CONFIRMADO** | CNM/Undime — registro no CAUC |
| 12 | Mínimo de 25% aferido no encerramento do exercício; envio é bimestral | **CONFIRMADO** | consistente com sistemática RREO/MDE |
| 13 | PNAE e PNATE: repasse fundo a fundo automático, base = Censo do ano anterior, conselho de controle social, prest. contas SIGPC | **CONFIRMADO** | FNDE |
| 14 | PNAE: valor = per capita × matrículas (Censo ano ant.) × dias atendimento; per capita varia por etapa/modalidade | **CONFIRMADO (estrutura)**; valores = parâmetro | FNDE. Tabela per capita vigente continua **a obter** |
| 15 | PNAE: até 8 parcelas anuais entre fev e set (Res. CD/FNDE 07/2024 reduziu de 10→8, sem mudar total) | **CONFIRMADO** | Res. CD/FNDE nº 7, de 02/05/2024, art. 18: **8 parcelas, fev–set**, cada uma ≥20 dias letivos |
| 16 | PNAE: mínimo 30% em agricultura familiar (Lei 11.947/2009 art. 14) | **CONFIRMADO na LEI; DESATUALIZADO na prática 2026** | Lei 11.947/2009 art. 14 fixa **30%**. MAS nova **Resolução FNDE de 02/03/2026** elevou o piso operacional para **45%** (+ regra de 85% in natura). Ver Risco nº 1 |
| 17 | PNATE: valor = per capita × alunos ed. básica pública rural que usam transporte (Censo ano ant.) | **CONFIRMADO** | FNDE |
| 18 | PNATE: per capita pelo FNRM = % pop. rural (IBGE) + área do município (IBGE) + % pop. abaixo linha pobreza (IPEADATA) + IDEB (INEP) | **CONFIRMADO** | FNDE (FAQ PNATE) — os 4 fatores conferem |
| 19 | PNATE: Res. CD/FNDE 05/2020 (critérios); repasse em 2 parcelas (mar e ago) conf. Res. CD/FNDE 05/2024 | **CONFIRMADO** | Res. CD/FNDE nº 5, de 09/04/2024: **2 parcelas, preferencialmente mar e ago** (1ª parcela 2026 já repassada) |
| 20 | FUNDEB: fundo contábil estadual, redistribui por matrículas (Censo), com complementação da União (Lei 14.113/2020, EC 108/2020) | **CONFIRMADO** | Planalto / FNDE / MEC |
| 21 | 3 modalidades de complementação: VAAF, VAAT, VAAR | **CONFIRMADO** | MEC/CNM. Repartição da complementação da União: VAAF 10% + VAAT 10,5% + VAAR 2,5% (= 23% do total) |
| 22 | Ponderações por etapa/modalidade/jornada/tipo de estabelecimento, definidas anualmente pela CIF | **CONFIRMADO (mecanismo)**; fatores = parâmetro | MEC ("Ponderação das Matrículas") + CIF. Tabela de fatores do exercício **a obter** |
| 23 | ≥70% do Fundeb na remuneração de profissionais da educação básica (EC 108 ampliou de magistério → profissionais) | **CONFIRMADO** | Lei 14.113/2020 (+ Lei 14.276/2021). Subiu de 60%→70% |
| 24 | Rol amplo de profissionais (apoio técnico/admin/operacional) | **CONFIRMADO (com divergência interpretativa)** | Lei 14.276/2021 lista docentes, suporte pedagógico, direção/admin, planejamento, inspeção, supervisão, orientação, coordenação, apoio técnico/admin/operacional. TCEs (SC/ES) divergem em detalhes de enquadramento → manter parametrizável |
| 25 | Saldo: até 10% no 1º trimestre do ano seguinte (regra de saldo Lei 14.113/2020) | **CONFIRMADO** | Lei 14.113/2020 **art. 25, §3º**: até **10%**, utilizável **até o 1º trimestre (até abril)** do exercício imediatamente subsequente |
| 26 | Controle social: CACS-Fundeb; CAE (PNAE) | **CONFIRMADO** | FNDE |
| 27 | BNCC normativa por etapa/modalidade; áreas do conhecimento | **CONFIRMADO** | MEC |
| 28 | Carga horária mínima: 800h / 200 dias letivos (LDB art. 24) | **CONFIRMADO** | LDB 9.394/96 art. 24, I: **mín. 800h e 200 dias** de efetivo trabalho escolar (1.000h no ensino médio). "Anos iniciais 800h em 40 semanas" é detalhamento — ok |
| 29 | Matriz/currículo elaborado pela rede municipal (BNCC = mínimos); sem leiaute nacional | **CONFIRMADO** | LDB/MEC — currículo é competência da rede; parametrizar por tenant |
| 30 | Nota 2025/2026: PNAE antecipado/concluído + novas regras de reprogramação de saldos (a partir de 2027) | **PLAUSÍVEL-SEM-FONTE** | Notícias/Conviva mencionam reprogramação de saldos PNAE/PNATE; regra "a partir de 2027" e detalhes (10º dia útil de fev, contas zeradas) **a confirmar na Resolução específica** |

---

## Placar

- **CONFIRMADO:** 22 itens (incl. 3 com correção/ressalva menor: #4 data, #9 base legal, #28 detalhe)
- **PLAUSÍVEL-SEM-FONTE:** 3 itens (#5 leiaute pipe, #22 fatores como mecanismo já contado em confirmados — manter; #30 reprogramação)
- **INCERTO / DESATUALIZADO:** #6 (ausência de API), #16 (30% AF — superado por 45% em 2026)

Em síntese: **a base legal e os fluxos da pesquisa estão sólidos**. As fragilidades são
**(a) números/leiautes que dependem de doc oficial vigente** (corretamente marcados como
parâmetro na pesquisa) e **(b) uma desatualização real** (agricultura familiar) decorrente de
norma publicada em março/2026 — posterior à redação da pesquisa.

---

## O que EXIGE documento oficial antes de implementar fiel (bloqueadores)

1. **Leiaute Educacenso** (caderno de conceitos + estrutura de registros/campos do TXT) do
   ciclo vigente — INEP. Confirmar campo-a-campo o **delimitador** (pesquisa assume pipe `|`,
   não confirmado oficialmente) e o layout de cada registro.
2. **Existência/ausência de API de envio** ao Educacenso — não modelar "ausência de API" como
   fato; obter confirmação INEP antes de decidir geração-de-arquivo-only.
3. **Resolução PNAE vigente (a de 02/03/2026)** — obter texto: confirmar **45% agricultura
   familiar**, **85% in natura / 10% teto ultraprocessados a partir de 2026**, e se mantém 8
   parcelas fev–set. **Atualizar a pesquisa** (que ainda cita 30%).
4. **Tabela per capita PNAE** vigente por etapa/modalidade.
5. **Per capita/FNRM PNATE** vigente + Res. CD/FNDE 05/2024 (texto), para o parâmetro por
   município e calendário das 2 parcelas.
6. **Fatores de ponderação FUNDEB** (resolução CIF do exercício) + VAAF-MIN/VAAT-MIN (Portaria
   Interministerial MEC/MF — há Port. Interm. MEC/MF nº 5/2025 alterando regras; obter).
7. **Mapeamento de contas → campos SIOPE** (leiaute SIOPE/FNDE) e confirmação da base
   normativa do prazo (Port. Interm. 424/2016, não LC 141/2012 que é da saúde).
8. **Regras de reprogramação de saldos PNAE/PNATE** (vigência 2027) — Resolução específica.

---

## 3 RISCOS (prioritários)

**RISCO 1 — Desatualização de percentual legal já materializada (agricultura familiar PNAE).**
A pesquisa cita **30%** (Lei 11.947/2009 art. 14), correto na lei-base, mas a **Resolução FNDE
de 02/03/2026** elevou o piso operacional para **45%** e adicionou regra de **85% in natura /
teto de ultraprocessados**. Implementar a validação de controle do PNAE com 30% hardcoded
geraria **falso "conforme"** para um município que está abaixo dos 45% exigidos em 2026 — risco
direto de apontamento de TCE. **Mitigação:** o percentual de AF é **parâmetro versionado por
exercício** (`vigência` 30% → 45%), nunca constante; e a pesquisa deve ser corrigida.

**RISCO 2 — Leiaute Educacenso assumido sem confirmação (delimitador pipe + estrutura).**
A pesquisa marca como MÉDIA, mas o **delimitador `|` e a estrutura de registros não foram
confirmados em fonte oficial** nesta auditoria. Construir o `EducacensoExportador` contra um
leiaute presumido produz um arquivo que **o sistema do INEP rejeita na importação** — e como o
Censo é a raiz de FUNDEB/PNAE/PNATE, um erro de migração propaga para todos os repasses.
**Mitigação:** tratar o leiaute como **contrato externo versionado** (`LeiauteEducacenso` por
ciclo), implementar o gerador parametrizado pelo layout, e **não declarar "ausência de API"**
sem confirmar — o canal de envio (upload vs. webservice) é decisão de arquitetura sensível.

**RISCO 3 — Confusão de base legal/periodicidade SIOPE × SIOPS e prazo bimestral.**
A pesquisa cita **LC 141/2012** como base do SIOPE; LC 141/2012 rege o **mínimo da saúde
(SIOPS)**, não a educação. O prazo bimestral do SIOPE ancora-se em **Portaria Interministerial
424/2016 + LRF art. 52 + CF art. 165§3 (RREO)**. Além disso, o **mínimo de 25% é aferido no
encerramento do exercício**, não bimestralmente — modelar o indicador MDE bimestral como
**informativo/acompanhamento** e o **veredicto de conformidade como anual** evita um alarme
falso de "abaixo de 25%" em bimestres iniciais. **Mitigação:** separar `IndicadorMdeBimestral`
(acompanhamento) de `AferimentoMdeAnual` (conformidade), e corrigir a citação de base legal.
