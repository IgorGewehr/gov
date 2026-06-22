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
                var tenantProperty = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
                var currentTenant = Expression.Property(contextoConstante, nameof(ModuleDbContext.CurrentTenantId));
                body = Expression.Equal(tenantProperty, currentTenant);
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

    /// <summary>Configura o mapeamento das entidades de infraestrutura (Outbox e Audit Trail).</summary>
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
            // Cadeia POR TENANT em ordem de gravação: acelera o "último selo do tenant" (interceptor)
            // e a leitura sequencial do verificador. NÃO é único — linhas LEGADAS (anteriores à
            // cadeia) compartilham Sequencia=0; a unicidade da posição é garantida pelo próprio
            // verificador (lacuna/duplicata = adulteração), e em produção pelo trigger WORM.
            builder.HasIndex(trail => new { trail.TenantId, trail.Sequencia });
        });
    }
}
