using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations.Planejamento;

/// <summary>Mapeamento EF Core do agregado <see cref="PlanoPlurianual"/> (PPA → Programa → Ação → Meta).</summary>
public sealed class PpaConfiguration : IEntityTypeConfiguration<PlanoPlurianual>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoPlurianual> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlanosPlurianuais");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PpaId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.NumeroLei).HasMaxLength(40).IsRequired();
        builder.Property(p => p.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => new { p.TenantId, p.AnoInicio });

        // Programas (entidades-filhas, tabela própria com FK).
        builder.HasMany(p => p.Programas)
            .WithOne()
            .HasForeignKey(prog => prog.PpaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Programas).AutoInclude();
        builder.Metadata.FindNavigation(nameof(PlanoPlurianual.Programas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="Programa"/>.</summary>
public sealed class ProgramaConfiguration : IEntityTypeConfiguration<Programa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Programa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PpaProgramas");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ProgramaId(value))
            .ValueGeneratedNever();
        builder.Property(p => p.PpaId).HasConversion(id => id.Value, value => new PpaId(value));

        builder.Property(p => p.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Nome).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Objetivo).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.PublicoAlvo).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Indicador).HasMaxLength(200).IsRequired();
        builder.Property(p => p.IndicadorLinhaBase).HasColumnType("decimal(18,4)");
        builder.Property(p => p.IndicadorMeta).HasColumnType("decimal(18,4)");

        builder.HasMany(p => p.Acoes)
            .WithOne()
            .HasForeignKey(a => a.ProgramaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Acoes).AutoInclude();
        builder.Metadata.FindNavigation(nameof(Programa.Acoes))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="AcaoPpa"/>.</summary>
public sealed class AcaoPpaConfiguration : IEntityTypeConfiguration<AcaoPpa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AcaoPpa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PpaAcoes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AcaoPpaId(value))
            .ValueGeneratedNever();
        builder.Property(a => a.ProgramaId).HasConversion(id => id.Value, value => new ProgramaId(value));

        builder.Property(a => a.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Nome).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.FuncionalProgramatica).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Produto).HasMaxLength(200).IsRequired();
        builder.Property(a => a.UnidadeMedida).HasMaxLength(30).IsRequired();

        builder.HasMany(a => a.Metas)
            .WithOne()
            .HasForeignKey(m => m.AcaoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Metas).AutoInclude();
        builder.Metadata.FindNavigation(nameof(AcaoPpa.Metas))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="MetaAcao"/>.</summary>
public sealed class MetaAcaoConfiguration : IEntityTypeConfiguration<MetaAcao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MetaAcao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PpaMetas");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MetaAcaoId(value))
            .ValueGeneratedNever();
        builder.Property(m => m.AcaoId).HasConversion(id => id.Value, value => new AcaoPpaId(value));

        builder.Property(m => m.Ano);
        builder.Property(m => m.MetaFisica).HasColumnType("decimal(18,4)");
        builder.Property(m => m.UnidadeMedida).HasMaxLength(30).IsRequired();
        builder.Property(m => m.MetaFinanceira)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v))
            .HasColumnType("decimal(18,2)");
        builder.Property(m => m.Regiao).HasMaxLength(100).IsRequired();
    }
}
