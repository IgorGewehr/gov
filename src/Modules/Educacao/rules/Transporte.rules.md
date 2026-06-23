---
modulo: Educacao
agregado: RotaTransporte
contexto: Educacao — Transporte Escolar (PNATE; rotas/itinerarios e alunos transportados)
poder: Executivo
schema: educacao
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 10.880/2004 e Lei 11.947/2009 (PNATE)", "Resolucao CD/FNDE do PNATE", "CTB Lei 9.503/1997 (transporte escolar)", "LDB Lei 9.394/1996 art. 11 V"]
---

# Transporte Escolar (PNATE) — Regras-as-Code (Rules-as-Code)

> Rota de transporte escolar por turno atendendo uma escola, com modalidade (proprio/terceirizado),
> quilometragem, **veiculo da Frota** (FK logica por Id ao agregado `Veiculo` de **Patrimonio**) e os
> **alunos transportados** (reuso de `Aluno`/`Matricula` por Id, no mesmo modulo). Operacao local;
> prestacao de contas **PNATE/FNDE = M10** (atras de ACL). Arquivo **normativo e versionado**.

---

## 1. Linguagem Ubiqua

Identificadores entre parenteses sao **VINCULANTES** (sem acento, PT-BR no dominio).

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Rota de Transporte (`RotaTransporte`) | Itinerario por turno atendendo uma escola; raiz de agregado. |
| Aluno Transportado (`AlunoTransportado`) | Vinculo aluno x rota com ponto de embarque; entidade-filha. |
| Modalidade (`ModalidadeTransporte` : enum) | Proprio (veiculo da Frota) ou Terceirizado. |
| Veiculo (`VeiculoId`) | Veiculo da Frota = `Veiculo` de Patrimonio, referenciado por Id (FK logica). |
| Turno (`Turno` : enum) | Turno de atendimento (reusa o Turno de Turmas). |
| Aluno (`AlunoId`) / Matricula (`MatriculaId`) | Reuso por Id dos agregados do proprio modulo. |
| Quilometragem (`Quilometragem`) | Extensao do itinerario (km, >= 0). |
| Tenant (`TenantId`) | Ente municipal (rede de ensino) dono do registro. |

---

## 2. Modelo

- **Identidades:** `RotaTransporteId`, `AlunoTransportadoId` — `readonly record struct (Guid Value)`; fabrica `New()`.
- **Raiz:** `RotaTransporte : AggregateRoot<RotaTransporteId>, IMustHaveTenant` (`sealed`).
- **Filha:** `AlunoTransportado` (com `Ativo`, `PontoEmbarque`, `Desligar()`).
- **Construtores:** privados (um sem parametros para o EF). Nasce valida via `RotaTransporte.Criar(...)`.
- **Constante:** `ComprimentoNome = 120`.

### Enum `ModalidadeTransporte`

| Valor | Numerico |
|---|---|
| `Proprio` | 0 |
| `Terceirizado` | 1 |

### Enum `SituacaoRotaTransporte`

| Valor | Numerico |
|---|---|
| `Planejada` | 0 |
| `Ativa` | 1 |
| `Encerrada` | 2 |

---

## 3. Invariantes

- **I-R1.** Rota `Encerrada` (terminal) nao admite novos alunos.
- **I-R2.** Coerencia veiculo x modalidade: `Proprio` exige `VeiculoId`; `Terceirizado` exige `VeiculoId` nulo.
- **I-R3.** Nome obrigatorio (max. 120 chars); escola obrigatoria; quilometragem >= 0; turno/modalidade validos.
- **I-R4.** Um aluno nao pode ter vinculo ATIVO duplicado na mesma rota.
- **I-R5.** Ativacao (`Ativar`) exige rota `Planejada` com ao menos um aluno ativo.
- **I-R6.** Isolamento por tenant = ente municipal; dados nao cruzam tenants (Global Query Filter).

---

## 4. Maquina de Estados

| Origem | Comando / metodo | Destino | Guarda | Evento de dominio |
|---|---|---|---|---|
| (nenhum) | `Criar` | `Planejada` | I-R2, I-R3 | `RotaTransporteCriada` |
| `Planejada`/`Ativa` | `VincularAluno` | (inalterado) | nao `Encerrada` (I-R1); sem duplicidade (I-R4) | `AlunoVinculadoRota` |
| `Planejada`/`Ativa` | `DesligarAluno` | (inalterado) | vinculo ativo existe | — |
| `Planejada` | `Ativar` | `Ativa` | alunos ativos > 0 (I-R5) | — |
| qualquer != `Encerrada` | `Encerrar` | `Encerrada` | — | — |

