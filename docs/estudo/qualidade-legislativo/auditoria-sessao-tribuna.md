# Auditoria adversarial — Sessão / Quórum / Tribuna (módulo Legislativo)

Escopo: instalação com quórum no mínimo, presença/ausência, ordem do dia e o cronômetro de
tribuna (limite, exceder, pausar/retomar, troca de orador, exclusão mútua) + edges de estado da
sessão (reabrir/suspender). Foco em demonstração para comissão de recebimento — onde um erro de
apuração é fatal.

Arquivos auditados:
- `src/.../Domain/Sessoes/Sessao.cs`, `Presenca.cs`, `ItemOrdemDoDia.cs`, `ValueObjects.cs`
- `src/.../Domain/Tribuna/TribunaSessao.cs`, `InscricaoOrador.cs`, `PausaFala.cs`
- `src/.../Application/Sessoes/*` e `Application/Tribuna/*`
- `src/.../Infrastructure/.../Configurations/TribunaSessaoConfiguration.cs`
- `tests/.../Legislativo.Tests/SessaoFluxoTests.cs`, `TribunaTests.cs`
- specs `rules/Sessao.rules.md`, `rules/Tribuna.rules.md`

Veredicto resumido: a máquina de estados da Sessão está sólida e bem testada. O **risco real está
no cronômetro da Tribuna**, onde há um bug de apuração demonstrável (B-1) e várias bordas sem
guarda nem teste. Quórum de instalação está correto. Há divergências código x spec que merecem
nota mas não são fatais.

---

## A. BUGS REAIS (comportamento errado, não apenas lacuna de teste)

### BUG-1 — Cronômetro: timestamp de encerramento/retomada anterior ao início produz tempo NEGATIVO (apuração corrompida) — CRÍTICO
- **Arquivo:** `InscricaoOrador.cs:82-92` (`TempoUtilizado`, `Excedente`) + `TribunaSessao.cs:146-156` (`EncerrarFala`).
- **Caso-limite:** `EncerrarFala` (ou `RetomarFala`) com `momento < IniciadoEm`. Acontece na prática
  com relógio do servidor não-monotônico: NTP step para trás, ajuste de horário de verão, troca de
  host por trás de load balancer com clock skew, ou simplesmente dois `TimeProvider.GetUtcNow()`
  fora de ordem. `EncerrarFala` **não valida** que `momento >= IniciadoEm`.
- **Errado:** `TempoUtilizado = (fim - inicio) - TotalPausas` vira **negativo**; `Excedente` calcula
  `usado > TempoConcedido` com `usado` negativo → `Excedente = 0` (esconde o erro), e o painel/ata
  exibe tempo negativo. Numa demo isso aparece como "−00:42 utilizado".
- **Observação:** `PausaFala.De` (`PausaFala.cs:31-38`) protege `fim < inicio` lançando exceção —
  ou seja, a borda já é conhecida em UM ponto do agregado, mas **não** em `IniciarFala→EncerrarFala`.
  Inconsistência. Pior: se `RetomarFala` recebe `momento < PausaIniciadaEm`, é o `PausaFala.De` que
  lança — mas a exceção vaza como erro técnico no meio da sessão, em vez de ser rejeitada na borda.
- **Certo:** `IniciarFala`/`Pausar`/`Retomar`/`Encerrar` devem rejeitar `momento` anterior ao último
  timestamp registrado (monotonicidade), OU `TempoUtilizado` deve fazer `clamp` em `TimeSpan.Zero`.
  Recomendo a guarda explícita (mais auditável) + clamp defensivo no cálculo.
- **Teste a adicionar:** `EncerrarFala_com_momento_anterior_ao_inicio_rejeita` (espera exceção) e
  `TempoUtilizado_nunca_negativo`.

