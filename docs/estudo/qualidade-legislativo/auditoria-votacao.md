# Auditoria adversarial — Votação / Apuração (módulo Legislativo)

> Escopo: corretude da apuração nas **fronteiras exatas** (quórum, empate, voto de minerva,
> abstenções, idempotência, votar após encerramento, votação secreta, recontagem).
> Postura: auditor adversarial — assumir bug sutil de borda até prova em contrário.
> Data: 2026-06-22. Sem execução de `dotnet`/5080 (análise estática + verificação matemática das fórmulas).

## Arquivos analisados

- `src/Modules/Legislativo/.../Domain/Votacoes/Votacao.cs` (apuração — `Apurar`, `Encerrar`, `RegistrarVoto`, `Cancelar`)
- `src/Modules/Legislativo/.../Domain/Votacoes/Voto.cs`, `Enums.cs`
- `src/Modules/Legislativo/.../Application/Votacoes/*` (Iniciar, RegistrarVoto, Encerrar, Cancelar, ObterPainel, ObterPlacar, ListarVotosNominais, ObterVotacaoPorId)
- `src/Modules/Legislativo/.../Infrastructure/Persistence/Configurations/VotacaoConfiguration.cs`
- `src/Modules/Legislativo/rules/Votacao.rules.md` (spec normativa 1.0.0)
- `tests/.../Legislativo.Tests/VotacaoFluxoTests.cs`, `ValidadoresEnumIntTests.cs`

## Veredito sobre as fórmulas de maioria (o que a demo NÃO pode errar)

Verifiquei por varredura exaustiva (todos `n`/`presentes` de 1..15 e todo `votosSim` possível)
que as três fórmulas implementadas em `Votacao.cs:226-238` são **matematicamente exatas nas bordas**:

- **Simples** (`votosSim > Presentes / 2`, divisão inteira): equivalente a `sim > Presentes/2` real
  para todo presentes par **e ímpar**. Empate em presentes par (`sim == P/2`) → `Rejeitado`. **Correto** (B-2).
- **Absoluta** (`votosSim >= (TotalMembros / 2) + 1`): equivalente a `sim > n/2` real para todo `n` par e ímpar. **Correto** (B-3).
- **Qualificada** (`votosSim >= ceil(2*TotalMembros/3)`): `Math.Ceiling` em `double` — exata para 11 (=8), 12 (=8), 9 (=6) etc. **Correto** (B-4).

**Ou seja: dado um conjunto de votos íntegro, o resultado Aprovado/Rejeitado sai certo.**
O risco da demo NÃO está na fórmula — está em **o conjunto de votos poder ser corrompido** antes da apuração
(votos a mais que presentes, vereador votando 2x em secreta) e em **não haver porteiro de quórum**. Detalhes abaixo.

---

## A. BUGS REAIS (lógica/domínio — corrigir antes da demo)

### BUG-1 — Secreta/Simbólica permitem o MESMO vereador votar várias vezes → apuração inflada
- **Arquivo:** `Votacao.cs:176` (`RegistrarVoto`)
- **Caso-limite:** a guarda "um voto por vereador" só roda quando `Tipo == TipoVotacao.Nominal`.
  Em `Secreta` e `Simbolica`, o mesmo `VereadorId` pode enviar N votos `Sim` (cada um com `votoId` novo).
  Como `VotosSim` é `_votos.Count(... Sim)`, um único vereador consegue empurrar `votosSim` acima do limite e **aprovar sozinho** uma matéria.
- **Certo/errado:** **ERRADO.** Em votação secreta de matéria sensível (ex.: cassação, veto), isso fabrica resultado. Fatal em demo.
- **Nuance:** o sigilo da secreta não exige permitir repetição — basta não *expor* a identidade. A unicidade por vereador deve valer também na secreta (a identidade existe internamente para controle, só não é projetada — I-12).
- **Teste a adicionar:** `Secreta_mesmo_vereador_nao_vota_duas_vezes` — `NovaVotacao(Secreta)`, registrar 2 votos do mesmo `VereadorId` com `votoId` distintos ⇒ esperar `InvalidOperationException` (ou no-op), e `VotosSim == 1`.

### BUG-2 — Nenhum teto: `VotosSim`/total de votos podem exceder `Presentes` e `TotalMembros`
- **Arquivo:** `Votacao.cs:157-183` (`RegistrarVoto`) + `Votacao.cs:223-241` (`Apurar`)
- **Caso-limite:** com `Presentes = 9`, é possível registrar 10+ votos `Sim` (vereadores distintos com `VereadorId.New()`, ou em simbólica/secreta o mesmo). `Apurar` não compara o total de votos contra `Presentes`/`TotalMembros`. Resultado: aprova matéria com mais votos do que gente na sala.
- **Certo/errado:** **ERRADO** (invariante de sanidade ausente). O painel real impede fisicamente, mas o domínio é a fonte de verdade jurídica e tem que rejeitar entrada impossível.
- **Teste a adicionar:** `Nao_aceita_mais_votos_que_presentes` — `presentes:9`, registrar 10 votos ⇒ o 10º lança; e `Total_de_votos_nao_excede_total_membros`.

