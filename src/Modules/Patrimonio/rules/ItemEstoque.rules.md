---
modulo: Patrimonio
agregado: ItemEstoque
contexto: Patrimonio (Almoxarifado — itens de consumo, lotes, movimentos, requisicoes)
poder: Ambos
schema: patrimonio
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["NBC TSP 12 (Estoques)", "MCASP/STN (Procedimentos Contabeis Patrimoniais)", "Lei 4.320/1964 (controle de almoxarifado e inventario)", "Lei 14.133/2021 (compras/aquisicao que abastecem o estoque)"]
---

# ItemEstoque — Regras-as-Code (Rules-as-Code)

> Item de consumo do **almoxarifado**, controlado por saldo, lotes e movimentos
> (entrada/saída) e requisições de material. Mensurado pelo **menor entre custo e valor
> realizável líquido**, com custeio **PEPS ou médio**; a despesa é reconhecida **no consumo**
> (NBC TSP 12 / MCASP / Lei 4.320). O atingimento do **ponto de pedido** dispara reposição
> junto à Administração. Este arquivo é **normativo e versionado**; o código
> (`ItemEstoque.cs`, handlers, validators, EF config, testes) é consequência dele.

---

## 1. Linguagem Ubíqua

Identificadores entre parênteses são **VINCULANTES** (sem acento, PT-BR no domínio).

| Termo (identificador-no-código) | Definição |
|---|---|
| Item de Estoque (`ItemEstoque`) | Item de consumo controlado por saldo no almoxarifado. Raiz de agregado. |
| Almoxarifado (`Almoxarifado`) | Depósito de itens de consumo controlados por saldo. |
| Lote (`Lote` : entidade) | Partida de entrada com custo unitário e validade próprios (base do PEPS). |
| Movimento de Estoque (`MovimentoEstoque` : entidade) | Entrada ou saída que altera o saldo. |
| Entrada (`RegistrarEntrada` / `TipoMovimento.Entrada`) | Ingresso de itens (compra, doação, devolução). |
| Saída / Consumo (`AtenderRequisicao` / `TipoMovimento.Saida`) | Baixa por requisição; reconhece despesa no consumo. |
| Requisição de Material (`Requisicao` : entidade / `RequisicaoAtendida`) | Pedido de saída de itens do almoxarifado. |
| Saldo de Almoxarifado (`SaldoAlmoxarifado` : VO) | Quantidade disponível do item. |
| Ponto de Pedido (`PontoPedido` : VO / `PontoPedidoAtingido`) | Saldo mínimo que dispara reposição. |
| Curva ABC (`CurvaABC` / `ClassificacaoAbc`) | Classificação do item por relevância de valor/giro. |
| Custeio (`MetodoCusteio` : enum) | Método de valoração da saída: PEPS ou médio. |
| Custo Unitário (`CustoUnitario` : `ValorMonetario`) | Custo de uma unidade do item/lote. |
| Valor Realizável Líquido (`ValorRealizavelLiquido`) | Valor de realização do item, líquido. Piso de mensuração junto ao custo. |
| Tenant (`TenantId`) | Ente público (Prefeitura/Câmara) dono do registro. |
| Situação (`Situacao` : `SituacaoItemEstoque`) | Estado do item no almoxarifado (ativo/inativo). |

---

## 2. Modelo

- **Identidade:** `ItemEstoqueId` — `readonly record struct ItemEstoqueId(Guid Value)`; fábrica `ItemEstoqueId.New()`; `ToString()` => `Value.ToString()`.
- **Raiz de agregado:** `ItemEstoque : AggregateRoot<ItemEstoqueId>, IMustHaveTenant` (`sealed`).
- **Construtores:** privados (um sem parâmetros para o EF; um completo). Nasce válido via factory `Cadastrar(...)`.

### Propriedades

