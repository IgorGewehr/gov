# P0 — Snapshots EF não-regenerados: diagnóstico cirúrgico + plano de correção

> Origem: AUDITORIA-ESTADO-PROJETO (M9-prep). Achado P0: migrations hand-written sem `.Designer.cs`
> e `...ModelSnapshot.cs` atrás do modelo real → risco de divergência modelo↔snapshot e de
> `MigrateAsync` aplicar um schema incompleto/incoerente em PRODUÇÃO.
>
> Documento READ-ONLY de planejamento. **Nenhum `dotnet`/`npm` foi executado.** A `:5080` não foi tocada.

## Contexto do ambiente (a raiz do problema)

- `global.json` → SDK **8.0.400** (`rollForward: latestMajor`). Runtime disponível na máquina: **.NET 10**.
- `.config/dotnet-tools.json` → `dotnet-ef` **8.0.11** (`rollForward: false`), ou seja, **ef tool é net8**.
- Por restrição do ambiente (só runtime .NET 10 instalado; o ef tool é net8 e não roda sob net10 sem o
  runtime 8 presente), os agentes vinham **escrevendo migrations à mão** em vez de `dotnet ef migrations add`.
- Consequência: o passo que o `dotnet ef` faz automaticamente — **(a)** gerar o `_Mig.Designer.cs` e
  **(b)** reescrever o `...ModelSnapshot.cs` para o novo estado do modelo — passou a ser feito **manualmente
  e de forma inconsistente**. Algumas migrations saíram sem Designer; em um módulo (Saúde) o snapshot ficou
  congelado várias migrations atrás.

### Por que cada artefato importa (e qual é/ não é crítico)

| Artefato | Para que serve | Falta dele quebra `MigrateAsync`? |
|---|---|---|
| `_Mig.cs` (`Up`/`Down`) | DDL aplicado em PROD por `MigrateAsync` | **SIM** — é o que roda. Todos os 9 órfãos TÊM o `.cs`. |
| `_Mig.Designer.cs` | Carimba `[Migration(id)]` + snapshot-naquele-ponto; usado pelo **tooling** para diffs | **NÃO** em runtime (o atributo `[Migration]` pode estar no `.cs`); **SIM** para o `ef` voltar a funcionar e para `add` futuro coerente |
| `...ModelSnapshot.cs` | Estado acumulado do modelo; base de diff do **próximo** `migrations add` | **NÃO** em PROD-migrate; **SIM** indiretamente: se estiver atrás, o próximo `add` gera DDL duplicado/errado e `HasPendingModelChanges()`=true |

> Tradução do risco: o gap de **Designer** é, isoladamente, **tooling/manutenção** (não derruba PROD hoje).
> O gap de **snapshot atrás do modelo** (Saúde) é o **risco real de drift**: o modelo tem tabelas que o
> snapshot não conhece → qualquer `migrations add` futuro tentaria recriá-las, e o invariante
> "migrations == modelo" está quebrado. Os dois precisam ser fechados juntos para destravar M9.

---

## 1) MAPA: gaps por módulo (migration/snapshot)

### 1.a — Migrations SEM par `.Designer.cs` (9 órfãos, em 4 módulos)

| Módulo | Migration órfã (sem Designer) | Snapshot já reflete? |
|---|---|---|
| **Saúde** | `20260623150000_CadastroEstabelecimentoProfissional` | ❌ não |
| **Saúde** | `20260623160000_Agendamento` | ❌ não |
| **Saúde** | `20260623170000_FarmaciaImunizacao` | ❌ não |
| **Saúde** | `20260623180000_VigilanciaSanitaria` | ❌ não |
| **Finanças** | `20260623000000_Contabilidade` | ✅ sim (snapshot tem as entidades) |
| **Finanças** | `20260623120000_MscGerada` | ✅ sim (`MscGeradaRegistro` no snapshot) |
| **Finanças** | `20260624000000_EncerramentoExercicio` | ✅ sim (`EncerramentoExercicio` no snapshot) |
| **RH** | `20260623140000_Onda2_Consignacoes` | ✅ sim (`Consignataria` + `ContratoConsignacao` no snapshot) |
| **Identidade** | `20260622200000_Aa5ProfundidadeDelegacao` | ✅ sim (`Delega…` presente no snapshot) |

