# Auditoria adversarial — Patrimonio + Administracao

Auditor adversarial. Foco: motor de depreciacao/reavaliacao/impairment (MCASP/NBC TSP 07),
almoxarifado, frota, e calculos de dinheiro publico em Administracao (Lei 14.133).

Lição do IPTU (DateTime.UtcNow no cálculo + faixa por valor exato): **NÃO se repete** no
motor de depreciação de Patrimonio. `Depreciar(DateOnly competencia)` usa o parâmetro de
competência, não `Now`; nenhuma das contas de depreciação/reavaliação/impairment lê relógio.
Os usos de `GetUtcNow()` no módulo são só para timestamp de evento (Outbox) e para a data
"hoje" de checagem de CNH/transferência — não entram em cálculo monetário. Esse é o principal
achado positivo. Os bugs abaixo são de outra natureza (lógica de negócio e fronteiras).

---

## BUGS REAIS (lógica/cálculo errados)

### P-1 — Depreciação NÃO recalcula a parcela após reavaliação/impairment (MCASP)
- Arquivo: `BemPatrimonial.cs:115-116` (`ParcelaMensal`) e `:201-232` (`Depreciar`).
- `ParcelaMensal => Depreciacao.De(ValorInicial.Valor - ValorTerreno, ValorResidual.Valor, VidaUtilMeses).ParcelaMensal`.
  A parcela é sempre derivada do **valor inicial original** e da **vida útil original**, ignorando
  reavaliações, impairments e a vida útil já consumida.
- Caso (certo/errado): bem 12.000, residual 2.000, vida 100m → parcela 100/mês. Após 50 meses
  ValorContabil = 7.000. Reavalia-se para 15.000 (laudo). NBC TSP 07/MCASP: a depreciação
  subsequente deve distribuir o **novo valor depreciável** (15.000 − 2.000 = 13.000) pela
  **vida útil remanescente** (50 meses) = 260/mês. O código continua depreciando **100/mês**
  sobre uma base maior → subdepreciação crônica, valor contábil estourando a vida útil estimada.
  Mesma falha após impairment (reduz ValorContabil mas mantém a parcela antiga → o bem chega
  ao residual antes do fim da vida e para; aceitável só por acaso).
- Errado: parcela congelada na origem.
- Teste a adicionar: depreciar N meses, `Reavaliar`, depreciar de novo e exigir que a nova
  parcela = (novoValor − residual)/mesesRemanescentes; idem para `RegistrarImpairment`.
  Hoje não há **nenhum** teste que deprecie *depois* de reavaliar/impairment.

### P-2 — `Depreciar` é re-executável para a MESMA competência (sem idempotência)
- Arquivo: `BemPatrimonial.cs:201-232` + handler `DepreciarBem.cs:25-45`.
- Não há verificação de `_historicosDepreciacao.Any(h => h.Competencia == competencia)`. Chamar
  `Depreciar(2026,02)` duas vezes lança a parcela duas vezes, gera dois `HistoricoDepreciacao`
  e dois eventos `BemDepreciado` na mesma competência.
- Caso: reprocessamento mensal (job que roda 2x, retry do Outbox, reenvio manual) duplica a
  despesa de depreciação do mês — quebra de **reprodutibilidade contábil** e risco para o TCE.
- Errado: depreciação por competência não é idempotente.
- Teste a adicionar: `Depreciar(comp)` duas vezes na mesma competência → segunda retorna 0m,
  `HistoricosDepreciacao` mantém um único registro daquela competência.

### P-3 — Bem `Cedido` não deprecia, mas continua no acervo (deveria depreciar)
- Arquivo: `BemPatrimonial.cs:204-207` (`Depreciar` exige `Situacao == Tombado`) e
  `PatrimonioRepositories.cs:24-29` (`ListarDepreciaveisAsync` filtra só `Tombado`).
- Um bem cedido/comodato (`Ceder()`, `BemPatrimonial.cs:294-302`) permanece no patrimônio do
  ente (sem baixa contábil — o próprio docstring diz "mantendo-o no acervo"), logo continua
  sujeito a depreciação por MCASP. O código o exclui tanto da listagem quanto do `Depreciar`
  (lança `InvalidOperationException`).
- Caso: bem cedido por 3 anos não acumula depreciação → ativo superavaliado no balanço.
- Errado: `Depreciar` e o filtro deveriam aceitar `Tombado` **ou** `Cedido` (já existe o helper
  `GarantirAtivoNoAcervo` que cobre os dois — `Depreciar` usa o critério restrito por engano).
