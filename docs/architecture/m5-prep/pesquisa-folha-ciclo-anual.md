# M5 — Pesquisa: CICLO ANUAL da Folha Pública (13º, Férias, Rescisão)

> **Status:** pesquisa de fundamentação (dirige a Rules-as-Code do *ciclo anual* do motor de folha do M5).
> **Pré-requisito provado:** o motor de folha **MENSAL** já existe (INSS/IRRF/RPPS parametrizáveis, rubricas
> com incidências eSocial, tabelas legais versionadas por `competencia`/`tenantId`, ponto). Ver
> `pesquisa-folha-calculo.md`. Este documento adiciona **APENAS** os três cálculos do ciclo anual:
> **13º salário, férias e rescisão** — **reusando** o mesmo motor (mesmas rubricas, mesmas tabelas).
> **Constituição §16 (REGRA DE OURO):** nada hardcoded. Todo número tem FONTE (URL) e **competência**;
> alíquotas/faixas/valores vivem em tabela versionada. O código aplica *fórmulas*; *números* são dado.
> **Reprodutibilidade:** cálculo é **função pura** de (vínculo, eventos, tabela da competência) — **sem
> relógio**. Datas (admissão, afastamentos, desligamento, competência de pagamento) são **entrada**, nunca
> `DateTime.Now`. Pontos que dependem de **lei municipal** ficam `[a confirmar — <documento>]`.

---

## 0. Sumário executivo (decisões que o ciclo anual precisa tomar)

1. **13º (gratificação natalina)** = `1/12 × remuneração de dezembro × (meses no ano com ≥15 dias)`.
   Tributação **EXCLUSIVA na fonte**: INSS e IRRF do 13º calculados sobre **base própria, separada da
   folha mensal**. 1ª parcela = adiantamento SEM desconto (até 30/nov); 2ª parcela com **todo** o INSS+IRRF
   do 13º (até 20/dez).
2. **Férias** = remuneração do período + **1/3 constitucional** (CF art. 7º XVII). IRRF sobre férias
   gozadas + terço incide pela **tabela progressiva mensal** (cálculo **separado**, mas **NÃO** exclusivo —
   compõe o ajuste anual). **Abono pecuniário** (venda de até 1/3, CF art. 7º XVII / CLT art. 143) é
   **isento** de IR e de contribuição previdenciária (verba indenizatória).
3. **Rescisão / desligamento** = composição de verbas conforme **tipo** de desligamento e **regime**
   (estatutário × celetista): saldo de salário, 13º proporcional, férias vencidas + proporcionais + 1/3,
   aviso prévio (quando aplicável), FGTS+40% (só celetista). Pagamento dentro do prazo legal.
4. **Estatutário × celetista** é atributo **do vínculo** (igual ao regime previdenciário). Determina:
   FGTS (só celetista), aviso prévio (só celetista), multa 40% (só celetista), e a fonte da regra de
   férias/13º (CLT p/ celetista; **estatuto municipal** p/ estatutário — `[a confirmar]`).

> **Piloto Maximiliano de Almeida/RS:** servidores **estatutários** (Lei municipal 327/2008) +
> comissionados/temporários, regime **RGPS/INSS** (município **não tem RPPS** — ver `RPPS-maximiliano.md`).
> Estatutário **não tem FGTS nem aviso prévio**; o 13º e as férias seguem a **CF + estatuto municipal**.

---

## 1. 13º SALÁRIO (gratificação natalina) — Lei 4.090/1962 + Lei 4.749/1965 + Dec. 57.155/1965

### 1.1 Fundamento e fórmula (proporcionalidade por avos)
- **Base constitucional:** CF/1988 **art. 7º, VIII** — 13º salário com base na remuneração integral ou no
  valor da aposentadoria (estendido ao servidor público por simetria/estatuto).
