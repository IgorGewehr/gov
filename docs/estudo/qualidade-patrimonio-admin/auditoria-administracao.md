# Auditoria adversarial — Módulo Administracao (Lei 14.133/2021)

Escopo: `src/Modules/Administracao/**` (Domain + Application) + `tests/Tensorroot.Gov.Modules.Administracao.Tests/**`.
Postura: auditor adversarial — dinheiro público, escrutínio TCE. Foco nas bordas das máquinas de estado
(Licitação, Contrato, Fornecedor) e nos tetos legais (aditivo 25%/50%, garantia 5%/10%).

LIÇÃO IPTU aplicada: o `DateTime.UtcNow` no cálculo **não** se repetiu — todos os handlers usam `TimeProvider`
(ex.: `CelebrarAditivoHandler` linha 67, `AplicarSancaoHandler` linha 70). **Porém a "faixa por valor exato"
reapareceu, em forma pior**: o limite legal do aditivo é calculado a partir de um `percentual` que o chamador
informa **livre e desacoplado** do `valorDelta` real (ver BUG-1). Mesma classe de defeito que o IPTU, só que
aqui controla teto de gasto público.

---

## 1. BUGS REAIS (defeito de código / regra legal incorreta)

### BUG-1 — Aditivo: `percentual` desacoplado do `valorDelta` permite furar o teto de 25%/50% (art. 125) — CRÍTICO
Arquivo: `Domain/Contratos/Contrato.cs:278-334` (`CelebrarAditivo`) e `Application/Contratos/CelebrarAditivo.cs:61-68`.

- O método recebe `percentual` **e** `valorDelta` como argumentos independentes. O teto legal é verificado só
  contra `percentual` (linhas 296-305), mas `ValorAtual` é alterado por `valorDelta` (linhas 311-325). Nada
  garante que `percentual == valorDelta / ValorContratado * 100`.
- Caso (TCE-relevante): contrato de R$ 100.000. `CelebrarAditivo(Acrescimo, percentual: 10m, valorDelta: 80.000)`.
  Passa na checagem (10% < 25%), mas o valor sobe para R$ 180.000 — **+80% real**, teto legal estourado, e
  `PercentualQuantitativoAcumulado` reporta apenas 10%. A trilha de auditoria e o evento
  `AditivoCeleradoIntegrationEvent` saem com 10%, mascarando o sobrepreço.
- O `Validator` (`CelebrarAditivo.cs:37-39`) só limita `Percentual` a `[0,50]` e não valida `ValorDelta` contra ele.
- Certo: o percentual deve ser **derivado** de `valorDelta / ValorContratado.Valor` dentro do agregado (fonte única),
  ou o agregado deve validar a coerência `percentual ≈ valorDelta/valorOriginal` e rejeitar divergência. O chamador
  nunca deveria informar os dois de forma independente.
- Teste a adicionar: `Aditivo_percentual_deve_ser_coerente_com_valorDelta` — `CelebrarAditivo(Acrescimo, 10m, 80.000m)`
  sobre contrato de 100.000 deve **lançar** (ou recalcular para 80% e estourar o teto). Hoje passa silenciosamente.

### BUG-2 — Supressão consome o mesmo teto de 25% do acréscimo (art. 125, §1º) — CRÍTICO
Arquivo: `Domain/Contratos/Contrato.cs:118-120, 293-305`.

- `PercentualQuantitativoAcumulado` soma **acréscimos e supressões no mesmo balde** (`a.EhQuantitativo`, que é
  `Acrescimo or Supressao` — `Aditivo.cs:74`). A checagem do teto (linha 299-300) usa esse acumulado único.
- A Lei 14.133/2021, art. 125, §1º trata acréscimos **e** supressões cada um até 25% (50% em reforma) — são
  limites **separados**, não um teto somado. Pior: como ambos somam positivamente (`+ percentual`, linha 299),
  uma supressão de 20% + um acréscimo de 10% acusam "30% acumulado" e o acréscimo legítimo é **barrado**, embora
  acréscimo (10%) e supressão (20%) estejam ambos dentro de seus limites.
