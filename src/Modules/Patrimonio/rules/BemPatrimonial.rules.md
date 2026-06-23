---
modulo: Patrimonio
agregado: BemPatrimonial
contexto: Patrimonio (Bens, Tombamento, Depreciacao, Reavaliacao, Impairment, Baixa/Alienacao)
poder: Ambos
schema: patrimonio
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["MCASP/STN (Procedimentos Contabeis Patrimoniais)", "NBC TSP 07 (Ativo Imobilizado)", "Lei 4.320/1964 (controle patrimonial e inventario)", "Lei 14.133/2021 art. 31 (leilao/avaliacao previa) e art. 76 (alienacao/doacao)", "Codigo Civil (comodato/cessao)"]
---

# BemPatrimonial — Regras-as-Code (Rules-as-Code)

> Bem público (móvel ou imóvel) gerido em todo o seu ciclo de vida — incorporação,
> tombamento, movimentação, depreciação linear, reavaliação, impairment e baixa/alienação —
> com mensuração contábil conforme MCASP/STN e NBC TSP 07. O imóvel é desmembrável em
> terreno (não deprecia) + benfeitoria (deprecia). Este arquivo é **normativo e versionado**;
> o código (`BemPatrimonial.cs`, handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Bem Patrimonial (`BemPatrimonial`) | Bem público móvel ou imóvel integrante do acervo. Raiz de agregado. |
| Tombamento / Número de Tombo (`Tombar` / `NumeroTombamento`) | Ato e identificador único de registro do bem no patrimônio. |
| Incorporação (`Incorporar` / `Incorporacao`) | Ingresso do bem ao acervo (aquisição, doação, produção própria). |
| Baixa (`Baixar` / `Baixada`) | Exclusão do bem do acervo (inservível, perda, alienação). |
| Alienação (`Alienar` / `Alienada`) | Transferência onerosa de domínio; em regra por leilão, com avaliação prévia (Lei 14.133 art. 31/76). |
| Cessão / Comodato (`Ceder` / `Cedido`) | Uso por terceiro sem (cessão) ou a título gratuito (comodato) — Código Civil. |
| Transferência (`Transferir` / `MovimentacaoPatrimonial`) | Mudança de localização/responsável dentro do tenant. |
| Depreciação (`Depreciar` / `Depreciacao` : VO) | Alocação sistemática (linear) do valor depreciável ao longo da vida útil. |
| Valor Residual (`ValorResidual`) | Resíduo estimado ao fim da vida útil. |
| Valor Depreciável (`ValorDepreciavel`) | Custo − valor residual. |
| Valor Contábil (`ValorContabil`) | Valor líquido atual do bem (custo − depreciação acumulada ± ajustes). |
| Reavaliação (`Reavaliar` / `Reavaliacao` : entidade) | Ajuste do valor contábil ao valor justo. |
| Impairment (`RegistrarImpairment` / `Impairment` : entidade) | Redução ao valor recuperável quando inferior ao contábil. |
| Vida Útil (`VidaUtilMeses`) | Período estimado de uso econômico do bem. |
| Histórico de Depreciação (`HistoricoDepreciacao` : entidade) | Registro por competência da depreciação reconhecida. |
| Terreno / Benfeitoria (`Terreno` / `Benfeitoria`) | Componentes do imóvel desmembrável: terreno não deprecia; edificação (benfeitoria) deprecia. |
| Competência (`Competencia`) | Mês/ano de referência do reconhecimento contábil. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoBemPatrimonial`) | Estado atual do bem no ciclo de vida patrimonial. |

---

## 2. Modelo

- **Identidade:** `BemPatrimonialId` — `readonly record struct BemPatrimonialId(Guid Value)`; fábrica `BemPatrimonialId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `BemPatrimonial : AggregateRoot<BemPatrimonialId>, IMustHaveTenant` (`sealed`; classe-base de `Veiculo`, que é-um `BemPatrimonial`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Incorporar(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `NumeroTombamento` | `NumeroTombamento?` (VO) | Número de tombo; nulo antes de `Tombar`. | `private set` |
| `Descricao` | `string` | Descrição do bem. | `private set` |
| `Tipo` | `TipoBem` (enum) | Móvel ou Imóvel. | `private set` |
| `ValorInicial` | `ValorMonetario` (VO) | Valor de incorporação (custo de ingresso). | `private set` |
| `ValorResidual` | `ValorMonetario` (VO) | Resíduo estimado ao fim da vida útil. | `private set` |
| `ValorContabil` | `ValorMonetario` (VO) | Valor líquido atual (≥ residual). | `private set` |
| `VidaUtilMeses` | `int` | Vida útil em meses. | `private set` |
| `DataIncorporacao` | `DateOnly` | Data de ingresso ao acervo. | `private set` |
| `EmCondicoesDeUso` | `bool` | Marca o início da depreciação (deprecia só quando true). | `private set` |
| `Situacao` | `SituacaoBemPatrimonial` (enum) | Situação atual. | `private set` |
| `Movimentacoes` | `IReadOnlyCollection<MovimentacaoPatrimonial>` | Transferências de local/responsável. | coleção controlada |
| `HistoricosDepreciacao` | `IReadOnlyCollection<HistoricoDepreciacao>` | Depreciação por competência. | coleção controlada |
| `Reavaliacoes` | `IReadOnlyCollection<Reavaliacao>` | Reavaliações a valor justo. | coleção controlada |
| `Impairments` | `IReadOnlyCollection<Impairment>` | Perdas por recuperabilidade. | coleção controlada |

> **Imóvel desmembrável:** quando `Tipo == Imovel`, o bem é desmembrado em **terreno** (não deprecia) + **benfeitoria/edificação** (deprecia). A parcela depreciável corresponde à edificação; o valor do terreno permanece inalterado pela depreciação.

### Value Objects (referenciados)

- `ValorMonetario` — valor monetário; expõe `Valor` (`decimal`). Não nulo.
- `NumeroTombamento` — `record struct` com identificador único de tombo (string/Guid), validado não vazio.
- `Depreciacao` — VO com `ValorDepreciavel` (= custo − residual), `VidaUtilMeses`, parcela mensal linear.

### Entidades (do agregado)

- `MovimentacaoPatrimonial` — transferência de localização/responsável (origem, destino, data, responsável).
- `HistoricoDepreciacao` — competência, valor depreciado, valor contábil resultante.
- `Reavaliacao` — data, novo valor justo, laudo.
- `Impairment` — data, valor recuperável, perda reconhecida, laudo/teste de recuperabilidade.

### Enum `TipoBem`

| Valor | Numérico | Descrição |
|---|---|---|
| `Movel` | 1 | Bem móvel. |
| `Imovel` | 2 | Bem imóvel (desmembrável em terreno + benfeitoria). |

### Enum `SituacaoBemPatrimonial`

| Valor | Numérico | Descrição |
|---|---|---|
| `EmIncorporacao` | 1 | Incorporado, ainda sem tombo. |
| `Tombado` | 2 | Tombado e ativo no acervo. |
| `Cedido` | 3 | Em cessão/comodato a terceiro. |
| `Baixada` | 4 | Baixado (terminal). |
| `Alienada` | 5 | Alienado (terminal). |

> **Conjuntos de referência usados nas guardas:**
> - **Ativo no acervo** = { `Tombado`, `Cedido` } (admite depreciação/reavaliação/impairment).
> - **Encerrado** = { `Baixada`, `Alienada` } (não admite novas transições).
> - **Depreciável** = `Tombado` && `EmCondicoesDeUso` && (parcela do bem que não seja terreno).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** A depreciação é **linear**; parcela mensal = `ValorDepreciavel / VidaUtilMeses`, onde `ValorDepreciavel = ValorInicial − ValorResidual` (MCASP / NBC TSP 07).
- **I-2.** A depreciação **só inicia** quando o bem está `EmCondicoesDeUso`; antes disso, `Depreciar` não reduz o valor contábil.
- **I-3.** **Terreno NÃO deprecia; edificação SIM** — em imóvel desmembrado, a depreciação incide apenas sobre a benfeitoria, mantendo o valor do terreno inalterado.
- **I-4.** O `ValorContabil` **nunca** é inferior ao `ValorResidual`; a depreciação do período é limitada de modo que, no máximo, `ValorContabil == ValorResidual`.
- **I-5.** A **reavaliação** ajusta o `ValorContabil` ao valor justo informado (laudo obrigatório); admitida apenas para bem **ativo no acervo**.
- **I-6.** O **impairment** é reconhecido quando o valor recuperável informado é **inferior** ao `ValorContabil`; a perda = `ValorContabil − ValorRecuperavel`. Se o recuperável ≥ contábil, nada é reconhecido.
- **I-7.** A **baixa** exige laudo/parecer anexado **e** autorização; sem laudo, a baixa é rejeitada e **nenhum** lançamento contábil é emitido.
- **I-8.** A baixa/alienação produz lançamento contábil **simultâneo** (publicação de `BemBaixadoIntegrationEvent` no mesmo commit, via Outbox).
- **I-9.** A **alienação** requer avaliação prévia e, em regra, **leilão** (Lei 14.133 art. 31/76); sem avaliação prévia registrada, a alienação é rejeitada.
- **I-10.** O **tombamento** atribui `NumeroTombamento` único por tenant; só pode ocorrer a partir de `EmIncorporacao`.
- **I-11.** Estados **encerrados** (`Baixada`, `Alienada`) não admitem depreciação, reavaliação, impairment, transferência, cessão nem novas baixas.
- **I-12.** Na **incorporação**, `ValorInicial` e `ValorResidual` são obrigatórios (`ArgumentNullException.ThrowIfNull`), `ValorResidual ≤ ValorInicial`, `VidaUtilMeses > 0`, situação inicial `EmIncorporacao` e emite-se `BemIncorporado`.
- **I-13.** A **transferência** (`Transferir`) só é permitida para bem **ativo no acervo**; registra `MovimentacaoPatrimonial` (origem/destino/responsável).
- **I-14.** A **cessão/comodato** (`Ceder`) só é permitida para bem `Tombado`; leva a `Cedido` sem baixa contábil (continua no acervo).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Incorporar` | `EmIncorporacao` | `ValorInicial != null` && `ValorResidual ≤ ValorInicial` && `VidaUtilMeses > 0` | `BemIncorporado` |
| `EmIncorporacao` | `Tombar` | `Tombado` | `numeroTombamento` único e não vazio; situação == `EmIncorporacao` | `BemTombado` |
| `Tombado` | `Depreciar` | `Tombado` | `EmCondicoesDeUso` && parcela não-terreno && resultante ≥ residual | `BemDepreciado` |
| `Tombado` \| `Cedido` | `Reavaliar` | (inalterada) | ativo no acervo; laudo informado | `BemReavaliado` |
| `Tombado` \| `Cedido` | `RegistrarImpairment` | (inalterada) | valor recuperável < contábil; laudo informado | `BemReavaliado` (impairment) |
| `Tombado` | `Transferir` | `Tombado` | ativo no acervo | — |
| `Tombado` | `Ceder` | `Cedido` | situação == `Tombado` | — |
| `Cedido` | `Transferir` | `Cedido` | ativo no acervo | — |
| `Tombado` \| `Cedido` | `Baixar` | `Baixada` | laudo anexado && autorização; ≠ encerrado | `BemBaixado` |
| `Tombado` \| `Cedido` | `Alienar` | `Alienada` | avaliação prévia registrada; ≠ encerrado | `BemBaixado` (alienação) |

> Observações:
> - `Reavaliar` e `RegistrarImpairment` não mudam a `Situacao`; alteram `ValorContabil` e registram a entidade correspondente.
> - O **processamento mensal de depreciação** percorre os bens depreciáveis do tenant e chama `Depreciar(competencia)` em cada um, respeitando I-2, I-3 e I-4.
> - `Baixar` e `Alienar` chamam a guarda de "não encerrado" **antes** das verificações específicas (laudo / avaliação prévia).

---

## 5. Comandos (escrita)

### 5.1 IncorporarBem

- **Command:** `IncorporarBemCommand(string Descricao, int Tipo, decimal ValorInicial, decimal ValorResidual, int VidaUtilMeses, DateOnly DataIncorporacao, string Origem) : ICommand<Guid>`.
- **Entrada (DTO):** descrição, tipo (móvel/imóvel), valores inicial e residual, vida útil em meses, data e origem (aquisição/doação/produção própria).
- **Dependências do handler:** `IBemPatrimonialRepository`, `IUnitOfWork`, `ITenantContext`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; `ValorInicial > 0`; `ValorResidual ≤ ValorInicial`; `VidaUtilMeses > 0`.
- **Efeito:** cria via `BemPatrimonial.Incorporar(tenant.TenantId, ...)`; `bens.Adicionar(bem)`; `SaveChangesAsync`.
- **Pós-condições:** bem em situação `EmIncorporacao`; retorna `bem.Id.Value` (`Guid`); publica `BemIncorporadoIntegrationEvent` (variação patrimonial aumentativa) via Outbox.
- **Exceções:** `ArgumentNullException` (request/valores); `ArgumentException` (valores inválidos).
- **Evento de domínio:** `BemIncorporado(Id, valorInicial, origem)`.
- **Evento de integração (publica):** `BemIncorporadoIntegrationEvent`.

### 5.2 TombarBem

- **Command:** `TombarBemCommand(Guid BemPatrimonialId, string NumeroTombamento) : ICommand`.
- **Entrada (DTO):** `BemPatrimonialId`, `NumeroTombamento`.
- **Dependências do handler:** `IBemPatrimonialRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; bem existe (senão `InvalidOperationException("Bem não encontrado.")`); situação == `EmIncorporacao` (I-10); `numeroTombamento` não vazio e único por tenant.
- **Efeito:** `bem.Tombar(request.NumeroTombamento)`; `SaveChangesAsync`.
- **Pós-condições:** `NumeroTombamento` preenchido; situação `Tombado`.
- **Exceções:** `ArgumentNullException`; `ArgumentException` (tombo vazio); `InvalidOperationException` (não encontrado / situação ≠ `EmIncorporacao` / tombo duplicado).
- **Evento de domínio:** `BemTombado(Id, numeroTombamento)`.

### 5.3 DepreciarBem (processamento mensal)

- **Command:** `DepreciarBemCommand(Guid BemPatrimonialId, int AnoCompetencia, int MesCompetencia) : ICommand` (ou comando em lote `ProcessarDepreciacaoMensalCommand(int Ano, int Mes)`).
- **Entrada (DTO):** identificação do bem (ou lote) e competência.
- **Dependências do handler:** `IBemPatrimonialRepository`, `IUnitOfWork`, `TimeProvider`.
- **Pré-condições:** `request` não nulo; bem existe; situação `Tombado` && `EmCondicoesDeUso` (I-2); imóvel deprecia apenas a benfeitoria (I-3).
- **Efeito:** `bem.Depreciar(competencia)` — calcula parcela linear, limita ao residual (I-4), grava `HistoricoDepreciacao`, atualiza `ValorContabil`; `SaveChangesAsync`.
- **Pós-condições:** `ValorContabil` reduzido (nunca abaixo do residual); novo `HistoricoDepreciacao`; publica `BemDepreciadoIntegrationEvent`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado).
- **Evento de domínio:** `BemDepreciado(Id, valorDepreciado, competencia)`.
- **Evento de integração (publica):** `BemDepreciadoIntegrationEvent`.

### 5.4 ReavaliarBem

- **Command:** `ReavaliarBemCommand(Guid BemPatrimonialId, decimal NovoValorJusto, string LaudoUri, DateOnly DataReavaliacao) : ICommand`.
- **Pré-condições:** `request` não nulo; bem existe; **ativo no acervo** (I-5); laudo informado.
- **Efeito:** `bem.Reavaliar(novoValorJusto, laudo, data)`; ajusta `ValorContabil`; grava `Reavaliacao`; `SaveChangesAsync`.
- **Pós-condições:** `ValorContabil = NovoValorJusto`; publica `BemReavaliadoIntegrationEvent`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado / sem laudo).
- **Evento de domínio:** `BemReavaliado(Id, novoValorJusto)`.
- **Evento de integração (publica):** `BemReavaliadoIntegrationEvent`.

### 5.5 RegistrarImpairment

- **Command:** `RegistrarImpairmentCommand(Guid BemPatrimonialId, decimal ValorRecuperavel, string LaudoUri, DateOnly DataTeste) : ICommand`.
- **Pré-condições:** `request` não nulo; bem existe; ativo no acervo; `ValorRecuperavel < ValorContabil` (I-6); laudo/teste informado.
- **Efeito:** `bem.RegistrarImpairment(valorRecuperavel, laudo, data)`; reduz `ValorContabil`; grava `Impairment`; `SaveChangesAsync`.
- **Pós-condições:** `ValorContabil = ValorRecuperavel`; perda reconhecida; publica `BemReavaliadoIntegrationEvent` (impairment).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado / recuperável ≥ contábil / sem laudo).
- **Evento de domínio:** `BemReavaliado(Id, valorRecuperavel)` (vertente impairment).
- **Evento de integração (publica):** `BemReavaliadoIntegrationEvent`.