| Propriedade | Tipo | Descrição | Mutabilidade |
|---|---|---|---|
| `TenantId` | `Guid` | Tenant (ente público) dono do registro. | `private set` |
| `Codigo` | `string` | Código do item no catálogo de almoxarifado. | `private set` |
| `Descricao` | `string` | Descrição do item. | `private set` |
| `UnidadeMedida` | `string` | Unidade (un, kg, cx, L). | `private set` |
| `Saldo` | `SaldoAlmoxarifado` (VO) | Quantidade total disponível (Σ saldo dos lotes). | `private set` |
| `PontoPedido` | `PontoPedido` (VO) | Saldo mínimo de reposição. | `private set` |
| `MetodoCusteio` | `MetodoCusteio` (enum) | PEPS ou Médio. | `private set` |
| `CustoMedio` | `ValorMonetario` | Custo médio ponderado atual (quando custeio médio). | `private set` |
| `ValorRealizavelLiquido` | `ValorMonetario?` | VRL informado para teste do menor entre custo e VRL. | `private set` |
| `ClassificacaoAbc` | `CurvaABC` (enum) | Classe A/B/C. | `private set` |
| `Situacao` | `SituacaoItemEstoque` (enum) | Ativo/Inativo. | `private set` |
| `Lotes` | `IReadOnlyCollection<Lote>` | Lotes (PEPS por entrada). | coleção controlada |
| `Movimentos` | `IReadOnlyCollection<MovimentoEstoque>` | Entradas e saídas. | coleção controlada |
| `Requisicoes` | `IReadOnlyCollection<Requisicao>` | Requisições atendidas/pendentes. | coleção controlada |

### Value Objects (referenciados)

- `SaldoAlmoxarifado` — `record struct` com `Quantidade` (`decimal`); nunca negativo.
- `PontoPedido` — `record struct` com `Quantidade` (`decimal`); saldo-gatilho de reposição.
- `ValorMonetario` — custo unitário/médio e VRL; expõe `Valor` (`decimal`). Não nulo onde aplicável.

### Entidades (do agregado)

- `Lote` — número/identificação, `CustoUnitario`, quantidade entrada, quantidade restante, validade, data de entrada (ordena o PEPS).
- `MovimentoEstoque` — `TipoMovimento` (Entrada/Saída), quantidade, valor unitário aplicado, lote(s) afetado(s), data, documento.
- `Requisicao` — solicitante, itens/quantidade, data, situação (pendente/atendida).

### Enum `TipoMovimento`

| Valor | Numérico | Descrição |
|---|---|---|
| `Entrada` | 1 | Ingresso de itens. |
| `Saida` | 2 | Consumo/baixa por requisição. |

### Enum `MetodoCusteio`

| Valor | Numérico | Descrição |
|---|---|---|
| `Peps` | 1 | Primeiro a entrar, primeiro a sair (FIFO). |
| `Medio` | 2 | Custo médio ponderado. |

### Enum `CurvaABC`

| Valor | Numérico | Descrição |
|---|---|---|
| `A` | 1 | Alta relevância de valor/giro. |
| `B` | 2 | Relevância média. |
| `C` | 3 | Baixa relevância. |

### Enum `SituacaoItemEstoque`

| Valor | Numérico | Descrição |
|---|---|---|
| `Ativo` | 1 | Item ativo no almoxarifado. |
| `Inativo` | 2 | Item inativado (terminal para movimentação). |

> **Conjuntos de referência usados nas guardas:**
> - **Movimentável** = `Situacao == Ativo`.
> - **Encerrado** = `Situacao == Inativo` (não admite entrada/saída).

---

## 3. Invariantes

Lista NUMERADA. Cada item `I-n` vira `[Fact] Invariante_n_*`.

