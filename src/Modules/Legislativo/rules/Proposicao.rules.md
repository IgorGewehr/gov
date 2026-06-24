---
modulo: Legislativo
agregado: Proposicao
contexto: Legislativo (Processo Legislativo Municipal — proposicoes e tramitacao)
poder: Legislativo
schema: legislativo
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/88 art. 29 (Lei Organica e iniciativa)", "CF/88 art. 30 (competencias municipais)", "CF/88 arts. 59-69 (processo legislativo - simetria)", "Lei Organica Municipal (LOM)", "Regimento Interno (Resolucao)"]
---

# Proposicao — Regras-as-Code (Rules-as-Code)

> Materia submetida a apreciacao do Plenario da Camara Municipal (projeto de lei, emenda a LOM,
> decreto legislativo, resolucao, requerimento, indicacao, mocao). Percorre o ciclo de protocolo,
> distribuicao, instrucao por Comissoes (pareceres), inclusao em Ordem do Dia, deliberacao,
> geracao de autografo e remessa ao Executivo para sancao/veto/promulgacao. Conformidade com
> CF/88 art. 29 (iniciativa e Lei Organica), arts. 59-69 (simetria do processo legislativo), a
> Lei Organica Municipal e o Regimento Interno. Este arquivo e **normativo e versionado**; o
> codigo (agregado `Proposicao`, handlers, validators, EF config, testes) e consequencia dele.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Proposicao (`Proposicao`) | Materia submetida a apreciacao do Plenario. Raiz de agregado. |
| Tipo de Proposicao (`TipoProposicao`) | Especie da materia (PLO, PLC, EmendaALOM, PDL, PR, Requerimento, Indicacao, Mocao). VO/enum. |
| Projeto de Lei Ordinaria (`ProjetoDeLeiOrdinaria` / `PLO`) | Norma de iniciativa legislativa; deliberacao por maioria simples. |
| Projeto de Lei Complementar (`ProjetoDeLeiComplementar` / `PLC`) | Norma de iniciativa legislativa; deliberacao por maioria absoluta. |
| Emenda a LOM (`EmendaALOM`) | Proposta de alteracao da Lei Organica; rito qualificado (2/3 em dois turnos). |
| Projeto de Decreto Legislativo (`ProjetoDeDecretoLegislativo` / `PDL`) | Materia de competencia exclusiva da Camara. |
| Projeto de Resolucao (`ProjetoDeResolucao` / `PR`) | Regula assuntos internos (ex.: Regimento Interno). |
| Requerimento / Indicacao / Mocao (`Requerimento` / `Indicacao` / `Mocao`) | Instrumentos de manifestacao parlamentar. |
| Ementa (`Ementa`) | Resumo do objeto da proposicao. VO. |
| Autoria (`Autoria`) | Autor(es) da proposicao (iniciativa). VO. |
| Regime de Tramitacao (`RegimeTramitacao`) | Rito aplicavel (ordinario, urgencia, prioridade, qualificado). VO. |
| Emenda (`Emenda` / `ApresentarEmenda`) | Modificacao pontual a proposicao em curso. Entidade do agregado. |
| Substitutivo (`Substitutivo` / `ApresentarSubstitutivo`) | Modificacao integral a proposicao em curso. Entidade do agregado. |
| Tramitacao (`Tramitacao` / `Tramitar`) | Sequencia de fases percorridas pela proposicao. Entidade (fases) do agregado. |
| Parecer (`Parecer` / `RegistrarParecer`) | Manifestacao de Comissao sobre a materia (referenciado pela tramitacao). |
| Comissao de Constituicao e Justica (`Ccj`) | Comissao que emite parecer de constitucionalidade (obrigatorio). |
| Comissao de Financas e Orcamento (`FinancasOrcamento`) | Comissao que emite parecer financeiro/orcamentario (obrigatorio). |
| Ordem do Dia (`OrdemDoDia` / `IncluirEmOrdemDoDia`) | Parte da sessao destinada a deliberacao. |
| Autografo (`Autografo` / `GerarAutografo`) | Texto final aprovado, enviado ao Executivo (assinado ICP-Brasil). |
| Sancao (`Sancao`) | Ato do prefeito de aprovacao do autografo (expressa ou tacita). |
| Veto (`Veto`) | Ato do prefeito de rejeicao total/parcial do autografo. |
| Promulgacao (`Promulgacao`) | Ato de aperfeicoamento da lei apos sancao ou derrubada de veto. |
| Distribuir (`Distribuir`) | Encaminhar a proposicao as Comissoes competentes para instrucao. |
| Aprovar / Rejeitar / Arquivar (`Aprovar` / `Rejeitar` / `Arquivar`) | Resultados deliberativos/terminais da tramitacao. |
| Tenant (`TenantId`) | Ente publico (Camara Municipal) dono do registro. |
| Situacao (`Situacao` : `SituacaoProposicao`) | Estado atual da proposicao no ciclo de tramitacao. |

---

## 2. Modelo

- **Identidade:** `ProposicaoId` — `readonly record struct ProposicaoId(Guid Value)`; fabrica `ProposicaoId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `Proposicao : AggregateRoot<ProposicaoId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parametros para o EF; um completo). Nasce valida via factory `Apresentar(...)`.

