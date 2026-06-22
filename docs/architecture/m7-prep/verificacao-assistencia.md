# M7 — Verificação de Fatos: Assistência Social (SUAS)

> Auditoria cética de `pesquisa-assistencia.md` contra fontes oficiais (gov.br/mds, FNAS,
> Planalto, DATAPREV, IBGE/CES, SAGI/SAGICAD). Cada afirmação foi reclassificada em
> **CONFIRMADO** (fonte oficial bate), **PLAUSÍVEL-SEM-FONTE** (padrão correto, número/leiaute
> não confirmado em fonte primária), **INCERTO** (afirmação genérica/datada/sem doc) ou
> **INCORRETO/DESATUALIZADO** (a fonte contradiz o que está escrito).
>
> Verificado em 2026-06. **§16 CLAUDE.md:** nenhum percentual/leiaute hardcoded — parametrizar
> por tenant+vigência onde for lei, e bloquear integração até obter contrato/leiaute oficial.

---

## Placar

| Classe | Qtde | Itens |
|---|---|---|
| **CONFIRMADO** | 11 | CadÚnico 2 anos; RMA mensal/30 dias + base CIT 4/2011 e 20/2013; Censo SUAS anual + Decreto 7.334/2010 + 2º semestre; Portaria 1.043/2024 revoga 113/2015; AgilizaSUAS + BB Gestão Ágil substitui Demonstrativo; prazo prest. contas 1º/mar; 4 blocos de financiamento; Plano de Ação SUASWeb descontinuado; PBF Decreto 12.064/2024 + Portaria 1.058/2025 (60%/75%); PBF saúde semestral / educação bimestral (Sistema Presença); IGD-M = FatorI×II×III×IV |
| **PLAUSÍVEL-SEM-FONTE** | 6 | Leiautes de arquivo CadÚnico (V7/V8 AVE/REV); campos exatos RMA por questionário; campos Censo por ano; tabela de pisos/valores; pesos numéricos exatos do IGD-M; campos do Prontuário SUAS eletrônico |
| **INCERTO** | 2 | "AVE25/REV25" como rótulo de 2025 (datado — muda a cada ano); existência de API/contrato programático (RMA/Censo/SICON/Presença) |
| **INCORRETO / DESATUALIZADO** | 2 | Benefício eventual "< 1/4 SM como parâmetro de referência LOAS"; prazo de transição AgilizaSUAS ano-base 2024 (foi prorrogado depois de escrito o doc) |

**Confirmados: 11 — Incertos/Incorretos que exigem ação: 4** (2 incertos + 2 a corrigir),
além de 6 pendências documentais já corretamente sinalizadas no doc original.

---

## 1. CadÚnico — periodicidade 2 anos
**CONFIRMADO.** Decreto 11.016/2022, art. 12: atualização a cada 2 anos contados de
inclusão/última atualização/revalidação. Operação Caixa/Dataprev confere. O ponto de design
(sync model, não substituir o app do MDS) é arquitetura, não fato — aceito.
- **Pendência (PLAUSÍVEL-SEM-FONTE):** leiaute exato de importação/retorno AVE/REV / layout V7/V8
  permanece **não obtido**. Mantém bloqueio para qualquer sync de arquivo.
- Fonte: https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2022/decreto/d11016.htm

**INCERTO — "AVE25 / REV25":** o doc fixa rótulos de 2025. São **nomes de campanha anuais**
(AVE26/REV26 em 2026, etc.). Tratar como *parâmetro de calendário federal por exercício*, nunca
como constante. Não é erro de fato em 2025, mas vira erro se não for parametrizado.

---

## 2. RMA — mensal, 30 dias após o mês
**CONFIRMADO.** Base legal Resolução CIT nº 4/2011 (CRAS/CREAS) + CIT nº 20/2013 (Centro POP);
prazo de inserção no sistema online em até 30 dias após o mês de referência — confere em manuais
MDS/SAGI. Estrutura "Formulário 1 (volume) + Formulário 2 (por NIS)" também confere.
- **PLAUSÍVEL-SEM-FONTE:** conjunto exato de campos por questionário (CRAS/CREAS/POP) — depende
  dos Manuais RMA (2022 disponíveis em aplicacoes.mds.gov.br/sagi/atendimento/doc/).
- **INCERTO:** ausência de API pública — provável, mas é afirmação negativa sem contrato oficial.
  Tratar integração como "web/arquivo até prova em contrário".
- Fonte: https://www.gov.br/mds/pt-br/acoes-e-programas/suas/gestao-do-suas/vigilancia-socioassistencial-1/registro-mensal-de-atendimentos-2013-rma · Manual RMA CRAS 2022 (PDF SAGI)

---