- **Lei 4.090/1962 (Lei do 13º):** a gratificação corresponde a **1/12 (um doze avos) da remuneração
  devida em dezembro, por mês de serviço do ano correspondente**. **Fração igual ou superior a 15 dias**
  de trabalho no mês conta como **mês integral**; fração inferior a 15 dias é desprezada.
- **Proporcionalidade:** devida proporcionalmente em rescisão/extinção do vínculo e na aposentadoria,
  mesmo que ocorra antes de dezembro (mesma regra de avos).
- **Fórmula (motor):** `valor13 = (remuneracaoBaseDezembro / 12) × nAvos`, onde
  `nAvos = Σ meses do ano-calendário em que houve ≥15 dias de vínculo/efetivo exercício`.
  A `remuneracaoBaseDezembro` é a soma das **rubricas que integram o 13º** (salário + médias de variáveis
  habituais, conforme natureza da rubrica) — **reusa a tabela de rubricas/incidências do motor mensal**.

### 1.2 Duas parcelas (prazos)
- **1ª parcela (adiantamento):** **50%** da remuneração (sem descontos), paga entre **fevereiro e 30 de
  novembro** (Lei 4.749/65 art. 2º; Dec. 57.155/65). **Não** sofre INSS nem IRRF.
- **2ª parcela:** **até 20 de dezembro**, com a apuração e retenção de **INSS e IRRF do 13º** (sobre o
  total), descontando-se o adiantamento já pago.
- **Motor:** as datas (30/nov, 20/dez) e o percentual da 1ª parcela são **parâmetros** (a 1ª parcela pode
  ser antecipada por requerimento/lei local). NÃO usar relógio: a competência/data de pagamento é entrada.

### 1.3 Incidência tributária do 13º — **REGRA PRÓPRIA, base separada**
- **Tributação EXCLUSIVA na fonte** (Lei 7.713/1988, **art. 12-A**): o IRRF do 13º é calculado **separado
  dos demais rendimentos** do mês de dezembro, pela **tabela progressiva mensal**, **sem** desconto
  simplificado e **sem** somar à base do salário de dezembro. Por ser exclusiva/definitiva, entra na ficha
  de "Rendimentos Sujeitos à Tributação Exclusiva" e **não** se soma no ajuste anual.
- **Base do IRRF do 13º** = valor bruto do 13º − **INSS/RPPS do próprio 13º** − dependentes (R$/dependente
  da competência) − pensão alimentícia. **Não** há redutor/simplificado e a retenção é devida ainda que
  inferior a R$ 10,00 (não se aplica a dispensa do art. 67 da Lei 9.430/96 — IN RFB 1.500/2014).
- **INSS/RPPS do 13º:** incide sobre o 13º com **base de cálculo separada** da remuneração mensal (tabela
  progressiva própria aplicada isoladamente ao 13º). Para RPPS, alíquota/base seguem a **lei municipal**.
- **Motor:** evento de competência **"13"** com `baseSeparada = true`. Aplica as **MESMAS tabelas**
  (INSS/RPPS §1–2 e IRRF §3 da pesquisa mensal), mas sobre a **base do 13º**, **sem** somar ao mês.

---

## 2. FÉRIAS — CF art. 7º XVII + CLT arts. 129–145 (celetista) / estatuto municipal (estatutário)

### 2.1 Períodos aquisitivo e concessivo
- **Período aquisitivo:** a cada **12 meses** de vínculo (efetivo exercício) o servidor/empregado adquire
  direito a **30 dias** de férias (CLT art. 130 — para celetista; para estatutário, conforme estatuto).
- **Período concessivo:** o empregador tem os **12 meses seguintes** para conceder o gozo (CLT art. 134).
  Ultrapassado o concessivo sem gozo → **pagamento em dobro** (CLT art. 137) — `[a confirmar]` se o
  estatuto municipal replica a dobra.
- **Faltas injustificadas reduzem o número de dias** de férias na escala do art. 130 (CLT) — regra a
  parametrizar para celetistas; estatutários seguem o estatuto.

