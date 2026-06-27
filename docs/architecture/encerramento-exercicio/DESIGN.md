# DESIGN — Encerramento de Exercício Contábil (PCASP/MCASP)

> **Módulo:** Finanças · **Camadas:** Domain/Application/Infrastructure (Clean Arch, CONVENCOES-ENGENHARIA.md §2)
> **Pré-requisito provado:** PCASP (lançamentos automáticos via
> `MotorContabil`), balancete (ΣD=ΣC), MSC agregada e DCASP.
> **Fundamentação:** STN/IPC 03, Lei 4.320/64 (arts. 35, 36, 38, 105),
> Regras Gerais MSC (Portaria STN 642/2019) e o doc dedicado SICONFI ["MSC de encerramento do exercício"](https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2024_Orientacoes_MSC_encerramento.pdf).
> Itens **[a confirmar]** preservados conforme CONVENCOES-ENGENHARIA.md §8.

---

## 1. Diagnóstico do GAP e princípio de design

O `MotorContabil` atual roteiriza **fatos do ciclo** (empenho/liquidação/…): 1 fato → 1 valor `V` → roteiro
estático (`EventoContabil`) → grupos por natureza → `LancamentoContabil`. **O encerramento é diferente em
espécie:** não há "um fato com valor V"; há **N contas a zerar, cada uma com SEU saldo** (o saldo apurado no
balancete de dezembro). Logo o encerramento **não cabe num único `EventoContabil` roteirizado** — ele é uma
**orquestração** que lê saldos e **emite vários `LancamentoContabil` de partida dobrada**, um por conta/par.

**Decisão-chave (reuso do motor):** reusar a peça **estável e provada** — o agregado
`LancamentoContabil.Registrar(...)` (valida ΣD=ΣC, natureza homogênea, conta analítica, período aberto) e o
pipeline `LancamentoContabilRegistrado → ProjetarBalanceteHandler → balancete → DerivadorMsc`. O encerramento
**não** inventa contabilização paralela: produz `LinhaLancamento[]` e chama o **mesmo factory**, com
`OrigemLancamento.Encerramento` (enum já existe) e período próprio. Assim a MSC e a DCA nascem corretas **de
graça**, pois leem o mesmo balancete.

---

## 2. Modelo de período de encerramento (mês 13/14 lógico)

A virada já é tratada no `BalanceteProjection.SaldoAnteriorAsync`: contas com `Encerramento=true` (3/4/5/6)
zeram em jan; permanentes (1/2) carregam saldo. **Esse comportamento é mantido**, mas precisamos de períodos
extraordinários para **escriturar** a apuração sem poluir dezembro/mês 12:

- **Mês 13 (Encerramento):** apuração patrimonial (zera 3/4) + apuração/conferência orçamentária (zera 5/6 de
  execução) + inscrição de RAP do exercício corrente. Lançamentos `OrigemLancamento.Encerramento`,
  `Data = 31/12/exercício`, `PeriodoMes = 13`.
- **Mês 0 (Abertura) no exercício seguinte:** transposição do resultado (`...01→...02`), ajustes de exercícios
  anteriores (`...03→...02`) e reabertura de saldos de Ativo/Passivo/PL. `PeriodoMes = 0`,
  `Data = 01/01/(exercício+1)`.

> **Mudança mínima necessária:** `LancamentoContabil.PeriodoMes` hoje deriva de `data.Month` (1–12). Adicionar
> um parâmetro opcional `periodoMesOverride` no factory `Registrar` (default = `data.Month`) para os períodos
> 13 e 0. `LancamentoContabilConfiguration` e a chave única do balancete (`conta+exercício+mês`) já comportam
> os valores 0 e 13. `BalanceteProjection` passa a aceitar mês 13/0 como período válido.

---

## 3. Agregado de controle: `EncerramentoExercicio`

Novo agregado em `Domain/Contabilidade/Encerramento/` — **máquina de estados idempotente e auditável** que
governa as fases (nunca apaga; preserva trilha, CONVENCOES-ENGENHARIA.md §4):

