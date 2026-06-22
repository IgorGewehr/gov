# Módulo Patrimonio
> Controle do ciclo de vida de bens, estoques de almoxarifado e frota, com mensuração contábil patrimonial conforme MCASP. · Poder: Ambos · Schema EF Core: `patrimonio` · Ativável por tenant.

## 1. Propósito & Marco Legal
Gerir o patrimônio público em todo o seu ciclo — incorporação, tombamento, movimentação, depreciação, reavaliação, impairment e baixa/alienação — além de almoxarifado (estoques de consumo) e frota (veículos). Garante a fidedignidade do registro físico e contábil e a conciliação físico×contábil exigida no encerramento do exercício.
Marco legal: **MCASP/STN** (Procedimentos Contábeis Patrimoniais — imobilizado, depreciação, reavaliação, impairment); **NBC TSP 07** (Ativo Imobilizado) e **NBC TSP 12** (Estoques); **Lei 4.320/1964** (controle patrimonial e inventário); **Lei 14.133/2021** (alienação art. 76, leilão art. 31, avaliação prévia, doação); **Código Civil** (comodato/cessão); **Lei 9.503/1997 – CTB** (licenciamento, IPVA, multas, CNH da frota).

## 2. Linguagem Ubíqua
- **Tombamento / NumeroDeTombo** — ato e identificador único de registro do bem no patrimônio.
- **Incorporacao** — ingresso do bem ao acervo (aquisição, doação, produção própria).
- **Baixa** — exclusão do bem do acervo (inservível, perda, alienação).
- **Alienacao** — transferência onerosa de domínio (regra: leilão, com avaliação prévia).
- **Cessao / Comodato** — uso por terceiro sem (cessão) ou a título gratuito (comodato).
- **Transferencia** — mudança de localização/responsável dentro do tenant.
- **Depreciacao** — alocação sistemática do valor depreciável ao longo da vida útil.
- **ValorResidual / ValorDepreciavel** — resíduo estimado ao fim da vida útil; custo − residual.
- **Reavaliacao** — ajuste do valor contábil ao valor justo.
- **Impairment** — redução ao valor recuperável quando inferior ao contábil.
- **Inventario / ComissaoDeInventario** — levantamento físico periódico por comissão segregada.
- **Almoxarifado** — depósito de itens de consumo controlados por saldo.
- **RequisicaoDeMaterial** — pedido de saída de itens do almoxarifado.
- **CurvaABC** — classificação de itens por relevância de valor/giro.
- **PontoDePedido** — saldo mínimo que dispara reposição.
- **Horimetro / Odometro** — horas de uso (máquinas) / quilometragem (frota).
- **OrdemDeServico** — registro de manutenção de veículo/equipamento.

## 3. Mapa de Domínio
**Agregados (raiz IMustHaveTenant):**
- **BemPatrimonial** (móvel/imóvel) — VOs `NumeroTombamento`, `Depreciacao`; entidades `MovimentacaoPatrimonial`, `HistoricoDepreciacao`, `Reavaliacao`, `Impairment`. Imóvel desmembrável em terreno + benfeitoria.
- **Estoque** — entidades `Item`, `Lote`, `MovimentoEstoque` (entrada/saída), `Requisicao`; VOs `SaldoAlmoxarifado`, `PontoPedido`.
- **Veiculo** (é-um BemPatrimonial) — `placa`/`RENAVAM`, entidades `Abastecimento`, `ManutencaoOS`, `Multa`, `Licenciamento`, `Motorista` (CNH); VOs `Odometro`, `Horimetro`.

**Eventos de Domínio:** `BemTombado`, `BemIncorporado`, `BemDepreciado`, `BemReavaliado`, `BemBaixado`, `RequisicaoAtendida`, `PontoPedidoAtingido`, `AbastecimentoRegistrado`, `ManutencaoConcluida`, `MultaRegistrada`.

