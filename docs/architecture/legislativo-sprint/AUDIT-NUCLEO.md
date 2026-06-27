# AUDIT-NUCLEO — Núcleo Legislativo (Câmara) para a PoC

> Auditoria do que **JÁ existe e é demonstrável fim-a-fim** no módulo Legislativo, requisito a
> requisito, com arquivos reais. Objetivo: dar ao dono uma base honesta para fechar um sprint
> pequeno e deixar a trilha legislativa "PoC-vencível".
> Data: 2026-06-22. Escopo: `src/Modules/Legislativo` (Domain/Application/Infrastructure/Contracts)
> + `src/Web/src/modules/legislativo`. **Nenhum código escrito** — só design/auditoria.
> Método: leitura dos arquivos-fonte (não de artefatos de `bin/`/`obj/`). Correção importante: a
> AUTOAVALIACAO-POC contou ocorrências em `bin/obj` (ex.: "14 arquivos com Comissao", "26 com Ata",
> "9 com quórum"). Refazendo a contagem **só no fonte**, esses números caem para zero entidades
> reais — ver §3. A autoavaliação de ~70–75% estava **otimista** quanto ao que está construído.

---

## 0. Veredito rápido

- **% demonstrável fim-a-fim (núcleo de processo legislativo): ~62%.**
  O **back-end de domínio é forte e honesto** (Proposição, Sessão, Votação são agregados ricos,
  com máquina de estados, invariantes e trilha append-only). O **fluxo principal fecha de ponta a
  ponta** (apresentar → distribuir → parecer → ordem do dia → sessão/quórum → votação nominal →
  aprovar → autógrafo → evento de integração) e **tem UI React** para quase tudo. O que **derruba a
  nota** é a ausência de quatro peças que uma banca de Câmara olha primeiro: **cadastro de
  parlamentares/vereadores**, **PAINEL ao vivo**, **ATA automática** e **dados de exemplo (seed)**.
- **Pontos fortes reais:** votação nominal com apuração por maioria simples/absoluta/qualificada,
  idempotência de voto (chave do painel), quórum de instalação, pareceres obrigatórios (CCJ +
  Finanças) travando a Ordem do Dia, autógrafo assinável e integração Câmara→Executivo por evento.
- **Pontos fracos que aparecem na 1ª demo:** sem Vereador cadastrado os votos/presenças usam **GUID
  cru** (sem nome no painel); a tela "Votações" é **consulta-por-ID** (sem lista, sem placar que
  atualiza sozinho); **não há ata**; **não há comissão como entidade**; **não há dado semeado**.

---

## 1. Mapa do que existe (arquivos reais)

**Domain** (`...Legislativo.Domain`):
- `Proposicoes/Proposicao.cs` — agregado rico (apresentar, distribuir, emenda, substitutivo,
  parecer, ordem do dia, aprovar, rejeitar, autógrafo, arquivar, sanção, veto).
- `Proposicoes/{Emenda,Substitutivo,Tramitacao,Autoria,Ementa,Enums}.cs`.
- `Sessoes/Sessao.cs` — agregado (agendar, presença, quórum, abrir, suspender, reabrir, encerrar,
  cancelar) + `Presenca.cs`, `ItemOrdemDoDia.cs`, `ValueObjects.cs` (define `VereadorId`, `DataHora`).
- `Votacoes/Votacao.cs` — agregado (iniciar, registrar voto, encerrar com apuração, cancelar) +
  `Voto.cs`, `Enums.cs` (Simbolica/Nominal/Secreta; Simples/Absoluta/Qualificada), `Identificadores.cs`.
- `Events/{LegislativoDomainEvents,VotacaoDomainEvents}.cs`.

**Application** (handlers MediatR + validators): 9 em `Sessoes/`, 7 em `Votacoes/`, 13 em
`Proposicoes/`, 4 em `Integracoes/` (recebe Sanção/Veto do Executivo). Queries de leitura para
detalhe, lista por situação, tramitação, presenças, placar e votos nominais.

