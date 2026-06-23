# Inventário — Rules-as-Code (Patrimônio)

Agregado **Inventario** (Lei 4.320 art. 96): levantamento anual/por setor, comissão designada,
coleta física item-a-item, conciliação **físico × contábil**, registro de **divergências** (falta,
sobra, divergência de localização/estado/valor) e encerramento que publica recomendações
(`InventarioEncerrado` via Outbox). NÃO muta `BemPatrimonial` diretamente — emite recomendações que o
operador efetiva pelos comandos de bem já existentes (baixa/transferência/incorporação/reavaliação).

## Fluxo (máquina de estados)
EmAbertura → (CarregarSnapshot) EmContagem → (Conciliar) EmConciliacao → (Encerrar) Encerrado | Cancelado.
O snapshot contábil é congelado (imutável) no momento do carregamento — não navega ao bem ao vivo (evita drift).

## Invariantes
- Comissão ≥ 3 membros + portaria.
- Snapshot congela; não muda se o acervo mudar depois.
- `Encerrar` exige conciliação feita.
- Conciliação: no snapshot e não contado ⇒ Falta; contado em setor ≠ esperado ⇒ DivergenciaLocalizacao;
  achado sem tombo ⇒ Sobra; estado/valor divergente ⇒ DivergenciaEstado/Valor.

## Endpoints
- `POST /api/patrimonio/inventarios` — AbrirInventarioCommand `[patrimonio.gerenciar]`
- `POST /api/patrimonio/inventarios/{id}/snapshot` — CarregarSnapshotCommand `[patrimonio.gerenciar]`
- `POST /api/patrimonio/inventarios/{id}/contagens` — RegistrarContagemCommand `[patrimonio.gerenciar]`
- `POST /api/patrimonio/inventarios/{id}/sobras` — RegistrarBemNaoCadastradoCommand `[patrimonio.gerenciar]`
- `POST /api/patrimonio/inventarios/{id}/conciliacao` — ConciliarInventarioCommand `[patrimonio.gerenciar]`
- `POST /api/patrimonio/inventarios/{id}/encerramento` — EncerrarInventarioCommand `[patrimonio.gerenciar]`
- `POST /api/patrimonio/inventarios/{id}/cancelamento` — CancelarInventarioCommand `[patrimonio.gerenciar]`
- `GET /api/patrimonio/inventarios` — BuscarInventariosQuery (paginado) `[patrimonio.ver]`
- `GET /api/patrimonio/inventarios/{id}` — ObterInventarioQuery `[patrimonio.ver]`
- `GET /api/patrimonio/inventarios/{id}/divergencias` — ListarDivergenciasQuery `[patrimonio.ver]`

<!-- manifest
commands: AbrirInventario, CarregarSnapshot, RegistrarContagem, RegistrarBemNaoCadastrado, ConciliarInventario, EncerrarInventario, CancelarInventario
queries: BuscarInventarios, ObterInventario, ListarDivergencias
domainEvents: InventarioAberto, InventarioEncerrado
integrationEventsPublished: InventarioEncerradoIntegrationEvent
integrationEventsConsumed: 
-->
