---
modulo: Patrimonio
agregado: Pneu, Apolice, Condutor
contexto: Patrimonio (Frota — gestão de pneus, apólices de seguro e condutores/CNH)
poder: Ambos
schema: patrimonio
ativavel_por_tenant: true
versao_regras: 1.0.0
---

# Frota — Pneus, Apólices e Condutores (Rules-as-Code)

Bounded Context: **Patrimonio** (Frota). Três agregados próprios que completam a gestão de frota:

- **Pneu:** item controlado individualmente (número de fogo). Ciclo: estoque → instalação numa posição
  (eixo/lado) de um veículo → rodízio/remoção → recapagem → descarte. Controla sulco (mm), km acumulado,
  recapagens e custo por km. Sulco mínimo legal de circulação **parametrizável por tenant** (CONTRAN/CTB,
  default 1,6 mm — CLAUDE.md §7/§16; nenhum número mágico no código).
- **Apolice:** apólice de seguro de um veículo (obrigatório DPVAT/SPVAT ou facultativo casco/RCF-V/frota),
  com vigência, prêmio e importância segurada. Ciclo: vigente → vencida (por decurso) ou cancelada;
  habilita o alerta de renovação (janela parametrizável por tenant).
- **Condutor:** motorista habilitado (CNH, categorias, validade do exame de aptidão — CTB Lei 9.503/1997).
  Habilita o alerta/bloqueio de viagem com CNH vencida. Dado pessoal sob **LGPD**.

## Invariantes

- **PN-1:** número de fogo do pneu obrigatório e **único por tenant**.
- **PN-2:** instalação exige pneu em estoque; uma posição (eixo/lado) de um veículo comporta **um** pneu por vez.
- **PN-3:** sulco é não-negativo e não cresce em rodagem; só a **recapagem** o restaura.
- **PN-4:** remoção exige pneu instalado e odômetro ≥ odômetro de instalação (km do ciclo não negativo).
- **PN-5:** descarte exige pneu fora do veículo (estoque/removido); é terminal.
- **AP-1:** apólice exige veículo existente no tenant; fim de vigência **posterior** ao início.
- **AP-2:** renovação não retroage antes do fim da vigência atual; apólice cancelada não renova.
- **AP-3:** cancelamento (motivo obrigatório) é terminal.
- **CD-1:** CNH obrigatória e **única por tenant**; não se cadastra condutor já com CNH vencida.
- **CD-2:** condutor só é apto a conduzir se ativo e com CNH válida (base do bloqueio de viagem).
- **CD-3:** renovação de CNH não retroage (nova validade posterior à atual).

## Parâmetros por tenant (sem número mágico — CLAUDE.md §7/§16)

- Sulco mínimo legal (mm), janela de alerta de CNH a vencer (dias) e janela de alerta de apólice a vencer
  (dias) vêm de `FrotaOptions` (seção `Patrimonio:Frota`), com defaults; nunca literais nos agregados.

<!-- manifest
commands: CadastrarPneu, InstalarPneu, RemoverPneu, EnviarPneuParaRecapagem, ConcluirRecapagem, DescartarPneu, ContratarApolice, RenovarApolice, CancelarApolice
queries: BuscarPneus, ObterPneu, ObterLayoutPneusDoVeiculo, ListarPneusNoLimiteDeSulco
domainEvents: PneuInstalado, PneuRemovido, PneuDescartado, ApoliceContratada, CnhRenovada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
