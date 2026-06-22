---
modulo: Financas
agregado: ReceitaArrecadada
contexto: Financas (Receitas)
poder: Ambos
schema: financas
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 4.320/64 (receita publica)", "Lei 4.320/64 art. 35 (regime de competencia da receita)", "Lei 4.320/64 art. 39 (Divida Ativa)"]
---

## 1. Linguagem Ubíqua

| Termo (identificador-no-codigo sem acento) | Definicao |
|---|---|
| Receita Arrecadada (`ReceitaArrecadada`) | Receita publica efetivamente arrecadada e reconhecida no modulo Financas a partir de um evento de integracao emitido por outro modulo (ex.: quitacao de Divida Ativa no modulo Tributos). E o agregado-raiz desta especificacao. |
| Identificador da Receita (`ReceitaArrecadadaId`) | Identidade forte do agregado; `readonly record struct` que encapsula um `Guid`. |
| Tenant (`TenantId`) | Ente publico (Prefeitura ou Camara) dono do registro. Isola os dados (multi-tenant). |
| Origem (`OrigemId`) | Identificador do fato gerador no modulo de origem (ex.: `DividaAtivaId` quitada em Tributos). Mantem rastreabilidade da receita ate sua fonte. |
| Valor (`Valor` / `ValorMonetario`) | Valor monetario arrecadado, representado pelo Value Object `ValorMonetario`. |
| Data (`Data`) | Data da arrecadacao (`DateOnly`), sem componente de hora. |
| Registrar (`Registrar`) | Factory de dominio que cria uma `ReceitaArrecadada` valida. |
| Evento de Integracao de Receita (`ReceitaArrecadadaIntegrationEvent`) | Evento de integracao definido em `Tensorroot.Gov.Modules.Tributos.Contracts`, consumido por Financas para reconhecer a receita. |

> Os identificadores acima sao **VINCULANTES**: o codigo gerado deve usa-los exatamente.

## 2. Modelo

- **Identidade:** `ReceitaArrecadadaId` — `readonly record struct ReceitaArrecadadaId(Guid Value)`.
  - `ReceitaArrecadadaId.New()` → gera novo Id via `Guid.NewGuid()`.
  - `ToString()` → `Value.ToString()`.
- **Agregado-raiz:** `ReceitaArrecadada : Entity<ReceitaArrecadadaId>, IMustHaveTenant` — `sealed`.
- **Construtores:** privados (um sem parametros para o EF; um privado completo). Instanciacao apenas via factory `Registrar`.

**Propriedades:**

| Propriedade | Tipo | Descricao | Mutabilidade |
|---|---|---|---|
| `Id` | `ReceitaArrecadadaId` | Identidade (herdada de `Entity<>`). | imutavel pos-criacao |
| `TenantId` | `Guid` | Tenant dono do registro (satisfaz `IMustHaveTenant`). | `private set` |
| `OrigemId` | `Guid` | Identificador de origem da receita no modulo de origem. | `private set` |
| `Valor` | `ValorMonetario` (VO) | Valor arrecadado. | `private set`, nao-nulo |
| `Data` | `DateOnly` | Data da arrecadacao. | `private set` |

**Value Objects:**
- `ValorMonetario` — Value Object monetario do modulo Financas (namespace `Tensorroot.Gov.Modules.Financas.Domain.ValueObjects`). Construido via `ValorMonetario.De(decimal)` no fluxo de consumo do evento.

**Enums:** nenhum. O agregado nao possui maquina de estados nem campo de status (ver secao 4).

## 3. Invariantes

Lista NUMERADA. Cada item I-n vira `[Fact] Invariante_n_*`.

- **I-1.** O `Valor` (`ValorMonetario`) e **obrigatorio e nao-nulo**. A factory `Registrar` lanca `ArgumentNullException` quando `valor` e nulo (`ArgumentNullException.ThrowIfNull(valor)`).
- **I-2.** Toda `ReceitaArrecadada` nasce **valida** atraves da factory `Registrar`; nao ha caminho publico de mutacao apos a criacao (todas as propriedades sao `private set`).
- **I-3.** O `Id` e atribuido na criacao via `ReceitaArrecadadaId.New()` e e **imutavel** durante todo o ciclo de vida.
- **I-4.** O agregado e **tenant-scoped**: implementa `IMustHaveTenant` e expoe `TenantId`. Nenhuma operacao cross-tenant e permitida (garantida por Global Query Filter e TenantInterceptor — ver secao 10).
- **I-5.** A receita e **derivada de um evento de integracao** (`ReceitaArrecadadaIntegrationEvent`): `TenantId`, `OrigemId`, `Valor` e `Data` provem integralmente do payload do evento consumido; o modulo Financas nao origina a receita por comando proprio nesta versao.
- **I-6.** `OrigemId` preserva a **rastreabilidade** da receita ate o fato gerador no modulo de origem (Lei 4.320/64 — reconhecimento e controle da receita publica).

