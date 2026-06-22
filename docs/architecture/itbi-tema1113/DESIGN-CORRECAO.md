# DESIGN-CORRECAO — Motor de ITBI conforme Tema 1.113/STJ

> **Papel:** Arquiteto. Este documento define o **comportamento correto** do motor de ITBI
> e a **mudança exata** em `CalculoItbi.cs` + `TransmissaoImobiliaria.cs`, mais as novas
> entidades/estados do **arbitramento (CTN art. 148)**.
> Base: `pesquisa-tese-1113.md`, `pesquisa-arbitramento-ctn148.md`, `verificacao.md`.
> Data: 2026-06-22. Itens incertos marcados `[a confirmar]` (CLAUDE.md §16).
> **Exige ADR + parecer da procuradoria** antes de produção (altera regra do M6-DESIGN §3.1).

---

## 1. Resumo (12 linhas)

1. **Problema:** o motor usa `base = MAIOR(valor venal de referência, valor declarado)` — ilegítimo sob o Tema 1.113/STJ (CONFIRMADO em `verificacao.md` §8).
2. **Tese (a):** a base do ITBI é o valor de mercado, **não vinculada ao IPTU**; valor venal nem como piso.
3. **Tese (b):** o **valor declarado presume-se** condizente com o mercado (presunção **relativa**); só cai por **processo administrativo art. 148 CTN**.
4. **Tese (c):** o município **não pode arbitrar previamente** com valor de referência unilateral.
5. **Correção do default:** `base = valor declarado`. Remover o `MAX` aritmético.
6. **Valor venal de referência** vira **parâmetro de triagem/alerta** (indício auditável), nunca base automática.
7. **Arbitramento** passa a ser **fluxo separado e auditado** (CTN 148): lançamento de ofício **só após** processo com contraditório.
8. A base só sobe via um **`ResultadoArbitramento`** vinculado a um processo (nº, fundamentação, contraditório registrado).
9. A memória de cálculo registra a **origem da base**: `Declarada` | `ArbitradaArt148` — substitui o booleano `BaseFoiValorVenal`.
10. **Alerta de divergência** quando `declarado < referência` por margem parametrizável por tenant — apenas *deflagra*, não altera a base.
11. **Novas entidades:** `ProcessoArbitramentoItbi` (agregado com máquina de estados) + `ResultadoArbitramento` (VO de entrada do recálculo).
12. **Pendências `[a confirmar]`:** contraditório diferido (tese de PGM, não vinculante), LC 227/2026 (texto/vetos/vigência), CTM de Maximiliano de Almeida/RS, sistemática de lançamento local.

---

## 2. Comportamento correto do motor (modelo conceitual)

### 2.1 Princípio
A base de cálculo **padrão e presumida** é o **valor declarado** pelo contribuinte. O motor de
cálculo é **puro e determinístico**: recebe a base já decidida e **nunca** escolhe entre valores
por aritmética. A elevação da base é uma **decisão de processo administrativo**, não do motor.

### 2.2 Origem da base (`OrigemBaseCalculoItbi`)
Enum que substitui o booleano `BaseFoiValorVenal`:

| Valor | Significado |
|---|---|
| `Declarada` | Base = valor declarado (regra padrão, presunção de veracidade — tese b). |
| `ArbitradaArt148` | Base = valor arbitrado, **somente** com `ProcessoArbitramentoItbi` concluído e vinculado. |

> Não existe valor `ValorVenalReferencia` como origem de base — o valor venal **nunca** é base automática (tese a/c).

### 2.3 Valor venal de referência → triagem/alerta
Deixa de entrar na aritmética da base. Serve a **dois** papéis, ambos sem alterar o tributo:
- **Triagem:** comparar `valorDeclarado` com `valorVenalReferencia` aplicando uma **margem de tolerância parametrizável por tenant** (`MargemDivergenciaPercentual`).
- **Alerta:** se `valorDeclarado < valorVenalReferencia × (1 − margem)`, emitir um **sinal de divergência** (`AlertaDivergenciaItbi`) que pode (opcionalmente, por decisão humana) *deflagrar* um processo de arbitramento. **A guia continua sendo emitida pelo valor declarado.**

### 2.4 Fluxo
```
Registro da transmissão
   └─ base = valor declarado  ──────────────►  guia (DAM) pelo valor declarado
        │
        └─ triagem: declarado < referência × (1 − margem)?
                 └─ sim → AlertaDivergenciaItbi (NÃO altera base)
                              └─ (decisão humana/fiscal) abrir ProcessoArbitramentoItbi
                                       └─ contraditório (CTN 148) → Concluído
                                                └─ ResultadoArbitramento (valor arbitrado)
                                                         └─ recálculo: base = arbitrada (origem ArbitradaArt148)
                                                                  └─ lançamento de ofício complementar (auditado)
```

