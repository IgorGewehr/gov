using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
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

        // Derivados não persistidos.
        builder.Ignore(liquidacao => liquidacao.SaldoAPagar);
        builder.Ignore(liquidacao => liquidacao.TotalRetido);
        builder.Ignore(liquidacao => liquidacao.ValorLiquido);

        // Retenções/consignações (IRRF, INSS, ISS, caução) — entidades-filhas da liquidação.
        builder.OwnsMany(liquidacao => liquidacao.Retencoes, retencoes =>
        {
            retencoes.ToTable("Retencoes");
            retencoes.WithOwner().HasForeignKey("LiquidacaoId");
            retencoes.HasKey(retencao => retencao.Id);
            retencoes.Property(retencao => retencao.Id)
                .HasConversion(id => id.Value, value => new RetencaoId(value))
                .ValueGeneratedNever();
            retencoes.Property(retencao => retencao.Natureza).HasConversion<string>().HasMaxLength(30);
            retencoes.Property(retencao => retencao.Valor)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
            retencoes.Property(retencao => retencao.BaseCalculo)
                .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
                .HasColumnType("decimal(18,2)");
            retencoes.Property(retencao => retencao.CodigoReceita).HasMaxLength(20);
            retencoes.Property(retencao => retencao.Aliquota).HasColumnType("decimal(9,6)");
            retencoes.Property(retencao => retencao.FavorecidoDocumento).HasMaxLength(20);
            retencoes.Property(retencao => retencao.Descricao).HasMaxLength(200).IsRequired();
            retencoes.Property(retencao => retencao.Recolhida);
            retencoes.Property(retencao => retencao.GuiaRecolhimentoId);
            retencoes.HasIndex("GuiaRecolhimentoId");
        });
        builder.Navigation(liquidacao => liquidacao.Retencoes).Metadata.SetField("_retencoes");
        builder.Navigation(liquidacao => liquidacao.Retencoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(liquidacao => liquidacao.EmpenhoId);
        builder.HasIndex(liquidacao => new { liquidacao.TenantId, liquidacao.EmpenhoId });
    }
}
