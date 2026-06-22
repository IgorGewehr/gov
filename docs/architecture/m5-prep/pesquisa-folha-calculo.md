# M5 — Pesquisa: Cálculo de Folha de Pagamento PÚBLICA

> **Status:** pesquisa de fundamentação (dirige a Rules-as-Code do motor de folha do M5).
> **Constituição §16 (REGRA DE OURO):** NÃO inventar alíquota/faixa/regra. Todo valor numérico
> abaixo tem FONTE (URL) e **competência explícita**; valores são **parametrizáveis por tenant/competência**.
> Pontos sem fonte oficial confirmada ficam marcados `[a confirmar — <documento>]`.
> **Princípio de engenharia:** o motor NÃO embute alíquotas no código. Tabelas (INSS, IRRF, RPPS,
> rubricas, margem) vivem em tabelas versionadas por `competencia` (AAAA-MM) e `tenantId`. O código
> aplica *fórmulas*; os *números* são dado configurável (seed inicial = valores oficiais abaixo).

---

## 0. Sumário executivo (4 decisões que o motor precisa tomar)

1. **Regime previdenciário do servidor**: RGPS (INSS, tabela progressiva federal) **vs** RPPS
   (regime próprio municipal — alíquota definida em LEI MUNICIPAL, mín. 14% por EC 103/2019).
   É atributo do vínculo, não do tenant. Maximiliano de Almeida/RS: **[a confirmar — verificar se o
   município tem RPPS instituído ou se todos os servidores são RGPS; obter lei previdenciária municipal]**.
2. **IRRF**: tabela progressiva federal mensal + dedução por dependente + opção desconto simplificado
   (escolher o mais vantajoso). Base = bruto − (INSS/RPPS) − deduções.
3. **Rubricas**: cada provento/desconto tem natureza eSocial (S-1010) com códigos de incidência
   (CP/IRRF/FGTS) que determinam se entra ou não em cada base de cálculo.
4. **Cálculos especiais**: 13º (tributação exclusiva, base separada do mês) e férias (+1/3 constitucional).

---

## 1. INSS / RGPS — contribuição do segurado (empregado/servidor celetista/comissionado sem RPPS)

Tabela **progressiva por faixas** (cada faixa aplica sua alíquota só sobre a parcela dentro dela —
NÃO é alíquota única sobre o total). Competência **2025** (a partir de jan/2025, SM = R$ 1.518,00):

| Faixa do salário de contribuição | Alíquota |
|---|---|
| até R$ 1.518,00 | 7,5% |
| R$ 1.518,01 a R$ 2.793,88 | 9,0% |
| R$ 2.793,89 a R$ 4.190,83 | 12,0% |
| R$ 4.190,84 a R$ 8.157,41 (teto) | 14,0% |

- **Teto do salário de contribuição 2025** = R$ 8.157,41 → contribuição máxima ≈ R$ 951,62.
- **Teto 2026** = R$ 8.475,55 (reajuste 3,9%) — **[a confirmar — obter Portaria Interministerial MPS/MF de jan/2026 com a tabela completa de faixas 2026]**.
- FONTE: gov.br/inss — alíquotas e tabela de contribuição mensal (jan/2025); reajuste teto 2026.
  - https://www.gov.br/inss/pt-br/direitos-e-deveres/inscricao-e-contribuicao/tabela-de-contribuicao-mensal
  - https://www.gov.br/inss/pt-br/noticias/confira-as-aliquotas-de-contribuicao-ao-inss-com-o-aumento-do-salario-minimo
  - https://www.gov.br/inss/pt-br/assuntos/com-reajuste-de-3-9-teto-do-inss-chega-a-r-8-475-55-em-2026

> **Motor:** calcular de forma cumulativa por faixa. Parametrizar `(limiteSuperior, aliquota)[]` por competência.
> Para múltiplos vínculos RGPS, somar salários respeitando o teto único (regra de concomitância) — **[a confirmar — Manual eSocial/IN sobre múltiplos vínculos e desconto do teto]**.

---

## 2. RPPS — regime próprio de previdência (servidor efetivo com RPPS municipal instituído)

- A alíquota do servidor **NÃO é federal** — é definida em **lei do ente** (município), respeitando
  o **mínimo de 14%** (igual à alíquota da União) quando o RPPS tem déficit atuarial, por força do
  **art. 9º, §4º da EC nº 103/2019** + Lei nº 9.717/1998.
