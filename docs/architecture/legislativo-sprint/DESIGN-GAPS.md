# DESIGN — Sprint de Gaps Legislativos (PoC-vencível)

> **Objetivo:** fechar 3 gaps recorrentes em editais de Câmara (baratos, de alto retorno) para a trilha
> LEGISLATIVA passar de ~75% para "PoC-vencível" em editais de **software de processo legislativo**
> (perfil Matão/SP, Salto/SP). NÃO é ERP-de-Câmara completo (esse depende de M5/Folha).
> **Documento de DESIGN/AUDITORIA — não há código aqui.** Pronto-para-implementar seguindo os
> padrões já consolidados do módulo Legislativo e dos módulos de referência (Finanças/Transparência).
> Data: 2026-06-22.

---

## 0. Escopo e fundamentação (das fontes verificadas)

Os 3 gaps abaixo são **CONFIRMADOS literalmente** em editais reais (ver `verificacao-requisitos-legislativo.md`):

| Gap | Evidência verificada | Veredito do auditor |
|---|---|---|
| **G1 — Normas Jurídicas (base consultável)** | REF SAPL "Manutenção da base de leis"; E3 (Curitiba) l.646 "consulta à legislação" | VÁLIDO. **Remover LexML do caminho crítico** (raro em texto de edital). |
| **G2 — Diário Oficial Eletrônico** | E1 (Matão) "diário oficial" | VÁLIDO, recorrente. Relação com Transparência/LAI. |
| **G3 — Cronômetro de tribuna + inscrição de oradores** | E2 (Vitória) l.462-463 "Controlar os cronômetros"; E3 l.75 "gerenciamento de oradores, pela inscrição de oradores"; l.80 "cronômetro digital auxiliar" | VÁLIDO, recorrente. |

**Fora deste sprint (decisão consciente):** deliberação remota (rebaixada a diferencial), terminais
físicos/hot-swap (licitação de hardware, condicional ao edital), votação simbólica informatizada
(E3 explicitamente NÃO informatiza), LexML verbatim (raro). Itens a **validar** em paralelo (já existem,
só confirmar): Ata Sintética automática ao fim da sessão e cadastro de membros de comissão.

### Aderência à Constituição (`CLAUDE.md`) — invariável para os 3 gaps

- **Isolamento de módulo:** tudo dentro de `Modules/Legislativo`. Cross-module só via `*.Contracts`
  (Integration Events). G2 publica para Transparência via Contracts; nunca chama handler/entidade de outro módulo.
- **Multi-tenant:** toda raiz de agregado implementa `IMustHaveTenant`; `TenantId` carimbado pelo
  `TenantSaveChangesInterceptor`; Global Query Filter herdado de `ModuleDbContext`. Banco DEDICADO por
  tenant (conexão via `ITenantConnectionResolver`) — padrão idêntico ao `LegislativoDbContext` atual.
- **Auditoria imutável:** mutações passam pelo `AuditSaveChangesInterceptor` (antes/depois, quem, quando, IP).
  Domain Events → Outbox via `ConvertDomainEventsToOutboxInterceptor`.
- **Domínio rico:** construtores privados + factory methods; entidade nasce válida; invariantes no domínio.
- **CQRS/MediatR:** Commands/Queries com `ICommand<T>`/`IQuery<T>`, validators FluentValidation, pipeline
  Validation→Logging→Transaction→Idempotency (já registrado no módulo).
- **RBAC deny-by-default:** novos escopos adicionados ao catálogo canônico `Permissoes` (módulo Identidade)
  e a `Permissoes.Todas`. Endpoints com `.RequirePermission(...)`.
- **Frontend gov.br DS:** telas em `src/Web/src/modules/legislativo`, seguindo `docs/design-system/`,
  WCAG 2.1 AA / eMAG. Web nativo, sem emulador (trunfo dos editais).

### Padrões concretos a replicar (já existentes no módulo)

- Id forte: `readonly record struct XId(Guid Value)` com `New()` — ver `SessaoId`, `ProposicaoId`.
- Agregado: `sealed class X : AggregateRoot<XId>, IMustHaveTenant` com `private X()` (EF) + ctor privado +
  factory estático que valida e dá `RaiseDomainEvent(...)`.