### 5.6 TransferirBem

- **Command:** `TransferirBemCommand(Guid BemPatrimonialId, string LocalizacaoDestino, Guid ResponsavelDestinoId) : ICommand`.
- **Pré-condições:** `request` não nulo; bem existe; ativo no acervo (I-13).
- **Efeito:** `bem.Transferir(destino, responsavel)`; grava `MovimentacaoPatrimonial`; `SaveChangesAsync`.
- **Pós-condições:** nova `MovimentacaoPatrimonial`; situação inalterada (`Tombado`/`Cedido`).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado).
- **Evento de domínio:** — (movimentação interna; sem evento publicado nesta versão).

### 5.7 CederBem (cessão/comodato)

- **Command:** `CederBemCommand(Guid BemPatrimonialId, Guid TerceiroId, bool Gratuito, DateOnly DataInicio, DateOnly? DataFim) : ICommand`.
- **Pré-condições:** `request` não nulo; bem existe; situação == `Tombado` (I-14).
- **Efeito:** `bem.Ceder(terceiro, gratuito, periodo)`; `SaveChangesAsync`.
- **Pós-condições:** situação `Cedido`; bem permanece no acervo (sem baixa contábil).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / situação ≠ `Tombado`).
- **Evento de domínio:** — (cessão/comodato; sem evento publicado nesta versão).

