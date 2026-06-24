---
modulo: Identidade
agregado: Papel
contexto: Identidade (RBAC — perfis e conjuntos de permissoes do tenant)
poder: Ambos
schema: identidade
ativavel_por_tenant: false
versao_regras: 1.0.0
fontes_legais: ["CLAUDE.md §6 (AuthZ policy-based + RBAC, negar por padrao)"]
---

# Papel — Regras-as-Code (Rules-as-Code)

> Perfil RBAC do tenant: um nome e um conjunto de **permissoes** (strings) que, quando atribuido a um
> usuario com escopo de UO, define o que ele pode fazer. Este arquivo e **normativo e versionado**.

---

## 1. Linguagem Ubiqua

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Papel (`Papel`) | Perfil RBAC (nome + conjunto de permissoes). Raiz de agregado. |
| Identidade do Papel (`PapelId`) | `readonly record struct PapelId(Guid Value)`; fabrica `PapelId.New()`. |
| Permissao (`string`) | Cadeia que identifica uma capacidade (ex.: `identidade.usuarios.gerenciar`). |
| Tenant (`TenantId`) | Ente publico dono do registro. |

---

## 2. Modelo

- **Raiz de agregado:** `Papel : AggregateRoot<PapelId>, IMustHaveTenant` (`sealed`).
- **Propriedades:** `TenantId` (`Guid`), `Nome` (`string`), `Permissoes` (`IReadOnlySet<string>` encapsulada).

---

## 3. Invariantes

- **I-1.** `Criar(nome, permissoes?)` exige `Nome` nao vazio; nasce com as permissoes informadas (ou vazio) e emite `PapelCriado(id, tenant)`.
- **I-2.** `DefinirPermissoes(permissoes)` substitui integralmente o conjunto de permissoes (colecao nao nula) e emite `PermissoesDoPapelDefinidas`.
- **I-3.** O `Nome` do papel e unico por tenant (validado na aplicacao/persistencia).

---

## 4. Comandos (escrita)

### 4.1 CriarPapel
- **Command:** `CriarPapelCommand(string Nome, IReadOnlyCollection<string>? Permissoes = null) : ICommand<Guid>`.
- **Efeito:** `Papel.Criar(nome, permissoes)`; persiste. Retorna `PapelId.Value`. **Evento:** `PapelCriado`.

### 4.2 DefinirPermissoesDoPapel
- **Command:** `DefinirPermissoesDoPapelCommand(Guid PapelId, IReadOnlyCollection<string> Permissoes) : ICommand`.
- **Efeito:** `papel.DefinirPermissoes(permissoes)`. **Evento:** `PermissoesDoPapelDefinidas`.

---

## 5. Consultas (leitura)

### 5.1 ListarPapeis
- **Query:** `ListarPapeisQuery : IQuery<IReadOnlyList<PapelDetalhe>>`. Tenant-scoped via Global Query Filter.

---

## 6. Eventos

### Dominio (in-process, MediatR; assembly `...Identidade.Domain`)

| Evento | Payload | Emitido por |
|---|---|---|
| `PapelCriado` | `(PapelId, Guid TenantId)` | `Papel.Criar` |
| `PermissoesDoPapelDefinidas` | `(PapelId)` | `Papel.DefinirPermissoes` |

> Identidade **nao publica nem consome Integration Events**.

---

## 7. Seguranca, Tenant e Auditoria

- **Tenant:** `Papel` implementa `IMustHaveTenant`; Global Query Filter por `TenantId`; gravacao cross-tenant lanca excecao.
- **AuthZ (negar por padrao):** gerir papeis/permissoes exige permissao administrativa de Identidade.
- **Auditoria imutavel:** trilha (antes/depois) em toda mutacao; `PermissoesDoPapelDefinidas` e sensivel (altera o alcance de acesso).

---

## 8. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — derivada do agregado `Papel` do modulo Identidade (P0 governanca: SpecCodeConsistency). |

<!-- manifest
commands: CriarPapel, DefinirPermissoesDoPapel
queries: ListarPapeis
domainEvents: PapelCriado, PermissoesDoPapelDefinidas
integrationEventsPublished: 
integrationEventsConsumed: 
-->