## 4. Integrações & Padrões Técnicos
- **MediatR** para Commands/Queries; eventos de domínio publicados in-process e projetados via **Outbox** para Integration Events.
- **Financas** — incorporação, baixa e depreciação geram lançamento contábil/variação patrimonial (via Integration Events; nunca chamada direta).
- **Administracao** — aquisição via licitação consome `ContratoAssinado`/recebimento, disparando tombamento ou entrada de estoque.
- **Georreferenciamento** de imóveis (GIS/matrícula) e **gestor de combustível** (cota por veículo).
- Persistência **EF Core 8** no schema `patrimonio`, com `IMustHaveTenant` aplicado por global query filter.

## 5. Regras de Negócio Críticas
- Depreciação **linear**; início quando o bem está em condições de uso.
- **Terreno NÃO deprecia; edificação SIM** — imóvel é desmembrado em terreno + benfeitoria.
- Valor contábil **nunca inferior ao residual**.
- **Reavaliacao** a valor justo; **impairment** quando valor recuperável < contábil.
- **Baixa** exige laudo/parecer + autorização + lançamento contábil simultâneo.
- **Alienacao** requer avaliação prévia e, em regra, leilão (Lei 14.133 art. 31/76).
- **Inventario anual** com comissão ≥ 3 membros sem vínculo com a área (segregação) e conciliação físico×contábil.
- **Estoque** mensurado pelo menor entre custo e valor realizável líquido; custeio **PEPS/médio**; despesa reconhecida no consumo.

## 6. Multi-Tenancy, Segurança & Auditoria
Todo agregado implementa `IMustHaveTenant`; `TenantId` filtrado globalmente e validado em comandos. Acesso por papéis (RBAC) com segregação entre quem requisita, autoriza e inventaria. Toda movimentação patrimonial, baixa, reavaliação e impairment é auditada (autor, data, justificativa, laudo anexado). Eventos persistidos via Outbox garantem trilha e entrega ao menos uma vez.

## 7. Contratos Públicos (Integration Events)
- `BemIncorporadoIntegrationEvent` { TombamentoId, ValorInicial, Origem } → Financas (variação patrimonial aumentativa).
- `BemDepreciadoIntegrationEvent` { TombamentoId, ValorDepreciado, Competencia } → Financas.
- `BemBaixadoIntegrationEvent` { TombamentoId, MotivoBaixa, ValorContabil } → Financas.
- `BemReavaliadoIntegrationEvent` { TombamentoId, NovoValorJusto } → Financas.
- `PontoPedidoAtingidoIntegrationEvent` { ItemId, SaldoAtual } → Administracao (gatilho de compra).

## 8. Cenários BDD
**Depreciação de imóvel**
Dado um imóvel incorporado com terreno e edificação
Quando o processamento mensal de depreciação é executado
Então apenas a edificação é depreciada e o valor contábil do terreno permanece inalterado.

**Limite do valor residual**
Dado um bem cujo valor contábil se aproxima do valor residual
Quando a depreciação do período seria suficiente para reduzi-lo abaixo do residual
Então a depreciação é limitada de modo que o valor contábil iguale o residual.

**Baixa sem laudo**
Dado um pedido de baixa de bem sem laudo/parecer anexado
Quando o comando de baixa é submetido
Então a baixa é rejeitada e nenhum lançamento contábil é emitido.

**Reposição de almoxarifado**
Dado um item cujo saldo atinge o ponto de pedido após uma requisição
Quando a `RequisicaoAtendida` é processada
Então o evento `PontoPedidoAtingido` é publicado para a Administracao.

**Impairment**
Dado um bem cujo valor recuperável é inferior ao valor contábil
Quando a comissão registra o teste de recuperabilidade
Então uma perda por impairment é reconhecida e o evento `BemReavaliado`/impairment é emitido a Financas.

**Inventário com comissão inválida**
Dado um inventário cuja comissão tem menos de 3 membros ou membro vinculado à área
Quando a abertura do inventário é solicitada
Então a operação é bloqueada por violação de segregação.

## 9. Fontes
- MCASP / STN — https://www.gov.br/tesouronacional/
- NBC TSP 07 e NBC TSP 12 — https://cfc.org.br/
- Lei 14.133/2021 — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm
- Lei 9.503/1997 (CTB) — https://www.planalto.gov.br/ccivil_03/leis/l9503compilado.htm
- Lei 4.320/1964; Código Civil (comodato/cessão).
