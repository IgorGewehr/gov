# Pesquisa — PPA / LDO / LOA: a estrutura legal do planejamento orçamentário municipal

> Módulo **Financas**. Insumo para fechar o GAP apontado pela avaliação independente.
> Data: 2026-06-22. Fontes oficiais (Planalto/CF, Lei 4.320/1964, LC 101/2000 LRF, Câmara dos
> Deputados, STF, ADCT). Itens incertos marcados **[a confirmar]** conforme §16 da Constituição
> de Engenharia.

---

## 0. Confirmação do GAP (estado do código)

- **Zero referências** a PPA/LDO/LOA/Plano Plurianual/Diretrizes Orçamentárias no **código-fonte**
  (`/src`, excluindo `/dist`): `grep -riE "planoplurianual|\bppa\b|\bldo\b|\bloa\b" src --include=*.ts --include=*.prisma | grep -v /dist/` → **0**.
  As únicas ocorrências aparecem em **bundles minificados** (`src/Web/dist/assets/*.js`), ou seja,
  strings de build, não modelagem.
- O **backend .NET** (`src/Modules/Financas`) só possui a camada de **execução**:
  - `Domain/Dotacoes/DotacaoOrcamentaria.cs` — crédito orçamentário (saldo p/ empenho).
  - `Domain/ValueObjects/ClassificacaoOrcamentaria.cs` — órgão, UO, funcional-programática, categoria econômica, fonte.
  - `Domain/Liquidacoes/`, `Empenho`, `Pagamento`, `Contabilidade/Msc/MatrizSaldosContabeis.cs` (PCASP/MSC).
- **Não existem** entidades de **planejamento**: `find . -name "*.cs" | grep -iE "ppa|ldo|loa|planejamento|plurianual|diretriz|qdd|programa|acao"` → só handlers de **Dotacao** (execução).
- **Contradição documentada:** o próprio `CLAUDE.md` (§4, módulo 2) declara o escopo de Financas como
  "Orçamento **(PPA/LDO/LOA)**, Empenho→Liquidação→Pagamento (Lei 4.320)…" — o planejamento está
  na **spec** mas **ausente do código**. Só existe o elo final da cadeia: a `DotacaoOrcamentaria`,
  cujo `ValorDotadoInicial` é comentado como "Dotação aprovada na LOA" — mas a **LOA não é modelada**;
  a dotação nasce solta, sem origem em LOA→LDO→PPA, sem QDD, sem programas/ações/metas.

**Conclusão:** o sistema modela a **execução** orçamentária sem o **planejamento** que a fundamenta
e a vincula juridicamente. Isso é, de fato, **reprovador de PoC municipal** [RECORRENTE]: nenhum
Tribunal de Contas / Lei Orgânica aceita execução sem PPA/LDO/LOA vigentes e compatíveis, e sem
o controle de compatibilidade entre os instrumentos.

---

## 1. Fundamento constitucional — CF/1988, art. 165

A CF/88 instituiu a **tríade orçamentária** no **art. 165**, leis de iniciativa do Poder Executivo:

- **art. 165, caput, I, II, III** — estabelecerá: **I** o plano plurianual; **II** as diretrizes
  orçamentárias; **III** os orçamentos anuais.
- **art. 165, §1º — PPA:** "A lei que instituir o plano plurianual estabelecerá, **de forma
  regionalizada**, as **diretrizes, objetivos e metas** da administração pública […] para as
  **despesas de capital** e outras delas decorrentes e para as relativas aos **programas de
  duração continuada**." (no plano municipal lê-se "administração pública municipal").
- **art. 165, §2º — LDO:** "A lei de diretrizes orçamentárias compreenderá as **metas e
  prioridades** da administração pública […], incluindo as **despesas de capital para o exercício
  financeiro subsequente**, **orientará a elaboração da lei orçamentária anual**, disporá sobre as
  **alterações na legislação tributária** e estabelecerá a política de aplicação das agências
  financeiras oficiais de fomento."
