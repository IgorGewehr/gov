---
modulo: RecursosHumanos
agregado: Portaria
contexto: RecursosHumanos (atos de pessoal / portarias)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 37 (atos administrativos de pessoal)", "Lei 8.112/1990 (provimento/exoneração — supletivo ao estatuto municipal)", "Lei 14.063/2020 (assinatura eletrônica de documentos públicos)"]
---

# Portaria — Regras-as-Code (Rules-as-Code)

> **Portaria / ato de pessoal** que formaliza decisões sobre o vínculo do servidor: **nomeação**,
> **exoneração**, **designação** de função e **concessão** de licença/benefício. Numeração **sequencial
> por exercício/tenant** (`NNN/AAAA`), com ementa, texto integral, vínculo opcional ao servidor e ciclo
> de vida **Emitida → Revogada**. Uso diário do RH. Este arquivo é **normativo e versionado**.

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| Portaria (`Portaria`) | Ato administrativo de pessoal. Raiz de agregado. |
| Numeração (`NumeroPortaria`) | Sequencial por exercício/tenant, formatada `NNN/AAAA`. |
| Exercício (`Exercicio`) | Ano civil que reinicia a sequência. |
| Sequencial (`Sequencial`) | Número dentro do exercício (≥ 1). |
| Natureza (`Tipo` : `TipoPortaria`) | Nomeação/Exoneração/Designação/Concessão/Outro. |
| Ementa (`Ementa`) | Resumo do ato (≤ 500). |
| Texto (`Texto`) | Corpo integral do ato (≤ 20.000). |
| Servidor vinculado (`ServidorId?`) | Servidor objeto do ato (opcional). |
| Situação (`Situacao` : `SituacaoPortaria`) | Emitida ou Revogada. |
| Tenant (`TenantId`) | Ente público dono do registro. |

---

## 2. Modelo

- **Identidade:** `PortariaId` — `readonly record struct PortariaId(Guid Value)`; fábrica `New()`.
- **Raiz:** `Portaria : AggregateRoot<PortariaId>, IMustHaveTenant` (`sealed`). Construtores privados; nasce válida via `Emitir(...)`.
- **VO:** `NumeroPortaria.De(exercicio, sequencial)` — `Sequencial ≥ 1`, `Exercicio > 0`; `Formatado` = `NNN/AAAA`. Reconstruído do par escalar persistido (`Exercicio`/`Sequencial`).

### Enum `TipoPortaria`

| Valor | Numérico |
|---|---|
| `Nomeacao` | 1 |
| `Exoneracao` | 2 |
| `Designacao` | 3 |
| `Concessao` | 4 |
| `Outro` | 5 |

### Enum `SituacaoPortaria`

| Valor | Numérico | Descrição |
|---|---|---|
| `Emitida` | 1 | Vigente (estado inicial). |
| `Revogada` | 2 | Tornada sem efeito (terminal); preserva a numeração consumida. |

---

## 3. Invariantes

- **I-1.** A emissão exige `Numero` não nulo, `Ementa` não vazia (≤ 500) e `Texto` não vazio (≤ 20.000).
- **I-2.** A numeração é **sequencial por (TenantId, Exercicio)**: o sequencial é `max(exercício) + 1`, iniciando em 1. Unicidade garantida por índice `(TenantId, Exercicio, Sequencial)`.
- **I-3.** O `Exercicio` é o **ano da data do ato** (data civil do tenant via `IDataHojeTenant`).
- **I-4.** A revogação preserva a numeração consumida (a sequência do exercício **não retrocede**).
- **I-5.** Portaria `Revogada` é **terminal**; nova revogação lança `InvalidOperationException`.
- **I-6.** Quando há `ServidorId`, o servidor deve existir no tenant (validado no handler).

---

## 4. Máquina de Estados

| Origem | Comando / método | Destino | Guarda | Evento |
|---|---|---|---|---|
| (nenhum) | `Emitir` | `Emitida` | `Numero`/`Ementa`/`Texto` válidos (I-1) | `PortariaEmitida` |
| `Emitida` | `Revogar` | `Revogada` | `motivo` não vazio; não revogada (I-5) | `PortariaRevogada` |

---

## 5. Comandos (escrita)

### 5.1 EmitirPortaria