- Handlers: `ICommandHandler<,>`/`IQueryHandler<,>` recebendo repositório + `IUnitOfWork` + `ITenantContext`.
- Repositórios: interface em `Application/Abstractions`, impl em `Infrastructure/Persistence/Repositories`.
- EF: `IEntityTypeConfiguration<T>` com `HasConversion` de Ids, `OwnsMany` para coleções filhas,
  `HasIndex(new { TenantId, ... })`, `Ignore` de propriedades calculadas.
- Migration por módulo no `LegislativoDbContext` (schema `legislativo`).

---

## G1 — NORMAS JURÍDICAS (base de leis/decretos/resoluções consultável)

### 1.1 Linguagem ubíqua e escopo

Base consultável das normas do município/Câmara: **Leis (ordinárias/complementares), Decretos
Legislativos, Resoluções, Emendas à LOM, Lei Orgânica**. Origem típica: uma `Proposicao` aprovada
que vira norma após sanção/promulgação. Requisitos: cadastro, vínculo com a proposição de origem,
**busca textual** (ementa/número/ano/tipo/palavra), e **ciclo de vigência** (em vigor / revogada /
alterada). Compilação/consolidação de texto entra como diferencial leve (campo de texto articulado),
sem motor de consolidação automática (fora do escopo barato).

### 1.2 Entidades e invariantes

**Agregado raiz: `Norma`** (`Domain/Normas/Norma.cs`)

- Id forte `NormaId`. `IMustHaveTenant`.
- Campos: `TipoNorma` (Lei, LeiComplementar, DecretoLegislativo, Resolucao, EmendaLOM, LeiOrganica),
  `Numero` (int), `Ano` (int), `Ementa` (reusar VO `Ementa` de Proposições), `DataPromulgacao` (DateOnly),
  `TextoArticulado` (string longa, opcional na PoC), `ProposicaoOrigemId?` (vínculo opcional),
  `SituacaoVigencia` (EmVigor, Revogada, Alterada), `DataRevogacao?`, `NormaRevogadoraId?`.
- Coleção filha **append-only `HistoricoVigencia`** (entidade filha `EventoVigencia`: tipo Promulgacao/
  Revogacao/Alteracao, data, normaReferenciaId?, observação) — trilha imutável para prova jurídica/LAI.

**Invariantes (no domínio, testáveis):**
- **N-1** `Numero > 0`, `Ano` plausível (ex.: 1900..2100), `Ementa` não vazia, `TipoNorma`/`SituacaoVigencia` definidos.
- **N-2** Unicidade lógica `(TenantId, TipoNorma, Numero, Ano)` — não cadastrar duas Lei nº/ano iguais
  (índice único no EF + verificação no handler antes de persistir).
- **N-3** Nasce **`EmVigor`** com 1 evento `Promulgacao` no histórico (factory `Promulgar`).
- **N-4** `Revogar(dataRevogacao, normaRevogadoraId?)` só a partir de `EmVigor`/`Alterada`; passa a
  `Revogada` (terminal: não admite nova revogação); `dataRevogacao >= DataPromulgacao`; emite `NormaRevogada`.
- **N-5** `RegistrarAlteracao(dataReferencia, normaAlteradoraId)` só se não `Revogada`; passa a `Alterada`
  (não terminal — pode ser revogada depois); append no histórico.
- **N-6** `VincularProposicaoOrigem(proposicaoId)` idempotente; só permitido enquanto `ProposicaoOrigemId == null`.
- **N-7** Histórico é **append-only** (sem update/delete de eventos).

> **Nota de fronteira (importante p/ isolamento):** o vínculo com `ProposicaoOrigemId` é **mesmo módulo**
> (Legislativo), logo é referência interna legítima — NÃO viola isolamento. A criação automática da Norma
> a partir de uma proposição aprovada/promulgada deve ser feita por **handler de Domain Event interno**
> (ex.: ao `ProposicaoAprovada`/promulgação) ou por ato manual do servidor; nunca por acoplamento cross-module.

