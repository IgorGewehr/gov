---
modulo: RecursosHumanos
agregado: ApuracaoPasep
contexto: RecursosHumanos (PASEP — contribuição do ente sobre a folha)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LC 8/1970 (PASEP)", "CF/1988 art. 239 (PIS/PASEP)", "Lei 9.715/1998 (contribuição sobre a folha das pessoas jurídicas de direito público)"]
---

# ApuracaoPasep — Regras-as-Code (Rules-as-Code)

> **PASEP** (Programa de Formação do Patrimônio do Servidor Público): contribuição do **ente público**
> incidente sobre a **folha de pagamento**, recolhida mensalmente (1% da base por padrão, **alíquota
> parametrizável por tenant**). Este agregado apura a **base** (folha bruta da competência) e o **valor**
> (base × alíquota). A **transmissão/recolhimento real** (GPS/DARF/agente arrecadador) é diferida ao
> **M10** (depende de credencial/integração). Este arquivo é **normativo e versionado**.

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| Apuração PASEP (`ApuracaoPasep`) | Apuração mensal da contribuição. Raiz de agregado. |
| Competência (`Competencia`) | Mês/ano de referência (VO `AAAA-MM`). |
| Base (`BaseContribuicao`) | Folha bruta da competência (soma dos proventos de todas as folhas). |
| Alíquota (`Aliquota`) | Percentual aplicado (1% padrão, parametrizável). |
| Valor (`Valor`) | `Base × Alíquota / 100`, arredondado a 2 casas. |
| Situação (`Situacao` : `SituacaoApuracaoPasep`) | Apurada ou Transmitida. |
| Tenant (`TenantId`) | Ente público dono do registro. |

---

## 2. Modelo

- **Identidade:** `ApuracaoPasepId` — `readonly record struct(Guid Value)`; fábrica `New()`.
- **Raiz:** `ApuracaoPasep : AggregateRoot<ApuracaoPasepId>, IMustHaveTenant` (`sealed`). Construtores privados; nasce válida via `Apurar(...)`.

### Enum `SituacaoApuracaoPasep`

| Valor | Numérico | Descrição |
|---|---|---|
| `Apurada` | 1 | Base e valor apurados (estado inicial). |
| `Transmitida` | 2 | Recolhida ao agente arrecadador (// TODO(M10)). |

---

## 3. Invariantes

- **I-1.** A apuração exige `Competencia` não nula, `BaseContribuicao ≥ 0` e `Aliquota ∈ (0, 100]`.
- **I-2.** `Valor = round(BaseContribuicao × Aliquota / 100, 2)`.
- **I-3.** Única por **(TenantId, Competencia)** — índice único; re-apuração exige excluir/recalcular.
- **I-4.** A **base** é a soma de `TotalProventos` de **todas** as folhas da competência (mensal + ciclo anual).
- **I-5.** A alíquota vem dos `ParametrosFolha` do tenant (1% padrão), salvo sobrescrita pontual no comando.
- **I-6.** `Transmitida` é terminal para nova transmissão (`InvalidOperationException`).

---

## 4. Máquina de Estados

| Origem | Comando / método | Destino | Guarda | Evento |
|---|---|---|---|---|
| (nenhum) | `Apurar` | `Apurada` | base ≥ 0; alíquota ∈ (0,100] (I-1) | `PasepApurado` |
| `Apurada` | `MarcarTransmitida` | `Transmitida` | não transmitida (I-6) | — |

---

## 5. Comandos (escrita)

### 5.1 ApurarPasep

- **Command:** `ApurarPasepCommand(int Ano, int Mes, decimal? AliquotaOverride) : ICommand<Guid>`.
- **Dependências:** `IApuracaoPasepRepository`, `IFolhaDePagamentoRepository`, `IParametrosFolhaProvider`, `IUnitOfWork`, `ITenantContext`.
- **Efeito:** valida unicidade; soma `TotalProventos` das folhas da competência (base); resolve alíquota; `ApuracaoPasep.Apurar(...)`; `Adicionar`; `SaveChangesAsync`.
- **Evento:** `PasepApurado(id, competencia, base, valor)`.

> `MarcarTransmitida` é método da raiz, exposto para a transmissão do M10; sem handler dedicado nesta versão.

---

## 6. Consultas (leitura)

### 6.1 ListarApuracoesPasep

- **Query:** `ListarApuracoesPasepQuery(int Ano) : IQuery<IReadOnlyList<ApuracaoPasepResumo>>`.
- **Handler:** `IApuracaoPasepRepository.ListarPorAnoAsync(...)`; tenant-scoped via Global Query Filter.

---

## 7. Eventos

### Domínio

| Evento | Payload | Emitido por |
|---|---|---|
| `PasepApurado` | `(ApuracaoPasepId, Competencia, decimal BaseContribuicao, decimal Valor)` | `ApuracaoPasep.Apurar` |

### Integração

- Nenhum nesta versão (// TODO(M10): recolhimento/transmissão real e eventual lançamento contábil cross-module via Contracts).

---

## 8. Validações (FluentValidation)

- **ApurarPasepValidator:** `Ano` `InclusiveBetween(2000, 2100)`; `Mes` `InclusiveBetween(1, 12)`; `AliquotaOverride` (quando informada) `GreaterThan(0)` + `LessThanOrEqualTo(100)`.

---

## 9. Persistência (EF Core 8)

- **Schema:** `recursoshumanos`. **Tabela:** `ApuracoesPasep`.
- Colunas: `Id` (PK, conversor `ApuracaoPasepId`↔`Guid`), `TenantId`, `Competencia` (int, `Ano*100+Mes`), `BaseContribuicao` (decimal 18,2), `Aliquota` (decimal 7,4), `Valor` (decimal 18,2), `Situacao` (string).
- Índice: único `(TenantId, Competencia)`.

---

## 10. Segurança, Tenant e Auditoria

- `IMustHaveTenant`; Global Query Filter por `TenantId`. RBAC: apuração `recursoshumanos.gerenciar`; consulta `recursoshumanos.ver` (negar por padrão).
- Auditoria imutável na apuração — relevante ao controle externo e à execução orçamentária.

---

## 11. Cenários BDD

- **C1.** Apurar competência com folha bruta R$ 100.000 e alíquota 1% ⇒ `Valor == 1.000,00`, evento `PasepApurado`.
- **C2.** Apurar sem folha na competência ⇒ base 0, valor 0.
- **C3.** Apurar competência já apurada ⇒ `InvalidOperationException` (I-3).
- **C4.** Alíquota sobrescrita 0,65% ⇒ valor recalculado pela alíquota informada (I-5).

---

## 12. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-24 | Versão inicial — PARIDADE-PoC Trilha A SW-A8 (PASEP: apuração da base = folha bruta × alíquota parametrizável; transmissão diferida ao M10). |

<!-- manifest
commands: ApurarPasep
queries: ListarApuracoesPasep
domainEvents: PasepApurado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