- Caso: contrato 100.000. `Supressao 20%` depois `Acrescimo 10%` → 2º aditivo é **rejeitado** indevidamente.
  Inversamente, supressões grandes "consomem" indevidamente espaço que deveria ser do acréscimo.
- Certo: separar acumulado de acréscimo e acumulado de supressão; cada um contra seu próprio teto.
- Teste a adicionar: `Supressao_e_acrescimo_tem_tetos_separados` — Supressão 20% + Acréscimo 25% deve ser **aceito**
  (hoje lança). E `Supressao_acumulada_acima_de_25_e_rejeitada` (hoje a supressão só é barrada se somada ao acréscimo).

### BUG-3 — `ValorMonetario.Subtrair` faz clamp em zero e mascara supressão inconsistente — ALTO
Arquivo: `Domain/ValueObjects/ValorMonetario.cs:39-44` e uso em `Contrato.cs:318-319`.

- `Subtrair` silenciosamente retorna 0 quando o resultado seria negativo (linha 43). Numa supressão cujo
  `valorDelta` excede `ValorAtual`, o `ValorAtual` vira R$ 0,00 **sem erro**, deixando o contrato com valor zerado
  e ainda "vigente". Para dinheiro público isso é um estado impossível que deveria ser rejeitado, não absorvido.
- Caso: contrato 100.000, já com supressão de 30.000 (ValorAtual 70.000). Nova supressão valorDelta 90.000 →
  ValorAtual = 0, sem exceção. Combinado com BUG-1/BUG-2, o teto não pega porque o percentual é livre.
- Certo: supressão que zere/negative o valor deve lançar; ou `Subtrair` deve lançar em underflow e o agregado
  decidir a regra. O clamp silencioso é apropriado para exibição, não para mutação de estado financeiro.
- Teste a adicionar: `Supressao_que_excede_valor_atual_e_rejeitada`.

### BUG-4 — `Homologar`: seleção do habilitado depende de ordem não-determinística — ALTO
Arquivo: `Domain/Licitacoes/Licitacao.cs:281-289` e `ExistePropostaValidaHabilitada` (381-403).

- Há múltiplas habilitações possíveis para o mesmo fornecedor (`HabilitarLicitante` só faz `_habilitacoes.Add`,
  sem dedup nem substituição — linha 257). Na homologação pega-se a "última" com
  `.OrderBy(_ => 0).LastOrDefault()` (linhas 283-284). `OrderBy(_ => 0)` é um sort por chave constante:
  é **estável**, mas a "ordem de inserção" do `List<T>` em memória não corresponde necessariamente à ordem
  materializada quando a entidade é recarregada do EF Core (a coleção é reidratada pela ordem do banco, sem
  `OrderBy` explícito na configuração). Resultado: o "último" resultado de habilitação pode divergir entre a
  instância em memória e a recarregada do banco.
- Caso (TCE-relevante): fornecedor é `Inabilitado` (doc vencido) e depois `Habilitado` por engano/recurso; ou
  vice-versa. Qual vale na homologação depende da ordem de reidratação — homologação pode validar um vencedor
  que está, de fato, inabilitado. Não há regra de "a habilitação mais recente prevalece" por **data**, porque
  `Habilitacao` nem sequer tem timestamp (`Habilitacao.cs` — sem `DataVerificacao`).
- Certo: `Habilitacao` deve ter data/sequência; a regra deve ser explícita ("prevalece a mais recente por data")
  e a configuração EF deve ordenar a coleção. Ou proibir habilitação duplicada para o mesmo fornecedor.
- Teste a adicionar: `Homologar_usa_habilitacao_mais_recente` (Habilitado→Inabilitado deve **bloquear** homologação)
  e teste de round-trip de persistência confirmando a mesma decisão antes/depois do `SaveChanges`.

### BUG-5 — `ExistePropostaValidaHabilitada` trata proposta sem habilitação como "válida", impedindo fracasso legítimo — MÉDIO
Arquivo: `Domain/Licitacoes/Licitacao.cs:381-403`, em especial linha 396 (`if (!inabilitado || habilitado)`).

