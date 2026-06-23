using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations.Planejamento;

/// <summary>Mapeamento EF Core do agregado <see cref="LeiDiretrizes"/> (LDO).</summary>
public sealed class LdoConfiguration : IEntityTypeConfiguration<LeiDiretrizes>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeiDiretrizes> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LeisDiretrizes");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => new LdoId(value))
            .ValueGeneratedNever();
        builder.Property(l => l.PpaId).HasConversion(id => id.Value, value => new PpaId(value));

        builder.Property(l => l.NumeroLei).HasMaxLength(40).IsRequired();
        builder.Property(l => l.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(l => new { l.TenantId, l.Exercicio });

        builder.HasMany(l => l.Prioridades).WithOne().HasForeignKey(p => p.LdoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.MetasFiscais).WithOne().HasForeignKey(m => m.LdoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.Anexos).WithOne().HasForeignKey(a => a.LdoId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Prioridades).AutoInclude();
        builder.Navigation(l => l.MetasFiscais).AutoInclude();
        builder.Navigation(l => l.Anexos).AutoInclude();
        builder.Metadata.FindNavigation(nameof(LeiDiretrizes.Prioridades))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(LeiDiretrizes.MetasFiscais))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(LeiDiretrizes.Anexos))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="PrioridadeLdo"/>.</summary>
public sealed class PrioridadeLdoConfiguration : IEntityTypeConfiguration<PrioridadeLdo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PrioridadeLdo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LdoPrioridades");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PrioridadeLdoId(value))
            .ValueGeneratedNever();
        builder.Property(p => p.LdoId).HasConversion(id => id.Value, value => new LdoId(value));
        builder.Property(p => p.AcaoPpaId).HasConversion(id => id.Value, value => new AcaoPpaId(value));
        builder.Property(p => p.Justificativa).HasMaxLength(1000).IsRequired();
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="MetaFiscal"/>.</summary>
public sealed class MetaFiscalConfiguration : IEntityTypeConfiguration<MetaFiscal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MetaFiscal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LdoMetasFiscais");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MetaFiscalId(value))
            .ValueGeneratedNever();
        builder.Property(m => m.LdoId).HasConversion(id => id.Value, value => new LdoId(value));

        builder.Property(m => m.ReceitaTotal).HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(m => m.DespesaTotal).HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(m => m.DividaConsolidada).HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(m => m.ResultadoPrimario).HasColumnType("decimal(18,2)");
        builder.Property(m => m.ResultadoNominal).HasColumnType("decimal(18,2)");
    }
}

/// <summary>Mapeamento EF Core da entidade-filha <see cref="AnexoLdo"/>.</summary>
public sealed class AnexoLdoConfiguration : IEntityTypeConfiguration<AnexoLdo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AnexoLdo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("LdoAnexos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AnexoLdoId(value))
            .ValueGeneratedNever();
        builder.Property(a => a.LdoId).HasConversion(id => id.Value, value => new LdoId(value));
        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.ReferenciaDocumento).HasMaxLength(200).IsRequired();
    }
}