### Propriedades

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (Camara) dono do registro. | `private set` |
| `Tipo` | `TipoProposicao` (VO/enum) | Especie da materia (PLO, PLC, EmendaALOM, PDL, PR, etc.). | `private set` |
| `Ementa` | `Ementa` (VO) | Resumo do objeto. | `private set` |
| `Autoria` | `Autoria` (VO) | Autor(es) / iniciativa. | `private set` |
| `Regime` | `RegimeTramitacao` (VO/enum) | Regime de tramitacao (ordinario, urgencia, qualificado). | `private set` |
| `Protocolo` | `string` | Numero de protocolo da proposicao no tenant. | `private set` |
| `DataApresentacao` | `DateOnly` | Data de apresentacao/protocolo. | `private set` |
| `Situacao` | `SituacaoProposicao` | Situacao atual. | `private set` |
| `Emendas` | `IReadOnlyList<Emenda>` | Emendas apresentadas. | colecao encapsulada |
| `Substitutivos` | `IReadOnlyList<Substitutivo>` | Substitutivos apresentados. | colecao encapsulada |
| `Tramitacoes` | `IReadOnlyList<Tramitacao>` | Fases de tramitacao (incl. pareceres referenciados). | colecao encapsulada |
| `NumeroAutografo` | `string?` | Numero do autografo, quando gerado (nulo antes). | `private set` |

### Maioria exigida por tipo (constantes de dominio)

- `PLO` (lei ordinaria): **maioria simples** (> 50% dos presentes).
- `PLC` / Regimento (Resolucao com efeito de norma interna) / derrubada de veto: **maioria absoluta** (> 50% dos membros).
- `EmendaALOM`: **maioria qualificada 2/3 em dois turnos** (CF/88 art. 29).

### Entidades internas

- **`Emenda`** — `EmendaId`, texto, autoria, data. Vinculada a `ProposicaoId`.
- **`Substitutivo`** — `SubstitutivoId`, texto, autoria, data. Vinculado a `ProposicaoId`.
- **`Tramitacao`** — `TramitacaoId`, fase, comissao (quando aplicavel), `Parecer` referenciado (orgao, sentido favoravel/contrario), data. Vinculada a `ProposicaoId`. Trilha **imutavel**.

### Value Objects (referenciados)

- `Ementa` — texto do resumo; nao vazio (validado em `Apresentar`).
- `Autoria` — descreve a iniciativa; nao vazia.
- `RegimeTramitacao` — regime; default ordinario.

### Enum `TipoProposicao`

| Valor | Numerico | Descricao |
|---|---|---|
| `ProjetoDeLeiOrdinaria` | 1 | PLO — lei ordinaria. |
| `ProjetoDeLeiComplementar` | 2 | PLC — lei complementar. |
| `EmendaALOM` | 3 | Emenda a Lei Organica Municipal (rito qualificado). |
| `ProjetoDeDecretoLegislativo` | 4 | PDL — competencia exclusiva da Camara. |
| `ProjetoDeResolucao` | 5 | PR — assuntos internos (ex.: Regimento). |
| `Requerimento` | 6 | Manifestacao parlamentar (requerimento). |
| `Indicacao` | 7 | Manifestacao parlamentar (indicacao). |
| `Mocao` | 8 | Manifestacao parlamentar (mocao). |

### Enum `SituacaoProposicao`

| Valor | Numerico | Descricao |
|---|---|---|
| `Apresentada` | 1 | Protocolada/apresentada (estado inicial). |
| `Distribuida` | 2 | Distribuida as Comissoes para instrucao. |
| `EmOrdemDoDia` | 3 | Incluida em Ordem do Dia para deliberacao. |
| `Aprovada` | 4 | Aprovada em Plenario. |
| `Rejeitada` | 5 | Rejeitada em Plenario (terminal). |
| `Arquivada` | 6 | Arquivada (terminal). |
| `AutografoEnviado` | 7 | Autografo gerado e enviado ao Executivo. |

