# M5 — DESIGN: Folha do CICLO ANUAL (13º, Férias, Rescisão) — pronto-para-implementar

> **Status:** design de implementação. Fundamentação legal em `pesquisa-folha-ciclo-anual.md` (fontes
> oficiais: CF art. 7º VIII/XVII; Lei 4.090/62; Lei 4.749/65; Dec. 57.155/65; Lei 7.713/88 art. 12-A; CLT
> arts. 129–145, 477, 484-A; Lei 12.506/2011; Súmula 386 STJ; STF Tema 985).
> **Pré-requisito PROVADO (reusar, não reescrever):** motor MENSAL em
> `RecursosHumanos.Domain/Calculo/MotorDeCalculoFolha.cs` (`Calcular(insumos, tabelaInss, tabelaIrrf,
> tabelaRpps)`), agregados `FolhaDePagamento`/`EventoFolha`, `RubricaFolha` (flags `IncideInss/Rpps/Irrf/Fgts`),
> tabelas versionadas `TabelaInss`/`TabelaIrrf`/`TabelaRpps`, e o pipeline
> `ApurarDescontosLegais → FolhaDePagamento.Calcular(abate-teto) → Fechar → eSocial (S-1200/1202/1210/1299)`.
> **Constituição §16 (REGRA DE OURO):** nada hardcoded — alíquotas/faixas/datas/percentuais/códigos de rubrica
> são DADO parametrizado por tenant/competência. **Reprodutibilidade:** todo cálculo é função pura de
> `(vínculo, eventos, datas-de-entrada, tabela-da-competência)` — **sem relógio**; datas (admissão, afastamentos,
> desligamento, competência, data-de-pagamento) são **entrada**, nunca `DateTime.Now`/`TimeProvider` dentro do
> cálculo. `[a confirmar]` = depende de lei municipal (Estatuto 327/2008 de Maximiliano de Almeida/RS).

---

## 0. Princípio arquitetural central (o que NÃO muda)

O motor mensal já faz **3 coisas separáveis**, e o ciclo anual reusa exatamente estas:

1. **`RubricaFolha`** carrega as INCIDÊNCIAS (`IncideInss/IncideRpps/IncideIrrf/IncideFgts`). A base é
   somada **por incidência da rubrica, nunca por nome** (`ApurarDescontosLegais` linhas 55–94).
2. **`MotorDeCalculoFolha.Calcular`** é função pura: recebe `InsumosCalculoServidor` (verbas com flags) +
   tabelas da competência, devolve `ResultadoCalculoServidor` (bases + descontos legais). **Determinístico.**
3. **`FolhaDePagamento`** é o agregado de transporte/estado (eventos, totais, abate-teto, situação,
   eSocial). O 13º/férias/rescisão **geram VERBAS** que entram nesse mesmo agregado.

> **Decisão-mãe:** o ciclo anual **NÃO** cria um segundo motor. Ele acrescenta (a) **calculadoras de avos
> e de verbas** (puras, no Domain) que produzem `VerbaCalculo`/eventos; (b) uma variante do motor com
> **base separada** (13º); (c) um **tipo de evento de folha** (`TipoFolha`) para que a mesma
> `FolhaDePagamento` saiba se é MENSAL/DÉCIMO_TERCEIRO/FÉRIAS/RESCISÃO e roteie incidências e eSocial.
> As tabelas INSS/IRRF/RPPS reusadas são **as mesmas** — só muda a BASE sobre a qual incidem.

---

## 1. Acréscimos ao modelo de domínio (mínimos, parametrizáveis)

### 1.1 Discriminador de tipo de folha (novo enum + atributo no agregado)
`Domain/Folha/Enums.cs`:
```
public enum TipoFolha { Mensal = 1, DecimoTerceiro = 2, Ferias = 3, Rescisao = 4 }
```
`FolhaDePagamento`: novo campo `TipoFolha Tipo { get; private set; }` + `bool BaseSeparada` (true p/ 13º).
`Abrir(tenantId, competencia, tipo = TipoFolha.Mensal)` — overload retrocompatível (default Mensal).
O motor de **abate-teto** (`Calcular`) **só** roda quando `Tipo == Mensal` (o teto do art. 37 XI é mensal;
13º/férias/rescisão não disparam abate-teto — apuram-se em folha própria). Migration aditiva (coluna nullable
→ default `Mensal` no backfill).

