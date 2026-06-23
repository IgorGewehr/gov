using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DotacaoOrcamentaria"/>.</summary>
public sealed class DotacaoOrcamentariaConfiguration : IEntityTypeConfiguration<DotacaoOrcamentaria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DotacaoOrcamentaria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Dotacoes");
        builder.HasKey(dotacao => dotacao.Id);
        builder.Property(dotacao => dotacao.Id)
            .HasConversion(id => id.Value, value => new DotacaoOrcamentariaId(value))
            .ValueGeneratedNever();

        builder.OwnsOne(dotacao => dotacao.Classificacao, classificacao =>
        {
            classificacao.Property(c => c.Orgao).HasColumnName("Orgao").HasMaxLength(10).IsRequired();
            classificacao.Property(c => c.UnidadeOrcamentaria).HasColumnName("UnidadeOrcamentaria").HasMaxLength(20).IsRequired();
            classificacao.Property(c => c.FuncionalProgramatica).HasColumnName("FuncionalProgramatica").HasMaxLength(50).IsRequired();
            classificacao.Property(c => c.CategoriaEconomica).HasColumnName("CategoriaEconomica").HasConversion<string>().HasMaxLength(30).IsRequired();
            classificacao.Property(c => c.FonteDeRecurso).HasColumnName("FonteDeRecurso").HasMaxLength(20).IsRequired();
        });
        builder.Navigation(dotacao => dotacao.Classificacao).IsRequired();

        builder.Property(dotacao => dotacao.ValorDotadoInicial)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(dotacao => dotacao.ValorReforcado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(dotacao => dotacao.ValorAnulado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(dotacao => dotacao.ValorEmpenhadoLiquido)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(dotacao => dotacao.Situacao).HasConversion<string>().HasMaxLength(20);

        // Origem rastreavel quando a dotacao NASCE da LOA (aditivo/retrocompativel — nullable).
        builder.Property(dotacao => dotacao.LoaId);
        builder.Property(dotacao => dotacao.ItemDespesaFixadaId);
        builder.Property(dotacao => dotacao.AcaoPpaId);

        // Derivados não persistidos.
        builder.Ignore(dotacao => dotacao.ValorAtualizado);
        builder.Ignore(dotacao => dotacao.SaldoDisponivel);

        builder.HasIndex(dotacao => new { dotacao.TenantId, dotacao.Exercicio });
    }
}