### BUG-2 — Re-inscrição idempotente devolve inscrição já CONCLUÍDA, impedindo segunda fala legítima na mesma fase — ALTO
- **Arquivo:** `TribunaSessao.cs:97-103` (`Inscrever`).
- **Caso-limite:** vereador fala no Grande Expediente, `EncerrarFala` → `Situacao = Concluido`.
  Mais tarde, na MESMA fase, a mesa o inscreve de novo (ex.: nova rodada, ou aparte que virou fala).
  O filtro de idempotência é `i.Situacao != Cancelado` — então casa com a inscrição **Concluído** e
  retorna a antiga.
- **Errado:** `IniciarFala` sobre essa inscrição lança ("exige Inscrito", `InscricaoOrador.cs:108`).
  Resultado: o orador fica **impossibilitado de falar de novo na fase** e a UI recebe um id de
  inscrição morto. Em sessão real isso trava a tribuna.
- **Certo:** idempotência deve casar apenas inscrições **ativas e não-iniciadas** (`Situacao ==
  Inscrito`), ou no máximo `Inscrito`/`EmUso`. `Concluido` e `Cancelado` não devem bloquear nova
  inscrição.
- **Teste a adicionar:** `Reinscrever_apos_concluir_cria_nova_inscricao_na_mesma_fase`.

### BUG-3 — Exclusão mútua falha por orador PAUSADO bloquear troca de orador — ALTO (decisão de design não documentada)
- **Arquivo:** `TribunaSessao.cs:57-58` (`OradorEmUso`) + `:119-123` (`IniciarFala`).
- **Caso-limite:** Orador A inicia fala, mesa **pausa** A (ainda `Situacao == EmUso`, só
  `PausaIniciadaEm != null`). Mesa tenta dar a palavra a B (troca de orador). `OradorEmUso` ainda
  encontra A (segue `EmUso`), então `IniciarFala(B)` lança "Já existe orador em uso".
- **Errado/ambíguo:** o regimento típico permite pausar A para conceder aparte/fala a B e depois
  retomar A. Hoje é impossível sem encerrar A (perdendo o tempo restante de A). A spec T-3/T-5 não
  esclarece se "EmUso pausado" conta como ocupando a tribuna. Como está, a **troca de orador com
  pausa é inviável** — exatamente um dos casos que o escopo pediu para validar.
- **Certo:** decidir explicitamente. Se pausar deve liberar a tribuna, `OradorEmUso` deve ignorar
  pausados, ou `Pausar` deve mover para um estado `Pausado` distinto. Se NÃO deve liberar,
  documentar e rejeitar `IniciarFala` com mensagem clara. Qualquer uma exige teste.
- **Teste a adicionar:** `Pausar_orador_e_iniciar_outro_<comportamento_decidido>`.

### BUG-4 — `Apartes` órfão: `RegistrarAparte` existe no domínio mas não há comando/handler nem é exposto — MÉDIO (lacuna funcional)
- **Arquivo:** `InscricaoOrador.cs:181-190` (`RegistrarAparte`) vs. `Application/Tribuna/ControleTribuna.cs`
  (nenhum `RegistrarAparteCommand`).
- **Caso-limite:** comportamento de domínio inacessível; a contagem de apartes anunciada na doc
  (`InscricaoOrador.cs:73`) nunca incrementa em produção. Não corrompe apuração, mas é "feature
  fantasma" — risco de a comissão pedir e não existir.
- **Certo:** ou expor o comando, ou remover o método e a propriedade até implementar.

---

## B. DIVERGÊNCIAS CÓDIGO × SPEC (corretas-de-um-lado, erradas-do-outro)

### DIV-1 — `IncluirNaOrdemDoDia` aceita sessão SUSPENSA; spec I-6 restringe a Agendada/Aberta
- **Arquivo:** `Sessao.cs:106-111` usa `GarantirNaoTerminal()` (permite `Suspensa`); `Sessao.rules.md:118`
  (I-6) e a máquina de estados (linha 135) dizem **"Agendada | Aberta"**.
- **Caso-limite:** pautar proposição com a sessão suspensa. Código permite, spec proíbe.
- **Errado:** depende da fonte de verdade. A spec é normativa ("o código é consequência dela",
  `Sessao.rules.md:20`). Então **o código está mais permissivo que o regimento declarado**. Não é
  fatal (pautar durante suspensão é defensável), mas é divergência code-vs-spec não rastreada.
