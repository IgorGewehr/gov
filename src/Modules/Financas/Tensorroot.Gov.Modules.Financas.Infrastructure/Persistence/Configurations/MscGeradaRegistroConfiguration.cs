using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do registro de controle <see cref="MscGeradaRegistro"/> (idempotência da geração
/// da MSC por competência). Índice único por <c>(TenantId, Exercicio, Mes, TipoMatriz)</c>.
/// </summary>
public sealed class MscGeradaRegistroConfiguration : IEntityTypeConfiguration<MscGeradaRegistro>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MscGeradaRegistro> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("msc_gerada");
        builder.HasKey(registro => registro.Id);

        builder.Property(registro => registro.Exercicio).IsRequired();
        builder.Property(registro => registro.Mes).IsRequired();
        builder.Property(registro => registro.TipoMatriz).IsRequired();
        builder.Property(registro => registro.EventId).IsRequired();
        builder.Property(registro => registro.QuantidadeLinhas).IsRequired();
        builder.Property(registro => registro.GeradaEmUtc).IsRequired();

        builder.HasIndex(registro => new
        {
            registro.TenantId,
            registro.Exercicio,
            registro.Mes,
            registro.TipoMatriz,
        }).IsUnique();
    }
}
