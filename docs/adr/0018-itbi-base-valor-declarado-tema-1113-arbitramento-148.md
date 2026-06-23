# ADR-0018 — ITBI: base de cálculo = valor declarado (Tema 1.113/STJ) + arbitramento (CTN art. 148) como processo com contraditório

- **Status:** Aceito (motor implementado; **pende parecer da procuradoria + CTM do município** antes de produção)
- **Data:** 2026-06-22
- **Código:** `Modules/Tributos/.../Domain/Calculo/CalculoItbi.cs`,
  `Domain/Itbi/{TransmissaoImobiliaria,Arbitramento/*}.cs`,
  `Application/Itbi/{LancarItbi,CalcularItbi,ApuradorItbi,ArbitramentoItbi}.cs`,
  `Modules/Tributos/rules/Itbi.rules.md`,
  `Infrastructure/.../Migrations/20260622230000_ItbiTema1113Arbitramento.cs`

## Contexto

O ITBI incide sobre transmissão de imóveis. Muitos municípios fixam a base pelo **valor venal de
referência** (PGV) e cobram de ofício quando o valor declarado é menor. O **STJ, no Tema 1.113
(REsp 1.937.821)**, firmou que: **(a)** a base é o **valor da transação declarado**, que goza de
**presunção de veracidade**; **(b)** o município **não pode** arbitrar previamente a base por um
valor de referência unilateral; **(c)** só pode afastar a presunção por **processo administrativo
regular** de arbitramento (**CTN art. 148**), individualizado e com **contraditório**. Um motor que
elevasse a base de ofício pelo valor venal seria **juridicamente nulo** — e isso muda o cálculo, não
só a tela.

## Decisão

Modelar o ITBI segundo o Tema 1.113, no domínio:

- **Base = valor declarado (regra padrão).** `CalculadoraItbi.Calcular` faz `baseCalculo =
  valorDeclarado`; o **valor venal de referência NÃO entra na aritmética** — serve apenas a
  **triagem/alerta** (divergência > margem **parametrizável por tenant** apenas **sinaliza** revisão
  fiscal via evento, nunca eleva a base). A `TransmissaoImobiliaria` **nasce sempre** com origem
  `Declarada` (invariante no `Registrar`).
- **Arbitramento (CTN art. 148) = processo, não cálculo.** `ProcessoArbitramentoItbi` é um **agregado
  com máquina de estados**: `Instaurado → AguardandoContraditorio → EmAnalise → Concluido | Cancelado`.
  A instauração **exige motivo individualizado** (não basta "menor que a pauta"); o **contraditório é
  obrigatório** (não se pula `AguardandoContraditorio`); o ônus da prova é do fisco.
- **Única porta de elevação da base.** Só um processo **`Concluido`** produz `ResultadoArbitramento`,
  e só ele habilita `CalculadoraItbi.RecalcularComArbitramento` (origem `ArbitradaArt148`), que
  **recusa** recálculo se `ProcessoConcluido == false`. A diferença a maior gera lançamento de ofício
  complementar. Todas as transições emitem domain events → trilha imutável (ADR-0016).

## Alternativas consideradas

- **Base = MAX(declarado, valor venal de referência):** prática comum, mas **contrária ao Tema
  1.113** — passível de anulação e devolução. Rejeitada.
- **Arbitramento como recálculo automático quando declarado < referência:** viola o contraditório
  do art. 148; seria arbitramento de ofício disfarçado. Rejeitado em favor do **processo com
  estados**.
- **Ignorar o valor de referência:** perde-se a triagem que aponta subdeclaração para revisão
  legítima. Mantido como **alerta**, fora da base.

## Consequências

- ➕ O motor é **juridicamente defensável** sob o Tema 1.113; a base sobe **apenas** por processo
  regular com contraditório, deixando trilha auditável (instauração → defesa → decisão).
- ➕ A triagem por referência continua útil (aponta subdeclaração) **sem** contaminar a base — separa
  fiscalização de lançamento.
- ➕ **Reprodutibilidade:** alíquota e parâmetros lidos por **exercício do fato gerador** (ADR-0021),
  não do relógio.
- ➖ **Mais superfície:** novo agregado, estados, migration (`ProcessosArbitramentoItbi`) e endpoints
  do rito; invariantes do contraditório viram testes obrigatórios.
- ➖ **Pendências jurídicas `[a confirmar]`** antes de congelar para produção: parecer da
  **procuradoria**, o **CTM de Maximiliano de Almeida/RS** (alíquotas, margem de triagem, rito de
  notificação/contraditório diferido) e o fluxo de lançamento complementar/restituição. O
  `Itbi.rules.md` exige **ADR + parecer** antes de produção — este ADR registra a decisão de modelo;
  os números fiscais permanecem parametrizáveis (CLAUDE.md §7/§16).
- 🔗 Decisão **jurídica que muda o motor** — referência obrigatória para qualquer evolução do cálculo
  de ITBI.