- **Teste a adicionar:** decidir e travar com `IncluirNaOrdemDoDia_em_sessao_suspensa_<permite|rejeita>`.

### DIV-2 — `VerificarQuorum` permitido em sessão TERMINAL; máquina de estados restringe a Agendada/Aberta/Suspensa
- **Arquivo:** `Sessao.cs:138-143` (`VerificarQuorum` sem guarda de estado) vs. `Sessao.rules.md:137`
  (origem "Agendada | Aberta | Suspensa").
- **Caso-limite:** chamar `VerificarQuorumCommand` numa sessão já `Encerrada`/`Cancelada`. Hoje
  apura e **emite `QuorumVerificado`** mesmo terminal — evento de domínio espúrio pós-encerramento.
- **Errado:** emissão de evento em estado terminal contraria a intenção da máquina de estados e
  polui a trilha. Baixo impacto, mas é apuração/evento fora de hora.
- **Teste a adicionar:** `VerificarQuorum_em_sessao_terminal_<rejeita_ou_naoEmiteEvento>`.

### DIV-3 — `SessaoRealizadaIntegrationEvent` publicado via `IPublisher` direto, não via Outbox
- **Arquivo:** `AbrirSessao.cs:53` e `EncerrarSessao.cs:53` chamam `publisher.Publish(...)` **após**
  `SaveChangesAsync`. A spec (`Sessao.rules.md:364, 298`) e o CLAUDE.md (§10) exigem **Outbox**
  (consistência transacional) para Integration Events.
- **Caso-limite:** processo morre entre o commit e o publish → evento de transparência (LAI)
  **perdido**, sem reentrega. Não afeta a demo de apuração, mas viola requisito constitucional de
  resiliência e é exatamente o tipo de "borda" que uma auditoria de engenharia (gatilho M5/M6)
  reprova.
- **Teste a adicionar:** teste de integração confirmando que abrir/encerrar grava a mensagem na
  tabela Outbox do contexto Legislativo na MESMA transação.

---

## C. LACUNAS DE TESTE (código provavelmente correto, mas sem rede de proteção nos edges pedidos)

Nenhum dos abaixo tem teste hoje em `SessaoFluxoTests.cs`/`TribunaTests.cs`:

- **L-1 (quórum no mínimo exato, par):** existe `B-3` para 11 membros (quórum 6) abrindo com 6, mas
  **não** o caso par `TotalMembros=10 → quórum 6`, abrir com exatamente 6 presentes. A fórmula
  `(10/2)+1 = 6` está certa (`Sessao.cs:69`), mas o edge par+exato não é exercitado ponta a ponta.
- **L-2 (quórum mínimo − 1):** `Abrir` com `presentes == quórum-1` em config par não testado
  (só implícito no 11→5). Adicionar para 10→5 falha.
- **L-3 (`TotalMembros == 1`):** `(1/2)+1 = 1`. Sessão de 1 membro instala com 1 presença. Caso
  degenerado válido pela fórmula — confirmar que é intencional (não há piso de membros).
- **L-4 (reabrir → encerrar):** `Suspensa→Reabrir→Aberta→Encerrar` cobre transições, mas a sequência
  encadeada (suspende, reabre, suspende de novo, encerra de suspensa) não é testada como ciclo.
- **L-5 (pausar duas vezes / retomar sem pausa):** `InscricaoOrador.cs:127-129` (já pausada lança) e
  `:140-143` (retomar sem pausa lança) **não têm teste**. São guardas de exclusão de estado do
  cronômetro diretamente no escopo pedido.
- **L-6 (encerrar fala com pausa ABERTA):** `EncerrarFala` fecha a pausa pendente (`InscricaoOrador.cs:159-163`).
  Comportamento correto e importante (orador pausado quando a mesa encerra), mas **sem teste**.
  Verificar que `TotalPausas` inclui o intervalo final e `TempoUtilizado` desconta.