> **Conjuntos de referencia usados nas guardas:**
> - **Encerrada/terminal** = { `Rejeitada`, `Arquivada` }.
> - **Pareceres obrigatorios** (para `IncluirEmOrdemDoDia`) = { parecer da `Ccj` (constitucionalidade), parecer de `FinancasOrcamento` }.
> - **Em tramitacao** = qualquer situacao que **nao** seja terminal nem `AutografoEnviado`.

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** Na apresentacao, `Ementa` e `Autoria` sao obrigatorias (`ArgumentNullException`/`ArgumentException`); a situacao inicial e `Apresentada` e e emitido `ProposicaoApresentada(id, tipo, autoria)`.
- **I-2.** O `Tipo` (`TipoProposicao`) e obrigatorio e imutavel apos a apresentacao (define a maioria exigida na deliberacao).
- **I-3.** A distribuicao (`Distribuir`) so ocorre a partir de `Apresentada`; passa a `Distribuida` e emite `ProposicaoDistribuida`. Caso contrario, `InvalidOperationException`.
- **I-4.** A apresentacao de `Emenda` ou `Substitutivo` so e permitida enquanto a proposicao estiver **em tramitacao** (nao terminal e nao `AutografoEnviado`); emite `EmendaApresentada`.
- **I-5.** O registro de `Parecer` (via `Tramitacao`) so ocorre enquanto em tramitacao; emite `ParecerEmitido`. A trilha de pareceres e **imutavel** (append-only).
- **I-6.** A inclusao em Ordem do Dia (`IncluirEmOrdemDoDia`) exige **pareceres obrigatorios** presentes: parecer da `Ccj` (constitucionalidade) **e** parecer de `FinancasOrcamento`. Ausencia de qualquer um **vicia a tramitacao** e a inclusao e rejeitada (`InvalidOperationException` — vicio de tramitacao).
- **I-7.** A inclusao em Ordem do Dia exige situacao `Distribuida` (instruida); passa a `EmOrdemDoDia`.
- **I-8.** A aprovacao (`Aprovar`) exige situacao `EmOrdemDoDia` e resultado deliberativo conforme a **maioria exigida pelo tipo** (simples p/ PLO; absoluta p/ PLC, Regimento e derrubada de veto; 2/3 p/ EmendaALOM); passa a `Aprovada` e emite `ProposicaoAprovada`.
- **I-8.1 (L-1).** Materia de rito qualificado (`EmendaALOM`) so se aprova apos **DOIS turnos** favoraveis (CF/88 art. 29, *caput*; simetria do art. 60 §2º), cada qual com a **maioria qualificada (2/3)**, em **datas/sessoes distintas** e respeitado o **intersticio minimo** entre eles (parametrizavel por tenant — Regimento Interno). O `turno` (1 ou 2) e consumido por `Aprovar` e materializado em `AprovacoesTurno`; o 2o turno fora de sequencia, sem o 1o, ou abaixo do intersticio e recusado. Concluido o 1o turno (sem o 2o) a materia **permanece em `EmOrdemDoDia`** e emite `TurnoAprovado`; so o ultimo turno exigido transita a `Aprovada` e emite `ProposicaoAprovada`. Materias de turno unico aprovam no 1o (e unico) turno.
- **I-9.** A rejeicao (`Rejeitar`) exige situacao `EmOrdemDoDia`; passa a `Rejeitada` (terminal) e emite `ProposicaoRejeitada`.
- **I-10.** A geracao de autografo (`GerarAutografo`) exige situacao `Aprovada` e `numeroAutografo` nao vazio; grava `NumeroAutografo`, passa a `AutografoEnviado` e emite `AutografoEnviado`. O autografo e assinado por ICP-Brasil (efeito de integracao).
- **I-11.** O arquivamento (`Arquivar`) e permitido sobre proposicao **nao terminal**; ocorre ao fim da legislatura (salvo excecoes regimentais); passa a `Arquivada` e emite `ProposicaoArquivada`.
- **I-12.** Estados terminais (`Rejeitada`, `Arquivada`) e `AutografoEnviado` nao admitem novas transicoes de tramitacao.
- **I-13.** A `Sancao` ou o `Veto` recebidos do Executivo (Integration Events consumidos) so se aplicam a proposicoes em situacao `AutografoEnviado`, atualizando a `Tramitacao`; veto derrubavel por **maioria absoluta**.
- **I-14.** Toda mutacao de situacao registra uma `Tramitacao` (fase + data) na trilha imutavel (prova juridica + LAI).

---

## 4. Maquina de Estados

Tabela: Estado origem → comando/metodo → Estado destino | guarda | evento emitido.

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Apresentar` | `Apresentada` | `Ementa` e `Autoria` validas; `Tipo` informado | `ProposicaoApresentada` |
| `Apresentada` | `Distribuir` | `Distribuida` | situacao == `Apresentada` | `ProposicaoDistribuida` |
| em tramitacao (≠ terminal/≠ `AutografoEnviado`) | `ApresentarEmenda` | (inalterada) | em tramitacao | `EmendaApresentada` |
| em tramitacao | `ApresentarSubstitutivo` | (inalterada) | em tramitacao | `EmendaApresentada` |
| em tramitacao | `RegistrarParecer` | (inalterada) | em tramitacao | `ParecerEmitido` |
| `Distribuida` | `IncluirEmOrdemDoDia` | `EmOrdemDoDia` | pareceres da `Ccj` **e** `FinancasOrcamento` presentes | — |
| `EmOrdemDoDia` | `Aprovar` | `Aprovada` | maioria exigida pelo `Tipo` atingida | `ProposicaoAprovada` |
| `EmOrdemDoDia` | `Rejeitar` | `Rejeitada` | situacao == `EmOrdemDoDia` | `ProposicaoRejeitada` |
| `Aprovada` | `GerarAutografo` | `AutografoEnviado` | `numeroAutografo` nao vazio | `AutografoEnviado` |
| ≠ terminal | `Arquivar` | `Arquivada` | situacao ∉ {`Rejeitada`,`Arquivada`} | `ProposicaoArquivada` |

> Observacoes:
> - `IncluirEmOrdemDoDia` chama a verificacao de pareceres obrigatorios **antes** da transicao; falta de parecer da CCJ ou de Financas => `InvalidOperationException` (vicio de tramitacao — Cenario 3).
> - A maioria exigida em `Aprovar` deriva do `Tipo` (ver secao 2); a apuracao concreta e responsabilidade do agregado `Votacao`, cujo `Resultado` alimenta `Aprovar`/`Rejeitar`.
> - `GerarAutografo` produz o `AutografoEnviado` que e publicado ao Executivo via Outbox (Integration Event).

---

## 5. Comandos (escrita)

### 5.1 ApresentarProposicao

- **Command:** `ApresentarProposicaoCommand(int Tipo, string Ementa, string Autoria, int Regime) : ICommand<Guid>`.
- **Entrada (DTO):** `Tipo` (`TipoProposicao`), `Ementa`, `Autoria`, `Regime` (`RegimeTramitacao`).
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pre-condicoes:** `request` nao nulo; `Ementa` e `Autoria` nao vazias; `Tipo` valido.
- **Efeito:** calcula `hoje`; cria via `Proposicao.Apresentar(tenant.TenantId, tipo, ementa, autoria, regime, hoje)`; `proposicoes.Adicionar(proposicao)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Proposicao` em situacao `Apresentada`; retorna `proposicao.Id.Value` (`Guid`).
- **Excecoes:** `ArgumentNullException` (request); `ArgumentException` (ementa/autoria vazias ou tipo invalido).
- **Evento de dominio:** `ProposicaoApresentada(id, tipo, autoria)`.

### 5.2 DistribuirProposicao

- **Command:** `DistribuirProposicaoCommand(Guid ProposicaoId) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; proposicao existe; situacao == `Apresentada` (I-3).
- **Efeito:** `proposicao.Distribuir()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Distribuida`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `Apresentada`).
- **Evento de dominio:** `ProposicaoDistribuida(Id)`.