```
EncerramentoExercicio : AggregateRoot<EncerramentoExercicioId>, IMustHaveTenant
  TenantId, Exercicio, Status, IniciadoEmUtc, ConcluidoEmUtc
  Status: Aberto → EncerramentoParcial → ApuracaoPatrimonial → ApuracaoOrcamentaria
          → RapInscrito → Encerrado → AberturaConcluida   (transições só "para frente")
  invariantes: 1 registro por (TenantId, Exercicio); cada fase só roda após a anterior;
               relança DomainEvent por fase (ex.: ResultadoPatrimonialApurado).
```

O agregado **não** carrega os lançamentos; ele **registra que cada fase ocorreu** (idempotência: reexecutar
uma fase já concluída é no-op). Cada fase chama o `LancamentoContabil.Registrar` via um **serviço de
domínio/application** (abaixo). O `EncerrarExercicioCommand` legado (`RestosAPagar/EncerrarExercicio.cs`, hoje
só inscreve RAP por empenho e tem `TODO(revisao-contabil)`) torna-se a **fase 3** orquestrada por este fluxo.

---

## 4. O motor de encerramento: `MotorEncerramento`

Serviço de Application em `Contabilidade/Encerramento/MotorEncerramento.cs`, espelhando o papel do
`MotorContabil` (não chama `SaveChanges` — o handler controla a transação). Recebe o balancete de dezembro e
emite `LancamentoContabil`. Métodos (um por fase), todos **idempotentes por `OrigemReferenciaId`**
(usa `ExisteParaOrigemAsync`, igual ao motor atual):

### 4.1 `ApurarResultadoPatrimonial` (zera classes 3 e 4 → `2.3.7.1.1.01.00`)
Lê do balancete (mês 12) todas as analíticas de classe **3** e **4** com saldo ≠ 0. Para cada conta gera **um
lançamento homogêneo patrimonial**:

```
VPD (3.x):  D 2.3.7.1.1.01.00   C 3.x.x...   (valor = saldo da conta 3)
VPA (4.x):  D 4.x.x...          C 2.3.7.1.1.01.00   (valor = saldo da conta 4)
```

`OrigemReferenciaId` = hash determinístico `(exercicio, "APUR-PATR", codigoConta)` → idempotência. Após a fase,
o saldo de `2.3.7.1.1.01.00` é o **resultado patrimonial** (credor=superávit / devedor=déficit). Contas 3/4
ficam zeradas no balancete (e portanto na MSC). **Pré-condição** validada: `2.3.7.1.1.01.00` zerada antes
(IPC 03 §21-23).

### 4.2 `ApurarResultadoOrcamentario` (confere/zera classes 5 e 6 de execução)
Não vai a PL — confronto receita×despesa é evidenciado no Balanço Orçamentário. Encerra verticalmente os pares
`5.2.1↔6.2.1` (previsão×execução receita) e `5.2.2↔6.2.2` (dotação×crédito), conforme IPC 03 §§33-53 e o seed
já existente (`5.2.2.1.01`, `6.2.1.1.01`, `6.2.1.2.01`, `6.2.2.1.1.00.00`, `6.2.2.1.3.0x`). Lançamentos
homogêneos **orçamentários**. As contas de execução/dotação (`Encerramento=true`) zeram. **Cenário §51 (sem
controle origem inicial×adicional)** adotado por default; §50 fica como evolução parametrizável.

### 4.3 `InscreverRestosAPagar` (RAP do exercício corrente)
Reusa a lógica de `EncerrarExercicioHandler` (classifica RPP=liquidado-não-pago / RPNP=não-liquidado por
empenho) **e** acrescenta os **lançamentos de controle** que faltavam (o `TODO(revisao-contabil)`): transfere os
empenhos para `6.2.2.1.3.05/06/07` e escritura `5.3`/`6.3`. **Estes lançamentos saem na MSC agregada de
dezembro** (não no mês 13) — exigência das Regras Gerais MSC: a inscrição de RAP integra a MSC de dezembro.
Por isso esta fase é escriturada com `PeriodoMes = 12` (Data 31/12), enquanto 4.1/4.2 vão ao mês 13.

### 4.4 `EncerrarParcial` (curto×longo prazo, perdas/provisões) — IPC 03 §31-32
Reclassificação Circulante↔Não Circulante (critério 12 meses) e ajustes para perdas/provisões. **Escopo
inicial:** estrutura de fase + ponto de extensão (roteiros parametrizáveis por tenant); a automação plena dos
ajustes fica marcada `// TODO(revisao-contabil)`. Não bloqueia a apuração.