- Teste a adicionar: tombar → condições de uso → `Ceder` → `Depreciar(comp)` deve depreciar.

### P-4 — Veículo (frota) não tem depreciação alguma
- Arquivo: `Veiculo.cs` (agregado inteiro). Tem `ValorInicial`, `ValorResidual`, `ValorContabil`,
  `VidaUtilMeses`, `EmCondicoesDeUso=true` — todo o aparato — porém **nenhum** método `Depreciar`,
  e `VeiculoRepository` não tem `ListarDepreciaveis`.
- Caso: a frota é "é-um bem patrimonial" (docstring `Veiculo.cs:21-26`) e deprecia por MCASP, mas
  o valor contábil do veículo nunca cai. Lacuna funcional, não só de teste.
- Errado: frota fora do motor de depreciação.
- Teste a adicionar: após implementado, depreciar veículo e validar piso no residual. (Hoje a
  ausência é silenciosa.)

### A-1 — Limite de aditivo soma acréscimo + supressão no MESMO acumulador (art. 125)
- Arquivo: `Contrato.cs:118-120` (`PercentualQuantitativoAcumulado`) e `:296-305` (checagem).
- `PercentualQuantitativoAcumulado = Σ percentual de aditivos onde EhQuantitativo` —
  `EhQuantitativo` é `Acrescimo OR Supressao` (`Aditivo.cs:74`). Acréscimos e supressões são
  **somados juntos**. A Lei 14.133 art. 125 fixa o limite de 25% (50% reforma) para acréscimos
  **e**, separadamente, o mesmo limite para supressões — não são o mesmo "saldo".
- Caso (errado): acréscimo 20% + supressão 10% → acumulado = 30% → um novo acréscimo de 0%...
  na verdade já bloqueia tudo acima de 25% de soma, então acréscimo 20% seguido de supressão 10%
  reporta 30% e impede aditivos legítimos; e o `AditivoCelebrado` propaga 30% como "percentual
  acumulado" para Finanças. Inversamente, uma supressão **infla** o consumo do limite de acréscimo.
- Certo: dois acumuladores distintos (Σ acréscimos vs Σ supressões), cada um contra seu limite.
- Teste a adicionar: acréscimo 25% + supressão 25% → ambos aceitos (limites independentes);
  hoje **não há nenhum teste de supressão** (`TipoAditivo.Supressao` sequer é exercitado).

### A-2 — Supressão pode "sumir" silenciosamente por causa do clamp em zero
- Arquivo: `Contrato.cs:318-320` + `ValorMonetario.Subtrair` (`ValorMonetario.cs:39-44`).
- `Subtrair` nunca fica negativo (`resultado < 0 ? 0 : resultado`). Uma supressão com `valorDelta`
  maior que `ValorAtual` zera o contrato em vez de rejeitar. Combinado com A-1 (percentual mal
  somado), o `valorDelta` e o `percentual` podem divergir sem trava.
- Caso: contrato 100k, supressão `valorDelta`=150k → `ValorAtual`=0 (deveria lançar).
- Teste a adicionar: supressão acima do valor atual deve ser rejeitada (não clampar a zero).

### A-3 — `JulgarPropostas` não valida menor preço nem o lote da proposta
- Arquivo: `Licitacao.cs:216-240`.
- Aceita qualquer proposta não desclassificada como vencedora, sem comparar com o critério
  `CriterioJulgamento.MenorPreco`/`MaiorDesconto` nem checar que a proposta pertence ao lote em
  disputa. A `Classificacao` é setada à força como 1 (`:234`) sem ordenar por valor.
- Caso (red flag TCE): com MenorPreco e propostas 90k / 80k / 100k, é possível indicar a de 100k
  como vencedora — sem qualquer invariante impedindo. Integridade de seleção competitiva ausente.
- Certo: ao menos validar que a vencedora é a melhor classificada pelo critério (menor valor /
  maior desconto) entre as habilitadas/não desclassificadas do mesmo lote.
- Teste a adicionar: indicar vencedora que não é a de menor preço → rejeitar (ou exigir
  justificativa registrada). Hoje `LicitacaoFluxoTests` não cobre disputa por valor.

---

## LACUNAS DE TESTE (código provavelmente correto, sem cobertura)

