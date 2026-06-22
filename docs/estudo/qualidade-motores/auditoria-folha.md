# Auditoria adversarial — Motor de Folha (RH)

> **Escopo:** `MotorDeCalculoFolha` + `TabelaInss/Irrf/Rpps` + `FaixaProgressiva/FaixaIrrf` + rubricas + o caso de uso `ApurarDescontosLegais` (ponte entre folha e motor).
> **Régua oficial:** `docs/architecture/m5-prep/pesquisa-folha-calculo.md` e `verificacao-folha-calculo.md`.
> **Postura:** adversarial — assumir bug sutil até provar o contrário. Valores conferidos numericamente (Decimal/HALF_UP) batem com os testes existentes (INSS 1500/3000/5000/8000/10000 = 112,50 / 248,60 / 501,51 / 921,51 / 988,09; IRRF 5000=312,89; IRRF 8000/2dep=933,58). A **mecânica** do motor (INSS/RPPS progressivo cumulativo, regra do mais vantajoso, arredondamento único por soma de faixas, fail-closed) está **correta**. Os achados abaixo são sobre **dados que nunca chegam ao motor** e **casos-limite sem teste**.

---

## A. BUGS REAIS (cálculo erra dinheiro público em produção)

### A1 — [CRÍTICO] Pensão alimentícia NUNCA é deduzida do IRRF (hardcoded `0m`)
- **Arquivo:** `ApurarDescontosLegais.cs:87`
  ```csharp
  var insumos = new InsumosCalculoServidor(servidorId, dados.Regime, dados.QuantidadeDependentes, 0m, verbas);
  //                                                                                              ^^^ pensão sempre zero
  ```
- **Cadeia da falha:** `MotorDeCalculoFolha` e `TabelaIrrf.CalcularImposto` aceitam `pensaoAlimenticia` corretamente (deduzem na base completa — linha 140 de `TabelaIrrf.cs`). Mas o **único** caminho de produção (o handler) sempre passa `0m`. A causa-raiz é que `DadosCalculoServidor` (`IServidorRegimeConsulta.cs:11`) carrega só `Regime` e `QuantidadeDependentes` — **não existe campo de pensão**, e o `Servidor`/`ServidorRegimeConsulta` não a fornece.
- **Caso-limite:** servidor com pensão alimentícia judicial. Ex.: rend. tributável 8.000, pensão 2.000 → IRRF correto = **R$ 487,85**; o sistema retém **R$ 1.037,85**. **R$ 550/mês a mais**, retidos indevidamente de um servidor, recolhidos à Receita — erro fiscal direto, líquido errado, e o TCE-RS verifica retenção de IRRF na remessa de pessoal. A pensão é dedução legal obrigatória (pesquisa §3, fórmula `baseIRRF = ... − pensãoAlimentícia`).
- **Está ERRADO.** O motor está certo; o handler/abstração estão incompletos.
- **Correção:** adicionar `PensaoAlimenticia` (e idealmente também a pensão como dedução, separada da pensão lançada como rubrica de desconto) a `DadosCalculoServidor` + `ServidorRegimeConsulta`; passar ao `InsumosCalculoServidor`.
- **Teste a adicionar:** integração em `ApurarDescontosLegaisTests` — servidor com pensão > 0, conferir IRRF reduzido; unit no motor já é possível mas o de integração é o que prova o fim-a-fim.

### A2 — [ALTO] `QuantidadeDependentes` = `Dependentes.Count` sem filtro de elegibilidade IRRF
- **Arquivos:** `RecursosHumanosProviders.cs:56` (`new DadosCalculoServidor(servidor.Regime, servidor.Dependentes.Count)`) + `Dependente.cs` (não há flag `dedutivelIrrf` nem regra de idade/condição).
- **Caso-limite:** `Dependente` registra qualquer parentesco (cônjuge, filho maior sem condição, etc.) e **todos** entram como dedução de R$ 189,59. A legislação do IRRF tem critérios de elegibilidade (idade/condição/relação). Hoje qualquer dependente cadastrado para fins de **benefício** (a doc do `Dependente.cs:18` diz "para fins de Imposto de Renda **e benefícios**") reduz IRRF indevidamente.
- **Está ERRADO / incompleto** (depende da política do ente, mas hoje é 100% sem filtro — risco de dedução a maior → IRRF a menor → glosa do TCE/Receita).
- **Teste a adicionar:** servidor com dependente não-elegível ao IRRF → dedução não aplicada. Exige primeiro modelar a elegibilidade no domínio.