### 1.2 Avos — Value Object puro (sem relógio), reusado por 13º e férias
`Domain/CicloAnual/Avos.cs` — função pura de datas de **entrada**:
```
public sealed record Avos(int Quantidade)        // 0..12
{
  // ≥15 dias no mês conta como mês integral (Lei 4.090/62). Admissão/afastamentos/desligamento são ENTRADA.
  public static Avos Apurar(DateOnly inicioPeriodo, DateOnly fimPeriodo,
                            IReadOnlyList<IntervaloAfastamento> afastamentosQueSuspendem);
}
public readonly record struct IntervaloAfastamento(DateOnly Inicio, DateOnly? Fim, bool SuspendeContagem);
```
- **Determinismo:** nenhuma chamada a `DateTime.Now`. `fimPeriodo` = 31/12 (ciclo) ou `dataDesligamento`
  (rescisão), passado pelo handler. Regra dos 15 dias e quais afastamentos suspendem contagem são
  parâmetros (`SuspendeContagem`) — `[a confirmar — Estatuto 327/2008]` para estatutário.

### 1.3 Períodos aquisitivos de férias (novo small-aggregate ou VO de consulta)
`Domain/CicloAnual/PeriodoAquisitivoFerias.cs`:
```
PeriodoAquisitivoFerias { Inicio, Fim, DiasDireito (param=30), DiasGozados, DiasVendidosAbono, FaltasInjustif }
  → Avos VencidoOuProporcional(DateOnly dataReferencia)   // puro
  → bool EstaVencido / EmDobra(DateOnly dataReferencia)    // [a confirmar] dobra p/ estatutário
```
Origem dos períodos: derivada de `Servidor.DataExercicio` + histórico de gozo (entrada do handler). A escala
de redução por faltas (CLT art. 130) é **tabela parametrizável** `[a confirmar]` p/ estatutário.

### 1.4 Catálogo de rubricas do ciclo anual (DADO, não código)
Nenhum código novo de rubrica é hardcoded. Acrescenta-se a `ParametrosFolha` (`Application/Configuracao`)
os **códigos parametrizáveis** (defaults documentais, sobrescrevíveis por tenant), espelhando o padrão
já existente (`CodigoRubricaInss/Rpps/Irrf`):
```
CodigoRubrica13Salario   (default "13-SAL")     CodigoRubricaFerias        (default "FERIAS")
CodigoRubricaInss13      (default "INSS-13")     CodigoRubricaTercoFerias   (default "1/3-FERIAS")
CodigoRubricaIrrf13      (default "IRRF-13")     CodigoRubricaAbonoPecun    (default "ABONO-PEC")
CodigoRubricaSaldoSalario, CodigoRubrica13Prop, CodigoRubricaFeriasVencidas,
CodigoRubricaFeriasProp, CodigoRubricaAvisoPrevio, CodigoRubricaMultaFgts
```
As **incidências** de cada uma vivem na `RubricaFolha` (S-1010) — ex.: `13-SAL` incide
INSS/RPPS/IRRF; `1/3-FERIAS` sobre **férias gozadas** incide; férias/terço **indenizadas** (rescisão) e
`ABONO-PEC` têm incidências **desligadas** (Súmula 386 STJ). O motor lê a flag, **não** o nome.

---

## 2. Folha de 13º (gratificação natalina) — base SEPARADA

### 2.1 Cálculo do valor (puro)
`Domain/CicloAnual/CalculadoraDecimoTerceiro.cs`:
```
valor13 = round( (remuneracaoBaseDezembro / 12) * avos.Quantidade , 2 )
```
- `remuneracaoBaseDezembro` = soma das rubricas que **integram o 13º** (salário + médias de variáveis
  habituais) — montada pelo handler a partir do catálogo de `RubricaFolha` (mesma mecânica de
  incidência/natureza). `avos` via `Avos.Apurar(01/01..31/12, afastamentos)`.

### 2.2 Tributação própria — **a extensão central do motor**
O IRRF/INSS do 13º são **exclusivos na fonte** (Lei 7.713/88 art. 12-A) e calculados sobre **base própria,
separada do mês**. Reusa as **mesmas tabelas** (`TabelaInss/Rpps/Irrf`), mas isoladamente.
**Extensão mínima e segura** ao motor (preserva a assinatura atual; adiciona overload):
```
// MotorDeCalculoFolha (novo overload, mesma lógica de bases/incidência):
ResultadoCalculoServidor CalcularBaseSeparada(
    InsumosCalculoServidor insumos, TabelaInss?, TabelaIrrf, TabelaRpps?,
    OpcoesIrrf opcoes /* { SemDescontoSimplificado = true } */ )
```
- Internamente reusa o **mesmo laço de bases por incidência** já existente (linhas 40–66) e a **mesma**
  previdência fail-closed (linhas 68–92). A diferença é **só no IRRF do 13º**: `art. 12-A` veda o desconto
  simplificado e a dispensa do art. 67 (IN RFB 1.500/2014). Isso entra como `OpcoesIrrf` passada a
  `TabelaIrrf.CalcularImposto(..., aplicarSimplificado: false)` — **acréscimo de parâmetro opcional** em
  `TabelaIrrf` (default mantém comportamento mensal). **Sem hardcode**: a tabela continua sendo o dado.