> Observacao: o codigo atual nao impoe regra de sinal/positividade sobre `Valor` no agregado `ReceitaArrecadada` (eventual restricao reside no VO `ValorMonetario`). Ver secao 13 (Casos de Borda).

## 4. Máquina de Estados

O agregado `ReceitaArrecadada` **nao possui maquina de estados** nem campo de status. E um registro de fato consumado (receita ja arrecadada e reconhecida), criado em estado unico e final no momento da `Registrar`.

| Origem | Comando | Destino | Guarda | Evento emitido |
|---|---|---|---|---|
| (inexistente) | `Registrar` (factory de dominio) | Registrada (estado unico) | `valor` nao-nulo (I-1) | — (nenhum evento de dominio emitido) |

Nao ha transicoes subsequentes: o registro e imutavel apos a criacao.

## 5. Comandos (escrita)

Nesta versao **nao existe um Command/Handler de aplicacao (CQRS) proprio** para `ReceitaArrecadada`. A escrita ocorre exclusivamente pelo **consumo de um Integration Event** (ver secao 11). Documenta-se aqui a unica operacao de escrita existente:

### Operacao de escrita: consumo de `ReceitaArrecadadaIntegrationEvent`

- **Tipo:** Notification handler (`INotificationHandler<ReceitaArrecadadaIntegrationEvent>`), nao um `IRequest`/Command.
- **Handler:** `RegistrarReceitaArrecadadaHandler` (namespace `Tensorroot.Gov.Modules.Financas.Application.Integracoes`).
- **Entrada (payload do evento):** `ReceitaArrecadadaIntegrationEvent { Guid TenantId; Guid OrigemId; decimal Valor; DateOnly Data; }` (definido em `Tensorroot.Gov.Modules.Tributos.Contracts`).
- **Pre-condicoes:**
  - `notification` nao-nulo (`ArgumentNullException.ThrowIfNull(notification)`).
  - `notification.Valor` deve produzir um `ValorMonetario` valido via `ValorMonetario.De(notification.Valor)`.
- **Efeito:**
  1. Cria o agregado: `ReceitaArrecadada.Registrar(TenantId, OrigemId, ValorMonetario.De(Valor), Data)`.
  2. Persiste via repositorio: `receitas.Adicionar(receita)`.
  3. Confirma a transacao: `await unitOfWork.SaveChangesAsync(cancellationToken)`.
- **Pos-condicoes:**
  - Existe uma `ReceitaArrecadada` persistida com `TenantId`, `OrigemId`, `Valor` e `Data` iguais aos do evento.
  - O `Id` foi gerado (`ReceitaArrecadadaId.New()`).
- **Excecoes:**
  - `ArgumentNullException` se `notification` for nulo.
  - `ArgumentNullException` se `valor` for nulo dentro de `Registrar` (I-1).
  - Excecoes propagadas por `ValorMonetario.De(...)` para valor invalido.
- **Evento emitido:** nenhum evento de dominio nem de integracao e publicado por este fluxo (consumidor terminal da cadeia de integracao).

## 6. Consultas (leitura)

Nesta versao **nao ha Query/Handler (CQRS) de leitura** implementado para `ReceitaArrecadada`. O acesso de leitura existente e o do repositorio interno, sempre **tenant-scoped** via Global Query Filter.

| Consulta | Entrada | Projecao (DTO) | Filtros |
|---|---|---|---|
| (nenhuma Query publica nesta versao) | — | — | Toda leitura futura DEVE ser tenant-scoped (`TenantId` do `ITenantContext`). |

> Ao adicionar consultas: definir `*Query` + `*Handler`, projecao DTO `sealed record`, e garantir filtro por `TenantId`.

## 7. Eventos

