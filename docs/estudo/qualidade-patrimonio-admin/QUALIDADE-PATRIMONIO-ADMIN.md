# Qualidade — Patrimonio + Administracao (consolidado)

> Auditoria adversarial dos módulos **Patrimonio** e **Administracao** (dinheiro público, escrutínio
> TCE). Consolida `auditoria-patrimonio.md` + `auditoria-administracao.md`, com cada achado
> **reverificado contra o código** (arquivo:linha conferidos nesta consolidação). Separa **bugs reais
> (corrigir)** de **edge-cases sem teste (cobrir)**, por severidade e módulo.

## Lição IPTU — onde reincidiu

A lição do IPTU tinha **dois** vícios: (a) `DateTime.UtcNow` dentro do cálculo (não-reprodutível) e
(b) faixa/regra controlada por valor exato passado solto.

- **(a) Relógio no cálculo — NÃO reincidiu.** O motor de depreciação não lê relógio: `Depreciar(DateOnly
  competencia)` usa o parâmetro (`BemPatrimonial.cs:201`), e nenhuma conta de depreciação/reavaliação/
  impairment chama `GetUtcNow()`/`DateTime.Now`. Em Administracao, handlers usam `TimeProvider`. **Achado
  positivo confirmado.**
- **(b) "Faixa por valor exato" — REINCIDIU, em forma pior, em Administracao.** O teto legal do aditivo
  (25%/50%, art. 125) é validado contra um `percentual` que o chamador informa **solto e desacoplado** do
  `valorDelta` que de fato altera o contrato (`Contrato.cs:278-334`). Mesma classe de defeito do IPTU,
  agora controlando teto de gasto público. **É o pior bug do escopo** (BUG-A1).
- **Reprodutibilidade na depreciação — risco análogo por outra porta:** `Depreciar` **não é idempotente**
  por competência (BUG-P2). Reprocessar/retry duplica a despesa do mês. Não é relógio, mas fere a mesma
  exigência de reprodutibilidade contábil.

---

## Placar

- **Bugs reais a corrigir: 11** (Patrimonio: 4 — P1, P2, P3, P4; Administracao: 7 — A1, A2, A3, A4, A5, A6, A7).
- **Edge-cases sem teste a cobrir: 19** (Patrimonio: 9; Administracao: 10).
- **Testes novos sugeridos: ~30** (1+ por bug + 1+ por lacuna).

---

# BUGS REAIS (corrigir)

## CRÍTICOS

### BUG-A1 — Aditivo: `percentual` desacoplado do `valorDelta` fura o teto de 25%/50% (art. 125) — Administracao
- Arquivo: `Domain/Contratos/Contrato.cs:278-334` (`CelebrarAditivo`); `Application/Contratos/CelebrarAditivo.cs:61-68`.
- `CelebrarAditivo` recebe `percentual` **e** `valorDelta` como argumentos **independentes**. O teto é
  checado só contra `percentual` (`:296-305`), mas `ValorAtual` muda por `valorDelta` (`:311-325`). Nada
  garante `percentual == valorDelta / ValorContratado * 100`. O Validator (`CelebrarAditivo.cs:37-39`) só
  limita `Percentual` a `[0,50]`.
- Caso (TCE): contrato R$100.000, `CelebrarAditivo(Acrescimo, percentual: 10, valorDelta: 80.000)` → passa
  (10%<25%), valor sobe para R$180.000 (**+80% real**), e o evento `AditivoCelebrado` reporta só 10% —
  sobrepreço mascarado na trilha.
- Correção: derivar `percentual` de `valorDelta / ValorContratado` dentro do agregado (fonte única), ou
  validar coerência e rejeitar divergência. Chamador nunca informa os dois soltos.
- Teste: `CelebrarAditivo(Acrescimo, 10, 80.000)` sobre 100.000 deve **lançar** (ou recalcular p/ 80% e
  estourar teto). Hoje passa silenciosamente — **nenhum** teste exercita divergência percentual×valorDelta.