Todos os demais módulos (Administração, Tributos, Patrimônio, Educação, Protocolo, Assistência,
Transparência, Legislativo, Cofre, PainelGestor, Platform) têm **Designer para 100% das migrations**.

### 1.b — Snapshot ATRÁS do modelo real (o drift de verdade)

Comparando as `DbSet<>` declaradas no DbContext × as entidades presentes no `...ModelSnapshot.cs`:

#### 🔴 SAÚDE — snapshot GRAVEMENTE atrasado (P0 dentro do P0)

- Snapshot (`SaudeDbContextModelSnapshot.cs`) congelado no estado da migration
  `20260623130812_NavegabilidadePacienteBusca` — **12 declarações `Entity(...)`** (≈9 tipos: `Atendimento`
  + owned, `Paciente` + owned, `FundoMunicipalSaude`, `RegraClassificacaoAsps`, `SolicitacaoRegulacao`,
  `LinhaExecucaoSaude`, `ParametroFiscalSaude`, `AuditTrail`, `OutboxMessage`).
- DbContext declara **21 `DbSet<>`**. Estão **AUSENTES do snapshot** as entidades das 4 migrations órfãs
  (Onda 3 de Saúde):
  - Estabelecimento/CNES: `Estabelecimento`, `Profissional` (mig `CadastroEstabelecimentoProfissional`, 3 tabelas)
  - Agenda: `AgendamentoRaiz`, `AgendaProfissional`, `FilaEspera` (mig `Agendamento`, 4 tabelas)
  - Farmácia/Imunização (HÓRUS/SI-PNI): `Medicamento`, `EstoqueMedicamento`, `Dispensacao`,
    `Imunobiologico`, `CarteiraVacinacao` (mig `FarmaciaImunizacao`, 9 tabelas)
  - Vigilância (VISA): `EstabelecimentoFiscalizavel`, `AutoVisa`, `Inspecao`, `LicencaSanitaria`
    (mig `VigilanciaSanitaria`, 5 tabelas)
- **Total: ~21 tabelas que existem no modelo + migrations, mas NÃO no snapshot.** `HasPendingModelChanges()`
  retornaria **true** para Saúde. Próximo `migrations add` recriaria todas → drift garantido.

#### 🟡 FINANÇAS — snapshot em SYNC, só faltam os Designers

- Snapshot tem 45 declarações `Entity(...)` incluindo `EncerramentoExercicio`, `MscGeradaRegistro`,
  `LancamentoContabil`, `EventoContabil`, `ContaContabil`. Ou seja: quem escreveu as 3 migrations órfãs
  **atualizou o snapshot à mão**, mas esqueceu de criar os `.Designer.cs`. Gap = apenas tooling/Designer.
  (Recomenda-se ainda validar via fitness function, porque sync feito à mão é frágil.)

#### 🟡 RH e IDENTIDADE — idem Finanças

- RH: snapshot contém `Consignataria` e `ContratoConsignacao` (a migration órfã `Onda2_Consignacoes`).
- Identidade: snapshot contém as configs de `Delegacao` (a órfã `Aa5ProfundidadeDelegacao`).
- Gap = apenas Designer ausente. Drift de modelo **não** confirmado (mas validar pela fitness function).

> **Resumo do mapa:** o único drift de modelo↔snapshot **confirmado** é **Saúde** (4 migrations não
> refletidas, ~21 tabelas). Os outros 5 órfãos (Finanças×3, RH×1, Identidade×1) são gap de **Designer**
> com snapshot aparentemente já sincronizado. A fitness function do item 3 é o que prova/garante isso.

---

## 2) VIA DE CORREÇÃO dada a restrição do ambiente

### 2.a — `dotnet ef migrations add` funciona aqui? — Avaliação (NÃO executado)

**Provável NÃO, sem ajuste de ambiente.** Evidência:

- `global.json` pina SDK **8.0.400**; o ef tool local é **net8 (8.0.11)**. Para `dotnet ef` rodar ele
  precisa **buildar o projeto de startup e carregá-lo no runtime do design-time**, que segue o SDK do
  `global.json` (net8). Se a máquina só tem **runtime .NET 10** (sem o runtime/SDK 8 que o `global.json`
  exige), o `dotnet ef` falha já na resolução do SDK/host — exatamente o motivo de os agentes escreverem
  migrations à mão.