- **Determinismo preservado:** dadas as mesmas entradas e tabelas, mesmo resultado. O 13º **não** soma à
  base do mês (`Tipo == DecimoTerceiro`, `BaseSeparada == true`): é folha própria.

### 2.3 Duas parcelas
`Domain/CicloAnual/ParcelaDecimoTerceiro.cs` (VO): `{ Ordem ∈ {1,2}, Percentual(param, default 0.5),
DataLimite(param, ENTRADA), AplicaDescontos }`.
- **1ª parcela:** `Tipo=DecimoTerceiro`, gera só a verba de provento `13-SAL × percentual`, `AplicaDescontos=false`
  → **sem** `ApurarDescontosLegais`. Fecha como adiantamento.
- **2ª parcela:** gera o `13-SAL` **integral**, roda `ApurarDescontosLegaisBaseSeparada` (INSS-13/IRRF-13) e
  **lança como desconto a 1ª parcela já paga** (rubrica informativa-dedutora parametrizável). Datas-limite
  (30/nov, 20/dez) são **parâmetros de entrada**, nunca relógio.

### 2.4 Gancho contábil/eSocial
- **eSocial:** 13º entra no **S-1200/S-1202** com `perApur` indicando a competência do 13º (grupo
  `infoPerApur` / `13` — `[a confirmar — leiaute MOS, ver pesquisa-esocial-eventos.md]`). O gerador atual
  (`GerarRemuneracaoFolha`) já soma `detVerbas` por rubrica da folha — reusa-se, roteando por `Tipo`.
- **Contábil:** `FolhaFechadaIntegrationEvent` já existe; acrescentar `TipoFolha` ao contrato para a
  Contabilidade empenhar na **dotação/rubrica de despesa de pessoal** correta (13º tem
  elemento de despesa próprio) — `[a confirmar — plano de despesa TCE-RS]`.

---

## 3. Folha de Férias — período + 1/3 + abono

### 3.1 Cálculo (puro)
`Domain/CicloAnual/CalculadoraFerias.cs`:
```
remuneracaoFerias = baseDias(diasGozados)                 // proporcional aos dias gozados
terco             = round( baseTerco * (1/3 param) , 2 )   // CF art. 7º XVII (param = fração, default 1/3)
abonoPecuniario   = baseDias(diasVendidos)  [+ tercoAbono] // CLT art. 143; venda de até 1/3
```
- `1/3` é **parâmetro** (`FracaoTercoConstitucional`, default `0,3333…`) — mínimo constitucional, mas o ente
  pode pagar mais; **nunca hardcoded**. Médias habituais que integram a base = catálogo de `RubricaFolha`.

### 3.2 Incidências (o que liga/desliga)
Cada verba vira `VerbaCalculo` com flags **vindas da `RubricaFolha`**, e o motor mensal padrão
(`MotorDeCalculoFolha.Calcular`, **não** base-separada) apura:
- **Férias GOZADAS + terço:** `IncideIrrf=true` (tabela progressiva mensal, tributação **normal**, soma no
  ajuste anual — **difere do 13º**). Previdência sobre o **terço**: incidência **patronal** (STF Tema 985);
  a cota do **segurado/RPPS** sobre o 1/3 fica como **flag de incidência** `[a confirmar — pós-Tema 985]`.
- **Abono pecuniário + terço-abono:** rubricas com incidências **desligadas** (isento IR + fora da base
  previdenciária — Súmula 386 STJ / natureza indenizatória).
- **Férias INDENIZADAS** (na rescisão, §4): incidências desligadas.

> **Determinismo:** a folha de férias é uma `FolhaDePagamento` com `Tipo=Ferias`. Não roda abate-teto.
> `diasGozados/diasVendidos`, datas do período e médias são **entrada** do handler.

### 3.3 Períodos aquisitivo/concessivo
`PeriodoAquisitivoFerias` (§1.3) controla vencido/proporcional/dobra. A **dobra** (CLT art. 137) é
parâmetro ligável `[a confirmar]` p/ estatutário. Avos proporcionais via `Avos.Apurar`.