### A3 — [MÉDIO] Rubrica de desconto manual NÃO valida incidência; descontos legais lançados à mão furam o motor
- **Arquivo:** `ApurarDescontosLegais.cs:60-65, 76-85`. As `verbas` só resolvem incidência para **proventos**; qualquer evento `Desconto` (que não seja INSS/RPPS/IRRF) cai em `outrosDescontos` no motor (`MotorDeCalculoFolha.cs:64`) sem checar nada. Combinado com `ApurarDescontosLegais.cs:80-82`: rubrica **não encontrada no catálogo vigente** assume `(false,false,false)` silenciosamente.
- **Caso-limite:** provento cuja `RubricaFolha` não está vigente na competência (erro de cadastro/vigência) entra como proventos mas **com base zero de INSS/IRRF** — subtributação silenciosa, sem erro, sem alerta. Para o TCE isso é base de cálculo errada sem rastro.
- **Está ERRADO como política fail-closed** (CLAUDE.md S16 manda recusar, não assumir zero). No mínimo deveria logar/alertar ou falhar.
- **Teste a adicionar:** provento com rubrica fora de vigência → hoje passa com base 0; decidir se deve falhar (fail-closed) e testar.

---

## B. CASOS-LIMITE SÓ FALTANDO TESTE (cálculo parece correto, mas o TCE exigiria a prova)

> Confirmei por código que a **mecânica** trata estes casos; o risco é **regressão sem teste**. O TCE-RS cobra reprodutibilidade do contracheque centavo a centavo.

### B1 — Fronteiras EXATAS das faixas (valor no limite) — INSS e IRRF
- **INSS (`FaixaProgressiva.cs:53-62`):** limite inferior **exclusivo**, superior **inclusivo**. Base = exatamente o topo de uma faixa (ex.: 1621,00; 2902,84; 4354,27; 8475,55 teto) não tem teste. A lógica `topo = base < LimiteSuperior ? base : LimiteSuperior` está correta, mas é exatamente onde 1 centavo vira faixa errada.
- **IRRF (`FaixaIrrf.cs:47` `Enquadra: base <= LimiteSuperior`):** base = exatamente 2428,80 (isento) e = 2428,81 (entra em 7,5%) não testadas. Confirmei: base 2428,80 → isento; 2428,81 → faixa 2. Correto, mas sem teste de fronteira.
- **Teste a adicionar:** `[Theory]` com base nos 4 limites do INSS e nos 4 limites do IRRF (valor exato e valor+0,01).

### B2 — Teto do INSS no valor EXATO
- `TabelaInss.CalcularContribuicao` (`TabelaInss.cs:101`): base = exatamente 8475,55 e base logo acima. Os testes cobrem 8000 e 10000, mas **não** o teto exato (a contribuição máxima ≈ 951,xx é o número que o TCE confere). RPPS com `Teto != null` no valor exato também sem teste (e o ramo `Teto == null`, sem limite, só tem o caso 5000).
- **Teste a adicionar:** INSS base = teto exato → contribuição máxima; base = teto+0,01 → mesma contribuição. RPPS com teto definido no limite.

### B3 — Múltiplas rubricas com incidências DIFERENTES (cenário real de contracheque)
- `Base_somada_por_incidencia_nao_por_nome` cobre 2 verbas (uma incide, outra não). Falta o caso realista: **vencimento (INSS+IRRF) + gratificação (só IRRF) + auxílio (nenhuma) + adicional (só INSS)** — bases INSS, RPPS e IRRF **todas diferentes entre si** no mesmo servidor. É o caso que mais quebra na prática (`MotorDeCalculoFolha.cs:40-66`).
- **Teste a adicionar:** 4+ rubricas, asserir `BaseInss ≠ BaseRpps ≠ BaseIrrf ≠ TotalProventos` simultaneamente.

### B4 — Dependentes IRRF: efeito marginal e base completa vencendo a simplificada
- Os testes têm 0, 1 e 2 dependentes, mas em casos onde o **simplificado** vence (a dedução por dependente fica **inócua**). Falta o caso onde a **base completa** (com muitos dependentes) é a mais vantajosa e cada dependente **muda o IRRF** — provando que `DeducaoPorDependente` realmente entra (`TabelaIrrf.cs:140`).
- **Teste a adicionar:** rend. alto com N dependentes onde base completa < simplificada; asserir queda monotônica do IRRF por dependente; e o caso N grande que **zera** o IRRF pela base completa.

