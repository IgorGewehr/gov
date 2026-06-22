using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TabelaAliquotaIss"/> e seus itens.</summary>
public sealed class TabelaAliquotaIssConfiguration : IEntityTypeConfiguration<TabelaAliquotaIss>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaAliquotaIss> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasAliquotaIss");
        builder.HasKey(tabela => tabela.Id);
        builder.Property(tabela => tabela.Id)
            .HasConversion(id => id.Value, value => new TabelaAliquotaIssId(value))
            .ValueGeneratedNever();

        builder.Property(tabela => tabela.VigenciaInicioAaaaMm);
        builder.Property(tabela => tabela.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(tabela => tabela.Vigente);

        builder.HasMany(tabela => tabela.Itens).WithOne().HasForeignKey(i => i.TabelaAliquotaIssId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(tabela => tabela.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(tabela => new { tabela.TenantId, tabela.VigenciaInicioAaaaMm });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="ItemAliquotaIss"/>.</summary>
public sealed class ItemAliquotaIssConfiguration : IEntityTypeConfiguration<ItemAliquotaIss>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ItemAliquotaIss> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ItensAliquotaIss");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemAliquotaIssId(value))
            .ValueGeneratedNever();

        builder.Property(item => item.TabelaAliquotaIssId)
            .HasConversion(id => id.Value, value => new TabelaAliquotaIssId(value));

        builder.Property(item => item.ItemListaServico).HasMaxLength(10).IsRequired();
        builder.Property(item => item.AliquotaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(item => item.RetencaoObrigatoria);
        builder.Property(item => item.SubstituicaoTributaria);

        builder.HasIndex(item => item.TabelaAliquotaIssId);
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="ApuracaoIss"/> (livro eletrônico) e suas linhas.</summary>
public sealed class ApuracaoIssConfiguration : IEntityTypeConfiguration<ApuracaoIss>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApuracaoIss> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ApuracoesIss");
        builder.HasKey(apuracao => apuracao.Id);
        builder.Property(apuracao => apuracao.Id)
            .HasConversion(id => id.Value, value => new ApuracaoIssId(value))
            .ValueGeneratedNever();

        builder.Property(apuracao => apuracao.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(apuracao => apuracao.Competencia)
            .HasConversion(c => (c.Ano * 100) + c.Mes, valor => Competencia.De(valor / 100, valor % 100));

        builder.Property(apuracao => apuracao.IssProprio)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(apuracao => apuracao.IssRetido)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(apuracao => apuracao.IssSubstituicao)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasMany(apuracao => apuracao.Itens).WithOne().HasForeignKey(i => i.ApuracaoIssId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(apuracao => apuracao.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(apuracao => new { apuracao.TenantId, apuracao.ContribuinteId, apuracao.Competencia }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="ItemApuracaoIss"/> (linha do livro).</summary>
public sealed class ItemApuracaoIssConfiguration : IEntityTypeConfiguration<ItemApuracaoIss>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ItemApuracaoIss> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ItensApuracaoIss");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemApuracaoIssId(value))
            .ValueGeneratedNever();

        builder.Property(item => item.ApuracaoIssId)
            .HasConversion(id => id.Value, value => new ApuracaoIssId(value));

        builder.Property(item => item.ChaveAcesso).HasMaxLength(60).IsRequired();
        builder.Property(item => item.ItemListaServico).HasMaxLength(10).IsRequired();
        builder.Property(item => item.BaseCalculo)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(item => item.AliquotaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(item => item.Modalidade).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.IssApurado)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(item => item.ApuracaoIssId);
    }
}