> Restituição: se o arbitramento **reduzir** o que foi pago, gera direito a restituição (CTN art. 165, I). `[a confirmar]` com a procuradoria o rito de restituição/lançamento complementar no tenant.

---

## 3. Mudança exata no motor — `CalculoItbi.cs`

### 3.1 `MemoriaItbi` — trocar `BaseFoiValorVenal` por origem da base
**Remover** o parâmetro `bool BaseFoiValorVenal` e **adicionar** `OrigemBaseCalculoItbi Origem` +
referência opcional ao processo de arbitramento.

```csharp
/// <summary>Origem da base de cálculo do ITBI adotada (Tema 1.113/STJ).</summary>
public enum OrigemBaseCalculoItbi
{
    /// <summary>Base = valor declarado pelo contribuinte (regra padrão; presunção de veracidade — tese b).</summary>
    Declarada = 0,

    /// <summary>Base = valor arbitrado por processo administrativo regular (CTN art. 148) com contraditório.</summary>
    ArbitradaArt148 = 1,
}

public sealed record MemoriaItbi(
    ValorMonetario ValorVenalReferencia,   // mantido apenas como referência/triagem auditável
    ValorMonetario ValorDeclarado,
    ValorMonetario BaseCalculo,
    OrigemBaseCalculoItbi Origem,           // substitui bool BaseFoiValorVenal
    Guid? ProcessoArbitramentoId,           // preenchido somente quando Origem == ArbitradaArt148
    bool HaDivergenciaReferencia,           // alerta de triagem (não altera a base)
    decimal AliquotaPercentual,
    ValorMonetario ImpostoBruto,
    ValorMonetario ValorIsencao,
    ValorMonetario ImpostoDevido);
```

### 3.2 `CalculadoraItbi.Calcular` — remover o `MAX`; base = declarado
Substituir as linhas 84-85 (`baseFoiValorVenal = valorVenalReferencia.Valor >= valorDeclarado.Valor; baseCalculo = baseFoiValorVenal ? ...`) por: **base sempre = valor declarado**, com triagem por margem.

```csharp
public static MemoriaItbi Calcular(
    ValorMonetario valorVenalReferencia,
    ValorMonetario valorDeclarado,
    decimal aliquotaGeralPercentual,
    decimal aliquotaSfhPercentual,
    decimal margemDivergenciaPercentual = 0m,   // parametrizável por tenant (CLAUDE.md §7) — só dispara alerta
    ParametrosItbi? parametros = null)
{
    ArgumentNullException.ThrowIfNull(valorVenalReferencia);
    ArgumentNullException.ThrowIfNull(valorDeclarado);
    parametros ??= ParametrosItbi.Padrao;

    if (parametros.PercentualIsencao is < 0m or > 100m)
    {
        throw new ArgumentOutOfRangeException(nameof(parametros), parametros.PercentualIsencao, "O percentual de isenção do ITBI deve estar entre 0 e 100.");
    }
    if (margemDivergenciaPercentual is < 0m or > 100m)
    {
        throw new ArgumentOutOfRangeException(nameof(margemDivergenciaPercentual), margemDivergenciaPercentual, "A margem de divergência do ITBI deve estar entre 0 e 100.");
    }

    // Tema 1.113/STJ (tese b): a base de cálculo padrão é o VALOR DECLARADO (presunção de veracidade).
    // O valor venal de referência NÃO entra na aritmética da base — serve apenas a triagem/alerta.
    var baseCalculo = valorDeclarado;
    var origem = OrigemBaseCalculoItbi.Declarada;

    // Triagem (tese a/c): referência só pode DEFLAGRAR verificação; nunca elevar a base de ofício.
    var limiteInferior = valorVenalReferencia.Valor * (1m - (margemDivergenciaPercentual / 100m));
    var haDivergenciaReferencia = valorDeclarado.Valor < limiteInferior;

    var aliquota = parametros.UsarAliquotaSfh ? aliquotaSfhPercentual : aliquotaGeralPercentual;
    var impostoBruto = baseCalculo.AplicarPercentual(aliquota);
    var valorIsencao = impostoBruto.AplicarPercentual(parametros.PercentualIsencao);
    var impostoDevido = ValorMonetario.De(impostoBruto.Valor - valorIsencao.Valor);

    return new MemoriaItbi(
        valorVenalReferencia,
        valorDeclarado,
        baseCalculo,
        origem,
        ProcessoArbitramentoId: null,
        haDivergenciaReferencia,
        aliquota,
        impostoBruto,
        valorIsencao,
        impostoDevido);
}
```