### BUG-A2 — Supressão e acréscimo somam no MESMO acumulador / mesmo teto (art. 125 §1º) — Administracao
- Arquivo: `Contrato.cs:118-120` (`PercentualQuantitativoAcumulado`), `:293-305`; `Aditivo.cs:74` (`EhQuantitativo => Acrescimo or Supressao`).
- `PercentualQuantitativoAcumulado` soma acréscimos **e** supressões no mesmo balde e ambos entram positivos
  (`+ percentual`, `:299`). Art. 125 §1º trata acréscimos e supressões com limites **separados** (25%, 50%
  reforma), não somados.
- Caso: Supressão 20% + Acréscimo 10% → acusa "30% acumulado" e barra o acréscimo **legítimo**.
  Inversamente, supressões grandes consomem o espaço do acréscimo.
- Correção: dois acumuladores distintos (Σ acréscimos vs Σ supressões), cada um contra seu próprio teto.
- Teste: Supressão 20% + Acréscimo 25% deve ser **aceito** (hoje lança); `Supressao_acumulada_acima_de_25_rejeitada`.
  Hoje **não há nenhum teste de supressão** — `TipoAditivo.Supressao` sequer é exercitado.

### BUG-P1 — Depreciação NÃO recalcula a parcela após reavaliação/impairment (MCASP/NBC TSP 07) — Patrimonio
- Arquivo: `Domain/Bens/BemPatrimonial.cs:115-116` (`ParcelaMensal`) e `:201-232` (`Depreciar`).
- `ParcelaMensal => Depreciacao.De(ValorInicial.Valor − ValorTerreno, ValorResidual.Valor, VidaUtilMeses).ParcelaMensal`
  — sempre derivada do **valor inicial original** e da **vida útil original**, ignorando reavaliações,
  impairments e a vida útil já consumida.
- Caso: bem 12.000, residual 2.000, vida 100m → 100/mês. Após 50m, ValorContabil=7.000. Reavalia p/ 15.000.
  MCASP: novo depreciável (15.000−2.000=13.000) / vida remanescente (50m) = **260/mês**. O código continua
  **100/mês** → subdepreciação crônica, valor contábil estoura a vida útil. Mesma falha pós-impairment.
- Correção: a parcela deve usar `ValorContabil` corrente, residual e **meses remanescentes** (vida útil −
  competências já depreciadas), recalculados após cada reavaliação/impairment.
- Teste: depreciar N meses → `Reavaliar` → depreciar; exigir nova parcela = (novoValor−residual)/mesesRem;
  idem `RegistrarImpairment`. **Confirmado sem cobertura**: `BemPatrimonialFluxoTests.cs:133` reavalia e
  para; nenhum teste deprecia *depois* de reavaliar/impairment.

### BUG-P2 — `Depreciar` re-executável para a MESMA competência (sem idempotência) — Patrimonio
- Arquivo: `BemPatrimonial.cs:201-232` (+ handler `DepreciarBem.cs`).
- Não há `_historicosDepreciacao.Any(h => h.Competencia == competencia)`. Chamar `Depreciar(2026,02)` 2x
  lança a parcela 2x, gera 2 `HistoricoDepreciacao` e 2 eventos `BemDepreciado` na mesma competência.
- Caso: job mensal que roda 2x / retry do Outbox / reenvio manual duplica a despesa do mês → quebra de
  reprodutibilidade contábil (família do IPTU, pela porta da idempotência).
- Correção: se já há histórico na competência, retornar 0 e não relançar (idempotente por competência).
- Teste: `Depreciar(comp)` 2x na mesma competência → 2ª retorna 0, histórico mantém 1 registro.
  **Confirmado sem cobertura**: os testes (`:102`/`:106`) usam competências **diferentes** (02 e 03).

## ALTOS

### BUG-A3 — `JulgarPropostas` não valida menor preço nem o lote — Administracao
- Arquivo: `Domain/Licitacoes/Licitacao.cs:216-240`.
- Aceita qualquer proposta não-desclassificada como vencedora, sem comparar com o critério
  (`MenorPreco`/`MaiorDesconto`) nem checar lote; força `Classificacao=1` (`:234`) sem ordenar por valor.
