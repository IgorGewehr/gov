---
modulo: PainelGestor
agregado: IndicadorMunicipio
contexto: PainelGestor (BI/read-model — KPIs do gestor a partir de Integration Events de outros modulos)
poder: Ambos
schema: painelgestor
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["LC 101/2000 (LRF) arts. 18-20 (Despesa com Pessoal)", "CF/88 art. 169 (alerta de pessoal)", "CF/88 art. 212 (minimo Educacao)", "EC 29 / LC 141/2012 (minimo Saude)"]
---

# Painel do Gestor — Regras-as-Code (Rules-as-Code)

> **M8 — Painel do Gestor (BI).** Modulo de **leitura/consolidacao**: NAO tem comandos de escrita de
> negocio nem agregados mutaveis por API. Ele **consome Integration Events** publicados por Financas,
> Tributos, RH e Transparencia (via Outbox), materializa um read-model por exercicio
> (`IndicadorMunicipio`) e expoe os **KPIs do gestor** (LRF/pessoal, minimos constitucionais, execucao
> orcamentaria, divida ativa, remessas TCE). A apuracao dos percentuais e semaforos e feita por funcoes
> de dominio puras; o numerador/denominador vem dos eventos. Este arquivo e **normativo e versionado**.

---

## 1. Linguagem Ubiqua

| Termo (identificador-no-codigo) | Definicao |
|---|---|
| Indicador do Municipio (`IndicadorMunicipio`) | Read-model consolidado por exercicio (snapshot dos KPIs). Alimentado por eventos. |
| Painel do Gestor (`PainelGestorDto`) | Projecao dos 5 KPIs do gestor para um exercicio. |
| Materializador (`MaterializadorIndicadores`) | Servico que aplica os eventos recebidos sobre o read-model. |
| Limites de Pessoal (`ILimitesPessoalProvider`) | Provedor dos limites da LRF (legal/prudencial/alerta) vigentes. |
| Exercicio (`Exercicio`) | Ano de referencia — ancora da consulta (sem relogio). |
| Tenant (`TenantId`) | Ente publico dono do read-model. |

---

## 2. Modelo

- **Read-model:** `IndicadorMunicipio` (snapshot por exercicio) — `IMustHaveTenant`. Persistido no schema `painelgestor`, sob Global Query Filter.
- **Caracteristica:** modulo de **leitura**. Toda mutacao do read-model decorre do consumo idempotente de Integration Events (nunca de comandos de API de negocio).

---

## 3. Invariantes

- **I-1.** A consulta e **reproduzivel**: dados os mesmos valores materializados e os mesmos limites vigentes, o painel e identico (o exercicio e a ancora; sem relogio).
- **I-2.** **Degradacao graciosa**: sem snapshot do exercicio, devolve painel zerado/`Indeterminado` — nunca lanca nem inventa numero.
- **I-3.** O consumo de cada Integration Event e **idempotente** (por chave do evento/competencia) — reentrega via Outbox nao duplica.
- **I-4.** Toda a regra (LRF, minimos) e do **dominio** (funcoes puras); o handler so orquestra portas.
- **I-5.** Tenant-scoped pelo Global Query Filter; nunca consolida dados de outro tenant.

---

## 4. Comandos (escrita)

> **Nenhum.** O Painel do Gestor nao expoe comandos de negocio; o estado deriva exclusivamente do consumo de Integration Events (secao 6).

---

## 5. Consultas (leitura)

### 5.1 ObterPainelGestor
- **Query:** `ObterPainelGestorQuery(int Exercicio) : IQuery<PainelGestorDto>`.
- **Handler:** `ObterPainelGestorHandler(IIndicadorMunicipioRepository, ILimitesPessoalProvider)`.
- **Efeito:** le o snapshot do exercicio e os limites de pessoal vigentes; deriva percentuais/semaforos via dominio puro; projeta o `PainelGestorDto`. Tenant-scoped.
- **Pre-condicoes:** `request` nao nulo.
- **Degradacao:** sem snapshot ⇒ painel vazio/`Indeterminado`.

---

## 6. Eventos

### Dominio (in-process)
> **Nenhum** evento de dominio proprio publicado por este modulo.

### Integracao (publica)
> **Nenhum.** O Painel do Gestor nao publica Integration Events (e consumidor terminal/BI).

### Integracao (CONSOME — via Outbox dos modulos de origem)

| Evento (assembly de origem) | Origem | Efeito no read-model |
|---|---|---|
| `DespesaEmpenhadaIntegrationEvent` | Financas | Atualiza execucao orcamentaria (empenhado). |
| `DespesaLiquidadaIntegrationEvent` | Financas | Atualiza execucao orcamentaria (liquidado). |
| `PagamentoEfetuadoIntegrationEvent` | Financas | Atualiza execucao orcamentaria (pago). |
| `DotacaoOrcamentariaPublicadaIntegrationEvent` | Financas | Atualiza a dotacao/orcamento de referencia. |
| `ReceitaCorrenteLiquidaApuradaIntegrationEvent` | Financas | Atualiza a RCL (denominador dos limites LRF). |
| `DespesaPessoalApuradaIntegrationEvent` | RH | Atualiza a Despesa com Pessoal (numerador LRF — arts. 18-20). |
| `ReceitaArrecadadaIntegrationEvent` | Tributos | Atualiza a receita arrecadada. |
| `PosicaoDividaAtivaIntegrationEvent` | Tributos | Atualiza a posicao da Divida Ativa. |
| `MinimoConstitucionalApuradoIntegrationEvent` | Transparencia | Atualiza os minimos constitucionais (Saude/Educacao). |
| `RemessaEnviadaTceIntegrationEvent` | Transparencia | Atualiza o status de remessas ao TCE. |
| `PrazoRemessaVencidoIntegrationEvent` | Transparencia | Sinaliza prazo de remessa vencido (alerta). |

> Cada consumidor (`Receber...Handler`) e idempotente por chave de competencia/evento e tenant-scoped.

---

## 7. Seguranca, Tenant e Auditoria

- **Tenant:** `IndicadorMunicipio` implementa `IMustHaveTenant`; Global Query Filter por `TenantId`; consolidacao cross-tenant **proibida**.
- **AuthZ (negar por padrao):** consulta ao painel exige permissao de leitura gerencial; modulo **ativavel por tenant** (requisicao sem licenca → 404/403 auditado).
- **Resiliencia:** consumo de eventos idempotente; reentrega via Outbox nao duplica indicadores.
- **Anti-SQLi:** acesso via EF parametrizado.

---

## 8. Changelog

| versao | data | mudanca |
|---|---|---|
| 1.0.0 | 2026-06-23 | Versao inicial — derivada do modulo PainelGestor (M8 — BI): 1 consulta + 11 Integration Events consumidos (P0 governanca: SpecCodeConsistency). |

<!-- manifest
commands: 
queries: ObterPainelGestor
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: DespesaPessoalApuradaIntegrationEvent, MinimoConstitucionalApuradoIntegrationEvent, RemessaEnviadaTceIntegrationEvent, PrazoRemessaVencidoIntegrationEvent, DespesaEmpenhadaIntegrationEvent, DespesaLiquidadaIntegrationEvent, PagamentoEfetuadoIntegrationEvent, DotacaoOrcamentariaPublicadaIntegrationEvent, ReceitaCorrenteLiquidaApuradaIntegrationEvent, ReceitaArrecadadaIntegrationEvent, PosicaoDividaAtivaIntegrationEvent
-->
