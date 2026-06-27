# DESIGN — Planejamento Orçamentário (PPA / LDO / LOA) no módulo Finanças

> **Escopo:** design do ciclo de planejamento PPA/LDO/LOA — entidades ricas + casos de uso.
> **Objetivo:** fechar o GAP recorrente apontado pela avaliação independente — o módulo `Financas`
> modela só a **execução** (Dotação→Empenho→Liquidação→Pagamento + PCASP/MSC) e **não tem
> planejamento**. Este documento especifica os agregados **PPA**, **LDO**, **LOA** + **Créditos
> Adicionais** e o **vínculo** que faz a `DotacaoOrcamentaria` **nascer da LOA** (dotação inicial =
> despesa fixada), com a cadeia de compatibilidade **LOA ⊆ LDO ⊆ PPA** validada.
> **Fontes:** CF/88 art. 165/166/167 + ADCT 35; Lei 4.320/1964 (arts. 2-15, 40-46); LC 101/2000
> (LRF) arts. 4-5.
> Itens incertos marcados **[a confirmar]** (CONVENCOES-ENGENHARIA.md §8).

---

## 1. Contexto e princípios de design

Adere à `CONVENCOES-ENGENHARIA.md`: Clean Architecture + DDD tático, domínio rico (invariantes no
construtor/factory, construtor privado, `sealed`), multi-tenant (`IMustHaveTenant` + Global Query
Filter), auditoria imutável, NRT/warnings-as-errors, PT-BR sem acento nos identificadores,
parâmetros legais **por tenant** (nunca hardcoded). Cross-module **só via `*.Contracts` + Integration
Events**. Tudo no schema `financas` (mesmo `FinancasDbContext`).

**Decisão central de modelagem.** A `DotacaoOrcamentaria` (execução, já existente e correta — garante
"despesa ≤ crédito" em `ReservarEmpenho`) é **preservada intacta**. O planejamento **alimenta** a
dotação, não a substitui: cada `ItemDespesaFixada` da LOA, quando a LOA entra em vigor, **gera** uma
`DotacaoOrcamentaria` cujo `ValorDotadoInicial` = despesa fixada do item, com FK de origem rastreável
(`LoaId`, `ItemDespesaFixadaId`, `AcaoPpaId`). Os créditos adicionais passam a ser registrados como
agregado próprio `CreditoAdicional`, que dispara os já existentes `Dotacao.Reforcar` / `AnularCredito`.

**Reaproveitamento.** `ClassificacaoOrcamentaria` (VO: órgão/UO/funcional/categoria/fonte),
`ValorMonetario`, e o gancho contábil `FatoContabil.DotacaoAprovada` (roteiro `EVT-DOT`: D 5.2.2.1.01
Dotação / C 6.2.2.1.1 Crédito Disponível) já existem — a fixação da LOA e os créditos adicionais
reusam esse roteiro **sem nova contabilização ad-hoc**.

---

## 2. Agregado 1 — PlanoPlurianual (PPA, 4 anos)

**Aggregate Root:** `PlanoPlurianual : AggregateRoot<PpaId>, IMustHaveTenant`.
Contém **Programas**; cada Programa contém **Ações**; cada Ação contém **Metas** (físicas/financeiras
por ano). Toda a árvore é **um único agregado** (consistência transacional da compatibilidade interna).

```
PlanoPlurianual (root)              — Lei nº/ano, AnoInicio, AnoFim (=Inicio+3), Situacao
 ├─ Programa (entity)               — Codigo, Nome, Objetivo, PublicoAlvo, Indicador(linha-base/meta)
 │   └─ AcaoPpa (entity)            — Codigo, Nome, TipoAcao(Projeto|Atividade|OperacaoEspecial),
 │       │                            FuncionalProgramatica(VO p/ ligar à classificação), Produto, UnidadeMedida
 │       └─ MetaAcao (VO/entity)    — Ano(do quadriênio), MetaFisica(qtd), MetaFinanceira(ValorMonetario), Regiao
 └─ (Situacao: Elaboracao → EmTramitacao → Vigente → Encerrado | Revisado)
```

**Identidades fortes:** `PpaId`, `ProgramaId`, `AcaoPpaId`, `MetaAcaoId` (`readonly record struct` com
`New()`), no padrão de `DotacaoOrcamentariaId`.

