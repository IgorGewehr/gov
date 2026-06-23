# Auditoria adversarial — Legislativo: Proposição / Tramitação / Normas / Diário

Escopo: máquina de estados da proposição (pareceres obrigatórios, emendas, substitutivo,
aprovação/rejeição, autógrafo, sanção/veto), busca de normas (case/accent, vigência/revogação
em datas-limite) e publicação de Diário (idempotência, retificação). Foco em correção e robustez
para a demo de recebimento. Premissa: há bugs sutis nas bordas.

Data: 2026-06-22 · Auditor: adversarial (olhos de banca de TI do TCE).

---

## Sumário executivo

A modelagem de domínio é sólida: agregados ricos, invariantes no construtor/factory, trilha
append-only, idempotência declarada onde importa. A **aritmética de apuração de votação está
correta** em todos os tamanhos de Câmara testados (ver Anexo A) — não há erro de contagem fatal
no núcleo de apuração. Os riscos de demo migraram para a **camada de orquestração (handlers) e
para a busca de normas**, onde há acoplamentos frágeis e dependência de comportamento de banco
não garantido.

- **Bugs reais (lógica/correção):** 5
- **Lacunas de teste (comportamento certo, sem cobertura):** 7
- **Os 3 piores** estão em: (1) aprovação que não confere se a votação pertence à proposição;
  (2) busca de normas que confia na collation do banco para case/acento; (3) inclusão em Ordem
  do Dia que aceita parecer CONTRÁRIO como se cumprisse o requisito de parecer obrigatório.

---

## BUGS REAIS

### B1 — `AprovarProposicao` não valida que a votação é da proposição [CRÍTICO]
`AprovarProposicao.cs:39-53`.
O handler carrega a proposição e a votação por IDs independentes e **nunca compara
`votacao.ProposicaoId == proposicao.Id`**. `Votacao` tem `ProposicaoId` (Votacao.cs:57) justamente
para esse vínculo, mas ele é ignorado.
- Caso-limite: aprovar a Proposição X passando o `VotacaoId` de uma votação aprovada da Proposição Y
  (ex.: dois projetos na mesma sessão; ID trocado no front). X é aprovada com base no resultado de Y.
- Errado: numa demo com 2+ matérias na ordem do dia, um clique no card errado "aprova" a matéria
  errada sem qualquer erro. É exatamente a classe de falha que inviabiliza a demonstração.
- Agrava: o handler também **mapeia a maioria EXIGIDA da votação como maioria ATINGIDA**
  (`MapearMaioria(votacao.MaioriaExigida)`, linha 50/56-61). Não usa o placar real (`VotosSim`,
  `TotalMembros`, `Presentes`). Como `votacao.Resultado == Aprovado` já foi apurado pelo domínio,
  na prática funciona — **mas** se a votação tiver sido aberta com `MaioriaExigida` MENOR que a do
  tipo da proposição (ex.: votação `Simples` para um PLC que exige `Absoluta`), o `Satisfaz` recebe
  `Simples` e corretamente falha; já se aberta com maioria MAIOR, passa. O vínculo é por confiança,
  não por verificação. Não há nenhum teste cobrindo divergência entre a maioria da votação e a do tipo.
- Teste a adicionar: `Aprovar_com_votacao_de_outra_proposicao_falha` (handler); e
  `Aprovar_PLC_com_votacao_aberta_como_simples_*` para fixar o comportamento esperado do mapeamento.

### B2 — Inclusão em Ordem do Dia aceita parecer CONTRÁRIO como parecer obrigatório [CRÍTICO]
`Proposicao.cs:229-249` + `Proposicao.cs:353-356` (`PossuiParecer`).
`IncluirEmOrdemDoDia` exige existir parecer da CCJ e de Finanças/Orçamento, mas `PossuiParecer`
só verifica `Fase == Parecer && Comissao == X` — **ignora `ParecerFavoravel`**. Um parecer
explicitamente CONTRÁRIO da CCJ (inconstitucionalidade) satisfaz o requisito.
- Caso-limite: CCJ emite parecer contrário (favoravel=false) por vício de constitucionalidade;
  a proposição é incluída em Ordem do Dia normalmente.
- Certo/errado: depende da regra interna. Parecer contrário da CCJ por inconstitucionalidade,
  em muitos regimentos, é terminativo (arquiva) ou exige recurso. No mínimo o estado atual
  silencia uma informação jurídica decisiva. O comentário do método fala em "ausência de parecer";
  o código trata "existe registro de parecer", sem olhar o sentido. É inconsistência entre intenção
  documentada e implementação.
- Teste a adicionar: `IncluirEmOrdemDoDia_com_parecer_ccj_contrario_*` (definir e travar a regra:
  bloquear, ou exigir flag de superação). Hoje passaria como sucesso por omissão.

