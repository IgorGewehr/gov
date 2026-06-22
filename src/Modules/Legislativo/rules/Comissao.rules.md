# Comissao — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). Gap G4: comissao como entidade de primeira classe
> (permanentes/temporarias) com composicao (membros efetivos/suplentes vinculados a vereadores) e
> presidencia — substitui a referencia por texto cru hoje usada em pareceres.
> Autoridade: `docs/architecture/legislativo-sprint/DESIGN-GAPS.md` (G4).
> Constituicao: multi-tenant, auditoria imutavel, dominio rico, banco-por-tenant.

---

## 1. Linguagem ubiqua

- **Comissao:** orgao colegiado (`Permanente`/`Temporaria`) — ex.: CCJ, Financas e Orcamento.
- **Membro:** vinculo de um `VereadorId` com `Papel` (`Efetivo`/`Suplente`) e `Cargo`
  (`Nenhum`/`Presidente`/`VicePresidente`/`Relator`).
- **Situacao:** `Ativa` -> `Extinta` (terminal — temporaria encerrada).

---

## 2. Invariantes do agregado `Comissao`

- **C-1** Criada `Ativa`; `Nome` nao vazio (<= 200), `Tipo` definido.
- **C-2** `DesignarMembro` idempotente por vereador (segunda designacao atualiza papel/cargo).
- **C-3** No maximo **um presidente** por comissao.
- **C-4** `RemoverMembro` retira da composicao (bloqueado se extinta).
- **C-5** `Extinguir` e terminal (nao admite alteracao de composicao depois).

---

## 3. Consultas

- **Q-1** `ListarComissoes` lista as comissoes do tenant (ordenadas por nome).
- **Q-2** `ObterComissaoPorId` retorna o detalhe com a composicao e a presidencia.

---

## 4. RBAC

- `legislativo.ver` — leitura (listar/detalhe).
- `legislativo.comissoes.gerenciar` — criar/compor/extinguir.

---

## 5. Cenarios BDD (resumo)

- **Composicao com presidencia:** dada comissao ativa, quando designar presidente + efetivos + suplentes,
  entao a comissao tem a composicao e o presidente correto.
- **Dois presidentes falham:** dada comissao com presidente, quando designar outro presidente, entao falha.
- **Designacao idempotente:** dado vereador ja membro, quando designar de novo com outro cargo, entao
  atualiza o cargo sem duplicar.
- **Extincao terminal:** dada comissao extinta, quando designar membro, entao falha.

<!-- manifest
commands: CriarComissao, DesignarMembro, RemoverMembro, ExtinguirComissao
queries: ListarComissoes, ObterComissaoPorId
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->