- **L-1 (Patrimonio):** depreciação após reavaliação/impairment — ver P-1 (revela o bug; é
  simultaneamente lacuna e bug).
- **L-2 (Patrimonio):** arredondamento da parcela na borda. `Depreciacao.ParcelaMensal` arredonda
  `AwayFromZero` a 2 casas (`Depreciacao.cs:22`). Ex.: 1000/3 = 333,33/mês → após 3 parcelas
  sobra resíduo de centavos que a última parcela (limitada por `Math.Min(parcela, depreciavel)`
  em `BemPatrimonial.cs:222`) absorve. Não há teste validando que o **somatório** das parcelas =
  valor depreciável exato (sem perder/criar centavo). Adicionar teste de fechamento de centavos
  ao longo de toda a vida útil.
- **L-3 (Patrimonio/Estoque):** PEPS com saldo de lotes inconsistente. `ValorarPeps`
  (`ItemEstoque.cs:273-292`) consome lotes; se `restante > 0` ao fim do loop, **subvalora**
  silenciosamente (custo menor que o devido) em vez de lançar. Hoje protegido pelo invariante
  `Saldo == Σ lotes`, mas não há teste que prove a invariante sob entrada/saída repetida + VRL.
- **L-4 (Patrimonio/Estoque):** custo médio com saída não recalcula nada (correto — médio
  persiste), mas não há teste cobrindo entrada→saída→entrada e a estabilidade do `CustoMedio`.
- **L-5 (Patrimonio/Estoque):** `AjustarValorRealizavelLiquido` (`ItemEstoque.cs:234-245`) só
  **reduz** o CustoMedio; se o VRL voltar a subir, não há reversão. NBC TSP 12 admite reversão
  de redução. Verificar regra e cobrir (potencial bug menor de mensuração).
- **L-6 (Patrimonio/Frota):** `RegistrarMulta`/`RegistrarLicenciamento` exigem
  `GarantirAtivoNoAcervo` (`Veiculo.cs:291,308`); uma multa/licenciamento pode chegar **após**
  baixa/alienação do veículo (fato gerador anterior) e seria bloqueada. Sem teste; avaliar se é
  bug de fronteira.
- **L-7 (Patrimonio/Frota):** `Abastecimento`/`OrdemServico` avançam odômetro em `RegistrarAbastecimento`
  e `AbrirOrdemServico`. Não há teste do caso `odometro == atual` (igual permitido por `Avancar`,
  `Odometro.cs:30`) nem de cota exatamente igual (`litros == cotaLitros` é permitido, `Veiculo.cs:229`).
- **L-8 (Administracao):** supressão de aditivo — nenhum teste (ver A-1/A-2).
- **L-9 (Administracao):** `PercentualQuantitativoAcumulado` propagado no Integration Event —
  nenhum teste de fronteira em 25,00% exato vs 25,01% (`> limite` em `Contrato.cs:300` deixa
  passar exatamente 25 — correto — mas sem teste do epsilon).
- **L-10 (Administracao):** `DeclararFracassada`/`ExistePropostaValidaHabilitada`
  (`Licitacao.cs:381-403`): a regra "sem habilitação ainda conta como válida" (`!inabilitado || habilitado`)
  pode impedir declarar fracassada mesmo sem nenhuma habilitação processada. Sem teste de borda.

---

## OS 3 PIORES

1. **P-1 — Parcela de depreciação congelada na origem (não recalcula após reavaliação/impairment).**
   Viola MCASP/NBC TSP 07, distorce o valor contábil de todo o acervo ao longo do tempo e é
   exatamente o tipo de erro de motor contábil que o TCE pega. Pior porque é silencioso e
   sistemático.

2. **A-1 — Limite de aditivo soma acréscimo + supressão no mesmo acumulador (art. 125).**
   Erro de cálculo legal de dinheiro público: bloqueia aditivos lícitos e/ou deixa o percentual
   informado divergir do valor real, propagando número errado para Finanças. Zero cobertura de
   supressão hoje.

3. **P-2 — Depreciação não-idempotente por competência.**
   Reprocessamento/retry duplica a despesa do mês. Reprodutibilidade é requisito explícito do
   módulo; é a "mesma família" do problema do IPTU, só que pela porta da idempotência em vez do
   relógio.

(Menção honrosa: **A-3**, ausência de enforcement de menor preço em `JulgarPropostas` — red flag
de integridade licitatória, embora possa ser "indicação manual" por design; precisa de decisão.)