**Invariantes (no domínio):**
- `AnoFim == AnoInicio + 3`; quadriênio **defasado** do mandato (2º ano do mandato → 1º do seguinte) —
  validado contra parâmetro do tenant (não hardcoded). [a confirmar] janela exata via LOM de
  Maximiliano de Almeida/RS.
- Código de Programa/Ação único dentro do PPA; `MetaAcao.Ano ∈ [AnoInicio, AnoFim]`.
- Soma das `MetaFinanceira` por ação/ano ≥ 0; programa Vigente exige ≥ 1 ação com ≥ 1 meta.
- Transições de `Situacao` só nos sentidos válidos (máquina de estados); só `Vigente` é referenciável
  por LDO/LOA. `Revisar()` cria nova versão preservando trilha (revisão de PPA é lei própria).

**Métodos ricos:** `Criar(...)`, `AdicionarPrograma(...)`, `AdicionarAcao(programaId, ...)`,
`DefinirMeta(acaoId, ano, fisica, financeira, regiao)`, `ColocarEmTramitacao()`, `Vigorar(lei)`,
`Encerrar()`, e a **query de domínio** `ContemAcaoVigente(AcaoPpaId) : bool` usada pela validação de
compatibilidade da LOA.

---

## 3. Agregado 2 — LeiDiretrizes (LDO, anual — o "elo")

**Aggregate Root:** `LeiDiretrizes : AggregateRoot<LdoId>, IMustHaveTenant`.

```
LeiDiretrizes (root)               — Lei nº/ano, Exercicio, PpaId(FK), Situacao
 ├─ PrioridadeLdo (entity)         — AcaoPpaId(FK p/ ação do PPA), Ordem, Justificativa
 ├─ MetaFiscal (entity)            — Exercicio + 2 seguintes (visão trienal LRF art.4º §1º):
 │                                    ReceitaTotal, DespesaTotal, ResultadoPrimario, ResultadoNominal, DividaConsolidada
 │                                    (valores correntes e constantes)
 └─ AnexoLdo (entity)              — Tipo(AnexoMetasFiscais|AnexoRiscosFiscais), Conteudo/refDocumento, Obrigatorio(bool)
```

**Vínculo / invariantes:**
- `PpaId` aponta para um PPA **Vigente** cujo quadriênio cobre o `Exercicio` da LDO.
- Toda `PrioridadeLdo.AcaoPpaId` **deve existir e estar vigente** no PPA referenciado
  (`ppa.ContemAcaoVigente`) — implementa CF 165 §2º (LDO seleciona prioridades do PPA).
- `MetaFiscal` cobre o exercício + 2 seguintes (LRF art. 4º §1º).
- **Obrigatoriedade dos anexos AMF/ARF parametrizável por tenant** (`OpcoesPlanejamentoLrf` via
  `IOptions`/config do tenant) — **[a confirmar]** posição vigente do TCE-RS para município < 50 mil
  hab. (porte do piloto). Default conservador: **obrigatórios**.
- Estados: `Elaboracao → EmTramitacao → Vigente → Encerrada`. Só `Vigente` habilita LOA do exercício.

**Métodos:** `Criar(exercicio, ppaId, ...)`, `PriorizarAcao(acaoPpaId, ordem, justificativa)`,
`DefinirMetaFiscal(...)`, `AnexarAmf(...)`, `AnexarArf(...)`, `Vigorar(lei)`, `Encerrar()`,
`ContemPrioridade(AcaoPpaId) : bool`.

---

## 4. Agregado 3 — LeiOrcamentariaAnual (LOA, 1 exercício)