- A regra considera "válida" qualquer proposta **não desclassificada e não inabilitada** — inclusive propostas
  **sem nenhuma habilitação verificada ainda**. Logo, se há proposta recebida mas a comissão ainda não habilitou
  ninguém, `DeclararFracassada` é bloqueada por "existe proposta válida" (linha 308-311), mesmo que de fato
  nenhum licitante tenha sido habilitado. O comentário na linha 395 assume isso como intencional, mas isso impede
  o fracasso por "nenhum habilitado" no caso real em que ninguém foi habilitado (todos pendentes/ausentes).
- Caso: 1 proposta recebida, comissão registra `Inabilitado` para esse fornecedor → fracasso OK (testado).
  Mas: 1 proposta recebida, ninguém habilitado ainda → `inabilitado=false`, `habilitado=false` → `!false=true` →
  "válida" → fracasso bloqueado. Para fracasso por inabilitação geral, a comissão é forçada a inabilitar
  explicitamente cada um; não há teste cobrindo o caminho "todos pendentes".
- Certo: definir e testar a semântica de "proposta sem habilitação" no fracasso (provável: não conta como válida
  quando a fase de habilitação já encerrou). Hoje é ambíguo e sem cobertura.
- Teste a adicionar: `Fracassada_quando_unica_proposta_inabilitada` já existe; falta `Fracassada_com_proposta_pendente_de_habilitacao` documentando o comportamento esperado.

### BUG-6 — Aditivo de prazo aceita `novaVigenciaFim` anterior ao fim atual sem erro (silenciosamente ignorado) — MÉDIO
Arquivo: `Domain/Contratos/Contrato.cs:327-330`.

- `if (novaFim > VigenciaFim) VigenciaFim = novaFim;` — um aditivo de Prazo com `novaVigenciaFim` **menor** que a
  vigência atual é aceito, registra o aditivo, emite evento, mas **não altera** a vigência. Fica um aditivo de
  prazo "fantasma" sem efeito, e não há rejeição de uma "prorrogação" que na verdade encurta. Também não se valida
  `novaVigenciaFim >= dataCelebracao` nem `>= VigenciaInicio`.
- Caso: contrato vigência até 2026-12-31. Aditivo Prazo com `novaVigenciaFim = 2026-06-01` → aceito, vigência
  permanece 2026-12-31, aditivo registrado sem efeito. Auditoria fica inconsistente.
- Certo: aditivo de Prazo deve exigir `novaVigenciaFim` informado e `> VigenciaFim`; caso contrário, lançar.
- Teste a adicionar: `Aditivo_prazo_com_data_anterior_e_rejeitado` e `Aditivo_prazo_exige_nova_data`.

### BUG-7 — `Encerrar()` não checa vigência nem dotação; e contrato pode ser encerrado direto de `Eficaz` sem execução — BAIXO/MÉDIO
Arquivo: `Domain/Contratos/Contrato.cs:409-418`.

- `Encerrar` exige apenas `Eficaz or EmExecucao`. Encerra-se um contrato que nunca executou (Eficaz) como se
  tivesse concluído objeto — `ContratoEncerrado` é emitido. Sem regra de data (encerrar antes de `VigenciaInicio`
  é permitido). Para conclusão de objeto isso deveria, no mínimo, registrar a distinção encerramento normal vs.
  antecipado (que é rescisão). Não é furo grave, mas a máquina de estados não distingue "encerrado por término de
  vigência" de "encerrado prematuramente".
- Teste a adicionar: definir/cobrir a política de encerramento de `Eficaz` (permitido? exige vigência decorrida?).

---

## 2. LACUNAS DE TESTE (código provavelmente correto, mas sem cobertura — risco TCE)

- **Isolamento de tenant nas mutações de aditivo/garantia/sanção**: os testes de tenant cobrem só consulta
  (`Cenario_13`, `Cenario_11`). Não há teste de que um handler (ex.: `CelebrarAditivoHandler`) carrega contrato
  de outro tenant — o repositório aplica Global Query Filter, mas isso não é exercido por teste de handler.