### 1.3 Handlers / endpoints

Pasta `Application/Normas`. Repositório `INormaRepository` (`Application/Abstractions`).

Commands:
- `CadastrarNormaCommand` (tipo, numero, ano, ementa, dataPromulgacao, textoArticulado?, proposicaoOrigemId?) → `Guid`.
- `RevogarNormaCommand` (normaId, dataRevogacao, normaRevogadoraId?).
- `RegistrarAlteracaoNormaCommand` (normaId, dataReferencia, normaAlteradoraId).
- `VincularProposicaoOrigemCommand` (normaId, proposicaoId).

Queries (projeções de leitura):
- `BuscarNormasQuery` (filtros: termo livre na ementa, tipo?, numero?, ano?, situacaoVigencia?, paginação) → lista resumo.
- `ObterNormaPorIdQuery` → detalhe (inclui histórico de vigência + proposição de origem).

Endpoints (`LegislativoEndpoints` → `MapearNormas`):
- `POST /api/legislativo/normas` — `legislativo.normas.gerenciar`
- `POST /api/legislativo/normas/{normaId:guid}/revogacao` — `legislativo.normas.gerenciar`
- `POST /api/legislativo/normas/{normaId:guid}/alteracao` — `legislativo.normas.gerenciar`
- `POST /api/legislativo/normas/{normaId:guid}/proposicao-origem` — `legislativo.normas.gerenciar`
- `GET  /api/legislativo/normas` (busca filtrada/paginada) — `legislativo.ver` (consulta pública interna)
- `GET  /api/legislativo/normas/{normaId:guid}` — `legislativo.ver`

> **Busca pública (cidadão):** para LAI, a consulta de normas é informação pública. Na PoC, expor um
> endpoint de leitura **sem RBAC restritivo** é aceitável (read-only, tenant-scoped pelo host público).
> Decisão de design: manter consulta atrás de `legislativo.ver` no backoffice e prever um espelho público
> no Portal da Transparência (G2/M8) reusando a mesma query — evita duplicar regra.

### 1.4 RBAC

Adicionar ao catálogo `Permissoes` (Identidade) + `Permissoes.Todas`:
- `legislativo.normas.gerenciar` = `"legislativo.normas.gerenciar"` (cadastrar/revogar/alterar).
- Leitura usa o `legislativo.ver` existente (granularidade suficiente para PoC).

### 1.5 Frontend (telas gov.br)

`src/Web/src/modules/legislativo/normas/`:
- **NormaListPage** — busca com `br-input` (termo), filtros `br-select` (tipo, ano, situação), tabela
  `br-table` com paginação; badge de situação (Em vigor = verde / Revogada = vermelho). Botão "Nova norma"
  visível só com `legislativo.normas.gerenciar`.
- **NormaDetailPage** — cabeçalho (tipo/número/ano/ementa), `br-tag` de vigência, linha do tempo
  (`Histórico de vigência`), link "Proposição de origem" (rota da `ProposicaoDetailPage` existente),
  texto articulado em bloco legível.
- **NormaFormPage / NormaAcoes** — formulário de cadastro + ações Revogar/Registrar alteração com modal
  de confirmação (gov.br `br-modal`).

### 1.6 Testes mínimos (BDD + unidade)

- `Norma_Promulgar_nasce_em_vigor_com_evento_no_historico` (N-3).
- `Norma_numero_ano_tipo_duplicado_no_tenant_e_rejeitado` (N-2).
- `Revogar_de_norma_em_vigor_marca_revogada_e_emite_evento` (N-4).
- `Revogar_norma_ja_revogada_lanca` (N-4 terminal).
- `RegistrarAlteracao_em_norma_revogada_lanca` (N-5).
- `Buscar_por_ementa_e_ano_retorna_apenas_do_tenant` (isolamento + busca).
- Handler: `Cadastrar` persiste e dispara auditoria; `Buscar` respeita Global Query Filter.

---

## G2 — DIÁRIO OFICIAL ELETRÔNICO (edições, matérias, publicação datada)

### 2.1 Linguagem ubíqua e escopo

