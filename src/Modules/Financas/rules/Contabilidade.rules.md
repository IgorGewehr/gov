# Contabilidade (PCASP/MCASP) — Rules-as-Code

Fundação contábil do módulo Finanças: Plano de Contas PCASP, lançamentos por partidas dobradas,
roteiros parametrizáveis (eventos contábeis) e projeção do Balancete. Spec oficial em
`docs/architecture/specs-oficiais/pcasp-plano-de-contas.md` e `mcasp-eventos-contabeis.md`.

## Linguagem ubíqua

- **ContaContabil** — conta do PCASP (código segmentado `C.G.SG.T.ST.IT.SI`, natureza, tipo, F/P).
- **LancamentoContabil / PartidaContabil** — partida dobrada homogênea e balanceada (ΣD=ΣC).
- **EventoContabil / LinhaRoteiro** — roteiro fato→partidas, parametrizável por tenant.
- **Balancete** — read model de saldos por conta/exercício/mês (base da MSC).

## Invariantes (domínio)

1. Partida dobrada: ΣDébitos = ΣCréditos por lançamento.
2. Homogeneidade: todas as partidas de um lançamento têm a mesma natureza de informação (MCASP §3).
3. Lançamento só em conta analítica (folha) e ativa.
4. Período contábil aberto.
5. Estorno gera lançamento inverso e preserva o original (auditoria imutável).

## RBAC

- Consultas (plano, balancete, razão) → `financas.ver`.
- Comandos (semear, lançamento manual) → `financas.gerenciar`.

## Mapa evento do ciclo → partidas (roteiros)

| Fato | Lançamentos | Naturezas |
|---|---|---|
| EmpenhoEmitido | D 6.2.2.1.1 / C 6.2.2.1.3.01 | Orçamentária |
| DespesaLiquidada | D 6.2.2.1.3.01 / C 6.2.2.1.3.03 ; D VPD / C Fornecedores | Orçamentária + Patrimonial |
| PagamentoEfetuado | D 6.2.2.1.3.03 / C 6.2.2.1.3.04 ; D Fornecedores / C Caixa | Orçamentária + Patrimonial |
| ReceitaArrecadada | D 6.2.1.1 / C 6.2.1.2 ; D Caixa / C VPA | Orçamentária + Patrimonial |

<!-- manifest
commands: SemearPlanoDeContas, RegistrarLancamentoManual, GerarMsc
queries: ConsultarBalancete, ConsultarRazao, ListarPlanoDeContas, GerarBalancoOrcamentario, GerarBalancoFinanceiro, GerarBalancoPatrimonial, GerarDvp
domainEvents: ContaContabilCriada, LancamentoContabilRegistrado, LancamentoContabilEstornado, ReceitaArrecadadaRegistrada
integrationEventsPublished: MSCGeradaIntegrationEvent, ReceitaCorrenteLiquidaApuradaIntegrationEvent
integrationEventsConsumed: 
-->
