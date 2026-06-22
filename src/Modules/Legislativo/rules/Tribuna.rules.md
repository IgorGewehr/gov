# Tribuna (Oradores + Cronometro) — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). Gap G3: inscricao de oradores por fase de uso da palavra
> e cronometro autoritativo do servidor (iniciar/pausar/retomar/encerrar), acoplado a `Sessao` sem
> incha-la (agregado proprio `TribunaSessao` vinculado a `SessaoId`).
> Autoridade: `docs/architecture/legislativo-sprint/DESIGN-GAPS.md` (G3).
> Constituicao: multi-tenant, auditoria imutavel, dominio rico, banco-por-tenant; tempo derivado de
> timestamps do servidor (anti-fraude/prova) — nunca do cliente.

---

## 1. Linguagem ubiqua

- **Tribuna:** controle de uso da palavra de uma sessao (uma por sessao).
- **Fase de uso da palavra:** `PequenoExpediente`, `GrandeExpediente`, `ExplicacaoPessoal`, `TribunaLivre`.
- **Inscricao de orador:** `Inscrito` -> `EmUso` -> `Concluido` (ou `Cancelado` antes de falar).
- **Tempo concedido / utilizado / excedente:** regimental vs. efetivo (descontadas as pausas) vs. estouro.
- **Cronometro server-side:** `IniciadoEm`/`EncerradoEm`/pausas gravados pelo servidor.

---

## 2. Invariantes do agregado `TribunaSessao`

- **T-1** So aberta para sessao **nao terminal**.
- **T-2** `Inscrever` so com sessao nao terminal; **idempotente por (vereador, fase)**; default de tempo =
  `TempoPadraoOrador` (parametrizavel por tenant, nunca hardcoded); `Ordem` sequencial.
- **T-3** **No maximo um orador `EmUso`** por vez (exclusao mutua).
- **T-4** `IniciarFala` so se `Inscrito` e ninguem `EmUso`; emite `OradorIniciouFala`.
- **T-5** `Pausar`/`Retomar` so sobre o orador `EmUso`; pausa acumula intervalo.
- **T-6** `EncerrarFala` so sobre `EmUso`; calcula `TempoUtilizado` e `Excedente = max(0, util - concedido)`;
  emite `OradorEncerrouFala`; libera a exclusao mutua.
- **T-7** `CancelarInscricao` so se `Inscrito`.
- **T-8** Tempo **derivado de timestamps do servidor** (handler usa `TimeProvider`), nunca do cliente.
- **T-9** Trilha das falas e **append-only** (situacao evolui, registros nao sao apagados).

---

## 3. Consulta

- **Q-1** `ObterTribunaDaSessao` retorna fila por fase, orador atual e tempos (concedido/utilizado/
  excedente + `IniciadoEm` para o cronometro visual no front).

---

## 4. RBAC

- `legislativo.ver` — leitura (estado da tribuna).
- `legislativo.gerenciar` — abrir tribuna, inscrever, cancelar inscricao.
- `legislativo.tribuna.controlar` — **verbo fino de mesa diretora**: iniciar/pausar/retomar/encerrar.

---

## 5. Cenarios BDD (resumo)

- **Inscricao idempotente:** dado vereador ja inscrito na fase, quando inscrever de novo, entao no-op.
- **Exclusao mutua:** dado um orador em uso, quando iniciar outro, entao falha.
- **Pausa desconta tempo:** dado orador que pausou e retomou, quando encerrar, entao o intervalo de pausa
  e descontado do tempo utilizado.
- **Excedente apurado:** dado tempo concedido de 5 min e fala de 7 min, quando encerrar, entao excedente = 2 min.
- **Tempo do servidor:** o `momento` vem de `TimeProvider`, nunca do cliente (T-8).

<!-- manifest
commands: AbrirTribuna, InscreverOrador, IniciarFala, PausarFala, RetomarFala, EncerrarFala, CancelarInscricao
queries: ObterTribunaDaSessao
domainEvents: OradorIniciouFala, OradorEncerrouFala
integrationEventsPublished: 
integrationEventsConsumed: 
-->