Veículo oficial de publicação da Câmara. Uma **`EdicaoDiario`** (ex.: "Edição nº 123, de 22/06/2026")
agrupa **`MateriaDiario`** (atos publicados: norma, ata de sessão, edital, portaria, extrato).
Fluxo: edição em **rascunho** → recebe matérias → é **publicada** (data/hora oficial imutável, número
sequencial, opcionalmente PDF assinado) → fica **disponível** para consulta pública (LAI). A publicação
é o **marco legal de eficácia** dos atos.

### 2.2 Entidades e invariantes

**Agregado raiz: `EdicaoDiario`** (`Domain/DiarioOficial/EdicaoDiario.cs`)

- `EdicaoDiarioId`, `IMustHaveTenant`.
- `Numero` (sequencial por tenant/ano), `Ano`, `SituacaoEdicao` (Rascunho, Publicada, RetificadaPor?),
  `DataPublicacao?` (DateTimeOffset — set só na publicação), `HashConteudo?` (integridade do PDF/edição).
- Coleção filha **`Materias`** (entidade `MateriaDiario`: `MateriaId`, `TipoMateria` (Norma, AtaSessao,
  Edital, Portaria, Extrato, Outro), `Titulo`, `Conteudo`/`ReferenciaId?` apontando à entidade de origem
  no mesmo módulo, `Ordem`).

**Invariantes:**
- **D-1** Edição nasce **`Rascunho`** com `DataPublicacao == null` (factory `Abrir`).
- **D-2** `AdicionarMateria(...)` só com edição **Rascunho**; matéria nasce com `Ordem` sequencial; título não vazio.
- **D-3** `Publicar(dataPublicacao, hashConteudo)` só a partir de `Rascunho`, **exige ≥ 1 matéria**;
  define `DataPublicacao` (imutável depois), `Numero` sequencial confirmado, passa a `Publicada`; emite
  `DiarioPublicado`. **Idempotente** (republicar é no-op se já `Publicada`).
- **D-4** Edição **`Publicada` é imutável**: não admite adicionar/remover matéria nem alterar data
  (append-only para prova/LAI). Correção pós-publicação só via **nova edição de retificação**
  (`Retificar(edicaoOriginalId)` cria edição que referencia a anterior) — não edita a original.
- **D-5** `Numero` único por `(TenantId, Ano)`; geração sequencial controlada no handler (lock otimista/índice único).
- **D-6** `DataPublicacao` não pode ser futura na publicação (relógio do servidor; carimbo de tempo é diferível).

### 2.3 Relação com Transparência/LAI (via Contracts — isolamento)

A publicação emite o Domain Event `DiarioPublicado`, mapeado para um **Integration Event público** no
`Modules.Legislativo.Contracts` para o módulo Transparência indexar/expor no Portal LAI:

`DiarioOficialPublicadoIntegrationEvent(EventId, OccurredOnUtc, TenantId, EdicaoDiarioId, Numero, Ano, DataPublicacao)`
— seguindo exatamente o padrão de `AutografoEnviadoIntegrationEvent` e `RemessaEnviadaTceIntegrationEvent`
(record `: IntegrationEvent`, publicado via Outbox, idempotente por `EventId` no consumidor).

> **Isolamento garantido:** Legislativo NÃO conhece entidades de Transparência. Transparência consome o
> Contract e decide como expor. Na PoC, se Transparência ainda não consumir, o evento fica no Outbox
> (sem quebrar nada) e a consulta pública pode ser servida pelo próprio endpoint de leitura do Diário.

### 2.4 Handlers / endpoints

Pasta `Application/DiarioOficial`. `IEdicaoDiarioRepository`.

Commands:
- `AbrirEdicaoDiarioCommand` (ano) → `Guid`.
- `AdicionarMateriaCommand` (edicaoId, tipoMateria, titulo, conteudo?/referenciaId?).
- `PublicarEdicaoDiarioCommand` (edicaoId) → confirma número/data.
- `RetificarEdicaoCommand` (edicaoOriginalId) → `Guid` (nova edição rascunho).