- **Dominio (publicados pelo agregado):** **nenhum**. `ReceitaArrecadada.Registrar` nao levanta Domain Events nesta versao.
- **Integracao — publicados por Financas (`Financas.Contracts`):** **nenhum**.
- **Integracao — consumidos por Financas:** `ReceitaArrecadadaIntegrationEvent` — definido em `Tensorroot.Gov.Modules.Tributos.Contracts`, publicado pelo modulo **Tributos** (ex.: ao quitar uma Divida Ativa). Payload: `{ Guid TenantId; Guid OrigemId; decimal Valor; DateOnly Data; }`. Consumido por `RegistrarReceitaArrecadadaHandler`.

## 8. Validações (FluentValidation)

Nesta versao **nao ha `*Validator` (FluentValidation)** dedicado, pois nao existe Command de aplicacao — a validacao e feita por invariantes de dominio. Mapeamento equivalente das regras minimas (a serem promovidas a Validator caso um Command seja introduzido):

| Campo | Regra | Mensagem |
|---|---|---|
| `notification` | nao-nulo | `ArgumentNullException` (handler) |
| `Valor` | nao-nulo (VO obrigatorio) | `ArgumentNullException` (factory `Registrar`, I-1) |
| `Valor` | conversivel para `ValorMonetario` | conforme regra de `ValorMonetario.De(decimal)` |
| `TenantId` | proveniente do evento; carimbado/validado pelo TenantInterceptor na persistencia | gravacao cross-tenant lanca excecao |
| `OrigemId` | proveniente do evento (rastreabilidade) | — |
| `Data` | `DateOnly` proveniente do evento | — |

## 9. Persistência

- **Schema EF:** `financas`.
- **DbContext:** DbContext do modulo Financas (um por modulo, conforme constituicao).
- **Tabela:** `ReceitaArrecadada` (sugerida; confirmar com a configuracao Fluent API do modulo).

| Coluna | Tipo logico | Conversor | Observacao |
|---|---|---|---|
| `Id` | `uniqueidentifier` (Guid) | conversor `ReceitaArrecadadaId` ⇄ `Guid` (`Value`) | PK |
| `TenantId` | `uniqueidentifier` (Guid) | — | parte do isolamento multi-tenant |
| `OrigemId` | `uniqueidentifier` (Guid) | — | rastreabilidade da origem |
| `Valor` | conforme mapeamento do VO `ValorMonetario` (ex.: `decimal(18,2)`) | conversor/owned-type de `ValorMonetario` | nao-nulo |
| `Data` | `date` | conversor `DateOnly` ⇄ `date` | sem hora |

- **Conversores:** `ReceitaArrecadadaId` (Id forte → Guid); `ValorMonetario` (Value Object → coluna(s) monetaria(s)); `DateOnly` (→ `date`).
- **Indices:**
  - PK em `Id`.
  - Recomendado indice em `TenantId` (suporte ao Global Query Filter).
  - Recomendado indice em `OrigemId` (consultas por origem) e indice composto `(TenantId, OrigemId)` para idempotencia/dedup (ver secao 13).
- **Outbox:** o modulo possui tabela Outbox propria; este fluxo e **consumidor** e nao publica via Outbox.

## 10. Segurança, Tenant e Auditoria

- **IMustHaveTenant:** `ReceitaArrecadada` implementa `IMustHaveTenant` (`TenantId`). **Global Query Filter** por `TenantId` ativo no DbContext; **TenantInterceptor** carimba/valida `TenantId` na insercao — gravacao cross-tenant **lanca excecao**.
- **RBAC / policies:** o fluxo e disparado por evento de integracao interno (sem endpoint HTTP nesta versao), portanto sem policy de usuario direta. Qualquer endpoint futuro de leitura/escrita deve negar por padrao e exigir policy adequada do modulo Financas.
- **LGPD:** o agregado nao armazena dados pessoais sensiveis (apenas `Guid`s, valor e data). Sem dado pessoal direto; nao requer base legal de dado sensivel.
- **Auditoria:** a persistencia passa pelo `AuditSaveChangesInterceptor`, gerando trilha imutavel (antes/depois, usuario/identidade do processo, IP/origem, timestamp) para o Tribunal de Contas. A criacao da receita e auditada na transacao do `SaveChangesAsync`.

## 11. Integrações Governamentais