### 3.3 Novo método: recálculo a partir de arbitramento concluído
A base só sobe aqui — e **somente** com um `ResultadoArbitramento` válido (processo concluído).
O motor não decide arbitrar; ele apenas **aplica** o resultado de um processo auditado.

```csharp
/// <summary>
/// Recalcula o ITBI com base ARBITRADA (CTN art. 148), a partir de um processo administrativo
/// regular concluído com contraditório. Não há escolha aritmética: a base é o valor arbitrado.
/// </summary>
public static MemoriaItbi RecalcularComArbitramento(
    ValorMonetario valorVenalReferencia,
    ValorMonetario valorDeclarado,
    ResultadoArbitramento resultado,        // exige processo concluído (ver §4)
    decimal aliquotaGeralPercentual,
    decimal aliquotaSfhPercentual,
    ParametrosItbi? parametros = null)
{
    ArgumentNullException.ThrowIfNull(resultado);
    if (!resultado.ProcessoConcluido)
    {
        throw new InvalidOperationException("Arbitramento da base do ITBI exige processo administrativo (CTN art. 148) concluído com contraditório.");
    }
    parametros ??= ParametrosItbi.Padrao;

    var baseCalculo = resultado.ValorArbitrado;
    var aliquota = parametros.UsarAliquotaSfh ? aliquotaSfhPercentual : aliquotaGeralPercentual;
    var impostoBruto = baseCalculo.AplicarPercentual(aliquota);
    var valorIsencao = impostoBruto.AplicarPercentual(parametros.PercentualIsencao);
    var impostoDevido = ValorMonetario.De(impostoBruto.Valor - valorIsencao.Valor);

    return new MemoriaItbi(
        valorVenalReferencia,
        valorDeclarado,
        baseCalculo,
        OrigemBaseCalculoItbi.ArbitradaArt148,
        resultado.ProcessoArbitramentoId,
        HaDivergenciaReferencia: true,
        aliquota,
        impostoBruto,
        valorIsencao,
        impostoDevido);
}
```

### 3.4 Atualizar a documentação XML do tipo
Remover a frase "Base de cálculo = **MAIOR** entre o valor venal..." (linhas 44-48) e o
`TODO(validar-oficial)` (linhas 49-56), substituindo por: "Base = **valor declarado** (Tema 1.113/STJ,
REsp 1.937.821; presunção de veracidade do valor declarado). O valor venal de referência é parâmetro
de triagem/alerta. Elevação da base **somente** via `RecalcularComArbitramento` (CTN art. 148)."

---

## 4. Novas entidades/estados do arbitramento (CTN art. 148)

Fluxo **separado** e **auditado** (CLAUDE.md §4, §6). Modelado como **agregado** com máquina de
estados — nunca como cálculo aritmético.

### 4.1 `ProcessoArbitramentoItbi` (AggregateRoot, IMustHaveTenant)
Vincula-se a uma `TransmissaoImobiliaria`. Guarda fundamentação, contraditório e desfecho.

**Campos:** `Id`, `TenantId`, `TransmissaoImobiliariaId`, `NumeroProcesso` (protocolo administrativo),
`MotivoInstauracao` (texto — por que a declaração "não merece fé"; CTN 148: omissa/falsa/não fidedigna),
`ValorPropostoFisco`, `FundamentacaoFisco`, `JustificativaContribuinte` (avaliação contraditória),
`ValorArbitradoFinal`, `Estado`, datas de cada transição, `ResponsavelId`.

**Máquina de estados (`EstadoArbitramentoItbi`):**

| Estado | Descrição | Transição válida |
|---|---|---|
| `Instaurado` | Processo aberto com motivo individualizado (não basta "menor que pauta"). | → `AguardandoContraditorio` |
| `AguardandoContraditorio` | Contribuinte notificado; prazo para defesa/avaliação contraditória (art. 148, parte final). | → `EmAnalise`, `Cancelado` |
| `EmAnalise` | Fisco avalia a defesa; ônus da prova é do fisco. | → `Concluido`, `Cancelado` |
| `Concluido` | Decisão final fundamentada; gera `ResultadoArbitramento`. | (terminal) |
| `Cancelado` | Encerrado sem arbitrar (declaração prevaleceu). | (terminal) |