Queries:
- `ListarEdicoesQuery` (ano?, situacao?, paginação) → resumo.
- `ObterEdicaoPorIdQuery` → detalhe com matérias.
- `ConsultarDiarioPublicoQuery` (data?/intervalo?/termo) → leitura pública (LAI).

Endpoints (`MapearDiarioOficial`):
- `POST /api/legislativo/diario/edicoes` — `legislativo.diario.gerenciar`
- `POST /api/legislativo/diario/edicoes/{edicaoId:guid}/materias` — `legislativo.diario.gerenciar`
- `POST /api/legislativo/diario/edicoes/{edicaoId:guid}/publicacao` — `legislativo.diario.publicar`
- `POST /api/legislativo/diario/edicoes/{edicaoId:guid}/retificacao` — `legislativo.diario.gerenciar`
- `GET  /api/legislativo/diario/edicoes` — `legislativo.ver`
- `GET  /api/legislativo/diario/edicoes/{edicaoId:guid}` — `legislativo.ver`
- `GET  /api/legislativo/diario/publico` (consulta cidadã) — `legislativo.ver` (espelho público no Portal M8)

### 2.5 RBAC (segregação ato de publicação)

Adicionar a `Permissoes` + `Todas`:
- `legislativo.diario.gerenciar` = `"legislativo.diario.gerenciar"` (montar edição/matérias/retificar).
- `legislativo.diario.publicar` = `"legislativo.diario.publicar"` — **verbo fino de SoD**: publicar é ato
  com efeito legal (eficácia dos atos), separado de quem monta a edição. Segue o padrão dos verbos
  `financas.*.assinar`/`transparencia.remessa.transmitir` já no catálogo.

### 2.6 Frontend (telas gov.br)

`src/Web/src/modules/legislativo/diario/`:
- **DiarioListPage** — tabela de edições (número, data, situação badge), filtro por ano/situação.
- **EdicaoEditorPage** — montagem da edição: lista de matérias (drag/ordem), `br-button` "Adicionar
  matéria" (modal com tipo/título/conteúdo ou seletor de norma/ata existente), botão "Publicar" (modal
  de confirmação, visível só com `legislativo.diario.publicar`), desabilitado quando `Publicada`.
- **DiarioPublicoPage** — consulta cidadã: busca por data/período/termo, visual gov.br de "Diário Oficial"
  (cabeçalho institucional, lista de edições publicadas com link de leitura). Acessível no Portal (M8).

### 2.7 Testes mínimos

- `Edicao_Abrir_nasce_rascunho_sem_data` (D-1).
- `AdicionarMateria_em_edicao_publicada_lanca` (D-4).
- `Publicar_sem_materia_lanca` (D-3).
- `Publicar_define_data_e_numero_e_emite_evento_integracao` (D-3 + Outbox).
- `Publicar_idempotente_nao_republica` (D-3).
- `Retificar_cria_nova_edicao_referenciando_original_sem_mutar_original` (D-4).
- `Numero_edicao_unico_por_tenant_ano` (D-5).
- Integração: ao publicar, `DiarioOficialPublicadoIntegrationEvent` cai no Outbox (consumidor idempotente).

---

## G3 — CRONÔMETRO DE TRIBUNA + INSCRIÇÃO DE ORADORES (na Sessão)

### 3.1 Linguagem ubíqua e escopo

Durante a sessão, vereadores se **inscrevem** para falar em uma **fase de uso da palavra** (Pequeno
Expediente, Grande Expediente, Explicação Pessoal, Tribuna Livre). Cada orador tem um **tempo regimental**
(ex.: 5 min). O presidente **chama** o orador, o **cronômetro** conta (iniciar/pausar/retomar/encerrar),
podendo registrar **excedente**. Apartes (interrupções) são diferencial leve — modelar como contagem
opcional, sem cronômetro próprio na PoC.

**Decisão de modelagem:** isto é comportamento **da Sessão** (agregado existente `Sessao`). Para não
inchar o agregado nem violar consistência, modela-se como **agregado próprio `TribunaSessao`**
(uma por sessão), referenciando `SessaoId`. Inscrições e turnos de fala vivem dentro dele. O cronômetro
é **autoritativo no servidor** (timestamps), com o front apenas refletindo — nunca confiar no relógio do cliente.

