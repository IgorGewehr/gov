# Serviço transversal de DIAS ÚTEIS / PRAZOS LEGAIS — DESIGN (W9.1)

> **Escopo.** Serviço compartilhado de **contagem de prazos legais** (dias úteis e corridos a
> partir de uma data, respeitando feriados) consumido por **três fluxos do M9**:
> **W9.1** (PNCP — Lei 14.133 art. 94: divulgação em **20 d.u.**; PCA/registro em **10 d.u.**),
> **W9.3** (Obras — art. 94 §3: **25 / 45 d.u.**) e
> **W9.6** (Convênios/MROSC — análise/saneamento **45 / 60 / 150 / 180 d**, mistos d.u./corridos).
> CLAUDE.md §7/§16: **sem números mágicos**, **prazos parametrizáveis por tenant**, **nada hardcoded**,
> determinismo/reprodutibilidade (sem relógio interno no cálculo).

> **Estado atual (âncora — não foto pré-M0).** Já existe `ICalendarioDiasUteis` (porta de domínio) em
> `Modules/Transparencia/...Domain/Esic/ICalendarioDiasUteis.cs` com **uma única operação**
> (`SomarDiasUteis`) e a impl `CalendarioDiasUteisPadrao`
> (`...Transparencia.Infrastructure/PortalPublico/`) que cobre **só fins de semana + feriados nacionais
> FIXOS**, sem feriados móveis e com um `// TODO(parametrizar-feriados)` explícito. Os três docs do M9
> (CONVENIOS-MROSC-DESIGN.md, OBRAS-DESIGN.md) já anotam: *"W9.1 vai promovê-lo a serviço transversal
> compartilhado… este módulo CONSOME, não recria."* **Este design executa essa promoção.**

---

## 1) ONDE FICA — decisão de placement

### Decisão

| Artefato | Projeto | Camada |
|---|---|---|
| **`ICalendarioDiasUteis`** (porta) | **`SharedKernel`** (`ValueObjects/Tempo/` ou `Tempo/`) | Domínio puro |
| **`PrazoLegal`** + `UnidadePrazo` (VO/enum) | **`SharedKernel`** (`ValueObjects/Tempo/`) | Domínio puro |
| **`IFeriadosTenantProvider`** (porta de feriados) | **`BuildingBlocks.Application/Abstractions`** | Application |
| **`CalendarioDiasUteis`** (impl. do algoritmo) | **`BuildingBlocks.Infrastructure/Tempo`** | Infraestrutura |
| **`FeriadosMoveis`** (Computus/Gauss, estático puro) | **`SharedKernel`** (`Tempo/`) | Domínio puro |
| **`IFeriadosTenantProvider` impl. (config + cache)** | **`BuildingBlocks.Infrastructure/Tempo`** | Infraestrutura |

> Resumo: **o contrato + o VO + o algoritmo de feriados móveis vão para o `SharedKernel`**; **a
> impl. que aplica regras de tenant e a fonte dos feriados municipais vão para os `BuildingBlocks`**.

### Justificativa (SharedKernel para o contrato + VO)

1. **Transversalidade real, não acidental.** O conceito "dia útil / prazo legal" não pertence a
   Administração, nem a Obras, nem a Convênios, nem a Transparência — é **vocabulário comum do
   domínio público brasileiro** (igual a `Cpf`, `Cnpj`, `OutboxMessage`, que já moram no SharedKernel).
   Deixá-lo num módulo cria a tentação de outro módulo referenciar o **interno de Transparência**, o
   que é **bug crítico** (CLAUDE.md §2: cross-module só via `*.Contracts`). O `SharedKernel`, ao
   contrário, é **referenciável por toda camada `Domain`** sem violar isolamento.

