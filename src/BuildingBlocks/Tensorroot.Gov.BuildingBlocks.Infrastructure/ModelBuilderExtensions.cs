using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure;

/// <summary>Extensões de <see cref="ModelBuilder"/> para multi-tenancy e infraestrutura comum.</summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Aplica os Global Query Filters reavaliados por consulta a partir do contexto:
    /// <list type="bullet">
    /// <item>filtro de TENANT por <c>TenantId</c> em toda entidade <see cref="IMustHaveTenant"/>;</item>
    /// <item>filtro de UNIDADE ORGANIZACIONAL (MODELO §5) por <c>UnidadeId</c> em toda entidade
    /// <see cref="IMustHaveUnidade"/>, restringindo leituras ao escopo efetivo do sujeito.</item>
    /// </list>
    /// Como o EF Core admite apenas UM filtro por tipo, os predicados aplicáveis são COMBINADOS por
    /// <c>AND</c>. Entidades sem <see cref="IMustHaveUnidade"/> não recebem o filtro de UO (não
    /// quebra os módulos atuais); o filtro de UO é DESLIGADO quando o contexto não exige filtragem
    /// (jobs/sistema), exatamente como o override de tenant.
    /// </summary>
    /// <param name="modelBuilder">Construtor do modelo.</param>
    /// <param name="context">Contexto que expõe o tenant e o escopo de UO atuais.</param>
    public static void ApplyTenantAndUnidadeQueryFilters(this ModelBuilder modelBuilder, ModuleDbContext context)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(context);

        var contextoConstante = Expression.Constant(context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var ehTenant = typeof(IMustHaveTenant).IsAssignableFrom(clrType);
            var ehUnidade = typeof(IMustHaveUnidade).IsAssignableFrom(clrType);
            if (!ehTenant && !ehUnidade)
            {
                continue;
            }

            var parameter = Expression.Parameter(clrType, "e");
            Expression? body = null;

            if (ehTenant)
            {
                // XT-2 / deny-by-default (CLAUDE.md §5). Três casos, reavaliados por consulta:
                //   context.ContextoDeSistema      ? true                                  (DDL/migração/seed: passe-livre)
                //   : context.NegarPorFaltaDeTenant ? false                                 (sem tenant: 1=0 — NUNCA Guid.Empty)
                //   :                                 (e.TenantId == context.CurrentTenantId) (isolamento normal por tenant)
                // "Sem tenant" NUNCA vira "tenant zero" (que devolveria linhas órfãs); o contexto de
                // sistema é o ÚNICO caminho de plataforma/bootstrap que escapa do filtro.
                var tenantProperty = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
                var currentTenant = Expression.Property(contextoConstante, nameof(ModuleDbContext.CurrentTenantId));
                var igualdade = Expression.Equal(tenantProperty, currentTenant);

                var negar = Expression.Property(contextoConstante, nameof(ModuleDbContext.NegarPorFaltaDeTenant));
                var sistema = Expression.Property(contextoConstante, nameof(ModuleDbContext.ContextoDeSistema));

                var negarOuFiltrar = Expression.Condition(negar, Expression.Constant(false), igualdade);
                body = Expression.Condition(sistema, Expression.Constant(true), negarOuFiltrar);
            }

            if (ehUnidade)
            {
                // (!context.FiltrarPorUnidade) || context.UnidadesPermitidas.Contains(e.UnidadeId)
                var filtrar = Expression.Property(contextoConstante, nameof(ModuleDbContext.FiltrarPorUnidade));
                var naoFiltrar = Expression.Not(filtrar);

                var permitidas = Expression.Property(contextoConstante, nameof(ModuleDbContext.UnidadesPermitidas));
                var unidadeProperty = Expression.Property(parameter, nameof(IMustHaveUnidade.UnidadeId));
                var contains = Expression.Call(ContainsMethod, permitidas, unidadeProperty);

                var unidadePredicado = Expression.OrElse(naoFiltrar, contains);
                body = body is null ? unidadePredicado : Expression.AndAlso(body, unidadePredicado);
            }

            entityType.SetQueryFilter(Expression.Lambda(body!, parameter));
        }
    }

    // Enumerable.Contains<Guid>(IEnumerable<Guid>, Guid) — traduzível pelo EF para IN(...).
    private static readonly System.Reflection.MethodInfo ContainsMethod = typeof(Enumerable)
        .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Single(method => method.Name == nameof(Enumerable.Contains) && method.GetParameters().Length == 2)
        .MakeGenericMethod(typeof(Guid));

    /// <summary>Configura o mapeamento das entidades de infraestrutura (Outbox, Inbox e Audit Trail).</summary>
    /// <param name="modelBuilder">Construtor do modelo.</param>
    public static void ApplyOutboxAndAuditMappings(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(message => message.Id);
            builder.Property(message => message.Type).HasMaxLength(500);
            // Índice de drenagem: filtra mensagens elegíveis (não processadas, não dead-letter, com
            // NextAttemptUtc vencido) e ordena por OccurredOnUtc sem varrer a tabela inteira.
            builder.HasIndex(message => new { message.ProcessedOnUtc, message.DeadLetteredOnUtc, message.NextAttemptUtc });
        });

        // INBOX (W9.7) — deduplicação de CONSUMO system-wide. Mapeado em TODO ModuleDbContext (como o
        // Outbox), no schema do módulo: cada consumidor sela aqui o que já processou, no banco do tenant.
        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("InboxMessages");
            builder.HasKey(message => message.Id);
            builder.Property(message => message.Handler).HasMaxLength(500);
            builder.Property(message => message.EventType).HasMaxLength(500);
            // Chave de idempotência (EventId, Handler, TenantId): UNIQUE → a 2ª entrega do mesmo evento ao
            // mesmo handler/tenant viola a unicidade (detectável) em vez de aplicar o efeito de novo. O
            // TenantId entra na chave porque o banco é compartilhado entre módulos no SQLite de dev e, em
            // produção, mantém a semântica explícita da trinca mesmo no banco dedicado.
            builder
                .HasIndex(message => new { message.EventId, message.Handler, message.TenantId })
                .IsUnique();
        });

        modelBuilder.Entity<AuditTrail>(builder =>
        {
            builder.ToTable("AuditTrail");
            builder.HasKey(trail => trail.Id);
            builder.Property(trail => trail.EntityName).HasMaxLength(256);
            builder.Property(trail => trail.Action).HasMaxLength(20);
            // Selos da cadeia de hash: SHA-256 em Base64 cabe em 44 chars; folga para o genesis.
            builder.Property(trail => trail.HashAnterior).HasMaxLength(64);
            builder.Property(trail => trail.HashAtual).HasMaxLength(64);
            builder.HasIndex(trail => new { trail.TenantId, trail.TimestampUtc });
            // P0-1 (robustez da fundação): índice ÚNICO FILTRADO em (TenantId, Sequencia) para
            // Sequencia > 0. Cadeia POR TENANT em ordem de gravação — acelera o "último selo do
            // tenant" (interceptor) e a leitura sequencial do verificador. Sob CONCORRÊNCIA do mesmo
            // tenant (lote de empenhos, importação de folha, NfseSync), duas requisições podiam ler o
            // mesmo "último selo = N" e ambas gravar Sequencia=N+1 → cadeia bifurcava e o verificador
            // do TCE reportava FALSA "adulteração". Com a unicidade, a colisão vira uma
            // DbUpdateException de chave duplicada (detectável + re-tentável) em vez de corrupção
            // silenciosa. O FILTRO Sequencia > 0 preserva as linhas LEGADAS (anteriores à cadeia), que
            // compartilham Sequencia=0 e não devem colidir entre si. Filtro honrado por SqlServer
            // (índice filtrado) e SQLite (índice parcial); demais provedores degradam para índice
            // único pleno — aceitável pois não há legado com Sequencia=0 fora de produção.
            builder
                .HasIndex(trail => new { trail.TenantId, trail.Sequencia })
                .IsUnique()
                .HasFilter("[Sequencia] > 0");
        });
    }
}