### 5.8 BaixarBem

- **Command:** `BaixarBemCommand(Guid BemPatrimonialId, int MotivoBaixa, string LaudoUri, Guid AutorizacaoId) : ICommand`.
- **Pré-condições:** `request` não nulo; bem existe; **não encerrado** (I-11); **laudo/parecer anexado e autorização** (I-7).
- **Efeito:** `bem.Baixar(motivo, laudo, autorizacao)`; `SaveChangesAsync`; publica `BemBaixadoIntegrationEvent` no mesmo commit (I-8).
- **Pós-condições:** situação `Baixada`; lançamento contábil simultâneo (Finanças).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado / **sem laudo** → baixa rejeitada e nenhum lançamento emitido).
- **Evento de domínio:** `BemBaixado(Id, motivoBaixa, valorContabil)`.
- **Evento de integração (publica):** `BemBaixadoIntegrationEvent`.

### 5.9 AlienarBem

- **Command:** `AlienarBemCommand(Guid BemPatrimonialId, Guid AvaliacaoPreviaId, bool PorLeilao, decimal ValorAlienacao) : ICommand`.
- **Pré-condições:** `request` não nulo; bem existe; não encerrado; **avaliação prévia registrada** e, em regra, **leilão** (I-9; Lei 14.133 art. 31/76).
- **Efeito:** `bem.Alienar(avaliacaoPrevia, porLeilao, valor)`; `SaveChangesAsync`; publica `BemBaixadoIntegrationEvent` (alienação).
- **Pós-condições:** situação `Alienada`; lançamento contábil simultâneo.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / encerrado / **sem avaliação prévia** → rejeitada).
- **Evento de domínio:** `BemBaixado(Id, motivoBaixa="Alienacao", valorContabil)`.
- **Evento de integração (publica):** `BemBaixadoIntegrationEvent`.