### B3 — Busca de normas: case/acento dependem da collation, não há garantia no código [CRÍTICO p/ demo]
`LegislativoRepositories.cs:160-213` + `NormaConfiguration.cs:38-39` + `LegislativoTestBase.cs:29` (SQLite).
A busca usa `EF.Functions.Like(EmentaBusca, padrão)`. O case-insensitive **não está no código** —
depende inteiramente da collation da coluna. O teste roda em **SQLite em memória**, cujo `LIKE` é
case-insensitive apenas para ASCII A–Z (não acentuado) por padrão. Em produção (SQL Server) o
resultado depende da collation da coluna `EmentaBusca`, que não é fixada na configuração
(`.HasMaxLength(...)` sem `.UseCollation(...)`).
- Caso-limite 1 (acento): termo "acacias" NÃO casa com "Acácias" em nenhum dos dois bancos —
  nem SQLite nem SQL Server CI_AS. O teste `Buscar_por_termo...` (NormaTests.cs:136) só passa
  porque a ementa de teto usa "Acacias" SEM acento. Em dados reais ("Acácias", "São João",
  "Iluminação"), a busca cidadã da LAI falha em retornar a norma.
- Caso-limite 2 (case em não-ASCII / Unicode): "ç", "Ã" não são normalizados pelo LIKE ASCII do SQLite.
- Errado: a robustez anunciada ("Busca case-insensitive") é uma propriedade do ambiente, não do
  sistema. Numa demo com SQLite/SQLite-like ou SQL Server sem collation AI, buscar acento falha.
- Teste a adicionar: `Buscar_por_termo_com_acento_diferente_casa` (termo "acacias" vs ementa
  "Acácias") — esse teste FALHA hoje e expõe o bug. Corrigir normalizando para uma coluna
  `EmentaBusca` já sem acento/lowercase (normalização na sincronização do `SaveChanges`), e buscar
  sobre o termo normalizado.

### B4 — `RegistrarAlteracao` não valida data nem ordem temporal [MÉDIO]
`Norma.cs:197-206`.
`Revogar` valida `dataRevogacao >= DataPromulgacao` (linha 177), mas `RegistrarAlteracao` **não tem
nenhuma validação de data**. Aceita `dataReferencia` anterior à promulgação, ou posterior a uma
revogação que ainda não ocorreu, sem qualquer crítica.
- Caso-limite: norma promulgada em 2025-03-10 recebe alteração datada de 2024-01-01.
- Errado: viola a invariante temporal da trilha de vigência (prova jurídica/LAI). A trilha
  append-only fica com eventos fora de ordem cronológica.
- Teste a adicionar: `RegistrarAlteracao_com_data_anterior_a_promulgacao_lanca` (espelhar a regra
  já existente em `Revogar`).

### B5 — Revogação/alteração permitem auto-referência (norma revoga a si mesma) [BAIXO/MÉDIO]
`Norma.cs:170-187` e `:197-206`; handlers `AcoesNorma.cs:35-37,71`.
`Revogar(normaRevogadoraId)` e `RegistrarAlteracao(normaAlteradoraId)` não impedem
`normaRevogadoraId == Id` (norma revogando/alterando a si mesma) nem verificam a existência da
norma referenciada. O handler também não confere se a norma alteradora/revogadora existe no tenant.
- Caso-limite: revogar a Norma A informando `NormaRevogadoraId = A`.
- Errado: cria referência circular na trilha jurídica; e o `RegistrarAlteracao` exige
  `NormaAlteradoraId` não-vazio no validator, mas nunca valida que aponta para norma real.
- Teste a adicionar: `Revogar_com_norma_revogadora_igual_a_si_mesma_lanca` e
  `RegistrarAlteracao_norma_alteradora_inexistente_*`.

---

## LACUNAS DE TESTE (comportamento aparentemente correto, sem cobertura — risco de regressão)

### L1 — Apuração: nenhum teste exercita os limiares com placar real
`Votacao.cs:223-241` (`Apurar`). A aritmética está correta (Anexo A), mas **não há um único teste
de fronteira** que registre N votos e confira aprovado/rejeitado exatamente no limiar (ex.: 10
membros, 5 sim → reprova absoluta; 6 sim → aprova). Toda a cobertura de aprovação no
`ProposicaoFluxoTests` usa `ResultadoDeliberacao` construído à mão, pulando a contagem. É o ponto
mais sensível da demo e está sem teste de borda end-to-end.
- Teste a adicionar (parametrizado): `Apurar_simples_no_limiar`, `Apurar_absoluta_no_limiar`,
  `Apurar_qualificada_no_limiar`, incluindo membros par e ímpar, e abstenções não compondo numerador.

### L2 — Maioria simples com Presentes ímpar vs quórum
`Votacao.cs:229`. `votosSim > Presentes/2` (divisão inteira). Para Presentes=11 exige 6 (correto),
para 7 exige 4 (correto). Comportamento correto, mas sem teste; recomenda-se fixar para impedir
regressão a `>=`.