### 2.2 Remuneração de férias + 1/3 constitucional
- **CF art. 7º, XVII:** gozo de férias com remuneração **acrescida de, no mínimo, 1/3** sobre o salário
  normal (terço constitucional). `remuneracaoFerias = remuneraçãoNormal_do_período + (1/3 × base)`.
- A base do terço usa as rubricas que integram férias (salário + médias habituais) — **reusa rubricas**.

### 2.3 Abono pecuniário (venda de até 1/3)
- **CLT art. 143:** o empregado pode **converter 1/3 das férias em abono pecuniário** (vender até 10 dias),
  com requerimento **até 15 dias antes** do término do período aquisitivo. O abono inclui o **terço
  constitucional proporcional** sobre os dias vendidos.
- **Tributação do abono pecuniário:** **isento de IR** e **fora da base previdenciária** (verba
  indenizatória — Súmula 386 STJ aplica-se a férias/abono indenizados; Solução COSIT trata o terço sobre
  abono no curso do contrato — divergência a parametrizar como **flag de incidência da rubrica**).
- **Estatutário:** a existência/condições do abono pecuniário dependem do **estatuto municipal** `[a confirmar]`.

### 2.4 Incidências sobre férias GOZADAS (≠ indenizadas)
- **IRRF:** incide sobre **férias gozadas + terço** pela **tabela progressiva mensal**, calculado
  **separadamente** dos demais rendimentos do mês — mas é **tributação normal (NÃO exclusiva)**: soma no
  ajuste anual (difere do 13º). (PGFN/STJ: IR incide sobre o terço de **férias gozadas**.)
- **Previdência sobre o 1/3:** STF **Tema 985** / STJ **REsp 1.559.926** — incide contribuição
  **patronal** sobre o terço; a incidência sobre a contribuição do **segurado/RPPS** deve ser verificada
  `[a confirmar — situação pós-Tema 985 para a cota do segurado]`. Modelar como **flag de incidência**.
- **Férias INDENIZADAS** (não gozadas, pagas na rescisão) + terço: **isentas de IR** (Súmula 386 STJ) e
  **fora da base previdenciária** → tratar como rubrica indenizatória (incidência desligada).

> **Motor:** férias = evento com rubricas `Ferias` (+ médias), `TercoFerias` (1/3) e, se houver,
> `AbonoPecuniario` + `TercoAbono`. Cada rubrica carrega `incideCP/incideIRRF/incideRPPS` (eSocial) que
> liga/desliga a base — **gozadas tributam; indenizadas/abono não**. Cálculo separado do mês, sem relógio.

---

## 3. RESCISÃO / DESLIGAMENTO — verbas por tipo e por regime

### 3.1 Verbas componentes (catálogo)
| Verba | Conteúdo | Observação |
|---|---|---|
| **Saldo de salário** | dias trabalhados no mês do desligamento (proporcional) | sempre devido |
| **13º proporcional** | `1/12 × base × avos do ano` (regra §1.1) | INSS/IRRF do 13º (regra própria, §1.3) |
| **Férias vencidas + 1/3** | períodos aquisitivos completos não gozados | **indenizadas = isentas IR** (Súmula 386) |
| **Férias proporcionais + 1/3** | avos do período aquisitivo em curso | **indenizadas = isentas IR** (Súmula 386) |
| **Aviso prévio** | 30 dias + 3/ano (máx. 60) — Lei 12.506/2011 | **só celetista**; estatutário não tem |
| **FGTS + multa 40%** | depósitos + 40% na dispensa sem justa causa | **só celetista**; estatutário não tem |

### 3.2 Matriz: tipo de desligamento × verbas (referência celetista)
- **Dispensa sem justa causa:** saldo + 13º proporcional + férias vencidas e proporcionais + 1/3 + aviso
  prévio + FGTS + **multa 40%**.
