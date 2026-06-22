using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Lancamento"/>.</summary>
public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Lancamentos");
        builder.HasKey(lancamento => lancamento.Id);
        builder.Property(lancamento => lancamento.Id)
            .HasConversion(id => id.Value, value => new LancamentoId(value))
            .ValueGeneratedNever();

        builder.Property(lancamento => lancamento.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(lancamento => lancamento.TipoTributo).HasConversion<string>().HasMaxLength(30);

        builder.Property(lancamento => lancamento.Competencia)
            .HasConversion(competencia => (competencia.Ano * 100) + competencia.Mes, valor => Competencia.De(valor / 100, valor % 100));

        builder.Property(lancamento => lancamento.ValorPrincipal)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(lancamento => lancamento.Situacao).HasConversion<string>().HasMaxLength(30);

        builder.Property(lancamento => lancamento.ImovelId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (ImovelId?)null : new ImovelId(value.Value));

        builder.HasIndex(lancamento => new { lancamento.TenantId, lancamento.ContribuinteId });
        builder.HasIndex(lancamento => lancamento.ImovelId);
    }
}
