---
modulo: Identidade
agregado: UnidadeOrganizacional
contexto: Identidade (arvore de Unidades Organizacionais — escopo do RBAC+ABAC)
poder: Ambos
schema: identidade
ativavel_por_tenant: false
versao_regras: 1.0.0
fontes_legais: ["CLAUDE.md §5 (multi-tenancy) e §6 (escopo organizacional do AuthZ)"]
---

# UnidadeOrganizacional — Regras-as-Code (Rules-as-Code)

> No da **arvore organizacional (UO)** do tenant: Secretaria, Departamento, Setor, etc. As UOs definem o
> **escopo** das atribuicoes de papel (RBAC+ABAC): uma atribuicao vale para uma UO e, opcionalmente, seus
> descendentes. Este arquivo e **normativo e versionado**.

---

## 1. Linguagem Ubiqua

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Unidade Organizacional (`UnidadeOrganizacional` / UO) | No da arvore administrativa do tenant. Raiz de agregado. |
| Identidade da UO (`UnidadeOrganizacionalId`) | `readonly record struct UnidadeOrganizacionalId(Guid Value)`; sentinela `RaizPendente`. |
| Tipo de Unidade (`TipoUnidade`) | Especie da UO (ex.: Secretaria, Departamento, Setor). VO/enum. |
| Pai (`UnidadePaiId`) | UO pai na arvore; `null` se raiz. |
| Reparentear (`Mover`) | Mudar o pai da UO na arvore (afeta escopos). |
| Tenant (`TenantId`) | Ente publico dono do registro. |

---

## 2. Modelo

- **Raiz de agregado:** `UnidadeOrganizacional : AggregateRoot<UnidadeOrganizacionalId>, IMustHaveTenant` (`sealed`).
- **Propriedades:** `TenantId` (`Guid`), `Nome` (`string`), `Tipo` (`TipoUnidade`), `UnidadePaiId` (`UnidadeOrganizacionalId?`), `Ativa` (`bool`).

---

## 3. Invariantes

- **I-1.** `Criar(nome, tipo, paiId?)` exige `Nome` nao vazio; nasce ativa e emite `UnidadeOrganizacionalCriada(id, tenant, paiId)`.
- **I-2.** `Renomear(nome, tipo)` valida e emite `UnidadeOrganizacionalEditada`.
- **I-3.** `Mover(novoPaiId)` reparenteia na arvore (sem criar ciclo) e emite `UnidadeOrganizacionalReparenteada` (evento sensivel — afeta escopos).
- **I-4.** `Ativar`/`Desativar` sao idempotentes; emitem `UnidadeOrganizacionalAtivada` / `UnidadeOrganizacionalDesativada`. Desativacao preserva o historico.

---

## 4. Comandos (escrita)

### 4.1 CriarUnidade
- **Command:** `CriarUnidadeCommand(string Nome, TipoUnidade Tipo, Guid? UnidadePaiId = null) : ICommand<Guid>`.
- **Efeito:** `UnidadeOrganizacional.Criar(...)`; persiste. Retorna `UnidadeOrganizacionalId.Value`. **Evento:** `UnidadeOrganizacionalCriada`.

### 4.2 RenomearUnidade
- **Command:** `RenomearUnidadeCommand(Guid UnidadeId, string Nome, TipoUnidade Tipo) : ICommand`.
- **Efeito:** `unidade.Renomear(nome, tipo)`. **Evento:** `UnidadeOrganizacionalEditada`.

### 4.3 MoverUnidade
- **Command:** `MoverUnidadeCommand(Guid UnidadeId, Guid NovoPaiId) : ICommand`.
- **Efeito:** `unidade.Mover(novoPaiId)`. **Evento:** `UnidadeOrganizacionalReparenteada`.

### 4.4 AtivarUnidade / DesativarUnidade
- **Commands:** `AtivarUnidadeCommand(Guid UnidadeId) : ICommand`; `DesativarUnidadeCommand(Guid UnidadeId) : ICommand`.
- **Efeito:** `unidade.Ativar()` / `unidade.Desativar()` (idempotentes). **Eventos:** `UnidadeOrganizacionalAtivada` / `UnidadeOrganizacionalDesativada`.

---

## 5. Consultas (leitura)

### 5.1 ListarArvoreUnidades
- **Query:** `ListarArvoreUnidadesQuery : IQuery<IReadOnlyList<NoUnidade>>`. Retorna a arvore (nos com pai/filhos). Tenant-scoped via Global Query Filter.

---

## 6. Eventos

### Dominio (in-process, MediatR; assembly `...Identidade.Domain`)

| Evento | Payload | Emitido por |
|---|---|---|
| `UnidadeOrganizacionalCriada` | `(UnidadeOrganizacionalId, Guid TenantId, UnidadeOrganizacionalId? UnidadePaiId)` | `UnidadeOrganizacional.Criar` |
| `UnidadeOrganizacionalEditada` | `(UnidadeOrganizacionalId)` | `UnidadeOrganizacional.Renomear` |
| `UnidadeOrganizacionalReparenteada` | `(UnidadeOrganizacionalId, UnidadeOrganizacionalId NovoPaiId)` | `UnidadeOrganizacional.Mover` |
| `UnidadeOrganizacionalAtivada` | `(UnidadeOrganizacionalId)` | `UnidadeOrganizacional.Ativar` |
| `UnidadeOrganizacionalDesativada` | `(UnidadeOrganizacionalId)` | `UnidadeOrganizacional.Desativar` |

> Identidade **nao publica nem consome Integration Events**.

---

## 7. Seguranca, Tenant e Auditoria

- **Tenant:** `UnidadeOrganizacional` implementa `IMustHaveTenant`; Global Query Filter por `TenantId`; gravacao cross-tenant lanca excecao.
- **AuthZ (negar por padrao):** gerir UOs exige permissao administrativa de Identidade.
- **Auditoria imutavel:** trilha (antes/depois) em toda mutacao; `UnidadeOrganizacionalReparenteada` e `UnidadeOrganizacionalDesativada` sao sensiveis (afetam escopos de acesso).

---

## 8. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — derivada do agregado `UnidadeOrganizacional` do modulo Identidade (P0 governanca: SpecCodeConsistency). |

<!-- manifest
commands: CriarUnidade, RenomearUnidade, MoverUnidade, AtivarUnidade, DesativarUnidade
queries: ListarArvoreUnidades
domainEvents: UnidadeOrganizacionalCriada, UnidadeOrganizacionalEditada, UnidadeOrganizacionalReparenteada, UnidadeOrganizacionalAtivada, UnidadeOrganizacionalDesativada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
