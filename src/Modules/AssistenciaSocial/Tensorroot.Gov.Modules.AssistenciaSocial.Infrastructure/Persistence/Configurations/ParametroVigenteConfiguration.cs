using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do parametro versionado por vigencia <see cref="ParametroVigente"/>.</summary>
public sealed class ParametroVigenteConfiguration : IEntityTypeConfiguration<ParametroVigente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParametroVigente> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ParametrosVigentes");
        builder.HasKey(parametro => parametro.Id);
        builder.Property(parametro => parametro.Id).ValueGeneratedNever();

        builder.Property(parametro => parametro.Chave).HasMaxLength(60);
        builder.Property(parametro => parametro.VigenciaInicio);
        builder.Property(parametro => parametro.Valor).HasColumnType("decimal(18,2)");

        builder.HasIndex(parametro => new { parametro.TenantId, parametro.Chave, parametro.VigenciaInicio });
    }
}