### 5.3 ApresentarEmenda

- **Command:** `ApresentarEmendaCommand(Guid ProposicaoId, string Texto, string Autoria) : ICommand<Guid>`.
- **Entrada (DTO):** `ProposicaoId`, `Texto`, `Autoria`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; proposicao existe; **em tramitacao** (I-4); `Texto` nao vazio.
- **Efeito:** `proposicao.ApresentarEmenda(texto, autoria)`; `SaveChangesAsync`.
- **Pos-condicoes:** nova `Emenda` vinculada; situacao inalterada.
- **Excecoes:** `ArgumentNullException`; `ArgumentException` (texto vazio); `InvalidOperationException` (nao encontrada ou nao em tramitacao).
- **Evento de dominio:** `EmendaApresentada(Id, emendaId)`.

### 5.4 RegistrarParecer

- **Command:** `RegistrarParecerCommand(Guid ProposicaoId, string Comissao, bool Favoravel) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`, `Comissao` (ex.: `Ccj`, `FinancasOrcamento`), `Favoravel`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; proposicao existe; em tramitacao (I-5).
- **Efeito:** `proposicao.RegistrarParecer(comissao, favoravel)` (acrescenta `Tramitacao` com parecer); `SaveChangesAsync`.
- **Pos-condicoes:** parecer registrado na trilha imutavel.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou nao em tramitacao).
- **Evento de dominio:** `ParecerEmitido(Id, comissao, favoravel)`.

### 5.5 IncluirEmOrdemDoDia

- **Command:** `IncluirEmOrdemDoDiaCommand(Guid ProposicaoId) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; proposicao existe; situacao == `Distribuida` (I-7); pareceres obrigatorios da `Ccj` **e** `FinancasOrcamento` presentes (I-6).
- **Efeito:** `proposicao.IncluirEmOrdemDoDia()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `EmOrdemDoDia`.
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada, situacao invalida, ou **vicio de tramitacao** por ausencia de parecer obrigatorio).
- **Evento de dominio:** — (sem evento dedicado nesta versao).

### 5.6 AprovarProposicao

- **Command:** `AprovarProposicaoCommand(Guid ProposicaoId, Guid VotacaoId) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`, `VotacaoId` (votacao cujo `Resultado` aprova).
- **Dependencias do handler:** `IProposicaoRepository`, `IVotacaoRepository`, `IUnitOfWork`, `ILegislativoParametros` (intersticio entre turnos, parametrizavel por tenant), `TimeProvider`.
- **Pre-condicoes:** `request` nao nulo; proposicao e votacao existem; votacao pertence a esta proposicao, esta `Encerrada` e `Aprovada`; situacao == `EmOrdemDoDia`; resultado satisfaz a **maioria exigida pelo `Tipo`** em **cada turno** (I-8/I-8.1).
- **Efeito:** `proposicao.Aprovar(resultado, votacao.Turno, intersticio, hoje)`; `SaveChangesAsync`. Para `EmendaALOM`, so o 2o turno transita a `Aprovada`; o 1o emite `TurnoAprovado` e mantem `EmOrdemDoDia`.
- **Pos-condicoes:** situacao `Aprovada` (apos todos os turnos exigidos) ou `EmOrdemDoDia` (apos 1o turno de rito qualificado).
- **Excecoes:** `ArgumentNullException`; `ArgumentOutOfRangeException` (turno ≠ 1\|2); `InvalidOperationException` (nao encontrada, vinculo votacao→proposicao, situacao ≠ `EmOrdemDoDia`, maioria nao atingida, turno fora de sequencia, turno alem do exigido, ou intersticio nao observado).
- **Evento de dominio:** `ProposicaoAprovada(Id)`.

### 5.7 RejeitarProposicao