2. **Domínio rico, sem dependências infra.** A porta `ICalendarioDiasUteis` é injetada **dentro do
   agregado** nas transições (exatamente como `PedidoInformacaoSic` já faz hoje, e como
   `ObraPublica`/`ConvenioRecebido`/`ParceriaOsc` vão fazer). Logo o contrato **precisa** estar onde o
   `Domain` pode vê-lo. `SharedKernel` não referencia `Application`/`Infrastructure`/EF/ASP.NET
   (CLAUDE.md §2 — tabela de dependências), então é o único lugar que **todos os `Domain` alcançam**
   sem inverter a regra de dependência. O `csproj` atual do SharedKernel só tem `MediatR.Contracts` —
   permanece **infra-free**.

3. **`PrazoLegal` é Value Object puro.** Imutável, comparado por valor, sem identidade — encaixa no
   `ValueObject` base já existente (`SharedKernel/Primitives/ValueObject.cs`). Reusável pelos três
   fluxos sem reescrever lógica de "vencido?/a vencer?".

4. **Por que a IMPL não vai no SharedKernel.** O **cálculo** (varrer dias, montar conjunto de
   feriados, decidir d.u. vs corrido) e principalmente a **fonte dos feriados municipais por tenant**
   (config/`IConfiguration`, cache, futuramente tabela) são **preocupações de infraestrutura**. Mantê-las
   nos `BuildingBlocks.Infrastructure` preserva o SharedKernel limpo e permite trocar a fonte (config →
   tabela → API municipal) sem tocar no domínio. Espelha o padrão já existente
   (`RegraAfastamentoProvider`, `ICalendarioFiscal`): **porta no domínio/abstração, fonte parametrizada
   na infra**.

### Migração do que já existe (não recriar)

- **Promover** `ICalendarioDiasUteis` de `Transparencia.Domain/Esic/` para
  `SharedKernel/Tempo/ICalendarioDiasUteis.cs`, **ampliando** a API (item 2).
- **Substituir** `CalendarioDiasUteisPadrao` por `CalendarioDiasUteis` em
  `BuildingBlocks.Infrastructure/Tempo/`, que **passa a tratar feriados móveis + municipais por tenant**
  e **remove o `// TODO(parametrizar-feriados)`**.
- O e-SIC (Transparência) deixa de registrar sua própria impl. (`AddSingleton<…, CalendarioDiasUteisPadrao>`)
  e passa a **consumir** o registro central (item 4) — **comportamento idêntico** (20 d.u. + prorrogação
  10 d.u.), agora com móveis/municipais corretos. **Manter um shim de compat** em
  `Transparencia.Domain/Esic` que apenas re-exporte/`using` o tipo do SharedKernel **só se** o
  refactor dos `using` for custoso; preferência é atualizar os `using` (poucos arquivos).

---

## 2) `ICalendarioDiasUteis` — contrato ampliado (SharedKernel)

> **Reprodutibilidade (CLAUDE.md §7).** Nenhum método lê o relógio. Toda contagem **recebe a data
> base** (`DateOnly`). O `TimeProvider` fica **fora** do calendário — quem decide "vencido hoje?" passa
> `hoje` explicitamente (vindo do `TimeProvider` no Application, nunca dentro do cálculo). Mesmo par de
> entradas ⇒ mesma saída, sempre (testável sem mockar tempo).