- **art. 165, §5º — LOA:** compreende **três orçamentos** — **fiscal** (Poderes, fundos, órgãos e
  entidades da administração direta e indireta); **de investimento das empresas** estatais; e da
  **seguridade social**. [No nível municipal, na prática a LOA concentra-se no orçamento fiscal +
  seguridade; o de investimento de estatais costuma ser inexpressivo — **[a confirmar]** caso a
  caso conforme a Lei Orgânica do município.]
- **art. 165, §7º — compatibilidade/regionalização:** os orçamentos do §5º, I e II, "terão entre
  suas funções a de **reduzir desigualdades inter-regionais**, segundo critério populacional" e
  serão **compatibilizados com o plano plurianual**.
- **art. 165, §8º — exclusividade orçamentária:** "A lei orçamentária anual **não conterá dispositivo
  estranho à previsão da receita e à fixação da despesa**, não se incluindo na proibição a autorização
  para abertura de **créditos suplementares** e contratação de operações de crédito…"
- **art. 165, §9º** — remete a **lei complementar** (ainda não editada) para dispor sobre o exercício
  financeiro, vigência, prazos, elaboração e organização do PPA, LDO e LOA. Enquanto não vier, valem
  os **prazos do ADCT art. 35, §2º** (ver §6).

**Vedações correlatas — art. 167 (citado pela tarefa):**
- **art. 167, I** — veda "o **início de programas ou projetos não incluídos na lei orçamentária anual**".
- **art. 167, II** — veda "a realização de **despesas ou a assunção de obrigações diretas que excedam
  os créditos orçamentários** ou adicionais".
- **art. 167, §1º** — nenhum **investimento cujo período de execução ultrapasse um exercício** pode ser
  iniciado **sem prévia inclusão no PPA** (ou lei autorizadora), sob pena de crime de responsabilidade.

> Esses dispositivos são o **núcleo da regra de compatibilidade** que o software precisa **fazer
> cumprir**: dotação na LOA → coberta por programa/ação do PPA → priorizada na LDO; empenho/despesa
> jamais excede o crédito orçamentário (isto o código **já** garante em `DotacaoOrcamentaria.ReservarEmpenho`).

---

## 2. PPA — Plano Plurianual (médio prazo, 4 anos)

- **Vigência:** **4 anos**, defasada do mandato — vai do **2º ano de um mandato ao 1º ano do mandato
  seguinte** (garante continuidade na transição de governo). Fonte: Câmara dos Deputados / ADCT.
- **Conteúdo (CF 165 §1º):** **diretrizes, objetivos e metas**, de forma **regionalizada**, para
  **despesas de capital** e decorrentes, e **programas de duração continuada**.
- **Estrutura típica (modelagem-alvo):**
  - **Programas** (unidade básica de organização da ação governamental, com objetivo e público-alvo).
  - **Ações** (projetos, atividades, operações especiais) vinculadas a cada programa.
  - **Metas físicas** (produto + unidade de medida + quantidade) e **metas financeiras** (valor por
    ano do quadriênio) de cada ação.
  - **Regionalização** (distribuição territorial das metas — bairro/distrito/região no município).
  - **Indicadores** de programa (linha de base + meta).
- **Vínculo:** nenhum investimento plurianual pode iniciar sem estar no PPA (CF 167 §1º). É a **âncora
  de médio prazo** de toda a cadeia.

---

## 3. LDO — Lei de Diretrizes Orçamentárias (anual, o "elo")

- **Função (CF 165 §2º):** é o **elo entre PPA e LOA** — seleciona, dentre os programas do PPA, as
  **metas e prioridades** do **exercício subsequente**, **orienta a elaboração da LOA** e dispõe sobre
  **alterações na legislação tributária**.