## 3. Censo SUAS — anual, 2º semestre
**CONFIRMADO** (e o `[a confirmar]` do doc sobre o decreto pode ser fechado):
Decreto nº 7.334, de 19/10/2010, institui o Censo SUAS; coleta **anual, no segundo semestre**,
desde 2007, via sistema eletrônico SNAS+SAGI. A citação do decreto no doc estava marcada como
incerta — **agora confirmada**.
- **PLAUSÍVEL-SEM-FONTE:** janela exata (set–nov + dez) e campos por questionário variam por
  portaria anual — confirmar no manual do ano vigente (ex.: Manual Censo SUAS 2025 Gestão Municipal).
- Fonte: https://www.planalto.gov.br/ccivil_03/_ato2007-2010/2010/decreto/d7334.htm · https://www.gov.br/mds/pt-br/acoes-e-programas/suas/gestao-do-suas/vigilancia-socioassistencial-1/censo-suas

---

## 4. Cofinanciamento FNAS / Portaria 1.043/2024 / AgilizaSUAS
**CONFIRMADO (núcleo):**
- Portaria MDS nº 1.043, de 24/12/2024, **revoga a Portaria 113/2015** e reescreve transferência,
  execução e prestação de contas fundo a fundo; incorpora regras de guarda documental (antes na
  Portaria SNAS 124/2017) e facilita reprogramação de saldo. **CONFIRMADO.**
- **4 blocos de financiamento** (PSB; PSE; Gestão do SUAS; Gestão do PBF e do CadÚnico). **CONFIRMADO.**
- **Plano de Ação (SUASWeb) descontinuado**; permanece o **Plano de Assistência Social** (≠ Plano de
  Ação). **CONFIRMADO.**
- **AgilizaSUAS** (+ **BB Gestão Ágil**/Caixa para a parte financeira) substitui o **Demonstrativo
  Sintético Anual** do SUASWeb; lançamento **concomitante à execução**, prazo final **1º de março do
  exercício subsequente** a partir do exercício 2025. **CONFIRMADO.**

**DESATUALIZADO — prazos de transição do ano-base 2024:** o doc fixa "gestores até 30/09/2025;
CMAS até 31/12/2025". A **Portaria SNAS/MDS nº 132, de 04/12/2025, prorrogou** esses prazos da
prestação de contas do ano-base 2024 (novo prazo de envio pelos gestores em torno de **1º/03/2026**).
→ **Corrigir** o doc e, no design, **nunca hardcodar prazos de transição** — vir de tabela de
calendário federal por exercício.

- **PLAUSÍVEL-SEM-FONTE:** tabela de pisos/valores de referência; contrato/API do AgilizaSUAS
  (provável só web/arquivo). Mantêm bloqueio — corretamente sinalizado.
- Fontes: https://fnas.mds.gov.br/portaria-mds-no-1-043-de-24-de-dezembro-de-2024-nova-regulacao-para-transferencias-fundo-a-fundo-no-suas/ · https://fnas.mds.gov.br/nova-ferramenta-para-gestao-de-prestacao-de-contas-no-suas-agilizasuas-e-bb-gestao-agil/

---

## 5. Prontuário SUAS
**CONFIRMADO (fluxo):** instrumento nacional padronizado para PAIF (CRAS) e PAEFI (CREAS);
não há envio periódico ao MDS (registro local/instrumental); alimenta RMA e Vigilância. A regra
"um prontuário por unidade quando PAIF+PAEFI simultâneos" e o tratamento LGPD são consistentes com
as orientações técnicas do MDS.
- **PLAUSÍVEL-SEM-FONTE:** campos exatos do Prontuário eletrônico dependem do Manual (versão
  preliminar disponível, mas confirmar versão vigente). Mantém bloqueio para schema fiel.

---

## 6. Benefícios — BPC, PBF, Eventuais

### 6.1 BPC — **CONFIRMADO.** LOAS (8.742/93); concessão/pagamento INSS, gestão MDS; município
informa/acompanha, não paga. Design read-model correto.

### 6.2 PBF — **CONFIRMADO.**
- Base: Lei 14.601/2023 + **Decreto 12.064/2024** + **Portaria MDS 1.058/2025** (condicionalidades).
- Frequência escolar **60% (4–6 anos)** e **75% (6–18 anos, sem ensino básico completo)** — confere
  no Decreto 12.064/2024. **Confirmar versão vigente sempre — tratar como parâmetro** (correto no doc).
- **Saúde semestral / Educação bimestral (Sistema Presença/MEC):** o doc marcou `[a confirmar]` na
  tabela §8 — **agora CONFIRMADO**. Saúde = 2 vigências/ano (jan–jun / jul–dez); Educação = bimestral
  no Sistema Presença. Pode fechar o `[a confirmar]`.
- **INCERTO:** APIs/contratos SICON/SIGPBF/Presença/PBF-Saúde — sem contrato público confirmado;
  tratar como web até obter doc.