### 4.5 `AbrirExercicioSeguinte` (mês 0 do exercício+1)
Transferência do resultado e reabertura (IPC 03 §27-28, §31):

```
D 2.3.7.1.1.01.00  C 2.3.7.1.1.02.00   (resultado do exercício → exercícios anteriores)
D 2.3.7.1.1.03.00  C 2.3.7.1.1.02.00   (ajustes de ex. anteriores, se houver saldo)
```

A **reabertura dos saldos de Ativo/Passivo/PL** é obtida **sem novo lançamento** — o `BalanceteProjection` já
transporta o saldo final das contas permanentes para o mês 1 do exercício seguinte. **Confirmação de
consistência:** ΣD=ΣC do BP de abertura. (Nota: **todas** Ativo/Passivo F e P transferem; o atributo F/P só
classifica para o superávit financeiro — [a confirmar] mantido.)

---

## 5. Apuração do superávit financeiro (Lei 4.320 art. 43 §2º / art. 105)

Já há base: `ContaContabil.IndicadorSuperavitFinanceiro` (F/P) e o BP calcula `AtivoFinanceiro −
PassivoFinanceiro` (`GerarBalancoPatrimonialDvp.cs`). O encerramento **reaproveita** esse cálculo para o
**Demonstrativo do Superávit Financeiro por Fonte** (fonte de créditos adicionais no exercício seguinte). A
mecânica exata por fonte fica `// TODO(revisao-contabil)` e **[a confirmar]** (MCASP Parte II/IV) — não bloqueia
o encerramento patrimonial/orçamentário.

---

## 6. Geração da MSC de Encerramento (anual)

`TipoMatrizMsc.Encerramento` já existe. Novo command `GerarMscEncerramentoCommand(Exercicio)` espelhando
`GerarMscCommand`, reusando `DerivadorMsc` + `MatrizSaldosContabeis.Montar(... Encerramento ...)`:

- **Saldo inicial (`beginning_balance`)** = saldo final da **MSC Agregada de dezembro** (mês 12).
- **`period_change`** = **toda a movimentação de encerramento** = lançamentos dos meses **13** (apuração) — a
  MSC de encerramento agrega o balancete do mês 13 sobre o de dezembro.
- **Saldo final (`ending_balance`)** = saldos **após apuração**, com **classes 3 e 4 zeradas** (e 5/6 de
  execução zeradas). Validação SICONFI `saldo_inicial + movimento = saldo_final` cai no invariante
  `MatrizSaldosContabeis.EstaBalanceada` já provado.
- Idempotente por `(Tenant, Exercicio, Mes=13, TipoMatriz=Encerramento)` no `IMscGeradaStore`. Publica
  `MSCGeradaIntegrationEvent` via Outbox (consumido por Transparência → SICONFI). **[a confirmar]** prazo de
  envio (Regras Gerais cita "último dia de março"); ver doc dedicado MSC de encerramento (2024).

---

## 7. Relação com a DCA anual

A **DCA** (Declaração de Contas Anuais, LC 101/2000 art. 51) é gerada pelo SICONFI/Transparência a partir do
**rascunho da MSC de Encerramento** (Regras Gerais MSC). No Tensorroot.Gov: **a MSC de Encerramento (§6) é o
insumo da DCA** — não há geração de DCA "à parte". Sem o encerramento, a DCA nasceria com classes 3/4 não
zeradas, RAP subnotificado e BP de abertura incorreto. Com o encerramento, as demonstrações
DCASP já existentes (BP, DVP, Balanços Orçamentário/Financeiro) refletem o resultado apurado, e a MSC de
encerramento (mês 13) alimenta a consolidação. Nenhum novo agregado de DCA é necessário nesta fase.

---

## 8. Orquestração, Handlers e transação

`EncerrarExercicioCompletoCommand(Exercicio)` (novo, em `Contabilidade/Encerramento/`) — handler orquestra, em
**uma unidade de trabalho por fase** (cada fase = SaveChanges → dispara `ProjetarBalanceteHandler` → atualiza
balancete → fase seguinte lê saldos já atualizados):

