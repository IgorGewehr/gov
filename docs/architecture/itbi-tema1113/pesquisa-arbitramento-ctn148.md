# Pesquisa — Arbitramento da base do ITBI (CTN art. 148) pós-Tema 1.113/STJ

> Módulo **Tributos** · contexto: a avaliação independente apontou que o motor de ITBI usa
> `base = MAIOR(valor venal, valor declarado)` em
> `src/Modules/Tributos/Tensorroot.Gov.Modules.Tributos.Domain/Calculo/CalculoItbi.cs`,
> o que **conflita** com o Tema 1.113/STJ.
> Este documento levanta como o município pode **legitimamente** contestar o valor declarado.
> Data da pesquisa: 2026-06-22. Itens incertos marcados `[a confirmar]` (CLAUDE.md §16).

---

## 1. O problema no código atual

`CalculadoraItbi.Calcular` (linhas 84-85) define:

```csharp
var baseFoiValorVenal = valorVenalReferencia.Valor >= valorDeclarado.Valor;
var baseCalculo = baseFoiValorVenal ? valorVenalReferencia : valorDeclarado;
```

Ou seja: **base = MAIOR(valor venal de referência, valor declarado)**. Isso é exatamente a
prática vedada pelo STJ — adotar **de ofício** um valor de referência municipal como piso/base
quando ele é maior que o declarado, sem instaurar processo administrativo. O próprio arquivo já
sinaliza o risco no `TODO(validar-oficial)` (linhas 50-56). **Confirmado: a regra atual é ilegítima
à luz do Tema 1.113.**

---

## 2. Tema 1.113/STJ — as 3 teses (REsp 1.937.821-SP)

Primeira Seção, Rel. Min. Gurgel de Faria, **j. 24/02/2022, DJe 03/03/2022**, recurso repetitivo
(Informativo STJ 730). Teses firmadas (texto oficial):

1. **"A base de cálculo do ITBI é o valor do imóvel transmitido em condições normais de mercado,
   não estando vinculada à base de cálculo do IPTU"** — e a base do IPTU **não pode sequer ser usada
   como piso**.
2. **"O valor da transação declarado pelo contribuinte goza da presunção de que é condizente com o
   valor de mercado, que somente pode ser afastada pelo fisco mediante a regular instauração de
   processo administrativo próprio (art. 148 do CTN)."**
3. **"O município não pode arbitrar previamente a base de cálculo do ITBI com respaldo em valor de
   referência por ele estabelecido de forma unilateral."**

Fundamentos centrais do acórdão (FONTES §7):
- O ITBI é tributo **lançado por declaração** (ou homologação, conforme a sistemática municipal);
  por isso prevalece a presunção de boa-fé/veracidade da declaração do contribuinte. *"A imposição de
  prévio arbitramento da base de cálculo é incompatível com o lançamento por homologação."*
- Adotar valor de referência prévio do fisco caracteriza **lançamento de ofício** indevido e gera
  **inversão do ônus da prova** em desfavor do contribuinte — **viola o art. 148 do CTN**.
- O valor de referência municipal pode, **no máximo**, servir de **gatilho/indício** para *iniciar*
  um procedimento de verificação — **nunca** para antecipar o juízo sobre o valor.

---

## 3. O que o município PODE fazer — arbitramento via CTN art. 148

**CTN, art. 148 (teor) `[a confirmar texto literal contra Planalto — fonte direta falhou no ambiente; teor abaixo conferido em múltiplas fontes secundárias]`:**
> *"Quando o cálculo do tributo tenha por base, ou tome em consideração, o valor ou o preço de bens,
> direitos, serviços ou atos jurídicos, a autoridade lançadora, mediante processo regular, arbitrará
> aquele valor ou preço, sempre que sejam omissos ou não mereçam fé as declarações ou os
> esclarecimentos prestados, ou os documentos expedidos pelo sujeito passivo ou pelo terceiro
> legalmente obrigado, ressalvada, em caso de contestação, avaliação contraditória, administrativa ou
> judicial."*

Requisitos cumulativos para um arbitramento **legítimo** (FONTES §7):
- **a)** A base **inicial** é o **valor declarado** pelo contribuinte (presunção de veracidade).
- **b)** Só cabe arbitrar quando a declaração for **omissa, falsa ou não merecer fé** — não basta
  ser "menor que a pauta de referência".
