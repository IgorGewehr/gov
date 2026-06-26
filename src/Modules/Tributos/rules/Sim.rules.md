# Regras — S.I.M. (Serviço de Inspeção Municipal — título de registro)

<!-- manifest
commands: RequererTituloSim, ConcederTituloSim, HabilitarProdutoSim, AlterarSituacaoTituloSim
queries: 
domainEvents: TituloRegistroSimRequerido, TituloRegistroSimConcedido, TituloRegistroSimSuspenso, TituloRegistroSimCassado, TituloRegistroSimRenovado
integrationEventsPublished: 
integrationEventsConsumed: 
-->

> Bounded Context: **Tributos** — registro de estabelecimentos no **Serviço de Inspeção Municipal (S.I.M.)**
> para produtos de **origem animal/vegetal**. Paridade com o incumbente **SAPI**. Base legal: **Lei
> 7.889/1989**, **Decreto 9.013/2017 (RIISPOA)**, **Lei 13.680/2018 + Decreto 9.918/2019 (SISBI/SUASA)**.
> Ato de **polícia sanitária** (CTN art. 77) — a Taxa de inspeção/licença (TLL) correlata é lançada à
> parte como `Taxa`.

---

## 1. Linguagem ubíqua

- **Título de registro (`TituloRegistroSim`)** — habilita o estabelecimento a industrializar/comercializar
  no âmbito municipal. Atribui o **número do S.I.M.** ao conceder; controla vigência (renovável).
- **Produto inspecionado (`ProdutoInspecionadoSim`)** — denominação de venda, classificação sanitária e
  número do rótulo aprovado (controle de rotulagem — Decreto 9.013/2017).
- **Situação** — EmAnalise → Registrado → (Suspenso ⇄ Registrado) → Cassado.

---

## 2. Comandos (escrita)

- **`RequererTituloSimCommand`** → protocola o requerimento (em análise) e habilita os produtos informados.
- **`ConcederTituloSimCommand`** → concede o registro: atribui o **número do S.I.M.** (sequencial do
  tenant por exercício) e fixa a vigência. Exige ≥ 1 produto habilitado.
- **`HabilitarProdutoSimCommand`** → habilita novo produto (com rótulo) num título existente.
- **`AlterarSituacaoTituloSimCommand`** → suspende (irregularidade sanitária) / reativa / cassa, com motivo
  auditável obrigatório para suspender/cassar.

---

## 3. Eventos

- **Domínio (in-process):** `TituloRegistroSimRequerido`, `TituloRegistroSimConcedido`,
  `TituloRegistroSimSuspenso`, `TituloRegistroSimCassado`, `TituloRegistroSimRenovado`.
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 4. Pendências `// TODO(validar-oficial)`

- **Máscara/sequência oficial** do número do S.I.M. conforme o regulamento municipal.
- Integração de consulta pública ao **e-SISBI/SGSI** (cadastro nacional) — defere (sem credencial).
- Vínculo automático da **TLL de inspeção** à concessão/renovação (parametrização por tenant).
