# Vereador, Painel ao Vivo, Ata e Seed — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). Demo-enablers do sprint legislativo: cadastro de
> parlamentares, painel eletronico ao vivo, ata automatica e dados semeados para demonstracao.
> Autoridade: `docs/architecture/legislativo-sprint/DESIGN-GAPS.md` e `AUDIT-NUCLEO.md`.
> Constituicao: isolamento de modulo, multi-tenant (`IMustHaveTenant` + Global Query Filter),
> auditoria imutavel, dominio rico, banco-por-tenant.

---

## 1. Linguagem ubiqua

- **Vereador (Parlamentar):** titular do mandato. Reusa o `VereadorId` ja referenciado por
  `Presenca`, `Voto` e tribuna — da **nome, partido e situacao de mandato** ao identificador antes cru.
- **Mesa Diretora:** Presidente, Vice-Presidente, 1.º e 2.º Secretarios (`CargoMesa`).
- **Legislatura:** periodo do mandato (4 anos): `LegislaturaInicio`/`LegislaturaFim`.
- **Situacao de mandato:** `EmExercicio`, `Licenciado`, `Afastado`, `Encerrado` (terminal).
- **Painel ao vivo:** estado que um painel eletronico consome — placar agregado (sim/nao/abstencao),
  totais, quorum, resultado apurado e a **lista nominal de votos COM o nome do vereador**.
- **Ata da sessao:** documento estruturado derivado da sessao (presenca, Ordem do Dia, votacoes e
  resultados), com nomes dos vereadores.

---

## 2. Invariantes do agregado `Vereador`

- **V-1** Cadastro nasce `EmExercicio` e emite `VereadorCadastrado` (factory `Cadastrar`).
- **V-2** `NomeCivil` e `Partido` nao vazios (sigla normalizada em maiusculas); nome <= 200, partido <= 30.
- **V-3** Legislatura plausivel: `LegislaturaInicio` em 1900..2100 e `LegislaturaFim > LegislaturaInicio`.
- **V-4** `AtualizarCadastro` edita nome/partido/cargo (filiacao pode mudar no mandato); bloqueada se terminal.
- **V-5** `AlterarSituacao`: `Encerrado` e **terminal** (nao retorna ao exercicio nem admite edicao).
- **Vinculo:** `Cadastrar` aceita reusar um `VereadorId` existente — liga o cadastro a presencas/votos
  preexistentes **sem quebrar** o modelo de `Sessao`/`Votacao` (referencia interna, mesmo modulo).

---

## 3. Painel ao vivo (consulta)

- **P-1** `ListarVotacoes` lista as votacoes do tenant (abertas primeiro), opcionalmente por sessao —
  remove a navegacao por ID (ponte Sessao → Votacoes).
- **P-2** `ObterPainelDaVotacao` agrega o placar (sim/nao/abstencao + totais), calcula quorum minimo
  (maioria absoluta dos membros), ausentes e resultado apurado/parcial, e resolve o **nome** de cada
  voto (GUID cru apenas para nao cadastrados).
- **P-3** Votacao **Secreta** nunca expoe a lista nominal (I-12) — so o agregado.

---

## 4. Ata automatica (consulta)

- **A-1** `GerarAtaDaSessao` monta a ata a partir do agregado existente: presencas (presentes/ausentes
  com nome), Ordem do Dia (com ementa) e votacoes (placar + resultado + nomes por sentido).
- **A-2** Ata e **derivada/read-only**: nao altera estado nem exige encerramento; reflete a situacao atual.

---

## 5. Seed de demonstracao (comando)

- **S-1** `SemearDemonstracaoLegislativa` e **idempotente por tenant**: se ja existem vereadores, e no-op.
- **S-2** Cria: legislatura + Mesa Diretora + 9 vereadores nomeados + 1 proposicao em tramitacao (com
  pareceres CCJ/Financas, em Ordem do Dia) + 1 sessao aberta com presencas e pauta + 1 votacao nominal
  com votos registrados.

---

## 6. RBAC

- `legislativo.ver` — leitura (lista/detalhe de vereadores, painel, ata).
- `legislativo.vereadores.gerenciar` — cadastrar/editar vereadores.
- `legislativo.demo.semear` — semear o cenario de demonstracao.

---

## 7. Cenarios BDD (resumo)

- **Cadastro nasce em exercicio:** dado nome/partido/legislatura validos, quando cadastrar, entao
  situacao = EmExercicio e evento `VereadorCadastrado` emitido.
- **Mandato encerrado e terminal:** dado vereador `Encerrado`, quando alterar situacao/cadastro, entao falha.
- **Painel mostra nomes:** dada votacao com votos de vereadores cadastrados, quando obter o painel,
  entao cada linha traz o nome parlamentar (nao o GUID) e o placar agregado bate com os votos.
- **Ata reflete a sessao:** dada sessao com presencas, pauta e votacao, quando gerar a ata, entao ela
  lista presentes/ausentes, a Ordem do Dia e o resultado da votacao.
- **Seed idempotente:** dado tenant ja semeado, quando semear de novo, entao nada e duplicado.

<!-- manifest
commands: CadastrarVereador, EditarVereador, SemearDemonstracaoLegislativa
queries: ListarVereadores, ObterVereadorPorId, GerarAtaDaSessao
domainEvents: VereadorCadastrado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
