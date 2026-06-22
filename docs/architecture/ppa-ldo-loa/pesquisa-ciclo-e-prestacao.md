# Pesquisa — Ciclo Orçamentário (PPA/LDO/LOA) e Prestação de Contas

> **Status:** pesquisa de fonte oficial (Fase 1 / §16). Dirige a engenharia de domínio do
> **planejamento orçamentário** no módulo `Financas` — hoje AUSENTE (GAP recorrente apontado pela
> avaliação independente: 0 referências a PPA/LDO/LOA/crédito adicional no código; só existe a
> **execução** Dotação→Empenho→Liquidação→Pagamento + PCASP/MSC).
> Pontos com layout/código/prazo exato a confirmar com fonte primária ficam marcados **`[a confirmar]`** (§16).

## 0. O GAP (por que isto reprova PoC municipal)

| Temos (execução) | Falta (planejamento — este doc) |
|---|---|
| `DotacaoOrcamentaria` (crédito da LOA), `Empenho`→`Liquidacao`→`Pagamento`, Restos a Pagar | **PPA**, **LDO**, **LOA** como agregados; **votação/aprovação**; **créditos adicionais** que ALTERAM a LOA |
| Contabilidade PCASP (partida dobrada), Balancete, MSC, remessa TCE-RS/SICONFI | A **origem** da `DotacaoOrcamentaria`: hoje ela é "criada" solta (`CriarDotacaoCommand`), **sem nascer de uma LOA aprovada** |

A `DotacaoOrcamentaria.Criar(...)` em
`src/Modules/Financas/.../Application/Dotacoes/CriarDotacao.cs` documenta-se literalmente como
*"crédito da LOA"* mas **não referencia nenhuma LOA** — é o ponto de costura que falta. O valor
dotado é fixo, sem trilha de **dotação inicial → créditos adicionais → dotação atualizada**.

---

## 1. O ciclo orçamentário (CF art. 165 — instituído pela CF/88)

Três leis de iniciativa do **Executivo**, votadas pelo **Legislativo** (no município, a **Câmara
de Vereadores**), encadeadas e hierárquicas:

| Instrumento | Horizonte | Função | Base legal |
|---|---|---|---|
| **PPA** — Plano Plurianual | 4 anos (2º ano do mandato ao 1º do seguinte) | Diretrizes, **objetivos e metas** regionalizadas da despesa de capital e programas de duração continuada | CF art. 165, I e §1º |
| **LDO** — Lei de Diretrizes Orçamentárias | Anual (orienta o exercício seguinte) | Metas e prioridades, **orienta a elaboração da LOA**, dispõe sobre alterações na legislação tributária; **+ Anexo de Metas Fiscais e Anexo de Riscos Fiscais** (LRF art. 4º, §§1º-3º) | CF art. 165, II e §2º; LC 101/2000 art. 4º |
| **LOA** — Lei Orçamentária Anual | Anual (1 exercício) | **Estima a receita** e **fixa a despesa**; contém orçamento fiscal, da seguridade social e de investimento | CF art. 165, III e §5º |

**Hierarquia / vinculação (a regra que liga planejamento → execução):**
- CF art. 165, **§7º** — os orçamentos (LOA) devem ser **compatibilizados com o PPA**.
- CF art. 165, **§2º** — a **LOA é elaborada conforme dispuser a LDO**.
- **Consequência de modelagem:** a `DotacaoOrcamentaria` (crédito) deve **nascer de uma LOA aprovada**,
  e cada despesa fixada na LOA referencia uma **ação/programa do PPA** e respeita as **prioridades da LDO**.
  Cadeia: `PPA (programa/ação) → LDO (prioridade + metas fiscais) → LOA (dotação) → Empenho → ...`.

> `[a confirmar]` Prazos de envio/devolução de PPA/LDO/LOA **municipais** não estão na CF (que fixa
> só os federais via ADCT art. 35, §2º enquanto não há a lei complementar do art. 165, §9º). No
> município, os prazos vêm da **Lei Orgânica Municipal** e/ou Constituição Estadual do RS. Confirmar
> os prazos de Maximiliano de Almeida/RS (LOM) e do TCE-RS antes de hardcodar qualquer data.

---

## 2. Elaboração e votação na Câmara (processo legislativo orçamentário — CF art. 166)

1. **Iniciativa do Executivo** (Prefeito): projetos de lei de PPA, LDO e LOA são de iniciativa
   privativa do chefe do Executivo.
2. **Apreciação pelo Legislativo** (CF art. 166): a Câmara discute e vota; **emendas** são
   admitidas, mas a LOA só aceita emenda **compatível com o PPA e a LDO** e que indique os recursos
   (anulação de despesa) — CF art. 166, §3º. O Executivo pode enviar **mensagem retificadora**
   enquanto não concluída a votação na comissão (art. 166, §5º).
3. **Sanção / promulgação**: aprovado, vira **lei** (sancionada/promulgada e publicada). Só então a
   LOA existe juridicamente e suas **dotações** podem ser executadas.
