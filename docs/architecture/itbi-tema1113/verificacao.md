# Verificação adversarial — Tema 1.113/STJ vs. motor de ITBI

> **Papel:** cético/auditor jurídico. Este documento confronta cada afirmação dos
> `pesquisa-*.md` contra o **acórdão real do Tema 1.113 (REsp 1.937.821/SP)** e o
> **CTN art. 148**, classificando **CONFIRMADO** / **INCERTO**. Itens incertos seguem
> marcados `[a confirmar]` (CLAUDE.md §16).
> Data da verificação: 2026-06-22. Fontes oficiais primárias (STJ) + secundárias jurídicas.

---

## TL;DR (resposta direta)

**A tese é CONFIRMADA.** A base de cálculo do ITBI é o **valor de mercado da transação**,
e o **valor declarado pelo contribuinte goza de presunção de veracidade**. O município
**não pode** adotar de ofício um "valor de referência" maior como base/piso. Logo,
**`base = MAIOR(valor venal, valor declarado)` no `CalculoItbi.cs` é juridicamente
ilegítimo** à luz do Tema 1.113.

**O que é mito (precisa ser corrigido na narrativa interna):**

1. **Mito:** "a tese diz que base = valor declarado, ponto final / imutável."
   **Realidade:** a base **inicial/presumida** é o valor declarado, mas a presunção é
   **relativa (iuris tantum)** — pode ser afastada. Não é "valor declarado sempre".
2. **Mito:** "o município nunca pode usar valor de referência."
   **Realidade:** pode usá-lo como **indício/gatilho** para *deflagrar* verificação;
   o que é vedado é **impô-lo como base prévia de ofício**.
3. **Mito:** "o arbitramento é livre / automático quando o declarado é menor que a pauta."
   **Realidade:** o arbitramento (art. 148 CTN) **exige processo administrativo regular,
   individualizado, com contraditório**; "ser menor que a pauta" **não** basta.

---

## 1. Identificação do precedente — CONFIRMADO

| Campo | Pesquisa afirma | Verificação |
|---|---|---|
| Tema repetitivo | 1.113 | **CONFIRMADO** (informativo STJ n. 730) |
| Acórdão paradigma | REsp 1.937.821/SP | **CONFIRMADO** |
| Órgão / Relator | 1ª Seção, Min. Gurgel de Faria | **CONFIRMADO** |
| Julgamento / DJe | 24/02/2022 · DJe 03/03/2022 · unânime | **CONFIRMADO** |
| Natureza | Recurso repetitivo (aplicação imediata) | **CONFIRMADO** |

Fonte primária: Informativo de Jurisprudência STJ n. 730, com a ementa-resumo exata:
*"ITBI. Base de cálculo. IPTU. Vinculação. Inexistência. Valor venal declarado pelo
contribuinte. Presunção de veracidade. Revisão pelo fisco. Processo administrativo.
Possibilidade. Adoção de prévio valor de referência. Inviabilidade. Tema 1113."*

---

## 2. As três teses — CONFIRMADO (texto literal conferido em fonte STJ)

Texto conferido contra a **notícia oficial do STJ (09/03/2022)** e o **Informativo 730**:

> **(a)** "A base de cálculo do ITBI é o valor do imóvel transmitido em condições normais
> de mercado, não estando vinculada à base de cálculo do IPTU, que nem sequer pode ser
> utilizada como piso de tributação;"
>
> **(b)** "o valor da transação declarado pelo contribuinte goza da presunção de que é
> condizente com o valor de mercado, que somente pode ser afastada pelo fisco mediante a
> regular instauração de processo administrativo próprio (art. 148 do CTN);"
>
> **(c)** "o município não pode arbitrar previamente a base de cálculo do ITBI com respaldo
> em valor de referência por ele estabelecido unilateralmente."

**Classificação: CONFIRMADO.** As três proposições reproduzidas nos `pesquisa-*.md` batem
com a fonte primária do STJ. A pendência anterior de "conferir grafia exata do trecho
*que nem sequer pode ser utilizada como piso*" está **resolvida**: o trecho consta da
notícia oficial do STJ e do enunciado do tema.

`[a confirmar]` (resíduo menor): a grafia caractere-a-caractere do **inteiro teor / DJe**
(o link `processo.stj.jus.br/.../GetInteiroTeorDoAcordao` não abriu neste ambiente). Não
altera o mérito — múltiplas fontes oficiais e secundárias convergem.

---

## 3. "A tese é mesmo *base = valor declarado presumido*?" — CONFIRMADO, com nuance

**Sim, CONFIRMADO**, mas com a nuance que evita o mito interno:

- A base **não é** "valor declarado por imposição"; é o **valor de mercado**. O valor
  declarado **presume-se** igual ao de mercado (presunção **relativa**). Trecho do voto
  (Min. Gurgel de Faria), conferido em fontes do julgado:
  > *"O valor da transação declarado pelo contribuinte presume-se condizente com o valor
  > médio de mercado do bem imóvel transacionado, presunção que somente pode ser afastada
  > pelo fisco se esse valor se mostrar, de pronto, incompatível com a realidade."*