- **Pedido de demissão / exoneração a pedido:** saldo + 13º proporcional + férias vencidas e proporcionais
  + 1/3; **sem** multa 40% e **sem** aviso a receber (devolve aviso se não cumprido, p/ celetista).
- **Justa causa:** saldo + **férias vencidas + 1/3** (vencidas sempre devidas); **sem** 13º proporcional,
  **sem** férias proporcionais, **sem** aviso/multa.
- **Distrato (acordo, CLT art. 484-A):** 13º e férias integrais; aviso e multa 40% **pela metade**;
  saque parcial FGTS — `[a confirmar]` se aplicável a vínculos públicos celetistas do município.
- **Aposentadoria / falecimento:** saldo + 13º proporcional + férias vencidas/proporcionais + 1/3.

### 3.3 Estatutário × celetista (o que muda)
- **Estatutário (regra do piloto):** **NÃO** tem FGTS, **NÃO** tem multa 40%, **NÃO** tem aviso prévio, e
  **não** há "rescisão" celetista — usa-se **exoneração/vacância** do estatuto (CF art. 41; Lei municipal
  327/2008). Devidos: **saldo + 13º proporcional + férias vencidas/proporcionais + 1/3 indenizados**.
  Regras finas (avos, dobra, indenização de licença-prêmio não gozada) = **estatuto municipal** `[a confirmar]`.
- **Comissionados/temporários:** comissionado **estatutário** segue o regime estatutário (sem FGTS/aviso);
  comissionado **celetista** segue verbas celetistas (FGTS+40%, aviso). É atributo do vínculo `[a confirmar]`
  qual o enquadramento dos comissionados/temporários de Maximiliano de Almeida.
- **Prazo de pagamento:** celetista — **10 dias** corridos do término (CLT art. 477, §6º). Estatutário —
  conforme estatuto/lei municipal `[a confirmar]`.

> **Motor:** `Desligamento { tipo, regime, dataDesligamento(entrada), avos }` → compõe um conjunto de
> rubricas a partir do **mesmo catálogo**; flags `temFGTS/temAviso/temMulta` derivam de `regime`+`tipo`.
> IR/INSS de cada verba pela **incidência da rubrica** (indenizatórias desligadas). Sem relógio.

---

## 4. Modelo de dados (acréscimo ao motor — NÃO hardcode)

```
EventoFolha           { tipo ∈ {MENSAL, DECIMO_TERCEIRO, FERIAS, RESCISAO}, competencia, baseSeparada }
ParcelaDecimoTerceiro { ordem ∈ {1,2}, percentual(param), dataLimite(param), aplicaDescontos }
PeriodoAquisitivoFerias { inicio, fim, diasDireito, avos, faltas }
LancamentoFerias      { rubricaFerias, rubricaTerco, abonoPecuniario?, rubricaTercoAbono?, indenizada:bool }
Desligamento          { tipo, regimeVinculo ∈ {ESTATUTARIO, CELETISTA}, dataDesligamento, avos13, avosFerias }
// Reusa: TabelaINSS/RPPS, TabelaIRRF, Rubrica{incideCP,incideIRRF,incideRPPS,incideFGTS}, VinculoServidor
```
- **Avos** (13º e férias) = **função pura** de datas de admissão/afastamentos/desligamento — entradas.
- **Indicador `indenizada`** liga/desliga incidências (gozadas tributam; indenizadas isentam).

---

## 5. Pendências de fonte oficial (bloqueiam cálculo correto do ciclo anual)

1. **[a confirmar — Lei municipal 327/2008 (Estatuto de Maximiliano de Almeida/RS)]** regras de **férias**
   (período aquisitivo, 30 dias, terço, abono pecuniário, faltas), **13º** (proporcionalidade, base) e
   **vacância/exoneração** (verbas, dobra, prazo de pagamento, licença-prêmio indenizável). Fonte 403 no
   fetch — **baixar texto integral** (leismunicipais / Diário Oficial RS).