### L3 — Encerrar votação sem nenhum voto
`Votacao.cs:192-204`. Encerrar com zero votos apura `votosSim=0` → sempre Rejeitado. Comportamento
defensável, mas não testado; uma demo pode encerrar votação vazia por engano e o estado terminal
não é reversível. Teste: `Encerrar_sem_votos_resulta_rejeitado`.

### L4 — Diário: número sequencial tem corrida (não há teste de concorrência)
`EdicaoDiarioRepository.ProximoNumeroAsync:231-240` lê `MAX(Numero)+1` fora de transação serializável.
Duas aberturas concorrentes geram o mesmo número; o índice único `(TenantId, Ano, Numero)`
(`EdicaoDiarioConfiguration.cs:38`) salva a integridade lançando `DbUpdateException`, mas o handler
`AbrirEdicaoDiario` não trata/retenta. Em demo single-user não dispara; sob carga, a segunda
abertura falha com 500. Teste: `AbrirEdicao_concorrente_respeita_indice_unico` + retry no handler.

### L5 — Retificação não exige que a original esteja publicada
`MontarEdicao.cs:102-122` (`RetificarEdicaoHandler`) e `EdicaoDiario.Retificar:91-95`. Permite
"retificar" uma edição ainda em rascunho (não publicada), o que é semanticamente incoerente
(retifica-se ato já publicado). Sem validação e sem teste. Teste:
`Retificar_edicao_nao_publicada_*` (definir regra).

### L6 — Publicação do Diário: idempotência do EVENTO de integração não é garantida sob falha parcial
`PublicarEdicaoDiario.cs:41-59`. O `SaveChangesAsync` (estado publicada) e o `publisher.Publish`
(Outbox) são duas etapas; o guard `if (edicao.Publicada) return;` na linha 41 protege a republicação,
mas se o processo cair ENTRE o SaveChanges e o Publish, a edição fica publicada sem evento emitido,
e uma nova chamada cai no early-return e **nunca reemite**. Para Transparência/LAI isso é perda de
evento silenciosa. Teste/ajuste: emitir o IntegrationEvent dentro da mesma transação via Outbox
(padrão do CLAUDE.md §10) em vez de `IPublisher` pós-commit.

### L7 — Sanção/veto: idempotência e múltiplos registros
`Proposicao.cs:327-351`. `RegistrarSancao`/`RegistrarVeto` apenas checam `Situacao == AutografoEnviado`
e fazem append. Não impedem registrar sanção E veto na mesma proposição, nem duas sanções. A trilha
aceitaria "Sancao, Veto, Sancao" para o mesmo autógrafo. Teste: `RegistrarVeto_apos_sancao_*` /
`RegistrarSancao_duas_vezes_*` (definir se a segunda é no-op ou erro).

---

## OS 3 PIORES (ordem de prioridade para a demo)

1. **B1 — Aprovar sem conferir vínculo votação↔proposição** (`AprovarProposicao.cs:39-53`).
   Aprova a matéria errada com o ID de outra votação. Risco direto de "apuração errada na demo".
   Correção: 1 linha de guard `if (votacao.ProposicaoId != proposicao.Id) throw`.

2. **B3 — Busca de normas case/acento dependente de collation** (`LegislativoRepositories.cs:171-174`).
   "Acácias", "São João" não são encontrados; o teste verde mascara o bug por usar texto sem acento
   em SQLite. É a funcionalidade-vitrine da consulta LAI. Correção: coluna `EmentaBusca` normalizada
   (lower + sem diacríticos) na sincronização e busca sobre termo normalizado.

3. **B2 — Ordem do Dia aceita parecer contrário como parecer obrigatório** (`Proposicao.cs:353-356`).
   `PossuiParecer` ignora o sentido; um parecer de inconstitucionalidade da CCJ "cumpre" o requisito.
   Correção: decidir a regra e fazer `PossuiParecer` considerar `ParecerFavoravel` (ou expor estado
   "parecer contrário pendente de superação").

---

## Anexo A — Verificação da aritmética de apuração (`Votacao.Apurar`)

Conferida para membros/presentes de 7 a 15 (par e ímpar). Todos corretos:

- Simples (`votosSim > Presentes/2`): P=8→5, P=10→6, P=11→6. OK (>50% dos presentes).
- Absoluta (`votosSim >= TotalMembros/2 + 1`): T=9→5, T=10→6, T=11→6. OK (>50% dos membros).
- Qualificada (`votosSim >= ceil(2*T/3)`): T=9→6, T=10→7, T=11→8, T=12→8. OK (≥2/3 dos membros).

Não há erro de limiar no núcleo de contagem. O risco de "apuração errada" NÃO está na aritmética,
e sim na orquestração (B1) e na regra de parecer (B2). Recomenda-se ainda assim L1 (testes de
fronteira end-to-end com placar real) antes da demo, pois hoje o caminho de contagem real está
exercitado apenas indiretamente.