### 6.3 Benefícios Eventuais — **CONTÉM ERRO FACTUAL.**
- **CONFIRMADO:** base LOAS art. 22 (natalidade, funeral, vulnerabilidade temporária, calamidade);
  **definidos e custeados pelo município** (lei/decreto + CMAS); **parametrizar por tenant** (§16) —
  correto e é o ponto mais importante do design.
- **INCORRETO/DESATUALIZADO:** a frase "em geral para renda per capita < 1/4 do salário mínimo
  (**parâmetro de referência LOAS**)". O critério rígido de 1/4 SM era da redação **original** da
  LOAS e foi **alterado pela Lei 12.435/2011**: hoje a LOAS art. 22 **não fixa o teto de 1/4 SM** —
  concessão, valor e critérios de renda são **definidos por Estados/DF/Municípios** via CMAS e LOA.
  → **Corrigir:** retirar "1/4 SM" como parâmetro de referência federal; deixar 100% como parâmetro
  municipal por tenant+vigência (alguns municípios adotam 1/4, 1/2 ou outro — é escolha local).
- Fontes: https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2011/lei/l12435.htm · https://www.planalto.gov.br/ccivil_03/leis/l8742.htm

---

## 7. IGD
**CONFIRMADO (estrutura):** IGD-M mensal; compõe base de cálculo de recursos transferidos;
**IGD-M = FatorI × FatorII × FatorIII × FatorIV** (FatorI mede atualização cadastral +
condicionalidades saúde/educação; FatorIII = lançamento da comprovação de gastos; FatorIV =
aprovação total pelo CMAS) — confere com material MDS e IN 091/SAGICAD.
- **PLAUSÍVEL-SEM-FONTE:** pesos/fórmula numérica exata dos fatores — depende da IN 091 e portaria
  de valores/teto vigente. Mantém bloqueio para cálculo fiel (o doc já trata como "estimativa de
  gestão", não cálculo oficial — design correto).
- Fonte: https://www.gov.br/mds/pt-br/acoes-e-programas/bolsa-familia/igd · IN 091 SAGICAD

---

## Correções a aplicar em `pesquisa-assistencia.md`
1. **§6.3** — remover "1/4 SM (parâmetro de referência LOAS)"; substituir por "critério de renda
   **definido por lei/decreto municipal + CMAS** (Lei 12.435/2011 retirou o teto fixo de 1/4 SM da
   LOAS) — 100% parametrizável por tenant+vigência".
2. **§4** — anotar que os prazos de transição do ano-base 2024 foram **prorrogados pela Portaria
   SNAS/MDS 132/2025**; mover prazos para tabela de calendário federal por exercício (não hardcodar).
3. **§1 / §3** — fechar o `[a confirmar]` do **Decreto 7.334/2010 (Censo SUAS)** → CONFIRMADO.
4. **§8** — fechar o `[a confirmar]` de **saúde semestral / educação bimestral** → CONFIRMADO.
5. **§1** — marcar **AVE/REV** como rótulo anual (parâmetro de calendário), não "AVE25/REV25" fixo.

---

## 3 RISCOS

1. **Prazos e rótulos federais hardcodados envelhecem em meses.** Prestação de contas (1.043/2024 +
   prorrogações como a Portaria 132/2025), campanhas AVE/REV anuais, vigências de condicionalidades e
   janela do Censo mudam por portaria a cada exercício. Qualquer prazo/rótulo gravado em código vira
   bug legal silencioso. **Mitigação:** tabela de "calendário federal SUAS por exercício" por tenant,
   versionada e auditável; nenhum prazo no domínio.

2. **Bloqueadores de leiaute/contrato mascarados de "pronto".** O fluxo está bem confirmado, mas
   **nenhum leiaute de arquivo nem contrato de API foi obtido** (CadÚnico V7/V8 AVE/REV, RMA, Censo,
   AgilizaSUAS, SICON/Presença, tabela de pisos, IN 091 IGD). Há risco de subestimar M7 assumindo
   integração programática que na prática é **preenchimento web manual**. **Mitigação:** marcar cada
   integração como BLOQUEADA-SEM-LEIAUTE e desenhar primeiro a geração interna (Prontuário→RMA,
   cadastro→Censo) + exportação, não o push automático.

3. **Erro factual de critério legal (1/4 SM) podia virar regra de elegibilidade no produto.** Tratar
   um teto federal revogado como "referência" levaria a indeferir benefício eventual fora do que a lei
   municipal manda — risco jurídico direto sobre cidadão e sobre o TCE. **Mitigação:** elegibilidade de
   benefício eventual 100% derivada da lei municipal do tenant (CMAS+LOA), sem default federal embutido;
   validar percentuais de condicionalidade PBF contra a versão vigente do Decreto 12.064/2024 antes de
   fixar, sempre como parâmetro.