4. **Compatibilidade obrigatória** (LRF art. 5º, I): o projeto de LOA traz, em anexo, **demonstrativo
   de compatibilidade** da programação orçamentária com os objetivos/metas do Anexo de Metas Fiscais
   da LDO; e **reserva de contingência** (LRF art. 5º, III) dimensionada sobre a RCL.

> **Modelagem:** a LOA é um agregado com **ciclo de vida / status** (`ProjetoEnviado` →
> `EmTramitacao` → `Aprovada/Sancionada` → `EmExecucao` → `Encerrada`). Só status "vigente/em execução"
> habilita criação de `DotacaoOrcamentaria` e empenho. A votação em si é do domínio **Legislativo**
> (módulo 10, tenant da Câmara) — costura via **Integration Event** (`LoaAprovadaIntegrationEvent`),
> nunca chamada direta (CLAUDE.md §2/§4). `[a confirmar]` o contrato exato do evento.

---

## 3. Créditos adicionais — o que ALTERA a LOA (Lei 4.320/1964, Título V, arts. 40-46)

**Definição (art. 40):** créditos adicionais são **autorizações de despesa não computadas ou
insuficientemente dotadas** na LOA. Três espécies (art. 41):

| Espécie | Finalidade | Autorização | Abertura | Fonte de recursos | Base legal |
|---|---|---|---|---|---|
| **Suplementar** | Reforço de dotação **já existente** | Lei (pode ser autorização prévia **na própria LOA**, até % do total) | **Decreto** do Executivo | Indicação obrigatória (art. 43) | art. 41, I; art. 7º |
| **Especial** | Despesa **sem dotação específica** (nova) | Lei **específica** (autorização não pode estar pré-dada na LOA) | Decreto do Executivo | Indicação obrigatória (art. 43) | art. 41, II |
| **Extraordinário** | Despesas **urgentes e imprevistas** (guerra, comoção interna, calamidade) | Independe de autorização prévia; aberto por **decreto/medida provisória**, dá-se ciência ao Legislativo | Imediata | **Dispensada** indicação prévia da fonte (art. 44) | art. 41, III; CF art. 167, §3º |

**Fontes de recursos (art. 43, §1º)** para suplementar/especial — a indicação obrigatória:
1. **Superávit financeiro** do balanço do exercício anterior;
2. **Excesso de arrecadação** (tendência do exercício);
3. **Anulação parcial ou total de dotações** ou de créditos autorizados em lei;
4. **Operações de crédito** autorizadas.

**Vigência (art. 45):** suplementar e especial **vigoram no exercício** em que abertos; o
extraordinário idem. Crédito **especial e extraordinário** autorizados nos **últimos 4 meses** do
exercício podem ser **reabertos** no exercício seguinte, nos limites de seus saldos (art. 167, §2º CF).

**Limites constitucionais (CF art. 167):**
- **V** — vedada abertura de crédito **suplementar ou especial sem prévia autorização legislativa e
  sem indicação dos recursos**.
- **VI** — vedados **transposição/remanejamento/transferência** entre categorias de programação ou
  órgãos sem prévia autorização legislativa.
- **VII** — vedada concessão/utilização de **créditos ilimitados**.
- **§3º** — crédito **extraordinário** só para despesa imprevisível e urgente (calamidade/guerra/comoção).

> **Modelagem (o núcleo do GAP):** agregado `CreditoAdicional` (espécie, ato autorizador, ato de
> abertura/decreto, valor, **fonte de recurso**) que **muta a `DotacaoOrcamentaria`**:
> `ValorDotado` (inicial) + Σ suplementações − Σ anulações = **`DotacaoAtualizada`**. Hoje a dotação
> tem **valor fixo** — falta exatamente essa trilha. Invariantes fortes: suplementar exige dotação
> preexistente; especial cria dotação nova; anulação não pode tornar saldo negativo; total de
> créditos abertos por decreto ≤ limite autorizado na LOA (art. 7º / art. 167, V). Isto é
> **auditável** (CLAUDE.md §1.4) — cada alteração da LOA é um fato com ato legal e trilha imutável.

---

## 4. O que vai ao TCE-RS / SICONFI e como deriva da LOA

Relatórios da **LRF (LC 101/2000)** — todos **derivados da execução contra a LOA** (a LOA é a
referência "previsto"; a contabilidade/PCASP é o "realizado"):

| Saída | Origem | Periodicidade | Base legal | Relação com a LOA |
|---|---|---|---|---|
| **RREO** — Relatório Resumido da Execução Orçamentária | Poder Executivo | **Bimestral** | LC 101 art. 52-53; CF art. 165, §3º | Balanço Orçamentário = **previsão (LOA) + créditos adicionais vs. execução**; demonstrativo da despesa por função etc. |
| **RGF** — Relatório de Gestão Fiscal | Cada Poder/órgão | **Quadrimestral** (ou semestral p/ pequenos municípios) | LC 101 art. 54-55 | Limites de despesa de pessoal, dívida, garantias — sobre a RCL e a execução |
| **DCA** — Declaração de Contas Anuais | Ente | Anual | STN/SICONFI | Consolida o exercício |
| **MSC** — Matriz de Saldos Contábeis | Contabilidade (PCASP) | Mensal | STN/SICONFI | Saldos contábeis (já existente no sistema) |

