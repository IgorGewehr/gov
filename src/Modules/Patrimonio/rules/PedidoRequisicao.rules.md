---
modulo: Patrimonio
agregado: PedidoRequisicao
contexto: Patrimonio (Almoxarifado — requisicao self-service multi-item por setor/UO)
poder: Ambos
schema: patrimonio
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["Lei 4.320/1964 (controle de almoxarifado; despesa reconhecida no consumo)", "MCASP/STN (Procedimentos Contabeis Patrimoniais)"]
---

# PedidoRequisicao — Regras-as-Code (Rules-as-Code)

> Pedido de requisicao de material do almoxarifado em **fluxo self-service multi-item por
> setor/UO**: o setor solicitante **abre** o pedido (Solicitado), a autoridade **aprova**
> (Aprovado) e o almoxarifado **atende**, gerando a **SAIDA de estoque por item** (baixa do
> `ItemEstoque` existente, respeitando o saldo — atendimento **parcial** permitido), fechando
> em **Atendido**. NAO reimplementa a baixa: **orquestra** `ItemEstoque.AtenderRequisicao` por
> linha, na mesma transacao. O consumo e reconhecido por setor (a despesa nasce no consumo,
> herdado do motor de estoque). Este arquivo e **normativo e versionado**; o codigo
> (`PedidoRequisicao.cs`, `ItemPedido.cs`, handlers, validators, EF config) e consequencia dele.

---

## 1. Linguagem Ubiqua

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Pedido de Requisicao (`PedidoRequisicao`) | Pedido multi-item de material por setor/UO. Raiz de agregado. |
| Item do Pedido (`ItemPedido`) | Linha do pedido: aponta um `ItemEstoque` por Id + quantidade solicitada/atendida. Entidade-filha. |
| Setor Solicitante (`SetorSolicitante`) | Rotulo do consumo por setor. |
| UO consumidora (`UnidadeId`) | Unidade Organizacional dona do consumo (escopo M1, `IMustHaveUnidade`). |
| Situacao (`SituacaoPedido`) | Solicitado -> Aprovado -> Atendido | Cancelado. |

## 2. Maquina de Estados

```
Solicitado --Aprovar--> Aprovado --Atender(baixa estoque)--> Atendido (terminal)
   |                        |
   +--------Cancelar--------+--> Cancelado (terminal)
```

- Atendido e Cancelado sao **terminais** (sem transicoes de saida).

## 3. Invariantes (R)

| # | Invariante | Onde |
|---|---|---|
| R-1 | Pedido nasce em **Solicitado** com **>= 1 linha**; itens repetidos sao consolidados (soma de quantidades); cada item referenciado deve existir e estar **movimentavel** no tenant. | `PedidoRequisicao.Abrir`, `AbrirPedidoHandler` |
| R-2 | So se **aprova** um pedido em **Solicitado** (transicao fora de ordem bloqueada). | `PedidoRequisicao.Aprovar` |
| R-3 | O **atendimento** exige pedido **Aprovado** e baixa o estoque via `ItemEstoque.AtenderRequisicao` (saida + valoracao + despesa no consumo), na **mesma transacao**. Por linha atende **min(pendente, saldo)** — **parcial** permitido; linha de item inativo/sem saldo e pulada. | `AtenderPedidoHandler`, `PedidoRequisicao.RegistrarAtendimentoDeLinha` |
| R-4 | O atendimento **so conclui (Atendido)** se **alguma** quantidade foi baixada; nenhuma baixa => erro (nao fecha vazio). Quantidade atendida por linha **nunca excede** o pendente. | `PedidoRequisicao.ConcluirAtendimento`, `ItemPedido.RegistrarAtendimento` |
| R-5 | **Cancelar** so de Solicitado/Aprovado; pedido **terminal** (Atendido/Cancelado) nao admite cancelamento (saida de estoque e irreversivel por aqui). | `PedidoRequisicao.Cancelar` |

## 4. Reuso (sem duplicar dominio)

- **`ItemEstoque` por Id** (FK logica) — nao cria itens; reusa o motor de saldo/lote/PEPS/medio,
  valoracao da saida e o disparo de **ponto de pedido** (`PontoPedidoAtingidoIntegrationEvent`,
  republicado pelo handler de atendimento para cada item que atingir o gatilho — I-6).

## 5. Eventos

- Dominio: `PedidoRequisicaoAberto`, `PedidoRequisicaoAprovado`, `PedidoRequisicaoAtendido`.
- Integracao (herdado do estoque): `PontoPedidoAtingidoIntegrationEvent` por item que atinge o ponto de pedido.

## 6. Endpoints (`/api/patrimonio/requisicoes`)

| Verbo | Rota | Permissao |
|---|---|---|
| POST | `/` (abrir) | `patrimonio.gerenciar` |
| GET | `/?status=&setor=&unidadeId=` (fila/lista) | `patrimonio.ver` |
| GET | `/{pedidoId}` (detalhe) | `patrimonio.ver` |
| POST | `/{pedidoId}/aprovacao` | `patrimonio.gerenciar` |
| POST | `/{pedidoId}/atendimento` | `patrimonio.gerenciar` |
| POST | `/{pedidoId}/cancelamento` | `patrimonio.gerenciar` |

## 7. Fora de escopo (M10 / outros)

- Compra/reposicao automatica (Administracao/Lei 14.133) — ja coberta via `PontoPedidoAtingidoIntegrationEvent` (Contracts).
- Estorno de saida (devolucao ao estoque) — fora desta sub-onda.

<!-- manifest
commands: AbrirPedido, AprovarPedido, AtenderPedido, CancelarPedido
queries: BuscarPedidos, ObterPedido
domainEvents: PedidoRequisicaoAberto, PedidoRequisicaoAprovado, PedidoRequisicaoAtendido
integrationEventsPublished: 
integrationEventsConsumed: 
-->

