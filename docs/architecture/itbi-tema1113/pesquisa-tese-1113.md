# Pesquisa — Tema 1.113/STJ e a base de cálculo do ITBI

> Pesquisa de fontes oficiais para conciliar o motor de cálculo do ITBI
> (`CalculoItbi.cs`) com o entendimento vinculante do STJ. Ver CLAUDE.md §16.
> **Status:** achado de avaliação independente — o motor atual conflita com a tese.

---

## 1. Identificação do precedente

| Campo | Valor |
|---|---|
| **Tema repetitivo** | Tema **1.113** do STJ |
| **Acórdão paradigma** | **REsp 1.937.821/SP** (recurso especial repetitivo) |
| **Órgão julgador** | **1ª Seção** do STJ |
| **Relator** | Ministro **Gurgel de Faria** |
| **Julgamento** | **24/02/2022**, por unanimidade (sessão noticiada em 09/03/2022) |
| **Publicação (DJe)** | **03/03/2022** |
| **Aplicação** | Imediata (recurso repetitivo) — independe de trânsito em julgado do paradigma |

> Afetação em 11/11/2021. Questão afetada: (a) se a base de cálculo do ITBI está
> vinculada à do IPTU; (b) se é legítima a adoção de "valor venal de referência"
> fixado previamente pelo fisco municipal como parâmetro do ITBI.

---

## 2. Tese firmada (texto literal — 3 proposições)

> **a)** "A base de cálculo do ITBI é o valor do imóvel transmitido em condições
> normais de mercado, não estando vinculada à base de cálculo do IPTU, que nem
> sequer pode ser utilizada como piso de tributação;"
>
> **b)** "o valor da transação declarado pelo contribuinte goza da presunção de
> que é condizente com o valor de mercado, que somente pode ser afastada pelo
> fisco mediante a regular instauração de processo administrativo próprio
> (art. 148 do CTN);"
>
> **c)** "o município não pode arbitrar previamente a base de cálculo do ITBI com
> respaldo em valor de referência por ele estabelecido unilateralmente."

---

## 3. Consequências para o motor (`CalculoItbi.cs`)

O motor atual adota **`base = MAIOR(valor venal de referência, valor declarado)`**
(`CalculadoraItbi.Calcular`, linhas 84-85: `baseFoiValorVenal = valorVenalReferencia.Valor >= valorDeclarado.Valor`).
Isso **CONFLITA** com o Tema 1.113 em três pontos:

1. **Tese (a):** vincula a base ao valor venal (de IPTU/PGV) — vedado; o valor
   venal não pode sequer ser piso de tributação.
2. **Tese (b):** ignora a **presunção de veracidade do valor declarado**; o motor
   sobrepõe o valor de referência sem processo administrativo (art. 148 CTN).
3. **Tese (c):** usa "valor venal de referência" fixado unilateralmente pelo
   município como base prévia — exatamente a prática reprovada.

**Direção correta (a confirmar com a procuradoria):** base **= valor declarado**
por padrão; o valor de referência é **meramente indicativo** e só pode prevalecer
via **arbitramento por processo administrativo (art. 148 CTN)** com contraditório
e ampla defesa. ITBI pago a maior nessa hipótese gera direito à restituição
(art. 165, I, CTN).

---

## 4. FONTES

- STJ (notícia oficial, 09/03/2022) — "Base de cálculo do ITBI é o valor do imóvel
  transmitido em condições normais de mercado, define Primeira Seção":
  <https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/09032022-Base-de-calculo-do-ITBI-e-o-valor-do-imovel-transmitido-em-condicoes-normais-de-mercado--define-Primeira-Secao.aspx>
- STJ (notícia de afetação, 18/11/2021):
  <https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/18112021%20Primeira-Secao-decidira-sobre-parametros-para-fixacao-da-base-de-calculo-do-ITBI.aspx>
- CTN — Lei 5.172/1966, **art. 148** (arbitramento por processo administrativo) e
  **art. 165, I** (restituição de tributo pago a maior):
  <http://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm>
- CF/88 — art. 156, II e §2º, I (competência do ITBI / imunidades):
  <http://www.planalto.gov.br/ccivil_03/constituicao/constituicao.htm>
- NUGEP/TRF1 — Boletim 08/2022 (publicação do acórdão do Tema 1113):
  <https://www.trf1.jus.br/trf1/conteudo/files/Boletim%20NUGEP%2008%202022.pdf>
- TJMG — ficha do recurso repetitivo Tema 1113 (datas processuais):
  <https://www.tjmg.jus.br/portal-tjmg/jurisprudencia/recurso-repetitivo-e-repercussao-geral/detalhes-de-recurso-repetitivo-8ACC812583D2CB7801841FE0B5241B20.htm>
- CNM — "Tema 1.113 do STJ: conceito de base de cálculo do ITBI":
  <https://cnm.org.br/biblioteca/download/15600>
- Migalhas (doutrina) — "Tema 1.113/STJ e a base de cálculo do ITBI nos negócios
  imobiliários": <https://www.migalhas.com.br/depeso/447875/tema-1-113-stj-e-a-base-de-calculo-do-itbi-nos-negocios-imobiliarios>

---

## 5. Pendências [a confirmar] (CLAUDE.md §16)

- **[a confirmar]** Texto literal da tese conferido na **íntegra do acórdão / DJe
  03/03/2022** (não apenas notícia/boletins) — a redação do trecho "que nem sequer
  pode ser utilizada como piso de tributação" provém de fontes secundárias e do
  enunciado do tema; confirmar grafia exata no inteiro teor.
- **[a confirmar]** **Modulação de efeitos** e eventual repercussão posterior
  (ex.: pedidos de restituição, RE/STF). A aplicação é imediata; verificar se há
  embargos/QO que alterem o alcance.
- **[a confirmar]** Decisão de produto/procurador: redesenhar `CalculoItbi.cs`
  para **base = valor declarado + fluxo de arbitramento (art. 148 CTN)**, em vez
  de `MAIOR(...)`. Hoje o código tem TODO(validar-oficial) reconhecendo o conflito
  (linhas 50-56), mas a regra implementada segue o M6-DESIGN — exige ADR.
- **[a confirmar]** Como o **CTM de Maximiliano de Almeida/RS** disciplina a base
  do ITBI e o "valor de referência" — alinhar lei municipal ao Tema 1.113.
