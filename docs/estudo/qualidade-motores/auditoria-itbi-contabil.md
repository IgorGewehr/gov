# Auditoria adversarial — Motor ITBI (Tema 1.113) + Motor Contábil PCASP

> Auditoria de correção fiscal sob ótica TCE-RS. Postura: assumir bug sutil até prova em
> contrário. Régua oficial: `docs/architecture/m6-prep/{pesquisa,verificacao}-*` e `M6-DESIGN.md`.
> Data: 2026-06-22. Escopo: `src/Modules/Tributos/**/Itbi` e `src/Modules/Financas/**/Contabilidade` + `Empenhos`.

---

## Resumo executivo

Separação pedida — **bugs reais** vs. **só falta teste**:

| # | Achado | Tipo |
|---|--------|------|
| B1 | Arbitramento perde isenção/SFH ao recalcular (overcharge do contribuinte) | **BUG real** |
| B2 | Anulação de empenho e estorno de liquidação NÃO geram contra-lançamento contábil (balancete/MSC superavaliados) | **BUG real** |
| B3 | Alíquota SFH aplicada sobre a base INTEIRA, não sobre a parcela financiada | **BUG real** (ou incompletude grave de modelagem) |
| B4 | `LancarItbi` aceita `ValorDeclarado = 0` → ITBI R$ 0,00 sem fato gerador válido | BUG menor / falta validação |
| B5 | Imunidades CF art. 156 §2º I não modeladas (integralização de capital / fusão-cisão) | Gap de escopo (régua exige) |
| B6 | `RegistrarPagamento` não chama `GarantirAtivo()` (paga empenho já em Restos a Pagar) | BUG potencial de estado |
| T1 | Sem teste de propagação de saldo no balancete para estorno/anulação | Falta teste |
| T2 | Sem teste de arredondamento ΣD=ΣC quando roteiro tem natureza com >1 par | Falta teste |
| T3 | Sem teste de `AnularTotal` sobre empenho já parcialmente liquidado | Falta teste |
| T4 | Sem teste de Restos a Pagar (inscrição + pagamento de RP no exercício seguinte) | Falta teste |
| T5 | Sem teste de triagem no limite exato da margem (`declarado == limiteInferior`) | Falta teste |

**Os 3 mais críticos: B1, B2, B3** (detalhados ao final).

---

## MOTOR ITBI

### B1 — Arbitramento recalcula sem os parâmetros originais (isenção/SFH) → overcharge **[BUG REAL]**

- **Arquivo:** `src/Modules/Tributos/.../Application/Itbi/ArbitramentoItbi.cs:239-244`
  (`ConcluirArbitramentoItbiHandler.Handle`) e `Domain/Itbi/TransmissaoImobiliaria.cs:37-61`
  (a transmissão não persiste `ParametrosItbi`).
- **Caso-limite:** transmissão lançada com `PercentualIsencao = 100` (1ª aquisição SFH, ex. do
  próprio teste `Isencao_reduz_o_imposto_devido`) ou `UsarAliquotaSfh = true`. Depois instaura-se
  arbitramento e conclui-se.
- **Está errado.** `RecalcularComArbitramento(...)` é chamado com `parametros` ausente →
  `CalculadoraItbi` usa `ParametrosItbi.Padrao` (isenção 0, alíquota GERAL). Como
  `TransmissaoImobiliaria` só guarda `AliquotaPercentual` e `ImpostoDevido` finais — e **não** guarda
  `PercentualIsencao` nem `UsarAliquotaSfh` —, o recálculo arbitrado:
  1. ignora a isenção (cobra imposto cheio sobre base arbitrada);
  2. troca a alíquota SFH pela geral.
  Resultado: `diferenca = impostoArbitrado(cheio) − impostoAnterior(isento)` gera lançamento de
  ofício complementar **indevido**. Um arbitramento de base, que deveria apenas ajustar a base,
  acaba também removendo benefício fiscal legítimo. Para o TCE isso é cobrança a maior — risco de
  glosa e de litígio com o contribuinte.