---

## 6. Consultas (leitura)

### 6.1 ObterBemPatrimonial

- **Query:** `ObterBemPatrimonialQuery(Guid BemPatrimonialId) : IQuery<BemPatrimonialDetalhe>`.
- **Handler:** `ObterBemPatrimonialHandler(IBemPatrimonialRepository bens)`.
- **Projeção (DTO):** `BemPatrimonialDetalhe(Guid Id, string? NumeroTombamento, string Descricao, string Tipo, decimal ValorInicial, decimal ValorResidual, decimal ValorContabil, int VidaUtilMeses, DateOnly DataIncorporacao, string Situacao)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarBensDepreciaveis

- **Query:** `ListarBensDepreciaveisQuery(int Ano, int Mes) : IQuery<IReadOnlyList<BemDepreciavelResumo>>`.
- **Handler:** lista bens `Tombado` && `EmCondicoesDeUso` cujo `ValorContabil > ValorResidual` para a competência.
- **Projeção (DTO):** `BemDepreciavelResumo(Guid Id, string NumeroTombamento, decimal ValorContabil, decimal ValorResidual, decimal ParcelaMensal)`.
- **Filtros:** tenant-scoped; usado pelo processamento mensal de depreciação.

### 6.3 ListarMovimentacoesDoBem

- **Query:** `ListarMovimentacoesDoBemQuery(Guid BemPatrimonialId) : IQuery<IReadOnlyList<MovimentacaoResumo>>`.
- **Projeção (DTO):** `MovimentacaoResumo(Guid Id, string LocalizacaoOrigem, string LocalizacaoDestino, Guid ResponsavelId, DateOnly Data)`.
- **Filtros:** por `BemPatrimonialId`; tenant-scoped.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Patrimonio.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `BemIncorporado` | `(BemPatrimonialId, ValorMonetario valorInicial, string origem)` | `BemPatrimonial.Incorporar` |
| `BemTombado` | `(BemPatrimonialId, string numeroTombamento)` | `BemPatrimonial.Tombar` |
| `BemDepreciado` | `(BemPatrimonialId, decimal valorDepreciado, DateOnly competencia)` | `BemPatrimonial.Depreciar` |
| `BemReavaliado` | `(BemPatrimonialId, decimal novoValor)` | `BemPatrimonial.Reavaliar` / `RegistrarImpairment` |
| `BemBaixado` | `(BemPatrimonialId, string motivoBaixa, ValorMonetario valorContabil)` | `BemPatrimonial.Baixar` / `Alienar` |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Patrimonio.Contracts`)

