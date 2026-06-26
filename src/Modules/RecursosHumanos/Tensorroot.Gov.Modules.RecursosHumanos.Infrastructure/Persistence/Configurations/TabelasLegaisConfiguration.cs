using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core da tabela INSS parametrizada (faixas como entidades-filhas owned).</summary>
public sealed class TabelaInssConfiguration : IEntityTypeConfiguration<TabelaInss>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaInss> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasInss");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TabelaInssId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.VigenciaInicio)
            .HasConversion(
                c => (c.Ano * 100) + c.Mes,
                v => Competencia.De(v / 100, v % 100));

        builder.Property(t => t.Teto).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BaseLegal).HasMaxLength(200).IsRequired();

        builder.OwnsMany(t => t.Faixas, faixas =>
        {
            faixas.ToTable("TabelasInssFaixas");
            faixas.WithOwner().HasForeignKey("TabelaInssId");
            faixas.Property<int>("Id").ValueGeneratedOnAdd();
            faixas.HasKey("Id");
            faixas.Property(f => f.LimiteInferior).HasColumnType("decimal(18,2)");
            faixas.Property(f => f.LimiteSuperior).HasColumnType("decimal(18,2)");
            faixas.Property(f => f.Aliquota).HasColumnType("decimal(9,6)");
        });
        builder.Navigation(t => t.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => new { t.TenantId, t.VigenciaInicio }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core da tabela IRRF parametrizada (faixas owned).</summary>
public sealed class TabelaIrrfConfiguration : IEntityTypeConfiguration<TabelaIrrf>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaIrrf> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasIrrf");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TabelaIrrfId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.VigenciaInicio)
            .HasConversion(
                c => (c.Ano * 100) + c.Mes,
                v => Competencia.De(v / 100, v % 100));

        builder.Property(t => t.DeducaoPorDependente).HasColumnType("decimal(18,2)");
        builder.Property(t => t.DescontoSimplificado).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BaseLegal).HasMaxLength(200).IsRequired();

        builder.OwnsMany(t => t.Faixas, faixas =>
        {
            faixas.ToTable("TabelasIrrfFaixas");
            faixas.WithOwner().HasForeignKey("TabelaIrrfId");
            faixas.Property<int>("Id").ValueGeneratedOnAdd();
            faixas.HasKey("Id");
            faixas.Property(f => f.LimiteSuperior).HasColumnType("decimal(28,2)");
            faixas.Property(f => f.Aliquota).HasColumnType("decimal(9,6)");
            faixas.Property(f => f.ParcelaDeduzir).HasColumnType("decimal(18,2)");
        });
        builder.Navigation(t => t.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Redutor mensal do IRRF (Lei 15.270/2025) — owned opcional: colunas nulas nas competencias sem redutor.
        builder.OwnsOne(t => t.Redutor, redutor =>
        {
            redutor.Property(r => r.CoeficienteBase).HasColumnName("RedutorCoeficienteBase").HasColumnType("decimal(18,2)");
            redutor.Property(r => r.CoeficienteRendimento).HasColumnName("RedutorCoeficienteRendimento").HasColumnType("decimal(9,6)");
            redutor.Property(r => r.TetoRedutor).HasColumnName("RedutorTeto").HasColumnType("decimal(18,2)");
            redutor.Property(r => r.LimiteRendimento).HasColumnName("RedutorLimiteRendimento").HasColumnType("decimal(18,2)");
        });
        builder.Navigation(t => t.Redutor).IsRequired(false);

        builder.HasIndex(t => new { t.TenantId, t.VigenciaInicio }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core da tabela RPPS municipal parametrizada (faixas owned).</summary>
public sealed class TabelaRppsConfiguration : IEntityTypeConfiguration<TabelaRpps>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaRpps> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasRpps");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TabelaRppsId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.VigenciaInicio)
            .HasConversion(
                c => (c.Ano * 100) + c.Mes,
                v => Competencia.De(v / 100, v % 100));

        builder.Property(t => t.Teto).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BaseLegal).HasMaxLength(200).IsRequired();

        builder.OwnsMany(t => t.Faixas, faixas =>
        {
            faixas.ToTable("TabelasRppsFaixas");
            faixas.WithOwner().HasForeignKey("TabelaRppsId");
            faixas.Property<int>("Id").ValueGeneratedOnAdd();
            faixas.HasKey("Id");
            faixas.Property(f => f.LimiteInferior).HasColumnType("decimal(18,2)");
            faixas.Property(f => f.LimiteSuperior).HasColumnType("decimal(18,2)");
            faixas.Property(f => f.Aliquota).HasColumnType("decimal(9,6)");
        });
        builder.Navigation(t => t.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => new { t.TenantId, t.VigenciaInicio }).IsUnique();
    }
}