- **I-1.** O **saldo nunca é negativo**: uma saída/atendimento de requisição que exceda o saldo disponível é **rejeitada**.
- **I-2.** O item é mensurado pelo **menor entre custo e valor realizável líquido** (NBC TSP 12); quando `ValorRealizavelLiquido < custo`, a mensuração do item adota o VRL (ajuste/redução ao valor realizável).
- **I-3.** A **saída** valora pelo método de custeio do item: **PEPS** (consome lotes em ordem de entrada) ou **médio** (`CustoMedio`); o valor unitário aplicado deriva exclusivamente do método.
- **I-4.** A **despesa é reconhecida no consumo** (na saída), não na entrada (Lei 4.320 / MCASP); a entrada apenas incorpora estoque.
- **I-5.** A **entrada** cria/atualiza `Lote` (PEPS) e recalcula o `CustoMedio` ponderado (médio); incrementa o `Saldo`.
- **I-6.** Quando, **após** uma saída/atendimento de requisição, o `Saldo` atinge ou fica abaixo do `PontoPedido`, emite-se `PontoPedidoAtingido` (e publica-se `PontoPedidoAtingidoIntegrationEvent` à Administração).
- **I-7.** O **atendimento de requisição** (`AtenderRequisicao`) gera `MovimentoEstoque` de `Saida`, reduz o `Saldo`, valora pelo custeio (I-3) e emite `RequisicaoAtendida`.
- **I-8.** **PEPS:** a saída consome primeiro os lotes mais antigos (menor data de entrada); o custo da saída é a soma ponderada das parcelas de cada lote consumido.
- **I-9.** Item **inativo** (`Situacao == Inativo`) não admite entrada nem saída (I-1/I-5 só sobre item movimentável).
- **I-10.** No **cadastro**, `Codigo`, `Descricao`, `UnidadeMedida` e `MetodoCusteio` são obrigatórios; `PontoPedido ≥ 0`; saldo inicial = 0; situação `Ativo`.
- **I-11.** A **quantidade** de qualquer movimento (entrada/saída/requisição) é **estritamente positiva**; quantidade ≤ 0 é rejeitada.
- **I-12.** O **inventário** do almoxarifado concilia saldo físico × contábil; divergências são tratadas por ajuste auditado (não há "acerto" silencioso de saldo).

---

## 4. Máquina de Estados

Tabela: Estado origem → comando/método → Estado destino | guarda | evento emitido.

> A `Situacao` do item (`Ativo`/`Inativo`) muda apenas em cadastro/inativação; entradas, saídas e requisições **não** mudam a `Situacao` (alteram saldo/lotes/movimentos).

| Origem | Comando / método | Destino | Guarda | Evento de domínio |
|---|---|---|---|---|
| (nenhum) | `Cadastrar` | `Ativo` | campos obrigatórios; `PontoPedido ≥ 0` | — |
| `Ativo` | `RegistrarEntrada` | `Ativo` | item movimentável; quantidade > 0 | — |
| `Ativo` | `AtenderRequisicao` | `Ativo` | item movimentável; quantidade > 0; saldo suficiente | `RequisicaoAtendida` (+ `PontoPedidoAtingido` se saldo ≤ ponto) |
| `Ativo` | `AjustarValorRealizavelLiquido` | `Ativo` | VRL ≥ 0 | — |
| `Ativo` | `ReclassificarAbc` | `Ativo` | classe válida (A/B/C) | — |
| `Ativo` | `InativarItem` | `Inativo` | saldo == 0 | — |

> Observações:
> - `AtenderRequisicao` verifica **saldo suficiente** (I-1) **antes** de valorar e baixar.
> - `PontoPedidoAtingido` é emitido **dentro** de `AtenderRequisicao` quando o saldo resultante ≤ `PontoPedido` (I-6).
> - `InativarItem` só é permitido com **saldo zero** (não se inativa item com estoque remanescente).

---

## 5. Comandos (escrita)

### 5.1 CadastrarItemEstoque

- **Command:** `CadastrarItemEstoqueCommand(string Codigo, string Descricao, string UnidadeMedida, int MetodoCusteio, decimal PontoPedido, int ClassificacaoAbc) : ICommand<Guid>`.
- **Entrada (DTO):** código, descrição, unidade, método de custeio (PEPS/médio), ponto de pedido, classe ABC.
- **Dependências do handler:** `IItemEstoqueRepository`, `IUnitOfWork`, `ITenantContext`.
- **Pré-condições:** `request` não nulo; campos obrigatórios (I-10); `PontoPedido ≥ 0`; `Codigo` único por tenant.
- **Efeito:** cria via `ItemEstoque.Cadastrar(tenant.TenantId, ...)`; `itens.Adicionar(item)`; `SaveChangesAsync`.
- **Pós-condições:** item `Ativo`, saldo 0; retorna `item.Id.Value` (`Guid`).
- **Exceções:** `ArgumentNullException`; `ArgumentException` (campos inválidos); `InvalidOperationException` (código duplicado).
- **Evento de domínio:** — (cadastro; sem evento publicado nesta versão).