- **L-7 (excedente exatamente no limite):** fala == tempo concedido (ex.: 5min concedido, 5min usado)
  → `Excedente = 0` (`InscricaoOrador.cs:89-92`, usa `>`). Borda "no limite" pedida explicitamente
  no escopo, sem teste. Idem 1 tick acima → excedente = 1 tick.
- **L-8 (tempo concedido inválido/zero na inscrição):** `Inscrever` com `tempoConcedido <= 0` cai no
  fallback `TempoPadraoOrador` (`TribunaSessao.cs:105`). Sem teste — confirmar que 0/negativo não
  zera o tempo concedido (senão todo orador "estoura" na hora).
- **L-9 (iniciar/encerrar inscrição inexistente):** `Localizar` lança "não encontrada"
  (`TribunaSessao.cs:163-165`). Sem teste para id inválido — borda de robustez de demo.
- **L-10 (uma tribuna por sessão / índice único):** `AbrirTribunaHandler` é idempotente por sessão
  (`AbrirTribuna.cs:41-45`) e há índice único `(TenantId, SessaoId)` (`TribunaSessaoConfiguration.cs:33`),
  mas não há teste cobrindo a segunda abertura retornando a mesma tribuna nem a violação de índice.

---

## D. NOTAS DE ROBUSTEZ (não-bug, mas relevante para a demo)

- **Cronômetro é apurado, não "vivo":** `TempoUtilizado`/`Excedente` só existem **após** `EncerrarFala`
  (retornam `null` durante a fala — `InscricaoOrador.cs:83-86`). O painel ao vivo depende de
  `IniciadoEm` + `TempoConcedido` + pausas calculados no front (`ObterTribunaDaSessao` expõe
  `IniciadoEm`, `Pausada`, mas **não** o tempo decorrido nem o total de pausas em curso). Numa demo
  com cronômetro visível, o front precisa subtrair as pausas concluídas — e `TribunaDto`/`InscricaoDto`
  **não expõem `TotalPausas` nem `PausaIniciadaEm`**. Risco: cronômetro do painel diverge do apurado
  no encerramento se houver pausa. Recomendo expor `TotalPausasSegundos` e `PausaIniciadaEm` no DTO.
- **Sem vínculo Tribuna↔estado da Sessão pós-abertura:** a tribuna só checa `sessao.Terminal` na
  abertura (`TribunaSessao.Abrir`). Depois disso, é possível `IniciarFala` numa sessão que foi
  `Suspensa` ou `Encerrada` (a `TribunaSessao` não reconsulta a `Sessao`). Falar com a sessão
  encerrada é incoerente. Considerar guarda ou ao menos teste documentando que é responsabilidade
  do orquestrador.

---

## OS 3 PIORES (prioridade para a demo)

1. **BUG-1 — tempo de tribuna negativo por timestamp não-monotônico** (`InscricaoOrador.cs:82-92`,
   `TribunaSessao.cs:146`). Apuração de tempo corrompida e visível no painel; é a falha de "apuração
   numa demo" que o escopo classifica como fatal. Guarda de monotonicidade + clamp + testes.

2. **BUG-3 — pausar não libera a tribuna, travando troca de orador** (`TribunaSessao.cs:57-58, 119-123`).
   "Troca de orador" e "pausar/retomar" foram pedidos explicitamente; hoje são mutuamente
   incompatíveis. Decidir o comportamento regimental e travá-lo com teste.

3. **BUG-2 — re-inscrição devolve inscrição concluída e impede nova fala** (`TribunaSessao.cs:97-103`).
   Trava a tribuna no meio da sessão para um orador que já falou. Restringir idempotência a
   `Situacao == Inscrito`.

> Menção honrosa de engenharia (gatilho M5/M6): **DIV-3** (Integration Event fora do Outbox) é o
> achado de persistência/resiliência que uma auditoria isenta vai apontar primeiro, mesmo não
> afetando a apuração na tela.