1. `EncerramentoExercicio.Iniciar` (cria/recupera agregado, valida pré-condições: dezembro fechado, `...01.00`
   zerada).
2. `MotorEncerramento.InscreverRestosAPagar` (mês 12) → SaveChanges → publica eventos.
3. `MotorEncerramento.EncerrarParcial` (mês 13).
4. `MotorEncerramento.ApurarResultadoPatrimonial` (mês 13).
5. `MotorEncerramento.ApurarResultadoOrcamentario` (mês 13).
6. `EncerramentoExercicio.MarcarEncerrado` + `GerarMscEncerramentoCommand`.
7. `MotorEncerramento.AbrirExercicioSeguinte` (mês 0 do exercício+1) → `MarcarAberturaConcluida`.

Cada fase é **idempotente** (status do agregado + `ExisteParaOrigemAsync`), permitindo retomar de falha sem
duplicar lançamentos (CONVENCOES-ENGENHARIA.md §8 resiliência). Pipeline MediatR padrão: Validation → Logging →
Transaction → Idempotency.

---

## 9. Endpoints / RBAC

Em `FinancasEndpoints.MapearContabilidade`, sob `/api/financas/contabilidade/encerramento` —
**mutações exigem `financas.gerenciar`** (operação contábil sensível), leituras `financas.ver`
(padrão do módulo). Idealmente uma permissão dedicada `financas.encerrar` mais restrita **[sugestão]**:

| Método | Rota | Command/Query | RBAC |
|---|---|---|---|
| POST | `/encerramento/{exercicio}/executar` | `EncerrarExercicioCompletoCommand` | `financas.gerenciar` |
| POST | `/encerramento/{exercicio}/apuracao-patrimonial` | fase isolada (reexec) | `financas.gerenciar` |
| POST | `/encerramento/{exercicio}/inscrever-rap` | fase RAP (substitui rota atual) | `financas.gerenciar` |
| POST | `/encerramento/{exercicio}/abrir-seguinte` | abertura | `financas.gerenciar` |
| POST | `/contabilidade/msc/encerramento/gerar` | `GerarMscEncerramentoCommand` | `financas.gerenciar` |
| GET | `/encerramento/{exercicio}` | status do agregado | `financas.ver` |

A rota legada `POST /restos-a-pagar/encerrar-exercicio` é mantida como atalho da fase RAP (compat).
Frontend: o `EncerrarExercicioModal.tsx` existente evolui para wizard de fases com status.

---

## 10. Persistência e migrations

- Nova tabela `financas.encerramento_exercicio` (1 por tenant+exercício) — config + migration por módulo
  (CONVENCOES-ENGENHARIA.md §9). Interceptors de tenant/auditoria já aplicados na base.
- `LancamentoContabilConfiguration` / chave única do balancete: aceitar `PeriodoMes ∈ {0,13}` (sem migration
  de schema se a coluna já é `int`; ajustar só índices/constraints se houver CHECK 1..12).
- `IMscGeradaStore`: já chaveado por `TipoMatriz` — comporta `Encerramento` sem mudança de schema.

---

## 11. Testes (BDD-first, CONVENCOES-ENGENHARIA.md §1/§12)

`Given` balancete de dezembro com VPA>VPD `When` apurar `Then` `2.3.7.1.1.01.00` credora = superávit e classes
3/4 zeradas; ΣD=ΣC após cada fase; MSC de encerramento `saldo_inicial+movimento=saldo_final` com 3/4 zeradas;
RAP inscrito aparece na MSC de **dezembro**; abertura: resultado migra para `...02.00` e BP de abertura fecha;
idempotência: reexecutar fase não duplica lançamentos.

---

## 12. Itens [a confirmar] (CONVENCOES-ENGENHARIA.md §8)

RPNP→RPP na edição vigente do MCASP; mecânica do superávit financeiro por fonte e tabela de atributo F/P
(MCASP Parte II/IV); prazo de envio da MSC de encerramento; nomenclatura mês 13/14 (não padronizada pela STN —
aqui modelada como 13=apuração e 0=abertura). Confirmar antes de codar as fases 4.2/4.5 e 5.