- **Agravante:** o `Validator` aceita `ValorArbitrado >= 0`. Arbitrar para um valor **menor** que o
  declarado é aceito pelo domínio, mas o handler só lança complementar se `diferenca > 0` — não há
  caminho de restituição/anulação da guia original quando o arbitramento reduz a base. (Cenário
  raro, mas o art. 148 não impede arbitramento para baixo.)
- **Teste a adicionar:** `Tests/MotorItbiTests` (ou um `ArbitramentoItbiHandlerTests`):
  - `Arbitramento_preserva_isencao_do_lancamento_original` — lança com isenção 100%, conclui
    arbitramento elevando a base, e verifica que `ImpostoDevidoArbitrado` continua respeitando os
    100% de isenção (esperado falhar hoje).
  - `Arbitramento_preserva_aliquota_sfh`.
  - **Correção sugerida:** persistir `ParametrosItbi` (ou ao menos `PercentualIsencao` e
    `UsarAliquotaSfh`) em `TransmissaoImobiliaria` e repassá-los em `RecalcularComArbitramento`.

### B3 — Alíquota SFH aplicada à base inteira, não à parcela financiada **[BUG REAL / modelagem]**

- **Arquivo:** `src/Modules/Tributos/.../Domain/Calculo/CalculoItbi.cs:174-177` (`Montar`):
  `var aliquota = parametros.UsarAliquotaSfh ? aliquotaSfhPercentual : aliquotaGeralPercentual;`
  aplicado a `baseCalculo` inteira.
- **Régua oficial:** `pesquisa-itbi-taxas.md:42` e `M6-DESIGN.md:71` — *"alíquota reduzida para
  imóveis financiados pelo SFH **sobre a parcela financiada**"*. O modelo real é misto:
  `imposto = parcelaFinanciada × aliquotaSfh + (base − parcelaFinanciada) × aliquotaGeral`.
- **Está errado** (ou, na melhor hipótese, é uma simplificação não documentada que o TCE rejeitaria):
  hoje `UsarAliquotaSfh = true` aplica a alíquota reduzida a **100%** do valor do imóvel,
  subtributando a parcela NÃO financiada (recursos próprios do adquirente). Em compra de R$ 250.000
  com R$ 150.000 financiados, o correto seria 150.000×0,5% + 100.000×2% = R$ 2.750; o código calcula
  250.000×0,5% = R$ 1.250 → **arrecadação a menor de R$ 1.500**.
- **Atenuante:** `AliquotaItbi.cs:59` tem `TODO(validar-oficial)` sobre a existência da redução SFH
  no CTM de Maximiliano de Almeida. Se o município não tiver redução SFH, o flag nunca é usado e o
  bug fica latente. Mas o motor é vendido como genérico/parametrizável (CLAUDE.md §0) — o modelo
  está incompleto para qualquer município que adote a regra.
- **Teste a adicionar:** `MotorItbiTests.Sfh_aplica_reduzida_so_na_parcela_financiada` — exige
  introduzir `ValorFinanciadoSfh` em `ParametrosItbi`/`CalcularItbi`. Hoje **não há sequer um campo**
  para a parcela financiada, o que confirma que a regra não foi modelada.

### B4 — `ValorDeclarado = 0` aceito → guia de ITBI zerada **[BUG menor]**

- **Arquivo:** `src/Modules/Tributos/.../Application/Itbi/LancarItbi.cs:69`
  (`RuleFor(c => c.ValorDeclarado).GreaterThanOrEqualTo(0m)`) e `:97`.
- **Caso-limite:** transmissão onerosa declarada com valor 0,00.
- **Está errado para o domínio:** ITBI incide sobre transmissão **onerosa** (CTN art. 35;
  `TransmissaoImobiliaria.cs:25` documenta "inter vivos onerosa"). Valor declarado zero não é
  transmissão onerosa — ou é doação (ITCMD, competência estadual) ou é fraude. O sistema gera
  `ImpostoDevido = 0`, `Dam` de R$ 0,00, e a triagem fica degenerada: `limiteInferior` é positivo,
  então `0 < limiteInferior` dispara alerta — mas a guia zerada já foi emitida. Deveria rejeitar
  (`GreaterThan(0)`).
