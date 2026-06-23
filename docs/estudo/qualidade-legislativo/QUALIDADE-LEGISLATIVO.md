# QUALIDADE-LEGISLATIVO — Consolidado de auditoria (módulo Legislativo)

> Consolidação adversarial das três auditorias parciais
> ([votação](auditoria-votacao.md), [proposição/norma/diário](auditoria-proposicao-norma.md),
> [sessão/tribuna](auditoria-sessao-tribuna.md)), com **reverificação dos achados críticos
> contra o código vivo** (arquivo:linha confirmados em 2026-06-22).
>
> **Foco:** o que QUEBRARIA na demo de recebimento da comissão. Numa PoC de Câmara, um erro de
> **apuração** (votação, quórum, tempo de tribuna) ou de **vínculo** (aprovar a matéria errada) é fatal.
> **Não rodar `dotnet`/5080** — análise estática + verificação matemática + leitura do código nas linhas citadas.

---

## Veredito de uma linha

A **aritmética de apuração está correta** nas três maiorias (simples/absoluta/qualificada),
verificada por varredura — **não é o risco**. O risco real está em **(1) integridade do conjunto
de votos**, **(2) vínculo votação↔proposição na orquestração**, **(3) apuração de tempo da tribuna**
e **(4) busca de normas que depende de collation**. São esses os flancos que um avaliador
adversarial provoca em minutos.

**Contagem:** **8 bugs reais** (corrigir) + **22 testes de borda a adicionar** (sem cobertura hoje).
Base atual: 126 testes no projeto `Legislativo.Tests` (11 arquivos), **zero** exercitando votação
secreta, tempo de tribuna negativo, busca de norma com acento, ou vínculo votação↔proposição.

---

## A. BUGS REAIS — corrigir antes da demo (8)

Ordenados por severidade. Todos **reverificados no código** nas linhas indicadas.

### CRÍTICO (quebra a demo na hora)

#### BUG-1 — `AprovarProposicao` não confere se a votação é da proposição
- **Arquivo:** `Application/Proposicoes/AprovarProposicao.cs:39-53` — **CONFIRMADO**.
- O handler carrega proposição e votação por IDs independentes e **nunca compara
  `votacao.ProposicaoId == proposicao.Id`** (o campo existe em `Votacao.cs:57`, é ignorado).
- **Cenário fatal:** 2+ matérias na ordem do dia; clicar no card errado aprova a matéria X com o
  resultado da votação de Y, **sem erro**. É a classe exata de "apuração errada na demo".
- **Agrava:** linha 50 mapeia a maioria **EXIGIDA** como maioria **ATINGIDA** (`MapearMaioria(votacao.MaioriaExigida)`),
  ignorando o placar real. Vínculo por confiança, não por verificação.
- **Correção:** 1 linha — `if (votacao.ProposicaoId != proposicao.Id) throw new InvalidOperationException(...)`.

#### BUG-2 — Secreta/Simbólica permitem o MESMO vereador votar N vezes → apuração inflada
- **Arquivo:** `Domain/Votacoes/Votacao.cs:176` — **CONFIRMADO**.
- A guarda de unicidade por vereador só roda quando `Tipo == TipoVotacao.Nominal`. Em `Secreta` e
  `Simbolica`, o mesmo `VereadorId` empurra `VotosSim` (que é `_votos.Count(... Sim)`) acima do
  limite e **aprova sozinho** uma matéria.
- **Cenário fatal:** votação secreta de veto/cassação fabricada. Indefensável perante o TCE.
- **Nuance:** o sigilo não exige permitir repetição — a identidade existe internamente para controle,
  só não é projetada. A unicidade deve valer em toda modalidade.
- **Correção:** mover a checagem `_votos.Any(v => v.VereadorId == vereadorId)` para fora do `if Nominal`.

