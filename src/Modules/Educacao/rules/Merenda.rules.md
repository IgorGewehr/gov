---
modulo: Educacao
agregado: Cardapio / DistribuicaoMerenda
contexto: Educacao — Merenda Escolar (PNAE; planejamento nutricional e baixa de generos do almoxarifado)
poder: Executivo
schema: educacao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 11.947/2009 (PNAE)", "Resolucao CD/FNDE 06/2020 (alimentacao escolar)", "Lei 8.666/1993 e Lei 14.133/2021 (aquisicoes; agricultura familiar 30%)", "LDB Lei 9.394/1996"]
---

# Merenda Escolar (PNAE) — Regras-as-Code (Rules-as-Code)

> Planejamento nutricional (`Cardapio` semanal por escola/faixa etaria) e a distribuicao efetiva do dia
> (`DistribuicaoMerenda`), que consome generos do almoxarifado (per capita x comensais). A baixa real do
> saldo/lote ocorre no modulo **Patrimonio** (`ItemEstoque.AtenderRequisicao`), acionada via Integration
> Event (`MerendaDistribuidaIntegrationEvent`) — **cross-module so via Contracts** (CLAUDE.md §2). Operacao
> local; prestacao de contas **PNAE/FNDE = M10** (atras de ACL). Este arquivo e **normativo e versionado**.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Cardapio (`Cardapio`) | Planejamento nutricional semanal de uma escola/faixa etaria; raiz de agregado. |
| Item de Cardapio (`ItemCardapio`) | Genero x refeicao x dia, com per capita; entidade-filha. |
| Distribuicao de Merenda (`DistribuicaoMerenda`) | Refeicao efetivamente servida em um dia, com baixa de generos; raiz de agregado. |
| Consumo de Genero (`ConsumoGenero`) | Baixa reconhecida de um genero numa distribuicao; entidade-filha. |
| Genero (`GeneroEstoqueId`) | Genero alimenticio = `ItemEstoque` de Patrimonio, referenciado por Id (FK logica). |
| Faixa Etaria PNAE (`FaixaEtariaPnae` : enum) | Creche/Pre-escola/Fundamental-Medio/EJA. |
| Tipo de Refeicao (`TipoRefeicao` : enum) | Desjejum/LancheManha/Almoco/LancheTarde/Jantar. |
| Comensais (`Comensais`) | Numero de alunos que receberam a refeicao. |
| Tenant (`TenantId`) | Ente municipal (rede de ensino) dono do registro. |

---

## 2. Modelo

- **Identidades:** `CardapioId`, `ItemCardapioId`, `DistribuicaoMerendaId` — `readonly record struct (Guid Value)`; fabrica `New()`.
- **Raizes:** `Cardapio : AggregateRoot<CardapioId>, IMustHaveTenant` (`sealed`); `DistribuicaoMerenda : AggregateRoot<DistribuicaoMerendaId>, IMustHaveTenant` (`sealed`).
- **Filhas:** `ItemCardapio`, `ConsumoGenero`.
- **Construtores:** privados (um sem parametros para o EF). Nascem validas via factories `Cardapio.Planejar(...)` e `DistribuicaoMerenda.Registrar(...)`.
- **VO de transporte:** `ConsumoPrevisto(Guid GeneroEstoqueId, decimal Quantidade, string UnidadeMedida)` — ponte entre planejamento e baixa.

### Enum `FaixaEtariaPnae`

| Valor | Numerico |
|---|---|
| `Creche` | 0 |
| `PreEscola` | 1 |
| `EnsinoFundamentalMedio` | 2 |
| `EducacaoJovensAdultos` | 3 |

### Enum `TipoRefeicao`

| Valor | Numerico |
|---|---|
| `Desjejum` | 0 |
| `LancheManha` | 1 |
| `Almoco` | 2 |
| `LancheTarde` | 3 |
| `Jantar` | 4 |

### Enum `DiaSemanaCardapio` (Segunda=1 .. Sexta=5)

### Enum `SituacaoCardapio`

| Valor | Numerico |
|---|---|
| `Planejado` | 0 |
| `Publicado` | 1 |
| `Encerrado` | 2 |

---

## 3. Invariantes

- **I-M1.** Edicao de itens (`AdicionarItem`) exige cardapio `Planejado`.
- **I-M2.** Escola obrigatoria (Id nao vazio) e faixa etaria valida no `Planejar`.
- **I-M3.** Publicacao exige cardapio `Planejado` e ao menos um item.
- **I-M4.** Per capita do item deve ser positivo.
- **I-M5.** A `DistribuicaoMerenda` exige ao menos um genero a consumir (consumo previsto nao vazio) e comensais positivos.
- **I-M6.** A baixa de saldo/lote dos generos e responsabilidade de **Patrimonio**, acionada via `MerendaDistribuidaIntegrationEvent` (idempotente por `EventId` no consumidor). Educacao **nao** referencia o interno de Patrimonio.
- **I-M7.** Isolamento por tenant = ente municipal; dados nao cruzam tenants (Global Query Filter).

---