- **Teste a adicionar:** `LancarItbiHandlerTests.Valor_declarado_zero_e_rejeitado` (hoje passa
  indevidamente).

### B5 — Imunidades CF art. 156 §2º I não modeladas **[GAP de escopo]**

- **Arquivo:** ausência — `Domain/Calculo/CalculoItbi.cs` só conhece `PercentualIsencao` genérico;
  não há conceito de imunidade por integralização de capital / fusão-incorporação-cisão nem o
  teste da "atividade imobiliária preponderante".
- **Régua:** `pesquisa-itbi-taxas.md` (§1.3) e `M6-DESIGN.md:71` marcam como *"regra de exceção a
  modelar"*. Imunidade ≠ isenção: imunidade afasta a própria incidência (base/alíquota irrelevantes)
  e tem regra condicional (preponderância nos 2 anos anteriores / 3 seguintes — CTN art. 37).
  Representá-la como `PercentualIsencao = 100` perde a fundamentação e a reavaliação de
  preponderância, que o TCE/Fisco precisam auditar.
- **Teste a adicionar:** cenário BDD de imunidade por integralização com posterior verificação de
  preponderância (não modelável hoje — registrar como dívida de feature, não só de teste).

### ITBI — o que está CORRETO (provado)

- Base = valor declarado mesmo abaixo do venal (`CalculoItbi.cs:97`); venal só dispara triagem
  (`:100-101`). Conforme Tema 1.113. ✔ (coberto por `MotorItbiTests`).
- Elevação de base só via `RecalcularComArbitramento` com `ProcessoConcluido = true`
  (`CalculoItbi.cs:140-143`) e máquina de estados Instaurado→Contraditório→EmAnalise→Concluído
  bloqueando pular o contraditório (`ProcessoArbitramentoItbi.cs:157,177,201`). ✔
- DAM: resíduo de arredondamento absorvido na última parcela, soma fecha com o total
  (`Dam.cs:90-99`). ✔ (mas falta teste explícito de parcelamento com resíduo — ver T-ITBI).
- `TransmissaoImobiliaria.Registrar` recusa nascer já arbitrada (`:132-135`) e
  `AplicarArbitramento` é idempotente/uma-vez (`:179-188`). ✔

> ⚠️ Observação de triagem (`CalculoItbi.cs:101`): `haDivergencia = valorDeclarado < limiteInferior`
> usa `<` estrito. No limite exato (`declarado == limiteInferior`) NÃO dispara alerta — comportamento
> aceitável, mas não testado (**T5**).

---

## MOTOR CONTÁBIL PCASP

### B2 — Anulação de empenho e estorno de liquidação não geram contra-lançamento **[BUG REAL]**

- **Arquivos:**
  - `src/Modules/Financas/.../Application/Empenhos/AnularEmpenho.cs:24-47` — muta empenho + dotação,
    `Empenho.AnularParcial` levanta `EmpenhoAnulado` (`Empenho.cs:192`), mas **nenhum**
    `INotificationHandler<EmpenhoAnulado>` existe (`Contabilidade/Handlers/` só tem Empenho, Liquidação,
    Pagamento, Receita, Balancete).
  - `src/Modules/Financas/.../Application/Liquidacoes/EstornarLiquidacao.cs:33-34` — chama
    `liquidacao.Estornar()` + `empenho.EstornarLiquidacao()` e **não** levanta evento de estorno; não
    há handler de `LiquidacaoEstornada`.
- **Caso-limite:** empenho contabilizado (D Crédito Disponível / C Empenhado a Liquidar via
  `EVT-EMP`), depois anulado. A anulação reverte o saldo orçamentário de controle no agregado
  `Empenho`, mas o **balancete/MSC** continuam com "Empenhado a Liquidar" cheio.