- Caso (red flag TCE): com MenorPreco e propostas 90k/80k/100k, é possível indicar a de 100k.
- Correção: validar que a vencedora é a melhor classificada pelo critério entre habilitadas/não-desclassificadas
  do mesmo lote (ou exigir justificativa registrada de indicação manual — decisão de produto).
- Teste: indicar vencedora que não é a de menor preço → rejeitar. Hoje sem cobertura de disputa por valor.

### BUG-A4 — Homologação depende de ordem não-determinística da habilitação — Administracao
- Arquivo: `Licitacao.cs:281-289` (`OrderBy(_ => 0).LastOrDefault()`); `Habilitacao.cs` (sem timestamp);
  `HabilitarLicitante` (`:257`) só faz `Add` (sem dedup/substituição).
- `OrderBy(_ => 0)` é sort por chave constante (estável só em memória). A coleção reidratada do EF Core não
  garante a ordem de inserção sem `OrderBy` explícito na config. Múltiplas habilitações do mesmo fornecedor
  (ex.: Inabilitado→Habilitado por recurso) → qual prevalece depende da reidratação → pode homologar
  vencedor de fato **inabilitado**. `Habilitacao` nem tem `DataVerificacao`.
- Correção: `Habilitacao` com data/sequência; regra explícita "prevalece a mais recente por data" + ordenação
  na config EF; ou proibir habilitação duplicada por fornecedor.
- Teste: `Homologar_usa_habilitacao_mais_recente` (Habilitado→Inabilitado deve **bloquear**) + round-trip de
  persistência confirmando a mesma decisão antes/depois do `SaveChanges`.

### BUG-A5 — `ValorMonetario.Subtrair` faz clamp em zero e mascara supressão inconsistente — Administracao
- Arquivo: `Domain/ValueObjects/ValorMonetario.cs:39-44`; uso em `Contrato.cs:318-319`.
- `Subtrair` retorna 0 quando o resultado seria negativo (`:43`). Supressão com `valorDelta > ValorAtual`
  zera o contrato **sem erro**, deixando-o "vigente" com valor R$0,00 — estado impossível p/ dinheiro
  público. Combinado com A1/A2, o teto não pega porque o percentual é livre.
- Caso: contrato 100k, ValorAtual 70k, nova supressão valorDelta 90k → ValorAtual=0 sem exceção.
- Correção: supressão que zere/negative deve **lançar**; clamp silencioso só serve para exibição, não para
  mutação de estado financeiro.
- Teste: `Supressao_que_excede_valor_atual_e_rejeitada`.

## MÉDIOS

### BUG-P3 — Bem `Cedido` não deprecia, mas continua no acervo — Patrimonio
- Arquivo: `BemPatrimonial.cs:204-207` (`Depreciar` exige `Situacao == Tombado`); `PatrimonioRepositories.cs`
  (`ListarDepreciaveisAsync` filtra só `Tombado`); `Ceder()` em `:294-302`.
- Bem cedido/comodato permanece no acervo do ente (docstring: "mantendo-o no acervo", sem baixa contábil),
  logo continua sujeito a depreciação por MCASP — mas o código o exclui da listagem e do `Depreciar`
  (lança). O helper `GarantirAtivoNoAcervo` (`:355-361`) já aceita `Tombado` **ou** `Cedido`; o `Depreciar`
  usa o critério restrito por engano.
- Caso: bem cedido por 3 anos não acumula depreciação → ativo superavaliado no balanço.
- Correção: `Depreciar` e o filtro do repositório aceitarem `Tombado` **ou** `Cedido`.
- Teste: tombar → condições de uso → `Ceder` → `Depreciar(comp)` deve depreciar.

### BUG-P4 — Veículo (frota) não tem depreciação alguma — Patrimonio
- Arquivo: `Domain/Frota/Veiculo.cs` (agregado inteiro). **Confirmado**: tem `ValorInicial`, `ValorResidual`,
  `ValorContabil`, `VidaUtilMeses` (`:89`), `EmCondicoesDeUso` — mas **nenhum** método `Depreciar`; o
  `VeiculoRepository` não tem `ListarDepreciaveis`.