#### BUG-3 — Busca de normas: case/acento dependem da collation, não há normalização no código
- **Arquivos:** `Infrastructure/.../LegislativoRepositories.cs:168-173` + `LegislativoDbContext.cs:79`
  + `NormaConfiguration.cs:38-39` — **CONFIRMADO**.
- A coluna-sombra `EmentaBusca` é sincronizada com o **valor cru** da ementa (`entrada.Property("EmentaBusca").CurrentValue = entrada.Entity.Ementa.Valor`),
  **sem lowercase nem remoção de diacríticos**. A busca é `EF.Functions.Like` sobre essa coluna crua →
  case/acento ficam por conta da collation (não há `.UseCollation`).
- **Cenário fatal:** termo "acacias" NÃO casa "Acácias"; "sao joao" NÃO casa "São João". O teste verde
  (`NormaTests.cs`) mascara o bug usando ementa sem acento. É a **funcionalidade-vitrine da consulta LAI**.
- **Correção:** normalizar `EmentaBusca` (lower + sem diacríticos) na sincronização do `SaveChanges`
  e normalizar o termo de busca igual antes do `Like`.

### ALTO

#### BUG-4 — Cronômetro de tribuna: tempo NEGATIVO por timestamp não-monotônico
- **Arquivos:** `Domain/Tribuna/InscricaoOrador.cs:82-92` + `TribunaSessao.cs:146-156` — **CONFIRMADO**.
- `TempoUtilizado = (fim - inicio) - TotalPausas` **não tem clamp** (linha 85); `EncerrarFala`/`RetomarFala`
  não validam `momento >= IniciadoEm`. Com clock skew/NTP step (load balancer, horário de verão), o tempo
  vira negativo e o painel mostra "−00:42 utilizado". `Excedente` ainda esconde o erro (vira 0).
- **Incoerência interna:** `PausaFala.De` já rejeita `fim < inicio` num ponto do agregado, mas
  `IniciarFala→EncerrarFala` não.
- **Correção:** guarda de monotonicidade nas transições de tempo + `clamp` em `TimeSpan.Zero` no cálculo.

#### BUG-5 — `IncluirEmOrdemDoDia` aceita parecer CONTRÁRIO como parecer obrigatório
- **Arquivos:** `Domain/Proposicoes/Proposicao.cs:237-245` + `:353-356` (`PossuiParecer`) — **CONFIRMADO**.
- `PossuiParecer` só verifica `Fase == Parecer && Comissao == X` — **ignora `ParecerFavoravel`**.
  Um parecer explicitamente CONTRÁRIO da CCJ (inconstitucionalidade) satisfaz o requisito de "parecer obrigatório".
- **Cenário:** CCJ emite contrário por vício de constitucionalidade; a matéria entra em Ordem do Dia
  normalmente — silenciando informação jurídica decisiva.
- **Correção:** decidir a regra (bloquear ou exigir flag de superação) e fazer `PossuiParecer`
  considerar `ParecerFavoravel`.

#### BUG-6 — Re-inscrição idempotente devolve inscrição já CONCLUÍDA, travando segunda fala
- **Arquivo:** `Domain/Tribuna/TribunaSessao.cs:97-103` (`Inscrever`) — confirmado por auditoria.
- O filtro de idempotência é `i.Situacao != Cancelado`, então casa com inscrição **Concluído** e a
  devolve. `IniciarFala` sobre ela lança ("exige Inscrito") → o orador **não consegue falar de novo na fase**
  e a UI recebe um id morto. Trava a tribuna em sessão real.
- **Correção:** restringir a idempotência a `Situacao == Inscrito` (no máximo `Inscrito`/`EmUso`).

### MÉDIO (porteiros de sanidade ausentes — flancos triviais de provocar)

#### BUG-7 — Apuração sem porteiros de quórum e sem teto de votos
- **Arquivos:** `Domain/Votacoes/Votacao.cs:157-183` (`RegistrarVoto`), `:192-204` (`Encerrar`),
  `:223-241` (`Apurar`) — **CONFIRMADO** que nenhuma das checagens existe.