- **Command:** `EmitirPortariaCommand(TipoPortaria Tipo, string Ementa, string Texto, Guid? ServidorId, DateOnly? DataAto) : ICommand<Guid>`.
- **Dependências:** `IPortariaRepository`, `IServidorRepository`, `IUnitOfWork`, `ITenantContext`, `IDataHojeTenant`.
- **Efeito:** apura `Exercicio` (ano da data do ato), `Sequencial` (`ProximoSequencialAsync`), cria via `Portaria.Emitir(...)`, `Adicionar`, `SaveChangesAsync`.
- **Evento:** `PortariaEmitida(id, numero, tipo, servidorId?)`.

### 5.2 RevogarPortaria

- **Command:** `RevogarPortariaCommand(Guid PortariaId, string Motivo) : ICommand`.
- **Dependências:** `IPortariaRepository`, `IUnitOfWork`.
- **Efeito:** carrega a portaria, `Revogar(motivo)`, `SaveChangesAsync`.
- **Evento:** `PortariaRevogada(id, motivo)`.

---

## 6. Consultas (leitura)

### 6.1 BuscarPortarias

- **Query:** `BuscarPortariasQuery(TipoPortaria? Tipo, SituacaoPortaria? Situacao, int? Exercicio, Guid? ServidorId, int? Pagina, int? Tamanho) : IQuery<ResultadoPaginado<PortariaResumo>>`.
- **Handler:** `IPortariaRepository.BuscarAsync(...)`; tenant-scoped via Global Query Filter.

### 6.2 ObterPortaria

- **Query:** `ObterPortariaQuery(Guid PortariaId) : IQuery<PortariaDetalhe>`.
- **Handler:** `IPortariaRepository.ObterPorIdAsync(...)`; projeta `PortariaDetalhe` (inclui texto integral).

---

## 7. Eventos

### Domínio

| Evento | Payload | Emitido por |
|---|---|---|
| `PortariaEmitida` | `(PortariaId, NumeroPortaria Numero, TipoPortaria Tipo, ServidorId? ServidorId)` | `Portaria.Emitir` |
| `PortariaRevogada` | `(PortariaId, string Motivo)` | `Portaria.Revogar` |

### Integração

- Nenhum nesta versão (// TODO(M10): remessa de pessoal ao TCE-RS pode consumir atos).

---

## 8. Validações (FluentValidation)

- **EmitirPortariaValidator:** `Tipo` `IsInEnum`; `Ementa` `NotEmpty` + `MaximumLength(500)`; `Texto` `NotEmpty` + `MaximumLength(20000)`.
- **RevogarPortariaValidator:** `PortariaId` `NotEmpty`; `Motivo` `NotEmpty`.

---

## 9. Persistência (EF Core 8)

- **Schema:** `recursoshumanos`. **Tabela:** `Portarias`.
- Colunas: `Id` (PK, conversor `PortariaId`↔`Guid`), `TenantId`, `Exercicio` (int), `Sequencial` (int), `Tipo`/`Situacao` (string), `DataAto` (date), `Ementa` (nvarchar 500), `Texto` (nvarchar max), `ServidorId` (Guid?), `MotivoRevogacao` (nvarchar 500). `Numero` é calculado (sem coluna; `Ignore`).
- Índices: único `(TenantId, Exercicio, Sequencial)`; `(TenantId, Tipo, Situacao)`; `(TenantId, ServidorId)`.

---

## 10. Segurança, Tenant e Auditoria

- `IMustHaveTenant`; Global Query Filter por `TenantId`. RBAC: emissão/revogação `recursoshumanos.gerenciar`; consulta `recursoshumanos.ver` (negar por padrão).
- Auditoria imutável em toda mutação (emissão/revogação) — relevante ao TCE-RS.

---

## 11. Cenários BDD

- **C1.** Emitir a 1ª portaria do exercício ⇒ `Sequencial == 1`, `Numero == 0001/AAAA`, evento `PortariaEmitida`.
- **C2.** Emitir a 2ª no mesmo exercício ⇒ `Sequencial == 2`.
- **C3.** Revogar portaria emitida ⇒ situação `Revogada`, numeração preservada, evento `PortariaRevogada`.
- **C4.** Revogar portaria já revogada ⇒ `InvalidOperationException` (I-5).
- **C5.** Emitir com `ServidorId` inexistente ⇒ `InvalidOperationException` (I-6).

---

## 12. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-24 | Versão inicial — PARIDADE-PoC Trilha A SW-A7 (portarias/atos de pessoal: numeração sequencial por exercício, emissão/revogação, vínculo opcional ao servidor). |

<!-- manifest
commands: EmitirPortaria, RevogarPortaria
queries: BuscarPortarias, ObterPortaria
domainEvents: PortariaEmitida, PortariaRevogada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