```csharp
namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>
/// Calendário de DIAS ÚTEIS por tenant: conhece fins de semana, feriados nacionais FIXOS (lei) e
/// MÓVEIS (Páscoa/Carnaval/Corpus Christi via Computus), e feriados MUNICIPAIS/pontos facultativos
/// parametrizados por tenant. Porta de DOMÍNIO (injetada nas transições do agregado). DETERMINÍSTICA:
/// não usa relógio interno — recebe sempre a data base (CLAUDE.md §7/§16).
/// </summary>
public interface ICalendarioDiasUteis
{
    /// <summary>Soma <paramref name="diasUteis"/> dias úteis a partir de <paramref name="inicio"/>,
    /// pulando fins de semana e feriados do tenant. A contagem começa no PRÓXIMO dia útil
    /// (inicio NÃO conta), convenção do PNCP/processo administrativo. n &gt;= 0.</summary>
    DateOnly AdicionarDiasUteis(DateOnly inicio, int diasUteis);

    /// <summary>Verdadeiro se <paramref name="data"/> é dia útil (não é fim de semana nem feriado do tenant).</summary>
    bool EhDiaUtil(DateOnly data);

    /// <summary>Próximo dia útil &gt;= <paramref name="data"/> (devolve a própria data se já for útil).</summary>
    DateOnly ProximoDiaUtil(DateOnly data);

    /// <summary>Quantidade de dias úteis no intervalo [<paramref name="a"/>, <paramref name="b"/>]
    /// (exclusivo no início, inclusivo no fim — d.u. "decorridos"). Negativo se b &lt; a.</summary>
    int DiasUteisEntre(DateOnly a, DateOnly b);
}
```

Notas de contrato (decisões para os três fluxos, sem ambiguidade — testável):

- **`AdicionarDiasUteis`** usa convenção **"início não conta, conta a partir do próximo dia útil"** —
  alinhada ao PNCP/Lei 14.133 (a publicação da data inicial não consome prazo). Mantém o comportamento
  hoje praticado pelo e-SIC (`SomarDiasUteis` atual também começa no `AddDays(1)`), portanto **sem
  regressão**. `AdicionarDiasUteis(x, 0) == x`.
- **`ProximoDiaUtil`** é **inclusivo** (idempotente em dia útil) — base para "vencimento cai em feriado
  → rola para o próximo útil".
- **Dias CORRIDOS** não precisam do calendário (`data.AddDays(n)`), mas **a regra do art. 110 da Lei
  14.133 / art. 224 CPC** ("vencimento em fim de semana/feriado prorroga para o 1º dia útil seguinte")
  é aplicada via `ProximoDiaUtil(vencimentoCorrido)` — encapsulada no VO `PrazoLegal` (item 3), não
  espalhada nos handlers.
- Não há sobrecarga "interna" lendo relógio. O `int n` vem sempre do **VO de prazo** do tenant, nunca
  literal no código.

---

## 3) VO de PRAZO — `PrazoLegal` (SharedKernel, reusável pelos 3 fluxos)

Encapsula **"X dias úteis OU corridos a partir de Y"** + **vencido?/a vencer?** + a **norma-fonte**
citável (para auditoria/TCE). Resolve o `Vencimento` **uma vez** e responde perguntas sem reler
calendário. É **value object** (compara por valor, imutável).

