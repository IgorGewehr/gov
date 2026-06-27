using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ProcessoTrabalhista"/>.</summary>
public sealed class ProcessoTrabalhistaConfiguration : IEntityTypeConfiguration<ProcessoTrabalhista>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProcessoTrabalhista> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProcessosTrabalhistas");
        builder.HasKey(processo => processo.Id);
        builder.Property(processo => processo.Id)
            .HasConversion(id => id.Value, value => new ProcessoTrabalhistaId(value))
            .ValueGeneratedNever();

        builder.Property(processo => processo.NumeroProcesso).HasMaxLength(ProcessoTrabalhista.ComprimentoMaximoNumero);
        builder.Property(processo => processo.Vara).HasMaxLength(200);
        builder.Property(processo => processo.Reclamante).HasMaxLength(200);
        builder.Property(processo => processo.Objeto).HasMaxLength(ProcessoTrabalhista.ComprimentoMaximoObjeto);

        builder.Property(processo => processo.ServidorId)
            .HasConversion(
                servidor => servidor!.Value.Value,
                valor => new ServidorId(valor));

        builder.Property(processo => processo.ValorCausa).HasColumnType("decimal(18,2)");
        builder.Property(processo => processo.ValorAcordo).HasColumnType("decimal(18,2)");
        builder.Property(processo => processo.ValorCondenacao).HasColumnType("decimal(18,2)");
        builder.Property(processo => processo.ValorProvisionado).HasColumnType("decimal(18,2)");
        builder.Property(processo => processo.DataAjuizamento);
        builder.Property(processo => processo.DataEncerramento);
        builder.Property(processo => processo.Prognostico).HasConversion<string>().HasMaxLength(20);
        builder.Property(processo => processo.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(processo => new { processo.TenantId, processo.NumeroProcesso }).IsUnique();
        builder.HasIndex(processo => new { processo.TenantId, processo.Situacao, processo.Prognostico });
    }
}
