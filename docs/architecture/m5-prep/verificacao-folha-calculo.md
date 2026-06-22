# M5 — Verificação adversária: `pesquisa-folha-calculo.md`

> **Papel:** auditor de fatos (CLAUDE.md §16). Tentei **refutar** cada afirmação factual da
> pesquisa contra **fonte oficial vigente** (versão de MOS/leiaute, alíquotas, instrumento legal).
> **Data da verificação:** 2026-06-22.
> **Legenda:** `CONFIRMADO` (fonte oficial localizada) · `PLAUSÍVEL-SEM-FONTE` (coerente mas sem
> doc oficial verificado nesta auditoria) · `INCERTO` (não verificável sem doc local/oficial específico) ·
> `CORRIGIR` (achei erro factual na pesquisa).

---

## Placar

- **CONFIRMADOS:** 9
- **CORRIGIR (erro factual encontrado):** 2 (instrumento legal do IRRF mai/2025; status da tabela INSS 2026)
- **PLAUSÍVEL-SEM-FONTE:** 3
- **INCERTOS / bloqueiam implementação sem doc oficial:** 8

---

## 1. INSS / RGPS 2025 — `CONFIRMADO`

A tabela progressiva por faixas, SM 2025 = R$ 1.518,00, teto R$ 8.157,41 e alíquotas 7,5/9/12/14%
**conferem** com a fonte oficial. Oficializada pela **Portaria Interministerial MPS/MF nº 6/2025** (DOU 13/01/2025).
A mecânica "cada faixa só sobre a parcela dentro dela" (cumulativa) está correta.