```csharp
namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>Unidade de contagem de um prazo legal.</summary>
public enum UnidadePrazo { DiasUteis = 1, DiasCorridos = 2 }

/// <summary>
/// Prazo legal: "X dias (úteis|corridos) a partir de Y", com norma-fonte citável. Imutável,
/// reproduzível (recebe o calendário + as datas; nunca lê relógio). Reusado por W9.1/W9.3/W9.6.
/// </summary>
public sealed class PrazoLegal : ValueObject
{
    private PrazoLegal(DateOnly inicio, int quantidade, UnidadePrazo unidade, DateOnly vencimento, string normaFonte)
    { Inicio = inicio; Quantidade = quantidade; Unidade = unidade; Vencimento = vencimento; NormaFonte = normaFonte; }

    public DateOnly Inicio { get; }
    public int Quantidade { get; }
    public UnidadePrazo Unidade { get; }
    /// <summary>Data-limite já resolvida (em d.u.: via calendário; em corridos: AddDays + rolagem p/ dia útil).</summary>
    public DateOnly Vencimento { get; }
    /// <summary>Citação legal (ex.: "Lei 14.133 art. 94"). Sem número mágico solto — a fonte viaja com o prazo.</summary>
    public string NormaFonte { get; }

    /// <summary>Constrói o prazo resolvendo o vencimento a partir de <paramref name="inicio"/>.
    /// d.u. ⇒ calendario.AdicionarDiasUteis; corridos ⇒ AddDays(n) e, se cair em dia não útil,
    /// prorroga para o próximo dia útil (Lei 14.133 art. 110 / art. 224 CPC).</summary>
    public static PrazoLegal Criar(DateOnly inicio, int quantidade, UnidadePrazo unidade,
                                   string normaFonte, ICalendarioDiasUteis calendario)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);
        ArgumentException.ThrowIfNullOrWhiteSpace(normaFonte);
        ArgumentNullException.ThrowIfNull(calendario);
        var venc = unidade == UnidadePrazo.DiasUteis
            ? calendario.AdicionarDiasUteis(inicio, quantidade)
            : calendario.ProximoDiaUtil(inicio.AddDays(quantidade));
        return new PrazoLegal(inicio, quantidade, unidade, venc, normaFonte);
    }

    /// <summary>Vencido em <paramref name="hoje"/> (data &gt; vencimento). 'hoje' vem do TimeProvider no Application.</summary>
    public bool Vencido(DateOnly hoje) => hoje > Vencimento;

    /// <summary>A vencer em <paramref name="hoje"/> (ainda dentro do prazo, vencimento incluído).</summary>
    public bool AVencer(DateOnly hoje) => hoje <= Vencimento;

    /// <summary>Dias úteis restantes até o vencimento (negativo se já vencido).</summary>
    public int DiasUteisRestantes(DateOnly hoje, ICalendarioDiasUteis calendario)
        => calendario.DiasUteisEntre(hoje, Vencimento);

    /// <summary>Prorroga UMA aplicação adicional (ex.: e-SIC +10 d.u.; PC OSC +30 d). Devolve novo VO.</summary>
    public PrazoLegal Prorrogar(int quantidadeAdicional, string normaFonte, ICalendarioDiasUteis calendario)
        => Criar(Vencimento, quantidadeAdicional, Unidade, normaFonte, calendario);

    protected override IEnumerable<object?> GetEqualityComponents()
    { yield return Inicio; yield return Quantidade; yield return (int)Unidade; yield return Vencimento; yield return NormaFonte; }
}
```

Como cada fluxo reutiliza (sem reescrever lógica de prazo):

| Fluxo | Prazo | Construção |
|---|---|---|
| **W9.1 PNCP** | divulgação **20 d.u.**; PCA **10 d.u.** | `PrazoLegal.Criar(dataAto, n, DiasUteis, "Lei 14.133 art. 94", cal)` |
| **W9.3 Obras** | art. 94 §3 **25 / 45 d.u.** | `PrazoLegal.Criar(dataConclusaoEdital, n, DiasUteis, "Lei 14.133 art. 94 §3", cal)` |
| **W9.6 Convênios** | **45/60/150/180** (mistos) | `PrazoLegal.Criar(dataSubmissao, n, unidadeDoTenant, normaFonte, cal)` |
| **e-SIC (já existe)** | **20 d.u. + 10 d.u.** | `Criar(...)` + `Prorrogar(10, "LAI art. 11 §2º", cal)` |

> O **`n` (quantidade), a `unidade` e a `normaFonte` NÃO são literais no código** — vêm dos
> **parâmetros do tenant** (`IConveniosParametros`, `IObrasParametros`, `IPncpParametros`…), cada um
> com `Quantidade`/`Unidade`/`NormaFonte`, exatamente como os docs de Obras e Convênios já preveem
> (A.5/B.5). O VO só **executa**; a **fonte do número** é parametrizável (CLAUDE.md §7/§16).

---

## 4) Consumo (DI), feriados por tenant e estratégia de testes

### 4a. Estratégia de feriados — 3 camadas