- **Periodicidade:** **anual** (uma LDO por exercício, antecedendo cada LOA).
- **Anexos obrigatórios da LRF (LC 101/2000, art. 4º):**
  - **art. 4º, caput e incisos** — a LDO disporá sobre: **equilíbrio entre receitas e despesas**;
    **critérios e forma de limitação de empenho** (contingenciamento, art. 9º); normas de **controle
    de custos** e **avaliação de resultados**; condições para transferências a entidades públicas/privadas.
  - **art. 4º, §1º — Anexo de Metas Fiscais (AMF):** estabelece **metas anuais, em valores correntes
    e constantes**, para **receitas, despesas, resultado nominal, resultado primário** e **montante da
    dívida pública**, para o **exercício a que se referir e os dois seguintes** (visão trienal).
  - **art. 4º, §2º** — demonstrativos do AMF: avaliação do cumprimento das metas do ano anterior;
    metas comparadas com as fixadas nos três exercícios anteriores; evolução do patrimônio líquido;
    avaliação da situação financeira/atuarial dos regimes de previdência (RPPS); renúncia de receita
    e margem de expansão das despesas obrigatórias de caráter continuado.
  - **art. 4º, §3º — Anexo de Riscos Fiscais (ARF):** avalia os **passivos contingentes e outros
    riscos** capazes de afetar as contas públicas, **informando as providências** caso se concretizem.
- **[a confirmar] Municípios pequenos:** parte da doutrina/STN aponta **facultatividade** dos anexos
  AMF/ARF para municípios com **menos de 50.000 habitantes** (base na redação original do art. 5º da
  LRF, hoje muito mitigada; o entendimento dominante atual é de **obrigatoriedade geral**, com prazo
  histórico já vencido). **Verificar a posição vigente do TCE-RS** antes de tornar os anexos opcionais
  no produto — Maximiliano de Almeida/RS tem porte pequeno, então este ponto é decisivo.

---

## 4. LOA — Lei Orçamentária Anual (execução de 1 exercício)

- **Núcleo (Lei 4.320/64, art. 2º; CF 165 §8º):** **estima a RECEITA** (prevista) e **FIXA a DESPESA**
  (autorizada/teto) para **um exercício**, sob o **princípio do equilíbrio**. Não pode conter
  "dispositivo estranho" (§8º) salvo autorização de créditos suplementares e operações de crédito.
- **Receita PREVISTA — classificação (Lei 4.320, arts. 8 a 11):** por **categorias econômicas**
  (**Receitas Correntes** e **Receitas de Capital**), depois por **origem, espécie, rubrica, alínea**
  (natureza de receita). É **estimativa**, não autorização de arrecadação.
- **Despesa FIXADA — classificação (Lei 4.320, arts. 12 a 15):**
  - **Por categoria econômica** (art. 12): **Despesas Correntes** e **Despesas de Capital** — já
    modelado no enum `CategoriaEconomica` (3 e 4) do código atual.
  - **Institucional:** **órgão** e **unidade orçamentária (UO)** — já há campos em `ClassificacaoOrcamentaria`.
  - **Funcional-programática** (art. 13 + funcional vigente STN): **função / subfunção / programa / ação
    (projeto/atividade/operação especial)** — hoje gravada como **string única** `FuncionalProgramatica`
    (ex.: "04.122.0002.2010"), **sem** vínculo estruturado ao PPA/programa.
  - **Por elemento de despesa / natureza** (art. 15): natureza da despesa detalhada (pessoal, material
    de consumo, serviços, investimentos…).
  - **Fonte/destinação de recurso** — já há `FonteDeRecurso`.
- **QDD — Quadro de Detalhamento da Despesa:** instrumento (editado por **decreto** do Executivo, exigido
  pela LDO/LOA) que **detalha a despesa fixada na LOA** até o **elemento de despesa** por UO / função /
  subfunção / programa / projeto-atividade / fonte. É o nível em que a **DotacaoOrcamentaria** efetivamente
  "nasce" para execução. **Hoje o QDD não é modelado**; as dotações são criadas avulsas.
- **Créditos adicionais (Lei 4.320, arts. 40 a 46):** **suplementares** (reforço de dotação existente —
  já refletido em `DotacaoOrcamentaria.Reforcar`), **especiais** (despesa sem dotação específica) e
  **extraordinários** (urgência/imprevisível). São o mecanismo legal de alteração da LOA em execução.

---

## 5. A CADEIA PPA → LDO → LOA → execução e a compatibilidade obrigatória