- **Integracao cross-module (intra-processo, nao governamental externa):**
  - **Origem:** modulo **Tributos** publica `ReceitaArrecadadaIntegrationEvent` (via `Tributos.Contracts`) — tipicamente ao quitar Divida Ativa (Lei 4.320/64 art. 39 — receita de Divida Ativa).
  - **Consumo:** modulo **Financas**, handler `RegistrarReceitaArrecadadaHandler` (`INotificationHandler<...>`), reconhece a receita.
  - **ACL:** o contrato `ReceitaArrecadadaIntegrationEvent` vive em `*.Contracts`; Financas nao referencia o interno de Tributos (apenas Contracts), preservando o isolamento de modulos.
- **Resiliencia / idempotencia:** o consumo deve ser **idempotente** (Idempotency behavior do pipeline MediatR / dedup por `OrigemId`). Nao ha chamada de I/O externa neste fluxo, portanto Polly nao se aplica diretamente aqui; a resiliencia da publicacao e responsabilidade do Outbox do modulo de origem.
- **Sistemas governamentais externos (TCE-RS / SICONFI):** a receita aqui reconhecida e insumo para remessas de prestacao de contas (Transparencia), fora do escopo deste agregado.

## 12. Cenários BDD

Cada cenario vira teste de integracao.

- **Cenario 1 — Reconhecimento de receita a partir do evento:**
  - **Dado** um `ReceitaArrecadadaIntegrationEvent` valido com `TenantId`, `OrigemId`, `Valor = 1500,00` e `Data`,
  - **Quando** `RegistrarReceitaArrecadadaHandler.Handle` for executado,
  - **Entao** uma `ReceitaArrecadada` e persistida com os mesmos `TenantId`, `OrigemId`, `Valor` e `Data`, com `Id` gerado, e `SaveChangesAsync` e chamado uma vez.

- **Cenario 2 — Evento nulo:**
  - **Dado** `notification = null`,
  - **Quando** `Handle(null, ct)` for chamado,
  - **Entao** lanca `ArgumentNullException` e **nada** e persistido.

- **Cenario 3 — Valor nulo na factory:**
  - **Dado** uma chamada `ReceitaArrecadada.Registrar(tenantId, origemId, valor: null, data)`,
  - **Quando** a factory executar,
  - **Entao** lanca `ArgumentNullException` (I-1) e nenhuma instancia e criada.

- **Cenario 4 — Isolamento de tenant na persistencia:**
  - **Dado** o `ITenantContext` resolvido para o tenant A e uma receita do tenant A persistida,
  - **Quando** uma leitura ocorrer no contexto do tenant B,
  - **Entao** o Global Query Filter impede o retorno do registro do tenant A.

- **Cenario 5 — Rastreabilidade de origem:**
  - **Dado** um evento com `OrigemId` de uma Divida Ativa quitada,
  - **Quando** a receita for registrada,
  - **Entao** `ReceitaArrecadada.OrigemId` e igual ao `OrigemId` do evento (rastreabilidade preservada).

## 13. Casos de Borda

Cada item vira teste.

- **CB-1.** `notification` nulo → `ArgumentNullException` (handler), sem persistencia.
- **CB-2.** `valor` nulo em `Registrar` → `ArgumentNullException` (I-1).
- **CB-3.** `ValorMonetario.De(notification.Valor)` com valor invalido (ex.: fora de dominio do VO) → excecao propagada do VO; receita nao e criada.
- **CB-4.** Evento duplicado (mesmo `OrigemId`/mesma chave) → deve ser idempotente (Idempotency behavior / dedup); **nao** deve gerar receita duplicada. (Atencao: o codigo atual nao implementa dedup explicita no handler — risco a cobrir.)
- **CB-5.** `Valor` zero ou negativo → o agregado nao impoe positividade; comportamento depende de `ValorMonetario`. Definir regra de sinal no VO se exigido pela contabilidade publica.
- **CB-6.** `Data` futura → nao ha validacao temporal no agregado; aceitar/recusar conforme regra contabil (Lei 4.320/64, regime de competencia) a ser definida.
- **CB-7.** `TenantId` do evento divergente do contexto do consumidor → o TenantInterceptor deve impedir gravacao cross-tenant (lanca excecao).
- **CB-8.** Falha em `SaveChangesAsync` (ex.: violacao de constraint) → transacao revertida; nenhuma receita persistida; excecao propagada ao pipeline.

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Especificacao inicial (retrofit fiel ao codigo existente): agregado `ReceitaArrecadada` e consumo de `ReceitaArrecadadaIntegrationEvent` via `RegistrarReceitaArrecadadaHandler`. |

<!-- manifest
commands: 
queries: 
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: ReceitaArrecadadaIntegrationEvent
-->