```
FERIADOS DO TENANT (para um exercício/ano) =
    Nacionais FIXOS (lei federal — constantes nomeadas, não mágicas)
  ∪ Nacionais MÓVEIS (derivados da Páscoa — Computus/Gauss, SEM hardcode de ano)
  ∪ Municipais + pontos facultativos (PARAMETRIZÁVEIS por tenant — config/tabela)
```

**(i) Nacionais FIXOS** — `(mês, dia)` constantes (Lei 662/1949 + Lei 6.802/1980): 01/01, 21/04, 01/05,
07/09, 12/10, 02/11, 15/11, **20/11 (Consciência Negra — Lei 14.759/2023, feriado nacional desde 2024)**,
25/12. Já existem (parcialmente) em `CalendarioDiasUteisPadrao`; **acrescentar 20/11** ao migrar.

**(ii) Nacionais MÓVEIS — algoritmo de Gauss/Computus (Páscoa), sem hardcode de ano.** A Páscoa fixa
Carnaval (−47 d), Sexta-feira Santa (−2 d) e Corpus Christi (+60 d). Tabela puro-domínio no SharedKernel:

```csharp
namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>Feriados/pontos móveis derivados da Páscoa (Computus de Gauss/Meeus). Puro, sem relógio, sem hardcode de ano.</summary>
public static class FeriadosMoveis
{
    /// <summary>Domingo de Páscoa do <paramref name="ano"/> (algoritmo "anonymous Gregorian"/Meeus-Jones-Butcher).</summary>
    public static DateOnly Pascoa(int ano)
    {
        int a = ano % 19, b = ano / 100, c = ano % 100, d = b / 4, e = b % 4,
            f = (b + 8) / 25, g = (b - f + 1) / 3,
            h = ((19 * a) + b - d - g + 15) % 30,
            i = c / 4, k = c % 4,
            l = (32 + (2 * e) + (2 * i) - h - k) % 7,
            m = (a + (11 * h) + (22 * l)) / 451,
            mes = (h + l - (7 * m) + 114) / 31,
            dia = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(ano, mes, dia);
    }

    public static DateOnly SextaFeiraSanta(int ano) => Pascoa(ano).AddDays(-2);
    public static DateOnly SegundaCarnaval(int ano) => Pascoa(ano).AddDays(-48); // facultativo seg.
    public static DateOnly TercaCarnaval(int ano)   => Pascoa(ano).AddDays(-47); // ponto facultativo terça
    public static DateOnly CorpusChristi(int ano)   => Pascoa(ano).AddDays(60);  // facultativo
}
```

> **Carnaval e Corpus Christi são, por padrão, PONTOS FACULTATIVOS** (não feriados nacionais de lei).
> Decisão: **default = NÃO contam como dia não-útil**, salvo o tenant marcá-los como feriado/ponto
> facultativo na config (muitos municípios decretam). Sexta-feira Santa **é** feriado (efeito de feriado
> religioso consolidado). Essa escolha é **parametrizável** (item iii), não hardcoded.

**(iii) Municipais + pontos facultativos — POR TENANT (config agora, tabela depois).** Porta na
Application + fonte de config na Infra, espelhando `RegraAfastamentoProvider`/`ICalendarioFiscal`:

```csharp
namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>Fonte de feriados específicos do tenant (municipais, religiosos locais, pontos facultativos
/// adotados) e do conjunto vigente para um ano. Parametrizável por tenant (CLAUDE.md §7/§16).</summary>
public interface IFeriadosTenantProvider
{
    /// <summary>Conjunto de datas NÃO ÚTEIS do tenant no <paramref name="ano"/> (fixos ∪ móveis ∪ municipais
    /// ∪ facultativos adotados). Reproduzível: depende só de (tenant, ano). Resultado cacheável por (tenant, ano).</summary>
    IReadOnlySet<DateOnly> FeriadosDoAno(int ano);
}
```

Config por tenant (`appsettings`/Key Vault-friendly, sem segredo) — **defaults documentais espelham a
lei, valores reais vêm da config do tenant**:

```jsonc
// Seção "Tempo:Feriados" — por tenant (futuro: tabela CalendarioFeriados schema 'core')
{
  "Tempo": {
    "Feriados": {
      "AdotaCarnaval": true,          // ponto facultativo seg/terça como não-útil
      "AdotaCorpusChristi": true,
      "MunicipaisFixos": [ "03-19", "07-25" ],   // (MM-dd) ex.: aniversário do município, padroeiro
      "MunicipaisData": [ "2026-08-15" ]         // datas pontuais (yyyy-MM-dd) quando necessário
    }
  }
}
```

> **Onde guardar os feriados municipais por tenant.** **Fase atual:** `IConfiguration` por tenant
> (seção `Tempo:Feriados`), lido por `FeriadosTenantProvider` na Infra, **resolvendo o `TenantId` via
> `ITenantContext`** (mesma mecânica de `RegraAfastamentoProvider`). **Evolução natural (sem mudar o
> contrato):** tabela `core.CalendarioFeriado` (`TenantId`, `Data`, `Descricao`, `Tipo`,
> `VigenciaInicio`), `IMustHaveTenant`, sob Global Query Filter — o provider passa a consultar o
> DbContext em vez da config. O **domínio não muda**: ele só conhece `ICalendarioDiasUteis`.

**Cache.** `FeriadosDoAno(ano)` é puro por `(tenant, ano)` → `IMemoryCache` com chave
`feriados:{tenantId}:{ano}` (invalida ao editar a config/tabela). Evita recomputar Computus em laços de
contagem.

### 4b. Registro (Composition Root) e injeção

Registro **único e central** nos `BuildingBlocks.Infrastructure` (não em cada módulo):

```csharp
// BuildingBlocks.Infrastructure/Tempo/TempoServiceCollectionExtensions.cs
public static IServiceCollection AddCalendarioDiasUteis(this IServiceCollection services)
{
    services.AddMemoryCache();
    services.AddScoped<IFeriadosTenantProvider, FeriadosTenantProvider>(); // usa ITenantContext + IConfiguration
    services.AddScoped<ICalendarioDiasUteis, CalendarioDiasUteis>();       // depende de IFeriadosTenantProvider
    return services;
}
```

- Chamado **uma vez** no ApiHost/Workers (Composition Root), **antes** dos módulos. **Scoped** (segue o
  `TenantContext` da requisição); o cache de feriados é cross-request por chave `(tenant, ano)`.
- **W9.1 / W9.3 / W9.6 consomem por DI** injetando `ICalendarioDiasUteis` **no Application** (handlers)
  e **passando ao agregado** nas transições — exatamente como `PedidoInformacaoSic.Abrir(..., calendario)`
  já faz. O **`hoje`** para `Vencido/AVencer` vem do **`TimeProvider`** injetado no handler (nunca dentro
  do calendário). Os números (`n`/`unidade`/`normaFonte`) vêm dos `*Parametros` do tenant (A.5/B.5).
- **Transparência (e-SIC)** troca seu `AddSingleton<…, CalendarioDiasUteisPadrao>` por consumir o
  registro central — comportamento preservado, agora com móveis/municipais.

### 4c. Estratégia de testes (xUnit + FluentAssertions; CLAUDE.md §12)

Calendário é **regra de negócio crítica de prazo legal** → cobertura obrigatória. Usar **fake
determinístico** de `IFeriadosTenantProvider` (conjunto fixo) — sem relógio, sem I/O.

1. **Anos bissextos.** 29/02 existente em ano bissexto vs inexistente em comum; contagem que atravessa
   fevereiro de **2024 (bissexto)** e **2025 (comum)** — `DiasUteisEntre` e `AdicionarDiasUteis` batem o
   nº exato de d.u. cruzando 28→29/02.