```
   PPA (4 anos)                LDO (anual)                 LOA (anual)              Execução (Lei 4.320)
   ───────────────            ─────────────────           ─────────────────        ──────────────────────
   Programas                  metas+prioridades do        Receita PREVISTA  ─┐     QDD/Decreto
     └ Ações                  exercício seguinte          Despesa FIXADA     ├──►  DotacaoOrcamentaria  ✅ (já existe)
        └ Metas físicas  ───► (subconjunto do PPA)  ───►  por UO/funcional-  │       └ Empenho ► Liquidação ► Pagamento ✅
        └ Metas financeiras   + AMF (metas fiscais)       programática/      │       └ Restos a Pagar ✅
        └ Regionalização      + ARF (riscos fiscais)      elemento/fonte ────┘       └ Contabilidade PCASP/MSC ✅
```

**Regras de compatibilidade que o software deve impor (provar, não só documentar):**
1. **LOA ⊆ LDO ⊆ PPA:** toda dotação da LOA deve referenciar um **programa/ação do PPA vigente**
   (CF 167, I e §1º) e estar entre as **prioridades da LDO** do exercício.
2. **Equilíbrio (LRF art. 4º):** total da despesa fixada compatível com a receita prevista + metas do AMF.
3. **Compatibilização com o PPA (CF 165 §7º):** orçamentos compatíveis com o plano plurianual.
4. **Emendas** ao orçamento só são admitidas se **compatíveis com PPA e LDO**.
5. **Execução (já garantido no código):** empenho/despesa **nunca excede o crédito** (`ReservarEmpenho`
   lança `SaldoOrcamentarioInsuficienteException`); falta o **degrau anterior** — a dotação nascer
   **derivada e validada** contra LOA/QDD/PPA, hoje inexistente.

---

## 6. Prazos (ADCT art. 35, §2º — default federal; municípios adaptam pela Lei Orgânica)

Enquanto não editada a LC do art. 165 §9º, valem os prazos do **ADCT art. 35, §2º** (referência
federal que os municípios **espelham/ajustam na Lei Orgânica** — CF art. 29):

| Instrumento | Envio ao Legislativo (Executivo) | Devolução p/ sanção (Legislativo) |
|---|---|---|
| **PPA** | até **4 meses antes do fim do 1º exercício** do mandato (≈ **31/ago** do 1º ano) | até o **fim da sessão legislativa** (≈ **22/dez**) |
| **LDO** | até **8,5 meses antes do fim do exercício** (≈ **15/abr**) | até **encerrar o 1º período** da sessão (≈ **17/jul**) |
| **LOA** | até **4 meses antes do fim do exercício** (≈ **31/ago**) | até o **fim da sessão legislativa** (≈ **22/dez**) |

- **[a confirmar]** As **datas exatas municipais** dependem da **Lei Orgânica de Maximiliano de
  Almeida/RS** e da jurisprudência do **TCE-RS**. Há entendimento (TCE-SC, p.ex.) de que a Lei Orgânica
  **pode alterar** os prazos de PPA e LOA, mas **não** o da LDO. **Os prazos devem ser parametrizáveis
  por tenant** (CLAUDE.md §7: "prazos são parametrizáveis por tenant, nunca hardcoded").
- A **vigência defasada do PPA** (2º ano → 1º ano do mandato seguinte) decorre justamente do envio do
  PPA no 1º ano do mandato.

---

## 7. Achados consolidados (resumo executivo)

1. **GAP real e confirmado:** planejamento (PPA/LDO/LOA) **ausente do código**; só há execução.
   Contradiz a própria spec (`CLAUDE.md §4`). Reprovador de PoC municipal.
2. **Reaproveitamento existente:** `ClassificacaoOrcamentaria` já cobre órgão/UO/funcional/categoria/fonte;
   `DotacaoOrcamentaria` já garante a invariante "despesa ≤ crédito" e créditos adicionais. A LOA/QDD
   deve **alimentar** essas dotações (origem rastreável), não substituí-las.
3. **Modelagem-alvo mínima (3 agregados + vínculos):** **Ppa** (Programa→Ação→Meta física/financeira,
   regionalização, quadriênio) → **Ldo** (exercício, prioridades = subconjunto de ações do PPA, AMF, ARF)
   → **Loa** (exercício, receita prevista por natureza, despesa fixada por UO/programa/ação/natureza/fonte,
   QDD) → **DotacaoOrcamentaria** (já existe) deriva da LOA/QDD.