### 3.2 Entidades e invariantes

**Agregado raiz: `TribunaSessao`** (`Domain/Tribuna/TribunaSessao.cs`)

- `TribunaSessaoId`, `IMustHaveTenant`, `SessaoId` (vínculo interno — mesmo módulo).
- `TempoPadraoOrador` (TimeSpan, parametrizável por tenant — NÃO hardcoded; default vem de `IOptions`/config do tenant).
- Coleção filha **`Inscricoes`** (entidade `InscricaoOrador`): `InscricaoId`, `VereadorId` (reusar VO de Sessões),
  `FaseUsoPalavra` (PequenoExpediente, GrandeExpediente, ExplicacaoPessoal, TribunaLivre), `Ordem`,
  `Situacao` (Inscrito, EmUso, Concluido, Cancelado), `TempoConcedido` (TimeSpan),
  `IniciadoEm?`/`EncerradoEm?` (DateTimeOffset), `Pausas` (lista de intervalos), `Apartes` (int, opcional).

**Invariantes:**
- **T-1** `TribunaSessao` só é criada para uma sessão **não terminal** (e idealmente `Aberta`).
- **T-2** `Inscrever(vereadorId, fase, tempoConcedido?)`: só com sessão não terminal; **idempotente por
  (vereadorId, fase)** — segunda inscrição na mesma fase é no-op (espelha a idempotência de `RegistrarPresenca`).
  `tempoConcedido` default = `TempoPadraoOrador`. Define `Ordem` sequencial na fila. Estado `Inscrito`.
- **T-3** **No máximo um orador `EmUso` por vez** em toda a tribuna (invariante de exclusão mútua).
- **T-4** `IniciarFala(inscricaoId, momento)`: só se inscrição `Inscrito` e nenhum outro `EmUso`; set `IniciadoEm`,
  estado `EmUso`; emite `OradorIniciouFala`.
- **T-5** `Pausar`/`Retomar(inscricaoId, momento)`: só sobre o orador `EmUso`; pausa acumula intervalo;
  retomar exige estado pausado. (Tempo decorrido = `(now - IniciadoEm) - Σ pausas`.)
- **T-6** `EncerrarFala(inscricaoId, momento)`: só sobre `EmUso`; set `EncerradoEm`, estado `Concluido`;
  calcula `TempoUtilizado` e `Excedente = max(0, TempoUtilizado - TempoConcedido)`; emite `OradorEncerrouFala`.
  Libera a exclusão mútua (T-3).
- **T-7** `CancelarInscricao(inscricaoId)`: só se `Inscrito` (não cancelar quem já falou/está falando).
- **T-8** Tempo é **derivado de timestamps do servidor**, nunca recebido pronto do cliente (anti-fraude/prova).
- **T-9** Trilha das falas é **append-only** (situação evolui, registros não são apagados) — prova jurídica/LAI.

> **Por que cronômetro server-side e não no front:** requisito de prova (LAI/ata) e anti-manipulação. O
> backend grava `IniciadoEm`/`EncerradoEm`/pausas; o front faz a contagem visual a partir do `IniciadoEm`
> retornado. Em rede instável, a verdade é reconstruível dos timestamps. Diferimento aceitável na PoC:
> push em tempo real (SignalR) — a PoC pode usar polling curto (web nativo, sem emulador).

### 3.3 Handlers / endpoints

Pasta `Application/Tribuna`. `ITribunaSessaoRepository`.

Commands:
- `AbrirTribunaCommand` (sessaoId, tempoPadraoSegundos?) → `Guid`.
- `InscreverOradorCommand` (tribunaId | sessaoId, vereadorId, fase, tempoConcedidoSegundos?) → `Guid` (inscricaoId).
- `IniciarFalaCommand` (tribunaId, inscricaoId).
- `PausarFalaCommand` / `RetomarFalaCommand` (tribunaId, inscricaoId).
- `EncerrarFalaCommand` (tribunaId, inscricaoId).
- `CancelarInscricaoCommand` (tribunaId, inscricaoId).

