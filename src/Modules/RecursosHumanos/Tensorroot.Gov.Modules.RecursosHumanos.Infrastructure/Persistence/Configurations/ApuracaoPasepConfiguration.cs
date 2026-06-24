using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ApuracaoPasep"/>.</summary>
public sealed class ApuracaoPasepConfiguration : IEntityTypeConfiguration<ApuracaoPasep>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApuracaoPasep> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ApuracoesPasep");
        builder.HasKey(apuracao => apuracao.Id);
        builder.Property(apuracao => apuracao.Id)
            .HasConversion(id => id.Value, value => new ApuracaoPasepId(value))
            .ValueGeneratedNever();

        // Competencia persistida como inteiro (Ano*100 + Mes), padrao do modulo (mesma do EventoESocial/Folha).
        builder.Property(apuracao => apuracao.Competencia)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Competencia.De(valor / 100, valor % 100))
            .HasColumnName("Competencia");

        builder.Property(apuracao => apuracao.BaseContribuicao).HasColumnType("decimal(18,2)");
        builder.Property(apuracao => apuracao.Aliquota).HasColumnType("decimal(7,4)");
        builder.Property(apuracao => apuracao.Valor).HasColumnType("decimal(18,2)");
        builder.Property(apuracao => apuracao.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(apuracao => new { apuracao.TenantId, apuracao.Competencia }).IsUnique();
    }
}