### BUG-3 — `Encerrar` apura SEM verificar quórum mínimo
- **Arquivo:** `Votacao.cs:192-204` (`Encerrar`) / `Apurar`
- **Caso-limite:** matéria que exige quórum de deliberação (maioria absoluta dos membros presentes para abrir a votação — CF art. 47/simetria, e o próprio painel calcula `QuorumMinimo = TotalMembros/2 + 1` em `ObterPainelDaVotacao.cs:94`). `Encerrar` não checa se `Presentes >= QuorumMinimo` nem se compareceram votos suficientes. Uma votação com 3 presentes em câmara de 11 (quórum 6) encerra e **aprova/rejeita** validamente.
- **Certo/errado:** **ERRADO / lacuna de regra.** O painel exibe `QuorumAtingido`, mas a apuração ignora. Inconsistência entre o que a tela mostra e o que o domínio decide.
- **Nuance:** a regra de quórum de *abertura* (presentes) vs. quórum de *deliberação* (votos válidos) deve ser explicitada na spec; hoje não existe nenhuma das duas no encerramento.
- **Teste a adicionar:** `Encerrar_sem_quorum_minimo_de_presentes_falha_ou_marca_prejudicada` — `totalMembros:11, presentes:3` ⇒ comportamento definido (exceção ou resultado `Prejudicado`), não silenciosamente `Rejeitado`.

### BUG-4 — Idempotência (I-3) e unicidade por vereador (I-4) só existem em memória; sem índice único no banco
- **Arquivos:** `VotacaoConfiguration.cs:48-59` (mapeamento de `Voto`) vs. spec §9 (`Votacao.rules.md:329-334`)
- **Caso-limite:** a spec **exige** índice único em `Voto(VotacaoId, VotoId)` e índice único em `Voto(VotacaoId, VereadorId)` para Nominal. O `VotacaoConfiguration` **não cria nenhum** deles (só PK do `Voto` por `Id` e um índice `(TenantId, ProposicaoId)` na raiz; falta até o `(TenantId, SessaoId)` prometido). Sob duas requisições concorrentes do painel carregando instâncias separadas do agregado, a checagem in-memory de I-3/I-4 passa em ambas e grava voto duplicado / vereador 2x. Sem rede de segurança no banco.
- **Certo/errado:** **ERRADO** (divergência código × spec normativa; risco de corrida).
- **Teste a adicionar:** teste de persistência que insere dois `Voto` com mesmo `(VotacaoId, VereadorId)` em Nominal direto via `DbContext` ⇒ esperar violação de índice único (`DbUpdateException`). Hoje passaria silenciosamente.

### BUG-5 — `Iniciar` não valida `Presentes <= TotalMembros`
- **Arquivos:** `Votacao.cs:107-144` e `IniciarVotacao.cs:44-45` (validator)
- **Caso-limite:** aceita `presentes: 20, totalMembros: 11`. Os dois só checam `> 0`. Presentes maior que o total de cadeiras é estado impossível e contamina as bases das maiorias simples e absoluta.
- **Certo/errado:** **ERRADO** (invariante de consistência ausente, I-1 incompleta).
- **Teste a adicionar:** `Iniciar_rejeita_presentes_maior_que_total_membros` ⇒ `ArgumentException` no factory e falha no validator.

---

## B. LACUNAS DE TESTE (código possivelmente correto, mas a borda NÃO é coberta — a demo está exposta)