- **Está errado.** Os roteiros inversos **existem e estão semeados** — `RoteirosCatalogo.cs:45-49`
  (`EVT-EMP-ANUL`) e `:63-69` (`EVT-LIQ-EST`) — mas **nada os dispara**. Os `FatoContabil.EmpenhoAnulado`
  (`RoteiroEnums.cs:16`) e `FatoContabil.LiquidacaoEstornada` (`:25`) são código morto. Consequência
  direta: o Balancete (`BalanceteProjection`/`ProjetarBalanceteHandler`) e a MSC derivada superavaliam
  a despesa empenhada/liquidada → demonstrações DCASP e remessa SICONFI/TCE **divergem dos saldos de
  controle**. É exatamente o tipo de inconsistência que a pré-validação SIAPC do TCE-RS aponta.
- **Teste a adicionar:**
  - `ContabilizacaoAnulacaoTests.Anulacao_de_empenho_gera_lancamento_inverso` — após anular, o
    balancete de "6.2.2.1.3.01.00 Empenhado a Liquidar" deve voltar ao saldo pré-empenho.
  - `ContabilizacaoEstornoLiquidacaoTests.Estorno_reverte_orcamentario_e_patrimonial`.
  - **Correção:** criar `ContabilizarAnulacaoEmpenhoHandler` (`INotificationHandler<EmpenhoAnulado>`)
    e levantar+consumir um `LiquidacaoEstornada`. (O motor já tem idempotência por `origemReferenciaId`
    + roteiro — mas atenção: anulações parciais sucessivas usam o MESMO `EmpenhoId`; ver nota abaixo.)

