using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Lookups;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do read model <see cref="UnidadeAtendimentoLookup"/>.</summary>
public sealed class UnidadeAtendimentoLookupConfiguration : IEntityTypeConfiguration<UnidadeAtendimentoLookup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnidadeAtendimentoLookup> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UnidadesAtendimento");
        builder.HasKey(unidade => unidade.Id);
        builder.Property(unidade => unidade.Id).ValueGeneratedNever();

        builder.Property(unidade => unidade.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(unidade => unidade.TerritorioCobertura).HasMaxLength(120);

        builder.HasIndex(unidade => new { unidade.TenantId, unidade.TerritorioCobertura });
    }
}