- **c)** Exige **processo administrativo regular, individualizado e prévio** ao lançamento revisto.
- **d)** **Contraditório e ampla defesa** assegurados; o contribuinte pode **justificar o valor
  declarado** (estado de conservação, ônus, peculiaridades do negócio) e oferecer **avaliação
  contraditória**.
- **e)** O **ônus da prova é do fisco**: cabe à administração demonstrar que o valor declarado está
  **fora das condições normais de mercado**.
- **f)** O valor de referência só pode **deflagrar** o procedimento — não pode ser a base imposta.

**Contraditório prévio x diferido:** doutrina/procuradorias municipais (e-book RPGM Joinville, 2025)
sustentam a **validade do contraditório diferido** — abrir o processo de arbitramento *após* o
lançamento pelo valor declarado, com defesa garantida em seguida — desde que respeitado o art. 148.
`[a confirmar]` se essa tese é aplicável ao perfil do piloto (Maximiliano de Almeida/RS) — validar com
a procuradoria; é entendimento de PGM, não súmula vinculante.

---

## 4. O que o município NÃO PODE fazer

- ❌ Adotar **base = MAIOR(valor venal, declarado)** automaticamente (o bug atual).
- ❌ Usar **valor venal/base do IPTU** como base ou como **piso** do ITBI.
- ❌ **Arbitrar de ofício** com valor de referência/pauta fixada **unilateralmente**, sem processo.
- ❌ **Presumir** base maior e inverter o ônus da prova contra o contribuinte.
- ❌ Emitir guia já no valor de referência exigindo que o contribuinte impugne depois **sem** processo
  do art. 148 instaurado.

---

## 5. PLP 108/2024 → **LC 227/2026** (mudança recente relevante)

O PLP 108/2024 foi **sancionado e convertido na Lei Complementar nº 227/2026** (publicada no DOU em
**14/01/2026**), no bojo da regulamentação da Reforma Tributária (altera também a LC 214/2025). Pontos
relevantes para o ITBI `[a confirmar redação final, vetos e vigência contra o texto oficial da LC]`:
- Define que a base do ITBI **corresponde ao valor de mercado** do imóvel, apurado por **critérios
  técnicos** (preços de mercado, localização, dados de cartórios e agentes financeiros).
- Mantém ao contribuinte o **direito de contestar** o valor atribuído mediante **comprovação técnica**.
- **Veto presidencial** à antecipação facultativa do ITBI na formalização do título (insegurança
  jurídica).

**Tensão com o Tema 1.113:** a LC 227/2026 reabre espaço para o município **estruturar uma metodologia
técnica de valor de referência** — porém **não** revoga a exigência de presunção do valor declarado,
processo próprio e contraditório. Para o motor, isso significa: o valor de referência pode existir como
**parâmetro técnico/indício**, mas **continua sem poder ser imposto como base de ofício**.
`[a confirmar]` como a LC 227/2026 disciplina exatamente o procedimento de contestação e se há regra de
transição/eficácia que afete o piloto em 2026.

---

## 6. Implicações de design para o motor (`CalculoItbi.cs`)

1. **Trocar a regra de base:** base de cálculo padrão = **valor declarado** (não o maior).
   O `valorVenalReferencia` passa a ser **referência/indício auditável**, nunca base automática.
2. **Modelar o arbitramento como estado/workflow** (não como `MAX` aritmético): a base só pode subir
   para um valor arbitrado quando houver um **processo administrativo art. 148** vinculado (com nº de
   processo, fundamentação, contraditório registrado). Sugere-se um `ResultadoArbitramento` que vira
   entrada do cálculo, em vez de o motor decidir sozinho.
3. **Memória de cálculo (`MemoriaItbi`)** deve registrar a **origem da base** entre: `Declarada`,
   `ArbitradaArt148` (com referência ao processo) — substituindo o booleano `BaseFoiValorVenal`.
4. **Sinalização (flag) de divergência:** quando `declarado < referência` por margem parametrizável
   por tenant, emitir **alerta** para *deflagrar* (opcionalmente) o processo — sem alterar a base.
5. **Auditoria** (CLAUDE.md §6): toda elevação de base precisa de trilha ligada ao processo do art. 148.
6. **Nada hardcoded** (CLAUDE.md §7): margens, alíquotas e a metodologia de referência por tenant/CTM.

