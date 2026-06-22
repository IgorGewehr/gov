using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Liquidacao"/>.</summary>
public sealed class LiquidacaoConfiguration : IEntityTypeConfiguration<Liquidacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Liquidacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Liquidacoes");
        builder.HasKey(liquidacao => liquidacao.Id);
        builder.Property(liquidacao => liquidacao.Id)
            .HasConversion(id => id.Value, value => new LiquidacaoId(value))
            .ValueGeneratedNever();

        builder.Property(liquidacao => liquidacao.EmpenhoId)
            .HasConversion(id => id.Value, value => new EmpenhoId(value));

        builder.Property(liquidacao => liquidacao.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(liquidacao => liquidacao.ValorPago)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(liquidacao => liquidacao.DataLiquidacao).HasColumnType("date");
        builder.Property(liquidacao => liquidacao.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(liquidacao => liquidacao.Documento, documento =>
        {
            documento.Property(d => d.Tipo).HasColumnName("DocumentoTipo").HasConversion<string>().HasMaxLength(20).IsRequired();
            documento.Property(d => d.Numero).HasColumnName("DocumentoNumero").HasMaxLength(60);
            documento.Property(d => d.ChaveAcessoNfse).HasColumnName("DocumentoChaveNfse").HasMaxLength(44);
            documento.Property(d => d.DataEmissao).HasColumnName("DocumentoDataEmissao").HasColumnType("date");
        });
        builder.Navigation(liquidacao => liquidacao.Documento).IsRequired();

        // Derivado não persistido.
        builder.Ignore(liquidacao => liquidacao.SaldoAPagar);

        builder.HasIndex(liquidacao => liquidacao.EmpenhoId);
        builder.HasIndex(liquidacao => new { liquidacao.TenantId, liquidacao.EmpenhoId });
    }
}