- Caso: frota é "é-um bem patrimonial" e deprecia por MCASP, mas o valor contábil nunca cai. **Lacuna
  funcional**, não só de teste.
- Correção: implementar depreciação do veículo (reaproveitando o motor linear), com piso no residual e
  idempotência por competência (evitar repetir BUG-P2).
- Teste: depreciar veículo e validar piso no residual + idempotência.

### BUG-A6 — Aditivo de prazo aceita `novaVigenciaFim` anterior ao fim atual (ignorado silenciosamente) — Administracao
- Arquivo: `Contrato.cs:327-330` (`if (novaFim > VigenciaFim) VigenciaFim = novaFim;`).
- Aditivo de Prazo com `novaVigenciaFim` **menor** que a vigência atual é aceito, registra aditivo, emite
  evento, mas **não altera** a vigência — aditivo "fantasma". Não valida `novaVigenciaFim >= VigenciaInicio`/
  `>= dataCelebracao`, nem exige a data em aditivo de Prazo.
- Caso: vigência até 2026-12-31, aditivo Prazo `novaVigenciaFim=2026-06-01` → aceito, vigência intacta.
- Correção: aditivo de Prazo deve exigir `novaVigenciaFim` informado e `> VigenciaFim`; senão, lançar.
- Teste: `Aditivo_prazo_com_data_anterior_e_rejeitado`; `Aditivo_prazo_exige_nova_data`.

## BAIXOS

### BUG-A7 — `Encerrar()` não checa vigência nem distingue encerramento normal de antecipado — Administracao
- Arquivo: `Contrato.cs:409-418`.
- `Encerrar` exige só `Eficaz or EmExecucao`. Encerra-se contrato que nunca executou (Eficaz) como se tivesse
  concluído objeto; emite `ContratoEncerrado`. Sem regra de data (encerrar antes de `VigenciaInicio` é
  permitido). A máquina não distingue "encerrado por término de vigência" de "rescisão antecipada".
- Correção/decisão: definir política de encerramento de `Eficaz` (permitido? exige vigência decorrida?) e
  registrar a distinção normal vs. antecipado.
- Teste: cobrir a política escolhida.

---

# EDGE-CASES SEM TESTE (cobrir — código provavelmente correto)

## Patrimonio
- **L-P1 — Depreciação após reavaliação/impairment:** revela BUG-P1 (é lacuna **e** bug).
- **L-P2 — Fechamento de centavos na vida útil inteira:** `Depreciacao.ParcelaMensal` arredonda `AwayFromZero`
  a 2 casas (`Depreciacao.cs:22`); a última parcela é limitada por `Math.Min(parcela, depreciavel)`
  (`BemPatrimonial.cs:222`). Falta teste de que Σ parcelas = valor depreciável exato (sem perder/criar centavo).
- **L-P3 — PEPS com saldo de lotes inconsistente:** `ValorarPeps` (`ItemEstoque.cs:273-292`) subvalora
  silenciosamente se `restante > 0` ao fim do loop. Protegido pelo invariante `Saldo == Σ lotes`, mas sem
  teste que prove a invariante sob entrada/saída repetida + VRL.
- **L-P4 — Custo médio estável:** entrada→saída→entrada e estabilidade do `CustoMedio` sem teste.
- **L-P5 — VRL não reverte (NBC TSP 12):** `AjustarValorRealizavelLiquido` (`ItemEstoque.cs:234-245`) só
  **reduz** o CustoMedio; se o VRL voltar a subir, não reverte. Verificar regra (potencial bug menor de mensuração).
- **L-P6 — Multa/licenciamento pós-baixa:** `RegistrarMulta`/`RegistrarLicenciamento` exigem
  `GarantirAtivoNoAcervo` (`Veiculo.cs`); fato gerador anterior à baixa/alienação seria bloqueado. Avaliar
  se é bug de fronteira.
- **L-P7 — Bordas de odômetro/cota:** `odometro == atual` (igual permitido, `Odometro.cs:30`) e
  `litros == cotaLitros` (permitido, `Veiculo.cs:229`) sem teste.