> 🔸 **Sub-risco de idempotência (B2'):** `MotorContabil.ContabilizarAsync` deduplica por
> `origemReferenciaId + evento.Id` (`MotorContabil.cs:50`). Se o handler de anulação usar
> `EmpenhoId` como origem, a **2ª anulação parcial do mesmo empenho seria silenciosamente ignorada**
> (já existe lançamento de `EVT-EMP-ANUL` para aquele empenho). A chave de idempotência da
> anulação/estorno precisa incluir o id do ato de anulação, não o do empenho. Registrar teste:
> `Duas_anulacoes_parciais_geram_dois_lancamentos`.

### B6 — `RegistrarPagamento` não verifica `GarantirAtivo()` **[BUG potencial de estado]**

- **Arquivo:** `src/Modules/Financas/.../Domain/Empenhos/Empenho.cs:243-253`. Compare com
  `AnularParcial` (`:183`), `RegistrarLiquidacao` (`:214`) e `EstornarLiquidacao` que **chamam**
  `GarantirAtivo()` — `RegistrarPagamento` **não**.
- **Caso-limite:** empenho com saldo liquidado a pagar é inscrito em Restos a Pagar
  (`InscreverEmRestosAPagar`, situação `InscritoRestosAPagar`). Em seguida chega um pagamento.
- **Está parcialmente protegido, mas inconsistente:** `RegistrarPagamento` só barra por
  `SaldoAPagar` (`:246`), então **aceita** pagar um empenho `InscritoRestosAPagar` (há saldo a pagar).
  Aí `AtualizarSituacaoPorSaldos` retorna cedo para `InscritoRestosAPagar` (`:274`) e **não atualiza**
  a situação para pago — o `ValorPago` sobe mas a situação trava em "InscritoRestosAPagar". O
  pagamento de RP deveria ser um fluxo próprio (baixa de RP), não um `RegistrarPagamento` no empenho
  do exercício. Hoje o comportamento é ambíguo: muta valor sem refletir situação.
- **Está certo NÃO permitir** pagar empenho `Anulado`/`TotalmentePago` — mas isso só funciona por
  acaso (saldo zero), não por guarda explícita. Um empenho `AnuladoParcial` com saldo a pagar
  remanescente pagaria normalmente (correto), porém a ausência de `GarantirAtivo()` torna o conjunto
  frágil a regressões.
- **Teste a adicionar:** `Empenho_inscrito_em_RP_nao_aceita_pagamento_direto` e
  `Pagamento_de_RP_segue_fluxo_proprio` (definir a regra com o domínio antes).

### Contábil — o que está CORRETO (provado)

- Partida dobrada ΣD=ΣC validada no factory (`LancamentoContabil.cs:270-281`), homogeneidade de
  natureza (`:259-268`), só conta analítica (`:250-256`), período aberto (`:159-162`). ✔ (cobertos
  por `ContabilidadeInvariantesTests`).
- Estorno gera lançamento inverso sem apagar, bloqueia estorno duplo (`LancamentoContabil.cs:202-236`).
  ✔ — **porém** isto é o estorno do *lançamento contábil*, não acionado pela anulação de empenho (ver B2).
- Roteiro nasce balanceado: como toda base é `ValorDoFato` (mesmo V), balance reduz-se a #D==#C por
  natureza (`EventoContabil.cs:122-132`). ✔ **enquanto** todas as linhas usarem `BaseValorRoteiro.ValorDoFato`.
- Balancete: vira de exercício zera contas de resultado (5/6/3/4) e carrega permanentes (1/2)
  (`BalanceteProjection.cs:110-121`); usa o change-tracker local para evitar violação de UNIQUE
  conta+exercício+mês (`:30-35`). ✔ Saldo recalculado por natureza (`LinhaBalancete.cs:57-60`). ✔
- `SaldoInvariantesTests` cobre teto de empenho/liquidação/pagamento e liquidação após anulação
  parcial (`:118-129`). ✔

### Riscos latentes (não-bug hoje, viram bug com roteiro novo) — falta teste

- **T2 — arredondamento por natureza:** `EventoContabil.Criar` assume que cada partida é exatamente
  `ValorDoFato` (`EventoContabil.cs:124`, comentário explícito). Existe `BaseValorRoteiro` (enum) que
  permitiria bases derivadas (percentuais). Se um roteiro futuro usar uma base proporcional (ex.:
  retenção de INSS/IRRF na liquidação), o balance #D==#C **não garante** ΣD(R$)=ΣC(R$) após
  arredondamento a 2 casas, e `LancamentoContabil.ValidarBalanceamento` (`:270`) lançaria
  `PartidaDobradaDesbalanceadaException` em runtime. Hoje nenhum roteiro usa base derivada, mas não
  há **teste** que prove que `ResolverPara` só aceita `ValorDoFato` ou trate o resíduo. Adicionar
  `Roteiro_com_base_derivada_arredonda_sem_desbalancear` antes de criar tais roteiros.
- **T4 — Restos a Pagar:** `InscreverEmRestosAPagar` (`Empenho.cs:258-266`) define situação mas o
  saldo inscrito = `SaldoEmpenhado − ValorPago` inclui valor **a liquidar** (RP não processado) e
  **liquidado a pagar** (RP processado) sem distinguir. A régua MCASP separa RP processado de não
  processado (há `EVT-RP-INSC` só para "não processado", `RoteirosCatalogo.cs:94`). Falta teste de
  inscrição + pagamento de RP no exercício n+1, e a distinção processado/não-processado.
- **T3 — `AnularTotal` sobre empenho parcialmente liquidado:** `AnularTotal` (`Empenho.cs:197-206`)
  rejeita se `ValorLiquidado > 0`. Caso-limite não testado: empenho liquidado em parte e depois
  anulado-total deve falhar com mensagem clara (hoje lança `InvalidOperationException` genérica).
- **T1 — propagação ao balancete de estorno/anulação:** depende de B2 ser corrigido primeiro.

---

## Notas de método

- Não executei `dotnet`/porta 5080 (instrução). Análise estática de código + testes + régua docs.
- `ValorMonetario` (Tributos e Finanças) arredonda `AwayFromZero` a 2 casas e barra negativos
  (`De`). `Subtrair` (Finanças) garante não-negatividade — o que torna **impossível** representar um
  saldo/lançamento negativo intencional (ex.: retificação que deixaria saldo negativo transitório);
  hoje é tratado por estorno (lançamento inverso positivo), o que é correto para PCASP. Sem bug, mas
  registrar como decisão: "valores negativos não existem; correção é sempre por partida inversa".