> Para idempotência/UnitOfWork, o `momento` (DateTimeOffset) é resolvido por um `IClock`/`TimeProvider`
> injetado no handler (não vem do cliente) — coerente com T-8.

Queries:
- `ObterTribunaDaSessaoQuery` (sessaoId) → estado da fila + orador atual + tempos.
- `ListarInscricoesQuery` (sessaoId, fase?) → ordem de inscrição.

Endpoints (`MapearTribuna`, sob a sessão):
- `POST /api/legislativo/sessoes/{sessaoId:guid}/tribuna` — `legislativo.gerenciar`
- `POST /api/legislativo/sessoes/{sessaoId:guid}/tribuna/inscricoes` — `legislativo.gerenciar`
  - (Auto-inscrição do próprio vereador pode usar a mesma permissão na PoC; verbo fino opcional abaixo.)
- `POST .../tribuna/inscricoes/{inscricaoId:guid}/inicio` — `legislativo.tribuna.controlar`
- `POST .../tribuna/inscricoes/{inscricaoId:guid}/pausa` — `legislativo.tribuna.controlar`
- `POST .../tribuna/inscricoes/{inscricaoId:guid}/retomada` — `legislativo.tribuna.controlar`
- `POST .../tribuna/inscricoes/{inscricaoId:guid}/encerramento` — `legislativo.tribuna.controlar`
- `POST .../tribuna/inscricoes/{inscricaoId:guid}/cancelamento` — `legislativo.gerenciar`
- `GET  /api/legislativo/sessoes/{sessaoId:guid}/tribuna` — `legislativo.ver`

### 3.4 RBAC

Adicionar a `Permissoes` + `Todas`:
- `legislativo.tribuna.controlar` = `"legislativo.tribuna.controlar"` — controle do cronômetro
  (iniciar/pausar/retomar/encerrar) é ato de **mesa diretora/presidência**, separado da gestão geral.
- Inscrição e leitura usam `legislativo.gerenciar` / `legislativo.ver` existentes.

### 3.5 Frontend (telas gov.br)

`src/Web/src/modules/legislativo/tribuna/`:
- **TribunaPainelPage** — painel de controle (mesa diretora): fila de oradores por fase (`br-list`),
  orador atual em destaque com **cronômetro grande** (mm:ss, cor muda ao exceder), botões Iniciar/Pausar/
  Retomar/Encerrar (visíveis só com `legislativo.tribuna.controlar`). Cronômetro renderizado do
  `IniciadoEm` do servidor (não do clique local). Atualização por polling curto (sem emulador).
- **InscricaoOradorForm** — `br-select` de vereador + fase + tempo (default do tenant); auto-inscrição
  simples para o vereador logado.
- **TribunaPublicaWidget** (opcional) — exibição read-only de quem está na tribuna e tempo restante
  (painel apregoador / TV Câmara web), reusando a query pública.

### 3.6 Testes mínimos

- `Inscrever_idempotente_por_vereador_e_fase` (T-2).
- `IniciarFala_com_outro_orador_em_uso_lanca` (T-3/T-4 exclusão mútua).
- `Pausar_e_retomar_descontam_intervalo_do_tempo_utilizado` (T-5).
- `EncerrarFala_calcula_tempo_e_excedente` (T-6).
- `Tempo_e_derivado_de_timestamps_do_servidor_nao_do_cliente` (T-8 — handler usa `TimeProvider`).
- `CancelarInscricao_de_orador_em_uso_lanca` (T-7).
- `Tribuna_so_para_sessao_nao_terminal` (T-1).

---

## 4. Integração transversal e impacto na solução

- **DbContext:** adicionar `DbSet<Norma>`, `DbSet<EdicaoDiario>`, `DbSet<TribunaSessao>` ao
  `LegislativoDbContext` (schema `legislativo`) + 3 `IEntityTypeConfiguration` + **1 migration**
  (`AddNormasDiarioTribuna`). Coleções filhas via `OwnsMany` (padrão `SessaoConfiguration`).
- **Contracts:** +1 Integration Event (`DiarioOficialPublicadoIntegrationEvent`). G1 e G3 não precisam de
  Contracts (sem necessidade de cross-module na PoC).