- Portanto, para o motor, o **default correto é `base = valor declarado`** — e o valor de
  referência **não** entra na aritmética da base. **A regra `MAX(...)` está errada.**

**Mito derrubado:** não existe "valor declarado é intocável". Existe presunção que **cede**
ao arbitramento regular (item 5).

---

## 4. "Há exceção que permita o município usar valor de referência?" — CONFIRMADO (sim, limitada)

**Sim, há — mas estritamente limitada.** Confronto contra o acórdão:

- O valor de referência **NÃO pode** ser: base de ofício, piso de tributação, nem parâmetro
  imposto previamente. Trecho do voto:
  > *"A prévia adoção de um valor de referência pela Administração configura indevido
  > lançamento de ofício do ITBI por mera estimativa e subverte o procedimento instituído
  > no art. 148 do CTN."*
- O valor de referência **PODE**, no máximo, funcionar como **indício/gatilho** para
  *deflagrar* o procedimento de verificação — nunca para antecipar o juízo de valor.

**Classificação: CONFIRMADO.** A afirmação dos `pesquisa-*.md` de que "o valor de referência
pode servir de gatilho/indício, nunca de base" está correta e fiel ao acórdão.

---

## 5. "O arbitramento exige processo?" — CONFIRMADO (sim, processo administrativo + contraditório)

**Sim, CONFIRMADO — é o ponto central.** Confronto contra **CTN art. 148** e o acórdão:

- **CTN art. 148** (teor conferido em fontes secundárias convergentes; texto primário do
  Planalto **indisponível neste ambiente** — ver §7):
  > *"Quando o cálculo do tributo tenha por base, ou tome em consideração, o valor ou o preço
  > de bens, direitos, serviços ou atos jurídicos, a autoridade lançadora, mediante processo
  > regular, arbitrará aquele valor ou preço, sempre que sejam omissos ou não mereçam fé as
  > declarações ou os esclarecimentos prestados, ou os documentos expedidos pelo sujeito
  > passivo ou pelo terceiro legalmente obrigado, ressalvada, em caso de contestação,
  > avaliação contraditória, administrativa ou judicial."*
- O acórdão exige **procedimento próprio, individualizado, com contraditório**:
  > *"procedimento próprio para o arbitramento da base de cálculo, em que deve ser assegurado
  > ao contribuinte o contraditório necessário para apresentação das peculiaridades."*
- Fundamento estrutural (CONFIRMADO): o ITBI admite **lançamento por declaração** ou **por
  homologação**; *"a imposição de prévio arbitramento da base de cálculo é incompatível com
  o lançamento por homologação"*. Por isso o arbitramento **prévio/de ofício** é vedado.

**Requisitos cumulativos do arbitramento legítimo (todos CONFIRMADOS contra art. 148 + acórdão):**
1. Base inicial = valor declarado (presunção de veracidade).
2. Só cabe quando a declaração for **omissa, falsa ou não merecer fé** — não basta "menor que a pauta".
3. **Processo administrativo regular e individualizado**.
4. **Contraditório e ampla defesa** (avaliação contraditória — art. 148, parte final).
5. **Ônus da prova do fisco**.
6. Valor de referência apenas **deflagra**, não é base.

**Classificação: CONFIRMADO.** A leitura dos `pesquisa-*.md` sobre o art. 148 é fiel.

---

## 6. Itens dos `pesquisa-*.md` que rebaixo para INCERTO

| Afirmação | Classificação | Motivo |
|---|---|---|
| **Contraditório diferido** (abrir arbitramento *após* lançar pelo declarado) é válido | **INCERTO** | É **tese de PGM (e-book Joinville 2025)**, **não** é parte do Tema 1.113 nem súmula. Defensável, mas **não confirmado** como entendimento vinculante. Validar com a procuradoria. `[a confirmar]` |
| **LC 227/2026 (ex-PLP 108/2024)**: base do ITBI por "critérios técnicos", veto à antecipação, conteúdo do procedimento de contestação | **INCERTO** | Redação final, **vetos**, vigência e regra de transição **não conferidos no texto oficial (DOU 14/01/2026)**. A premissa de que "não revoga a presunção do declarado" é razoável, mas a disciplina exata é `[a confirmar]`. Não usar como base de design sem o texto oficial. |
| CTM de **Maximiliano de Almeida/RS** prevê pauta/valor de referência como base ou piso | **INCERTO** | Lei municipal **não verificada**. Se previr piso/base por pauta, é **inconstitucional/ilegal** sob o Tema 1.113 e precisa de adequação. `[a confirmar]` |
| Sistemática de lançamento do ITBI no município (declaração x homologação) | **INCERTO** | Define o desenho do workflow de arbitramento; **não verificado**. `[a confirmar]` |
| **STF**: existe tese/repercussão geral própria sobre o ponto | **INCERTO** | A pesquisa cobriu o **STJ**. Não confirmado se há tese do STF que altere o alcance. `[a confirmar]` |
| Texto **primário** (Planalto) do CTN art. 148 / art. 38 | **INCERTO (fonte primária)** | `planalto.gov.br` **indisponível neste ambiente** (socket fechado/redirect). Texto conferido só em **fontes secundárias convergentes**. Conferir no Planalto antes de citar em parecer. `[a confirmar]` |
| Inteiro teor / DJe 03/03/2022 caractere-a-caractere | **INCERTO (resíduo)** | `GetInteiroTeorDoAcordao` não abriu aqui. Mérito confirmado por fontes oficiais; grafia literal do DJe pendente. `[a confirmar]` |

