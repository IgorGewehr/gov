---
modulo: Tributos
agregado: PlantaValores (PGV) + TabelaAliquotaIptu
contexto: Tributos (Parâmetros Fiscais do IPTU — lei municipal versionada)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "CTN art. 33 (base de cálculo do IPTU = valor venal)"
  - "Súmula 160/STJ (decreto só atualiza base por índice oficial; majoração real exige lei)"
  - "EC 29/2000; CF art. 156 §1 (progressividade por valor venal; diferenciação por uso/localização)"
  - "CF art. 182 §4 + Lei 10.257/2001 (progressividade no tempo — função social)"
---

# PlantaValores (PGV) e TabelaAliquotaIptu — Regras Normativas

> **Fonte da verdade.** Parâmetros fiscais do IPTU **versionados por exercício** (lei municipal).
> O motor de cálculo SEMPRE lê a versão vigente; **nenhum número fiscal vive no código** (CLAUDE.md §16).

## 1. Linguagem Ubíqua

| Termo | Definição |
|---|---|
| **PlantaValores** | Planta Genérica de Valores (PGV): VUT/VUC por zona fiscal + fatores, versionada por exercício. Raiz de agregado. |
| **ValorZona** | Entidade-filha: VUT (`ValorM2Terreno`) e VUC (`ValorM2Construcao`) de uma zona fiscal. |
| **FatorPgv** | Entidade-filha: multiplicador de correção (padrão construtivo, depreciação, uso). |
| **TipoFatorPgv** | Enum: PadraoConstrutivo, Depreciacao, Uso. |
| **TabelaAliquotaIptu** | Tabela de alíquotas do IPTU por exercício (predial × territorial), com faixas de progressividade. Raiz de agregado. |
| **FaixaAliquotaIptu** | Entidade-filha: intervalo [mínimo, máximo) de valor venal → alíquota (%). |
| **Vigente** | Indica versão publicada (imutável) apta ao motor. |

## 2. Modelo

- `PlantaValores : AggregateRoot<PlantaValoresId>, IMustHaveTenant`; filhas `ValorZona`, `FatorPgv`.
- `TabelaAliquotaIptu : AggregateRoot<TabelaAliquotaIptuId>, IMustHaveTenant`; filha `FaixaAliquotaIptu`.
- Constante `TabelaAliquotaIptu.SemTeto` = sentinela de faixa sem teto (armazenável em decimal(18,2)).

## 3. Invariantes

- **I-1.** Pertencem a exatamente um tenant; `TenantId` imutável.
- **I-2.** PGV/Tabela só são editáveis enquanto **não** `Vigente` (Súmula 160/STJ — nova versão por exercício).
- **I-3.** Publicar PGV exige ≥ 1 zona definida.
- **I-4.** VUT/VUC e multiplicadores são não-negativos (multiplicador > 0).
- **I-5.** `ObterFator` retorna 1 (neutro) quando a chave não está parametrizada (motor determinístico).
- **I-6.** Faixas de alíquota não podem se sobrepor; publicar exige cobertura **contínua a partir de zero**.
- **I-7.** `AliquotaPara(valorVenal)` seleciona a faixa em que o valor se enquadra (marginal-simples).

## 4. Comandos (escrita)

### 4.1 `ConfigurarPlantaValores` — `ConfigurarPlantaValoresCommand : ICommand<Guid>`
- Cria a PGV do exercício, define zonas/fatores (todos vindos do comando — lei municipal) e opcionalmente publica.

### 4.2 `ConfigurarTabelaAliquotaIptu` — `ConfigurarTabelaAliquotaIptuCommand : ICommand<Guid>`
- Cria a tabela de alíquotas (predial/territorial), adiciona faixas e opcionalmente publica.

## 5. Consultas (leitura)

Nenhuma consulta dedicada nesta versão (a apuração é exposta pela query `CalcularIptu` — ver Imovel/Iptu).

## 6. Eventos

| Evento | Emitido em |
|---|---|
| `PlantaValoresCriada` | `PlantaValores.Criar`. |
| `PlantaValoresPublicada` | `PlantaValores.Publicar`. |
| `TabelaAliquotaIptuCriada` | `TabelaAliquotaIptu.Criar`. |
| `TabelaAliquotaIptuPublicada` | `TabelaAliquotaIptu.Publicar`. |

Nenhum Integration Event publicado/consumido nesta versão.

## 7. Persistência

- Tabelas: `tributos.PlantasValores`, `tributos.PgvValoresZona`, `tributos.PgvFatores`, `tributos.TabelasAliquotaIptu`, `tributos.FaixasAliquotaIptu`.
- Índices únicos: `(PlantaValoresId, ZonaFiscal)`, `(PlantaValoresId, Tipo, Chave)`.

## 8. Segurança, Tenant e Auditoria

- `IMustHaveTenant` + Global Query Filter; RBAC `tributos.gerenciar`; auditoria imutável de toda alteração de parâmetro (crítico para TCE-RS).

## 9. Cenários BDD

- **C-1.** Configurar PGV com zonas/fatores e publicar → `Vigente`; emite `PlantaValoresPublicada`.
- **C-2.** Editar PGV já vigente → `InvalidOperationException` (Súmula 160/STJ).
- **C-3.** Publicar tabela com lacuna entre faixas → `InvalidOperationException`.
- **C-4.** Isolamento: tenant B não enxerga PGV/tabelas do tenant A.

## 10. Pendências `// TODO(validar-oficial)`

- VUT/VUC, fatores, faixas e alíquotas = lei da PGV / CTM / decreto anual de Maximiliano de Almeida/RS.

## 11. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-22 | PGV e tabela de alíquotas parametrizáveis e versionadas por exercício (M6 parte 1). |

<!-- manifest
commands: ConfigurarPlantaValores, ConfigurarTabelaAliquotaIptu
queries: 
domainEvents: PlantaValoresCriada, PlantaValoresPublicada, TabelaAliquotaIptuCriada, TabelaAliquotaIptuPublicada
integrationEventsPublished: 
integrationEventsConsumed: 
-->