**Invariantes:**
- Não se pode pular `AguardandoContraditorio` (contraditório é obrigatório — tese b + acórdão).
- `Concluido` exige `ValorArbitradoFinal` definido e `JustificativaContribuinte` registrada (ou prazo de defesa decorrido, com data — `[a confirmar]` regra de revelia com a procuradoria).
- Toda transição gera **domain event** (`ProcessoArbitramentoItbi*`) → trilha de auditoria imutável (CLAUDE.md §6).
- `[a confirmar]` **contraditório diferido** (lançar pelo declarado e abrir arbitramento depois): tese de PGM (Joinville 2025), **não vinculante** — só adotar se a procuradoria do tenant aprovar.

### 4.2 `ResultadoArbitramento` (Value Object — entrada do recálculo)
```csharp
public sealed record ResultadoArbitramento(
    Guid ProcessoArbitramentoId,
    ValorMonetario ValorArbitrado,
    bool ProcessoConcluido);   // true SOMENTE quando EstadoArbitramentoItbi == Concluido
```
Produzido **apenas** por `ProcessoArbitramentoItbi` no estado `Concluido`. É a **única** porta de
entrada para elevar a base no motor (§3.3).

### 4.3 `AlertaDivergenciaItbi` (sinal de triagem)
Emitido no cálculo padrão quando `HaDivergenciaReferencia == true`. **Não** altera tributo; serve para
a fila de revisão fiscal decidir (humano) se instaura o processo. Pode ser um domain event/registro
de leitura, não parte da `MemoriaItbi` aritmética.

---

## 5. Impacto em `TransmissaoImobiliaria.cs`

- Trocar a propriedade `bool BaseFoiValorVenal` (linhas 55, 85-86) por `OrigemBaseCalculoItbi Origem` e adicionar `Guid? ProcessoArbitramentoId`.
- Atualizar a doc da propriedade `BaseCalculo` (linhas 82-83): de "maior entre venal e declarado" para "valor declarado (Tema 1.113) ou valor arbitrado (CTN 148)".
- Atualizar a doc do `Registrar` (linhas 94-96): remover "base = maior entre valor venal e valor declarado".
- Adicionar comportamento de **recálculo/lançamento complementar** quando um arbitramento concluir: método de domínio (ex.: `AplicarArbitramento(ResultadoArbitramento, MemoriaItbi)`) que atualiza `BaseCalculo`, `Origem`, `ProcessoArbitramentoId`, `ImpostoDevido` e levanta evento → guia/lançamento de ofício complementar auditado. A `TransmissaoImobiliaria` nasce sempre pela base **declarada**.

---

## 6. Conformidade com CLAUDE.md

- **§7 (nada hardcoded):** `MargemDivergenciaPercentual`, alíquotas e metodologia de referência são parâmetros por tenant/CTM.
- **§4/§6 (auditoria):** toda elevação de base tem trilha ligada ao nº do processo art. 148; máquina de estados gera eventos imutáveis.
- **§5 (multi-tenant):** `ProcessoArbitramentoItbi` é `IMustHaveTenant`.
- **§16:** itens incertos marcados `[a confirmar]`; **exige ADR + procuradoria** antes de produção.

---

## 7. Pendências `[a confirmar]`

1. **Contraditório diferido** — tese de PGM (não vinculante); validar com a procuradoria do tenant.
2. **LC 227/2026 (ex-PLP 108/2024)** — redação final do dispositivo de ITBI, vetos, vigência e rito de contestação (DOU 14/01/2026); não usar como base de design sem texto oficial.
3. **CTM de Maximiliano de Almeida/RS** — se prevê pauta/valor de referência como base ou piso, é ilegal sob o Tema 1.113 e precisa de adequação.
4. **Sistemática de lançamento local** (declaração x homologação) — define o encaixe do workflow.
5. **Regra de revelia** no contraditório (prazo decorrido sem defesa) — confirmar com a procuradoria.
6. **Rito de restituição/lançamento complementar** (CTN art. 165, I) no tenant.

---

## 8. Arquivos afetados

- `src/Modules/Tributos/Tensorroot.Gov.Modules.Tributos.Domain/Calculo/CalculoItbi.cs` (motor — §3)
- `src/Modules/Tributos/Tensorroot.Gov.Modules.Tributos.Domain/Itbi/TransmissaoImobiliaria.cs` (agregado — §5)
- **Novos** (em `.../Domain/Itbi/Arbitramento/`): `ProcessoArbitramentoItbi.cs`, `EstadoArbitramentoItbi.cs`, `ResultadoArbitramento.cs`, `OrigemBaseCalculoItbi.cs`, eventos de domínio do arbitramento — §4.
- ADR a criar em `docs/adr/` registrando a decisão (substitui regra do M6-DESIGN §3.1).