### B5 — Arredondamento (centavos) com valores que caem em meio-centavo
- `decimal.Round(..., 2, MidpointRounding.AwayFromZero)` em `FaixaIrrf.cs:55`, `TabelaInss.cs:108`, `TabelaRpps.cs:135`, `MotorDeCalculoFolha.cs:112-121`. **Nenhum teste** força um resultado terminando em `...5` na 3ª casa para provar a política HALF-UP (vs. banker's rounding default do C#). O comentário em `FaixaProgressiva.cs:46-49` (arredondar UMA vez sobre a soma) está correto e é a decisão certa — mas não há teste que **falharia** se alguém arredondasse por faixa.
- **Teste a adicionar:** base escolhida para gerar contribuição bruta = X,XX5 → asserir arredondamento para cima; e um caso que diverge entre "arredonda por faixa" e "arredonda na soma" (blindar a decisão de `FaixaProgressiva`).

### B6 — Rubrica informativa NÃO incidindo (presente no domínio, ausente no motor de ponta a ponta)
- O domínio impede informativa com incidência (`RubricaFolha.cs:154-160`, testado). Mas **não há teste fim-a-fim** provando que uma rubrica `Informativa`/`InformativaDedutora` lançada na folha **não altera nenhuma base nem o líquido** ao passar pelo `ApurarDescontosLegais` → motor. `RubricaFolha.EhProvento` é `false` para informativa, então ela viraria `EhProvento:false` no `VerbaCalculo` e **somaria em `outrosDescontos`** (`MotorDeCalculoFolha.cs:64`) — possivelmente reduzindo o líquido de forma indevida para `InformativaDedutora`/`Informativa`. **Isto pode ser um bug latente** dependendo de como informativas são lançadas como evento de folha — recomendo investigar e cobrir.
- **Teste a adicionar:** lançar rubrica informativa na folha e asserir bases e líquido inalterados (ou o comportamento esperado explícito).

### B7 — Servidor SEM rubrica / sem proventos
- `MotorDeCalculoFolha.Calcular` com `Verbas` vazia: `totalProventos=0`, bases 0, descontos 0, líquido 0 — caminho não testado. E servidor na folha sem nenhum evento de provento (só desconto manual) → `Liquido` clampado a 0 (`MotorDeCalculoFolha.cs:105-108`), testado só com 1000 vs 2000. Falta o caso **verbas totalmente vazias** e **só informativa**.
- **Teste a adicionar:** `Verbas = []` → `ResultadoCalculoServidor` todo zero, sem exceção.

### B8 — Base após dedução INSS no IRRF (já coberto indiretamente, falta o explícito do "mais vantajoso virando")
- A regra do mais vantajoso (`TabelaIrrf.cs:148 Math.Min`) é o coração do IRRF. Os testes cobrem casos onde o simplificado vence. Falta o **ponto de virada**: rend. onde base completa == base simplificada (empate) e rend. logo abaixo/acima — provar que o `Math.Min` escolhe certo no empate e que o INSS realmente entra na base completa.
- **Teste a adicionar:** `[Theory]` em torno do ponto onde `INSS + deps + pensão == descontoSimplificado`.

---

## C. Observação de conformidade (não-bug, mas TCE-relevante)
- **IRRF 2026 não semeada** (`SemearTabelasFederais.cs:126`): competência 2026 herda a tabela mai-dez/2025. É decisão consciente e documentada (fail-aware), mas **a folha de 2026 está rodando com a tabela de 2025**. Se a IN da Lei 15.270/2025 já vigorar, o IRRF retido estará incorreto. Não é bug de motor (é dado faltando), mas é o tipo de coisa que o TCE aponta. Recomendo um teste/guarda que **alerte** quando a competência calculada não tem tabela própria daquele ano.

---

## RESUMO — bugs reais x casos-só-faltando-teste

**Bugs reais (3):** A1 pensão hardcoded `0m`, A2 dependentes sem filtro de elegibilidade IRRF, A3 rubrica fora de vigência/desconto sem validação (assume incidência zero em silêncio). B6 é bug **latente a confirmar** (informativa somando em `outrosDescontos`).

**Casos só faltando teste (7):** B1 fronteiras exatas das faixas (INSS+IRRF), B2 teto INSS exato, B3 múltiplas rubricas com bases todas diferentes, B4 dependentes onde a base completa vence, B5 arredondamento meio-centavo HALF-UP, B7 servidor sem rubrica/verbas vazias, B8 ponto de virada do mais vantajoso.

### Os 3 mais críticos
1. **A1 — Pensão alimentícia ignorada (`ApurarDescontosLegais.cs:87`).** Dinheiro errado garantido para todo servidor com pensão; IRRF a maior (ex.: +R$ 550/mês). Não há campo de pensão em `DadosCalculoServidor`. **Corrigir antes de qualquer folha real.**
2. **A2 — Dependentes contados sem elegibilidade IRRF (`RecursosHumanosProviders.cs:56`).** `Dependentes.Count` deduz R$ 189,59 por qualquer dependente cadastrado (inclusive os "de benefício"), reduzindo o IRRF indevidamente → glosa.
3. **B1 — Fronteiras exatas das faixas sem teste (INSS `FaixaProgressiva.cs:53` / IRRF `FaixaIrrf.cs:47`).** A lógica está correta hoje, mas é o ponto onde 1 centavo muda de faixa e onde uma regressão futura passa despercebida — exatamente o que o TCE-RS reconfere centavo a centavo.