2. **Virada de ano.** `AdicionarDiasUteis(2026-12-28, 5)` deve **pular 01/01** e cair em jan/2027 —
   exercita carregar feriados de **dois anos** (o provider deve unir `FeriadosDoAno(2026) ∪
   FeriadosDoAno(2027)` quando a contagem cruza a fronteira).
3. **Feriado em fim de semana.** Feriado fixo que cai sábado/domingo **não soma "dia extra"** (já não
   era útil); `EhDiaUtil(sábado-que-é-feriado) == false` sem dupla contagem. **Vencimento corrido** que
   cai em feriado/fim de semana **rola** via `ProximoDiaUtil` (art. 110 / 224 CPC).
4. **Feriados móveis (Computus).** `FeriadosMoveis.Pascoa` validada contra tabela conhecida
   (2024-03-31, 2025-04-20, 2026-04-05, 2027-03-28); Sexta Santa/Carnaval/Corpus derivados; **sem
   hardcode de ano** (parametrizado pelo `int ano`). Caso de borda: Carnaval em **fevereiro** vs
   Corpus em **maio/junho** no mesmo cálculo.
5. **Determinismo/reprodutibilidade.** Mesma entrada chamada N vezes ⇒ mesma saída; nenhuma dependência
   de `DateTime.Now`/`TimeProvider` no caminho de cálculo (teste roda em qualquer data do sistema).
6. **Convenção "início não conta".** `AdicionarDiasUteis(x,0)==x`; `DiasUteisEntre(seg, sex)==4`;
   `ProximoDiaUtil(dia útil)` idempotente.
7. **VO `PrazoLegal`.** d.u. vs corridos; `Vencido/AVencer` nas bordas (`hoje==Vencimento` ⇒ a vencer);
   `Prorrogar` encadeado (e-SIC 20+10; PC OSC 90+30); igualdade estrutural do VO.
8. **Multi-tenant.** Tenant A (com Carnaval) e tenant B (sem) ⇒ vencimentos diferentes para o mesmo
   `Criar`, provando que a fonte de feriados é por tenant (sem vazamento).
9. **Não-regressão e-SIC.** Cenário LAI (20 d.u. + 10 d.u.) reproduz os prazos atuais após a migração.

---

## Resumo executivo

- **Placement:** `ICalendarioDiasUteis` + `PrazoLegal`/`UnidadePrazo` + `FeriadosMoveis` →
  **`SharedKernel/Tempo`** (transversal, alcançável por todo `Domain`, infra-free). Impl. do algoritmo
  e fonte de feriados → **`BuildingBlocks` (Application port `IFeriadosTenantProvider` + Infra
  `CalendarioDiasUteis`/`FeriadosTenantProvider`)**. **Promove** o `ICalendarioDiasUteis` que hoje vive
  em Transparência (remove o `// TODO(parametrizar-feriados)`); módulos **consomem, não recriam**.
- **API:** `AdicionarDiasUteis`, `EhDiaUtil`, `ProximoDiaUtil`, `DiasUteisEntre` — todas recebem a data
  base, **sem relógio interno** (reproduzível).
- **VO `PrazoLegal`:** "X d.u./corridos a partir de Y" + `Vencido?`/`AVencer?` + `NormaFonte` citável +
  `Prorrogar`; reusado por W9.1/W9.3/W9.6/e-SIC com `n`/`unidade`/`norma` **vindos dos parâmetros do
  tenant** (sem número mágico).
- **Feriados móveis:** **Computus de Gauss/Meeus** (Páscoa → Carnaval/Sexta Santa/Corpus), **sem
  hardcode de ano**; nacionais fixos como constantes (incl. **20/11**); municipais/facultativos **por
  tenant via config (hoje) → tabela `core.CalendarioFeriado` (evolução)**, com cache `(tenant, ano)`.
- **DI:** registro central único nos BuildingBlocks; `ICalendarioDiasUteis` **scoped** (segue o tenant),
  injetado no Application e **passado ao agregado** nas transições; `hoje` sempre do `TimeProvider`,
  fora do cálculo.