- **Contribuição patronal** ≥ contribuição do segurado (Lei 9.717/1998).
- Após instituição de **RPC** (regime de previdência complementar), benefícios do RPPS ficam
  limitados ao **teto do RGPS** (igual ao do INSS). A parcela acima do teto pode ir para o RPC.
- A EC 103 permitiu **alíquotas progressivas/escalonadas** no RPPS, mas a faixa e os percentuais
  exatos dependem da LEI MUNICIPAL → **parametrizar 100% por tenant/competência. NÃO assumir 14% fixo.**
- FONTE: gov.br/previdência — aplicação da EC 103/2019 aos RPPS; Lei 9.717/1998 (Planalto).
  - https://www.gov.br/previdencia/pt-br/assuntos/rpps/legislacao-dos-rpps/aplicacao-da-emenda-constitucional-no-103-de-2019-aos-rpps
  - https://www.planalto.gov.br/ccivil_03/leis/l9717.htm
- **[a confirmar — obter LEI do RPPS de Maximiliano de Almeida/RS (se existir) com tabela de alíquotas e base; senão, vínculos = RGPS]**

> **Motor:** `regimePrevidenciario ∈ {RGPS, RPPS}` no vínculo. Se RPPS, usar tabela de alíquotas do
> tenant; se RGPS, usar tabela INSS federal (seção 1). Base de cálculo do RPPS pode diferir da do RGPS
> (algumas verbas entram/saem) — **[a confirmar — base de cálculo da contribuição RPPS por lei municipal]**.

---

## 3. IRRF — Imposto de Renda Retido na Fonte (tabela progressiva mensal)

**Vigência a partir de MAIO/2025** (Lei nº 15.270/2025 alterou a faixa de isenção; valores mensais):

| Base de cálculo mensal | Alíquota | Parcela a deduzir (R$) |
|---|---|---|
| até R$ 2.428,80 | isento | — |
| R$ 2.428,81 a R$ 2.826,65 | 7,5% | 182,16 |
| R$ 2.826,66 a R$ 3.751,05 | 15,0% | 394,16 |
| R$ 3.751,06 a R$ 4.664,68 | 22,5% | 675,49 |
| acima de R$ 4.664,68 | 27,5% | 908,73 |

Vigência **jan–abr/2025** (parcela a deduzir diferente — manter no histórico de competências):
faixa isenta até R$ 2.259,20; parcelas a deduzir 169,44 / 381,44 / 662,77 / 896,00.

- **Dedução por dependente**: R$ 189,59/mês.
- **Desconto simplificado** (substitui TODAS as deduções legais): R$ 607,20/mês (mai/2025+);
  R$ 564,80 (jan–abr/2025) = 25% do limite da 1ª faixa.
- **Regra do mais vantajoso**: a fonte pagadora deve aplicar o desconto simplificado quando ele for
  maior que a soma das deduções legais (INSS/RPPS + dependentes + pensão alimentícia).
- **2026**: redução adicional do IR para quem ganha até R$ 5 mil (Lei 15.270/2025) — para retenção na
  fonte, Receita orienta cálculo a partir de jan/2026 → **[a confirmar — obter IN/tabela IRRF vigência 2026]**.
- FONTE: Receita Federal — tabelas IRPF/IRRF 2025 e exemplos Lei 15.270/2025.
  - https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/tabelas/2025
  - https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/tabelas/exemplos-de-aplicacao-da-lei-15-270-2025
  - https://www.gov.br/receitafederal/pt-br/assuntos/noticias/2025/dezembro/receita-federal-orienta-fontes-pagadoras-e-contribuintes-a-calcular-a-reducao-do-imposto-de-renda-a-partir-de-1o-de-janeiro-de-2026

> **Motor (fórmula):** `baseIRRF = totalProventosTributáveis − INSS/RPPS − (Σdependentes × valorDependente) − pensãoAlimentícia`.
> Comparar com `baseSimplificada = totalProventosTributáveis − descontoSimplificado`. Usar a **menor base**.
> `IRRF = base × alíquotaFaixa − parcelaDeduzir`. Tudo parametrizado por competência.

---

## 4. Rubricas (proventos / descontos) e bases de cálculo — eSocial S-1010