| Evento | Payload | Publicado por | Destino |
|---|---|---|---|
| `BemIncorporadoIntegrationEvent` | `{ TombamentoId, ValorInicial, Origem }` | `IncorporarBemHandler` | Finanças (variação patrimonial aumentativa) |
| `BemDepreciadoIntegrationEvent` | `{ TombamentoId, ValorDepreciado, Competencia }` | `DepreciarBemHandler` | Finanças |
| `BemBaixadoIntegrationEvent` | `{ TombamentoId, MotivoBaixa, ValorContabil }` | `BaixarBemHandler` / `AlienarBemHandler` | Finanças |
| `BemReavaliadoIntegrationEvent` | `{ TombamentoId, NovoValorJusto }` | `ReavaliarBemHandler` / `RegistrarImpairmentHandler` | Finanças |

### Integração (consome)

| Evento | Payload | Origem | Efeito |
|---|---|---|---|
| `ContratoAssinadoIntegrationEvent` | `{ ContratoId, ... }` | Administracao | Aquisição via licitação dispara tombamento (entrada de bem). |

---

## 8. Validações (FluentValidation)

### IncorporarBemValidator (`AbstractValidator<IncorporarBemCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Descricao` | `NotEmpty()` + `MaximumLength(200)` | Descrição obrigatória, máx. 200 caracteres. |
| `Tipo` | `IsInEnum()` | Tipo de bem inválido. |
| `ValorInicial` | `GreaterThan(0)` | Valor inicial deve ser positivo. |
| `ValorResidual` | `GreaterThanOrEqualTo(0)` + `LessThanOrEqualTo(x => x.ValorInicial)` | Valor residual não pode exceder o valor inicial. |
| `VidaUtilMeses` | `GreaterThan(0)` | Vida útil deve ser positiva. |

