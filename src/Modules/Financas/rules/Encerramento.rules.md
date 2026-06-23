# Encerramento de Exercício (PCASP/MCASP) — Rules-as-Code

Encerramento contábil do exercício: apuração do resultado patrimonial (zera VPA/VPD classes 3/4
contra `2.3.7.1.1.01.00`), apuração do resultado orçamentário (zera classes 5/6 de execução),
inscrição de Restos a Pagar (Lei 4.320 art. 36), transposição de saldos e abertura do exercício
seguinte. Autoridade: `docs/architecture/encerramento-exercicio/DESIGN.md` + `pesquisa.md`
(STN/IPC 03, Lei 4.320/64 arts. 35/36/38/105, Regras Gerais MSC — Portaria STN 642/2019).

## Linguagem ubíqua

- **EncerramentoExercicio** — agregado de controle: máquina de estados "só para frente"
  (`Aberto → RapInscrito → EncerramentoParcial → ApuracaoPatrimonial → ApuracaoOrcamentaria →
  Encerrado → AberturaConcluida`), 1 por (tenant, exercício). Encerrado = exercício congelado.
- **MotorEncerramento** — serviço de Application que lê o balancete e emite `LancamentoContabil`
  de partida dobrada via o mesmo factory provado (`OrigemLancamento.Encerramento`); não contabiliza
  ad-hoc, não chama `SaveChanges`.
- **Mês 13** — período lógico da apuração (31/12). **Mês 0** — abertura do exercício seguinte (01/01).
- **MSC de Encerramento** — matriz anual (mês 13) que alimenta o rascunho da DCA (LC 101/2000 art. 51).

## Invariantes (domínio)

1. Reprodutibilidade: nada de relógio no cálculo; a competência (exercício) define data e idempotência.
2. Idempotência: reexecutar uma fase concluída é no-op (`OrigemReferenciaId` determinístico + status).
3. Encerrar um exercício já encerrado = no-op/recusa (congelamento — DESIGN §5).
4. Pré-condição da apuração patrimonial: `2.3.7.1.1.01.00` zerada antes (IPC 03 §§21-23).
5. Cada fase preserva ΣD=ΣC (reusa `LancamentoContabil.Registrar`).
6. As fases avançam uma a uma; pular fases é recusado.

## RBAC

- Execução/fases/MSC de encerramento → `financas.gerenciar` (operação contábil sensível).
- Status do encerramento → `financas.ver`.

## Endpoints

| Método | Rota | Command/Query |
|---|---|---|
| POST | `/contabilidade/encerramento/{exercicio}/executar` | EncerrarExercicioCompleto |
| POST | `/contabilidade/encerramento/{exercicio}/apuracao-patrimonial` | ApurarResultadoPatrimonial |
| POST | `/contabilidade/encerramento/{exercicio}/inscrever-rap` | EncerrarExercicio (RAP) |
| POST | `/contabilidade/encerramento/{exercicio}/abrir-seguinte` | AbrirExercicioSeguinte |
| POST | `/contabilidade/msc/encerramento/gerar` | GerarMscEncerramento |
| GET | `/contabilidade/encerramento/{exercicio}` | ConsultarEncerramento |

<!-- manifest
commands: EncerrarExercicioCompleto, ApurarResultadoPatrimonial, AbrirExercicioSeguinte, GerarMscEncerramento
queries: ConsultarEncerramento
domainEvents: RestosAPagarInscritos, ResultadoPatrimonialApurado, ResultadoOrcamentarioApurado, ExercicioEncerrado, ExercicioSeguinteAberto
integrationEventsPublished: 
integrationEventsConsumed: 
-->
