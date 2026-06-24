using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DestinacaoProcesso"/> (ficha de destinacao).</summary>
public sealed class DestinacaoProcessoConfiguration : IEntityTypeConfiguration<DestinacaoProcesso>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DestinacaoProcesso> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DestinacoesProcesso");
        builder.HasKey(destinacao => destinacao.Id);
        builder.Property(destinacao => destinacao.Id)
            .HasConversion(id => id.Value, value => new DestinacaoProcessoId(value))
            .ValueGeneratedNever();

        builder.Property(destinacao => destinacao.CodigoClassificacao).HasMaxLength(ClasseDocumental.ComprimentoMaximoCodigo).IsRequired();
        builder.Property(destinacao => destinacao.DestinacaoFinal).HasConversion<string>().HasMaxLength(20);
        builder.Property(destinacao => destinacao.Estado).HasConversion<string>().HasMaxLength(30);
        builder.Property(destinacao => destinacao.TermoEliminacaoHash).HasMaxLength(Hash.ComprimentoSha256);
        builder.Property(destinacao => destinacao.EditalEliminacaoRef).HasMaxLength(200);

        // Uma ficha por processo; busca por processo e por estado (varredura de aptidao).
        builder.HasIndex(destinacao => new { destinacao.TenantId, destinacao.ProcessoId }).IsUnique();
        builder.HasIndex(destinacao => new { destinacao.TenantId, destinacao.Estado });
    }
}
