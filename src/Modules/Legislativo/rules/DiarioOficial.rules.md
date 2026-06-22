# Diario Oficial Eletronico — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). Gap G2: veiculo oficial de publicacao. Edicoes agrupam
> materias (normas, atas, editais, portarias, extratos); a publicacao e o marco legal de eficacia dos
> atos e fica disponivel para consulta publica (LAI).
> Autoridade: `docs/architecture/legislativo-sprint/DESIGN-GAPS.md` (G2).
> Constituicao: isolamento de modulo (relacao com Transparencia SO via Contracts), multi-tenant,
> auditoria imutavel, dominio rico, banco-por-tenant.

---

## 1. Linguagem ubiqua

- **Edicao do Diario:** "Edicao n. X, de DD/MM/AAAA" — numero sequencial por (tenant, ano).
- **Materia:** ato publicado (`Norma`, `AtaSessao`, `Edital`, `Portaria`, `Extrato`, `Outro`), com
  conteudo bruto ou referencia a entidade de origem (mesmo modulo).
- **Publicacao:** marco legal de eficacia (data/hora oficial imutavel).
- **Retificacao:** correcao pos-publicacao SO via nova edicao que referencia a original.

---

## 2. Invariantes do agregado `EdicaoDiario`

- **D-1** Nasce `Rascunho` com `DataPublicacao == null` (factory `Abrir`).
- **D-2** `AdicionarMateria` so com edicao `Rascunho`; materia nasce com `Ordem` sequencial; titulo nao vazio.
- **D-3** `Publicar` so de `Rascunho`, exige >= 1 materia; define `DataPublicacao` (imutavel), vira
  `Publicada`; emite `DiarioPublicado`. **Idempotente** (republicar e no-op).
- **D-4** Edicao `Publicada` e **imutavel** (sem add/remover materia); correcao SO via `Retificar`
  (nova edicao referencia a original; a original nao e mutada).
- **D-5** `Numero` unico por `(TenantId, Ano)` (sequencia controlada no handler + indice unico).
- **D-6** `DataPublicacao` nao pode ser futura (relogio do servidor).

---

## 3. Relacao com Transparencia/LAI (via Contracts)

- **R-1** Ao publicar, emite `DiarioPublicado` (domain) mapeado para
  `DiarioOficialPublicadoIntegrationEvent` (Contracts), publicado via Outbox. Idempotente por `EventId`
  no consumidor. Isolamento preservado: Legislativo nao conhece entidades de Transparencia.

---

## 4. Consultas

- **C-1** `ListarEdicoes` (backoffice): filtros por ano/situacao, paginada — inclui rascunhos.
- **C-2** `ObterEdicaoPorId`: detalhe com materias ordenadas.
- **C-3** `ConsultarDiarioPublico` (cidadao/LAI): apenas edicoes publicadas, por ano, paginada.

---

## 5. RBAC (segregacao do ato de publicar)

- `legislativo.ver` — leitura (listar/detalhe/publico).
- `legislativo.diario.gerenciar` — montar edicao/materias/retificar.
- `legislativo.diario.publicar` — **verbo fino de SoD**: publicar (efeito legal), separado de quem monta.

---

## 6. Cenarios BDD (resumo)

- **Abrir nasce rascunho:** quando abrir edicao, entao situacao = Rascunho e sem data.
- **Publicar sem materia falha:** dada edicao vazia, quando publicar, entao falha.
- **Publicar emite evento de integracao:** dada edicao com materia, quando publicar, entao define data,
  vira Publicada e cai `DiarioOficialPublicadoIntegrationEvent` no Outbox.
- **Republicar e no-op:** dada edicao publicada, quando publicar de novo, entao nada muda e nao reemite.
- **Retificacao nao muta a original:** dada edicao publicada, quando retificar, entao nasce nova edicao
  rascunho que referencia a original, e a original permanece intacta.

<!-- manifest
commands: AbrirEdicaoDiario, AdicionarMateria, PublicarEdicaoDiario, RetificarEdicao
queries: ListarEdicoes, ObterEdicaoPorId, ConsultarDiarioPublico
domainEvents: DiarioPublicado
integrationEventsPublished: DiarioOficialPublicadoIntegrationEvent
integrationEventsConsumed: 
-->