- O cadastro de rubricas é o evento **S-1010 (Tabela de Rubricas)**. Cada rubrica carrega:
  - `natRubr` (natureza — tabela 03 do eSocial),
  - `tpRubr` (1=provento, 2=desconto, 3/4=informativo),
  - `codIncCP` (incidência previdenciária — tabela 20/incidência CP),
  - `codIncIRRF` (incidência IRRF — **tabela 21** do eSocial),
  - `codIncFGTS` (incidência FGTS — **tabela 23**).
- Regras conhecidas de validação:
  - Rubrica **informativa** (`tpRubr ∈ {3,4}`) deve ter `codIncCP=00` e `codIncIRRF=9` (não incide).
  - Rubricas com `codIncFGTS ∈ {21,93}` só em eventos de desligamento (S-2299/S-2399) ou no grupo de
    remuneração de período anterior do S-1200.
- O **código de incidência de cada rubrica determina se ela entra na base** de CP (INSS/RPPS), IRRF e FGTS.
  → O motor de folha deve somar bases **por código de incidência**, não por nome de rubrica.
- FONTE: eSocial — documentação técnica, Tabelas (versão S-1.3, NT 06/2026) e S-1010.
  - https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026/tabelas.html
  - http://portal.esocial.gov.br/servicos/producao-empresas/S-1010-Tabela-de-Rubricas
- **[a confirmar — baixar leiaute S-1.3 + Tabelas 03 (naturezas), 20/21/23 (incidências) e MOS eSocial vigente; mapear cada código de incidência → flag de base no motor]**
- **[a confirmar — Maximiliano de Almeida envia eSocial pelo módulo Simplificado (entes públicos) ou completo? Verificar faseamento do ente público no eSocial]**

> **Motor:** modelar `Rubrica { tipo, natRubr, incideCP, incideIRRF, incideFGTS, incideRPPS }`.
> Folha = Σproventos − Σdescontos; bases = Σ(valor da rubrica onde incide<X>=true). FGTS só p/ celetistas;
> servidor estatutário/RPPS em regra NÃO tem FGTS — **[a confirmar — vínculos celetistas no município]**.

---

## 5. Margem consignável (servidor público)

- **ATENÇÃO — REGRA EM TRANSIÇÃO (2026).** Regra federal anterior: total ≤ **45%** da remuneração
  (35% empréstimos + 5% cartão de crédito consignado + 5% cartão benefício).
- **Novo modelo (MP/Portaria 2026)**: margem global reduzida para **40%**; redução progressiva de 2 p.p./ano
  a partir de 2027 até **30% em 2031**; cartão de crédito/benefício consignado zerados para novas
  operações em 2029.
- Estes são parâmetros do **Executivo FEDERAL** (Lei 14.509/2022 e MP 2026). Para servidor MUNICIPAL,
  a margem pode ser regida por **lei/decreto municipal próprio** → **parametrizar por tenant**.
- FONTE: gov.br/gestão — regras de consignação 2026.
  - https://www.gov.br/gestao/pt-br/assuntos/noticias/2026/maio/conheca-as-novas-regras-de-protecao-prazo-e-margem-do-consignado-no-executivo-federal
  - https://www.gov.br/gestao/pt-br/assuntos/noticias/2026/fevereiro/portaria-atualiza-regras-de-consignacao-em-folha-para-servidoras-e-servidores-1
- **[a confirmar — obter LEI/DECRETO municipal de Maximiliano de Almeida/RS sobre consignações em folha; definir % e base de cálculo da margem (sobre líquido? sobre bruto menos descontos obrigatórios?)]**

> **Motor:** `margemDisponível = baseMargem × percentualMáx − consignadosAtivos`. NÃO permitir desconto
> facultativo que ultrapasse a margem. `percentualMáx` e `baseMargem` = parâmetros do tenant/competência.

---

## 6. 13º salário (gratificação natalina)

- **Tributação EXCLUSIVA na fonte** (art. 12-A da Lei nº 7.713/1988) → base e IRRF calculados
  **SEPARADAMENTE** da folha mensal (não somam à base do salário do mês).
- Incide **INSS/RPPS** sobre o 13º (com tabela própria, base separada) e **IRRF** (tabela progressiva
  aplicada sobre o 13º isoladamente, com dedução de dependentes/pensão sobre essa base).
- 1ª parcela (adiantamento) sem retenção; 2ª parcela com cálculo de INSS+IRRF sobre o total.
- FONTE: PGFN/Receita (Lei 7.713/1988, art. 12-A) — tributação exclusiva.
  - https://www.gov.br/pgfn/pt-br/cidadania-tributaria/por-assunto/imposto-de-renda-pessoa-fisica-irpf-2