**Infrastructure**: `LegislativoEndpoints.cs` (Minimal API `/api/legislativo/...`, todos com
`RequirePermission`), `LegislativoModule.cs`, `LegislativoDbContext.cs` + 3 Configurations +
Migrations (`Inicial`, `OutboxResiliencia`) + `LegislativoRepositories.cs`.

**Contracts**: `SessaoRealizadaIntegrationEvent`, `ResultadoVotacaoIntegrationEvent`,
`AutografoEnviadoIntegrationEvent` (saída para Executivo/Transparência via Outbox).

**Frontend** (`src/Web/src/modules/legislativo`): páginas Proposição (List/Detail/Form/Ações/
Deliberação), Sessão (List/Detail/Form/Ações/Modais), Votação (Consulta/Detail/Form/Ações) +
`*.test.tsx` + `index.tsx` (rotas + nav "Legislativo"). Segue o padrão-ouro de Tributos.

---

## 2. Requisito → estado (ok / parcial / falta) com arquivo

Requisitos recorrentes ("obrigatórios de facto") extraídos de editais reais de pregão de software para Câmaras.

| # | Requisito (fonte verificada) | Estado | Onde está / o que falta |
|---|---|---|---|
| 1 | **Proposições por tipo + protocolo** | **OK** | `Domain/Proposicoes/Proposicao.Apresentar` (PLO/PLC/Emenda à LOM/PDL/PR/Req/Ind/Moção em `Enums.cs`); protocolo gerado; `POST /proposicoes`; `ProposicaoFormModal.tsx`. |
| 2 | **Tramitação (fases, consulta de matérias)** | **OK** | `Tramitacao.cs` append-only; `ObterTramitacaoDaProposicao`, `ListarProposicoesPorSituacao`; `GET /proposicoes`, `/{id}/tramitacao`; `ProposicaoDetailPage.tsx`. |
| 3 | **Emendas / substitutivos** | **OK** | `ApresentarEmenda`/`ApresentarSubstitutivo`; `POST /proposicoes/{id}/emendas`. |
| 4 | **Pareceres de comissão** | **PARCIAL** | `Proposicao.RegistrarParecer(comissao:string,...)` exige CCJ + Finanças antes da Ordem do Dia (invariante forte). Mas **comissão é apenas string**, não há cadastro de comissões nem de membros (efetivos/suplentes) exigido em E2/E3. |
| 5 | **Comissões + membros efetivos/suplentes + atas/pautas de reunião** | **FALTA** | Não existe entidade `Comissao` no fonte. As ocorrências contadas pela autoavaliação são doc-comments e artefatos de build. |
| 6 | **Sessão: agendar/abrir/suspender/encerrar** | **OK** | `Sessao.cs` máquina de estados completa; endpoints `/sessoes/{id}/{abertura,suspensao,reabertura,encerramento,cancelamento}`; `SessaoDetailPage.tsx` + `SessaoAcoes.tsx`. |
| 7 | **Ordem do Dia** | **OK (manual)** | `Sessao.IncluirNaOrdemDoDia` + `Proposicao.IncluirEmOrdemDoDia` (com travas de parecer). **Geração automática** da OD a partir da tramitação (E2 §90-91) **NÃO** existe — é inclusão manual item a item. |
| 8 | **Registro de presença + quórum de instalação** | **OK (dado, ver risco)** | `Sessao.RegistrarPresenca` (idempotente) + `VerificarQuorum` (maioria absoluta dos membros); `POST/GET /sessoes/{id}/presencas`, `/quorum`. **Recomposição de quórum** (cancelar e re-registrar, E2 §511-513) não modelada. |
| 9 | **Votação nominal — abrir/fechar/cancelar + resultado** | **OK** | `Votacao.cs`: `Iniciar`/`RegistrarVoto` (idempotente por VotoId, 1 voto/vereador no nominal)/`Encerrar` (apura Simples/Absoluta/Qualificada)/`Cancelar`; endpoints `/votacoes/...`; `VotacaoDetailPage.tsx`. |
| 10 | **Votação simbólica** | **OK (modelada)** | `TipoVotacao.Simbolica` apura pelo agregado. (Nota da pesquisa: simbólica raramente é informatizada — não é gap.) |
| 11 | **Parlamentar impedido de votar** | **FALTA** | Sem conceito de impedimento/declaração de suspeição na `Votacao`. |
| 12 | **PAINEL eletrônico ao vivo (presentes/ausentes/orador/resultado)** | **PARCIAL/FRACO** | Só o `PlacarVotacao` (Sim/Não/Abstenção/total) num `Card` em `VotacaoDetailPage.tsx`. **Sem tela de painel/telão**, **sem auto-refresh** (nenhum `setInterval`/`refetchInterval`/SignalR), **sem nomes** (mostra `vereadorId` cru). |
| 13 | **Cronômetro de tribuna + inscrição de oradores** | **FALTA** | Zero no fonte. |
| 14 | **ATA (sintética) automática ao fim da sessão** | **FALTA** | `EncerrarSessao` só publica `SessaoRealizadaIntegrationEvent`; **não gera ata** (E2 item 2.1.8 é literal nos editais). |
| 15 | **Cadastro de Vereadores/Parlamentares** | **FALTA (crítico)** | `VereadorId` é só `record struct` (`Sessoes/ValueObjects.cs:45`); **não há entidade Vereador** com nome/partido/mandato. Toda presença/voto referencia GUID arbitrário. |
| 16 | **Autógrafo + remessa ao Executivo (assinado)** | **OK** | `Proposicao.GerarAutografo`; `AutografoEnviadoIntegrationEvent`; integração de Sanção/Veto de volta. |
| 17 | **Base de Normas Jurídicas consultável** | **FALTA** | Nenhuma entidade de norma/lei. |
| 18 | **Diário Oficial eletrônico** | **FALTA** | Inexistente. |
| 19 | **Deliberação remota (voto/tribuna online)** | **FALTA** | Inexistente (rebaixado a diferencial pela pesquisa — não bloqueia PoC típica). |
| 20 | **Multi-tenant + auditoria + permissões** | **OK** | Todos os agregados são `IMustHaveTenant`; endpoints com `RequirePermission("legislativo.{ver,gerenciar}")`; Outbox por contexto (constituição §5/§6). |