- **Não executar para "ver se vai"** (build lane ocupado / `:5080`). Tratar como indisponível por padrão.

**Caminho para destravar o ef tool (preferível, fora do hot-path):** instalar o **SDK .NET 8.0.4xx** ao
lado do .NET 10 (side-by-side; o `rollForward: latestMajor` do `global.json` aceita), ou rodar o ef numa
máquina/CI com SDK 8. Se isso for possível **fora** do build lane atual, a via correta é deixar a
ferramenta **regenerar** os Designers e o snapshot de Saúde (one-shot), e nunca mais escrever à mão.

### 2.b — Plano de HAND-FIX (assumindo ef indisponível agora)

**(B1) Os 5 órfãos com snapshot já em sync (Finanças×3, RH×1, Identidade×1): gerar só o `.Designer.cs`.**
Cada Designer é mecânico e tem 2 partes:
1. Atributos: `[DbContext(typeof(XDbContext))]` + `[Migration("<timestampId>")]` sobre a classe parcial.
2. `BuildTargetModel(ModelBuilder)`: **uma cópia do `...ModelSnapshot.cs` no estado VÁLIDO APÓS aquela
   migration.** Como o snapshot já está sincronizado e essas são as últimas migrations de cada cadeia, o
   `BuildTargetModel` do Designer da **última** migration === conteúdo atual do `ModelSnapshot`. Para
   migrations intermediárias, usar o Designer da migration seguinte como referência de estado.
   - Copiar o Designer existente mais recente do módulo como template, trocar o `[Migration(...)]` id e o
     nome da classe parcial (`<NomeDaMigration>`), e ajustar o `BuildTargetModel` para o estado correto.

**(B2) Saúde — o caso real: reconstruir o snapshot + 4 Designers em ordem.**
Aqui o snapshot precisa **avançar** por 4 estados. Sequência (cada passo produz o Designer da migration e
o novo estado acumulado):
1. Estado base = `BuildTargetModel` do Designer de `20260623130812_NavegabilidadePacienteBusca` (último
   Designer existente) === snapshot atual.
2. `+ CadastroEstabelecimentoProfissional` → adicionar configs de `Estabelecimento`, `Profissional`
   (ler `…/Persistence/Configurations/*` correspondentes para nomes de tabela/colunas/índices) → escrever
   `20260623150000_CadastroEstabelecimentoProfissional.Designer.cs` com esse estado.
3. `+ Agendamento` → `AgendamentoRaiz`, `AgendaProfissional`, `FilaEspera` → Designer correspondente.
4. `+ FarmaciaImunizacao` → `Medicamento`, `EstoqueMedicamento`, `Dispensacao`, `Imunobiologico`,
   `CarteiraVacinacao` → Designer correspondente.
5. `+ VigilanciaSanitaria` → `EstabelecimentoFiscalizavel`, `AutoVisa`, `Inspecao`, `LicencaSanitaria` →
   Designer correspondente.
6. **`SaudeDbContextModelSnapshot.cs` final = `BuildTargetModel` do passo 5** (estado completo, 21 DbSets).

   Fonte de verdade para cada bloco: as classes `IEntityTypeConfiguration<>` em
   `…Saude.Infrastructure/Persistence/Configurations/` (carregadas via
   `ApplyConfigurationsFromAssembly` no `SaudeDbContext.OnModelCreating`). O DDL exato (tabelas/colunas/
   índices/FKs) já está nas 4 migrations órfãs — o snapshot deve **espelhar** esse DDL.

> Hand-fix do snapshot é **trabalhoso e propenso a erro** (owned types, índices filtrados, `HasDefaultSchema`,
> sombras de FK). Por isso a recomendação forte é **2.a** (regenerar com ef numa máquina/CI com SDK 8) e
> usar o hand-fix só se 2.a for inviável. Em ambos os casos, **o item 3 é a trava que prova a correção.**

### 2.c — Ordem segura