4. **Invariantes a provar:** LOA ⊆ LDO ⊆ PPA; equilíbrio receita×despesa; dotação só nasce de item da
   LOA; reforços/créditos adicionais rastreados; compatibilidade verificada antes de abrir exercício.
5. **Parametrização:** prazos e facultatividade de anexos LRF **por tenant** (TCE-RS), nunca hardcoded.

---

## 8. Pendências / [a confirmar] (§16)

- **[a confirmar]** Obrigatoriedade vigente dos anexos **AMF/ARF** para município **< 50 mil hab.**
  (porte de Maximiliano de Almeida/RS) — confirmar **posição atual do TCE-RS** e da STN.
- **[a confirmar]** **Datas exatas** de PPA/LDO/LOA na **Lei Orgânica de Maximiliano de Almeida/RS**
  (o ADCT é só o default federal).
- **[a confirmar]** Layout/estrutura de **importação/remessa** do PPA/LDO/LOA exigido pelo **TCE-RS
  (SIAPC/PAD)** — verificar se há schema/leiaute próprio do tribunal para o planejamento.
- **[a confirmar]** Existência prática e relevância do **orçamento de investimento de estatais** (CF 165
  §5º II) no município-piloto (em geral inexpressivo, mas precisa de decisão de escopo).
- **[a confirmar]** Tabela vigente de **funções/subfunções** (Portaria STN/MOG) e **natureza da receita/
  despesa** a adotar para validar a funcional-programática (hoje gravada como string livre).
- **[a confirmar]** Texto **literal** integral de CF art. 165 e LC 101/2000 art. 4º/5º direto do
  **Planalto** (as páginas do `planalto.gov.br` retornaram socket-close nas tentativas; o conteúdo
  aqui veio de STF/Câmara dos Deputados + sumários oficiais — reconferir literalidade antes de citar
  em código/spec).

---

## 9. FONTES

- **CF/88, art. 165, 167 e ADCT art. 35** — Planalto / STF Constituição:
  - https://www.planalto.gov.br/ccivil_03/constituicao/constituicao.htm
  - https://portal.stf.jus.br/constituicao-supremo/artigo.asp?abrirBase=CF&abrirArtigo=165
  - https://portal.stf.jus.br/constituicao-supremo/artigo.asp?abrirBase=AD&abrirArtigo=35
  - https://normas.leg.br/?urn=urn:lex:br:federal:constituicao:1988-10-05;1988!art165
- **Lei nº 4.320/1964** (normas gerais de orçamento) — Planalto:
  - https://www.planalto.gov.br/ccivil_03/leis/l4320.htm
- **LC nº 101/2000 (LRF)** — Planalto / Tesouro Transparente / TCE:
  - https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm
  - https://www.tesourotransparente.gov.br/temas/execucao-orcamentaria-e-financeira/lei-de-responsabilidade-fiscal-lrf
  - https://www.tce.sp.gov.br/sites/default/files/publicacoes/LRF.pdf
- **Câmara dos Deputados — Instrumentos de Planejamento e Orçamento (PPA/LDO/LOA, prazos, compatibilidade):**
  - https://www2.camara.leg.br/orcamento-da-uniao/cidadao/entenda/cursopo/planejamento
- **ENAP / TCMGO — material institucional PPA/LDO/LOA:**
  - https://repositorio.enap.gov.br/bitstream/1/6450/3/M%C3%B3dulo%203%20-%20PPA,%20LDO%20e%20LOA.pdf
  - https://www.tcmgo.tc.br/site/wp-content/uploads/2019/12/Instrumentos-de-Planejamento-Governamental.pdf
- **TCE-SC — prazos de envio (PPA/LOA via Lei Orgânica; LDO não):**
  - https://consulta.tce.sc.gov.br/relatoriosdecisao/relatoriotecnico/3696801.HTML
- **QDD — Quadro de Detalhamento da Despesa (exemplos municipais oficiais):**
  - https://portal.londrina.pr.gov.br/quadro-de-detalhamento-da-despesa-qdd
  - https://www.saofranciscodeassis.rs.gov.br/legislacao/lei-orcamentaria-anual-loa/lei-orcamentaria-anual-loa-2025-decreto-qdd-quadro-de-detalhamento-da-despesa-2025