### 5.2 RegistrarEntrada

- **Command:** `RegistrarEntradaCommand(Guid ItemEstoqueId, decimal Quantidade, decimal CustoUnitario, DateOnly DataEntrada, DateOnly? Validade, string Documento) : ICommand`.
- **Entrada (DTO):** item, quantidade, custo unitário, data, validade (lote), documento (NF/recebimento).
- **Dependências do handler:** `IItemEstoqueRepository`, `IUnitOfWork`.
- **Pré-condições:** `request` não nulo; item existe; **movimentável** (I-9); `Quantidade > 0` (I-11); `CustoUnitario ≥ 0`.
- **Efeito:** `item.RegistrarEntrada(...)`; cria/atualiza `Lote`, recalcula `CustoMedio` (I-5), gera `MovimentoEstoque(Entrada)`, incrementa `Saldo`; `SaveChangesAsync`.
- **Pós-condições:** saldo aumentado; lote registrado; **sem** reconhecimento de despesa (I-4).
- **Exceções:** `ArgumentNullException`; `ArgumentException`; `InvalidOperationException` (não encontrado / inativo).
- **Evento de domínio:** — (entrada; sem evento publicado nesta versão).

### 5.3 AtenderRequisicao

- **Command:** `AtenderRequisicaoCommand(Guid ItemEstoqueId, Guid RequisicaoId, Guid SolicitanteId, decimal Quantidade, DateOnly Data) : ICommand`.
- **Entrada (DTO):** item, requisição, solicitante, quantidade, data.
- **Dependências do handler:** `IItemEstoqueRepository`, `IUnitOfWork`, `IPublisher` (MediatR), `ITenantContext`.
- **Pré-condições:** `request` não nulo; item existe; movimentável; `Quantidade > 0` (I-11); **saldo suficiente** (I-1).
- **Efeito:** `item.AtenderRequisicao(...)` — valora pelo custeio (PEPS consome lotes/médio aplica `CustoMedio`, I-3/I-8), gera `MovimentoEstoque(Saida)`, reduz `Saldo`, **reconhece despesa no consumo** (I-4); `SaveChangesAsync`. Se saldo resultante ≤ `PontoPedido`, publica `PontoPedidoAtingidoIntegrationEvent` (I-6).
- **Pós-condições:** saldo reduzido; requisição `atendida`; emite `RequisicaoAtendida` e, condicionalmente, `PontoPedidoAtingido`.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / inativo / **saldo insuficiente**).
- **Evento de domínio:** `RequisicaoAtendida(ItemEstoqueId, requisicaoId, quantidade)`; condicional `PontoPedidoAtingido(ItemEstoqueId, saldoAtual)`.
- **Evento de integração (publica):** `PontoPedidoAtingidoIntegrationEvent` (condicional).

### 5.4 AjustarValorRealizavelLiquido

- **Command:** `AjustarValorRealizavelLiquidoCommand(Guid ItemEstoqueId, decimal ValorRealizavelLiquido, DateOnly Data) : ICommand`.
- **Pré-condições:** `request` não nulo; item existe; `ValorRealizavelLiquido ≥ 0`.
- **Efeito:** `item.AjustarValorRealizavelLiquido(vrl, data)`; aplica o **menor entre custo e VRL** (I-2); `SaveChangesAsync`.
- **Pós-condições:** mensuração reduzida ao VRL quando VRL < custo.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado).
- **Evento de domínio:** — (ajuste de mensuração; sem evento publicado nesta versão).

### 5.5 ReclassificarAbc

- **Command:** `ReclassificarAbcCommand(Guid ItemEstoqueId, int ClassificacaoAbc) : ICommand`.
- **Pré-condições:** `request` não nulo; item existe; classe válida (A/B/C).
- **Efeito:** `item.ReclassificarAbc(classe)`; `SaveChangesAsync`.
- **Pós-condições:** `ClassificacaoAbc` atualizada.
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado).
- **Evento de domínio:** —.

### 5.6 InativarItem