1. **Primeiro a trava (item 3): fitness function `HasPendingModelChanges`** — ela é o oráculo. Sem
   build/run aqui, mas é o critério objetivo de "consertado": deve ficar **verde** ao fim.
2. **Saúde** (drift real) — reconstruir snapshot + 4 Designers (B2). É o maior risco e o que a fitness pega.
3. **Finanças, RH, Identidade** (B1) — só Designers; rápido e mecânico.
4. **Re-rodar a fitness function** para os 15 contextos → tudo verde = drift fechado.
5. (Quando o SDK 8 estiver disponível) rodar `dotnet ef migrations has-pending-model-changes` por módulo
   como dupla checagem fora do teste.

---

## 3) FITNESS FUNCTION `HasPendingModelChanges` (ArchitectureTests)

**Objetivo:** um teste por `ModuleDbContext` que **falha** se `context.Database.HasPendingModelChanges()`
(EF Core 8) for `true`. Trava que impede o drift modelo↔snapshot voltar. `HasPendingModelChanges()` compara
o **modelo atual** (configs/entidades) com o **último snapshot** — exatamente o invariante violado em Saúde.

**Onde:** `tests/Tensorroot.Gov.ArchitectureTests/` (projeto já existe; já referencia `ApiHost`,
`Microsoft.EntityFrameworkCore.Sqlite`, xUnit, FluentAssertions). Novo arquivo
`PendingModelChangesFitness.cs`.

**Detalhes de construção dos contextos no teste:**
- Cada `XDbContext(DbContextOptions<XDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)`
  herda de `ModuleDbContext`. O ctor aceita `holder = null` (construção direta para migração/teste — já
  previsto no XML doc da base). Precisamos de um `ITenantContext` fake (sem tenant) — basta um stub que
  retorne `HasTenant = false`.
- `HasPendingModelChanges()` **não toca o banco** (compara modelo × snapshot em memória); ainda assim o EF
  exige um provider configurado. Usar **Sqlite in-memory** (`DataSource=:memory:`) é suficiente e barato —
  o provider relacional precisa bater com o usado para gerar as migrations (relacional). Não chamar
  `EnsureCreated`/`Migrate`.

**Esboço (estilo do `FitnessFunctions.cs` existente — PT-BR, sealed, FluentAssertions):**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;   // GetService<IMigrationsAssembly>() se quiser logar
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy; // ITenantContext
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Fitness Function P0: nenhum DbContext de módulo pode ter alteração de modelo PENDENTE em relação
/// ao seu ModelSnapshot. Trava que impede o drift "migrations hand-written sem regenerar snapshot"
/// de voltar — em PROD o schema vem de MigrateAsync (das migrations), então modelo != snapshot = bug.
/// </summary>
public sealed class PendingModelChangesFitness
{
    // Stub de tenant: construção direta (migração/teste), sem tenant -> Global Query Filter no-op.
    private sealed class TenantContextFake : ITenantContext
    {
        public bool HasTenant => false;
        public Guid TenantId => Guid.Empty;
        // demais membros da interface -> valores neutros (ajustar à assinatura real de ITenantContext)
    }

    public static TheoryData<string, Func<DbContext>> Contextos()
    {
        var t = new TenantContextFake();

        DbContextOptions<T> Opts<T>() where T : DbContext =>
            new DbContextOptionsBuilder<T>()
                .UseSqlite("DataSource=:memory:")   // provider relacional; NÃO abre/cria o banco
                .Options;

        return new TheoryData<string, Func<DbContext>>
        {
            { "Saude",            () => new SaudeDbContext(Opts<SaudeDbContext>(), t) },
            { "Financas",         () => new FinancasDbContext(Opts<FinancasDbContext>(), t) },
            { "RecursosHumanos",  () => new RecursosHumanosDbContext(Opts<RecursosHumanosDbContext>(), t) },
            { "Identidade",       () => new IdentidadeDbContext(Opts<IdentidadeDbContext>(), t) },
            { "Tributos",         () => new TributosDbContext(Opts<TributosDbContext>(), t) },
            { "Administracao",    () => new AdministracaoDbContext(Opts<AdministracaoDbContext>(), t) },
            { "Patrimonio",       () => new PatrimonioDbContext(Opts<PatrimonioDbContext>(), t) },
            { "Educacao",         () => new EducacaoDbContext(Opts<EducacaoDbContext>(), t) },
            { "Protocolo",        () => new ProtocoloDbContext(Opts<ProtocoloDbContext>(), t) },
            { "AssistenciaSocial",() => new AssistenciaSocialDbContext(Opts<AssistenciaSocialDbContext>(), t) },
            { "Legislativo",      () => new LegislativoDbContext(Opts<LegislativoDbContext>(), t) },
            { "Transparencia",    () => new TransparenciaDbContext(Opts<TransparenciaDbContext>(), t) },
            { "Cofre",            () => new CofreDbContext(Opts<CofreDbContext>(), t) },
            { "PainelGestor",     () => new PainelGestorDbContext(Opts<PainelGestorDbContext>(), t) },
            { "Cidadao",          () => new CidadaoDbContext(Opts<CidadaoDbContext>(), t) },
            // Platform/AuditoriaRead: DbContext próprio (não ModuleDbContext) — incluir se tiverem migrations.
        };
    }