### 3.4 Gancho contábil/eSocial
Férias gozadas entram no **S-1200/S-1202** da competência de pagamento (verbas próprias). Contábil: mesmo
`FolhaFechadaIntegrationEvent` com `Tipo=Ferias`.

---

## 4. Folha de Rescisão / Desligamento — composição por tipo × regime

### 4.1 Tipo de desligamento (novo enum, parametrizável a matriz)
`Domain/CicloAnual/Enums.cs`:
```
public enum TipoDesligamento {
  DispensaSemJustaCausa=1, PedidoDemissaoExoneracao=2, JustaCausa=3, Distrato=4,
  Aposentadoria=5, Falecimento=6, ExoneracaoVacancia=7 /* estatutário */ }
```
`RegimeVinculo { Estatutario=1, Celetista=2 }` — atributo **do vínculo** (deriva de `TipoCargo`/`Regime`;
estatutário↔RPPS, celetista/comissionado/temporário↔RGPS, mas **rescisão depende do REGIME jurídico**, que é
`[a confirmar]` p/ comissionados/temporários do município).

### 4.2 Compositor de verbas (puro, orientado a dado)
`Domain/CicloAnual/CompositorRescisao.cs` — função pura que, dado
`Desligamento { tipo, regime, dataDesligamento(ENTRADA), avos13, periodosFerias }`, devolve a lista de
**verbas a lançar** a partir do **catálogo (§1.4)**, decidindo por uma **matriz parametrizável** (não
`if` mágico):
```
record RegraVerbaRescisoria(TipoDesligamento Tipo, RegimeVinculo Regime, string CodigoRubrica, bool Devida);
// Matriz default (pesquisa §3.2) carregada como DADO; flags temFGTS/temAviso/temMulta derivam de regime+tipo.
```
| Verba | Sem justa causa | Pedido/Exoner. | Justa causa | Aposent./Faleci. | Exoner./Vacância (estat.) |
|---|---|---|---|---|---|
| Saldo de salário | ✔ | ✔ | ✔ | ✔ | ✔ |
| 13º proporcional | ✔ | ✔ | ✖ | ✔ | ✔ |
| Férias vencidas + 1/3 | ✔ | ✔ | ✔ | ✔ | ✔ |
| Férias proporcionais + 1/3 | ✔ | ✔ | ✖ | ✔ | ✔ |
| Aviso prévio (Lei 12.506) | ✔ | ✖ | ✖ | ✖ | **✖ (estatutário)** |
| FGTS + multa 40% | ✔ | ✖ | ✖ | ✖ | **✖ (estatutário)** |

- **Estatutário (piloto):** sem FGTS/aviso/multa; usa `ExoneracaoVacancia`; férias/13º indenizados. Regras
  finas (avos, dobra, licença-prêmio indenizável, prazo de pagamento) = `[a confirmar — Estatuto 327/2008]`.
- **Distrato (CLT 484-A):** aviso e multa **pela metade** — parâmetro `[a confirmar]` p/ celetistas públicos.

### 4.3 Cálculo das verbas
- **Saldo de salário** = `vencimento × diasTrabalhados/diasDoMês` (datas de entrada).
- **13º proporcional** = `CalculadoraDecimoTerceiro` com `avos = Avos.Apurar(01/01..dataDesligamento)`,
  tributação **base separada** (§2.2).
- **Férias vencidas/proporcionais + 1/3** = `CalculadoraFerias`, mas **indenizadas** → incidências
  desligadas (Súmula 386). Avos via `PeriodoAquisitivoFerias`.
- **Saldo de salário e aviso prévio trabalhado** incidem normalmente (verba salarial); **aviso indenizado**
  segue a flag da rubrica `[a confirmar]`.
- Previdência/IRRF de cada verba: **sempre pela incidência da `RubricaFolha`** — o motor não decide por nome.

### 4.4 Gancho contábil/eSocial
- **eSocial:** desligamento dispara **S-2299** (já há `InsumoS2299` + `ServidorDesligado`); o grupo
  `verbasResc` é alimentado pelas verbas desta folha de rescisão `[a confirmar — leiaute verbasResc]`. O
  `DesligarServidorHandler` atual publica `ServidorDesligadoIntegrationEvent` — encadear a **abertura da
  folha de rescisão** (Tipo=Rescisao) como reação a esse evento (ou comando explícito do operador).
- **Contábil:** `FolhaFechadaIntegrationEvent(Tipo=Rescisao)` → empenho das verbas rescisórias.

---

## 5. Reuso do pipeline existente (sequência por tipo)