- **L-P8 — Depreciar de bem não em condições de uso:** `Depreciar` retorna 0 se `!EmCondicoesDeUso`
  (`BemPatrimonial.cs:210-213`) — cobrir o retorno 0.
- **L-P9 — Idempotência da depreciação:** ver BUG-P2 (lacuna **e** bug).

## Administracao
- **L-A1 — Supressão de aditivo:** nenhum teste (revela A2/A5).
- **L-A2 — `PercentualQuantitativoAcumulado` no Integration Event:** fronteira 25,00% vs 25,01% (`> limite`
  em `Contrato.cs:300`) sem teste de epsilon (`24.999%`, arredondamento).
- **L-A3 — `DeclararFracassada` / `ExistePropostaValidaHabilitada`** (`Licitacao.cs:381-403`, esp. `:396`
  `if (!inabilitado || habilitado)`): proposta **sem habilitação ainda** conta como "válida" e bloqueia o
  fracasso quando ninguém foi habilitado (todos pendentes). Definir/cobrir a semântica de "proposta sem
  habilitação" no fracasso. (Possível bug latente médio; precisa decisão.)
- **L-A4 — Isolamento de tenant em mutações:** testes de tenant cobrem só consulta. Falta teste de handler
  (ex.: `CelebrarAditivoHandler`) tentando carregar contrato de outro tenant.
- **L-A5 — `BloquearEficaciaPorDotacaoIndisponivel` quando `EmExecucao`:** `Contrato.cs:224-232` só reverte de
  `Eficaz`; se já `EmExecucao` e a dotação some, permanece `EmExecucao` (gastando sem cobertura). Sem teste —
  provável bug latente (art. 105-106/LRF).
- **L-A6 — Idempotência real do handler de bloqueio:** comentário afirma idempotência por EventId/Inbox, sem
  teste de reprocessamento do mesmo `EventId`.
- **L-A7 — Eficácia do aditivo (art. 174):** `MarcarAditivoPublicado` / `PublicadoNoPncp` do aditivo sem teste.
- **L-A8 — `Recurso` (impugnação) e `Lote`:** `Recurso.cs` (`Interpor`/`Julgar`) e `Lote` sem nenhum teste.
- **L-A9 — `Proposta.Desclassificar`** e reflexo no julgamento/fracasso sem teste.
- **L-A10 — Sanção com `DataInicio` futura / fornecedor `Inativo`:** `AplicarSancao` que não muda situação hoje
  mas deveria ao chegar a data (sem reavaliação temporal); e sanção sobre fornecedor `Inativo` ainda emite
  evento — sem teste.

---

# OS MAIS CRÍTICOS (priorizar)

1. **BUG-A1 — percentual do aditivo desacoplado do valorDelta** (`Contrato.cs:278-334`). Permite furar o teto
   legal de 25%/50% (art. 125) com sobrepreço mascarado na trilha e no Integration Event. **Reincidência da
   classe IPTU ("faixa por valor exato"), agora sobre gasto público. O pior do escopo.**
2. **BUG-P1 — parcela de depreciação congelada na origem** (`BemPatrimonial.cs:115-116`). Não recalcula após
   reavaliação/impairment; viola MCASP/NBC TSP 07 e distorce o valor contábil de todo o acervo. Silencioso e
   sistemático — o tipo de erro que o TCE pega.
3. **BUG-A2 — supressão e acréscimo no mesmo teto somado** (`Contrato.cs:118-120, 293-305`; `Aditivo.cs:74`).
   Viola art. 125 §1º (limites separados); barra acréscimos legítimos e propaga percentual errado. Zero
   cobertura de supressão.
4. **BUG-P2 — depreciação não-idempotente por competência** (`BemPatrimonial.cs:201-232`). Reprocessamento/
   retry duplica a despesa do mês — mesma família do IPTU pela porta da reprodutibilidade.

(Menção honrosa: **BUG-A4** homologação não-determinística e **BUG-A3** ausência de enforcement de menor preço
— red flags de integridade licitatória sob escrutínio TCE.)

---

**Caminho:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/estudo/qualidade-patrimonio-admin/QUALIDADE-PATRIMONIO-ADMIN.md`