- Fonte: [INSS — alíquotas com aumento do SM](https://www.gov.br/inss/pt-br/noticias/confira-como-ficaram-as-aliquotas-de-contribuicao-ao-inss)

## 1b. INSS 2026 — `CORRIGIR` (deixou de ser "[a confirmar]" — agora é CONFIRMADO)

A pesquisa marcou a tabela 2026 como `[a confirmar — obter Portaria jan/2026]`. **Já está publicada.**

- Teto 2026 = **R$ 8.475,55** — `CONFIRMADO`.
- Instrumento = **Portaria Interministerial MPS/MF nº 13**, publicada em **09/01/2026**, efeitos desde 01/01/2026.
- Tabela de faixas 2026 (confirmada na notícia oficial do INSS):

  | Faixa do salário de contribuição | Alíquota |
  |---|---|
  | até R$ 1.621,00 | 7,5% |
  | R$ 1.621,01 a R$ 2.902,84 | 9,0% |
  | R$ 2.902,85 a R$ 4.354,27 | 12,0% |
  | R$ 4.354,28 a R$ 8.475,55 (teto) | 14,0% |

- Fonte: [INSS — teto R$ 8.475,55 em 2026](https://www.gov.br/inss/pt-br/assuntos/com-reajuste-de-3-9-teto-do-inss-chega-a-r-8-475-55-em-2026)
- **Ação:** atualizar a pesquisa; semear a tabela INSS-2026 no motor. (Limite da 1ª faixa = SM 2026 R$ 1.621,00 — confirma reajuste do mínimo.)

## 2. RPPS / EC 103/2019 — `CONFIRMADO` (regra federal) + `INCERTO` (Maximiliano de Almeida)

- A afirmação "alíquota do servidor é definida em lei do ente, mínimo 14% (= União) quando há déficit
  atuarial, por força do art. 9º §4º da EC 103/2019 + Lei 9.717/1998" está **correta**. A fonte oficial
  acrescenta nuance importante a registrar: **se NÃO houver déficit atuarial**, a alíquota pode ser menor
  que 14%, mas **não inferior à do RGPS**; e a EC 103 permite **alíquotas progressivas/escalonadas** usando
  as alíquotas da União como piso (déficit) ou do RGPS (sem déficit). A pesquisa captou isso ("não assumir
  14% fixo" / "parametrizar 100%") — **correto e bem conservador**.
- Fonte: [MPS — Aplicação da EC 103/2019 aos RPPS](https://www.gov.br/previdencia/pt-br/assuntos/rpps/legislacao-dos-rpps/aplicacao-da-emenda-constitucional-no-103-de-2019-aos-rpps)
- `INCERTO` / **NÃO implementar sem doc:** existência e tabela do RPPS de **Maximiliano de Almeida/RS**.
  Sem a lei previdenciária municipal não há alíquota nem base. Default seguro: tratar vínculos como RGPS
  até obter a lei. **Mantém-se como bloqueio.**

## 3. IRRF 2025 — tabela `CONFIRMADO` · **instrumento legal `CORRIGIR`**

- Os **números** da tabela mai–dez/2025 (isenção R$ 2.428,80; parcelas a deduzir 182,16 / 394,16 /
  675,49 / **908,73**; dependente R$ 189,59; simplificado R$ 607,20) e a tabela jan–abr/2025
  (isenção R$ 2.259,20; parcelas 169,44 / 381,44 / 662,77 / 896,00; simplificado R$ 564,80)
  **conferem exatamente** com a tabela oficial da Receita. `CONFIRMADO`.
  - *Nota de auditoria:* uma busca secundária retornou "884,96" para a última parcela — **valor incorreto**;
    a fonte oficial confirma **908,73**. A pesquisa está certa.
- **ERRO FACTUAL A CORRIGIR:** a pesquisa atribui a mudança de mai/2025 à **"Lei nº 15.270/2025"**. **Errado.**
  - A alteração de mai/2025 (faixa isenta → R$ 2.428,80) veio da **MP nº 1.294/2025**, depois convertida na
    **Lei nº 15.191, de 11/08/2025** (que revogou a MP 1.294). É a "isenção até 2 salários mínimos".
  - A **Lei nº 15.270, de 26/11/2025** é **outra coisa**: é a reforma maior (isenção efetiva até R$ 5.000 e
    tributação de altas rendas), com efeitos **a partir de jan/2026** — NÃO altera a tabela de mai/2025.
  - **Impacto no motor:** a pesquisa mistura os dois instrumentos. Para a competência mai–dez/2025 o
    fundamento é Lei 15.191/2025 (ex-MP 1.294). Para 2026 é Lei 15.270/2025. Corrigir as citações.
  - Fontes: [Câmara — Lei 15.191/2025 entra em vigor (isenção 2 SM)](https://www.camara.leg.br/noticias/1187381-entra-em-vigor-lei-que-isenta-do-imposto-de-renda-quem-recebe-ate-dois-salarios-minimos/) ·
    [Fazenda — sanção Lei 15.270/2025 (isenção até R$ 5 mil, vigência 2026)](https://www.gov.br/fazenda/pt-br/assuntos/noticias/2025/novembro/presidente-sanciona-lei-que-amplia-isencao-do-imposto-de-renda-para-quem-ganha-ate-r-5-mil)
- IRRF **2026** (efeitos da Lei 15.270/2025 na retenção mensal): `INCERTO`. A Receita publicou orientação em
  dez/2025 ("calcular a redução a partir de 01/01/2026"), mas o **mecanismo de retenção mensal**
  (redução progressiva aplicada APÓS a tabela tradicional, que segue inalterada nas 5 faixas) precisa da
  **IN/ato da Receita vigente**. **NÃO implementar a regra 2026 sem a IN.** A pesquisa acerta ao mantê-la `[a confirmar]`.
  - Fonte: [Receita — atualiza normas IRPF (dez/2025)](https://www.gov.br/receitafederal/pt-br/assuntos/noticias/2025/dezembro/receita-federal-atualiza-normas-relativas-ao-imposto-sobre-a-renda-das-pessoas-fisicas)

> **Atenção de engenharia (não é erro, é risco):** a "regra do mais vantajoso" deve comparar a base com
> deduções legais **vs** base com desconto simplificado e usar a **menor base** — isso a pesquisa descreve
> certo. Mas, a partir de 2026, somar a isso o **redutor da Lei 15.270** muda o resultado final. Não acoplar
> as duas lógicas até ter a IN.

## 4. Rubricas / eSocial S-1010 — `CONFIRMADO` (estrutura) · `INCERTO` (códigos exatos)

- Estrutura **confirmada**: S-1010 é a Tabela de Rubricas; correlação obrigatória com **Tabela 03 (Naturezas
  das Rubricas)**; campos **codIncCP**, **codIncIRRF**, **codIncFGTS** existem e são os usados pelo eSocial
  para apuração de CP, IRRF e FGTS. A ideia central da pesquisa — *somar bases por código de incidência, não
  por nome de rubrica* — está **correta e é a abordagem certa**.
- Versão vigente **confirmada existir**: **leiautes S-1.3 consolidados até NT 06/2026** e **MOS S-1.3**
  (versão consolidada até Nota Orientativa S-1.3 nº 07/2026 publicada). A referência da pesquisa à
  "versão S-1.3, NT 06/2026" é **real e atual**.
  - Fontes: [eSocial — Documentação Técnica](https://www.gov.br/esocial/pt-br/documentacao-tecnica) ·
    [MOS S-1.3 consolidado (PDF)](https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-3-consolidada-ate-a-no-s-1-3-07-2026.pdf) ·
    [Tabelas S-1.3 (até NT 06/2026)](https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026/tabelas.html)
- `INCERTO` / **NÃO implementar sem o doc:** os **números das tabelas 03/20/21/23** e as regras de validação
  citadas (ex.: "informativa ⇒ codIncCP=00 e codIncIRRF=9"; "codIncFGTS ∈ {21,93} só em desligamento").
  Essas afirmações são **PLAUSÍVEIS** (consistentes com o leiaute) mas **NÃO foram conferidas linha a linha
  contra o XSD/MOS S-1.3 nesta auditoria**. **Baixar XSD + MOS S-1.3 e mapear cada código antes de codar.**
  Hardcodar essas tabelas de cabeça é exatamente o que a §16 proíbe.

## 5. Margem consignável — `CONFIRMADO` (federal) · **instrumentos a nomear** · `INCERTO` (municipal)

- A trajetória federal está **correta e agora tem instrumento nomeado** (a pesquisa não citou os atos):
  - Regra anterior: 45% (35% empréstimo + 5% cartão crédito consignado + 5% cartão benefício) — `CONFIRMADO`.
  - 2026: margem global **40%**; redução de **2 p.p./ano a partir de 14/01/2027** → 38/36/34/32/**30% em 2031**;
    cartões consignados extintos para novas operações **a partir de 2029** (a notícia detalha "extinção das
    modalidades de cartão em 2029, na transição p/ 34%"). `CONFIRMADO`.
  - **Instrumentos (a ADICIONAR na pesquisa):** **MP nº 1.355/2026** (margem 45→40% e trajetória, altera Lei
    14.509/2022) + **Decreto nº 12.957/2026** (prazo 96→120 parcelas, vigência 20/05/2026).
  - Fonte: [MGISP — novas regras do consignado no Executivo federal (mai/2026)](https://www.gov.br/gestao/pt-br/assuntos/noticias/2026/maio/conheca-as-novas-regras-de-protecao-prazo-e-margem-do-consignado-no-executivo-federal)
- **CRÍTICO p/ o produto:** essas regras são do **Executivo FEDERAL**. Para o servidor **MUNICIPAL** de
  Maximiliano de Almeida, a margem é regida por **lei/decreto municipal**. A pesquisa acerta em
  "parametrizar por tenant". `INCERTO` até obter a norma municipal — **não aplicar 40% federal por default**.

## 6. 13º salário — `PLAUSÍVEL-SEM-FONTE` (tese certa, citação a fechar)

- "Tributação **exclusiva** na fonte do 13º (art. 12-A da Lei 7.713/1988); base/IRRF calculados em separado
  da folha mensal; incide INSS/RPPS sobre o 13º em base própria; 1ª parcela sem retenção, 2ª com cálculo
  sobre o total" — é a **regra tradicional e amplamente aplicada**, considerada **correta**.
- Porém **não localizei nesta auditoria** o texto consolidado do art. 12-A no Planalto nem doc oficial que
  confirme cada detalhe (a fonte citada na pesquisa é uma página genérica da PGFN). `PLAUSÍVEL-SEM-FONTE`.
- **Ação:** obter [Lei 7.713/1988 art. 12-A — Planalto] e regra de proporcionalidade 1/12. Baixo risco, mas
  fechar a citação antes de marcar como CONFIRMADO.

## 7. Férias + 1/3 — `PLAUSÍVEL` (IRRF) · `INCERTO` (CP do segurado sobre o terço)

- "1/3 constitucional; IRRF incide sobre o terço de férias gozadas (tributado junto, não exclusivo)" — tese
  **plausível/correta**, mas a fonte é página genérica da PGFN; classificar `PLAUSÍVEL-SEM-FONTE`.
- **`INCERTO` real:** a **incidência de contribuição previdenciária do SEGURADO sobre o 1/3 de férias**
  após **STF Tema 985 / STJ REsp 1.559.926** é juridicamente sensível e **muda conforme o regime (RGPS x
  RPPS) e jurisprudência vigente**. A pesquisa corretamente marca `[a confirmar]`. **NÃO codar a flag de
  incidência do terço sem decisão jurídica/contábil do ente.** Tornar parametrizável por rubrica.

## 8. Modelo de dados / "não hardcode" — `CONFIRMADO` (boa engenharia)

A abordagem de tabelas versionadas por `competencia`/`tenantId`, motor aplicando *fórmulas* sobre *números
parametrizados*, e cálculo de bases por código de incidência está **alinhada à §7 e §16 da Constituição**.
Sem reparo. É o caminho certo e protege contra exatamente os erros encontrados acima (instrumento legal
trocado não quebra o motor se os números forem seed configurável).

---

## O que NÃO implementar sem o doc oficial vigente (bloqueios duros)

1. **Tabelas de incidência eSocial (03/20/21/23) e regras de validação do S-1010** → baixar **XSD + MOS S-1.3**
   (consolidado até NO 07/2026) e mapear código a código. Não transcrever de memória.
2. **RPPS de Maximiliano de Almeida** (alíquota servidor/patronal + base) → **lei previdenciária municipal**.
   Sem ela, default = RGPS.
3. **IRRF 2026 (redutor Lei 15.270/2025 na retenção mensal)** → **IN/ato da Receita** vigente. A tabela
   progressiva tradicional 2025 (mai–dez) pode ser semeada já; o redutor 2026, não.
4. **Margem consignável municipal** → lei/decreto de Maximiliano de Almeida (% e base). Não herdar 40% federal.
5. **Faseamento/modalidade do ente público no eSocial** (Simplificado x completo; eventos S-1200/S-1207/S-1210)
   → confirmar no portal/cronograma do ente. Define quais eventos o motor gera.
6. **Leiaute TCE-RS de folha/pessoal** (remessa de RH) → obter layout vigente; a pesquisa nem detalha campos.
7. **Estatuto dos servidores municipais** (férias/abono pecuniário/13º proporcional/gratificações = rubricas locais).
8. **Incidência de CP do segurado sobre 1/3 de férias** (Tema 985) → parecer jurídico/contábil do ente.

---

## 3 MAIORES RISCOS

1. **Confusão de instrumento legal no IRRF (Lei 15.270 vs Lei 15.191/MP 1.294).** A pesquisa atribui a tabela
   de **mai/2025** à Lei 15.270/2025, que na verdade rege **2026** (regra diferente: redutor até R$ 5 mil).
   Risco de implementar a lógica de 2026 na competência errada ou de citar fundamento errado em documento
   sob escrutínio do TCE. **Mitigação:** separar competências por instrumento; semear só números, nunca
   acoplar a regra-2026 à folha de 2025; corrigir as citações na pesquisa.

2. **Tabelas de incidência eSocial assumidas "de cabeça".** Os códigos (codIncCP/IRRF/FGTS, naturezas Tab.03)
   e as regras de validação determinam **se uma rubrica entra ou não em cada base** — erro aqui propaga para
   INSS, IRRF, FGTS e para os eventos S-1200/S-1210, gerando rejeição no eSocial e remessa errada ao TCE.
   As afirmações da pesquisa são plausíveis mas **não foram conferidas contra o XSD/MOS S-1.3**. **Mitigação:**
   carregar as tabelas do MOS/XSD oficial como dado versionado; testes de contrato contra o XSD; nunca hardcode.

3. **Aplicar regras FEDERAIS onde a regra é MUNICIPAL (RPPS, margem consignável, estatuto/férias/13º).** O
   piloto é municipal; alíquota RPPS, margem de consignado e regras de férias/abono dependem de **legislação
   de Maximiliano de Almeida**, ainda não obtida. Usar default federal (14%, 40%, etc.) produziria folha
   **incorreta e auditável como erro pelo TCE-RS**. **Mitigação:** travar esses parâmetros como obrigatórios
   por tenant/competência, com o motor recusando cálculo enquanto não houver tabela municipal carregada
   (fail-closed), e default conservador documentado (RGPS) só onde juridicamente seguro.