**Aggregate Root:** `LeiOrcamentariaAnual : AggregateRoot<LoaId>, IMustHaveTenant`. Estima a receita
e **fixa a despesa** (Lei 4.320 art. 2º). O QDD é a coleção de itens de despesa fixada (o "nível em que
a dotação nasce").

```
LeiOrcamentariaAnual (root)        — Lei nº/ano, Exercicio, LdoId(FK), PpaId(FK), Situacao,
 │                                    LimiteSuplementacaoPercentual (% autorizado p/ créditos por decreto — art.7º/167 V)
 ├─ ReceitaPrevista (entity)       — NaturezaReceita, CategoriaEconomica(Corrente|Capital), Origem/Especie/Rubrica, FonteDeRecurso, ValorPrevisto
 └─ ItemDespesaFixada (entity)     — Classificacao(ClassificacaoOrcamentaria VO), AcaoPpaId(FK), NaturezaDespesa(elemento),
        ≡ QDD                        ValorFixado, e (após vigência) DotacaoId(FK 1:1 gerada na execução)
 └─ (Situacao: ProjetoLei → EmTramitacao → Aprovada → EmExecucao → Encerrada)
```

**Invariantes de compatibilidade (o coração do GAP — "provar, não documentar"):**
1. **LOA ⊆ LDO ⊆ PPA:** todo `ItemDespesaFixada.AcaoPpaId` deve (a) existir/vigente no PPA
   (`ppa.ContemAcaoVigente`) **e** (b) estar priorizado na LDO do exercício
   (`ldo.ContemPrioridade`) — CF 167, I e §1º + CF 165 §2º. Validado em `Aprovar()`.
2. **Equilíbrio:** Σ `ReceitaPrevista.ValorPrevisto` ≥ Σ `ItemDespesaFixada.ValorFixado`
   (princípio do equilíbrio, Lei 4.320 art. 2º; compatível com AMF da LDO — LRF art. 4º).
   Tolerância/reserva de contingência parametrizável (LRF art. 5º III). [a confirmar] % RCL da reserva.
3. **Exercício** da LOA == exercício da LDO == dentro do quadriênio do PPA.
4. `LimiteSuplementacaoPercentual` define o teto de créditos suplementares por decreto (art. 7º /
   CF 167 V) — usado pelo agregado `CreditoAdicional`.

**Máquina de estados e o ponto de costura LOA→Dotação:**
`ProjetoLei → EmTramitacao → Aprovada → EmExecucao → Encerrada`.
- `Aprovar()` roda a validação de compatibilidade (1-3) e exige LDO/PPA vigentes; falha → exceção de
  domínio `IncompatibilidadeOrcamentariaException` (lista os itens infratores).
- `EntrarEmExecucao()` (no virar do exercício / publicação): para **cada** `ItemDespesaFixada`,
  publica um domain event `ItemLoaEntrouEmExecucao` que o handler converte em **uma**
  `DotacaoOrcamentaria.Criar(...)` (ver §6). Idempotente: item já com `DotacaoId` não recria.

**Métodos:** `Criar(exercicio, ldoId, ppaId, limiteSupl)`, `PreverReceita(...)`, `FixarDespesa(classificacao,
acaoPpaId, natureza, valor)`, `Aprovar(ICompatibilidadeOrcamentariaService)`, `EntrarEmExecucao()`,
`Encerrar()`, `RegistrarDotacaoGerada(itemId, dotacaoId)` (fecha o elo 1:1).

---

## 5. Agregado 4 — CreditoAdicional (o que ALTERA a LOA — Lei 4.320 arts. 40-46)

**Aggregate Root:** `CreditoAdicional : AggregateRoot<CreditoAdicionalId>, IMustHaveTenant`.

| Campo | Conteúdo |
|---|---|
| `LoaId` (FK) | LOA alterada |
| `Especie` | `Suplementar` \| `Especial` \| `Extraordinario` (art. 41) |
| `AtoAutorizador` | lei/autorização (na LOA p/ suplementar; lei específica p/ especial; dispensado no extraordinário) |
| `AtoAbertura` | decreto/MP de abertura (art. 42-44) |
| `FonteRecurso` | `SuperavitFinanceiro` \| `ExcessoArrecadacao` \| `AnulacaoDotacao` \| `OperacaoCredito` (art. 43 §1º; dispensada no extraordinário) |
| `Valor`, `DotacaoAlvoId`, `DotacaoAnuladaId?` | dotação reforçada/criada e (se anulação) a anulada |

**Invariantes:** `Suplementar` exige `DotacaoAlvoId` preexistente; `Especial` **cria** dotação nova
(gera `ItemDespesaFixada` extraordinário + `DotacaoOrcamentaria.Criar`); anulação não pode tornar saldo
negativo (já garantido em `Dotacao.AnularCredito`); soma de suplementares por decreto **≤
`Loa.LimiteSuplementacaoPercentual`** sobre o total fixado (art. 7º / CF 167 V); extraordinário só
calamidade/urgência (CF 167 §3º), dá ciência ao Legislativo.

**Efeito (em `Abrir()`):** dispara `Dotacao.Reforcar(valor)` no alvo e, se houver fonte por anulação,
`Dotacao.AnularCredito(valor)` na origem — reusando o roteiro contábil `EVT-DOT` (`FatoContabil.DotacaoAprovada`).
Trilha imutável: cada alteração da LOA é um fato com ato legal (auditoria, CONVENCOES-ENGENHARIA.md §1/§6).

---

## 6. O VÍNCULO: a Dotação nasce da LOA (sem quebrar a execução existente)

**Mudança na `DotacaoOrcamentaria`** (aditiva, retrocompatível):
- Novos campos opcionais: `LoaId? `, `ItemDespesaFixadaId?`, `AcaoPpaId?` (origem rastreável).
- Nova factory `DotacaoOrcamentaria.CriarDeLoa(tenantId, exercicio, classificacao, valorFixado,
  loaId, itemId, acaoPpaId)` — idêntica à atual, gravando a origem. A `Criar(...)` legada **permanece**
  (compat. com dados/migração; mas o caminho de produção passa a ser `CriarDeLoa`).
- Comentário `ValorDotadoInicial` ("Dotação aprovada na LOA") passa a ser **verdade rastreável**.

**Fluxo de costura (handler em `Application/Integracoes`):**
`Loa.EntrarEmExecucao()` → domain event `ItemLoaEntrouEmExecucao(loaId, itemId, classificacao,
valorFixado, acaoPpaId)` → `GerarDotacaoDeItemLoaHandler` cria a `DotacaoOrcamentaria.CriarDeLoa(...)`,
persiste, e chama `loa.RegistrarDotacaoGerada(itemId, dotacaoId)`. Idempotência por `itemId`.

**Costura cross-tenant (Executivo × Câmara) — Integration Event:** a **votação/aprovação** é fato do
tenant **Câmara** (módulo Legislativo). Novo contrato em
`Modules.Legislativo.Contracts/LoaAprovadaIntegrationEvent.cs` (EventId, OccurredOnUtc, TenantId,
ProposicaoId, NumeroLei, Exercicio) publicado via **Outbox**; consumido por handler em Finanças que
move a LOA para `Aprovada`. **[a confirmar]** contrato exato. Hoje, sem Legislativo licenciado, a
aprovação é manual via endpoint (RBAC restrito), mantendo o mesmo método de domínio.

**Contabilidade:** nenhuma rota nova — `Dotacao.Criar*` já levanta `DotacaoCriada`/`CreditoReforcado`,
roteados por `FatoContabil.DotacaoAprovada` (D 5.2.2.1.01 / C 6.2.2.1.1). RREO/Balanço Orçamentário
agora têm o "previsto" fiel (LOA + créditos), fechando a lacuna do ciclo orçamentário.

---

## 7. Persistência (EF Core 8, schema `financas`)

Um `IEntityTypeConfiguration<>` por agregado, no padrão de `DotacaoOrcamentariaConfiguration`:
- IDs fortes via `HasConversion`; `ValorMonetario`/`ClassificacaoOrcamentaria` como
  `OwnsOne`/converters; coleções-filhas (`Programa→Acao→Meta`, `Receita/Item`) via `OwnsMany` ou
  entidades com FK + `HasMany`/`WithOwner` conforme cardinalidade do agregado.
- DbSets novos no `FinancasDbContext`: `Ppas`, `Ldos`, `Loas`, `CreditosAdicionais`.
- `HasIndex(TenantId, Exercicio)` em LDO/LOA; `(TenantId, AnoInicio)` no PPA; FK
  `ItemDespesaFixada→DotacaoId` única.
- **Uma migration nova** `..._Planejamento` (PPA/LDO/LOA/Crédito + colunas de origem em `Dotacoes`).
  Repositórios novos: `IPpaRepository`, `ILdoRepository`, `ILoaRepository`, `ICreditoAdicionalRepository`.
- Global Query Filter por `TenantId` herdado do `ModuleDbContext` (nada a desabilitar).

---

## 8. Handlers / Endpoints / RBAC

**Commands (MediatR, pipeline Validation→Logging→UoW→Idempotency):**
PPA: `CriarPpa`, `AdicionarPrograma`, `AdicionarAcao`, `DefinirMeta`, `TramitarPpa`, `VigorarPpa`.
LDO: `CriarLdo`, `PriorizarAcao`, `DefinirMetaFiscal`, `AnexarAmf/Arf`, `VigorarLdo`.
LOA: `CriarLoa`, `PreverReceita`, `FixarDespesa`, `AprovarLoa`, `ColocarLoaEmExecucao`, `EncerrarLoa`.
Crédito: `AbrirCreditoAdicional`. Queries: `ConsultarPpa/Ldo/Loa`, `ListarItensQdd`,
`ConsultarCompatibilidade(loaId)` (relatório dos itens fora de PPA/LDO).

Serviço de domínio `ICompatibilidadeOrcamentariaService` injeta os 3 repositórios e roda a checagem
LOA⊆LDO⊆PPA (Application; lê só `*.Domain` próprio — sem violar isolamento).

**Endpoints** (`FinancasEndpoints`, grupo `/api/financas`, padrão existente):
`/planejamento/ppa[...]`, `/planejamento/ldo[...]`, `/planejamento/loa[...]`,
`/planejamento/creditos-adicionais`. Leituras → `financas.ver`; mutações de planejamento → **nova
permission `financas.planejar`** (separa o ato de planejar do `financas.gerenciar` da execução —
segregação de funções, exigência de controle interno/TCE). `AprovarLoa`/`AbrirCreditoAdicional` →
`financas.planejar` (e, idealmente, alçada distinta — [a confirmar] com RBAC do tenant).

---

## 9. Ordem de implementação (entregas)

- **P1 — PPA (base da cadeia):** agregado `PlanoPlurianual` (Programa/Ação/Meta) + config EF +
  migration parcial + repo + commands/queries + endpoints + BDD. Sem ele nada referencia.
- **P2 — LDO:** `LeiDiretrizes` (Prioridade/MetaFiscal/Anexos) + FK→PPA + validação
  `ContemAcaoVigente` + anexos LRF parametrizáveis + endpoints/BDD.
- **P3 — LOA + QDD:** `LeiOrcamentariaAnual` (Receita/ItemDespesaFixada) +
  `ICompatibilidadeOrcamentariaService` (LOA⊆LDO⊆PPA + equilíbrio) + `Aprovar()`/`EntrarEmExecucao()`.
- **P4 — Vínculo execução:** campos de origem em `DotacaoOrcamentaria` + `CriarDeLoa` +
  `GerarDotacaoDeItemLoaHandler` (domain event → dotação, idempotente) + testes de "dotação nasce da LOA".
- **P5 — Créditos adicionais:** `CreditoAdicional` (3 espécies, fontes, limite de suplementação) →
  `Reforcar`/`AnularCredito` + ato legal/auditoria + BDD de invariantes.
- **P6 — Costura cross-tenant:** `LoaAprovadaIntegrationEvent` (Legislativo.Contracts) + handler de
  consumo via Outbox [a confirmar contrato]; endpoint manual de aprovação como fallback.

Cada entrega: spec BDD `.md` **antes** do código (CONVENCOES-ENGENHARIA.md §1/§12), testes das invariantes
críticas (compatibilidade, equilíbrio, isolamento de tenant) e NetArchTest verde.

---

## 10. Pendências [a confirmar] (CONVENCOES-ENGENHARIA.md §8)

- Obrigatoriedade vigente de AMF/ARF p/ município < 50 mil hab. — posição **TCE-RS**.
- Datas exatas de envio/devolução de PPA/LDO/LOA — **LOM de Maximiliano de Almeida/RS** (parametrizável).
- Contrato do `LoaAprovadaIntegrationEvent` (Legislativo ↔ Finanças).
- % limite de suplementação por decreto e % reserva de contingência (RCL) — vêm da LOA aprovada/tenant.
- Tabela vigente de funções/subfunções e natureza de receita/despesa (STN) p/ validar a
  funcional-programática hoje gravada como string livre.
```