- **Command:** `InativarItemCommand(Guid ItemEstoqueId) : ICommand`.
- **Pré-condições:** `request` não nulo; item existe; **saldo == 0**.
- **Efeito:** `item.Inativar()`; situação → `Inativo`; `SaveChangesAsync`.
- **Pós-condições:** item `Inativo` (não movimentável).
- **Exceções:** `ArgumentNullException`; `InvalidOperationException` (não encontrado / saldo > 0).
- **Evento de domínio:** —.

---

## 6. Consultas (leitura)

### 6.1 ObterItemEstoque

- **Query:** `ObterItemEstoqueQuery(Guid ItemEstoqueId) : IQuery<ItemEstoqueDetalhe>`.
- **Handler:** `ObterItemEstoqueHandler(IItemEstoqueRepository itens)`.
- **Projeção (DTO):** `ItemEstoqueDetalhe(Guid Id, string Codigo, string Descricao, string UnidadeMedida, decimal Saldo, decimal PontoPedido, string MetodoCusteio, decimal CustoMedio, string ClassificacaoAbc, string Situacao)`.
- **Filtros:** por `Id`; **sempre tenant-scoped** via Global Query Filter por `TenantId`.
- **Pré-condições:** `request` não nulo (`ArgumentNullException.ThrowIfNull`).

### 6.2 ListarItensAbaixoDoPontoPedido

- **Query:** `ListarItensAbaixoDoPontoPedidoQuery() : IQuery<IReadOnlyList<ItemReposicaoResumo>>`.
- **Handler:** lista itens `Ativo` cujo `Saldo ≤ PontoPedido` (gatilho de reposição/compras).
- **Projeção (DTO):** `ItemReposicaoResumo(Guid Id, string Codigo, string Descricao, decimal Saldo, decimal PontoPedido)`.
- **Filtros:** tenant-scoped.

### 6.3 ListarMovimentosDoItem

- **Query:** `ListarMovimentosDoItemQuery(Guid ItemEstoqueId, DateOnly De, DateOnly Ate) : IQuery<IReadOnlyList<MovimentoResumo>>`.
- **Projeção (DTO):** `MovimentoResumo(Guid Id, string Tipo, decimal Quantidade, decimal ValorUnitario, DateOnly Data, string Documento)`.
- **Filtros:** por `ItemEstoqueId` e período; tenant-scoped.

### 6.4 ObterPosicaoCurvaAbc

- **Query:** `ObterPosicaoCurvaAbcQuery() : IQuery<IReadOnlyList<PosicaoAbcResumo>>`.
- **Projeção (DTO):** `PosicaoAbcResumo(string Classe, int QuantidadeItens, decimal ValorTotal)`.
- **Filtros:** tenant-scoped; agrega itens por classe A/B/C.

---

## 7. Eventos

### Domínio (in-process, MediatR; assembly `...Patrimonio.Domain.Events`)

| Evento | Payload | Emitido por |
|---|---|---|
| `RequisicaoAtendida` | `(ItemEstoqueId, RequisicaoId, decimal quantidade)` | `ItemEstoque.AtenderRequisicao` |
| `PontoPedidoAtingido` | `(ItemEstoqueId, decimal saldoAtual)` | `ItemEstoque.AtenderRequisicao` (condicional) |

### Integração (publica via `*.Contracts` + Outbox; assembly `...Patrimonio.Contracts`)

| Evento | Payload | Publicado por | Destino |
|---|---|---|---|
| `PontoPedidoAtingidoIntegrationEvent` | `{ ItemId, SaldoAtual }` | `AtenderRequisicaoHandler` | Administracao (gatilho de compra/reposição) |

### Integração (consome)

| Evento | Payload | Origem | Efeito |
|---|---|---|---|
| `ContratoAssinadoIntegrationEvent` | `{ ContratoId, ... }` | Administracao | Aquisição via licitação consome contrato/recebimento, disparando **entrada de estoque** (`RegistrarEntrada`). |

---

## 8. Validações (FluentValidation)