- **Command:** `RejeitarProposicaoCommand(Guid ProposicaoId, Guid VotacaoId) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`, `VotacaoId`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; proposicao existe; situacao == `EmOrdemDoDia` (I-9).
- **Efeito:** `proposicao.Rejeitar(resultado)`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Rejeitada` (terminal).
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou situacao ≠ `EmOrdemDoDia`).
- **Evento de dominio:** `ProposicaoRejeitada(Id)`.

### 5.8 GerarAutografo

- **Command:** `GerarAutografoCommand(Guid ProposicaoId, string NumeroAutografo) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`, `NumeroAutografo`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider` (para o Integration Event).
- **Pre-condicoes:** `request` nao nulo; proposicao existe; situacao == `Aprovada`; `NumeroAutografo` nao vazio (I-10).
- **Efeito:** `proposicao.GerarAutografo(request.NumeroAutografo)`; `SaveChangesAsync`; o evento de dominio `AutografoEnviado` mapeia para o **Integration Event** `AutografoEnviadoIntegrationEvent` (Outbox) destinado ao Executivo (autografo assinado ICP-Brasil).
- **Pos-condicoes:** `NumeroAutografo` preenchido; situacao `AutografoEnviado`; autografo publicado ao Executivo.
- **Excecoes:** `ArgumentNullException`; `ArgumentException` (numero vazio); `InvalidOperationException` (nao encontrada ou situacao ≠ `Aprovada`).
- **Evento de dominio:** `AutografoEnviado(Id, numeroAutografo)`.
- **Evento de integracao (publica):** `AutografoEnviadoIntegrationEvent`.

### 5.9 ArquivarProposicao

- **Command:** `ArquivarProposicaoCommand(Guid ProposicaoId) : ICommand`.
- **Entrada (DTO):** `ProposicaoId`.
- **Dependencias do handler:** `IProposicaoRepository`, `IUnitOfWork`.
- **Pre-condicoes:** `request` nao nulo; proposicao existe; situacao nao terminal (I-11).
- **Efeito:** `proposicao.Arquivar()`; `SaveChangesAsync`.
- **Pos-condicoes:** situacao `Arquivada` (terminal).
- **Excecoes:** `ArgumentNullException`; `InvalidOperationException` (nao encontrada ou ja terminal).
- **Evento de dominio:** `ProposicaoArquivada(Id)`.

---

## 6. Consultas (leitura)

### 6.1 ObterProposicaoPorId

- **Query:** `ObterProposicaoPorIdQuery(Guid ProposicaoId) : IQuery<ProposicaoDetalhe>`.
- **Entrada:** `ProposicaoId`.
- **Handler:** `ObterProposicaoPorIdHandler(IProposicaoRepository proposicoes)`; chama `proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), ct)`.
- **Projecao (DTO):** `ProposicaoDetalhe(Guid Id, string Tipo, string Ementa, string Autoria, string Regime, string Protocolo, DateOnly DataApresentacao, string Situacao, string? NumeroAutografo, IReadOnlyList<TramitacaoResumo> Tramitacoes)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarProposicoesPorSituacao

- **Query:** `ListarProposicoesPorSituacaoQuery(int Situacao) : IQuery<IReadOnlyList<ProposicaoResumo>>`.
- **Entrada:** `Situacao` (`SituacaoProposicao`).
- **Handler:** `ListarProposicoesPorSituacaoHandler(IProposicaoRepository proposicoes)`; chama `proposicoes.ListarPorSituacaoAsync(situacao, ct)`.
- **Projecao (DTO):** `ProposicaoResumo(Guid Id, string Tipo, string Ementa, string Situacao, DateOnly DataApresentacao)`.
- **Filtros:** por `Situacao`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pre-condicoes:** `request` nao nulo.

### 6.3 ObterTramitacaoDaProposicao

- **Query:** `ObterTramitacaoDaProposicaoQuery(Guid ProposicaoId) : IQuery<IReadOnlyList<TramitacaoResumo>>`.
- **Entrada:** `ProposicaoId`.
- **Handler:** `ObterTramitacaoDaProposicaoHandler(IProposicaoRepository proposicoes)`; le a trilha imutavel de fases/pareceres.
- **Projecao (DTO):** `TramitacaoResumo(Guid Id, string Fase, string? Comissao, bool? ParecerFavoravel, DateOnly Data)`.
- **Filtros:** por `ProposicaoId`; **sempre tenant-scoped**; alimenta transparencia (LAI/APIs abertas).
- **Pre-condicoes:** `request` nao nulo.

---

## 7. Eventos

### Dominio (in-process, MediatR; assembly `...Legislativo.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `ProposicaoApresentada` | `(ProposicaoId, TipoProposicao, Autoria)` | `Proposicao.Apresentar` (construtor) |
| `ProposicaoDistribuida` | `(ProposicaoId)` | `Proposicao.Distribuir` |
| `EmendaApresentada` | `(ProposicaoId, EmendaId)` | `Proposicao.ApresentarEmenda` / `ApresentarSubstitutivo` |
| `ParecerEmitido` | `(ProposicaoId, string comissao, bool favoravel)` | `Proposicao.RegistrarParecer` |
| `TurnoAprovado` | `(ProposicaoId, int Turno, int TurnosExigidos)` | `Proposicao.Aprovar` (turno intermediario de rito qualificado; materia ainda nao aprovada) |
| `ProposicaoAprovada` | `(ProposicaoId)` | `Proposicao.Aprovar` (apos todos os turnos exigidos) |
| `ProposicaoRejeitada` | `(ProposicaoId)` | `Proposicao.Rejeitar` |
| `ProposicaoArquivada` | `(ProposicaoId)` | `Proposicao.Arquivar` |
| `AutografoEnviado` | `(ProposicaoId, string numeroAutografo)` | `Proposicao.GerarAutografo` |