2. **[a confirmar]** Enquadramento de **comissionados/temporários** do município (estatutário ou celetista)
   → define FGTS/aviso/multa.
3. **[a confirmar]** Incidência da **cota do segurado/RPPS** sobre o **1/3 de férias** após STF Tema 985.
4. **[a confirmar]** Tabela INSS/IRRF da **competência de pagamento** (já tratado em `pesquisa-folha-calculo.md`).
5. **[a confirmar]** Eventos eSocial do ciclo anual (S-1200/S-2299/S-2399) e mapeamento das rubricas de
   13º/férias/rescisão (ver `pesquisa-esocial-eventos.md`).

---

## Fontes (oficiais e jurisprudência)

- CF/1988 art. 7º (VIII 13º; XVII férias+1/3): https://www.planalto.gov.br/ccivil_03/constituicao/constituicao.htm
- Lei 4.090/1962 (Lei do 13º / gratificação natalina): https://www.planalto.gov.br/ccivil_03/leis/l4090.htm
- Lei 4.749/1965 (pagamento do 13º em duas parcelas): https://www.planalto.gov.br/ccivil_03/leis/l4749.htm
- Decreto 57.155/1965 (regulamenta o 13º — prazos/parcelas): https://www.planalto.gov.br/ccivil_03/decreto/1950-1969/d57155.htm
- Lei 7.713/1988, art. 12-A (tributação exclusiva do 13º): https://www.planalto.gov.br/ccivil_03/leis/l7713.htm
- CLT (Dec.-Lei 5.452/1943) — férias arts. 129–145 (130 aquisitivo, 134 concessivo, 137 dobra, 143 abono): https://www.planalto.gov.br/ccivil_03/decreto-lei/del5452.htm
- Lei 12.506/2011 (aviso prévio proporcional): https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12506.htm
- CLT art. 477 §6º (prazo 10 dias) e art. 484-A (distrato): https://www.planalto.gov.br/ccivil_03/decreto-lei/del5452.htm
- IN RFB 1.500/2014 (IRRF — base/deduções, dispensa do art. 67 Lei 9.430/96 não se aplica ao 13º): http://normas.receita.fazenda.gov.br/sijut2consulta/link.action?idAto=57670
- PGFN — IRRF sobre terço de férias gozadas: https://www.gov.br/pgfn/pt-br/cidadania-tributaria/por-assunto/imposto-de-renda-pessoa-fisica-irpf-2/copy_of_conceito-de-rendimentos-e-verbas-nao-tributaveis/ferias
- STJ Súmula 386 (férias proporcionais indenizadas + abono = isentas de IR): https://www.stj.jus.br/docs_internet/revista/eletronica/stj-revista-sumulas-2011_30_capSumula386.pdf
- STF Tema 985 / STJ REsp 1.559.926 (contribuição sobre 1/3 de férias): https://www.gov.br/previdencia/pt-br/assuntos/rpps/legislacao-dos-rpps/julgamentos-stj/resp-1559926-contribuicao-sobre-1-3-de-ferias
- TST — direito a férias (período aquisitivo/concessivo): https://www.tst.jus.br/en/ferias1
- TST — verbas rescisórias por tipo de desligamento: https://www.tst.jus.br/en/-/pedido-de-demissao-quais-verbas-rescisorias-sao-devidas-1
- MTE — 13º salário (direito, regras e prazos): https://www.gov.br/trabalho-e-emprego/pt-br/noticias-e-conteudo/2025/novembro/decimo-terceiro-salario-entenda-o-direito-regras-e-prazos-de-pagamento
- Estatuto dos Servidores de Maximiliano de Almeida/RS (Lei 327/2008): https://leismunicipais.com.br/estatuto-do-servidor-funcionario-publico-maximiliano-de-almeida-rs
- (Referência interna) `pesquisa-folha-calculo.md`, `RPPS-maximiliano.md`, `pesquisa-esocial-eventos.md`.