- **L-1 — Votar após encerramento já tem teste (I-2), mas falta "votar após CANCELAMENTO + reabertura":** não existe reabertura de votação (bom), porém não há teste afirmando que `Cancelar` é realmente terminal contra `Encerrar` (só o inverso). `Transicao_cancelar_ja_encerrada_falha` cobre encerrada→cancelar; falta cancelada→encerrar. Add: `Encerrar_votacao_cancelada_falha`.
- **L-2 — Maioria simples com `Presentes` ÍMPAR no fio da navalha:** os testes usam só presentes par (10) e o caso 9/5. Falta `presentes:7, sim:4 ⇒ Aprovado` e `presentes:7, sim:3 ⇒ Rejeitado` (verifiquei: corretos, mas não testados). Idem absoluta com `TotalMembros` PAR (ex.: 10 → limiar 6): nenhum teste com membros par. Add: `Absoluta_membros_par_limite` (`membros:10, sim:6 ⇒ Aprovado; sim:5 ⇒ Rejeitado`).
- **L-3 — Qualificada onde `2n/3` é inteiro exato:** testes cobrem `n=11` (ceil de 7.33=8). Falta `n=9` (2*9/3=6 exato → limiar 6) e `n=12` (=8). Risco de off-by-one no `Math.Ceiling` de valor inteiro. Add: `Qualificada_quando_dois_tercos_e_inteiro` (`membros:9, sim:6 ⇒ Aprovado; sim:5 ⇒ Rejeitado`).
- **L-4 — Abstenção que muda o desfecho na simples (não só rejeitar tudo):** `Invariante_13` testa 9 abstenções (rejeita). Falta o caso decisivo: `presentes:9, sim:4, nao:0, abstencao:5` — 4 não supera 9/2; deve `Rejeitado` mesmo com 4 sendo a maioria dos *votos não-abstenção*. Confirma que a base é `Presentes`, não votos válidos. Add: `Abstencao_nao_reduz_base_da_maioria_simples`.
- **L-5 — Sigilo da secreta NÃO é testado em nenhum lugar:** `ListarVotosNominais` lança para `Secreta` e `ObterPainel`/placar omitem nomes (I-12), mas **não há um único teste** exercitando `TipoVotacao.Secreta`. Add: `Secreta_lista_nominal_bloqueada` e `Secreta_painel_omite_lista_mas_mantem_placar`.
- **L-6 — Recontagem / determinismo:** não há teste de que reapurar o mesmo conjunto dá o mesmo resultado (apuração é função pura de `votos+bases`), nem de que `Resultado` é imutável após encerrar (I-9 só testa que vira não-nulo). Add: `Encerrar_e_idempotente_no_resultado` (segundo `Encerrar` lança — já existe; falta afirmar `Resultado` inalterado) e `Apuracao_e_deterministica`.
- **L-7 — Voto de minerva / desempate do Presidente:** o enum `PapelVereador` documenta "voto de qualidade/desempate" (`Vereadores/Enums.cs:25`), mas a apuração trata todo voto igual e **não há conceito de desempate**. Em empate na simples/absoluta a matéria é `Rejeitado` — o que é uma escolha legítima de regra, mas é **regra implícita não testada e não documentada na spec de Votação**. Add à spec: explicitar "empate ⇒ Rejeitado, sem voto de minerva nesta versão" + teste `Empate_simples_rejeita_sem_minerva`. (Modelar minerva de fato é trabalho de domínio, não bug.)
- **L-8 — `ObterPainel.ApurarParcial` duplica a regra de `Votacao.Apurar`:** `ObterPainelDaVotacao.cs:123-134` reescreve a fórmula. Se um dia `Apurar` mudar e o painel não, a tela mostra desfecho diferente do oficial. Sem teste cruzado. Add: `ParcialDoPainel_coincide_com_resultado_oficial_ao_encerrar` (property test: para vários conjuntos, `ResultadoParcial == Encerrar()`). Recomendação de design: extrair a regra para um único método de domínio e o painel chamá-lo.

---

## C. Os 3 piores (prioridade para a demo da comissão)

1. **BUG-1 — Secreta/Simbólica deixam o mesmo vereador votar N vezes** (`Votacao.cs:176`).
   Fabrica aprovação. Numa votação secreta de veto/cassação a apuração é falsa e indefensável perante o TCE. **Corrigir já:** mover a unicidade por `VereadorId` para fora do `if Nominal` (vale para toda modalidade; em secreta só a *projeção* omite a identidade).

2. **BUG-3 — Encerramento apura sem porteiro de quórum** (`Votacao.cs:192`).
   O painel mostra `QuorumAtingido=false` e mesmo assim a votação "aprova". Contradição visível na própria demo entre a tela e o resultado oficial — o avaliador vê na hora. Definir regra de quórum no `Encerrar`.

3. **BUG-2 — Sem teto de votos vs. presentes/membros** (`Votacao.cs:157`/`Apurar`).
   "13 votos Sim numa câmara de 11" aprova. Quebra a sanidade mais básica da apuração e é trivial de um avaliador adversarial provocar. Validar `total de votos <= Presentes` e `<= TotalMembros` no registro.

> Bônus de baixo custo e alto retorno antes da demo: **BUG-4** (criar os índices únicos que a própria spec §9 já manda) e **BUG-5** (`Presentes <= TotalMembros`) fecham os flancos de concorrência e de estado impossível com poucas linhas.

## Resumo

- **Fórmulas de maioria (simples/absoluta/qualificada):** corretas nas fronteiras exatas — verificado por varredura. Não são o problema.
- **5 bugs reais de domínio**, todos na *integridade do conjunto de votos* e nos *porteiros de estado*, não na aritmética: BUG-1 (unicidade só em Nominal), BUG-2 (sem teto), BUG-3 (sem quórum no encerramento), BUG-4 (índices únicos da spec não implementados), BUG-5 (presentes>membros aceito).
- **8 lacunas de teste**, com destaque para **votação secreta sem nenhum teste** (L-5) e a **duplicação da regra de apuração no painel** (L-8).
