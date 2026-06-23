using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations.Planejamento;

/// <summary>Mapeamento EF Core do agregado <see cref="LeiOrcamentariaAnual"/> (LOA + QDD).</summary>
public sealed class LoaConfiguration : IEntityTypeConfiguration<LeiOrcamentariaAnual>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeiOrcamentariaAnual> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LeisOrcamentarias");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => new LoaId(value))
            .ValueGeneratedNever();
        builder.Property(l => l.LdoId).HasConversion(id => id.Value, value => new LdoId(value));
        builder.Property(l => l.PpaId).HasConversion(id => id.Value, value => new PpaId(value));

        builder.Property(l => l.NumeroLei).HasMaxLength(40).IsRequired();
        builder.Property(l => l.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.LimiteSuplementacaoPercentual).HasColumnType("decimal(9,4)");
        builder.HasIndex(l => new { l.TenantId, l.Exercicio });

        builder.Ignore(l => l.TotalReceitaPrevista);
        builder.Ignore(l => l.TotalDespesaFixada);

        builder.HasMany(l => l.Receitas).WithOne().HasForeignKey(r => r.LoaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.Itens).WithOne().HasForeignKey(i => i.LoaId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.Receitas).AutoInclude();
        builder.Navigation(l => l.Itens).AutoInclude();
        builder.Metadata.FindNavigation(nameof(LeiOrcamentariaAnual.Receitas))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(LeiOrcamentariaAnual.Itens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="ReceitaPrevista"/>.</summary>
public sealed class ReceitaPrevistaConfiguration : IEntityTypeConfiguration<ReceitaPrevista>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReceitaPrevista> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LoaReceitasPrevistas");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new ReceitaPrevistaId(value))
            .ValueGeneratedNever();
        builder.Property(r => r.LoaId).HasConversion(id => id.Value, value => new LoaId(value));
        builder.Property(r => r.FonteDeRecurso).HasMaxLength(20).IsRequired();
        builder.Property(r => r.ValorPrevisto).HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.OwnsOne(r => r.Natureza, n =>
        {
            n.Property(x => x.Categoria).HasColumnName("ReceitaCategoria").HasConversion<string>().HasMaxLength(30).IsRequired();
            n.Property(x => x.Origem).HasColumnName("ReceitaOrigem").HasMaxLength(20).IsRequired();
            n.Property(x => x.Especie).HasColumnName("ReceitaEspecie").HasMaxLength(20).IsRequired();
            n.Property(x => x.Rubrica).HasColumnName("ReceitaRubrica").HasMaxLength(40).IsRequired();
        });
        builder.Navigation(r => r.Natureza).IsRequired();
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="ItemDespesaFixada"/> (QDD).</summary>
public sealed class ItemDespesaFixadaConfiguration : IEntityTypeConfiguration<ItemDespesaFixada>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ItemDespesaFixada> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LoaItensDespesaFixada");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new ItemDespesaFixadaId(value))
            .ValueGeneratedNever();
        builder.Property(i => i.LoaId).HasConversion(id => id.Value, value => new LoaId(value));
        builder.Property(i => i.AcaoPpaId).HasConversion(id => id.Value, value => new AcaoPpaId(value));
        builder.Property(i => i.NaturezaDespesa).HasMaxLength(30).IsRequired();
        builder.Property(i => i.OrigemCreditoEspecial);
        builder.Property(i => i.ValorFixado).HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(i => i.DotacaoId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? null : new DotacaoOrcamentariaId(value.Value));
        builder.Ignore(i => i.DotacaoGerada);

        builder.OwnsOne(i => i.Classificacao, c =>
        {
            c.Property(x => x.Orgao).HasColumnName("Orgao").HasMaxLength(10).IsRequired();
            c.Property(x => x.UnidadeOrcamentaria).HasColumnName("UnidadeOrcamentaria").HasMaxLength(20).IsRequired();
            c.Property(x => x.FuncionalProgramatica).HasColumnName("FuncionalProgramatica").HasMaxLength(50).IsRequired();
            c.Property(x => x.CategoriaEconomica).HasColumnName("CategoriaEconomica").HasConversion<string>().HasMaxLength(30).IsRequired();
            c.Property(x => x.FonteDeRecurso).HasColumnName("FonteDeRecurso").HasMaxLength(20).IsRequired();
        });
        builder.Navigation(i => i.Classificacao).IsRequired();

        builder.HasIndex(i => i.DotacaoId).IsUnique();
    }
}