- **Domain Events:** novos records em `Domain/Events` (`NormaRevogada`, `DiarioPublicado`,
  `OradorIniciouFala`, `OradorEncerrouFala`, etc.) → Outbox via interceptor existente.
- **RBAC:** +5 escopos no catálogo `Permissoes` e em `Permissoes.Todas` (`legislativo.normas.gerenciar`,
  `legislativo.diario.gerenciar`, `legislativo.diario.publicar`, `legislativo.tribuna.controlar`;
  leitura reaproveita `legislativo.ver`). Papel "Administrador" semeado recebe `Todas` → ganha tudo automaticamente.
- **Endpoints:** 3 novos blocos em `LegislativoEndpoints` (`MapearNormas`, `MapearDiarioOficial`, `MapearTribuna`).
- **NetArchTest:** nada novo a configurar; os 3 gaps respeitam Domain←Application←Infrastructure e
  cross-module só via Contracts. O teste de arquitetura existente cobre automaticamente.
- **Frontend:** 3 áreas novas em `src/Web/src/modules/legislativo`, todas gov.br DS / WCAG AA / web nativo.

---

## 5. Riscos e decisões conscientes (honestidade de auditor)

- **LexML fora do caminho crítico** (raro em edital). `TextoArticulado` é campo livre; URN/consolidação
  automática NÃO entram na PoC. Se um edital-alvo exigir LexML nominalmente, vira item contratual diferível (5%).
- **Cronômetro tempo real:** PoC com polling (web nativo, trunfo dos editais). SignalR/push é melhoria pós-PoC.
- **Carimbo de tempo qualificado** (ICP-Brasil) na publicação do Diário: hoje é relógio do servidor —
  mesma limitação já registrada do Protocolo; diferível, não bloqueia PoC de processo legislativo.
- **Diário ↔ Transparência:** desacoplado por Contracts; se Transparência não consumir a tempo, o Diário
  se sustenta sozinho pela consulta pública própria. Sem dívida de isolamento.
- **Itens a só VALIDAR (não construir):** Ata Sintética automática ao encerrar sessão e cadastro de
  membros de comissão (efetivos/suplentes) — confirmar que já existem; se faltarem, são micro-gaps baratos
  do mesmo sprint (recorrentes em E2/E3), mas estão FORA do escopo destes 3 gaps designados.

---

## 6. Ordem de implementação (recomendada)

1. **RBAC primeiro** (5 escopos no catálogo `Permissoes` + `Todas`) — destrava `.RequirePermission` dos endpoints; baixo risco, base para todo o resto.
2. **G1 — Normas Jurídicas.** Mais simples (CRUD rico + busca + vigência), sem cross-module, sem cronômetro. Valida o pipeline domínio→handler→EF→endpoint→tela no sprint. Entrega visível e demonstrável cedo.
3. **G2 — Diário Oficial.** Reusa o padrão de G1 e **consome G1** (matéria do tipo Norma referencia a Norma criada). Adiciona o Integration Event (Contracts) e o ato segregado `publicar`. Demonstra a relação com Transparência/LAI.
4. **G3 — Cronômetro + Oradores.** Mais sensível (estado/tempo server-side, exclusão mútua, `TimeProvider`). Deixar por último para implementar com o pipeline já maduro; depende da Sessão (já pronta).
5. **Migration única** consolidando os 3 agregados + **suite de testes mínima** (invariantes N/D/T) + **NetArchTest** verde.
6. **Frontend** em paralelo a partir do passo 2 (cada gap tem suas telas gov.br assim que os endpoints existem).
7. **Validação de fechamento:** rodar o cenário PoC ponta-a-ponta (cadastrar norma → publicá-la no Diário →
   abrir sessão → inscrever orador e cronometrar) e conferir auditoria/Outbox/isolamento de tenant.

> **Sequência crítica:** RBAC → G1 → G2 → G3. G2 depende de G1 (referência de norma); G3 depende só da
> Sessão (já existe). Nada depende de M5 — este sprint fecha a trilha de **processo legislativo** sem tocar Folha.