**Contagem do núcleo de "processo legislativo + sessão + votação"** (req. 1-16, 20 — exclui Normas/
Diário/Remota que a pesquisa tira do mínimo): **OK ~11, Parcial ~3, Falta ~3 → ≈62% demonstrável.**

---

## 3. Correção à AUTOAVALIACAO-POC (por que ~75% era otimista)

A autoavaliação afirmou "Comissões parcial (14 arquivos)", "Ata parcial (26 arquivos)", "quórum em
9 arquivos", "Painel em 5 arquivos". Recontando **apenas em `*.cs` de fonte** (sem `bin/`/`obj/`):
não há **nenhuma** entidade `Comissao`, **nenhuma** `Ata`, **nenhum** `Painel`/`Cronometro`/`Orador`
e **nenhum** `Vereador` como entidade. Os hits eram doc-comments (a palavra "comissão"/"painel"
aparece em XML docs) e DLLs/PDBs duplicadas em build. O que existe de verdade é forte, mas **menor**
do que o board sugere. Número honesto de demonstrabilidade do núcleo: **~62%**, não 75%.

---

## 4. POLISH para uma demo convincente (sem becos)

Prioridade pelo retorno em banca, do mais barato ao mais caro:

1. **Seed de demonstração (custo baixo, retorno altíssimo).** Não existe seed legislativo (só há em
   Finanças/Transparência). Sem ele, o avaliador começa numa tela vazia. Semear: 9–11 vereadores
   (nome/partido), 1 sessão ordinária agendada com presenças, 2–3 proposições em estágios diferentes
   (uma já com pareceres CCJ+Finanças pronta para Ordem do Dia), 1 votação encerrada e 1 aberta.
   Padrão a seguir: `Financas/.../Contabilidade/Seed/ContaSeed.cs`.