### Integracao (publica via `*.Contracts` + Outbox; assembly `...Legislativo.Contracts`)

| Evento | Payload | Publicado por |
|---|---|---|
| `AutografoEnviadoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ProposicaoId, string NumeroAutografo)` | `GerarAutografoHandler` (→ Executivo, para sancao/veto/promulgacao) |

### Integracao (consome)

| Evento | Payload | Efeito |
|---|---|---|
| `SancaoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ProposicaoId)` | atualiza a `Tramitacao` da `Proposicao` (sancao recebida do Executivo). |
| `VetoIntegrationEvent` | `(Guid EventId, DateTime OcorridoEmUtc, Guid TenantId, Guid ProposicaoId, string Motivo)` | atualiza a `Tramitacao`; habilita apreciacao de derrubada de veto por maioria absoluta. |

---

## 8. Validacoes (FluentValidation)

### ApresentarProposicaoValidator (`AbstractValidator<ApresentarProposicaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Tipo` | `IsInEnum()` | (padrao: tipo de proposicao invalido) |
| `Ementa` | `NotEmpty()` + `MaximumLength(1000)` | (padrao: ementa obrigatoria, max. 1000 caracteres) |
| `Autoria` | `NotEmpty()` + `MaximumLength(400)` | (padrao: autoria obrigatoria) |

### DistribuirProposicaoValidator / IncluirEmOrdemDoDiaValidator / AprovarProposicaoValidator / RejeitarProposicaoValidator / ArquivarProposicaoValidator

| Campo | Regra | Mensagem |
|---|---|---|
| `ProposicaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `VotacaoId` (Aprovar/Rejeitar) | `NotEmpty()` | (padrao: votacao obrigatoria para deliberacao) |

### ApresentarEmendaValidator (`AbstractValidator<ApresentarEmendaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProposicaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `Texto` | `NotEmpty()` + `MaximumLength(4000)` | (padrao: texto da emenda obrigatorio) |
| `Autoria` | `NotEmpty()` | (padrao: autoria obrigatoria) |

### RegistrarParecerValidator (`AbstractValidator<RegistrarParecerCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProposicaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `Comissao` | `NotEmpty()` | (padrao: comissao obrigatoria) |

### GerarAutografoValidator (`AbstractValidator<GerarAutografoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ProposicaoId` | `NotEmpty()` | (padrao: identificador obrigatorio) |
| `NumeroAutografo` | `NotEmpty()` + `MaximumLength(40)` | (padrao: numero do autografo obrigatorio, max. 40) |

> Consultas (`ObterProposicaoPorId`, `ListarProposicoesPorSituacao`, `ObterTramitacaoDaProposicao`) nao possuem validador dedicado; protecao por existencia no repositorio e tenant-scope.

---

## 9. Persistencia (EF Core 8)

- **Schema:** `legislativo` (isolado por modulo). **DbContext:** o do modulo Legislativo. **Migrations:** por modulo.
- **Tabela:** `Proposicao` (raiz de agregado); tabelas filhas `Emenda`, `Substitutivo`, `Tramitacao`.