- **(a) sem teto:** `Apurar` não compara o total de votos contra `Presentes`/`TotalMembros`.
  "13 votos Sim numa câmara de 11" aprova. (Consolida o antigo BUG-2 da auditoria de votação.)
- **(b) sem quórum no encerramento:** `Encerrar` apura mesmo com `Presentes < QuorumMinimo`
  (o painel calcula `QuorumMinimo = TotalMembros/2 + 1` e exibe `QuorumAtingido=false`, mas a apuração
  ignora → tela contradiz o resultado oficial, visível na demo). (Antigo BUG-3.)
- **Correção:** no `RegistrarVoto`, rejeitar quando `_votos.Count >= Presentes` (e `<= TotalMembros`);
  no `Encerrar`, decidir a regra de quórum de deliberação (exceção ou resultado `Prejudicado`).

#### BUG-8 — `Iniciar` não valida `Presentes <= TotalMembros`; índices únicos da spec ausentes
- **Arquivos:** `Domain/Votacoes/Votacao.cs:107-144` + `IniciarVotacao.cs:44-45`; e
  `Infrastructure/.../VotacaoConfiguration.cs:48-59` vs. spec §9 (`Votacao.rules.md:329-334`).
- **(a)** factory e validator só checam `> 0`: aceitam `presentes:20, totalMembros:11` (estado impossível
  que contamina as bases das maiorias simples/absoluta).
- **(b)** a spec exige índice único em `Voto(VotacaoId, VotoId)` e `Voto(VotacaoId, VereadorId)` para
  Nominal; `VotacaoConfiguration` **não cria nenhum** — a idempotência (I-3) e a unicidade (I-4) só
  existem em memória, sem rede de segurança contra concorrência do painel.
- **Correção:** validar `Presentes <= TotalMembros`; criar os dois índices únicos da spec.

> **Também rastrear (não-fatal para a apuração na tela, mas reprova auditoria de engenharia no M5/M6):**
> Integration Events publicados via `IPublisher` direto, **fora do Outbox** — `AbrirSessao.cs:53`,
> `EncerrarSessao.cs:53`, `PublicarEdicaoDiario.cs:41-59` — viola CLAUDE.md §10 (perda silenciosa de
> evento LAI sob falha entre commit e publish). Tratar no endurecimento de resiliência, não na vitrine.

---

## B. EDGE-CASES SEM TESTE — adicionar (22)

Código possivelmente correto, mas a borda **não tem rede de proteção** — a demo está exposta a regressão.

### Votação / apuração (8)
1. `Secreta_mesmo_vereador_nao_vota_duas_vezes` — trava BUG-2 (esperar exceção/no-op; `VotosSim==1`).
2. `Nao_aceita_mais_votos_que_presentes` / `Total_de_votos_nao_excede_total_membros` — trava BUG-7(a).
3. `Encerrar_sem_quorum_minimo_falha_ou_marca_prejudicada` (`totalMembros:11, presentes:3`) — trava BUG-7(b).
4. `Iniciar_rejeita_presentes_maior_que_total_membros` — trava BUG-8(a).
5. `Maioria_simples_presentes_impar_no_limite` (`presentes:7 → sim:4 Aprovado; sim:3 Rejeitado`).
6. `Absoluta_membros_par_limite` (`membros:10 → sim:6 Aprovado; sim:5 Rejeitado`) e
   `Qualificada_quando_dois_tercos_e_inteiro` (`membros:9 → sim:6 Aprovado; sim:5 Rejeitado`).
7. `Abstencao_nao_reduz_base_da_maioria_simples` (`presentes:9, sim:4, abstencao:5 → Rejeitado`).
8. `Secreta_lista_nominal_bloqueada` + `Secreta_painel_omite_lista_mas_mantem_placar` (sigilo nunca testado).