### TombarBemValidator (`AbstractValidator<TombarBemCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `BemPatrimonialId` | `NotEmpty()` | Identificador obrigatório. |
| `NumeroTombamento` | `NotEmpty()` + `MaximumLength(40)` | Número de tombo obrigatório, máx. 40 caracteres. |

### BaixarBemValidator (`AbstractValidator<BaixarBemCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `BemPatrimonialId` | `NotEmpty()` | Identificador obrigatório. |
| `MotivoBaixa` | `IsInEnum()` | Motivo de baixa inválido. |
| `LaudoUri` | `NotEmpty()` | Laudo/parecer é obrigatório para baixa (I-7). |
| `AutorizacaoId` | `NotEmpty()` | Autorização é obrigatória para baixa. |

### AlienarBemValidator (`AbstractValidator<AlienarBemCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `BemPatrimonialId` | `NotEmpty()` | Identificador obrigatório. |
| `AvaliacaoPreviaId` | `NotEmpty()` | Avaliação prévia é obrigatória para alienação (I-9). |

---

## 9. Persistência (EF Core 8)

- **Schema:** `patrimonio` (isolado por módulo). **DbContext:** o do módulo Patrimonio. **Migrations:** por módulo.
- **Tabela:** `BemPatrimonial` (raiz de agregado). Tabelas filhas: `MovimentacaoPatrimonial`, `HistoricoDepreciacao`, `Reavaliacao`, `Impairment`.

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `BemPatrimonialId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `NumeroTombamento` | `nvarchar(40)?` | conversor de `NumeroTombamento`; nulável antes do tombamento. |
| `Descricao` | `nvarchar(200)` | descrição do bem. |
| `Tipo` | `int` | enum `TipoBem`. |
| `ValorInicial` | `decimal` | owned/conversor de `ValorMonetario`. |
| `ValorResidual` | `decimal` | owned/conversor de `ValorMonetario`. |
| `ValorContabil` | `decimal` | owned/conversor de `ValorMonetario`. |
| `VidaUtilMeses` | `int` | vida útil. |
| `DataIncorporacao` | `date` | `DateOnly`. |
| `EmCondicoesDeUso` | `bit` | início da depreciação. |
| `Situacao` | `int` | enum `SituacaoBemPatrimonial`. |

- **Não persistidas (calculadas):** parcela mensal de depreciação (derivada de `Depreciacao`).
- **Índices:**
  - PK em `Id`.
  - **Único** em `(TenantId, NumeroTombamento)` quando `NumeroTombamento` não nulo (unicidade do tombo por tenant — I-10).
  - Índice em `(TenantId, Situacao, EmCondicoesDeUso)` para `ListarBensDepreciaveis`.
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto Patrimonio para `BemIncorporado/Depreciado/Baixado/ReavaliadoIntegrationEvent` (consistência transacional com o estado).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `BemPatrimonial` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based, negar por padrão; segregação de funções):**
  - Incorporar/tombar/depreciar: papéis da **Gestão Patrimonial** (ex.: `Patrimonio.Bens.Gerir`).
  - Baixar/alienar/reavaliar/impairment: papéis com **autorização** específica (ex.: `Patrimonio.Bens.Baixar`), **segregados** de quem requisita.
  - Consultas: papel de **leitura patrimonial** (ex.: `Patrimonio.Bens.Ler`).
  - Módulo Patrimonio é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** o agregado registra responsáveis (`ResponsavelId`) e terceiros (cessão/comodato); minimização aplicada nas projeções. Sem dados sensíveis de saúde/assistência.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em **toda** mutação (`Incorporar`, `Tombar`, `Depreciar`, `Reavaliar`, `RegistrarImpairment`, `Transferir`, `Ceder`, `Baixar`, `Alienar`) — incluindo **laudo anexado** em baixa/reavaliação/impairment — destinada ao Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Saída — Finanças (intra-aplicação, via Contracts):** incorporação, depreciação, baixa/alienação e reavaliação/impairment publicam Integration Events (Outbox) para variação patrimonial e lançamento contábil pelo módulo Finanças (Lei 4.320, MCASP). Idempotente por `EventId`. **Nunca** há chamada direta a Finanças.
- **Entrada — Administracao (intra-aplicação):** aquisição via licitação consome `ContratoAssinadoIntegrationEvent`/recebimento, disparando o tombamento (entrada do bem). ACL + idempotência.
- **Georreferenciamento de imóveis (GIS/matrícula):** integração externa para imóveis (matrícula cartorial e geometria). ACL + Polly (timeout, retry, circuit breaker) + idempotência.
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Depreciação de imóvel (terreno não deprecia)**
- **Dado** um imóvel incorporado com terreno e edificação
- **Quando** o processamento mensal de depreciação é executado
- **Então** apenas a edificação é depreciada e o valor contábil do terreno permanece inalterado (I-3).

**Cenário 2 — Limite do valor residual**
- **Dado** um bem cujo valor contábil se aproxima do valor residual
- **Quando** a depreciação do período seria suficiente para reduzi-lo abaixo do residual
- **Então** a depreciação é limitada de modo que o valor contábil iguale o residual (I-4).

**Cenário 3 — Baixa sem laudo**
- **Dado** um pedido de baixa de bem **sem** laudo/parecer anexado
- **Quando** o comando `BaixarBemCommand` é submetido
- **Então** a baixa é **rejeitada** e **nenhum** lançamento contábil é emitido (I-7).

**Cenário 4 — Baixa válida publica lançamento**
- **Dado** um bem `Tombado` com laudo anexado e autorização
- **Quando** executo `BaixarBemCommand`
- **Então** situação = `Baixada`, o evento `BemBaixado` é emitido e `BemBaixadoIntegrationEvent` é publicado no mesmo commit (I-8).

**Cenário 5 — Impairment**
- **Dado** um bem cujo valor recuperável é inferior ao valor contábil
- **Quando** a comissão registra o teste de recuperabilidade
- **Então** uma perda por impairment é reconhecida (`ValorContabil = ValorRecuperavel`) e o evento `BemReavaliado`/impairment é emitido a Finanças (I-6).

**Cenário 6 — Reavaliação a valor justo**
- **Dado** um bem `Tombado` ativo no acervo, com laudo
- **Quando** executo `ReavaliarBemCommand(novoValorJusto, laudo)`
- **Então** `ValorContabil = NovoValorJusto`, o evento `BemReavaliado` é emitido e `BemReavaliadoIntegrationEvent` é publicado (I-5).

**Cenário 7 — Alienação sem avaliação prévia**
- **Dado** um bem `Tombado` sem avaliação prévia registrada
- **Quando** executo `AlienarBemCommand`
- **Então** a alienação é rejeitada por ausência de avaliação prévia (I-9; Lei 14.133 art. 31/76).

**Cenário 8 — Tombamento a partir de incorporação**
- **Dado** um bem em situação `EmIncorporacao`
- **Quando** executo `TombarBemCommand(bemId, "TOMBO-2026-0001")`
- **Então** `NumeroTombamento = "TOMBO-2026-0001"`, situação = `Tombado` e o evento `BemTombado` é emitido (I-10).

**Cenário 9 — Incorporação publica variação patrimonial**
- **Dado** uma aquisição válida
- **Quando** executo `IncorporarBemCommand`
- **Então** é criado um `BemPatrimonial` em `EmIncorporacao` e `BemIncorporadoIntegrationEvent` é publicado a Finanças (variação aumentativa) (I-12).

**Cenário 10 — Operações sobre bem encerrado**
- **Dado** um bem `Baixada` (ou `Alienada`)
- **Quando** chamo `Depreciar` / `Reavaliar` / `Transferir` / `Baixar`
- **Então** ocorre `InvalidOperationException` (estado encerrado não admite novas transições) (I-11).

**Cenário 11 — Cessão/comodato mantém no acervo**
- **Dado** um bem `Tombado`
- **Quando** executo `CederBemCommand` (a título gratuito)
- **Então** situação = `Cedido` e o bem permanece no acervo, **sem** baixa contábil (I-14).

**Cenário 12 — Consulta tenant-scoped**
- **Dado** bens do tenant A e bens do tenant B
- **Quando** executo `ObterBemPatrimonialQuery` no contexto do tenant A
- **Então** somente bens do tenant A são retornados.

---

## 13. Casos de Borda

- **B-1.** `ValorResidual > ValorInicial` em `Incorporar` ⇒ rejeitado (I-12 / validator).
- **B-2.** `VidaUtilMeses == 0` ⇒ rejeitado (divisão da parcela linear inválida; I-1/I-12).
- **B-3.** Depreciar bem **não** `EmCondicoesDeUso` ⇒ nenhuma redução do valor contábil (I-2).
- **B-4.** Depreciar imóvel ⇒ apenas a benfeitoria deprecia; valor do terreno inalterado (I-3).
- **B-5.** Última parcela de depreciação que cruzaria o residual ⇒ limitada a `ValorContabil = ValorResidual` (I-4).
- **B-6.** Depreciar bem já em `ValorContabil == ValorResidual` ⇒ parcela zero (sem novo histórico relevante).
- **B-7.** Tombar duas vezes ⇒ a 2ª falha (situação já `Tombado` ≠ `EmIncorporacao`) (I-10).
- **B-8.** Número de tombo duplicado no mesmo tenant ⇒ violação do índice único `(TenantId, NumeroTombamento)`.
- **B-9.** Impairment com `ValorRecuperavel ≥ ValorContabil` ⇒ nada é reconhecido (I-6).
- **B-10.** Baixa sem laudo ⇒ rejeitada e **nenhum** Integration Event publicado (I-7/I-8).
- **B-11.** Alienar sem avaliação prévia ⇒ rejeitada (I-9).
- **B-12.** Transferir/Ceder bem encerrado ⇒ `InvalidOperationException` (I-11).
- **B-13.** `BemIncorporado/Depreciado/Baixado/ReavaliadoIntegrationEvent` devem ser idempotentes no consumidor (Finanças) por `EventId` (reentrega via Outbox).
- **B-14.** Reavaliar/Impairment sem laudo ⇒ rejeitado (I-5/I-6).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — regras derivadas do README do módulo Patrimonio (ciclo de vida do bem: incorporação, tombamento, depreciação linear, reavaliação, impairment, baixa/alienação; MCASP, NBC TSP 07, Lei 4.320, Lei 14.133). |

<!-- manifest
commands: IncorporarBem, TombarBem, DepreciarBem, ReavaliarBem, RegistrarImpairment, TransferirBem, CederBem, BaixarBem, AlienarBem
queries: ObterBemPatrimonial, ListarBensDepreciaveis, ListarMovimentacoesDoBem, BuscarBens
domainEvents: BemIncorporado, BemTombado, BemDepreciado, BemReavaliado, BemBaixado
integrationEventsPublished: BemIncorporadoIntegrationEvent, BemDepreciadoIntegrationEvent, BemBaixadoIntegrationEvent, BemReavaliadoIntegrationEvent
integrationEventsConsumed: ContratoAssinadoIntegrationEvent
-->