## 4. Maquina de Estados (Cardapio)

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Planejar` | `Planejado` | escola valida; faixa valida | `CardapioPlanejado` |
| `Planejado` | `AdicionarItem` | `Planejado` | situacao == `Planejado` | — |
| `Planejado` | `Publicar` | `Publicado` | itens > 0 | `CardapioPublicado` |
| qualquer != `Encerrado` | `Encerrar` | `Encerrado` | — | — |

`DistribuicaoMerenda.Registrar` nasce com os consumos e emite `MerendaDistribuida` (sem maquina de estados — registro factual).

---

## 5. Comandos (escrita)

### 5.1 PlanejarCardapio
- **Command:** `PlanejarCardapioCommand(Guid EscolaId, FaixaEtariaPnae FaixaEtaria, DateOnly Semana) : ICommand<Guid>`.
- **Efeito:** `Cardapio.Planejar(...)`; persiste; retorna `CardapioId`.
- **Evento:** `CardapioPlanejado`.

### 5.2 AdicionarItemCardapio
- **Command:** `AdicionarItemCardapioCommand(Guid CardapioId, DiaSemanaCardapio Dia, TipoRefeicao Refeicao, Guid GeneroEstoqueId, decimal QuantidadePerCapita, string UnidadeMedida) : ICommand<Guid>`.
- **Pre:** cardapio existe e `Planejado` (I-M1).
- **Efeito:** `cardapio.AdicionarItem(...)`.

### 5.3 PublicarCardapio
- **Command:** `PublicarCardapioCommand(Guid CardapioId) : ICommand`.
- **Pre:** `Planejado` com itens (I-M3).
- **Efeito:** `cardapio.Publicar()`; evento `CardapioPublicado`.

### 5.4 RegistrarDistribuicaoMerenda
- **Command:** `RegistrarDistribuicaoMerendaCommand(Guid CardapioId, DateOnly Data, DiaSemanaCardapio Dia, TipoRefeicao Refeicao, int Comensais) : ICommand<Guid>`.
- **Pre:** cardapio `Publicado`; consumo previsto (per capita x comensais) nao vazio (I-M5).
- **Efeito:** `DistribuicaoMerenda.Registrar(...)`; emite `MerendaDistribuida` (dominio) e publica `MerendaDistribuidaIntegrationEvent` (Outbox) para Patrimonio baixar os generos.

---

## 6. Consultas (leitura)

### 6.1 ObterCardapio
- **Query:** `ObterCardapioQuery(Guid CardapioId) : IQuery<CardapioDto?>`.

### 6.2 ListarCardapios
- **Query:** `ListarCardapiosQuery(Guid? EscolaId, DateOnly? Semana) : IQuery<IReadOnlyList<CardapioDto>>`.

### 6.3 ObterConsumoMerenda
- **Query:** `ObterConsumoMerendaQuery(Guid EscolaId, DateOnly De, DateOnly Ate) : IQuery<RelatorioConsumoDto>` — relatorio de consumo por genero no periodo.

Todas **tenant-scoped** via Global Query Filter.

---

## 7. Eventos

### Dominio (in-process, MediatR)

| Evento | Payload | Emitido por |
|---|---|---|
| `CardapioPlanejado` | `(CardapioId)` | `Cardapio.Planejar` |
| `CardapioPublicado` | `(CardapioId)` | `Cardapio.Publicar` |
| `MerendaDistribuida` | `(DistribuicaoMerendaId, EscolaId, DateOnly Data, TipoRefeicao)` | `DistribuicaoMerenda.Registrar` |

### Integracao (publica via `*.Contracts` + Outbox)

| Evento | Consumidor previsto |
|---|---|
| `MerendaDistribuidaIntegrationEvent` | Patrimonio (baixa de saldo/lote dos generos — `ItemEstoque.AtenderRequisicao`, idempotente por `EventId`). |

> `ConsumoGeneroMerenda` e DTO carregado no evento (nao e Integration Event proprio).

### Integracao (consome)
- Nenhum diretamente neste agregado.

---

## 8. Persistencia (EF Core 8)

- **Schema:** `educacao`. **Tabelas:** `Cardapio`, `ItemCardapio` (owned/child), `DistribuicaoMerenda`, `ConsumoGenero` (child).
- `TenantId` carimbado por interceptor; Global Query Filter por `TenantId`.
- Indices por `(TenantId, EscolaId, Semana)` no cardapio e `(TenantId, EscolaId, Data)` na distribuicao.
- Conversores de Id/enum via Fluent API (sem data annotations no dominio).

---

## 9. Seguranca, Tenant e Auditoria

- `IMustHaveTenant` em ambas as raizes; gravacao cross-tenant lanca excecao.
- RBAC: papeis da gestao da merenda (planejar/publicar/distribuir) e leitura (consultas). Negar por padrao.
- Auditoria imutavel em toda mutacao (planejar/adicionar/publicar/distribuir).
- Anti-SQLi: EF parametrizado.

---

## 10. Integracoes Governamentais

- **PNAE/FNDE (saida) = M10:** prestacao de contas (SiGPC/Contas Online) atras de ACL; certificado/credencial no Key Vault. A operacao local (cardapio + distribuicao + consumo) nasce aqui.

---

## 11. Cenarios BDD

**C1 — Planejar e publicar cardapio:** dado cardapio Planejado com itens, ao `PublicarCardapio`, vira `Publicado` e emite `CardapioPublicado`.
**C2 — Publicar sem itens falha:** cardapio Planejado vazio -> `InvalidOperationException` (I-M3).
**C3 — Distribuir merenda baixa generos:** dado cardapio Publicado, ao `RegistrarDistribuicaoMerenda(comensais=N)`, registra consumo (per capita x N) e publica `MerendaDistribuidaIntegrationEvent` (Patrimonio baixa o saldo).
**C4 — Tenant-scoped:** consultas retornam apenas dados do tenant atual.

---

## 12. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — Onda 3b: agregados Cardapio + DistribuicaoMerenda (PNAE), consumindo generos do almoxarifado de Patrimonio via Integration Event. |

<!-- manifest
commands: PlanejarCardapio, AdicionarItemCardapio, PublicarCardapio, RegistrarDistribuicaoMerenda
queries: ObterCardapio, ListarCardapios, ObterConsumoMerenda
domainEvents: CardapioPlanejado, CardapioPublicado, MerendaDistribuida
integrationEventsPublished: MerendaDistribuidaIntegrationEvent
integrationEventsConsumed: 
-->