### CadastrarItemEstoqueValidator (`AbstractValidator<CadastrarItemEstoqueCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `Codigo` | `NotEmpty()` + `MaximumLength(40)` | Código do item obrigatório, máx. 40 caracteres. |
| `Descricao` | `NotEmpty()` + `MaximumLength(200)` | Descrição obrigatória, máx. 200 caracteres. |
| `UnidadeMedida` | `NotEmpty()` + `MaximumLength(10)` | Unidade de medida obrigatória. |
| `MetodoCusteio` | `IsInEnum()` | Método de custeio inválido (PEPS/Médio). |
| `PontoPedido` | `GreaterThanOrEqualTo(0)` | Ponto de pedido não pode ser negativo. |
| `ClassificacaoAbc` | `IsInEnum()` | Classe ABC inválida. |

### RegistrarEntradaValidator (`AbstractValidator<RegistrarEntradaCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ItemEstoqueId` | `NotEmpty()` | Identificador obrigatório. |
| `Quantidade` | `GreaterThan(0)` | Quantidade deve ser positiva (I-11). |
| `CustoUnitario` | `GreaterThanOrEqualTo(0)` | Custo unitário inválido. |

### AtenderRequisicaoValidator (`AbstractValidator<AtenderRequisicaoCommand>`)

| Campo | Regra | Mensagem |
|---|---|---|
| `ItemEstoqueId` | `NotEmpty()` | Identificador obrigatório. |
| `RequisicaoId` | `NotEmpty()` | Requisição obrigatória. |
| `SolicitanteId` | `NotEmpty()` | Solicitante obrigatório. |
| `Quantidade` | `GreaterThan(0)` | Quantidade deve ser positiva (I-11). |

---

## 9. Persistência (EF Core 8)

- **Schema:** `patrimonio` (isolado por módulo). **DbContext:** o do módulo Patrimonio. **Migrations:** por módulo.
- **Tabela:** `ItemEstoque` (raiz de agregado). Tabelas filhas: `Lote`, `MovimentoEstoque`, `Requisicao`.

| Coluna | Tipo lógico | Conversor / observação |
|---|---|---|
| `Id` | `Guid` (PK) | conversor de `ItemEstoqueId` ↔ `Guid`. |
| `TenantId` | `Guid` | carimbado pelo `SaveChangesInterceptor`; alvo do Global Query Filter. |
| `Codigo` | `nvarchar(40)` | código do item. |
| `Descricao` | `nvarchar(200)` | descrição. |
| `UnidadeMedida` | `nvarchar(10)` | unidade. |
| `Saldo` | `decimal` | conversor de `SaldoAlmoxarifado`. |
| `PontoPedido` | `decimal` | conversor de `PontoPedido`. |
| `MetodoCusteio` | `int` | enum `MetodoCusteio`. |
| `CustoMedio` | `decimal` | owned/conversor de `ValorMonetario`. |
| `ValorRealizavelLiquido` | `decimal?` | owned/conversor de `ValorMonetario`; nulável. |
| `ClassificacaoAbc` | `int` | enum `CurvaABC`. |
| `Situacao` | `int` | enum `SituacaoItemEstoque`. |

- **Tabela `Lote`:** `Id`, `ItemEstoqueId` (FK), `CustoUnitario` (decimal), `QuantidadeEntrada`, `QuantidadeRestante`, `Validade?`, `DataEntrada` (ordena o PEPS).
- **Índices:**
  - PK em `Id`.
  - **Único** em `(TenantId, Codigo)` (unicidade do item por tenant — I-10).
  - Índice em `(TenantId, Situacao)` e índice de cobertura para `Saldo ≤ PontoPedido` (consulta de reposição).
  - Índice em `Lote(ItemEstoqueId, DataEntrada)` para o PEPS (I-8).
- **Conversores (VO/Id):** Fluent API (sem data annotations no domínio).
- **Outbox:** tabela Outbox do contexto Patrimonio para `PontoPedidoAtingidoIntegrationEvent` (consistência transacional com a baixa de saldo).

---

## 10. Segurança, Tenant e Auditoria