    [Theory]
    [MemberData(nameof(Contextos))]
    public void Modulo_naoDeveTer_AlteracaoDeModeloPendente(string modulo, Func<DbContext> criar)
    {
        using var ctx = criar();

        // EF Core 8: compara o modelo atual com o ModelSnapshot da Migrations Assembly do contexto.
        var pendente = ctx.Database.HasPendingModelChanges();

        pendente.Should().BeFalse(
            $"o módulo '{modulo}' tem alteração de modelo PENDENTE: o ModelSnapshot está atrás do modelo " +
            "(migration hand-written sem regenerar o snapshot). Regenere o snapshot/Designer e rode " +
            "`dotnet ef migrations add` (ou hand-fix conforme docs/architecture/m9-prep/P0-SNAPSHOTS-PLANO.md).");
    }
}
```

**Notas de robustez do teste:**
- Antes de "consertar", este teste deve **falhar para Saúde** (prova que pega o drift). Se também falhar
  para Finanças/RH/Identidade, significa que o snapshot deles NÃO estava de fato em sync (hand-edit
  incompleto) — útil: a fitness vira a fonte de verdade, não a inspeção manual.
- `HasPendingModelChanges()` exige um `IMigrationsAssembly` resolvível. Como cada Infrastructure tem suas
  migrations no próprio assembly e o ApiHost referencia todos, basta o provider relacional configurado;
  não é preciso `UseSqlServer` (snapshot é provider-agnóstico para essa comparação). Se algum contexto
  fizer config provider-específica em `OnConfiguring`, preferir refletir o provider de PROD (SqlServer)
  via `UseSqlServer("Server=.;Database=_;")` **sem abrir conexão** — ainda não toca o banco.
- Verificar a assinatura real de `ITenantContext` (membros além de `HasTenant`/`TenantId`) ao implementar
  o `TenantContextFake`; pode haver `ITenantUnidadeContext` opcional — o ctor já o trata como nulo.

---

## 4) RISCO DE REGRESSÃO — o que NÃO pode quebrar

A correção mexe em snapshot/Designers; o que **não** pode regredir:

1. **DEV (SQLite) usa `EnsureCreated` + `GenerateCreateScript` do MODELO, não das migrations.**
   `SchemaProvisioner.AplicarAsync(..., ehSqlServer:false, ...)` chama `EnsureCreatedAsync()` e depois
   `GenerateCreateScript()` (tornado idempotente com `IF NOT EXISTS`). Isso deriva do **modelo em
   runtime**, então **independe do snapshot**. ⇒ Corrigir o snapshot **não muda o comportamento de DEV**.
   Risco real seria o oposto: alguém "consertar" o snapshot mexendo nas **configs/entidades** — aí mudaria
   o DDL gerado em DEV. **Regra:** o hand-fix do snapshot é cópia-do-modelo; **não tocar** em
   `Configurations/*`, `OnModelCreating`, entidades ou DbSets.

2. **PROD (SqlServer) usa `MigrateAsync` das migrations** (`SchemaProvisioner`, ramo `ehSqlServer:true`).
   As 4 migrations órfãs de Saúde **já têm o `.cs` com o DDL** — PROD aplica esse DDL hoje. **Adicionar os
   Designers e atualizar o snapshot NÃO altera o DDL aplicado** (Designer/snapshot são metadados de
   tooling). ⇒ Banco PROD existente continua migrando igual; nenhuma migration nova é introduzida pelo
   hand-fix. **Não** renomear/reordenar/alterar `Up`/`Down` das migrations existentes (mudaria o histórico
   já aplicável e o `__EFMigrationsHistory`).

3. **`__EFMigrationsHistory` e ordenação por timestamp.** Os Designers carregam `[Migration("<id>")]`. O id
   tem de bater **exatamente** com o nome do arquivo/`__EFMigrationsHistory` (ex.: `20260623170000_FarmaciaImunizacao`).
   Errar o id quebra a aplicação ordenada. Conferir 1:1 com o nome do `.cs`.

4. **`Platform` e `AuditoriaReadDbContext` são `DbContext` puros (não `ModuleDbContext`).** Platform tem
   migrations próprias e Designers OK; `AuditoriaReadDbContext` é read-model (pode não ter migrations).
   Incluir Platform na fitness; só incluir AuditoriaRead se tiver migrations. Não aplicar regra de tenant a
   eles.

5. **WORM/AuditTrail e Outbox (colunas de resiliência).** Em PROD vêm de migration; em DEV o
   `SchemaProvisioner` injeta trigger WORM e colunas `AttemptCount/NextAttemptUtc/DeadLetteredOnUtc` de
   forma idempotente. O hand-fix do snapshot **não** pode remover essas configs do modelo (senão o
   `GenerateCreateScript` de DEV pararia de criá-las). Garantir que `OutboxMessage`/`AuditTrail`
   permaneçam no snapshot final de cada módulo.

6. **A fitness function não pode tocar o banco / não derrubar a :5080.** Usa provider in-memory/sem
   conexão e só `HasPendingModelChanges()` (comparação em memória). Não chama `Migrate`/`EnsureCreated`.

---

## Resumo executivo

- **Gaps (módulo → migration/snapshot):**
  - Designer ausente (9 migrations): **Saúde** ×4 (`CadastroEstabelecimentoProfissional`, `Agendamento`,
    `FarmaciaImunizacao`, `VigilanciaSanitaria`), **Finanças** ×3 (`Contabilidade`, `MscGerada`,
    `EncerramentoExercicio`), **RH** ×1 (`Onda2_Consignacoes`), **Identidade** ×1 (`Aa5ProfundidadeDelegacao`).
  - **Snapshot atrás do modelo (drift real): só SAÚDE** — snapshot congelado em
    `20260623130812_NavegabilidadePacienteBusca`; faltam ~21 tabelas (Estabelecimento/CNES, Agenda,
    Farmácia/Imunização, Vigilância). Finanças/RH/Identidade têm snapshot aparentemente em sync (validar
    pela fitness).
- **Via escolhida:** preferencial = **regenerar com `dotnet ef` em ambiente com SDK 8 side-by-side/CI**
  (o ef tool é net8 e o `global.json` pina SDK 8; provável que não rode só com runtime .NET 10).
  Fallback = **hand-fix** dos 5 Designers triviais + **reconstrução do snapshot de Saúde em 4 passos**
  (espelhando o DDL das 4 migrations órfãs, sem tocar em entidades/configs).
- **Trava (fitness function):** `[Theory]` por `ModuleDbContext` que falha se
  `Database.HasPendingModelChanges()` for `true`; oráculo objetivo do conserto (deve ficar verde).
- **Ordem segura:** (1) escrever a fitness; (2) Saúde (snapshot + 4 Designers); (3) Finanças/RH/Identidade
  (só Designers); (4) fitness verde nos 15 contextos; (5) dupla checagem com `ef … has-pending-model-changes`
  quando o SDK 8 estiver disponível.
- **Não regride:** DEV (`EnsureCreated`/`GenerateCreateScript` do modelo) é imune ao snapshot; PROD
  (`MigrateAsync` do DDL já existente) é imune ao Designer/snapshot — desde que **não** se mexa em
  entidades/configs/`Up`/`Down`/ids de migration nem se remova Outbox/AuditTrail do modelo.
