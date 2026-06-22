using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Dam"/> (guia/carnê) e suas parcelas.</summary>
public sealed class DamConfiguration : IEntityTypeConfiguration<Dam>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Dam> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Dams");
        builder.HasKey(dam => dam.Id);
        builder.Property(dam => dam.Id)
            .HasConversion(id => id.Value, value => new DamId(value))
            .ValueGeneratedNever();

        builder.Property(dam => dam.LancamentoId)
            .HasConversion(id => id.Value, value => new LancamentoId(value));
        builder.Property(dam => dam.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(dam => dam.ValorTotal)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.HasMany(dam => dam.Parcelas).WithOne().HasForeignKey(p => p.DamId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(dam => dam.Parcelas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(dam => new { dam.TenantId, dam.LancamentoId });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="Parcela"/>.</summary>
public sealed class ParcelaConfiguration : IEntityTypeConfiguration<Parcela>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Parcela> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Parcelas");
        builder.HasKey(parcela => parcela.Id);
        builder.Property(parcela => parcela.Id)
            .HasConversion(id => id.Value, value => new ParcelaId(value))
            .ValueGeneratedNever();

        builder.Property(parcela => parcela.DamId)
            .HasConversion(id => id.Value, value => new DamId(value));

        builder.Property(parcela => parcela.Numero);
        builder.Property(parcela => parcela.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(parcela => parcela.Vencimento);
        builder.Property(parcela => parcela.Paga);
        builder.Property(parcela => parcela.DataPagamento);

        builder.HasIndex(parcela => parcela.DamId);
    }
}
