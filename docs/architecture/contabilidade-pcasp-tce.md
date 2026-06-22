# Cadeia Contabilidade (PCASP) → MSC → Prestação de Contas (TCE-RS / SICONFI)

> **Status:** spec de arquitetura (dirige a geração Rules-as-Code das Fases 2.2 e 2.3).
> **Princípio:** a **contabilidade é a FONTE**. Sem lançamento contábil correto, MSC e remessa ao TCE
> nascem ocas. Por isso a ordem é **contabilidade primeiro, transmissão por último**.
> **Constituição §16:** layouts/códigos oficiais NÃO são inventados — pontos assim ficam marcados
> `// TODO(validar-leiaute-oficial)` e listados na seção *Pendências de fonte oficial*.

## 1. Fluxo ponta a ponta

```
[Tributos]  ReceitaArrecadada ─┐
[Financas]  Empenho → Liquidacao → Pagamento ─┐
                                               ▼
                         (Domain Events: EmpenhoEmitido, DespesaLiquidada,
                          PagamentoEfetuado, ReceitaArrecadada)
                                               ▼
                 ┌─────────────────────────────────────────────┐
                 │ CONTABILIDADE (PCASP) — módulo Financas      │
                 │  Evento contábil → LancamentoContabil        │
                 │  (partida dobrada: Σdébitos = Σcréditos)     │
                 │  → Balancete (saldos por conta/período)      │
                 └─────────────────────────────────────────────┘
                                               ▼
                 MatrizSaldosContabeis (MSC) — saldos + info complementares
                                               ▼ (Integration Event: MSCGeradaIntegrationEvent)
                 ┌─────────────────────────────────────────────┐
                 │ Transparencia (CONSUMIDOR)                   │
                 │  • DeclaracaoFiscal → SICONFI (MSC)          │
                 │  • RemessaTce (SIAPC/PAD) → e-Validador(RDI) │
                 │    → assinatura A1 → transmissão → SICOE     │
                 └─────────────────────────────────────────────┘
```

## 2. Modelo contábil (PCASP) — a construir em `Modules/Financas`

| Agregado/Entidade | Papel | Invariantes |
|---|---|---|
| **PlanoDeContas** / **ContaContabil** | Plano de Contas Aplicado ao Setor Público; conta com código, título, **natureza da informação** (Patrimonial, Orçamentária, Controle), natureza do saldo (Devedora/Credora), nível, conta-mãe. | código único por tenant; folha vs sintética; encerra por exercício. |
| **LancamentoContabil** | Registro de **partida dobrada** com ≥1 partida a débito e ≥1 a crédito. Originado de um **evento contábil** (não digitado à mão no fluxo automático). | **Σdébitos = Σcréditos** (invariante forte); data dentro do período aberto; contas folha; imutável após registro (estorno por novo lançamento). |
| **PartidaContabil** (filha) | Linha do lançamento: conta, débito|crédito, valor. | valor > 0; lado D ou C. |
| **EventoContabil** | Mapa **fato orçamentário/patrimonial → roteiro de partidas** (ex.: empenho ⇒ D 5.2.x / C 6.2.x). Parametrizável por tenant (não *hardcoded*). | roteiro com ΣD=ΣC; vigência por exercício. |
| **Balancete** (read model) | Saldos por conta e período (saldo anterior, débitos, créditos, saldo atual). | derivado dos lançamentos; conferência D=C global. |

**Lançamento automático (coração da integração):** cada Domain Event do ciclo
orçamentário dispara um handler que resolve o `EventoContabil` vigente e gera o
`LancamentoContabil` correspondente, na MESMA unidade de trabalho/Outbox:

| Fato | Evento contábil (roteiro — *natureza* das contas; **códigos exatos = TODO oficial**) |
|---|---|
| Dotação aprovada | D crédito disponível / C dotação inicial (contas de **controle orçamentário** 5.x/6.x) |
| Empenho emitido | D crédito disponível / C crédito empenhado a liquidar |
| Despesa liquidada | D crédito empenhado a liquidar / C crédito empenhado liquidado a pagar **+** registro **patrimonial** da variação (VPD) |
| Pagamento efetuado | D liquidado a pagar / C disponibilidade (banco) **+** baixa financeira |
| Receita arrecadada | D disponibilidade / C VPA (variação patrimonial aumentativa) **+** controle da receita |
| Restos a pagar (virada) | reclassificação empenhos não pagos → RP processado/não processado |

## 3. MatrizSaldosContabeis (MSC) — a ponte com SICONFI/TCE

- Gerada do **Balancete** por período: para cada conta, **saldo + atributos** (Tipo de Valor:
  saldo inicial/movimento devedor/credor/final; informações complementares conforme PCASP estendido).
- Publica `MSCGeradaIntegrationEvent` (Contracts) → **Transparencia** consome para SICONFI (declaração)
  e como insumo da remessa TCE-RS.
- A MSC **já existe como conceito** em Transparencia (`MatrizSaldos`, `LinhaContabil`,
  `MSCGeradaIntegrationEvent`) — falta a **origem** (Financas gerá-la de verdade a partir do PCASP).

## 4. Cadeia TCE-RS (Transparencia — já esqueletizada, falta o real)

1. **Geração** (`GerarRemessaTce`): hoje monta `RegistroLeiaute(tipo, conteudo)` genérico → trocar pelo
   **leiaute SIAPC/PAD campo-a-campo** (posição/tamanho/formato por tipo de registro). `// TODO(validar-leiaute-oficial)`.
2. **Validação** (`e-Validador` / RDI): hoje porta `IEValidadorTce` → implementar regras do RDI vigente.
3. **Assinatura A1**: certificado do tenant (cifrado no banco dedicado — ver `docs/architecture/certificado-a1.md`).
4. **Transmissão** (`EnviarRemessaTce`): hoje é comentário → implementar canal real (SOAP/REST do SICOE)
   com **Polly** (retry/circuit breaker) + ACL, idempotente por `RemessaTceId`, via **Outbox**.

## 5. Ordem de implementação

1. **Fase 2.1 (em curso):** ciclo da despesa em Financas (Dotação→Empenho→Liquidação→Pagamento→Restos).
2. **Fase 2.2 — Contabilidade PCASP:** PlanoDeContas, LancamentoContabil (partida dobrada),
   EventoContabil parametrizável, lançamento **automático** a partir dos eventos do ciclo, Balancete,
   **geração da MSC** + `MSCGeradaIntegrationEvent`. Inclui `*.rules.md` ricas (manifesto + BDD dos invariantes).
3. **Fase 2.3 — TCE real:** leiaute SIAPC/PAD, e-Validador, assinatura A1, transmissão SICOE.

## 6. Pendências de fonte oficial (NÃO inventar — §16)

- [ ] **Plano de Contas PCASP** vigente (códigos/contas exatos) — STN/MCASP.
- [ ] **Tabela de eventos contábeis** (roteiros de partidas oficiais) por fato.
- [ ] **Leiaute SIAPC/PAD do TCE-RS** vigente (registros, posições, tamanhos, versão) — Resoluções TCE-RS / e-Validador.
- [ ] **Layout MSC SICONFI** (matriz + tabelas de apoio) — STN/SICONFI.
- [ ] **Protocolo de transmissão** SICOE (endpoint, autenticação, envelope, recibo).

Enquanto não temos as fontes oficiais, modelamos **fiéis ao conceito** (PCASP/4.320/MCASP/SICONFI)
com os roteiros pela *natureza* das contas e marcamos cada layout/código exato com `// TODO(validar-leiaute-oficial)`,
para troca cirúrgica quando o material oficial chegar — sem retrabalho de arquitetura.
