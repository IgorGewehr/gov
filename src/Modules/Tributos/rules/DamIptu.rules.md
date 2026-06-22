---
modulo: Tributos
agregado: Dam (Arrecadação) + Motor IPTU
contexto: Tributos (Lançamento anual do IPTU + guia/carnê de arrecadação)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "CTN art. 142/149 (IPTU — lançamento de ofício, uma vez por exercício)"
  - "CTN art. 33 (base de cálculo = valor venal)"
  - "EC 29/2000; CF art. 156 §1 (alíquotas progressivas/diferenciadas)"
---

# Dam (Arrecadação) e Motor de Cálculo do IPTU — Regras Normativas

> **Fonte da verdade.** Motor de cálculo determinístico e auditável + geração da guia (DAM) com
> cota única ou N parcelas. Reúne cadastro imobiliário (`Imovel`) + parâmetros (`PGV`/alíquotas).

## 1. Linguagem Ubíqua

| Termo | Definição |
|---|---|
| **Dam** | Documento de Arrecadação Municipal (guia/carnê) de um lançamento. Raiz de agregado. |
| **Parcela** | Entidade-filha: número, valor, vencimento, controle de pagamento (cota única = 1 parcela). |
| **CalculadoraValorVenal** | Serviço de domínio: `ValorVenal = (AreaTerreno×VUT) + (AreaConstruida×VUC×fatores)` × fração ideal. |
| **CalculadoraIptu** | Serviço de domínio: `IPTU = ValorVenal × alíquota − isenção − desconto`. |
| **MemoriaValorVenal / MemoriaIptu** | Memórias de cálculo (auditáveis) com cada parcela e fator. |
| **ParametrosIsencaoIptu** | Percentuais de isenção/desconto (lei municipal) — entrada do motor. |

## 2. Modelo

- `Dam : AggregateRoot<DamId>, IMustHaveTenant`; filha `Parcela`.
- Motores são `static` (domínio puro, determinístico, sem estado/IO).
- `Lancamento.LancarIptu(...)` constitui o IPTU anual vinculado ao `ImovelId`.

## 3. Invariantes

- **I-1.** `Dam` e `Lancamento` pertencem a exatamente um tenant; `TenantId` imutável.
- **I-2.** `Gerar` exige `numeroParcelas ≥ 1`; a soma das parcelas fecha **exatamente** o total (resíduo na última).
- **I-3.** O valor venal só é calculado com PGV **vigente** que **cobre a zona fiscal** do imóvel.
- **I-4.** O IPTU só é apurado com tabela de alíquotas **vigente** (predial × territorial conforme `Edificado`).
- **I-5.** Isenção/desconto ∈ [0, 100]%; aplicados na ordem isenção → desconto.
- **I-6.** Registrar pagamento de parcela inexistente ou já paga → `InvalidOperationException`.
- **I-7.** Cálculo é **determinístico**: mesmas entradas (imóvel + PGV + tabela) ⇒ mesmo resultado.

## 4. Comandos (escrita)

### 4.1 `LancarIptuAnual` — `LancarIptuAnualCommand : ICommand<ResultadoLancamentoIptu>`
- **Efeito:** apura o IPTU (PGV/alíquota vigentes), constitui `Lancamento` de IPTU vinculado ao imóvel e gera o `Dam` com cota única ou N parcelas.
- **Handler:** `LancarIptuAnualHandler(IImovelRepository, IPlantaValoresRepository, ITabelaAliquotaIptuRepository, ILancamentoRepository, IDamRepository, IUnitOfWork, ITenantContext)`.

## 5. Consultas (leitura)

### 5.1 `CalcularIptu` — `CalcularIptuQuery : IQuery<ResultadoIptu>`
- Apura (sem lançar) o IPTU de um imóvel num exercício — preview/simulação com memória de cálculo achatada.

## 6. Eventos

| Evento | Emitido em |
|---|---|
| `DamGerado` | `Dam.Gerar`. |
| `DamQuitado` | última parcela paga (`RegistrarPagamentoParcela`). |

`Lancamento.LancarIptu` emite `CreditoTributarioLancado` (ver regras de `Lancamento`).
Nenhum Integration Event publicado/consumido nesta versão.

## 7. Persistência

- Tabelas: `tributos.Dams`, `tributos.Parcelas`; coluna nova `Lancamentos.ImovelId` (nullable) + índice.

## 8. Segurança, Tenant e Auditoria

- `IMustHaveTenant` + Global Query Filter; RBAC `tributos.gerenciar` (lançar) / `tributos.ver` (apurar); auditoria imutável.

## 9. Cenários BDD

- **C-1.** Valor venal bate em caso conhecido (terreno + construção).
- **C-2.** IPTU = valor venal × alíquota; isenção/desconto reduzem o devido.
- **C-3.** Lançar IPTU anual gera `Lancamento` (Iptu) vinculado ao imóvel + `Dam` com parcelas que somam o total.
- **C-4.** Alíquota progressiva seleciona a faixa do valor venal.
- **C-5.** Isolamento: tenant B não enxerga DAMs/lançamentos do tenant A.

## 10. Pendências `// TODO(validar-oficial)`

- Calendário fiscal (nº máx. de parcelas, valor mínimo, vencimentos), isenções/descontos = decreto anual de Maximiliano de Almeida/RS.
- Código de barras FEBRABAN / linha digitável / PIX cobv / CNAB (fases posteriores — M6-DESIGN §5).

## 11. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-22 | Motor de cálculo IPTU + lançamento anual + DAM/parcelas (M6 parte 1). |

<!-- manifest
commands: LancarIptuAnual
queries: CalcularIptu
domainEvents: DamGerado, DamQuitado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
