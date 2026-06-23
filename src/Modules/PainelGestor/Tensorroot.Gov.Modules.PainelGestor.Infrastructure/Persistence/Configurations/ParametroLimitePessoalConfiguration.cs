using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do parâmetro versionado <see cref="ParametroLimitePessoal"/> (limites LRF).</summary>
public sealed class ParametroLimitePessoalConfiguration : IEntityTypeConfiguration<ParametroLimitePessoal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParametroLimitePessoal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParametrosLimitePessoal");
        builder.HasKey(parametro => parametro.Id);
        builder.Property(parametro => parametro.Id).ValueGeneratedNever();
        builder.Property(parametro => parametro.VigenciaInicio);
        builder.Property(parametro => parametro.LimiteLegal).HasPrecision(9, 6);
        builder.Property(parametro => parametro.FatorPrudencial).HasPrecision(9, 6);
        builder.Property(parametro => parametro.FatorAlerta).HasPrecision(9, 6);

        builder.HasIndex(parametro => new { parametro.TenantId, parametro.VigenciaInicio });
    }
}
