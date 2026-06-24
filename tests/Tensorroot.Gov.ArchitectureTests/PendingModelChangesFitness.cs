using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions; // ITenantContext
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.Platform.Persistence;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Fitness Function P0: nenhum DbContext de módulo pode ter alteração de modelo PENDENTE em relação
/// ao seu ModelSnapshot. Trava que impede o drift "migrations hand-written sem regenerar snapshot"
/// de voltar — em PROD o schema vem de MigrateAsync (das migrations), então modelo != snapshot = bug.
/// </summary>
public sealed class PendingModelChangesFitness
{
    /// <summary>
    /// Stub de tenant para CONSTRUÇÃO DIRETA (migração/teste), sem tenant resolvido. A assinatura real
    /// de <see cref="ITenantContext"/> tem somente <c>HasTenant</c> e <c>TenantId</c> — sem tenant, o
    /// Global Query Filter degenera para no-op/deny e NÃO toca o banco. O ctor do <c>ModuleDbContext</c>
    /// trata <c>holder</c> e <c>unidadeContext</c> como nulos (já previsto no XML doc da base).
    /// </summary>
    private sealed class TenantContextFake : ITenantContext
    {
        public bool HasTenant => false;

        public Guid TenantId => Guid.Empty;
    }

    public static TheoryData<string, Func<DbContext>> Contextos()
    {
        var t = new TenantContextFake();

        // Provider relacional = SqlServer, IGUAL ao usado para gerar as migrations/snapshot (PROD).
        // HasPendingModelChanges() compara modelo × ModelSnapshot EM MEMÓRIA — a connection string é
        // de design-time e NÃO abre/cria o banco. Usar SqlServer (e não Sqlite) é obrigatório: o
        // ModelSnapshot carimba tipos de coluna específicos do provedor (ex.: nvarchar(max), uniqueidentifier,
        // datetimeoffset, índice filtrado). Comparar contra um modelo materializado por Sqlite produziria
        // FALSO-POSITIVO de "pendência" em TODOS os contextos (tipos divergentes) — exatamente o que o
        // oráculo `dotnet ef migrations has-pending-model-changes` (que usa as design-time factories
        // SqlServer) NÃO acusa. Espelhar o provedor de PROD alinha a fitness ao oráculo do EF.
        static DbContextOptions<T> Opts<T>()
            where T : DbContext =>
            new DbContextOptionsBuilder<T>()
                .UseSqlServer("Server=_;Database=_;Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        return new TheoryData<string, Func<DbContext>>
        {
            { "Saude", () => new SaudeDbContext(Opts<SaudeDbContext>(), t) },
            { "Financas", () => new FinancasDbContext(Opts<FinancasDbContext>(), t) },
            { "RecursosHumanos", () => new RecursosHumanosDbContext(Opts<RecursosHumanosDbContext>(), t) },
            { "Identidade", () => new IdentidadeDbContext(Opts<IdentidadeDbContext>(), t) },
            { "Tributos", () => new TributosDbContext(Opts<TributosDbContext>(), t) },
            { "Administracao", () => new AdministracaoDbContext(Opts<AdministracaoDbContext>(), t) },
            { "Patrimonio", () => new PatrimonioDbContext(Opts<PatrimonioDbContext>(), t) },
            { "Educacao", () => new EducacaoDbContext(Opts<EducacaoDbContext>(), t) },
            { "Protocolo", () => new ProtocoloDbContext(Opts<ProtocoloDbContext>(), t) },
            { "AssistenciaSocial", () => new AssistenciaSocialDbContext(Opts<AssistenciaSocialDbContext>(), t) },
            { "Legislativo", () => new LegislativoDbContext(Opts<LegislativoDbContext>(), t) },
            { "Transparencia", () => new TransparenciaDbContext(Opts<TransparenciaDbContext>(), t) },
            { "Cofre", () => new CofreDbContext(Opts<CofreDbContext>(), t) },
            { "PainelGestor", () => new PainelGestorDbContext(Opts<PainelGestorDbContext>(), t) },
            { "Cidadao", () => new CidadaoDbContext(Opts<CidadaoDbContext>(), t) },
            // Platform: DbContext próprio (não ModuleDbContext, sem tenant), mas TEM migrations + snapshot.
            { "Platform", () => new PlatformDbContext(Opts<PlatformDbContext>()) },
            // AuditoriaReadDbContext é read-model SEM migrations/snapshot → fora do escopo do drift.
        };
    }

    [Theory]
    [MemberData(nameof(Contextos))]
    public void Modulo_naoDeveTer_AlteracaoDeModeloPendente(string modulo, Func<DbContext> criar)
    {
        using var ctx = criar();

        // EF Core 8: compara o modelo atual com o ModelSnapshot da Migrations Assembly do contexto.
        // Comparação em memória — NÃO chama EnsureCreated/Migrate, não toca o banco.
        var pendente = ctx.Database.HasPendingModelChanges();

        pendente.Should().BeFalse(
            $"o módulo '{modulo}' tem alteração de modelo PENDENTE: o ModelSnapshot está atrás do modelo " +
            "(migration hand-written sem regenerar o snapshot). Regenere o snapshot/Designer e rode " +
            "`dotnet ef migrations add` (ou hand-fix conforme docs/architecture/m9-prep/P0-SNAPSHOTS-PLANO.md).");
    }
}