- **SICONFI** (STN) recebe DCA, RREO, RGF e MSC; estrutura e regras de preenchimento no **MDF —
  Manual de Demonstrativos Fiscais** (LRF art. 55, §4º) e nas *Regras Gerais e Instruções de
  preenchimento* publicadas anualmente pelo SICONFI. `[a confirmar]` versão/exercício vigente do MDF
  e do leiaute SICONFI antes de gerar.
- **TCE-RS**: remessa **SIAPC/PAD** (mensal/anual) — já mapeada em
  `docs/architecture/INTEGRACOES-PRESTACAO-DE-CONTAS.md` e `contabilidade-pcasp-tce.md`. O **Balanço
  Orçamentário** (Anexo 12 da Lei 4.320) e o RREO **precisam do "previsto"** = LOA + créditos
  adicionais, que hoje o sistema **não tem como gerar fielmente** porque não modela a LOA nem os créditos.

---

## 5. Relação com o que já existe (contabilidade / MSC / execução)

- **Ponto de costura primário:** `DotacaoOrcamentaria` passa a **nascer de uma `Loa` aprovada**
  (FK `LoaId` + `AcaoPpaId`), em vez do atual `CriarDotacaoCommand` solto. O `ValorDotado` vira
  **`DotacaoInicial`** e ganha **`DotacaoAtualizada`** (recalculada por `CreditoAdicional`).
- **Contabilidade:** o evento "Dotação aprovada" e "Crédito adicional aberto" já têm lugar na tabela
  de eventos contábeis (`contabilidade-pcasp-tce.md` §2: *Dotação aprovada → D crédito disponível /
  C dotação inicial* nas contas de **controle orçamentário 5.x/6.x**). Crédito adicional gera
  lançamento análogo (reforço/anulação de crédito disponível). Já há gancho — falta a **origem**.
- **MSC / Transparência:** inalterada como mecanismo; passa a ter dado "previsto" correto para os
  demonstrativos orçamentários.
- **Multi-tenant / isolamento (CLAUDE.md §4-5):** Executivo e Câmara são **tenants distintos**. A
  **votação** é fato do tenant Câmara (módulo Legislativo); a **LOA vigente** é fato do tenant
  Prefeitura (módulo Finanças). Costura **só via Contracts/Integration Events**.

---

## FONTES (oficiais / oficiais-derivadas)

- **CF/1988, art. 165, 166, 167** (PPA/LDO/LOA, processo legislativo orçamentário, vedações) — Planalto `planalto.gov.br/ccivil_03/Constituicao/Constituicao.htm`; STF Constituição anotada `portal.stf.jus.br/constituicao-supremo/artigo.asp?abrirBase=CF&abrirArtigo=165` e `...Artigo=167`.
- **Lei 4.320/1964, Título V, arts. 40-46** (créditos adicionais) — Planalto `planalto.gov.br/ccivil_03/leis/l4320.htm`.
- **LC 101/2000 (LRF), arts. 4º, 5º, 8º, 9º, 52-55** (LDO/Anexos, compatibilidade, RREO/RGF) — Planalto `planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm`.
- **SICONFI / MDF** — Tesouro: `siconfi.tesouro.gov.br` (Regras Gerais e Instruções de preenchimento RREO/RGF, exercício corrente); `tesourotransparente.gov.br/temas/.../lei-de-responsabilidade-fiscal-lrf`.
- **ENAP** (didático, corroborante) — `repositorio.enap.gov.br` Módulo 3 PPA/LDO/LOA.

## Pendências de fonte oficial (`[a confirmar]` — §16)

- [ ] **Prazos municipais** de envio/devolução de PPA/LDO/LOA — **Lei Orgânica de Maximiliano de Almeida/RS** + Constituição Estadual RS (CF não fixa prazo municipal).
- [ ] **Contrato do `LoaAprovadaIntegrationEvent`** entre módulo Legislativo (Câmara) e Finanças (Prefeitura).
- [ ] **Versão vigente do MDF** e do **leiaute/taxonomia SICONFI** (RREO/RGF) para o exercício.
- [ ] **Leiaute do Balanço Orçamentário (Anexo 12 da Lei 4.320)** e demonstrativos do RREO no SIAPC/PAD TCE-RS.
- [ ] **% limite de suplementação autorizado na LOA** (parametrizável por tenant — vem da LOA aprovada, não hardcoded; CLAUDE.md §7).
- [ ] Estrutura de **classificação do PPA** (programa → ação → produto/meta) e o vínculo exato ação-PPA ↔ dotação-LOA conforme padrão STN/RS.