Todas as folhas do ciclo anual reusam **a mesma máquina de estados** `Aberta → (Apurar) → Calcular → Fechar →
[eSocial S-1200/1202/1210/1299/2299] → Pagar`:

| Etapa | Mensal (hoje) | 13º | Férias | Rescisão |
|---|---|---|---|---|
| Abrir | `Abrir(comp)` | `Abrir(comp, DecimoTerceiro)` | `Abrir(comp, Ferias)` | `Abrir(comp, Rescisao)` |
| Gerar verbas | lançamentos | `CalculadoraDecimoTerceiro` | `CalculadoraFerias` | `CompositorRescisao` |
| Apurar legais | `MotorDeCalculoFolha.Calcular` | `CalcularBaseSeparada` | `Calcular` (normal) | por verba (incidência) |
| Abate-teto | sim | **não** | **não** | **não** |
| Fechar/eSocial | S-1200/1299 | S-1200(13) | S-1200(férias) | S-2299/verbasResc |

> **Compatibilidade:** assinatura atual de `MotorDeCalculoFolha.Calcular` **preservada**; o novo
> `CalcularBaseSeparada` e o parâmetro opcional de `TabelaIrrf.CalcularImposto(aplicarSimplificado)` são
> **aditivos**. `FolhaDePagamento.Abrir`/`Calcular` ganham overloads retrocompatíveis (default Mensal).

---

## 6. Ordem de implementação (sub-workflows)

1. **SW-1 — Fundações puras (Domain, sem I/O):** `TipoFolha`/`BaseSeparada` no agregado + migration aditiva;
   `Avos` (VO + testes de ≥15 dias, afastamentos); `OpcoesIrrf` + overload `TabelaIrrf.CalcularImposto`;
   `MotorDeCalculoFolha.CalcularBaseSeparada`. **BDD primeiro.** Não toca no fluxo mensal.
2. **SW-2 — 13º salário:** `CalculadoraDecimoTerceiro`, `ParcelaDecimoTerceiro`, códigos de rubrica em
   `ParametrosFolha`; handlers `AbrirFolha13`, `GerarDecimoTerceiro(1ª/2ª)`, `ApurarDescontosLegaisBaseSeparada`.
   eSocial S-1200(13) roteado por `Tipo`. Seeds de rubricas `13-SAL/INSS-13/IRRF-13` (dado).
3. **SW-3 — Férias:** `PeriodoAquisitivoFerias`, `CalculadoraFerias` (período/terço/abono); handlers
   `AbrirFolhaFerias`, `GerarFerias`; rubricas `FERIAS/1/3-FERIAS/ABONO-PEC` com incidências corretas
   (gozadas tributam, abono isento). eSocial S-1200(férias).
4. **SW-4 — Rescisão:** `TipoDesligamento`/`RegimeVinculo`, `CompositorRescisao` + **matriz parametrizável**;
   encadear ao `ServidorDesligado`; handler `AbrirFolhaRescisao`/`GerarVerbasRescisorias`; S-2299/`verbasResc`.
5. **SW-5 — Ganchos contábil/eSocial:** acrescentar `TipoFolha` ao `FolhaFechadaIntegrationEvent`;
   mapear elementos de despesa por tipo (Contabilidade); validar leiautes eSocial do ciclo anual.
6. **SW-6 — Lei municipal:** resolver os `[a confirmar]` do Estatuto 327/2008 (avos/dobra/abono/exoneração/
   prazo/licença-prêmio) e parametrizar; só então liberar o piloto para estatutários.

> **Dependências:** SW-1 bloqueia SW-2/3/4. SW-6 bloqueia o **piloto real** (não a implementação). Cada SW
> abre com cenários BDD (CLAUDE.md §1) e fecha com testes de invariante/determinismo + NetArchTest.

---

## 7. Pendências de fonte (carregam `[a confirmar]`)

- **Estatuto 327/2008 (Maximiliano de Almeida/RS):** regras finas de férias (período/terço/abono/faltas/dobra),
  13º (proporcionalidade) e exoneração/vacância (verbas, prazo, licença-prêmio). **Baixar texto integral.**
- Enquadramento jurídico de **comissionados/temporários** (estatutário × celetista) → FGTS/aviso/multa.
- Incidência da **cota do segurado/RPPS** sobre o **1/3 de férias** pós-STF Tema 985.
- Leiautes eSocial do ciclo anual: `infoPerApur/13` (S-1200), `verbasResc` (S-2299) — `pesquisa-esocial-eventos.md`.
- Elemento/dotação de **despesa de pessoal** por tipo (13º/férias/rescisão) no plano TCE-RS.
</content>
</invoke>