### Proposição / norma / diário (7)
9. `Aprovar_com_votacao_de_outra_proposicao_falha` — trava BUG-1 (o teste mais importante da lista).
10. `Aprovar_PLC_com_votacao_aberta_como_simples_*` — fixa o mapeamento maioria exigida×atingida.
11. `IncluirEmOrdemDoDia_com_parecer_ccj_contrario_*` — trava BUG-5.
12. `Buscar_por_termo_com_acento_diferente_casa` (termo "acacias" vs ementa "Acácias") — **FALHA hoje**, expõe BUG-3.
13. `RegistrarAlteracao_com_data_anterior_a_promulgacao_lanca` (`Norma.cs:197-206` não valida data; espelhar `Revogar`).
14. `Revogar_com_norma_revogadora_igual_a_si_mesma_lanca` (auto-referência na trilha jurídica).
15. `RegistrarSancao_duas_vezes_*` / `RegistrarVeto_apos_sancao_*` (`Proposicao.cs:327-351` aceita "Sancao, Veto, Sancao").

### Sessão / tribuna (7)
16. `EncerrarFala_com_momento_anterior_ao_inicio_rejeita` + `TempoUtilizado_nunca_negativo` — trava BUG-4.
17. `Reinscrever_apos_concluir_cria_nova_inscricao_na_mesma_fase` — trava BUG-6.
18. `Pausar_orador_e_iniciar_outro_<comportamento_decidido>` — troca de orador com pausa
    (`OradorEmUso` bloqueia pausado, `TribunaSessao.cs:57-58,119-123`); decidir regra e travar.
19. `Quorum_minimo_exato_membros_par` (`TotalMembros:10 → quórum 6`, abrir com 6) +
    `Abrir_com_quorum_menos_um_falha` (`10 → 5`).
20. `Excedente_exatamente_no_limite_e_zero` + `Excedente_um_tick_acima` (`InscricaoOrador.cs:89-92`).
21. `Pausar_duas_vezes_lanca` + `Retomar_sem_pausa_lanca` (`InscricaoOrador.cs:127-129,140-143`, sem teste).
22. `VerificarQuorum_em_sessao_terminal_nao_emite_evento` (`Sessao.cs:138-143` emite `QuorumVerificado`
    mesmo terminal — evento espúrio).

> Bônus de baixo custo: teste cruzado `ParcialDoPainel_coincide_com_resultado_oficial_ao_encerrar`
> — `ObterPainelDaVotacao.cs:123-134` **duplica** a fórmula de `Votacao.Apurar`; se uma mudar e a outra
> não, a tela mostra desfecho diferente do oficial. Recomenda-se extrair a regra para um único método de domínio.

---

## C. OS MAIS CRÍTICOS (prioridade absoluta para a demo)

1. **BUG-1** (`AprovarProposicao.cs:39-53`) — aprova a matéria errada com o ID de outra votação.
   Correção de 1 linha. **O maior risco isolado da demo.**
2. **BUG-2** (`Votacao.cs:176`) — mesmo vereador vota N vezes em secreta/simbólica → aprovação fabricada.
3. **BUG-3** (`LegislativoDbContext.cs:79` + `LegislativoRepositories.cs:173`) — busca LAI não encontra
   "Acácias"/"São João"; teste verde mascara o bug.
4. **BUG-4** (`InscricaoOrador.cs:82-92`) — tempo de tribuna negativo no painel por clock skew.
5. **BUG-7** (`Votacao.cs:157`/`192`/`Apurar`) — "13 votos numa câmara de 11" aprova; encerra sem quórum
   contradizendo o próprio painel.

Os três testes que **mais** protegem a demo: nº 9 (`Aprovar_com_votacao_de_outra_proposicao_falha`),
nº 1 (`Secreta_mesmo_vereador_nao_vota_duas_vezes`), nº 12 (`Buscar_por_termo_com_acento_diferente_casa`).

---

## Caminho

`/Users/igorgewehr/Development/Tensorroot.Gov/docs/estudo/qualidade-legislativo/QUALIDADE-LEGISLATIVO.md`