- **Tenant:** `ItemEstoque` implementa `IMustHaveTenant`. `TenantId` carimbado na inserção; **Global Query Filter** por `TenantId` em todas as consultas. Gravação cross-tenant **lança exceção**. `ITenantContext` resolvido do JWT por requisição.
- **RBAC (policy-based, negar por padrão; segregação de funções):**
  - Cadastrar item / registrar entrada: papéis do **Almoxarifado** (ex.: `Patrimonio.Almoxarifado.Gerir`).
  - Atender requisição: papel de **atendimento de almoxarifado**, **segregado** de quem **requisita** (o solicitante não atende a própria requisição).
  - Consultas: papel de **leitura de almoxarifado** (ex.: `Patrimonio.Almoxarifado.Ler`).
  - Módulo Patrimonio é **ativável por tenant** (Ambos); requisição a tenant sem licença → 404/403 auditado.
- **LGPD:** registra solicitantes (`SolicitanteId`) de requisições; minimização nas projeções. Sem dados sensíveis de saúde/assistência.
- **Auditoria imutável:** `AuditSaveChangesInterceptor` grava trilha (antes/depois em JSON, usuário, IP, timestamp) em **toda** mutação (`Cadastrar`, `RegistrarEntrada`, `AtenderRequisicao`, `AjustarValorRealizavelLiquido`, `ReclassificarAbc`, `Inativar`) — base do inventário e da conciliação físico×contábil (I-12) para o Tribunal de Contas (TCE-RS).
- **Anti-SQLi:** acesso via EF parametrizado; proibido SQL concatenado.

---

## 11. Integrações Governamentais

- **Saída — Administracao (intra-aplicação, via Contracts):** ao atingir o ponto de pedido após uma saída, publica `PontoPedidoAtingidoIntegrationEvent` (Outbox) como gatilho de compra/reposição na Administração (Lei 14.133). Idempotente por `EventId`. **Nunca** há chamada direta.
- **Entrada — Administracao (intra-aplicação):** aquisição via licitação consome `ContratoAssinadoIntegrationEvent`/recebimento, disparando a **entrada de estoque** (`RegistrarEntrada`). ACL + idempotência.
- **Saída — Finanças (previsto):** o consumo (saída) reconhece despesa (Lei 4.320 / MCASP); o reconhecimento contábil junto a Finanças, quando orquestrado, ocorre via Integration Event/Outbox (idempotente). Mensuração pelo menor entre custo e VRL (NBC TSP 12).
- **Resiliência:** toda I/O externa idempotente, com timeout, retry e circuit breaker (Polly); eventos via Outbox; mapeamento explícito de erros atrás de Anti-Corruption Layer.

---

## 12. Cenários BDD

Cada cenário vira teste de integração.

**Cenário 1 — Reposição de almoxarifado (ponto de pedido)**
- **Dado** um item cujo saldo atinge o ponto de pedido após uma requisição
- **Quando** a `RequisicaoAtendida` é processada
- **Então** o evento `PontoPedidoAtingido` é publicado para a Administracao (I-6) e `PontoPedidoAtingidoIntegrationEvent` é emitido.

**Cenário 2 — Saída além do saldo**
- **Dado** um item com saldo 10 unidades
- **Quando** executo `AtenderRequisicaoCommand` para 15 unidades
- **Então** a operação é rejeitada por **saldo insuficiente** e o saldo permanece 10 (I-1).

**Cenário 3 — Custeio PEPS**
- **Dado** um item PEPS com lote A (10 un a R$ 2) e lote B (10 un a R$ 3), nesta ordem de entrada
- **Quando** atendo requisição de 15 unidades
- **Então** consome 10 do lote A e 5 do lote B; o custo da saída = 10×2 + 5×3 = R$ 35 (I-3/I-8).

**Cenário 4 — Custeio médio**
- **Dado** um item de custeio médio com `CustoMedio` atual de R$ 2,50
- **Quando** atendo requisição de 4 unidades
- **Então** o custo da saída = 4 × R$ 2,50 = R$ 10,00 (I-3).

**Cenário 5 — Despesa reconhecida no consumo**
- **Dado** uma entrada de itens no almoxarifado
- **Quando** registro a entrada
- **Então** **nenhuma** despesa é reconhecida; a despesa só é reconhecida na **saída**/consumo (I-4).

**Cenário 6 — Menor entre custo e VRL**
- **Dado** um item cujo valor realizável líquido informado é inferior ao custo
- **Quando** executo `AjustarValorRealizavelLiquidoCommand`
- **Então** o item passa a ser mensurado pelo VRL (menor que o custo) (I-2; NBC TSP 12).