---

## 7. FONTES

**Oficiais / primárias**
- STJ — Notícia oficial do julgamento (3 teses): <https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/09032022-Base-de-calculo-do-ITBI-e-o-valor-do-imovel-transmitido-em-condicoes-normais-de-mercado--define-Primeira-Secao.aspx>
- STJ — Informativo de Jurisprudência n. 730 (Tema 1.113, REsp 1.937.821-SP): <https://scon.stj.jus.br/jurisprudencia/externo/informativo/?aplicacao=informativo&acao=pesquisar&livre=%40CNOT%3D%27018919%27>
- STJ — Inteiro teor do acórdão (REsp 1.937.821): <https://processo.stj.jus.br/SCON/GetInteiroTeorDoAcordao?num_registro=202000120791&dt_publicacao=03/03/2022>
- STJ — "ITBI e IPTU: o STJ e os impostos municipais sobre imóveis (parte 1)": <https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/2022/16102022-ITBI-e-IPTU-o-STJ-e-os-impostos-municipais-que-incidem-sobre-imoveis--parte-1-.aspx>
- Congresso Nacional — PLP 108/2024 (tramitação): <https://www.congressonacional.leg.br/materias/materias-bicamerais/-/ver/plp-108-2024>
- CTN — Lei 5.172/66 (Planalto, texto compilado): <https://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm> `[a confirmar: indisponível/instável neste ambiente — texto do art. 148 e art. 38 não foi conferido na fonte primária direta]`

**Procuradoria municipal / doutrina**
- PGM Joinville — e-book "Arbitramento da base de cálculo do ITBI pós-Tema 1113/STJ: preponderância do processo administrativo próprio e validade do contraditório diferido" (RPGMJ, Ano 3, n.1, 2025): <https://www.joinville.sc.gov.br/wp-content/uploads/2026/02/Arbitramento-da-base-de-calculo-do-ITBI-pos-Tema-1113STJ-preponderancia-do-processo-administrativo-proprio-e-validade-do-contraditorio-diferido-EBOOK-RPGMJ-Ano-3-Numero-1-2025.pdf>
- CNM — Tema 1.113/STJ, conceito de base de cálculo: <https://cnm.org.br/biblioteca/download/15600>
- Migalhas — "Tema 1.113/STJ e a base de cálculo do ITBI": <https://www.migalhas.com.br/depeso/447875/tema-1-113-stj-e-a-base-de-calculo-do-itbi-nos-negocios-imobiliarios>
- ConJur (Ribeiro Brazuna) — "Lançamentos complementares de ITBI": <https://www.conjur.com.br/2023-set-01/ribeiro-brazuna-lancamentos-complementares-itbi/>
- Pedrosa Soares e Esteves — "ITBI e a base de cálculo: Tema 1113 do STJ e PLP 108/2024": <https://pse.adv.br/itbi-e-a-base-de-calculo-tema-1113-do-stj-e-plp-108-2024/>
- Mattos Filho / IOB — aprovação e sanção do PLP 108/2024: <https://www.mattosfilho.com.br/unico/reforma-tributaria-aprovado-plp-ccj/>
- BVP Advogados — sanção do PLP 108/24 e conversão na LC 227/2026: <https://bvp.adv.br/reforma-tributaria-sancao-do-plp-no-108-24-e-conversao-na-lei-complementar-no-227-26-cgibs-ibs/>

---

## 8. Pendências (`[a confirmar]`)

1. **Texto literal** do CTN art. 148 e art. 38 contra o **Planalto** (fonte primária falhou aqui).
2. **LC 227/2026 (ex-PLP 108/2024):** redação final do dispositivo do ITBI, **vetos**, vigência e o
   procedimento de contestação — conferir no texto oficial publicado no DOU 14/01/2026.
3. **Aplicação ao piloto (Maximiliano de Almeida/RS):** o **CTM** local prevê pauta de valores / valor
   de referência como base ou piso? Precisa de adequação? Validar com a procuradoria do município.
4. **Contraditório diferido:** confirmar admissibilidade no rito administrativo adotado pelo tenant.
5. **Sistemática de lançamento do ITBI** no município (por declaração x homologação) — define como o
   workflow de arbitramento se encaixa.
6. Conferir se o **STF** tem tese própria/repercussão geral sobre o ponto (a pesquisa tratou do STJ).
