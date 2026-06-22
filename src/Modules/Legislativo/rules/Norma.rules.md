# Normas Juridicas — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). Gap G1: base consultavel das normas do municipio/Camara
> (leis, decretos legislativos, resolucoes, emendas a LOM, Lei Organica), com vinculo opcional a
> proposicao de origem, busca textual e ciclo de vigencia.
> Autoridade: `docs/architecture/legislativo-sprint/DESIGN-GAPS.md` (G1).
> Constituicao: isolamento de modulo, multi-tenant (`IMustHaveTenant` + Global Query Filter),
> auditoria imutavel, dominio rico, banco-por-tenant.

---

## 1. Linguagem ubiqua

- **Norma:** ato normativo vigente (`Lei`, `LeiComplementar`, `DecretoLegislativo`, `Resolucao`,
  `EmendaLOM`, `LeiOrganica`) identificado por tipo/numero/ano.
- **Ementa:** resumo do objeto (reusa o VO `Ementa` de Proposicoes).
- **Vigencia:** ciclo `EmVigor` -> `Alterada` (nao terminal) -> `Revogada` (terminal).
- **Historico de vigencia:** trilha append-only de eventos (`Promulgacao`, `Alteracao`, `Revogacao`).
- **Proposicao de origem:** proposicao aprovada (mesmo modulo) que originou a norma — referencia interna.

---

## 2. Invariantes do agregado `Norma`

- **N-1** `Numero > 0`, `Ano` em 1900..2100, `Ementa` nao vazia, `TipoNorma`/`SituacaoVigencia` definidos.
- **N-2** Unicidade logica `(TenantId, TipoNorma, Numero, Ano)` (indice unico + verificacao no handler).
- **N-3** Nasce `EmVigor` com 1 evento `Promulgacao` no historico (factory `Promulgar`); emite `NormaPromulgada`.
- **N-4** `Revogar` so de `EmVigor`/`Alterada`; vira `Revogada` (terminal); `dataRevogacao >= DataPromulgacao`;
  emite `NormaRevogada`.
- **N-5** `RegistrarAlteracao` so se nao `Revogada`; vira `Alterada` (nao terminal — pode ser revogada depois).
- **N-6** `VincularProposicaoOrigem` idempotente; so enquanto `ProposicaoOrigemId == null`.
- **N-7** Historico e **append-only** (sem update/delete de eventos).

---

## 3. Busca (consulta)

- **B-1** `BuscarNormas` filtra por termo livre na ementa, tipo, numero, ano, situacao; paginada;
  sempre tenant-scoped (Global Query Filter).
- **B-2** `ObterNormaPorId` retorna o detalhe com o historico de vigencia e a proposicao de origem.

---

## 4. RBAC

- `legislativo.ver` — leitura (busca/detalhe).
- `legislativo.normas.gerenciar` — cadastrar/revogar/alterar/vincular.

---

## 5. Cenarios BDD (resumo)

- **Promulgacao nasce em vigor:** dado tipo/numero/ano/ementa validos, quando cadastrar, entao situacao
  = EmVigor e historico contem 1 evento `Promulgacao`.
- **Duplicidade rejeitada:** dada Lei n/ano ja cadastrada no tenant, quando cadastrar de novo, entao falha.
- **Revogacao terminal:** dada norma em vigor, quando revogar, entao vira Revogada e emite `NormaRevogada`;
  revogar de novo falha.
- **Busca isolada por tenant:** dada norma de outro tenant, quando buscar, entao ela nao aparece.

<!-- manifest
commands: CadastrarNorma, RevogarNorma, RegistrarAlteracaoNorma, VincularProposicaoOrigem
queries: BuscarNormas, ObterNormaPorId
domainEvents: NormaPromulgada, NormaRevogada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