---

## 7. Limitações desta verificação (transparência)

- **Fontes primárias bloqueadas neste ambiente:** `planalto.gov.br` (CTN), inteiro teor do
  acórdão no `processo.stj.jus.br`, e agregadores (`jusbrasil`, `legjur`) retornaram
  403/socket fechado. O **mérito** foi confirmado pela **notícia oficial do STJ**, pelo
  **Informativo STJ n. 730** e por trechos do voto reproduzidos em fontes do julgado; o
  **texto literal de leis** depende de fontes secundárias convergentes.
- Nada aqui substitui **parecer da procuradoria** do município antes de produção (CLAUDE.md §16).

---

## 8. Veredito para o motor (`CalculoItbi.cs`)

- **`base = MAIOR(valor venal de referência, valor declarado)` → ILEGÍTIMO. CONFIRMADO.**
  Viola as três teses (vincula a valor venal; ignora presunção do declarado; arbitra
  unilateralmente de ofício). O `TODO(validar-oficial)` (linhas 50-56) e o booleano
  `BaseFoiValorVenal` (linhas 27/36/84-85/97) materializam o conflito.
- **Direção CONFIRMADA:** `base = valor declarado` por padrão; valor de referência vira
  **indício auditável**; elevação de base **somente** via `ResultadoArbitramento` ligado a
  **processo administrativo art. 148** (nº do processo, fundamentação, contraditório
  registrado) — modelado como **workflow/estado**, não como `MAX` aritmético. Memória de
  cálculo deve registrar **origem da base**: `Declarada` | `ArbitradaArt148`.
- **Exige ADR** (decisão de produto + procuradoria) antes de alterar a regra do M6-DESIGN.

---

## 9. FONTES (verificação)

**Oficiais / primárias**
- STJ — Notícia oficial (3 teses), 09/03/2022:
  <https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/09032022-Base-de-calculo-do-ITBI-e-o-valor-do-imovel-transmitido-em-condicoes-normais-de-mercado--define-Primeira-Secao.aspx>
- STJ — Informativo de Jurisprudência n. 730 (Tema 1.113, REsp 1.937.821-SP):
  <https://scon.stj.jus.br/jurisprudencia/externo/informativo/?aplicacao=informativo&acao=pesquisar&livre=%40CNOT%3D%27018919%27>
- STJ — Inteiro teor do acórdão (REsp 1.937.821) `[não abriu neste ambiente — a confirmar]`:
  <https://processo.stj.jus.br/SCON/GetInteiroTeorDoAcordao?num_registro=202000120791&dt_publicacao=03/03/2022>
- CTN — Lei 5.172/66 (Planalto, art. 148/38) `[indisponível neste ambiente — a confirmar]`:
  <https://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm>

**Secundárias (convergentes, usadas p/ texto literal de lei e trechos do voto)**
- REsp 1.937.821-SP (resumo com trechos do voto) — Trilhante:
  <https://informativos.trilhante.com.br/julgados/stj-resp-1937821-sp>
- PGM Joinville — e-book "Arbitramento da base de cálculo do ITBI pós-Tema 1113/STJ"
  (RPGMJ, Ano 3, n.1, 2025) — fonte da tese do **contraditório diferido** (INCERTO):
  <https://www.joinville.sc.gov.br/wp-content/uploads/2026/02/Arbitramento-da-base-de-calculo-do-ITBI-pos-Tema-1113STJ-preponderancia-do-processo-administrativo-proprio-e-validade-do-contraditorio-diferido-EBOOK-RPGMJ-Ano-3-Numero-1-2025.pdf>
- CNM — Tema 1.113/STJ, conceito de base de cálculo: <https://cnm.org.br/biblioteca/download/15600>
- Migalhas — "Tema 1.113/STJ e a base de cálculo do ITBI":
  <https://www.migalhas.com.br/depeso/447875/tema-1-113-stj-e-a-base-de-calculo-do-itbi-nos-negocios-imobiliarios>