2. **Entidade Vereador (cadastro mínimo).** Hoje presença/voto são GUID. Um cadastro
   nome/partido/mandato (mesmo simples) faz o painel e a lista de votos nominais mostrarem **nomes** —
   é o que a banca espera ver. Resolve o "beco" do GUID cru em `VotacaoDetailPage`/`ObterPresencas`.
3. **Painel/telão da votação + auto-refresh.** Já existe `ObterPlacarDaVotacao` (placar agregado).
   Falta uma **tela de painel** (presentes/ausentes + Sim/Não/Abstenção + resultado) que **atualiza
   sozinha** (polling/`refetchInterval` ou SignalR) para a demonstração de votação ao vivo. Hoje a
   tela "Votações" é consulta-por-ID, sem lista nem atualização — fluxo travado para quem não decora o GUID.
4. **Ata sintética automática no encerramento.** `EncerrarSessao` já tem todos os dados (presenças,
   itens da Ordem do Dia, resultados de votação). Gerar uma ata (mesmo texto/PDF simples) ao encerrar
   fecha o requisito literal de E2 e dá um artefato palpável para o avaliador baixar.
5. **Lista de votações por sessão + atalho na SessaoDetailPage.** Ligar Sessão → suas votações →
   placar, para a demo fluir "abro a sessão, pauto, voto, vejo o painel, encerro, gero a ata" sem
   precisar copiar IDs entre telas.
6. **Comissão como cadastro leve (opcional para PoC de "processo legislativo").** Promover a string
   `comissao` para um cadastro com membros efetivos/suplentes cobre E2/E3 e remove o "parcial" do
   item 4/5. Não é eliminatório nas PoCs tipo Matão/Salto, mas é barato e fecha a crítica óbvia.

> Itens 1-3 são o **mínimo** para uma demo sem becos. Itens 4-5 elevam a "convincente". Item 6 é o
> diferencial de cobertura.

---

## 5. Os 5 maiores riscos numa demo (resposta direta)

1. **Tela inicial vazia / sem dados (sem seed).** Maior risco prático: o avaliador entra e não há o
   que mostrar; toda demo depende de cadastrar tudo na hora, o que consome o tempo e expõe formulários.
   → mitigar com seed (Polish #1).
2. **Painel sem nomes e sem vida.** Votos e presenças aparecem como **GUID** e o placar **não
   atualiza sozinho**; não há tela de painel/telão. A "votação eletrônica com painel" — coração da PoC
   de Câmara — fica anêmica. → mitigar com Vereador + painel auto-refresh (Polish #2 e #3).
3. **Sem ATA ao final.** Encerrar a sessão não produz nenhum documento; o avaliador pede a ata
   (requisito literal E2 item 2.1.8) e não há artefato. → mitigar com Polish #4.
4. **Navegação por ID (becos entre telas).** "Votações" é consulta-por-ID e não há ponte
   Sessão→Votação; o operador precisa colar GUIDs. Numa demo ao vivo isso trava e parece imaturo.
   → mitigar com listagem + atalhos (Polish #3/#5).
5. **Comissão é string, sem cadastro de membros.** Se a banca pedir "mostre as comissões e seus
   membros efetivos/suplentes" (E2 §3.13, literal), não há tela. O fluxo de parecer funciona, mas a
   gestão de comissões não existe. → mitigar com Polish #6 (ou contornar declarando escopo "processo
   legislativo" e diferindo comissões).

---

## 6. Conclusão

O núcleo legislativo tem **arquitetura e domínio sólidos** (provavelmente o módulo mais bem modelado
do sistema depois de Finanças/Tributos) e o **fluxo principal fecha de ponta a ponta com UI**. Mas
"demonstrável fim-a-fim numa banca" hoje é **~62%**, não ~75%: faltam as quatro peças que um avaliador
de Câmara verifica primeiro — **dados de exemplo, vereadores nominais, painel ao vivo e ata
automática**. São gaps de **baixo a médio custo** (nenhum exige integração externa nem motor fiscal),
e todos os dados para preenchê-los já existem nos agregados atuais. Fechando os Polish #1-#4, a trilha
legislativa passa de "tecnicamente pronta mas com becos" para **PoC-vencível** no recorte "software de
processo legislativo" (Matão/Salto).