**Cenário 7 — Entrada incrementa saldo e atualiza custeio**
- **Dado** um item ativo
- **Quando** executo `RegistrarEntradaCommand(quantidade, custoUnitario)`
- **Então** o saldo aumenta, um `Lote` é criado (PEPS) e o `CustoMedio` é recalculado (médio) (I-5).

**Cenário 8 — Atendimento normal de requisição**
- **Dado** um item com saldo suficiente acima do ponto de pedido
- **Quando** executo `AtenderRequisicaoCommand`
- **Então** gera `MovimentoEstoque(Saida)`, reduz o saldo, emite `RequisicaoAtendida` e **não** emite `PontoPedidoAtingido` (saldo ainda acima do ponto).

**Cenário 9 — Movimentação sobre item inativo**
- **Dado** um item `Inativo`
- **Quando** executo `RegistrarEntradaCommand` ou `AtenderRequisicaoCommand`
- **Então** a operação é rejeitada por item inativo (I-9).

**Cenário 10 — Inativação exige saldo zero**
- **Dado** um item com saldo > 0
- **Quando** executo `InativarItemCommand`
- **Então** a inativação é rejeitada (saldo remanescente).

**Cenário 11 — Quantidade não positiva**
- **Dado** um item ativo
- **Quando** executo entrada/saída com quantidade ≤ 0
- **Então** a operação é rejeitada (I-11).

**Cenário 12 — Consulta tenant-scoped**
- **Dado** itens do tenant A e do tenant B
- **Quando** executo `ObterItemEstoqueQuery` no contexto do tenant A
- **Então** somente itens do tenant A são retornados.

---

## 13. Casos de Borda

- **B-1.** Saída exatamente igual ao saldo ⇒ permitida; saldo resultante 0 (I-1).
- **B-2.** Saída que zera o saldo e o ponto de pedido é > 0 ⇒ emite `PontoPedidoAtingido` (saldo 0 ≤ ponto) (I-6).
- **B-3.** PEPS com lote insuficiente seguido de outro lote ⇒ consome em cascata por ordem de entrada (I-8).
- **B-4.** Entrada com `CustoUnitario == 0` (doação) ⇒ permitida; recalcula custeio com custo zero.
- **B-5.** VRL ≥ custo ⇒ mantém mensuração pelo custo (sem redução) (I-2).
- **B-6.** Atender requisição de item inativo ⇒ rejeitado (I-9).
- **B-7.** Inativar item com saldo 0 ⇒ permitido; com saldo > 0 ⇒ rejeitado.
- **B-8.** Quantidade 0 ou negativa em qualquer movimento ⇒ rejeitada (I-11 / validator).
- **B-9.** Código duplicado por tenant ⇒ violação do índice único `(TenantId, Codigo)` (I-10).
- **B-10.** Saldo nunca fica negativo por concorrência ⇒ a baixa é transacional; saída concorrente que excederia o saldo falha (I-1).
- **B-11.** `PontoPedidoAtingidoIntegrationEvent` deve ser idempotente no consumidor (Administracao) por `EventId` (reentrega via Outbox).
- **B-12.** Conciliação de inventário (físico × contábil) com divergência ⇒ ajuste **auditado**, nunca acerto silencioso (I-12).

---

## 14. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-21 | Versão inicial — regras derivadas do README do módulo Patrimonio (almoxarifado: itens, lotes, movimentos, requisições; saldo não negativo, PEPS/médio, despesa no consumo, menor entre custo e VRL, ponto de pedido; NBC TSP 12 / MCASP / Lei 4.320). |

<!-- manifest
commands: CadastrarItemEstoque, RegistrarEntrada, AtenderRequisicao, AjustarValorRealizavelLiquido, ReclassificarAbc, InativarItem
queries: ObterItemEstoque, ListarItensAbaixoDoPontoPedido, ListarMovimentosDoItem, ObterPosicaoCurvaAbc, BuscarItensEstoque
domainEvents: RequisicaoAtendida, PontoPedidoAtingido
integrationEventsPublished: PontoPedidoAtingidoIntegrationEvent
integrationEventsConsumed: ContratoAssinadoIntegrationEvent
-->