- **[a confirmar — Lei 7.713/1988 art. 12-A texto consolidado (Planalto) + regra de proporcionalidade 1/12 por mês trabalhado]**

> **Motor:** módulo de cálculo de 13º com `baseSeparada=true`, aplicando as MESMAS tabelas (INSS/RPPS,
> IRRF) mas sobre evento de competência "13º", não somando à base mensal.

---

## 7. Férias + 1/3 constitucional

- Remuneração de férias = salário + **adicional de 1/3 constitucional** (art. 7º, XVII, CF/CLT; servidor
  estatutário por estatuto/lei municipal).
- **IRRF incide sobre o 1/3 de férias gozadas** (acréscimo patrimonial, sem isenção) — tributado JUNTO
  com a remuneração de férias na competência (NÃO é tributação exclusiva como o 13º).
- **Contribuição previdenciária sobre o 1/3**: STF Tema 985 / STJ (REsp 1.559.926) — incide
  contribuição **patronal**; a incidência sobre a contribuição do **servidor/RPPS** deve ser verificada
  caso a caso → **[a confirmar — situação atual da incidência de CP do segurado sobre o terço de férias após Tema 985]**.
- FONTE: PGFN (IRRF sobre terço de férias); gov.br/previdência (REsp 1.559.926 / Tema 985).
  - https://www.gov.br/pgfn/pt-br/cidadania-tributaria/por-assunto/imposto-de-renda-pessoa-fisica-irpf-2/copy_of_conceito-de-rendimentos-e-verbas-nao-tributaveis/ferias
  - https://www.gov.br/previdencia/pt-br/assuntos/rpps/legislacao-dos-rpps/julgamentos-stj/resp-1559926-contribuicao-sobre-1-3-de-ferias
- **[a confirmar — estatuto dos servidores de Maximiliano de Almeida/RS: regra de aquisição/conversão de férias, abono pecuniário (venda 1/3), e se há terço sobre 13º]**

---

## 8. Modelo de dados sugerido (parametrização — NÃO hardcode)

```
TabelaContribuicaoPrevidenciaria { tenantId?, regime (RGPS|RPPS), competencia, faixas[(limite, aliquota)], teto }
TabelaIRRF                       { competencia, faixas[(limite, aliquota, parcelaDeduzir)], valorDependente, descontoSimplificado }
ParametroMargemConsignavel       { tenantId, competencia, percentualGlobal, sublimites{}, baseCalculo }
Rubrica                          { tenantId, codigo, descricao, tipo, natRubrEsocial, incideCP, incideIRRF, incideFGTS, incideRPPS }
VinculoServidor                  { regimePrevidenciario, categoriaEsocial, dependentesIRRF, ... }
```

---

## 9. Pendências de fonte oficial (consolidado — bloqueiam cálculo correto)

1. **[a confirmar]** Maximiliano de Almeida/RS tem RPPS instituído? Obter lei previdenciária municipal (alíquota servidor + patronal + base). Senão, todos RGPS.
2. **[a confirmar]** Portaria Interministerial MPS/MF jan/2026 — tabela INSS faixas/teto **2026** completa.
3. **[a confirmar]** IN/tabela IRRF vigência **2026** (efeitos Lei 15.270/2025 na retenção mensal).
4. **[a confirmar]** Leiaute eSocial **S-1.3 (NT vigente)** + Tabelas 03/20/21/23 + **MOS eSocial vX.Y** — mapear códigos de incidência → flags de base.
5. **[a confirmar]** Faseamento/modalidade do ente público no eSocial (Simplificado x completo) e eventos de folha aplicáveis (S-1200/S-1207/S-1210).
6. **[a confirmar]** Lei/decreto MUNICIPAL de consignações (% e base da margem).
7. **[a confirmar]** Estatuto dos servidores municipais (férias, abono pecuniário, 13º proporcional, vantagens/gratificações próprias = rubricas locais).
8. **[a confirmar]** Leiaute **TCE-RS** de folha de pagamento (SIAPC/PAD ou módulo de pessoal) — campos e validações da remessa de RH.
9. **[a confirmar]** Incidência atual de CP do segurado sobre 1/3 de férias pós-Tema 985.
10. **[a confirmar]** Salário-mínimo nacional e piso regional/RS vigentes na competência (impactam pisos e base INSS).