- **`BloquearEficaciaPorDotacaoIndisponivel` quando contrato está `EmExecucao`**: `Contrato.cs:224-232` só
  reverte de `Eficaz`. Se já está `EmExecucao` e a dotação some, o estado **permanece EmExecucao** (gastando sem
  cobertura). Sem teste. Verificar se é intencional — provável bug latente (art. 105-106/LRF).
- **Idempotência real do `BloquearEficaciaQuandoDotacaoIndisponivelHandler`**: o comentário (linhas 40-41) afirma
  idempotência por EventId/Inbox, mas não há teste de reprocessamento do mesmo `EventId`.
- **`MarcarAditivoPublicado` / eficácia do aditivo (art. 174)**: `Contrato.cs:342-347` existe, mas nenhum teste
  cobre publicação de aditivo no PNCP nem o efeito de `PublicadoNoPncp` no aditivo.
- **`ConfirmarDotacao` após `BloquearEficacia`** (re-confirmação): caminho Assinado→bloqueado→reconfirmado→Eficaz
  sem teste.
- **`AvaliarEficacia` só promove a partir de `Assinado`** (`Contrato.cs:448-454`): se `ConfirmarDotacao` chega
  com contrato já `EmExecucao`/`Encerrado` o efeito é nulo — sem teste de borda.
- **`Recurso` (impugnação) e `Lote`**: `Recurso.cs` tem `Interpor`/`Julgar` e `Lote` existe, mas **não há
  nenhum teste** para recursos nem para a máquina de lotes/propostas múltiplas.
- **`Proposta.Desclassificar` e reflexo no julgamento/fracasso**: sem teste cobrindo desclassificação.
- **Sanção com `DataInicio` futura** (aplicada hoje, vigente no futuro): `EstaVigente` é coberto por datas, mas
  não o caso de `AplicarSancao` que não muda situação hoje mas deveria ao chegar a data — não há reavaliação
  temporal (provável lacuna de regra, não só de teste).
- **`AplicarSancao` em fornecedor `Inativo`**: `Contrato`/`Fornecedor.cs:122` bloqueia mudança para Sancionado se
  Inativo, mas ainda emite evento e registra sanção — comportamento não testado.
- **Limite do aditivo com `percentual` fracionário/arredondamento** (ex.: 24.999%): sem teste de borda do `>`.
- **`CelebrarContrato` com `VigenciaInicio == VigenciaFim`** (vigência de 1 dia): aceito pelo domínio (`<`), sem
  teste; verificar se faz sentido legal.

---

## 3. OS 3 PIORES (priorizar)

1. **BUG-1 — percentual do aditivo desacoplado do valorDelta** (`Contrato.cs:278-334`). Permite ultrapassar o teto
   legal de 25%/50% (art. 125) com sobrepreço mascarado na auditoria e no Integration Event. É a reincidência da
   classe IPTU ("faixa por valor exato"), agora controlando gasto público. **Mais grave do escopo.**

2. **BUG-2 — supressão e acréscimo no mesmo teto somado** (`Contrato.cs:118-120, 293-305`; `Aditivo.cs:74`).
   Viola art. 125, §1º (limites separados): barra acréscimos legítimos e/ou deixa passar combinações erradas.
   Cálculo legal incorreto, não apenas falta de teste.

3. **BUG-4 — homologação sobre habilitação não-determinística** (`Licitacao.cs:281-289`; `Habilitacao.cs` sem
   timestamp). Pode homologar vencedor de fato inabilitado dependendo da ordem de reidratação do EF Core. Risco
   direto de adjudicação irregular sob escrutínio do TCE.

---

## Nota sobre a lição IPTU
- `DateTime.UtcNow` no cálculo: **NÃO reincidiu** — `TimeProvider` é usado consistentemente nos handlers.
- "Faixa por valor exato / não-reprodutível": **reincidiu em forma pior** (BUG-1/BUG-2): o teto legal do aditivo
  depende de um percentual informado pelo chamador, desacoplado do valor real e com limites somados indevidamente.