---

## 5. Comandos (escrita)

### 5.1 CriarRota
- **Command:** `CriarRotaCommand(Guid EscolaId, string Nome, Turno Turno, ModalidadeTransporte Modalidade, Guid? VeiculoId, decimal Quilometragem) : ICommand<Guid>`.
- **Efeito:** `RotaTransporte.Criar(...)`; evento `RotaTransporteCriada`.

### 5.2 VincularAlunoRota
- **Command:** `VincularAlunoRotaCommand(Guid RotaId, Guid AlunoId, Guid? MatriculaId, string PontoEmbarque) : ICommand<Guid>`.
- **Pre:** rota nao `Encerrada` (I-R1); sem duplicidade (I-R4).
- **Efeito:** `rota.VincularAluno(...)`; evento `AlunoVinculadoRota`.

### 5.3 DesligarAlunoRota
- **Command:** `DesligarAlunoRotaCommand(Guid RotaId, Guid AlunoTransportadoId) : ICommand`.
- **Efeito:** `rota.DesligarAluno(...)`.

### 5.4 AtivarRota
- **Command:** `AtivarRotaCommand(Guid RotaId) : ICommand`.
- **Pre:** `Planejada` com alunos ativos (I-R5).

### 5.5 EncerrarRota
- **Command:** `EncerrarRotaCommand(Guid RotaId) : ICommand`.

---

## 6. Consultas (leitura)

### 6.1 ObterRotasPorEscola
- **Query:** `ObterRotasPorEscolaQuery(Guid? EscolaId) : IQuery<IReadOnlyList<RotaTransporteItemLista>>`.

### 6.2 ListarAlunosDaRota
- **Query:** `ListarAlunosDaRotaQuery(Guid RotaId) : IQuery<RotaTransporteDto?>`.

Todas **tenant-scoped** via Global Query Filter.

---

## 7. Eventos

### Dominio (in-process, MediatR)

| Evento | Payload | Emitido por |
|---|---|---|
| `RotaTransporteCriada` | `(RotaTransporteId)` | `RotaTransporte.Criar` |
| `AlunoVinculadoRota` | `(RotaTransporteId, AlunoId)` | `RotaTransporte.VincularAluno` |

### Integracao (publica / consome)
- Nenhum nesta versao. O `VeiculoId` da Frota e referencia por Id (Patrimonio); a coordenacao com a Frota, quando necessaria, e via Contracts.

---

## 8. Persistencia (EF Core 8)

- **Schema:** `educacao`. **Tabelas:** `RotaTransporte`, `AlunoTransportado` (child).
- `TenantId` carimbado por interceptor; Global Query Filter por `TenantId`.
- Indice por `(TenantId, EscolaId, Turno)`; conversores de Id/enum via Fluent API.

---

## 9. Seguranca, Tenant e Auditoria

- `IMustHaveTenant`; gravacao cross-tenant lanca excecao.
- RBAC: papeis da gestao do transporte (criar/vincular/ativar/encerrar) e leitura. Negar por padrao.
- Auditoria imutavel em toda mutacao.
- LGPD: aluno menor — minimizacao (somente ponto de embarque/vinculo necessario a operacao da rota).
- Anti-SQLi: EF parametrizado.

---

## 10. Integracoes Governamentais

- **PNATE/FNDE (saida) = M10:** prestacao de contas atras de ACL; a operacao local (rotas + alunos) nasce aqui.

---

## 11. Cenarios BDD

**C1 — Criar rota propria exige veiculo:** `CriarRota(Proprio, veiculoId=null)` -> `InvalidOperationException` (I-R2).
**C2 — Vincular aluno:** dado rota Planejada, ao `VincularAlunoRota`, cria o vinculo e emite `AlunoVinculadoRota`.
**C3 — Ativar sem alunos falha:** rota Planejada sem alunos -> `InvalidOperationException` (I-R5).
**C4 — Vinculo duplicado:** vincular o mesmo aluno ativo duas vezes -> `InvalidOperationException` (I-R4).
**C5 — Tenant-scoped:** consultas retornam apenas dados do tenant atual.

---

## 12. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — Onda 3b: agregado RotaTransporte (PNATE) reusando Veiculo (Frota/Patrimonio) por Id + Aluno/Matricula. |

<!-- manifest
commands: CriarRota, VincularAlunoRota, DesligarAlunoRota, AtivarRota, EncerrarRota
queries: ObterRotasPorEscola, ListarAlunosDaRota
domainEvents: RotaTransporteCriada, AlunoVinculadoRota
integrationEventsPublished: 
integrationEventsConsumed: 
-->