| Coluna | Tipo logico | Conversor / observacao |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `ProposicaoId` ↔ `Guid` (Value Converter). |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Tipo` | `int` | enum `TipoProposicao` (persistido por valor numerico). |
| `Ementa` | `nvarchar(1000)` | owned/conversor de `Ementa`. |
| `Autoria` | `nvarchar(400)` | owned/conversor de `Autoria`. |
| `Regime` | `int` | enum `RegimeTramitacao`. |
| `Protocolo` | `nvarchar(40)` | numero de protocolo no tenant. |
| `DataApresentacao` | `date` | `DateOnly`. |
| `Situacao` | `int` | enum `SituacaoProposicao`. |
| `NumeroAutografo` | `nvarchar(40)?` | nulavel; preenchido em `GerarAutografo`. |

- **Tabela `Tramitacao`** (trilha imutavel, append-only): `Id` (PK), `ProposicaoId` (FK), `Fase` (`int`), `Comissao` (`nvarchar?`), `ParecerFavoravel` (`bit?`), `Data` (`date`).
- **Tabelas `Emenda` / `Substitutivo`:** `Id` (PK), `ProposicaoId` (FK), `Texto` (`nvarchar(4000)`), `Autoria` (`nvarchar(400)`), `Data` (`date`).
- **Indices:**
  - PK em `Id`.
  - Indice unico em `(TenantId, Protocolo)` (unicidade do protocolo por tenant).
  - Indice em `(TenantId, Situacao)` para `ListarProposicoesPorSituacao`.
  - Indices em `Tramitacao(ProposicaoId)`, `Emenda(ProposicaoId)`, `Substitutivo(ProposicaoId)`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no dominio).
- **Outbox:** tabela Outbox do contexto Legislativo para `AutografoEnviadoIntegrationEvent` (consistencia transacional com o estado).

---

## 10. Seguranca, Tenant e Auditoria

- **Tenant:** `Proposicao` implementa `IMustHaveTenant`. A **Camara e tenant DISTINTO do Executivo** (CNPJs distintos). `TenantId` carimbado na insercao pelo interceptor; **Global Query Filter** por `TenantId` em todas as consultas. Gravacao cross-tenant **lanca excecao**. `ITenantContext` resolvido do JWT por requisicao.
- **RBAC (policy-based + RBAC `Usuario → Departamento → Roles`, negar por padrao):**
  - Apresentar/emendar/distribuir: papel de **Vereador** ou **Secretaria Legislativa** (ex.: `Legislativo.Proposicao.Apresentar`).
  - Registrar parecer: papel de **Comissao** (ex.: `Legislativo.Proposicao.RegistrarParecer`).
  - Incluir em Ordem do Dia / aprovar / rejeitar / arquivar / gerar autografo: papel da **Mesa Diretora** (ex.: `Legislativo.Proposicao.Deliberar`).
  - Consultas: papel de **leitura legislativa** (ex.: `Legislativo.Proposicao.Ler`); transparencia publica via APIs abertas (LAI).
  - Modulo Legislativo e **ativavel por tenant**; requisicao a tenant sem licenca → 404/403 auditado.
- **Auditoria imutavel:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuario, IP, timestamp) em toda mutacao (`Apresentar`, `Distribuir`, `ApresentarEmenda`, `RegistrarParecer`, `IncluirEmOrdemDoDia`, `Aprovar`, `Rejeitar`, `GerarAutografo`, `Arquivar`). A trilha de **tramitacao e pareceres e imutavel (append-only)** para prova juridica e LAI — destinada ao Tribunal de Contas (TCE-RS).
- **LAI/Transparencia:** tramitacao e resultados sao dados publicos; projecoes minimizam dados pessoais conforme necessario.
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integracoes Governamentais

- **Saida — Executivo (intra-aplicacao, via Contracts):** ao gerar autografo, publica `AutografoEnviadoIntegrationEvent` (Outbox) para o modulo do Executivo conduzir sancao/veto/promulgacao. Idempotente por `EventId`. PDF do autografo assinado por **ICP-Brasil**.
- **Entrada — Executivo (consome):** `SancaoIntegrationEvent` e `VetoIntegrationEvent` atualizam a `Tramitacao` da `Proposicao` (sancao tacita em 15 dias uteis; veto derrubavel por maioria absoluta).
- **Publicacao de atos:** Diario Oficial do municipio / LeisMunicipais para autografo, leis sancionadas e atos legislativos.
- **SAPL/Interlegis:** interoperacao e importacao de tramitacao (ACL + idempotencia).
- **APIs abertas (LAI):** publica eventos de transparencia (proposicoes, tramitacao, resultados) para o portal.
- **Resiliencia:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explicito de erros atras de Anti-Corruption Layer.

---

## 12. Cenarios BDD

Cada cenario vira teste de integracao.

**Cenario 1 — Apresentacao de proposicao**
- **Dado** os dados de um PLO (tipo, ementa, autoria) validos
- **Quando** executo `ApresentarProposicaoCommand(...)`
- **Entao** e criada uma `Proposicao` em situacao `Apresentada` e o evento `ProposicaoApresentada` e emitido.

**Cenario 2 — Distribuicao as Comissoes**
- **Dado** uma proposicao `Apresentada`
- **Quando** executo `DistribuirProposicaoCommand(id)`
- **Entao** situacao = `Distribuida` e o evento `ProposicaoDistribuida` e emitido.

**Cenario 3 — Vicio por ausencia de parecer (README Cenario 3)**
- **Dado** um PLC `Distribuida` sem `Parecer` da CCJ
- **Quando** executo `IncluirEmOrdemDoDiaCommand(id)`
- **Entao** a inclusao e rejeitada por vicio de tramitacao (`InvalidOperationException`).

**Cenario 4 — Inclusao valida em Ordem do Dia**
- **Dado** uma proposicao `Distribuida` com pareceres da CCJ e de Financas presentes
- **Quando** executo `IncluirEmOrdemDoDiaCommand(id)`
- **Entao** situacao = `EmOrdemDoDia`.

**Cenario 5 — Aprovacao de lei ordinaria (README Cenario 2)**
- **Dado** um PLO em `EmOrdemDoDia` com pareceres favoraveis e uma votacao cujo `Resultado` aprova por maioria simples
- **Quando** executo `AprovarProposicaoCommand(id, votacaoId)`
- **Entao** situacao = `Aprovada` e o evento `ProposicaoAprovada` e emitido.

**Cenario 6 — Rejeicao**
- **Dado** uma proposicao `EmOrdemDoDia` com votacao cujo resultado rejeita
- **Quando** executo `RejeitarProposicaoCommand(id, votacaoId)`
- **Entao** situacao = `Rejeitada` e o evento `ProposicaoRejeitada` e emitido.

**Cenario 7 — Autografo ao Executivo (README Cenario 4)**
- **Dado** uma `Proposicao` `Aprovada`
- **Quando** executo `GerarAutografoCommand(id, "AUT-2026-0001")` (assinado ICP-Brasil)
- **Entao** situacao = `AutografoEnviado`, o evento `AutografoEnviado` e emitido e `AutografoEnviadoIntegrationEvent` e publicado ao Executivo via Outbox.

**Cenario 8 — Maioria qualificada para Emenda a LOM**
- **Dado** uma `EmendaALOM` em `EmOrdemDoDia`
- **Quando** a deliberacao nao alcanca 2/3 em dois turnos
- **Entao** `AprovarProposicaoCommand` falha por maioria nao atingida (`InvalidOperationException`).

**Cenario 9 — Maioria absoluta para PLC**
- **Dado** um `ProjetoDeLeiComplementar` em `EmOrdemDoDia`
- **Quando** a votacao alcanca maioria absoluta favoravel
- **Entao** situacao = `Aprovada`.

**Cenario 10 — Arquivamento ao fim da legislatura**
- **Dado** uma proposicao em tramitacao (nao terminal)
- **Quando** executo `ArquivarProposicaoCommand(id)`
- **Entao** situacao = `Arquivada` e o evento `ProposicaoArquivada` e emitido.

**Cenario 11 — Emenda em proposicao em tramitacao**
- **Dado** uma proposicao `Distribuida`
- **Quando** executo `ApresentarEmendaCommand(id, texto, autoria)`
- **Entao** uma `Emenda` e vinculada e o evento `EmendaApresentada` e emitido.

**Cenario 12 — Consumo de sancao do Executivo**
- **Dado** uma proposicao em `AutografoEnviado`
- **Quando** chega `SancaoIntegrationEvent(proposicaoId)`
- **Entao** a `Tramitacao` e atualizada registrando a sancao.

**Cenario 13 — Derrubada de veto (README Cenario 5)**
- **Dado** um `VetoIntegrationEvent` recebido do Executivo para a proposicao
- **Quando** ha deliberacao com maioria absoluta pela rejeicao do veto
- **Entao** a `Tramitacao` registra a derrubada e a materia segue a promulgacao.

**Cenario 14 — Consulta de tramitacao (transparencia)**
- **Dado** uma proposicao com varias fases registradas
- **Quando** executo `ObterTramitacaoDaProposicaoQuery(id)` no contexto do tenant
- **Entao** retorna a trilha imutavel de fases/pareceres, tenant-scoped.

---

## 13. Casos de Borda

- **B-1.** `Ementa` ou `Autoria` vazias em `Apresentar` ⇒ `ArgumentException`/validator (I-1).
- **B-2.** `Distribuir` sobre proposicao ja `Distribuida`/`EmOrdemDoDia` ⇒ `InvalidOperationException` (so a partir de `Apresentada`).
- **B-3.** `IncluirEmOrdemDoDia` com apenas o parecer da CCJ (sem o de Financas) ⇒ falha por vicio (I-6).
- **B-4.** `IncluirEmOrdemDoDia` com apenas o parecer de Financas (sem o da CCJ) ⇒ falha por vicio (I-6).
- **B-5.** `Aprovar` PLO com maioria simples nao atingida ⇒ `InvalidOperationException`.
- **B-6.** `Aprovar` PLC sem maioria absoluta ⇒ `InvalidOperationException`.
- **B-7.** `Aprovar` `EmendaALOM` sem 2/3 em dois turnos ⇒ `InvalidOperationException`.
- **B-8.** `GerarAutografo` sobre proposicao nao `Aprovada` ⇒ `InvalidOperationException`.
- **B-9.** `GerarAutografo` com `NumeroAutografo` vazio ⇒ `ArgumentException`/validator (I-10).
- **B-10.** `ApresentarEmenda`/`RegistrarParecer` sobre proposicao terminal (`Rejeitada`/`Arquivada`) ou `AutografoEnviado` ⇒ `InvalidOperationException` (nao em tramitacao).
- **B-11.** `Arquivar` proposicao ja terminal ⇒ `InvalidOperationException`.
- **B-12.** `SancaoIntegrationEvent`/`VetoIntegrationEvent` para proposicao que nao esta em `AutografoEnviado` ⇒ ignorado/auditado (I-13).
- **B-13.** Protocolo duplicado no mesmo tenant ⇒ violacao do indice unico `(TenantId, Protocolo)`.
- **B-14.** `AutografoEnviadoIntegrationEvent` deve ser idempotente no consumidor (Executivo) por `EventId` (reentrega via Outbox).
- **B-15.** Consultas em tenant da Camara nao retornam dados do tenant do Executivo (isolamento por Global Query Filter).

---

## 14. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versao inicial — derivada do README do modulo Legislativo (linguagem ubiqua, mapa de dominio, regras criticas, integracoes, cenarios BDD e fontes legais) para o agregado `Proposicao`. |

<!-- manifest
commands: ApresentarProposicao, DistribuirProposicao, ApresentarEmenda, RegistrarParecer, IncluirEmOrdemDoDia, AprovarProposicao, RejeitarProposicao, GerarAutografo, ArquivarProposicao
queries: ObterProposicaoPorId, ListarProposicoesPorSituacao, ObterTramitacaoDaProposicao
domainEvents: ProposicaoApresentada, ProposicaoDistribuida, EmendaApresentada, ParecerEmitido, TurnoAprovado, ProposicaoAprovada, ProposicaoRejeitada, ProposicaoArquivada, AutografoEnviado
integrationEventsPublished: AutografoEnviadoIntegrationEvent
integrationEventsConsumed: SancaoIntegrationEvent, VetoIntegrationEvent
-->
